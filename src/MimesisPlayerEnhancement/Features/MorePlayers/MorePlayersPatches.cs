using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using FishySteamworks.Server;
using HarmonyLib;
using MimesisPlayerEnhancement.Util;
using ReluProtocol.Enum;

// Patch logic derived from NeoMimicry/MorePlayers; networking helpers from MimicAPI are inlined in
// Util/GameNetworkApi.cs (see upstream: https://github.com/NeoMimicry/MorePlayers ,
// https://github.com/NeoMimicry/MimicAPI/tree/main/MimicAPI/GameAPI ).
namespace MimesisPlayerEnhancement.Features.MorePlayers;

public static class MorePlayersPatches
{
    private const string Feature = "MorePlayers";
    private const int VanillaMaxPlayers = 4;

    private static readonly MethodInfo GetMaxPlayersMethod =
        AccessTools.Method(typeof(MorePlayersPatches), nameof(GetMaxPlayers))!;

    public static void Apply(HarmonyLib.Harmony harmony)
    {
        harmony.CreateClassProcessor(typeof(MorePlayersPatches)).Patch();
        LogPatchAudit(harmony);
        ModLog.Info(Feature, "Patches applied.");
    }

    private static void LogPatchAudit(HarmonyLib.Harmony harmony)
    {
        var patched = new HashSet<MethodBase>(harmony.GetPatchedMethods());
        var applied = new List<string>();
        var missing = new List<string>();

        void Check(string label, MethodBase? method)
        {
            if (method == null)
                missing.Add($"{label} (type/method not found)");
            else if (patched.Contains(method))
                applied.Add(label);
            else
                missing.Add(label);
        }

        Check("CanEnterChannel/IVroom", AccessTools.DeclaredMethod(typeof(IVroom), "CanEnterChannel"));
        Check("CanEnterChannel/VWaitingRoom", AccessTools.DeclaredMethod(typeof(VWaitingRoom), "CanEnterChannel"));
        Check("CanEnterChannel/MaintenanceRoom", AccessTools.DeclaredMethod(typeof(MaintenanceRoom), "CanEnterChannel"));
        Check("GetMemberCount/IVroom", AccessTools.DeclaredMethod(typeof(IVroom), "GetMemberCount"));
        Check("GetMaximumClients/ServerSocket", AccessTools.Method(typeof(ServerSocket), "GetMaximumClients"));
        Check("SetMaximumClients/ServerSocket", AccessTools.Method(typeof(ServerSocket), "SetMaximumClients"));
        Check("ServerSocket.ctor", AccessTools.Constructor(typeof(ServerSocket)));
        Check("AddPlayerSteamID/GameSessionInfo", AccessTools.Method(typeof(GameSessionInfo), "AddPlayerSteamID"));
        Check("CreateLobby/SteamInviteDispatcher", AccessTools.Method(typeof(SteamInviteDispatcher), "CreateLobby"));
        Check("EnterWaitingRoom/VRoomManager", AccessTools.Method(typeof(VRoomManager), "EnterWaitingRoom"));
        Check("EnterMaintenenceRoom/VRoomManager", AccessTools.Method(typeof(VRoomManager), "EnterMaintenenceRoom"));

        if (applied.Count > 0)
            ModLog.Info(Feature, $"Patch audit — applied: {string.Join(", ", applied)}");

        foreach (string label in missing)
            ModLog.Warn(Feature, $"Patch audit — not applied: {label}");
    }

    /// <summary>Re-applies player-cap limits to live networking state after config changes.</summary>
    public static void RefreshFromConfig()
    {
        if (!ModConfig.EnableMorePlayers.Value)
            return;

        try
        {
            var socket = GameNetworkApi.GetServerSocket();
            if (socket != null)
            {
                GameNetworkApi.SetMaximumClients(socket, MaxClientConnections);
                ModLog.Debug(
                    Feature,
                    $"Server socket max clients refreshed to {MaxClientConnections} (session cap {MaxPlayers} including host).");
            }
        }
        catch (Exception ex)
        {
            ModLog.Warn(Feature, $"Server socket refresh: {ex.Message}");
        }

        try
        {
            UpdateRoomMaxPlayers(GameNetworkApi.GetVRoomManager());
        }
        catch (Exception ex)
        {
            ModLog.Warn(Feature, $"Room max players refresh: {ex.Message}");
        }
    }

    private static void UpdateRoomMaxPlayers(object? vroomManager)
    {
        if (vroomManager == null)
            return;

        var vrooms = ReflectionHelper.GetFieldValue(vroomManager, "_vrooms") as IDictionary;
        if (vrooms == null)
            return;

        foreach (var room in vrooms.Values)
        {
            if (room == null)
                continue;

            var maxPlayersField = room.GetType().BaseType?.GetField("_maxPlayers", BindingFlags.NonPublic | BindingFlags.Instance);
            maxPlayersField?.SetValue(room, MaxPlayers);
            ModLog.Debug(Feature, $"Room {room.GetType().Name} _maxPlayers refreshed to {MaxPlayers}.");
        }
    }

    /// <summary>Called from transpiled game IL — must not bake config in at patch time.</summary>
    public static int GetMaxPlayers() =>
        ModConfig.EnableMorePlayers.Value ? ModConfig.MaxPlayers.Value : VanillaMaxPlayers;

    internal static int MaxPlayers => GetMaxPlayers();

    /// <summary>Remote client slots; the host always occupies one player slot.</summary>
    internal static int MaxClientConnections => Math.Max(0, MaxPlayers - 1);

    private static int TotalPlayersInRoom(IDictionary? vPlayerDict) =>
        1 + (vPlayerDict?.Count ?? 0);

    private static CodeInstruction LoadMaxPlayers() =>
        new(OpCodes.Call, GetMaxPlayersMethod);

    private static bool TryHandleCanEnterChannel(ref MsgErrorCode __result, IVroom __instance, long playerUID)
    {
        if (!ModConfig.EnableMorePlayers.Value)
            return true;

        try
        {
            var vPlayerDict = ReflectionHelper.GetFieldValue(__instance, "_vPlayerDict") as IDictionary;
            if (vPlayerDict != null)
            {
                foreach (var player in vPlayerDict.Values)
                {
                    if (player == null)
                        continue;

                    var uid = ReflectionHelper.GetFieldValue<long>(player, "UID");
                    if (uid == 0)
                    {
                        var uidProp = player.GetType().GetProperty("UID", BindingFlags.Public | BindingFlags.Instance);
                        if (uidProp != null)
                            uid = Convert.ToInt64(uidProp.GetValue(player));
                    }

                    if (uid == playerUID)
                    {
                        __result = MsgErrorCode.DuplicatePlayer;
                        ModLog.Info(
                            Feature,
                            $"Join denied — duplicate player uid={playerUID} in {__instance.GetType().Name}.");
                        return false;
                    }
                }

                int connectedClients = vPlayerDict.Count;
                if (connectedClients >= MaxClientConnections)
                {
                    __result = MsgErrorCode.PlayerCountExceeded;
                    ModLog.Info(
                        Feature,
                        $"Join denied — session full ({TotalPlayersInRoom(vPlayerDict)}/{MaxPlayers} players) in {__instance.GetType().Name}, uid={playerUID}.");
                    return false;
                }
            }
            else if (MaxClientConnections == 0)
            {
                __result = MsgErrorCode.PlayerCountExceeded;
                ModLog.Info(
                    Feature,
                    $"Join denied — solo session (max {MaxPlayers} player) in {__instance.GetType().Name}, uid={playerUID}.");
                return false;
            }

            __result = MsgErrorCode.Success;
            int totalAfterJoin = TotalPlayersInRoom(vPlayerDict) + 1;
            ModLog.Info(
                Feature,
                $"Join allowed — uid={playerUID} in {__instance.GetType().Name} ({totalAfterJoin}/{MaxPlayers} players).");
            return false;
        }
        catch (Exception ex)
        {
            ModLog.Warn(Feature, $"CanEnterChannel patch error: {ex.Message}");
            return true;
        }
    }

    private static void VRoomManagerEnterRoomPrefix(VRoomManager __instance, MethodBase __originalMethod)
    {
        if (!ModConfig.EnableMorePlayers.Value)
            return;

        try
        {
            UpdateRoomMaxPlayers(__instance);
            ModLog.Info(
                Feature,
                $"Enter room — {__originalMethod.Name} (session cap {MaxPlayers}, client slots {MaxClientConnections}).");
        }
        catch (Exception ex)
        {
            ModLog.Warn(Feature, $"VRoomManager enter room patch error: {ex.Message}");
        }
    }

    [HarmonyPatch(typeof(IVroom), "CanEnterChannel")]
    public static class IVroomCanEnterChannelTranspiler
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Ldfld
                    && codes[i].operand is FieldInfo field
                    && field.Name == "C_MaxPlayerCount")
                {
                    codes[i] = new CodeInstruction(OpCodes.Pop);
                    codes.Insert(i + 1, LoadMaxPlayers());
                    break;
                }
            }

            return codes;
        }
    }

    [HarmonyPatch(typeof(IVroom), "CanEnterChannel")]
    [HarmonyPatch(typeof(VWaitingRoom), "CanEnterChannel")]
    [HarmonyPatch(typeof(MaintenanceRoom), "CanEnterChannel")]
    public static class AllRoomsCanEnterChannelPatch
    {
        static bool Prefix(ref MsgErrorCode __result, IVroom __instance, long playerUID) =>
            TryHandleCanEnterChannel(ref __result, __instance, playerUID);
    }

    [HarmonyPatch(typeof(ServerSocket), "GetMaximumClients")]
    public static class GetMaximumClientsPatch
    {
        static bool Prefix(ref int __result)
        {
            if (!ModConfig.EnableMorePlayers.Value)
                return true;

            __result = MaxClientConnections;
            return false;
        }
    }

    [HarmonyPatch(typeof(ServerSocket), "SetMaximumClients")]
    public static class SetMaximumClientsPatch
    {
        static bool Prefix(ref int value)
        {
            if (!ModConfig.EnableMorePlayers.Value)
                return true;

            value = MaxClientConnections;
            return true;
        }
    }

    [HarmonyPatch(typeof(ServerSocket), MethodType.Constructor)]
    public static class ServerSocketConstructorPatch
    {
        static void Postfix(ServerSocket __instance)
        {
            if (!ModConfig.EnableMorePlayers.Value)
                return;

            try
            {
                GameNetworkApi.SetMaximumClients(__instance, MaxClientConnections);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"Server socket ctor postfix failed: {ex.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(IVroom), "GetMemberCount")]
    public static class GetMemberCountSmartPatch
    {
        static bool Prefix(ref int __result, object __instance)
        {
            if (!ModConfig.EnableMorePlayers.Value)
                return true;

            try
            {
                int actualCount = GameNetworkApi.GetRoomPlayerCount(__instance);

                var stackTrace = new System.Diagnostics.StackTrace();
                bool isFromEnterCheck = false;
                bool isFromSessionCount = false;

                for (int i = 0; i < Math.Min(stackTrace.FrameCount, 10); i++)
                {
                    var frame = stackTrace.GetFrame(i);
                    var method = frame?.GetMethod();
                    if (method == null)
                        continue;

                    string methodName = method.Name;
                    if (methodName.Contains("EnterWaitingRoom")
                        || methodName.Contains("EnterMaintenenceRoom")
                        || methodName.Contains("EnterMaintenanceRoom")
                        || methodName.Contains("CanEnter"))
                    {
                        isFromEnterCheck = true;
                        break;
                    }

                    if (methodName.Contains("GetSessionCount") || methodName.Contains("GetRoomMemberCount"))
                    {
                        isFromSessionCount = true;
                        break;
                    }
                }

                if (isFromEnterCheck)
                {
                    __result = 0;
                    return false;
                }

                if (isFromSessionCount)
                {
                    __result = actualCount;
                    return false;
                }

                __result = actualCount;
                return false;
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"GetMemberCount patch error: {ex.Message}");
                return true;
            }
        }
    }

    [HarmonyPatch(typeof(VRoomManager), "EnterWaitingRoom")]
    [HarmonyPatch(typeof(VRoomManager), "EnterMaintenenceRoom")]
    public static class VRoomManagerEnterRoomPatches
    {
        static void Prefix(VRoomManager __instance, MethodBase __originalMethod) =>
            VRoomManagerEnterRoomPrefix(__instance, __originalMethod);
    }

    [HarmonyPatch(typeof(GameSessionInfo), "AddPlayerSteamID")]
    public static class GameSessionInfoAddPlayerSteamIdPatch
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Ldc_I4_4)
                {
                    codes[i] = LoadMaxPlayers();
                }
                else if (codes[i].opcode == OpCodes.Ldfld
                         && codes[i].operand is FieldInfo field
                         && field.Name == "C_MaxPlayerCount")
                {
                    codes[i] = new CodeInstruction(OpCodes.Pop);
                    codes.Insert(i + 1, LoadMaxPlayers());
                    i++;
                }
            }

            return codes;
        }
    }

    [HarmonyPatch(typeof(SteamInviteDispatcher), "CreateLobby")]
    public static class SteamLobbyCreationPatch
    {
        static bool Prefix(bool isOpenForRandomMatch)
        {
            if (!ModConfig.EnableMorePlayers.Value)
                return true;

            try
            {
                var steamMatchmakingType = Type.GetType("Steamworks.SteamMatchmaking, com.rlabrecque.steamworks.net");
                var eLobbyTypeType = Type.GetType("Steamworks.ELobbyType, com.rlabrecque.steamworks.net");
                var playerPrefsType = Type.GetType("UnityEngine.PlayerPrefs, UnityEngine.CoreModule");

                if (steamMatchmakingType == null || eLobbyTypeType == null || playerPrefsType == null)
                {
                    ModLog.Warn(Feature, "Steam lobby creation skipped — Steamworks or PlayerPrefs types not found.");
                    return true;
                }

                var createLobbyMethod = steamMatchmakingType.GetMethod("CreateLobby", BindingFlags.Public | BindingFlags.Static);
                var setIntMethod = playerPrefsType.GetMethod("SetInt", BindingFlags.Public | BindingFlags.Static);
                if (createLobbyMethod == null || setIntMethod == null)
                {
                    ModLog.Warn(Feature, "Steam lobby creation skipped — CreateLobby or PlayerPrefs.SetInt not found.");
                    return true;
                }

                var friendsOnly = Enum.ToObject(eLobbyTypeType, 2);
                createLobbyMethod.Invoke(null, new object[] { friendsOnly, MaxPlayers });
                setIntMethod.Invoke(null, new object[] { "TempLobbyIsOpen", isOpenForRandomMatch ? 1 : 0 });
                ModLog.Info(
                    Feature,
                    $"Steam lobby created — maxPlayers={MaxPlayers}, openForMatchmaking={isOpenForRandomMatch}.");
                return false;
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"Steam lobby creation patch error: {ex.Message}");
                return true;
            }
        }
    }
}

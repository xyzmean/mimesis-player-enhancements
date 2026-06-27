using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using MimesisPlayerEnhancement.Util;

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
        ModLog.Info(Feature, "Patches applied.");
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

    [HarmonyPatch]
    public static class IVroomCanEnterChannelTranspiler
    {
        static IEnumerable<MethodBase> TargetMethods()
        {
            var method = GameNetworkApi.GetIVroomType()?.GetMethod(
                "CanEnterChannel",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            return method != null ? new[] { method } : Array.Empty<MethodBase>();
        }

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

    [HarmonyPatch]
    public static class GetMaximumClientsPatch
    {
        static IEnumerable<MethodBase> TargetMethods() => FindSocketMethods("GetMaximumClients");

        static bool Prefix(ref int __result)
        {
            if (!ModConfig.EnableMorePlayers.Value)
                return true;

            __result = MaxClientConnections;
            return false;
        }
    }

    [HarmonyPatch]
    public static class SetMaximumClientsPatch
    {
        static IEnumerable<MethodBase> TargetMethods() => FindSocketMethods("SetMaximumClients");

        static bool Prefix(ref int value)
        {
            if (!ModConfig.EnableMorePlayers.Value)
                return true;

            value = MaxClientConnections;
            return true;
        }
    }

    private static IEnumerable<MethodBase> FindSocketMethods(string methodName)
    {
        var assembly = GameNetworkApi.GetGameAssembly();
        if (assembly == null)
            yield break;

        foreach (var typeName in new[] { "FishySteamworks.Server.ServerSocket", "FishyNet.Transporting.Server.ServerSocket" })
        {
            var type = assembly.GetType(typeName);
            var method = type?.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (method != null)
                yield return method;
        }
    }

    [HarmonyPatch]
    public static class ServerSocketConstructorPatch
    {
        static MethodBase? TargetMethod()
        {
            var assembly = GameNetworkApi.GetGameAssembly();
            foreach (var typeName in new[] { "FishySteamworks.Server.ServerSocket", "FishyNet.Transporting.Server.ServerSocket" })
            {
                var type = assembly?.GetType(typeName);
                var ctor = type?.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).FirstOrDefault();
                if (ctor != null)
                    return ctor;
            }

            return null;
        }

        static void Postfix(object __instance)
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

    [HarmonyPatch]
    public static class GetMemberCountSmartPatch
    {
        static IEnumerable<MethodBase> TargetMethods()
        {
            var method = GameNetworkApi.GetIVroomType()?.GetMethod(
                "GetMemberCount",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            return method != null ? new[] { method } : Array.Empty<MethodBase>();
        }

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

    [HarmonyPatch]
    public static class AllRoomsCanEnterChannelPatch
    {
        static IEnumerable<MethodBase> TargetMethods()
        {
            var assembly = GameNetworkApi.GetGameAssembly();
            if (assembly == null)
                return Array.Empty<MethodBase>();

            var methods = new List<MethodBase>();
            foreach (var typeName in new[] { "VWaitingRoom", "MaintenanceRoom", "IVroom" })
            {
                var type = assembly.GetType(typeName);
                var method = type?.GetMethod("CanEnterChannel", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (method != null)
                    methods.Add(method);
            }

            return methods;
        }

        static bool Prefix(ref object __result, object __instance, long playerUID)
        {
            if (!ModConfig.EnableMorePlayers.Value)
                return true;

            try
            {
                var msgErrorCodeType = GameNetworkApi.GetGameAssembly()?.GetTypes().FirstOrDefault(t => t.Name == "MsgErrorCode");
                if (msgErrorCodeType == null || !msgErrorCodeType.IsEnum)
                    return true;

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
                            __result = Enum.Parse(msgErrorCodeType, "DuplicatePlayer");
                            ModLog.Info(
                                Feature,
                                $"Join denied — duplicate player uid={playerUID} in {__instance.GetType().Name}.");
                            return false;
                        }
                    }

                    int connectedClients = vPlayerDict.Count;
                    if (connectedClients >= MaxClientConnections)
                    {
                        __result = Enum.Parse(msgErrorCodeType, "PlayerCountExceeded");
                        ModLog.Info(
                            Feature,
                            $"Join denied — session full ({TotalPlayersInRoom(vPlayerDict)}/{MaxPlayers} players) in {__instance.GetType().Name}, uid={playerUID}.");
                        return false;
                    }
                }
                else if (MaxClientConnections == 0)
                {
                    __result = Enum.Parse(msgErrorCodeType, "PlayerCountExceeded");
                    ModLog.Info(
                        Feature,
                        $"Join denied — solo session (max {MaxPlayers} player) in {__instance.GetType().Name}, uid={playerUID}.");
                    return false;
                }

                __result = Enum.Parse(msgErrorCodeType, "Success");
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
    }

    [HarmonyPatch]
    public static class VRoomManagerEnterRoomPatches
    {
        static IEnumerable<MethodBase> TargetMethods()
        {
            var vroomManagerType = GameNetworkApi.GetGameAssembly()?.GetType("VRoomManager");
            if (vroomManagerType == null)
                return Array.Empty<MethodBase>();

            var methods = new List<MethodBase>();
            foreach (var name in new[] { "EnterWaitingRoom", "EnterMaintenenceRoom", "EnterMaintenanceRoom" })
            {
                var method = vroomManagerType.GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (method != null)
                    methods.Add(method);
            }

            return methods;
        }

        static void Prefix(object __instance)
        {
            if (!ModConfig.EnableMorePlayers.Value)
                return;

            try
            {
                UpdateRoomMaxPlayers(__instance);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"VRoomManager enter room patch error: {ex.Message}");
            }
        }
    }

    [HarmonyPatch]
    public static class GameSessionInfoAddPlayerSteamIdPatch
    {
        static MethodBase? TargetMethod()
        {
            return GameNetworkApi.GetGameSessionInfoType()?.GetMethod(
                "AddPlayerSteamID",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        }

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

    [HarmonyPatch]
    public static class SteamLobbyCreationPatch
    {
        static MethodBase? TargetMethod()
        {
            var assembly = GameNetworkApi.GetGameAssembly();
            return assembly?.GetTypes().FirstOrDefault(t => t.Name == "SteamInviteDispatcher")
                ?.GetMethod("CreateLobby", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        }

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
                    return true;

                var createLobbyMethod = steamMatchmakingType.GetMethod("CreateLobby", BindingFlags.Public | BindingFlags.Static);
                var setIntMethod = playerPrefsType.GetMethod("SetInt", BindingFlags.Public | BindingFlags.Static);
                if (createLobbyMethod == null || setIntMethod == null)
                    return true;

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

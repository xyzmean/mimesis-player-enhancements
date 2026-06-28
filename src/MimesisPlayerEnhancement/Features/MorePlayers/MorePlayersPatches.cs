using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using MimesisPlayerEnhancement.Util;

// Patch logic derived from NeoMimicry/MorePlayers; networking helpers from MimicAPI are inlined in
// Util/GameNetworkApi.cs (see upstream: https://github.com/NeoMimicry/MorePlayers ,
// https://github.com/NeoMimicry/MimicAPI/tree/main/MimicAPI/GameAPI ).
namespace MimesisPlayerEnhancement.Features.MorePlayers
{
    public static class MorePlayersPatches
    {
        private const string Feature = "MorePlayers";
        private const int VanillaMaxPlayers = 4;

        private const BindingFlags InstanceMethodFlags =
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

        private static readonly MethodInfo GetMaxPlayersMethod =
            AccessTools.Method(typeof(MorePlayersPatches), nameof(GetMaxPlayers));

        private static readonly MethodInfo GetLobbyPlayerCountSuffixMethod =
            AccessTools.Method(typeof(MorePlayersPatches), nameof(GetLobbyPlayerCountSuffix));

        public static void Apply(HarmonyLib.Harmony harmony)
        {
            _ = GameNetworkApi.GetGameAssembly();

            HarmonyPatchHelper.PatchApplyResult result = HarmonyPatchHelper.ApplyPatchTypes(
                harmony,
                Feature,
                HarmonyPatchHelper.GetNestedPatchTypes(typeof(MorePlayersPatches)));

            LogPatchAudit(harmony);
            HarmonyPatchHelper.LogPatchSummary(Feature, result);
        }

        private static void LogPatchAudit(HarmonyLib.Harmony harmony)
        {
            List<(string label, MethodBase? method)> checks = [];

            void Check(string label, MethodBase? method) => checks.Add((label, method));

            Check("CanEnterChannel/IVroom", ResolveRoomMethod("IVroom", "CanEnterChannel"));
            Check("CanEnterChannel/VWaitingRoom", ResolveRoomMethod("VWaitingRoom", "CanEnterChannel"));
            Check("CanEnterChannel/MaintenanceRoom", ResolveRoomMethod("MaintenanceRoom", "CanEnterChannel"));
            Check("GetMemberCount/IVroom", GameNetworkApi.GetIVroomType()?.GetMethod("GetMemberCount", InstanceMethodFlags));

            foreach (MethodBase method in FindSocketMethods("GetMaximumClients"))
            {
                Check("GetMaximumClients/ServerSocket", method);
            }

            foreach (MethodBase method in FindSocketMethods("SetMaximumClients"))
            {
                Check("SetMaximumClients/ServerSocket", method);
            }

            Check("ServerSocket.ctor", ResolveServerSocketConstructor());
            Check("AddPlayerSteamID/GameSessionInfo", ResolveGameSessionInfoMethod("AddPlayerSteamID"));
            Check("CreateLobby/SteamInviteDispatcher", ResolveSteamInviteDispatcherMethod("CreateLobby"));
            Check("UpdatePlayerGroupSize/SteamInviteDispatcher", ResolveSteamInviteDispatcherMethod("UpdatePlayerGroupSize"));
            Check("SetRoomList/UIPrefab_PublicRoomList", ResolveGameTypeMethod("UIPrefab_PublicRoomList", "SetRoomList"));
            Check("SetRoomData/UiPrefab_RoomCard", ResolveGameTypeMethod("UiPrefab_RoomCard", "SetRoomData"));
            Check("EnterWaitingRoom/VRoomManager", ResolveVRoomManagerMethod("EnterWaitingRoom"));
            Check("EnterMaintenenceRoom/VRoomManager", ResolveVRoomManagerMethod("EnterMaintenenceRoom"));

            HarmonyPatchHelper.LogPatchAudit(Feature, harmony, checks);
        }

        /// <summary>Re-applies player-cap limits to live networking state after config changes.</summary>
        public static void RefreshFromConfig()
        {
            int cap = ModConfig.EnableMorePlayers.Value ? MaxClientConnections : 4;

            try
            {
                var socket = GameNetworkApi.GetServerSocket();
                if (socket != null)
                {
                    GameNetworkApi.SetMaximumClients(socket, cap);
                    ModLog.Debug(
                        Feature,
                        $"Server socket max clients refreshed to {cap} (session cap {(ModConfig.EnableMorePlayers.Value ? MaxPlayers : 4)}).");
                }
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"Server socket refresh: {ex.Message}");
            }

            try
            {
                UpdateRoomMaxPlayers(GameNetworkApi.GetVRoomManager(), logRefresh: true);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"Room max players refresh: {ex.Message}");
            }
        }

        private static void UpdateRoomMaxPlayers(object? vroomManager, bool logRefresh = false)
        {
            if (vroomManager == null)
            {
                return;
            }

            if (ReflectionHelper.GetFieldValue(vroomManager, "_vrooms") is not IDictionary vrooms)
            {
                return;
            }

            foreach (var room in vrooms.Values)
            {
                if (room == null)
                {
                    continue;
                }

                Type roomType = room.GetType();
                while (roomType != null)
                {
                    FieldInfo maxPlayersField = roomType.GetField("_maxPlayers", BindingFlags.NonPublic | BindingFlags.Instance);
                    if (maxPlayersField != null)
                    {
                        int cap = ModConfig.EnableMorePlayers.Value ? MaxPlayers : VanillaMaxPlayers;
                        maxPlayersField.SetValue(room, cap);
                        if (logRefresh)
                        {
                            ModLog.Debug(Feature, $"Room {room.GetType().Name} max players refreshed to {cap}.");
                        }

                        break;
                    }

                    roomType = roomType.BaseType;
                }
            }
        }

        /// <summary>Called from transpiled game IL — must not bake config in at patch time.</summary>
        public static int GetMaxPlayers()
        {
            return ModConfig.EnableMorePlayers.Value ? ModConfig.MaxPlayers.Value : VanillaMaxPlayers;
        }

        /// <summary>Called from transpiled UI IL for room list player count labels (e.g. "3/32").</summary>
        public static string GetLobbyPlayerCountSuffix()
        {
            return "/" + GetMaxPlayers();
        }

        internal static int MaxPlayers => GetMaxPlayers();

        internal static int MaxClientConnections => MaxPlayers;

        private static int PlayersInRoom(IDictionary? vPlayerDict)
        {
            return vPlayerDict?.Count ?? 0;
        }

        private static CodeInstruction LoadMaxPlayers()
        {
            return new(OpCodes.Call, GetMaxPlayersMethod);
        }

        private static IEnumerable<CodeInstruction> ReplaceVanillaLobbyCap(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = [.. instructions];
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Ldc_I4_4)
                {
                    codes[i] = LoadMaxPlayers();
                }
            }

            return codes;
        }

        private static IEnumerable<CodeInstruction> ReplaceLobbyPlayerCountSuffix(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = [.. instructions];
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Ldstr && codes[i].operand is string literal && literal == "/4")
                {
                    codes[i] = new CodeInstruction(OpCodes.Call, GetLobbyPlayerCountSuffixMethod);
                }
            }

            return codes;
        }

        private static MethodBase? ResolveGameTypeMethod(string typeName, string methodName)
        {
            return GameNetworkApi.GetGameAssembly()?.GetType(typeName)?.GetMethod(methodName, InstanceMethodFlags);
        }

        private static MethodBase? ResolveRoomMethod(string typeName, string methodName)
        {
            return GameNetworkApi.GetGameAssembly()?.GetType(typeName)?.GetMethod(methodName, InstanceMethodFlags);
        }

        private static MethodBase? ResolveGameSessionInfoMethod(string methodName)
        {
            return GameNetworkApi.GetGameSessionInfoType()?.GetMethod(methodName, InstanceMethodFlags);
        }

        private static MethodBase? ResolveVRoomManagerMethod(string methodName)
        {
            return GameNetworkApi.GetGameAssembly()?.GetType("VRoomManager")?.GetMethod(methodName, InstanceMethodFlags);
        }

        private static MethodBase? ResolveSteamInviteDispatcherMethod(string methodName)
        {
            return GameNetworkApi.GetGameAssembly()?.GetTypes().FirstOrDefault(t => t.Name == "SteamInviteDispatcher")
                ?.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        }

        private static MethodBase? ResolveServerSocketConstructor()
        {
            Assembly? assembly = GameNetworkApi.GetGameAssembly();
            foreach (var typeName in new[] { "FishySteamworks.Server.ServerSocket", "FishyNet.Transporting.Server.ServerSocket" })
            {
                Type? type = assembly?.GetType(typeName);
                ConstructorInfo? ctor = type?.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).FirstOrDefault();
                if (ctor != null)
                {
                    return ctor;
                }
            }

            return null;
        }

        private static IEnumerable<MethodBase> FindSocketMethods(string methodName)
        {
            Assembly? assembly = GameNetworkApi.GetGameAssembly();
            if (assembly == null)
            {
                yield break;
            }

            foreach (var typeName in new[] { "FishySteamworks.Server.ServerSocket", "FishyNet.Transporting.Server.ServerSocket" })
            {
                Type type = assembly.GetType(typeName);
                MethodInfo? method = type?.GetMethod(methodName, InstanceMethodFlags);
                if (method != null)
                {
                    yield return method;
                }
            }
        }

        [HarmonyPatch]
        public static class IVroomCanEnterChannelTranspiler
        {
            public static IEnumerable<MethodBase> TargetMethods()
            {
                MethodInfo? method = GameNetworkApi.GetIVroomType()?.GetMethod("CanEnterChannel", InstanceMethodFlags);
                return method != null ? [method] : Array.Empty<MethodBase>();
            }

            [HarmonyTranspiler]
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                List<CodeInstruction> codes = [.. instructions];
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
            public static IEnumerable<MethodBase> TargetMethods()
            {
                return FindSocketMethods("GetMaximumClients");
            }

            [HarmonyPrefix]
            public static bool Prefix(ref int __result)
            {
                if (!ModConfig.EnableMorePlayers.Value)
                {
                    return true;
                }

                __result = MaxClientConnections;
                return false;
            }
        }

        [HarmonyPatch]
        public static class SetMaximumClientsPatch
        {
            public static IEnumerable<MethodBase> TargetMethods()
            {
                return FindSocketMethods("SetMaximumClients");
            }

            [HarmonyPrefix]
            public static bool Prefix(ref int value)
            {
                if (!ModConfig.EnableMorePlayers.Value)
                {
                    return true;
                }

                value = MaxClientConnections;
                return true;
            }
        }

        [HarmonyPatch]
        public static class ServerSocketConstructorPatch
        {
            public static MethodBase? TargetMethod()
            {
                return ResolveServerSocketConstructor();
            }

            [HarmonyPostfix]
            public static void Postfix(object __instance)
            {
                if (!ModConfig.EnableMorePlayers.Value)
                {
                    return;
                }

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
            public static IEnumerable<MethodBase> TargetMethods()
            {
                MethodInfo? method = GameNetworkApi.GetIVroomType()?.GetMethod("GetMemberCount", InstanceMethodFlags);
                return method != null ? [method] : Array.Empty<MethodBase>();
            }

            [HarmonyPrefix]
            public static bool Prefix(ref int __result, object __instance)
            {
                if (!ModConfig.EnableMorePlayers.Value)
                {
                    return true;
                }

                try
                {
                    int actualCount = GameNetworkApi.GetRoomPlayerCount(__instance);

                    StackTrace stackTrace = new();
                    bool isFromEnterCheck = false;
                    bool isFromSessionCount = false;

                    for (int i = 0; i < Math.Min(stackTrace.FrameCount, 10); i++)
                    {
                        StackFrame frame = stackTrace.GetFrame(i);
                        MethodBase? method = frame?.GetMethod();
                        if (method == null)
                        {
                            continue;
                        }

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
                        ModLog.Debug(
                            Feature,
                            $"GetMemberCount returning 0 for enter check in {__instance.GetType().Name}");
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
            public static IEnumerable<MethodBase> TargetMethods()
            {
                Assembly? assembly = GameNetworkApi.GetGameAssembly();
                if (assembly == null)
                {
                    return [];
                }

                List<MethodBase> methods = [];
                foreach (var typeName in new[] { "VWaitingRoom", "MaintenanceRoom", "IVroom" })
                {
                    MethodInfo? method = assembly.GetType(typeName)?.GetMethod("CanEnterChannel", InstanceMethodFlags);
                    if (method != null && !methods.Contains(method))
                    {
                        methods.Add(method);
                    }
                }

                return methods;
            }

            [HarmonyPrefix]
            public static bool Prefix(ref object __result, object __instance, long playerUID)
            {
                if (!ModConfig.EnableMorePlayers.Value)
                {
                    return true;
                }

                try
                {
                    Type? msgErrorCodeType = GameNetworkApi.GetGameAssembly()?.GetTypes().FirstOrDefault(t => t.Name == "MsgErrorCode");
                    if (msgErrorCodeType == null || !msgErrorCodeType.IsEnum)
                    {
                        return true;
                    }

                    IDictionary? vPlayerDict = ReflectionHelper.GetFieldValue(__instance, "_vPlayerDict") as IDictionary;
                    if (vPlayerDict != null)
                    {
                        foreach (var player in vPlayerDict.Values)
                        {
                            if (player == null)
                            {
                                continue;
                            }

                            var uid = ReflectionHelper.GetFieldValue<long>(player, "UID");
                            if (uid == 0)
                            {
                                PropertyInfo uidProp = player.GetType().GetProperty("UID", BindingFlags.Public | BindingFlags.Instance);
                                if (uidProp != null)
                                {
                                    uid = Convert.ToInt64(uidProp.GetValue(player));
                                }
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

                        if (PlayersInRoom(vPlayerDict) >= MaxPlayers)
                        {
                            __result = Enum.Parse(msgErrorCodeType, "PlayerCountExceeded");
                            ModLog.Info(
                                Feature,
                                $"Join denied — session full ({PlayersInRoom(vPlayerDict)}/{MaxPlayers} players) in {__instance.GetType().Name}, uid={playerUID}.");
                            return false;
                        }
                    }
                    else
                    {
                        ModLog.Warn(
                            Feature,
                            $"Join denied — player dictionary unavailable in {__instance.GetType().Name}, uid={playerUID}.");
                        __result = Enum.Parse(msgErrorCodeType, "PlayerCountExceeded");
                        return false;
                    }

                    __result = Enum.Parse(msgErrorCodeType, "Success");
                    int totalAfterJoin = PlayersInRoom(vPlayerDict) + 1;
                    ModLog.Debug(
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
            public static IEnumerable<MethodBase> TargetMethods()
            {
                Type? vroomManagerType = GameNetworkApi.GetGameAssembly()?.GetType("VRoomManager");
                if (vroomManagerType == null)
                {
                    return [];
                }

                List<MethodBase> methods = [];
                foreach (var name in new[] { "EnterWaitingRoom", "EnterMaintenenceRoom", "EnterMaintenanceRoom" })
                {
                    MethodInfo method = vroomManagerType.GetMethod(name, InstanceMethodFlags);
                    if (method != null)
                    {
                        methods.Add(method);
                    }
                }

                return methods;
            }

            [HarmonyPrefix]
            public static void Prefix(object __instance, MethodBase __originalMethod)
            {
                if (!ModConfig.EnableMorePlayers.Value)
                {
                    return;
                }

                try
                {
                    UpdateRoomMaxPlayers(__instance);
                    string roomName = __originalMethod.Name switch
                    {
                        "EnterWaitingRoom" => "WaitingRoom",
                        "EnterMaintenenceRoom" or "EnterMaintenanceRoom" => "MaintenanceRoom",
                        _ => __originalMethod.Name
                    };
                    ModLog.Info(
                        Feature,
                        $"Enter room — {roomName} (session cap {MaxPlayers}).");
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
            public static MethodBase? TargetMethod()
            {
                return ResolveGameSessionInfoMethod("AddPlayerSteamID");
            }

            [HarmonyTranspiler]
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                List<CodeInstruction> codes = [.. instructions];
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
        public static class PublicRoomListSetRoomListTranspiler
        {
            public static MethodBase? TargetMethod()
            {
                return ResolveGameTypeMethod("UIPrefab_PublicRoomList", "SetRoomList");
            }

            [HarmonyTranspiler]
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                return ReplaceVanillaLobbyCap(instructions);
            }
        }

        [HarmonyPatch]
        public static class RoomCardSetRoomDataTranspiler
        {
            public static MethodBase? TargetMethod()
            {
                return ResolveGameTypeMethod("UiPrefab_RoomCard", "SetRoomData");
            }

            [HarmonyTranspiler]
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                return ReplaceLobbyPlayerCountSuffix(instructions);
            }
        }

        [HarmonyPatch]
        public static class UpdatePlayerGroupSizeTranspiler
        {
            public static MethodBase? TargetMethod()
            {
                return ResolveSteamInviteDispatcherMethod("UpdatePlayerGroupSize");
            }

            [HarmonyTranspiler]
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                return ReplaceVanillaLobbyCap(instructions);
            }
        }

        [HarmonyPatch]
        public static class SteamLobbyCreationPatch
        {
            public static MethodBase? TargetMethod()
            {
                return ResolveSteamInviteDispatcherMethod("CreateLobby");
            }

            [HarmonyPrefix]
            public static bool Prefix(bool isOpenForRandomMatch)
            {
                if (!ModConfig.EnableMorePlayers.Value)
                {
                    return true;
                }

                try
                {
                    Type steamMatchmakingType = Type.GetType("Steamworks.SteamMatchmaking, com.rlabrecque.steamworks.net");
                    Type eLobbyTypeType = Type.GetType("Steamworks.ELobbyType, com.rlabrecque.steamworks.net");
                    Type playerPrefsType = Type.GetType("UnityEngine.PlayerPrefs, UnityEngine.CoreModule");

                    if (steamMatchmakingType == null || eLobbyTypeType == null || playerPrefsType == null)
                    {
                        ModLog.Warn(Feature, "Steam lobby creation skipped — Steamworks or PlayerPrefs types not found.");
                        return true;
                    }

                    MethodInfo createLobbyMethod = steamMatchmakingType.GetMethod("CreateLobby", BindingFlags.Public | BindingFlags.Static);
                    MethodInfo setIntMethod = playerPrefsType.GetMethod("SetInt", BindingFlags.Public | BindingFlags.Static);
                    if (createLobbyMethod == null || setIntMethod == null)
                    {
                        ModLog.Warn(Feature, "Steam lobby creation skipped — CreateLobby or PlayerPrefs.SetInt not found.");
                        return true;
                    }

                    var friendsOnly = Enum.ToObject(eLobbyTypeType, 2);
                    _ = createLobbyMethod.Invoke(null, [friendsOnly, MaxPlayers]);
                    _ = setIntMethod.Invoke(null, ["TempLobbyIsOpen", isOpenForRandomMatch ? 1 : 0]);
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
}

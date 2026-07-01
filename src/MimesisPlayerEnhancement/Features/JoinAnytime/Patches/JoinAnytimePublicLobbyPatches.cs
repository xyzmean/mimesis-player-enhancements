using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using MimesisPlayerEnhancement.Util;

namespace MimesisPlayerEnhancement.Features.JoinAnytime.Patches
{
    [HarmonyPatch]
    internal static class SteamInviteDispatcherSetLobbyPublicCoroutinePrefix
    {
        private static MethodBase? TargetMethod() =>
            AccessTools.Method(typeof(SteamInviteDispatcher), "SetLobbyPublicCoroutine", [typeof(bool)]);

        [HarmonyPrefix]
        private static void Prefix()
        {
            if (!ModConfig.EnableJoinAnytime.Value)
            {
                return;
            }

            if (JoinAnytimeLobbyController.HostWantsPublicMatchmaking())
            {
                JoinAnytimeInGameMenuTools.SyncPublicRoomToggle(isPublic: true);
            }
        }
    }

    [HarmonyPatch]
    internal static class SteamInviteDispatcherSetLobbyPublicCoroutineTranspiler
    {
        private static readonly MethodInfo CoercePublicRoomWriteFlagMethod =
            AccessTools.Method(
                typeof(JoinAnytimePublicLobbyTools),
                nameof(JoinAnytimePublicLobbyTools.CoercePublicRoomWriteFlag));

        private static MethodBase? TargetMethod() =>
            AccessTools.Method(typeof(SteamInviteDispatcher), "SetLobbyPublicCoroutine", [typeof(bool)]);

        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = [.. instructions];
            MethodInfo? toStringMethod = AccessTools.Method(typeof(bool), nameof(bool.ToString), []);

            for (int i = 1; i < codes.Count; i++)
            {
                if (codes[i].opcode != OpCodes.Call || codes[i].operand is not MethodInfo calledMethod)
                {
                    continue;
                }

                if (calledMethod != toStringMethod)
                {
                    continue;
                }

                OpCode loadLocalOpcode = codes[i - 1].opcode;
                if (loadLocalOpcode != OpCodes.Ldloc && loadLocalOpcode != OpCodes.Ldloc_0
                    && loadLocalOpcode != OpCodes.Ldloc_1 && loadLocalOpcode != OpCodes.Ldloc_2
                    && loadLocalOpcode != OpCodes.Ldloc_3 && loadLocalOpcode != OpCodes.Ldloc_S)
                {
                    continue;
                }

                CodeInstruction loadFlag = codes[i - 1];
                codes[i - 1] = new CodeInstruction(OpCodes.Ldarg_1);
                codes.Insert(i, loadFlag);
                codes.Insert(i + 1, new CodeInstruction(OpCodes.Call, CoercePublicRoomWriteFlagMethod));
                break;
            }

            return codes;
        }
    }

    [HarmonyPatch]
    internal static class MaintenanceSceneCheckPublicTramTranspiler
    {
        private static readonly MethodInfo ShouldBlockPublicRoomCloseMethod =
            AccessTools.Method(
                typeof(JoinAnytimeLobbyController),
                nameof(JoinAnytimeLobbyController.ShouldBlockPublicRoomClose));

        private static readonly MethodInfo ApplyHostPublicLobbyIntentMethod =
            AccessTools.Method(
                typeof(JoinAnytimeLobbyController),
                nameof(JoinAnytimeLobbyController.ApplyHostPublicLobbyIntent));

        private static readonly MethodInfo UpdateLobbyDataMethod =
            AccessTools.Method(typeof(SteamInviteDispatcher), nameof(SteamInviteDispatcher.UpdateLobbyData));

        private static MethodBase? TargetMethod() =>
            AccessTools.Method(
                typeof(MaintenanceScene),
                "CheckPublicTramAndChangeGameState",
                [typeof(float), typeof(Hub.PersistentData.eGameState)]);

        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Transpiler(
            IEnumerable<CodeInstruction> instructions,
            ILGenerator generator)
        {
            List<CodeInstruction> codes = [.. instructions];
            Label skipLabel = generator.DefineLabel();

            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode != OpCodes.Call || codes[i].operand is not MethodInfo calledMethod)
                {
                    continue;
                }

                if (calledMethod != UpdateLobbyDataMethod || i < 2)
                {
                    continue;
                }

                if (codes[i - 1].opcode != OpCodes.Ldstr
                    || codes[i - 1].operand as string != "false"
                    || codes[i - 2].opcode != OpCodes.Ldstr
                    || codes[i - 2].operand as string != SteamInviteDispatcher.IS_PUBLIC_KEY)
                {
                    continue;
                }

                int insertAt = i - 2;
                codes.Insert(insertAt, new CodeInstruction(OpCodes.Call, ShouldBlockPublicRoomCloseMethod));
                codes.Insert(insertAt + 1, new CodeInstruction(OpCodes.Brtrue, skipLabel));
                i += 2;

                int nopIndex = i + 1;
                codes.Insert(nopIndex, new CodeInstruction(OpCodes.Nop) { labels = [skipLabel] });
                codes.Insert(nopIndex + 1, new CodeInstruction(OpCodes.Call, ApplyHostPublicLobbyIntentMethod));
                break;
            }

            return codes;
        }
    }

    [HarmonyPatch]
    internal static class MaintenanceSceneCorRunTranspiler
    {
        private static readonly MethodInfo ShouldBlockPublicRoomCloseMethod =
            AccessTools.Method(
                typeof(JoinAnytimeLobbyController),
                nameof(JoinAnytimeLobbyController.ShouldBlockPublicRoomClose));

        private static readonly FieldInfo? IsPublicLobbyField =
            AccessTools.Field(typeof(Hub.PersistentData), nameof(Hub.PersistentData.IsPublicLobby));

        private static MethodBase? TargetMethod() =>
            AccessTools.Method(typeof(MaintenanceScene), "CorRun");

        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Transpiler(
            IEnumerable<CodeInstruction> instructions,
            ILGenerator generator)
        {
            if (IsPublicLobbyField == null)
            {
                return instructions;
            }

            List<CodeInstruction> codes = [.. instructions];
            Label skipLabel = generator.DefineLabel();

            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode != OpCodes.Stfld || codes[i].operand is not FieldInfo field
                    || field != IsPublicLobbyField)
                {
                    continue;
                }

                if (i < 1 || codes[i - 1].opcode != OpCodes.Ldc_I4_0)
                {
                    continue;
                }

                codes.Insert(i - 1, new CodeInstruction(OpCodes.Call, ShouldBlockPublicRoomCloseMethod));
                codes.Insert(i, new CodeInstruction(OpCodes.Brtrue, skipLabel));
                i += 2;
                codes.Insert(i + 1, new CodeInstruction(OpCodes.Nop) { labels = [skipLabel] });
                break;
            }

            return codes;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Mimic.Actors;
using MimesisPlayerEnhancement.Util;

namespace MimesisPlayerEnhancement.Features.MimicTuning
{
    public static class MimicTuningPatches
    {
        private const string Feature = "MimicTuning";

        private static readonly FieldInfo? PossessionDurationField =
            AccessTools.Field(typeof(Bifrost.ConstEnum.DataConsts), nameof(Bifrost.ConstEnum.DataConsts.C_PossessionDuration));

        private static readonly FieldInfo? PossessionCooltimeField =
            AccessTools.Field(typeof(Bifrost.ConstEnum.DataConsts), nameof(Bifrost.ConstEnum.DataConsts.C_PossessionCooltime));

        private static readonly MethodInfo RollPossessionDurationMsMethod =
            AccessTools.Method(typeof(MimicTuningResolver), nameof(MimicTuningResolver.RollPossessionDurationMs));

        private static readonly MethodInfo ScalePossessionCooltimeMsMethod =
            AccessTools.Method(typeof(MimicTuningResolver), nameof(MimicTuningResolver.ScalePossessionCooltimeMs));

        private static readonly MethodInfo GetProgressBarTotalSecondsMethod =
            AccessTools.Method(typeof(MimicTuningResolver), nameof(MimicTuningResolver.GetProgressBarTotalSeconds));

        public static void Apply(HarmonyLib.Harmony harmony)
        {
            _ = GameNetworkApi.GetGameAssembly();

            HarmonyPatchHelper.PatchApplyResult result = HarmonyPatchHelper.ApplyPatchTypes(
                harmony,
                Feature,
                HarmonyPatchHelper.GetNestedPatchTypes(typeof(MimicTuningPatches)));

            LogPatchAudit(harmony);
            HarmonyPatchHelper.LogPatchSummary(Feature, result);
        }

        private static void LogPatchAudit(HarmonyLib.Harmony harmony)
        {
            HarmonyPatchHelper.LogPatchAudit(Feature, harmony,
            [
                ("HandleStartPossessing/PossessionController",
                    AccessTools.Method(typeof(PossessionController), nameof(PossessionController.HandleStartPossessing))),
                ("ClearPossessingStateInternal/PossessionController",
                    AccessTools.Method(typeof(PossessionController), "ClearPossessingStateInternal")),
                ("UpdatePossessionProgressbar/ProtoActor",
                    AccessTools.Method(typeof(ProtoActor), nameof(ProtoActor.UpdatePossessionProgressbar))),
                ("Start/UIPrefab_Spectator",
                    AccessTools.Method(typeof(UIPrefab_Spectator), "Start")),
                ("UpdatePossessionCooltime/UIPrefab_Spectator",
                    AccessTools.Method(typeof(UIPrefab_Spectator), nameof(UIPrefab_Spectator.UpdatePossessionCooltime))),
                ("OnEndPossession/ProtoActor",
                    AccessTools.Method(typeof(ProtoActor), nameof(ProtoActor.OnEndPossession))),
            ]);
        }

        [HarmonyPatch(typeof(PossessionController), nameof(PossessionController.HandleStartPossessing))]
        internal static class HandleStartPossessingTranspiler
        {
            [HarmonyTranspiler]
            internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                return ReplaceConstIntLoad(
                    instructions,
                    PossessionDurationField,
                    afterLoad =>
                    [
                        new CodeInstruction(OpCodes.Ldarg_1),
                        new CodeInstruction(OpCodes.Call, RollPossessionDurationMsMethod),
                    ]);
            }
        }

        [HarmonyPatch(typeof(PossessionController), "ClearPossessingStateInternal")]
        internal static class ClearPossessingStateInternalTranspiler
        {
            [HarmonyTranspiler]
            internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                return ReplaceConstIntLoad(
                    instructions,
                    PossessionCooltimeField,
                    _ =>
                    [
                        new CodeInstruction(OpCodes.Call, ScalePossessionCooltimeMsMethod),
                    ]);
            }
        }

        [HarmonyPatch(typeof(ProtoActor), nameof(ProtoActor.UpdatePossessionProgressbar))]
        internal static class UpdatePossessionProgressbarTranspiler
        {
            [HarmonyTranspiler]
            internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                if (PossessionDurationField == null || GetProgressBarTotalSecondsMethod == null)
                {
                    return instructions;
                }

                List<CodeInstruction> codes = [.. instructions];
                for (int i = 0; i < codes.Count; i++)
                {
                    if (codes[i].opcode != OpCodes.Ldfld
                        || !ReferenceEquals(codes[i].operand, PossessionDurationField))
                    {
                        continue;
                    }

                    // Replace: load duration ms + conv to float + * 0.001f
                    // With: GetProgressBarTotalSeconds(this.ActorID, inServerLeftTime)
                    int removeThrough = i;
                    while (removeThrough + 1 < codes.Count)
                    {
                        OpCode next = codes[removeThrough + 1].opcode;
                        if (next != OpCodes.Conv_R4 && next != OpCodes.Ldc_R4 && next != OpCodes.Mul)
                        {
                            break;
                        }

                        removeThrough++;
                    }

                    codes.RemoveRange(i, removeThrough - i + 1);
                    codes.Insert(i, new CodeInstruction(OpCodes.Ldarg_0));
                    codes.Insert(i + 1, new CodeInstruction(
                        OpCodes.Callvirt,
                        AccessTools.PropertyGetter(typeof(ProtoActor), nameof(ProtoActor.ActorID))));
                    codes.Insert(i + 2, new CodeInstruction(OpCodes.Ldarg_1));
                    codes.Insert(i + 3, new CodeInstruction(OpCodes.Call, GetProgressBarTotalSecondsMethod));
                    break;
                }

                return codes;
            }
        }

        [HarmonyPatch(typeof(UIPrefab_Spectator), "Start")]
        internal static class UIPrefabSpectatorStartPostfix
        {
            [HarmonyPostfix]
            internal static void Postfix(ref float ___possessionCooltime)
            {
                try
                {
                    if (!ModConfig.EnableMimicTuning.Value)
                    {
                        return;
                    }

                    ___possessionCooltime = MimicTuningResolver.GetCooltimeTotalSeconds();
                }
                catch (Exception ex)
                {
                    ModLog.Warn(Feature, $"Spectator Start postfix failed — {ex.Message}");
                }
            }
        }

        [HarmonyPatch(typeof(UIPrefab_Spectator), nameof(UIPrefab_Spectator.UpdatePossessionCooltime))]
        internal static class UIPrefabSpectatorUpdatePossessionCooltimePostfix
        {
            [HarmonyPostfix]
            internal static void Postfix(
                float inCooltime,
                UnityEngine.UI.Image ___possessionKeyCooltime,
                ref float ___possessionCooltime)
            {
                try
                {
                    if (!ModConfig.EnableMimicTuning.Value
                        || !MimicTuningResolver.ShouldScaleCooltime
                        || inCooltime <= 0f)
                    {
                        return;
                    }

                    if (___possessionKeyCooltime != null && ___possessionKeyCooltime.fillAmount >= 0.99f)
                    {
                        ___possessionCooltime = inCooltime * 0.001f;
                    }
                }
                catch (Exception ex)
                {
                    ModLog.Warn(Feature, $"UpdatePossessionCooltime postfix failed — {ex.Message}");
                }
            }
        }

        [HarmonyPatch(typeof(ProtoActor), nameof(ProtoActor.OnEndPossession))]
        internal static class ProtoActorOnEndPossessionPostfix
        {
            [HarmonyPostfix]
            internal static void Postfix(ProtoActor __instance)
            {
                try
                {
                    MimicTuningPossessionSessions.ClearSession(__instance.ActorID);
                }
                catch (Exception ex)
                {
                    ModLog.Warn(Feature, $"OnEndPossession postfix failed — {ex.Message}");
                }
            }
        }

        private static IEnumerable<CodeInstruction> ReplaceConstIntLoad(
            IEnumerable<CodeInstruction> instructions,
            FieldInfo? constField,
            Func<int, CodeInstruction[]> insertAfterLoad)
        {
            if (constField == null)
            {
                ModLog.Warn(Feature, "Const field not found — transpiler skipped");
                return instructions;
            }

            List<CodeInstruction> codes = [.. instructions];
            int insertCount = 0;
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode != OpCodes.Ldfld || !ReferenceEquals(codes[i].operand, constField))
                {
                    continue;
                }

                CodeInstruction[] insert = insertAfterLoad(insertCount++);
                for (int j = 0; j < insert.Length; j++)
                {
                    codes.Insert(i + 1 + j, insert[j]);
                }

                i += insert.Length;
            }

            return codes;
        }
    }
}

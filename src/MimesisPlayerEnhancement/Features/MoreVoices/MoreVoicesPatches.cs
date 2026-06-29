using System;
using HarmonyLib;
using Mimic.Voice.SpeechSystem;
using MimesisPlayerEnhancement.Features.Persistence;
using MimesisPlayerEnhancement.Util;

namespace MimesisPlayerEnhancement.Features.MoreVoices
{
    public static class MoreVoicesPatches
    {
        private const string Feature = "MoreVoices";

        public static void Apply(HarmonyLib.Harmony harmony)
        {
            if (!SpeechEventArchiveLimits.FieldsAvailable)
            {
                ModLog.Warn(Feature, "SpeechEventArchive limit fields not found — voice cap patches may not apply");
            }

            if (AccessTools.Method(typeof(SpeechEventArchive), "RemoveLowerValueEventsIfExceeded") == null)
            {
                ModLog.Warn(Feature, "RemoveLowerValueEventsIfExceeded not found — re-trim may not apply");
            }

            HarmonyPatchHelper.PatchApplyResult result = HarmonyPatchHelper.ApplyPatchTypes(
                harmony,
                Feature,
                HarmonyPatchHelper.GetNestedPatchTypes(typeof(MoreVoicesPatches)));

            LogPatchAudit(harmony);
            HarmonyPatchHelper.LogPatchSummary(Feature, result);
        }

        /// <summary>Updates voice limits on all live archives after config changes.</summary>
        public static void RefreshFromConfig()
        {
            if (!ModConfig.EnableMoreVoices.Value)
            {
                return;
            }

            SpeechEventArchiveLimits.PoolLimits? limits = SpeechEventArchiveLimits.ResolveFromConfig();
            if (limits == null)
            {
                return;
            }

            int updated = 0;
            foreach (SpeechEventArchive archive in SpeechEventArchiveRegistry.EnumerateActive())
            {
                if (archive == null)
                {
                    continue;
                }

                if (SpeechEventArchiveLimits.TryApply(archive, retrimOnDecrease: true))
                {
                    updated++;
                }
            }

            string caps = SpeechEventArchiveLimits.FormatEffectiveCaps(
                SpeechEventArchiveLimits.ToEffectiveCaps(limits.Value));
            if (updated > 0)
            {
                ModLog.Info(Feature, $"Refreshed voice limits on {updated} archive(s) — {caps}.");
            }
            else
            {
                ModLog.Debug(Feature, $"Voice limit refresh complete — {caps}, no active archives.");
            }
        }

        private static void LogPatchAudit(HarmonyLib.Harmony harmony)
        {
            HarmonyPatchHelper.LogPatchAudit(Feature, harmony,
            [
                ("OnStartClient/SpeechEventArchive", AccessTools.Method(typeof(SpeechEventArchive), "OnStartClient")),
                ("RemoveLowerValueEventsIfExceeded/SpeechEventArchive",
                    AccessTools.Method(typeof(SpeechEventArchive), "RemoveLowerValueEventsIfExceeded")),
            ]);
        }

        [HarmonyPatch(typeof(SpeechEventArchive), "OnStartClient")]
        [HarmonyPriority(-100)]
        internal static class SpeechEventArchiveOnStartClientPatch
        {
            [HarmonyPrefix]
            public static void Prefix(SpeechEventArchive __instance)
            {
                if (!ModConfig.EnableMoreVoices.Value || __instance == null)
                {
                    return;
                }

                try
                {
                    if (!SpeechEventArchiveLimits.TryApply(__instance, retrimOnDecrease: false))
                    {
                        return;
                    }

                    SpeechEventArchiveLimits.PoolLimits? limits = SpeechEventArchiveLimits.ResolveFromConfig();
                    if (limits == null)
                    {
                        return;
                    }

                    ModLog.Info(
                        Feature,
                        $"Voice archive started — {SpeechEventArchiveLimits.FormatEffectiveCaps(SpeechEventArchiveLimits.ToEffectiveCaps(limits.Value))}, " +
                        $"{VoiceEventStats.DescribePlayer(__instance)}");
                }
                catch (Exception ex)
                {
                    ModLog.Warn(Feature, $"Voice archive prefix failed: {ex.Message}");
                }
            }
        }

        [HarmonyPatch(typeof(SpeechEventArchive), "RemoveLowerValueEventsIfExceeded")]
        internal static class RemoveLowerValueEventsIfExceededPrefix
        {
            [HarmonyPrefix]
            public static void Prefix(SpeechEventArchive __instance)
            {
                if (!ModConfig.EnableMoreVoices.Value || __instance == null)
                {
                    return;
                }

                try
                {
                    _ = SpeechEventArchiveLimits.TryApplyFields(__instance);
                }
                catch (Exception ex)
                {
                    ModLog.Warn(Feature, $"Voice limit prefix before eviction failed: {ex.Message}");
                }
            }
        }
    }
}

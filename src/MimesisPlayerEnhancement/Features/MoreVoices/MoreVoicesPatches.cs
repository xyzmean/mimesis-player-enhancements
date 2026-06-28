using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Mimic.Voice.SpeechSystem;
using MimesisPlayerEnhancement.Util;
using UnityEngine;

namespace MimesisPlayerEnhancement.Features.MoreVoices;

public static class MoreVoicesPatches
{
    private const string Feature = "MoreVoices";

    private const BindingFlags InstanceFieldFlags =
        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    private static readonly FieldInfo? MaxEventsField =
        typeof(SpeechEventArchive).GetField("maxEvents", InstanceFieldFlags);

    private static readonly FieldInfo? MaxDeathMatchEventsField =
        typeof(SpeechEventArchive).GetField("maxDeathMatchEvents", InstanceFieldFlags);

    private static readonly FieldInfo? MaxOutDoorEventsField =
        typeof(SpeechEventArchive).GetField("maxOutDoorEvents", InstanceFieldFlags);

    public static void Apply(HarmonyLib.Harmony harmony)
    {
        if (MaxEventsField == null || MaxDeathMatchEventsField == null || MaxOutDoorEventsField == null)
            ModLog.Warn(Feature, "One or more SpeechEventArchive limit fields not found — voice cap patches may not apply");

        var result = HarmonyPatchHelper.ApplyPatchTypes(
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
            return;

        int maxIndoor = ModConfig.MaxIndoorVoiceEvents.Value;
        int maxDeathMatch = ModConfig.MaxDeathMatchVoiceEvents.Value;
        int maxOutdoor = ModConfig.MaxOutdoorVoiceEvents.Value;
        int updated = 0;

        foreach (var archive in UnityEngine.Object.FindObjectsByType<SpeechEventArchive>(FindObjectsSortMode.None))
        {
            if (archive == null)
                continue;

            if (ApplyLimitsToArchive(archive, maxIndoor, maxDeathMatch, maxOutdoor))
                updated++;
        }

        string limits = FormatLimits(maxIndoor, maxDeathMatch, maxOutdoor);
        if (updated > 0)
        {
            ModLog.Info(Feature, $"Refreshed voice limits on {updated} archive(s) — {limits}.");
        }
        else
        {
            ModLog.Debug(Feature, $"Voice limit refresh complete — {limits}, no active archives.");
        }
    }

    private static void LogPatchAudit(HarmonyLib.Harmony harmony)
    {
        HarmonyPatchHelper.LogPatchAudit(Feature, harmony, new (string, MethodBase?)[]
        {
            ("OnStartClient/SpeechEventArchive", AccessTools.Method(typeof(SpeechEventArchive), "OnStartClient")),
        });
    }

    [HarmonyPatch(typeof(SpeechEventArchive), "OnStartClient")]
    public static class SpeechEventArchiveLimitsPatch
    {
        [HarmonyPostfix]
        public static void Postfix(SpeechEventArchive __instance)
        {
            if (!ModConfig.EnableMoreVoices.Value || __instance == null)
                return;

            try
            {
                int maxIndoor = ModConfig.MaxIndoorVoiceEvents.Value;
                int maxDeathMatch = ModConfig.MaxDeathMatchVoiceEvents.Value;
                int maxOutdoor = ModConfig.MaxOutdoorVoiceEvents.Value;

                if (ApplyLimitsToArchive(__instance, maxIndoor, maxDeathMatch, maxOutdoor))
                {
                    ModLog.Info(
                        Feature,
                        $"Voice archive started — {FormatLimits(maxIndoor, maxDeathMatch, maxOutdoor)}, " +
                        $"{VoiceEventStats.DescribePlayer(__instance)}");
                }

                ModLog.Debug(
                    Feature,
                    $"Voice archive detail — maxEvents={GetFieldValue(__instance, MaxEventsField)}, " +
                    $"maxDeathMatch={GetFieldValue(__instance, MaxDeathMatchEventsField)}, " +
                    $"maxOutdoor={GetFieldValue(__instance, MaxOutDoorEventsField)}, " +
                    $"{VoiceEventStats.DescribePlayerVerbose(__instance)}");
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"Voice archive postfix failed: {ex.Message}");
            }
        }
    }

    private static bool ApplyLimitsToArchive(
        SpeechEventArchive archive,
        int maxIndoor,
        int maxDeathMatch,
        int maxOutdoor)
    {
        try
        {
            int oldMaxEvents = GetFieldValue(archive, MaxEventsField);
            int oldMaxDeathMatch = GetFieldValue(archive, MaxDeathMatchEventsField);
            int oldMaxOutdoor = GetFieldValue(archive, MaxOutDoorEventsField);

            SetFieldValue(archive, MaxEventsField, maxIndoor);
            SetFieldValue(archive, MaxDeathMatchEventsField, maxDeathMatch);
            SetFieldValue(archive, MaxOutDoorEventsField, maxOutdoor);

            ModLog.Debug(
                Feature,
                $"Applied instance limits -> {FormatLimits(maxIndoor, maxDeathMatch, maxOutdoor)} " +
                $"(was maxEvents={oldMaxEvents}, maxDeathMatch={oldMaxDeathMatch}, maxOutdoor={oldMaxOutdoor}).");
            return true;
        }
        catch (Exception ex)
        {
            ModLog.Warn(Feature, $"Failed to set voice limit fields: {ex.Message}");
            return false;
        }
    }

    private static string FormatLimits(int maxIndoor, int maxDeathMatch, int maxOutdoor) =>
        $"indoor={maxIndoor}, deathmatch={maxDeathMatch}, outdoor={maxOutdoor}";

    private static int GetFieldValue(SpeechEventArchive archive, FieldInfo? field)
    {
        if (field == null)
            throw new MissingFieldException(typeof(SpeechEventArchive).FullName, "voice limit field");

        return (int)field.GetValue(archive)!;
    }

    private static void SetFieldValue(SpeechEventArchive archive, FieldInfo? field, int value)
    {
        if (field == null)
            throw new MissingFieldException(typeof(SpeechEventArchive).FullName, "voice limit field");

        field.SetValue(archive, value);
    }
}

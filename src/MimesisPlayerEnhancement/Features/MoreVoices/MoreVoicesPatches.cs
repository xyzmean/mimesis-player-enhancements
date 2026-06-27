using System;
using System.Reflection;
using HarmonyLib;
using Mimic.Voice.SpeechSystem;

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
        harmony.CreateClassProcessor(typeof(MoreVoicesPatches)).Patch();
        ModLog.Info(Feature, "Patches applied.");
    }

    [HarmonyPatch(typeof(SpeechEventArchive), "OnStartClient")]
    public static class SpeechEventArchiveLimitsPatch
    {
        [HarmonyPrefix]
        public static void Prefix(SpeechEventArchive __instance)
        {
            if (!ModConfig.EnableMoreVoices.Value || __instance == null)
                return;

            int max = ModConfig.MaxVoiceEvents.Value;

            try
            {
                int oldMaxEvents = GetFieldValue(__instance, MaxEventsField);
                int oldMaxDeathMatch = GetFieldValue(__instance, MaxDeathMatchEventsField);
                int oldMaxOutdoor = GetFieldValue(__instance, MaxOutDoorEventsField);

                SetFieldValue(__instance, MaxEventsField, max);
                SetFieldValue(__instance, MaxDeathMatchEventsField, max);
                SetFieldValue(__instance, MaxOutDoorEventsField, max);

                ModLog.Debug(
                    Feature,
                    $"Applied instance limits -> {max} " +
                    $"(was maxEvents={oldMaxEvents}, maxDeathMatch={oldMaxDeathMatch}, maxOutdoor={oldMaxOutdoor}).");
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"Failed to set voice limit fields: {ex.Message}");
            }
        }

        [HarmonyPostfix]
        public static void Postfix(SpeechEventArchive __instance)
        {
            if (!ModConfig.EnableMoreVoices.Value || __instance == null)
                return;

            ModLog.Info(
                Feature,
                $"Voice archive started — maxCap={ModConfig.MaxVoiceEvents.Value}, " +
                $"maxEvents={GetFieldValue(__instance, MaxEventsField)}, " +
                $"{VoiceEventStats.DescribePlayer(__instance)}");
        }
    }

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

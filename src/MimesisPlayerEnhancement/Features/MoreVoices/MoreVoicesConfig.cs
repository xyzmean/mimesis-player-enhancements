using MelonLoader;

namespace MimesisPlayerEnhancement.Features.MoreVoices
{
    /// <summary>
    /// Registers the [MimesisPlayerEnhancement_MoreVoices] section. Entries are still
    /// exposed via <see cref="ModConfig"/> properties; only registration lives here.
    /// Call order is driven by <see cref="ModConfig.Initialize"/> to keep TOML layout unchanged.
    /// </summary>
    internal static class MoreVoicesConfig
    {
        private static MelonPreferences_Category _category = null!;

        internal static void CreateCategory()
        {
            _category = ModConfig.CreateCategory("MimesisPlayerEnhancement_MoreVoices", "Больше голосов (Mimic)");
        }

        internal static void CreateEntries()
        {
            ModConfig.EnableMoreVoices = ModConfig.CreateTrackedEntry(_category,
                "EnableMoreVoices",
                true,
                "Включить расширение голосов",
                "Увеличить лимиты записи голосов игроков для мимика.");

            ModConfig.MaxIndoorVoiceEvents = ModConfig.CreateTrackedEntry(_category,
                "MaxIndoorVoiceEvents",
                3000,
                "Макс. записей (Внутри)",
                "Максимум хранимых записей голоса для подземелий.");

            ModConfig.MaxDeathMatchVoiceEvents = ModConfig.CreateTrackedEntry(_category,
                "MaxDeathMatchVoiceEvents",
                3000,
                "Макс. записей (Deathmatch)",
                "Максимум хранимых записей голоса для детматча.");

            ModConfig.MaxOutdoorVoiceEvents = ModConfig.CreateTrackedEntry(_category,
                "MaxOutdoorVoiceEvents",
                3000,
                "Макс. записей (Снаружи)",
                "Максимум хранимых записей голоса на улице.");
        }

        internal static void WireValidation(MelonLogger.Instance logger)
        {
            WireMinOne(logger, ModConfig.MaxIndoorVoiceEvents);
            WireMinOne(logger, ModConfig.MaxDeathMatchVoiceEvents);
            WireMinOne(logger, ModConfig.MaxOutdoorVoiceEvents);
            ModConfig.EnableMoreVoices.OnEntryValueChanged.Subscribe((_, _) => ModConfig.NotifyChanged(ModConfig.EnableMoreVoices));
        }

        private static void WireMinOne(MelonLogger.Instance logger, MelonPreferences_Entry<int> entry)
        {
            entry.OnEntryValueChanged.Subscribe((_, value) =>
            {
                if (value < 1)
                {
                    logger.Warning($"{entry.Identifier} must be at least 1; resetting to 1.");
                    entry.Value = 1;
                    return;
                }

                ModConfig.NotifyChanged(entry);
            });
        }
    }
}

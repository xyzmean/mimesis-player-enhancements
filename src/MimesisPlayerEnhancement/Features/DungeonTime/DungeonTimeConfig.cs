using MelonLoader;

namespace MimesisPlayerEnhancement.Features.DungeonTime
{
    /// <summary>
    /// Registers the [MimesisPlayerEnhancement_DungeonTime] section. Entries are still
    /// exposed via <see cref="ModConfig"/> properties; only registration lives here.
    /// Call order is driven by <see cref="ModConfig.Initialize"/> to keep TOML layout unchanged.
    /// </summary>
    internal static class DungeonTimeConfig
    {
        private static MelonPreferences_Category _category = null!;

        internal static void CreateCategory()
        {
            _category = ModConfig.CreateCategory("MimesisPlayerEnhancement_DungeonTime", "Время в подземелье");
        }

        internal static void CreateEntries()
        {
            ModConfig.EnableDungeonTime = ModConfig.CreateTrackedEntry(_category,
                "EnableDungeonTime",
                false,
                "Включить управление временем",
                "Продлевает время прохождения подземелья, когда игроков больше стандарта.");

            ModConfig.DungeonTimeBaselinePlayerCount = ModConfig.CreateTrackedEntry(_category,
                "DungeonTimeBaselinePlayerCount",
                4,
                "Стандартное кол-во игроков",
                "Без доп. времени при этом числе игроков и меньше (по умолчанию 4).");

            ModConfig.ExtraShiftSecondsPerPlayerAboveBaseline = ModConfig.CreateTrackedEntry(_category,
                "ExtraShiftSecondsPerPlayerAboveBaseline",
                10f,
                "Доп. время (сек) за каждого сверх лимита",
                "Сколько секунд добавляется ко времени подземелья за каждого лишнего игрока.");
        }

        internal static void WireValidation(MelonLogger.Instance logger)
        {
            ModConfig.EnableDungeonTime.OnEntryValueChanged.Subscribe((_, _) => ModConfig.NotifyChanged(ModConfig.EnableDungeonTime));
            ModConfig.DungeonTimeBaselinePlayerCount.OnEntryValueChanged.Subscribe((_, value) =>
            {
                if (value < 1)
                {
                    logger.Warning("DungeonTimeBaselinePlayerCount must be at least 1; resetting to 1.");
                    ModConfig.DungeonTimeBaselinePlayerCount.Value = 1;
                    return;
                }

                ModConfig.NotifyChanged(ModConfig.DungeonTimeBaselinePlayerCount);
            });
            ModConfig.ExtraShiftSecondsPerPlayerAboveBaseline.OnEntryValueChanged.Subscribe((_, value) =>
                OnExtraShiftSecondsPerPlayerChanged(logger, value));
        }

        internal static void RegisterFloatEntries()
        {
            ModConfig.TrackFloatEntry(ModConfig.ExtraShiftSecondsPerPlayerAboveBaseline);
        }

        private static void OnExtraShiftSecondsPerPlayerChanged(MelonLogger.Instance logger, float value)
        {
            if (value < 0f)
            {
                logger.Warning("ExtraShiftSecondsPerPlayerAboveBaseline must be >= 0; resetting to 0.");
                ModConfig.ExtraShiftSecondsPerPlayerAboveBaseline.Value = 0f;
                return;
            }

            ModConfigFloatHelper.SanitizeEntry(ModConfig.ExtraShiftSecondsPerPlayerAboveBaseline);
            ModConfig.NotifyChanged(ModConfig.ExtraShiftSecondsPerPlayerAboveBaseline);
        }
    }
}

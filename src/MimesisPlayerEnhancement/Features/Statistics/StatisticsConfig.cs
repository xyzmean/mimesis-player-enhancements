using MelonLoader;

namespace MimesisPlayerEnhancement.Features.Statistics
{
    /// <summary>
    /// Registers the [MimesisPlayerEnhancement_Statistics] section. Entries are still
    /// exposed via <see cref="ModConfig"/> properties; only registration lives here.
    /// Call order is driven by <see cref="ModConfig.Initialize"/> to keep TOML layout unchanged.
    /// </summary>
    internal static class StatisticsConfig
    {
        private static MelonPreferences_Category _category = null!;

        internal static void CreateCategory()
        {
            _category = ModConfig.CreateCategory("MimesisPlayerEnhancement_Statistics", "Статистика");
        }

        internal static void CreateEntries()
        {
            ModConfig.EnableStatistics = ModConfig.CreateTrackedEntry(_category,
                "EnableStatistics",
                true,
                "Enable Player Statistics",
                "Отслеживать статистику игроков за сессию и за всё время (по слотам сохранения).");

            ModConfig.SessionReconnectGraceMinutes = ModConfig.CreateTrackedEntry(_category,
                "SessionReconnectGraceMinutes",
                5,
                "Окно переподключения (минуты)",
                "Использовать ту же сессию, если игрок переподключился в течение указанного времени.");

            ModConfig.ShowStatisticsToasts = ModConfig.CreateTrackedEntry(_category,
                "ShowStatisticsToasts",
                true,
                "Показывать статистику в уведомлениях",
                "Показывать уведомления со статистикой мода (при входе/выходе). Не заменяет сообщения игры.");
        }

        internal static void WireValidation(MelonLogger.Instance logger)
        {
            ModConfig.SessionReconnectGraceMinutes.OnEntryValueChanged.Subscribe((_, value) =>
            {
                if (value < 1)
                {
                    logger.Warning("SessionReconnectGraceMinutes must be at least 1; resetting to 1.");
                    ModConfig.SessionReconnectGraceMinutes.Value = 1;
                    return;
                }

                ModConfig.NotifyChanged(ModConfig.SessionReconnectGraceMinutes);
            });

            ModConfig.EnableStatistics.OnEntryValueChanged.Subscribe((_, _) => ModConfig.NotifyChanged(ModConfig.EnableStatistics));
            ModConfig.ShowStatisticsToasts.OnEntryValueChanged.Subscribe((_, _) => ModConfig.NotifyChanged(ModConfig.ShowStatisticsToasts));
        }
    }
}

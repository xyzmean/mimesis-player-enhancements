using System;
using MelonLoader;

namespace MimesisPlayerEnhancement.Features.DungeonRandomizer
{
    /// <summary>
    /// Registers the [MimesisPlayerEnhancement_DungeonRandomizer] section. Entries are still
    /// exposed via <see cref="ModConfig"/> properties; only registration lives here.
    /// Call order is driven by <see cref="ModConfig.Initialize"/> to keep TOML layout unchanged.
    /// </summary>
    internal static class DungeonRandomizerConfig
    {
        private static MelonPreferences_Category _category = null!;

        internal static void CreateCategory()
        {
            _category = ModConfig.CreateCategory("MimesisPlayerEnhancement_DungeonRandomizer", "Рандомизатор подземелий");
        }

        internal static void CreateEntries()
        {
            ModConfig.EnableDungeonRandomizer = ModConfig.CreateTrackedEntry(_category,
                "EnableDungeonRandomizer",
                false,
                "Включить рандомизатор подземелий",
                "Рандомизирует выбор подземелья, варианты карты и генерацию лабиринта.");

            ModConfig.RandomizeDungeonPick = ModConfig.CreateTrackedEntry(_category,
                "RandomizeDungeonPick",
                true,
                "Случайный выбор подземелья",
                "Переопределяет выбранное подземелье при отправке трамвая.");

            ModConfig.DungeonPickPoolMode = ModConfig.CreateTrackedEntry(_category,
                "DungeonPickPoolMode",
                "WidenVanilla",
                "Режим выбора подземелий",
                "WidenVanilla = ванильный с повторами; AllActiveUniform = полностью случайный.");

            ModConfig.DungeonAllowlist = ModConfig.CreateTrackedEntry(_category,
                "DungeonAllowlist",
                "",
                "Белый список подземелий",
                "ID подземелий через запятую (будут выпадать только они).");

            ModConfig.DungeonBlocklist = ModConfig.CreateTrackedEntry(_category,
                "DungeonBlocklist",
                "",
                "Черный список подземелий",
                "ID подземелий через запятую (никогда не выпадут).");

            ModConfig.IgnoreDungeonExcludeList = ModConfig.CreateTrackedEntry(_category,
                "IgnoreDungeonExcludeList",
                true,
                "Игнорировать список исключений",
                "При WidenVanilla не исключать недавно сыгранные подземелья.");

            ModConfig.RandomizeLayoutFlow = ModConfig.CreateTrackedEntry(_category,
                "RandomizeLayoutFlow",
                true,
                "Случайный Layout Flow",
                "Случайный выбор шаблонов генерации подземелья.");

            ModConfig.RandomizeMapVariant = ModConfig.CreateTrackedEntry(_category,
                "RandomizeMapVariant",
                true,
                "Случайный вариант карты",
                "Случайный вариант макета подземелья.");

            ModConfig.RandomizeDungeonSeed = ModConfig.CreateTrackedEntry(_category,
                "RandomizeDungeonSeed",
                true,
                "Случайный сид генерации",
                "Заменяет сид генерации на случайный каждый раз.");
        }

        /// <summary>Clamps persisted values once at startup, before change handlers are wired.</summary>
        internal static void SanitizeInitialValues(MelonLogger.Instance logger)
        {
            OnDungeonPickPoolModeChanged(logger, ModConfig.DungeonPickPoolMode.Value);
        }

        internal static void WireValidation(MelonLogger.Instance logger)
        {
            ModConfig.EnableDungeonRandomizer.OnEntryValueChanged.Subscribe((_, _) => ModConfig.NotifyChanged(ModConfig.EnableDungeonRandomizer));
            ModConfig.RandomizeDungeonPick.OnEntryValueChanged.Subscribe((_, _) => ModConfig.NotifyChanged(ModConfig.RandomizeDungeonPick));
            ModConfig.DungeonPickPoolMode.OnEntryValueChanged.Subscribe((_, value) => OnDungeonPickPoolModeChanged(logger, value));
            ModConfig.DungeonAllowlist.OnEntryValueChanged.Subscribe((_, _) => ModConfig.NotifyChanged(ModConfig.DungeonAllowlist));
            ModConfig.DungeonBlocklist.OnEntryValueChanged.Subscribe((_, _) => ModConfig.NotifyChanged(ModConfig.DungeonBlocklist));
            ModConfig.IgnoreDungeonExcludeList.OnEntryValueChanged.Subscribe((_, _) => ModConfig.NotifyChanged(ModConfig.IgnoreDungeonExcludeList));
            ModConfig.RandomizeLayoutFlow.OnEntryValueChanged.Subscribe((_, _) => ModConfig.NotifyChanged(ModConfig.RandomizeLayoutFlow));
            ModConfig.RandomizeMapVariant.OnEntryValueChanged.Subscribe((_, _) => ModConfig.NotifyChanged(ModConfig.RandomizeMapVariant));
            ModConfig.RandomizeDungeonSeed.OnEntryValueChanged.Subscribe((_, _) => ModConfig.NotifyChanged(ModConfig.RandomizeDungeonSeed));
        }

        private static void OnDungeonPickPoolModeChanged(MelonLogger.Instance logger, string value)
        {
            if (!string.Equals(value, "WidenVanilla", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(value, "AllActiveUniform", StringComparison.OrdinalIgnoreCase))
            {
                logger.Warning("DungeonPickPoolMode must be WidenVanilla or AllActiveUniform; resetting to WidenVanilla.");
                ModConfig.DungeonPickPoolMode.Value = "WidenVanilla";
                return;
            }

            ModConfig.NotifyChanged(ModConfig.DungeonPickPoolMode);
        }
    }
}

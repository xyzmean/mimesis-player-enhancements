using MelonLoader;

namespace MimesisPlayerEnhancement.Features.ExtendedSaveSlots
{
    /// <summary>
    /// Registers the [MimesisPlayerEnhancement_ExtendedSaveSlots] section. Entries are still
    /// exposed via <see cref="ModConfig"/> properties; only registration lives here.
    /// Call order is driven by <see cref="ModConfig.Initialize"/> to keep TOML layout unchanged.
    /// </summary>
    internal static class ExtendedSaveSlotsConfig
    {
        private static MelonPreferences_Category _category = null!;

        internal static void CreateCategory()
        {
            _category = ModConfig.CreateCategory("MimesisPlayerEnhancement_ExtendedSaveSlots", "Расширенные слоты сохранений");
        }

        internal static void CreateEntries()
        {
            ModConfig.EnableExtendedSaveSlots = ModConfig.CreateTrackedEntry(_category,
                "EnableExtendedSaveSlots",
                true,
                "Включить расширенные слоты",
                "When enabled, replaces the separate New/Load Tram menus with a unified save picker (up to 99 manual slots). When disabled, vanilla New/Load Tram behavior is used.");
        }

        internal static void WireValidation()
        {
            ModConfig.EnableExtendedSaveSlots.OnEntryValueChanged.Subscribe((_, _) => ModConfig.NotifyChanged(ModConfig.EnableExtendedSaveSlots));
        }
    }
}

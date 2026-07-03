using MelonLoader;

namespace MimesisPlayerEnhancement.Features.PlayerTuning
{
    /// <summary>
    /// Registers the [MimesisPlayerEnhancement_PlayerTuning] section. Entries are still
    /// exposed via <see cref="ModConfig"/> properties; only registration lives here.
    /// Call order is driven by <see cref="ModConfig.Initialize"/> to keep TOML layout unchanged.
    /// </summary>
    internal static class PlayerTuningConfig
    {
        private static MelonPreferences_Category _category = null!;

        internal static void CreateCategory()
        {
            _category = ModConfig.CreateCategory("MimesisPlayerEnhancement_PlayerTuning", "Настройки игрока");
        }

        internal static void CreateEntries()
        {
            ModConfig.EnablePlayerTuning = ModConfig.CreateTrackedEntry(_category,
                "EnablePlayerTuning",
                false,
                "Включить настройки игрока",
                "Масштабирует скорость, выносливость и переносимый вес (нужно только хосту).");

            ModConfig.MoveSpeedMultiplier = ModConfig.CreateTrackedEntry(_category,
                "MoveSpeedMultiplier",
                1f,
                "Множитель скорости бега",
                "Множитель скорости ходьбы и бега (1 = ванилла, 2 = в 2 раза быстрее).");

            ModConfig.MaxStaminaMultiplier = ModConfig.CreateTrackedEntry(_category,
                "MaxStaminaMultiplier",
                1f,
                "Множитель выносливости",
                "Множитель максимальной выносливости (1 = ванилла, 2 = двойная).");

            ModConfig.StaminaDrainMultiplier = ModConfig.CreateTrackedEntry(_category,
                "StaminaDrainMultiplier",
                1f,
                "Множитель расхода выносливости",
                "Множитель расхода выносливости при беге (1 = ванилла, 0.5 = половина).");

            ModConfig.StaminaRegenMultiplier = ModConfig.CreateTrackedEntry(_category,
                "StaminaRegenMultiplier",
                1f,
                "Множитель регена выносливости",
                "Множитель скорости восстановления выносливости (1 = ванилла).");

            ModConfig.StaminaRegenDelayMultiplier = ModConfig.CreateTrackedEntry(_category,
                "StaminaRegenDelayMultiplier",
                1f,
                "Множитель задержки регена",
                "Задержка перед восстановлением выносливости после бега.");

            ModConfig.MaxCarryWeightMultiplier = ModConfig.CreateTrackedEntry(_category,
                "MaxCarryWeightMultiplier",
                1f,
                "Множитель переносимого веса",
                "Влияет на вес, при котором начинается замедление.");
        }

        internal static void WireValidation(MelonLogger.Instance logger)
        {
            ModConfig.EnablePlayerTuning.OnEntryValueChanged.Subscribe((_, _) => ModConfig.NotifyChanged(ModConfig.EnablePlayerTuning));
            ModConfig.MoveSpeedMultiplier.OnEntryValueChanged.Subscribe((_, value) =>
                OnPlayerTuningMultiplierChanged(logger, value, ModConfig.MoveSpeedMultiplier));
            ModConfig.MaxStaminaMultiplier.OnEntryValueChanged.Subscribe((_, value) =>
                OnPlayerTuningMultiplierChanged(logger, value, ModConfig.MaxStaminaMultiplier));
            ModConfig.StaminaDrainMultiplier.OnEntryValueChanged.Subscribe((_, value) =>
                OnPlayerTuningMultiplierChanged(logger, value, ModConfig.StaminaDrainMultiplier));
            ModConfig.StaminaRegenMultiplier.OnEntryValueChanged.Subscribe((_, value) =>
                OnPlayerTuningMultiplierChanged(logger, value, ModConfig.StaminaRegenMultiplier));
            ModConfig.StaminaRegenDelayMultiplier.OnEntryValueChanged.Subscribe((_, value) =>
                OnPlayerTuningMultiplierChanged(logger, value, ModConfig.StaminaRegenDelayMultiplier));
            ModConfig.MaxCarryWeightMultiplier.OnEntryValueChanged.Subscribe((_, value) =>
                OnPlayerTuningMultiplierChanged(logger, value, ModConfig.MaxCarryWeightMultiplier));
        }

        internal static void RegisterFloatEntries()
        {
            ModConfig.TrackFloatEntry(ModConfig.MoveSpeedMultiplier);
            ModConfig.TrackFloatEntry(ModConfig.MaxStaminaMultiplier);
            ModConfig.TrackFloatEntry(ModConfig.StaminaDrainMultiplier);
            ModConfig.TrackFloatEntry(ModConfig.StaminaRegenMultiplier);
            ModConfig.TrackFloatEntry(ModConfig.StaminaRegenDelayMultiplier);
            ModConfig.TrackFloatEntry(ModConfig.MaxCarryWeightMultiplier);
        }

        private static void OnPlayerTuningMultiplierChanged(
            MelonLogger.Instance logger,
            float value,
            MelonPreferences_Entry<float> entry)
        {
            if (value < PlayerTuningResolver.MinMultiplier)
            {
                logger.Warning(
                    $"{entry.Identifier} must be at least {PlayerTuningResolver.MinMultiplier}; resetting.");
                entry.Value = PlayerTuningResolver.MinMultiplier;
                return;
            }

            if (value > PlayerTuningResolver.MaxMultiplier)
            {
                logger.Warning(
                    $"{entry.Identifier} must be at most {PlayerTuningResolver.MaxMultiplier}; resetting.");
                entry.Value = PlayerTuningResolver.MaxMultiplier;
                return;
            }

            ModConfigFloatHelper.SanitizeEntry(entry);
            ModConfig.NotifyChanged(entry);
        }
    }
}

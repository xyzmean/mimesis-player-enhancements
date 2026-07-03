using MelonLoader;

namespace MimesisPlayerEnhancement.Features.SpawnScaling
{
    /// <summary>
    /// Registers the [MimesisPlayerEnhancement_SpawnScaling] section. Entries are still
    /// exposed via <see cref="ModConfig"/> properties; only registration lives here.
    /// Call order (category → entries → validation → floats → migration) is driven by
    /// <see cref="ModConfig.Initialize"/> to keep TOML section/entry order unchanged.
    /// </summary>
    internal static class SpawnScalingConfig
    {
        private static MelonPreferences_Category _category = null!;

        internal static void CreateCategory()
        {
            _category = ModConfig.CreateCategory("MimesisPlayerEnhancement_SpawnScaling", "Масштабирование врагов");
        }

        internal static void CreateEntries()
        {
            ModConfig.EnableSpawnScaling = ModConfig.CreateTrackedEntry(_category,
                "EnableSpawnScaling",
                false,
                "Включить масштабирование врагов",
                "Масштабировать количество монстров по типам. Только для хоста.");

            ModConfig.AutoScaleMimicSpawnsByPlayerCount = ModConfig.CreateTrackedEntry(_category,
                "AutoScaleMimicSpawnsByPlayerCount",
                true,
                "Авто-масштабирование Мимиков",
                "Умножает количество мимиков на (число игроков / 4) при онлайне больше 4.");

            ModConfig.MimicSpawnMultiplier = ModConfig.CreateTrackedEntry(_category,
                "MimicSpawnMultiplier",
                1f,
                "Множитель Мимиков",
                "Общий бюджет появления мимиков (1 = ванилла, 2 = в два раза больше).");

            ModConfig.AutoScaleBossSpawnsByPlayerCount = ModConfig.CreateTrackedEntry(_category,
                "AutoScaleBossSpawnsByPlayerCount",
                true,
                "Авто-масштабирование Боссов",
                "Умножает количество боссов на (игроков / 4) при онлайне больше 4 человек.");

            ModConfig.BossSpawnMultiplier = ModConfig.CreateTrackedEntry(_category,
                "BossSpawnMultiplier",
                1f,
                "Множитель Боссов",
                "Включает альт. маркеры боссов и спавнит доп. боссов (1 = ванилла, 2 = в два раза больше).");

            ModConfig.AutoScaleJakoSpawnsByPlayerCount = ModConfig.CreateTrackedEntry(_category,
                "AutoScaleJakoSpawnsByPlayerCount",
                true,
                "Авто-масштабирование Жако",
                "Умножает количество Жако на (игроков / 4) при онлайне больше 4 человек.");

            ModConfig.JakoSpawnMultiplier = ModConfig.CreateTrackedEntry(_category,
                "JakoSpawnMultiplier",
                1f,
                "Множитель Жако",
                "Обычные монстры в подземелье (1 = ванилла, 2 = в два раза больше).");

            ModConfig.AutoScaleSpecialSpawnsByPlayerCount = ModConfig.CreateTrackedEntry(_category,
                "AutoScaleSpecialSpawnsByPlayerCount",
                true,
                "Авто-масштабирование особых врагов",
                "Умножает количество особых врагов на (игроков / 4) при онлайне больше 4 человек.");

            ModConfig.SpecialSpawnMultiplier = ModConfig.CreateTrackedEntry(_category,
                "SpecialSpawnMultiplier",
                1f,
                "Множитель особых врагов",
                "Бюджет особых монстров для периодических спавнов и точек на карте.");

            ModConfig.AutoScaleTrapSpawnsByPlayerCount = ModConfig.CreateTrackedEntry(_category,
                "AutoScaleTrapSpawnsByPlayerCount",
                true,
                "Авто-масштабирование ловушек",
                "Умножает количество ловушек на (игроков / 4) при онлайне больше 4 человек.");

            ModConfig.TrapSpawnMultiplier = ModConfig.CreateTrackedEntry(_category,
                "TrapSpawnMultiplier",
                1f,
                "Множитель ловушек",
                "Включает альт. маркеры и спавнит доп. ловушки (1 = ванилла, 2 = в два раза больше).");

            ModConfig.MapPlacedEncounterDelayMinSeconds = ModConfig.CreateTrackedEntry(_category,
                "MapPlacedEncounterDelayMinSeconds",
                5f,
                "Мин. задержка спавна (сек)",
                "Мин. ожидание после зачистки точки перед спавном бонусного объекта.");

            ModConfig.MapPlacedEncounterDelayMaxSeconds = ModConfig.CreateTrackedEntry(_category,
                "MapPlacedEncounterDelayMaxSeconds",
                30f,
                "Макс. задержка спавна (сек)",
                "Макс. ожидание случайной задержки спавна бонусного объекта.");

            ModConfig.MapPlacedEncounterMinPlayerDistanceMeters = ModConfig.CreateTrackedEntry(_category,
                "MapPlacedEncounterMinPlayerDistanceMeters",
                10f,
                "Мин. дистанция спавна от игрока (м)",
                "Не спавнить объекты, пока в этом радиусе есть игроки. 0 = спавнить мгновенно.");

            ModConfig.AutoScaleOtherSpawnsByPlayerCount = ModConfig.CreateTrackedEntry(_category,
                "AutoScaleOtherSpawnsByPlayerCount",
                true,
                "Авто-масштабирование остальных",
                "Умножает количество остальных объектов на (игроков / 4) при онлайне больше 4 человек.");

            ModConfig.OtherSpawnMultiplier = ModConfig.CreateTrackedEntry(_category,
                "OtherSpawnMultiplier",
                1f,
                "Множитель остальных объектов",
                "Множитель сущностей, не являющихся мимиками, боссами, жако, особыми врагами и ловушками.");
        }

        internal static void WireValidation(MelonLogger.Instance logger)
        {
            ModConfig.EnableSpawnScaling.OnEntryValueChanged.Subscribe((_, _) => ModConfig.NotifyChanged(ModConfig.EnableSpawnScaling));
            ModConfig.AutoScaleMimicSpawnsByPlayerCount.OnEntryValueChanged.Subscribe((_, _) => ModConfig.NotifyChanged(ModConfig.AutoScaleMimicSpawnsByPlayerCount));
            ModConfig.AutoScaleBossSpawnsByPlayerCount.OnEntryValueChanged.Subscribe((_, _) => ModConfig.NotifyChanged(ModConfig.AutoScaleBossSpawnsByPlayerCount));
            ModConfig.AutoScaleJakoSpawnsByPlayerCount.OnEntryValueChanged.Subscribe((_, _) => ModConfig.NotifyChanged(ModConfig.AutoScaleJakoSpawnsByPlayerCount));
            ModConfig.AutoScaleSpecialSpawnsByPlayerCount.OnEntryValueChanged.Subscribe((_, _) => ModConfig.NotifyChanged(ModConfig.AutoScaleSpecialSpawnsByPlayerCount));
            ModConfig.AutoScaleTrapSpawnsByPlayerCount.OnEntryValueChanged.Subscribe((_, _) => ModConfig.NotifyChanged(ModConfig.AutoScaleTrapSpawnsByPlayerCount));
            ModConfig.MapPlacedEncounterDelayMinSeconds.OnEntryValueChanged.Subscribe((_, value) => OnMapPlacedEncounterDelayChanged(logger, value, ModConfig.MapPlacedEncounterDelayMinSeconds));
            ModConfig.MapPlacedEncounterDelayMaxSeconds.OnEntryValueChanged.Subscribe((_, value) => OnMapPlacedEncounterDelayChanged(logger, value, ModConfig.MapPlacedEncounterDelayMaxSeconds));
            ModConfig.MapPlacedEncounterMinPlayerDistanceMeters.OnEntryValueChanged.Subscribe((_, value) => OnMapPlacedEncounterMinPlayerDistanceChanged(logger, value));
            ModConfig.AutoScaleOtherSpawnsByPlayerCount.OnEntryValueChanged.Subscribe((_, _) => ModConfig.NotifyChanged(ModConfig.AutoScaleOtherSpawnsByPlayerCount));

            ModConfig.MimicSpawnMultiplier.OnEntryValueChanged.Subscribe((_, value) => ModConfig.OnSpawnMultiplierChanged(logger, value, ModConfig.MimicSpawnMultiplier));
            ModConfig.BossSpawnMultiplier.OnEntryValueChanged.Subscribe((_, value) => ModConfig.OnSpawnMultiplierChanged(logger, value, ModConfig.BossSpawnMultiplier));
            ModConfig.JakoSpawnMultiplier.OnEntryValueChanged.Subscribe((_, value) => ModConfig.OnSpawnMultiplierChanged(logger, value, ModConfig.JakoSpawnMultiplier));
            ModConfig.SpecialSpawnMultiplier.OnEntryValueChanged.Subscribe((_, value) => ModConfig.OnSpawnMultiplierChanged(logger, value, ModConfig.SpecialSpawnMultiplier));
            ModConfig.TrapSpawnMultiplier.OnEntryValueChanged.Subscribe((_, value) => ModConfig.OnSpawnMultiplierChanged(logger, value, ModConfig.TrapSpawnMultiplier));
            ModConfig.OtherSpawnMultiplier.OnEntryValueChanged.Subscribe((_, value) => ModConfig.OnSpawnMultiplierChanged(logger, value, ModConfig.OtherSpawnMultiplier));
        }

        internal static void RegisterFloatEntries()
        {
            ModConfig.TrackFloatEntry(ModConfig.MimicSpawnMultiplier);
            ModConfig.TrackFloatEntry(ModConfig.BossSpawnMultiplier);
            ModConfig.TrackFloatEntry(ModConfig.JakoSpawnMultiplier);
            ModConfig.TrackFloatEntry(ModConfig.SpecialSpawnMultiplier);
            ModConfig.TrackFloatEntry(ModConfig.TrapSpawnMultiplier);
            ModConfig.TrackFloatEntry(ModConfig.MapPlacedEncounterDelayMinSeconds);
            ModConfig.TrackFloatEntry(ModConfig.MapPlacedEncounterDelayMaxSeconds);
            ModConfig.TrackFloatEntry(ModConfig.MapPlacedEncounterMinPlayerDistanceMeters);
            ModConfig.TrackFloatEntry(ModConfig.OtherSpawnMultiplier);
        }

        internal static void MigrateLegacyKeys(MelonLogger.Instance logger)
        {
            bool migrated = false;
            migrated |= TryMigrateLegacyFloatKey("FixedSpawnRespawnDelayMinSeconds", ModConfig.MapPlacedEncounterDelayMinSeconds);
            migrated |= TryMigrateLegacyFloatKey("FixedSpawnRespawnDelayMaxSeconds", ModConfig.MapPlacedEncounterDelayMaxSeconds);
            migrated |= TryMigrateLegacyFloatKey("FixedSpawnRespawnMinPlayerDistanceMeters", ModConfig.MapPlacedEncounterMinPlayerDistanceMeters);

            if (migrated)
            {
                logger.Msg(
                    "Spawn Scaling config migrated — FixedSpawnRespawn* keys copied to MapPlacedEncounter* keys.");
            }
        }

        private static bool TryMigrateLegacyFloatKey(
            string legacyKey,
            MelonPreferences_Entry<float> targetEntry)
        {
            if (_category.GetEntry<float>(legacyKey) is not MelonPreferences_Entry<float> legacyEntry)
            {
                return false;
            }

            targetEntry.Value = legacyEntry.Value;
            return true;
        }

        private static void OnMapPlacedEncounterDelayChanged(MelonLogger.Instance logger, float value, MelonPreferences_Entry<float> entry)
        {
            if (value < 0f)
            {
                logger.Warning($"{entry.Identifier} must be >= 0; resetting to 0.");
                entry.Value = 0f;
                return;
            }

            float min = ModConfig.MapPlacedEncounterDelayMinSeconds.Value;
            float max = ModConfig.MapPlacedEncounterDelayMaxSeconds.Value;
            if (max < min)
            {
                logger.Warning("MapPlacedEncounterDelayMaxSeconds must be >= MapPlacedEncounterDelayMinSeconds; syncing max to min.");
                ModConfig.MapPlacedEncounterDelayMaxSeconds.Value = min;
            }

            ModConfigFloatHelper.SanitizeEntry(entry);
            ModConfig.NotifyChanged(entry);
        }

        private static void OnMapPlacedEncounterMinPlayerDistanceChanged(MelonLogger.Instance logger, float value)
        {
            if (value < 0f)
            {
                logger.Warning("MapPlacedEncounterMinPlayerDistanceMeters must be >= 0; resetting to 0.");
                ModConfig.MapPlacedEncounterMinPlayerDistanceMeters.Value = 0f;
                return;
            }

            ModConfigFloatHelper.SanitizeEntry(ModConfig.MapPlacedEncounterMinPlayerDistanceMeters);
            ModConfig.NotifyChanged(ModConfig.MapPlacedEncounterMinPlayerDistanceMeters);
        }
    }
}

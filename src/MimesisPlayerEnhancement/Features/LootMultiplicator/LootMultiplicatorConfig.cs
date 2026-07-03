using System;
using MelonLoader;

namespace MimesisPlayerEnhancement.Features.LootMultiplicator
{
    /// <summary>
    /// Registers the [MimesisPlayerEnhancement_LootMultiplicator] section. Entries are still
    /// exposed via <see cref="ModConfig"/> properties; only registration lives here.
    /// Call order is driven by <see cref="ModConfig.Initialize"/> to keep TOML layout unchanged.
    /// </summary>
    internal static class LootMultiplicatorConfig
    {
        private static MelonPreferences_Category _category = null!;

        internal static void CreateCategory()
        {
            _category = ModConfig.CreateCategory("MimesisPlayerEnhancement_LootMultiplicator", "Умножитель лута");
        }

        internal static void CreateEntries()
        {
            ModConfig.EnableLootMultiplicator = ModConfig.CreateTrackedEntry(_category,
                "EnableLootMultiplicator",
                false,
                "Включить умножитель лута",
                "Масштабирует лут на карте и предметы с врагов. Только для хоста.");

            ModConfig.AutoScaleMapLootByPlayerCount = ModConfig.CreateTrackedEntry(_category,
                "AutoScaleMapLootByPlayerCount",
                true,
                "Авто-масштабирование лута на карте",
                "Умножает лут на полках и полу на (игроков / 4) при онлайне больше 4.");

            ModConfig.MapLootMultiplier = ModConfig.CreateTrackedEntry(_category,
                "MapLootMultiplier",
                1f,
                "Множитель лута на карте",
                "Множитель лута, который можно подобрать на карте (1 = ванилла, 2 = двойной).");

            ModConfig.AutoScaleDropLootByPlayerCount = ModConfig.CreateTrackedEntry(_category,
                "AutoScaleDropLootByPlayerCount",
                true,
                "Авто-масштабирование лута с врагов",
                "Умножает лут с врагов на (игроков / 4) при онлайне больше 4.");

            ModConfig.DropLootMultiplier = ModConfig.CreateTrackedEntry(_category,
                "DropLootMultiplier",
                1f,
                "Множитель лута с врагов",
                "Множитель лута с мертвых врагов (1 = ванилла, 2 = двойной).");

            ModConfig.LootItemFilterMode = ModConfig.CreateTrackedEntry(_category,
                "LootItemFilterMode",
                "All",
                "Режим фильтра лута",
                "All = все предметы; AllowlistOnly = только IDs из белого списка; BlocklistOnly = все, кроме черного списка.");

            ModConfig.LootAllowlist = ModConfig.CreateTrackedEntry(_category,
                "LootAllowlist",
                "",
                "Белый список лута",
                "ID предметов через запятую. Работает при режиме AllowlistOnly.");

            ModConfig.LootBlocklist = ModConfig.CreateTrackedEntry(_category,
                "LootBlocklist",
                "",
                "Черный список лута",
                "ID предметов через запятую для исключения из масштабирования.");

            ModConfig.ConvertFakeActorDyingDropChancePercent = ModConfig.CreateTrackedEntry(_category,
                "ConvertFakeActorDyingDropChancePercent",
                30,
                "Шанс превращения фейк-лута",
                "Шанс (0-100%), что фейковые предметы из мимика превратятся в настоящий лут.");
        }

        internal static void WireValidation(MelonLogger.Instance logger)
        {
            ModConfig.EnableLootMultiplicator.OnEntryValueChanged.Subscribe((_, _) => ModConfig.NotifyChanged(ModConfig.EnableLootMultiplicator));
            ModConfig.AutoScaleMapLootByPlayerCount.OnEntryValueChanged.Subscribe((_, _) => ModConfig.NotifyChanged(ModConfig.AutoScaleMapLootByPlayerCount));
            ModConfig.AutoScaleDropLootByPlayerCount.OnEntryValueChanged.Subscribe((_, _) => ModConfig.NotifyChanged(ModConfig.AutoScaleDropLootByPlayerCount));

            ModConfig.MapLootMultiplier.OnEntryValueChanged.Subscribe((_, value) => ModConfig.OnSpawnMultiplierChanged(logger, value, ModConfig.MapLootMultiplier));
            ModConfig.DropLootMultiplier.OnEntryValueChanged.Subscribe((_, value) => ModConfig.OnSpawnMultiplierChanged(logger, value, ModConfig.DropLootMultiplier));
            ModConfig.LootItemFilterMode.OnEntryValueChanged.Subscribe((_, value) => OnLootItemFilterModeChanged(logger, value));
            ModConfig.LootAllowlist.OnEntryValueChanged.Subscribe((_, _) => ModConfig.NotifyChanged(ModConfig.LootAllowlist));
            ModConfig.LootBlocklist.OnEntryValueChanged.Subscribe((_, _) => ModConfig.NotifyChanged(ModConfig.LootBlocklist));
            ModConfig.ConvertFakeActorDyingDropChancePercent.OnEntryValueChanged.Subscribe((_, value) =>
                OnFakeActorDyingDropChancePercentChanged(logger, value));
        }

        internal static void RegisterFloatEntries()
        {
            ModConfig.TrackFloatEntry(ModConfig.MapLootMultiplier);
            ModConfig.TrackFloatEntry(ModConfig.DropLootMultiplier);
        }

        private static void OnLootItemFilterModeChanged(MelonLogger.Instance logger, string value)
        {
            if (!string.Equals(value, "All", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(value, "AllowlistOnly", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(value, "BlocklistOnly", StringComparison.OrdinalIgnoreCase))
            {
                logger.Warning("LootItemFilterMode must be All, AllowlistOnly, or BlocklistOnly; resetting to All.");
                ModConfig.LootItemFilterMode.Value = "All";
                return;
            }

            ModConfig.NotifyChanged(ModConfig.LootItemFilterMode);
        }

        private static void OnFakeActorDyingDropChancePercentChanged(MelonLogger.Instance logger, int value)
        {
            if (value is < 0 or > 100)
            {
                logger.Warning("ConvertFakeActorDyingDropChancePercent must be 0-100; resetting to 30.");
                ModConfig.ConvertFakeActorDyingDropChancePercent.Value = 30;
                return;
            }

            ModConfig.NotifyChanged(ModConfig.ConvertFakeActorDyingDropChancePercent);
        }
    }
}

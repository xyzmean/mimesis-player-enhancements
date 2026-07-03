using MelonLoader;

namespace MimesisPlayerEnhancement.Features.MoneyMultiplier
{
    /// <summary>
    /// Registers the [MimesisPlayerEnhancement_MoneyMultiplier] section. Entries are still
    /// exposed via <see cref="ModConfig"/> properties; only registration lives here.
    /// Call order is driven by <see cref="ModConfig.Initialize"/> to keep TOML layout unchanged.
    /// </summary>
    internal static class MoneyMultiplierConfig
    {
        private static MelonPreferences_Category _category = null!;

        internal static void CreateCategory()
        {
            _category = ModConfig.CreateCategory("MimesisPlayerEnhancement_MoneyMultiplier", "Множитель денег");
        }

        internal static void CreateEntries()
        {
            ModConfig.EnableMoneyMultiplier = ModConfig.CreateTrackedEntry(_category,
                "EnableMoneyMultiplier",
                false,
                "Включить множитель денег",
                "Изменяет стартовые деньги, квоту, цены продажи/покупки и стоимость улучшений.");

            ModConfig.AutoScaleStartupMoneyByPlayerCount = ModConfig.CreateTrackedEntry(_category,
                "AutoScaleStartupMoneyByPlayerCount",
                true,
                "Авто-масштаб стартовых денег",
                "Умножает стартовые деньги на (игроков / 4) при онлайне больше 4.");

            ModConfig.StartupMoneyMultiplier = ModConfig.CreateTrackedEntry(_category,
                "StartupMoneyMultiplier",
                1f,
                "Множитель стартовых денег",
                "Начальные деньги при старте нового сохранения (1 = ванилла, 2 = двойные).");

            ModConfig.AutoScaleRoundGoalMoneyByPlayerCount = ModConfig.CreateTrackedEntry(_category,
                "AutoScaleRoundGoalMoneyByPlayerCount",
                true,
                "Авто-масштаб квоты этапа",
                "Умножает квоту на (игроков / 4) при онлайне больше 4.");

            ModConfig.RoundGoalMoneyMultiplier = ModConfig.CreateTrackedEntry(_category,
                "RoundGoalMoneyMultiplier",
                1f,
                "Множитель квоты этапа",
                "Сумма, необходимая для завершения уровня (1 = ванилла, 2 = двойная).");

            ModConfig.AutoScaleScrapSellValueByPlayerCount = ModConfig.CreateTrackedEntry(_category,
                "AutoScaleScrapSellValueByPlayerCount",
                true,
                "Авто-масштаб ценности продажи",
                "Умножает ценность продаваемых предметов на (игроков / 4).");

            ModConfig.ScrapSellValueMultiplier = ModConfig.CreateTrackedEntry(_category,
                "ScrapSellValueMultiplier",
                1f,
                "Множитель цены продажи",
                "Множитель цены продаваемого лома в трамвае (1 = ванилла, 2 = в два раза дороже).");

            ModConfig.AutoScaleShopBuyPriceByPlayerCount = ModConfig.CreateTrackedEntry(_category,
                "AutoScaleShopBuyPriceByPlayerCount",
                true,
                "Авто-масштаб цен в магазине",
                "Умножает цены на предметы в магазине на (игроков / 4).");

            ModConfig.ShopBuyPriceMultiplier = ModConfig.CreateTrackedEntry(_category,
                "ShopBuyPriceMultiplier",
                1f,
                "Множитель цен в магазине",
                "Множитель стоимости покупки в магазине и автоматах.");

            ModConfig.ShopDiscountMinPercent = ModConfig.CreateTrackedEntry(_category,
                "ShopDiscountMinPercent",
                0,
                "Мин. скидка в магазине (%)",
                "Минимальная скидка на предмет, если он получил скидку.");

            ModConfig.ShopDiscountMaxPercent = ModConfig.CreateTrackedEntry(_category,
                "ShopDiscountMaxPercent",
                100,
                "Макс. скидка в магазине (%)",
                "Максимальная скидка на предмет, если он получил скидку.");

            ModConfig.ShopDiscountChancePercent = ModConfig.CreateTrackedEntry(_category,
                "ShopDiscountChancePercent",
                0,
                "Шанс скидки в магазине (%)",
                "Шанс применения случайной скидки на предметы в магазине (0 = выкл).");

            ModConfig.AutoScaleReinforcePriceByPlayerCount = ModConfig.CreateTrackedEntry(_category,
                "AutoScaleReinforcePriceByPlayerCount",
                true,
                "Авто-масштаб цены улучшений",
                "Умножает стоимость улучшений на (игроков / 4) при онлайне больше 4.");

            ModConfig.ReinforcePriceMultiplier = ModConfig.CreateTrackedEntry(_category,
                "ReinforcePriceMultiplier",
                1f,
                "Множитель цены улучшений",
                "Множитель стоимости усиления предметов (1 = ванилла, 2 = в два раза дороже).");
        }

        /// <summary>Clamps persisted shop discount percents once at startup, before change handlers are wired.</summary>
        internal static void SanitizeInitialValues(MelonLogger.Instance logger)
        {
            OnShopDiscountPercentChanged(logger, ModConfig.ShopDiscountMinPercent.Value, ModConfig.ShopDiscountMinPercent);
            OnShopDiscountPercentChanged(logger, ModConfig.ShopDiscountMaxPercent.Value, ModConfig.ShopDiscountMaxPercent);
            OnShopDiscountPercentChanged(logger, ModConfig.ShopDiscountChancePercent.Value, ModConfig.ShopDiscountChancePercent);
        }

        internal static void WireValidation(MelonLogger.Instance logger)
        {
            ModConfig.EnableMoneyMultiplier.OnEntryValueChanged.Subscribe((_, _) => ModConfig.NotifyChanged(ModConfig.EnableMoneyMultiplier));
            ModConfig.AutoScaleStartupMoneyByPlayerCount.OnEntryValueChanged.Subscribe((_, _) => ModConfig.NotifyChanged(ModConfig.AutoScaleStartupMoneyByPlayerCount));
            ModConfig.AutoScaleRoundGoalMoneyByPlayerCount.OnEntryValueChanged.Subscribe((_, _) => ModConfig.NotifyChanged(ModConfig.AutoScaleRoundGoalMoneyByPlayerCount));
            ModConfig.AutoScaleScrapSellValueByPlayerCount.OnEntryValueChanged.Subscribe((_, _) => ModConfig.NotifyChanged(ModConfig.AutoScaleScrapSellValueByPlayerCount));
            ModConfig.AutoScaleShopBuyPriceByPlayerCount.OnEntryValueChanged.Subscribe((_, _) => ModConfig.NotifyChanged(ModConfig.AutoScaleShopBuyPriceByPlayerCount));
            ModConfig.AutoScaleReinforcePriceByPlayerCount.OnEntryValueChanged.Subscribe((_, _) => ModConfig.NotifyChanged(ModConfig.AutoScaleReinforcePriceByPlayerCount));

            ModConfig.StartupMoneyMultiplier.OnEntryValueChanged.Subscribe((_, value) => ModConfig.OnSpawnMultiplierChanged(logger, value, ModConfig.StartupMoneyMultiplier));
            ModConfig.RoundGoalMoneyMultiplier.OnEntryValueChanged.Subscribe((_, value) => ModConfig.OnSpawnMultiplierChanged(logger, value, ModConfig.RoundGoalMoneyMultiplier));
            ModConfig.ScrapSellValueMultiplier.OnEntryValueChanged.Subscribe((_, value) => ModConfig.OnSpawnMultiplierChanged(logger, value, ModConfig.ScrapSellValueMultiplier));
            ModConfig.ShopBuyPriceMultiplier.OnEntryValueChanged.Subscribe((_, value) => ModConfig.OnSpawnMultiplierChanged(logger, value, ModConfig.ShopBuyPriceMultiplier));
            ModConfig.ShopDiscountMinPercent.OnEntryValueChanged.Subscribe((_, value) => OnShopDiscountPercentChanged(logger, value, ModConfig.ShopDiscountMinPercent));
            ModConfig.ShopDiscountMaxPercent.OnEntryValueChanged.Subscribe((_, value) => OnShopDiscountPercentChanged(logger, value, ModConfig.ShopDiscountMaxPercent));
            ModConfig.ShopDiscountChancePercent.OnEntryValueChanged.Subscribe((_, value) => OnShopDiscountPercentChanged(logger, value, ModConfig.ShopDiscountChancePercent));
            ModConfig.ReinforcePriceMultiplier.OnEntryValueChanged.Subscribe((_, value) => ModConfig.OnSpawnMultiplierChanged(logger, value, ModConfig.ReinforcePriceMultiplier));
        }

        internal static void RegisterFloatEntries()
        {
            ModConfig.TrackFloatEntry(ModConfig.StartupMoneyMultiplier);
            ModConfig.TrackFloatEntry(ModConfig.RoundGoalMoneyMultiplier);
            ModConfig.TrackFloatEntry(ModConfig.ScrapSellValueMultiplier);
            ModConfig.TrackFloatEntry(ModConfig.ShopBuyPriceMultiplier);
            ModConfig.TrackFloatEntry(ModConfig.ReinforcePriceMultiplier);
        }

        private static void OnShopDiscountPercentChanged(MelonLogger.Instance logger, int value, MelonPreferences_Entry<int> entry)
        {
            if (value < 0)
            {
                logger.Warning($"{entry.Identifier} must be >= 0; resetting to 0.");
                entry.Value = 0;
                return;
            }

            if (value > 100)
            {
                logger.Warning($"{entry.Identifier} must be <= 100; resetting to 100.");
                entry.Value = 100;
                return;
            }

            if (ModConfig.ShopDiscountMaxPercent.Value < ModConfig.ShopDiscountMinPercent.Value)
            {
                logger.Warning("ShopDiscountMaxPercent must be >= ShopDiscountMinPercent; syncing max to min.");
                ModConfig.ShopDiscountMaxPercent.Value = ModConfig.ShopDiscountMinPercent.Value;
            }

            ModConfig.NotifyChanged(entry);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MimesisPlayerEnhancement.Util;
using ReluProtocol;

namespace MimesisPlayerEnhancement.Features.MoneyMultiplier
{
    internal static class MoneyMultiplierApplier
    {
        private const string Feature = "MoneyMultiplier";
        private const int MaxShopItemRollAttemptsPerSlot = 32;

        private static readonly FieldInfo TargetCurrencyField =
            AccessTools.Field(typeof(GameSessionInfo), "_targetCurrency")
            ?? throw new InvalidOperationException("GameSessionInfo._targetCurrency not found");

        internal static bool IsEnabled()
        {
            return ModConfig.EnableMoneyMultiplier.Value && HostApplyGate.ShouldApplyHostOnlyFeature();
        }

        internal static bool TryGetVanillaInitialMoney(out int value)
        {
            value = 0;
            ExcelDataManager? excel = HubGameDataAccess.Excel;
            if (excel == null)
            {
                return false;
            }

            try
            {
                value = excel.Consts.C_InitialMoney;
                return true;
            }
            catch
            {
                return false;
            }
        }

        internal static int ScaleForType(MoneyType type, int vanilla, int playerCount)
        {
            float effective = MoneyMultiplierResolver.GetEffectiveMultiplier(type, playerCount);
            int scaled = MoneyMultiplierResolver.ScaleAmount(vanilla, effective);
            MoneyMultiplierLog.DebugScaled(type, vanilla, scaled, playerCount, effective);
            return scaled;
        }

        internal static void ApplyStartupMoney(MaintenanceRoom room, ref int currency)
        {
            if (!IsEnabled() || StartupMoneyLoadGuard.IsActive)
            {
                return;
            }

            if (!TryGetVanillaInitialMoney(out int vanillaInitial) || currency != vanillaInitial)
            {
                return;
            }

            int playerCount = SessionPlayerCountHelper.ResolveFromRoom(room);
            currency = ScaleForType(MoneyType.Startup, currency, playerCount);
        }

        internal static void ApplyRoundGoal(GameSessionInfo info)
        {
            if (!IsEnabled())
            {
                return;
            }

            int vanilla = (int)(TargetCurrencyField.GetValue(info) ?? 0);
            if (vanilla <= 0)
            {
                return;
            }

            int playerCount = SessionPlayerCountHelper.ResolveFromSession(info);
            int scaled = ScaleForType(MoneyType.RoundGoal, vanilla, playerCount);
            TargetCurrencyField.SetValue(info, scaled);
        }

        internal static int ScaleScrapValue(int vanilla)
        {
            if (!IsEnabled() || vanilla == 0)
            {
                return vanilla;
            }

            int playerCount = SessionPlayerCountHelper.ResolveFromSession();
            float effective = MoneyMultiplierResolver.GetEffectiveMultiplier(MoneyType.ScrapSellValue, playerCount);
            if (effective == 1f)
            {
                return vanilla;
            }

            return ScaleForType(MoneyType.ScrapSellValue, vanilla, playerCount);
        }

        internal static int ScaleReinforcePrice(MaintenanceRoom room, int vanilla)
        {
            if (!IsEnabled())
            {
                return vanilla;
            }

            int playerCount = SessionPlayerCountHelper.ResolveFromRoom(room);
            return ScaleForType(MoneyType.ReinforcePrice, vanilla, playerCount);
        }

        internal static int ScaleReinforceCost(int upgradeCost, MaintenanceRoom room)
        {
            return ScaleReinforcePrice(room, upgradeCost);
        }

        internal static void ApplyShopItems(MaintenanceRoom room)
        {
            if (!IsEnabled())
            {
                return;
            }

            if (MaintenanceRoomAccess.GetPriceForItems(room) is not Dictionary<int, ShopItemPriceInfo> priceForItems)
            {
                return;
            }

            int vanillaCount = priceForItems.Count;
            if (vanillaCount == 0)
            {
                return;
            }

            int playerCount = SessionPlayerCountHelper.ResolveFromRoom(room);
            int targetCount = ScaleForType(MoneyType.ShopItems, vanillaCount, playerCount);
            if (targetCount <= vanillaCount)
            {
                return;
            }

            DynamicDataManager? dynamicData = HubGameDataAccess.DynamicData;
            ExcelDataManager? excel = HubGameDataAccess.Excel;
            if (dynamicData == null || excel == null)
            {
                return;
            }

            List<int> shopGroupIds = [];
            foreach (VendingMachineLevelObject? machine in dynamicData.GetVendingMachineLevelObjects())
            {
                if (machine == null || machine.shopGroupID <= 0)
                {
                    continue;
                }

                shopGroupIds.Add(machine.shopGroupID);
            }

            if (shopGroupIds.Count == 0)
            {
                ModLog.Debug(Feature, "Shop items scaling skipped — no vending-machine shop groups on map");
                return;
            }
            int added = 0;
            int attempts = 0;
            int maxAttempts = (targetCount - vanillaCount) * MaxShopItemRollAttemptsPerSlot;

            while (priceForItems.Count < targetCount && attempts < maxAttempts)
            {
                attempts++;
                int shopGroupId = shopGroupIds[attempts % shopGroupIds.Count];
                (int masterId, int price, float discountRate) = excel.GetShopGroupInfo(shopGroupId);
                if (masterId <= 0 || priceForItems.ContainsKey(masterId))
                {
                    continue;
                }

                priceForItems.Add(masterId, new ShopItemPriceInfo
                {
                    Price = price,
                    DiscountRate = discountRate,
                });
                added++;
            }

            if (added > 0)
            {
                float effective = MoneyMultiplierResolver.GetEffectiveMultiplier(MoneyType.ShopItems, playerCount);
                ModLog.Info(
                    Feature,
                    $"Shop items scaled — {vanillaCount} -> {priceForItems.Count} unique items " +
                    $"(target={targetCount}, added={added}, players={playerCount}, effective={effective:0.##}×)");
            }
            else if (ModConfig.EnableDebugLogging.Value)
            {
                ModLog.Debug(
                    Feature,
                    $"Shop items unchanged at {vanillaCount} — could not roll {targetCount - vanillaCount} additional unique items");
            }
        }
    }
}

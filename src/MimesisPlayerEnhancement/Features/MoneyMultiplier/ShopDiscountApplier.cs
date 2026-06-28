using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using ReluProtocol;

namespace MimesisPlayerEnhancement.Features.MoneyMultiplier;

internal static class ShopDiscountApplier
{
    private const string Feature = "MoneyMultiplier";

    private const BindingFlags InstanceFlags =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    private static readonly FieldInfo PriceForItemsField =
        AccessTools.Field(typeof(MaintenanceRoom), "_priceForItems")
        ?? throw new InvalidOperationException("MaintenanceRoom._priceForItems not found");

    internal static void Apply(MaintenanceRoom room)
    {
        if (!MoneyMultiplierApplier.IsEnabled())
            return;

        if (ModConfig.ShopDiscountChancePercent.Value <= 0)
            return;

        if (PriceForItemsField.GetValue(room) is not Dictionary<int, ShopItemPriceInfo> priceForItems
            || priceForItems.Count == 0)
            return;

        int minPercent = ModConfig.ShopDiscountMinPercent.Value;
        int maxPercent = ModConfig.ShopDiscountMaxPercent.Value;
        int chancePercent = ModConfig.ShopDiscountChancePercent.Value;
        if (maxPercent < minPercent)
            maxPercent = minPercent;

        int discounted = 0;
        foreach (ShopItemPriceInfo info in priceForItems.Values)
        {
            if (info == null)
                continue;

            int basePrice = GetBasePrice(info.Price, info.DiscountRate);
            if (basePrice <= 0)
                continue;

            if (!RollDiscount(chancePercent))
            {
                info.DiscountRate = 0f;
                info.Price = basePrice;
                continue;
            }

            int discountPercent = RollDiscountPercent(minPercent, maxPercent);
            info.DiscountRate = discountPercent / 100f;
            info.Price = Math.Max(1, (int)Math.Round(basePrice * (1f - info.DiscountRate)));
            discounted++;
        }

        MaintenanceShopPriceSync.SyncVendingMachineLevelObjects(room, priceForItems);

        if (discounted > 0 || ModConfig.EnableDebugLogging.Value)
        {
            ModLog.Debug(
                Feature,
                $"Shop discounts applied — {discounted}/{priceForItems.Count} items discounted " +
                $"(chance={chancePercent}%, range={minPercent}-{maxPercent}%)");
        }
    }

    private static int GetBasePrice(int price, float discountRate)
    {
        if (price <= 0)
            return 0;

        if (discountRate <= 0f || discountRate >= 1f)
            return price;

        return Math.Max(1, (int)Math.Round(price / (1f - discountRate)));
    }

    private static bool RollDiscount(int chancePercent)
    {
        if (chancePercent >= 100)
            return true;

        if (chancePercent <= 0)
            return false;

        return SimpleRandUtil.Next(0, 10000) < chancePercent * 100;
    }

    private static int RollDiscountPercent(int minPercent, int maxPercent)
    {
        if (maxPercent <= minPercent)
            return minPercent;

        return SimpleRandUtil.Next(minPercent, maxPercent + 1);
    }
}

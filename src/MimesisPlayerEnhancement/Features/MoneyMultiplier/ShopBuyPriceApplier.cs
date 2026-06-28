using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using ReluProtocol;

namespace MimesisPlayerEnhancement.Features.MoneyMultiplier;

internal static class ShopBuyPriceApplier
{
    private const string Feature = "MoneyMultiplier";

    private static readonly FieldInfo PriceForItemsField =
        AccessTools.Field(typeof(MaintenanceRoom), "_priceForItems")
        ?? throw new InvalidOperationException("MaintenanceRoom._priceForItems not found");

    internal static void Apply(MaintenanceRoom room)
    {
        if (!MoneyMultiplierApplier.IsEnabled())
            return;

        if (PriceForItemsField.GetValue(room) is not Dictionary<int, ShopItemPriceInfo> priceForItems
            || priceForItems.Count == 0)
            return;

        int playerCount = MoneyPlayerCountHelper.ResolveFromRoom(room);
        float effective = MoneyMultiplierResolver.GetEffectiveMultiplier(MoneyType.ShopBuyPrice, playerCount);
        if (effective == 1f)
            return;

        int scaledCount = 0;
        foreach (ShopItemPriceInfo info in priceForItems.Values)
        {
            if (info == null || info.Price <= 0)
                continue;

            int vanilla = info.Price;
            int scaled = MoneyMultiplierResolver.ScaleAmount(vanilla, effective);
            if (scaled == vanilla)
                continue;

            info.Price = scaled;
            scaledCount++;
        }

        if (scaledCount == 0)
            return;

        MaintenanceShopPriceSync.SyncVendingMachineLevelObjects(room, priceForItems);

        ModLog.Info(
            Feature,
            $"Shop buy prices scaled — {scaledCount}/{priceForItems.Count} items " +
            $"(players={playerCount}, effective={effective:0.##}×)");
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using ReluProtocol;

namespace MimesisPlayerEnhancement.Features.MoneyMultiplier
{
    internal static class MaintenanceShopPriceSync
    {
        private static readonly FieldInfo LevelObjectsField =
            AccessTools.Field(typeof(IVroom), "_levelObjects")
            ?? throw new InvalidOperationException("IVroom._levelObjects not found");

        internal static void SyncVendingMachineLevelObjects(
            MaintenanceRoom room,
            Dictionary<int, ShopItemPriceInfo> priceForItems)
        {
            if (LevelObjectsField.GetValue(room) is not IDictionary levelObjects)
            {
                return;
            }

            foreach (DictionaryEntry entry in levelObjects)
            {
                if (entry.Value is not InsertLevelObjectInfo insertLevelObjectInfo)
                {
                    continue;
                }

                if (insertLevelObjectInfo.InsertLevelObjectType != InsertLevelObjectType.VendingMachine)
                {
                    continue;
                }

                if (!priceForItems.TryGetValue(insertLevelObjectInfo.OutputItemMasterID, out ShopItemPriceInfo? shopInfo)
                    || shopInfo == null)
                {
                    continue;
                }

                insertLevelObjectInfo.InputAmount = shopInfo.Price;
                insertLevelObjectInfo.DiscountRate = shopInfo.DiscountRate;
            }
        }
    }
}

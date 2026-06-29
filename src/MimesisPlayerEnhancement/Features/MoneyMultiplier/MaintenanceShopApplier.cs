using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using MimesisPlayerEnhancement.Util;
using ReluProtocol;

namespace MimesisPlayerEnhancement.Features.MoneyMultiplier
{
    internal static class MaintenanceShopApplier
    {
        private const string Feature = "MoneyMultiplier";

        private sealed class RoomState
        {
            internal int AppliedConfigGeneration = -1;
        }

        private static int _configGeneration;
        private static readonly ConditionalWeakTable<MaintenanceRoom, RoomState> States = [];
        private static readonly ConditionalWeakTable<MaintenanceRoom, Dictionary<int, int>> VanillaPricesByRoom = [];

        internal static void NotifyConfigChanged()
        {
            _ = Interlocked.Increment(ref _configGeneration);
        }

        internal static void PrepareForShopInit(MaintenanceRoom room)
        {
            MarkDirty(room);
            ClearVanillaPrices(room);
        }

        internal static void ApplyAfterShopInit(MaintenanceRoom room)
        {
            MoneyMultiplierApplier.ApplyShopItems(room);
            ApplyDiscounts(room);
            ApplyBuyPrices(room);
            MarkApplied(room);
        }

        internal static void ApplyAfterLoad(MaintenanceRoom room)
        {
            PrepareForShopInit(room);
            ApplyDiscounts(room);
            ApplyBuyPrices(room);
            MarkApplied(room);
        }

        internal static void EnsureApplied(MaintenanceRoom room)
        {
            if (room == null)
            {
                return;
            }

            RoomState state = GetState(room);
            if (state.AppliedConfigGeneration == _configGeneration)
            {
                return;
            }

            ApplyBuyPrices(room);
            state.AppliedConfigGeneration = _configGeneration;
        }

        private static void MarkDirty(MaintenanceRoom room)
        {
            if (room == null)
            {
                return;
            }

            GetState(room).AppliedConfigGeneration = -1;
        }

        private static void MarkApplied(MaintenanceRoom room)
        {
            if (room == null)
            {
                return;
            }

            GetState(room).AppliedConfigGeneration = Volatile.Read(ref _configGeneration);
        }

        private static void ClearVanillaPrices(MaintenanceRoom room)
        {
            if (room == null)
            {
                return;
            }

            _ = VanillaPricesByRoom.Remove(room);
        }

        private static void ApplyDiscounts(MaintenanceRoom room)
        {
            if (!MoneyMultiplierApplier.IsEnabled())
            {
                return;
            }

            if (ModConfig.ShopDiscountChancePercent.Value <= 0)
            {
                return;
            }

            if (MaintenanceRoomAccess.GetPriceForItems(room) is not Dictionary<int, ShopItemPriceInfo> priceForItems
                || priceForItems.Count == 0)
            {
                return;
            }

            int minPercent = ModConfig.ShopDiscountMinPercent.Value;
            int maxPercent = ModConfig.ShopDiscountMaxPercent.Value;
            int chancePercent = ModConfig.ShopDiscountChancePercent.Value;
            if (maxPercent < minPercent)
            {
                maxPercent = minPercent;
            }

            int discounted = 0;
            foreach (ShopItemPriceInfo info in priceForItems.Values)
            {
                if (info == null)
                {
                    continue;
                }

                int basePrice = GetBasePrice(info.Price, info.DiscountRate);
                if (basePrice <= 0)
                {
                    continue;
                }

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

            MaintenanceRoomAccess.SyncVendingMachines(room, priceForItems);

            if (discounted > 0 || ModConfig.EnableDebugLogging.Value)
            {
                ModLog.Debug(
                    Feature,
                    $"Shop discounts applied — {discounted}/{priceForItems.Count} items discounted " +
                    $"(chance={chancePercent}%, range={minPercent}-{maxPercent}%)");
            }
        }

        private static void ApplyBuyPrices(MaintenanceRoom room)
        {
            if (!MoneyMultiplierApplier.IsEnabled())
            {
                return;
            }

            if (MaintenanceRoomAccess.GetPriceForItems(room) is not Dictionary<int, ShopItemPriceInfo> priceForItems
                || priceForItems.Count == 0)
            {
                return;
            }

            int playerCount = SessionPlayerCountHelper.ResolveFromRoom(room);
            float effective = MoneyMultiplierResolver.GetEffectiveMultiplier(MoneyType.ShopBuyPrice, playerCount);

            Dictionary<int, int> vanillaPrices = VanillaPricesByRoom.GetOrCreateValue(room);

            int scaledCount = 0;
            foreach (KeyValuePair<int, ShopItemPriceInfo> entry in priceForItems)
            {
                ShopItemPriceInfo? info = entry.Value;
                if (info == null || info.Price <= 0)
                {
                    continue;
                }

                if (!vanillaPrices.TryGetValue(entry.Key, out int vanilla))
                {
                    vanilla = info.Price;
                    vanillaPrices[entry.Key] = vanilla;
                }

                int scaled = effective == 1f ? vanilla : MoneyMultiplierResolver.ScaleAmount(vanilla, effective);
                if (info.Price == scaled)
                {
                    continue;
                }

                info.Price = scaled;
                scaledCount++;
            }

            if (scaledCount == 0 && effective == 1f)
            {
                return;
            }

            MaintenanceRoomAccess.SyncVendingMachines(room, priceForItems);

            if (scaledCount > 0 || ModConfig.EnableDebugLogging.Value)
            {
                ModLog.Info(
                    Feature,
                    $"Shop buy prices scaled — {scaledCount}/{priceForItems.Count} items " +
                    $"(players={playerCount}, effective={effective:0.##}×)");
            }
        }

        private static int GetBasePrice(int price, float discountRate)
        {
            return price <= 0 ? 0 : discountRate is <= 0f or >= 1f ? price : Math.Max(1, (int)Math.Round(price / (1f - discountRate)));
        }

        private static bool RollDiscount(int chancePercent)
        {
            return chancePercent >= 100 || chancePercent > 0 && SimpleRandUtil.Next(0, 10000) < chancePercent * 100;
        }

        private static int RollDiscountPercent(int minPercent, int maxPercent)
        {
            return maxPercent <= minPercent ? minPercent : SimpleRandUtil.Next(minPercent, maxPercent + 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static RoomState GetState(MaintenanceRoom room)
        {
            return States.GetOrCreateValue(room);
        }
    }
}

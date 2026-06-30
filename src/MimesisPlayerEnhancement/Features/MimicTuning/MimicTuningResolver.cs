using System;
using System.Reflection;
using Bifrost.Cooked;
using MimesisPlayerEnhancement.Util;

namespace MimesisPlayerEnhancement.Features.MimicTuning
{
    internal static class MimicTuningResolver
    {
        private const BindingFlags InstanceFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static readonly PropertyInfo? HubDatamanProperty =
            typeof(Hub).GetProperty("dataman", InstanceFlags);
        internal const float MinMultiplier = 0.1f;
        internal const float MaxMultiplier = 10f;
        private const float VanillaEpsilon = 0.001f;

        internal static bool ShouldApplyHost =>
            HostApplyGate.ShouldApplyHostOnlyFeature(() => ModConfig.EnableMimicTuning.Value);

        internal static bool ShouldRandomizeDuration =>
            ShouldApplyHost && ModConfig.RandomizeMimicPossessionDuration.Value;

        internal static bool ShouldScaleCooltime =>
            ShouldApplyHost && !IsVanillaMultiplier(ModConfig.MimicPossessionCooltimeMultiplier.Value);

        internal static bool IsVanillaMultiplier(float multiplier) =>
            Math.Abs(multiplier - 1f) < VanillaEpsilon;

        internal static long GetVanillaPossessionDurationMs() =>
            TryGetConsts(out Bifrost.ConstEnum.DataConsts consts)
                ? consts.C_PossessionDuration
                : 0L;

        internal static long GetVanillaPossessionCooltimeMs() =>
            TryGetConsts(out Bifrost.ConstEnum.DataConsts consts)
                ? consts.C_PossessionCooltime
                : 0L;

        private static bool TryGetConsts(out Bifrost.ConstEnum.DataConsts consts)
        {
            consts = null!;
            try
            {
                if (Hub.s == null || HubDatamanProperty?.GetValue(Hub.s) is not DataManager dataman)
                {
                    return false;
                }

                consts = dataman.ExcelDataManager.Consts;
                return consts != null;
            }
            catch
            {
                return false;
            }
        }

        internal static long RollPossessionDurationMs(long vanillaMs, int mimicActorId)
        {
            if (!ModConfig.EnableMimicTuning.Value || !ShouldRandomizeDuration || vanillaMs <= 0)
            {
                return vanillaMs;
            }

            float minMultiplier = ModConfig.MimicPossessionMinTimeMultiplier.Value;
            float maxMultiplier = ModConfig.MimicPossessionMaxTimeMultiplier.Value;
            long minMs = Math.Max(1L, (long)(vanillaMs * minMultiplier));
            long maxMs = Math.Max(minMs, (long)(vanillaMs * maxMultiplier));

            long rolled = minMs >= maxMs
                ? minMs
                : UnityEngine.Random.Range((int)minMs, (int)maxMs + 1);

            MimicTuningPossessionSessions.SetSessionDurationMs(mimicActorId, rolled);
            return rolled;
        }

        internal static long ScalePossessionCooltimeMs(long vanillaMs)
        {
            if (!ModConfig.EnableMimicTuning.Value || !ShouldScaleCooltime || vanillaMs <= 0)
            {
                return vanillaMs;
            }

            return Math.Max(0L, (long)(vanillaMs * ModConfig.MimicPossessionCooltimeMultiplier.Value));
        }

        internal static float GetProgressBarTotalSeconds(int mimicActorId, float serverLeftTimeMs)
        {
            float vanillaSeconds = GetVanillaPossessionDurationMs() * 0.001f;
            if (!ModConfig.EnableMimicTuning.Value || !ModConfig.RandomizeMimicPossessionDuration.Value)
            {
                return vanillaSeconds;
            }

            if (MimicTuningPossessionSessions.TryGetSessionDurationMs(mimicActorId, out long sessionMs))
            {
                return sessionMs * 0.001f;
            }

            if (serverLeftTimeMs > 0f)
            {
                MimicTuningPossessionSessions.SetSessionDurationMs(mimicActorId, (long)serverLeftTimeMs);
                return serverLeftTimeMs * 0.001f;
            }

            return vanillaSeconds;
        }

        internal static float GetCooltimeTotalSeconds()
        {
            long vanillaMs = GetVanillaPossessionCooltimeMs();
            if (vanillaMs <= 0)
            {
                return 0f;
            }

            if (!ModConfig.EnableMimicTuning.Value || !ShouldScaleCooltime)
            {
                return vanillaMs * 0.001f;
            }

            return ScalePossessionCooltimeMs(vanillaMs) * 0.001f;
        }
    }
}

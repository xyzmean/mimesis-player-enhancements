using System;

namespace MimesisPlayerEnhancement.Features.SpectatorTransition;

internal static class SpectatorTransitionApplier
{
    private const long MinDyingWaitTimeMs = 1;
    private const float MinDeadCameraSeconds = 0.01f;

    internal static bool IsEnabled => ModConfig.EnableSpectatorTransition.Value;

    internal static long ScaleDyingWaitTime(long vanillaMs)
    {
        if (!IsEnabled)
            return vanillaMs;

        var scaled = (long)Math.Round(vanillaMs * ModConfig.DyingWaitTimeMultiplier.Value);
        if (scaled < MinDyingWaitTimeMs && vanillaMs > 0)
            return MinDyingWaitTimeMs;

        return scaled;
    }

    internal static float ScaleDeadCameraDuration(float vanillaSeconds)
    {
        if (!IsEnabled)
            return vanillaSeconds;

        var scaled = vanillaSeconds * ModConfig.DeadCameraDurationMultiplier.Value;
        if (scaled < MinDeadCameraSeconds && vanillaSeconds > 0f)
            return MinDeadCameraSeconds;

        return scaled;
    }
}

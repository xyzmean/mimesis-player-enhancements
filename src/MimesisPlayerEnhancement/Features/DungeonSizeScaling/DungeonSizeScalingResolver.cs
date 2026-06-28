namespace MimesisPlayerEnhancement.Features.DungeonSizeScaling
{
    internal static class DungeonSizeScalingResolver
    {
        internal static float GetLengthMultiplier(int playerCount)
        {
            if (!ModConfig.EnableDungeonSizeScaling.Value)
            {
                return 1f;
            }

            float baseMultiplier = ModConfig.DungeonSizeMultiplier.Value;
            if (baseMultiplier < 0f)
            {
                baseMultiplier = 0f;
            }

            float playerScale = GetPlayerScale(playerCount);
            return baseMultiplier * playerScale;
        }

        internal static float GetPlayerScale(int playerCount)
        {
            if (!ModConfig.AutoScaleDungeonSizeByPlayerCount.Value)
            {
                return 1f;
            }

            int baseline = ModConfig.DungeonSizeBaselinePlayerCount.Value;
            return playerCount <= baseline ? 1f : playerCount / (float)baseline;
        }
    }
}

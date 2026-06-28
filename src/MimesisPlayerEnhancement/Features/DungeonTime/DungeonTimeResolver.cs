namespace MimesisPlayerEnhancement.Features.DungeonTime
{
    internal static class DungeonTimeResolver
    {
        internal static double GetBonusSeconds(int playerCount)
        {
            if (!ModConfig.EnableDungeonTime.Value)
            {
                return 0d;
            }

            int baseline = ModConfig.DungeonTimeBaselinePlayerCount.Value;
            return playerCount <= baseline ? 0d : (double)((playerCount - baseline) * ModConfig.ExtraShiftSecondsPerPlayerAboveBaseline.Value);
        }

        internal static long GetBonusMilliseconds(int playerCount)
        {
            double bonusSeconds = GetBonusSeconds(playerCount);
            return bonusSeconds <= 0d ? 0L : (long)(bonusSeconds * 1000d);
        }
    }
}

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
            if (playerCount <= baseline)
            {
                return 0d;
            }

            return (playerCount - baseline) * ModConfig.ExtraShiftSecondsPerPlayerAboveBaseline.Value;
        }

        internal static long GetBonusMilliseconds(int playerCount)
        {
            return (long)(GetBonusSeconds(playerCount) * 1000d);
        }
    }
}

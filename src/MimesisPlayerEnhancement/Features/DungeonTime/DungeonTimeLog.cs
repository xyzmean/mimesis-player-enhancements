namespace MimesisPlayerEnhancement.Features.DungeonTime
{
    internal static class DungeonTimeLog
    {
        private const string Feature = "DungeonTime";

        internal static void InfoApplied(int playerCount, long bonusMs, long newSessionEndTime)
        {
            double bonusSeconds = bonusMs / 1000d;
            ModLog.Info(
                Feature,
                $"Shift extended — players={playerCount}, baseline={ModConfig.DungeonTimeBaselinePlayerCount.Value}, " +
                $"+{bonusSeconds:0.##}s ({ModConfig.ExtraShiftSecondsPerPlayerAboveBaseline.Value:0.##}s/player above baseline), " +
                $"newSessionEndTime={newSessionEndTime}");
        }

        internal static void DebugSkipped(string reason)
        {
            ModLog.Debug(Feature, $"Shift extension skipped — {reason}");
        }
    }
}

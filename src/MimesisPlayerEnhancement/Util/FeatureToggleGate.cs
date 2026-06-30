namespace MimesisPlayerEnhancement.Util
{
    /// <summary>
    /// Neutral values returned by feature resolvers when the master Enable* toggle is off.
    /// Enforcement: (1) resolvers return these, (2) appliers/patches early-return,
    /// (3) live-state features revert mutations in SyncFromConfig when disabled.
    /// </summary>
    internal static class FeatureToggleGate
    {
        internal const float NeutralMultiplier = 1f;
    }
}

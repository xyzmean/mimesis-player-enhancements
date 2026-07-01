namespace MimesisPlayerEnhancement.Features.JoinAnytime
{
    /// <summary>
    /// Helpers for SetLobbyPublicCoroutine IL patches and public-room writes.
    /// </summary>
    internal static class JoinAnytimePublicLobbyTools
    {
        /// <summary>
        /// When the host requested a public lobby, do not let a stale ESC toggle downgrade PublicRoom.
        /// </summary>
        internal static bool CoercePublicRoomWriteFlag(bool isPublicRequested, bool toggleOrFallbackFlag)
        {
            if (!ModConfig.EnableJoinAnytime.Value)
            {
                return toggleOrFallbackFlag;
            }

            if (isPublicRequested)
            {
                return true;
            }

            return toggleOrFallbackFlag;
        }
    }
}

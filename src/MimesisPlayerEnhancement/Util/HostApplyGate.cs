using System;
using ReluNetwork.ConstEnum;

namespace MimesisPlayerEnhancement.Util
{
    internal static class HostApplyGate
    {
        internal static bool IsParticipantClient()
        {
            return JoinAnytimeHub.GetPdata()?.ClientMode == NetworkClientMode.Participant;
        }

        internal static bool ShouldApplyHostOnlyFeature(Func<bool>? isFeatureEnabled = null)
        {
            if (isFeatureEnabled != null && !isFeatureEnabled())
            {
                return false;
            }

            if (IsParticipantClient())
            {
                return false;
            }

            // Solo/local play often has no network host flags yet; pdata may also be null early on.
            return JoinAnytimeHub.GetPdata() == null || JoinAnytimeHub.GetPdata()?.ClientMode == NetworkClientMode.Host || MimesisSaveManager.IsHost();
        }
    }
}

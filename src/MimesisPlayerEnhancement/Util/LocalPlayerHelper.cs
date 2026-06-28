using System.Reflection;

namespace MimesisPlayerEnhancement.Util
{
    internal static class LocalPlayerHelper
    {
        private const BindingFlags InstanceMemberFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        internal static ulong TryGetLocalSteamId()
        {
            try
            {
                PlatformMgr platformMgr = MonoSingleton<PlatformMgr>.Instance;
                FieldInfo pathField = typeof(PlatformMgr).GetField("_uniqueUserPath", InstanceMemberFlags);
                string? userPath = pathField?.GetValue(platformMgr) as string;
                return !string.IsNullOrEmpty(userPath) && ulong.TryParse(userPath, out ulong localSteam)
                    ? localSteam
                    : 0;
            }
            catch
            {
                return 0;
            }
        }

        internal static bool IsLocalSteamId(ulong steamId)
        {
            if (steamId == 0)
            {
                return false;
            }

            try
            {
                PlatformMgr platformMgr = MonoSingleton<PlatformMgr>.Instance;
                FieldInfo pathField = typeof(PlatformMgr).GetField("_uniqueUserPath", InstanceMemberFlags);
                string? userPath = pathField?.GetValue(platformMgr) as string;
                return !string.IsNullOrEmpty(userPath)
                       && ulong.TryParse(userPath, out ulong localSteam)
                       && localSteam == steamId;
            }
            catch
            {
                return false;
            }
        }
    }
}

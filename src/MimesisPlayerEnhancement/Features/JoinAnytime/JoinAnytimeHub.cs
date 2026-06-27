using System.Reflection;

namespace MimesisPlayerEnhancement.Features.JoinAnytime;

internal static class JoinAnytimeHub
{
    private const BindingFlags InstanceFlags =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    private static readonly FieldInfo? PdataField =
        typeof(Hub).GetField("pdata", InstanceFlags);

    private static readonly FieldInfo? SteamInviteField =
        typeof(Hub).GetField("steamInviteDispatcher", InstanceFlags);

    internal static Hub.PersistentData? GetPdata()
    {
        if (Hub.s == null)
            return null;

        return PdataField?.GetValue(Hub.s) as Hub.PersistentData;
    }

    internal static SteamInviteDispatcher? GetSteamInviteDispatcher()
    {
        if (Hub.s == null)
            return null;

        return SteamInviteField?.GetValue(Hub.s) as SteamInviteDispatcher;
    }
}

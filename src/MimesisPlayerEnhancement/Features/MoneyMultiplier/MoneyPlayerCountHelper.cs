namespace MimesisPlayerEnhancement.Features.MoneyMultiplier
{
    internal static class MoneyPlayerCountHelper
    {
        internal static int ResolveFromRoom(MaintenanceRoom? room)
        {
            return Util.SessionPlayerCountHelper.ResolveFromRoom(room);
        }

        internal static int ResolveFromSession(GameSessionInfo? info)
        {
            return Util.SessionPlayerCountHelper.ResolveFromSession(info);
        }

        internal static int ResolveForItemPrices()
        {
            return Util.SessionPlayerCountHelper.ResolveFromSession();
        }
    }
}

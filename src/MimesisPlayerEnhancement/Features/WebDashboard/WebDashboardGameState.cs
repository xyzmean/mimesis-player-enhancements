namespace MimesisPlayerEnhancement.Features.WebDashboard
{
    internal static class WebDashboardGameState
    {
        private static bool _sessionSticky;
        private static int _cachedSaveSlotId = -1;

        internal static bool IsInSession()
        {
            Hub.PersistentData? pdata = JoinAnytimeHub.GetPdata();
            if (pdata == null)
            {
                return _sessionSticky = false;
            }

            if (!pdata.SessionJoined)
            {
                _cachedSaveSlotId = -1;
                return _sessionSticky = false;
            }

            if (pdata.main is InTramWaitingScene
                or GamePlayScene
                or MaintenanceScene
                or DeathMatchScene)
            {
                return _sessionSticky = true;
            }

            // Between maps the active scene can be unset while SessionJoined stays true.
            return _sessionSticky;
        }

        internal static bool IsHost()
        {
            return MimesisSaveManager.IsHost();
        }

        internal static int GetSaveSlotId()
        {
            if (MimesisSaveManager.TryGetActiveSaveSlotId(out int slotId))
            {
                _cachedSaveSlotId = slotId;
                return slotId;
            }

            return _sessionSticky && _cachedSaveSlotId >= 0 ? _cachedSaveSlotId : -1;
        }
    }
}

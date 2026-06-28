using System;

namespace MimesisPlayerEnhancement.Features.JoinAnytime
{
    internal static class LobbyVisibilityHelper
    {
        private const string Feature = "JoinAnytime";

        private static bool? _lastLoggedHostPublic;

        internal static void SyncHostPreference(bool wantsPublic, string source)
        {
            if (_lastLoggedHostPublic == wantsPublic)
            {
                return;
            }

            _lastLoggedHostPublic = wantsPublic;
            ModLog.Debug(
                Feature,
                $"Host lobby visibility -> {(wantsPublic ? "public" : "private")} ({source})");
        }

        internal static void OnLobbyCreated(SteamInviteDispatcher dispatcher, bool isOpenForRandomMatch)
        {
            if (!ModConfig.EnableJoinAnytime.Value)
            {
                return;
            }

            SyncHostPreference(isOpenForRandomMatch, "CreateLobby");
            SyncHostPreference(JoinAnytimeHub.IsHostLobbyPublic(dispatcher), "CreateLobby state");
        }

        internal static void OnSetLobbyPublicCompleted(SteamInviteDispatcher dispatcher, bool requestedPublic)
        {
            if (!ModConfig.EnableJoinAnytime.Value)
            {
                return;
            }

            bool hostWantsPublic = JoinAnytimeHub.IsHostLobbyPublic(dispatcher);
            SyncHostPreference(hostWantsPublic, "SetLobbyPublic");
            ApplyPresence(dispatcher, hostWantsPublic, requestedPublic);
        }

        internal static void RefreshAfterLobbyDataUpdate()
        {
            if (!ModConfig.EnableJoinAnytime.Value)
            {
                return;
            }

            SteamInviteDispatcher? dispatcher = JoinAnytimeHub.GetSteamInviteDispatcher();
            if (dispatcher == null)
            {
                return;
            }

            bool hostWantsPublic = JoinAnytimeHub.IsHostLobbyPublic(dispatcher);
            SyncHostPreference(hostWantsPublic, "lobby data refresh");

            try
            {
                if (hostWantsPublic)
                {
                    dispatcher.SetLobbyPublic(true);
                    ModLog.Debug(Feature, "Lobby refresh — keeping lobby public for join-anytime.");
                }
                else
                {
                    dispatcher.SetLobbyPublic(false);
                    ModLog.Debug(Feature, "Lobby refresh — keeping lobby private.");
                }
            }
            catch (Exception ex)
            {
                ModLog.Debug(Feature, $"Lobby visibility refresh failed — {ex.Message}");
            }
        }

        private static void ApplyPresence(SteamInviteDispatcher dispatcher, bool hostWantsPublic, bool requestedPublic)
        {
            if (hostWantsPublic)
            {
                dispatcher.SetPresenceInLobbyPublic();
            }
            else if (!requestedPublic)
            {
                dispatcher.SetPresenceInLobby();
            }
        }
    }
}

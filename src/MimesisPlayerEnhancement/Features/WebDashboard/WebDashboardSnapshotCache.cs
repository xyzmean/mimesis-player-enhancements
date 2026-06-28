using System.Collections.Generic;
using System.Threading;
using MimesisPlayerEnhancement.Features.Statistics.Models;
using MimesisPlayerEnhancement.Features.WebDashboard.Models;

namespace MimesisPlayerEnhancement.Features.WebDashboard
{
    internal static class WebDashboardSnapshotCache
    {
        private static WebDashboardSnapshot _snapshot = new();
        private static int _version;
        private static volatile bool _dirty = true;
        private static bool _lastInSession;
        private static List<WebDashboardPlayerDto> _lastPlayers = [];
        private static string? _lastLeaderboardJson;
        private static List<ulong> _lastConnectedSteamIds = [];

        internal static int Version => Volatile.Read(ref _version);

        internal static WebDashboardSnapshot Get()
        {
            return _snapshot;
        }

        internal static void MarkDirty()
        {
            _dirty = true;
        }

        internal static void Tick(string listenUrl)
        {
            bool inSession = WebDashboardGameState.IsInSession();
            if (inSession != _lastInSession)
            {
                _dirty = true;
                _lastInSession = inSession;
            }

            if (inSession)
            {
                _dirty = true;
            }

            if (!_dirty)
            {
                return;
            }

            Refresh(listenUrl);
            _dirty = false;
        }

        internal static void Refresh(string listenUrl)
        {
            bool inSession = WebDashboardGameState.IsInSession();
            bool isHost = WebDashboardGameState.IsHost();
            int saveSlotId = WebDashboardGameState.GetSaveSlotId();
            _lastInSession = inSession;

            WebDashboardSnapshot next = new()
            {
                Status = new WebDashboardStatusDto
                {
                    InSession = inSession,
                    IsHost = isHost,
                    SaveSlotId = saveSlotId,
                    ModVersion = VersionInfo.ModuleVersion,
                    ListenUrl = listenUrl,
                    SnapshotVersion = Version,
                    ConfigVersion = ModConfig.Version,
                },
            };

            if (!inSession)
            {
                _lastPlayers = [];
                _lastLeaderboardJson = null;
                _lastConnectedSteamIds = [];
                WebDashboardAvatarService.Clear();
            }
            else
            {
                List<WebDashboardPlayerDto> players = WebDashboardPlayerService.CollectPlayers();
                if (players.Count > 0)
                {
                    _lastPlayers = players;
                }
                else if (_lastPlayers.Count > 0)
                {
                    players = _lastPlayers;
                }

                next.Players = players;

                HashSet<ulong> avatarSteamIds = [];
                foreach (WebDashboardPlayerDto player in players)
                {
                    if (player.SteamId != 0)
                    {
                        _ = avatarSteamIds.Add(player.SteamId);
                    }
                }

                if (isHost && saveSlotId >= 0)
                {
                    List<ulong> connectedSteamIds = WebDashboardStatisticsBridge.GetConnectedSteamIds();
                    if (connectedSteamIds.Count > 0)
                    {
                        _lastConnectedSteamIds = connectedSteamIds;
                    }
                    else if (_lastConnectedSteamIds.Count > 0)
                    {
                        connectedSteamIds = _lastConnectedSteamIds;
                    }

                    next.ConnectedSteamIds = connectedSteamIds;
                    LeaderboardDocument? leaderboard = WebDashboardStatisticsBridge.GetLeaderboardDocument(saveSlotId);
                    string? leaderboardJson = leaderboard == null
                        ? null
                        : WebDashboardJson.SerializeLeaderboardResponse(leaderboard, connectedSteamIds);
                    if (!string.IsNullOrEmpty(leaderboardJson))
                    {
                        _lastLeaderboardJson = leaderboardJson;
                    }
                    else if (!string.IsNullOrEmpty(_lastLeaderboardJson))
                    {
                        leaderboardJson = _lastLeaderboardJson;
                    }

                    next.LeaderboardJson = leaderboardJson;

                    foreach (ulong steamId in connectedSteamIds)
                    {
                        if (steamId != 0)
                        {
                            _ = avatarSteamIds.Add(steamId);
                        }
                    }

                    if (leaderboard?.Entries != null)
                    {
                        foreach (LeaderboardEntry entry in leaderboard.Entries)
                        {
                            if (entry.SteamId == 0)
                            {
                                continue;
                            }

                            _ = avatarSteamIds.Add(entry.SteamId);

                            string? statsJson = WebDashboardStatisticsBridge.BuildPlayerStatsJson(
                                saveSlotId,
                                entry.SteamId,
                                leaderboard);
                            if (!string.IsNullOrEmpty(statsJson))
                            {
                                next.PlayerStatsJson[entry.SteamId] = statsJson;
                            }
                        }
                    }
                }

                WebDashboardAvatarService.PrewarmForPlayers([.. avatarSteamIds]);

                WebDashboardMinimapLayoutBuilder.EnsureLayout();
                next.MinimapLayout = WebDashboardMinimapLayoutBuilder.Current;
                next.MinimapMarkers = WebDashboardMinimapService.CollectMarkers(players, out WebDashboardMinimapTrainDto? train);
                next.MinimapTrain = train;
            }

            _ = Interlocked.Exchange(ref _snapshot, next);
            _ = Interlocked.Increment(ref _version);
            WebDashboardSseHub.NotifySnapshotChanged();
        }
    }
}

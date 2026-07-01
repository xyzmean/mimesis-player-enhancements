using System;
using System.Collections.Generic;
using MimesisPlayerEnhancement.Features.Statistics.Models;
using MimesisPlayerEnhancement.Util;

namespace MimesisPlayerEnhancement.Features.Statistics
{
    public static class StatisticsStore
    {
        private const string Feature = "Statistics";
        private const int MaxRecentSessions = 20;

        public static int MaxRecentSessionsPerPlayer => MaxRecentSessions;

        public static void LoadAllPlayersForSlot(int slotId, Dictionary<ulong, PlayerStatisticsDocument> cache)
        {
            SlotStatisticsDocument? slot = TryLoadSlot(slotId);
            if (slot?.Players == null)
            {
                return;
            }

            foreach (KeyValuePair<ulong, PlayerStatisticsDocument> pair in slot.Players)
            {
                cache[pair.Key] = NormalizePlayer(pair.Value, pair.Key);
            }
        }

        public static PlayerStatisticsDocument LoadPlayer(int slotId, ulong steamId)
        {
            SlotStatisticsDocument? slot = TryLoadSlot(slotId);
            if (slot?.Players != null && slot.Players.TryGetValue(steamId, out PlayerStatisticsDocument? doc))
            {
                return NormalizePlayer(doc, steamId);
            }

            return NewPlayerDocument(steamId);
        }

        internal static void SaveSlot(
            int slotId,
            IReadOnlyDictionary<ulong, PlayerStatisticsDocument> players,
            bool waitForCompletion = false)
        {
            string? path = SaveSidecarPaths.GetStatisticsPath(slotId);
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            BackgroundFileWriteQueue.EnqueueText(path, SerializeSlot(players), Feature, waitForCompletion);
            ModLog.Debug(Feature, $"Saved slot {slotId} statistics ({players.Count} players) -> {path}");
        }

        public static void DeleteStatisticsData(int slotId)
        {
            try
            {
                SaveSidecarPaths.DeleteSidecars(slotId, SidecarKind.Statistics);
                ModLog.Info(Feature, $"Deleted statistics data for slot {slotId}.");
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"DeleteStatisticsData: {ex.Message}");
            }
        }

        private static string SerializeSlot(IReadOnlyDictionary<ulong, PlayerStatisticsDocument> players)
        {
            Dictionary<ulong, PlayerStatisticsDocument> prepared = new(players.Count);
            foreach (KeyValuePair<ulong, PlayerStatisticsDocument> pair in players)
            {
                PreparePlayer(pair.Value);
                prepared[pair.Key] = pair.Value;
            }

            return StatisticsJson.SerializeSlot(new SlotStatisticsDocument
            {
                Version = SlotStatisticsDocument.CurrentVersion,
                Players = prepared,
            });
        }

        private static void PreparePlayer(PlayerStatisticsDocument doc)
        {
            TrimRecentSessions(doc);
            doc.Version = PlayerStatisticsDocument.CurrentVersion;
        }

        private static SlotStatisticsDocument? TryLoadSlot(int slotId)
        {
            string? path = SaveSidecarPaths.GetStatisticsPath(slotId);
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            string? json = AtomicFileIO.ReadText(path, Feature);
            if (string.IsNullOrEmpty(json))
            {
                return null;
            }

            SlotStatisticsDocument? slot = StatisticsJson.DeserializeSlot(json);
            if (slot == null)
            {
                ModLog.Warn(Feature, $"Corrupt statistics file — ignoring: {path}");
            }

            return slot;
        }

        private static PlayerStatisticsDocument NormalizePlayer(PlayerStatisticsDocument doc, ulong steamId)
        {
            doc.SteamId = steamId;
            doc.RecentSessions ??= [];
            doc.Global ??= new GlobalStats();
            doc.Global.Counters ??= new StatCounters();
            EnsureCounterDictionaries(doc.Global.Counters);
            if (doc.CurrentSession != null)
            {
                doc.CurrentSession.Counters ??= new StatCounters();
                EnsureCounterDictionaries(doc.CurrentSession.Counters);
            }

            foreach (SessionStats session in doc.RecentSessions)
            {
                session.Counters ??= new StatCounters();
                EnsureCounterDictionaries(session.Counters);
            }

            if (doc.Version < PlayerStatisticsDocument.CurrentVersion)
            {
                doc.Version = PlayerStatisticsDocument.CurrentVersion;
            }

            return doc;
        }

        private static PlayerStatisticsDocument NewPlayerDocument(ulong steamId)
        {
            return new()
            {
                SteamId = steamId,
                Global = new GlobalStats(),
                RecentSessions = [],
            };
        }

        private static void EnsureCounterDictionaries(StatCounters? counters)
        {
            if (counters == null)
            {
                return;
            }

            counters.MonsterKillsByMasterId ??= [];
            counters.DeathsByTrapType ??= [];
        }

        private static void TrimRecentSessions(PlayerStatisticsDocument doc)
        {
            while (doc.RecentSessions.Count > MaxRecentSessions)
            {
                doc.RecentSessions.RemoveAt(0);
            }
        }
    }
}

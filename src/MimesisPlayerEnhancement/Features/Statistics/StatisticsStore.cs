using System;
using System.Collections.Generic;
using System.IO;
using MimesisPlayerEnhancement.Features.Statistics.Models;
using MimesisPlayerEnhancement.Util;

namespace MimesisPlayerEnhancement.Features.Statistics
{
    public static class StatisticsStore
    {
        private const string Feature = "Statistics";
        private const string StatisticsFolder = "statistics";
        private const string PlayersFolder = "players";
        private const string LeaderboardFile = "leaderboard.json";
        private const int MaxRecentSessions = 20;

        public static int MaxRecentSessionsPerPlayer => MaxRecentSessions;

        public static string? GetStatisticsRootPath(int slotId)
        {
            string? slotPath = MimesisSaveManager.GetMimesisSlotPath(slotId);
            return string.IsNullOrEmpty(slotPath) ? null : Path.Combine(slotPath, StatisticsFolder);
        }

        public static string? GetPlayerFilePath(int slotId, ulong steamId)
        {
            string? root = GetStatisticsRootPath(slotId);
            return string.IsNullOrEmpty(root) ? null : Path.Combine(root, PlayersFolder, steamId + ".json");
        }

        public static string? GetLeaderboardFilePath(int slotId)
        {
            string? root = GetStatisticsRootPath(slotId);
            return string.IsNullOrEmpty(root) ? null : Path.Combine(root, LeaderboardFile);
        }

        public static void SafeWriteText(string filePath, string text)
        {
            _ = Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            AtomicFileIO.WriteText(filePath, text, Feature);
        }

        public static string? SafeReadText(string filePath)
        {
            return AtomicFileIO.ReadText(filePath, Feature);
        }

        public static PlayerStatisticsDocument LoadPlayer(int slotId, ulong steamId)
        {
            string? path = GetPlayerFilePath(slotId, steamId);
            if (string.IsNullOrEmpty(path))
            {
                return NewPlayerDocument(steamId);
            }

            string? json = SafeReadText(path);
            if (string.IsNullOrEmpty(json))
            {
                return NewPlayerDocument(steamId);
            }

            PlayerStatisticsDocument? doc = StatisticsJson.DeserializePlayer(json);
            if (doc == null)
            {
                ModLog.Warn(Feature, $"Corrupt player statistics — replacing with fresh document: {Path.GetFileName(path)}");
                return NewPlayerDocument(steamId);
            }

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

        public static void SavePlayer(int slotId, PlayerStatisticsDocument doc)
        {
            string? path = GetPlayerFilePath(slotId, doc.SteamId);
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            SafeWriteText(path, SerializePreparedPlayer(doc));
        }

        internal static string SerializePreparedPlayer(PlayerStatisticsDocument doc)
        {
            TrimRecentSessions(doc);
            doc.Version = PlayerStatisticsDocument.CurrentVersion;
            return StatisticsJson.SerializePlayer(doc);
        }

        public static void SaveLeaderboard(int slotId, LeaderboardDocument leaderboard)
        {
            string? path = GetLeaderboardFilePath(slotId);
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            leaderboard.Version = LeaderboardDocument.CurrentVersion;
            leaderboard.SaveSlotId = slotId;
            leaderboard.UpdatedAtUtc = DateTime.UtcNow;
            SafeWriteText(path, StatisticsJson.SerializeLeaderboard(leaderboard));
        }


        public static void LoadAllPlayersForSlot(int slotId, Dictionary<ulong, PlayerStatisticsDocument> cache)
        {
            string? root = GetStatisticsRootPath(slotId);
            if (string.IsNullOrEmpty(root))
            {
                return;
            }

            string playersDir = Path.Combine(root, PlayersFolder);
            if (!Directory.Exists(playersDir))
            {
                return;
            }

            foreach (string file in Directory.GetFiles(playersDir, "*.json"))
            {
                string name = Path.GetFileNameWithoutExtension(file);
                if (!ulong.TryParse(name, out ulong steamId))
                {
                    continue;
                }

                try
                {
                    cache[steamId] = LoadPlayer(slotId, steamId);
                }
                catch (Exception ex)
                {
                    ModLog.Warn(Feature, $"Skipping player statistics file {Path.GetFileName(file)} — {ex.Message}");
                }
            }
        }

        public static void DeleteStatisticsData(int slotId)
        {
            string? root = GetStatisticsRootPath(slotId);
            if (string.IsNullOrEmpty(root) || !Directory.Exists(root))
            {
                return;
            }

            try
            {
                Directory.Delete(root, true);
                ModLog.Info(Feature, $"Deleted statistics data for slot {slotId}.");
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"DeleteStatisticsData: {ex.Message}");
            }
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

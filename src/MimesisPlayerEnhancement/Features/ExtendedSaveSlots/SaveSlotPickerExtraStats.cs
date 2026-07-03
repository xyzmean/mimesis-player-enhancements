using System.Collections.Generic;
using MimesisPlayerEnhancement.Features.Persistence;
using MimesisPlayerEnhancement.Features.Statistics;
using MimesisPlayerEnhancement.Features.Statistics.Models;

namespace MimesisPlayerEnhancement.Features.ExtendedSaveSlots
{
    internal static class SaveSlotPickerExtraStats
    {
        private const string NoStatisticsText = "Статистика пока недоступна";

        internal static string FormatLine3(int slotId)
        {
            List<string> parts = [];

            LeaderboardDocument? leaderboard = LoadLeaderboard(slotId);
            if (leaderboard?.Entries is { Count: > 0 } entries)
            {
                AppendLeaderboardSummary(parts, entries);
            }

            int voiceEvents = SpeechEventFileStore.TryGetSavedSpeechEventCount(slotId);
            if (voiceEvents > 0)
            {
                parts.Add(voiceEvents + " голосовых событий");
            }

            return parts.Count == 0 ? NoStatisticsText : string.Join(" · ", parts);
        }

        internal static float ComputeRowHeight()
        {
            const float verticalPadding = 10f;
            const float line1Height = 24f;
            const float line2Height = 22f;
            const float line3Height = 18f;

            return verticalPadding + line1Height + line2Height + line3Height + verticalPadding;
        }

        private static void AppendLeaderboardSummary(List<string> parts, List<LeaderboardEntry> entries)
        {
            parts.Add(entries.Count + (entries.Count == 1 ? " игр." : " игр."));

            long sessions = 0;
            long survivalWins = 0;
            long survivalDeaths = 0;
            long revives = 0;
            long playSeconds = 0;

            foreach (LeaderboardEntry entry in entries)
            {
                sessions += entry.SessionsCompleted;
                survivalWins += entry.SurvivalWins;
                survivalDeaths += entry.SurvivalDeaths;
                revives += entry.Revives;
                playSeconds += entry.TotalConnectedSeconds;
            }

            if (sessions > 0)
            {
                parts.Add(sessions + (sessions == 1 ? " сессия" : " сессий"));
            }

            if (survivalWins > 0)
            {
                parts.Add(survivalWins + " поб. (выживание)");
            }

            if (survivalDeaths > 0)
            {
                parts.Add(survivalDeaths + " смерт. (выживание)");
            }

            if (revives > 0)
            {
                parts.Add(revives + " воскр.");
            }

            if (playSeconds >= 60)
            {
                parts.Add(FormatPlaytime(playSeconds));
            }
        }

        private static LeaderboardDocument? LoadLeaderboard(int slotId)
        {
            Dictionary<ulong, PlayerStatisticsDocument> players = [];
            StatisticsStore.LoadAllPlayersForSlot(slotId, players);
            return players.Count == 0 ? null : LeaderboardBuilder.Build(slotId, players.Values);
        }

        private static string FormatPlaytime(long totalSeconds)
        {
            if (totalSeconds < 60)
            {
                return totalSeconds + "с в игре";
            }

            long hours = totalSeconds / 3600;
            long minutes = (totalSeconds % 3600) / 60;
            return hours > 0
                ? minutes > 0 ? hours + "ч " + minutes + "м в игре" : hours + "ч в игре"
                : minutes + "м в игре";
        }
    }
}

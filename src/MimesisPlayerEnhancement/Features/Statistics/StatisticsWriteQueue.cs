using System;
using System.Collections.Generic;
using MimesisPlayerEnhancement.Features.Statistics.Models;
using MimesisPlayerEnhancement.Features.WebDashboard;
using MimesisPlayerEnhancement.Util;

namespace MimesisPlayerEnhancement.Features.Statistics
{
    internal static class StatisticsWriteQueue
    {
        private const float DebounceSeconds = 3f;

        private static readonly object DirtyLock = new();

        private static bool _slotDirty;
        private static float _nextFlushTime;
        private static int _loadedSlotId = -999;
        private static Func<IReadOnlyDictionary<ulong, PlayerStatisticsDocument>>? _allPlayersResolver;

        internal static void Configure(
            int slotId,
            Func<IReadOnlyDictionary<ulong, PlayerStatisticsDocument>> allPlayersResolver)
        {
            _loadedSlotId = slotId;
            _allPlayersResolver = allPlayersResolver;
        }

        internal static void Clear()
        {
            lock (DirtyLock)
            {
                _slotDirty = false;
            }

            _nextFlushTime = 0;
            _loadedSlotId = -999;
            _allPlayersResolver = null;
        }

        internal static void MarkDirty(ulong steamId)
        {
            _ = steamId;
            lock (DirtyLock)
            {
                _slotDirty = true;
                if (_nextFlushTime <= UnityEngine.Time.time)
                {
                    _nextFlushTime = UnityEngine.Time.time + DebounceSeconds;
                }
            }
        }

        internal static void ProcessDebounced()
        {
            bool dirty;
            lock (DirtyLock)
            {
                dirty = _slotDirty;
            }

            if (!dirty)
            {
                return;
            }

            if (UnityEngine.Time.time < _nextFlushTime)
            {
                return;
            }

            FlushDirty(waitForCompletion: false);
            _nextFlushTime = UnityEngine.Time.time + DebounceSeconds;
            WebDashboardSnapshotCache.MarkDirty();
        }

        internal static void FlushAllSync()
        {
            StatisticsTracker.PersistLoadedSlot(waitForCompletion: true);
            BackgroundFileWriteQueue.FlushAllSync();
        }

        internal static void FlushPendingWrites()
        {
            FlushDirty(waitForCompletion: false);
            _nextFlushTime = UnityEngine.Time.time + DebounceSeconds;
        }

        internal static void PersistImmediate(bool waitForCompletion)
        {
            lock (DirtyLock)
            {
                _slotDirty = false;
            }

            _nextFlushTime = UnityEngine.Time.time + DebounceSeconds;
            WriteLoadedSlot(waitForCompletion);
        }

        private static void FlushDirty(bool waitForCompletion)
        {
            lock (DirtyLock)
            {
                if (!_slotDirty)
                {
                    return;
                }

                _slotDirty = false;
            }

            WriteLoadedSlot(waitForCompletion);
        }

        private static void WriteLoadedSlot(bool waitForCompletion)
        {
            if (_loadedSlotId < 0 || _allPlayersResolver == null)
            {
                return;
            }

            StatisticsStore.SaveSlot(_loadedSlotId, _allPlayersResolver(), waitForCompletion);
        }
    }
}

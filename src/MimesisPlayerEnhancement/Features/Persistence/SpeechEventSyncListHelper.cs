using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using Mimic.Voice.SpeechSystem;

namespace MimesisPlayerEnhancement.Features.Persistence
{
    internal static class SpeechEventSyncListHelper
    {
        private const BindingFlags InstanceFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static readonly FieldInfo? EventsField =
            typeof(SpeechEventArchive).GetField("events", InstanceFlags);

        private static readonly ConcurrentDictionary<Type, SyncListAccessor> Accessors = new();

        internal static int CollectFromArchive(
            SpeechEventArchive archive,
            HashSet<long> seenIds,
            List<SpeechEvent> output)
        {
            if (EventsField == null)
            {
                return 0;
            }

            object? syncList = EventsField.GetValue(archive);
            if (syncList == null)
            {
                return 0;
            }

            if (!Accessors.TryGetValue(syncList.GetType(), out SyncListAccessor? accessor))
            {
                accessor = SyncListAccessor.TryCreate(syncList.GetType());
                if (accessor == null)
                {
                    return 0;
                }

                Accessors[syncList.GetType()] = accessor;
            }

            return accessor.Collect(syncList, seenIds, output);
        }

        internal static int CollectFromSyncList(
            object syncList,
            HashSet<long> seenIds,
            Action<SpeechEvent> onEvent)
        {
            if (!Accessors.TryGetValue(syncList.GetType(), out SyncListAccessor? accessor))
            {
                accessor = SyncListAccessor.TryCreate(syncList.GetType());
                if (accessor == null)
                {
                    return 0;
                }

                Accessors[syncList.GetType()] = accessor;
            }

            return accessor.Visit(syncList, seenIds, onEvent);
        }

        private sealed class SyncListAccessor
        {
            private readonly PropertyInfo _countProp;
            private readonly PropertyInfo _indexer;

            private SyncListAccessor(PropertyInfo countProp, PropertyInfo indexer)
            {
                _countProp = countProp;
                _indexer = indexer;
            }

            internal static SyncListAccessor? TryCreate(Type syncListType)
            {
                PropertyInfo? countProp = syncListType.GetProperty("Count", InstanceFlags);
                PropertyInfo? indexer = syncListType.GetProperty("Item", InstanceFlags, null, null, [typeof(int)], null);
                return countProp == null || indexer == null ? null : new SyncListAccessor(countProp, indexer);
            }

            internal int Collect(object syncList, HashSet<long> seenIds, List<SpeechEvent> output)
            {
                int added = 0;
                int count = (int)_countProp.GetValue(syncList);
                for (int i = 0; i < count; i++)
                {
                    if (_indexer.GetValue(syncList, [i]) is not SpeechEvent ev)
                    {
                        continue;
                    }

                    if (!seenIds.Add(ev.Id))
                    {
                        continue;
                    }

                    output.Add(ev);
                    added++;
                }

                return added;
            }

            internal int Visit(object syncList, HashSet<long> seenIds, Action<SpeechEvent> onEvent)
            {
                int visited = 0;
                int count = (int)_countProp.GetValue(syncList);
                for (int i = 0; i < count; i++)
                {
                    if (_indexer.GetValue(syncList, [i]) is not SpeechEvent ev)
                    {
                        continue;
                    }

                    if (!seenIds.Add(ev.Id))
                    {
                        continue;
                    }

                    onEvent(ev);
                    visited++;
                }

                return visited;
            }
        }
    }
}

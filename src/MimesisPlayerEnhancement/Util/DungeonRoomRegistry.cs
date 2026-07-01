using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace MimesisPlayerEnhancement.Util
{
    internal static class DungeonRoomAppliedSet
    {
        private sealed class Marker
        {
        }

        private static readonly ConditionalWeakTable<DungeonRoom, Marker> Applied = new();
        private static readonly ConditionalWeakTable<DungeonRoom, Marker> SkippedOnce = new();

        internal static bool IsApplied(DungeonRoom room)
        {
            return Applied.TryGetValue(room, out _);
        }

        internal static void MarkApplied(DungeonRoom room)
        {
            Applied.Add(room, new Marker());
        }

        internal static bool MarkSkippedOnce(DungeonRoom room)
        {
            if (SkippedOnce.TryGetValue(room, out _))
            {
                return false;
            }

            SkippedOnce.Add(room, new Marker());
            return true;
        }
    }

    internal sealed class DungeonRoomStateRegistry<T> where T : class
    {
        private readonly ConditionalWeakTable<DungeonRoom, T> _states = new();
        private readonly List<WeakReference<DungeonRoom>> _tracked = [];

        internal T GetOrCreate(DungeonRoom room, Func<T> factory)
        {
            if (!_states.TryGetValue(room, out T? state))
            {
                state = factory();
                _states.Add(room, state);
                Track(room);
            }

            return state;
        }

        internal bool TryGet(DungeonRoom room, out T state)
        {
            if (_states.TryGetValue(room, out T? value) && value != null)
            {
                state = value;
                return true;
            }

            state = null!;
            return false;
        }

        internal void Register(DungeonRoom room, T state)
        {
            _states.Add(room, state);
            Track(room);
        }

        internal IEnumerable<KeyValuePair<DungeonRoom, T>> EnumerateAll()
        {
            PruneDead();
            foreach (WeakReference<DungeonRoom> weak in _tracked)
            {
                if (!weak.TryGetTarget(out DungeonRoom? room))
                {
                    continue;
                }

                if (_states.TryGetValue(room, out T? state) && state != null)
                {
                    yield return new KeyValuePair<DungeonRoom, T>(room, state);
                }
            }
        }

        private void Track(DungeonRoom room)
        {
            PruneDead();
            foreach (WeakReference<DungeonRoom> weak in _tracked)
            {
                if (weak.TryGetTarget(out DungeonRoom? existing) && ReferenceEquals(existing, room))
                {
                    return;
                }
            }

            _tracked.Add(new WeakReference<DungeonRoom>(room));
        }

        private void PruneDead()
        {
            for (int i = _tracked.Count - 1; i >= 0; i--)
            {
                if (!_tracked[i].TryGetTarget(out _))
                {
                    _tracked.RemoveAt(i);
                }
            }
        }
    }
}

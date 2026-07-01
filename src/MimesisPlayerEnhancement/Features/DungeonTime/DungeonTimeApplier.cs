using System.Collections.Generic;
using MimesisPlayerEnhancement.Util;

namespace MimesisPlayerEnhancement.Features.DungeonTime
{
    internal static class DungeonTimeApplier
    {
        internal static void EnsureApplied(DungeonRoom room)
        {
            if (DungeonRoomAppliedSet.IsApplied(room))
            {
                return;
            }

            if (!HostApplyGate.ShouldApplyHostOnlyFeature(() => ModConfig.EnableDungeonTime.Value))
            {
                DungeonRoomAppliedSet.MarkApplied(room);
                if (!ModConfig.EnableDungeonTime.Value)
                {
                    DungeonTimeLog.DebugSkipped("EnableDungeonTime is off");
                }
                else
                {
                    DungeonTimeLog.DebugSkipped("not host");
                }

                return;
            }

            int playerCount = room.GetMemberCount();
            long bonusMs = DungeonTimeResolver.GetBonusMilliseconds(playerCount);
            if (bonusMs <= 0)
            {
                DungeonRoomAppliedSet.MarkApplied(room);
                DungeonTimeLog.DebugSkipped($"no bonus for players={playerCount}");
                return;
            }

            if (!DungeonRoomSessionTime.TryExtendEndTime(room, bonusMs, out long newEndTime))
            {
                DungeonRoomAppliedSet.MarkApplied(room);
                DungeonTimeLog.DebugSkipped("failed to extend session end time");
                return;
            }

            DungeonRoomAppliedSet.MarkApplied(room);
            DungeonTimeLog.InfoApplied(playerCount, bonusMs, newEndTime);
        }
    }
}

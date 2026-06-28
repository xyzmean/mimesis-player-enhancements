using System.Collections.Generic;
using System.Reflection;
using Bifrost.Cooked;
using MimesisPlayerEnhancement.Util;

namespace MimesisPlayerEnhancement.Features.JoinAnytime
{
    internal static class JoinAnytimeRoomTools
    {
        private const string Feature = "JoinAnytime";

        private const BindingFlags InstanceFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static readonly PropertyInfo? HubDatamanProperty =
            typeof(Hub).GetProperty("dataman", InstanceFlags);

        private static readonly PropertyInfo? HubVworldProperty =
            typeof(Hub).GetProperty("vworld", InstanceFlags);

        internal static void MoveCurrentPlayerToSnapshot(SessionContext context)
        {
            FieldInfo playerField = typeof(SessionContext).GetField("_vPlayer", InstanceFlags);
            if (playerField?.GetValue(context) is not VPlayer player)
            {
                ModLog.Warn(Feature, "MoveCurrentPlayerToSnapshot skipped — _vPlayer not found");
                return;
            }

            IVroom? oldRoom = player.VRoom;
            int oldActorId = player.ObjectID;

            ModLog.Debug(
                Feature,
                $"Removing old player actor={oldActorId} room={oldRoom?.GetType().Name ?? "null"}");

            oldRoom?.PendRemovePlayer(oldActorId, backup: false, kill: false);
            context.CreatePlayerSnapshot(true);
        }

        internal static string GetSceneNameFromMapId(int mapMasterId)
        {
            if (mapMasterId == 0)
            {
                return string.Empty;
            }

            if (!TryGetDataman(out DataManager dataman))
            {
                ModLog.Warn(Feature, "GetSceneNameFromMapId failed — dataman unavailable");
                return string.Empty;
            }

            MapMasterInfo? mapInfo = dataman.ExcelDataManager.GetMapInfo(mapMasterId);
            return mapInfo?.SceneName ?? string.Empty;
        }

        internal static string GetSceneNameFromDungeon(int dungeonMasterId, int pickedMapId = 0)
        {
            int resolvedMapId = pickedMapId != 0 ? pickedMapId : ResolvePickedMapId(null);
            if (resolvedMapId != 0)
            {
                return GetSceneNameFromMapId(resolvedMapId);
            }

            if (!TryGetDataman(out DataManager dataman))
            {
                ModLog.Warn(Feature, "GetSceneNameFromDungeon failed — dataman unavailable");
                return string.Empty;
            }

            DungeonMasterInfo? dungeonInfo = dataman.ExcelDataManager.GetDungeonInfo(dungeonMasterId);
            if (dungeonInfo == null)
            {
                return string.Empty;
            }

            if (dungeonInfo.MapIDs.IsDefaultOrEmpty)
            {
                return string.Empty;
            }

            return GetSceneNameFromMapId(dungeonInfo.MapIDs[0]);
        }

        internal static int ResolvePickedMapId(IVroom? room)
        {
            if (room is DungeonRoom dungeonRoom && dungeonRoom.PickedMapID != 0)
            {
                return dungeonRoom.PickedMapID;
            }

            if (TryGetVRoomManager(out VRoomManager? vroomManager) && vroomManager != null)
            {
                GameSessionInfo sessionInfo = vroomManager.GetGameSessionInfo();
                if (sessionInfo.PickedMapID != 0)
                {
                    return sessionInfo.PickedMapID;
                }
            }

            Hub.PersistentData? pdata = JoinAnytimeHub.GetPdata();
            return pdata?.PickedMapID ?? 0;
        }

        internal static IVroom? GetActiveDungeonRoom()
        {
            if (!TryGetVRoomManager(out VRoomManager? vroomManager) || vroomManager == null)
            {
                ModLog.Warn(Feature, "GetActiveDungeonRoom failed — VRoomManager unavailable");
                return null;
            }

            if (ReflectionHelper.GetFieldValue(vroomManager, "_vrooms") is not Dictionary<long, IVroom> rooms)
            {
                return null;
            }

            IVroom? bestOccupied = null;
            int bestOccupiedCount = -1;
            IVroom? newest = null;
            long newestRoomId = long.MinValue;

            foreach (IVroom room in rooms.Values)
            {
                if (room is not DungeonRoom)
                {
                    continue;
                }

                if (room.RoomID > newestRoomId)
                {
                    newest = room;
                    newestRoomId = room.RoomID;
                }

                int memberCount = room.GetMemberCount();
                if (memberCount <= 0)
                {
                    continue;
                }

                if (bestOccupied == null
                    || memberCount > bestOccupiedCount
                    || (memberCount == bestOccupiedCount && room.RoomID > bestOccupied.RoomID))
                {
                    bestOccupied = room;
                    bestOccupiedCount = memberCount;
                }
            }

            return bestOccupied ?? newest;
        }

        private static bool TryGetDataman(out DataManager dataman)
        {
            dataman = null!;
            if (Hub.s == null || HubDatamanProperty?.GetValue(Hub.s) is not DataManager resolved)
            {
                return false;
            }

            dataman = resolved;
            return true;
        }

        private static bool TryGetVRoomManager(out VRoomManager? vroomManager)
        {
            vroomManager = null;
            if (Hub.s == null || HubVworldProperty?.GetValue(Hub.s) is not VWorld vworld)
            {
                return false;
            }

            vroomManager = vworld.VRoomManager;
            return vroomManager != null;
        }
    }
}

namespace MimesisPlayerEnhancement.Features.RoomEntryDelay
{
    internal static class RoomEntryDelayFilter
    {
        internal static bool IsIndoorOutdoorCrossingDoor(LevelObject? origin)
        {
            if (origin == null)
            {
                return false;
            }

            if (origin.LevelObjectType is not (LevelObjectClientType.Teleporter
                or LevelObjectClientType.RandomTeleporter))
            {
                return false;
            }

            bool destinationIsToInDoor = origin switch
            {
                TeleporterLevelObject teleporter => teleporter.DestinationIsToInDoor,
                RandomTeleporterLevelObject randomTeleporter => randomTeleporter.DestinationIsToInDoor,
                _ => false,
            };

            return origin.IsIndoor != destinationIsToInDoor;
        }

        internal static bool ShouldScaleServerDelay(ILevelObjectInfo info, int fromState, int toState)
        {
            _ = fromState;
            _ = toState;
            return IsIndoorOutdoorCrossingDoor(info.DataOrigin);
        }

        internal static bool ShouldScaleClientTransition(StaticLevelObject levelObject, int fromState, int toState)
        {
            if (!IsIndoorOutdoorCrossingDoor(levelObject))
            {
                return false;
            }

            return levelObject.HasStateActionTransition(fromState, toState, out _);
        }
    }
}

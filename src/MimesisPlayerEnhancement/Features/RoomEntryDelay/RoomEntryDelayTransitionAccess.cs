using System;
using System.Reflection;
using HarmonyLib;

namespace MimesisPlayerEnhancement.Features.RoomEntryDelay
{
    internal static class RoomEntryDelayTransitionAccess
    {
        private const int TeleporterOnState = 1;
        private const int RandomTeleporterReadyState = 0;
        private const int RandomTeleporterStartState = 1;

        private static readonly FieldInfo? CurrentTransitionField =
            AccessTools.Field(typeof(StaticLevelObject), "currentTransition");

        private static readonly FieldInfo? FromStateField;
        private static readonly FieldInfo? ToStateField;

        static RoomEntryDelayTransitionAccess()
        {
            Type? transitionContextType = AccessTools.Inner(typeof(StaticLevelObject), "TransitionContext");
            if (transitionContextType == null)
            {
                return;
            }

            FromStateField = AccessTools.Field(transitionContextType, "FromState");
            ToStateField = AccessTools.Field(transitionContextType, "ToState");
        }

        internal static bool TryGetCurrentTransition(StaticLevelObject levelObject, out int fromState, out int toState)
        {
            fromState = 0;
            toState = 0;

            if (CurrentTransitionField == null
                || FromStateField == null
                || ToStateField == null)
            {
                return false;
            }

            if (CurrentTransitionField.GetValue(levelObject) is not object transitionContext)
            {
                return false;
            }

            fromState = (int)FromStateField.GetValue(transitionContext)!;
            toState = (int)ToStateField.GetValue(transitionContext)!;
            return true;
        }

        internal static bool TryGetPendingInteractTransition(StaticLevelObject levelObject, out float transitionSeconds)
        {
            transitionSeconds = 0f;

            int fromState = levelObject.State;
            int toState;

            switch (levelObject)
            {
                case TeleporterLevelObject:
                    toState = TeleporterOnState;
                    break;
                case RandomTeleporterLevelObject randomTeleporter:
                    if (randomTeleporter.State != RandomTeleporterReadyState)
                    {
                        return false;
                    }

                    toState = RandomTeleporterStartState;
                    break;
                default:
                    return false;
            }

            if (!levelObject.HasStateActionTransition(fromState, toState, out LevelObject.StateActionInfo? stateActionInfo)
                || stateActionInfo == null)
            {
                return false;
            }

            transitionSeconds = stateActionInfo.transitionDurtaion;
            return true;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Reflection;
using MimesisPlayerEnhancement.Util;
using ReluProtocol.Enum;
using UnityEngine;

namespace MimesisPlayerEnhancement.Features.Statistics
{
    internal static class DeathAttributionHelper
    {
        private const BindingFlags InstanceFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static readonly FieldInfo? LevelObjectsField =
            typeof(IVroom).GetField("_levelObjects", InstanceFlags);

        internal static bool TryResolveTrapDeath(VPlayer victim, ActorDyingSig sig, IVroom room, out TrapType trapType)
        {
            trapType = TrapType.Default;

            if (TryGetTrapTypeFromLevelObject(victim.OccupiedLevelObjectInfo, out trapType))
            {
                return true;
            }

            VActor? attacker = room.FindActorByObjectID(sig.attackerActorID);
            if (attacker is FieldSkillObject fieldSkill)
            {
                if (TryFindTrapNearPosition(room, fieldSkill.PositionVector, out trapType))
                {
                    return true;
                }
            }

            if (victim.ReasonOfDeath == ReasonOfDeath.Fall)
            {
                if (TryFindTrapNearPosition(room, victim.PositionVector, out trapType)
                    && IsWeightTrap(trapType))
                {
                    return true;
                }

                trapType = TrapType.Weight_Controller;
                return true;
            }

            if (victim.ReasonOfDeath == ReasonOfDeath.FieldSkill)
            {
                if (TryFindTrapNearPosition(room, victim.PositionVector, out trapType))
                {
                    return true;
                }
            }

            if (TryFindTrapNearPosition(room, victim.PositionVector, out trapType))
            {
                return true;
            }

            return false;
        }

        internal static string FormatTrapType(TrapType trapType)
        {
            return trapType.ToString();
        }

        internal static string FormatMonsterMasterId(int masterId)
        {
            return masterId.ToString();
        }

        private static bool TryGetTrapTypeFromLevelObject(ILevelObjectInfo? info, out TrapType trapType)
        {
            trapType = TrapType.Default;
            if (info is not StateLevelObjectInfo state)
            {
                return false;
            }

            if (state.DataOrigin != null)
            {
                TrapLevelObject? trapComponent = state.DataOrigin.GetComponent<TrapLevelObject>();
                if (trapComponent != null)
                {
                    trapType = trapComponent.TrapType;
                    return true;
                }
            }

            return TryParseTrapTypeFromName(state.Name, out trapType);
        }

        private static bool TryFindTrapNearPosition(IVroom room, Vector3 position, out TrapType trapType)
        {
            trapType = TrapType.Default;
            if (!TryGetLevelObjects(room, out Dictionary<int, ILevelObjectInfo>? levelObjects))
            {
                return false;
            }

            const float maxDistance = 8f;
            float bestDistance = float.MaxValue;
            TrapType bestType = TrapType.Default;
            bool found = false;

            foreach (ILevelObjectInfo info in levelObjects!.Values)
            {
                if (!TryGetTrapTypeFromLevelObject(info, out TrapType candidate))
                {
                    continue;
                }

                Vector3 trapPos = info is StateLevelObjectInfo state
                    ? new Vector3(state.Pos.x, state.Pos.y, state.Pos.z)
                    : Vector3.zero;
                float distance = Vector3.Distance(position, trapPos);
                if (distance > maxDistance || distance >= bestDistance)
                {
                    continue;
                }

                bestDistance = distance;
                bestType = candidate;
                found = true;
            }

            if (found)
            {
                trapType = bestType;
            }

            return found;
        }

        private static bool TryGetLevelObjects(IVroom room, out Dictionary<int, ILevelObjectInfo>? levelObjects)
        {
            levelObjects = null;
            if (LevelObjectsField?.GetValue(room) is not Dictionary<int, ILevelObjectInfo> dict)
            {
                return false;
            }

            levelObjects = dict;
            return true;
        }

        private static bool IsWeightTrap(TrapType trapType)
        {
            return trapType is TrapType.Weight_Controller or TrapType.Weight_Repeater;
        }

        private static bool TryParseTrapTypeFromName(string name, out TrapType trapType)
        {
            trapType = TrapType.Default;
            if (string.IsNullOrWhiteSpace(name))
            {
                return false;
            }

            string normalized = name.ToLowerInvariant();
            if (normalized.Contains("mine"))
            {
                trapType = TrapType.Mine_Invisible;
                return true;
            }

            if (normalized.Contains("sprinkler"))
            {
                trapType = TrapType.Sprinkler;
                return true;
            }

            if (normalized.Contains("weight") && normalized.Contains("repeat"))
            {
                trapType = TrapType.Weight_Repeater;
                return true;
            }

            if (normalized.Contains("weight"))
            {
                trapType = TrapType.Weight_Controller;
                return true;
            }

            if (normalized.Contains("corridor") || normalized.Contains("corrider"))
            {
                trapType = TrapType.Corrider;
                return true;
            }

            if (normalized.Contains("trap"))
            {
                trapType = TrapType.Default;
                return true;
            }

            return false;
        }
    }
}

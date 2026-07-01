using System;
using System.Reflection;
using ReluNetwork.ConstEnum;
using Steamworks;

namespace MimesisPlayerEnhancement.Features.JoinAnytime
{
    internal static class JoinAnytimeHub
    {
        private const string Feature = "JoinAnytime";

        private const BindingFlags InstanceFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static readonly FieldInfo? PdataField =
            typeof(Hub).GetField("pdata", InstanceFlags);

        private static readonly FieldInfo? SteamInviteField =
            typeof(Hub).GetField("steamInviteDispatcher", InstanceFlags);

        private static readonly FieldInfo? IsPublicRoomField =
            typeof(SteamInviteDispatcher).GetField("isPublicRoom", InstanceFlags);

        private static bool _warnedMissingFields;

        internal static Hub.PersistentData? GetPdata()
        {
            if (Hub.s == null)
            {
                return null;
            }

            if (PdataField == null)
            {
                WarnMissingFieldsOnce();
                return null;
            }

            return PdataField.GetValue(Hub.s) as Hub.PersistentData;
        }

        internal static SteamInviteDispatcher? GetSteamInviteDispatcher()
        {
            if (Hub.s == null)
            {
                return null;
            }

            if (SteamInviteField == null)
            {
                WarnMissingFieldsOnce();
                return null;
            }

            return SteamInviteField.GetValue(Hub.s) as SteamInviteDispatcher;
        }

        internal static bool IsHostLobbyPublic(SteamInviteDispatcher? dispatcher)
        {
            if (dispatcher == null)
            {
                return false;
            }

            if (JoinAnytimeLobbyController.HostWantsPublicMatchmaking())
            {
                return true;
            }

            if (IsPublicRoomField != null && IsPublicRoomField.GetValue(dispatcher) is true)
            {
                return true;
            }

            return ReadPublicRoomFromSteam(dispatcher);
        }

        internal static bool ReadPublicRoomFromSteam(SteamInviteDispatcher dispatcher)
        {
            if (dispatcher.joinedLobbyID == CSteamID.Nil)
            {
                return false;
            }

            try
            {
                string value = SteamMatchmaking.GetLobbyData(
                    dispatcher.joinedLobbyID,
                    SteamInviteDispatcher.IS_PUBLIC_KEY);
                return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                ModLog.Debug(Feature, $"Read PublicRoom lobby data failed — {ex.Message}");
                return false;
            }
        }

        internal static void SyncIsPublicRoomField(SteamInviteDispatcher dispatcher, bool isPublic)
        {
            if (IsPublicRoomField == null)
            {
                return;
            }

            try
            {
                IsPublicRoomField.SetValue(dispatcher, isPublic);
            }
            catch (Exception ex)
            {
                ModLog.Debug(Feature, $"Sync isPublicRoom field failed — {ex.Message}");
            }
        }

        internal static bool IsHost()
        {
            return GetPdata()?.ClientMode == NetworkClientMode.Host;
        }

        private static void WarnMissingFieldsOnce()
        {
            if (_warnedMissingFields)
            {
                return;
            }

            _warnedMissingFields = true;
            ModLog.Warn(Feature, "Hub reflection fields missing — pdata or steamInviteDispatcher not found");
        }
    }
}

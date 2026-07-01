using System.Reflection;
using ReluNetwork.ConstEnum;
using UnityEngine;
using UnityEngine.UI;

namespace MimesisPlayerEnhancement.Features.JoinAnytime
{
    internal static class JoinAnytimeInGameMenuTools
    {
        private const string Feature = "JoinAnytime";

        private const BindingFlags InstanceFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static FieldInfo? _uimanField;
        private static PropertyInfo? _uimanProperty;

        internal static void SyncPublicRoomToggle(bool isPublic)
        {
            UIPrefab_InGameMenu? menu = TryGetInGameMenu();
            if (menu == null)
            {
                return;
            }

            try
            {
                menu.UE_PublicRoomToggle.GetComponent<Toggle>().SetIsOnWithoutNotify(isPublic);
            }
            catch (System.Exception ex)
            {
                ModLog.Debug(Feature, $"Public room toggle sync failed — {ex.Message}");
            }
        }

        internal static void EnsurePublicRoomControlsAccessible(UIPrefab_InGameMenu menu)
        {
            if (!ModConfig.EnableJoinAnytime.Value
                || JoinAnytimeHub.GetPdata()?.ClientMode != NetworkClientMode.Host
                || menu == null)
            {
                return;
            }

            try
            {
                menu.UE_PublicRoom.GetComponent<RectTransform>().SetAsLastSibling();
                menu.UE_RoomPassword.GetComponent<RectTransform>().SetAsLastSibling();

                Toggle toggle = menu.UE_PublicRoomToggle.GetComponent<Toggle>();
                SteamInviteDispatcher? dispatcher = JoinAnytimeHub.GetSteamInviteDispatcher();
                bool isPublic = dispatcher != null && JoinAnytimeHub.IsHostLobbyPublic(dispatcher);
                SyncPublicRoomToggle(isPublic);

                toggle.onValueChanged.RemoveAllListeners();
                toggle.onValueChanged.AddListener(OnPublicRoomToggleChanged);
            }
            catch (System.Exception ex)
            {
                ModLog.Debug(Feature, $"Public room menu unlock failed — {ex.Message}");
            }
        }

        private static void OnPublicRoomToggleChanged(bool isPublic)
        {
            if (!ModConfig.EnableJoinAnytime.Value
                || JoinAnytimeHub.GetPdata()?.ClientMode != NetworkClientMode.Host)
            {
                return;
            }

            SteamInviteDispatcher? dispatcher = JoinAnytimeHub.GetSteamInviteDispatcher();
            if (dispatcher == null)
            {
                return;
            }

            ModLog.Debug(Feature, $"Public room toggle — isPublic={isPublic}");
            JoinAnytimeLobbyController.SetHostWantsPublicMatchmaking(isPublic);
            dispatcher.SetLobbyPublic(isPublic);
        }

        private static UIPrefab_InGameMenu? TryGetInGameMenu()
        {
            if (Hub.s == null)
            {
                return null;
            }

            UIManager? uiman = ResolveUiManager();
            return uiman?.inGameMenu;
        }

        private static UIManager? ResolveUiManager()
        {
            if (Hub.s == null)
            {
                return null;
            }

            _uimanProperty ??= typeof(Hub).GetProperty("uiman", InstanceFlags);
            if (_uimanProperty?.GetValue(Hub.s) is UIManager propertyManager)
            {
                return propertyManager;
            }

            _uimanField ??= typeof(Hub).GetField("uiman", InstanceFlags)
                ?? typeof(Hub).GetField("<uiman>k__BackingField", InstanceFlags);
            return _uimanField?.GetValue(Hub.s) as UIManager;
        }
    }
}

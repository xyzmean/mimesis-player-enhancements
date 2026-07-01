using ReluNetwork.ConstEnum;
using UnityEngine;
using UnityEngine.UI;

namespace MimesisPlayerEnhancement.Features.JoinAnytime
{
    internal static class JoinAnytimeInGameMenuTools
    {
        private const string Feature = "JoinAnytime";

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
                toggle.SetIsOnWithoutNotify(isPublic);

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
            dispatcher.SetLobbyPublic(isPublic);
        }
    }
}

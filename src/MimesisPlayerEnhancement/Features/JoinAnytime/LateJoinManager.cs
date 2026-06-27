using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using ReluNetwork.ConstEnum;
using ReluProtocol;
using ReluProtocol.C2S;
using UnityEngine;

namespace MimesisPlayerEnhancement.Features.JoinAnytime;

/// <summary>
/// Late-join flow adapted from Shlygly/MimesisJoinAnytime for MIMESIS 0.3.0.
/// </summary>
internal static class LateJoinManager
{
    private const string Feature = "JoinAnytime";

    internal static LateJoinRedirectState RedirectState { get; set; } = LateJoinRedirectState.None;
    internal static string PendingSceneName { get; set; } = string.Empty;

    private static bool voiceCheckRunning;

    internal static bool IsEnabled => ModConfig.EnableJoinAnytime.Value;

    internal static void Log(string message)
    {
        if (ModConfig.EnableDebugLogging.Value)
            ModLog.Debug(Feature, message);
    }

    internal static void OnServerLogin(SessionContext context)
    {
        if (!IsEnabled)
            return;

        Hub.PersistentData? pdata = JoinAnytimeHub.GetPdata();
        if (pdata?.ClientMode != NetworkClientMode.Host)
            return;

        if (pdata.main is not GamePlayScene gps)
            return;

        Log($"Login while in-game: uid={context.GetPlayerUID()} dungeon={gps.DungeonMasterID} seed={gps.RandDungeonSeed}");
        JoinAnytimeNetworkTools.SendOnPlayingStateToClient(context);
    }

    internal static void OnServerPlayerCreated(VPlayer player)
    {
        if (!IsEnabled)
            return;

        Hub.PersistentData? pdata = JoinAnytimeHub.GetPdata();
        if (pdata?.ClientMode != NetworkClientMode.Host || player.IsHost)
            return;

        Log($"VPlayer created: uid={player.UID} room={player.VRoom?.GetType().Name} main={pdata.main?.GetType().Name}");

        if (pdata.main is InTramWaitingScene
            && player.VRoom is MaintenanceRoom)
        {
            Log($"Pre-game join detected, sending MoveToWaitingRoomSig to uid={player.UID}");
            player.SendToMe(new MoveToWaitingRoomSig());
            return;
        }

        if (pdata.main is GamePlayScene
            && player.VRoom is MaintenanceRoom)
        {
            JoinAnytimeNetworkTools.SendOnPlayingStateToClient(player);
        }
    }

    internal static bool OnClientPacket(IMsg msg)
    {
        if (!IsEnabled)
            return true;

        Hub.PersistentData? pdata = JoinAnytimeHub.GetPdata();

        switch (msg)
        {
            case AllMemberEnterRoomSig all:
                if (ShouldIgnoreEarlyGameAllMemberEnterRoomSig(pdata, all))
                    return false;
                break;

            case MoveToWaitingRoomSig:
                if (pdata?.ClientMode != NetworkClientMode.Participant)
                    return true;

                Log($"Pre-game redirect signal received. main={pdata.main?.GetType().Name ?? "null"}");

                RedirectState = LateJoinRedirectState.PendingWaitingRoomRedirect;
                PendingSceneName = "InTramWaitingScene";

                if (pdata.main is MaintenanceScene)
                {
                    RedirectState = LateJoinRedirectState.None;
                    Hub.LoadScene("InTramWaitingScene");
                }

                break;

            case MoveToDungeonSig moveToDungeonSig:
                if (pdata == null)
                    return true;

                pdata.dungeonMasterID = moveToDungeonSig.selectedDungeonMasterID;
                pdata.randDungeonSeed = moveToDungeonSig.randDungeonSeed;
                Log($"MoveToDungeonSig: dungeon={pdata.dungeonMasterID} seed={pdata.randDungeonSeed}");
                break;

            case MakeRoomCompleteSig completeSig:
                if (pdata == null)
                    return true;

                pdata.completeMakingRoomSig = completeSig;

                if (completeSig.nextRoomInfo.roomMasterID != 0)
                    pdata.dungeonMasterID = completeSig.nextRoomInfo.roomMasterID;

                string sceneName = JoinAnytimeRoomTools.GetSceneNameFromDungeon(pdata.dungeonMasterID);
                Log(
                    $"MakeRoomCompleteSig: roomUID={completeSig.nextRoomInfo.roomUID}, " +
                    $"scene={sceneName}, main={pdata.main?.GetType().Name ?? "null"}");

                PendingSceneName = sceneName;

                if (pdata.ClientMode == NetworkClientMode.Participant)
                {
                    if (pdata.main is MaintenanceScene && !string.IsNullOrEmpty(sceneName))
                    {
                        Log($"Redirect now -> {sceneName}");
                        RedirectState = LateJoinRedirectState.EnteringDungeon;
                        Hub.LoadScene(sceneName);
                    }
                    else
                    {
                        RedirectState = LateJoinRedirectState.PendingDungeonRedirect;
                    }
                }

                break;
        }

        return true;
    }

    private static bool ShouldIgnoreEarlyGameAllMemberEnterRoomSig(
        Hub.PersistentData? pdata,
        AllMemberEnterRoomSig sig)
    {
        if (pdata?.ClientMode != NetworkClientMode.Participant)
            return false;

        if (pdata.main is not GamePlayScene gamePlayScene)
            return false;

        var enteringCompleteAllField = typeof(GamePlayScene).GetField(
            "EnteringCompleteAll",
            BindingFlags.NonPublic | BindingFlags.Instance);
        if (enteringCompleteAllField?.GetValue(gamePlayScene) is true)
            return false;

        if (RedirectState != LateJoinRedirectState.EnteringDungeon)
            return false;

        bool hasArriveCutscene =
            sig.enterCutsceneNames != null
            && sig.enterCutsceneNames.Contains("ArriveCutScene");

        if (hasArriveCutscene)
        {
            RedirectState = LateJoinRedirectState.None;
            PendingSceneName = string.Empty;
            return false;
        }

        Log($"Ignoring early AllMemberEnterRoomSig names={string.Join(",", sig.enterCutsceneNames ?? new List<string>())}");
        return true;
    }

    internal static void OnLoadScene(string sceneName)
    {
        if (!IsEnabled || RedirectState == LateJoinRedirectState.None)
            return;

        if (RedirectState == LateJoinRedirectState.EnteringDungeon
            && sceneName == PendingSceneName)
        {
            return;
        }

        if (sceneName != PendingSceneName)
        {
            RedirectState = LateJoinRedirectState.None;
            PendingSceneName = string.Empty;
        }
    }

    internal static void OnMaintenanceSceneStart()
    {
        if (!IsEnabled)
            return;

        Hub.PersistentData? pdata = JoinAnytimeHub.GetPdata();
        if (pdata == null)
            return;

        if (RedirectState == LateJoinRedirectState.PendingWaitingRoomRedirect)
        {
            Log("Pending pre-game redirect -> InTramWaitingScene");
            RedirectState = LateJoinRedirectState.None;
            Hub.LoadScene("InTramWaitingScene");
        }
        else if (RedirectState == LateJoinRedirectState.PendingDungeonRedirect)
        {
            if (string.IsNullOrEmpty(PendingSceneName))
                return;

            Log($"Pending in-game redirect -> {PendingSceneName}");
            RedirectState = LateJoinRedirectState.EnteringDungeon;
            Hub.LoadScene(PendingSceneName);
        }
    }

    internal static void EnsureVoiceConnected(VoiceManager voiceManager, VoiceMode mode)
    {
        if (!IsEnabled || voiceCheckRunning)
            return;

        Hub.PersistentData? pdata = JoinAnytimeHub.GetPdata();
        if (pdata?.ClientMode != NetworkClientMode.Participant)
            return;

        bool sceneAllowed = pdata.main switch
        {
            InTramWaitingScene => true,
            GamePlayScene => true,
            DeathMatchScene => true,
            _ => false,
        };

        if (!sceneAllowed || voiceManager.IsConnected())
            return;

        Log($"Voice reconnect: addr={pdata.GameServerAddressOrSteamId} relay={pdata.WithRelay}");
        voiceManager.StartCoroutine(ConnectVoiceDelayed(voiceManager));
    }

    private static IEnumerator ConnectVoiceDelayed(VoiceManager voiceManager)
    {
        voiceCheckRunning = true;

        yield return new WaitForSeconds(0.5f);

        Hub.PersistentData? pdata = JoinAnytimeHub.GetPdata();
        if (pdata != null && voiceManager != null && !voiceManager.IsConnected())
        {
            voiceManager.TryRecoverMicrophone();
            voiceManager.ConnectVoiceChat(pdata.GameServerAddressOrSteamId, pdata.WithRelay);
        }

        yield return new WaitForSeconds(2f);
        voiceCheckRunning = false;
    }

    internal static void OnServerEnterWaitingRoom(SessionContext context)
    {
        if (!IsEnabled || context == null || !context.ExistPlayer())
            return;

        Hub.PersistentData? pdata = JoinAnytimeHub.GetPdata();
        if (context.GetVRoomType() == VRoomType.Maintenance
            && pdata?.main is InTramWaitingScene)
        {
            Log("Moving player snapshot Maintenance -> Waiting");
            JoinAnytimeRoomTools.MoveCurrentPlayerToSnapshot(context);
        }
    }

    internal static void OnServerEnterDungeon(SessionContext context, long roomUid)
    {
        if (!IsEnabled || context == null || !context.ExistPlayer())
            return;

        Hub.PersistentData? pdata = JoinAnytimeHub.GetPdata();
        if (context.GetVRoomType() == VRoomType.Maintenance
            && pdata?.main is GamePlayScene)
        {
            Log($"Moving player snapshot Maintenance -> Dungeon, roomUID={roomUid}");
            JoinAnytimeRoomTools.MoveCurrentPlayerToSnapshot(context);
        }
    }

    internal static void KeepLobbyOpen()
    {
        if (!IsEnabled)
            return;

        try
        {
            SteamInviteDispatcher? dispatcher = JoinAnytimeHub.GetSteamInviteDispatcher();
            dispatcher?.SetLobbyPublic(true);
        }
        catch
        {
            /* Steam may be unavailable */
        }
    }
}

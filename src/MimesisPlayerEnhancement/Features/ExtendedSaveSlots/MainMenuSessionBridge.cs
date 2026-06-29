using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using MimesisPlayerEnhancement.Util;
using ReluProtocol;
using UnityEngine;

namespace MimesisPlayerEnhancement.Features.ExtendedSaveSlots
{
    internal static class MainMenuSessionBridge
    {
        private const string Feature = "ExtendedSaveSlots";

        private static MethodInfo? _createNewGameInSlotLoadTram;
        private static MethodInfo? _tryDeleteSaveGameData;
        private static MethodInfo? _createRoom;

        internal static void LoadSaveAndCreateRoom(
            MainMenu menu,
            UIPrefabScript pickerShell,
            int slotId,
            MMSaveGameData saveData)
        {
            UIManager? uiManager = SaveSlotGameAccess.TryGetUiManager();
            if (uiManager == null)
            {
                return;
            }

            uiManager.ui_escapeStack.Remove(pickerShell);
            pickerShell.Hide();

            Hub.PersistentData? pdata = SaveSlotGameAccess.TryGetPdata();
            if (pdata == null)
            {
                return;
            }
            pdata.SaveSlotID = slotId;
            pdata.StageCount = saveData.StageCount;
            pdata.CycleCount = saveData.CycleCount;
            pdata.Repaired = saveData.TramRepaired;
            pdata.TramUpgradeIDs = saveData.TramUpgradeList != null
                ? saveData.TramUpgradeList.Clone()
                : new List<int>();
            pdata.TramUpgradeIDs = pdata.TramUpgradeIDs.Distinct().ToList();
            pdata.IsMaintenanceMachineAvailable = saveData.TramRepaired;

            StartCreateRoom(menu);
        }

        internal static void CreateNewGameInSlot(MainMenu menu, UIPrefabScript pickerShell, int slotId)
        {
            MethodInfo? method = GetCreateNewGameInSlotLoadTram();
            if (method == null)
            {
                ModLog.Warn(Feature, "CreateNewGameInSlot(UIPrefab_LoadTram) not found.");
                return;
            }

            // MainMenu only needs Hide/escape-stack removal; LoadTram type is accepted by the game method.
            UIPrefab_LoadTram? loadTramShell = pickerShell as UIPrefab_LoadTram
                ?? pickerShell.GetComponent<UIPrefab_LoadTram>();
            if (loadTramShell == null)
            {
                UIManager? uiManager = SaveSlotGameAccess.TryGetUiManager();
                uiManager?.ui_escapeStack.Remove(pickerShell);
                pickerShell.Hide();
                Hub.PersistentData? pdata = SaveSlotGameAccess.TryGetPdata();
                if (pdata != null)
                {
                    pdata.ResetCycleInfos();
                    pdata.SaveSlotID = slotId;
                }

                StartCreateRoom(menu);
                return;
            }

            _ = method.Invoke(menu, [loadTramShell, slotId]);
        }

        internal static bool TryDeleteSaveGameData(MainMenu menu, int slotId)
        {
            MethodInfo? method = GetTryDeleteSaveGameData();
            if (method == null)
            {
                ModLog.Warn(Feature, "TryDeleteSaveGameData not found.");
                return false;
            }

            object? result = method.Invoke(menu, [slotId]);
            return result is bool deleted && deleted;
        }

        private static void StartCreateRoom(MainMenu menu)
        {
            MethodInfo? method = GetCreateRoom();
            if (method == null)
            {
                ModLog.Warn(Feature, "CreateRoom not found.");
                return;
            }

            if (method.Invoke(menu, null) is IEnumerator routine)
            {
                ((MonoBehaviour)menu).StartCoroutine(routine);
            }
        }

        private static MethodInfo? GetCreateNewGameInSlotLoadTram()
        {
            _createNewGameInSlotLoadTram ??= AccessTools.Method(
                typeof(MainMenu),
                "CreateNewGameInSlot",
                [typeof(UIPrefab_LoadTram), typeof(int)]);

            return _createNewGameInSlotLoadTram;
        }

        private static MethodInfo? GetTryDeleteSaveGameData()
        {
            _tryDeleteSaveGameData ??= AccessTools.Method(typeof(MainMenu), "TryDeleteSaveGameData", [typeof(int)]);
            return _tryDeleteSaveGameData;
        }

        private static MethodInfo? GetCreateRoom()
        {
            _createRoom ??= AccessTools.Method(typeof(MainMenu), "CreateRoom", System.Type.EmptyTypes);
            return _createRoom;
        }
    }
}

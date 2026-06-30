using System.Reflection;
using HarmonyLib;
using MimesisPlayerEnhancement.Util;
using ReluProtocol;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MimesisPlayerEnhancement.Features.ExtendedSaveSlots
{
    internal static class MainMenuSessionBridge
    {
        private const string Feature = "ExtendedSaveSlots";

        private static MethodInfo? _tryLoadSaveAndCreateRoom;
        private static MethodInfo? _handleNewGameSlotSelection;

        internal static void TryLoadSaveAndCreateRoom(
            MainMenu menu,
            UIPrefab_PublicRoomList pickerShell,
            UIPrefab_LoadTram loadTram,
            int slotId)
        {
            MethodInfo? method = GetTryLoadSaveAndCreateRoom();
            if (method == null)
            {
                ModLog.Warn(Feature, "TryLoadSaveAndCreateRoom not found.");
                return;
            }

            UIManager? uiManager = SaveSlotGameAccess.TryGetUiManager();
            if (uiManager != null)
            {
                uiManager.ui_escapeStack.Remove(pickerShell);
                pickerShell.Hide();
                TramSavePickerController.SetSavePickerOpen(false, pickerShell);
            }

            loadTram.InitSaveInfoList();
            _ = method.Invoke(menu, [loadTram, slotId]);
        }

        internal static void HandleNewGameSlotSelection(
            MainMenu menu,
            UIPrefab_NewTram newTram,
            UIPrefab_NewTramPopUp newTramPopUp,
            int slotId)
        {
            MethodInfo? method = GetHandleNewGameSlotSelection();
            if (method == null)
            {
                ModLog.Warn(Feature, "HandleNewGameSlotSelection not found.");
                return;
            }

            UIManager? uiManager = SaveSlotGameAccess.TryGetUiManager();
            UIPrefab_PublicRoomList? picker = TramSavePickerController.ActiveSavePickerList;
            if (uiManager != null && picker != null)
            {
                uiManager.ui_escapeStack.Remove(picker);
                picker.Hide();
                TramSavePickerController.SetSavePickerOpen(false, picker);
            }

            EventSystem.current?.SetSelectedGameObject(null);
            _ = method.Invoke(menu, [newTram, newTramPopUp, slotId]);
        }

        private static MethodInfo? GetTryLoadSaveAndCreateRoom()
        {
            _tryLoadSaveAndCreateRoom ??= AccessTools.Method(
                typeof(MainMenu),
                "TryLoadSaveAndCreateRoom",
                [typeof(UIPrefab_LoadTram), typeof(int)]);

            return _tryLoadSaveAndCreateRoom;
        }

        private static MethodInfo? GetHandleNewGameSlotSelection()
        {
            _handleNewGameSlotSelection ??= AccessTools.Method(
                typeof(MainMenu),
                "HandleNewGameSlotSelection",
                [typeof(UIPrefab_NewTram), typeof(UIPrefab_NewTramPopUp), typeof(int)]);

            return _handleNewGameSlotSelection;
        }
    }
}

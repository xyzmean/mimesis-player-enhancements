using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MimesisPlayerEnhancement.Util;
using ReluProtocol;
using Steamworks;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MimesisPlayerEnhancement.Features.ExtendedSaveSlots
{
    internal sealed class SaveSlotPickerPanel
    {
        private const string Feature = "ExtendedSaveSlots";
        private const BindingFlags InstanceFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static readonly FieldInfo? RoomListDataField =
            typeof(UIPrefab_PublicRoomList).GetField("roomListData", InstanceFlags);

        private static readonly MethodInfo? SetRoomListUiMethod =
            typeof(UIPrefab_PublicRoomList).GetMethod("SetRoomListUI", InstanceFlags);

        private static readonly FieldInfo? JoinTramUiField =
            typeof(UIPrefab_PublicRoomList).GetField("joinTramUI", InstanceFlags);

        private readonly MainMenu _mainMenu;
        private readonly UIPrefab_MainMenu _mainMenuUi;
        private readonly UIPrefab_LoadTram _loadTram;
        private readonly UIPrefab_NewTram _newTram;
        private readonly UIPrefab_NewTramPopUp _newTramPopUp;

        private UIPrefab_PublicRoomList? _list;
        private readonly Dictionary<int, MMSaveGameData> _saveCache = new();
        private Dictionary<CSteamID, SaveSlotRowContext> _rowContexts = new();

        internal SaveSlotPickerPanel(
            MainMenu mainMenu,
            UIPrefab_MainMenu mainMenuUi,
            UIPrefab_LoadTram loadTram,
            UIPrefab_NewTram newTram,
            UIPrefab_NewTramPopUp newTramPopUp)
        {
            _mainMenu = mainMenu;
            _mainMenuUi = mainMenuUi;
            _loadTram = loadTram;
            _newTram = newTram;
            _newTramPopUp = newTramPopUp;
        }

        internal bool IsOpen => _list != null && _list.gameObject.activeInHierarchy;

        internal UIPrefab_PublicRoomList? List => _list;

        internal bool TryGetCachedSave(int slotId, out MMSaveGameData? data) =>
            _saveCache.TryGetValue(slotId, out data);

        internal bool TryGetRowContext(CSteamID rowKey, out SaveSlotRowContext? context) =>
            _rowContexts.TryGetValue(rowKey, out context);

        internal bool TryOpen()
        {
            UIManager? uiManager = SaveSlotGameAccess.TryGetUiManager();
            if (uiManager == null)
            {
                ModLog.Warn(Feature, "UIManager unavailable; cannot show save picker.");
                return false;
            }

            if (_list == null)
            {
                _list = SaveSlotGameAccess.CreateSavePickerShell(uiManager);
            }

            if (_list == null)
            {
                ModLog.Warn(Feature, "Failed to create save picker shell from public room list prefab.");
                return false;
            }

            JoinTramUiField?.SetValue(_list, null);
            ConfigureHandlers(_list);
            RefreshSaveList();

            EventSystem.current?.SetSelectedGameObject(null);
            SaveSlotGameAccess.TryGetPdata()?.SaveSlotID = -1;

            if (!uiManager.ui_escapeStack.Contains(_list))
            {
                uiManager.ui_escapeStack.Add(_list);
            }

            TramSavePickerController.SetSavePickerOpen(true, _list);
            _list.Show();
            return _list.gameObject.activeInHierarchy;
        }

        internal void Close()
        {
            if (_list == null)
            {
                TramSavePickerController.SetSavePickerOpen(false, null);
                return;
            }

            UIManager? uiManager = SaveSlotGameAccess.TryGetUiManager();
            uiManager?.ui_escapeStack.Remove(_list);
            _list.Hide();
            TramSavePickerController.SetSavePickerOpen(false, _list);
            _mainMenuUi.Show();
        }

        internal void RefreshSaveList()
        {
            if (_list == null || RoomListDataField?.GetValue(_list) is not List<PublicRoomListData> roomListData)
            {
                return;
            }

            _saveCache.Clear();
            SaveSlotEntry? autosave = SaveSlotDiscovery.TryLoadAutosave();
            if (autosave != null)
            {
                _saveCache[autosave.SlotId] = autosave.Data;
            }

            foreach (SaveSlotEntry entry in SaveSlotDiscovery.GetManualSaves())
            {
                _saveCache[entry.SlotId] = entry.Data;
            }

            List<PublicRoomListData> rows = SaveSlotRoomListMapper.BuildRoomListData(out _rowContexts);
            roomListData.Clear();
            roomListData.AddRange(rows);

            if (rows.Count == 0)
            {
                _list.SetEmptyListText();
                return;
            }

            SetRoomListElementActive(_list, "UE_EmptyListText", false);
            SetRoomListUiMethod?.Invoke(_list, null);
        }

        internal void HandleRowClick(CSteamID rowKey, bool requestCreate)
        {
            if (!_rowContexts.TryGetValue(rowKey, out SaveSlotRowContext? context))
            {
                return;
            }

            if (context.IsEmpty || requestCreate)
            {
                MainMenuSessionBridge.HandleNewGameSlotSelection(_mainMenu, _newTram, _newTramPopUp, context.SlotId);
                return;
            }

            if (context.Entry == null)
            {
                return;
            }

            if (!context.Entry.Display.IsVersionCompatible)
            {
                ModLog.Debug(Feature, $"Save slot {context.SlotId} blocked by version mismatch.");
                if (context.SlotId <= 3)
                {
                    _loadTram.InitSaveInfoList();
                    _loadTram.CanNotLoadSaveData(context.SlotId);
                }

                return;
            }

            MainMenuSessionBridge.TryLoadSaveAndCreateRoom(_mainMenu, _list!, _loadTram, context.SlotId);
        }

        internal void HandleCreateFirstFreeSlot()
        {
            int slotId = SaveSlotDiscovery.FindFirstFreeManualSlot();
            if (slotId < 0)
            {
                ModLog.Warn(Feature, "All manual save slots are full.");
                return;
            }

            MainMenuSessionBridge.HandleNewGameSlotSelection(_mainMenu, _newTram, _newTramPopUp, slotId);
        }

        private void ConfigureHandlers(UIPrefab_PublicRoomList list)
        {
            SaveSlotTextHelper.SetText(GetRoomListText(list, "UE_Title"), SaveSlotGameAccess.GetL10NText("UI_PREFAB_MAIN_MENU_LOAD_TRAM"));
            list.OnButtonBack = _ =>
            {
                Close();
                EventSystem.current?.SetSelectedGameObject(null);
            };
            list.OnButtonRefresh = _ => HandleCreateFirstFreeSlot();
            list.UE_ButtonRefresh.gameObject.SetActive(true);
            ApplyButtonLabel(list.UE_ButtonRefresh.gameObject, "Create");
        }

        private static Component? GetRoomListText(UIPrefab_PublicRoomList list, string propertyName)
        {
            return typeof(UIPrefab_PublicRoomList).GetProperty(propertyName)?.GetValue(list) as Component;
        }

        private static void SetRoomListElementActive(UIPrefab_PublicRoomList list, string propertyName, bool active)
        {
            if (GetRoomListText(list, propertyName) is Component component)
            {
                component.gameObject.SetActive(active);
            }
        }

        private static void ApplyButtonLabel(GameObject buttonRoot, string label)
        {
            foreach (Component component in buttonRoot.GetComponentsInChildren<Component>(true))
            {
                if (component == null || component.GetType().Name is not ("TextMeshProUGUI" or "TMP_Text"))
                {
                    continue;
                }

                SaveSlotTextHelper.SetText(component, label);
            }
        }
    }
}

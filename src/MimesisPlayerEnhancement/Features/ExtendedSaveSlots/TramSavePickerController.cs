using System;
using HarmonyLib;
using MimesisPlayerEnhancement.Util;
using ReluProtocol;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MimesisPlayerEnhancement.Features.ExtendedSaveSlots
{
    internal static class TramSavePickerController
    {
        private const string Feature = "ExtendedSaveSlots";

        private static readonly Action<string> HostButtonHandler = static _ => { };

        private static MainMenu? _mainMenu;
        private static UIPrefab_MainMenu? _mainMenuUi;
        private static UIPrefab_LoadTram? _styleDonor;
        private static TramSavePickerPanel? _panel;

        internal static bool IsActive => ModConfig.EnableExtendedSaveSlots.Value;

        internal static bool TryGetCachedSave(int slotId, out MMSaveGameData? data)
        {
            if (_panel != null && _panel.TryGetCachedSave(slotId, out data))
            {
                return true;
            }

            data = null;
            return false;
        }

        internal static void Initialize(MainMenu mainMenu, UIPrefab_MainMenu mainMenuUi, UIPrefab_LoadTram styleDonor)
        {
            if (!IsActive)
            {
                return;
            }

            _mainMenu = mainMenu;
            _mainMenuUi = mainMenuUi;
            _styleDonor = styleDonor;
            RestoreMainMenuRoot();

            if (!TryEnsurePanelReady())
            {
                ModLog.Warn(Feature, "Failed to initialize tram save picker during MainMenu.Start.");
            }
            else
            {
                ModLog.Info(Feature, "Unified tram save picker initialized.");
            }

            ConfigureMainMenuButtons();
            ApplyFunnyHostButtonLabel();
            HideLoadButton();
        }

        internal static void OnMainMenuShown(UIPrefab_MainMenu mainMenuUi)
        {
            if (!IsActive)
            {
                return;
            }

            _mainMenuUi = mainMenuUi;
            RestoreMainMenuRoot();
            ConfigureMainMenuButtons();
            ApplyFunnyHostButtonLabel();
            HideLoadButton();
        }

        internal static void OnJoinTramShellStarted(UIPrefab_JoinTram shell)
        {
            _panel?.OnJoinTramShellStarted(shell);
        }

        internal static void TryHandleHostButtonClick(UIPrefab_MainMenu mainMenuUi)
        {
            if (!IsActive)
            {
                return;
            }

            _mainMenuUi = mainMenuUi;
            ConfigureMainMenuButtons();

            if (!TryEnsurePanelReady())
            {
                ModLog.Warn(Feature, "Save picker is not ready; falling back to vanilla New Tram.");
                TryOpenVanillaNewTram();
                return;
            }

            if (_panel!.TryOpen())
            {
                ModLog.Info(Feature, "Save picker opened.");
                return;
            }

            ModLog.Warn(Feature, "Save picker failed to open; falling back to vanilla New Tram.");
            TryOpenVanillaNewTram();
        }

        internal static void OpenPicker()
        {
            if (!IsActive)
            {
                return;
            }

            if (!TryEnsurePanelReady())
            {
                ModLog.Warn(Feature, "Save picker is not ready; could not open.");
                return;
            }

            if (_panel!.TryOpen())
            {
                ModLog.Info(Feature, "Save picker opened.");
            }
        }

        private static void TryOpenVanillaNewTram()
        {
            Type? newTramType = AccessTools.TypeByName("UIPrefab_NewTram");
            if (newTramType == null)
            {
                ModLog.Warn(Feature, "Vanilla New Tram UI type not found.");
                return;
            }

            UIPrefabScript? newTram = FindUiInstance(newTramType);
            if (newTram == null)
            {
                ModLog.Warn(Feature, "Vanilla New Tram UI instance not found.");
                return;
            }

            UIManager? uiManager = SaveSlotGameAccess.TryGetUiManager();
            if (uiManager == null)
            {
                return;
            }

            EventSystem.current?.SetSelectedGameObject(null);
            SaveSlotGameAccess.TryGetPdata()?.SaveSlotID = -1;

            AccessTools.Method(newTramType, "InitSaveInfoList")?.Invoke(newTram, null);

            if (!uiManager.ui_escapeStack.Contains(newTram))
            {
                uiManager.ui_escapeStack.Add(newTram);
            }

            newTram.Show();
        }

        private static UIPrefabScript? FindUiInstance(Type uiType)
        {
            foreach (UnityEngine.Object obj in Resources.FindObjectsOfTypeAll(uiType))
            {
                if (obj is UIPrefabScript script)
                {
                    return script;
                }
            }

            return null;
        }

        private static bool TryEnsurePanelReady()
        {
            if (_panel != null)
            {
                if (_panel.IsShellAlive)
                {
                    return true;
                }

                ModLog.Warn(Feature, "Save picker shell was destroyed; recreating.");
                _panel = null;
            }

            if (_mainMenuUi == null)
            {
                ModLog.Warn(Feature, "Main menu UI reference is missing.");
                return false;
            }

            _mainMenu ??= FindMainMenu();
            _styleDonor ??= FindStyleDonor();

            if (_mainMenu == null || _styleDonor == null)
            {
                ModLog.Warn(Feature, "MainMenu or LoadTram style donor not found.");
                return false;
            }

            _styleDonor.Hide();

            UIPrefab_JoinTram? shell = SaveSlotGameAccess.CreateSavePickerShell(_mainMenuUi);
            if (shell == null)
            {
                ModLog.Warn(Feature, "Failed to create save picker shell from Join Tram prefab.");
                return false;
            }

            _panel = shell.gameObject.AddComponent<TramSavePickerPanel>();
            _panel.Configure(shell, _mainMenuUi, _mainMenu, _styleDonor);
            return true;
        }

        private static void ConfigureMainMenuButtons()
        {
            if (_mainMenuUi == null)
            {
                return;
            }

            _mainMenuUi.OnHostButton = HostButtonHandler;
            MainMenuButtonWiring.RegisterHandler(
                _mainMenuUi,
                _mainMenuUi.UE_HostButton,
                HostButtonHandler,
                UIPrefab_MainMenu.UEID_HostButton);

            MainMenuButtonWiring.RegisterHandler(
                _mainMenuUi,
                _mainMenuUi.UE_LoadButton,
                static _ => { },
                UIPrefab_MainMenu.UEID_LoadButton);
        }

        private static void HideLoadButton()
        {
            if (_mainMenuUi?.UE_LoadButton != null)
            {
                _mainMenuUi.UE_LoadButton.gameObject.SetActive(false);
            }
        }

        private static void RestoreMainMenuRoot()
        {
            if (_mainMenuUi?.UE_rootNode != null)
            {
                _mainMenuUi.UE_rootNode.gameObject.SetActive(true);
            }
        }

        private static void ApplyFunnyHostButtonLabel()
        {
            if (_mainMenuUi?.UE_HostButton == null)
            {
                return;
            }

            ApplyLabelToButton(_mainMenuUi.UE_HostButton.gameObject, FunnyTramMenuLabels.PickRandom());
        }

        private static void ApplyLabelToButton(GameObject buttonRoot, string label)
        {
            Component[] components = buttonRoot.GetComponentsInChildren<Component>(true);
            bool applied = false;
            foreach (Component component in components)
            {
                if (component == null || component.GetType().Name is not ("TextMeshProUGUI" or "TMP_Text"))
                {
                    continue;
                }

                SaveSlotTextHelper.SetText(component, label);
                applied = true;
            }

            if (!applied)
            {
                ModLog.Warn(Feature, "Could not find HostButton label text to replace.");
            }
        }

#pragma warning disable CS0618
        private static MainMenu? FindMainMenu() => UnityEngine.Object.FindObjectOfType<MainMenu>();

        private static UIPrefab_LoadTram? FindStyleDonor() => UnityEngine.Object.FindObjectOfType<UIPrefab_LoadTram>(true);
#pragma warning restore CS0618
    }
}

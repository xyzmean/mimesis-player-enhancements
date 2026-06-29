using System;
using HarmonyLib;
using MimesisPlayerEnhancement.Util;
using ReluProtocol;

namespace MimesisPlayerEnhancement.Features.ExtendedSaveSlots
{
    public static class ExtendedSaveSlotsPatches
    {
        private const string Feature = "ExtendedSaveSlots";

        public static void Apply(HarmonyLib.Harmony harmony)
        {
            HarmonyPatchHelper.PatchApplyResult result = HarmonyPatchHelper.ApplyPatchTypes(
                harmony,
                Feature,
                HarmonyPatchHelper.GetNestedPatchTypes(typeof(ExtendedSaveSlotsPatches)));

            HarmonyPatchHelper.LogPatchSummary(Feature, result);
        }

        [HarmonyPatch(typeof(MMSaveGameData), nameof(MMSaveGameData.CheckSaveSlotID))]
        internal static class CheckSaveSlotIdPrefix
        {
            [HarmonyPrefix]
            private static bool Prefix(int slotID, bool includeAutoSlot, ref bool __result)
            {
                if (!ModConfig.EnableExtendedSaveSlots.Value)
                {
                    return true;
                }

                if (slotID == -1)
                {
                    __result = false;
                    return false;
                }

                if (includeAutoSlot && slotID == SaveSlotLimits.AutosaveSlotId)
                {
                    __result = true;
                    return false;
                }

                int maxManual = SaveSlotDiscovery.GetMaxManualSlots();
                __result = slotID >= SaveSlotLimits.MinManualSlotId && slotID <= maxManual;
                return false;
            }
        }

        [HarmonyPatch(typeof(UIPrefab_LoadTram), nameof(UIPrefab_LoadTram.GetLoadedSaveData))]
        internal static class GetLoadedSaveDataPostfix
        {
            [HarmonyPostfix]
            private static void Postfix(int slotID, ref MMSaveGameData __result)
            {
                if (!TramSavePickerController.IsActive)
                {
                    return;
                }

                if (TramSavePickerController.TryGetCachedSave(slotID, out MMSaveGameData? cached) && cached != null)
                {
                    __result = cached;
                }
            }
        }

        [HarmonyPatch(typeof(MainMenu), "Start")]
        internal static class MainMenuStartPostfix
        {
            [HarmonyPostfix]
            private static void Postfix(MainMenu __instance)
            {
                if (!TramSavePickerController.IsActive)
                {
                    return;
                }

#pragma warning disable CS0618
                UIPrefab_LoadTram? loadTram = UnityEngine.Object.FindObjectOfType<UIPrefab_LoadTram>(true);
#pragma warning restore CS0618
                UIPrefab_MainMenu? mainMenuUi = AccessTools.Field(typeof(MainMenu), "ui_mainmenu")
                    .GetValue(__instance) as UIPrefab_MainMenu;

                if (loadTram == null || mainMenuUi == null)
                {
                    ModLog.Warn(Feature, "Failed to initialize tram save picker — UI references missing.");
                    return;
                }

                TramSavePickerController.Initialize(__instance, mainMenuUi, loadTram);
            }
        }

        [HarmonyPatch(typeof(UIPrefab_MainMenu), "OnEnable")]
        internal static class MainMenuOnEnablePostfix
        {
            [HarmonyPostfix]
            private static void Postfix(UIPrefab_MainMenu __instance)
            {
                TramSavePickerController.OnMainMenuShown(__instance);
            }
        }

        [HarmonyPatch(typeof(UIPrefab_MainMenu), "Start")]
        internal static class MainMenuUiStartPostfix
        {
            [HarmonyPostfix]
            private static void Postfix(UIPrefab_MainMenu __instance)
            {
                TramSavePickerController.OnMainMenuShown(__instance);
            }
        }

        [HarmonyPatch(typeof(UIPrefabScript), "OnButtonClick")]
        internal static class MainMenuHostButtonClickPrefix
        {
            [HarmonyPrefix]
            private static bool Prefix(UIPrefabScript __instance, string _id)
            {
                if (__instance is not UIPrefab_MainMenu mainMenuUi)
                {
                    return true;
                }

                if (!MainMenuButtonWiring.IsHostButtonElement(__instance, _id, mainMenuUi.UE_HostButton))
                {
                    return true;
                }

                if (!TramSavePickerController.IsActive)
                {
                    return true;
                }

                TramSavePickerController.TryHandleHostButtonClick(mainMenuUi);
                return false;
            }
        }

        [HarmonyPatch(typeof(UIPrefab_MainMenu), nameof(UIPrefab_MainMenu.OnHostButton), MethodType.Setter)]
        internal static class MainMenuHostButtonSetterPrefix
        {
            [HarmonyPrefix]
            private static void Prefix(ref Action<string> value)
            {
                if (!TramSavePickerController.IsActive)
                {
                    return;
                }

                // Click handling is done in OnButtonClick prefix; suppress vanilla New Tram handler.
                value = static _ => { };
            }
        }

        [HarmonyPatch(typeof(UIPrefab_JoinTram), "Start")]
        internal static class JoinTramStartPostfix
        {
            [HarmonyPostfix]
            private static void Postfix(UIPrefab_JoinTram __instance)
            {
                TramSavePickerController.OnJoinTramShellStarted(__instance);
            }
        }
    }
}

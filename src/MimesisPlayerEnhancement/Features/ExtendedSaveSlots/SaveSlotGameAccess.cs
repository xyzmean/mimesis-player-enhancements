using System.Reflection;
using HarmonyLib;
using MimesisPlayerEnhancement.Util;
using ReluProtocol;
using UnityEngine;

namespace MimesisPlayerEnhancement.Features.ExtendedSaveSlots
{
    internal static class SaveSlotGameAccess
    {
        private const BindingFlags InstanceFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static readonly MethodInfo? GetL10NTextMethod =
            AccessTools.Method(typeof(Hub), "GetL10NText", [typeof(string), typeof(object[])]);

        private static readonly MethodInfo? LoadSaveMethod =
            AccessTools.Method(typeof(PlatformMgr), "Load")?.MakeGenericMethod(typeof(MMSaveGameData));

        private static readonly MethodInfo? InstantiatePublicRoomListMethod =
            AccessTools.Method(typeof(UIManager), "InstatiateUIPrefab")?.MakeGenericMethod(typeof(UIPrefab_PublicRoomList));

        private static FieldInfo? _uimanField;
        private static PropertyInfo? _uimanProperty;

        internal static string GetL10NText(string key, params object[] formattingArgs)
        {
            if (GetL10NTextMethod != null)
            {
                return GetL10NTextMethod.Invoke(null, [key, formattingArgs]) as string ?? key;
            }

            return key;
        }

        internal static UIManager? TryGetUiManager()
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

        internal static Hub.PersistentData? TryGetPdata()
        {
            return GameSessionAccess.TryGetPdata();
        }

        internal static UIPrefab_PublicRoomList? CreateSavePickerShell(UIManager uiManager)
        {
            if (uiManager.prefab_PublicRoomList == null || InstantiatePublicRoomListMethod == null)
            {
                return null;
            }

            object topHeight = System.Enum.Parse(typeof(eUIHeight), "Top");
            return InstantiatePublicRoomListMethod.Invoke(
                uiManager,
                [uiManager.prefab_PublicRoomList, topHeight]) as UIPrefab_PublicRoomList;
        }

        internal static MMSaveGameData? LoadSaveData(PlatformMgr platformMgr, string fileName)
        {
            if (LoadSaveMethod == null)
            {
                return null;
            }

            try
            {
                return LoadSaveMethod.Invoke(platformMgr, [fileName]) as MMSaveGameData;
            }
            catch
            {
                return null;
            }
        }

#pragma warning disable CS0618
        internal static UIPrefab_LoadTram? TryFindHiddenLoadTram() =>
            Object.FindObjectOfType<UIPrefab_LoadTram>(true);

        internal static UIPrefab_NewTram? TryFindHiddenNewTram() =>
            Object.FindObjectOfType<UIPrefab_NewTram>(true);

        internal static UIPrefab_NewTramPopUp? TryFindHiddenNewTramPopUp() =>
            Object.FindObjectOfType<UIPrefab_NewTramPopUp>(true);
#pragma warning restore CS0618
    }
}

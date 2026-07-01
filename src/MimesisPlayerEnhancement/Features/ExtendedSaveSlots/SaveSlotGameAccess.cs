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

        internal static object? TryGetInputManager()
        {
            if (Hub.s == null)
            {
                return null;
            }

            FieldInfo? field = typeof(Hub).GetField("inputman", InstanceFlags)
                ?? typeof(Hub).GetField("<inputman>k__BackingField", InstanceFlags);
            PropertyInfo? property = typeof(Hub).GetProperty("inputman", InstanceFlags);
            return field?.GetValue(Hub.s) ?? property?.GetValue(Hub.s);
        }

        internal static void TryPlaySfx(string sfxId)
        {
            if (string.IsNullOrEmpty(sfxId) || Hub.s == null || !Application.isFocused)
            {
                return;
            }

            FieldInfo? field = typeof(Hub).GetField("audioman", InstanceFlags)
                ?? typeof(Hub).GetField("<audioman>k__BackingField", InstanceFlags);
            PropertyInfo? property = typeof(Hub).GetProperty("audioman", InstanceFlags);
            object? audioManager = field?.GetValue(Hub.s) ?? property?.GetValue(Hub.s);
            if (audioManager == null)
            {
                return;
            }

            MethodInfo? playSfx = audioManager.GetType().GetMethod(
                "PlaySfx",
                InstanceFlags,
                binder: null,
                [typeof(string)],
                modifiers: null);
            playSfx?.Invoke(audioManager, [sfxId]);
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

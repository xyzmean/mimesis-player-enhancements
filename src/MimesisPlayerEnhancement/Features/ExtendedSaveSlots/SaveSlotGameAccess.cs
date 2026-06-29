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

        private static readonly MethodInfo? InstantiateJoinTramMethod =
            AccessTools.Method(typeof(UIManager), "InstatiateUI")?.MakeGenericMethod(typeof(UIPrefab_JoinTram));

        private static FieldInfo? _uimanField;
        private static PropertyInfo? _uimanProperty;
        private static FieldInfo? _joinTramPrefabField;

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

        internal static Color GetMouseOverTextColor()
        {
            return TryGetUiManager()?.mouseOverTextColor ?? SaveSlotDisplayFormatter.DefaultTextColor;
        }

        internal static UIPrefab_JoinTram? CreateSavePickerShell(UIPrefab_MainMenu mainMenuUi)
        {
            UIManager? uiManager = TryGetUiManager();
            if (uiManager == null)
            {
                return null;
            }

            _joinTramPrefabField ??= typeof(UIPrefab_MainMenu).GetField("joinTramUIPrefab", InstanceFlags);
            if (_joinTramPrefabField?.GetValue(mainMenuUi) is not GameObject prefab || prefab == null)
            {
                return null;
            }

            if (InstantiateJoinTramMethod == null)
            {
                return null;
            }

            // Same layer as New/Load Tram overlays so the picker renders above the main menu.
            object topHeight = System.Enum.Parse(typeof(eUIHeight), "Top");
            return InstantiateJoinTramMethod.Invoke(uiManager, [prefab, topHeight]) as UIPrefab_JoinTram;
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

        internal static Component? GetLoadTramTextComponent(UIPrefab_LoadTram loadTram, string propertyName)
        {
            object? value = typeof(UIPrefab_LoadTram).GetProperty(propertyName)?.GetValue(loadTram);
            return value as Component;
        }
    }
}

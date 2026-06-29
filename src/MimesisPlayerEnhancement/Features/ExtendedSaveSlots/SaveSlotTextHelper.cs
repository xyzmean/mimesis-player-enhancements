using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace MimesisPlayerEnhancement.Features.ExtendedSaveSlots
{
    internal static class SaveSlotTextHelper
    {
        private static readonly Dictionary<System.Type, (PropertyInfo? Text, PropertyInfo? Color)> PropertyCache = new();

        internal static Component? FindTextComponent(GameObject root)
        {
            Component[] components = root.GetComponentsInChildren<Component>(true);
            foreach (Component component in components)
            {
                if (component == null)
                {
                    continue;
                }

                if (component.GetType().Name is "TextMeshProUGUI" or "TMP_Text")
                {
                    return component;
                }
            }

            return null;
        }

        internal static void SetText(Component? textComponent, string value)
        {
            if (textComponent == null)
            {
                return;
            }

            PropertyInfo? textProperty = GetTextProperty(textComponent);
            textProperty?.SetValue(textComponent, value, null);
        }

        internal static void SetColor(Component? textComponent, Color color)
        {
            if (textComponent == null)
            {
                return;
            }

            PropertyInfo? colorProperty = GetColorProperty(textComponent);
            colorProperty?.SetValue(textComponent, color, null);
        }

        internal static void ApplyDefaultColor(Component? textComponent)
        {
            SetColor(textComponent, SaveSlotDisplayFormatter.DefaultTextColor);
        }

        internal static void SetAlignment(Component? textComponent, bool upperLeft)
        {
            if (textComponent == null)
            {
                return;
            }

            PropertyInfo? alignmentProperty = textComponent.GetType().GetProperty(
                "alignment",
                BindingFlags.Instance | BindingFlags.Public);
            if (alignmentProperty == null)
            {
                return;
            }

            object value = Enum.Parse(alignmentProperty.PropertyType, upperLeft ? "TopLeft" : "Top");
            alignmentProperty.SetValue(textComponent, value, null);
        }

        private static PropertyInfo? GetTextProperty(Component textComponent)
        {
            return GetCachedProperties(textComponent).Text;
        }

        private static PropertyInfo? GetColorProperty(Component textComponent)
        {
            return GetCachedProperties(textComponent).Color;
        }

        private static (PropertyInfo? Text, PropertyInfo? Color) GetCachedProperties(Component textComponent)
        {
            System.Type textType = textComponent.GetType();
            if (!PropertyCache.TryGetValue(textType, out (PropertyInfo? Text, PropertyInfo? Color) cached))
            {
                cached = (
                    textType.GetProperty("text", BindingFlags.Instance | BindingFlags.Public),
                    textType.GetProperty("color", BindingFlags.Instance | BindingFlags.Public));
                PropertyCache[textType] = cached;
            }

            return cached;
        }
    }
}

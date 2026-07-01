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
            SetAlignment(textComponent, upperLeft ? TextAlignment.TopLeft : TextAlignment.Top);
        }

        internal static void SetMiddleCenterAlignment(Component? textComponent)
        {
            SetAlignment(textComponent, TextAlignment.MiddleCenter);
        }

        private static void SetAlignment(Component? textComponent, TextAlignment alignment)
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

            object value = Enum.ToObject(alignmentProperty.PropertyType, (int)alignment);
            alignmentProperty.SetValue(textComponent, value, null);
        }

        private enum TextAlignment
        {
            TopLeft = 257,
            Top = 258,
            MiddleCenter = 514,
        }

        internal static void ConfigureTextLayout(Component? textComponent, bool wordWrap, int overflowMode)
        {
            if (textComponent == null)
            {
                return;
            }

            System.Type textType = textComponent.GetType();
            PropertyInfo? wrapProp = textType.GetProperty(
                "enableWordWrapping",
                BindingFlags.Instance | BindingFlags.Public);
            wrapProp?.SetValue(textComponent, wordWrap, null);

            PropertyInfo? overflowProp = textType.GetProperty(
                "overflowMode",
                BindingFlags.Instance | BindingFlags.Public);
            if (overflowProp != null && overflowProp.PropertyType.IsEnum)
            {
                overflowProp.SetValue(textComponent, Enum.ToObject(overflowProp.PropertyType, overflowMode), null);
            }

            PropertyInfo? raycastProp = textType.GetProperty(
                "raycastTarget",
                BindingFlags.Instance | BindingFlags.Public);
            raycastProp?.SetValue(textComponent, false, null);
        }

        internal static void EnableRichText(Component? textComponent)
        {
            if (textComponent == null)
            {
                return;
            }

            PropertyInfo? richTextProp = textComponent.GetType().GetProperty(
                "richText",
                BindingFlags.Instance | BindingFlags.Public);
            richTextProp?.SetValue(textComponent, true, null);
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

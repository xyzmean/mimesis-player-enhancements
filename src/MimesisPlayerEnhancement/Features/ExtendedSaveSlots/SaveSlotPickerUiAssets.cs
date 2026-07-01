using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace MimesisPlayerEnhancement.Features.ExtendedSaveSlots
{
    internal sealed class SaveSlotPickerUiAssets
    {
        internal static readonly SaveSlotPickerUiAssets Fallback = new();

        internal Sprite? SlotButtonSprite { get; private set; }
        internal Image.Type SlotButtonImageType { get; private set; } = Image.Type.Sliced;
        internal Sprite? FooterButtonSprite { get; private set; }
        internal Image.Type FooterButtonImageType { get; private set; } = Image.Type.Sliced;
        internal Sprite? RowHighlightSprite { get; private set; }
        internal Image.Type RowHighlightImageType { get; private set; } = Image.Type.Sliced;
        internal Component? FontTemplate { get; private set; }
        internal Color SlotTextColor { get; private set; } = SaveSlotDisplayFormatter.DefaultTextColor;
        internal Color TitleTextColor { get; private set; } = Color.white;
        internal Color HoverTextColor { get; private set; } = Color.white;
        internal Color DisabledTextColor { get; private set; } = Color.gray;
        internal Color PanelBackdropColor { get; private set; } = new(0.06f, 0.06f, 0.08f, 0.94f);
        internal Color DimOverlayColor { get; private set; } = new(0f, 0f, 0f, 0.45f);
        internal string ButtonClickSfxId { get; private set; } = "ButtonClick";
        internal string ButtonHoverSfxId { get; private set; } = "ButtonHover";
        internal float SlotRowHeight { get; private set; } = 112f;
        internal float FooterButtonWidth { get; private set; } = 200f;
        internal float FooterButtonHeight { get; private set; } = 48f;

        internal static bool TryCapture(UIPrefab_MainMenu mainMenu, UIPrefab_LoadTram loadTram, out SaveSlotPickerUiAssets assets)
        {
            assets = new SaveSlotPickerUiAssets();

            CaptureImage(loadTram.UE_SavedFile1?.GetComponent<Image>(), out Sprite? slotSprite, out Image.Type slotType);
            assets.SlotButtonSprite = slotSprite;
            assets.SlotButtonImageType = slotType;

            CaptureImage(
                loadTram.UE_ButtonClose?.GetComponent<Image>() ?? mainMenu.UE_HostButton?.GetComponent<Image>(),
                out Sprite? footerSprite,
                out Image.Type footerType);
            assets.FooterButtonSprite = footerSprite;
            assets.FooterButtonImageType = footerType;

            UiPrefab_RoomCard? roomCard = FindRoomCardTemplate();
            if (roomCard != null)
            {
                CaptureImage(roomCard.UE_mouseover, out Sprite? highlightSprite, out Image.Type highlightType);
                assets.RowHighlightSprite = highlightSprite;
                assets.RowHighlightImageType = highlightType;
            }

            if (mainMenu.UE_HostButton != null)
            {
                assets.FontTemplate = SaveSlotTextHelper.FindTextComponent(mainMenu.UE_HostButton.gameObject);
            }

            if (assets.FontTemplate == null && loadTram.UE_SavedFile1 != null)
            {
                assets.FontTemplate = SaveSlotTextHelper.FindTextComponent(loadTram.UE_SavedFile1.gameObject);
            }

            RectTransform? slotRect = loadTram.UE_SavedFile1?.GetComponent<RectTransform>();
            if (slotRect != null && slotRect.rect.height > 1f)
            {
                assets.SlotRowHeight = Mathf.Max(slotRect.rect.height, 104f);
            }

            RectTransform? footerRect = loadTram.UE_ButtonClose?.GetComponent<RectTransform>();
            if (footerRect != null)
            {
                if (footerRect.rect.width > 1f)
                {
                    assets.FooterButtonWidth = footerRect.rect.width;
                }

                if (footerRect.rect.height > 1f)
                {
                    assets.FooterButtonHeight = footerRect.rect.height;
                }
            }

            UIManager? uiManager = SaveSlotGameAccess.TryGetUiManager();
            if (uiManager != null)
            {
                PropertyInfo? hoverColorProp = typeof(UIManager).GetProperty(
                    "mouseOverTextColor",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (hoverColorProp?.GetValue(uiManager) is Color hoverColor)
                {
                    assets.HoverTextColor = hoverColor;
                }
            }

            return assets.FontTemplate != null || assets.SlotButtonSprite != null;
        }

        internal void ApplyFont(Component? textComponent)
        {
            if (textComponent == null || FontTemplate == null)
            {
                return;
            }

            System.Type textType = textComponent.GetType();
            CopyProperty(FontTemplate, textComponent, "font");
            CopyProperty(FontTemplate, textComponent, "fontSharedMaterial");
            CopyProperty(FontTemplate, textComponent, "fontMaterial");

            PropertyInfo? extraPaddingProp = textType.GetProperty(
                "extraPadding",
                BindingFlags.Instance | BindingFlags.Public);
            extraPaddingProp?.SetValue(textComponent, true, null);
        }

        private static void CopyProperty(Component source, Component target, string propertyName)
        {
            PropertyInfo? targetProp = target.GetType().GetProperty(
                propertyName,
                BindingFlags.Instance | BindingFlags.Public);
            PropertyInfo? sourceProp = source.GetType().GetProperty(
                propertyName,
                BindingFlags.Instance | BindingFlags.Public);
            if (targetProp == null || sourceProp == null)
            {
                return;
            }

            object? value = sourceProp.GetValue(source);
            if (value != null)
            {
                targetProp.SetValue(target, value);
            }
        }

        private static void CaptureImage(Image? image, out Sprite? sprite, out Image.Type imageType)
        {
            sprite = image?.sprite;
            imageType = image?.type ?? Image.Type.Sliced;
        }

#pragma warning disable CS0618
        private static UiPrefab_RoomCard? FindRoomCardTemplate()
        {
            foreach (Object obj in Resources.FindObjectsOfTypeAll(typeof(UiPrefab_RoomCard)))
            {
                if (obj is UiPrefab_RoomCard card)
                {
                    return card;
                }
            }

            return null;
        }
#pragma warning restore CS0618
    }
}

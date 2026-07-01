using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MimesisPlayerEnhancement.Features.ExtendedSaveSlots
{
    internal sealed class SaveSlotPickerUi : MonoBehaviour
    {
        private SaveSlotPickerUiAssets _assets = SaveSlotPickerUiAssets.Fallback;
        private RectTransform _content = null!;
        private ScrollRect _scrollRect = null!;
        private GameObject? _emptyLabel;
        private readonly List<SaveSlotPickerRow> _rows = [];
        private SaveSlotPickerRow? _selectedRow;

        internal Button BackButton { get; private set; } = null!;
        internal Button NewTramButton { get; private set; } = null!;
        internal Button DeleteButton { get; private set; } = null!;
        internal Button LoadButton { get; private set; } = null!;

        internal event Action<SaveSlotPickerRow>? RowSelected;
        internal event Action<SaveSlotPickerRow>? RowDoubleClicked;
        internal event Action? BackClicked;
        internal event Action? NewTramClicked;
        internal event Action? DeleteClicked;
        internal event Action? LoadClicked;

        internal static SaveSlotPickerUi? Create(
            Transform parent,
            UIPrefab_MainMenu mainMenu,
            UIPrefab_LoadTram loadTram)
        {
            if (!SaveSlotPickerUiAssets.TryCapture(mainMenu, loadTram, out SaveSlotPickerUiAssets assets))
            {
                assets = SaveSlotPickerUiAssets.Fallback;
            }

            GameObject rootGo = SaveSlotPickerUiBuilder.CreateUiRoot(parent, "SaveSlotPickerUi");
            SaveSlotPickerUi ui = rootGo.AddComponent<SaveSlotPickerUi>();
            ui._assets = assets;
            ui.Build(rootGo.transform, loadTram);
            rootGo.SetActive(false);
            return ui;
        }

        internal bool IsVisible => gameObject.activeInHierarchy;

        internal void Show() => gameObject.SetActive(true);

        internal void Hide() => gameObject.SetActive(false);

        internal SaveSlotPickerRow? GetSelectedRow() => _selectedRow;

        internal void RebuildRows(IReadOnlyList<SaveSlotEntry> entries)
        {
            ClearRows();

            if (entries.Count == 0)
            {
                if (_emptyLabel == null)
                {
                    _emptyLabel = SaveSlotPickerUiBuilder
                        .CreateEmptyLabel(_content, _assets, "No save games found.")
                        .gameObject;
                }

                _emptyLabel.SetActive(true);
                return;
            }

            if (_emptyLabel != null)
            {
                _emptyLabel.SetActive(false);
            }

            foreach (SaveSlotEntry entry in entries)
            {
                SaveSlotPickerRow row = SaveSlotPickerUiBuilder.CreateSlotRow(
                    _content,
                    _assets,
                    entry,
                    OnRowSelected,
                    OnRowDoubleClicked);
                _rows.Add(row);
            }

            RefreshScrollLayout();
        }

        private void RefreshScrollLayout()
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(_content);
            _scrollRect.verticalNormalizedPosition = 1f;
        }

        internal void SetSelection(SaveSlotPickerRow? row)
        {
            if (_selectedRow != null)
            {
                _selectedRow.SetSelected(selected: false);
            }

            _selectedRow = row;
            if (_selectedRow != null)
            {
                _selectedRow.SetSelected(selected: true);
            }
        }

        internal void SetActionButtons(bool loadEnabled, bool deleteEnabled, bool newTramEnabled)
        {
            SaveSlotPickerChrome.SetButtonEnabled(
                LoadButton,
                loadEnabled,
                _assets.SlotTextColor,
                _assets.DisabledTextColor);
            SaveSlotPickerChrome.SetButtonEnabled(
                DeleteButton,
                deleteEnabled,
                _assets.SlotTextColor,
                _assets.DisabledTextColor);
            SaveSlotPickerChrome.SetButtonEnabled(
                NewTramButton,
                newTramEnabled,
                _assets.SlotTextColor,
                _assets.DisabledTextColor);
        }

        private void Build(Transform root, UIPrefab_LoadTram loadTram)
        {
            GameObject panel = SaveSlotPickerUiBuilder.CreateFullScreenPanel(root, _assets);
            SaveSlotPickerLayoutBands bands = SaveSlotPickerUiBuilder.CreateLayoutBands(panel.transform);
            bands.ScrollBand.SetAsLastSibling();

            string loadLabel = SaveSlotGameAccess.GetL10NText("UI_PREFAB_MAIN_MENU_LOAD_TRAM");
            string newLabel = SaveSlotGameAccess.GetL10NText("UI_PREFAB_MAIN_MENU_NEW_TRAM");
            SaveSlotPickerUiBuilder.CreateTitle(bands.TitleBand, _assets, loadLabel + " / " + newLabel);

            _content = SaveSlotPickerUiBuilder.CreateScrollView(bands.ScrollBand, out _scrollRect);

            RectTransform actionRow = SaveSlotPickerUiBuilder.CreateActionButtonRow(bands.ActionBand);
            NewTramButton = SaveSlotPickerUiBuilder.CreateFooterButton(
                actionRow,
                _assets,
                newLabel,
                expandWidth: true,
                () => NewTramClicked?.Invoke());
            DeleteButton = SaveSlotPickerUiBuilder.CreateFooterButton(
                actionRow,
                _assets,
                "Delete",
                expandWidth: true,
                () => DeleteClicked?.Invoke());
            LoadButton = SaveSlotPickerUiBuilder.CreateFooterButton(
                actionRow,
                _assets,
                loadLabel,
                expandWidth: true,
                () => LoadClicked?.Invoke());

            RectTransform backRow = SaveSlotPickerUiBuilder.CreateBackButtonRow(bands.BackBand);
            string backLabel = ReadButtonLabel(loadTram.UE_ButtonClose.gameObject) ?? "Back";
            BackButton = SaveSlotPickerUiBuilder.CreateFooterButton(
                backRow,
                _assets,
                backLabel,
                expandWidth: false,
                () => BackClicked?.Invoke());
        }

        private void OnRowSelected(SaveSlotPickerRow row)
        {
            SetSelection(row);
            RowSelected?.Invoke(row);
        }

        private void OnRowDoubleClicked(SaveSlotPickerRow row)
        {
            SetSelection(row);
            RowDoubleClicked?.Invoke(row);
        }

        private void ClearRows()
        {
            foreach (SaveSlotPickerRow row in _rows)
            {
                if (row != null)
                {
                    Destroy(row.gameObject);
                }
            }

            _rows.Clear();
            _selectedRow = null;
        }

        private static string? ReadButtonLabel(GameObject buttonRoot)
        {
            Component? text = SaveSlotTextHelper.FindTextComponent(buttonRoot);
            if (text == null)
            {
                return null;
            }

            System.Reflection.PropertyInfo? textProp = text.GetType().GetProperty(
                "text",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
            return textProp?.GetValue(text) as string;
        }

        private void OnDestroy() => ClearRows();
    }
}

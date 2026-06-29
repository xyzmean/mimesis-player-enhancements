using System;
using System.Collections;
using System.Collections.Generic;
using MimesisPlayerEnhancement.Util;
using ReluProtocol;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MimesisPlayerEnhancement.Features.ExtendedSaveSlots
{
    /// <summary>
    /// Standalone save picker overlay built on the Join Tram prefab shell (no baked slot labels).
    /// Load Tram is only used as a hidden style donor for row/button visuals.
    /// </summary>
    internal sealed class TramSavePickerPanel : MonoBehaviour
    {
        private const string Feature = "ExtendedSaveSlots";
        private const float DoubleClickWindowSeconds = 0.35f;
        private const float TitleBandFraction = 0.10f;
        private const float ListBandFraction = 0.80f;
        private const float ActionBandFraction = 0.10f;
        private const float HorizontalPadding = 16f;
        private const float ScrollBarWidth = 16f;
        private const float ScrollBarGap = 14f;
        private const float PanelScreenMargin = 24f;

        private UIPrefab_JoinTram? _shell;
        private UIPrefab_MainMenu? _mainMenuUi;
        private MainMenu? _mainMenu;
        private UIPrefab_LoadTram? _styleDonor;
        private bool _uiBuilt;

        private RectTransform? _panelRect;
        private RectTransform? _scrollRoot;
        private RectTransform? _scrollContent;
        private ScrollRect? _scrollRect;
        private GameObject? _rowTemplate;
        private GameObject? _autosaveRowTemplate;
        private Button? _backButton;
        private Button? _createButton;
        private Button? _joinButton;
        private Button? _deleteButton;
        private HoldButtonHelper? _deleteHoldHelper;
        private float _cachedRowWidth;
        private float _cachedRowHeight;
        private float _cachedAutosaveRowHeight;

        private readonly Dictionary<int, SaveSlotEntry> _saveCache = new();
        private readonly List<RowBinding> _manualRows = [];
        private readonly List<RowBinding> _autosaveRows = [];

        private int _selectedSlotId = -1;
        private float _lastClickTime;
        private int _lastClickSlotId = -1;
        private Coroutine? _blinkCoroutine;

        private sealed class RowBinding
        {
            internal int SlotId { get; set; }
            internal GameObject RowRoot { get; set; } = null!;
            internal Button Button { get; set; } = null!;
            internal Component? TextComponent { get; set; }
            internal SaveSlotDisplayInfo Display { get; set; } = null!;
        }

        internal UIPrefabScript Shell => _shell!;

        internal bool IsShellAlive => _shell != null;

        internal bool TryGetCachedSave(int slotId, out MMSaveGameData? data)
        {
            if (_saveCache.TryGetValue(slotId, out SaveSlotEntry? entry))
            {
                data = entry.Data;
                return true;
            }

            data = null;
            return false;
        }

        internal void Configure(
            UIPrefab_JoinTram shell,
            UIPrefab_MainMenu mainMenuUi,
            MainMenu mainMenu,
            UIPrefab_LoadTram styleDonor)
        {
            _shell = shell;
            _mainMenuUi = mainMenuUi;
            _mainMenu = mainMenu;
            _styleDonor = styleDonor;

            _shell.mainMenuUI = mainMenuUi;
            _shell.UE_joinTram.gameObject.SetActive(false);
            _shell.UE_joinTramCode.gameObject.SetActive(false);
            _shell.UE_joinTramPublic.gameObject.SetActive(false);
            _shell.UE_ButtonBack.gameObject.SetActive(false);

            ReapplyBackHandler();
        }

        internal void OnJoinTramShellStarted(UIPrefab_JoinTram shell)
        {
            if (!ReferenceEquals(_shell, shell))
            {
                return;
            }

            ReapplyBackHandler();
        }

        internal void ReapplyBackHandler()
        {
            if (_shell == null)
            {
                return;
            }

            // Back lives in the action bar; keep shell handler inert for escape-stack compatibility.
            _shell.OnButtonBack = _ => { };
        }

        internal bool TryOpen()
        {
            if (_shell == null || _mainMenuUi == null)
            {
                ModLog.Warn(Feature, "Save picker shell or main menu reference is missing.");
                return false;
            }

            try
            {
                EnsureUiBuilt();
            }
            catch (Exception ex)
            {
                ModLog.Error(Feature, $"Failed to build save picker UI — {ex.Message}");
                return false;
            }

            _selectedSlotId = -1;
            RefreshSaveList();
            UpdateActionButtons();
            UpdateSelectionVisuals();

            EventSystem.current?.SetSelectedGameObject(null);
            SaveSlotGameAccess.TryGetPdata()?.SaveSlotID = -1;

            _shell.mainMenuUI = _mainMenuUi;
            _shell.room = _mainMenuUi.UE_rootNode.gameObject;

            UIManager? uiManager = SaveSlotGameAccess.TryGetUiManager();
            if (uiManager == null)
            {
                ModLog.Warn(Feature, "UIManager unavailable; cannot show save picker.");
                return false;
            }

            if (!uiManager.ui_escapeStack.Contains(_shell))
            {
                uiManager.ui_escapeStack.Add(_shell);
            }

            _shell.Show();
            ReapplyBackHandler();
            return _shell.gameObject.activeInHierarchy;
        }

        internal void Open()
        {
            _ = TryOpen();
        }

        internal void Close()
        {
            if (_shell == null)
            {
                return;
            }

            UIManager? uiManager = SaveSlotGameAccess.TryGetUiManager();
            uiManager?.ui_escapeStack.Remove(_shell);
            _shell.Hide();
            _mainMenuUi?.Show();
        }

        private void EnsureUiBuilt()
        {
            if (_uiBuilt || _shell == null || _styleDonor == null)
            {
                return;
            }

            if (_styleDonor.UE_SavedFile1 == null || _styleDonor.UE_SavedFile0 == null)
            {
                throw new InvalidOperationException("Load Tram row templates are unavailable.");
            }

            _rowTemplate = _styleDonor.UE_SavedFile1.transform.parent.gameObject;
            _autosaveRowTemplate = _styleDonor.UE_SavedFile0.transform.parent.parent.gameObject;
            CacheRowMetrics();
            BuildLayout();
            _uiBuilt = true;
            ModLog.Debug(Feature, "Tram save picker panel built on Join Tram shell.");
        }

        private void CacheRowMetrics()
        {
            if (_styleDonor == null || _rowTemplate == null)
            {
                _cachedRowWidth = 640f;
                _cachedRowHeight = 56f;
                _cachedAutosaveRowHeight = 72f;
                return;
            }

            RectTransform loadRoot = _styleDonor.GetComponent<RectTransform>();
            LayoutRebuilder.ForceRebuildLayoutImmediate(loadRoot);

            RectTransform rowRect = _rowTemplate.GetComponent<RectTransform>();
            _cachedRowWidth = rowRect.rect.width > 1f ? rowRect.rect.width : loadRoot.rect.width;
            _cachedRowHeight = rowRect.rect.height > 1f ? rowRect.rect.height : 56f;

            if (_autosaveRowTemplate != null)
            {
                RectTransform autosaveRect = _autosaveRowTemplate.GetComponent<RectTransform>();
                _cachedAutosaveRowHeight = autosaveRect.rect.height > 1f ? autosaveRect.rect.height : _cachedRowHeight;
            }
            else
            {
                _cachedAutosaveRowHeight = _cachedRowHeight;
            }

            if (_cachedRowWidth < 1f)
            {
                _cachedRowWidth = 640f;
            }
        }

        private float MeasureMinPanelWidth()
        {
            return _cachedRowWidth + ScrollBarWidth + ScrollBarGap + (HorizontalPadding * 2f);
        }

        private void HideJoinTramShellDecorations()
        {
            if (_shell == null)
            {
                return;
            }

            foreach (Transform child in _shell.transform)
            {
                if (child.name == "ExtendedSavePickerPanel")
                {
                    continue;
                }

                child.gameObject.SetActive(false);
            }
        }

        private void BuildLayout()
        {
            if (_shell == null || _styleDonor == null || _rowTemplate == null || _mainMenuUi == null)
            {
                return;
            }

            HideJoinTramShellDecorations();

            RectTransform shellRect = _shell.GetComponent<RectTransform>();
            float minPanelWidth = MeasureMinPanelWidth();
            float panelWidth = Mathf.Min(
                shellRect.rect.width - PanelScreenMargin,
                Mathf.Max(minPanelWidth, shellRect.rect.width * 0.92f));

            GameObject panelGo = new("ExtendedSavePickerPanel", typeof(RectTransform));
            _panelRect = panelGo.GetComponent<RectTransform>();
            _panelRect.SetParent(shellRect, false);
            _panelRect.anchorMin = new Vector2(0.5f, 0f);
            _panelRect.anchorMax = new Vector2(0.5f, 1f);
            _panelRect.pivot = new Vector2(0.5f, 0.5f);
            _panelRect.sizeDelta = new Vector2(panelWidth, -PanelScreenMargin);
            _panelRect.anchoredPosition = Vector2.zero;

            BuildTitleBand(_panelRect);
            BuildScrollArea(_panelRect);
            BuildActionBar(_panelRect);
        }

        private void BuildTitleBand(RectTransform panel)
        {
            if (_styleDonor == null)
            {
                return;
            }

            GameObject titleBandGo = new("TitleBand", typeof(RectTransform));
            RectTransform titleBand = titleBandGo.GetComponent<RectTransform>();
            titleBand.SetParent(panel, false);
            SetBandAnchors(titleBand, 1f - TitleBandFraction, 1f);
            titleBand.offsetMin = new Vector2(HorizontalPadding, 0f);
            titleBand.offsetMax = new Vector2(-HorizontalPadding, -4f);

            Component? titleSource = SaveSlotGameAccess.GetLoadTramTextComponent(_styleDonor, "UE_title");
            if (titleSource == null)
            {
                return;
            }

            GameObject titleGo = UnityEngine.Object.Instantiate(titleSource.gameObject, titleBand);
            titleGo.name = "ExtendedSavePickerTitle";
            titleGo.SetActive(true);

            RectTransform titleRect = titleGo.GetComponent<RectTransform>();
            StretchRect(titleRect);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;

            Component? titleText = SaveSlotTextHelper.FindTextComponent(titleGo);
            SaveSlotTextHelper.SetText(titleText, SaveSlotGameAccess.GetL10NText("UI_PREFAB_MAIN_MENU_LOAD_TRAM"));
            SaveSlotDisplayFormatter.ApplyDefaultTextColor(titleText);
            SaveSlotTextHelper.SetAlignment(titleText, upperLeft: true);
        }

        private void BuildScrollArea(RectTransform panel)
        {
            if (_rowTemplate == null)
            {
                return;
            }

            GameObject listBandGo = new("ListBand", typeof(RectTransform));
            RectTransform listBand = listBandGo.GetComponent<RectTransform>();
            listBand.SetParent(panel, false);
            SetBandAnchors(listBand, ActionBandFraction, ActionBandFraction + ListBandFraction);
            listBand.offsetMin = new Vector2(HorizontalPadding, 6f);
            listBand.offsetMax = new Vector2(-HorizontalPadding, -6f);

            GameObject scrollRootGo = new("SaveListScroll", typeof(RectTransform));
            _scrollRoot = scrollRootGo.GetComponent<RectTransform>();
            _scrollRoot.SetParent(listBand, false);
            StretchRect(_scrollRoot);

            float scrollbarReserve = ScrollBarWidth + ScrollBarGap;

            GameObject viewportGo = new("Viewport", typeof(RectTransform), typeof(RectMask2D), typeof(Image));
            RectTransform viewportRect = viewportGo.GetComponent<RectTransform>();
            viewportRect.SetParent(_scrollRoot, false);
            StretchRect(viewportRect);
            viewportRect.offsetMax = new Vector2(-scrollbarReserve, 0f);
            viewportGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.18f);

            GameObject contentGo = new("Content", typeof(RectTransform));
            _scrollContent = contentGo.GetComponent<RectTransform>();
            _scrollContent.SetParent(viewportRect, false);
            _scrollContent.anchorMin = new Vector2(0f, 1f);
            _scrollContent.anchorMax = new Vector2(1f, 1f);
            _scrollContent.pivot = new Vector2(0.5f, 1f);
            _scrollContent.anchoredPosition = Vector2.zero;
            _scrollContent.sizeDelta = new Vector2(0f, 0f);

            VerticalLayoutGroup layout = contentGo.AddComponent<VerticalLayoutGroup>();
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.spacing = 4f;
            layout.padding = new RectOffset(0, 4, 0, 0);

            ContentSizeFitter fitter = contentGo.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            Scrollbar scrollbar = CreateVerticalScrollbar(_scrollRoot);

            _scrollRect = scrollRootGo.AddComponent<ScrollRect>();
            _scrollRect.viewport = viewportRect;
            _scrollRect.content = _scrollContent;
            _scrollRect.horizontal = false;
            _scrollRect.vertical = true;
            _scrollRect.movementType = ScrollRect.MovementType.Clamped;
            _scrollRect.scrollSensitivity = 24f;
            _scrollRect.verticalScrollbar = scrollbar;
            _scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
            _scrollRect.verticalScrollbarSpacing = ScrollBarGap;
        }

        private static Scrollbar CreateVerticalScrollbar(RectTransform scrollRoot)
        {
            GameObject scrollbarGo = new("Scrollbar Vertical", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
            RectTransform scrollbarRect = scrollbarGo.GetComponent<RectTransform>();
            scrollbarRect.SetParent(scrollRoot, false);
            scrollbarRect.anchorMin = new Vector2(1f, 0f);
            scrollbarRect.anchorMax = new Vector2(1f, 1f);
            scrollbarRect.pivot = new Vector2(1f, 0.5f);
            scrollbarRect.sizeDelta = new Vector2(ScrollBarWidth, 0f);
            scrollbarRect.anchoredPosition = new Vector2(0f, 0f);

            Image trackImage = scrollbarGo.GetComponent<Image>();
            trackImage.color = new Color(1f, 1f, 1f, 0.12f);

            Scrollbar scrollbar = scrollbarGo.GetComponent<Scrollbar>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;

            GameObject slidingGo = new("Sliding Area", typeof(RectTransform));
            RectTransform slidingRect = slidingGo.GetComponent<RectTransform>();
            slidingRect.SetParent(scrollbarRect, false);
            StretchRect(slidingRect);
            slidingRect.offsetMin = new Vector2(2f, 2f);
            slidingRect.offsetMax = new Vector2(-2f, -2f);

            GameObject handleGo = new("Handle", typeof(RectTransform), typeof(Image));
            RectTransform handleRect = handleGo.GetComponent<RectTransform>();
            handleRect.SetParent(slidingRect, false);
            handleRect.sizeDelta = new Vector2(ScrollBarWidth - 4f, 48f);

            Image handleImage = handleGo.GetComponent<Image>();
            handleImage.color = new Color32(255, 240, 194, 220);

            scrollbar.handleRect = handleRect;
            scrollbar.targetGraphic = handleImage;

            return scrollbar;
        }

        private void BuildActionBar(RectTransform panel)
        {
            if (_mainMenuUi == null)
            {
                return;
            }

            Button templateButton = _mainMenuUi.UE_HostButton;

            GameObject actionBandGo = new("ActionBand", typeof(RectTransform));
            RectTransform actionBand = actionBandGo.GetComponent<RectTransform>();
            actionBand.SetParent(panel, false);
            SetBandAnchors(actionBand, 0f, ActionBandFraction);
            actionBand.offsetMin = new Vector2(HorizontalPadding, 8f);
            actionBand.offsetMax = new Vector2(-HorizontalPadding, -8f);

            GameObject actionBarGo = new("ActionBar", typeof(RectTransform));
            RectTransform actionBarRect = actionBarGo.GetComponent<RectTransform>();
            actionBarRect.SetParent(actionBand, false);
            StretchRect(actionBarRect);

            HorizontalLayoutGroup layout = actionBarGo.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 10f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            layout.padding = new RectOffset(0, 0, 0, 0);

            _backButton = CreateMenuStyleButton(templateButton, actionBarRect, "Back", Close);
            _createButton = CreateMenuStyleButton(templateButton, actionBarRect, "Create", OnCreateClicked);
            _joinButton = CreateMenuStyleButton(templateButton, actionBarRect, "Join", OnJoinClicked);
            _deleteButton = CreateMenuStyleButton(templateButton, actionBarRect, "Delete", null);
            _deleteHoldHelper = HoldButtonHelper.Attach(_deleteButton, OnDeleteHoldComplete);
        }

        private Button CreateMenuStyleButton(Button templateButton, RectTransform parent, string label, Action? onClick)
        {
            GameObject cloneGo = UnityEngine.Object.Instantiate(templateButton.gameObject, parent);
            cloneGo.name = $"ExtendedSavePicker{label}Button";
            cloneGo.SetActive(true);

            Button button = cloneGo.GetComponent<Button>();
            button.onClick = new Button.ButtonClickedEvent();
            if (onClick != null)
            {
                button.onClick.AddListener(() => onClick());
            }

            ApplyButtonLabel(cloneGo, label);

            RectTransform rect = cloneGo.GetComponent<RectTransform>();
            rect.localScale = Vector3.one;

            float templateHeight = templateButton.GetComponent<RectTransform>().rect.height;
            float templateWidth = templateButton.GetComponent<RectTransform>().rect.width;
            if (templateHeight < 1f)
            {
                templateHeight = 44f;
            }

            if (templateWidth < 1f)
            {
                templateWidth = 120f;
            }

            LayoutElement layoutElement = cloneGo.GetComponent<LayoutElement>() ?? cloneGo.AddComponent<LayoutElement>();
            layoutElement.minHeight = templateHeight;
            layoutElement.preferredHeight = templateHeight;
            layoutElement.flexibleWidth = 1f;
            layoutElement.preferredWidth = templateWidth;

            Component? text = SaveSlotTextHelper.FindTextComponent(cloneGo);
            WireHoverEvents(cloneGo, text);
            return button;
        }

        private static void ApplyButtonLabel(GameObject buttonRoot, string label)
        {
            Component[] components = buttonRoot.GetComponentsInChildren<Component>(true);
            foreach (Component component in components)
            {
                if (component == null || component.GetType().Name is not ("TextMeshProUGUI" or "TMP_Text"))
                {
                    continue;
                }

                SaveSlotTextHelper.SetText(component, label);
                SaveSlotDisplayFormatter.ApplyDefaultTextColor(component);
            }
        }

        private void RefreshSaveList()
        {
            _saveCache.Clear();
            ClearRows(_autosaveRows);
            ClearRows(_manualRows);

            if (_scrollContent == null || _rowTemplate == null)
            {
                return;
            }

            SaveSlotEntry? autosave = SaveSlotDiscovery.TryLoadAutosave();
            if (autosave != null)
            {
                _saveCache[autosave.SlotId] = autosave;
                RowBinding autosaveRow = CreateRow(_scrollContent, autosave, _autosaveRowTemplate, includeTitle: true);
                _autosaveRows.Add(autosaveRow);
            }

            List<SaveSlotEntry> manualSaves = SaveSlotDiscovery.GetManualSaves();
            foreach (SaveSlotEntry entry in manualSaves)
            {
                _saveCache[entry.SlotId] = entry;
                RowBinding row = CreateRow(_scrollContent, entry, _rowTemplate, includeTitle: false);
                _manualRows.Add(row);
            }

            RebuildScrollContentLayout();
        }

        private void RebuildScrollContentLayout()
        {
            if (_scrollContent == null)
            {
                return;
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(_scrollContent);
            if (_scrollRect != null)
            {
                _scrollRect.verticalNormalizedPosition = 1f;
            }
        }

        private RowBinding CreateRow(Transform parent, SaveSlotEntry entry, GameObject? template, bool includeTitle)
        {
            if (template == null)
            {
                throw new InvalidOperationException("Save picker row template is missing.");
            }

            float rowHeight = includeTitle ? MeasureAutosaveRowHeight() : MeasureRowHeight();
            GameObject rowGo = UnityEngine.Object.Instantiate(template, parent);
            rowGo.name = includeTitle
                ? "ExtendedSaveAutosaveRow"
                : $"ExtendedSaveSlot{entry.SlotId:D3}";
            rowGo.SetActive(true);

            NormalizeListRowRect(rowGo, rowHeight);

            HideChildNamed(rowGo.transform, "Title1");
            HideChildNamed(rowGo.transform, "Title2");
            HideChildNamed(rowGo.transform, "Title3");

            if (includeTitle)
            {
                Component? title = FindChildText(rowGo.transform, "Title0");
                SaveSlotTextHelper.SetText(title, SaveSlotDisplayFormatter.FormatAutosaveTitle(entry.Data));
                SaveSlotDisplayFormatter.ApplyDefaultTextColor(title);
            }
            else
            {
                HideChildNamed(rowGo.transform, "Title0");
            }

            Button button = rowGo.GetComponentInChildren<Button>(true);
            Component? text = FindRowBodyText(rowGo.transform);
            SaveSlotTextHelper.SetText(text, entry.Display.FullText);
            SaveSlotDisplayFormatter.ApplyDefaultTextColor(text);

            AddLayoutElement(rowGo, rowHeight);
            WireRowSelection(button, entry.SlotId);
            WireHoverEvents(button.gameObject, text);

            return new RowBinding
            {
                SlotId = entry.SlotId,
                RowRoot = rowGo,
                Button = button,
                TextComponent = text,
                Display = entry.Display,
            };
        }

        private static Component? FindRowBodyText(Transform rowRoot)
        {
            Component? text = FindChildText(rowRoot, "Text0")
                ?? FindChildText(rowRoot, "Text1")
                ?? FindChildText(rowRoot, "Text2")
                ?? FindChildText(rowRoot, "Text3");
            return text ?? SaveSlotTextHelper.FindTextComponent(rowRoot.gameObject);
        }

        private static Component? FindChildText(Transform root, string name)
        {
            Transform? child = root.Find(name);
            return child != null ? SaveSlotTextHelper.FindTextComponent(child.gameObject) : null;
        }

        private static void HideChildNamed(Transform root, string name)
        {
            Transform? child = root.Find(name);
            if (child != null)
            {
                child.gameObject.SetActive(false);
            }
        }

        private float MeasureRowHeight()
        {
            return _cachedRowHeight > 1f ? _cachedRowHeight : 56f;
        }

        private float MeasureAutosaveRowHeight()
        {
            return _cachedAutosaveRowHeight > 1f ? _cachedAutosaveRowHeight : MeasureRowHeight();
        }

        private static void SetBandAnchors(RectTransform rect, float minY, float maxY)
        {
            rect.anchorMin = new Vector2(0f, minY);
            rect.anchorMax = new Vector2(1f, maxY);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
        }

        private static void NormalizeListRowRect(GameObject rowGo, float rowHeight)
        {
            RectTransform rect = rowGo.GetComponent<RectTransform>();
            rect.localScale = Vector3.one;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(0f, rowHeight);

            foreach (RectTransform child in rowGo.GetComponentsInChildren<RectTransform>(true))
            {
                if (child == rect)
                {
                    continue;
                }

                child.localScale = Vector3.one;
            }
        }

        private static void StretchRect(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
        }

        private static void AddLayoutElement(GameObject go, float height)
        {
            LayoutElement layoutElement = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
            layoutElement.minHeight = height;
            layoutElement.preferredHeight = height;
            layoutElement.flexibleWidth = 1f;
        }

        private void ClearRows(List<RowBinding> rows)
        {
            foreach (RowBinding row in rows)
            {
                if (row.RowRoot != null)
                {
                    UnityEngine.Object.Destroy(row.RowRoot);
                }
            }

            rows.Clear();
        }

        private void WireRowSelection(Button button, int slotId)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => OnSlotClicked(slotId));
        }

        private void OnSlotClicked(int slotId)
        {
            if (!_saveCache.ContainsKey(slotId))
            {
                return;
            }

            float now = Time.unscaledTime;
            if (_lastClickSlotId == slotId && now - _lastClickTime <= DoubleClickWindowSeconds)
            {
                _selectedSlotId = slotId;
                UpdateSelectionVisuals();
                OnJoinClicked();
                _lastClickSlotId = -1;
                return;
            }

            _lastClickSlotId = slotId;
            _lastClickTime = now;
            _selectedSlotId = slotId;
            UpdateSelectionVisuals();
            UpdateActionButtons();
        }

        private void UpdateSelectionVisuals()
        {
            Color selectedColor = SaveSlotGameAccess.GetMouseOverTextColor();
            ApplySelectionToRows(_autosaveRows, selectedColor);
            ApplySelectionToRows(_manualRows, selectedColor);
        }

        private void ApplySelectionToRows(IEnumerable<RowBinding> rows, Color selectedColor)
        {
            foreach (RowBinding row in rows)
            {
                SaveSlotDisplayFormatter.ApplyDefaultTextColor(row.TextComponent);
                if (row.SlotId == _selectedSlotId)
                {
                    SaveSlotTextHelper.SetColor(row.TextComponent, selectedColor);
                }
            }
        }

        private void UpdateActionButtons()
        {
            bool hasSelection = _selectedSlotId >= 0 && _saveCache.ContainsKey(_selectedSlotId);
            bool isManualOccupied = hasSelection
                && _selectedSlotId >= SaveSlotLimits.MinManualSlotId
                && SaveSlotDiscovery.IsManualSlotOccupied(_selectedSlotId);

            if (_joinButton != null)
            {
                _joinButton.interactable = hasSelection;
            }

            if (_createButton != null)
            {
                _createButton.interactable = SaveSlotDiscovery.FindFirstFreeManualSlot() >= 0;
            }

            if (_deleteButton != null)
            {
                _deleteButton.interactable = isManualOccupied;
            }

            _deleteHoldHelper?.SetInteractable(isManualOccupied);
        }

        private void OnCreateClicked()
        {
            if (_mainMenu == null || _shell == null)
            {
                return;
            }

            int slotId = SaveSlotDiscovery.FindFirstFreeManualSlot();
            if (slotId < 0)
            {
                ModLog.Warn(Feature, "All manual save slots are full.");
                UpdateActionButtons();
                return;
            }

            MainMenuSessionBridge.CreateNewGameInSlot(_mainMenu, _shell, slotId);
        }

        private void OnJoinClicked()
        {
            if (_mainMenu == null || _shell == null || _selectedSlotId < 0)
            {
                return;
            }

            if (!_saveCache.TryGetValue(_selectedSlotId, out SaveSlotEntry? entry))
            {
                return;
            }

            if (!entry.Display.IsVersionCompatible)
            {
                StartVersionBlink(entry);
                return;
            }

            MainMenuSessionBridge.LoadSaveAndCreateRoom(_mainMenu, _shell, _selectedSlotId, entry.Data);
        }

        private void OnDeleteHoldComplete()
        {
            if (_mainMenu == null || _selectedSlotId < SaveSlotLimits.MinManualSlotId)
            {
                return;
            }

            if (!SaveSlotDiscovery.IsManualSlotOccupied(_selectedSlotId))
            {
                UpdateActionButtons();
                return;
            }

            if (MainMenuSessionBridge.TryDeleteSaveGameData(_mainMenu, _selectedSlotId))
            {
                ModLog.Debug(Feature, $"Deleted save slot {_selectedSlotId}.");
            }

            _selectedSlotId = -1;
            RefreshSaveList();
            UpdateSelectionVisuals();
            UpdateActionButtons();
        }

        private void StartVersionBlink(SaveSlotEntry entry)
        {
            Component? text = GetTextForSlot(entry.SlotId);
            if (text == null || _shell == null)
            {
                return;
            }

            if (_blinkCoroutine != null)
            {
                _shell.StopCoroutine(_blinkCoroutine);
            }

            _blinkCoroutine = _shell.StartCoroutine(BlinkVersionWarning(entry, text));
        }

        private Component? GetTextForSlot(int slotId)
        {
            RowBinding? row = _autosaveRows.Find(binding => binding.SlotId == slotId)
                ?? _manualRows.Find(binding => binding.SlotId == slotId);
            return row?.TextComponent;
        }

        private IEnumerator BlinkVersionWarning(SaveSlotEntry entry, Component textElement)
        {
            const float halfPeriod = 0.125f;
            for (int blink = 0; blink < 4; blink++)
            {
                SaveSlotTextHelper.SetText(textElement, SaveSlotDisplayFormatter.BuildTransparentBlinkText(entry.Display));
                yield return new WaitForSeconds(halfPeriod);
                SaveSlotTextHelper.SetText(textElement, SaveSlotDisplayFormatter.BuildBlinkText(entry.Display, showVersionWarning: true));
                yield return new WaitForSeconds(halfPeriod);
            }

            SaveSlotTextHelper.SetText(textElement, SaveSlotDisplayFormatter.BuildBlinkText(entry.Display, showVersionWarning: true));
            _blinkCoroutine = null;
        }

        private void WireHoverEvents(GameObject go, Component? textTarget)
        {
            if (textTarget == null)
            {
                return;
            }

            EventTrigger trigger = go.GetComponent<EventTrigger>() ?? go.AddComponent<EventTrigger>();

            AddHoverTrigger(trigger, EventTriggerType.PointerEnter, () =>
            {
                Button? button = go.GetComponent<Button>();
                if (button != null && !button.interactable)
                {
                    return;
                }

                SaveSlotTextHelper.SetColor(textTarget, SaveSlotGameAccess.GetMouseOverTextColor());
            });

            AddHoverTrigger(trigger, EventTriggerType.PointerExit, () =>
            {
                Button? button = go.GetComponent<Button>();
                if (button != null && !button.interactable)
                {
                    return;
                }

                int slotId = FindSlotIdForText(textTarget);
                if (slotId >= 0 && slotId == _selectedSlotId)
                {
                    SaveSlotTextHelper.SetColor(textTarget, SaveSlotGameAccess.GetMouseOverTextColor());
                    return;
                }

                SaveSlotDisplayFormatter.ApplyDefaultTextColor(textTarget);
            });
        }

        private int FindSlotIdForText(Component textTarget)
        {
            RowBinding? match = _autosaveRows.Find(row => row.TextComponent == textTarget)
                ?? _manualRows.Find(row => row.TextComponent == textTarget);
            return match?.SlotId ?? -1;
        }

        private static void AddHoverTrigger(EventTrigger trigger, EventTriggerType type, Action callback)
        {
            EventTrigger.Entry entry = new()
            {
                eventID = type,
            };
            entry.callback.AddListener(_ => callback());
            trigger.triggers.Add(entry);
        }
    }
}

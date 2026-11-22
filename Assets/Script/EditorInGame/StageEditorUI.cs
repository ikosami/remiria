using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Pulling
{
    public class StageEditorUI : MonoBehaviour
    {
        public GameObject editorRoot;
        public GameObject settingsPanel;
        public GameObject hintRoot;
        public Transform paletteContent;
        public Text statusLabel;
        public Button settingsButton;
        public Button finishButton;
        public Button saveAsNewButton;
        public Button overwriteButton;
        public Button openFolderButton;
        public Button closeButton;
        public Button changeStageButton;
        public GameObject stageListPanel;
        public Transform stageListContent;
        public InputField newStageInputField;
        public Button createStageButton;
        public Button closeStageListButton;
        public Text newStageWarningLabel;
        public Text stageNameLabel;
        public Text widthCurrentLabel;
        public InputField widthInputField;
        public Text widthWarningLabel;
        public Button widthApplyButton;
        public GameObject paletteRoot;
        public GameObject topBarRoot;

        const float PaletteEdge = 16f;
        const float PaletteTopInset = 80f;
        const float PaletteBottomInset = 16f;
        const float PaletteScrollInset = 8f;
        const float PaletteHeaderHeight = 44f;
        const float PaletteCellWidth = 202f;
        const float PaletteCellHeight = 72f;
        const float PaletteCellSpacing = 8f;
        const int PaletteContentPad = 8;

        public bool IsOverlayOpen =>
            (settingsPanel != null && settingsPanel.activeSelf)
            || (stageListPanel != null && stageListPanel.activeSelf);

        StageEditorController _editor;
        int _currentWidth;
        bool _chromeVisible = true;
        bool _commitOrderQueued;
        bool _settingsPrepared;
        bool _paletteOnRight;
        bool _paletteThreeColumns;
        bool _paletteChromeReady;
        RectTransform _dockTriangle;
        RectTransform _columnTriangleLeft;
        RectTransform _columnTriangleRight;
        Button _paletteDockButton;
        Button _paletteColumnsButton;
        RectTransform _settingsRect;
        StageWidthPreview _widthPreview;
        readonly List<StageListItem> _stageRows = new List<StageListItem>();

        public void Bind(GameObject editor, GameObject settings, GameObject hint, Transform content, Text status, StageEditorController controller)
        {
            editorRoot = editor;
            settingsPanel = settings;
            hintRoot = hint;
            paletteContent = content;
            statusLabel = status;
            _editor = controller;
            PrepareSettings();
            EnsurePaletteChrome();
            WireButtons();
            SetEditorVisible(false);
            SetSettingsVisible(false);
            SetStageListVisible(false);
        }

        void OnEnable()
        {
            WireButtons();
        }

        void OnDisable()
        {
            UnwireAll();
        }

        void WireButtons()
        {
            UnwireAll();
            Wire(settingsButton, OnSettingsClicked);
            Wire(finishButton, OnFinishClicked);
            Wire(saveAsNewButton, OnSaveAsNewClicked);
            Wire(overwriteButton, OnOverwriteClicked);
            Wire(openFolderButton, OnOpenFolderClicked);
            Wire(closeButton, OnCloseClicked);
            Wire(changeStageButton, OnChangeStageClicked);
            Wire(createStageButton, OnCreateStageClicked);
            Wire(closeStageListButton, OnCloseStageListClicked);
            Wire(widthApplyButton, OnWidthApplyClicked);
            Wire(_paletteDockButton, OnPaletteDockClicked);
            Wire(_paletteColumnsButton, OnPaletteColumnsClicked);
            if (widthInputField != null)
            {
                widthInputField.onEndEdit.AddListener(OnWidthEndEdit);
                widthInputField.onValueChanged.AddListener(OnWidthValueChanged);
            }
        }

        void UnwireAll()
        {
            Unwire(settingsButton, OnSettingsClicked);
            Unwire(finishButton, OnFinishClicked);
            Unwire(saveAsNewButton, OnSaveAsNewClicked);
            Unwire(overwriteButton, OnOverwriteClicked);
            Unwire(openFolderButton, OnOpenFolderClicked);
            Unwire(closeButton, OnCloseClicked);
            Unwire(changeStageButton, OnChangeStageClicked);
            Unwire(createStageButton, OnCreateStageClicked);
            Unwire(closeStageListButton, OnCloseStageListClicked);
            Unwire(widthApplyButton, OnWidthApplyClicked);
            Unwire(_paletteDockButton, OnPaletteDockClicked);
            Unwire(_paletteColumnsButton, OnPaletteColumnsClicked);
            if (widthInputField != null)
            {
                widthInputField.onEndEdit.RemoveListener(OnWidthEndEdit);
                widthInputField.onValueChanged.RemoveListener(OnWidthValueChanged);
            }
        }

        static void Wire(Button button, UnityAction action)
        {
            if (button != null)
            {
                button.onClick.AddListener(action);
            }
        }

        static void Unwire(Button button, UnityAction action)
        {
            if (button != null)
            {
                button.onClick.RemoveListener(action);
            }
        }

        void OnSettingsClicked() => _editor?.OpenSettings();
        void OnFinishClicked() => _editor?.Exit();
        void OnSaveAsNewClicked() => _editor?.SaveAsNew();
        void OnOverwriteClicked() => _editor?.Overwrite();
        void OnOpenFolderClicked() => _editor?.OpenEditStageFolder();
        void OnCloseClicked() => _editor?.CloseSettings();
        void OnChangeStageClicked() => _editor?.OpenStageList();
        void OnCreateStageClicked() => _editor?.CreateStage(newStageInputField != null ? newStageInputField.text : string.Empty);
        void OnCloseStageListClicked() => _editor?.CloseStageList();
        void OnWidthApplyClicked() => TryApplyWidth();
        void OnPaletteDockClicked()
        {
            _paletteOnRight = !_paletteOnRight;
            ApplyPaletteFrame();
        }

        void OnPaletteColumnsClicked()
        {
            _paletteThreeColumns = !_paletteThreeColumns;
            ApplyPaletteFrame();
            ScrollPaletteToTop();
        }

        public void RefreshSettings(string stageId, int currentWidth)
        {
            if (stageNameLabel != null)
            {
                stageNameLabel.text = string.IsNullOrEmpty(stageId) ? "ステージ" : StagePersistence.DisplayName(stageId);
            }

            RefreshWidthSettings(currentWidth);
        }

        public void RefreshWidthSettings(int currentWidth)
        {
            _currentWidth = currentWidth;
            if (widthInputField != null)
            {
                widthInputField.SetTextWithoutNotify(currentWidth.ToString());
            }

            SyncWidthPreview();
        }

        void OnWidthValueChanged(string _) => SyncWidthPreview();

        void OnWidthEndEdit(string _) => TryApplyWidth();

        void TryApplyWidth()
        {
            if (_editor == null || widthInputField == null)
            {
                return;
            }

            if (!TryParseWidth(widthInputField.text, out int newWidth))
            {
                widthInputField.SetTextWithoutNotify(_currentWidth.ToString());
                return;
            }

            _editor.ApplyGridWidth(newWidth);
        }

        static bool TryParseWidth(string text, out int width)
        {
            width = 0;
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            if (!int.TryParse(text.Trim(), out width))
            {
                return false;
            }

            return width >= 1 && width <= 256;
        }

        void CompactWidthInput()
        {
            if (widthInputField == null)
            {
                return;
            }

            var fieldLayout = widthInputField.GetComponent<LayoutElement>();
            if (fieldLayout != null)
            {
                fieldLayout.minHeight = 34f;
                fieldLayout.preferredHeight = 34f;
                fieldLayout.flexibleHeight = 0f;
            }

            var row = widthInputField.transform.parent;
            if (row == null)
            {
                return;
            }

            var rowLayout = row.GetComponent<LayoutElement>();
            if (rowLayout != null)
            {
                rowLayout.minHeight = 34f;
                rowLayout.preferredHeight = 34f;
                rowLayout.flexibleHeight = 0f;
            }

            var horizontal = row.GetComponent<HorizontalLayoutGroup>();
            if (horizontal != null)
            {
                horizontal.childForceExpandHeight = false;
            }

            EnsureWidthStepButtons(row);
        }

        void EnsureWidthStepButtons(Transform row)
        {
            if (row.Find("WidthMinus") != null)
            {
                return;
            }

            var minus = CreateButton(row, "WidthMinus", "−1", new Vector2(44f, 34f), () => StepWidth(-1), new Color(0.28f, 0.32f, 0.4f));
            FitWidthStep(minus);
            var plus = CreateButton(row, "WidthPlus", "+1", new Vector2(44f, 34f), () => StepWidth(1), new Color(0.28f, 0.32f, 0.4f));
            FitWidthStep(plus);
        }

        static void FitWidthStep(UiButton button)
        {
            button.layout.minWidth = 44f;
            button.layout.preferredWidth = 44f;
            button.layout.flexibleWidth = 0f;
            button.layout.minHeight = 34f;
            button.layout.preferredHeight = 34f;
            button.layout.flexibleHeight = 0f;
            button.label.fontSize = 18;
        }

        void StepWidth(int delta)
        {
            int basis = _currentWidth;
            if (widthInputField != null && TryParseWidth(widthInputField.text, out int typed))
            {
                basis = typed;
            }

            int next = Mathf.Clamp(basis + delta, 1, 256);
            if (widthInputField != null)
            {
                widthInputField.SetTextWithoutNotify(next.ToString());
            }

            _editor?.ApplyGridWidth(next);
        }

        void EnsureWidthPreview()
        {
            if (_widthPreview != null || widthInputField == null)
            {
                return;
            }

            var row = widthInputField.transform.parent;
            if (row == null || row.parent == null)
            {
                return;
            }

            _widthPreview = StageWidthPreview.Create(row.parent, row.GetSiblingIndex() + 1);
            _widthPreview.gameObject.SetActive(false);
        }

        void SyncWidthPreview()
        {
            if (_widthPreview == null || _editor == null || settingsPanel == null || !settingsPanel.activeSelf)
            {
                return;
            }

            if (!_editor.TryGetWidthPreviewSource(out Camera gameplay, out float cellSize))
            {
                return;
            }

            int cells = _currentWidth;
            if (widthInputField != null && TryParseWidth(widthInputField.text, out int typed))
            {
                cells = typed;
            }

            _widthPreview.SetSource(gameplay, cellSize, cells);
        }

        public void SetEditorVisible(bool visible)
        {
            if (editorRoot != null)
            {
                editorRoot.SetActive(visible);
            }

            if (hintRoot != null)
            {
                hintRoot.SetActive(!visible);
            }

            if (!visible)
            {
                SetSettingsVisible(false);
                SetStageListVisible(false);
            }
            else
            {
                SetChromeVisible(true);
                EnsurePaletteChrome();
                ApplyPaletteFrame();
            }
        }

        public void ToggleChrome()
        {
            SetChromeVisible(!_chromeVisible);
        }

        public void SetChromeVisible(bool visible)
        {
            _chromeVisible = visible;
            if (paletteRoot != null)
            {
                paletteRoot.SetActive(visible);
            }

            if (topBarRoot != null)
            {
                topBarRoot.SetActive(visible);
            }

            if (statusLabel != null)
            {
                statusLabel.gameObject.SetActive(visible);
            }

            if (!visible)
            {
                SetSettingsVisible(false);
                SetStageListVisible(false);
            }
        }

        public void SetStageListVisible(bool visible)
        {
            if (stageListPanel != null)
            {
                stageListPanel.SetActive(visible);
            }
        }

        public void ClearNewStageInput()
        {
            if (newStageInputField != null)
            {
                newStageInputField.SetTextWithoutNotify(string.Empty);
            }
        }

        public void SetNewStageWarning(string text)
        {
            if (newStageWarningLabel == null)
            {
                return;
            }

            bool show = !string.IsNullOrEmpty(text);
            newStageWarningLabel.gameObject.SetActive(show);
            newStageWarningLabel.text = text ?? string.Empty;
        }

        public void RebuildStageList(StageCatalog catalog, int currentIndex)
        {
            if (stageListContent == null)
            {
                return;
            }

            _stageRows.Clear();
            for (int i = stageListContent.childCount - 1; i >= 0; i--)
            {
                Destroy(stageListContent.GetChild(i).gameObject);
            }

            if (catalog == null || catalog.stages == null)
            {
                return;
            }

            for (int i = 0; i < catalog.stages.Count; i++)
            {
                var stage = catalog.stages[i];
                if (stage == null)
                {
                    continue;
                }

                bool current = i == currentIndex;
                string name = StagePersistence.DisplayName(stage.id);
                string label = current ? $"{name}（編集中）" : name;
                var color = current ? new Color(0.28f, 0.48f, 0.38f) : new Color(0.18f, 0.18f, 0.22f);
                int index = i;

                var row = new GameObject($"StageItem_{i}", typeof(RectTransform));
                row.transform.SetParent(stageListContent, false);
                var item = row.AddComponent<StageListItem>();
                item.stageId = stage.id;
                var rowElement = row.AddComponent<LayoutElement>();
                rowElement.minHeight = 52f;
                rowElement.preferredHeight = 52f;
                var rowLayout = row.AddComponent<HorizontalLayoutGroup>();
                rowLayout.spacing = 8f;
                rowLayout.childForceExpandWidth = false;
                rowLayout.childForceExpandHeight = true;
                rowLayout.childControlWidth = true;
                rowLayout.childControlHeight = true;

                CreateDragHandle(row.transform, rowElement);

                var button = CreateButton(row.transform, "Open", label, new Vector2(320f, 52f), () => _editor?.SwitchToStage(index), color);
                button.layout.flexibleWidth = 1f;
                button.layout.minWidth = 120f;
                button.label.fontSize = 20;

                var nameInput = CreateInputField(row.transform, "RenameInput", "ステージ名", new Vector2(180f, 52f), InputField.ContentType.Standard);
                nameInput.field.text = name;
                nameInput.field.gameObject.SetActive(false);
                nameInput.layout.flexibleWidth = 1f;
                nameInput.layout.minWidth = 120f;

                var rename = CreateButton(row.transform, "Rename", "名前変更", new Vector2(112f, 52f), null, new Color(0.32f, 0.38f, 0.58f));
                rename.layout.minWidth = 112f;
                rename.layout.preferredWidth = 112f;
                rename.layout.flexibleWidth = 0f;
                rename.label.fontSize = 18;

                var renameControl = rename.button.gameObject.AddComponent<StageListRenameControl>();
                renameControl.Init(this, stage.id, button.button, nameInput.field, rename.label, rename.button);

                var delete = CreateButton(row.transform, "Delete", "削除", new Vector2(76f, 52f), null, new Color(0.55f, 0.28f, 0.28f));
                delete.layout.minWidth = 76f;
                delete.layout.preferredWidth = 76f;
                delete.layout.flexibleWidth = 0f;
                delete.label.fontSize = 20;

                var deleteHold = delete.button.gameObject.AddComponent<StageListDeleteHold>();
                deleteHold.Init(this, stage.id, delete.label);
                _stageRows.Add(item);
            }
        }

        public void CommitStageRename(string stageId, string requestedName)
        {
            _editor?.RenameStage(stageId, requestedName);
        }

        public void RequestDeleteStage(string stageId)
        {
            _editor?.DeleteStageById(stageId);
        }

        public void CommitStageOrderFromList()
        {
            _commitOrderQueued = true;
        }

        void LateUpdate()
        {
            SyncWidthPreview();
            if (!_commitOrderQueued)
            {
                return;
            }

            _commitOrderQueued = false;
            ApplyStageOrderFromList();
        }

        void ApplyStageOrderFromList()
        {
            if (stageListContent == null || _editor == null)
            {
                return;
            }

            _stageRows.Sort((a, b) => a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex()));
            var ids = new List<string>(_stageRows.Count);
            for (int i = 0; i < _stageRows.Count; i++)
            {
                if (!string.IsNullOrEmpty(_stageRows[i].stageId))
                {
                    ids.Add(_stageRows[i].stageId);
                }
            }

            _editor.ReorderStages(ids);
        }

        void CreateDragHandle(Transform parent, LayoutElement rowLayout)
        {
            var go = new GameObject("DragHandle", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            image.color = new Color(0.28f, 0.28f, 0.34f, 1f);
            var element = go.AddComponent<LayoutElement>();
            element.minWidth = 44f;
            element.preferredWidth = 44f;
            element.flexibleWidth = 0f;
            element.minHeight = 52f;
            element.preferredHeight = 52f;
            var handle = go.AddComponent<StageListDragHandle>();
            handle.Init(this, rowLayout);

            var textGo = new GameObject("Text", typeof(RectTransform));
            textGo.transform.SetParent(go.transform, false);
            var textRt = (RectTransform)textGo.transform;
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;
            var text = textGo.AddComponent<Text>();
            text.font = UiFonts.Default;
            text.fontSize = 22;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.text = "≡";
            text.raycastTarget = false;
        }

        public void SetSettingsVisible(bool visible)
        {
            if (settingsPanel == null)
            {
                return;
            }

            if (visible)
            {
                PrepareSettings();
            }

            settingsPanel.SetActive(visible);
            if (stageListPanel != null)
            {
                stageListPanel.SetActive(visible);
            }

            if (visible && _settingsRect != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(_settingsRect);
            }

            if (_widthPreview != null)
            {
                _widthPreview.gameObject.SetActive(visible);
            }

            if (visible)
            {
                SyncWidthPreview();
            }
        }

        void PrepareSettings()
        {
            if (_settingsPrepared || settingsPanel == null)
            {
                return;
            }

            _settingsPrepared = true;
            _settingsRect = (RectTransform)settingsPanel.transform;
            if (!TryUseSettingsPrefab())
            {
                Debug.LogError("SettingsPanel は Prefab の SettingsPanelView を使ってください。");
            }

            CompactWidthInput();
            EnsureWidthPreview();

            SetInactive(changeStageButton);
            SetInactive(saveAsNewButton);
            SetInactive(overwriteButton);
        }

        bool TryUseSettingsPrefab()
        {
            var view = settingsPanel.GetComponent<SettingsPanelView>();
            if (view == null || !view.IsReady)
            {
                return false;
            }

            stageNameLabel = view.stageNameLabel;
            widthInputField = view.widthInputField;
            closeButton = view.closeButton;
            openFolderButton = view.openFolderButton;
            stageListPanel = view.stageListPanel;
            stageListContent = view.stageListContent;
            newStageInputField = view.newStageInputField;
            createStageButton = view.createStageButton;
            newStageWarningLabel = view.newStageWarningLabel;
            return true;
        }


        static void SetInactive(Component component)
        {
            if (component != null)
            {
                component.gameObject.SetActive(false);
            }
        }
        public void SetStatus(string text)
        {
            if (statusLabel != null)
            {
                statusLabel.text = text ?? string.Empty;
            }
        }

        public void RebuildPalette(TileDatabase database)
        {
            if (paletteContent == null || _editor == null)
            {
                return;
            }

            for (int i = paletteContent.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(paletteContent.GetChild(i).gameObject);
            }

            CreatePaletteButton(TileIds.PlayerSpawn, "初期位置", new Color(0.3f, 0.75f, 1f), SpriteFactory.CreateCircle(new Color(0.3f, 0.75f, 1f)));
            foreach (var tile in database.EnumeratePalette())
            {
                if (tile != null)
                {
                    CreatePaletteButton(tile.id, tile.displayName, tile.color, tile.sprite);
                }
            }

            ApplyPaletteFrame();
            ScrollPaletteToTop();
        }

        void EnsurePaletteChrome()
        {
            if (_paletteChromeReady || paletteRoot == null)
            {
                return;
            }

            var palette = (RectTransform)paletteRoot.transform;
            var scroll = palette.Find("Scroll") as RectTransform;
            if (scroll == null || paletteContent == null)
            {
                return;
            }

            var header = palette.Find("PaletteHeader") as RectTransform;
            if (header == null)
            {
                var headerGo = new GameObject("PaletteHeader", typeof(RectTransform));
                headerGo.transform.SetParent(palette, false);
                header = (RectTransform)headerGo.transform;
                var headerImage = headerGo.AddComponent<Image>();
                headerImage.color = new Color(0.14f, 0.14f, 0.18f, 1f);
                var headerLayout = headerGo.AddComponent<HorizontalLayoutGroup>();
                headerLayout.padding = new RectOffset(4, 4, 4, 4);
                headerLayout.spacing = 8f;
                headerLayout.childAlignment = TextAnchor.MiddleLeft;
                headerLayout.childForceExpandWidth = false;
                headerLayout.childForceExpandHeight = true;
                headerLayout.childControlWidth = true;
                headerLayout.childControlHeight = true;

                var dock = CreateButton(header, "DockButton", string.Empty, new Vector2(40f, 36f), null, new Color(0.28f, 0.32f, 0.4f));
                dock.layout.minWidth = 40f;
                dock.layout.preferredWidth = 40f;
                dock.layout.flexibleWidth = 0f;
                dock.layout.minHeight = 36f;
                dock.layout.preferredHeight = 36f;
                dock.label.gameObject.SetActive(false);
                _paletteDockButton = dock.button;
                _dockTriangle = CreateTriangle(dock.rect, "Triangle", Vector2.zero);

                var columns = CreateButton(header, "ColumnsButton", string.Empty, new Vector2(72f, 36f), null, new Color(0.28f, 0.32f, 0.4f));
                columns.layout.minWidth = 72f;
                columns.layout.preferredWidth = 72f;
                columns.layout.flexibleWidth = 0f;
                columns.layout.minHeight = 36f;
                columns.layout.preferredHeight = 36f;
                columns.label.gameObject.SetActive(false);
                _paletteColumnsButton = columns.button;
                _columnTriangleLeft = CreateTriangle(columns.rect, "Left", new Vector2(-14f, 0f));
                _columnTriangleRight = CreateTriangle(columns.rect, "Right", new Vector2(14f, 0f));
            }

            header.anchorMin = new Vector2(0f, 1f);
            header.anchorMax = new Vector2(1f, 1f);
            header.pivot = new Vector2(0.5f, 1f);
            header.anchoredPosition = new Vector2(0f, -PaletteScrollInset);
            header.sizeDelta = new Vector2(-PaletteScrollInset * 2f, PaletteHeaderHeight);
            header.SetAsLastSibling();

            scroll.anchorMin = Vector2.zero;
            scroll.anchorMax = Vector2.one;
            scroll.pivot = new Vector2(0.5f, 0.5f);
            scroll.offsetMin = new Vector2(PaletteScrollInset, PaletteScrollInset);
            scroll.offsetMax = new Vector2(-PaletteScrollInset, -(PaletteScrollInset + PaletteHeaderHeight + PaletteScrollInset));

            EnsureVerticalList();
            _paletteChromeReady = _paletteDockButton != null && _paletteColumnsButton != null;
            ApplyPaletteFrame();
        }

        void EnsureVerticalList()
        {
            if (paletteContent == null)
            {
                return;
            }

            var content = (RectTransform)paletteContent;
            var grid = content.GetComponent<GridLayoutGroup>();
            if (grid != null)
            {
                DestroyImmediate(grid);
            }

            var vertical = content.GetComponent<VerticalLayoutGroup>();
            if (vertical == null)
            {
                vertical = content.gameObject.AddComponent<VerticalLayoutGroup>();
            }

            vertical.enabled = true;
            vertical.spacing = PaletteCellSpacing;
            vertical.padding = new RectOffset(PaletteContentPad, PaletteContentPad, PaletteContentPad, PaletteContentPad);
            vertical.childAlignment = TextAnchor.UpperLeft;
            vertical.childForceExpandWidth = true;
            vertical.childForceExpandHeight = false;
            vertical.childControlWidth = true;
            vertical.childControlHeight = true;

            var fitter = content.GetComponent<ContentSizeFitter>();
            if (fitter == null)
            {
                fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            }

            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
        }

        void ApplyPaletteFrame()
        {
            if (paletteRoot == null)
            {
                return;
            }

            int columns = _paletteThreeColumns ? 3 : 1;
            float width = PaletteWidth(columns);
            var palette = (RectTransform)paletteRoot.transform;
            float side = PaletteEdge + width * 0.5f;
            float y = (PaletteBottomInset - PaletteTopInset) * 0.5f;
            if (_paletteOnRight)
            {
                palette.anchorMax = new Vector2(1f, 1f);
                palette.anchorMin = new Vector2(1f, 0f);
            }
            else
            {
                palette.anchorMin = new Vector2(0f, 0f);
                palette.anchorMax = new Vector2(0f, 1f);
            }

            palette.pivot = new Vector2(0.5f, 0.5f);
            palette.sizeDelta = new Vector2(width, -(PaletteBottomInset + PaletteTopInset));
            palette.anchoredPosition = new Vector2(_paletteOnRight ? -side : side, y);

            if (_dockTriangle != null)
            {
                _dockTriangle.localScale = new Vector3(_paletteOnRight ? -1f : 1f, 1f, 1f);
            }

            if (_columnTriangleLeft != null)
            {
                _columnTriangleLeft.localScale = new Vector3(_paletteThreeColumns ? 1f : -1f, 1f, 1f);
            }

            if (_columnTriangleRight != null)
            {
                _columnTriangleRight.localScale = new Vector3(_paletteThreeColumns ? -1f : 1f, 1f, 1f);
            }

            EnsureVerticalList();
            PlacePaletteItems();
            if (paletteContent is RectTransform content)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(content);
            }
        }

        static float PaletteWidth(int columns)
        {
            float gaps = PaletteCellSpacing * Mathf.Max(0, columns - 1);
            return PaletteScrollInset * 2f + PaletteContentPad * 2f + PaletteCellWidth * columns + gaps;
        }

        void PlacePaletteItems()
        {
            if (paletteContent == null)
            {
                return;
            }

            var items = new List<Transform>();
            var rows = new List<GameObject>();
            CollectPaletteItems(paletteContent, items, rows);
            for (int i = 0; i < items.Count; i++)
            {
                items[i].SetParent(paletteContent, false);
            }

            for (int i = 0; i < rows.Count; i++)
            {
                if (rows[i] != null)
                {
                    DestroyImmediate(rows[i]);
                }
            }

            if (!_paletteThreeColumns)
            {
                return;
            }

            int rowIndex = 0;
            for (int i = 0; i < items.Count; i += 3)
            {
                var row = CreatePaletteRow();
                row.SetSiblingIndex(rowIndex);
                rowIndex++;
                int count = Mathf.Min(3, items.Count - i);
                for (int c = 0; c < count; c++)
                {
                    items[i + c].SetParent(row, false);
                }
            }
        }

        Transform CreatePaletteRow()
        {
            var go = new GameObject("PaletteRow", typeof(RectTransform));
            go.transform.SetParent(paletteContent, false);
            var layout = go.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = PaletteCellSpacing;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            var element = go.AddComponent<LayoutElement>();
            element.minHeight = PaletteCellHeight;
            element.preferredHeight = PaletteCellHeight;
            element.flexibleWidth = 1f;
            return go.transform;
        }

        static void CollectPaletteItems(Transform parent, List<Transform> items, List<GameObject> rows)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                if (child.name == "PaletteRow")
                {
                    rows.Add(child.gameObject);
                    CollectPaletteItems(child, items, rows);
                    continue;
                }

                if (child.GetComponent<PaletteItem>() != null)
                {
                    items.Add(child);
                }
            }
        }

        void ScrollPaletteToTop()
        {
            if (paletteRoot == null)
            {
                return;
            }

            var scroll = paletteRoot.transform.Find("Scroll")?.GetComponent<ScrollRect>();
            if (scroll != null)
            {
                scroll.verticalNormalizedPosition = 1f;
            }
        }

        static RectTransform CreateTriangle(Transform parent, string name, Vector2 anchoredPosition)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(18f, 18f);
            var image = go.AddComponent<Image>();
            image.sprite = RightTriangleSprite();
            image.color = Color.white;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return rect;
        }

        static Sprite _rightTriangle;

        static Sprite RightTriangleSprite()
        {
            if (_rightTriangle != null)
            {
                return _rightTriangle;
            }

            const int size = 32;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                name = "PaletteRightTriangle"
            };
            var pixels = new Color[size * size];
            float centerY = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float t = (x + 0.5f - 2f) / (size - 5f);
                    float half = (1f - Mathf.Clamp01(t)) * (size * 0.5f - 3f);
                    bool inside = t >= 0f && t <= 1f && Mathf.Abs(y + 0.5f - centerY) <= half;
                    pixels[y * size + x] = inside ? Color.white : Color.clear;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            _rightTriangle = Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
            return _rightTriangle;
        }

        void CreatePaletteButton(int id, string label, Color color, Sprite sprite)
        {
            var go = new GameObject(label, typeof(RectTransform));
            go.transform.SetParent(paletteContent, false);
            var element = go.AddComponent<LayoutElement>();
            element.minWidth = 40f;
            element.flexibleWidth = 1f;
            element.minHeight = PaletteCellHeight;
            element.preferredHeight = PaletteCellHeight;

            var image = go.AddComponent<Image>();
            image.color = new Color(0.18f, 0.18f, 0.22f, 0.95f);
            var button = go.AddComponent<Button>();
            button.targetGraphic = image;
            int tileId = id;
            button.onClick.AddListener(() => _editor.SelectTile(tileId));
            go.AddComponent<PaletteItem>().tileId = id;

            var iconGo = new GameObject("Icon", typeof(RectTransform));
            iconGo.transform.SetParent(go.transform, false);
            var iconRt = (RectTransform)iconGo.transform;
            iconRt.anchorMin = new Vector2(0f, 0.5f);
            iconRt.anchorMax = new Vector2(0f, 0.5f);
            iconRt.pivot = new Vector2(0.5f, 0.5f);
            iconRt.anchoredPosition = new Vector2(36f, 0f);
            iconRt.sizeDelta = new Vector2(44f, 44f);
            var icon = iconGo.AddComponent<Image>();
            icon.sprite = sprite != null ? sprite : SpriteFactory.CreateSolid(color);
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            var labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(go.transform, false);
            var labelRt = (RectTransform)labelGo.transform;
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = new Vector2(72f, 8f);
            labelRt.offsetMax = new Vector2(-8f, -8f);
            var text = labelGo.AddComponent<Text>();
            text.font = UiFonts.Default;
            text.fontSize = 20;
            text.alignment = TextAnchor.MiddleLeft;
            text.color = Color.white;
            text.text = label;
            text.raycastTarget = false;
        }

        public static UiButton CreateButton(Transform parent, string name, string label, Vector2 size, UnityAction onClick, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.sizeDelta = size;
            var image = go.AddComponent<Image>();
            image.color = color;
            var element = go.AddComponent<LayoutElement>();
            element.minHeight = size.y;
            element.preferredHeight = size.y;
            var button = go.AddComponent<Button>();
            button.targetGraphic = image;
            if (onClick != null)
            {
                button.onClick.AddListener(onClick);
            }

            var textGo = new GameObject("Text", typeof(RectTransform));
            textGo.transform.SetParent(go.transform, false);
            Stretch((RectTransform)textGo.transform);
            var text = textGo.AddComponent<Text>();
            text.font = UiFonts.Default;
            text.fontSize = 22;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.text = label;
            return new UiButton { button = button, rect = rect, layout = element, label = text };
        }

        public static UiField CreateInputField(Transform parent, string name, string placeholder, Vector2 size)
        {
            return CreateInputField(parent, name, placeholder, size, InputField.ContentType.IntegerNumber);
        }

        public static UiField CreateInputField(Transform parent, string name, string placeholder, Vector2 size, InputField.ContentType contentType)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.sizeDelta = size;
            var element = go.AddComponent<LayoutElement>();
            element.minHeight = size.y;
            element.preferredHeight = size.y;

            var image = go.AddComponent<Image>();
            image.color = new Color(0.08f, 0.08f, 0.1f, 0.95f);

            var input = go.AddComponent<InputField>();
            input.contentType = contentType;

            var textGo = new GameObject("Text", typeof(RectTransform));
            textGo.transform.SetParent(go.transform, false);
            var textRt = (RectTransform)textGo.transform;
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(12f, 6f);
            textRt.offsetMax = new Vector2(-12f, -6f);
            var text = textGo.AddComponent<Text>();
            text.font = UiFonts.Default;
            text.fontSize = 20;
            text.alignment = TextAnchor.MiddleLeft;
            text.color = Color.white;
            text.supportRichText = false;
            input.textComponent = text;

            var placeholderGo = new GameObject("Placeholder", typeof(RectTransform));
            placeholderGo.transform.SetParent(go.transform, false);
            var placeholderRt = (RectTransform)placeholderGo.transform;
            placeholderRt.anchorMin = Vector2.zero;
            placeholderRt.anchorMax = Vector2.one;
            placeholderRt.offsetMin = new Vector2(12f, 6f);
            placeholderRt.offsetMax = new Vector2(-12f, -6f);
            var placeholderText = placeholderGo.AddComponent<Text>();
            placeholderText.font = UiFonts.Default;
            placeholderText.fontSize = 20;
            placeholderText.alignment = TextAnchor.MiddleLeft;
            placeholderText.color = new Color(1f, 1f, 1f, 0.35f);
            placeholderText.text = placeholder;
            input.placeholder = placeholderText;

            return new UiField { field = input, rect = rect, layout = element };
        }

        public static UiLabel CreateLabel(Transform parent, string name, string text, int fontSize, TextAnchor alignment)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            var label = go.AddComponent<Text>();
            label.font = UiFonts.Default;
            label.fontSize = fontSize;
            label.alignment = alignment;
            label.color = Color.white;
            label.text = text;
            label.raycastTarget = false;
            var element = go.AddComponent<LayoutElement>();
            element.minHeight = fontSize + 12f;
            element.preferredHeight = fontSize + 12f;
            return new UiLabel { text = label, rect = rect, layout = element };
        }

        static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }

    public struct UiButton
    {
        public Button button;
        public RectTransform rect;
        public LayoutElement layout;
        public Text label;
    }

    public struct UiField
    {
        public InputField field;
        public RectTransform rect;
        public LayoutElement layout;
    }

    public struct UiLabel
    {
        public Text text;
        public RectTransform rect;
        public LayoutElement layout;
    }

    public static class UiFonts
    {
        static Font _font;

        public static Font Default
        {
            get
            {
                if (_font == null)
                {
                    _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    if (_font == null)
                    {
                        _font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                    }
                }

                return _font;
            }
        }
    }

    public class StageListItem : MonoBehaviour
    {
        public string stageId;
    }

    public class StageListRenameControl : MonoBehaviour
    {
        StageEditorUI _ui;
        string _stageId;
        Button _openButton;
        InputField _input;
        Text _label;
        bool _editing;

        public void Init(StageEditorUI ui, string stageId, Button openButton, InputField input, Text label, Button renameButton)
        {
            _ui = ui;
            _stageId = stageId;
            _openButton = openButton;
            _input = input;
            _label = label;
            if (renameButton != null)
            {
                renameButton.onClick.AddListener(OnClicked);
            }

            if (_input != null)
            {
                _input.onSubmit.AddListener(OnSubmit);
            }
        }

        void OnClicked()
        {
            if (!_editing)
            {
                Open();
                return;
            }

            Submit();
        }

        void OnSubmit(string _)
        {
            if (_editing)
            {
                Submit();
            }
        }

        void Open()
        {
            _editing = true;
            if (_openButton != null)
            {
                _openButton.gameObject.SetActive(false);
            }

            if (_input != null)
            {
                _input.gameObject.SetActive(true);
                _input.text = StagePersistence.DisplayName(_stageId);
                _input.ActivateInputField();
                _input.caretPosition = _input.text != null ? _input.text.Length : 0;
            }

            if (_label != null)
            {
                _label.text = "決定";
            }
        }

        void Submit()
        {
            _ui?.CommitStageRename(_stageId, _input != null ? _input.text : string.Empty);
        }
    }

    public class StageListDeleteHold : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        const float HoldSeconds = 5f;

        StageEditorUI _ui;
        string _stageId;
        Text _label;
        bool _holding;
        float _held;

        public void Init(StageEditorUI ui, string stageId, Text label)
        {
            _ui = ui;
            _stageId = stageId;
            _label = label;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _holding = true;
            _held = 0f;
            ShowRemaining();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            Cancel();
        }

        void Update()
        {
            if (!_holding)
            {
                return;
            }

            _held += Time.unscaledDeltaTime;
            if (_held >= HoldSeconds)
            {
                _holding = false;
                if (_label != null)
                {
                    _label.text = "削除";
                }

                _ui?.RequestDeleteStage(_stageId);
                return;
            }

            ShowRemaining();
        }

        void ShowRemaining()
        {
            if (_label == null)
            {
                return;
            }

            int remaining = Mathf.CeilToInt(HoldSeconds - _held);
            _label.text = Mathf.Clamp(remaining, 1, 5).ToString();
        }

        void Cancel()
        {
            if (!_holding)
            {
                return;
            }

            _holding = false;
            _held = 0f;
            if (_label != null)
            {
                _label.text = "削除";
            }
        }
    }

    public class StageListDragHandle : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        StageEditorUI _ui;
        RectTransform _row;
        RectTransform _content;
        RectTransform _placeholder;
        LayoutElement _rowLayout;
        int _startIndex = -1;
        float _pointerOffsetY;
        bool _dragging;

        public void Init(StageEditorUI ui, LayoutElement rowLayout)
        {
            _ui = ui;
            _rowLayout = rowLayout;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            _row = transform.parent as RectTransform;
            _content = _row != null ? _row.parent as RectTransform : null;
            if (_row == null || _content == null || _rowLayout == null)
            {
                return;
            }

            _startIndex = _row.GetSiblingIndex();
            _dragging = true;

            var placeholderGo = new GameObject("Placeholder", typeof(RectTransform));
            placeholderGo.transform.SetParent(_content, false);
            placeholderGo.transform.SetSiblingIndex(_startIndex);
            _placeholder = (RectTransform)placeholderGo.transform;
            var placeholderLayout = placeholderGo.AddComponent<LayoutElement>();
            placeholderLayout.minHeight = _row.rect.height;
            placeholderLayout.preferredHeight = _row.rect.height;
            var placeholderImage = placeholderGo.AddComponent<Image>();
            placeholderImage.color = new Color(1f, 1f, 1f, 0.12f);
            placeholderImage.raycastTarget = false;

            Vector2 size = _row.rect.size;
            _rowLayout.ignoreLayout = true;
            _row.SetAsLastSibling();
            _row.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);
            _row.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y);
            _pointerOffsetY = _row.position.y - PointerWorldY(eventData);
            FollowPointer(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_dragging || _row == null || _content == null || _placeholder == null)
            {
                return;
            }

            FollowPointer(eventData);
            int insert = IndexForRowCenter(_row.position.y);
            if (_placeholder.GetSiblingIndex() != insert)
            {
                _placeholder.SetSiblingIndex(insert);
                _row.SetAsLastSibling();
                LayoutRebuilder.ForceRebuildLayoutImmediate(_content);
                FollowPointer(eventData);
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!_dragging)
            {
                return;
            }

            int target = _placeholder != null ? _placeholder.GetSiblingIndex() : _startIndex;
            FinishDrag(target, true);
            if (target != _startIndex)
            {
                _ui?.CommitStageOrderFromList();
            }
        }

        void OnDisable()
        {
            if (!_dragging)
            {
                return;
            }

            FinishDrag(_startIndex, false);
        }

        void FinishDrag(int index, bool immediate)
        {
            _dragging = false;
            if (_rowLayout != null)
            {
                _rowLayout.ignoreLayout = false;
            }

            if (_placeholder != null)
            {
                if (immediate)
                {
                    DestroyImmediate(_placeholder.gameObject);
                }
                else
                {
                    Destroy(_placeholder.gameObject);
                }

                _placeholder = null;
            }

            if (_row != null && _content != null)
            {
                int last = Mathf.Max(0, _content.childCount - 1);
                _row.SetSiblingIndex(Mathf.Clamp(index, 0, last));
                if (immediate)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(_content);
                }
            }
        }

        void FollowPointer(PointerEventData eventData)
        {
            Vector3 position = _row.position;
            position.y = PointerWorldY(eventData) + _pointerOffsetY;
            _row.position = position;
        }

        float PointerWorldY(PointerEventData eventData)
        {
            if (!RectTransformUtility.ScreenPointToWorldPointInRectangle(
                    _content,
                    eventData.position,
                    eventData.pressEventCamera,
                    out Vector3 world))
            {
                return _row != null ? _row.position.y - _pointerOffsetY : 0f;
            }

            return world.y;
        }

        int IndexForRowCenter(float rowY)
        {
            int insert = 0;
            for (int i = 0; i < _content.childCount; i++)
            {
                var child = _content.GetChild(i) as RectTransform;
                if (child == null || child == _row || child == _placeholder)
                {
                    continue;
                }

                if (rowY < child.position.y)
                {
                    insert++;
                }
                else
                {
                    break;
                }
            }

            return insert;
        }
    }
}

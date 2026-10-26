using UnityEngine;
using UnityEngine.Events;
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

        public bool IsOverlayOpen =>
            (settingsPanel != null && settingsPanel.activeSelf)
            || (stageListPanel != null && stageListPanel.activeSelf);

        StageEditorController _editor;
        int _currentWidth;
        bool _chromeVisible = true;

        public void Bind(GameObject editor, GameObject settings, GameObject hint, Transform content, Text status, StageEditorController controller)
        {
            editorRoot = editor;
            settingsPanel = settings;
            hintRoot = hint;
            paletteContent = content;
            statusLabel = status;
            _editor = controller;
            EnsureSettingsControls();
            EnsureStageListControls();
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
            if (widthInputField != null)
            {
                widthInputField.onEndEdit.AddListener(OnWidthEndEdit);
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
            if (widthInputField != null)
            {
                widthInputField.onEndEdit.RemoveListener(OnWidthEndEdit);
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

        public void RefreshSettings(string stageId, int currentWidth)
        {
            if (stageNameLabel != null)
            {
                stageNameLabel.text = string.IsNullOrEmpty(stageId) ? "ステージ" : stageId;
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
        }

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
            }
        }

        public void ToggleChrome()
        {
            SetChromeVisible(!_chromeVisible);
        }

        public void SetChromeVisible(bool visible)
        {
            _chromeVisible = visible;
            SetNamedChildActive(editorRoot, "Palette", visible);
            SetNamedChildActive(editorRoot, "TopBar", visible);
            SetNamedChildActive(editorRoot, "Status", visible);
            if (!visible)
            {
                SetSettingsVisible(false);
                SetStageListVisible(false);
            }
        }

        static void SetNamedChildActive(GameObject root, string childName, bool active)
        {
            if (root == null)
            {
                return;
            }

            var child = root.transform.Find(childName);
            if (child != null)
            {
                child.gameObject.SetActive(active);
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
                string label = current ? $"{stage.id}（編集中）" : stage.id;
                var color = current ? new Color(0.28f, 0.48f, 0.38f) : new Color(0.18f, 0.18f, 0.22f);
                int index = i;
                var button = CreateButton(stageListContent, $"StageItem_{i}", label, new Vector2(400f, 52f), () => _editor?.SwitchToStage(index), color);
                var text = button.GetComponentInChildren<Text>();
                if (text != null)
                {
                    text.fontSize = 20;
                }
            }
        }

        public void SetSettingsVisible(bool visible)
        {
            if (settingsPanel == null)
            {
                return;
            }

            if (visible)
            {
                EnsureSettingsControls();
            }

            settingsPanel.SetActive(visible);
        }

        void EnsureSettingsControls()
        {
            if (settingsPanel == null)
            {
                return;
            }

            EnsureSettingsPanelLayout();
            HideUnusedSettingsObjects();

            if (stageNameLabel == null)
            {
                var existing = settingsPanel.transform.Find("CurrentStageName");
                stageNameLabel = existing != null
                    ? existing.GetComponent<Text>()
                    : CreateLabel(settingsPanel.transform, "CurrentStageName", "ステージ", 24, TextAnchor.MiddleLeft);
            }

            stageNameLabel.gameObject.SetActive(true);

            EnsureWidthRow();

            if (changeStageButton == null)
            {
                var existing = settingsPanel.transform.Find("ChangeStage");
                changeStageButton = existing != null
                    ? existing.GetComponent<Button>()
                    : CreateButton(
                        settingsPanel.transform,
                        "ChangeStage",
                        "ステージ変更",
                        new Vector2(360f, 48f),
                        null,
                        new Color(0.32f, 0.38f, 0.58f));
            }

            changeStageButton.gameObject.SetActive(true);
            SetButtonLabel(changeStageButton, "ステージ変更");

            if (openFolderButton == null)
            {
                var existing = settingsPanel.transform.Find("OpenEditStageFolder");
                openFolderButton = existing != null
                    ? existing.GetComponent<Button>()
                    : CreateButton(
                        settingsPanel.transform,
                        "OpenEditStageFolder",
                        "フォルダを開く",
                        new Vector2(360f, 48f),
                        null,
                        new Color(0.2f, 0.45f, 0.4f));
            }

            openFolderButton.gameObject.SetActive(true);
            SetButtonLabel(openFolderButton, "フォルダを開く");

            if (closeButton == null)
            {
                var existing = settingsPanel.transform.Find("ClosePanel");
                closeButton = existing != null
                    ? existing.GetComponent<Button>()
                    : CreateButton(
                        settingsPanel.transform,
                        "ClosePanel",
                        "閉じる",
                        new Vector2(360f, 48f),
                        null,
                        new Color(0.3f, 0.3f, 0.33f));
            }

            closeButton.gameObject.SetActive(true);
            NormalizeSettingsOrder();
        }

        void HideUnusedSettingsObjects()
        {
            SetInactive(saveAsNewButton);
            SetInactive(overwriteButton);
            SetInactive(widthApplyButton);
            SetInactive(widthCurrentLabel);
            SetInactive(widthWarningLabel);

            var root = settingsPanel.transform;
            for (int i = 0; i < root.childCount; i++)
            {
                var child = root.GetChild(i);
                if (!IsKeptSettingsChild(child.name))
                {
                    child.gameObject.SetActive(false);
                }
            }
        }

        static bool IsKeptSettingsChild(string name)
        {
            return name == "CurrentStageName"
                   || name == "WidthRow"
                   || name == "ChangeStage"
                   || name == "OpenEditStageFolder"
                   || name == "ClosePanel";
        }

        static void SetInactive(Component component)
        {
            if (component != null)
            {
                component.gameObject.SetActive(false);
            }
        }

        static void SetButtonLabel(Button button, string label)
        {
            if (button == null)
            {
                return;
            }

            var text = button.GetComponentInChildren<Text>();
            if (text != null)
            {
                text.text = label;
            }
        }

        void EnsureWidthRow()
        {
            var settings = settingsPanel.transform;
            var row = settings.Find("WidthRow") as RectTransform;
            if (row == null)
            {
                var go = new GameObject("WidthRow", typeof(RectTransform));
                go.transform.SetParent(settings, false);
                row = go.GetComponent<RectTransform>();
                var element = go.AddComponent<LayoutElement>();
                element.minHeight = 48f;
                element.preferredHeight = 48f;
                var layout = go.AddComponent<HorizontalLayoutGroup>();
                layout.spacing = 12f;
                layout.childAlignment = TextAnchor.MiddleLeft;
                layout.childForceExpandWidth = false;
                layout.childForceExpandHeight = true;
                layout.childControlWidth = true;
                layout.childControlHeight = true;
                var label = CreateLabel(row, "WidthLabel", "横幅", 22, TextAnchor.MiddleLeft);
                var labelElement = label.GetComponent<LayoutElement>();
                if (labelElement != null)
                {
                    labelElement.minWidth = 72f;
                    labelElement.preferredWidth = 72f;
                    labelElement.flexibleWidth = 0f;
                }
            }

            if (widthInputField == null)
            {
                var existing = row.Find("WidthInput") ?? settings.Find("WidthInput");
                widthInputField = existing != null
                    ? existing.GetComponent<InputField>()
                    : CreateInputField(row, "WidthInput", "48", new Vector2(240f, 48f));
            }

            if (widthInputField != null && widthInputField.transform.parent != row)
            {
                widthInputField.transform.SetParent(row, false);
            }

            if (widthInputField != null)
            {
                widthInputField.gameObject.SetActive(true);
                var inputElement = widthInputField.GetComponent<LayoutElement>();
                if (inputElement != null)
                {
                    inputElement.flexibleWidth = 1f;
                    inputElement.minWidth = 120f;
                    inputElement.minHeight = 48f;
                    inputElement.preferredHeight = 48f;
                }
            }

            row.gameObject.SetActive(true);
        }

        void EnsureSettingsPanelLayout()
        {
            var rt = settingsPanel.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(420f, 360f);
                rt.anchoredPosition = Vector2.zero;
            }

            var layout = settingsPanel.GetComponent<VerticalLayoutGroup>();
            if (layout != null)
            {
                layout.childForceExpandHeight = false;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = true;
                layout.childControlWidth = true;
                layout.spacing = 12f;
                layout.padding = new RectOffset(24, 24, 24, 24);
            }
        }

        void NormalizeSettingsOrder()
        {
            int index = 0;
            Place(stageNameLabel, ref index);
            var widthRow = settingsPanel.transform.Find("WidthRow");
            if (widthRow != null)
            {
                widthRow.SetSiblingIndex(index);
                index++;
            }

            Place(changeStageButton, ref index);
            Place(openFolderButton, ref index);
            Place(closeButton, ref index);
        }

        static void Place(Component component, ref int index)
        {
            if (component != null)
            {
                component.transform.SetSiblingIndex(index);
                index++;
            }
        }

        void EnsureStageListControls()
        {
            if (stageListPanel == null)
            {
                Transform parent = settingsPanel != null ? settingsPanel.transform.parent : transform;
                var existing = parent != null ? parent.Find("StageListPanel") : null;
                stageListPanel = existing != null ? existing.gameObject : CreateStageListPanel(parent);
            }

            var panel = stageListPanel.transform;
            if (stageListContent == null)
            {
                var content = panel.Find("Scroll/Viewport/Content");
                stageListContent = content != null ? content : BuildStageListBody(panel);
            }

            if (newStageInputField == null)
            {
                var input = panel.Find("NewStageRow/NewStageInput");
                newStageInputField = input != null ? input.GetComponent<InputField>() : null;
            }

            if (createStageButton == null)
            {
                var button = panel.Find("NewStageRow/CreateStage");
                createStageButton = button != null ? button.GetComponent<Button>() : null;
            }

            if (closeStageListButton == null)
            {
                var button = panel.Find("CloseStageList");
                closeStageListButton = button != null ? button.GetComponent<Button>() : null;
            }

            if (newStageWarningLabel == null)
            {
                var warning = panel.Find("NewStageWarning");
                newStageWarningLabel = warning != null ? warning.GetComponent<Text>() : null;
            }

            SetNewStageWarning(null);
            stageListPanel.SetActive(false);
        }

        GameObject CreateStageListPanel(Transform parent)
        {
            var go = new GameObject("StageListPanel", typeof(RectTransform));
            if (parent != null)
            {
                go.transform.SetParent(parent, false);
            }

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(520f, 680f);
            rt.anchoredPosition = Vector2.zero;

            var image = go.AddComponent<Image>();
            image.color = new Color(0.12f, 0.12f, 0.16f, 0.96f);
            var layout = go.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(24, 24, 24, 24);
            layout.spacing = 12f;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            BuildStageListBody(go.transform);
            return go;
        }

        Transform BuildStageListBody(Transform panel)
        {
            CreateLabel(panel, "StageListTitle", "ステージ一覧", 24, TextAnchor.MiddleLeft);

            var scrollGo = new GameObject("Scroll", typeof(RectTransform));
            scrollGo.transform.SetParent(panel, false);
            var scrollElement = scrollGo.AddComponent<LayoutElement>();
            scrollElement.minHeight = 360f;
            scrollElement.preferredHeight = 360f;
            scrollElement.flexibleHeight = 1f;
            var scrollImage = scrollGo.AddComponent<Image>();
            scrollImage.color = new Color(0.08f, 0.08f, 0.1f, 0.9f);
            var scroll = scrollGo.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            var viewport = new GameObject("Viewport", typeof(RectTransform));
            viewport.transform.SetParent(scrollGo.transform, false);
            var viewportRt = viewport.GetComponent<RectTransform>();
            viewportRt.anchorMin = Vector2.zero;
            viewportRt.anchorMax = Vector2.one;
            viewportRt.offsetMin = new Vector2(8f, 8f);
            viewportRt.offsetMax = new Vector2(-8f, -8f);
            viewport.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.02f);
            var mask = viewport.AddComponent<Mask>();
            mask.showMaskGraphic = false;
            scroll.viewport = viewportRt;

            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);
            var contentRt = content.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.offsetMin = Vector2.zero;
            contentRt.offsetMax = Vector2.zero;
            var fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var contentLayout = content.AddComponent<VerticalLayoutGroup>();
            contentLayout.spacing = 8f;
            contentLayout.padding = new RectOffset(4, 4, 4, 4);
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;
            contentLayout.childControlHeight = true;
            contentLayout.childControlWidth = true;
            scroll.content = contentRt;
            stageListContent = contentRt;

            var warning = CreateLabel(panel, "NewStageWarning", string.Empty, 18, TextAnchor.MiddleLeft);
            warning.color = new Color(1f, 0.72f, 0.45f, 1f);
            warning.gameObject.SetActive(false);
            newStageWarningLabel = warning;

            var row = new GameObject("NewStageRow", typeof(RectTransform));
            row.transform.SetParent(panel, false);
            var rowElement = row.AddComponent<LayoutElement>();
            rowElement.minHeight = 48f;
            rowElement.preferredHeight = 48f;
            var rowLayout = row.AddComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = 8f;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = true;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;

            newStageInputField = CreateInputField(row.transform, "NewStageInput", "新規ステージ名（空なら自動）", new Vector2(280f, 48f), InputField.ContentType.Standard);
            var inputElement = newStageInputField.GetComponent<LayoutElement>();
            if (inputElement != null)
            {
                inputElement.flexibleWidth = 1f;
                inputElement.minWidth = 180f;
            }

            createStageButton = CreateButton(row.transform, "CreateStage", "作成", new Vector2(120f, 48f), null, new Color(0.25f, 0.5f, 0.35f));
            var createElement = createStageButton.GetComponent<LayoutElement>();
            if (createElement != null)
            {
                createElement.minWidth = 120f;
                createElement.preferredWidth = 120f;
                createElement.flexibleWidth = 0f;
            }

            closeStageListButton = CreateButton(panel, "CloseStageList", "閉じる", new Vector2(360f, 48f), null, new Color(0.3f, 0.3f, 0.33f));
            return contentRt;
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
                Destroy(paletteContent.GetChild(i).gameObject);
            }

            CreatePaletteButton(TileIds.PlayerSpawn, "初期位置", new Color(0.3f, 0.75f, 1f), SpriteFactory.CreateCircle(new Color(0.3f, 0.75f, 1f)));
            foreach (var tile in database.EnumeratePalette())
            {
                if (tile != null)
                {
                    CreatePaletteButton(tile.id, tile.displayName, tile.color, tile.sprite);
                }
            }
        }

        void CreatePaletteButton(int id, string label, Color color, Sprite sprite)
        {
            var go = new GameObject(label, typeof(RectTransform));
            go.transform.SetParent(paletteContent, false);
            go.AddComponent<LayoutElement>().minHeight = 72f;
            go.GetComponent<LayoutElement>().preferredHeight = 72f;

            var image = go.AddComponent<Image>();
            image.color = new Color(0.18f, 0.18f, 0.22f, 0.95f);
            var button = go.AddComponent<Button>();
            button.targetGraphic = image;
            int tileId = id;
            button.onClick.AddListener(() => _editor.SelectTile(tileId));
            go.AddComponent<PaletteItem>().tileId = id;

            var iconGo = new GameObject("Icon", typeof(RectTransform));
            iconGo.transform.SetParent(go.transform, false);
            var iconRt = iconGo.GetComponent<RectTransform>();
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
            var labelRt = labelGo.GetComponent<RectTransform>();
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

        public static Button CreateButton(Transform parent, string name, string label, Vector2 size, UnityAction onClick, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.GetComponent<RectTransform>().sizeDelta = size;
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
            var textRt = textGo.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;
            var text = textGo.AddComponent<Text>();
            text.font = UiFonts.Default;
            text.fontSize = 22;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.text = label;
            return button;
        }

        public static InputField CreateInputField(Transform parent, string name, string placeholder, Vector2 size)
        {
            return CreateInputField(parent, name, placeholder, size, InputField.ContentType.IntegerNumber);
        }

        public static InputField CreateInputField(Transform parent, string name, string placeholder, Vector2 size, InputField.ContentType contentType)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.GetComponent<RectTransform>().sizeDelta = size;
            var element = go.AddComponent<LayoutElement>();
            element.minHeight = size.y;
            element.preferredHeight = size.y;

            var image = go.AddComponent<Image>();
            image.color = new Color(0.08f, 0.08f, 0.1f, 0.95f);

            var input = go.AddComponent<InputField>();
            input.contentType = contentType;

            var textGo = new GameObject("Text", typeof(RectTransform));
            textGo.transform.SetParent(go.transform, false);
            var textRt = textGo.GetComponent<RectTransform>();
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
            var placeholderRt = placeholderGo.GetComponent<RectTransform>();
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

            return input;
        }

        public static Text CreateLabel(Transform parent, string name, string text, int fontSize, TextAnchor alignment)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
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
            return label;
        }
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
}

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

namespace Pulling
{
    public static class GameSceneFactory
    {
        public static void Build(GameManager manager)
        {
            var camera = Camera.main;
            if (camera == null)
            {
                var camGo = new GameObject("Main Camera");
                camera = camGo.AddComponent<Camera>();
                camGo.AddComponent<AudioListener>();
                camGo.tag = "MainCamera";
                camera.orthographic = true;
                camera.orthographicSize = 6f;
                camera.backgroundColor = new Color(0.19f, 0.3f, 0.47f);
                camera.clearFlags = CameraClearFlags.SolidColor;
                camGo.transform.position = new Vector3(0f, 0f, -10f);
            }

            var gridGo = FindOrCreate("StageGrid");
            var grid = GetOrAdd<Grid>(gridGo);
            grid.cellSize = new Vector3(1f, 1f, 1f);

            var tilemapGo = FindOrCreate("GroundTilemap");
            if (tilemapGo.transform.parent != gridGo.transform)
            {
                tilemapGo.transform.SetParent(gridGo.transform, false);
            }

            GetOrAdd<Tilemap>(tilemapGo);
            var tileRenderer = GetOrAdd<TilemapRenderer>(tilemapGo);
            tileRenderer.sortOrder = TilemapRenderer.SortOrder.BottomLeft;
            tileRenderer.sortingOrder = 0;

            var tileBody = GetOrAdd<Rigidbody2D>(tilemapGo);
            tileBody.bodyType = RigidbodyType2D.Static;

            var tileCollider = GetOrAdd<TilemapCollider2D>(tilemapGo);
            tileCollider.compositeOperation = Collider2D.CompositeOperation.Merge;

            var composite = GetOrAdd<CompositeCollider2D>(tilemapGo);
            composite.geometryType = CompositeCollider2D.GeometryType.Polygons;
            composite.generationType = CompositeCollider2D.GenerationType.Synchronous;

            var specials = FindOrCreate("StageSpecials");

            var builder = GetOrAdd<GridMapBuilder>(manager.gameObject);
            builder.groundTilemap = tilemapGo.GetComponent<Tilemap>();
            builder.specialRoot = specials.transform;
            builder.tileDatabase = manager.tileDatabase;

            var player = FindOrCreate("Player");
            player.transform.localScale = Vector3.one * 0.8f;
            var playerRenderer = GetOrAdd<SpriteRenderer>(player);
            playerRenderer.sortingOrder = 5;

            var playerBody = GetOrAdd<Rigidbody2D>(player);
            playerBody.gravityScale = PlayerTuning.GravityScale;
            playerBody.linearDamping = PlayerTuning.FlightLinearDamping;
            playerBody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            playerBody.freezeRotation = true;
            playerBody.interpolation = RigidbodyInterpolation2D.Interpolate;

            var playerCol = GetOrAdd<CircleCollider2D>(player);
            playerCol.radius = 0.42f;

            var aimGo = FindOrCreate("AimLine");
            var line = GetOrAdd<LineRenderer>(aimGo);
            line.startColor = new Color(1f, 1f, 1f, 0.95f);
            line.endColor = new Color(1f, 1f, 1f, 0.15f);
            line.startWidth = 0.07f;
            line.endWidth = 0.02f;
            line.sortingOrder = 20;
            line.positionCount = 2;
            line.enabled = false;
            GetOrAdd<AimLineView>(aimGo);

            var playerCtrl = GetOrAdd<PlayerController>(player);

            var camCtrl = GetOrAdd<StageCameraController>(camera.gameObject);
            camCtrl.target = player.transform;

            var lightGo = FindOrCreate("SkyLight");
            var skyLight = GetOrAdd<SkyLightView>(lightGo);

            var overlayGo = FindOrCreate("GridOverlay");
            var overlay = GetOrAdd<GridOverlay>(overlayGo);
            var hoverGo = FindOrCreate("CellHover");
            var hoverRenderer = GetOrAdd<SpriteRenderer>(hoverGo);
            hoverRenderer.sortingOrder = 8;

            var ghostGo = FindOrCreate("EditorGhost");
            GetOrAdd<SpriteRenderer>(ghostGo).sortingOrder = 9;
            ghostGo.SetActive(false);

            var spawnGo = FindOrCreate("SpawnMarker");
            var spawnRenderer = GetOrAdd<SpriteRenderer>(spawnGo);
            spawnRenderer.sortingOrder = 7;
            spawnGo.transform.localScale = Vector3.one * 0.7f;
            spawnGo.SetActive(false);

            BuildUi(manager);

            manager.mapBuilder = builder;
            manager.player = playerCtrl;
            manager.stageCamera = camCtrl;
            manager.transition = GetOrAdd<StageTransition>(manager.gameObject);
            manager.editor = GetOrAdd<StageEditorController>(manager.gameObject);
            manager.editorUI = GetOrAdd<StageEditorUI>(manager.gameObject);
            manager.gridOverlay = overlay;
            manager.skyLight = skyLight;
            manager.cellHover = hoverGo.transform;
            manager.editorGhost = ghostGo.transform;
            manager.spawnMarker = spawnGo.transform;
        }

        static void BuildUi(GameManager manager)
        {
            var canvasGo = FindOrCreate("GameCanvas");
            if (canvasGo.GetComponent<RectTransform>() == null)
            {
                canvasGo.AddComponent<RectTransform>();
            }

            var canvas = GetOrAdd<Canvas>(canvasGo);
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = GetOrAdd<CanvasScaler>(canvasGo);
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            GetOrAdd<GraphicRaycaster>(canvasGo);

            if (Object.FindFirstObjectByType<EventSystem>() == null)
            {
                var eventGo = new GameObject("EventSystem");
                eventGo.AddComponent<EventSystem>();
                eventGo.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
            }

            var editorRoot = FindOrCreateRect("EditorRoot", canvasGo.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var settings = FindOrCreateRect("SettingsPanel", canvasGo.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-210f, -180f), new Vector2(210f, 180f));
            settings.offsetMin = new Vector2(-210f, -180f);
            settings.offsetMax = new Vector2(210f, 180f);
            var hint = FindOrCreateRect("Hint", canvasGo.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-280f, 16f), new Vector2(280f, 56f));
            var fade = FindOrCreateRect("WhiteFade", canvasGo.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var fadeImg = GetOrAdd<Image>(fade.gameObject);
            fadeImg.color = new Color(1f, 1f, 1f, 0f);
            fadeImg.raycastTarget = false;
            manager.fadeImage = fadeImg;

            var ui = GetOrAdd<StageEditorUI>(manager.gameObject);
            BuildEditorChrome(editorRoot, settings, hint, ui);
            editorRoot.gameObject.SetActive(false);
            settings.gameObject.SetActive(false);
            hint.gameObject.SetActive(true);
        }

        static void BuildEditorChrome(RectTransform editorRoot, RectTransform settings, RectTransform hint, StageEditorUI ui)
        {
            var palette = FindOrCreateRect("Palette", editorRoot, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(16f, 16f), new Vector2(250f, -80f));
            var paletteImage = GetOrAdd<Image>(palette.gameObject);
            paletteImage.color = new Color(0.08f, 0.08f, 0.1f, 0.82f);

            var scrollGo = FindOrCreateRect("Scroll", palette, Vector2.zero, Vector2.one, new Vector2(8f, 8f), new Vector2(-8f, -8f));
            var scroll = GetOrAdd<ScrollRect>(scrollGo.gameObject);
            var viewport = FindOrCreateRect("Viewport", scrollGo, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            GetOrAdd<Image>(viewport.gameObject).color = new Color(1f, 1f, 1f, 0.02f);
            var mask = GetOrAdd<Mask>(viewport.gameObject);
            mask.showMaskGraphic = false;

            var content = FindOrCreateRect("Content", viewport, new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            content.pivot = new Vector2(0.5f, 1f);
            var fitter = GetOrAdd<ContentSizeFitter>(content.gameObject);
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var layout = GetOrAdd<VerticalLayoutGroup>(content.gameObject);
            layout.spacing = 8f;
            layout.padding = new RectOffset(8, 8, 8, 8);
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.childControlHeight = true;
            layout.childControlWidth = true;

            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            var topBar = FindOrCreateRect("TopBar", editorRoot, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-520f, -84f), new Vector2(-16f, -16f));
            var settingsBtn = EnsureButton(topBar, "SettingsButton", "設定", new Vector2(200f, 56f), new Color(0.2f, 0.35f, 0.55f));
            settingsBtn.anchorMin = new Vector2(0f, 0.5f);
            settingsBtn.anchorMax = new Vector2(0f, 0.5f);
            settingsBtn.pivot = new Vector2(0f, 0.5f);
            settingsBtn.anchoredPosition = Vector2.zero;

            var finishBtn = EnsureButton(topBar, "FinishButton", "編集完了", new Vector2(200f, 56f), new Color(0.25f, 0.5f, 0.3f));
            finishBtn.anchorMin = new Vector2(1f, 0.5f);
            finishBtn.anchorMax = new Vector2(1f, 0.5f);
            finishBtn.pivot = new Vector2(1f, 0.5f);
            finishBtn.anchoredPosition = Vector2.zero;

            var statusGo = FindOrCreateRect("Status", editorRoot, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-240f, -72f), new Vector2(240f, -16f));
            var status = GetOrAdd<Text>(statusGo.gameObject);
            status.font = UiFonts.Default;
            status.fontSize = 22;
            status.alignment = TextAnchor.MiddleCenter;
            status.color = Color.white;
            status.raycastTarget = false;

            var settingsImage = GetOrAdd<Image>(settings.gameObject);
            settingsImage.color = new Color(0.12f, 0.12f, 0.16f, 0.96f);
            var settingsLayout = GetOrAdd<VerticalLayoutGroup>(settings.gameObject);
            settingsLayout.padding = new RectOffset(24, 24, 24, 24);
            settingsLayout.spacing = 12f;
            settingsLayout.childForceExpandWidth = true;
            settingsLayout.childForceExpandHeight = false;
            settingsLayout.childControlHeight = true;
            settingsLayout.childControlWidth = true;

            HideChild(settings, "WidthCurrent");
            HideChild(settings, "WidthWarning");
            HideChild(settings, "WidthApply");
            HideChild(settings, "SaveAsNew");
            HideChild(settings, "Overwrite");
            HideChild(settings, "StageName");

            var stageName = EnsureLabel(settings, "CurrentStageName", "ステージ", 24, TextAnchor.MiddleLeft);
            var changeStage = EnsureButton(settings, "ChangeStage", "ステージ変更", new Vector2(360f, 48f), new Color(0.32f, 0.38f, 0.58f));
            var openFolder = EnsureButton(settings, "OpenEditStageFolder", "フォルダを開く", new Vector2(360f, 48f), new Color(0.2f, 0.45f, 0.4f));
            var closePanel = EnsureButton(settings, "ClosePanel", "閉じる", new Vector2(360f, 48f), new Color(0.3f, 0.3f, 0.33f));

            var hintText = GetOrAdd<Text>(hint.gameObject);
            hintText.font = UiFonts.Default;
            hintText.fontSize = 20;
            hintText.alignment = TextAnchor.MiddleCenter;
            hintText.color = new Color(1f, 1f, 1f, 0.7f);
            hintText.text = "R でリトライ    Shift+D でステージ編集";
            hintText.raycastTarget = false;

            ui.editorRoot = editorRoot.gameObject;
            ui.settingsPanel = settings.gameObject;
            ui.hintRoot = hint.gameObject;
            ui.paletteContent = content;
            ui.statusLabel = status;
            ui.settingsButton = settingsBtn.GetComponent<Button>();
            ui.finishButton = finishBtn.GetComponent<Button>();
            ui.saveAsNewButton = null;
            ui.overwriteButton = null;
            ui.openFolderButton = openFolder.GetComponent<Button>();
            ui.closeButton = closePanel.GetComponent<Button>();
            ui.changeStageButton = changeStage.GetComponent<Button>();
            ui.stageNameLabel = stageName;
            ui.widthCurrentLabel = null;
            ui.widthWarningLabel = null;
            ui.widthApplyButton = null;
        }

        static void HideChild(Transform parent, string name)
        {
            var child = parent.Find(name);
            if (child != null)
            {
                child.gameObject.SetActive(false);
            }
        }

        static Text EnsureLabel(Transform parent, string name, string text, int fontSize, TextAnchor alignment)
        {
            var existing = parent.Find(name);
            if (existing != null)
            {
                return existing.GetComponent<Text>();
            }

            return StageEditorUI.CreateLabel(parent, name, text, fontSize, alignment);
        }

        static InputField EnsureInputField(Transform parent, string name, string placeholder, Vector2 size)
        {
            var existing = parent.Find(name);
            if (existing != null)
            {
                return existing.GetComponent<InputField>();
            }

            return StageEditorUI.CreateInputField(parent, name, placeholder, size);
        }

        static RectTransform EnsureButton(Transform parent, string name, string label, Vector2 size, Color color)
        {
            var existing = parent.Find(name) as RectTransform;
            if (existing != null)
            {
                return existing;
            }

            var button = StageEditorUI.CreateButton(parent, name, label, size, null, color);
            return button.GetComponent<RectTransform>();
        }

        static RectTransform FindOrCreateRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var existing = parent.Find(name) as RectTransform;
            if (existing != null)
            {
                return existing;
            }

            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
            return rt;
        }

        static GameObject FindOrCreate(string name)
        {
            var go = GameObject.Find(name);
            if (go == null)
            {
                go = new GameObject(name);
            }

            return go;
        }

        static T GetOrAdd<T>(GameObject go) where T : Component
        {
            var component = go.GetComponent<T>();
            if (component == null)
            {
                component = go.AddComponent<T>();
            }

            return component;
        }
    }
}

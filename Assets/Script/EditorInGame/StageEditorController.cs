using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Pulling
{
    public class StageEditorController : MonoBehaviour
    {
        public float panSpeed = 14f;

        GameManager _game;
        GridMapBuilder _builder;
        StageCameraController _camera;
        StageEditorUI _ui;
        GridOverlay _overlay;
        Transform _ghost;
        Transform _spawnMarker;
        SpriteRenderer _ghostRenderer;

        bool _active;
        bool _painting;
        bool _erasing;
        bool _paintDirty;
        bool _dragPanning;
        bool _middlePressing;
        bool _middleDragged;
        int _selectedTile = TileIds.Ground;
        Vector2 _dragPanScreen;
        Vector2 _middlePressScreen;

        const float MiddleClickSlop = 8f;

        public bool IsActive => _active;

        public void Bind(
            GameManager game,
            GridMapBuilder builder,
            StageCameraController cameraController,
            StageEditorUI ui,
            GridOverlay overlay,
            Transform ghost,
            Transform spawnMarker)
        {
            _game = game;
            _builder = builder;
            _camera = cameraController;
            _ui = ui;
            _overlay = overlay;
            _ghost = ghost;
            _spawnMarker = spawnMarker;
            _ghostRenderer = ghost != null ? ghost.GetComponent<SpriteRenderer>() : null;
            SetGhostVisible(false);
            if (_spawnMarker != null)
            {
                _spawnMarker.gameObject.SetActive(false);
            }
        }

        public void Toggle()
        {
            if (_active) Exit();
            else Enter();
        }

        public void Enter()
        {
            if (_active || _game == null || _game.IsTransitioning)
            {
                return;
            }

            _active = true;
            _game.OnEditorEntered();
            _overlay.Show(_builder.Width, _builder.Height, _builder.CellSize);
            _ui.SetEditorVisible(true);
            _ui.SetSettingsVisible(false);
            _ui.RebuildPalette(_game.TileDatabase);
            SelectTile(TileIds.Ground);
            RefreshSpawnMarker();
            _ui.SetStatus(CurrentStatusText());
        }

        public void Exit()
        {
            if (!_active)
            {
                return;
            }

            _active = false;
            _painting = false;
            _erasing = false;
            _dragPanning = false;
            _middlePressing = false;
            _middleDragged = false;
            AutoSave();
            _paintDirty = false;
            SetGhostVisible(false);
            _overlay.Hide();
            _ui.SetEditorVisible(false);
            if (_spawnMarker != null)
            {
                _spawnMarker.gameObject.SetActive(false);
            }

            _game.OnEditorExited();
        }

        public void OpenSettings()
        {
            _ui.RefreshSettings(_game.RuntimeStage != null ? _game.RuntimeStage.id : string.Empty, _builder.Width);
            _ui.SetSettingsVisible(true);
        }
        public void CloseSettings() => _ui.SetSettingsVisible(false);

        public void OpenStageList()
        {
            _ui.RebuildStageList(_game.Catalog, _game.CatalogIndex);
            _ui.SetStageListVisible(true);
        }

        public void CloseStageList() => _ui.SetStageListVisible(false);

        public void SwitchToStage(int index)
        {
            if (_game == null || _game.Catalog == null || index < 0 || index >= _game.Catalog.Count)
            {
                return;
            }

            if (index == _game.CatalogIndex)
            {
                _ui.SetStatus(CurrentStatusText());
                _ui.SetStageListVisible(false);
                return;
            }

            AutoSave();
            _game.LoadStage(index, true);
            RefreshAfterStageChange();
            _ui.SetStageListVisible(false);
            _ui.SetStatus($"{_game.RuntimeStage.id} に切り替えました");
        }

        public void CreateStage(string requestedId)
        {
            if (_game == null)
            {
                return;
            }

            AutoSave();
            if (_game.TryCreateBlankStage(requestedId, out string message))
            {
                RefreshAfterStageChange();
                _ui.RebuildStageList(_game.Catalog, _game.CatalogIndex);
                _ui.ClearNewStageInput();
                _ui.SetNewStageWarning(null);
                _ui.SetStatus(message);
                return;
            }

            _ui.SetNewStageWarning(message);
            _ui.SetStatus(message);
        }

        void RefreshAfterStageChange()
        {
            if (_builder != null)
            {
                _overlay?.Show(_builder.Width, _builder.Height, _builder.CellSize);
                if (_camera != null)
                {
                    _camera.followEnabled = false;
                    _camera.SetMapDimensions(_builder.Width, _builder.Height, _builder.CellSize);
                }
            }

            RefreshSpawnMarker();
            _ui.RefreshSettings(
                _game.RuntimeStage != null ? _game.RuntimeStage.id : string.Empty,
                _builder != null ? _builder.Width : 0);
        }

        public void OpenEditStageFolder()
        {
            StagePersistence.OpenEditStageFolder();
            _ui.SetStatus("EditStageフォルダを開きました");
        }

        public void SaveAsNew()
        {
            AutoSave();
        }

        public void Overwrite()
        {
            AutoSave();
        }

        void AutoSave()
        {
            if (_game == null)
            {
                return;
            }

            _game.CommitEditorToRuntime();
            _game.OverwriteRuntime();
        }

        public void ApplyGridWidth(int newWidth)
        {
            if (_builder == null || _game?.RuntimeStage == null)
            {
                return;
            }

            newWidth = Mathf.Clamp(newWidth, 1, 256);
            if (newWidth == _builder.Width)
            {
                _ui.RefreshWidthSettings(_builder.Width);
                _ui.SetStatus(CurrentStatusText());
                return;
            }

            _builder.ResizeWidth(newWidth);
            var stage = _game.RuntimeStage;
            stage.width = _builder.Width;
            if (stage.spawnX >= _builder.Width)
            {
                stage.spawnX = _builder.Width - 1;
            }

            _overlay.Show(_builder.Width, _builder.Height, _builder.CellSize);
            _camera?.SetMapDimensions(_builder.Width, _builder.Height, _builder.CellSize);
            if (_camera != null)
            {
                var pos = _camera.transform.position;
                pos = _camera.ClampPosition(pos);
                pos.z = _camera.transform.position.z;
                _camera.transform.position = pos;
            }

            RefreshSpawnMarker();
            _ui.RefreshSettings(_game.RuntimeStage.id, _builder.Width);
            AutoSave();
            _ui.SetStatus(CurrentStatusText());
        }

        public bool WouldLoseTilesOnWidthResize(int newWidth)
        {
            return _builder != null && newWidth < _builder.Width && _builder.HasContentInColumnsFrom(newWidth);
        }

        public void SelectTile(int tileId)
        {
            _selectedTile = tileId;
            UpdateGhostVisual();
            _ui.SetStatus(CurrentStatusText());
        }

        void Update()
        {
            if (_game == null || _game.IsTransitioning)
            {
                return;
            }

            var keyboard = Keyboard.current;
            if (keyboard != null
                && (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed)
                && keyboard.dKey.wasPressedThisFrame)
            {
                Toggle();
            }

            if (!_active)
            {
                return;
            }

            HandlePan(keyboard);
            HandleDragPan();
            HandlePaint();
        }

        void HandlePan(Keyboard keyboard)
        {
            if (keyboard == null || _camera == null)
            {
                return;
            }

            Vector3 delta = Vector3.zero;
            bool shift = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) delta.x -= 1f;
            if ((keyboard.dKey.isPressed && !shift) || keyboard.rightArrowKey.isPressed) delta.x += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) delta.y -= 1f;
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) delta.y += 1f;
            if (delta.sqrMagnitude > 0f)
            {
                var pos = _camera.transform.position;
                pos += delta.normalized * panSpeed * Time.unscaledDeltaTime;
                pos = _camera.ClampPosition(pos);
                pos.z = _camera.transform.position.z;
                _camera.transform.position = pos;
            }
        }

        void HandleDragPan()
        {
            if (Mouse.current == null)
            {
                return;
            }

            var mouse = Mouse.current;
            Vector2 screen = mouse.position.ReadValue();
            if (mouse.middleButton.wasPressedThisFrame)
            {
                _middlePressing = true;
                _middleDragged = false;
                _dragPanning = false;
                _middlePressScreen = screen;
                _dragPanScreen = screen;
            }

            if (_middlePressing && mouse.middleButton.isPressed)
            {
                if (!_middleDragged && (screen - _middlePressScreen).sqrMagnitude > MiddleClickSlop * MiddleClickSlop)
                {
                    _middleDragged = true;
                    _dragPanScreen = screen;
                }

                bool overUi = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
                bool overlayOpen = _ui != null && _ui.IsOverlayOpen;
                if (_middleDragged && !overUi && !overlayOpen && _camera != null)
                {
                    _dragPanning = true;
                    var cam = Camera.main;
                    if (cam != null)
                    {
                        float z = Mathf.Abs(cam.transform.position.z);
                        Vector3 before = cam.ScreenToWorldPoint(new Vector3(_dragPanScreen.x, _dragPanScreen.y, z));
                        Vector3 after = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, z));
                        Vector3 delta = before - after;
                        delta.z = 0f;

                        var pos = _camera.transform.position + delta;
                        pos = _camera.ClampPosition(pos);
                        pos.z = _camera.transform.position.z;
                        _camera.transform.position = pos;
                    }

                    _dragPanScreen = screen;
                }
            }

            if (mouse.middleButton.wasReleasedThisFrame && _middlePressing)
            {
                if (!_middleDragged)
                {
                    _ui?.ToggleChrome();
                }

                _middlePressing = false;
                _middleDragged = false;
                _dragPanning = false;
            }
        }

        void HandlePaint()
        {
            if (Mouse.current == null)
            {
                return;
            }

            bool overUi = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            bool overlayOpen = _ui != null && _ui.IsOverlayOpen;
            bool leftPressed = Mouse.current.leftButton.wasPressedThisFrame;
            bool leftHeld = Mouse.current.leftButton.isPressed;
            bool leftReleased = Mouse.current.leftButton.wasReleasedThisFrame;
            bool rightPressed = Mouse.current.rightButton.wasPressedThisFrame;
            bool rightHeld = Mouse.current.rightButton.isPressed;
            bool rightReleased = Mouse.current.rightButton.wasReleasedThisFrame;
            Vector2 screen = Mouse.current.position.ReadValue();

            if (overUi || overlayOpen || _dragPanning)
            {
                SetGhostVisible(false);
                _overlay.SetHover(false, Vector3.zero, _builder.CellSize);
                if (leftReleased || rightReleased)
                {
                    FinishPaintStroke();
                }

                return;
            }

            ShowHover(screen);
            if (!_painting && !_erasing)
            {
                if (leftPressed)
                {
                    _painting = true;
                    PaintAt(screen, _selectedTile);
                }
                else if (rightPressed)
                {
                    _erasing = true;
                    PaintAt(screen, TileIds.Empty);
                }
            }
            else if (_painting && leftHeld)
            {
                PaintAt(screen, _selectedTile);
            }
            else if (_erasing && rightHeld)
            {
                PaintAt(screen, TileIds.Empty);
            }

            if ((_painting && leftReleased) || (_erasing && rightReleased))
            {
                FinishPaintStroke();
            }
        }

        void ShowHover(Vector2 screen)
        {
            if (!TryScreenToCell(screen, out int x, out int y, out Vector3 center))
            {
                SetGhostVisible(false);
                _overlay.SetHover(false, Vector3.zero, _builder.CellSize);
                return;
            }

            SetGhostVisible(true);
            if (_ghost != null)
            {
                _ghost.position = center;
            }

            _overlay.SetHover(true, center, _builder.CellSize);
        }

        void PaintAt(Vector2 screen, int tileId)
        {
            if (!TryScreenToCell(screen, out int x, out int y, out _))
            {
                return;
            }

            if (tileId == TileIds.PlayerSpawn)
            {
                if (_game.RuntimeStage.spawnX != x || _game.RuntimeStage.spawnY != y)
                {
                    _game.RuntimeStage.spawnX = x;
                    _game.RuntimeStage.spawnY = y;
                    _paintDirty = true;
                    RefreshSpawnMarker();
                    _ui.SetStatus(CurrentStatusText());
                }

                return;
            }

            if (_builder.GetCell(x, y) == tileId)
            {
                return;
            }

            _builder.SetCell(x, y, tileId);
            _builder.SetCollidersEnabled(false);
            _paintDirty = true;
        }

        void FinishPaintStroke()
        {
            _painting = false;
            _erasing = false;
            if (_paintDirty)
            {
                AutoSave();
                _paintDirty = false;
            }
        }

        bool TryScreenToCell(Vector2 screen, out int x, out int y, out Vector3 center)
        {
            x = 0;
            y = 0;
            center = Vector3.zero;
            var cam = Camera.main;
            if (cam == null || _builder == null)
            {
                return false;
            }

            Vector3 world = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, Mathf.Abs(cam.transform.position.z)));
            world.z = 0f;
            if (!_builder.TryWorldToCell(world, out x, out y))
            {
                return false;
            }

            center = _builder.CellCenter(x, y);
            return true;
        }

        void UpdateGhostVisual()
        {
            if (_ghostRenderer == null)
            {
                return;
            }

            if (_selectedTile == TileIds.PlayerSpawn)
            {
                _ghostRenderer.sprite = SpriteFactory.CreateCircle(new Color(0.3f, 0.75f, 1f, 0.7f));
                _ghostRenderer.color = Color.white;
                return;
            }

            var def = _game != null && _game.TileDatabase != null ? _game.TileDatabase.Get(_selectedTile) : null;
            if (def != null)
            {
                _ghostRenderer.sprite = def.sprite != null ? def.sprite : SpriteFactory.CreateSolid(def.color);
                var color = def.color;
                color.a = 0.7f;
                _ghostRenderer.color = color;
            }
        }

        void RefreshSpawnMarker()
        {
            if (_spawnMarker == null || _game.RuntimeStage == null)
            {
                return;
            }

            _spawnMarker.gameObject.SetActive(true);
            _spawnMarker.position = _builder.CellCenter(_game.RuntimeStage.spawnX, _game.RuntimeStage.spawnY);
        }

        void SetGhostVisible(bool visible)
        {
            if (_ghost != null)
            {
                _ghost.gameObject.SetActive(visible);
            }
        }

        string CurrentStatusText()
        {
            var stage = _game.RuntimeStage;
            return stage != null ? $"{stage.id}  初期位置 ({stage.spawnX},{stage.spawnY})" : string.Empty;
        }
    }
}

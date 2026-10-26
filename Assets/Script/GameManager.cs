using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Pulling
{
    public class GameManager : MonoBehaviour
    {
        public StageCatalog catalog;
        public TileDatabase tileDatabase;
        public GridMapBuilder mapBuilder;
        public PlayerController player;
        public StageCameraController stageCamera;
        public StageTransition transition;
        public StageEditorController editor;
        public StageEditorUI editorUI;
        public GridOverlay gridOverlay;
        public SkyLightView skyLight;
        public Transform cellHover;
        public Transform editorGhost;
        public Transform spawnMarker;
        public UnityEngine.UI.Image fadeImage;

        GridMapBuilder _builder;
        PlayerController _player;
        StageCameraController _camera;
        StageTransition _transition;
        StageEditorController _editor;

        StageAsset _sourceAsset;
        int _catalogIndex;
        bool _transitioning;
        bool _resetting;
        bool _booted;
        bool _inSunlight;

        public TileDatabase TileDatabase => tileDatabase;
        public StageCatalog Catalog => catalog;
        public int CatalogIndex => _catalogIndex;
        public StageAsset RuntimeStage { get; private set; }
        public bool IsTransitioning => _transitioning;

        public bool HasSceneRefs()
        {
            return mapBuilder != null
                   && player != null
                   && stageCamera != null
                   && transition != null
                   && editor != null
                   && editorUI != null
                   && fadeImage != null
                   && gridOverlay != null
                   && skyLight != null;
        }

        void Awake()
        {
            if (_booted)
            {
                return;
            }

            _booted = true;
            ResolveData();
            tileDatabase.EnsureDefaults();
            GameSceneRuntime.Bind(this);
            enabled = true;
        }

        void Start()
        {
            LoadStage(0, false);
        }

        void Update()
        {
            if (_transitioning || _resetting || (_editor != null && _editor.IsActive))
            {
                return;
            }

            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.rKey.wasPressedThisFrame)
            {
                _resetting = true;
                _player?.SetLocked(true);
            }
        }

        void LateUpdate()
        {
            if (_player != null)
            {
                _player.CheckFallenOff();
            }

            TickSunlight();
            if (_resetting)
            {
                RebuildCurrent();
                _resetting = false;
            }
        }

        void OnDisable()
        {
            Time.timeScale = 1f;
        }

        public void BindRuntime(
            GridMapBuilder builder,
            PlayerController player,
            StageCameraController cameraController,
            StageTransition transition,
            StageEditorController editor)
        {
            _builder = builder;
            _player = player;
            _camera = cameraController;
            _transition = transition;
            _editor = editor;
        }

        public void NotifyGoalReached()
        {
            if (_transitioning || (_editor != null && _editor.IsActive))
            {
                return;
            }

            StartCoroutine(ClearRoutine());
        }

        public void NotifyHazardHit()
        {
            if (_transitioning || _resetting || (_editor != null && _editor.IsActive))
            {
                return;
            }

            _resetting = true;
            _player?.SetLocked(true);
        }

        void TickSunlight()
        {
            if (_transitioning || _resetting || (_editor != null && _editor.IsActive) || _player == null || _builder == null)
            {
                _inSunlight = false;
                _player?.ClearSunlightExposure();
                return;
            }

            Vector3 pos = _player.transform.position;
            bool exposed = _builder.TryWorldToCell(pos, out int x, out int y)
                           && SunlightZone.IsInSkyLight(_builder, tileDatabase, x, y);
            if (exposed && !_inSunlight && _player.ConsumeSakuyaShield())
            {
                _player.ClearSunlightExposure();
                _player.ReturnToSafePosition();
                _inSunlight = false;
                return;
            }

            _inSunlight = exposed;
            if (!exposed)
            {
                _player.ClearSunlightExposure();
                return;
            }

            if (_player.DrainSunlight(Time.unscaledDeltaTime))
            {
                _inSunlight = false;
                NotifyHazardHit();
            }
        }

        public void OnEditorEntered()
        {
            _player?.SetInputEnabled(false);
            _player?.Freeze();
            if (_camera != null)
            {
                _camera.followEnabled = false;
            }

            _builder?.SetCollidersEnabled(false);
            _player?.SetHpBarVisible(false);
        }

        public void OnEditorExited()
        {
            CommitEditorToRuntime();
            RebuildCurrent();
            if (_camera != null)
            {
                _camera.followEnabled = true;
            }

            _player?.SetInputEnabled(true);
            _player?.SetHpBarVisible(true);
        }

        public void CommitEditorToRuntime()
        {
            if (RuntimeStage == null || _builder == null || _builder.Grid == null)
            {
                return;
            }

            RuntimeStage.width = _builder.Width;
            RuntimeStage.height = _builder.Height;
            RuntimeStage.cellSize = _builder.CellSize;
            RuntimeStage.rows = StageCodec.Encode(_builder.Grid, _builder.Width, _builder.Height);
        }

        public StageAsset SaveRuntimeAsNew()
        {
            var created = StagePersistence.SaveAsNew(RuntimeStage, catalog);
            if (created != null)
            {
                _sourceAsset = created;
                _catalogIndex = catalog != null ? catalog.IndexOf(created) : -1;
                RuntimeStage.id = created.id;
            }

            return created;
        }

        public bool OverwriteRuntime()
        {
            if (_sourceAsset == null)
            {
                var created = SaveRuntimeAsNew();
                return created != null;
            }

            _sourceAsset.CopyFrom(RuntimeStage);
            _sourceAsset.id = RuntimeStage.id;
            if (StagePersistence.Overwrite(_sourceAsset))
            {
                return true;
            }

            return SaveRuntimeAsNew() != null;
        }

        public bool TryCreateBlankStage(string requestedId, out string message)
        {
            string id = requestedId;
            if (string.IsNullOrWhiteSpace(id))
            {
                id = $"Stage_{StagePersistence.NextStageNumber(catalog)}";
            }
            else if (!StagePersistence.TryNormalizeStageId(id, out id, out message))
            {
                return false;
            }

            if (catalog != null && catalog.IndexOfId(id) >= 0)
            {
                message = $"{id} は既に存在します。";
                return false;
            }

            var created = StagePersistence.SaveNamed(StageFactory.CreateBlank(id), catalog);
            if (created == null)
            {
                message = "新規ステージの作成に失敗しました。";
                return false;
            }

            LoadStage(catalog.IndexOf(created), true);
            message = $"{created.id} を作成しました";
            return true;
        }

        public void LoadStage(int index, bool snapCamera)
        {
            if (catalog == null || catalog.Count == 0)
            {
                return;
            }

            _catalogIndex = ((index % catalog.Count) + catalog.Count) % catalog.Count;
            _sourceAsset = catalog.Get(_catalogIndex);
            if (_sourceAsset == null)
            {
                _sourceAsset = StageFactory.CreateDefault();
            }

            RuntimeStage = _sourceAsset.CreateRuntimeClone();
            RebuildCurrent();
            if (snapCamera)
            {
                _camera?.SnapToTarget();
            }

            if (_editor != null && _editor.IsActive)
            {
                _builder?.SetCollidersEnabled(false);
            }
        }

        void RebuildCurrent()
        {
            if (RuntimeStage == null || _builder == null)
            {
                return;
            }

            _inSunlight = false;
            _builder.Build(RuntimeStage);
            _builder.SetCollidersEnabled(true);
            Vector3 spawn = _builder.CellCenter(RuntimeStage.spawnX, RuntimeStage.spawnY);
            _player?.PlaceAt(spawn);
            _player?.SetFallBounds(-2f, _builder.Width * _builder.CellSize + 2f, -2f, NotifyHazardHit);
            _player?.SetLocked(false);
            _player?.SetInputEnabled(_editor == null || !_editor.IsActive);
            _camera?.Configure(RuntimeStage);
        }

        IEnumerator ClearRoutine()
        {
            _transitioning = true;
            _player?.SetLocked(true);

            yield return _transition.Play(() =>
            {
                LoadStage(_catalogIndex + 1, true);
            });

            _transitioning = false;
        }

        void ResolveData()
        {
            if (catalog == null)
            {
                catalog = StagePersistence.LoadCatalog();
            }

            if (tileDatabase == null)
            {
                tileDatabase = StagePersistence.LoadTileDatabase();
            }

            if (tileDatabase == null)
            {
                tileDatabase = ScriptableObject.CreateInstance<TileDatabase>();
                tileDatabase.name = "TileDatabase (runtime)";
            }

            catalog = StagePersistence.CreateRuntimeCatalog(catalog);
            StagePersistence.ApplyEditStageOverrides(catalog);

            if (catalog.Count == 0)
            {
                catalog.stages.Add(StageFactory.CreateDefault());
            }
        }
    }
}

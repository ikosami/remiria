using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Pulling
{
    public class GameManager : MonoBehaviour
    {
        public StageCatalog catalog;
        public TileDatabase tileDatabase;
        public Remiria remiria;
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

            if (_player != null && _player.TryRespawnAtSakuya())
            {
                _inSunlight = false;
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

            float sunlightDt = _player.IsAirSlowMotion ? Time.deltaTime : Time.unscaledDeltaTime;
            _player.TickSunImmunity(sunlightDt);

            Vector3 pos = _player.transform.position;
            bool exposed = SunlightZone.IsInSkyLight(_builder, tileDatabase, pos);
            if (_player.IsSunImmune)
            {
                _inSunlight = exposed;
                _player.ClearSunlightExposure();
                return;
            }

            _inSunlight = exposed;
            if (!exposed)
            {
                _player.ClearSunlightExposure();
                return;
            }

            if (_player.DrainSunlight(sunlightDt))
            {
                _inSunlight = false;
                NotifyHazardHit();
            }
        }

        public void OnEditorEntered()
        {
            _player?.SetInputEnabled(false);
            _player?.SetVisible(false);
            if (_camera != null)
            {
                _camera.followEnabled = false;
            }

            _builder?.SetCollidersEnabled(false);
            _builder?.SilenceEnemies();
            _player?.SetHpBarVisible(false);
        }

        public void OnEditorExited()
        {
            CommitEditorToRuntime();
            _player?.SetVisible(true);
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
            RuntimeStage.PruneMeilings(_builder.Width, _builder.Height, (x, y) => _builder.GetCell(x, y) == TileIds.Meiling);
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

            if (RuntimeStage != null && StagePersistence.IsDeletedEditStage(RuntimeStage.id, catalog))
            {
                return false;
            }

            _sourceAsset.CopyFrom(RuntimeStage);
            _sourceAsset.id = RuntimeStage.id;
            return StagePersistence.Overwrite(_sourceAsset);
        }

        public bool TryDeleteStage(int index, out string message)
        {
            if (catalog == null || index < 0 || index >= catalog.Count || catalog.stages[index] == null)
            {
                message = "ステージが見つかりません。";
                return false;
            }

            string id = catalog.stages[index].id;
            bool current = RuntimeStage != null && RuntimeStage.id == id;
            if (!StagePersistence.TryDelete(id, catalog, out message))
            {
                return false;
            }

            message = $"{StagePersistence.DisplayName(id)} を削除しました";
            if (catalog.Count == 0)
            {
                return true;
            }

            if (current)
            {
                int next = index >= catalog.Count ? catalog.Count - 1 : index;
                LoadStage(next, true);
            }
            else if (index < _catalogIndex)
            {
                _catalogIndex--;
            }

            return true;
        }

        public bool TryRenameStage(string currentId, string requestedName, out string message)
        {
            message = null;
            if (catalog == null)
            {
                message = "ステージが見つかりません。";
                return false;
            }

            int index = catalog.IndexOfId(currentId);
            if (index < 0 || catalog.stages[index] == null)
            {
                message = "ステージが見つかりません。";
                return false;
            }

            var stage = catalog.stages[index];
            if (!StagePersistence.TryRename(stage, catalog, requestedName, out bool changed, out message))
            {
                return false;
            }

            if (!changed)
            {
                return true;
            }

            string newId = stage.id;
            if (RuntimeStage != null && RuntimeStage.id == currentId)
            {
                RuntimeStage.id = newId;
            }

            if (_sourceAsset != null && _sourceAsset.id == currentId)
            {
                _sourceAsset.id = newId;
            }

            message = $"{StagePersistence.DisplayName(newId)} に変更しました";
            return true;
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
                message = $"{StagePersistence.DisplayName(id)} は既に存在します。";
                return false;
            }

            var created = StagePersistence.SaveNamed(StageFactory.CreateBlank(id), catalog);
            if (created == null)
            {
                message = "新規ステージの作成に失敗しました。";
                return false;
            }

            LoadStage(catalog.IndexOf(created), true);
            message = $"{StagePersistence.DisplayName(created.id)} を作成しました";
            return true;
        }

        public bool TryReorderStages(System.Collections.Generic.IList<string> orderedIds, out string message)
        {
            if (!StagePersistence.TryApplyStageOrder(catalog, orderedIds, out message))
            {
                return false;
            }

            if (RuntimeStage != null)
            {
                int index = catalog.IndexOfId(RuntimeStage.id);
                if (index >= 0)
                {
                    _catalogIndex = index;
                    _sourceAsset = catalog.Get(index);
                }
            }

            message = "ステージの順番を保存しました";
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
                _player?.SetVisible(false);
                _builder?.SetCollidersEnabled(false);
                _builder?.SetMeilingGuides(true);
            }
        }

        void RebuildCurrent()
        {
            if (RuntimeStage == null || _builder == null)
            {
                return;
            }

            _inSunlight = false;
            _builder.SetCollidersEnabled(true);
            _builder.Build(RuntimeStage);
            Vector3 spawn = _builder.CellCenter(RuntimeStage.spawnX, RuntimeStage.spawnY);
            _player?.PlaceAt(spawn);
            _player?.SetFallBounds(-2f, _builder.Width * _builder.CellSize + 2f, -2f, NotifyHazardHit);
            _player?.SetLocked(false);
            _player?.SetInputEnabled(_editor == null || !_editor.IsActive);
            _camera?.Configure(RuntimeStage);
        }

        public void RefreshStageCatalog()
        {
            string id = RuntimeStage != null ? RuntimeStage.id : null;
            StagePersistence.ApplyEditStageOverrides(catalog);
            if (catalog == null || catalog.Count == 0)
            {
                return;
            }

            int index = string.IsNullOrEmpty(id) ? -1 : catalog.IndexOfId(id);
            if (index >= 0)
            {
                _catalogIndex = index;
                _sourceAsset = catalog.Get(index);
                return;
            }

            LoadStage(Mathf.Clamp(_catalogIndex, 0, catalog.Count - 1), true);
        }

        IEnumerator ClearRoutine()
        {
            _transitioning = true;
            _player?.SetLocked(true);
            string currentId = RuntimeStage != null ? RuntimeStage.id : null;
            int fallbackIndex = _catalogIndex;

            yield return _transition.Play(() =>
            {
                StagePersistence.ApplyEditStageOverrides(catalog);
                LoadStage(StagePersistence.ResolveNextIndex(catalog, currentId, fallbackIndex), true);
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

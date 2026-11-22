using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Pulling
{
    public class GridMapBuilder : MonoBehaviour
    {
        public Tilemap groundTilemap;
        public Transform specialRoot;
        public TileDatabase tileDatabase;

        readonly Dictionary<int, Tile> _tiles = new Dictionary<int, Tile>();
        readonly List<TilePiece> _specials = new List<TilePiece>();
        readonly List<TileLabelView> _labels = new List<TileLabelView>();
        readonly List<CompanionMeiling> _meilings = new List<CompanionMeiling>();
        readonly List<EnemyBlock> _enemies = new List<EnemyBlock>();
        readonly List<CeilingWall> _ceilingWalls = new List<CeilingWall>();
        readonly List<DoorGate> _doors = new List<DoorGate>();
        readonly List<Vector2Int> _doorCells = new List<Vector2Int>();
        readonly List<SwitchPlate> _toggles = new List<SwitchPlate>();
        readonly List<SwitchPlate> _lights = new List<SwitchPlate>();
        readonly List<BlastPending> _blasts = new List<BlastPending>();
        readonly List<BlastPending> _blastPop = new List<BlastPending>();
        bool _doorsOpen;
        bool _latchedOpen;
        int _lightHold;
        int _doorEpoch;
        bool _assembling;
        bool _suppressDoors;
        const float CeilingWallHeight = 4096f;
        const float CeilingSeam = 0.05f;

        CompositeCollider2D _composite;
        TilemapCollider2D _tilemapCollider;
        PhysicsMaterial2D _blockMaterial;
        GameManager _game;
        SkyLightView _skyLight;
        Transform _ceilingRoot;
        bool _collidersEnabled = true;
        bool _showMeilingGuides;

        public int Width { get; private set; }
        public int Height { get; private set; }
        public float CellSize { get; private set; } = 1f;
        public int[] Grid { get; private set; }
        public string StageId { get; private set; }
        public bool IsEditing => _game != null && _game.editor != null && _game.editor.IsActive;
        public bool DeferSensors => _assembling || IsEditing;

        public void Bind(GameManager game, Tilemap tilemap, Transform specials, TileDatabase database)
        {
            _game = game;
            groundTilemap = tilemap;
            specialRoot = specials;
            tileDatabase = database;
            _tilemapCollider = tilemap.GetComponent<TilemapCollider2D>();
            _composite = tilemap.GetComponent<CompositeCollider2D>();
        }

        public void BindSkyLight(SkyLightView skyLight)
        {
            _skyLight = skyLight;
        }

        public void Build(StageAsset stage, int[] grid = null)
        {
            if (stage == null)
            {
                return;
            }

            tileDatabase.EnsureDefaults();
            _blasts.Clear();
            _blastPop.Clear();
            StageId = stage.id;
            Width = Mathf.Max(1, stage.width);
            Height = Mathf.Max(1, stage.height);
            CellSize = stage.cellSize > 0f ? stage.cellSize : 1f;
            Grid = grid ?? StageCodec.Decode(stage);
            if (groundTilemap != null)
            {
                groundTilemap.ClearAllTiles();
                var gridComp = groundTilemap.layoutGrid;
                if (gridComp != null)
                {
                    gridComp.cellSize = new Vector3(CellSize, CellSize, 1f);
                }
            }

            ClearSpecials();

            _assembling = true;
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    ApplyCell(x, y, Grid[y * Width + x]);
                }
            }

            _assembling = false;
            RefreshPhysics();
            RefreshSkyLight();
        }

        public void SetCell(int x, int y, int id, bool refresh = true)
        {
            if (!InBounds(x, y))
            {
                return;
            }

            Grid[y * Width + x] = id;
            var stage = _game != null ? _game.RuntimeStage : null;
            if (stage != null)
            {
                if (id == TileIds.Meiling)
                {
                    stage.EnsureMeiling(x, y);
                }
                else
                {
                    stage.RemoveMeiling(x, y);
                }
            }

            ApplyCell(x, y, id);
            if (!refresh)
            {
                return;
            }

            RefreshSkyLight();
            RefreshPhysics();
        }

        public int GetCell(int x, int y)
        {
            return InBounds(x, y) ? Grid[y * Width + x] : TileIds.Empty;
        }

        public bool InBounds(int x, int y)
        {
            return x >= 0 && y >= 0 && x < Width && y < Height;
        }

        public Vector3 CellCenter(int x, int y)
        {
            return new Vector3((x + 0.5f) * CellSize, (y + 0.5f) * CellSize, 0f);
        }

        public bool TryWorldToCell(Vector3 world, out int x, out int y)
        {
            x = Mathf.FloorToInt(world.x / CellSize);
            y = Mathf.FloorToInt(world.y / CellSize);
            return InBounds(x, y);
        }

        public void SetMeilingGuides(bool visible)
        {
            _showMeilingGuides = visible;
            for (int i = 0; i < _meilings.Count; i++)
            {
                _meilings[i].SetGuideVisible(visible);
            }
        }

        public void ConfigureMeiling(int x, int y, float angle, float strength)
        {
            _game?.RuntimeStage?.SetMeiling(x, y, angle, strength);
            var meiling = FindMeiling(x, y);
            if (meiling == null)
            {
                return;
            }

            meiling.Apply(angle, strength);
        }

        public void SilenceEnemies()
        {
            for (int i = 0; i < _enemies.Count; i++)
            {
                _enemies[i].Silence();
            }
        }

        public void SetCollidersEnabled(bool enabled)
        {
            if (_tilemapCollider != null)
            {
                _tilemapCollider.enabled = enabled;
                _tilemapCollider.compositeOperation = enabled
                    ? Collider2D.CompositeOperation.Merge
                    : Collider2D.CompositeOperation.None;
            }

            for (int i = 0; i < _specials.Count; i++)
            {
                _specials[i].SetSolid(enabled);
            }

            _collidersEnabled = enabled;
            for (int i = 0; i < _ceilingWalls.Count; i++)
            {
                _ceilingWalls[i].box.enabled = enabled;
            }
        }

        public void RefreshPhysics()
        {
            if (_tilemapCollider != null)
            {
                _tilemapCollider.ProcessTilemapChanges();
            }

            if (_composite != null)
            {
                _composite.GenerateGeometry();
            }

            Physics2D.SyncTransforms();
        }

        public void Clear()
        {
            if (groundTilemap != null)
            {
                groundTilemap.ClearAllTiles();
            }

            ClearSpecials();
            _blasts.Clear();
            _blastPop.Clear();
            Grid = null;
            RefreshSkyLight();
        }

        public void IgniteBlast(int x, int y)
        {
            float next = Time.time + PlayerTuning.BlastWaveDelay;
            ScheduleBlast(x + 1, y, next);
            ScheduleBlast(x - 1, y, next);
            ScheduleBlast(x, y + 1, next);
            ScheduleBlast(x, y - 1, next);
        }

        void Update()
        {
            TickBlasts();
        }

        void TickBlasts()
        {
            if (_blasts.Count == 0 || Grid == null)
            {
                return;
            }

            float now = Time.time;
            _blastPop.Clear();
            for (int i = 0; i < _blasts.Count; i++)
            {
                if (_blasts[i].time <= now)
                {
                    _blastPop.Add(_blasts[i]);
                }
            }

            if (_blastPop.Count == 0)
            {
                return;
            }

            for (int i = _blasts.Count - 1; i >= 0; i--)
            {
                if (_blasts[i].time <= now)
                {
                    _blasts.RemoveAt(i);
                }
            }

            bool cleared = false;
            for (int i = 0; i < _blastPop.Count; i++)
            {
                BlastPending pop = _blastPop[i];
                if (GetCell(pop.x, pop.y) != TileIds.BlastWall)
                {
                    continue;
                }

                BlastFlash.Spawn(specialRoot, CellCenter(pop.x, pop.y), CellSize);
                SetCell(pop.x, pop.y, TileIds.Empty, false);
                cleared = true;
                float next = pop.time + PlayerTuning.BlastWaveDelay;
                if (next <= now)
                {
                    next = now + PlayerTuning.BlastWaveDelay;
                }

                ScheduleBlast(pop.x + 1, pop.y, next);
                ScheduleBlast(pop.x - 1, pop.y, next);
                ScheduleBlast(pop.x, pop.y + 1, next);
                ScheduleBlast(pop.x, pop.y - 1, next);
            }

            _blastPop.Clear();
            if (cleared)
            {
                RefreshSkyLight();
                RefreshPhysics();
            }
        }

        void ScheduleBlast(int x, int y, float time)
        {
            if (GetCell(x, y) != TileIds.BlastWall)
            {
                return;
            }

            for (int i = 0; i < _blastPop.Count; i++)
            {
                if (_blastPop[i].x == x && _blastPop[i].y == y)
                {
                    return;
                }
            }

            for (int i = 0; i < _blasts.Count; i++)
            {
                if (_blasts[i].x != x || _blasts[i].y != y)
                {
                    continue;
                }

                if (time < _blasts[i].time)
                {
                    BlastPending earlier = _blasts[i];
                    earlier.time = time;
                    _blasts[i] = earlier;
                }

                return;
            }

            _blasts.Add(new BlastPending
            {
                x = x,
                y = y,
                time = time
            });
        }

        struct BlastPending
        {
            public int x;
            public int y;
            public float time;
        }

        public bool HasContentInColumnsFrom(int fromX)
        {
            if (Grid == null || fromX >= Width)
            {
                return false;
            }

            fromX = Mathf.Max(0, fromX);
            for (int y = 0; y < Height; y++)
            {
                for (int x = fromX; x < Width; x++)
                {
                    if (Grid[y * Width + x] != TileIds.Empty)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public void ResizeWidth(int newWidth)
        {
            newWidth = Mathf.Max(1, newWidth);
            if (Grid == null || newWidth == Width)
            {
                return;
            }

            int oldWidth = Width;
            var resized = new int[newWidth * Height];
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < newWidth; x++)
                {
                    resized[y * newWidth + x] = x < oldWidth ? Grid[y * oldWidth + x] : TileIds.Empty;
                }
            }

            Width = newWidth;
            Grid = resized;
            _blasts.Clear();
            _blastPop.Clear();
            RebuildAllCells();
        }

        void RebuildAllCells()
        {
            if (groundTilemap != null)
            {
                groundTilemap.ClearAllTiles();
            }

            ClearSpecials();
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    ApplyCell(x, y, Grid[y * Width + x]);
                }
            }

            RefreshPhysics();
            RefreshSkyLight();
        }

        void RefreshSkyLight()
        {
            _skyLight?.Rebuild();
            RebuildCeilingWalls();
            for (int i = _lights.Count - 1; i >= 0; i--)
            {
                if (_lights[i] == null)
                {
                    _lights.RemoveAt(i);
                    continue;
                }

                _lights[i].CatchLight();
            }
        }

        void RebuildCeilingWalls()
        {
            if (_ceilingRoot != null)
            {
                Destroy(_ceilingRoot.gameObject);
                _ceilingRoot = null;
                _ceilingWalls.Clear();
            }

            if (Grid == null || Width <= 0 || Height <= 0)
            {
                return;
            }

            int x = 0;
            while (x < Width)
            {
                if (IsOpenSkyColumn(x))
                {
                    x++;
                    continue;
                }

                if (_ceilingRoot == null)
                {
                    Transform parent = groundTilemap != null ? groundTilemap.transform : transform;
                    _ceilingRoot = SpawnLibrary.Spawn(SpawnLibrary.Current.ceilingRoot, parent);
                }

                int start = x;
                bool solid = IsSolidCeiling(x);
                x++;
                while (x < Width && !IsOpenSkyColumn(x) && IsSolidCeiling(x) == solid)
                {
                    x++;
                }

                AddCeilingWall(start, x - start, solid);
            }
        }

        bool IsOpenSkyColumn(int x)
        {
            return GetCell(x, Height - 1) == TileIds.Empty;
        }

        bool IsSolidCeiling(int x)
        {
            return SunlightZone.IsSolid(this, tileDatabase, x, Height - 1);
        }

        void AddCeilingWall(int startX, int span, bool solid)
        {
            float cell = CellSize;
            float width = span * cell;
            float bottom = Height * cell;
            if (solid)
            {
                bottom -= Mathf.Min(CeilingSeam, cell * 0.2f);
            }

            var wall = SpawnLibrary.Spawn(SpawnLibrary.Current.ceilingWall, _ceilingRoot);
            _ceilingWalls.Add(wall);
            var box = wall.box;
            box.sharedMaterial = BlockMaterial();
            box.enabled = _collidersEnabled;
            box.size = new Vector2(width, CeilingWallHeight);
            box.offset = new Vector2((startX + span * 0.5f) * cell, bottom + CeilingWallHeight * 0.5f);
        }

        void ApplyCell(int x, int y, int id)
        {
            var pos = new Vector3Int(x, y, 0);
            RemoveSpecialAt(x, y);

            if (groundTilemap != null)
            {
                groundTilemap.SetTile(pos, null);
            }

            if (id == TileIds.Empty)
            {
                return;
            }

            var def = tileDatabase.Get(id);
            if (def == null)
            {
                return;
            }

            if (UsesTilemap(def.id))
            {
                if (groundTilemap != null)
                {
                    groundTilemap.SetTile(pos, GetOrCreateTile(def));
                }

                SpawnLabelOnly(x, y, def);
                return;
            }

            SpawnSpecial(x, y, def);
        }

        static bool UsesTilemap(int id)
        {
            return id == TileIds.Ground;
        }

        void SpawnLabelOnly(int x, int y, TileDefinition def)
        {
            if (!SpriteFactory.UsesTextLabel(def.id))
            {
                return;
            }

            var label = SpawnLibrary.Spawn(SpawnLibrary.Current.tileLabel, CellCenter(x, y), Quaternion.identity, specialRoot);
            AttachLabel(label, def);
            label.marker.x = x;
            label.marker.y = y;
            label.marker.labelOnly = true;
            _labels.Add(label);
        }

        void SpawnSpecial(int x, int y, TileDefinition def)
        {
            var piece = Instantiate(def.prefab, CellCenter(x, y), Quaternion.identity, specialRoot);
            piece.sprite.sprite = def.sprite;
            TileSprites.FitToCell(piece.transform, piece.sprite.sprite, CellSize);
            FitBox(piece, def);
            piece.marker.x = x;
            piece.marker.y = y;
            AttachComponents(piece, def, x, y);
            if (SpriteFactory.UsesTextLabel(def.id))
            {
                var label = SpawnLibrary.Spawn(SpawnLibrary.Current.tileLabel, piece.transform);
                AttachLabel(label, def);
            }

            _specials.Add(piece);
        }

        void FitBox(TilePiece piece, TileDefinition def)
        {
            if (IsDiagonal(def.id) || def.id == TileIds.SwitchLight)
            {
                return;
            }

            var col = piece.box;
            col.isTrigger = def.isTrigger || def.id == TileIds.Fran;
            bool fullSize = def.id == TileIds.Breakable
                            || def.id == TileIds.BreakableStrong
                            || def.id == TileIds.Bomb
                            || def.id == TileIds.BlastWall
                            || def.id == TileIds.HighBounce
                            || def.id == TileIds.OneWay
                            || def.id == TileIds.Fran
                            || def.id == TileIds.EnemyWeak
                            || def.id == TileIds.EnemyMid
                            || def.id == TileIds.EnemyHeavy
                            || def.id == TileIds.EnemyBoss
                            || def.id == TileIds.Switch
                            || def.id == TileIds.SwitchToggle
                            || def.id == TileIds.Door;
            col.size = fullSize
                ? new Vector2(CellSize, CellSize)
                : new Vector2(CellSize * 0.9f, CellSize * 0.9f);

            if (fullSize && def.id != TileIds.Fran)
            {
                col.sharedMaterial = BlockMaterial();
            }

            if (def.id == TileIds.Meiling)
            {
                col.isTrigger = true;
                col.sharedMaterial = null;
                col.size = new Vector2(CellSize * 0.98f, CellSize * 0.98f);
            }
        }

        void AttachComponents(TilePiece piece, TileDefinition def, int x, int y)
        {
            switch (def.id)
            {
                case TileIds.Goal:
                case TileIds.Hazard:
                    piece.trigger.Bind(_game, def.id == TileIds.Goal ? StageTriggerKind.Goal : StageTriggerKind.Hazard);
                    break;
                case TileIds.Breakable:
                    piece.breakable.Bind(this, x, y, PlayerTuning.BreakableWeakSpeed, 1);
                    piece.bounce.Bind(def.id, TileBounceTable.GetBounciness(def.id));
                    break;
                case TileIds.BreakableStrong:
                    piece.breakable.Bind(this, x, y, PlayerTuning.BreakableStrongSpeed, PlayerTuning.BreakableStrongHits, def.damagedSprite);
                    piece.bounce.Bind(def.id, TileBounceTable.GetBounciness(def.id));
                    break;
                case TileIds.Bomb:
                    piece.breakable.Bind(this, x, y, PlayerTuning.BreakableWeakSpeed, 1, null, IgniteBlast);
                    piece.bounce.Bind(def.id, TileBounceTable.GetBounciness(def.id));
                    break;
                case TileIds.BlastWall:
                case TileIds.HighBounce:
                case TileIds.SlopeNE:
                case TileIds.SlopeNW:
                case TileIds.SlopeSE:
                case TileIds.SlopeSW:
                case TileIds.MirrorNE:
                case TileIds.MirrorNW:
                case TileIds.MirrorSE:
                case TileIds.MirrorSW:
                    piece.bounce.Bind(def.id, TileBounceTable.GetBounciness(def.id));
                    break;
                case TileIds.OneWay:
                    piece.bounce.Bind(def.id, TileBounceTable.GetBounciness(def.id));
                    piece.oneWay.Setup();
                    break;
                case TileIds.Meiling:
                    var setting = _game != null && _game.RuntimeStage != null
                        ? _game.RuntimeStage.FindMeiling(x, y)
                        : null;
                    float angle = setting != null ? setting.angle : 0f;
                    float strength = setting != null ? setting.strength : PlayerTuning.MeilingDefaultSpeed;
                    piece.meiling.Apply(angle, strength);
                    piece.meiling.SetGuideVisible(_showMeilingGuides);
                    _meilings.Add(piece.meiling);
                    break;
                case TileIds.Fran:
                    piece.fran.Bind(this, tileDatabase, x, y);
                    break;
                case TileIds.EnemyWeak:
                    piece.bounce.Bind(def.id, TileBounceTable.GetBounciness(def.id));
                    piece.enemy.Bind(this, x, y, 1, EnemyAttack.Pellet);
                    _enemies.Add(piece.enemy);
                    break;
                case TileIds.EnemyMid:
                    piece.bounce.Bind(def.id, TileBounceTable.GetBounciness(def.id));
                    piece.enemy.Bind(this, x, y, 1, EnemyAttack.ThreeWay);
                    _enemies.Add(piece.enemy);
                    break;
                case TileIds.EnemyHeavy:
                    piece.bounce.Bind(def.id, TileBounceTable.GetBounciness(def.id));
                    piece.enemy.Bind(this, x, y, 1, EnemyAttack.Heavy);
                    _enemies.Add(piece.enemy);
                    break;
                case TileIds.EnemyBoss:
                    piece.bounce.Bind(def.id, TileBounceTable.GetBounciness(def.id));
                    piece.enemy.Bind(this, x, y, 1, EnemyAttack.Laser);
                    _enemies.Add(piece.enemy);
                    break;
                case TileIds.Switch:
                    piece.bounce.Bind(def.id, TileBounceTable.GetBounciness(def.id));
                    piece.plate.Bind(this, x, y, false, def.color);
                    break;
                case TileIds.SwitchToggle:
                    piece.bounce.Bind(def.id, TileBounceTable.GetBounciness(def.id));
                    piece.plate.Bind(this, x, y, true, def.color);
                    break;
                case TileIds.SwitchLight:
                    piece.plate.Bind(this, x, y, false, def.color, true);
                    break;
                case TileIds.Door:
                    piece.bounce.Bind(def.id, TileBounceTable.GetBounciness(def.id));
                    piece.door.Bind(this, x, y);
                    break;
            }

            if (IsDiagonal(def.id))
            {
                piece.slope.Setup(GetSlopeVariant(def.id), CellSize);
            }
        }

        CompanionMeiling FindMeiling(int x, int y)
        {
            for (int i = 0; i < _specials.Count; i++)
            {
                var piece = _specials[i];
                if (piece.marker.x == x && piece.marker.y == y)
                {
                    return piece.meiling;
                }
            }

            return null;
        }

        static void AttachLabel(TileLabelView view, TileDefinition def)
        {
            string label = string.IsNullOrEmpty(def.displayName) ? def.shortLabel : def.displayName;
            view.SetLabel(label);
        }


        static bool IsSlope(int id)
        {
            return id == TileIds.SlopeNE || id == TileIds.SlopeNW
                   || id == TileIds.SlopeSE || id == TileIds.SlopeSW;
        }

        static bool IsMirrorSlope(int id)
        {
            return id == TileIds.MirrorNE || id == TileIds.MirrorNW
                   || id == TileIds.MirrorSE || id == TileIds.MirrorSW;
        }

        static bool IsDiagonal(int id)
        {
            return IsSlope(id) || IsMirrorSlope(id);
        }

        static SlopeVariant GetSlopeVariant(int id)
        {
            switch (id)
            {
                case TileIds.SlopeNE:
                case TileIds.MirrorNE:
                    return SlopeVariant.NE;
                case TileIds.SlopeNW:
                case TileIds.MirrorNW:
                    return SlopeVariant.NW;
                case TileIds.SlopeSE:
                case TileIds.MirrorSE:
                    return SlopeVariant.SE;
                default:
                    return SlopeVariant.SW;
            }
        }

        void RemoveSpecialAt(int x, int y)
        {
            for (int i = _specials.Count - 1; i >= 0; i--)
            {
                var piece = _specials[i];
                if (piece.marker.x == x && piece.marker.y == y)
                {
                    _meilings.Remove(piece.meiling);
                    _enemies.Remove(piece.enemy);
                    Destroy(piece.gameObject);
                    _specials.RemoveAt(i);
                }
            }

            for (int i = _labels.Count - 1; i >= 0; i--)
            {
                var label = _labels[i];
                if (label.marker.x == x && label.marker.y == y)
                {
                    Destroy(label.gameObject);
                    _labels.RemoveAt(i);
                }
            }
        }

        void ClearSpecials()
        {
            _suppressDoors = true;
            for (int i = 0; i < _specials.Count; i++)
            {
                Destroy(_specials[i].gameObject);
            }

            for (int i = 0; i < _labels.Count; i++)
            {
                Destroy(_labels[i].gameObject);
            }

            _specials.Clear();
            _labels.Clear();
            _meilings.Clear();
            _enemies.Clear();
            if (specialRoot != null)
            {
                for (int i = specialRoot.childCount - 1; i >= 0; i--)
                {
                    Destroy(specialRoot.GetChild(i).gameObject);
                }
            }

            _doors.Clear();
            _doorCells.Clear();
            _toggles.Clear();
            _lights.Clear();
            _doorsOpen = false;
            _latchedOpen = false;
            _lightHold = 0;
            _doorEpoch++;
            _suppressDoors = false;
        }

        public int DoorEpoch => _doorEpoch;

        public void RegisterDoor(DoorGate door)
        {
            if (door == null || _doors.Contains(door))
            {
                return;
            }

            _doors.Add(door);
            var cell = new Vector2Int(door.X, door.Y);
            if (!_doorCells.Contains(cell))
            {
                _doorCells.Add(cell);
            }
        }

        public void UnregisterDoor(DoorGate door)
        {
            _doors.Remove(door);
        }

        public void RegisterToggle(SwitchPlate plate)
        {
            if (plate != null && !_toggles.Contains(plate))
            {
                _toggles.Add(plate);
            }
        }

        public void UnregisterToggle(SwitchPlate plate)
        {
            _toggles.Remove(plate);
        }

        public void RegisterLight(SwitchPlate plate)
        {
            if (plate != null && !_lights.Contains(plate))
            {
                _lights.Add(plate);
            }
        }

        public void UnregisterLight(SwitchPlate plate)
        {
            if (plate == null || !_lights.Remove(plate))
            {
                return;
            }

            if (plate.IsLit)
            {
                _lightHold = Mathf.Max(0, _lightHold - 1);
            }

            if (_suppressDoors)
            {
                return;
            }

            ApplyDoors();
        }

        public void NotifyLight(bool lit)
        {
            _lightHold += lit ? 1 : -1;
            if (_lightHold < 0)
            {
                _lightHold = 0;
            }

            if (_suppressDoors)
            {
                return;
            }

            ApplyDoors();
        }

        public void SetDoorsOpen(bool open)
        {
            if (_latchedOpen == open)
            {
                ApplyDoors();
                return;
            }

            _latchedOpen = open;
            ApplyDoors();
        }

        void ApplyDoors()
        {
            bool open = _latchedOpen || _lightHold > 0;
            if (_doorsOpen == open)
            {
                if (open)
                {
                    OpenDoorObjects();
                }

                RefreshToggles();
                return;
            }

            _doorsOpen = open;
            if (open)
            {
                OpenDoorObjects();
            }
            else
            {
                CloseDoorCells();
            }

            RefreshToggles();
        }

        public void ToggleDoors()
        {
            SetDoorsOpen(!_doorsOpen);
        }

        void OpenDoorObjects()
        {
            for (int i = _doors.Count - 1; i >= 0; i--)
            {
                if (_doors[i] == null)
                {
                    _doors.RemoveAt(i);
                    continue;
                }

                _doors[i].Open();
            }
        }

        void CloseDoorCells()
        {
            var spots = _doorCells.ToArray();
            for (int i = 0; i < spots.Length; i++)
            {
                int x = spots[i].x;
                int y = spots[i].y;
                var door = FindDoor(x, y);
                if (door != null)
                {
                    door.PlayReverse();
                    continue;
                }

                if (GetCell(x, y) == TileIds.Empty)
                {
                    SetCell(x, y, TileIds.Door);
                    FindDoor(x, y)?.PlayReverseFromHidden();
                }
            }
        }

        DoorGate FindDoor(int x, int y)
        {
            for (int i = 0; i < _doors.Count; i++)
            {
                var door = _doors[i];
                if (door != null && door.X == x && door.Y == y)
                {
                    return door;
                }
            }

            return null;
        }

        void RefreshToggles()
        {
            for (int i = _toggles.Count - 1; i >= 0; i--)
            {
                if (_toggles[i] == null)
                {
                    _toggles.RemoveAt(i);
                    continue;
                }

                _toggles[i].ApplyDoorState(_doorsOpen);
            }
        }

        PhysicsMaterial2D BlockMaterial()
        {
            if (_blockMaterial == null)
            {
                _blockMaterial = new PhysicsMaterial2D("PullingBreakable")
                {
                    friction = 0f,
                    bounciness = 0f
                };
            }

            return _blockMaterial;
        }

        Tile GetOrCreateTile(TileDefinition def)
        {
            if (_tiles.TryGetValue(def.id, out var tile) && tile != null)
            {
                return tile;
            }

            tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = def.sprite != null ? def.sprite : SpriteFactory.CreateSolid(def.color);
            tile.colliderType = def.hasCollider ? Tile.ColliderType.Sprite : Tile.ColliderType.None;
            tile.color = Color.white;
            _tiles[def.id] = tile;
            return tile;
        }
    }
}

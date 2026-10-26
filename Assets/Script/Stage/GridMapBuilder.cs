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
        readonly List<GameObject> _specials = new List<GameObject>();
        CompositeCollider2D _composite;
        TilemapCollider2D _tilemapCollider;
        PhysicsMaterial2D _blockMaterial;
        GameManager _game;
        SkyLightView _skyLight;

        public int Width { get; private set; }
        public int Height { get; private set; }
        public float CellSize { get; private set; } = 1f;
        public int[] Grid { get; private set; }

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

        public void SetCell(int x, int y, int id)
        {
            if (!InBounds(x, y))
            {
                return;
            }

            Grid[y * Width + x] = id;
            RefreshSkyLight();
            ApplyCell(x, y, id);
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

        public void SetCollidersEnabled(bool enabled)
        {
            if (_tilemapCollider != null)
            {
                _tilemapCollider.enabled = enabled;
                _tilemapCollider.compositeOperation = enabled
                    ? Collider2D.CompositeOperation.Merge
                    : Collider2D.CompositeOperation.None;
            }

            foreach (var special in _specials)
            {
                if (special == null)
                {
                    continue;
                }

                var colliders = special.GetComponents<Collider2D>();
                for (int i = 0; i < colliders.Length; i++)
                {
                    colliders[i].enabled = enabled;
                }
            }
        }

        public void RefreshPhysics()
        {
            if (_composite != null)
            {
                _composite.GenerateGeometry();
            }
        }

        public void Clear()
        {
            if (groundTilemap != null)
            {
                groundTilemap.ClearAllTiles();
            }

            ClearSpecials();
            Grid = null;
            RefreshSkyLight();
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

            var go = new GameObject($"Label_{def.displayName}_{x}_{y}");
            go.transform.SetParent(specialRoot, false);
            go.transform.position = CellCenter(x, y);
            AttachLabel(go, def);
            var marker = go.AddComponent<SpecialTileMarker>();
            marker.x = x;
            marker.y = y;
            marker.labelOnly = true;
            _specials.Add(go);
        }

        void SpawnSpecial(int x, int y, TileDefinition def)
        {
            GameObject go;
            if (def.prefab != null)
            {
                go = Instantiate(def.prefab, CellCenter(x, y), Quaternion.identity, specialRoot);
            }
            else
            {
                go = CreateSpecialObject(x, y, def);
            }

            var marker = go.GetComponent<SpecialTileMarker>();
            if (marker == null)
            {
                marker = go.AddComponent<SpecialTileMarker>();
            }

            marker.x = x;
            marker.y = y;
            AttachComponents(go, def, x, y);
            AttachLabel(go, def);
            _specials.Add(go);
        }

        GameObject CreateSpecialObject(int x, int y, TileDefinition def)
        {
            var go = new GameObject($"Tile_{def.displayName}_{x}_{y}");
            go.transform.SetParent(specialRoot, false);
            go.transform.position = CellCenter(x, y);

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = def.sprite != null ? def.sprite : SpriteFactory.CreateSolid(def.color);
            renderer.color = def.color;
            renderer.sortingOrder = 1;

            if (IsSlope(def.id))
            {
                var slope = go.AddComponent<SlopeCollider>();
                slope.Setup(GetSlopeVariant(def.id), CellSize);
            }
            else
            {
                var col = go.AddComponent<BoxCollider2D>();
                col.isTrigger = def.isTrigger;
                bool fullSize = def.id == TileIds.Breakable
                                || def.id == TileIds.BreakableStrong
                                || def.id == TileIds.HighBounce
                                || def.id == TileIds.Mirror
                                || def.id == TileIds.OneWay
                                || def.id == TileIds.Fran
                                || def.id == TileIds.EnemyWeak
                                || def.id == TileIds.EnemyMid
                                || def.id == TileIds.EnemyBoss;
                col.size = fullSize
                    ? new Vector2(CellSize, CellSize)
                    : new Vector2(CellSize * 0.9f, CellSize * 0.9f);

                if (def.id == TileIds.Breakable || def.id == TileIds.BreakableStrong
                    || def.id == TileIds.HighBounce || def.id == TileIds.Mirror
                    || def.id == TileIds.OneWay || def.id == TileIds.EnemyWeak
                    || def.id == TileIds.EnemyMid || def.id == TileIds.EnemyBoss
                    || def.id == TileIds.Fran)
                {
                    col.sharedMaterial = BlockMaterial();
                }
            }

            return go;
        }

        void AttachComponents(GameObject go, TileDefinition def, int x, int y)
        {
            switch (def.id)
            {
                case TileIds.Goal:
                case TileIds.Hazard:
                    {
                        var trigger = EnsureComponent<StageTrigger>(go);
                        trigger.Bind(_game, def.id == TileIds.Goal ? StageTriggerKind.Goal : StageTriggerKind.Hazard);
                        break;
                    }
                case TileIds.Breakable:
                    {
                        var block = EnsureComponent<BreakableBlock>(go);
                        block.Bind(this, x, y, PlayerTuning.BreakableWeakSpeed, 1);
                        AttachBounce(go, def.id);
                        break;
                    }
                case TileIds.BreakableStrong:
                    {
                        var block = EnsureComponent<BreakableBlock>(go);
                        block.Bind(this, x, y, PlayerTuning.BreakableStrongSpeed, PlayerTuning.BreakableStrongHits);
                        AttachBounce(go, def.id);
                        break;
                    }
                case TileIds.HighBounce:
                case TileIds.Mirror:
                case TileIds.OneWay:
                case TileIds.EnemyWeak:
                case TileIds.EnemyMid:
                case TileIds.EnemyBoss:
                case TileIds.SlopeNE:
                case TileIds.SlopeNW:
                case TileIds.SlopeSE:
                case TileIds.SlopeSW:
                    AttachBounce(go, def.id);
                    break;
            }

            switch (def.id)
            {
                case TileIds.OneWay:
                {
                    var platform = EnsureComponent<OneWayPlatform>(go);
                    platform.Setup(go.GetComponent<BoxCollider2D>());
                    break;
                }
                case TileIds.Meiling:
                    EnsureComponent<CompanionMeiling>(go);
                    break;
                case TileIds.Patchouli:
                    EnsureComponent<CompanionPatchouli>(go);
                    break;
                case TileIds.Sakuya:
                    EnsureComponent<CompanionSakuya>(go);
                    break;
                case TileIds.Fran:
                    {
                        var fran = EnsureComponent<FranNpc>(go);
                        fran.Bind(this, tileDatabase, x, y);
                        break;
                    }
                case TileIds.EnemyWeak:
                    {
                        var enemy = EnsureComponent<EnemyBlock>(go);
                        enemy.Bind(this, x, y, 1);
                        break;
                    }
                case TileIds.EnemyMid:
                    {
                        var enemy = EnsureComponent<EnemyBlock>(go);
                        enemy.Bind(this, x, y, 2);
                        break;
                    }
                case TileIds.EnemyBoss:
                    {
                        var enemy = EnsureComponent<EnemyBlock>(go);
                        enemy.Bind(this, x, y, 5);
                        break;
                    }
                case TileIds.Bullet:
                    EnsureComponent<BulletBlock>(go);
                    break;
                case TileIds.Mirror:
                    EnsureComponent<MirrorBlock>(go);
                    break;
            }

            if (IsSlope(def.id))
            {
                var slope = go.GetComponent<SlopeCollider>();
                if (slope != null)
                {
                    slope.Setup(GetSlopeVariant(def.id), CellSize);
                }
            }
        }

        static T EnsureComponent<T>(GameObject go) where T : Component
        {
            var component = go.GetComponent<T>();
            if (component == null)
            {
                component = go.AddComponent<T>();
            }

            return component;
        }

        void AttachBounce(GameObject go, int tileId)
        {
            var surface = EnsureComponent<BounceSurface>(go);
            surface.Bind(tileId, TileBounceTable.GetBounciness(tileId));
        }

        static void AttachLabel(GameObject go, TileDefinition def)
        {
            if (!SpriteFactory.UsesTextLabel(def.id))
            {
                return;
            }

            string label = string.IsNullOrEmpty(def.shortLabel) ? def.displayName : def.shortLabel;
            TileLabelView.Attach(go.transform, label, Color.white);
        }

        static bool IsSlope(int id)
        {
            return id == TileIds.SlopeNE || id == TileIds.SlopeNW
                   || id == TileIds.SlopeSE || id == TileIds.SlopeSW;
        }

        static SlopeVariant GetSlopeVariant(int id)
        {
            switch (id)
            {
                case TileIds.SlopeNE: return SlopeVariant.NE;
                case TileIds.SlopeNW: return SlopeVariant.NW;
                case TileIds.SlopeSE: return SlopeVariant.SE;
                default: return SlopeVariant.SW;
            }
        }

        void RemoveSpecialAt(int x, int y)
        {
            for (int i = _specials.Count - 1; i >= 0; i--)
            {
                var go = _specials[i];
                if (go == null)
                {
                    _specials.RemoveAt(i);
                    continue;
                }

                var marker = go.GetComponent<SpecialTileMarker>();
                if (marker != null && marker.x == x && marker.y == y)
                {
                    Destroy(go);
                    _specials.RemoveAt(i);
                }
            }
        }

        void ClearSpecials()
        {
            for (int i = 0; i < _specials.Count; i++)
            {
                if (_specials[i] != null)
                {
                    Destroy(_specials[i]);
                }
            }

            _specials.Clear();
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

    public class SpecialTileMarker : MonoBehaviour
    {
        public int x;
        public int y;
        public bool labelOnly;
    }
}

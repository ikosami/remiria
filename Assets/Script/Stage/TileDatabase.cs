using System.Collections.Generic;
using UnityEngine;

namespace Pulling
{
    [CreateAssetMenu(fileName = "TileDatabase", menuName = "Pulling/Tile Database")]
    public class TileDatabase : ScriptableObject
    {
        public List<TileDefinition> tiles = new List<TileDefinition>();

        Dictionary<int, TileDefinition> _lookup;

        /// <summary>エディタパレットの表示順（同系統を隣接させる）</summary>
        static readonly int[] PaletteOrder =
        {
            TileIds.Empty,
            TileIds.Ground,
            TileIds.HighBounce,
            TileIds.OneWay,
            TileIds.SlopeNE,
            TileIds.SlopeNW,
            TileIds.SlopeSE,
            TileIds.SlopeSW,
            TileIds.MirrorNE,
            TileIds.MirrorNW,
            TileIds.MirrorSE,
            TileIds.MirrorSW,
            TileIds.Breakable,
            TileIds.BreakableStrong,
            TileIds.Bomb,
            TileIds.BlastWall,
            TileIds.Switch,
            TileIds.SwitchToggle,
            TileIds.SwitchLight,
            TileIds.Door,
            TileIds.Goal,
            TileIds.Hazard,
            TileIds.Sunlight,
            TileIds.Fran,
            TileIds.Meiling,
            TileIds.Patchouli,
            TileIds.Sakuya,
            TileIds.EnemyWeak,
            TileIds.EnemyMid,
            TileIds.EnemyHeavy,
            TileIds.EnemyBoss,
            TileIds.Bullet,
        };

        public void EnsureDefaults()
        {
            if (tiles == null)
            {
                tiles = new List<TileDefinition>();
            }

            EnsureTile(TileIds.Empty, "消しゴム", "消", new Color(0.85f, 0.85f, 0.9f, 0.35f), false, false);
            EnsureTile(TileIds.Ground, "低反発", "低", new Color(0.45f, 0.32f, 0.2f), true, false);
            EnsureTile(TileIds.HighBounce, "高反発", "高", new Color(0.55f, 0.25f, 0.65f), false, false);
            EnsureTile(TileIds.OneWay, "すり抜け", "足", new Color(0.5f, 0.7f, 0.9f), false, false);
            EnsureTile(TileIds.SlopeNE, "斜め↗", "", new Color(0.6f, 0.55f, 0.45f), false, false);
            EnsureTile(TileIds.SlopeNW, "斜め↖", "", new Color(0.6f, 0.55f, 0.45f), false, false);
            EnsureTile(TileIds.SlopeSE, "斜め↘", "", new Color(0.6f, 0.55f, 0.45f), false, false);
            EnsureTile(TileIds.SlopeSW, "斜め↙", "", new Color(0.6f, 0.55f, 0.45f), false, false);
            EnsureTile(TileIds.MirrorNE, "鏡↗", "", new Color(0.72f, 0.86f, 0.96f), false, false);
            EnsureTile(TileIds.MirrorNW, "鏡↖", "", new Color(0.72f, 0.86f, 0.96f), false, false);
            EnsureTile(TileIds.MirrorSE, "鏡↘", "", new Color(0.72f, 0.86f, 0.96f), false, false);
            EnsureTile(TileIds.MirrorSW, "鏡↙", "", new Color(0.72f, 0.86f, 0.96f), false, false);
            EnsureTile(TileIds.Breakable, "壊れ弱", "弱", new Color(0.82f, 0.62f, 0.38f), false, false);
            EnsureTile(TileIds.BreakableStrong, "壊れ強", "強", new Color(0.65f, 0.45f, 0.28f), false, false);
            EnsureTile(TileIds.Bomb, "爆弾", "爆", new Color(0.72f, 0.16f, 0.14f), false, false);
            EnsureTile(TileIds.BlastWall, "爆壁", "壁", new Color(0.62f, 0.62f, 0.66f), false, false);
            EnsureTile(TileIds.Switch, "一発", "一", new Color(0.82f, 0.18f, 0.24f), false, false);
            EnsureTile(TileIds.SwitchToggle, "オンオフ", "オ", new Color(0.16f, 0.62f, 0.48f), false, false);
            EnsureTile(TileIds.SwitchLight, "感光", "感", new Color(0.95f, 0.68f, 0.16f), false, false);
            EnsureTile(TileIds.Door, "扉", "扉", new Color(0.32f, 0.4f, 0.58f), false, false);
            EnsureTile(TileIds.Goal, "ゴール", "旗", new Color(0.95f, 0.8f, 0.2f), false, true);
            EnsureTile(TileIds.Hazard, "危険", "穴", new Color(0.85f, 0.2f, 0.2f), false, true);
            EnsureTile(TileIds.Sunlight, "太陽光", "光", new Color(1f, 0.95f, 0.4f, 0.7f), false, true);
            EnsureTile(TileIds.Fran, "フラン", "弗", new Color(0.95f, 0.55f, 0.7f), false, true);
            EnsureTile(TileIds.Meiling, "紅美鈴", "鈴", new Color(0.9f, 0.25f, 0.25f), false, true);
            EnsureTile(TileIds.Patchouli, "パチュリー", "帕", new Color(0.55f, 0.4f, 0.75f), false, true);
            EnsureTile(TileIds.Sakuya, "咲夜", "咲", new Color(0.75f, 0.75f, 0.85f), false, true);
            EnsureTile(TileIds.EnemyWeak, "玉", "玉", new Color(0.6f, 0.85f, 0.5f), false, false);
            EnsureTile(TileIds.EnemyMid, "３方向", "３", new Color(0.4f, 0.75f, 0.95f), false, false);
            EnsureTile(TileIds.EnemyHeavy, "大玉", "大", new Color(0.95f, 0.55f, 0.15f), false, false);
            EnsureTile(TileIds.EnemyBoss, "レーザー", "光", new Color(0.85f, 0.35f, 0.55f), false, false);
            EnsureTile(TileIds.Bullet, "弾", "弾", new Color(0.95f, 0.5f, 0.2f), false, true);

            foreach (var tile in tiles)
            {
                if (tile == null)
                {
                    continue;
                }

                if (tile.sprite == null)
                {
                    tile.sprite = TileSprites.Load(tile.id) ?? SpriteFactory.CreateForTile(tile.id, tile.color);
                }

                if (tile.id == TileIds.BreakableStrong && tile.damagedSprite == null)
                {
                    tile.damagedSprite = TileSprites.LoadDamaged() ?? SpriteFactory.CreateFractured(tile.color);
                }
            }

            ReorderTilesForPalette();
            RebuildLookup();
        }

        public TileDefinition Get(int id)
        {
            if (_lookup == null)
            {
                RebuildLookup();
            }

            return _lookup.TryGetValue(id, out var def) ? def : null;
        }

        public IEnumerable<TileDefinition> EnumeratePalette()
        {
            EnsureDefaults();

            var yielded = new HashSet<int>();
            for (int i = 0; i < PaletteOrder.Length; i++)
            {
                var def = Get(PaletteOrder[i]);
                if (def == null || def.id == TileIds.Empty)
                {
                    continue;
                }

                yielded.Add(def.id);
                yield return def;
            }

            for (int i = 0; i < tiles.Count; i++)
            {
                var tile = tiles[i];
                if (tile != null && tile.id != TileIds.Empty && !yielded.Contains(tile.id))
                {
                    yield return tile;
                }
            }
        }

        void EnsureTile(int id, string name, string shortLabel, Color color, bool hasCollider, bool isTrigger)
        {
            for (int i = 0; i < tiles.Count; i++)
            {
                if (tiles[i] != null && tiles[i].id == id)
                {
                    if (id == TileIds.Meiling)
                    {
                        tiles[i].isTrigger = true;
                    }

                    if (id == TileIds.EnemyWeak || id == TileIds.EnemyMid || id == TileIds.EnemyHeavy
                        || id == TileIds.EnemyBoss
                        || string.IsNullOrEmpty(tiles[i].displayName))
                    {
                        tiles[i].displayName = name;
                    }

                    if (id == TileIds.EnemyWeak || id == TileIds.EnemyMid || id == TileIds.EnemyHeavy
                        || id == TileIds.EnemyBoss
                        || string.IsNullOrEmpty(tiles[i].shortLabel))
                    {
                        tiles[i].shortLabel = shortLabel;
                    }

                    if (id == TileIds.Fran)
                    {
                        tiles[i].isTrigger = true;
                    }

                    if (id == TileIds.Bomb || id == TileIds.BlastWall)
                    {
                        bool renamed = tiles[i].displayName != name || tiles[i].shortLabel != shortLabel;
                        tiles[i].displayName = name;
                        tiles[i].shortLabel = shortLabel;
                        if (renamed)
                        {
                            tiles[i].sprite = null;
                        }
                    }

                    if (id == TileIds.Switch || id == TileIds.SwitchToggle || id == TileIds.SwitchLight)
                    {
                        bool changed = tiles[i].displayName != name
                                       || tiles[i].shortLabel != shortLabel
                                       || tiles[i].color != color;
                        tiles[i].displayName = name;
                        tiles[i].shortLabel = shortLabel;
                        tiles[i].color = color;
                        if (changed || id == TileIds.SwitchToggle || id == TileIds.SwitchLight)
                        {
                            tiles[i].sprite = null;
                        }
                    }

                    return;
                }
            }

            tiles.Add(new TileDefinition
            {
                id = id,
                displayName = name,
                shortLabel = shortLabel,
                color = color,
                hasCollider = hasCollider,
                isTrigger = isTrigger
            });
        }

        void ReorderTilesForPalette()
        {
            var ordered = new List<TileDefinition>(tiles.Count);
            var used = new HashSet<int>();

            for (int i = 0; i < PaletteOrder.Length; i++)
            {
                var def = GetByIdUnsorted(PaletteOrder[i]);
                if (def != null)
                {
                    ordered.Add(def);
                    used.Add(def.id);
                }
            }

            for (int i = 0; i < tiles.Count; i++)
            {
                var tile = tiles[i];
                if (tile != null && !used.Contains(tile.id))
                {
                    ordered.Add(tile);
                }
            }

            tiles = ordered;
        }

        TileDefinition GetByIdUnsorted(int id)
        {
            for (int i = 0; i < tiles.Count; i++)
            {
                if (tiles[i] != null && tiles[i].id == id)
                {
                    return tiles[i];
                }
            }

            return null;
        }

        void RebuildLookup()
        {
            _lookup = new Dictionary<int, TileDefinition>();
            if (tiles == null)
            {
                return;
            }

            foreach (var tile in tiles)
            {
                if (tile != null)
                {
                    _lookup[tile.id] = tile;
                }
            }
        }
    }
}

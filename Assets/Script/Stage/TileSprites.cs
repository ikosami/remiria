using UnityEngine;

namespace Pulling
{
    public static class TileSprites
    {
        const string Folder = "Assets/Image/tiles/";

        public static Sprite Load(int tileId)
        {
            string file = FileName(tileId);
            return file == null ? null : LoadFile(file);
        }

        public static Sprite LoadDamaged()
        {
            return LoadFile("壊れ強ひび.png");
        }

        public static void FitToCell(Transform target, Sprite sprite, float cellSize)
        {
            if (target == null)
            {
                return;
            }

            if (sprite == null || cellSize <= 0f)
            {
                target.localScale = Vector3.one;
                return;
            }

            Vector2 size = sprite.bounds.size;
            float width = size.x > 0.0001f ? size.x : 1f;
            float height = size.y > 0.0001f ? size.y : 1f;
            target.localScale = new Vector3(cellSize / width, cellSize / height, 1f);
        }

        static string FileName(int tileId)
        {
            switch (tileId)
            {
                case TileIds.Ground: return "低反発.png";
                case TileIds.HighBounce: return "高反発.png";
                case TileIds.OneWay: return "すり抜け.png";
                case TileIds.SlopeNE: return "斜め右上.png";
                case TileIds.SlopeNW: return "斜め左上.png";
                case TileIds.SlopeSE: return "斜め右下.png";
                case TileIds.SlopeSW: return "斜め左下.png";
                case TileIds.Breakable: return "壊れ弱.png";
                case TileIds.BreakableStrong: return "壊れ強.png";
                case TileIds.Switch: return "一発.png";
                case TileIds.SwitchToggle: return "オンオフ.png";
                case TileIds.Door: return "扉.png";
                case TileIds.Goal: return "ゴール.png";
                case TileIds.Hazard: return "危険.png";
                case TileIds.Sunlight: return "太陽光.png";
                case TileIds.Fran: return "フラン.png";
                case TileIds.Meiling: return "紅美鈴.png";
                case TileIds.Patchouli: return "パチュリー.png";
                case TileIds.Sakuya: return "咲夜.png";
                case TileIds.EnemyWeak: return "敵弱.png";
                case TileIds.EnemyMid: return "敵中.png";
                case TileIds.EnemyBoss: return "敵BOSS.png";
                case TileIds.Bullet: return "弾.png";
                default: return null;
            }
        }

        static Sprite LoadFile(string file)
        {
#if UNITY_EDITOR
            var assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(Folder + file);
            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is Sprite sprite)
                {
                    return sprite;
                }
            }
#endif
            return null;
        }
    }
}

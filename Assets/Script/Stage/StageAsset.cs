using UnityEngine;

namespace Pulling
{
    [System.Serializable]
    public class MeilingSetting
    {
        public int x;
        public int y;
        public float angle;
        public float strength;
    }

    [CreateAssetMenu(fileName = "Stage", menuName = "Pulling/Stage")]
    public class StageAsset : ScriptableObject
    {
        public int version = 1;
        public string id = "Stage";
        public int width = 40;
        public int height = 12;
        public float cellSize = 1f;
        public int cameraVisibleRows = 12;
        public int spawnX = 2;
        public int spawnY = 1;
        public string[] rows;
        public MeilingSetting[] meilings;

        public void CopyFrom(StageAsset other)
        {
            version = other.version;
            id = other.id;
            width = other.width;
            height = other.height;
            cellSize = other.cellSize;
            cameraVisibleRows = other.cameraVisibleRows;
            spawnX = other.spawnX;
            spawnY = other.spawnY;
            rows = other.rows != null ? (string[])other.rows.Clone() : null;
            meilings = CloneMeilings(other.meilings);
        }

        public MeilingSetting FindMeiling(int x, int y)
        {
            if (meilings == null)
            {
                return null;
            }

            for (int i = 0; i < meilings.Length; i++)
            {
                var setting = meilings[i];
                if (setting != null && setting.x == x && setting.y == y)
                {
                    return setting;
                }
            }

            return null;
        }

        public void EnsureMeiling(int x, int y)
        {
            if (FindMeiling(x, y) != null)
            {
                return;
            }

            SetMeiling(x, y, 0f, PlayerTuning.MeilingDefaultSpeed);
        }

        public void SetMeiling(int x, int y, float angle, float strength)
        {
            angle = Mathf.Repeat(angle, 360f);
            strength = Mathf.Clamp(strength, PlayerTuning.MeilingMinSpeed, PlayerTuning.MeilingMaxSpeed);
            var existing = FindMeiling(x, y);
            if (existing != null)
            {
                existing.angle = angle;
                existing.strength = strength;
                return;
            }

            var setting = new MeilingSetting
            {
                x = x,
                y = y,
                angle = angle,
                strength = strength
            };
            if (meilings == null || meilings.Length == 0)
            {
                meilings = new[] { setting };
                return;
            }

            var next = new MeilingSetting[meilings.Length + 1];
            for (int i = 0; i < meilings.Length; i++)
            {
                next[i] = meilings[i];
            }

            next[meilings.Length] = setting;
            meilings = next;
        }

        public void RemoveMeiling(int x, int y)
        {
            if (meilings == null || FindMeiling(x, y) == null)
            {
                return;
            }

            int count = 0;
            for (int i = 0; i < meilings.Length; i++)
            {
                var setting = meilings[i];
                if (setting != null && (setting.x != x || setting.y != y))
                {
                    count++;
                }
            }

            if (count == 0)
            {
                meilings = null;
                return;
            }

            var next = new MeilingSetting[count];
            int write = 0;
            for (int i = 0; i < meilings.Length; i++)
            {
                var setting = meilings[i];
                if (setting != null && (setting.x != x || setting.y != y))
                {
                    next[write++] = setting;
                }
            }

            meilings = next;
        }

        public void PruneMeilings(int width, int height, System.Func<int, int, bool> keep)
        {
            if (meilings == null || meilings.Length == 0)
            {
                return;
            }

            int count = 0;
            for (int i = 0; i < meilings.Length; i++)
            {
                if (KeepsMeiling(meilings[i], width, height, keep))
                {
                    count++;
                }
            }

            if (count == meilings.Length)
            {
                return;
            }

            if (count == 0)
            {
                meilings = null;
                return;
            }

            var next = new MeilingSetting[count];
            int write = 0;
            for (int i = 0; i < meilings.Length; i++)
            {
                if (KeepsMeiling(meilings[i], width, height, keep))
                {
                    next[write++] = meilings[i];
                }
            }

            meilings = next;
        }

        static bool KeepsMeiling(MeilingSetting setting, int width, int height, System.Func<int, int, bool> keep)
        {
            if (setting == null || setting.x < 0 || setting.y < 0 || setting.x >= width || setting.y >= height)
            {
                return false;
            }

            return keep == null || keep(setting.x, setting.y);
        }

        public static MeilingSetting[] CloneMeilings(MeilingSetting[] source)
        {
            if (source == null)
            {
                return null;
            }

            var copy = new MeilingSetting[source.Length];
            for (int i = 0; i < source.Length; i++)
            {
                var setting = source[i];
                if (setting == null)
                {
                    continue;
                }

                copy[i] = new MeilingSetting
                {
                    x = setting.x,
                    y = setting.y,
                    angle = setting.angle,
                    strength = setting.strength
                };
            }

            return copy;
        }

        public StageAsset CreateRuntimeClone()
        {
            var clone = CreateInstance<StageAsset>();
            clone.name = id + " (runtime)";
            clone.CopyFrom(this);
            return clone;
        }
    }
}

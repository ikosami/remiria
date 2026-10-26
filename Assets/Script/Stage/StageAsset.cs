using UnityEngine;

namespace Pulling
{
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

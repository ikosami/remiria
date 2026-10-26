using UnityEngine;

namespace Pulling
{
    [System.Serializable]
    public class TileDefinition
    {
        public int id;
        public string displayName;
        public string shortLabel;
        public Color color = Color.white;
        public bool hasCollider;
        public bool isTrigger;
        public Sprite sprite;
        public GameObject prefab;
    }
}

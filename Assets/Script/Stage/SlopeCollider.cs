using UnityEngine;

namespace Pulling
{
    public enum SlopeVariant
    {
        NE,
        NW,
        SE,
        SW
    }

    public class SlopeCollider : MonoBehaviour
    {
        public SlopeVariant variant;
        public PolygonCollider2D shape;
        float _cellSize = 1f;

        public void Setup(SlopeVariant slopeVariant, float cellSize)
        {
            variant = slopeVariant;
            _cellSize = cellSize > 0f ? cellSize : 1f;
            BuildCollider();
        }

        void BuildCollider()
        {
            shape.pathCount = 1;
            shape.SetPath(0, Triangle(_cellSize * 0.5f));
        }

        Vector2[] Triangle(float h)
        {
            switch (variant)
            {
                case SlopeVariant.NE:
                    return new[] { new Vector2(-h, -h), new Vector2(h, -h), new Vector2(h, h) };
                case SlopeVariant.NW:
                    return new[] { new Vector2(-h, h), new Vector2(-h, -h), new Vector2(h, -h) };
                case SlopeVariant.SE:
                    return new[] { new Vector2(h, -h), new Vector2(h, h), new Vector2(-h, h) };
                default:
                    return new[] { new Vector2(-h, h), new Vector2(-h, -h), new Vector2(h, h) };
            }
        }
    }
}

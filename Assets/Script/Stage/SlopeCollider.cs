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
        float _cellSize = 1f;

        public void Setup(SlopeVariant slopeVariant, float cellSize)
        {
            variant = slopeVariant;
            _cellSize = cellSize > 0f ? cellSize : 1f;
            BuildCollider();
        }

        void BuildCollider()
        {
            var box = GetComponent<BoxCollider2D>();
            if (box != null)
            {
                Destroy(box);
            }

            var edge = GetComponent<EdgeCollider2D>();
            if (edge != null)
            {
                Destroy(edge);
            }

            var polygon = GetComponent<PolygonCollider2D>();
            if (polygon == null)
            {
                polygon = gameObject.AddComponent<PolygonCollider2D>();
            }

            polygon.pathCount = 1;
            polygon.SetPath(0, Triangle(_cellSize * 0.5f));
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

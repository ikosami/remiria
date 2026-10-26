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
            if (edge == null)
            {
                edge = gameObject.AddComponent<EdgeCollider2D>();
            }

            float h = _cellSize * 0.5f;
            switch (variant)
            {
                case SlopeVariant.NE:
                    edge.points = new[] { new Vector2(-h, -h), new Vector2(h, h) };
                    break;
                case SlopeVariant.NW:
                    edge.points = new[] { new Vector2(-h, h), new Vector2(h, -h) };
                    break;
                case SlopeVariant.SE:
                    edge.points = new[] { new Vector2(-h, h), new Vector2(h, -h) };
                    break;
                case SlopeVariant.SW:
                    edge.points = new[] { new Vector2(-h, -h), new Vector2(h, h) };
                    break;
            }
        }
    }
}


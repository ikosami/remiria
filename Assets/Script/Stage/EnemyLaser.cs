using UnityEngine;

namespace Pulling
{
    public class EnemyLaser : MonoBehaviour
    {
        SpriteRenderer _renderer;
        Collider2D _ignore;
        float _cell = 1f;
        float _reach = 24f;
        float _halfWidth = 0.12f;
        Vector2 _direction = Vector2.left;
        bool _pushing;

        public void Bind(Collider2D ignore, float cellSize, float reach)
        {
            _ignore = ignore;
            _cell = cellSize > 0f ? cellSize : 1f;
            _reach = Mathf.Max(_cell, reach);
            _halfWidth = _cell * 0.02f;
            EnsureVisual();
            Hide();
        }

        public void Begin(Vector2 direction)
        {
            _direction = direction.sqrMagnitude < 0.0001f ? Vector2.left : direction.normalized;
            _pushing = false;
        }

        public void Preview(Vector2 origin, Vector2 direction)
        {
            _direction = direction.sqrMagnitude < 0.0001f ? Vector2.left : direction.normalized;
            Draw(origin, _cell * 0.028f, new Color(1f, 0.75f, 0.85f, 0.55f), out _, out _);
        }

        public void Show(Vector2 origin, PlayerController player)
        {
            if (!Draw(origin, _cell * 0.045f, new Color(1f, 0.35f, 0.65f, 0.95f), out Vector2 start, out float length))
            {
                _pushing = false;
                return;
            }

            if (player != null && Overlaps(start, length, player))
            {
                if (!_pushing)
                {
                    _pushing = true;
                    player.Nudge(_direction * PlayerTuning.EnemyLaserPush);
                }
            }
            else
            {
                _pushing = false;
            }
        }

        bool Draw(Vector2 origin, float width, Color color, out Vector2 start, out float length)
        {
            EnsureVisual();
            start = origin + _direction * (_cell * 0.52f);
            length = Trace(start, _direction);
            if (length < 0.05f)
            {
                Hide();
                return false;
            }

            _halfWidth = width * 0.5f;
            _renderer.enabled = true;
            _renderer.color = color;
            transform.position = start + _direction * (length * 0.5f);
            transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg);
            transform.localScale = new Vector3(length, width, 1f);
            return true;
        }

        public void Hide()
        {
            _pushing = false;
            if (_renderer != null)
            {
                _renderer.enabled = false;
            }
        }

        void EnsureVisual()
        {
            if (_renderer != null)
            {
                return;
            }

            _renderer = gameObject.AddComponent<SpriteRenderer>();
            _renderer.sprite = SpriteFactory.CreateSolid(Color.white, 8);
            _renderer.color = new Color(1f, 0.75f, 0.85f, 0.55f);
            _renderer.sortingOrder = 4;
            SpriteFactory.AssignPipelineMaterial(_renderer);
        }

        float Trace(Vector2 start, Vector2 direction)
        {
            var hits = Physics2D.RaycastAll(start, direction, _reach);
            float length = _reach;
            for (int i = 0; i < hits.Length; i++)
            {
                var col = hits[i].collider;
                if (col == null || col.isTrigger || col == _ignore || col.GetComponent<PlayerController>() != null)
                {
                    continue;
                }

                if (hits[i].distance < length)
                {
                    length = hits[i].distance;
                }
            }

            return length;
        }

        bool Overlaps(Vector2 start, float length, PlayerController player)
        {
            Vector2 toPlayer = (Vector2)player.transform.position - start;
            float along = Vector2.Dot(toPlayer, _direction);
            if (along < -0.05f || along > length + 0.05f)
            {
                return false;
            }

            float side = (toPlayer - _direction * along).magnitude;
            float radius = 0.36f;
            var circle = player.GetComponent<CircleCollider2D>();
            if (circle != null)
            {
                float scale = Mathf.Max(Mathf.Abs(player.transform.lossyScale.x), Mathf.Abs(player.transform.lossyScale.y));
                radius = circle.radius * scale;
            }

            return side <= radius + _halfWidth;
        }
    }
}

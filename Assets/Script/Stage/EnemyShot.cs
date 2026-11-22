using UnityEngine;

namespace Pulling
{
    public class EnemyShot : MonoBehaviour
    {
        Vector2 _velocity;
        float _radius;
        float _life;
        float _push;
        Collider2D _ignore;
        bool _spent;

        public void Launch(Vector2 origin, Vector2 direction, Sprite sprite, float cellSize, Collider2D ignore, float sizeScale = 1f, float push = PlayerTuning.EnemyShotPush)
        {
            direction = direction.sqrMagnitude < 0.0001f ? Vector2.left : direction.normalized;
            _ignore = ignore;
            sizeScale = Mathf.Max(0.25f, sizeScale);
            _push = push;
            _radius = Mathf.Max(0.08f, cellSize * 0.16f * sizeScale);
            _life = PlayerTuning.EnemyShotLife;
            _velocity = direction * PlayerTuning.EnemyShotSpeed;

            float scale = Mathf.Max(0.2f, cellSize * 0.34f * sizeScale);
            transform.position = origin + direction * (cellSize * 0.5f + _radius + 0.04f);
            transform.localScale = new Vector3(scale, scale, 1f);

            var renderer = gameObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite != null
                ? sprite
                : SpriteFactory.CreateCircle(new Color(0.95f, 0.5f, 0.2f));
            renderer.sortingOrder = 4;

            if (TryHitOverlap(transform.position))
            {
                return;
            }
        }

        void FixedUpdate()
        {
            if (_spent)
            {
                return;
            }

            float dt = Time.fixedDeltaTime;
            _life -= dt;
            if (_life <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            Vector2 pos = transform.position;
            float dist = _velocity.magnitude * dt;
            if (dist > 0.0001f)
            {
                var hits = Physics2D.CircleCastAll(pos, _radius, _velocity / _velocity.magnitude, dist);
                for (int i = 0; i < hits.Length; i++)
                {
                    if (Resolve(hits[i].collider))
                    {
                        return;
                    }
                }
            }

            transform.position = pos + _velocity * dt;
        }

        bool TryHitOverlap(Vector2 pos)
        {
            var hits = Physics2D.OverlapCircleAll(pos, _radius);
            for (int i = 0; i < hits.Length; i++)
            {
                if (Resolve(hits[i]))
                {
                    return true;
                }
            }

            return false;
        }

        bool Resolve(Collider2D col)
        {
            if (col == null || col.isTrigger || col == _ignore)
            {
                return false;
            }

            var player = col.GetComponent<PlayerController>();
            if (player != null)
            {
                _spent = true;
                Vector2 push = _velocity.sqrMagnitude > 0.0001f ? _velocity.normalized : Vector2.right;
                player.Nudge(push * _push);
                Destroy(gameObject);
                return true;
            }

            _spent = true;
            Destroy(gameObject);
            return true;
        }
    }
}

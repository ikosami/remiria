using UnityEngine;

namespace Pulling
{
    public class EnemyBlock : MonoBehaviour
    {
        GridMapBuilder _builder;
        int _x;
        int _y;
        int _durability = 1;

        public void Bind(GridMapBuilder builder, int x, int y, int durability)
        {
            _builder = builder;
            _x = x;
            _y = y;
            _durability = Mathf.Max(1, durability);
        }

        void OnCollisionEnter2D(Collision2D collision)
        {
            var player = FindPlayer(collision);
            if (player == null || player.IsSettling || player.State != PlayerMoveState.Flying)
            {
                return;
            }

            float impact = ClosingSpeed(player.PrePhysicsVelocity, collision);
            if (impact < PlayerTuning.EnemyHitSpeed)
            {
                return;
            }

            _durability--;
            if (_durability > 0)
            {
                return;
            }

            if (_builder != null)
            {
                _builder.SetCell(_x, _y, TileIds.Empty);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        static PlayerController FindPlayer(Collision2D collision)
        {
            if (collision.collider != null)
            {
                var player = collision.collider.GetComponent<PlayerController>();
                if (player != null)
                {
                    return player;
                }
            }

            if (collision.otherCollider != null)
            {
                return collision.otherCollider.GetComponent<PlayerController>();
            }

            return null;
        }

        static float ClosingSpeed(Vector2 incoming, Collision2D collision)
        {
            float best = 0f;
            int count = collision.contactCount;
            for (int i = 0; i < count; i++)
            {
                Vector2 normal = collision.GetContact(i).normal;
                best = Mathf.Max(best, Mathf.Abs(Vector2.Dot(incoming, normal)));
            }

            return count == 0 ? incoming.magnitude : best;
        }
    }
}


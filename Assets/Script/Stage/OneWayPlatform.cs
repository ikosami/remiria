using UnityEngine;

namespace Pulling
{
    public class OneWayPlatform : MonoBehaviour
    {
        /// <summary>真上を中心にした受け止め角度。180度だと真横の法線も床になる。</summary>
        public const float SurfaceArc = 120f;
        const float Thickness = 0.22f;
        const float SurfaceSlack = 0.06f;

        BoxCollider2D _collider;
        Collider2D _playerCollider;
        Rigidbody2D _playerBody;
        bool _ignoringPlayer;
        bool _supported;

        public void Setup(BoxCollider2D col)
        {
            if (col == null)
            {
                return;
            }

            float width = col.size.x;
            float top = col.offset.y + col.size.y * 0.5f;
            float thickness = Mathf.Min(Thickness, col.size.y);
            col.size = new Vector2(width, thickness);
            col.offset = new Vector2(col.offset.x, top - thickness * 0.5f);
            col.usedByEffector = true;

            var effector = GetComponent<PlatformEffector2D>();
            if (effector == null)
            {
                effector = gameObject.AddComponent<PlatformEffector2D>();
            }

            effector.useOneWay = true;
            effector.useOneWayGrouping = false;
            effector.useSideFriction = false;
            effector.useSideBounce = false;
            effector.surfaceArc = SurfaceArc;
            effector.rotationalOffset = 0f;

            _collider = col;
            _supported = false;
            _ignoringPlayer = false;
            _playerCollider = null;
            _playerBody = null;
            UpdatePassThrough();
        }

        void FixedUpdate()
        {
            UpdatePassThrough();
        }

        void OnDisable()
        {
            SetPlayerCollisionIgnored(false);
        }

        void UpdatePassThrough()
        {
            if (_collider == null)
            {
                return;
            }

            var playerCol = ResolvePlayerCollider();
            if (playerCol == null)
            {
                SetPlayerCollisionIgnored(false);
                return;
            }

            Vector2 velocity = _playerBody != null ? _playerBody.linearVelocity : Vector2.zero;
            SetPlayerCollisionIgnored(ShouldPass(playerCol.bounds, velocity));
        }

        bool ShouldPass(Bounds player, Vector2 velocity)
        {
            Bounds platform = _collider.bounds;
            float top = platform.max.y;
            float feet = player.min.y;
            float pad = 0.04f + Mathf.Abs(velocity.x) * Time.fixedDeltaTime;
            bool overlapX = player.max.x > platform.min.x - pad && player.min.x < platform.max.x + pad;
            if (!overlapX)
            {
                _supported = false;
                return true;
            }

            bool feetNearSurface = feet >= top - SurfaceSlack;
            if (_supported && feet >= top - SurfaceSlack * 2f)
            {
                return false;
            }

            // 下から上昇して重なっている間は接触を作らない。
            // 低速だと法線が上向きに反転し、Effector が床として押し返してしまう。
            if (velocity.y > 0.05f || !feetNearSurface)
            {
                _supported = false;
                return true;
            }

            _supported = true;
            return false;
        }

        void SetPlayerCollisionIgnored(bool ignore)
        {
            var playerCol = ResolvePlayerCollider();
            if (playerCol == null || _collider == null || _ignoringPlayer == ignore)
            {
                return;
            }

            Physics2D.IgnoreCollision(_collider, playerCol, ignore);
            _ignoringPlayer = ignore;
        }

        Collider2D ResolvePlayerCollider()
        {
            if (_playerCollider != null)
            {
                return _playerCollider;
            }

            var player = FindFirstObjectByType<PlayerController>();
            if (player == null)
            {
                return null;
            }

            _playerCollider = player.GetComponent<Collider2D>();
            _playerBody = player.GetComponent<Rigidbody2D>();
            return _playerCollider;
        }

        public static bool IsRisingInto(Collider2D platformCol, Bounds playerBounds, Vector2 velocity)
        {
            if (platformCol == null || platformCol.GetComponent<OneWayPlatform>() == null)
            {
                return false;
            }

            float top = platformCol.bounds.max.y;
            float feet = playerBounds.min.y;
            return velocity.y > 0.05f && feet < top + SurfaceSlack;
        }

        public static bool IsSurfaceNormal(Vector2 normal)
        {
            if (normal.sqrMagnitude < 0.0001f)
            {
                return false;
            }

            float limit = Mathf.Cos(SurfaceArc * 0.5f * Mathf.Deg2Rad);
            return Vector2.Dot(normal.normalized, Vector2.up) > limit;
        }

        public static bool ShouldPassQuery(Collider2D col, Vector2 normal)
        {
            return col != null
                   && col.GetComponent<OneWayPlatform>() != null
                   && !IsSurfaceNormal(normal);
        }
    }
}

using UnityEngine;

namespace Pulling
{
    [DefaultExecutionOrder(100)]
    public class BreakableBlock : MonoBehaviour
    {
        GridMapBuilder _builder;
        int _x;
        int _y;
        bool _broken;
        bool _awaitSeparation;
        int _hitsLeft = 1;
        float _speedThreshold = PlayerTuning.BreakableWeakSpeed;
        BoxCollider2D _collider;
        SpriteRenderer _renderer;
        PlayerController _player;
        Collider2D _playerCollider;

        public float MinImpactSpeed => _speedThreshold;

        public void Bind(GridMapBuilder builder, int x, int y, float speedThreshold = -1f, int hitsToBreak = 1)
        {
            _builder = builder;
            _x = x;
            _y = y;
            _broken = false;
            _awaitSeparation = false;
            _hitsLeft = Mathf.Max(1, hitsToBreak);
            if (speedThreshold >= 0f)
            {
                _speedThreshold = speedThreshold;
            }

            CacheCollider();
            if (_hitsLeft > 1)
            {
                ApplySprite(false);
            }

            SyncTrigger();
        }

        void Awake()
        {
            CacheCollider();
        }

        void Update()
        {
            ReleaseIfSeparated();
            SyncTrigger();
        }

        void FixedUpdate()
        {
            ReleaseIfSeparated();
            SyncTrigger();
        }

        public bool WouldBreak(float speed)
        {
            return !_broken && !_awaitSeparation && _hitsLeft <= 1 && speed >= _speedThreshold;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            TryConsume(other);
        }

        void OnTriggerStay2D(Collider2D other)
        {
            TryConsume(other);
        }

        void OnCollisionEnter2D(Collision2D collision)
        {
            if (_broken)
            {
                return;
            }

            var player = FindPlayer(collision);
            if (player == null || player.IsSettling || player.State != PlayerMoveState.Flying)
            {
                return;
            }

            float speed = player.PrePhysicsVelocity.magnitude;
            if (speed < _speedThreshold || _awaitSeparation)
            {
                return;
            }

            if (_hitsLeft > 1)
            {
                Crack();
                return;
            }

            Consume();
        }

        void SyncTrigger()
        {
            if (_broken)
            {
                return;
            }

            CacheCollider();
            if (_collider == null)
            {
                return;
            }

            bool passThrough = ShouldPassThrough();
            if (_collider.isTrigger != passThrough)
            {
                _collider.isTrigger = passThrough;
            }

            if (!passThrough)
            {
                return;
            }

            var playerCol = ResolvePlayerCollider();
            if (playerCol != null && _collider.IsTouching(playerCol))
            {
                Consume();
            }
        }

        bool ShouldPassThrough()
        {
            var player = ResolvePlayer();
            if (player == null || player.IsSettling || player.State != PlayerMoveState.Flying)
            {
                return false;
            }

            return WouldBreak(player.PrePhysicsVelocity.magnitude);
        }

        void TryConsume(Collider2D other)
        {
            if (_broken || other == null)
            {
                return;
            }

            var player = other.GetComponent<PlayerController>();
            if (player == null || player.IsSettling || player.State != PlayerMoveState.Flying)
            {
                return;
            }

            float speed = player.PrePhysicsVelocity.magnitude;
            var body = other.GetComponent<Rigidbody2D>();
            if (body != null)
            {
                speed = Mathf.Max(speed, body.linearVelocity.magnitude);
            }

            if (!WouldBreak(speed))
            {
                return;
            }

            Consume();
        }

        void Crack()
        {
            if (_broken || _hitsLeft <= 1)
            {
                return;
            }

            _hitsLeft = 1;
            _awaitSeparation = true;
            ApplySprite(true);
        }

        void ApplySprite(bool fractured)
        {
            if (_renderer == null)
            {
                _renderer = GetComponent<SpriteRenderer>();
            }

            if (_renderer == null)
            {
                return;
            }

            _renderer.sprite = fractured
                ? SpriteFactory.CreateFractured(_renderer.color)
                : SpriteFactory.CreateSturdy(_renderer.color);
        }

        void ReleaseIfSeparated()
        {
            if (!_awaitSeparation)
            {
                return;
            }

            CacheCollider();
            var playerCol = ResolvePlayerCollider();
            if (_collider == null || playerCol == null || !_collider.IsTouching(playerCol))
            {
                _awaitSeparation = false;
            }
        }

        void Consume()
        {
            if (_broken)
            {
                return;
            }

            _broken = true;
            if (_collider != null)
            {
                _collider.enabled = false;
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

        void CacheCollider()
        {
            if (_collider == null)
            {
                _collider = GetComponent<BoxCollider2D>();
            }
        }

        PlayerController ResolvePlayer()
        {
            if (_player == null)
            {
                _player = FindFirstObjectByType<PlayerController>();
            }

            return _player;
        }

        Collider2D ResolvePlayerCollider()
        {
            if (_playerCollider == null)
            {
                var player = ResolvePlayer();
                if (player != null)
                {
                    _playerCollider = player.GetComponent<Collider2D>();
                }
            }

            return _playerCollider;
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
    }
}

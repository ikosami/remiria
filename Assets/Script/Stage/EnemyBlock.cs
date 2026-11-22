using UnityEngine;

namespace Pulling
{
    public enum EnemyAttack
    {
        Pellet,
        ThreeWay,
        Heavy,
        Laser
    }

    public class EnemyBlock : MonoBehaviour
    {
        GridMapBuilder _builder;
        int _x;
        int _y;
        int _durability = 1;
        EnemyAttack _attack;
        int _shotsLeft = PlayerTuning.EnemyBurstCount;
        float _cooldown;
        LaserStep _laserStep;
        Sprite _bulletSprite;
        Collider2D _collider;
        PlayerController _player;
        EnemyLaser _laser;

        public void Bind(GridMapBuilder builder, int x, int y, int durability, EnemyAttack attack)
        {
            _builder = builder;
            _x = x;
            _y = y;
            _durability = Mathf.Max(1, durability);
            _attack = attack;
            _shotsLeft = PlayerTuning.EnemyBurstCount;
            _laserStep = LaserStep.Wait;
            _cooldown = attack == EnemyAttack.Laser
                ? PlayerTuning.EnemyLaserPause
                : 0.4f + ((x + y * 3) % 5) * 0.12f;
            _collider = GetComponent<Collider2D>();
            var bullet = builder != null && builder.tileDatabase != null
                ? builder.tileDatabase.Get(TileIds.Bullet)
                : null;
            _bulletSprite = bullet != null ? bullet.sprite : null;
            if (_attack == EnemyAttack.Laser)
            {
                EnsureLaser();
            }
        }

        void EnsureLaser()
        {
            if (_laser == null)
            {
                var go = new GameObject("Laser");
                go.transform.SetParent(transform, false);
                _laser = go.AddComponent<EnemyLaser>();
            }

            float reach = _builder != null
                ? (_builder.Width + _builder.Height) * _builder.CellSize
                : 32f;
            _laser.Bind(_collider, _builder != null ? _builder.CellSize : 1f, reach);
        }

        public void Silence()
        {
            _laser?.Hide();
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
                if (child.GetComponent<EnemyShot>() != null)
                {
                    Destroy(child.gameObject);
                }
            }
        }

        void Update()
        {
            if (_builder != null && _builder.IsEditing)
            {
                Silence();
                return;
            }

            if (_builder == null || (_collider != null && !_collider.enabled))
            {
                _laser?.Hide();
                return;
            }

            if (_attack == EnemyAttack.Laser)
            {
                TickLaser();
                return;
            }

            _cooldown -= Time.deltaTime;
            if (_cooldown > 0f)
            {
                return;
            }

            if (_attack == EnemyAttack.Heavy)
            {
                Fire(AimAtPlayer());
                _cooldown = PlayerTuning.EnemyBurstPause;
                return;
            }

            bool lastShot = _shotsLeft <= 1;
            if (_attack != EnemyAttack.ThreeWay || lastShot)
            {
                Fire(AimAtPlayer());
            }

            _shotsLeft--;
            if (_shotsLeft <= 0)
            {
                _shotsLeft = PlayerTuning.EnemyBurstCount;
                _cooldown = PlayerTuning.EnemyBurstPause;
            }
            else
            {
                _cooldown = PlayerTuning.EnemyShotGap;
            }
        }

        enum LaserStep
        {
            Wait,
            Predict,
            Fire
        }

        void TickLaser()
        {
            EnsureLaser();
            _cooldown -= Time.deltaTime;
            if (_cooldown <= 0f)
            {
                switch (_laserStep)
                {
                    case LaserStep.Wait:
                        _laserStep = LaserStep.Predict;
                        _cooldown = PlayerTuning.EnemyLaserPredict;
                        break;
                    case LaserStep.Predict:
                        _laserStep = LaserStep.Fire;
                        _cooldown = PlayerTuning.EnemyLaserFire;
                        _laser.Begin(AimAtPlayer());
                        break;
                    default:
                        _laserStep = LaserStep.Wait;
                        _cooldown = PlayerTuning.EnemyLaserPause;
                        break;
                }
            }

            if (_laserStep == LaserStep.Predict)
            {
                _laser.Preview(transform.position, AimAtPlayer());
            }
            else if (_laserStep == LaserStep.Fire)
            {
                _laser.Show(transform.position, FindPlayer());
            }
            else
            {
                _laser.Hide();
            }
        }

        void Fire(Vector2 aim)
        {
            Vector2 origin = transform.position;
            if (_attack == EnemyAttack.Heavy)
            {
                SpawnShot(origin, aim, PlayerTuning.EnemyHeavyScale, PlayerTuning.EnemyHeavyPush);
                return;
            }

            SpawnShot(origin, aim);
            if (_attack != EnemyAttack.ThreeWay)
            {
                return;
            }

            SpawnShot(origin, Rotate(aim, PlayerTuning.EnemyThreeWayAngle));
            SpawnShot(origin, Rotate(aim, -PlayerTuning.EnemyThreeWayAngle));
        }

        void SpawnShot(Vector2 origin, Vector2 direction, float sizeScale = 1f, float push = PlayerTuning.EnemyShotPush)
        {
            var shot = new GameObject("EnemyShot");
            shot.transform.SetParent(transform, true);
            shot.AddComponent<EnemyShot>().Launch(
                origin,
                direction,
                _bulletSprite,
                _builder.CellSize,
                _collider,
                sizeScale,
                push);
        }

        Vector2 AimAtPlayer(bool refresh = true)
        {
            if (refresh || _player == null)
            {
                _player = FindPlayer();
            }

            Vector2 origin = transform.position;
            Vector2 aim = _player != null ? (Vector2)_player.transform.position - origin : Vector2.left;
            if (aim.sqrMagnitude < 0.04f)
            {
                aim = Vector2.left;
            }

            return aim;
        }

        PlayerController FindPlayer()
        {
            if (_player == null)
            {
                _player = FindAnyObjectByType<PlayerController>();
            }

            return _player;
        }

        static Vector2 Rotate(Vector2 value, float degrees)
        {
            float rad = degrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(rad);
            float sin = Mathf.Sin(rad);
            return new Vector2(value.x * cos - value.y * sin, value.x * sin + value.y * cos);
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


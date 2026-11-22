using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Pulling
{
    public enum PlayerMoveState
    {
        Stopped,
        Aiming,
        Flying
    }

    public class PlayerController : MonoBehaviour
    {
        Rigidbody2D _body;
        CircleCollider2D _collider;
        AimLineView _aimLine;
        Camera _camera;

        PlayerMoveState _state = PlayerMoveState.Stopped;
        float _grace;
        int _lowSpeedFrames;
        bool _inputEnabled = true;
        bool _locked;
        bool _pointerHeld;
        Vector2 _aimStartWorld;
        Vector3 _spawnPosition;
        float _fallMinX = -4f;
        float _fallMaxX = 64f;
        float _fallMinY = -2f;
        System.Action _onFellOff;
        readonly List<ContactPoint2D> _contacts = new List<ContactPoint2D>();
        Vector2 _internalVelocity;
        Vector2 _prePhysicsVelocity;
        Vector2 _lastAimDirection = Vector2.right;
        Vector3 _lastSafePosition;
        GridMapBuilder _mapBuilder;
        bool _hasSakuyaPoint;
        Vector3 _sakuyaPoint;
        float _sunImmunity;
        float _health = PlayerTuning.MaxHealth;
        float _sunExposure;
        Image _hpFill;
        LowHealthVignette _healthVignette;
        bool _settling;
        bool _airJumpReady = true;
        bool _airborneJumpReady;
        bool _aimFromAir;
        Remiria _remiria;
        SpriteRenderer _visual;
        readonly AirJumpOutline _airJumpOutline = new AirJumpOutline();
        bool _faceLeft;
        FlightPose[] _flightPoses = System.Array.Empty<FlightPose>();
        MeilingControl _meilingPhase;
        bool _meilingPending;
        float _meilingTime;
        Vector2 _meilingOrigin;
        Vector2 _meilingCenter;
        Vector2 _meilingVelocity;

        enum MeilingControl
        {
            None,
            Pull,
            Hold,
            Launch
        }

        const float BodyToImage = 0.86f;

        struct FlightPose
        {
            public Vector2 Direction;
            public Sprite Sprite;
        }

        public bool HasAppearance => _remiria != null && (_remiria.normalLeft != null || _remiria.normalRight != null);

        public PlayerMoveState State => _state;
        public float Health => _health;
        public bool IsBusy => _locked || !_inputEnabled;
        public Vector2 PrePhysicsVelocity => _prePhysicsVelocity;
        public Vector2 LastAimDirection => _lastAimDirection;
        public Vector3 LastSafePosition => _lastSafePosition;
        public bool HasSakuyaPoint => _hasSakuyaPoint;
        public bool IsSunImmune => _sunImmunity > 0f;
        public bool IsSettling => _settling;
        public bool IsAirSlowMotion => _state == PlayerMoveState.Aiming && _aimFromAir;
        public bool IsMeilingControlled => _meilingPending || _meilingPhase != MeilingControl.None;

        public bool BeginMeiling(Vector2 center, Vector2 velocity)
        {
            if (_body == null || _locked || IsMeilingControlled)
            {
                return false;
            }

            if (velocity.sqrMagnitude < 0.01f)
            {
                velocity = Vector2.right * PlayerTuning.MeilingDefaultSpeed;
            }

            _pointerHeld = false;
            _aimLine?.Hide();
            Time.timeScale = 1f;
            _aimFromAir = false;
            _settling = false;
            _meilingPending = true;
            _meilingCenter = center;
            _meilingVelocity = velocity;
            _lastAimDirection = velocity.normalized;
            return true;
        }

        public void SmashThrough(Vector2 velocity)
        {
            if (_body == null)
            {
                return;
            }

            _internalVelocity = velocity;
            _prePhysicsVelocity = velocity;
            ApplyActualVelocity();
            _lowSpeedFrames = 0;
            if (_state != PlayerMoveState.Flying)
            {
                _state = PlayerMoveState.Flying;
            }
        }

        public void Nudge(Vector2 impulse)
        {
            if (_body == null || _locked || _settling || impulse.sqrMagnitude < 0.0001f)
            {
                return;
            }

            if (_state == PlayerMoveState.Aiming)
            {
                _pointerHeld = false;
                _aimFromAir = false;
                _aimLine?.Hide();
                Time.timeScale = 1f;
            }

            _internalVelocity += impulse;
            _prePhysicsVelocity = _internalVelocity;
            _lowSpeedFrames = 0;
            _body.constraints = RigidbodyConstraints2D.FreezeRotation;
            _body.WakeUp();
            ApplyActualVelocity();
            _state = PlayerMoveState.Flying;
        }

        public void Bind(Rigidbody2D body, CircleCollider2D col, AimLineView aimLine, Camera cam)
        {
            _body = body;
            _collider = col;
            _aimLine = aimLine;
            _camera = cam;
            _lastSafePosition = transform.position;
            EnterStopped();
            RefreshAppearance();
        }

        public void Apply(Remiria remiria)
        {
            if (remiria != null)
            {
                _remiria = remiria;
            }

            RebuildFlightPoses();
            RefreshAppearance();
        }

        void RebuildFlightPoses()
        {
            if (_remiria == null)
            {
                _flightPoses = System.Array.Empty<FlightPose>();
                return;
            }

            _flightPoses = new[]
            {
                Pose(Vector2.right, _remiria.right),
                Pose(new Vector2(1f, 1f), _remiria.upRight),
                Pose(Vector2.up, _remiria.up),
                Pose(new Vector2(-1f, 1f), _remiria.upLeft),
                Pose(Vector2.left, _remiria.left),
                Pose(new Vector2(-1f, -1f), _remiria.downLeft),
                Pose(Vector2.down, _remiria.down),
                Pose(new Vector2(1f, -1f), _remiria.downRight)
            };
        }

        static FlightPose Pose(Vector2 direction, Sprite sprite)
        {
            return new FlightPose
            {
                Direction = direction.normalized,
                Sprite = sprite
            };
        }

        public void BindMapBuilder(GridMapBuilder builder)
        {
            _mapBuilder = builder;
        }

        public void BindHpBar(Image fill)
        {
            _hpFill = fill;
            RefreshHpBar();
        }

        public void BindHealthVignette(LowHealthVignette vignette)
        {
            _healthVignette = vignette;
            RefreshHpBar();
        }

        public void SetHpBarVisible(bool visible)
        {
            var bar = HpBarRoot();
            if (bar != null)
            {
                bar.gameObject.SetActive(visible);
            }

            _healthVignette?.SetShown(visible);
        }

        public void SetVisible(bool visible)
        {
            if (!visible)
            {
                Freeze();
            }

            if (gameObject.activeSelf != visible)
            {
                gameObject.SetActive(visible);
            }
        }

        Transform HpBarRoot()
        {
            if (_hpFill == null)
            {
                return null;
            }

            var current = _hpFill.transform;
            while (current != null)
            {
                if (current.name == "HPBar")
                {
                    return current;
                }

                current = current.parent;
            }

            return null;
        }

        public void ResetHealth()
        {
            _health = PlayerTuning.MaxHealth;
            _sunExposure = 0f;
            RefreshHpBar();
        }

        public void ClearSunlightExposure()
        {
            _sunExposure = 0f;
        }

        public bool DrainSunlight(float deltaTime)
        {
            if (_locked || _health <= 0f || deltaTime <= 0f || IsSunImmune)
            {
                return false;
            }

            float before = PlayerTuning.SunlightDamageUpTo(_sunExposure);
            _sunExposure += deltaTime;
            _health -= PlayerTuning.SunlightDamageUpTo(_sunExposure) - before;
            if (_health > 0f)
            {
                RefreshHpBar();
                return false;
            }

            _health = 0f;
            RefreshHpBar();
            return true;
        }

        void RefreshHpBar()
        {
            float ratio = PlayerTuning.MaxHealth <= 0f ? 0f : Mathf.Clamp01(_health / PlayerTuning.MaxHealth);
            if (_hpFill != null)
            {
                _hpFill.fillAmount = ratio;
            }

            _healthVignette?.SetRatio(ratio);
        }

        public void TouchSakuya(Vector3 world)
        {
            _hasSakuyaPoint = true;
            _sakuyaPoint = world;
            ResetHealth();
        }

        public bool TryRespawnAtSakuya()
        {
            if (!_hasSakuyaPoint)
            {
                return false;
            }

            Vector3 point = _sakuyaPoint;
            Vector2 current = _body != null ? _body.position : (Vector2)transform.position;
            if (((Vector2)point - current).sqrMagnitude < 0.0001f)
            {
                return false;
            }

            PlaceAt(point, true);
            _hasSakuyaPoint = true;
            _sakuyaPoint = point;
            return true;
        }

        public void GrantSunImmunity(float seconds)
        {
            if (seconds <= 0f)
            {
                return;
            }

            _sunImmunity = Mathf.Max(_sunImmunity, seconds);
        }

        public void TickSunImmunity(float deltaTime)
        {
            if (_sunImmunity <= 0f || deltaTime <= 0f)
            {
                return;
            }

            _sunImmunity = Mathf.Max(0f, _sunImmunity - deltaTime);
        }

        public void SetInputEnabled(bool enabled)
        {
            _inputEnabled = enabled;
            if (!enabled && _state == PlayerMoveState.Aiming)
            {
                CancelAim();
            }
        }

        public void SetLocked(bool locked)
        {
            _locked = locked;
            if (locked)
            {
                CancelAim();
                Freeze();
            }
        }

        public void PlaceAt(Vector3 world)
        {
            PlaceAt(world, true);
        }

        public void PlaceAt(Vector3 world, bool resetHealth)
        {
            CancelMeilingMotion();
            _spawnPosition = world;
            _airJumpReady = true;
            _airborneJumpReady = false;
            _aimFromAir = false;
            if (_body != null)
            {
                _body.position = world;
            }

            transform.position = world;
            _lastSafePosition = world;
            _hasSakuyaPoint = false;
            _sunImmunity = 0f;
            _settling = false;
            if (resetHealth)
            {
                ResetHealth();
            }

            // 開始直後の形状問い合わせはタイルが未反映で、空中なのに足場ありと誤ることがある。接触するまで落とす。
            BeginDrop();
        }

        public void Respawn()
        {
            PlaceAt(_spawnPosition);
        }

        public void SetFallBounds(float minX, float maxX, float minY, System.Action onFellOff)
        {
            _fallMinX = minX;
            _fallMaxX = maxX;
            _fallMinY = minY;
            _onFellOff = onFellOff;
        }

        public void CheckFallenOff()
        {
            if (_locked || _body == null || _onFellOff == null)
            {
                return;
            }

            Vector2 pos = _body.position;
            if (pos.y < _fallMinY || pos.x < _fallMinX || pos.x > _fallMaxX)
            {
                _onFellOff.Invoke();
            }
        }

        public void Freeze()
        {
            if (_body == null)
            {
                return;
            }

            CancelMeilingMotion();
            _settling = false;
            _internalVelocity = Vector2.zero;
            _prePhysicsVelocity = Vector2.zero;
            RestoreBodyFlightForces();
            _body.linearVelocity = Vector2.zero;
            _body.angularVelocity = 0f;
            _body.sleepMode = RigidbodySleepMode2D.StartAwake;
            _body.constraints = RigidbodyConstraints2D.FreezeAll;
            _state = PlayerMoveState.Stopped;
            _pointerHeld = false;
            _lowSpeedFrames = 0;
            _aimLine?.Hide();
        }

        void Update()
        {
            if (_body == null || _locked || !_inputEnabled || IsMeilingControlled)
            {
                return;
            }

            if (!TryReadPointer(out Vector2 screen, out bool pressedThisFrame, out bool releasedThisFrame, out bool held))
            {
                return;
            }

            Vector2 world = ScreenToWorld(screen);

            if (CanBeginAim() && pressedThisFrame)
            {
                _pointerHeld = true;
                BeginAim(world);
                UpdateAim(world);
                return;
            }

            if (_state == PlayerMoveState.Aiming)
            {
                UpdateAim(world);
                if (releasedThisFrame || !held)
                {
                    _pointerHeld = false;
                    ReleaseAim(world);
                }
            }
        }

        void LateUpdate()
        {
            RefreshAppearance();
        }

        void FixedUpdate()
        {
            CheckFallenOff();
            if (_body == null)
            {
                return;
            }

            if (_meilingPending)
            {
                _meilingPending = false;
                ActivateMeiling();
            }

            if (_meilingPhase != MeilingControl.None)
            {
                StepMeiling();
                return;
            }

            if (_state == PlayerMoveState.Aiming && _aimFromAir)
            {
                StepFlight();
                return;
            }

            if (_state == PlayerMoveState.Flying)
            {
                StepFlight();
                if (!_settling)
                {
                    TryAutoStop();
                }

                return;
            }

            RestoreBodyFlightForces();
            _internalVelocity = _body.linearVelocity;
            _prePhysicsVelocity = _body.linearVelocity;
        }

        void StepFlight()
        {
            AbsorbBlockedVelocity();
            _prePhysicsVelocity = _internalVelocity;
            float dt = Time.fixedDeltaTime;
            Vector2 gravity = Physics2D.gravity * PlayerTuning.GravityScale;
            _internalVelocity = IntegrateFlightVelocity(_internalVelocity, gravity, PlayerTuning.FlightLinearDamping, dt);
            ApplyActualVelocity();
        }

        // 接触中に内部速度へ重力を溜めると、床にめり込む速度のまま止まらなくなる。
        void AbsorbBlockedVelocity()
        {
            if (_collider == null)
            {
                return;
            }

            _contacts.Clear();
            var filter = new ContactFilter2D
            {
                useTriggers = false,
                useLayerMask = false
            };
            int count = _collider.GetContacts(filter, _contacts);
            for (int i = 0; i < count; i++)
            {
                var contact = _contacts[i];
                if (contact.collider == null || contact.collider == _collider || contact.collider.isTrigger)
                {
                    continue;
                }

                if (OneWayPlatform.IsRisingInto(contact.collider, _collider.bounds, _internalVelocity))
                {
                    continue;
                }

                var breakable = contact.collider.GetComponent<BreakableBlock>();
                if (breakable != null && breakable.WouldBreak(_internalVelocity.magnitude))
                {
                    continue;
                }

                Vector2 normal = contact.normal;
                float into = Vector2.Dot(_internalVelocity, normal);
                if (into < 0f)
                {
                    _internalVelocity -= into * normal;
                }
            }
        }

        // 剛体には実速度だけ載せる。内部速度のまま進むと、先の位置まで掃引して当たる。
        void ApplyActualVelocity()
        {
            if (_body == null)
            {
                return;
            }

            _body.gravityScale = 0f;
            _body.linearDamping = 0f;
            _body.linearVelocity = ToActualVelocity(_internalVelocity);
        }

        void RestoreBodyFlightForces()
        {
            if (_body == null)
            {
                return;
            }

            _body.gravityScale = PlayerTuning.GravityScale;
            _body.linearDamping = PlayerTuning.FlightLinearDamping;
        }

        static Vector2 ToActualVelocity(Vector2 internalVelocity)
        {
            float cap = PlayerTuning.MaxMoveSpeed;
            float sqr = internalVelocity.sqrMagnitude;
            if (cap <= 0f || sqr <= cap * cap || sqr < 0.0000001f)
            {
                return internalVelocity;
            }

            return internalVelocity * (cap / Mathf.Sqrt(sqr));
        }

        void ClampFallSpeed()
        {
            if (_body == null)
            {
                return;
            }

            _body.linearVelocity = ClampFallSpeed(_body.linearVelocity);
        }

        // Yだけ切ると斜め落下の角度が変わる。上限を超えた分は向きを保ったまま全体を縮める。
        static Vector2 ClampFallSpeed(Vector2 velocity)
        {
            float minY = -PlayerTuning.MaxFallSpeed;
            if (velocity.y >= minY)
            {
                return velocity;
            }

            return velocity * (minY / velocity.y);
        }

        void OnCollisionEnter2D(Collision2D collision)
        {
            if (_meilingPhase == MeilingControl.Pull || _meilingPhase == MeilingControl.Hold)
            {
                return;
            }

            if (TryFinishDrop(collision))
            {
                return;
            }

            if (_body == null || !TryGetCollisionContact(collision, out ContactPoint2D contact))
            {
                return;
            }

            if (!IsFlightCollisionActive() || _grace > 0f)
            {
                return;
            }

            if (_collider != null
                && OneWayPlatform.IsRisingInto(collision.collider, _collider.bounds, _prePhysicsVelocity))
            {
                _body.linearVelocity = ToActualVelocity(_internalVelocity);
                return;
            }

            Vector2 normal = contact.normal;
            float impactSpeed = GetImpactSpeed(normal);
            var breakable = collision.collider != null ? collision.collider.GetComponent<BreakableBlock>() : null;
            if (breakable != null && breakable.WouldBreak(_prePhysicsVelocity.magnitude))
            {
                _body.linearVelocity = ToActualVelocity(_internalVelocity);
                return;
            }

            if (_meilingPhase == MeilingControl.Launch)
            {
                ReleaseMeilingToPhysics();
            }

            bool isGround = normal.y >= PlayerTuning.GroundNormalMin;
            if (isGround && impactSpeed < PlayerTuning.LandSpeedThreshold)
            {
                if (_state == PlayerMoveState.Aiming)
                {
                    return;
                }

                EnterStopped();
                return;
            }

            float bounce = BounceResolver.GetBounciness(collision.collider, contact.point, _mapBuilder);
            _internalVelocity = BounceVelocity(_prePhysicsVelocity, normal, bounce);
            _prePhysicsVelocity = _internalVelocity;
            _body.linearVelocity = ToActualVelocity(_internalVelocity);
        }

        bool TryGetCollisionContact(Collision2D collision, out ContactPoint2D contact)
        {
            contact = default;
            if (collision.collider == null || collision.collider.isTrigger)
            {
                return false;
            }

            if (collision.contactCount == 0)
            {
                return false;
            }

            contact = collision.GetContact(0);
            float bestDot = Vector2.Dot(contact.normal, _prePhysicsVelocity);
            for (int i = 1; i < collision.contactCount; i++)
            {
                var candidate = collision.GetContact(i);
                float dot = Vector2.Dot(candidate.normal, _prePhysicsVelocity);
                if (dot < bestDot)
                {
                    bestDot = dot;
                    contact = candidate;
                }
            }

            return true;
        }

        void OnCollisionStay2D(Collision2D collision)
        {
            TryFinishDrop(collision);
        }

        bool IsFlightCollisionActive()
        {
            return _state == PlayerMoveState.Flying
                   || (_state == PlayerMoveState.Aiming && _aimFromAir);
        }

        float GetImpactSpeed(Vector2 normal)
        {
            return Mathf.Max(0f, -Vector2.Dot(_prePhysicsVelocity, normal));
        }

        void TryAutoStop()
        {
            if (_grace > 0f)
            {
                _grace -= Time.deltaTime;
                return;
            }

            if (IsGrounded())
            {
                if (_body.linearVelocity.magnitude < PlayerTuning.StopThreshold)
                {
                    EnterStopped();
                }

                return;
            }

            float speed = _body.linearVelocity.magnitude;
            if (!PlayerTuning.RequireGroundedToStop && speed < PlayerTuning.StopThreshold)
            {
                _lowSpeedFrames++;
                if (_lowSpeedFrames >= PlayerTuning.StopHoldFrames)
                {
                    EnterStopped();
                }
            }
            else
            {
                _lowSpeedFrames = 0;
            }
        }

        bool CanBeginAim()
        {
            if (IsMeilingControlled)
            {
                return false;
            }

            if (_state == PlayerMoveState.Stopped)
            {
                return true;
            }

            if (_state != PlayerMoveState.Flying || _grace > 0f || _body == null)
            {
                return false;
            }

            if (_airJumpReady && !IsGrounded())
            {
                return true;
            }

            if (_settling)
            {
                return false;
            }

            return _body.linearVelocity.magnitude < PlayerTuning.StopThreshold
                   && IsGrounded();
        }

        void BeginAim(Vector2 world)
        {
            _aimFromAir = _state == PlayerMoveState.Flying && !IsGrounded();
            if (_aimFromAir)
            {
                _settling = false;
            }

            _state = PlayerMoveState.Aiming;
            _aimStartWorld = world;
            Time.timeScale = PlayerTuning.LaunchTimeScale;
        }

        Vector2 ReadPull(Vector2 world)
        {
            return Vector2.ClampMagnitude(world - _aimStartWorld, PlayerTuning.MaxPullDistance);
        }

        Vector2 ReadLaunchImpulse(Vector2 pullBack)
        {
            return -pullBack / PlayerTuning.MaxPullDistance * PlayerTuning.LaunchImpulse;
        }

        void UpdateAim(Vector2 world)
        {
            _aimLine?.ShowPullOrigin(_aimStartWorld, PlayerTuning.MaxPullDistance * 0.5f);
            Vector2 pullBack = ReadPull(world);
            if (pullBack.sqrMagnitude < 0.04f)
            {
                _aimLine?.HideDirection();
                return;
            }

            _lastAimDirection = -pullBack.normalized;
            float strength = pullBack.magnitude / PlayerTuning.MaxPullDistance;
            _aimLine?.ShowDirection(_body.position, _lastAimDirection, strength);
        }

        void ReleaseAim(Vector2 world)
        {
            Time.timeScale = 1f;
            Vector2 pullBack = ReadPull(world);
            _aimLine?.Hide();

            if (pullBack.sqrMagnitude < 0.04f)
            {
                AbortAim();
                return;
            }

            bool consumeAirJump = _aimFromAir && !IsGrounded();
            _aimFromAir = false;
            _settling = false;
            _body.constraints = RigidbodyConstraints2D.FreezeRotation;
            _body.WakeUp();
            Vector2 launchVel = ReadLaunchImpulse(pullBack) / Mathf.Max(0.01f, _body.mass);
            _lastAimDirection = launchVel.sqrMagnitude > 0.01f ? launchVel.normalized : _lastAimDirection;
            _internalVelocity = launchVel;
            _prePhysicsVelocity = launchVel;
            ApplyActualVelocity();
            _state = PlayerMoveState.Flying;
            _grace = PlayerTuning.LaunchGrace;
            _lowSpeedFrames = 0;
            if (consumeAirJump)
            {
                _airJumpReady = false;
            }
            else
            {
                _airJumpReady = true;
                _airborneJumpReady = true;
            }
        }

        void OnDisable()
        {
            CancelMeilingMotion();
            Time.timeScale = 1f;
        }

        void ActivateMeiling()
        {
            if (_body == null || _locked)
            {
                return;
            }

            _meilingPhase = MeilingControl.Pull;
            _meilingTime = 0f;
            _meilingOrigin = _body.position;
            _state = PlayerMoveState.Flying;
            _settling = false;
            _aimFromAir = false;
            _grace = 0f;
            _lowSpeedFrames = 0;
            _body.constraints = RigidbodyConstraints2D.FreezeRotation;
            _body.bodyType = RigidbodyType2D.Kinematic;
            _body.linearVelocity = Vector2.zero;
            _body.angularVelocity = 0f;
            _internalVelocity = _meilingCenter - _meilingOrigin;
            _prePhysicsVelocity = Vector2.zero;
        }

        void StepMeiling()
        {
            if (_body == null)
            {
                CancelMeilingMotion();
                return;
            }

            float dt = Time.fixedDeltaTime;
            _meilingTime += dt;
            switch (_meilingPhase)
            {
                case MeilingControl.Pull:
                    {
                        float duration = Mathf.Max(0.01f, PlayerTuning.MeilingPullSeconds);
                        float t = Mathf.Clamp01(_meilingTime / duration);
                        Vector2 pos = Vector2.Lerp(_meilingOrigin, _meilingCenter, t);
                        _internalVelocity = (pos - _body.position) / Mathf.Max(dt, 0.0001f);
                        _body.MovePosition(pos);
                        if (t >= 1f)
                        {
                            _meilingPhase = MeilingControl.Hold;
                            _meilingTime = 0f;
                        }

                        break;
                    }
                case MeilingControl.Hold:
                    {
                        _internalVelocity = Vector2.zero;
                        _body.MovePosition(_meilingCenter);
                        if (_meilingTime >= PlayerTuning.MeilingHoldSeconds)
                        {
                            BeginMeilingLaunch();
                        }

                        break;
                    }
                case MeilingControl.Launch:
                    {
                        _internalVelocity = _meilingVelocity;
                        _prePhysicsVelocity = _meilingVelocity;
                        _body.gravityScale = 0f;
                        _body.linearDamping = 0f;
                        _body.linearVelocity = _meilingVelocity;
                        if (_meilingTime >= PlayerTuning.MeilingLaunchSeconds)
                        {
                            EndMeilingLaunch();
                        }

                        break;
                    }
            }
        }

        void BeginMeilingLaunch()
        {
            _meilingPhase = MeilingControl.Launch;
            _meilingTime = 0f;
            _body.bodyType = RigidbodyType2D.Dynamic;
            _body.constraints = RigidbodyConstraints2D.FreezeRotation;
            _body.gravityScale = 0f;
            _body.linearDamping = 0f;
            _internalVelocity = _meilingVelocity;
            _prePhysicsVelocity = _meilingVelocity;
            _body.linearVelocity = _meilingVelocity;
            _body.WakeUp();
            _state = PlayerMoveState.Flying;
            _grace = 0f;
            _lowSpeedFrames = 0;
        }

        void EndMeilingLaunch()
        {
            _meilingPhase = MeilingControl.None;
            if (_body != null && _body.bodyType != RigidbodyType2D.Dynamic)
            {
                _body.bodyType = RigidbodyType2D.Dynamic;
            }

            _internalVelocity = _meilingVelocity;
            _prePhysicsVelocity = _meilingVelocity;
            _state = PlayerMoveState.Flying;
            _grace = PlayerTuning.LaunchGrace;
            _lowSpeedFrames = 0;
            ApplyActualVelocity();
        }

        void ReleaseMeilingToPhysics()
        {
            _meilingPhase = MeilingControl.None;
            if (_body != null && _body.bodyType != RigidbodyType2D.Dynamic)
            {
                _body.bodyType = RigidbodyType2D.Dynamic;
            }

            _internalVelocity = _meilingVelocity;
            _prePhysicsVelocity = _meilingVelocity;
            _state = PlayerMoveState.Flying;
            _grace = 0f;
            _lowSpeedFrames = 0;
        }

        void CancelMeilingMotion()
        {
            _meilingPending = false;
            _meilingPhase = MeilingControl.None;
            if (_body != null && _body.bodyType != RigidbodyType2D.Dynamic)
            {
                _body.bodyType = RigidbodyType2D.Dynamic;
            }
        }

        static Vector2 IntegrateFlightVelocity(Vector2 velocity, Vector2 gravity, float damping, float dt)
        {
            velocity = ClampFallSpeed(velocity);
            float damp = 1f / (1f + damping * dt);
            velocity = velocity * damp + gravity * dt;
            return ClampFallSpeed(velocity);
        }

        static Vector2 BounceVelocity(Vector2 velocity, Vector2 normal, float bounciness)
        {
            float vn = Vector2.Dot(velocity, normal);
            if (vn < 0f)
            {
                velocity -= (1f + bounciness) * vn * normal;
            }

            Vector2 tangent = velocity - normal * Vector2.Dot(velocity, normal);
            velocity = tangent * (1f - PlayerTuning.BounceFriction) + normal * Vector2.Dot(velocity, normal);
            return velocity;
        }

        void CancelAim()
        {
            _pointerHeld = false;
            _aimLine?.Hide();
            Time.timeScale = 1f;
            if (_state == PlayerMoveState.Aiming)
            {
                AbortAim();
            }
        }

        void AbortAim()
        {
            if (_aimFromAir && !IsGrounded())
            {
                _aimFromAir = false;
                _pointerHeld = false;
                _settling = false;
                _state = PlayerMoveState.Flying;
                if (_body != null)
                {
                    _body.constraints = RigidbodyConstraints2D.FreezeRotation;
                    ApplyActualVelocity();
                }

                return;
            }

            _aimFromAir = false;
            EnterStopped();
        }

        void EnterStopped()
        {
            if (_body != null)
            {
                _lastSafePosition = _body.position;
            }

            _airJumpReady = true;
            _airborneJumpReady = false;
            _aimFromAir = false;
            Freeze();
        }

        void BeginDrop()
        {
            _settling = true;
            _grace = 0f;
            _lowSpeedFrames = 0;
            _pointerHeld = false;
            _aimLine?.Hide();
            Time.timeScale = 1f;
            _state = PlayerMoveState.Flying;
            if (_body == null)
            {
                return;
            }

            _body.bodyType = RigidbodyType2D.Dynamic;
            _body.constraints = RigidbodyConstraints2D.FreezeRotation;
            _body.sleepMode = RigidbodySleepMode2D.NeverSleep;
            _internalVelocity = Vector2.zero;
            _prePhysicsVelocity = Vector2.zero;
            RestoreBodyFlightForces();
            _body.linearVelocity = Vector2.zero;
            _body.angularVelocity = 0f;
            _body.WakeUp();
        }

        bool TryFinishDrop(Collision2D collision)
        {
            if (!_settling || _body == null)
            {
                return false;
            }

            if (TryGetCollisionContact(collision, out ContactPoint2D contact)
                && contact.normal.y >= PlayerTuning.GroundNormalMin)
            {
                EnterStopped();
            }

            return true;
        }

        bool TryReadPointer(out Vector2 screen, out bool pressedThisFrame, out bool releasedThisFrame, out bool held)
        {
            screen = default;
            pressedThisFrame = false;
            releasedThisFrame = false;
            held = false;

            if (Mouse.current != null)
            {
                screen = Mouse.current.position.ReadValue();
                pressedThisFrame = Mouse.current.leftButton.wasPressedThisFrame;
                releasedThisFrame = Mouse.current.leftButton.wasReleasedThisFrame;
                held = Mouse.current.leftButton.isPressed;
                return true;
            }

            if (Pointer.current != null)
            {
                screen = Pointer.current.position.ReadValue();
                pressedThisFrame = Pointer.current.press.wasPressedThisFrame;
                releasedThisFrame = Pointer.current.press.wasReleasedThisFrame;
                held = Pointer.current.press.isPressed;
                return true;
            }

            if (Touchscreen.current != null)
            {
                var touch = Touchscreen.current.primaryTouch;
                screen = touch.position.ReadValue();
                pressedThisFrame = touch.press.wasPressedThisFrame;
                releasedThisFrame = touch.press.wasReleasedThisFrame;
                held = touch.press.isPressed;
                return true;
            }

            return false;
        }

        Vector2 ScreenToWorld(Vector2 screen)
        {
            Camera cam = _camera != null ? _camera : Camera.main;
            float depth = Mathf.Abs(cam.transform.position.z);
            return cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, depth));
        }

        bool IsGrounded()
        {
            if (_collider == null || _body == null)
            {
                return false;
            }

            _contacts.Clear();
            var filter = new ContactFilter2D
            {
                useTriggers = false,
                useLayerMask = false
            };
            int count = _collider.GetContacts(filter, _contacts);
            for (int i = 0; i < count; i++)
            {
                var contact = _contacts[i];
                if (contact.collider == null || contact.collider == _collider || contact.collider.isTrigger)
                {
                    continue;
                }

                if (contact.normal.y >= PlayerTuning.GroundNormalMin)
                {
                    return true;
                }
            }

            float scale = Mathf.Abs(transform.lossyScale.x);
            float radius = _collider.radius * scale;
            Vector2 origin = _body.position + Vector2.down * (radius * 0.2f);
            var hits = Physics2D.OverlapCircleAll(origin, radius * 0.75f);
            for (int i = 0; i < hits.Length; i++)
            {
                var hit = hits[i];
                if (hit == null || hit == _collider || hit.isTrigger)
                {
                    continue;
                }

                if (hit.bounds.max.y <= _body.position.y + 0.05f || hit.transform.position.y <= _body.position.y)
                {
                    return true;
                }
            }

            return false;
        }

        void RefreshAppearance()
        {
            if (_remiria == null)
            {
                return;
            }

            EnsureVisual();
            Vector2 face = FacingDirection();
            Sprite sprite;
            if (_state == PlayerMoveState.Aiming)
            {
                sprite = SideSprite(_remiria.stanceLeft, _remiria.stanceRight, face);
            }
            else if (_state == PlayerMoveState.Flying)
            {
                sprite = FlightSprite(face);
            }
            else
            {
                sprite = SideSprite(_remiria.normalLeft, _remiria.normalRight, face);
            }

            if (sprite == null)
            {
                return;
            }

            _visual.sprite = sprite;
            _visual.flipX = false;
            _visual.flipY = false;
            if (CanBreakBlocks())
            {
                _visual.color = new Color(1f, 0.25f, 0.25f, 1f);
            }
            else if (IsSunImmune)
            {
                _visual.color = new Color(1f, 0.9f, 0.45f, 1f);
            }
            else
            {
                _visual.color = Color.white;
            }
            AlignVisual(sprite);
            _airJumpOutline.Apply(_visual, _airJumpReady && _airborneJumpReady && !IsGrounded());
        }

        bool CanBreakBlocks()
        {
            return _state == PlayerMoveState.Flying
                   && _internalVelocity.magnitude >= PlayerTuning.BreakableWeakSpeed;
        }

        Sprite FlightSprite(Vector2 velocity)
        {
            if (_settling || velocity.sqrMagnitude <= PlayerTuning.FallPoseSpeed * PlayerTuning.FallPoseSpeed)
            {
                return SideSprite(_remiria.fallLeft, _remiria.fallRight, velocity);
            }

            if (_flightPoses == null || _flightPoses.Length == 0)
            {
                RebuildFlightPoses();
            }

            Vector2 direction = velocity.normalized;
            Sprite best = null;
            float bestDot = float.NegativeInfinity;
            for (int i = 0; i < _flightPoses.Length; i++)
            {
                Sprite candidate = _flightPoses[i].Sprite;
                if (candidate == null)
                {
                    continue;
                }

                float dot = Vector2.Dot(direction, _flightPoses[i].Direction);
                if (dot > bestDot)
                {
                    bestDot = dot;
                    best = candidate;
                }
            }

            return best != null ? best : SideSprite(_remiria.fallLeft, _remiria.fallRight, velocity);
        }

        Sprite SideSprite(Sprite left, Sprite right, Vector2 direction)
        {
            if (Mathf.Abs(direction.x) > 0.2f)
            {
                _faceLeft = direction.x < 0f;
            }

            if (_faceLeft)
            {
                return left != null ? left : right;
            }

            return right != null ? right : left;
        }

        Vector2 FacingDirection()
        {
            if (_state == PlayerMoveState.Flying)
            {
                if (_internalVelocity.sqrMagnitude > 0.04f)
                {
                    return _internalVelocity;
                }

                if (_body != null && _body.linearVelocity.sqrMagnitude > 0.04f)
                {
                    return _body.linearVelocity;
                }
            }

            return _lastAimDirection;
        }

        void EnsureVisual()
        {
            if (_visual == null)
            {
                var existing = transform.Find("Visual");
                if (existing != null)
                {
                    _visual = existing.GetComponent<SpriteRenderer>();
                }
            }

            if (_visual == null)
            {
                var go = new GameObject("Visual");
                go.transform.SetParent(transform, false);
                _visual = go.AddComponent<SpriteRenderer>();
            }

            _visual.sortingOrder = 5;
            var root = GetComponent<SpriteRenderer>();
            Material template = root != null && root != _visual ? root.sharedMaterial : null;
            SpriteFactory.AssignPipelineMaterial(_visual, template);
            if (root != null && root != _visual)
            {
                root.enabled = false;
            }
        }

        void AlignVisual(Sprite sprite)
        {
            if (_visual == null || sprite == null)
            {
                return;
            }

            float pixelsPerUnit = sprite.pixelsPerUnit > 0.001f ? sprite.pixelsPerUnit : 100f;
            float imagePixels = Mathf.Max(sprite.texture.width, sprite.texture.height);
            float diameter = _collider != null ? _collider.radius * 2f : 0.84f;
            float imageUnits = imagePixels / pixelsPerUnit;
            float localScale = imageUnits > 0.001f ? diameter / (imageUnits * BodyToImage) : 1f;
            _visual.transform.localScale = new Vector3(localScale, localScale, 1f);

            Rect rect = sprite.textureRect;
            Vector2 canvasCenter = new Vector2(sprite.texture.width, sprite.texture.height) * 0.5f;
            Vector2 offset = (rect.center - canvasCenter) / pixelsPerUnit * localScale;
            _visual.transform.localPosition = new Vector3(offset.x, offset.y, 0f);
        }
    }
}

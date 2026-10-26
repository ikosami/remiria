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
        bool _sakuyaShield;
        float _health = PlayerTuning.MaxHealth;
        float _sunExposure;
        Image _hpFill;
        bool _settling;
        [SerializeField] Sprite _normalSprite;
        [SerializeField] Sprite _jumpWaitSprite;
        [SerializeField] Sprite _jumpSprite;
        SpriteRenderer _visual;
        bool _faceLeft;

        const float SpawnSupportProbe = 0.4f;
        const float VisualHeight = 0.9f;

        public bool HasAppearance => _normalSprite != null;

        public PlayerMoveState State => _state;
        public float Health => _health;
        public bool IsBusy => _locked || !_inputEnabled;
        public Vector2 PrePhysicsVelocity => _prePhysicsVelocity;
        public Vector2 LastAimDirection => _lastAimDirection;
        public Vector3 LastSafePosition => _lastSafePosition;
        public bool HasSakuyaShield => _sakuyaShield;
        public bool IsSettling => _settling;

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

        public void SetSprites(Sprite normal, Sprite jumpWait, Sprite jump)
        {
            if (normal != null)
            {
                _normalSprite = normal;
            }

            if (jumpWait != null)
            {
                _jumpWaitSprite = jumpWait;
            }

            if (jump != null)
            {
                _jumpSprite = jump;
            }

            RefreshAppearance();
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

        public void SetHpBarVisible(bool visible)
        {
            var bar = HpBarRoot();
            if (bar != null)
            {
                bar.gameObject.SetActive(visible);
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
            if (_locked || _health <= 0f || deltaTime <= 0f)
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
            if (_hpFill == null)
            {
                return;
            }

            _hpFill.fillAmount = Mathf.Clamp01(_health / PlayerTuning.MaxHealth);
        }

        public void GrantSakuyaShield()
        {
            _sakuyaShield = true;
        }

        public bool ConsumeSakuyaShield()
        {
            if (!_sakuyaShield)
            {
                return false;
            }

            _sakuyaShield = false;
            return true;
        }

        public void ReturnToSafePosition()
        {
            PlaceAt(_lastSafePosition, false);
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
            _spawnPosition = world;
            if (_body != null)
            {
                _body.position = world;
            }

            transform.position = world;
            _lastSafePosition = world;
            _sakuyaShield = false;
            _settling = false;
            if (resetHealth)
            {
                ResetHealth();
            }

            Physics2D.SyncTransforms();
            if (HasSupportBelow())
            {
                EnterStopped();
            }
            else
            {
                BeginDrop();
            }
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

            _settling = false;
            _internalVelocity = Vector2.zero;
            _prePhysicsVelocity = Vector2.zero;
            RestoreBodyFlightForces();
            _body.linearVelocity = Vector2.zero;
            _body.angularVelocity = 0f;
            _body.constraints = RigidbodyConstraints2D.FreezeAll;
            _state = PlayerMoveState.Stopped;
            _pointerHeld = false;
            _lowSpeedFrames = 0;
            _aimLine?.Hide();
        }

        void Update()
        {
            if (_body == null || _locked || !_inputEnabled)
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

            if (_state == PlayerMoveState.Flying && !_settling)
            {
                StepFlight();
                TryAutoStop();
                return;
            }

            RestoreBodyFlightForces();
            _internalVelocity = _body.linearVelocity;
            _prePhysicsVelocity = _body.linearVelocity;
            if (_state == PlayerMoveState.Flying)
            {
                ClampFallSpeed();
            }
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
            Vector2 vel = _body.linearVelocity;
            float minY = -PlayerTuning.MaxFallSpeed;
            if (vel.y < minY)
            {
                vel.y = minY;
                _body.linearVelocity = vel;
            }
        }

        static Vector2 ClampFallSpeed(Vector2 velocity)
        {
            velocity.y = Mathf.Max(velocity.y, -PlayerTuning.MaxFallSpeed);
            return velocity;
        }

        void OnCollisionEnter2D(Collision2D collision)
        {
            if (TryFinishDrop(collision))
            {
                return;
            }

            if (_body == null || _state != PlayerMoveState.Flying || _grace > 0f)
            {
                return;
            }

            if (!TryGetCollisionContact(collision, out ContactPoint2D contact))
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

            bool isGround = normal.y >= PlayerTuning.GroundNormalMin;
            if (isGround && impactSpeed < PlayerTuning.LandSpeedThreshold)
            {
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
            if (_settling)
            {
                return false;
            }

            if (_state == PlayerMoveState.Stopped)
            {
                return true;
            }

            return _state == PlayerMoveState.Flying
                   && _grace <= 0f
                   && _body.linearVelocity.magnitude < PlayerTuning.StopThreshold
                   && IsGrounded();
        }

        void BeginAim(Vector2 world)
        {
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
            Vector2 pullBack = ReadPull(world);
            if (pullBack.sqrMagnitude < 0.04f)
            {
                _aimLine?.Hide();
                return;
            }

            _lastAimDirection = -pullBack.normalized;
            _aimLine?.ShowDirection(_body.position, _lastAimDirection);
        }

        void ReleaseAim(Vector2 world)
        {
            Time.timeScale = 1f;
            Vector2 pullBack = ReadPull(world);
            _aimLine?.Hide();

            if (pullBack.sqrMagnitude < 0.04f)
            {
                EnterStopped();
                return;
            }

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
        }

        void OnDisable()
        {
            Time.timeScale = 1f;
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
                EnterStopped();
            }
        }

        void EnterStopped()
        {
            if (_body != null)
            {
                _lastSafePosition = _body.position;
            }

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

            _body.constraints = RigidbodyConstraints2D.FreezeRotation;
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

        bool HasSupportBelow()
        {
            if (_collider == null)
            {
                return true;
            }

            float scale = Mathf.Abs(transform.lossyScale.x);
            float radius = _collider.radius * scale;
            Vector2 origin = _body != null ? _body.position : (Vector2)transform.position;
            var hits = Physics2D.CircleCastAll(origin, radius * 0.9f, Vector2.down, SpawnSupportProbe);
            for (int i = 0; i < hits.Length; i++)
            {
                var hit = hits[i];
                if (hit.collider == null || hit.collider == _collider || hit.collider.isTrigger)
                {
                    continue;
                }

                if (hit.normal.y >= PlayerTuning.GroundNormalMin)
                {
                    return true;
                }
            }

            return false;
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
            if (_normalSprite == null)
            {
                return;
            }

            EnsureVisual();
            Sprite sprite = _normalSprite;
            if (_state == PlayerMoveState.Aiming && _jumpWaitSprite != null)
            {
                sprite = _jumpWaitSprite;
            }
            else if (_state == PlayerMoveState.Flying && _jumpSprite != null)
            {
                sprite = _jumpSprite;
            }

            Vector2 face = FacingDirection();
            if (Mathf.Abs(face.x) > 0.2f)
            {
                _faceLeft = face.x < 0f;
            }

            _visual.sprite = sprite;
            _visual.flipX = _faceLeft;
            AlignVisual(sprite);
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
            if (_visual != null)
            {
                return;
            }

            var existing = transform.Find("Visual");
            if (existing != null)
            {
                _visual = existing.GetComponent<SpriteRenderer>();
            }

            if (_visual == null)
            {
                var go = new GameObject("Visual");
                go.transform.SetParent(transform, false);
                _visual = go.AddComponent<SpriteRenderer>();
            }

            _visual.sortingOrder = 5;
            var root = GetComponent<SpriteRenderer>();
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

            float parentScale = Mathf.Abs(transform.lossyScale.y);
            if (parentScale < 0.0001f)
            {
                parentScale = 1f;
            }

            float spriteHeight = sprite.bounds.size.y;
            float localScale = spriteHeight > 0.001f ? VisualHeight / (spriteHeight * parentScale) : 1f;
            _visual.transform.localScale = new Vector3(localScale, localScale, 1f);
            float centerX = sprite.bounds.center.x * localScale;
            float centerY = sprite.bounds.center.y * localScale;
            float x = _visual.flipX ? centerX : -centerX;
            _visual.transform.localPosition = new Vector3(x, -centerY, 0f);
        }
    }
}

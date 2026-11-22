using System.Collections;
using UnityEngine;

namespace Pulling
{
    public class SwitchPlate : MonoBehaviour
    {
        const float ToggleCooldown = 0.4f;
        const float SquashSeconds = 0.08f;
        const float ShrinkSeconds = 0.22f;
        const float OffAngle = 38f;
        const float OnAngle = -38f;
        const float FlipSeconds = 0.1f;
        static readonly Color SensorOff = new Color(1f, 0.78f, 0.22f, 1f);
        static readonly Color SensorOn = new Color(1f, 0.98f, 0.55f, 1f);

        GridMapBuilder _builder;
        SpriteRenderer _renderer;
        BoxCollider2D _collider;
        Transform _lever;
        int _x;
        int _y;
        bool _toggle;
        bool _light;
        bool _lit;
        bool _detached;
        bool _consumed;
        float _unlockTime;
        float _shownAngle = OffAngle;
        float _fromAngle = OffAngle;
        float _toAngle = OffAngle;
        float _flipT = 1f;
        Vector3 _sensorScale = Vector3.one;

        public bool IsLit => _lit;

        public void Bind(GridMapBuilder builder, int x, int y, bool toggle, Color color, bool light = false)
        {
            _builder = builder;
            _x = x;
            _y = y;
            _toggle = toggle && !light;
            _light = light;
            _renderer = GetComponent<SpriteRenderer>();
            _collider = GetComponent<BoxCollider2D>();
            if (_toggle)
            {
                SetupLever(color);
                builder?.RegisterToggle(this);
            }
            else if (_light)
            {
                SetupSensor(color);
                builder?.RegisterLight(this);
                if (builder != null && !builder.DeferSensors)
                {
                    CatchLight();
                }
            }
        }

        void OnDestroy()
        {
            if (_toggle)
            {
                _builder?.UnregisterToggle(this);
            }

            DetachLight();
        }

        public void DetachLight()
        {
            if (!_light || _detached)
            {
                return;
            }

            _detached = true;
            _builder?.UnregisterLight(this);
        }

        public void CatchLight()
        {
            if (!_light || _detached || _builder == null || _builder.IsEditing)
            {
                return;
            }

            bool lit = SunlightZone.IsInSkyLight(_builder, _builder.tileDatabase, _x, _y);
            if (lit == _lit)
            {
                return;
            }

            _lit = lit;
            _builder.NotifyLight(_lit);
        }

        void OnCollisionEnter2D(Collision2D collision)
        {
            TryPress(collision.collider);
        }

        public void ApplyDoorState(bool open)
        {
            if (!_toggle || _consumed)
            {
                return;
            }

            _fromAngle = _shownAngle;
            _toAngle = open ? OnAngle : OffAngle;
            _flipT = 0f;
        }

        void Update()
        {
            if (_light)
            {
                CatchLight();
                if (_renderer != null)
                {
                    float wave = _lit ? 0.9f + Mathf.Sin(Time.unscaledTime * 5.5f) * 0.1f : 1f;
                    Color tint = _lit ? SensorOn : SensorOff;
                    tint.a *= wave;
                    _renderer.color = tint;
                    transform.localScale = _sensorScale * (_lit ? 0.94f + wave * 0.1f : 1f);
                }

                return;
            }

            if (_lever == null || _flipT >= 1f)
            {
                return;
            }

            _flipT = Mathf.Min(1f, _flipT + Time.unscaledDeltaTime / FlipSeconds);
            float eased = Mathf.SmoothStep(0f, 1f, _flipT);
            _shownAngle = Mathf.LerpAngle(_fromAngle, _toAngle, eased);
            _lever.localRotation = Quaternion.Euler(0f, 0f, _shownAngle);
        }

        void TryPress(Collider2D other)
        {
            if (_light || _consumed || _builder == null || _builder.IsEditing || other == null)
            {
                return;
            }

            if (other.GetComponent<PlayerController>() == null)
            {
                return;
            }

            if (_toggle)
            {
                if (Time.time < _unlockTime)
                {
                    return;
                }

                _unlockTime = Time.time + ToggleCooldown;
                _builder.ToggleDoors();
                return;
            }

            _consumed = true;
            if (_collider != null)
            {
                _collider.enabled = false;
            }

            _builder.SetDoorsOpen(true);
            StartCoroutine(Vanish());
        }

        void SetupSensor(Color color)
        {
            _sensorScale = transform.localScale * 0.9f;
            transform.localScale = _sensorScale;
            if (_renderer != null)
            {
                _renderer.sprite = SpriteFactory.CreateLightSensor(color);
                _renderer.color = SensorOff;
            }
        }

        void SetupLever(Color color)
        {
            if (_renderer != null)
            {
                _renderer.sprite = SpriteFactory.CreateLeverBase(color);
                _renderer.color = Color.white;
            }

            var leverObject = new GameObject("Lever");
            leverObject.transform.SetParent(transform, false);
            leverObject.transform.localPosition = new Vector3(0f, -0.18f, 0f);
            leverObject.transform.localRotation = Quaternion.Euler(0f, 0f, OffAngle);
            var leverRenderer = leverObject.AddComponent<SpriteRenderer>();
            leverRenderer.sprite = SpriteFactory.CreateLeverHandle(color);
            leverRenderer.sortingOrder = _renderer != null ? _renderer.sortingOrder + 1 : 2;
            _lever = leverObject.transform;
            _shownAngle = OffAngle;
        }

        IEnumerator Vanish()
        {
            Transform body = transform;
            Vector3 rest = body.localScale;
            Color color = _renderer != null ? _renderer.color : Color.white;
            float elapsed = 0f;
            while (elapsed < SquashSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(elapsed / SquashSeconds);
                body.localScale = new Vector3(
                    rest.x * Mathf.Lerp(1f, 1.12f, u),
                    rest.y * Mathf.Lerp(1f, 0.7f, u),
                    rest.z);
                yield return null;
            }

            Vector3 from = body.localScale;
            elapsed = 0f;
            while (elapsed < ShrinkSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(elapsed / ShrinkSeconds);
                float ease = u * u;
                body.localScale = Vector3.Lerp(from, Vector3.zero, ease);
                if (_renderer != null)
                {
                    color.a = 1f - ease;
                    _renderer.color = color;
                }

                yield return null;
            }

            if (_builder != null && _builder.GetCell(_x, _y) == TileIds.Switch)
            {
                _builder.SetCell(_x, _y, TileIds.Empty);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}

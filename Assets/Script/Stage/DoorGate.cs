using System.Collections;
using UnityEngine;

namespace Pulling
{
    public class DoorGate : MonoBehaviour
    {
        const float SquashSeconds = 0.1f;
        const float ShrinkSeconds = 0.32f;
        const float TotalSeconds = SquashSeconds + ShrinkSeconds;

        GridMapBuilder _builder;
        SpriteRenderer _renderer;
        BoxCollider2D _collider;
        Vector3 _restScale = Vector3.one;
        Color _baseColor = Color.white;
        int _x;
        int _y;
        int _epoch;
        int _direction;
        float _time;
        bool _running;

        public int X => _x;
        public int Y => _y;
        public bool IsOpening => _direction != 0 || _time > 0.001f;

        public void Bind(GridMapBuilder builder, int x, int y)
        {
            _builder = builder;
            _x = x;
            _y = y;
            _renderer = GetComponent<SpriteRenderer>();
            _collider = GetComponent<BoxCollider2D>();
            _restScale = transform.localScale;
            if (_renderer != null)
            {
                _baseColor = _renderer.color;
            }

            builder?.RegisterDoor(this);
        }

        void OnDestroy()
        {
            _builder?.UnregisterDoor(this);
        }

        public void Open()
        {
            if (_time >= TotalSeconds && _direction >= 0)
            {
                return;
            }

            _epoch = _builder != null ? _builder.DoorEpoch : 0;
            Begin(1);
        }

        public void PlayReverse()
        {
            if (_time <= 0f)
            {
                return;
            }

            Begin(-1);
        }

        public void PlayReverseFromHidden()
        {
            _time = TotalSeconds;
            Apply(_time);
            Begin(-1);
        }

        void Begin(int direction)
        {
            _direction = direction;
            if (_collider != null)
            {
                _collider.enabled = false;
            }

            if (!_running)
            {
                StartCoroutine(Playback());
            }
        }

        IEnumerator Playback()
        {
            _running = true;
            while (_direction != 0)
            {
                _time += _direction * Time.unscaledDeltaTime;
                if (_direction > 0 && _time >= TotalSeconds)
                {
                    _time = TotalSeconds;
                    Apply(_time);
                    _direction = 0;
                    CommitOpen();
                    break;
                }

                if (_direction < 0 && _time <= 0f)
                {
                    _time = 0f;
                    Apply(_time);
                    _direction = 0;
                    if (_collider != null)
                    {
                        _collider.enabled = true;
                    }

                    break;
                }

                Apply(_time);
                yield return null;
            }

            _running = false;
        }

        void Apply(float time)
        {
            float t = Mathf.Clamp(time, 0f, TotalSeconds);
            Vector3 scale;
            float alpha;
            if (t <= SquashSeconds)
            {
                float u = SquashSeconds <= 0f ? 1f : t / SquashSeconds;
                scale = new Vector3(
                    _restScale.x * Mathf.Lerp(1f, 1.14f, u),
                    _restScale.y * Mathf.Lerp(1f, 0.7f, u),
                    _restScale.z);
                alpha = 1f;
            }
            else
            {
                float u = Mathf.Clamp01((t - SquashSeconds) / ShrinkSeconds);
                float ease = u * u;
                var squashed = new Vector3(_restScale.x * 1.14f, _restScale.y * 0.7f, _restScale.z);
                scale = Vector3.Lerp(squashed, Vector3.zero, ease);
                alpha = 1f - ease;
            }

            transform.localScale = scale;
            if (_renderer != null)
            {
                var color = _baseColor;
                color.a = alpha;
                _renderer.color = color;
            }
        }

        void CommitOpen()
        {
            if (_builder != null && _builder.DoorEpoch == _epoch && _builder.GetCell(_x, _y) == TileIds.Door)
            {
                _builder.SetCell(_x, _y, TileIds.Empty);
            }
        }
    }
}

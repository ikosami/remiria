using UnityEngine;

namespace Pulling
{
    public class AimLineView : MonoBehaviour
    {
        const float ArrowLength = 1.25f;

        [SerializeField] Sprite _arrowSprite;

        LineRenderer _line;
        SpriteRenderer _arrow;

        public void SetArrow(Sprite sprite)
        {
            if (sprite != null)
            {
                _arrowSprite = sprite;
            }

            EnsureArrow();
            if (_arrow != null && _arrowSprite != null)
            {
                _arrow.sprite = _arrowSprite;
            }

            ApplyArrowScale();
        }

        public void Bind(LineRenderer line)
        {
            _line = line;
            if (_line != null)
            {
                _line.enabled = false;
            }

            EnsureArrow();
            Hide();
        }

        public void ShowDirection(Vector2 origin, Vector2 direction)
        {
            if (_arrow == null || _arrow.sprite == null || direction.sqrMagnitude < 0.0001f)
            {
                Hide();
                return;
            }

            if (_line != null)
            {
                _line.enabled = false;
            }

            ApplyArrowScale();
            Vector2 dir = direction.normalized;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            var rotation = Quaternion.Euler(0f, 0f, angle);
            float scale = _arrow.transform.localScale.x;
            var tail = new Vector3(_arrow.sprite.bounds.min.x, _arrow.sprite.bounds.center.y, 0f) * scale;
            _arrow.transform.SetPositionAndRotation((Vector3)origin - rotation * tail, rotation);
            _arrow.enabled = true;
        }

        public void Hide()
        {
            if (_line != null)
            {
                _line.enabled = false;
            }

            if (_arrow != null)
            {
                _arrow.enabled = false;
            }
        }

        void EnsureArrow()
        {
            if (_arrow == null)
            {
                var existing = transform.Find("Arrow");
                if (existing != null)
                {
                    _arrow = existing.GetComponent<SpriteRenderer>();
                }
            }

            if (_arrow == null)
            {
                var go = new GameObject("Arrow");
                go.transform.SetParent(transform, false);
                _arrow = go.AddComponent<SpriteRenderer>();
            }

            _arrow.sortingOrder = 6;
            if (_arrowSprite != null)
            {
                _arrow.sprite = _arrowSprite;
            }

            ApplyArrowScale();
        }

        void ApplyArrowScale()
        {
            if (_arrow == null || _arrow.sprite == null)
            {
                return;
            }

            float width = _arrow.sprite.bounds.size.x;
            float scale = width > 0.001f ? ArrowLength / width : 1f;
            _arrow.transform.localScale = new Vector3(scale, scale, 1f);
        }
    }
}

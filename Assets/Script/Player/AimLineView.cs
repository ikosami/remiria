using UnityEngine;
using UnityEngine.UI;

namespace Pulling
{
    public class AimLineView : MonoBehaviour
    {
        const float ArrowLength = 1.25f;
        const float ArrowGap = 0.35f;

        Remiria _remiria;

        const int OriginTexSize = 256;

        static Sprite _originSprite;

        LineRenderer _line;
        Transform _pivot;
        RectTransform _canvas;
        Image _back;
        Image _fill;
        Image _outline;
        SpriteRenderer _origin;

        public void Apply(Remiria remiria)
        {
            if (remiria != null)
            {
                _remiria = remiria;
            }

            EnsureArrow();
            ApplySprites();
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

        public void ShowPullOrigin(Vector2 world, float radius)
        {
            EnsureOrigin();
            _origin.enabled = true;
            _origin.transform.position = new Vector3(world.x, world.y, 0f);
            float diameter = Mathf.Max(0.01f, radius) * 2f;
            _origin.transform.localScale = new Vector3(diameter, diameter, 1f);
        }

        public void HideDirection()
        {
            if (_line != null)
            {
                _line.enabled = false;
            }

            SetVisible(_back, false);
            SetVisible(_fill, false);
            SetVisible(_outline, false);
        }

        public void ShowDirection(Vector2 origin, Vector2 direction, float strength)
        {
            if (_pivot == null || direction.sqrMagnitude < 0.0001f || !HasSprite())
            {
                HideDirection();
                return;
            }

            if (_line != null)
            {
                _line.enabled = false;
            }

            ApplyArrowScale();
            Vector2 dir = direction.normalized;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
            var rotation = Quaternion.Euler(0f, 0f, angle);
            float scale = _pivot.localScale.x;
            var tail = new Vector3(0f, TailY(), 0f) * scale;
            Vector3 start = (Vector3)(origin + dir * ArrowGap);
            _pivot.SetPositionAndRotation(start - rotation * tail, rotation);
            SetVisible(_back, _back != null && _back.sprite != null);
            if (_fill != null)
            {
                _fill.fillAmount = Mathf.Clamp01(strength);
                SetVisible(_fill, _fill.sprite != null);
            }

            SetVisible(_outline, _outline != null && _outline.sprite != null);
        }

        public void Hide()
        {
            HideDirection();
            if (_origin != null)
            {
                _origin.enabled = false;
            }
        }

        bool HasSprite()
        {
            return (_back != null && _back.sprite != null)
                   || (_fill != null && _fill.sprite != null)
                   || (_outline != null && _outline.sprite != null);
        }

        void EnsureArrow()
        {
            if (_pivot == null)
            {
                var existing = transform.Find("Arrow");
                if (existing != null)
                {
                    _pivot = existing;
                }
            }

            if (_pivot == null)
            {
                var go = new GameObject("Arrow");
                go.transform.SetParent(transform, false);
                _pivot = go.transform;
            }

            var leftover = _pivot.GetComponent<SpriteRenderer>();
            if (leftover != null)
            {
                leftover.enabled = false;
            }

            _canvas = GetOrAddCanvas(_pivot);
            _back = GetOrAddImage(_canvas, "Back");
            _fill = GetOrAddImage(_canvas, "Fill");
            _outline = GetOrAddImage(_canvas, "Outline");
            _fill.type = Image.Type.Filled;
            _fill.fillMethod = Image.FillMethod.Vertical;
            _fill.fillOrigin = (int)Image.OriginVertical.Bottom;
            ApplySprites();
            ApplyArrowScale();
        }

        static RectTransform GetOrAddCanvas(Transform pivot)
        {
            var existing = pivot.Find("ArrowCanvas");
            GameObject go;
            if (existing != null)
            {
                go = existing.gameObject;
            }
            else
            {
                go = new GameObject("ArrowCanvas");
                go.transform.SetParent(pivot, false);
            }

            var canvas = go.GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = go.AddComponent<Canvas>();
            }

            canvas.renderMode = RenderMode.WorldSpace;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 6;

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.localPosition = Vector3.zero;
            rect.localRotation = Quaternion.identity;
            return rect;
        }

        Image GetOrAddImage(Transform parent, string name)
        {
            var child = parent.Find(name);
            if (child == null && _pivot != null)
            {
                var stray = _pivot.Find(name);
                if (stray != null)
                {
                    stray.SetParent(parent, false);
                    child = stray;
                }
            }

            if (child == null)
            {
                var go = new GameObject(name);
                go.transform.SetParent(parent, false);
                child = go.transform;
            }

            var renderer = child.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                Destroy(renderer);
            }

            var image = child.GetComponent<Image>();
            if (image == null)
            {
                image = child.gameObject.AddComponent<Image>();
            }

            image.raycastTarget = false;
            image.maskable = false;
            return image;
        }

        void ApplySprites()
        {
            if (_remiria == null)
            {
                return;
            }

            Layout(_back, _remiria.arrowFillBack);
            Layout(_fill, _remiria.arrowFill);
            Layout(_outline, _remiria.arrowOutline);
        }

        static void Layout(Image image, Sprite sprite)
        {
            if (image == null || sprite == null)
            {
                return;
            }

            image.sprite = sprite;
            image.color = Color.white;
            image.preserveAspect = false;
            var rect = image.rectTransform;
            Vector2 size = sprite.rect.size;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.pivot = new Vector2(
                size.x > 0.01f ? sprite.pivot.x / size.x : 0.5f,
                size.y > 0.01f ? sprite.pivot.y / size.y : 0.5f);
            rect.localPosition = Vector3.zero;
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;
        }

        void ApplyArrowScale()
        {
            if (_pivot == null)
            {
                return;
            }

            Sprite sprite = ReferenceSprite();
            float ppu = sprite != null && sprite.pixelsPerUnit > 0.001f ? sprite.pixelsPerUnit : 100f;
            if (_canvas != null)
            {
                _canvas.localScale = Vector3.one / ppu;
            }

            float length = 0f;
            if (sprite != null)
            {
                length = sprite.rect.height / ppu;
            }

            float scale = length > 0.001f ? ArrowLength / length : 1f;
            _pivot.localScale = new Vector3(scale, scale, 1f);
        }

        float TailY()
        {
            Sprite sprite = ReferenceSprite();
            if (sprite == null)
            {
                return 0f;
            }

            float ppu = sprite.pixelsPerUnit > 0.001f ? sprite.pixelsPerUnit : 100f;
            return -sprite.pivot.y / ppu;
        }

        Sprite ReferenceSprite()
        {
            if (_remiria == null)
            {
                return null;
            }

            if (_remiria.arrowOutline != null)
            {
                return _remiria.arrowOutline;
            }

            if (_remiria.arrowFillBack != null)
            {
                return _remiria.arrowFillBack;
            }

            return _remiria.arrowFill;
        }

        void EnsureOrigin()
        {
            if (_origin != null)
            {
                return;
            }

            var existing = transform.Find("PullOrigin");
            GameObject go;
            if (existing != null)
            {
                go = existing.gameObject;
            }
            else
            {
                go = new GameObject("PullOrigin");
                go.transform.SetParent(transform, false);
            }

            _origin = go.GetComponent<SpriteRenderer>();
            if (_origin == null)
            {
                _origin = go.AddComponent<SpriteRenderer>();
            }

            _origin.sprite = OriginSprite();
            _origin.color = Color.white;
            _origin.sortingOrder = 4;
            _origin.enabled = false;
            SpriteFactory.AssignPipelineMaterial(_origin);
        }

        static Sprite OriginSprite()
        {
            if (_originSprite != null)
            {
                return _originSprite;
            }

            int size = OriginTexSize;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                name = "PullOrigin",
                hideFlags = HideFlags.HideAndDontSave
            };

            var fill = new Color(0.72f, 0.88f, 1f, 0.1f);
            var rim = new Color(0.85f, 0.94f, 1f, 0.7f);
            var dot = new Color(1f, 1f, 1f, 0.9f);
            Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            float radius = size * 0.5f - 1.5f;
            float rimPx = 1.15f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float px = Vector2.Distance(new Vector2(x, y), center);
                    float dist = px / radius;
                    Color color = Color.clear;
                    if (dist <= 0.028f)
                    {
                        color = dot;
                    }
                    else if (px <= radius)
                    {
                        float rimT = Mathf.Clamp01((radius - px) / rimPx);
                        color = Color.Lerp(rim, fill, rimT);
                    }

                    tex.SetPixel(x, y, color);
                }
            }

            tex.Apply();
            _originSprite = Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
            _originSprite.name = "PullOrigin";
            _originSprite.hideFlags = HideFlags.HideAndDontSave;
            return _originSprite;
        }

        static void SetVisible(Image image, bool visible)
        {
            if (image != null)
            {
                image.enabled = visible;
            }
        }
    }
}

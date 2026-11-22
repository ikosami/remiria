using UnityEngine;
using UnityEngine.UI;

namespace Pulling
{
    public class LowHealthVignette : MonoBehaviour
    {
        const float ShowBelow = 0.8f;
        const float MaxAlpha = 0.82f;
        const float PulseStart = 0.45f;
        const float PulseDepth = 0.28f;
        const float PulseSpeed = 7.2f;
        public Image image;
        float _ratio = 1f;
        bool _shown = true;

        void Awake()
        {
            StretchToScreen();
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            image.raycastTarget = false;
            image.canvasRenderer.cullTransparentMesh = false;

            var shader = Resources.Load<Shader>("LowHealthVignette");
            if (shader != null)
            {
                image.sprite = null;
                image.material = new Material(shader)
                {
                    name = "LowHealthVignette (runtime)",
                    hideFlags = HideFlags.HideAndDontSave
                };
            }
            else
            {
                image.sprite = CreateEdgeSprite();
                Debug.LogWarning("LowHealthVignette シェーダーが見つからないため、テクスチャで縁を描きます。");
            }

            Apply(0f);
        }

        void StretchToScreen()
        {
            var rect = (RectTransform)transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
        }

        public void SetRatio(float ratio)
        {
            _ratio = Mathf.Clamp01(ratio);
        }

        public void SetShown(bool shown)
        {
            _shown = shown;
            if (!shown)
            {
                Apply(0f);
            }
        }

        void Update()
        {
            if (!_shown)
            {
                return;
            }

            float danger = Mathf.InverseLerp(ShowBelow, 0f, _ratio);
            danger = Mathf.SmoothStep(0f, 1f, danger);
            float pulse = 1f;
            if (danger > PulseStart)
            {
                float amount = Mathf.InverseLerp(PulseStart, 1f, danger) * PulseDepth;
                float wave = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * PulseSpeed);
                pulse = 1f - amount * (1f - wave);
            }

            Apply(danger * pulse * MaxAlpha);
        }

        void Apply(float alpha)
        {
            var color = image.color;
            color.a = alpha;
            image.color = color;
            image.enabled = alpha > 0.01f;
        }

        static Sprite CreateEdgeSprite()
        {
            const int size = 256;
            const float edge = 0.2f;
            const float power = 1.7f;
            float aspect = Screen.height > 0 ? (float)Screen.width / Screen.height : 16f / 9f;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave,
                name = "HealthEdge"
            };

            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = (x + 0.5f) / size;
                    float v = (y + 0.5f) / size;
                    float dx = Mathf.Min(u, 1f - u) * aspect;
                    float dy = Mathf.Min(v, 1f - v);
                    float rim = Mathf.Clamp01(1f - Mathf.Min(dx, dy) / edge);
                    rim = Mathf.Pow(rim, power);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, rim);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            var sprite = Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }
    }
}

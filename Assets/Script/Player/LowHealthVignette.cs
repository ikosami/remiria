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
        static readonly Color RimColor = new Color(0.86f, 0.02f, 0.05f, 0f);

        Image _image;
        float _ratio = 1f;
        bool _shown = true;

        public void Bind(Image image)
        {
            _image = image;
            Apply(0f);
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
            if (_image == null || !_shown)
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
            if (_image == null)
            {
                return;
            }

            var color = RimColor;
            color.a = alpha;
            _image.color = color;
            _image.enabled = alpha > 0.01f;
        }
    }
}

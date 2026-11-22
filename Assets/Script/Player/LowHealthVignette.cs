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
    }
}

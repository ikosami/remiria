using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Pulling
{
    public class StageTransition : MonoBehaviour
    {
        public float fadeSeconds = 2f;
        Image _overlay;

        public void Bind(Image overlay)
        {
            _overlay = overlay;
            SetAlpha(0f);
            if (_overlay != null)
            {
                _overlay.raycastTarget = false;
            }
        }

        public IEnumerator Play(Action buildNext)
        {
            if (_overlay != null)
            {
                _overlay.raycastTarget = true;
            }

            yield return Fade(0f, 1f, fadeSeconds);
            buildNext?.Invoke();
            yield return Fade(1f, 0f, fadeSeconds);

            if (_overlay != null)
            {
                _overlay.raycastTarget = false;
            }
        }

        IEnumerator Fade(float from, float to, float duration)
        {
            float elapsed = 0f;
            SetAlpha(from);
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
                SetAlpha(Mathf.Lerp(from, to, t));
                yield return null;
            }

            SetAlpha(to);
        }

        void SetAlpha(float alpha)
        {
            if (_overlay == null)
            {
                return;
            }

            var color = _overlay.color;
            color.a = alpha;
            _overlay.color = color;
            _overlay.enabled = alpha > 0.001f;
        }
    }
}

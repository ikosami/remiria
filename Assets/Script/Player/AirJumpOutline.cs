using UnityEngine;

namespace Pulling
{
    public class AirJumpOutline
    {
        static readonly Vector2[] Directions =
        {
            new Vector2(1f, 0f),
            new Vector2(-1f, 0f),
            new Vector2(0f, 1f),
            new Vector2(0f, -1f),
            new Vector2(0.70710678f, 0.70710678f),
            new Vector2(-0.70710678f, 0.70710678f),
            new Vector2(0.70710678f, -0.70710678f),
            new Vector2(-0.70710678f, -0.70710678f)
        };

        readonly SpriteRenderer[] _renderers = new SpriteRenderer[Directions.Length];
        Transform _root;
        Material _material;
        bool _missingShader;

        public void Apply(SpriteRenderer source, bool visible)
        {
            if (source == null)
            {
                return;
            }

            Ensure(source.transform);
            if (_root == null)
            {
                return;
            }

            if (_root.gameObject.activeSelf != visible)
            {
                _root.gameObject.SetActive(visible);
            }

            if (!visible)
            {
                return;
            }

            float scale = Mathf.Abs(source.transform.lossyScale.x);
            float radius = scale > 0.0001f ? PlayerTuning.AirJumpOutlineThickness / scale : 0f;
            float mirrorX = source.flipX ? -1f : 1f;
            float mirrorY = source.flipY ? -1f : 1f;
            _root.localScale = new Vector3(mirrorX, mirrorY, 1f);
            for (int i = 0; i < _renderers.Length; i++)
            {
                SpriteRenderer renderer = _renderers[i];
                renderer.sprite = source.sprite;
                renderer.flipX = false;
                renderer.flipY = false;
                renderer.sortingLayerID = source.sortingLayerID;
                renderer.sortingOrder = source.sortingOrder - 1;
                renderer.color = Color.white;
                renderer.transform.localPosition = Directions[i] * radius;
            }
        }

        void Ensure(Transform visual)
        {
            if (_root != null || _missingShader)
            {
                return;
            }

            Shader shader = Resources.Load<Shader>("SpriteWhiteSilhouette");
            if (shader == null)
            {
                shader = Shader.Find("Pulling/SpriteWhiteSilhouette");
            }

            if (shader == null)
            {
                _missingShader = true;
                Debug.LogWarning("白枠シェーダーが見つかりません。");
                return;
            }

            _material = new Material(shader)
            {
                name = "SpriteWhiteSilhouette (runtime)",
                hideFlags = HideFlags.HideAndDontSave
            };
            _material.SetColor("_OutlineColor", Color.white);

            var rootGo = new GameObject("AirJumpOutline");
            rootGo.hideFlags = HideFlags.HideAndDontSave;
            _root = rootGo.transform;
            _root.SetParent(visual, false);

            for (int i = 0; i < _renderers.Length; i++)
            {
                var go = new GameObject("Edge");
                go.hideFlags = HideFlags.HideAndDontSave;
                go.transform.SetParent(_root, false);
                var renderer = go.AddComponent<SpriteRenderer>();
                renderer.sharedMaterial = _material;
                renderer.color = Color.white;
                _renderers[i] = renderer;
            }
        }
    }
}

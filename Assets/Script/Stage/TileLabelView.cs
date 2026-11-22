using UnityEngine;

namespace Pulling
{
    public class TileLabelView : MonoBehaviour
    {
        public TextMesh labelMesh;
        public SpecialTileMarker marker;

        Material _material;

        public void SetLabel(string text)
        {
            string value = text ?? string.Empty;
            labelMesh.text = value;
            labelMesh.characterSize = FitCharacterSize(value, labelMesh.fontSize);
            labelMesh.color = Color.black;
            ApplyFontMaterial();
        }

        void LateUpdate()
        {
            // TextMesh は文字更新のたびにビルトインのフォントマテリアルへ戻す。
            // URP ではそのシェーダーが頂点色を無視して白く出る。
            ApplyFontMaterial();
        }

        void OnDestroy()
        {
            if (_material != null)
            {
                Destroy(_material);
            }
        }

        void ApplyFontMaterial()
        {
            if (labelMesh == null)
            {
                return;
            }

            var renderer = labelMesh.GetComponent<MeshRenderer>();
            var font = labelMesh.font;
            if (renderer == null || font == null || font.material == null)
            {
                return;
            }

            if (_material == null)
            {
                Shader shader = Resources.Load<Shader>("TileLabel");
                if (shader == null)
                {
                    shader = Shader.Find("Pulling/TileLabel");
                }

                if (shader == null)
                {
                    return;
                }

                _material = new Material(shader)
                {
                    name = "TileLabel (runtime)",
                    hideFlags = HideFlags.HideAndDontSave
                };
            }

            Texture texture = font.material.mainTexture;
            if (renderer.sharedMaterial == _material && _material.mainTexture == texture)
            {
                return;
            }

            _material.mainTexture = texture;
            _material.color = Color.black;
            renderer.sharedMaterial = _material;
        }

        static float FitCharacterSize(string text, int fontSize)
        {
            float width = 0f;
            if (!string.IsNullOrEmpty(text))
            {
                for (int i = 0; i < text.Length; i++)
                {
                    width += text[i] <= 0x7F ? 0.55f : 1f;
                }
            }

            if (width < 1f)
            {
                width = 1f;
            }

            // TextMesh の字高はおよそ fontSize * characterSize * 0.1。1マスに収める。
            float em = Mathf.Min(0.42f, 0.9f / width);
            return em * 10f / fontSize;
        }
    }
}


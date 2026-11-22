using UnityEngine;

namespace Pulling
{
    public class TileLabelView : MonoBehaviour
    {
        public TextMesh labelMesh;
        public SpecialTileMarker marker;

        public void SetLabel(string text)
        {
            string value = text ?? string.Empty;
            labelMesh.text = value;
            labelMesh.characterSize = FitCharacterSize(value, labelMesh.fontSize);
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


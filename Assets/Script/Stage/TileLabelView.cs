using UnityEngine;

namespace Pulling
{
    public class TileLabelView : MonoBehaviour
    {
        public void SetLabel(string text, Color color)
        {
            var mesh = GetComponent<TextMesh>();
            if (mesh == null)
            {
                mesh = gameObject.AddComponent<TextMesh>();
            }

            string value = text ?? string.Empty;
            mesh.text = value;
            mesh.font = UiFonts.Default;
            mesh.fontSize = 64;
            mesh.characterSize = FitCharacterSize(value, 64);
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.color = color;
            mesh.fontStyle = FontStyle.Bold;

            var renderer = GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sortingOrder = 3;
            }
        }

        public static void Attach(Transform parent, string text, Color color)
        {
            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(parent, false);
            labelGo.transform.localPosition = Vector3.zero;
            var view = labelGo.AddComponent<TileLabelView>();
            view.SetLabel(text, color);
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


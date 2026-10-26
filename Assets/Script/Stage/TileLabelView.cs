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

            mesh.text = text ?? string.Empty;
            mesh.fontSize = 48;
            mesh.characterSize = 0.08f;
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
            view.SetLabel(text, Color.white);
            view.SetLabel(text, color);
        }
    }
}


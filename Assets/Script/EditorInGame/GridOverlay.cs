using UnityEngine;

namespace Pulling
{
    public class GridOverlay : MonoBehaviour
    {
        int _width;
        int _height;
        float _cellSize = 1f;
        bool _visible;
        Material _material;
        Transform _hover;

        public void Bind(Transform hover)
        {
            _hover = hover;
            if (_hover != null)
            {
                _hover.gameObject.SetActive(false);
            }
        }

        public void Show(int width, int height, float cellSize)
        {
            _width = width;
            _height = height;
            _cellSize = cellSize;
            _visible = true;
        }

        public void Hide()
        {
            _visible = false;
            if (_hover != null)
            {
                _hover.gameObject.SetActive(false);
            }
        }

        public void SetHover(bool on, Vector3 center, float cellSize)
        {
            if (_hover == null)
            {
                return;
            }

            _hover.gameObject.SetActive(on && _visible);
            if (on)
            {
                _hover.position = center;
                _hover.localScale = new Vector3(cellSize, cellSize, 1f);
            }
        }

        void OnRenderObject()
        {
            if (!_visible)
            {
                return;
            }

            if (_material == null)
            {
                var shader = Shader.Find("Hidden/Internal-Colored");
                if (shader == null)
                {
                    shader = Shader.Find("Sprites/Default");
                }

                _material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            }

            _material.SetPass(0);
            GL.PushMatrix();
            GL.MultMatrix(Matrix4x4.identity);
            GL.Begin(GL.LINES);
            GL.Color(new Color(1f, 1f, 1f, 0.28f));

            float w = _width * _cellSize;
            float h = _height * _cellSize;
            for (int x = 0; x <= _width; x++)
            {
                float px = x * _cellSize;
                GL.Vertex3(px, 0f, 0f);
                GL.Vertex3(px, h, 0f);
            }

            for (int y = 0; y <= _height; y++)
            {
                float py = y * _cellSize;
                GL.Vertex3(0f, py, 0f);
                GL.Vertex3(w, py, 0f);
            }

            GL.End();
            GL.PopMatrix();
        }

        void OnDestroy()
        {
            if (_material != null)
            {
                Destroy(_material);
            }
        }
    }
}

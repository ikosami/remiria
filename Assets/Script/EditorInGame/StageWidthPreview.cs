using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace Pulling
{
    public class StageWidthPreview : MonoBehaviour
    {
        const int TextureWidth = 640;
        const int TextureHeight = 144;

        Camera _gameplay;
        Camera _camera;
        RenderTexture _target;
        RawImage _image;
        Text _caption;
        int _cells = 1;
        float _cellSize = 1f;

        public static StageWidthPreview Create(Transform column, int siblingIndex)
        {
            var rootGo = new GameObject("WidthPreview", typeof(RectTransform));
            rootGo.transform.SetParent(column, false);
            rootGo.transform.SetSiblingIndex(siblingIndex);
            var root = (RectTransform)rootGo.transform;
            var rootLayout = rootGo.AddComponent<LayoutElement>();
            rootLayout.minHeight = 132f;
            rootLayout.preferredHeight = 132f;
            rootLayout.flexibleHeight = 0f;
            rootLayout.flexibleWidth = 1f;
            var vertical = rootGo.AddComponent<VerticalLayoutGroup>();
            vertical.spacing = 4f;
            vertical.childAlignment = TextAnchor.UpperLeft;
            vertical.childForceExpandWidth = true;
            vertical.childForceExpandHeight = false;
            vertical.childControlWidth = true;
            vertical.childControlHeight = true;

            var viewGo = new GameObject("View", typeof(RectTransform));
            viewGo.transform.SetParent(root, false);
            var viewLayout = viewGo.AddComponent<LayoutElement>();
            viewLayout.minHeight = 100f;
            viewLayout.preferredHeight = 100f;
            viewLayout.flexibleWidth = 1f;
            viewLayout.flexibleHeight = 0f;
            var frame = viewGo.AddComponent<Image>();
            frame.color = new Color(0.05f, 0.06f, 0.08f, 1f);
            frame.raycastTarget = false;

            var imageGo = new GameObject("CameraView", typeof(RectTransform));
            imageGo.transform.SetParent(viewGo.transform, false);
            Stretch((RectTransform)imageGo.transform);
            var image = imageGo.AddComponent<RawImage>();
            image.color = Color.white;
            image.raycastTarget = false;

            var captionGo = new GameObject("Caption", typeof(RectTransform));
            captionGo.transform.SetParent(root, false);
            var captionLayout = captionGo.AddComponent<LayoutElement>();
            captionLayout.minHeight = 28f;
            captionLayout.preferredHeight = 28f;
            captionLayout.flexibleHeight = 0f;
            var caption = captionGo.AddComponent<Text>();
            caption.font = UiFonts.Default;
            caption.fontSize = 16;
            caption.alignment = TextAnchor.MiddleLeft;
            caption.color = new Color(1f, 1f, 1f, 0.86f);
            caption.horizontalOverflow = HorizontalWrapMode.Wrap;
            caption.verticalOverflow = VerticalWrapMode.Truncate;
            caption.raycastTarget = false;
            caption.text = string.Empty;

            var preview = rootGo.AddComponent<StageWidthPreview>();
            preview._image = image;
            preview._caption = caption;
            preview.BuildCamera();
            return preview;
        }

        public void SetSource(Camera gameplay, float cellSize, int cells)
        {
            _gameplay = gameplay;
            _cellSize = cellSize > 0f ? cellSize : 1f;
            _cells = Mathf.Clamp(cells, 1, 256);
            Frame();
        }

        void OnDisable()
        {
            if (_camera != null)
            {
                _camera.enabled = false;
            }
        }

        void OnDestroy()
        {
            if (_camera != null)
            {
                Destroy(_camera.gameObject);
            }

            if (_target != null)
            {
                _target.Release();
                Destroy(_target);
            }
        }

        void LateUpdate() => Frame();

        void Frame()
        {
            if (!isActiveAndEnabled || _camera == null || _gameplay == null)
            {
                return;
            }

            float stageWidth = _cells * _cellSize;
            float aspect = TextureHeight > 0 ? (float)TextureWidth / TextureHeight : _camera.aspect;
            if (aspect < 0.01f)
            {
                aspect = 1f;
            }

            Vector3 position = _gameplay.transform.position;
            position.x = stageWidth * 0.5f;
            _camera.transform.position = position;
            _camera.orthographicSize = stageWidth / (2f * aspect);
            Color background = _gameplay.backgroundColor;
            background.a = 1f;
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = background;
            _camera.cullingMask = _gameplay.cullingMask;
            _camera.enabled = true;

            if (_caption != null)
            {
                float visibleWidth = _gameplay.orthographicSize * 2f * Mathf.Max(0.01f, _gameplay.aspect);
                float visibleCells = visibleWidth / _cellSize;
                float screens = visibleCells > 0.01f ? _cells / visibleCells : 0f;
                _caption.text = $"{_cells} マス（{screens:0.#} 画面分）";
            }
        }

        void BuildCamera()
        {
            _target = new RenderTexture(TextureWidth, TextureHeight, 16, RenderTextureFormat.ARGB32)
            {
                name = "StageWidthPreview",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            _target.Create();
            if (_image != null)
            {
                _image.texture = _target;
            }

            var cameraGo = new GameObject("WidthPreviewCamera");
            _camera = cameraGo.AddComponent<Camera>();
            _camera.orthographic = true;
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = new Color(0.1f, 0.45f, 1f, 1f);
            _camera.nearClipPlane = 0.3f;
            _camera.farClipPlane = 1000f;
            _camera.targetTexture = _target;
            _camera.forceIntoRenderTexture = true;
            _camera.depth = -20f;
            _camera.enabled = false;
            var data = _camera.GetUniversalAdditionalCameraData();
            data.renderType = CameraRenderType.Base;
            data.SetRenderer(-1);
            data.renderShadows = false;
            data.renderPostProcessing = false;
        }

        static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}

using UnityEngine;

namespace Pulling
{
    public class StageCameraController : MonoBehaviour
    {
        public Transform target;
        public float smoothTime = 0.18f;
        public bool followEnabled = true;

        Camera _camera;
        float _cellSize = 1f;
        float _mapWidth;
        float _mapHeight;
        Vector3 _velocity;

        public void Bind(Camera cam, Transform followTarget)
        {
            _camera = cam;
            target = followTarget;
        }

        public void Configure(StageAsset stage)
        {
            if (stage == null || _camera == null)
            {
                return;
            }

            _cellSize = stage.cellSize > 0f ? stage.cellSize : 1f;
            _mapWidth = stage.width * _cellSize;
            _mapHeight = stage.height * _cellSize;
            int rows = Mathf.Max(1, stage.cameraVisibleRows);
            _camera.orthographic = true;
            _camera.orthographicSize = rows * _cellSize * 0.5f;
            SnapToTarget();
        }

        public void SnapToTarget()
        {
            if (_camera == null)
            {
                return;
            }

            Vector3 next = ComputeTargetPosition();
            next.z = transform.position.z;
            transform.position = next;
            _velocity = Vector3.zero;
        }

        public void SetMapDimensions(int width, int height, float cellSize)
        {
            _cellSize = cellSize > 0f ? cellSize : 1f;
            _mapWidth = width * _cellSize;
            _mapHeight = height * _cellSize;
        }

        public Vector3 ClampPosition(Vector3 position)
        {
            if (_camera == null)
            {
                return position;
            }

            float halfHeight = _camera.orthographicSize;
            float halfWidth = halfHeight * _camera.aspect;
            float minX = halfWidth;
            float maxX = Mathf.Max(minX, _mapWidth - halfWidth);
            position.x = Mathf.Clamp(position.x, minX, maxX);

            float y = _mapHeight * 0.5f;
            if (_mapHeight > halfHeight * 2f)
            {
                y = Mathf.Clamp(position.y, halfHeight, _mapHeight - halfHeight);
            }

            position.y = y;
            return position;
        }

        void LateUpdate()
        {
            if (_camera == null || !followEnabled)
            {
                return;
            }

            Vector3 next = ComputeTargetPosition();
            next.z = transform.position.z;
            transform.position = Vector3.SmoothDamp(transform.position, next, ref _velocity, smoothTime);
        }

        Vector3 ComputeTargetPosition()
        {
            float halfHeight = _camera.orthographicSize;
            float halfWidth = halfHeight * _camera.aspect;
            float x = target != null ? target.position.x : halfWidth;
            float minX = halfWidth;
            float maxX = Mathf.Max(minX, _mapWidth - halfWidth);
            x = Mathf.Clamp(x, minX, maxX);
            float y = _mapHeight * 0.5f;
            if (_mapHeight > halfHeight * 2f)
            {
                float targetY = target != null ? target.position.y : y;
                y = Mathf.Clamp(targetY, halfHeight, _mapHeight - halfHeight);
            }

            return new Vector3(x, y, transform.position.z);
        }
    }
}

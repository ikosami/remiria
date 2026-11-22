using UnityEngine;

namespace Pulling
{
    public class CompanionMeiling : MonoBehaviour
    {
        float _angle;
        float _strength = PlayerTuning.MeilingDefaultSpeed;
        Vector2 _direction = Vector2.right;
        bool _guideVisible;
        float _retriggerAt;
        Transform _guideRoot;
        SpriteRenderer _shaft;
        SpriteRenderer _head;

        public void Apply(float angleDegrees, float strength)
        {
            _angle = Mathf.Repeat(angleDegrees, 360f);
            _strength = Mathf.Clamp(strength, PlayerTuning.MeilingMinSpeed, PlayerTuning.MeilingMaxSpeed);
            float rad = _angle * Mathf.Deg2Rad;
            _direction = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
            RefreshGuide();
        }

        public void SetGuideVisible(bool visible)
        {
            _guideVisible = visible;
            RefreshGuide();
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (Time.time < _retriggerAt)
            {
                return;
            }

            var player = other.GetComponent<PlayerController>();
            if (player == null || player.IsMeilingControlled)
            {
                return;
            }

            if (!player.BeginMeiling((Vector2)transform.position, _direction * _strength))
            {
                return;
            }

            _retriggerAt = Time.time + 0.2f;
        }

        void OnTriggerExit2D(Collider2D other)
        {
            if (other.GetComponent<PlayerController>() == null)
            {
                return;
            }

            _retriggerAt = Time.time + 0.2f;
        }

        void RefreshGuide()
        {
            EnsureGuide();
            _guideRoot.gameObject.SetActive(_guideVisible);
            if (!_guideVisible)
            {
                return;
            }

            float span = Mathf.InverseLerp(PlayerTuning.MeilingMinSpeed, PlayerTuning.MeilingMaxSpeed, _strength);
            float length = Mathf.Lerp(0.45f, 1.65f, span);
            _guideRoot.localRotation = Quaternion.Euler(0f, 0f, _angle);
            _shaft.transform.localPosition = new Vector3(length * 0.5f, 0f, 0f);
            _shaft.transform.localScale = new Vector3(length, 0.08f, 1f);
            _head.transform.localPosition = new Vector3(length + 0.02f, 0f, 0f);
            _head.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            _head.transform.localScale = new Vector3(0.18f, 0.18f, 1f);
        }

        void EnsureGuide()
        {
            if (_guideRoot != null)
            {
                return;
            }

            var root = new GameObject("Guide");
            root.transform.SetParent(transform, false);
            _guideRoot = root.transform;

            _shaft = CreateGuidePart("Shaft", root.transform);
            _head = CreateGuidePart("Head", root.transform);
            root.SetActive(_guideVisible);
        }

        static SpriteRenderer CreateGuidePart(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = SpriteFactory.CreateSolid(new Color(1f, 0.92f, 0.35f, 0.95f));
            renderer.sortingOrder = 12;
            SpriteFactory.AssignPipelineMaterial(renderer);
            return renderer;
        }
    }
}

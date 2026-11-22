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
            var shaftPosition = _shaft.transform.localPosition;
            shaftPosition.x = length * 0.5f;
            _shaft.transform.localPosition = shaftPosition;
            var shaftScale = _shaft.transform.localScale;
            shaftScale.x = length;
            _shaft.transform.localScale = shaftScale;
            var headPosition = _head.transform.localPosition;
            headPosition.x = length + 0.02f;
            _head.transform.localPosition = headPosition;
        }

        void EnsureGuide()
        {
            if (_guideRoot != null)
            {
                return;
            }

            var guide = SpawnLibrary.Spawn(SpawnLibrary.Current.meilingGuide, transform);
            _guideRoot = guide.transform;
            _shaft = PrepareGuidePart(guide.shaft);
            _head = PrepareGuidePart(guide.head);
            guide.gameObject.SetActive(_guideVisible);
        }

        static SpriteRenderer PrepareGuidePart(SpriteRenderer renderer)
        {
            if (renderer.sprite == null)
            {
                renderer.sprite = SpriteFactory.CreateSolid(new Color(1f, 0.92f, 0.35f, 0.95f));
            }

            SpriteFactory.AssignPipelineMaterial(renderer);
            return renderer;
        }
    }
}

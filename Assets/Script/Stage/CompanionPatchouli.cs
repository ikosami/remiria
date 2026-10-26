using UnityEngine;

namespace Pulling
{
    public class CompanionPatchouli : MonoBehaviour
    {
        SpriteRenderer _renderer;
        bool _activated;

        void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (_activated || other.GetComponent<PlayerController>() == null)
            {
                return;
            }

            _activated = true;
            if (_renderer != null)
            {
                _renderer.color = new Color(0.9f, 0.7f, 1f, 1f);
            }

            Debug.Log("[Patchouli] ステージ変化は今後実装予定");
        }
    }
}


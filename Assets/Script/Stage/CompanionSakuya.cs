using UnityEngine;

namespace Pulling
{
    public class CompanionSakuya : MonoBehaviour
    {
        bool _used;

        void OnTriggerEnter2D(Collider2D other)
        {
            if (_used)
            {
                return;
            }

            var player = other.GetComponent<PlayerController>();
            if (player == null)
            {
                return;
            }

            _used = true;
            player.GrantSakuyaShield();
        }
    }
}


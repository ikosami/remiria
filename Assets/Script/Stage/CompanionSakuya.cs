using UnityEngine;

namespace Pulling
{
    public class CompanionSakuya : MonoBehaviour
    {
        void OnTriggerEnter2D(Collider2D other)
        {
            var player = other.GetComponent<PlayerController>();
            if (player == null)
            {
                return;
            }

            player.TouchSakuya(transform.position);
        }
    }
}


using UnityEngine;

namespace Pulling
{
    public class BulletBlock : MonoBehaviour
    {
        void OnTriggerEnter2D(Collider2D other)
        {
            var player = other.GetComponent<PlayerController>();
            if (player == null)
            {
                return;
            }

            Vector2 dir = ((Vector2)player.transform.position - (Vector2)transform.position).normalized;
            if (dir.sqrMagnitude < 0.01f)
            {
                dir = Vector2.up;
            }

            player.SmashThrough(dir * PlayerTuning.BulletKnockback);
        }
    }
}


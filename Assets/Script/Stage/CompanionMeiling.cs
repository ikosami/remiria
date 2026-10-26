using UnityEngine;

namespace Pulling
{
    public class CompanionMeiling : MonoBehaviour
    {
        bool _used;

        void OnTriggerEnter2D(Collider2D other)
        {
            if (_used)
            {
                return;
            }

            var player = other.GetComponent<PlayerController>();
            if (player == null || player.State != PlayerMoveState.Flying && player.State != PlayerMoveState.Stopped)
            {
                return;
            }

            _used = true;
            Vector2 dir = player.LastAimDirection;
            if (dir.sqrMagnitude < 0.01f)
            {
                dir = Vector2.right;
            }

            player.SmashThrough(dir.normalized * PlayerTuning.LaunchImpulse);
        }
    }
}


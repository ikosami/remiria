using UnityEngine;

namespace Pulling
{
    public enum StageTriggerKind
    {
        Goal,
        Hazard
    }

    public class StageTrigger : MonoBehaviour
    {
        public StageTriggerKind kind;
        GameManager _game;

        public void Bind(GameManager game, StageTriggerKind triggerKind)
        {
            _game = game;
            kind = triggerKind;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (_game == null || !other.GetComponent<PlayerController>())
            {
                return;
            }

            if (kind == StageTriggerKind.Goal)
            {
                _game.NotifyGoalReached();
            }
            else
            {
                _game.NotifyHazardHit();
            }
        }
    }
}

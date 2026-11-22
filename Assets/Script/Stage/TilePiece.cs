using UnityEngine;

namespace Pulling
{
    public class TilePiece : MonoBehaviour
    {
        public SpriteRenderer sprite;
        public SpecialTileMarker marker;
        public BoxCollider2D box;
        public SlopeCollider slope;
        public BounceSurface bounce;
        public StageTrigger trigger;
        public BreakableBlock breakable;
        public OneWayPlatform oneWay;
        public CompanionMeiling meiling;
        public CompanionPatchouli patchouli;
        public CompanionSakuya sakuya;
        public FranNpc fran;
        public EnemyBlock enemy;
        public BulletBlock bullet;
        public SwitchPlate plate;
        public DoorGate door;
        public Collider2D[] solids = System.Array.Empty<Collider2D>();

        public void SetSolid(bool enabled)
        {
            if (door != null && door.IsOpening)
            {
                return;
            }

            for (int i = 0; i < solids.Length; i++)
            {
                solids[i].enabled = enabled;
            }
        }
    }
}

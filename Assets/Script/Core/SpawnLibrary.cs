using UnityEngine;

namespace Pulling
{
    [CreateAssetMenu(fileName = "SpawnLibrary", menuName = "Pulling/Spawn Library")]
    public class SpawnLibrary : ScriptableObject
    {
        public BlastFlash blastFlash;
        public EnemyShot enemyShot;
        public EnemyLaser enemyLaser;
        public LowHealthVignette healthVignette;
        public GameObject eventSystem;
        public TileLabelView tileLabel;
        public SpriteRenderer playerVisual;
        public AimArrowView aimArrow;
        public SpriteRenderer pullOrigin;
        public AirJumpOutlineView airJumpOutline;
        public SpriteRenderer skyBeam;
        public SpriteRenderer switchLever;
        public GuideView meilingGuide;
        public Transform ceilingRoot;
        public CeilingWall ceilingWall;
        public GameObject stageRow;
        public GameObject paletteItem;
        public GameObject paletteHeader;

        public static SpawnLibrary Current { get; private set; }

        public static void Use(SpawnLibrary library)
        {
            Current = library;
        }

        public static T Spawn<T>(T prefab, Transform parent) where T : Component
        {
            var instance = Object.Instantiate(prefab, parent);
            instance.name = prefab.name;
            return instance;
        }

        public static T Spawn<T>(T prefab, Vector3 position, Quaternion rotation, Transform parent) where T : Component
        {
            var instance = Object.Instantiate(prefab, position, rotation, parent);
            instance.name = prefab.name;
            return instance;
        }
    }
}

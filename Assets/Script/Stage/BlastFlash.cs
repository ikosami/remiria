using UnityEngine;

namespace Pulling
{
    public class BlastFlash : MonoBehaviour
    {
        const float Duration = 0.14f;

        public SpriteRenderer halo;
        public SpriteRenderer core;
        float _age;
        float _cell = 1f;

        public static void Spawn(Transform parent, Vector3 position, float cellSize)
        {
            var flash = SpawnLibrary.Spawn(SpawnLibrary.Current.blastFlash, position, Quaternion.identity, parent);
            flash._cell = cellSize > 0f ? cellSize : 1f;
            PrepareLayer(flash.halo);
            PrepareLayer(flash.core);
        }

        static void PrepareLayer(SpriteRenderer renderer)
        {
            if (renderer.sprite == null)
            {
                renderer.sprite = SpriteFactory.CreateCircle(Color.white);
            }

            SpriteFactory.AssignPipelineMaterial(renderer);
        }

        void Update()
        {
            _age += Time.deltaTime;
            float t = Mathf.Clamp01(_age / Duration);
            float haloScale = _cell * Mathf.Lerp(0.5f, 1.05f, t);
            float coreScale = _cell * Mathf.Lerp(0.25f, 0.55f, t);
            halo.transform.localScale = new Vector3(haloScale, haloScale, 1f);
            core.transform.localScale = new Vector3(coreScale, coreScale, 1f);

            float fade = 1f - t;
            var haloColor = Color.Lerp(new Color(1f, 0.7f, 0.18f, 1f), new Color(0.85f, 0.16f, 0.05f, 0f), t);
            haloColor.a = fade;
            halo.color = haloColor;

            var coreColor = Color.Lerp(Color.white, new Color(1f, 0.86f, 0.45f, 0f), t);
            coreColor.a = fade * fade;
            core.color = coreColor;

            if (t >= 1f)
            {
                Destroy(gameObject);
            }
        }
    }
}

using UnityEngine;

namespace Pulling
{
    public class BlastFlash : MonoBehaviour
    {
        const float Duration = 0.14f;

        SpriteRenderer _core;
        SpriteRenderer _halo;
        float _age;
        float _cell = 1f;

        public static void Spawn(Transform parent, Vector3 position, float cellSize)
        {
            var go = new GameObject("BlastFlash");
            if (parent != null)
            {
                go.transform.SetParent(parent, false);
            }

            go.transform.position = position;
            var flash = go.AddComponent<BlastFlash>();
            flash._cell = cellSize > 0f ? cellSize : 1f;
            flash._halo = CreateLayer(go.transform, 4);
            flash._core = CreateLayer(go.transform, 5);
        }

        static SpriteRenderer CreateLayer(Transform parent, int sortingOrder)
        {
            var go = new GameObject(sortingOrder >= 5 ? "Core" : "Halo");
            go.transform.SetParent(parent, false);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = SpriteFactory.CreateCircle(Color.white);
            renderer.sortingOrder = sortingOrder;
            SpriteFactory.AssignPipelineMaterial(renderer);
            return renderer;
        }

        void Update()
        {
            _age += Time.deltaTime;
            float t = Mathf.Clamp01(_age / Duration);
            float halo = _cell * Mathf.Lerp(0.5f, 1.05f, t);
            float core = _cell * Mathf.Lerp(0.25f, 0.55f, t);
            _halo.transform.localScale = new Vector3(halo, halo, 1f);
            _core.transform.localScale = new Vector3(core, core, 1f);

            float fade = 1f - t;
            var haloColor = Color.Lerp(new Color(1f, 0.7f, 0.18f, 1f), new Color(0.85f, 0.16f, 0.05f, 0f), t);
            haloColor.a = fade;
            _halo.color = haloColor;

            var coreColor = Color.Lerp(Color.white, new Color(1f, 0.86f, 0.45f, 0f), t);
            coreColor.a = fade * fade;
            _core.color = coreColor;

            if (t >= 1f)
            {
                Destroy(gameObject);
            }
        }
    }
}

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Pulling
{
    public static class GameSceneRuntime
    {
        public static bool Bind(GameManager manager)
        {
            if (manager == null || !manager.HasSceneRefs())
            {
                Debug.LogError("GameScene の参照が足りません。GameManager の参照をシーン上で割り当ててください。");
                return false;
            }

            EnsureUiInput();
            if (manager.fadeImage != null)
            {
                manager.fadeImage.raycastTarget = false;
                manager.fadeImage.enabled = false;
            }

            var camera = Camera.main;
            var player = manager.player;
            var playerBody = player.GetComponent<Rigidbody2D>();
            if (playerBody != null)
            {
                playerBody.gravityScale = PlayerTuning.GravityScale;
                playerBody.linearDamping = PlayerTuning.FlightLinearDamping;
            }

            var playerCol = player.GetComponent<CircleCollider2D>();
            ApplyBounceMaterial(player, playerCol, manager);
            var playerRenderer = player.GetComponent<SpriteRenderer>();
            player.Apply(manager.remiria);
            if (!player.HasAppearance && playerRenderer != null && playerRenderer.sprite == null)
            {
                playerRenderer.sprite = SpriteFactory.CreateCircle(new Color(0.25f, 0.65f, 1f));
            }

            var aimGo = GameObject.Find("AimLine");
            var aimView = aimGo != null ? aimGo.GetComponent<AimLineView>() : Object.FindFirstObjectByType<AimLineView>();

            if (aimView != null)
            {
                var line = aimView.GetComponent<LineRenderer>();
                if (line != null && line.material == null)
                {
                    var shader = Shader.Find("Sprites/Default");
                    line.material = new Material(shader);
                }

                aimView.Apply(manager.remiria);
                aimView.Bind(line);
            }

            player.Bind(playerBody, playerCol, aimView, camera);
            player.BindMapBuilder(manager.mapBuilder);
            player.BindHpBar(FindHpFill());
            player.BindHealthVignette(EnsureHealthVignette());

            manager.stageCamera.Bind(camera, player.transform);

            if (manager.cellHover != null)
            {
                var hoverRenderer = manager.cellHover.GetComponent<SpriteRenderer>();
                if (hoverRenderer != null && hoverRenderer.sprite == null)
                {
                    hoverRenderer.sprite = SpriteFactory.CreateBorder(new Color(1f, 1f, 0.4f, 0.9f));
                }

                manager.gridOverlay.Bind(manager.cellHover);
            }

            if (manager.spawnMarker != null)
            {
                var spawnRenderer = manager.spawnMarker.GetComponent<SpriteRenderer>();
                if (spawnRenderer != null && spawnRenderer.sprite == null)
                {
                    spawnRenderer.sprite = SpriteFactory.CreateCircle(new Color(0.3f, 0.85f, 1f, 0.85f));
                }
            }

            manager.mapBuilder.Bind(manager, manager.mapBuilder.groundTilemap, manager.mapBuilder.specialRoot, manager.tileDatabase);
            manager.skyLight.Bind(manager.mapBuilder, manager.tileDatabase);
            manager.transition.Bind(manager.fadeImage);
            manager.editor.Bind(
                manager,
                manager.mapBuilder,
                manager.stageCamera,
                manager.editorUI,
                manager.gridOverlay,
                manager.editorGhost,
                manager.spawnMarker);
            manager.editorUI.Bind(
                manager.editorUI.editorRoot,
                manager.editorUI.settingsPanel,
                manager.editorUI.hintRoot,
                manager.editorUI.paletteContent,
                manager.editorUI.statusLabel,
                manager.editor);

            manager.BindRuntime(manager.mapBuilder, player, manager.stageCamera, manager.transition, manager.editor);
            return true;
        }

        static Image FindHpFill()
        {
            var canvas = GameObject.Find("GameCanvas");
            if (canvas == null)
            {
                Debug.LogWarning("GameCanvas が見つかりません。");
                return null;
            }

            var mask = canvas.transform.Find("HPBar/Mask");
            if (mask == null)
            {
                Debug.LogWarning("HPBar/Mask が見つかりません。");
                return null;
            }

            return mask.GetComponent<Image>();
        }

        static LowHealthVignette EnsureHealthVignette()
        {
            var canvas = GameObject.Find("GameCanvas");
            if (canvas == null)
            {
                return null;
            }

            var existing = canvas.transform.Find("HealthVignette");
            var go = existing != null ? existing.gameObject : new GameObject("HealthVignette", typeof(RectTransform));
            if (existing == null)
            {
                go.transform.SetParent(canvas.transform, false);
            }

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
            go.transform.SetAsFirstSibling();

            var image = go.GetComponent<Image>();
            if (image == null)
            {
                image = go.AddComponent<Image>();
            }

            image.raycastTarget = false;
            image.maskable = false;
            image.color = new Color(0.86f, 0.02f, 0.05f, 0f);
            image.enabled = false;

            var shader = Resources.Load<Shader>("LowHealthVignette");
            if (shader != null)
            {
                image.sprite = SpriteFactory.CreateSolid(Color.white, 4);
                image.material = new Material(shader)
                {
                    name = "LowHealthVignette (runtime)",
                    hideFlags = HideFlags.HideAndDontSave
                };
            }
            else
            {
                image.sprite = CreateEdgeSprite();
                Debug.LogWarning("LowHealthVignette シェーダーが見つからないため、テクスチャで縁を描きます。");
            }

            var vignette = go.GetComponent<LowHealthVignette>();
            if (vignette == null)
            {
                vignette = go.AddComponent<LowHealthVignette>();
            }

            vignette.Bind(image);
            return vignette;
        }

        static Sprite CreateEdgeSprite()
        {
            const int size = 256;
            const float edge = 0.2f;
            const float power = 1.7f;
            float aspect = Screen.height > 0 ? (float)Screen.width / Screen.height : 16f / 9f;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave,
                name = "HealthEdge"
            };

            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = (x + 0.5f) / size;
                    float v = (y + 0.5f) / size;
                    float dx = Mathf.Min(u, 1f - u) * aspect;
                    float dy = Mathf.Min(v, 1f - v);
                    float rim = Mathf.Clamp01(1f - Mathf.Min(dx, dy) / edge);
                    rim = Mathf.Pow(rim, power);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, rim);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            var sprite = Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }

        static void EnsureUiInput()
        {
            var eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                var go = new GameObject("EventSystem");
                eventSystem = go.AddComponent<EventSystem>();
            }

            var module = eventSystem.GetComponent<InputSystemUIInputModule>();
            if (module == null)
            {
                module = eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
            }

            module.AssignDefaultActions();
        }

        static void ApplyBounceMaterial(PlayerController player, CircleCollider2D playerCol, GameManager manager)
        {
            var material = new PhysicsMaterial2D("PullingBounce")
            {
                friction = 0f,
                bounciness = 0f
            };

            if (playerCol != null)
            {
                playerCol.sharedMaterial = material;
            }

            if (manager.mapBuilder != null && manager.mapBuilder.groundTilemap != null)
            {
                var composite = manager.mapBuilder.groundTilemap.GetComponent<CompositeCollider2D>();
                if (composite != null)
                {
                    composite.sharedMaterial = material;
                }
            }
        }
    }
}

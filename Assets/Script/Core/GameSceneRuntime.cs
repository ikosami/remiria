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
                Debug.LogError("GameScene の参照が足りません。Unityメニューの Pulling/Build Game Scene を実行してください。");
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
            player.SetSprites(RemiriaSprites.Normal, RemiriaSprites.JumpWait, RemiriaSprites.Jump);
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

                aimView.SetArrow(RemiriaSprites.Arrow);
                aimView.Bind(line);
            }

            player.Bind(playerBody, playerCol, aimView, camera);
            player.BindMapBuilder(manager.mapBuilder);
            player.BindHpBar(FindHpFill());

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

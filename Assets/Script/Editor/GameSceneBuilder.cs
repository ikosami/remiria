#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Pulling
{
    [InitializeOnLoad]
    public static class GameSceneBuilder
    {
        const string ScenePath = "Assets/Scenes/GameScene.unity";

        static GameSceneBuilder()
        {
            EditorApplication.delayCall += TryBuildOpenScene;
        }

        [MenuItem("Pulling/Build Game Scene")]
        public static void BuildAndSave()
        {
            if (Application.isPlaying)
            {
                Debug.LogWarning("Play中はシーン構築できません。");
                return;
            }

            StageAssetSaver.EnsureDefaultAssets();
            var scene = EditorSceneManager.OpenScene(ScenePath);
            var manager = Object.FindFirstObjectByType<GameManager>();
            if (manager == null)
            {
                var go = new GameObject("GameManager");
                manager = go.AddComponent<GameManager>();
            }

            AssignData(manager);
            GameSceneFactory.Build(manager);
            AssignSkyLight(manager);
            AssignRemiria();
            EditorUtility.SetDirty(manager);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("GameScene にオブジェクトを配置して保存しました。");
        }

        static void TryBuildOpenScene()
        {
            if (Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ScenePath)
            {
                return;
            }

            var manager = Object.FindFirstObjectByType<GameManager>();
            if (manager != null && manager.HasSceneRefs())
            {
                return;
            }

            BuildAndSave();
        }

        static void AssignData(GameManager manager)
        {
            if (manager.catalog == null)
            {
                manager.catalog = AssetDatabase.LoadAssetAtPath<StageCatalog>(StagePersistence.CatalogPath);
            }

            if (manager.tileDatabase == null)
            {
                manager.tileDatabase = AssetDatabase.LoadAssetAtPath<TileDatabase>(StagePersistence.TileDatabasePath);
            }
        }

        static void AssignSkyLight(GameManager manager)
        {
            if (manager.skyLight == null || manager.skyLight.beamSprite != null)
            {
                return;
            }

            manager.skyLight.beamSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Image/light.png");
            EditorUtility.SetDirty(manager.skyLight);
        }

        static void AssignRemiria()
        {
            var player = Object.FindFirstObjectByType<PlayerController>();
            if (player != null)
            {
                player.SetSprites(RemiriaSprites.Normal, RemiriaSprites.JumpWait, RemiriaSprites.Jump);
                EditorUtility.SetDirty(player);
            }

            var aim = Object.FindFirstObjectByType<AimLineView>();
            if (aim != null)
            {
                aim.SetArrow(RemiriaSprites.Arrow);
                EditorUtility.SetDirty(aim);
            }
        }
    }
}
#endif

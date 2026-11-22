#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Pulling
{
    [InitializeOnLoad]
    public static class StageAssetSaver
    {
        static StageAssetSaver()
        {
            EditorApplication.delayCall += EnsureDefaultAssets;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                EditorApplication.delayCall += EnsureDefaultAssets;
            }
        }

        [MenuItem("Pulling/Ensure Default Stage Assets")]
        public static void EnsureDefaultAssets()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            StagePersistence.EnsureFolders();

            var tiles = AssetDatabase.LoadAssetAtPath<TileDatabase>(StagePersistence.TileDatabasePath);
            if (tiles == null)
            {
                tiles = ScriptableObject.CreateInstance<TileDatabase>();
                tiles.name = "TileDatabase";
                AssetDatabase.CreateAsset(tiles, StagePersistence.TileDatabasePath);
                EditorUtility.SetDirty(tiles);
            }

            bool changed = StagePersistence.SyncCatalogToEditStage();
            if (changed)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }
    }
}
#endif

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
        }

        [MenuItem("Pulling/Ensure Default Stage Assets")]
        public static void EnsureDefaultAssets()
        {
            StagePersistence.EnsureFolders();

            var tiles = AssetDatabase.LoadAssetAtPath<TileDatabase>(StagePersistence.TileDatabasePath);
            if (tiles == null)
            {
                tiles = ScriptableObject.CreateInstance<TileDatabase>();
                tiles.name = "TileDatabase";
                AssetDatabase.CreateAsset(tiles, StagePersistence.TileDatabasePath);
            }

            string stagePath = $"{StagePersistence.StagesFolder}/Stage_1.asset";
            var stage = AssetDatabase.LoadAssetAtPath<StageAsset>(stagePath);
            if (stage == null)
            {
                stage = StageFactory.CreateDefault();
                AssetDatabase.CreateAsset(stage, stagePath);
            }

            var catalog = AssetDatabase.LoadAssetAtPath<StageCatalog>(StagePersistence.CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<StageCatalog>();
                catalog.name = "StageCatalog";
                catalog.stages.Add(stage);
                AssetDatabase.CreateAsset(catalog, StagePersistence.CatalogPath);
            }
            else if (catalog.stages == null || catalog.stages.Count == 0 || catalog.stages[0] == null)
            {
                if (catalog.stages == null)
                {
                    catalog.stages = new System.Collections.Generic.List<StageAsset>();
                }

                if (!catalog.stages.Contains(stage))
                {
                    catalog.stages.Add(stage);
                }

                EditorUtility.SetDirty(catalog);
            }

            EditorUtility.SetDirty(tiles);
            AssetDatabase.SaveAssets();
        }
    }
}
#endif

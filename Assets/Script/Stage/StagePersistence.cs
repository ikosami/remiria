using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Pulling
{
    public static class StagePersistence
    {
        public const string DataFolder = "Assets/Data";
        public const string StagesFolder = "Assets/Data/Stages";
        public const string TilesFolder = "Assets/Data/Tiles";
        public const string CatalogPath = "Assets/Data/StageCatalog.asset";
        public const string TileDatabasePath = "Assets/Data/Tiles/TileDatabase.asset";
        public const string EditStageFolderName = "EditStage";

        static readonly Regex StageNumberRegex = new Regex(@"^Stage_(\d+)$", RegexOptions.IgnoreCase);

        public static string GetExecutableDirectory()
        {
            var parent = Directory.GetParent(Application.dataPath);
            return parent != null ? parent.FullName : Application.dataPath;
        }

        public static string GetEditStageFolder()
        {
            return Path.Combine(GetExecutableDirectory(), EditStageFolderName);
        }

        public static string EnsureEditStageFolder()
        {
            string folder = GetEditStageFolder();
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            return folder;
        }

        public static void OpenEditStageFolder()
        {
            string folder = EnsureEditStageFolder();
            try
            {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"\"{folder}\"",
                    UseShellExecute = true
                });
#else
                Application.OpenURL(new Uri(folder).AbsoluteUri);
#endif
            }
            catch (Exception e)
            {
                Debug.LogWarning($"EditStageフォルダを開けませんでした: {e.Message}");
            }
        }

        public static StageCatalog CreateRuntimeCatalog(StageCatalog source)
        {
            var clone = ScriptableObject.CreateInstance<StageCatalog>();
            clone.name = "StageCatalog (runtime)";
            clone.stages = new List<StageAsset>();
            if (source?.stages == null)
            {
                return clone;
            }

            for (int i = 0; i < source.stages.Count; i++)
            {
                if (source.stages[i] != null)
                {
                    clone.stages.Add(source.stages[i]);
                }
            }

            return clone;
        }

        public static void ApplyEditStageOverrides(StageCatalog catalog)
        {
            if (catalog == null)
            {
                return;
            }

            if (catalog.stages == null)
            {
                catalog.stages = new List<StageAsset>();
            }

            string folder = EnsureEditStageFolder();
            if (!Directory.Exists(folder))
            {
                return;
            }

            string[] files = Directory.GetFiles(folder, "*.json");
            Array.Sort(files, StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < files.Length; i++)
            {
                var loaded = LoadStageFile(files[i]);
                if (loaded == null)
                {
                    continue;
                }

                int index = IndexOfId(catalog, loaded.id);
                if (index >= 0)
                {
                    catalog.stages[index] = loaded;
                }
                else
                {
                    catalog.stages.Add(loaded);
                }
            }
        }

        public static bool TryNormalizeStageId(string raw, out string id, out string error)
        {
            id = null;
            error = null;
            if (string.IsNullOrWhiteSpace(raw))
            {
                error = "ステージ名を入力してください。";
                return false;
            }

            string trimmed = raw.Trim();
            if (trimmed.Length > 64)
            {
                error = "ステージ名は64文字以内にしてください。";
                return false;
            }

            if (trimmed.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                error = "ステージ名に使えない文字が含まれています。";
                return false;
            }

            id = trimmed;
            return true;
        }

        public static StageAsset SaveAsNew(StageAsset source, StageCatalog catalog)
        {
            if (source == null)
            {
                Debug.LogWarning("保存するステージがありません。");
                return null;
            }

            int number = NextStageNumber(catalog);
            var asset = ScriptableObject.CreateInstance<StageAsset>();
            asset.CopyFrom(source);
            asset.id = $"Stage_{number}";
            asset.name = asset.id;
            return PersistNew(asset, catalog);
        }

        public static StageAsset SaveNamed(StageAsset source, StageCatalog catalog)
        {
            if (source == null)
            {
                Debug.LogWarning("保存するステージがありません。");
                return null;
            }

            if (!TryNormalizeStageId(source.id, out string id, out string error))
            {
                Debug.LogWarning(error);
                return null;
            }

            if (catalog != null && catalog.IndexOfId(id) >= 0)
            {
                Debug.LogWarning($"{id} は既に存在します。");
                return null;
            }

            var asset = ScriptableObject.CreateInstance<StageAsset>();
            asset.CopyFrom(source);
            asset.id = id;
            asset.name = id;
            return PersistNew(asset, catalog);
        }

        static StageAsset PersistNew(StageAsset asset, StageCatalog catalog)
        {
            bool jsonOk = SaveToEditStage(asset);
            bool editorOk = false;
#if UNITY_EDITOR
            editorOk = SaveAsNewAsset(asset, catalog);
#endif
            if (!jsonOk && !editorOk)
            {
                return null;
            }

            if (catalog != null)
            {
                if (catalog.stages == null)
                {
                    catalog.stages = new List<StageAsset>();
                }

                if (!catalog.stages.Contains(asset))
                {
                    catalog.stages.Add(asset);
                }
            }

            return asset;
        }

        public static bool Overwrite(StageAsset target)
        {
            if (target == null)
            {
                Debug.LogWarning("上書き対象のステージがありません。");
                return false;
            }

            bool jsonOk = SaveToEditStage(target);
            bool editorOk = false;
#if UNITY_EDITOR
            editorOk = OverwriteAsset(target);
#endif
            if (!jsonOk && !editorOk)
            {
                Debug.LogWarning("ステージの上書き保存に失敗しました。");
                return false;
            }

            return true;
        }

        public static StageCatalog LoadCatalog()
        {
#if UNITY_EDITOR
            return AssetDatabase.LoadAssetAtPath<StageCatalog>(CatalogPath);
#else
            return Resources.Load<StageCatalog>("StageCatalog");
#endif
        }

        public static TileDatabase LoadTileDatabase()
        {
#if UNITY_EDITOR
            return AssetDatabase.LoadAssetAtPath<TileDatabase>(TileDatabasePath);
#else
            return Resources.Load<TileDatabase>("TileDatabase");
#endif
        }

#if UNITY_EDITOR
        public static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder(DataFolder))
            {
                AssetDatabase.CreateFolder("Assets", "Data");
            }

            if (!AssetDatabase.IsValidFolder(StagesFolder))
            {
                AssetDatabase.CreateFolder(DataFolder, "Stages");
            }

            if (!AssetDatabase.IsValidFolder(TilesFolder))
            {
                AssetDatabase.CreateFolder(DataFolder, "Tiles");
            }
        }
#endif

        public static int NextStageNumber(StageCatalog catalog)
        {
            int max = 0;
            if (catalog?.stages != null)
            {
                for (int i = 0; i < catalog.stages.Count; i++)
                {
                    ConsiderStageName(catalog.stages[i] != null ? catalog.stages[i].id : null, ref max);
                }
            }

            string folder = GetEditStageFolder();
            if (Directory.Exists(folder))
            {
                foreach (string file in Directory.GetFiles(folder, "*.json"))
                {
                    ConsiderStageName(Path.GetFileNameWithoutExtension(file), ref max);
                }
            }

#if UNITY_EDITOR
            if (Directory.Exists(StagesFolder))
            {
                foreach (string file in Directory.GetFiles(StagesFolder, "*.asset"))
                {
                    ConsiderStageName(Path.GetFileNameWithoutExtension(file), ref max);
                }
            }
#endif
            return max + 1;
        }

        static void ConsiderStageName(string name, ref int max)
        {
            if (string.IsNullOrEmpty(name))
            {
                return;
            }

            Match match = StageNumberRegex.Match(name);
            if (match.Success && int.TryParse(match.Groups[1].Value, out int n) && n > max)
            {
                max = n;
            }
        }

        static bool SaveToEditStage(StageAsset stage)
        {
            if (stage == null)
            {
                return false;
            }

            try
            {
                string folder = EnsureEditStageFolder();
                string path = Path.Combine(folder, FileNameFor(stage.id));
                var data = StageFileData.From(stage);
                File.WriteAllText(path, JsonUtility.ToJson(data, true));
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"EditStageへの保存に失敗しました: {e.Message}");
                return false;
            }
        }

        static StageAsset LoadStageFile(string path)
        {
            try
            {
                string json = File.ReadAllText(path);
                if (string.IsNullOrWhiteSpace(json))
                {
                    return null;
                }

                var data = JsonUtility.FromJson<StageFileData>(json);
                if (data == null)
                {
                    Debug.LogWarning($"ステージデータを読めませんでした: {path}");
                    return null;
                }

                if (string.IsNullOrWhiteSpace(data.id))
                {
                    data.id = Path.GetFileNameWithoutExtension(path);
                }

                return data.ToAsset();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"ステージデータを読めませんでした: {path}\n{e.Message}");
                return null;
            }
        }

        static int IndexOfId(StageCatalog catalog, string id)
        {
            if (catalog?.stages == null || string.IsNullOrEmpty(id))
            {
                return -1;
            }

            for (int i = 0; i < catalog.stages.Count; i++)
            {
                if (catalog.stages[i] != null && catalog.stages[i].id == id)
                {
                    return i;
                }
            }

            return -1;
        }

        static string FileNameFor(string id)
        {
            string safe = string.IsNullOrWhiteSpace(id) ? "Stage" : id.Trim();
            char[] invalid = Path.GetInvalidFileNameChars();
            for (int i = 0; i < invalid.Length; i++)
            {
                safe = safe.Replace(invalid[i], '_');
            }

            return safe + ".json";
        }

#if UNITY_EDITOR
        static bool SaveAsNewAsset(StageAsset asset, StageCatalog catalog)
        {
            EnsureFolders();
            string path = $"{StagesFolder}/{asset.id}.asset";
            if (!string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(path)))
            {
                path = AssetDatabase.GenerateUniqueAssetPath(path);
            }

            AssetDatabase.CreateAsset(asset, path);

            var targetCatalog = catalog;
            if (targetCatalog == null || string.IsNullOrEmpty(AssetDatabase.GetAssetPath(targetCatalog)))
            {
                targetCatalog = AssetDatabase.LoadAssetAtPath<StageCatalog>(CatalogPath);
            }

            if (targetCatalog != null)
            {
                if (targetCatalog.stages == null)
                {
                    targetCatalog.stages = new List<StageAsset>();
                }

                if (!targetCatalog.stages.Contains(asset))
                {
                    targetCatalog.stages.Add(asset);
                }

                EditorUtility.SetDirty(targetCatalog);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return true;
        }

        static bool OverwriteAsset(StageAsset target)
        {
            string path = AssetDatabase.GetAssetPath(target);
            if (string.IsNullOrEmpty(path))
            {
                return false;
            }

            EditorUtility.SetDirty(target);
            AssetDatabase.SaveAssets();
            return true;
        }
#endif

        [Serializable]
        class StageFileData
        {
            public int version = 1;
            public string id = "Stage";
            public int width = 40;
            public int height = 12;
            public float cellSize = 1f;
            public int cameraVisibleRows = 12;
            public int spawnX = 2;
            public int spawnY = 1;
            public string[] rows;

            public static StageFileData From(StageAsset stage)
            {
                return new StageFileData
                {
                    version = stage.version,
                    id = stage.id,
                    width = stage.width,
                    height = stage.height,
                    cellSize = stage.cellSize,
                    cameraVisibleRows = stage.cameraVisibleRows,
                    spawnX = stage.spawnX,
                    spawnY = stage.spawnY,
                    rows = stage.rows != null ? (string[])stage.rows.Clone() : null
                };
            }

            public StageAsset ToAsset()
            {
                var asset = ScriptableObject.CreateInstance<StageAsset>();
                asset.name = id + " (edit)";
                asset.version = version;
                asset.id = id;
                asset.width = width;
                asset.height = height;
                asset.cellSize = cellSize;
                asset.cameraVisibleRows = cameraVisibleRows;
                asset.spawnX = spawnX;
                asset.spawnY = spawnY;
                asset.rows = rows != null ? (string[])rows.Clone() : null;
                return asset;
            }
        }
    }
}

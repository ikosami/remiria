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
        static readonly Regex OrderPrefixRegex = new Regex(@"^(\d+)_(.+)$", RegexOptions.IgnoreCase);

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

            var liveIds = ReadEditStageIds(folder);
            string[] files = GetStageJsonFiles(folder);
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

            if (liveIds.Count > 0)
            {
                RemoveStagesExcept(catalog, liveIds);
            }
            else
            {
                RemoveNullStages(catalog);
            }

            OrderStagesForPlay(catalog);
        }

        public static bool IsDeletedEditStage(string id, StageCatalog catalog)
        {
            if (string.IsNullOrWhiteSpace(id) || catalog == null || IndexOfId(catalog, id) < 0)
            {
                return false;
            }

            string folder = GetEditStageFolder();
            if (!Directory.Exists(folder) || FindEditStageFile(id) != null)
            {
                return false;
            }

            return GetStageJsonFiles(folder).Length > 0;
        }

        public static bool TryDelete(string id, StageCatalog catalog, out string error)
        {
            error = null;
            if (string.IsNullOrWhiteSpace(id) || catalog == null || IndexOfId(catalog, id) < 0)
            {
                error = "ステージが見つかりません。";
                return false;
            }

            if (catalog.Count <= 1)
            {
                error = "最後のステージは消せません。";
                return false;
            }

            DeleteEditFile(id);
            if (FindEditStageFile(id) != null)
            {
                error = "ステージファイルを消せませんでした。";
                return false;
            }

            int index = IndexOfId(catalog, id);
            if (index >= 0)
            {
                catalog.stages.RemoveAt(index);
            }

            DeleteStageAsset(id);
            return true;
        }

        public static bool TryRename(StageAsset stage, StageCatalog catalog, string requestedName, out bool changed, out string error)
        {
            changed = false;
            error = null;
            if (stage == null || string.IsNullOrWhiteSpace(stage.id))
            {
                error = "ステージが見つかりません。";
                return false;
            }

            if (!TryNormalizeStageId(requestedName, out string newId, out error))
            {
                return false;
            }

            string oldId = stage.id;
            if (newId == oldId || DisplayName(oldId) == newId)
            {
                return true;
            }

            if (catalog != null && catalog.IndexOfId(newId) >= 0)
            {
                error = $"{DisplayName(newId)} は既に存在します。";
                return false;
            }

            string oldPath = FindEditStageFile(oldId);
            int? order = null;
            if (!string.IsNullOrEmpty(oldPath))
            {
                string stem = Path.GetFileNameWithoutExtension(oldPath);
                if (TryParseOrderPrefix(stem, out int fileOrder, out _))
                {
                    order = fileOrder;
                }
            }

            string folder = EnsureEditStageFolder();
            string newPath = Path.Combine(
                folder,
                order.HasValue ? order.Value + "_" + SanitizeFileName(newId) + ".json" : FileNameFor(newId));
            if (File.Exists(newPath)
                && (string.IsNullOrEmpty(oldPath)
                    || !string.Equals(Path.GetFullPath(oldPath), Path.GetFullPath(newPath), StringComparison.OrdinalIgnoreCase)))
            {
                error = $"{DisplayName(newId)} は既に存在します。";
                return false;
            }

            stage.id = newId;
            stage.name = newId;
            try
            {
                File.WriteAllText(newPath, JsonUtility.ToJson(StageFileData.From(stage), true));
                if (!string.IsNullOrEmpty(oldPath)
                    && !string.Equals(Path.GetFullPath(oldPath), Path.GetFullPath(newPath), StringComparison.OrdinalIgnoreCase))
                {
                    File.Delete(oldPath);
                }
            }
            catch (Exception e)
            {
                stage.id = oldId;
                stage.name = oldId;
                Debug.LogWarning($"ステージ名を変更できませんでした: {e.Message}");
                error = "ステージ名を変更できませんでした。";
                return false;
            }

            FranRecords.Move(oldId, newId);
            RenameStageAsset(oldId, newId);
            changed = true;
            return true;
        }

        public static bool SyncCatalogToEditStage()
        {
#if !UNITY_EDITOR
            return false;
#else
            bool changed = false;
            var catalog = AssetDatabase.LoadAssetAtPath<StageCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<StageCatalog>();
                catalog.name = "StageCatalog";
                catalog.stages = new List<StageAsset>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
                changed = true;
            }

            if (catalog.stages == null)
            {
                catalog.stages = new List<StageAsset>();
                changed = true;
            }

            int before = catalog.stages.Count;
            RemoveNullStages(catalog);
            if (catalog.stages.Count != before)
            {
                changed = true;
            }

            var removedIds = new List<string>();
            var liveIds = ReadEditStageIds(GetEditStageFolder());
            if (liveIds.Count > 0)
            {
                for (int i = catalog.stages.Count - 1; i >= 0; i--)
                {
                    var stage = catalog.stages[i];
                    if (stage == null || liveIds.Contains(stage.id))
                    {
                        continue;
                    }

                    removedIds.Add(stage.id);
                    catalog.stages.RemoveAt(i);
                    changed = true;
                }
            }

            if (catalog.stages.Count == 0 && !HasAnyStageFile())
            {
                var stage = StageFactory.CreateDefault();
                AssetDatabase.CreateAsset(stage, $"{StagesFolder}/Stage_1.asset");
                catalog.stages.Add(stage);
                changed = true;
            }

            if (changed)
            {
                EditorUtility.SetDirty(catalog);
                AssetDatabase.SaveAssets();
            }

            for (int i = 0; i < removedIds.Count; i++)
            {
                DeleteStageAsset(removedIds[i]);
            }

            return changed;
#endif
        }

        static HashSet<string> ReadEditStageIds(string folder)
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder))
            {
                return ids;
            }

            string[] files = GetStageJsonFiles(folder);
            for (int i = 0; i < files.Length; i++)
            {
                if (TryReadEditStage(files[i], out string id, out _))
                {
                    ids.Add(id);
                }
            }

            return ids;
        }

        static void RemoveStagesExcept(StageCatalog catalog, HashSet<string> liveIds)
        {
            if (catalog?.stages == null || liveIds == null || liveIds.Count == 0)
            {
                return;
            }

            for (int i = catalog.stages.Count - 1; i >= 0; i--)
            {
                var stage = catalog.stages[i];
                if (stage == null || !liveIds.Contains(stage.id))
                {
                    catalog.stages.RemoveAt(i);
                }
            }
        }

        static void RemoveNullStages(StageCatalog catalog)
        {
            if (catalog?.stages == null)
            {
                return;
            }

            for (int i = catalog.stages.Count - 1; i >= 0; i--)
            {
                if (catalog.stages[i] == null)
                {
                    catalog.stages.RemoveAt(i);
                }
            }
        }

        static bool IsRemovedEditFile(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return false;
            }

            string folder = GetEditStageFolder();
            if (!Directory.Exists(folder) || FindEditStageFile(id) != null)
            {
                return false;
            }

            return GetStageJsonFiles(folder).Length > 0;
        }

        static void DeleteEditFile(string id)
        {
            try
            {
                string folder = GetEditStageFolder();
                string[] files = GetStageJsonFiles(folder);
                for (int i = 0; i < files.Length; i++)
                {
                    if (TryReadEditStage(files[i], out string fileId, out _) && fileId == id)
                    {
                        File.Delete(files[i]);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"ステージファイルを消せませんでした: {e.Message}");
            }
        }

        static void RenameStageAsset(string oldId, string newId)
        {
#if UNITY_EDITOR
            if (string.IsNullOrWhiteSpace(oldId) || string.IsNullOrWhiteSpace(newId) || oldId == newId)
            {
                return;
            }

            string oldRelative = $"{StagesFolder}/{SanitizeFileName(oldId)}.asset";
            string newRelative = $"{StagesFolder}/{SanitizeFileName(newId)}.asset";
            if (string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(oldRelative)))
            {
                return;
            }

            if (!string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(newRelative)) && oldRelative != newRelative)
            {
                return;
            }

            string renameError = AssetDatabase.RenameAsset(oldRelative, SanitizeFileName(newId));
            if (!string.IsNullOrEmpty(renameError))
            {
                Debug.LogWarning($"ステージアセットの名前を変更できませんでした: {renameError}");
                return;
            }

            var asset = AssetDatabase.LoadAssetAtPath<StageAsset>(newRelative);
            if (asset != null)
            {
                asset.id = newId;
                asset.name = newId;
                EditorUtility.SetDirty(asset);
                AssetDatabase.SaveAssets();
            }
#else
            _ = oldId;
            _ = newId;
#endif
        }

        static void DeleteStageAsset(string id)
        {
#if UNITY_EDITOR
            if (string.IsNullOrWhiteSpace(id))
            {
                return;
            }

            string relative = $"{StagesFolder}/{SanitizeFileName(id)}.asset";
            if (!string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(relative)))
            {
                AssetDatabase.DeleteAsset(relative);
            }

            string full = Path.GetFullPath(Path.Combine(GetExecutableDirectory(), relative));
            DeleteFileIfExists(full);
            DeleteFileIfExists(full + ".meta");
#else
            _ = id;
#endif
        }

        static void DeleteFileIfExists(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"ステージファイルを消せませんでした: {e.Message}");
            }
        }

        static bool HasAnyStageFile()
        {
            string stages = Path.GetFullPath(Path.Combine(GetExecutableDirectory(), StagesFolder));
            if (Directory.Exists(stages) && Directory.GetFiles(stages, "*.asset").Length > 0)
            {
                return true;
            }

            string folder = GetEditStageFolder();
            return GetStageJsonFiles(folder).Length > 0;
        }

        public static void OrderStagesForPlay(StageCatalog catalog)
        {
            if (catalog?.stages == null || catalog.stages.Count < 2)
            {
                return;
            }

            var entries = new List<StageAsset>();
            for (int i = 0; i < catalog.stages.Count; i++)
            {
                if (catalog.stages[i] != null)
                {
                    entries.Add(catalog.stages[i]);
                }
            }

            if (entries.Count < 2)
            {
                return;
            }

            var fileOrders = ReadFileOrderById();
            var prefixed = new List<OrderedStage>();
            var unprefixed = new List<StageAsset>();
            for (int i = 0; i < entries.Count; i++)
            {
                if (TryGetExplicitOrder(entries[i], fileOrders, out int order))
                {
                    prefixed.Add(new OrderedStage(entries[i], order, i));
                }
                else
                {
                    unprefixed.Add(entries[i]);
                }
            }

            if (prefixed.Count == 0)
            {
                ReplaceStages(catalog, OrderBaseline(entries));
                return;
            }

            int count = entries.Count;
            var slots = new StageAsset[count];
            var taken = new bool[count];
            prefixed.Sort((a, b) =>
            {
                int compared = a.Order.CompareTo(b.Order);
                return compared != 0 ? compared : a.Original.CompareTo(b.Original);
            });

            for (int i = 0; i < prefixed.Count; i++)
            {
                int desired = Mathf.Clamp(prefixed[i].Order - 1, 0, count - 1);
                while (desired < count && taken[desired])
                {
                    desired++;
                }

                if (desired >= count)
                {
                    desired = 0;
                    while (desired < count && taken[desired])
                    {
                        desired++;
                    }
                }

                if (desired >= count)
                {
                    continue;
                }

                taken[desired] = true;
                slots[desired] = prefixed[i].Stage;
            }

            var fill = OrderBaseline(unprefixed);
            int fillIndex = 0;
            var ordered = new List<StageAsset>(count);
            for (int i = 0; i < count; i++)
            {
                StageAsset stage = slots[i];
                if (stage == null && fillIndex < fill.Count)
                {
                    stage = fill[fillIndex++];
                }

                if (stage != null)
                {
                    ordered.Add(stage);
                }
            }

            ReplaceStages(catalog, ordered);
        }

        public static bool TryApplyStageOrder(StageCatalog catalog, IList<string> orderedIds, out string error)
        {
            error = null;
            if (catalog?.stages == null)
            {
                error = "ステージ一覧がありません。";
                return false;
            }

            if (orderedIds == null || orderedIds.Count == 0)
            {
                error = "順番を変更できません。";
                return false;
            }

            var byId = new Dictionary<string, StageAsset>(StringComparer.Ordinal);
            for (int i = 0; i < catalog.stages.Count; i++)
            {
                var stage = catalog.stages[i];
                if (stage != null && !string.IsNullOrEmpty(stage.id) && !byId.ContainsKey(stage.id))
                {
                    byId.Add(stage.id, stage);
                }
            }

            var next = new List<StageAsset>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < orderedIds.Count; i++)
            {
                string id = orderedIds[i];
                if (string.IsNullOrEmpty(id) || !seen.Add(id) || !byId.TryGetValue(id, out StageAsset stage))
                {
                    continue;
                }

                next.Add(stage);
            }

            for (int i = 0; i < catalog.stages.Count; i++)
            {
                var stage = catalog.stages[i];
                if (stage != null && !string.IsNullOrEmpty(stage.id) && seen.Add(stage.id))
                {
                    next.Add(stage);
                }
            }

            if (next.Count == 0)
            {
                error = "順番を変更できません。";
                return false;
            }

            ReplaceStages(catalog, next);
            if (!WriteOrderPrefixes(catalog))
            {
                error = "順番の保存に失敗しました。";
                return false;
            }

            return true;
        }

        public static int ResolveNextIndex(StageCatalog catalog, string currentId, int fallbackIndex)
        {
            if (catalog == null || catalog.Count == 0)
            {
                return 0;
            }

            int current = catalog.IndexOfId(currentId);
            if (current < 0)
            {
                current = fallbackIndex;
            }

            int count = catalog.Count;
            return ((current + 1) % count + count) % count;
        }

        struct OrderedStage
        {
            public OrderedStage(StageAsset stage, int order, int original)
            {
                Stage = stage;
                Order = order;
                Original = original;
            }

            public StageAsset Stage { get; }
            public int Order { get; }
            public int Original { get; }
        }

        static void ReplaceStages(StageCatalog catalog, List<StageAsset> stages)
        {
            catalog.stages.Clear();
            catalog.stages.AddRange(stages);
        }

        static List<StageAsset> OrderBaseline(List<StageAsset> stages)
        {
            var leading = new List<StageAsset>();
            var numbered = new List<StageAsset>();
            var trailing = new List<StageAsset>();
            bool seenNumbered = false;
            for (int i = 0; i < stages.Count; i++)
            {
                var stage = stages[i];
                if (stage == null)
                {
                    continue;
                }

                if (TryGetStageNumber(stage.id, out _))
                {
                    seenNumbered = true;
                    numbered.Add(stage);
                }
                else if (!seenNumbered)
                {
                    leading.Add(stage);
                }
                else
                {
                    trailing.Add(stage);
                }
            }

            numbered.Sort((a, b) =>
            {
                TryGetStageNumber(a.id, out int an);
                TryGetStageNumber(b.id, out int bn);
                return an.CompareTo(bn);
            });

            var ordered = new List<StageAsset>(leading.Count + numbered.Count + trailing.Count);
            ordered.AddRange(leading);
            ordered.AddRange(numbered);
            ordered.AddRange(trailing);
            return ordered;
        }

        static bool TryGetExplicitOrder(StageAsset stage, Dictionary<string, int> fileOrders, out int order)
        {
            order = 0;
            if (stage == null || string.IsNullOrEmpty(stage.id))
            {
                return false;
            }

            if (fileOrders != null && fileOrders.TryGetValue(stage.id, out order))
            {
                return true;
            }

            return TryParseOrderPrefix(stage.id, out order, out _);
        }

        public static bool TryParseOrderPrefix(string name, out int order, out string remainder)
        {
            order = 0;
            remainder = null;
            if (string.IsNullOrWhiteSpace(name))
            {
                return false;
            }

            Match match = OrderPrefixRegex.Match(name.Trim());
            if (!match.Success || !int.TryParse(match.Groups[1].Value, out order) || order < 1)
            {
                return false;
            }

            remainder = match.Groups[2].Value;
            return !string.IsNullOrWhiteSpace(remainder);
        }

        public static string DisplayName(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return id;
            }

            return TryParseOrderPrefix(id, out _, out string remainder) ? remainder : id.Trim();
        }

        static string[] GetStageJsonFiles(string folder)
        {
            if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder))
            {
                return new string[0];
            }

            string[] files = Directory.GetFiles(folder, "*.json");
            var list = new List<string>(files.Length);
            for (int i = 0; i < files.Length; i++)
            {
                string name = Path.GetFileName(files[i]);
                if (string.IsNullOrEmpty(name) || name[0] == '.')
                {
                    continue;
                }

                list.Add(files[i]);
            }

            return list.ToArray();
        }

        static bool TryReadEditStage(string path, out string id, out int? order)
        {
            id = null;
            order = null;
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                return false;
            }

            string stem = Path.GetFileNameWithoutExtension(path);
            if (TryParseOrderPrefix(stem, out int fileOrder, out _))
            {
                order = fileOrder;
            }

            try
            {
                string json = File.ReadAllText(path);
                if (!string.IsNullOrWhiteSpace(json))
                {
                    var data = JsonUtility.FromJson<StageFileData>(json);
                    if (data != null && !string.IsNullOrWhiteSpace(data.id))
                    {
                        id = data.id.Trim();
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"ステージデータを読めませんでした: {path}\n{e.Message}");
                return false;
            }

            if (string.IsNullOrEmpty(id))
            {
                if (TryParseOrderPrefix(stem, out _, out string remainder))
                {
                    id = remainder.Trim();
                }
                else
                {
                    id = stem;
                }
            }

            return !string.IsNullOrEmpty(id);
        }

        static Dictionary<string, int> ReadFileOrderById()
        {
            var orders = new Dictionary<string, int>(StringComparer.Ordinal);
            string[] files = GetStageJsonFiles(GetEditStageFolder());
            for (int i = 0; i < files.Length; i++)
            {
                if (TryReadEditStage(files[i], out string id, out int? order) && order.HasValue)
                {
                    orders[id] = order.Value;
                }
            }

            return orders;
        }

        static string FindEditStageFile(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return null;
            }

            string[] files = GetStageJsonFiles(GetEditStageFolder());
            for (int i = 0; i < files.Length; i++)
            {
                if (TryReadEditStage(files[i], out string fileId, out _) && fileId == id)
                {
                    return files[i];
                }
            }

            return null;
        }

        static void DeleteDuplicateEditFiles(string id, string keepPath)
        {
            string[] files = GetStageJsonFiles(GetEditStageFolder());
            for (int i = 0; i < files.Length; i++)
            {
                if (string.Equals(files[i], keepPath, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (TryReadEditStage(files[i], out string fileId, out _) && fileId == id)
                {
                    DeleteFileIfExists(files[i]);
                }
            }
        }

        static bool WriteOrderPrefixes(StageCatalog catalog)
        {
            if (catalog?.stages == null)
            {
                return false;
            }

            string folder = EnsureEditStageFolder();
            var planned = new List<KeyValuePair<string, string>>();
            for (int i = 0; i < catalog.stages.Count; i++)
            {
                var stage = catalog.stages[i];
                if (stage == null || string.IsNullOrWhiteSpace(stage.id))
                {
                    continue;
                }

                string current = FindEditStageFile(stage.id);
                if (string.IsNullOrEmpty(current))
                {
                    continue;
                }

                string finalPath = Path.Combine(folder, (i + 1) + "_" + SanitizeFileName(stage.id) + ".json");
                if (string.Equals(Path.GetFullPath(current), Path.GetFullPath(finalPath), StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                planned.Add(new KeyValuePair<string, string>(current, finalPath));
            }

            var temps = new List<KeyValuePair<string, string>>();
            try
            {
                for (int i = 0; i < planned.Count; i++)
                {
                    string temp = Path.Combine(folder, ".reorder_" + Guid.NewGuid().ToString("N") + ".json");
                    File.Move(planned[i].Key, temp);
                    temps.Add(new KeyValuePair<string, string>(temp, planned[i].Value));
                }

                for (int i = 0; i < temps.Count; i++)
                {
                    if (File.Exists(temps[i].Value))
                    {
                        File.Delete(temps[i].Value);
                    }

                    File.Move(temps[i].Key, temps[i].Value);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"ステージの順番を保存できませんでした: {e.Message}");
                for (int i = 0; i < temps.Count; i++)
                {
                    if (!File.Exists(temps[i].Key))
                    {
                        continue;
                    }

                    try
                    {
                        if (!File.Exists(planned[i].Key))
                        {
                            File.Move(temps[i].Key, planned[i].Key);
                        }
                    }
                    catch (Exception restoreError)
                    {
                        Debug.LogWarning($"ステージファイルを戻せませんでした: {restoreError.Message}");
                    }
                }

                return false;
            }

            for (int i = 0; i < catalog.stages.Count; i++)
            {
                var stage = catalog.stages[i];
                if (stage == null)
                {
                    continue;
                }

                string finalPath = Path.Combine(folder, (i + 1) + "_" + SanitizeFileName(stage.id) + ".json");
                if (File.Exists(finalPath))
                {
                    DeleteDuplicateEditFiles(stage.id, finalPath);
                }
            }

            return true;
        }

        public static bool TryGetStageNumber(string id, out int number)
        {
            number = 0;
            if (string.IsNullOrWhiteSpace(id))
            {
                return false;
            }

            Match match = StageNumberRegex.Match(id.Trim());
            return match.Success && int.TryParse(match.Groups[1].Value, out number);
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

            if (IsRemovedEditFile(target.id))
            {
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
                foreach (string file in GetStageJsonFiles(folder))
                {
                    ConsiderStageName(Path.GetFileNameWithoutExtension(file), ref max);
                    if (TryReadEditStage(file, out string id, out _))
                    {
                        ConsiderStageName(id, ref max);
                    }
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
                string path = FindEditStageFile(stage.id);
                if (string.IsNullOrEmpty(path))
                {
                    path = Path.Combine(folder, FileNameFor(stage.id));
                }

                var data = StageFileData.From(stage);
                File.WriteAllText(path, JsonUtility.ToJson(data, true));
                DeleteDuplicateEditFiles(stage.id, path);
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

        static string SanitizeFileName(string id)
        {
            string safe = string.IsNullOrWhiteSpace(id) ? "Stage" : id.Trim();
            char[] invalid = Path.GetInvalidFileNameChars();
            for (int i = 0; i < invalid.Length; i++)
            {
                safe = safe.Replace(invalid[i], '_');
            }

            return safe;
        }

        static string FileNameFor(string id)
        {
            return SanitizeFileName(id) + ".json";
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
            public MeilingSetting[] meilings;

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
                    rows = stage.rows != null ? (string[])stage.rows.Clone() : null,
                    meilings = StageAsset.CloneMeilings(stage.meilings)
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
                asset.meilings = StageAsset.CloneMeilings(meilings);
                return asset;
            }
        }
    }
}

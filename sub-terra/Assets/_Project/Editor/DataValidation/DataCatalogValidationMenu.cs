using SubTerra.App.Core.Data;
using UnityEditor;
using UnityEngine;

namespace SubTerra.App.Editor.DataValidation
{
    /// <summary>
    /// Editor 전용 데이터 검증 메뉴. 런타임과 동일한 CatalogValidator를 호출한다.
    /// </summary>
    public static class DataCatalogValidationMenu
    {
        private const string DefaultCatalogPath = "Assets/_Project/Data/Catalog/GameDataCatalog.asset";

        [MenuItem("SubTerra/Data/Validate Game Data Catalog")]
        public static void ValidateDefaultCatalog()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<GameDataCatalog>(DefaultCatalogPath);
            if (catalog == null)
            {
                // 기본 경로에 없으면 프로젝트에서 첫 카탈로그를 찾는다(암묵 등록이 아니라 검증 대상 탐색).
                var guids = AssetDatabase.FindAssets("t:GameDataCatalog");
                if (guids == null || guids.Length == 0)
                {
                    Debug.LogError("[SubTerra] No GameDataCatalog asset found.");
                    return;
                }

                var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                catalog = AssetDatabase.LoadAssetAtPath<GameDataCatalog>(path);
            }

            ValidateCatalog(catalog);
        }

        [MenuItem("SubTerra/Data/Validate Selected Catalog")]
        public static void ValidateSelectedCatalog()
        {
            var catalog = Selection.activeObject as GameDataCatalog;
            if (catalog == null)
            {
                Debug.LogError("[SubTerra] Select a GameDataCatalog asset first.");
                return;
            }

            ValidateCatalog(catalog);
        }

        public static CatalogValidationResult ValidateCatalog(GameDataCatalog catalog)
        {
            if (catalog == null)
            {
                Debug.LogError("[SubTerra] Catalog is null.");
                return null;
            }

            var result = catalog.ValidateAll();
            ValidateUnregisteredAssets(catalog, result);
            // 런타임 검증기는 에셋 이름만 쓰므로, Editor에서는 실제 경로로 보강한다.
            var enriched = EnrichPaths(result);
            if (result.IsValid)
            {
                Debug.Log(
                    $"[SubTerra] Catalog validation passed. DictionaryInitialized={result.DictionaryInitialized}. " +
                    $"Minerals={catalog.Minerals.Count}, Buildings={catalog.Buildings.Count}, " +
                    $"Recipes={catalog.Recipes.Count}, Upgrades={catalog.Upgrades.Count}, Dialogues={catalog.Dialogues.Count}.");
            }
            else
            {
                Debug.LogError(
                    $"[SubTerra] Catalog validation failed. Errors={result.ErrorCount}, " +
                    $"DictionaryInitialized={result.DictionaryInitialized}.\n{enriched}");
            }

            return result;
        }

        private static void ValidateUnregisteredAssets(
            GameDataCatalog catalog,
            CatalogValidationResult result)
        {
            var found = false;
            found |= ReportUnregistered(catalog.Minerals, "MineralData", "mineral", result);
            found |= ReportUnregistered(catalog.Buildings, "BuildingData", "building", result);
            found |= ReportUnregistered(catalog.Recipes, "RecipeData", "recipe", result);
            found |= ReportUnregistered(catalog.Upgrades, "UpgradeData", "upgrade", result);
            found |= ReportUnregistered(catalog.Dialogues, "DialogueTemplateData", "dialogue", result);
            if (found)
            {
                result.SetDictionaryInitialized(false);
            }
        }

        private static bool ReportUnregistered<T>(
            System.Collections.Generic.IReadOnlyList<T> registered,
            string assetType,
            string label,
            CatalogValidationResult result) where T : Object
        {
            var registeredSet = new System.Collections.Generic.HashSet<T>();
            if (registered != null)
            {
                for (var i = 0; i < registered.Count; i++)
                {
                    if (registered[i] != null)
                    {
                        registeredSet.Add(registered[i]);
                    }
                }
            }

            var found = false;
            var guids = AssetDatabase.FindAssets(
                "t:" + assetType,
                new[] { "Assets/_Project/Data" });
            for (var i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var asset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset == null || registeredSet.Contains(asset))
                {
                    continue;
                }

                found = true;
                result.AddError(
                    path,
                    "catalogRegistration",
                    $"Unregistered {label} data asset.");
            }

            return found;
        }

        private static string EnrichPaths(CatalogValidationResult result)
        {
            if (result == null || result.Issues.Count == 0)
            {
                return "No issues.";
            }

            var lines = new System.Text.StringBuilder();
            for (var i = 0; i < result.Issues.Count; i++)
            {
                var issue = result.Issues[i];
                var path = issue.AssetPath;
                if (!string.IsNullOrEmpty(path) && !path.StartsWith("Assets/", System.StringComparison.Ordinal))
                {
                    var guids = AssetDatabase.FindAssets(path);
                    for (var g = 0; g < guids.Length; g++)
                    {
                        var candidate = AssetDatabase.GUIDToAssetPath(guids[g]);
                        if (System.IO.Path.GetFileNameWithoutExtension(candidate) == path)
                        {
                            path = candidate;
                            break;
                        }
                    }
                }

                if (i > 0)
                {
                    lines.AppendLine();
                }

                var field = string.IsNullOrEmpty(issue.FieldName) ? "-" : issue.FieldName;
                lines.Append($"[{issue.Severity}] {path} :: {field} — {issue.Message}");
            }

            return lines.ToString();
        }
    }
}

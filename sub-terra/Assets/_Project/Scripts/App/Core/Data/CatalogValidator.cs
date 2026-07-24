using System.Collections.Generic;
using UnityEngine;

namespace SubTerra.App.Core.Data
{
    /// <summary>
    /// 카탈로그 검증 순수 로직. 첫 오류에서 중단하지 않고 이슈를 모두 모은다.
    /// Editor 메뉴와 런타임 Bootstrap이 동일 진입점을 사용한다.
    /// </summary>
    public static class CatalogValidator
    {
        public static CatalogValidationResult Validate(GameDataCatalog catalog)
        {
            var result = new CatalogValidationResult();
            if (catalog == null)
            {
                result.AddError(string.Empty, "catalog", "GameDataCatalog is null.");
                result.SetDictionaryInitialized(false);
                return result;
            }

            var minerals = catalog.Minerals;
            var buildings = catalog.Buildings;
            var recipes = catalog.Recipes;
            var upgrades = catalog.Upgrades;
            var dialogues = catalog.Dialogues;

            ValidateMinerals(minerals, result);
            ValidateBuildings(buildings, result);
            ValidateRecipes(recipes, result);
            ValidateUpgrades(upgrades, result);
            ValidateDialogues(dialogues, result);
            ValidateReferences(minerals, buildings, recipes, upgrades, result);
            ValidateRequiredMvpIds(minerals, buildings, recipes, upgrades, dialogues, result);

            // 중복을 포함한 검증 오류가 하나라도 있으면 조회 Dictionary를 성공 상태로 공개하지 않는다.
            var hasDuplicate = HasAnyDuplicateIds(minerals, buildings, recipes, upgrades, dialogues, result);
            result.SetDictionaryInitialized(!hasDuplicate && result.ErrorCount == 0);

            return result;
        }

        private static bool HasAnyDuplicateIds(
            IReadOnlyList<MineralData> minerals,
            IReadOnlyList<BuildingData> buildings,
            IReadOnlyList<RecipeData> recipes,
            IReadOnlyList<UpgradeData> upgrades,
            IReadOnlyList<DialogueTemplateData> dialogues,
            CatalogValidationResult result)
        {
            var found = false;
            found |= ReportDuplicates(CollectIds(minerals, m => m != null ? m.Id : null, GetPath), result, "mineral");
            found |= ReportDuplicates(CollectIds(buildings, b => b != null ? b.Id : null, GetPath), result, "building");
            found |= ReportDuplicates(CollectIds(recipes, r => r != null ? r.Id : null, GetPath), result, "recipe");
            found |= ReportDuplicates(CollectIds(upgrades, u => u != null ? u.Id : null, GetPath), result, "upgrade");
            found |= ReportDuplicates(CollectIds(dialogues, d => d != null ? d.Id : null, GetPath), result, "dialogue");
            found |= ReportCrossTypeDuplicates(minerals, buildings, recipes, upgrades, dialogues, result);
            return found;
        }

        private static bool ReportCrossTypeDuplicates(
            IReadOnlyList<MineralData> minerals,
            IReadOnlyList<BuildingData> buildings,
            IReadOnlyList<RecipeData> recipes,
            IReadOnlyList<UpgradeData> upgrades,
            IReadOnlyList<DialogueTemplateData> dialogues,
            CatalogValidationResult result)
        {
            var entries = new List<(string Id, string Type, string Path)>();
            AddTypedIds(entries, minerals, m => m != null ? m.Id : null, "mineral");
            AddTypedIds(entries, buildings, b => b != null ? b.Id : null, "building");
            AddTypedIds(entries, recipes, r => r != null ? r.Id : null, "recipe");
            AddTypedIds(entries, upgrades, u => u != null ? u.Id : null, "upgrade");
            AddTypedIds(entries, dialogues, d => d != null ? d.Id : null, "dialogue");

            var found = false;
            var byId = new Dictionary<string, List<(string Type, string Path)>>();
            for (var i = 0; i < entries.Count; i++)
            {
                if (!byId.TryGetValue(entries[i].Id, out var uses))
                {
                    uses = new List<(string Type, string Path)>();
                    byId[entries[i].Id] = uses;
                }

                uses.Add((entries[i].Type, entries[i].Path));
            }

            foreach (var pair in byId)
            {
                var types = new HashSet<string>();
                for (var i = 0; i < pair.Value.Count; i++)
                {
                    types.Add(pair.Value[i].Type);
                }

                if (types.Count < 2)
                {
                    continue;
                }

                found = true;
                for (var i = 0; i < pair.Value.Count; i++)
                {
                    result.AddError(
                        pair.Value[i].Path,
                        "id",
                        $"ID '{pair.Key}' is reused across data types: {string.Join(", ", types)}.");
                }
            }

            return found;
        }

        private static void AddTypedIds<T>(
            List<(string Id, string Type, string Path)> entries,
            IReadOnlyList<T> source,
            System.Func<T, string> idOf,
            string type) where T : Object
        {
            if (source == null)
            {
                return;
            }

            for (var i = 0; i < source.Count; i++)
            {
                var item = source[i];
                var id = item != null ? idOf(item) : null;
                if (!string.IsNullOrEmpty(id))
                {
                    entries.Add((id, type, GetPath(item)));
                }
            }
        }

        private static List<(string Id, string Path)> CollectIds<T>(
            IReadOnlyList<T> list,
            System.Func<T, string> idSelector,
            System.Func<Object, string> pathSelector) where T : Object
        {
            var items = new List<(string Id, string Path)>();
            if (list == null)
            {
                return items;
            }

            for (var i = 0; i < list.Count; i++)
            {
                var entry = list[i];
                if (entry == null)
                {
                    continue;
                }

                var id = idSelector(entry);
                if (string.IsNullOrEmpty(id))
                {
                    continue;
                }

                items.Add((id, pathSelector(entry)));
            }

            return items;
        }

        private static bool ReportDuplicates(
            List<(string Id, string Path)> items,
            CatalogValidationResult result,
            string typeLabel)
        {
            var found = false;
            var map = new Dictionary<string, List<string>>();
            for (var i = 0; i < items.Count; i++)
            {
                var id = items[i].Id;
                if (!map.TryGetValue(id, out var paths))
                {
                    paths = new List<string>();
                    map[id] = paths;
                }

                paths.Add(items[i].Path);
            }

            foreach (var pair in map)
            {
                if (pair.Value.Count < 2)
                {
                    continue;
                }

                found = true;
                // 각 중복 에셋 경로를 결과에 포함해 어느 에셋인지 바로 찾게 한다.
                for (var i = 0; i < pair.Value.Count; i++)
                {
                    result.AddError(
                        pair.Value[i],
                        "id",
                        $"Duplicate {typeLabel} id '{pair.Key}' also used by: {string.Join(", ", pair.Value)}");
                }
            }

            return found;
        }

        private static void ValidateMinerals(IReadOnlyList<MineralData> minerals, CatalogValidationResult result)
        {
            if (minerals == null)
            {
                return;
            }

            for (var i = 0; i < minerals.Count; i++)
            {
                var data = minerals[i];
                if (data == null)
                {
                    result.AddError(string.Empty, $"minerals[{i}]", "Null mineral entry in catalog list.");
                    continue;
                }

                var path = GetPath(data);
                ValidateIdField(data.Id, "mineral.", path, result);
                if (string.IsNullOrWhiteSpace(data.DisplayName))
                {
                    result.AddError(path, "displayName", "Display name is empty.");
                }

                if (data.UnitWeight <= 0f)
                {
                    result.AddError(path, "unitWeight", "Unit weight must be greater than 0.");
                }

                if (data.UnitPrice < 0)
                {
                    result.AddError(path, "unitPrice", "Unit price must not be negative.");
                }

                if (data.Icon == null)
                {
                    result.AddError(path, "icon", "Required mineral icon is missing.");
                }
            }
        }

        private static void ValidateBuildings(IReadOnlyList<BuildingData> buildings, CatalogValidationResult result)
        {
            if (buildings == null)
            {
                return;
            }

            for (var i = 0; i < buildings.Count; i++)
            {
                var data = buildings[i];
                if (data == null)
                {
                    result.AddError(string.Empty, $"buildings[{i}]", "Null building entry in catalog list.");
                    continue;
                }

                var path = GetPath(data);
                ValidateIdField(data.Id, "building.", path, result);
                if (string.IsNullOrWhiteSpace(data.DisplayName))
                {
                    result.AddError(path, "displayName", "Display name is empty.");
                }

                // 필수 Runtime Prefab: 누락 시 에셋 경로와 필드명을 보고한다.
                if (data.RuntimePrefab == null)
                {
                    result.AddError(path, "runtimePrefab", "Required runtime prefab is missing.");
                }

                if (data.Icon == null)
                {
                    result.AddError(path, "icon", "Required building icon is missing.");
                }

                if (data.PowerDraw < 0)
                {
                    result.AddError(path, "powerDraw", "Power draw must not be negative.");
                }

                ValidateCosts(data.BuildCosts, path, "buildCosts", result, allowEmpty: false);
            }
        }

        private static void ValidateRecipes(IReadOnlyList<RecipeData> recipes, CatalogValidationResult result)
        {
            if (recipes == null)
            {
                return;
            }

            for (var i = 0; i < recipes.Count; i++)
            {
                var data = recipes[i];
                if (data == null)
                {
                    result.AddError(string.Empty, $"recipes[{i}]", "Null recipe entry in catalog list.");
                    continue;
                }

                var path = GetPath(data);
                ValidateIdField(data.Id, "recipe.", path, result);
                if (string.IsNullOrWhiteSpace(data.DisplayName))
                {
                    result.AddError(path, "displayName", "Display name is empty.");
                }

                if (data.Inputs == null || data.Inputs.Count == 0)
                {
                    result.AddError(path, "inputs", "Recipe inputs are empty.");
                }
                else
                {
                    ValidateCosts(data.Inputs, path, "inputs", result, allowEmpty: false);
                }

                if (data.Outputs == null || data.Outputs.Count == 0)
                {
                    if (string.IsNullOrEmpty(data.ResultBuildingId))
                    {
                        result.AddError(path, "outputs", "Recipe has no outputs and no result building id.");
                    }
                }

                if (!string.IsNullOrEmpty(data.ResultBuildingId) &&
                    !DataIdRules.IsValidPermanentId(data.ResultBuildingId))
                {
                    result.AddError(
                        path,
                        "resultBuildingId",
                        DataIdRules.DescribeInvalid(data.ResultBuildingId));
                }
                else
                {
                    ValidateCosts(data.Outputs, path, "outputs", result, allowEmpty: false);
                }
            }
        }

        private static void ValidateUpgrades(IReadOnlyList<UpgradeData> upgrades, CatalogValidationResult result)
        {
            if (upgrades == null)
            {
                return;
            }

            for (var i = 0; i < upgrades.Count; i++)
            {
                var data = upgrades[i];
                if (data == null)
                {
                    result.AddError(string.Empty, $"upgrades[{i}]", "Null upgrade entry in catalog list.");
                    continue;
                }

                var path = GetPath(data);
                ValidateIdField(data.Id, "upgrade.", path, result);
                if (string.IsNullOrWhiteSpace(data.DisplayName))
                {
                    result.AddError(path, "displayName", "Display name is empty.");
                }

                if (data.MaxLevel <= 0)
                {
                    result.AddError(path, "maxLevel", "Max level must be greater than 0.");
                }

                if (data.Levels == null || data.Levels.Count == 0)
                {
                    result.AddError(path, "levels", "Upgrade levels are empty.");
                    continue;
                }

                if (data.Levels.Count != data.MaxLevel)
                {
                    result.AddError(path, "levels", "Upgrade level count must match maxLevel.");
                }

                for (var l = 0; l < data.Levels.Count; l++)
                {
                    var level = data.Levels[l];
                    if (level == null)
                    {
                        result.AddError(path, $"levels[{l}]", "Null upgrade level entry.");
                        continue;
                    }

                    if (level.Level <= 0)
                    {
                        result.AddError(path, $"levels[{l}].level", "Upgrade level index must be greater than 0.");
                    }

                    if (level.Level != l + 1)
                    {
                        result.AddError(
                            path,
                            $"levels[{l}].level",
                            $"Upgrade levels must be sequential; expected {l + 1}.");
                    }

                    if (level.EffectValue <= 0f)
                    {
                        result.AddError(path, $"levels[{l}].effectValue", "Effect value must be greater than 0.");
                    }

                    ValidateCosts(level.Costs, path, $"levels[{l}].costs", result, allowEmpty: false);
                }
            }
        }

        private static void ValidateReferences(
            IReadOnlyList<MineralData> minerals,
            IReadOnlyList<BuildingData> buildings,
            IReadOnlyList<RecipeData> recipes,
            IReadOnlyList<UpgradeData> upgrades,
            CatalogValidationResult result)
        {
            var mineralIds = CollectIdSet(minerals, m => m != null ? m.Id : null);
            var buildingIds = CollectIdSet(buildings, b => b != null ? b.Id : null);

            if (buildings != null)
            {
                for (var i = 0; i < buildings.Count; i++)
                {
                    var data = buildings[i];
                    if (data != null)
                    {
                        ValidateRegisteredCosts(
                            data.BuildCosts,
                            mineralIds,
                            GetPath(data),
                            "buildCosts",
                            result);
                    }
                }
            }

            if (recipes != null)
            {
                for (var i = 0; i < recipes.Count; i++)
                {
                    var data = recipes[i];
                    if (data == null)
                    {
                        continue;
                    }

                    var path = GetPath(data);
                    ValidateRegisteredCosts(data.Inputs, mineralIds, path, "inputs", result);
                    ValidateRegisteredCosts(
                        data.Outputs,
                        MergeIds(mineralIds, buildingIds),
                        path,
                        "outputs",
                        result);

                    if (!string.IsNullOrEmpty(data.ResultBuildingId) &&
                        !buildingIds.Contains(data.ResultBuildingId))
                    {
                        result.AddError(
                            path,
                            "resultBuildingId",
                            $"Referenced building id '{data.ResultBuildingId}' is not registered.");
                    }
                }
            }

            if (upgrades != null)
            {
                for (var i = 0; i < upgrades.Count; i++)
                {
                    var data = upgrades[i];
                    if (data == null || data.Levels == null)
                    {
                        continue;
                    }

                    for (var level = 0; level < data.Levels.Count; level++)
                    {
                        var definition = data.Levels[level];
                        if (definition != null)
                        {
                            ValidateRegisteredCosts(
                                definition.Costs,
                                mineralIds,
                                GetPath(data),
                                $"levels[{level}].costs",
                                result);
                        }
                    }
                }
            }
        }

        private static HashSet<string> CollectIdSet<T>(
            IReadOnlyList<T> list,
            System.Func<T, string> idOf)
        {
            var ids = new HashSet<string>();
            if (list == null)
            {
                return ids;
            }

            for (var i = 0; i < list.Count; i++)
            {
                var id = idOf(list[i]);
                if (!string.IsNullOrEmpty(id))
                {
                    ids.Add(id);
                }
            }

            return ids;
        }

        private static HashSet<string> MergeIds(HashSet<string> first, HashSet<string> second)
        {
            var merged = new HashSet<string>(first);
            merged.UnionWith(second);
            return merged;
        }

        private static void ValidateRegisteredCosts(
            IReadOnlyList<ItemCostEntry> costs,
            HashSet<string> registeredIds,
            string assetPath,
            string fieldName,
            CatalogValidationResult result)
        {
            if (costs == null)
            {
                return;
            }

            for (var i = 0; i < costs.Count; i++)
            {
                var id = costs[i].ItemId;
                if (!string.IsNullOrEmpty(id) && !registeredIds.Contains(id))
                {
                    result.AddError(
                        assetPath,
                        $"{fieldName}[{i}].itemId",
                        $"Referenced item id '{id}' is not registered.");
                }
            }
        }

        private static void ValidateDialogues(
            IReadOnlyList<DialogueTemplateData> dialogues,
            CatalogValidationResult result)
        {
            if (dialogues == null)
            {
                return;
            }

            for (var i = 0; i < dialogues.Count; i++)
            {
                var data = dialogues[i];
                if (data == null)
                {
                    result.AddError(string.Empty, $"dialogues[{i}]", "Null dialogue entry in catalog list.");
                    continue;
                }

                var path = GetPath(data);
                ValidateIdField(data.Id, "dialogue.", path, result);
                if (string.IsNullOrWhiteSpace(data.DisplayName))
                {
                    result.AddError(path, "displayName", "Display name is empty.");
                }

                if (string.IsNullOrWhiteSpace(data.SituationKey))
                {
                    result.AddError(path, "situationKey", "Situation key is empty.");
                }

                if (string.IsNullOrWhiteSpace(data.Template))
                {
                    result.AddError(path, "template", "Dialogue template text is empty.");
                }
            }
        }

        private static void ValidateCosts(
            IReadOnlyList<ItemCostEntry> costs,
            string assetPath,
            string fieldName,
            CatalogValidationResult result,
            bool allowEmpty)
        {
            if (costs == null || costs.Count == 0)
            {
                if (!allowEmpty)
                {
                    result.AddError(assetPath, fieldName, "Cost list is empty.");
                }

                return;
            }

            for (var i = 0; i < costs.Count; i++)
            {
                var cost = costs[i];
                if (string.IsNullOrWhiteSpace(cost.ItemId))
                {
                    result.AddError(assetPath, $"{fieldName}[{i}].itemId", "Cost item id is empty.");
                }
                else if (!DataIdRules.IsValidPermanentId(cost.ItemId))
                {
                    result.AddError(
                        assetPath,
                        $"{fieldName}[{i}].itemId",
                        DataIdRules.DescribeInvalid(cost.ItemId));
                }

                if (cost.Quantity <= 0)
                {
                    result.AddError(assetPath, $"{fieldName}[{i}].quantity", "Cost quantity must be greater than 0.");
                }
            }
        }

        private static void ValidateIdField(
            string id,
            string expectedPrefix,
            string assetPath,
            CatalogValidationResult result)
        {
            if (!DataIdRules.IsValidPermanentId(id))
            {
                result.AddError(assetPath, "id", DataIdRules.DescribeInvalid(id));
                return;
            }

            if (!id.StartsWith(expectedPrefix, System.StringComparison.Ordinal))
            {
                result.AddError(assetPath, "id", $"ID must start with '{expectedPrefix}'.");
            }
        }

        /// <summary>MVP 필수 영구 ID가 카탈로그 목록에 하나 이상 등록됐는지 확인한다.</summary>
        private static void ValidateRequiredMvpIds(
            IReadOnlyList<MineralData> minerals,
            IReadOnlyList<BuildingData> buildings,
            IReadOnlyList<RecipeData> recipes,
            IReadOnlyList<UpgradeData> upgrades,
            IReadOnlyList<DialogueTemplateData> dialogues,
            CatalogValidationResult result)
        {
            RequireIds(
                minerals,
                m => m != null ? m.Id : null,
                new[]
                {
                    DataIds.Minerals.Copper,
                    DataIds.Minerals.Iron,
                    DataIds.Minerals.Lithium
                },
                "mineral",
                result);

            RequireIds(
                buildings,
                b => b != null ? b.Id : null,
                new[]
                {
                    DataIds.Buildings.SupportBasic,
                    DataIds.Buildings.LightBasic,
                    DataIds.Buildings.ChargerBasic,
                    DataIds.Buildings.StorageBasic,
                    DataIds.Buildings.SettlementBasic,
                    DataIds.Buildings.OutpostCoreBasic
                },
                "building",
                result);

            RequireIds(
                recipes,
                r => r != null ? r.Id : null,
                new[] { DataIds.Recipes.SupportBasic },
                "recipe",
                result);

            RequireIds(
                upgrades,
                u => u != null ? u.Id : null,
                new[]
                {
                    DataIds.Upgrades.DrillSpeed,
                    DataIds.Upgrades.DrillEfficiency,
                    DataIds.Upgrades.DroneScan,
                    DataIds.Upgrades.DroneRescue
                },
                "upgrade",
                result);

            RequireIds(
                dialogues,
                d => d != null ? d.Id : null,
                new[] { DataIds.Dialogue.LowPowerWarning },
                "dialogue",
                result);
        }

        private static void RequireIds<T>(
            IReadOnlyList<T> list,
            System.Func<T, string> idOf,
            string[] required,
            string label,
            CatalogValidationResult result)
        {
            var present = new HashSet<string>();
            if (list != null)
            {
                for (var i = 0; i < list.Count; i++)
                {
                    var id = idOf(list[i]);
                    if (!string.IsNullOrEmpty(id))
                    {
                        present.Add(id);
                    }
                }
            }

            for (var i = 0; i < required.Length; i++)
            {
                if (!present.Contains(required[i]))
                {
                    result.AddError(
                        string.Empty,
                        "requiredIds",
                        $"Missing required {label} id '{required[i]}' in catalog registration.");
                }
            }
        }

        /// <summary>
        /// 런타임 어셈블리에서는 UnityEditor를 참조하지 않는다.
        /// 에셋 이름(또는 인스턴스명)으로 위치를 식별하고, Editor 메뉴는 필요 시 경로를 보강한다.
        /// </summary>
        private static string GetPath(Object asset)
        {
            if (asset == null)
            {
                return string.Empty;
            }

            return string.IsNullOrEmpty(asset.name) ? asset.GetType().Name : asset.name;
        }
    }
}

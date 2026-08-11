#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using SubTerra.App.Core.Data;
using SubTerra.App.Integration;
using SubTerra.Gameplay.Building;
using SubTerra.Shared;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SubTerra.App.Editor.DataValidation
{
    /// <summary>
    /// 시설 구매 UI가 선택한 BuildingData를 실제 배치 경로까지 연결한다.
    /// 비용의 원본은 BuildingData이며, 배치 정의에는 같은 비용을 복사해 A 배치 시스템의 최종 차감 검증에 사용한다.
    /// </summary>
    public static class FacilityPurchaseFlowBuilder
    {
        public const string IntegrationScenePath = "Assets/_Project/Scenes/App/Mine_Demo_Integration.unity";
        private const string CatalogPath = "Assets/_Project/Data/Catalog/GameDataCatalog.asset";
        private const string DefinitionFolder = "Assets/_Project/Data/Buildings/Placement";

        private static readonly string[] FacilityIds =
        {
            DataIds.Buildings.SupportBasic,
            DataIds.Buildings.LightBasic,
            DataIds.Buildings.ChargerBasic,
            DataIds.Buildings.StorageBasic,
            DataIds.Buildings.SettlementBasic,
            DataIds.Buildings.OutpostCoreBasic,
            DataIds.Buildings.EmergencyEscapePortal
        };

        [MenuItem("SubTerra/UI/Build Facility Purchase Flow")]
        public static void BuildFromMenu()
        {
            Debug.Log("[SubTerra] " + Build());
        }

        public static string Build()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<GameDataCatalog>(CatalogPath);
            if (catalog == null)
            {
                throw new InvalidOperationException("GameDataCatalog is missing.");
            }

            EnsureFolder("Assets/_Project/Data/Buildings", "Placement");
            var definitions = new List<BuildingPlacementDefinition>(FacilityIds.Length);
            for (var i = 0; i < FacilityIds.Length; i++)
            {
                if (!catalog.TryGetBuilding(FacilityIds[i], out var data)
                    || data == null
                    || data.RuntimePrefab == null)
                {
                    throw new InvalidOperationException("Missing runtime facility data: " + FacilityIds[i]);
                }

                definitions.Add(BuildDefinition(data));
            }

            WireIntegrationScene(definitions);
            RemoveSupersededSupportDefinition();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return "Facility purchase flow wired for " + definitions.Count + " facilities.";
        }

        private static BuildingPlacementDefinition BuildDefinition(BuildingData data)
        {
            var path = data.Id == DataIds.Buildings.SupportBasic
                ? PhaseFSupportBuilder.SupportDefinitionPath
                : DefinitionFolder + "/" + ToAssetName(data.Id) + "Placement.asset";
            var definition = AssetDatabase.LoadAssetAtPath<BuildingPlacementDefinition>(path);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<BuildingPlacementDefinition>();
                AssetDatabase.CreateAsset(definition, path);
            }

            definition.EditorSet(data.Id, data.RuntimePrefab, Vector2Int.one, true);
            definition.EditorSetCosts(ToCosts(data.BuildCosts));
            EditorUtility.SetDirty(definition);
            return definition;
        }

        private static void WireIntegrationScene(IReadOnlyList<BuildingPlacementDefinition> definitions)
        {
            var scene = SceneManager.GetSceneByPath(IntegrationScenePath);
            var wasLoaded = scene.IsValid() && scene.isLoaded;
            if (!wasLoaded)
            {
                scene = EditorSceneManager.OpenScene(IntegrationScenePath, OpenSceneMode.Additive);
            }

            try
            {
                var placement = FindInScene<BuildingPlacementSystem>(scene);
                var bridge = FindInScene<GameplayBuildingPlacementBridge>(scene);
                if (placement == null || bridge == null)
                {
                    throw new InvalidOperationException(
                        "Facility purchase flow requires BuildingPlacementSystem and GameplayBuildingPlacementBridge.");
                }

                var placementSo = new SerializedObject(placement);
                SetDefinitions(placementSo.FindProperty("restoreDefinitions"), definitions);
                placementSo.ApplyModifiedPropertiesWithoutUndo();

                var bridgeSo = new SerializedObject(bridge);
                SetBindings(bridgeSo.FindProperty("bindings"), definitions);
                bridgeSo.ApplyModifiedPropertiesWithoutUndo();

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            finally
            {
                if (!wasLoaded && scene.IsValid())
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        private static void SetDefinitions(SerializedProperty target, IReadOnlyList<BuildingPlacementDefinition> definitions)
        {
            var merged = new List<BuildingPlacementDefinition>();
            for (var i = 0; i < target.arraySize; i++)
            {
                var existing = target.GetArrayElementAtIndex(i).objectReferenceValue as BuildingPlacementDefinition;
                if (existing != null && !ContainsId(definitions, existing.BuildingId))
                {
                    merged.Add(existing);
                }
            }

            for (var i = 0; i < definitions.Count; i++)
            {
                merged.Add(definitions[i]);
            }

            target.arraySize = merged.Count;
            for (var i = 0; i < merged.Count; i++)
            {
                target.GetArrayElementAtIndex(i).objectReferenceValue = merged[i];
            }
        }

        private static void SetBindings(SerializedProperty target, IReadOnlyList<BuildingPlacementDefinition> definitions)
        {
            var retained = new List<BuildingPlacementBindingData>();
            for (var i = 0; i < target.arraySize; i++)
            {
                var binding = target.GetArrayElementAtIndex(i);
                var id = binding.FindPropertyRelative("buildingId").stringValue;
                var definition = binding.FindPropertyRelative("definition").objectReferenceValue as BuildingPlacementDefinition;
                if (!ContainsId(definitions, id))
                {
                    retained.Add(new BuildingPlacementBindingData(id, definition));
                }
            }

            target.arraySize = retained.Count + definitions.Count;
            for (var i = 0; i < retained.Count; i++)
            {
                SetBinding(target.GetArrayElementAtIndex(i), retained[i].Id, retained[i].Definition);
            }

            for (var i = 0; i < definitions.Count; i++)
            {
                SetBinding(target.GetArrayElementAtIndex(retained.Count + i), definitions[i].BuildingId, definitions[i]);
            }
        }

        private static bool ContainsId(IReadOnlyList<BuildingPlacementDefinition> definitions, string id)
        {
            for (var i = 0; i < definitions.Count; i++)
            {
                if (definitions[i] != null && definitions[i].BuildingId == id)
                {
                    return true;
                }
            }

            return false;
        }

        private static void SetBinding(SerializedProperty binding, string id, BuildingPlacementDefinition definition)
        {
            binding.FindPropertyRelative("buildingId").stringValue = id ?? string.Empty;
            binding.FindPropertyRelative("definition").objectReferenceValue = definition;
        }

        private readonly struct BuildingPlacementBindingData
        {
            public string Id { get; }
            public BuildingPlacementDefinition Definition { get; }

            public BuildingPlacementBindingData(string id, BuildingPlacementDefinition definition)
            {
                Id = id;
                Definition = definition;
            }
        }

        private static ItemCostDto[] ToCosts(IReadOnlyList<ItemCostEntry> costs)
        {
            if (costs == null || costs.Count == 0)
            {
                return Array.Empty<ItemCostDto>();
            }

            var result = new List<ItemCostDto>(costs.Count);
            for (var i = 0; i < costs.Count; i++)
            {
                var cost = costs[i];
                if (!string.IsNullOrWhiteSpace(cost.ItemId) && cost.Quantity > 0)
                {
                    result.Add(new ItemCostDto(cost.ItemId, cost.Quantity));
                }
            }

            return result.ToArray();
        }

        private static string ToAssetName(string id)
        {
            return id.Replace("building.", string.Empty).Replace('.', '_');
        }

        private static void EnsureFolder(string parent, string child)
        {
            if (!AssetDatabase.IsValidFolder(parent + "/" + child))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }

        private static void RemoveSupersededSupportDefinition()
        {
            var generatedPath = DefinitionFolder + "/support_basicPlacement.asset";
            if (AssetDatabase.LoadAssetAtPath<BuildingPlacementDefinition>(generatedPath) != null)
            {
                AssetDatabase.DeleteAsset(generatedPath);
            }
        }

        private static T FindInScene<T>(Scene scene) where T : Component
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                var component = root.GetComponentInChildren<T>(true);
                if (component != null)
                {
                    return component;
                }
            }

            return null;
        }
    }
}
#endif

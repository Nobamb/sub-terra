using System.Collections.Generic;
using NUnit.Framework;
using SubTerra.App.Core.Data;
using SubTerra.App.Integration;
using SubTerra.App.Editor.DataValidation;
using SubTerra.Gameplay.Building;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SubTerra.App.Tests.UI
{
    public sealed class FacilityPurchaseFlowStaticTests
    {
        [Test]
        public void FacilityPurchaseFlow_WiresEveryCatalogFacilityToPlacementDefinition()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<GameDataCatalog>(
                "Assets/_Project/Data/Catalog/GameDataCatalog.asset");
            Assert.That(catalog, Is.Not.Null);

            var scene = EditorSceneManager.OpenScene(
                FacilityPurchaseFlowBuilder.IntegrationScenePath,
                OpenSceneMode.Additive);
            try
            {
                var placement = FindInScene<BuildingPlacementSystem>(scene);
                var bridge = FindInScene<GameplayBuildingPlacementBridge>(scene);
                Assert.That(placement, Is.Not.Null);
                Assert.That(bridge, Is.Not.Null);

                var placementDefinitions = ReadDefinitions(placement);
                var bindings = ReadBindings(bridge);
                foreach (var id in FacilityIds)
                {
                    Assert.That(catalog.TryGetBuilding(id, out var data), Is.True, id);
                    Assert.That(data.RuntimePrefab, Is.Not.Null, id);
                    Assert.That(placementDefinitions.ContainsKey(id), Is.True, id);
                    Assert.That(bindings.ContainsKey(id), Is.True, id);

                    var definition = placementDefinitions[id];
                    Assert.That(definition.RuntimePrefab, Is.SameAs(data.RuntimePrefab), id);
                    Assert.That(bindings[id], Is.SameAs(definition), id);
                    AssertCostsMatch(data, definition, id);
                }
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void FacilityPurchaseFlow_UsesOneBoundBuildingMenu()
        {
            var scene = EditorSceneManager.OpenScene(
                FacilityPurchaseFlowBuilder.IntegrationScenePath,
                OpenSceneMode.Additive);
            try
            {
                var menus = FindAllInScene<SubTerra.App.UI.Building.BuildingMenuBinder>(scene);
                Assert.That(menus.Count, Is.EqualTo(1),
                    "Only the Phase Q BuildingPanel may remain in the integration HUD.");

                var integration = FindInScene<BuildingUiIntegrationBinder>(scene);
                Assert.That(integration, Is.Not.Null);
                var boundMenu = new SerializedObject(integration)
                    .FindProperty("buildingMenu").objectReferenceValue;
                Assert.That(boundMenu, Is.SameAs(menus[0]));
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static readonly string[] FacilityIds =
        {
            DataIds.Buildings.SupportBasic,
            DataIds.Buildings.LightBasic,
            DataIds.Buildings.ChargerBasic,
            DataIds.Buildings.StorageBasic,
            DataIds.Buildings.SettlementBasic,
            DataIds.Buildings.OutpostCoreBasic
        };

        private static Dictionary<string, BuildingPlacementDefinition> ReadDefinitions(
            BuildingPlacementSystem placement)
        {
            var property = new SerializedObject(placement).FindProperty("restoreDefinitions");
            var result = new Dictionary<string, BuildingPlacementDefinition>();
            for (var i = 0; i < property.arraySize; i++)
            {
                var definition = property.GetArrayElementAtIndex(i).objectReferenceValue as BuildingPlacementDefinition;
                if (definition != null)
                {
                    result[definition.BuildingId] = definition;
                }
            }

            return result;
        }

        private static Dictionary<string, BuildingPlacementDefinition> ReadBindings(
            GameplayBuildingPlacementBridge bridge)
        {
            var property = new SerializedObject(bridge).FindProperty("bindings");
            var result = new Dictionary<string, BuildingPlacementDefinition>();
            for (var i = 0; i < property.arraySize; i++)
            {
                var binding = property.GetArrayElementAtIndex(i);
                result[binding.FindPropertyRelative("buildingId").stringValue] =
                    binding.FindPropertyRelative("definition").objectReferenceValue as BuildingPlacementDefinition;
            }

            return result;
        }

        private static void AssertCostsMatch(
            BuildingData data,
            BuildingPlacementDefinition definition,
            string id)
        {
            Assert.That(definition.Costs.Count, Is.EqualTo(data.BuildCosts.Count), id);
            for (var i = 0; i < data.BuildCosts.Count; i++)
            {
                Assert.That(definition.Costs[i].ItemId, Is.EqualTo(data.BuildCosts[i].ItemId), id);
                Assert.That(definition.Costs[i].Quantity, Is.EqualTo(data.BuildCosts[i].Quantity), id);
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

        private static List<T> FindAllInScene<T>(Scene scene) where T : Component
        {
            var result = new List<T>();
            foreach (var root in scene.GetRootGameObjects())
            {
                result.AddRange(root.GetComponentsInChildren<T>(true));
            }

            return result;
        }
    }
}

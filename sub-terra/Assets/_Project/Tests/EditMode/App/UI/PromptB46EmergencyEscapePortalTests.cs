using System.Linq;
using NUnit.Framework;
using SubTerra.App.Core.Data;
using SubTerra.App.Integration;
using SubTerra.App.UI.Building;
using SubTerra.App.UI.HUD;
using SubTerra.Gameplay.Building;
using SubTerra.Gameplay.Power;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SubTerra.App.Tests.UI
{
    public sealed class PromptB46EmergencyEscapePortalTests
    {
        private const string CatalogPath =
            "Assets/_Project/Data/Catalog/GameDataCatalog.asset";
        private const string MenuPath =
            "Assets/_Project/Prefabs/UI/BuildingMenu.prefab";
        private const string PortalPrefabPath =
            "Assets/_Project/Prefabs/Gameplay/Buildings/EmergencyEscapePortal.prefab";
        private const string IntegrationPath =
            "Assets/_Project/Scenes/App/Mine_Demo_Integration.unity";

        [Test]
        public void PromptB46_CatalogAndPrefab_HaveRequestedCostsPowerAndPassableTrigger()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<GameDataCatalog>(CatalogPath);
            Assert.That(catalog, Is.Not.Null);
            Assert.That(catalog.TryGetBuilding(DataIds.Buildings.EmergencyEscapePortal, out var data), Is.True);
            Assert.That(data.PowerDraw, Is.EqualTo(30));
            Assert.That(data.BuildCosts.Count, Is.EqualTo(2));
            Assert.That(data.BuildCosts.Any(c =>
                c.ItemId == DataIds.Minerals.Iron && c.Quantity == 3), Is.True);
            Assert.That(data.BuildCosts.Any(c =>
                c.ItemId == DataIds.Minerals.Lithium && c.Quantity == 3), Is.True);

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PortalPrefabPath);
            Assert.That(prefab, Is.Not.Null);
            Assert.That(prefab.GetComponent<EmergencyEscapePortal>(), Is.Not.Null);
            Assert.That(prefab.GetComponent<PowerNode>().Demand, Is.EqualTo(30));
            Assert.That(prefab.GetComponentsInChildren<Collider2D>(true),
                Has.All.Matches<Collider2D>(collider => collider.isTrigger));
        }

        [Test]
        public void PromptB46_BuildingMenu_HasOneEmergencyPortalEntry()
        {
            var menu = AssetDatabase.LoadAssetAtPath<GameObject>(MenuPath);
            var entries = menu.GetComponentsInChildren<BuildingMenuEntryButton>(true);
            var matches = 0;
            for (var i = 0; i < entries.Length; i++)
            {
                var so = new SerializedObject(entries[i]);
                if (so.FindProperty("buildingId").stringValue ==
                    DataIds.Buildings.EmergencyEscapePortal)
                {
                    matches++;
                }
            }

            Assert.That(matches, Is.EqualTo(1));
        }

        [Test]
        public void PromptB46_Integration_WiresPortalAndStartsAtElevatorCenter()
        {
            var scene = EditorSceneManager.OpenScene(IntegrationPath, OpenSceneMode.Additive);
            try
            {
                var bridge = FindInScene<EmergencyEscapePortalRuntimeBridge>(scene);
                Assert.That(bridge, Is.Not.Null);
                var bridgeSo = new SerializedObject(bridge);
                var player = bridgeSo.FindProperty("playerTransform").objectReferenceValue as Transform;
                var elevator = bridgeSo.FindProperty("elevatorCenter").objectReferenceValue as Transform;
                Assert.That(player, Is.Not.Null);
                Assert.That(elevator, Is.Not.Null);
                Assert.That(player.position, Is.EqualTo(elevator.position));

                var fallback = FindInSceneByName(scene, "RunFailureSurfaceFallback");
                Assert.That(fallback, Is.Not.Null);
                Assert.That(fallback.transform.position, Is.EqualTo(elevator.position));

                var chrome = FindInScene<HudPanelChromeController>(scene);
                var chromeSo = new SerializedObject(chrome);
                Assert.That(chromeSo.FindProperty("inventoryPanelView").objectReferenceValue,
                    Is.Not.Null);
                Assert.That(chromeSo.FindProperty("inventoryPanelRoot").objectReferenceValue,
                    Is.Not.Null);
                Assert.That(chromeSo.FindProperty("inventoryCloseButton").objectReferenceValue,
                    Is.Not.Null);

                var placement = FindInScene<BuildingPlacementSystem>(scene);
                var placementSo = new SerializedObject(placement);
                var definitions = placementSo.FindProperty("restoreDefinitions");
                var found = false;
                for (var i = 0; i < definitions.arraySize; i++)
                {
                    var definition = definitions.GetArrayElementAtIndex(i).objectReferenceValue
                        as BuildingPlacementDefinition;
                    found |= definition != null
                        && definition.BuildingId == DataIds.Buildings.EmergencyEscapePortal;
                }
                Assert.That(found, Is.True);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static T FindInScene<T>(Scene scene) where T : Component
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                var component = root.GetComponentInChildren<T>(true);
                if (component != null) return component;
            }
            return null;
        }

        private static GameObject FindInSceneByName(Scene scene, string name)
        {
            foreach (var root in scene.GetRootGameObjects())
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == name) return child.gameObject;
            }
            return null;
        }
    }
}

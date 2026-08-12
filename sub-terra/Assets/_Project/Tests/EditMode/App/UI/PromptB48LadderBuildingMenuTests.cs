using System.Linq;
using NUnit.Framework;
using SubTerra.App.Core.Data;
using SubTerra.App.Integration;
using SubTerra.App.UI.Building;
using SubTerra.Gameplay.Building;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SubTerra.App.Tests.UI
{
    /// <summary>prompt-B 48: 시설 건설 창 사다리 버튼·카탈로그·배치 바인딩 검증.</summary>
    public sealed class PromptB48LadderBuildingMenuTests
    {
        private const string CatalogPath =
            "Assets/_Project/Data/Catalog/GameDataCatalog.asset";
        private const string MenuPath =
            "Assets/_Project/Prefabs/UI/BuildingMenu.prefab";
        private const string LadderDataPath =
            "Assets/_Project/Data/Buildings/Building_Ladder_Basic.asset";
        private const string LadderPlacementPath =
            "Assets/_Project/Data/Buildings/LadderPlacement.asset";
        private const string DefaultLadderPrefabPath =
            "Assets/_Project/Prefabs/Gameplay/Traversal/Ladder.prefab";
        private const string BuildableLadderPrefabPath =
            "Assets/_Project/Prefabs/Gameplay/Traversal/Ladder_Buildable.prefab";
        private const string IntegrationPath =
            "Assets/_Project/Scenes/App/Mine_Demo_Integration.unity";

        private const float FirstButtonY = -246f;
        private const float LastButtonY = -498f;
        private const float PanelHeight = 560f;

        [Test]
        public void PromptB48_Catalog_ContainsLadderAfterSupport()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<GameDataCatalog>(CatalogPath);
            Assert.That(catalog, Is.Not.Null);
            Assert.That(catalog.TryGetBuilding(DataIds.Buildings.LadderBasic, out var ladder), Is.True);
            Assert.That(ladder.DisplayName, Does.Contain("사다리"));
            Assert.That(ladder.PowerDraw, Is.EqualTo(0));
            Assert.That(ladder.RuntimePrefab, Is.Not.Null);
            Assert.That(ladder.BuildCosts.Count, Is.EqualTo(2));
            Assert.That(ladder.BuildCosts.Any(c =>
                c.ItemId == DataIds.Minerals.Iron && c.Quantity == 1), Is.True);
            Assert.That(ladder.BuildCosts.Any(c =>
                c.ItemId == DataIds.Minerals.Copper && c.Quantity == 3), Is.True);

            var supportIndex = -1;
            var ladderIndex = -1;
            for (var i = 0; i < catalog.Buildings.Count; i++)
            {
                var building = catalog.Buildings[i];
                if (building == null)
                {
                    continue;
                }

                if (building.Id == DataIds.Buildings.SupportBasic)
                {
                    supportIndex = i;
                }

                if (building.Id == DataIds.Buildings.LadderBasic)
                {
                    ladderIndex = i;
                }
            }

            Assert.That(supportIndex, Is.GreaterThanOrEqualTo(0));
            Assert.That(ladderIndex, Is.EqualTo(supportIndex + 1));
        }

        [Test]
        public void PromptB48_BuildingMenu_HasLadderButtonBelowSupportAndInsidePanel()
        {
            var menu = AssetDatabase.LoadAssetAtPath<GameObject>(MenuPath);
            Assert.That(menu, Is.Not.Null);

            var support = FindEntry(menu, DataIds.Buildings.SupportBasic);
            var ladder = FindEntry(menu, DataIds.Buildings.LadderBasic);
            var escape = FindEntry(menu, DataIds.Buildings.EmergencyEscapePortal);
            Assert.That(support, Is.Not.Null);
            Assert.That(ladder, Is.Not.Null);
            Assert.That(escape, Is.Not.Null);

            var supportRect = support.GetComponent<RectTransform>();
            var ladderRect = ladder.GetComponent<RectTransform>();
            var escapeRect = escape.GetComponent<RectTransform>();

            Assert.That(supportRect.anchoredPosition.y, Is.EqualTo(FirstButtonY).Within(0.1f));
            Assert.That(escapeRect.anchoredPosition.y, Is.EqualTo(LastButtonY).Within(0.1f));
            // 사다리는 버팀목 바로 아래(y가 더 음수).
            Assert.That(ladderRect.anchoredPosition.y, Is.LessThan(supportRect.anchoredPosition.y));
            Assert.That(ladderRect.anchoredPosition.y, Is.GreaterThan(escapeRect.anchoredPosition.y));

            // 마지막 버튼 하단이 패널 높이 안에 있어야 한다.
            var lastBottom = Mathf.Abs(escapeRect.anchoredPosition.y) + escapeRect.sizeDelta.y;
            Assert.That(lastBottom, Is.LessThanOrEqualTo(PanelHeight));

            var matches = 0;
            var entries = menu.GetComponentsInChildren<BuildingMenuEntryButton>(true);
            for (var i = 0; i < entries.Length; i++)
            {
                var so = new SerializedObject(entries[i]);
                if (so.FindProperty("buildingId").stringValue == DataIds.Buildings.LadderBasic)
                {
                    matches++;
                }
            }

            Assert.That(matches, Is.EqualTo(1));
        }

        [Test]
        public void PromptB48_Integration_WiresLadderPlacementBinding()
        {
            var placementDefinition =
                AssetDatabase.LoadAssetAtPath<BuildingPlacementDefinition>(LadderPlacementPath);
            Assert.That(placementDefinition, Is.Not.Null);
            Assert.That(placementDefinition.BuildingId, Is.EqualTo(DataIds.Buildings.LadderBasic));
            Assert.That(placementDefinition.RequiresGround, Is.False);
            Assert.That(placementDefinition.Footprint, Is.EqualTo(new Vector2Int(1, 5)));
            Assert.That(placementDefinition.Costs.Count, Is.EqualTo(2));
            Assert.That(placementDefinition.Costs.Any(c =>
                c.ItemId == DataIds.Minerals.Iron && c.Quantity == 1), Is.True);
            Assert.That(placementDefinition.Costs.Any(c =>
                c.ItemId == DataIds.Minerals.Copper && c.Quantity == 3), Is.True);

            // 시설 건설은 5칸 Prefab, 씬 기본 사다리는 이전 1유닛 높이 Prefab을 쓴다.
            var defaultLadder = AssetDatabase.LoadAssetAtPath<GameObject>(DefaultLadderPrefabPath);
            var buildableLadder = AssetDatabase.LoadAssetAtPath<GameObject>(BuildableLadderPrefabPath);
            Assert.That(defaultLadder, Is.Not.Null);
            Assert.That(buildableLadder, Is.Not.Null);
            Assert.That(defaultLadder.GetComponent<SpriteRenderer>().size.y, Is.EqualTo(1f).Within(0.01f));
            Assert.That(defaultLadder.GetComponent<BoxCollider2D>().size.y, Is.EqualTo(1f).Within(0.01f));
            Assert.That(buildableLadder.GetComponent<SpriteRenderer>().size.y, Is.EqualTo(5f).Within(0.01f));
            Assert.That(buildableLadder.GetComponent<BoxCollider2D>().size.y, Is.EqualTo(5f).Within(0.01f));
            Assert.That(placementDefinition.RuntimePrefab, Is.EqualTo(buildableLadder));

            var scene = EditorSceneManager.OpenScene(IntegrationPath, OpenSceneMode.Additive);
            try
            {
                var placement = FindInScene<BuildingPlacementSystem>(scene);
                var bridge = FindInScene<GameplayBuildingPlacementBridge>(scene);
                Assert.That(placement, Is.Not.Null);
                Assert.That(bridge, Is.Not.Null);

                var placementSo = new SerializedObject(placement);
                var definitions = placementSo.FindProperty("restoreDefinitions");
                var foundDefinition = false;
                for (var i = 0; i < definitions.arraySize; i++)
                {
                    var definition = definitions.GetArrayElementAtIndex(i).objectReferenceValue
                        as BuildingPlacementDefinition;
                    foundDefinition |= definition != null
                        && definition.BuildingId == DataIds.Buildings.LadderBasic;
                }

                Assert.That(foundDefinition, Is.True);

                var bridgeSo = new SerializedObject(bridge);
                var bindings = bridgeSo.FindProperty("bindings");
                var foundBinding = false;
                for (var i = 0; i < bindings.arraySize; i++)
                {
                    var binding = bindings.GetArrayElementAtIndex(i);
                    if (binding.FindPropertyRelative("buildingId").stringValue
                        == DataIds.Buildings.LadderBasic)
                    {
                        foundBinding = true;
                        Assert.That(
                            binding.FindPropertyRelative("definition").objectReferenceValue,
                            Is.Not.Null);
                    }
                }

                Assert.That(foundBinding, Is.True);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static BuildingMenuEntryButton FindEntry(GameObject menu, string buildingId)
        {
            var entries = menu.GetComponentsInChildren<BuildingMenuEntryButton>(true);
            for (var i = 0; i < entries.Length; i++)
            {
                var so = new SerializedObject(entries[i]);
                if (so.FindProperty("buildingId").stringValue == buildingId)
                {
                    return entries[i];
                }
            }

            return null;
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

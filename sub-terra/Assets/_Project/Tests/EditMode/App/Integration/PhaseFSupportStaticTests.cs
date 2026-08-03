using System.Linq;
using NUnit.Framework;
using SubTerra.App.Core.Data;
using SubTerra.App.Editor.DataValidation;
using SubTerra.App.Integration;
using SubTerra.App.UI.Building;
using SubTerra.Gameplay.Building;
using SubTerra.Gameplay.Structural;
using SubTerra.Shared;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SubTerra.App.Tests.Integration
{
    public sealed class PhaseFSupportStaticTests
    {
        [Test]
        public void F_S01_SupportPrefab_HasRuntimeColliderSupportAndVisualRoot()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                PhaseFSupportBuilder.SupportPrefabPath);

            Assert.That(prefab, Is.Not.Null);
            Assert.That(prefab.transform.localScale, Is.EqualTo(Vector3.one));
            Assert.That(prefab.GetComponent<BoxCollider2D>(), Is.Not.Null);
            Assert.That(prefab.GetComponent<StructuralSupport>(), Is.Not.Null);
            Assert.That(prefab.GetComponent<BuildingInstance>(), Is.Not.Null);
            var visualRoot = prefab.transform.Find("VisualRoot");
            Assert.That(visualRoot, Is.Not.Null);
            Assert.That(visualRoot.GetComponent<SpriteRenderer>(), Is.Not.Null);
        }

        [Test]
        public void F_S02_IntegrationScene_WiresMenuBridgePreviewAndPlacementLimits()
        {
            var scene = EditorSceneManager.OpenScene(
                PhaseFSupportBuilder.IntegrationScenePath,
                OpenSceneMode.Single);
            var placement = Find<BuildingPlacementSystem>(scene);
            var bridge = Find<GameplayBuildingPlacementBridge>(scene);

            Assert.That(placement, Is.Not.Null);
            Assert.That(bridge, Is.Not.Null);
            Assert.That(Find<BuildingMenuBinder>(scene), Is.Not.Null);
            Assert.That(Find<BuildingPlacementPreview>(scene), Is.Not.Null);

            var placementSo = new SerializedObject(placement);
            Assert.That(
                placementSo.FindProperty("placementOrigin").objectReferenceValue,
                Is.Not.Null);
            Assert.That(
                placementSo.FindProperty("allowedPlacementArea").objectReferenceValue,
                Is.Not.Null);
            Assert.That(
                placementSo.FindProperty("maximumPlacementDistance").floatValue,
                Is.EqualTo(6f));
            var definitions = placementSo.FindProperty("restoreDefinitions");
            Assert.That(Enumerable.Range(0, definitions.arraySize).Any(index =>
                definitions.GetArrayElementAtIndex(index).objectReferenceValue
                    == AssetDatabase.LoadAssetAtPath<BuildingPlacementDefinition>(
                        PhaseFSupportBuilder.SupportDefinitionPath)), Is.True);

            var bridgeSo = new SerializedObject(bridge);
            Assert.That(bridgeSo.FindProperty("placementSystem").objectReferenceValue, Is.Not.Null);
            Assert.That(bridgeSo.FindProperty("preview").objectReferenceValue, Is.Not.Null);
            Assert.That(bridgeSo.FindProperty("sceneReferences").objectReferenceValue, Is.Not.Null);
        }

        [Test]
        public void F_S03_SupportDataAndPlacementDefinition_UseActualPrefabAndCopperCost()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                PhaseFSupportBuilder.SupportPrefabPath);
            var data = AssetDatabase.LoadAssetAtPath<BuildingData>(
                PhaseFSupportBuilder.SupportDataPath);
            var definition = AssetDatabase.LoadAssetAtPath<BuildingPlacementDefinition>(
                PhaseFSupportBuilder.SupportDefinitionPath);

            Assert.That(data.RuntimePrefab, Is.EqualTo(prefab));
            Assert.That(definition.RuntimePrefab, Is.EqualTo(prefab));
            Assert.That(definition.BuildingId, Is.EqualTo(DataIds.Buildings.SupportBasic));
            Assert.That(definition.Costs.Count, Is.EqualTo(1));
            Assert.That(definition.Costs[0].ItemId, Is.EqualTo(DataIds.Minerals.Copper));
            Assert.That(definition.Costs[0].Quantity, Is.EqualTo(2));
        }

        [Test]
        public void F_S04_BuildingSnapshot_ContainsOnlyIdentityCoordinatesAndState()
        {
            var fields = typeof(BuildingSnapshotDto).GetFields();

            Assert.That(fields.Select(field => field.Name), Is.EquivalentTo(new[]
            {
                "instanceId",
                "buildingTypeId",
                "x",
                "y",
                "rotation",
                "level",
                "health"
            }));
            Assert.That(fields.Any(field => typeof(Object).IsAssignableFrom(field.FieldType)), Is.False);
        }

        private static T Find<T>(Scene scene) where T : Object
        {
            return scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<T>(true))
                .FirstOrDefault();
        }
    }
}

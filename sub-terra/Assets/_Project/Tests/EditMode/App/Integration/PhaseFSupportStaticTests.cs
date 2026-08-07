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
            Assert.That(prefab.GetComponent<StructuralSupport>(), Is.Not.Null);
            Assert.That(prefab.GetComponent<BuildingInstance>(), Is.Not.Null);

            // T자 버팀목: 세로 기둥 + 상단 가로 캡 콜라이더 2개.
            var colliders = prefab.GetComponents<BoxCollider2D>();
            Assert.That(colliders.Length, Is.EqualTo(2));
            Assert.That(
                colliders.Any(c => Approximately(c.size, new Vector2(0.4f, 1.8f))
                    && Approximately(c.offset, Vector2.zero)),
                Is.True,
                "세로 기둥 콜라이더(0.4×1.8)가 있어야 한다.");
            Assert.That(
                colliders.Any(c => Approximately(c.size, new Vector2(1f, 0.4f))),
                Is.True,
                "상단 가로 캡 콜라이더(1.0×0.4, 블록 너비×기둥 너비)가 있어야 한다.");

            var visualRoot = prefab.transform.Find("VisualRoot");
            Assert.That(visualRoot, Is.Not.Null);
            var post = visualRoot.Find("Post");
            var cap = visualRoot.Find("Cap");
            Assert.That(post, Is.Not.Null, "T자 세로 기둥 Visual Post가 있어야 한다.");
            Assert.That(cap, Is.Not.Null, "T자 가로 캡 Visual Cap이 있어야 한다.");
            Assert.That(post.GetComponent<SpriteRenderer>(), Is.Not.Null);
            Assert.That(cap.GetComponent<SpriteRenderer>(), Is.Not.Null);
            Assert.That(post.GetComponent<SpriteRenderer>().size, Is.EqualTo(new Vector2(0.4f, 1.8f)));
            Assert.That(cap.GetComponent<SpriteRenderer>().size, Is.EqualTo(new Vector2(1f, 0.4f)));
        }

        private static bool Approximately(Vector2 a, Vector2 b)
        {
            return Mathf.Abs(a.x - b.x) < 0.001f && Mathf.Abs(a.y - b.y) < 0.001f;
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

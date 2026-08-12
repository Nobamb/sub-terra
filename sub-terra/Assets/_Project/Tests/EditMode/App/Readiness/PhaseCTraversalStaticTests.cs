using System.Linq;
using NUnit.Framework;
using SubTerra.App.Integration;
using SubTerra.App.Save;
using SubTerra.Gameplay.Building;
using SubTerra.Gameplay.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace SubTerra.App.Tests.Readiness
{
    public sealed class PhaseCTraversalStaticTests
    {
        [Test]
        public void InputActions_ContainInteractAndVerticalMovementBindings()
        {
            var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(
                "Assets/Settings/InputSystem_Actions.inputactions");
            var interact = actions.FindAction("Player/Interact", true);
            var move = actions.FindAction("Player/Move", true);

            Assert.That(
                interact.bindings.Select(binding => binding.effectivePath),
                Does.Contain("<Keyboard>/e"));
            Assert.That(
                move.bindings.Select(binding => binding.name),
                Does.Contain("up").And.Contain("down"));
        }

        [Test]
        public void TraversalPrefabs_HaveRequiredRuntimeComponentsAndColliders()
        {
            var ladder = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Project/Prefabs/Gameplay/Traversal/Ladder.prefab");
            var elevator = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Project/Prefabs/Gameplay/Traversal/StartElevator.prefab");

            Assert.NotNull(ladder);
            Assert.NotNull(ladder.GetComponent<LadderZone>());
            Assert.IsTrue(ladder.GetComponent<Collider2D>().isTrigger);
            Assert.NotNull(elevator);
            Assert.NotNull(elevator.GetComponent<ElevatorController>());
            Assert.IsTrue(elevator.GetComponent<Collider2D>().isTrigger);
        }

        [Test]
        public void IntegrationScene_WiresStationBridgeAndRestorableLadder()
        {
            var scene = EditorSceneManager.OpenScene(
                "Assets/_Project/Scenes/App/Mine_Demo_Integration.unity",
                OpenSceneMode.Single);

            Assert.NotNull(Find<ElevatorController>(scene));
            Assert.NotNull(Find<ElevatorTravelBridge>(scene));
            Assert.NotNull(scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .FirstOrDefault(item => item.name == "LadderTrainingShaft_6m"));

            var placement = Find<BuildingPlacementSystem>(scene);
            var serialized = new SerializedObject(placement);
            var definitions = serialized.FindProperty("restoreDefinitions");
            Assert.IsTrue(Enumerable.Range(0, definitions.arraySize).Any(index =>
                definitions.GetArrayElementAtIndex(index).objectReferenceValue != null
                && definitions.GetArrayElementAtIndex(index).objectReferenceValue.name
                    == "LadderPlacement"));
        }

        [Test]
        public void LadderSnapshot_RestoresSameCoordinatesAndClimbingComponent()
        {
            var placementDefinition = AssetDatabase.LoadAssetAtPath<BuildingPlacementDefinition>(
                "Assets/_Project/Data/Buildings/LadderPlacement.asset");
            Assert.AreEqual(new Vector2Int(1, 5), placementDefinition.Footprint);
            Assert.AreEqual(2, placementDefinition.Costs.Count);
            Assert.IsTrue(placementDefinition.Costs.Any(c =>
                c.ItemId == "mineral.iron" && c.Quantity == 1));
            Assert.IsTrue(placementDefinition.Costs.Any(c =>
                c.ItemId == "mineral.copper" && c.Quantity == 3));
            var host = new GameObject("LadderRestoreTest");
            try
            {
                var placement = host.AddComponent<BuildingPlacementSystem>();
                var serialized = new SerializedObject(placement);
                var definitions = serialized.FindProperty("restoreDefinitions");
                definitions.arraySize = 1;
                definitions.GetArrayElementAtIndex(0).objectReferenceValue = placementDefinition;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                Assert.IsTrue(placement.TryRestoreBuilding(new SubTerra.Shared.BuildingSnapshotDto
                {
                    instanceId = "ladder-test-0001",
                    buildingTypeId = "building.ladder.basic",
                    x = 3,
                    y = -8
                }));

                var ladder = host.GetComponentInChildren<LadderZone>();
                Assert.NotNull(ladder);
                // 1x5 footprint 복원 시 좌하단 origin (3,-8) 기준 중심은 (3,-6).
                Assert.AreEqual(new Vector3(3f, -6f, 0f), ladder.transform.position);
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void CheckpointDto_DoesNotContainUnityObjectReferences()
        {
            Assert.IsFalse(typeof(OutpostSaveData)
                .GetFields()
                .Any(field => typeof(Object).IsAssignableFrom(field.FieldType)));
        }

        private static T Find<T>(Scene scene) where T : Object
        {
            return scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<T>(true))
                .FirstOrDefault();
        }
    }
}

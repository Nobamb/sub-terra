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
        public void PlayerPrefab_UsesFiveSharedLadderFramesInFrontOfLadder()
        {
            var player = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Project/Prefabs/Gameplay/Player/Player.prefab");
            var ladder = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Project/Prefabs/Gameplay/Traversal/Ladder.prefab");

            var visualRoot = player.transform.Find("VisualRoot");
            var playerRenderer = visualRoot.GetComponent<SpriteRenderer>();
            var ladderRenderer = ladder.GetComponent<SpriteRenderer>();
            var animation = visualRoot.GetComponent<PlayerAnimationController>();
            var serializedAnimation = new SerializedObject(animation);
            var ladderFrames = serializedAnimation.FindProperty("ladderFrames");

            Assert.Greater(playerRenderer.sortingOrder, ladderRenderer.sortingOrder);
            Assert.IsNull(visualRoot.Find("LadderRig"), "보존 파츠 리그가 Player 프리팹에서 활성화되면 안 된다.");
            Assert.IsNull(
                visualRoot.GetComponent<PlayerLadderPoseController>(),
                "런타임은 파츠 컨트롤러가 아니라 전체 프레임을 사용해야 한다.");
            Assert.AreEqual(5, ladderFrames.arraySize);
            Assert.AreEqual(0.25f, serializedAnimation.FindProperty("ladderDistancePerFrame").floatValue);
            Assert.IsNull(serializedAnimation.FindProperty("ladderDownFrames"));
            bool styled = AssetDatabase.GetAssetPath(ladderFrames.GetArrayElementAtIndex(0).objectReferenceValue)
                .EndsWith("ladder_back_neutral_v2.png");
            for (var index = 0; index < ladderFrames.arraySize; index++)
            {
                Assert.NotNull(ladderFrames.GetArrayElementAtIndex(index).objectReferenceValue);
                string path = AssetDatabase.GetAssetPath(ladderFrames.GetArrayElementAtIndex(index).objectReferenceValue);
                if (index == 0)
                {
                    // The original neutral pose remains a supported rollback during the art trial.
                    Assert.That(path, Is.EqualTo(
                        "Assets/_Project/Art/Characters/Player/Frames/LadderBack/ladder_back_neutral_v2.png").Or.EqualTo(
                        "Assets/_Project/Art/Characters/Player/Frames/LadderBack/ladder_back_01.png"));
                    continue;
                }
                Assert.AreEqual(
                    "Assets/_Project/Art/Characters/Player/Frames/LadderBack/ladder_back_" + (styled ? "style_" : "")
                        + (index + 1).ToString("D2") + ".png",
                    path);
            }

            Assert.NotNull(AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/_Project/Art/Characters/Player/LadderRig/player_ladder_back_torso.png"));
            Assert.NotNull(AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/_Project/Art/Characters/Player/LadderRig/player_ladder_back_arm.png"));
            Assert.NotNull(AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/_Project/Art/Characters/Player/LadderRig/player_ladder_back_leg.png"));
        }

        [Test]
        public void StyledLadderFrames_KeepStandingMinerHeightAndFootLevel()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Gameplay/Player/Player.prefab");
            var animation = new SerializedObject(prefab.GetComponentInChildren<PlayerAnimationController>(true));
            var standing = (Sprite)animation.FindProperty("idleFrames").GetArrayElementAtIndex(0).objectReferenceValue;
            var ladder = animation.FindProperty("ladderFrames");
            if (!AssetDatabase.GetAssetPath(ladder.GetArrayElementAtIndex(0).objectReferenceValue).EndsWith("ladder_back_neutral_v2.png"))
                Assert.Ignore("The original ladder set is selected.");
            var standingPixels = OpaqueVerticalExtent(standing);
            for (int i = 0; i < ladder.arraySize; i++)
            {
                var sprite = (Sprite)ladder.GetArrayElementAtIndex(i).objectReferenceValue;
                var pixels = OpaqueVerticalExtent(sprite);
                Assert.That(pixels.y - pixels.x, Is.EqualTo(standingPixels.y - standingPixels.x).Within(0.015f), "Frame " + i + " must not shrink on ladder entry.");
                Assert.That(pixels.x, Is.EqualTo(standingPixels.x).Within(0.015f), "Frame " + i + " feet must stay at standing level.");
            }
        }

        private static Vector2 OpaqueVerticalExtent(Sprite sprite)
        {
            var texture = new Texture2D(2, 2);
            try
            {
                Assert.That(texture.LoadImage(System.IO.File.ReadAllBytes(AssetDatabase.GetAssetPath(sprite))), Is.True);
                var pixels = texture.GetPixels32();
                int bottom = texture.height, top = -1;
                for (int y = 0; y < texture.height; y++)
                for (int x = 0; x < texture.width; x++)
                    if (pixels[y * texture.width + x].a >= 64) { bottom = Mathf.Min(bottom, y); top = Mathf.Max(top, y); }
                Assert.That(top, Is.GreaterThanOrEqualTo(bottom));
                return new Vector2((bottom - sprite.rect.y - sprite.pivot.y) / sprite.pixelsPerUnit,
                    (top + 1 - sprite.rect.y - sprite.pivot.y) / sprite.pixelsPerUnit);
            }
            finally { Object.DestroyImmediate(texture); }
        }

        [Test]
        public void IntegrationScene_RemovesDemoCoreButPreservesStartingSupplyAndDroneAnchor()
        {
            var scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/App/Mine_Demo_Integration.unity", OpenSceneMode.Single);
            var transforms = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true)).ToArray();
            Assert.That(transforms.Any(item => item.name == "OutpostCore_Demo"), Is.False);
            var supply = transforms.Single(item => item.name == "SurfaceBasePowerSupply");
            Assert.That(supply.GetComponentsInChildren<SpriteRenderer>(true), Is.Empty);
            Assert.That(supply.GetComponent<BuildingInstance>(), Is.Null, "Removed demo core must not keep an invisible label or interaction.");
            var node = supply.GetComponents<MonoBehaviour>().Single(item => item.GetType().Name == "PowerNode");
            var serializedNode = new SerializedObject(node);
            Assert.That(serializedNode.FindProperty("isPowerSource").boolValue, Is.True);
            Assert.That(serializedNode.FindProperty("supply").intValue, Is.EqualTo(5));
            Assert.That(serializedNode.FindProperty("network").objectReferenceValue, Is.Not.Null);
            var sensor = transforms.SelectMany(item => item.GetComponents<MonoBehaviour>()).First(item => item != null && item.GetType().Name == "DroneSensor");
            var anchors = new SerializedObject(sensor).FindProperty("outpostCores");
            Assert.That(Enumerable.Range(0, anchors.arraySize).Any(i => anchors.GetArrayElementAtIndex(i).objectReferenceValue == supply), Is.True);
            Assert.That(scene.isDirty, Is.False);
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

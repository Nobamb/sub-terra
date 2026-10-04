using NUnit.Framework;
using SubTerra.Gameplay.Player;
using SubTerra.Shared;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace SubTerra.App.Tests.Readiness
{
    public sealed class ElevatorHologramPromptTests
    {
        private const string ElevatorPrefabPath =
            "Assets/_Project/Prefabs/Gameplay/Traversal/StartElevator.prefab";

        [TestCase(ElevatorTravelState.Idle, ElevatorDestination.SurfaceBase, ElevatorPromptKind.Return)]
        [TestCase(ElevatorTravelState.Arrived, ElevatorDestination.SurfaceBase, ElevatorPromptKind.Return)]
        [TestCase(ElevatorTravelState.Idle, ElevatorDestination.Mine, ElevatorPromptKind.Board)]
        [TestCase(ElevatorTravelState.Calling, ElevatorDestination.SurfaceBase, ElevatorPromptKind.None)]
        [TestCase(ElevatorTravelState.Moving, ElevatorDestination.SurfaceBase, ElevatorPromptKind.None)]
        [TestCase(ElevatorTravelState.Blocked, ElevatorDestination.SurfaceBase, ElevatorPromptKind.None)]
        public void Resolve_FollowsStateWhenRiderIsInRange(
            ElevatorTravelState state, ElevatorDestination destination, ElevatorPromptKind expected)
        {
            Assert.AreEqual(
                expected,
                ElevatorPromptResolver.Resolve(state, true, false, destination));
        }

        [Test]
        public void Resolve_IsNoneWithoutRiderOrWhileTravelIsInProgress()
        {
            Assert.AreEqual(
                ElevatorPromptKind.None,
                ElevatorPromptResolver.Resolve(
                    ElevatorTravelState.Idle, false, false, ElevatorDestination.SurfaceBase));
            Assert.AreEqual(
                ElevatorPromptKind.None,
                ElevatorPromptResolver.Resolve(
                    ElevatorTravelState.Arrived, true, true, ElevatorDestination.SurfaceBase));
        }

        [Test]
        public void GetLabel_UsesPlayerFacingKeyPrompts()
        {
            Assert.AreEqual("E키를 눌러 탑승", ElevatorPromptResolver.GetLabel(ElevatorPromptKind.Board));
            Assert.AreEqual("E키를 눌러 귀환", ElevatorPromptResolver.GetLabel(ElevatorPromptKind.Return));
            Assert.AreEqual(string.Empty, ElevatorPromptResolver.GetLabel(ElevatorPromptKind.None));
        }

        [Test]
        public void ElevatorPrefab_HasHiddenHologramAndNoDebugStatus()
        {
            var elevator = AssetDatabase.LoadAssetAtPath<GameObject>(ElevatorPrefabPath);
            Assert.NotNull(elevator);

            var prompt = elevator.GetComponentInChildren<ElevatorHologramPrompt>(true);
            Assert.NotNull(prompt, "ElevatorHologramPrompt가 StartElevator 프리팹에 없습니다.");

            var serialized = new SerializedObject(prompt);
            var panel = serialized.FindProperty("panel").objectReferenceValue as Transform;
            Assert.NotNull(panel);
            Assert.IsFalse(panel.gameObject.activeSelf, "홀로그램 패널은 기본 비활성이어야 합니다.");
            Assert.NotNull(serialized.FindProperty("label").objectReferenceValue as TMP_Text);
            Assert.Greater(serialized.FindProperty("panelSprites").arraySize, 0);

            var particles = serialized.FindProperty("particles");
            Assert.Greater(particles.arraySize, 0);
            for (int i = 0; i < particles.arraySize; i++)
            {
                var renderer = particles.GetArrayElementAtIndex(i).objectReferenceValue as SpriteRenderer;
                Assert.NotNull(renderer);
                Assert.IsFalse(renderer.enabled, "파티클은 기본 비활성이어야 합니다.");
            }

            var controller = new SerializedObject(elevator.GetComponent<ElevatorController>());
            Assert.IsFalse(controller.FindProperty("showDebugStatus").boolValue);
            var status = controller.FindProperty("statusText").objectReferenceValue as TMP_Text;
            if (status != null)
            {
                Assert.IsFalse(status.gameObject.activeSelf, "디버그 상태 문구가 기본으로 꺼져 있어야 합니다.");
            }
        }
    }
}

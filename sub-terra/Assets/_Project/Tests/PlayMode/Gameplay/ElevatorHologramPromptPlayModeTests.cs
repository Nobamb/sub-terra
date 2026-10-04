using System.Collections;
using System.Reflection;
using NUnit.Framework;
using SubTerra.Shared;
using UnityEngine;
using UnityEngine.TestTools;

namespace SubTerra.Gameplay.Player.Tests
{
    public sealed class ElevatorHologramPromptPlayModeTests
    {
        private GameObject portObject;
        private GameObject playerObject;
        private GameObject elevatorObject;
        private ElevatorController elevator;
        private ElevatorControllerPlayModeTests.RecordingTravelPort port;
        private PlayerMovement movement;
        private Rigidbody2D body;
        private ElevatorHologramPrompt prompt;
        private Transform panel;

        [SetUp]
        public void SetUp()
        {
            portObject = new GameObject("TravelPort");
            port = portObject.AddComponent<ElevatorControllerPlayModeTests.RecordingTravelPort>();

            playerObject = new GameObject("Player");
            body = playerObject.AddComponent<Rigidbody2D>();
            playerObject.AddComponent<CapsuleCollider2D>();
            movement = playerObject.AddComponent<PlayerMovement>();

            elevatorObject = new GameObject("Elevator");
            elevatorObject.AddComponent<BoxCollider2D>();
            elevator = elevatorObject.AddComponent<ElevatorController>();
            SetField(elevator, "callDelaySeconds", 0f);
            SetField(elevator, "travelDelaySeconds", 0f);

            var hologram = new GameObject("ElevatorHologram");
            hologram.SetActive(false);
            hologram.transform.SetParent(elevatorObject.transform, false);
            panel = new GameObject("Panel").transform;
            panel.SetParent(hologram.transform, false);
            var sprite = new GameObject("Frame", typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
            sprite.transform.SetParent(panel, false);
            prompt = hologram.AddComponent<ElevatorHologramPrompt>();
            SetPromptField("elevator", elevator);
            SetPromptField("panel", panel);
            SetPromptField("panelSprites", new[] { sprite });
            SetPromptField("particles", new SpriteRenderer[0]);
            hologram.SetActive(true);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(elevatorObject);
            Object.DestroyImmediate(playerObject);
            Object.DestroyImmediate(portObject);
        }

        [UnityTest]
        public IEnumerator WithoutRider_PromptStaysHidden()
        {
            yield return new WaitForSecondsRealtime(0.1f);

            Assert.IsFalse(prompt.IsShown);
            Assert.IsFalse(panel.gameObject.activeSelf);
        }

        [UnityTest]
        public IEnumerator RiderInRange_ShowsPromptAndLeavingHidesIt()
        {
            EnterRange();
            Assert.IsTrue(prompt.IsShown);

            yield return new WaitForSecondsRealtime(0.5f);
            Assert.That(prompt.Visibility, Is.EqualTo(1f).Within(0.001f));
            Assert.IsTrue(panel.gameObject.activeSelf);

            LeaveRange();
            Assert.IsFalse(prompt.IsShown);

            yield return new WaitForSecondsRealtime(0.4f);
            Assert.IsFalse(panel.gameObject.activeSelf);
        }

        [UnityTest]
        public IEnumerator Calling_HidesPrompt()
        {
            EnterRange();
            yield return new WaitForSecondsRealtime(0.4f);
            SetField(elevator, "callDelaySeconds", 10f);

            Assert.IsTrue(elevator.RequestTravel());
            Assert.AreEqual(ElevatorTravelState.Calling, elevator.State);
            Assert.AreEqual(ElevatorPromptKind.None, elevator.PromptKind);
            Assert.IsFalse(prompt.IsShown);

            yield return new WaitForSecondsRealtime(0.4f);
            Assert.IsFalse(panel.gameObject.activeSelf);
        }

        [UnityTest]
        public IEnumerator Blocked_KeepsPromptHidden()
        {
            port.ShouldSucceed = false;
            EnterRange();
            yield return new WaitForSecondsRealtime(0.4f);

            Assert.IsTrue(elevator.RequestTravel());
            yield return new WaitForSecondsRealtime(0.4f);

            Assert.AreEqual(ElevatorTravelState.Blocked, elevator.State);
            Assert.IsFalse(prompt.IsShown);
            Assert.IsFalse(panel.gameObject.activeSelf);
        }

        private void EnterRange()
        {
            SetField(elevator, "riderMovement", movement);
            SetField(elevator, "riderBody", body);
            RefreshStatus();
        }

        private void LeaveRange()
        {
            SetField<PlayerMovement>(elevator, "riderMovement", null);
            SetField<Rigidbody2D>(elevator, "riderBody", null);
            RefreshStatus();
        }

        private void RefreshStatus()
        {
            typeof(ElevatorController)
                .GetMethod("RefreshStatus", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(elevator, null);
        }

        private static void SetField<T>(ElevatorController target, string name, T value)
        {
            typeof(ElevatorController)
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(target, value);
        }

        private void SetPromptField(string name, object value)
        {
            typeof(ElevatorHologramPrompt)
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(prompt, value);
        }
    }
}

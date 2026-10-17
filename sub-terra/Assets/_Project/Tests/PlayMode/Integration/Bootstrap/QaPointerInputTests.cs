using System.Collections;
using NUnit.Framework;
using SubTerra.App.UI.HUD;
using SubTerra.Gameplay.Player;
using SubTerra.Shared;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace SubTerra.App.Tests.PlayMode
{
    public sealed class QaPointerInputTests
    {
        private GameObject root;
        private UiTestEnvironment environment;
        private bool previousSettingsOpen;

        [SetUp]
        public void SetUp()
        {
            environment = new UiTestEnvironment();
            previousSettingsOpen = ControlPreferences.IsSettingsOpen;
            ControlPreferences.IsSettingsOpen = false;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (root != null) Object.Destroy(root);
            yield return null;
            environment.Dispose();
            ControlPreferences.IsSettingsOpen = previousSettingsOpen;
        }

        [UnityTest]
        public IEnumerator PointerPulse_UsesExistingMovementAndExpires()
        {
            root = new GameObject("QaInputTest");
            var controls = root.AddComponent<QaPointerInputOverlay>();
            controls.Pulse(Key.RightArrow, 0.15f);
            yield return null;
            yield return null;
            Assert.That(PlayerKeyboardControls.ReadMovement(ControlPreferences.Scheme).x, Is.EqualTo(1f));
            yield return new WaitForSecondsRealtime(0.25f);
            yield return null;
            Assert.That(PlayerKeyboardControls.ReadMovement(ControlPreferences.Scheme), Is.EqualTo(Vector2.zero));
            controls.Pulse(Key.LeftArrow, 1f);
            yield return null;
            Object.Destroy(root);
            yield return null;
            yield return null;
            Assert.That(PlayerKeyboardControls.ReadMovement(ControlPreferences.Scheme), Is.EqualTo(Vector2.zero));
        }

        [UnityTest]
        public IEnumerator PointerInteract_StartsExistingKeyActionOnce()
        {
            root = new GameObject("QaInputTest");
            var controls = root.AddComponent<QaPointerInputOverlay>();
            using var action = new InputAction("Interact", InputActionType.Button, "<Keyboard>/e");
            int started = 0;
            action.started += _ => started++;
            action.Enable();
            controls.Pulse(Key.E, 0.15f);
            yield return null;
            yield return null;
            Assert.That(started, Is.EqualTo(1));
            yield return new WaitForSecondsRealtime(0.25f);
            yield return null;
            Assert.That(started, Is.EqualTo(1));
            Assert.That(action.IsPressed(), Is.False);
        }
    }
}

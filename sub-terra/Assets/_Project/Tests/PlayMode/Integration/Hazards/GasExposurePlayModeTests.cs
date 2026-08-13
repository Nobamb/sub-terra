using System.Collections;
using System.Reflection;
using NUnit.Framework;
using SubTerra.App.Integration;
using SubTerra.App.State;
using SubTerra.App.UI.Hazards;
using SubTerra.Gameplay.Hazards;
using SubTerra.Gameplay.Drone;
using SubTerra.Gameplay.Player;
using UnityEngine;
using UnityEngine.TestTools;
using AppGasRiskLevel = SubTerra.App.State.GasRiskLevel;
using GameplayGasRiskLevel = SubTerra.Gameplay.Hazards.GasRiskLevel;

namespace SubTerra.App.Tests.PlayMode.Hazards
{
    public sealed class GasExposurePlayModeTests
    {
        [UnityTest]
        public IEnumerator H_F02_F05_RuntimeMovementVisionHudAndRunStateStayInSync()
        {
            var playerObject = new GameObject("Player");
            playerObject.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
            var movement = playerObject.AddComponent<PlayerMovement>();
            var overlayObject = new GameObject("GasVisionOverlay");
            var overlay = overlayObject.AddComponent<CanvasGroup>();
            var root = new GameObject("GasIntegration");
            var controller = root.AddComponent<GasExposureEffectController>();
            var bridge = root.AddComponent<GameplayHazardStatusBridge>();
            var droneSensor = root.AddComponent<DroneSensor>();
            bridge.enabled = false;
            SetField(controller, "playerMovement", movement);
            SetField(controller, "visionOverlay", overlay);
            SetField(bridge, "gasEffectController", controller);
            var state = GameState.CreateNew();
            controller.Bind(state, null);
            controller.EffectStateChanged += effect => droneSensor.SetAppliedGasRisk(effect.Risk);
            bridge.BindGameState(state);
            bridge.enabled = true;

            controller.ApplyExposure(new GasExposureState(
                true,
                GameplayGasRiskLevel.Critical,
                GasType.Toxic,
                "gas-playmode",
                20f,
                0.9f));

            Assert.That(movement.CurrentSpeedMultiplier, Is.LessThan(1f));
            Assert.That(overlay.alpha, Is.GreaterThan(0f));
            Assert.That(state.Run.GasExposure, Is.EqualTo(AppGasRiskLevel.Hazard));
            Assert.That(bridge.GasStatus.Severity, Is.EqualTo(HazardSeverity.Critical));
            Assert.That(bridge.GasStatus.ValueText, Does.Contain("한계"));
            Assert.That(droneSensor.CaptureContext().GasRisk,
                Is.EqualTo(GameplayGasRiskLevel.Critical));

            controller.ApplyExposure(default);
            controller.Advance(1f);

            Assert.That(movement.CurrentSpeedMultiplier, Is.EqualTo(1f));
            Assert.That(overlay.alpha, Is.Zero);
            Assert.That(state.Run.GasExposure, Is.EqualTo(AppGasRiskLevel.Safe));
            Assert.That(droneSensor.CaptureContext().GasRisk,
                Is.EqualTo(GameplayGasRiskLevel.Safe));

            Object.Destroy(root);
            Object.Destroy(overlayObject);
            Object.Destroy(playerObject);
            yield return null;
        }

        private static void SetField(object target, string name, object value)
        {
            target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(target, value);
        }
    }
}

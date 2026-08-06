using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using SubTerra.Shared;
using UnityEngine;
using UnityEngine.TestTools;

namespace SubTerra.Gameplay.Player.Tests
{
    public sealed class PlayerSurvivalControllerPlayModeTests
    {
        [UnityTest]
        public IEnumerator L_F01_RuntimeController_NormalizesCollapseGasAndPowerFailures()
        {
            var player = new GameObject("L_Player");
            player.transform.position = new Vector3(0.5f, 0.5f, 0f);
            var host = new GameObject("L_Survival");
            var settings = ScriptableObject.CreateInstance<PlayerSurvivalSettings>();
            var controller = host.AddComponent<PlayerSurvivalController>();
            controller.Configure(settings, player.transform);
            var failures = new List<RunFailureInputDto>();
            controller.FailureRequested += failures.Add;

            Assert.That(controller.ApplyCollapse(new StructuralCollapseEventDto
            {
                worldSeed = 77,
                severity = StructuralCollapseSeverity.Severe,
                cells = new List<CollapseCellDto> { new CollapseCellDto { x = 0, y = 0 } }
            }), Is.True);
            Assert.That(failures.Count, Is.EqualTo(1));
            Assert.That(failures[0].cause, Is.EqualTo(RunFailureCause.StructuralCollapse));
            Assert.That(controller.State.CanAct, Is.False);

            controller.RestoreAfterRescue();
            Assert.That(controller.ApplyGasFailure(new GasExposureFailureInputDto
            {
                gasZoneId = "gas.mid.01",
                cumulativeExposureSeconds = 12f,
                severity = GasExposureFailureSeverity.RescueRequired
            }), Is.True);
            Assert.That(failures.Count, Is.EqualTo(2));
            Assert.That(failures[1].cause, Is.EqualTo(RunFailureCause.GasExposure));

            controller.RestoreAfterRescue();
            Assert.That(controller.ApplyPowerDepletion(), Is.True);
            Assert.That(failures.Count, Is.EqualTo(3));
            Assert.That(failures[2].cause, Is.EqualTo(RunFailureCause.PowerDepleted));

            Object.Destroy(host);
            Object.Destroy(player);
            Object.Destroy(settings);
            yield return null;
        }

        [UnityTest]
        public IEnumerator L_CollapseOutsidePlayer_DoesNotDamageOrFail()
        {
            var player = new GameObject("L_Player_Outside");
            var host = new GameObject("L_Survival_Outside");
            var settings = ScriptableObject.CreateInstance<PlayerSurvivalSettings>();
            var controller = host.AddComponent<PlayerSurvivalController>();
            controller.Configure(settings, player.transform);
            var failureCount = 0;
            controller.FailureRequested += _ => failureCount++;

            Assert.That(controller.ApplyCollapse(new StructuralCollapseEventDto
            {
                worldSeed = 77,
                severity = StructuralCollapseSeverity.Severe,
                cells = new List<CollapseCellDto> { new CollapseCellDto { x = 20, y = -20 } }
            }), Is.False);
            Assert.That(controller.State.Health, Is.EqualTo(controller.State.MaximumHealth));
            Assert.That(failureCount, Is.Zero);

            Object.Destroy(host);
            Object.Destroy(player);
            Object.Destroy(settings);
            yield return null;
        }
    }
}

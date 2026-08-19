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
            Assert.That(failures.Count, Is.Zero);
            Assert.That(controller.State.Health, Is.EqualTo(75));
            Assert.That(controller.State.CanAct, Is.True);

            controller.RestoreAfterRescue();
            Assert.That(controller.ApplyGasFailure(new GasExposureFailureInputDto
            {
                gasZoneId = "gas.mid.01",
                cumulativeExposureSeconds = 12f,
                severity = GasExposureFailureSeverity.RescueRequired
            }), Is.True);
            Assert.That(failures.Count, Is.EqualTo(1));
            Assert.That(failures[0].cause, Is.EqualTo(RunFailureCause.GasExposure));

            controller.RestoreAfterRescue();
            Assert.That(controller.ApplyPowerDepletion(), Is.True);
            Assert.That(failures.Count, Is.EqualTo(2));
            Assert.That(failures[1].cause, Is.EqualTo(RunFailureCause.PowerDepleted));

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

        [UnityTest]
        public IEnumerator CollapseDamage_CountsOnlyCellsThatActuallyHitPlayer_AndCapsAtFifty()
        {
            var player = new GameObject("CollapseHitPlayer");
            player.transform.position = new Vector3(0.5f, 0.5f, 0f);
            var host = new GameObject("CollapseHitSurvival");
            var settings = ScriptableObject.CreateInstance<PlayerSurvivalSettings>();
            var controller = host.AddComponent<PlayerSurvivalController>();
            controller.Configure(settings, player.transform);

            Assert.That(controller.ApplyCollapse(new StructuralCollapseEventDto
            {
                worldSeed = 9,
                severity = StructuralCollapseSeverity.Severe,
                cells = new List<CollapseCellDto>
                {
                    new CollapseCellDto { x = 0, y = 0 },
                    new CollapseCellDto { x = 20, y = 20 },
                    new CollapseCellDto { x = -20, y = -20 }
                }
            }), Is.True);
            Assert.That(controller.State.Health, Is.EqualTo(75));

            Object.Destroy(host);
            Object.Destroy(player);
            Object.Destroy(settings);
            yield return null;

            player = new GameObject("TwoCollapseHitPlayer");
            player.transform.position = new Vector3(0.5f, 0.5f, 0f);
            host = new GameObject("TwoCollapseHitSurvival");
            settings = ScriptableObject.CreateInstance<PlayerSurvivalSettings>();
            controller = host.AddComponent<PlayerSurvivalController>();
            controller.Configure(settings, player.transform);
            Assert.That(controller.ApplyCollapse(new StructuralCollapseEventDto
            {
                worldSeed = 10,
                severity = StructuralCollapseSeverity.Severe,
                cells = new List<CollapseCellDto>
                {
                    new CollapseCellDto { x = 0, y = 0 },
                    new CollapseCellDto { x = 0, y = 1 },
                    new CollapseCellDto { x = 20, y = 20 }
                }
            }), Is.True);
            Assert.That(controller.State.Health, Is.EqualTo(50));

            Object.Destroy(host);
            Object.Destroy(player);
            Object.Destroy(settings);
            yield return null;
        }

        [UnityTest]
        public IEnumerator PromptB55_1_CollapseContactCanDamageAgainAfterInvulnerability()
        {
            var player = new GameObject("CollapseContactPlayer");
            player.transform.position = new Vector3(0.5f, 0.5f, 0f);
            var renderer = player.AddComponent<SpriteRenderer>();
            renderer.color = Color.white;
            var host = new GameObject("CollapseContactSurvival");
            var cameraObject = new GameObject("DamageFeedbackCamera", typeof(Camera));
            var cameraFollow = cameraObject.AddComponent<PlayerCameraFollow>();
            var settings = ScriptableObject.CreateInstance<PlayerSurvivalSettings>();
            var controller = host.AddComponent<PlayerSurvivalController>();
            controller.Configure(settings, player.transform);
            controller.BindCameraFollow(cameraFollow);

            AccessibilityPreferences.ReduceMotion = false;
            Assert.That(controller.IsCollapseContact(0.5f, 3f, 0.5f, -1f), Is.True);
            Assert.That(controller.ApplyCollapseImpact(), Is.True);
            Assert.That(controller.State.Health, Is.EqualTo(75));
            Assert.That(renderer.color.a, Is.LessThan(1f));
            Assert.That(cameraFollow.IsShakeActive, Is.True);

            Assert.That(controller.ApplyCollapseImpact(), Is.False);
            Assert.That(controller.State.Health, Is.EqualTo(75));

            yield return new WaitForSecondsRealtime(0.8f);
            Assert.That(renderer.color.a, Is.EqualTo(1f).Within(0.001f));
            Assert.That(controller.ApplyCollapseImpact(), Is.True);
            Assert.That(controller.State.Health, Is.EqualTo(50));

            AccessibilityPreferences.ReduceMotion = true;
            controller.RestoreAfterRescue();
            Assert.That(controller.ApplyCollapseImpact(), Is.True);
            Assert.That(cameraFollow.IsShakeActive, Is.False);
            Assert.That(renderer.color.a, Is.LessThan(1f));

            AccessibilityPreferences.ReduceMotion = false;
            Object.Destroy(host);
            Object.Destroy(player);
            Object.Destroy(cameraObject);
            Object.Destroy(settings);
            yield return null;
        }
    }
}

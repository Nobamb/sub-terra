using System.Collections;
using System.Reflection;
using NUnit.Framework;
using SubTerra.App.Core;
using SubTerra.App.Integration;
using SubTerra.App.Inventory;
using SubTerra.App.Save;
using SubTerra.App.State;
using SubTerra.Gameplay.Player;
using SubTerra.Shared;
using UnityEngine;
using UnityEngine.TestTools;

namespace SubTerra.App.Tests.PlayMode.RunFailure
{
    public sealed class RunFailureRuntimePlayModeTests
    {
        [UnityTest]
        public IEnumerator PromptB46_SurfaceFallback_RespawnsAtElevatorCenterInMine()
        {
            GameBootstrapper.ResetInstanceForTests();
            if (SaveRuntimeController.Instance != null)
            {
                Object.DestroyImmediate(SaveRuntimeController.Instance.gameObject);
            }

            var state = GameState.CreateNew();
            state.BeginRun();
            var bootstrapObject = new GameObject("PromptB46_Bootstrap");
            var bootstrap = bootstrapObject.AddComponent<GameBootstrapper>();
            bootstrap.enabled = false;
            Assert.That(bootstrap.TryReplaceState(state), Is.True);

            var runtimeObject = new GameObject("PromptB46_Save");
            var runtime = runtimeObject.AddComponent<SaveRuntimeController>();
            yield return null;
            yield return null;

            var catalog = new InMemoryMineralCatalog();
            catalog.Register("mineral.copper", 1f, 10, "Copper");
            var inventoryState = new InventoryState(100f);
            var inventory = new InventoryService(catalog, inventoryState, state);
            inventory.TryAddMineral("mineral.copper", 10);
            SetField(runtime, "inventory", inventoryState);
            SetField(runtime, "inventoryService", inventory);

            var player = new GameObject("PromptB46_Player");
            player.transform.position = new Vector3(8f, -20f, 0f);
            player.AddComponent<Rigidbody2D>().gravityScale = 0f;
            var movement = player.AddComponent<PlayerMovement>();
            var fallback = new GameObject("PromptB46_ElevatorCenter");
            fallback.transform.position = new Vector3(-6.5f, -0.65f, 0f);
            var settings = ScriptableObject.CreateInstance<PlayerSurvivalSettings>();
            var host = new GameObject("PromptB46_RunFailure");
            var survival = host.AddComponent<PlayerSurvivalController>();
            survival.Configure(settings, player.transform);
            var controller = host.AddComponent<RunFailureRuntimeController>();
            SetField(controller, "survivalController", survival);
            SetField(controller, "playerMovement", movement);
            SetField(controller, "playerTransform", player.transform);
            SetField(controller, "localSurfaceFallback", fallback.transform);
            SetField(controller, "failureDisplaySeconds", 0f);
            controller.Bind(runtime, state);

            state.SetCurrentEnergy(0);
            yield return null;

            Assert.That(controller.IsHandling, Is.False);
            Assert.That(player.transform.position, Is.EqualTo(fallback.transform.position));
            Assert.That(state.Run.LifecyclePhase, Is.EqualTo(RunLifecyclePhase.Active));
            Assert.That(state.Player.Energy,
                Is.EqualTo(SaveRuntimeController.MineElevatorEnergyCost));
            Assert.That(movement.CanMove, Is.True);

            controller.Unbind();
            Object.Destroy(host);
            Object.Destroy(settings);
            Object.Destroy(fallback);
            Object.Destroy(player);
            Object.Destroy(runtimeObject);
            Object.Destroy(bootstrapObject);
            yield return null;
            GameBootstrapper.ResetInstanceForTests();
        }

        [UnityTest]
        public IEnumerator L_F04_F05_RuntimeCheckpointRescue_LocksInputAndCommitsOnce()
        {
            GameBootstrapper.ResetInstanceForTests();
            if (SaveRuntimeController.Instance != null)
            {
                Object.DestroyImmediate(SaveRuntimeController.Instance.gameObject);
            }

            var state = GameState.CreateNew();
            state.BeginRun();
            var bootstrapObject = new GameObject("L_Runtime_Bootstrap");
            var bootstrap = bootstrapObject.AddComponent<GameBootstrapper>();
            // 이 테스트는 Scene 전환이 아니라 실패 Runtime만 검증하므로 Bootstrap.Start 자동 전환을 막는다.
            bootstrap.enabled = false;
            Assert.That(bootstrap.TryReplaceState(state), Is.True);

            var runtimeObject = new GameObject("L_Runtime_Save");
            var runtime = runtimeObject.AddComponent<SaveRuntimeController>();
            yield return null;
            yield return null;

            var catalog = new InMemoryMineralCatalog();
            catalog.Register("mineral.copper", 1f, 10, "Copper");
            var inventoryState = new InventoryState(100f);
            var inventory = new InventoryService(catalog, inventoryState, state);
            Assert.That(inventory.TryAddMineral("mineral.copper", 10).Status,
                Is.EqualTo(InventoryMutationStatus.Success));
            SetField(runtime, "inventory", inventoryState);
            SetField(runtime, "inventoryService", inventory);

            var player = new GameObject("L_Runtime_Player");
            player.AddComponent<Rigidbody2D>().gravityScale = 0f;
            var movement = player.AddComponent<PlayerMovement>();
            var playerInput = player.AddComponent<PlayerController>();
            var settings = ScriptableObject.CreateInstance<PlayerSurvivalSettings>();
            var host = new GameObject("L_Runtime_Orchestrator");
            var survival = host.AddComponent<PlayerSurvivalController>();
            survival.Configure(settings, player.transform);
            var controller = host.AddComponent<RunFailureRuntimeController>();
            SetField(controller, "survivalController", survival);
            SetField(controller, "playerMovement", movement);
            SetField(controller, "playerTransform", player.transform);
            SetField(controller, "gameplayInputBehaviours", new Behaviour[] { playerInput });
            SetField(controller, "failureDisplaySeconds", 0.05f);
            controller.Bind(runtime, state);

            var rescueCount = 0;
            PlayerRescueResultDto lastRescue = null;
            controller.PlayerRescued += rescue =>
            {
                rescueCount++;
                lastRescue = rescue;
            };
            controller.Publish(new GameplayEventDto
            {
                type = GameplayEventType.OutpostStatusChanged,
                outpostStatus = new OutpostStatusDto
                {
                    isActive = true,
                    checkpointId = "checkpoint.runtime",
                    checkpointX = 4,
                    checkpointY = -20
                }
            });

            var gasFailure = new GasExposureFailureInputDto
            {
                gasZoneId = "gas.runtime",
                cumulativeExposureSeconds = 12f,
                severity = GasExposureFailureSeverity.RescueRequired
            };
            controller.Publish(new GameplayEventDto
            {
                type = GameplayEventType.GasExposureThreshold,
                gasExposureFailure = gasFailure
            });

            Assert.That(controller.IsHandling, Is.True);
            Assert.That(playerInput.enabled, Is.False);
            Assert.That(movement.CanMove, Is.False);
            yield return new WaitForSecondsRealtime(0.1f);

            Assert.That(controller.IsHandling, Is.False);
            Assert.That(rescueCount, Is.EqualTo(1));
            Assert.That(lastRescue.usedCheckpoint, Is.True);
            Assert.That(lastRescue.cause, Is.EqualTo(RunFailureCause.GasExposure));
            Assert.That(inventory.State.GetQuantity("mineral.copper"), Is.EqualTo(6));
            Assert.That(player.transform.position.x, Is.EqualTo(4.5f).Within(0.001f));
            Assert.That(player.transform.position.y, Is.EqualTo(-19f).Within(0.001f));
            Assert.That(playerInput.enabled, Is.True);
            Assert.That(movement.CanMove, Is.True);
            Assert.That(state.Run.LifecyclePhase, Is.EqualTo(RunLifecyclePhase.Active));

            // 같은 확정 입력을 재발행해도 loss/save/return 흐름은 다시 시작하지 않는다.
            controller.Publish(new GameplayEventDto
            {
                type = GameplayEventType.GasExposureThreshold,
                gasExposureFailure = gasFailure
            });
            yield return null;
            Assert.That(rescueCount, Is.EqualTo(1));
            Assert.That(inventory.State.GetQuantity("mineral.copper"), Is.EqualTo(6));
            Assert.That(survival.State.CanAct, Is.True);

            controller.Unbind();
            Object.Destroy(host);
            Object.Destroy(player);
            Object.Destroy(settings);
            Object.Destroy(runtimeObject);
            Object.Destroy(bootstrapObject);
            yield return null;
            GameBootstrapper.ResetInstanceForTests();
        }

        private static void SetField(object target, string name, object value)
        {
            var field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, "Missing field: " + name);
            field.SetValue(target, value);
        }
    }
}

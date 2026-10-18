using System.Collections;
using System.Reflection;
using NUnit.Framework;
using SubTerra.App.Core;
using SubTerra.App.Integration;
using SubTerra.App.Inventory;
using SubTerra.App.Save;
using SubTerra.App.State;
using SubTerra.App.UI.RunFailure;
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

            var startPos = new Vector3(8f, -20f, 0f);
            var player = new GameObject("PromptB46_Player");
            player.transform.position = startPos;
            player.AddComponent<Rigidbody2D>().gravityScale = 0f;
            var movement = player.AddComponent<PlayerMovement>();
            var fallback = new GameObject("PromptB46_ElevatorCenter");
            fallback.transform.position = new Vector3(-6.5f, -0.65f, 0f);

            var canvasObject = new GameObject("PromptB46_Canvas", typeof(RectTransform), typeof(Canvas));
            var panelObject = new GameObject("RunFailurePanel", typeof(RectTransform));
            panelObject.transform.SetParent(canvasObject.transform, false);
            var view = panelObject.AddComponent<RunFailurePanelView>();
            view.BuildOrUpdateHierarchy();
            view.VisualSettings.darkenDuration = 0.08f;
            view.VisualSettings.heavyGlitchDuration = 0.50f;
            view.VisualSettings.tvTurnOnDuration = 0.08f;

            var settings = ScriptableObject.CreateInstance<PlayerSurvivalSettings>();
            var host = new GameObject("PromptB46_RunFailure");
            var survival = host.AddComponent<PlayerSurvivalController>();
            survival.Configure(settings, player.transform);
            var controller = host.AddComponent<RunFailureRuntimeController>();
            SetField(controller, "survivalController", survival);
            SetField(controller, "playerMovement", movement);
            SetField(controller, "playerTransform", player.transform);
            SetField(controller, "localSurfaceFallback", fallback.transform);
            SetField(controller, "failureView", view);
            controller.Bind(runtime, state);

            state.SetCurrentEnergy(0);
            Assert.That(survival.ApplyFall(100f), Is.True);
            yield return null;

            // 확인 전에는 진행하지 않으며 순간이동도 일어나지 않는다.
            Assert.That(controller.IsHandling, Is.True);
            Assert.That(controller.IsWaitingForConfirmation, Is.True);
            Assert.That(player.transform.position, Is.EqualTo(startPos));
            Assert.That(movement.CanMove, Is.False);

            // 1차 입력: 연출 스킵
            Assert.That(controller.ConfirmOrSkip(), Is.False);
            Assert.That(controller.IsWaitingForConfirmation, Is.True);

            // 2차 입력: 확인 확정 후 암전 → 강한 글리치(이동 1회) → TV 켜짐 전환
            Assert.That(controller.ConfirmOrSkip(), Is.True);
            var elapsed = 0f;
            while (controller.IsHandling && elapsed < 2.5f)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            Assert.That(controller.IsHandling, Is.False);
            Assert.That(controller.PlayerMoveExecutionCount, Is.EqualTo(1));
            Assert.That(player.transform.position, Is.EqualTo(fallback.transform.position));
            Assert.That(state.Run.LifecyclePhase, Is.EqualTo(RunLifecyclePhase.Active));
            Assert.That(movement.CanMove, Is.True);

            controller.Unbind();
            Object.Destroy(canvasObject);
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
            var elevatorFallback = new GameObject("L_Runtime_ElevatorFallback");
            elevatorFallback.transform.position = new Vector3(-6.5f, -0.65f, 0f);

            var canvasObject = new GameObject("L_Runtime_Canvas", typeof(RectTransform), typeof(Canvas));
            var panelObject = new GameObject("RunFailurePanel", typeof(RectTransform));
            panelObject.transform.SetParent(canvasObject.transform, false);
            var view = panelObject.AddComponent<RunFailurePanelView>();
            view.BuildOrUpdateHierarchy();
            view.VisualSettings.darkenDuration = 0.08f;
            view.VisualSettings.heavyGlitchDuration = 0.50f;
            view.VisualSettings.tvTurnOnDuration = 0.08f;

            var survival = host.AddComponent<PlayerSurvivalController>();
            survival.Configure(settings, player.transform);
            var controller = host.AddComponent<RunFailureRuntimeController>();
            SetField(controller, "survivalController", survival);
            SetField(controller, "playerMovement", movement);
            SetField(controller, "playerTransform", player.transform);
            SetField(controller, "localSurfaceFallback", elevatorFallback.transform);
            SetField(controller, "failureView", view);
            SetField(controller, "gameplayInputBehaviours", new Behaviour[] { playerInput });
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
            Assert.That(view.DisplayedCauseTitle, Is.EqualTo("가스 중독"));

            // 스킵 후 확인 확정
            Assert.That(controller.ConfirmOrSkip(), Is.False);
            Assert.That(controller.ConfirmOrSkip(), Is.True);

            var elapsed = 0f;
            while (controller.IsHandling && elapsed < 2.5f)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            Assert.That(controller.IsHandling, Is.False);
            Assert.That(rescueCount, Is.EqualTo(1));
            Assert.That(lastRescue.usedCheckpoint, Is.False);
            Assert.That(lastRescue.cause, Is.EqualTo(RunFailureCause.GasExposure));
            Assert.That(inventory.State.GetQuantity("mineral.copper"), Is.EqualTo(6));
            Assert.That(player.transform.position, Is.EqualTo(elevatorFallback.transform.position));
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
            Object.Destroy(canvasObject);
            Object.Destroy(host);
            Object.Destroy(elevatorFallback);
            Object.Destroy(player);
            Object.Destroy(settings);
            Object.Destroy(runtimeObject);
            Object.Destroy(bootstrapObject);
            yield return null;
            GameBootstrapper.ResetInstanceForTests();
        }

        [Test]
        public void DeferredSurfaceLoadFailure_RestoresActiveRunAndMovement()
        {
            var host = new GameObject("DeferredFailureRecovery");
            var player = new GameObject("DeferredFailurePlayer");
            try
            {
                player.AddComponent<Rigidbody2D>().gravityScale = 0f;
                var movement = player.AddComponent<PlayerMovement>();
                movement.SetCanMove(false);
                var state = GameState.CreateNew();
                state.BeginRun();
                state.SetRunLifecyclePhase(RunLifecyclePhase.Returning);
                var controller = host.AddComponent<RunFailureRuntimeController>();
                SetField(controller, "gameState", state);
                SetField(controller, "playerMovement", movement);
                var callback = typeof(RunFailureRuntimeController).GetMethod(
                    "OnSurfaceLoadCompleted", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(callback, Is.Not.Null);
                callback.Invoke(controller, new object[] { false });
                Assert.That(state.Run.LifecyclePhase, Is.EqualTo(RunLifecyclePhase.Active));
                Assert.That(movement.CanMove, Is.True);
                Assert.That(controller.IsHandling, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(host);
                Object.DestroyImmediate(player);
            }
        }

        private static void SetField(object target, string name, object value)
        {
            var field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, "Missing field: " + name);
            field.SetValue(target, value);
        }
    }
}

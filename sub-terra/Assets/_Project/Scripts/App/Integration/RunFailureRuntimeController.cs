using System;
using System.Collections;
using SubTerra.App.Core;
using SubTerra.App.Run;
using SubTerra.App.Save;
using SubTerra.App.State;
using SubTerra.App.UI.RunFailure;
using SubTerra.Gameplay.Player;
using SubTerra.Shared;
using UnityEngine;

namespace SubTerra.App.Integration
{
    /// <summary>
    /// 실제 위험 이벤트, 입력 잠금, 실패 UI, 저장과 복귀를 순서대로 실행하는 Scene Orchestrator.
    /// </summary>
    public sealed class RunFailureRuntimeController : MonoBehaviour, IGameplayEventSink
    {
        [SerializeField] private PlayerSurvivalController survivalController;
        [SerializeField] private PlayerMovement playerMovement;
        [SerializeField] private Transform playerTransform;
        [SerializeField] private Transform localSurfaceFallback;
        [SerializeField] private RunFailurePanelView failureView;
        [SerializeField] private Behaviour[] gameplayInputBehaviours;
        [SerializeField, Range(0.3f, 0.5f)] private float baseCargoLossRatio = 0.4f;
        [SerializeField, Min(0f)] private float failureDisplaySeconds = 1.25f;

        private SaveRuntimeController runtime;
        private GameState gameState;
        private RunFailureService failureService;
        private OutpostStatusDto latestOutpostStatus;
        private bool[] inputWasEnabled;
        private Coroutine resolveRoutine;

        public event Action<PlayerRescueResultDto> PlayerRescued;
        public bool IsHandling => failureService?.IsHandling ?? false;
        public PlayerSurvivalController SurvivalController => survivalController;

        public void Bind(SaveRuntimeController saveRuntime, GameState state)
        {
            Unbind();
            runtime = saveRuntime;
            gameState = state;
            if (runtime == null || !GameState.IsComplete(gameState) || runtime.InventoryService == null)
            {
                return;
            }

            failureService = new RunFailureService(
                gameState,
                runtime.InventoryService,
                runtime.Progression?.Effects,
                baseCargoLossRatio);
            if (survivalController != null)
            {
                survivalController.BindTarget(playerTransform);
                survivalController.BindMovement(playerMovement);
                survivalController.BindUpgradeEffects(
                    runtime.Progression?.Effects as IPlayerHealthUpgradeProvider);
                survivalController.FailureRequested += OnFailureRequested;
            }

            gameState.EnergyChanged += OnEnergyChanged;
            if (gameState.Run.LifecyclePhase == RunLifecyclePhase.Active
                && gameState.Player.Energy <= 0)
            {
                survivalController?.ApplyPowerDepletion();
            }
        }

        public void Unbind()
        {
            if (survivalController != null)
            {
                survivalController.FailureRequested -= OnFailureRequested;
            }

            if (gameState != null)
            {
                gameState.EnergyChanged -= OnEnergyChanged;
            }

            runtime = null;
            gameState = null;
            failureService = null;
            latestOutpostStatus = null;
        }

        private void OnDestroy()
        {
            Unbind();
        }

        public void Publish(GameplayEventDto gameplayEvent)
        {
            if (gameplayEvent == null || survivalController == null)
            {
                return;
            }

            switch (gameplayEvent.type)
            {
                case GameplayEventType.OutpostStatusChanged:
                    latestOutpostStatus = gameplayEvent.outpostStatus;
                    break;
                case GameplayEventType.StructuralCollapse:
                    // 실제 HP 피해는 낙석 이동 경로와 Player가 접촉한 순간 Gameplay에서 전달한다.
                    break;
                case GameplayEventType.GasExposureThreshold:
                    survivalController.ApplyGasFailure(gameplayEvent.gasExposureFailure);
                    break;
            }
        }

        private void OnEnergyChanged(EnergyReadModel energy)
        {
            if (energy.Current <= 0
                && gameState?.Run?.LifecyclePhase == RunLifecyclePhase.Active)
            {
                survivalController?.ApplyPowerDepletion();
            }
        }

        private void OnFailureRequested(RunFailureInputDto input)
        {
            if (resolveRoutine != null)
            {
                return;
            }

            var returnOutpost = input != null && input.returnToElevator
                    ? null
                    : latestOutpostStatus;
            if (failureService == null
                || !failureService.TryBegin(input, returnOutpost, out var result))
            {
                // 실패 Service가 준비되지 않은 경우에도 Player를 영구 행동불능으로 남기지 않는다.
                survivalController?.RestoreAfterRescue();
                playerMovement?.SetCanMove(true);
                return;
            }

            LockGameplayInput();
            failureView?.Show(result.Rescue);
            resolveRoutine = StartCoroutine(ResolveFailure(result));
        }

        private IEnumerator ResolveFailure(RunFailureResult result)
        {
            var wait = Mathf.Max(0f, failureDisplaySeconds);
            if (wait > 0f)
            {
                yield return new WaitForSecondsRealtime(wait);
            }

            var atCheckpoint = result.ReturnTarget.Kind == RunReturnTargetKind.OutpostCheckpoint;
            var canRecoverInMine = atCheckpoint || localSurfaceFallback != null;
            if (atCheckpoint)
            {
                MovePlayerTo(result.ReturnTarget.X, result.ReturnTarget.Y);
            }
            else if (localSurfaceFallback != null && playerTransform != null)
            {
                playerTransform.position = localSurfaceFallback.position;
                if (gameState?.Player != null
                    && gameState.Player.Energy < SaveRuntimeController.MineElevatorEnergyCost)
                {
                    // 엘리베이터 앞 부활 직후 전력 부족으로 지상 귀환까지 막히는 상태를 방지한다.
                    gameState.SetCurrentEnergy(Mathf.Min(
                        gameState.Player.MaxEnergy,
                        SaveRuntimeController.MineElevatorEnergyCost));
                }
            }

            if (!failureService.Complete(result.Input.failureToken, canRecoverInMine))
            {
                RecoverLocally();
                yield break;
            }

            // Scene을 떠나기 전에 현재 월드 변경점과 화물 손실을 원자 저장 경로로 캡처한다.
            PlayerRescued?.Invoke(result.Rescue);
            runtime?.SaveCurrent(AutoSaveReason.RunFailure);

            if (canRecoverInMine)
            {
                CompleteLocalRecovery();
                yield break;
            }

            if (!new UnitySceneLoader().Load(SceneNames.SurfaceBase))
            {
                // Build 설정 이상에도 플레이어가 영구 행동불능이 되지 않도록 Mine 안전 지점으로 폴백한다.
                gameState?.SetRunLifecyclePhase(RunLifecyclePhase.Active);
                runtime?.SaveCurrent(AutoSaveReason.RunFailure);
                RecoverLocally();
            }
        }

        private void RecoverLocally()
        {
            if (gameState?.Run?.LifecyclePhase == RunLifecyclePhase.Returning)
            {
                gameState.SetRunLifecyclePhase(RunLifecyclePhase.Active);
            }

            if (localSurfaceFallback != null && playerTransform != null)
            {
                playerTransform.position = localSurfaceFallback.position;
            }

            failureView?.ShowSurfaceFallback();
            CompleteLocalRecovery();
        }

        private void CompleteLocalRecovery()
        {
            survivalController?.RestoreAfterRescue();
            playerMovement?.SetCanMove(true);
            RestoreGameplayInput();
            failureView?.Hide();
            resolveRoutine = null;
        }

        private void MovePlayerTo(int x, int y)
        {
            if (playerTransform != null)
            {
                playerTransform.position = new Vector3(x + 0.5f, y + 1f, playerTransform.position.z);
            }
        }

        private void LockGameplayInput()
        {
            playerMovement?.SetCanMove(false);
            if (gameplayInputBehaviours == null)
            {
                return;
            }

            inputWasEnabled = new bool[gameplayInputBehaviours.Length];
            for (var i = 0; i < gameplayInputBehaviours.Length; i++)
            {
                var behaviour = gameplayInputBehaviours[i];
                if (behaviour == null)
                {
                    continue;
                }

                inputWasEnabled[i] = behaviour.enabled;
                behaviour.enabled = false;
            }
        }

        private void RestoreGameplayInput()
        {
            if (gameplayInputBehaviours == null || inputWasEnabled == null)
            {
                return;
            }

            var count = Mathf.Min(gameplayInputBehaviours.Length, inputWasEnabled.Length);
            for (var i = 0; i < count; i++)
            {
                if (gameplayInputBehaviours[i] != null)
                {
                    gameplayInputBehaviours[i].enabled = inputWasEnabled[i];
                }
            }

            inputWasEnabled = null;
        }
    }
}

using System;
using System.Collections;
using SubTerra.App.Core;
using SubTerra.App.Core.Data;
using SubTerra.App.Progression;
using SubTerra.App.Run;
using SubTerra.App.Save;
using SubTerra.App.State;
using SubTerra.App.UI.RunFailure;
using SubTerra.Gameplay.Player;
using SubTerra.Shared;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SubTerra.App.Integration
{
    /// <summary>
    /// 실제 위험 이벤트, 입력 잠금, 실패 UI(글리치·목록·확인 대기), 암전·강한 글리치·TV 켜짐 전환, 저장과 복귀를 순서대로 실행하는 Scene Orchestrator.
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

        private SaveRuntimeController runtime;
        private ProgressionService progression;
        private GameState gameState;
        private RunFailureService failureService;
        private OutpostStatusDto latestOutpostStatus;
        private bool[] inputWasEnabled;
        private Coroutine resolveRoutine;
        private RunFailureResult pendingResult;
        private bool hasCommittedPendingFailure;
        private bool headlessRevealSkipped;
        private bool headlessConfirmed;
        private int playerMoveExecutionCount;

        public event Action<PlayerRescueResultDto> PlayerRescued;
        public bool IsHandling => (failureService != null && failureService.IsHandling) || resolveRoutine != null;
        public bool IsWaitingForConfirmation =>
            resolveRoutine != null
            && !headlessConfirmed
            && (failureView == null || !failureView.IsConfirmed);
        public int PlayerMoveExecutionCount => playerMoveExecutionCount;
        public PlayerSurvivalController SurvivalController => survivalController;
        public RunFailurePanelView FailureView => failureView;

        public void Bind(SaveRuntimeController saveRuntime, GameState state)
        {
            Unbind();
            runtime = saveRuntime;
            progression = runtime != null ? runtime.Progression : null;
            gameState = state;
            if (runtime == null || !GameState.IsComplete(gameState) || runtime.InventoryService == null)
            {
                return;
            }

            if (failureView != null && GameBootstrapper.Instance != null)
            {
                var catalog = GameBootstrapper.Instance.Catalog as GameDataCatalog;
                if (catalog == null)
                {
                    catalog = GameBootstrapper.Instance.AssignedCatalog as GameDataCatalog;
                }

                failureView.BindCatalog(catalog);
            }

            failureService = new RunFailureService(
                gameState,
                runtime.InventoryService,
                progression != null ? progression.Effects : null,
                baseCargoLossRatio);
            if (survivalController != null)
            {
                survivalController.BindTarget(playerTransform);
                survivalController.BindMovement(playerMovement);
                survivalController.BindUpgradeEffects(
                    progression != null ? progression.Effects as IPlayerHealthUpgradeProvider : null);
                survivalController.FailureRequested += OnFailureRequested;
                if (playerMovement != null)
                {
                    var animController = playerMovement.GetComponentInChildren<PlayerAnimationController>(true);
                    if (animController != null)
                    {
                        animController.BindSurvival(survivalController);
                    }
                }
            }

            if (progression != null)
            {
                progression.UpgradeChanged += OnUpgradeChanged;
            }
        }

        public void Unbind()
        {
            if (survivalController != null)
            {
                survivalController.FailureRequested -= OnFailureRequested;
            }

            if (progression != null)
            {
                progression.UpgradeChanged -= OnUpgradeChanged;
            }

            runtime = null;
            progression = null;
            gameState = null;
            failureService = null;
            latestOutpostStatus = null;
            pendingResult = null;
            hasCommittedPendingFailure = false;
            headlessRevealSkipped = false;
            headlessConfirmed = false;
        }

        private void OnUpgradeChanged(UpgradeSnapshot upgrade)
        {
            if (upgrade.UpgradeId != DataIds.Upgrades.MaximumHealth
                && upgrade.UpgradeId != DataIds.Upgrades.HealthRegeneration)
            {
                return;
            }

            // 구매 직후 현재 생존 상태를 다시 계산해 씬 전환 없이 체력 효과를 반영한다.
            if (survivalController != null)
            {
                survivalController.BindUpgradeEffects(
                    progression != null ? progression.Effects as IPlayerHealthUpgradeProvider : null);
            }
        }

        private void OnApplicationQuit()
        {
            CommitPendingFailureForQuit();
        }

        private void OnDestroy()
        {
            Unbind();
        }

        /// <summary>
        /// 확인 버튼 입력 전 게임이 종료되는 경우에도 화물 손실과 복귀 상태가 누락되지 않도록 저장한다.
        /// </summary>
        public void CommitPendingFailureForQuit()
        {
            if (pendingResult == null || hasCommittedPendingFailure || failureService == null)
            {
                return;
            }

            var atCheckpoint = pendingResult.ReturnTarget.Kind == RunReturnTargetKind.OutpostCheckpoint;
            var canRecoverInMine = atCheckpoint || localSurfaceFallback != null;
            MovePlayerToTarget(pendingResult);
            TryCommitAndSave(pendingResult, canRecoverInMine);
        }

        /// <summary>
        /// 연출 진행 중이면 즉시 최종 상태로 스킵하고 false를 반환하며,
        /// 이미 최종 상태에 도달해 있으면 확인을 확정하고 true를 반환한다.
        /// </summary>
        public bool ConfirmOrSkip()
        {
            if (resolveRoutine == null)
            {
                return false;
            }

            if (failureView != null)
            {
                return failureView.ConfirmOrSkip();
            }

            if (!headlessRevealSkipped)
            {
                headlessRevealSkipped = true;
                return false;
            }

            headlessConfirmed = true;
            return true;
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
                if (survivalController != null)
                {
                    survivalController.RestoreAfterRescue();
                }

                if (playerMovement != null)
                {
                    playerMovement.SetCanMove(true);
                }

                return;
            }

            pendingResult = result;
            hasCommittedPendingFailure = false;
            headlessRevealSkipped = false;
            headlessConfirmed = false;
            playerMoveExecutionCount = 0;

            LockGameplayInput();
            if (failureView != null)
            {
                failureView.Show(result.Rescue);
            }

            resolveRoutine = StartCoroutine(ResolveFailure(result));
        }

        private IEnumerator ResolveFailure(RunFailureResult result)
        {
            while (!IsConfirmationReady())
            {
                if (failureView == null && WasHeadlessKeyPressedThisFrame())
                {
                    ConfirmOrSkip();
                }

                yield return null;
            }

            var atCheckpoint = result.ReturnTarget.Kind == RunReturnTargetKind.OutpostCheckpoint;
            var canRecoverInMine = atCheckpoint || localSurfaceFallback != null;
            var commitSucceeded = false;
            var committedInGlitch = false;

            void ExecuteHiddenMoveAndCommit()
            {
                if (committedInGlitch)
                {
                    return;
                }

                committedInGlitch = true;
                MovePlayerToTarget(result);
                commitSucceeded = TryCommitAndSave(result, canRecoverInMine);
            }

            if (failureView != null)
            {
                yield return failureView.PlayTransitionRoutine(canRecoverInMine, ExecuteHiddenMoveAndCommit);
            }
            else
            {
                var fallbackTimeline = new RunFailureTransitionTimeline(
                    result.Rescue != null ? result.Rescue.cause : RunFailureCause.Unknown,
                    null,
                    true);
                var targetDuration = canRecoverInMine
                    ? fallbackTimeline.TotalDuration
                    : fallbackTimeline.PreSurfaceLoadDuration;
                while (fallbackTimeline.ElapsedSeconds < targetDuration - 0.0001f)
                {
                    var dt = Mathf.Max(0.02f, Time.unscaledDeltaTime);
                    var frame = fallbackTimeline.Advance(dt);
                    if (frame.ShouldMovePlayer)
                    {
                        ExecuteHiddenMoveAndCommit();
                    }

                    yield return null;
                }
            }

            if (!committedInGlitch)
            {
                ExecuteHiddenMoveAndCommit();
            }

            if (!commitSucceeded)
            {
                RecoverLocally();
                yield break;
            }

            if (canRecoverInMine)
            {
                CompleteLocalRecovery();
                yield break;
            }

            // 광산 시계는 지상 Scene 로드 전에 먼저 꺼진다.
            var surfaceLoader = runtime != null ? runtime.CreateSurfaceSceneLoader() : new UnitySceneLoader();
            if (!surfaceLoader.Load(SceneNames.SurfaceBase))
            {
                // Build 설정 이상에도 플레이어가 영구 행동불능이 되지 않도록 Mine 안전 지점으로 폴백한다.
                if (gameState != null)
                {
                    gameState.SetRunLifecyclePhase(RunLifecyclePhase.Active);
                }

                if (runtime != null)
                {
                    runtime.SaveCurrent(AutoSaveReason.RunFailure);
                }

                RecoverLocally();
            }
        }

        private bool IsConfirmationReady()
        {
            if (headlessConfirmed)
            {
                return true;
            }

            if (failureView != null)
            {
                return failureView.IsConfirmed;
            }

            return false;
        }

        private bool TryCommitAndSave(RunFailureResult result, bool canRecoverInMine)
        {
            if (hasCommittedPendingFailure)
            {
                return true;
            }

            if (failureService == null
                || result == null
                || result.Input == null
                || !failureService.Complete(result.Input.failureToken, canRecoverInMine))
            {
                return false;
            }

            hasCommittedPendingFailure = true;
            if (PlayerRescued != null)
            {
                PlayerRescued.Invoke(result.Rescue);
            }

            if (runtime != null)
            {
                runtime.SaveCurrent(AutoSaveReason.RunFailure);
            }

            return true;
        }

        private void MovePlayerToTarget(RunFailureResult result)
        {
            if (result == null)
            {
                return;
            }

            playerMoveExecutionCount++;
            var atCheckpoint = result.ReturnTarget.Kind == RunReturnTargetKind.OutpostCheckpoint;
            if (atCheckpoint)
            {
                MovePlayerTo(result.ReturnTarget.X, result.ReturnTarget.Y);
            }
            else if (localSurfaceFallback != null && playerTransform != null)
            {
                ApplyPlayerWorldPosition(localSurfaceFallback.position);
            }
        }

        private void RecoverLocally()
        {
            if (gameState != null
                && gameState.Run != null
                && gameState.Run.LifecyclePhase == RunLifecyclePhase.Returning)
            {
                gameState.SetRunLifecyclePhase(RunLifecyclePhase.Active);
            }

            if (localSurfaceFallback != null && playerTransform != null)
            {
                ApplyPlayerWorldPosition(localSurfaceFallback.position);
            }

            if (failureView != null)
            {
                failureView.ShowSurfaceFallback();
            }

            CompleteLocalRecovery();
        }

        private void CompleteLocalRecovery()
        {
            if (survivalController != null)
            {
                survivalController.RestoreAfterRescue();
            }

            if (playerMovement != null)
            {
                playerMovement.SetCanMove(true);
            }

            RestoreGameplayInput();
            if (failureView != null)
            {
                failureView.Hide();
            }

            pendingResult = null;
            resolveRoutine = null;
        }

        private void MovePlayerTo(int x, int y)
        {
            if (playerTransform != null)
            {
                ApplyPlayerWorldPosition(new Vector3(x + 0.5f, y + 1f, playerTransform.position.z));
            }
        }

        private void ApplyPlayerWorldPosition(Vector3 target)
        {
            if (playerTransform == null)
            {
                return;
            }

            target.z = playerTransform.position.z;
            var body = playerTransform.GetComponent<Rigidbody2D>();
            if (body != null)
            {
                body.linearVelocity = Vector2.zero;
                body.angularVelocity = 0f;
                body.position = target;
            }

            playerTransform.position = target;
            if (Camera.main != null)
            {
                Camera.main.SendMessage("SnapToTarget", SendMessageOptions.DontRequireReceiver);
            }
        }

        private void LockGameplayInput()
        {
            if (playerMovement != null)
            {
                playerMovement.SetCanMove(false);
            }

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

        private static bool WasHeadlessKeyPressedThisFrame()
        {
            var keyboard = Keyboard.current;
            return keyboard != null && keyboard.anyKey.wasPressedThisFrame;
        }
    }
}

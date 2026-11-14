using System;
using System.Collections;
using System.Collections.Generic;
using SubTerra.App.Core;
using SubTerra.App.Core.Data;
using SubTerra.App.Run;
using SubTerra.App.Save;
using SubTerra.App.State;
using SubTerra.App.Tutorial;
using SubTerra.App.UI;
using SubTerra.App.UI.EmergencyEscape;
using SubTerra.Gameplay.Building;
using SubTerra.Gameplay.Mining;
using SubTerra.Gameplay.Player;
using SubTerra.Shared;
using UnityEngine;

namespace SubTerra.App.Integration
{
    /// <summary>포탈 목적지 목록, 결제, 플레이어 이동과 선택 UI를 한 경로에서 처리한다.</summary>
    public sealed class EmergencyEscapePortalRuntimeBridge :
        MonoBehaviour,
        IEmergencyEscapePortalPort,
        IEmergencyEscapeDestinationPreview
    {
        [SerializeField] private Transform playerTransform;
        [SerializeField] private Transform elevatorCenter;
        [SerializeField] private Vector2 outpostArrivalOffset = new(0f, 1f);
        [SerializeField] private EmergencyEscapePanelBinder panelBinder;
        [SerializeField] private PlayerCameraFollow cameraFollow;
        [Tooltip("워프 연출 시간(초). 포탈은 검은 화면 + 웜홀 빨림 → 암전 속 이동 → 목적지 웜홀에서 나옴.")]
        [SerializeField] private EscapeWarpSettings warpSettings = new EscapeWarpSettings();

        /// <summary>워프 연출이 끝날 때까지 게임 진행을 멈추는 소유자. 창이 닫혀도 풀리지 않는다.</summary>
        public const string WarpPauseOwner = "emergency-escape-warp";

        private GameState gameState;
        private EmergencyEscapeService service;
        private EscapeWarpRunner warpRunner;
        private Coroutine warpRoutine;
        private bool warpPending;
        private Vector3 warpTarget;
        private bool warpArrived;

        /// <summary>결제 뒤 연출이 끝나 플레이어가 옮겨지기까지 true.</summary>
        public bool IsWarping => warpPending;
        public EscapeWarpSettings WarpSettings => warpSettings;

        private void Start()
        {
            EnsureRuntimeBindings();
            panelBinder?.BindTo(this);
            // B-162: 포탈 대기음. 씬에 미리 붙여 두면 그 설정을 쓴다.
            if (GetComponent<PortalIdleLoopAudio>() == null)
            {
                gameObject.AddComponent<PortalIdleLoopAudio>();
            }
        }

        public void Bind(GameState state, Transform player, Transform elevator)
        {
            gameState = state;
            if (player != null)
            {
                playerTransform = player;
            }

            if (elevator != null)
            {
                elevatorCenter = elevator;
            }

            service = GameState.IsComplete(state) ? new EmergencyEscapeService(state) : null;
        }

        public void BindPanel(EmergencyEscapePanelBinder binder)
        {
            panelBinder = binder;
            panelBinder?.BindTo(this);
        }

        public void BindCameraFollow(PlayerCameraFollow follow)
        {
            cameraFollow = follow;
        }

        /// <summary>
        /// 선택한 엘리베이터/전진기지 코어를 화면 중앙에 둔다.
        /// 플레이어는 포탈에 그대로 두고 비용도 청구하지 않는다.
        /// </summary>
        public bool TryPreviewDestination(
            EmergencyEscapeDestination kind,
            string outpostInstanceId,
            out string reason)
        {
            reason = string.Empty;
            EnsureRuntimeBindings();
            if (!TryResolveDestination(
                    kind,
                    outpostInstanceId,
                    forArrival: false,
                    out var previewPosition,
                    out reason))
            {
                return false;
            }

            var follow = ResolveCameraFollow();
            if (follow == null)
            {
                return true;
            }

            follow.PreviewWorldPosition(previewPosition, snapImmediately: true);
            return true;
        }

        public bool TryGetDestinationPosition(
            EmergencyEscapeDestination kind,
            string outpostInstanceId,
            out Vector2 position)
        {
            EnsureRuntimeBindings();
            if (!TryResolveDestination(kind, outpostInstanceId, forArrival: false, out var world, out _))
            {
                position = default;
                return false;
            }

            position = world;
            return true;
        }

        public void ClearDestinationPreview()
        {
            var follow = ResolveCameraFollow();
            if (follow == null)
            {
                return;
            }

            follow.ClearPreview(snapImmediately: true);
        }

        public bool TryOpenEscapePanel(out string reason)
        {
            reason = string.Empty;
            EnsureRuntimeBindings();

            if (service == null || playerTransform == null)
            {
                reason = "긴급 탈출 상태가 준비되지 않았습니다.";
                return false;
            }

            if (warpPending)
            {
                reason = "긴급 이동 중입니다.";
                return false;
            }

            if (panelBinder == null)
            {
                reason = "긴급 탈출 선택 창이 준비되지 않았습니다.";
                return false;
            }

            // 비활성 패널 루트도 Open 전에 다시 붙인다.
            panelBinder.BindTo(this);

            var options = GetDestinationOptions();
            if (options.Count == 0)
            {
                reason = "이동할 엘리베이터를 찾을 수 없습니다.";
                return false;
            }

            // 창을 열 때마다 현재 목적지 목록과 이동 가능 여부를 새로 받아 그린다.
            panelBinder.Open(options, service.CurrentCost, service.PeekFailure());
            return true;
        }

        public IReadOnlyList<EmergencyEscapeDestinationOption> GetDestinationOptions()
        {
            var options = new List<EmergencyEscapeDestinationOption>
            {
                new(
                    EmergencyEscapeDestination.Elevator,
                    string.Empty,
                    "엘리베이터")
            };

            var buildings = FindObjectsByType<BuildingInstance>(FindObjectsInactive.Exclude);
            var outposts = new List<BuildingInstance>();
            for (var i = 0; i < buildings.Length; i++)
            {
                var candidate = buildings[i];
                if (candidate != null
                    && candidate.BuildingId == DataIds.Buildings.OutpostCoreBasic
                    && !string.IsNullOrWhiteSpace(candidate.InstanceId))
                {
                    outposts.Add(candidate);
                }
            }

            outposts.Sort((left, right) =>
                string.CompareOrdinal(left.InstanceId, right.InstanceId));

            for (var i = 0; i < outposts.Count; i++)
            {
                var outpost = outposts[i];
                options.Add(new EmergencyEscapeDestinationOption(
                    EmergencyEscapeDestination.OutpostCore,
                    outpost.InstanceId,
                    (ItemDisplayNames.OutpostName + " ") + (i + 1)));
            }

            return options;
        }

        public bool TryEscapeTo(
            EmergencyEscapeDestination kind,
            string outpostInstanceId,
            out string reason)
        {
            reason = string.Empty;
            EnsureRuntimeBindings();

            if (service == null || playerTransform == null)
            {
                reason = "긴급 탈출 상태가 준비되지 않았습니다.";
                return false;
            }

            if (!TryResolveDestination(
                    kind,
                    outpostInstanceId,
                    forArrival: true,
                    out var targetPosition,
                    out reason))
            {
                return false;
            }

            // 결제는 확인 직후 한 번만. 이동은 연출이 끝난 뒤(BeginWarp)에 한다.
            if (!service.TrySpend(out _, out var failure))
            {
                reason = ToReason(failure);
                return false;
            }

            // 창 닫기(웜홀 빨려듦) 연출은 그대로 둔다. 암전은 창이 다 닫힌 뒤에 시작한다.
            panelBinder?.Close();
            reason = kind == EmergencyEscapeDestination.OutpostCore
                ? (ItemDisplayNames.OutpostName + "로 긴급 이동했습니다.")
                : "엘리베이터로 긴급 이동했습니다.";
            BeginWarp(targetPosition);
            return true;
        }

        private void BeginWarp(Vector3 target)
        {
            warpTarget = target;
            warpArrived = false;
            warpPending = true;
            if (!Application.isPlaying)
            {
                // 에디트 모드(테스트)에서는 연출 없이 바로 도착 처리한다.
                CompleteArrival();
                warpPending = false;
                return;
            }

            // 창이 닫혀 자기 정지를 풀어도 연출이 끝날 때까지 월드가 멈춰 있게 먼저 잡는다.
            UiPauseGate.Acquire(WarpPauseOwner);
            warpRoutine = StartCoroutine(WarpRoutine());
        }

        private IEnumerator WarpRoutine()
        {
            // 이 프레임에 바인더가 Close를 시작한다. 창(웜홀 빨려듦)이 완전히 숨겨진 뒤에 포탈 웜홀 연출을 시작한다.
            yield return null;
            while (panelBinder != null && !panelBinder.IsPanelHidden)
            {
                yield return null;
            }

            // 포탈은 글리치 대신 웜홀 스타일: 검은 전체화면 + 웜홀에 빨려 들어간다. 카메라는 움직이지 않는다.
            warpRunner = new EscapeWarpRunner(
                warpSettings,
                EscapeWarpStyle.Wormhole,
                ResolveCameraFollow(),
                playerTransform,
                CompleteArrival);
            yield return StartCoroutine(warpRunner.Run());
            FinishWarp();
        }

        /// <summary>플레이어 이동과 그 뒤 처리(진행도, Enter 억제, 저장). 한 번만 실행한다.</summary>
        private void CompleteArrival()
        {
            if (warpArrived)
            {
                return;
            }

            warpArrived = true;
            if (playerTransform != null)
            {
                TeleportPlayer(warpTarget);
            }

            DemoObjectiveDirector.AdvancePersistedState(
                gameState,
                DemoProgressSignal.EmergencyEscapeSucceeded);
            // B-166: Enter로 확인했으면 아직 눌린 Enter가 도착 자리 옆 칸 채굴(실패음)로 이어지지 않게 한다.
            if (playerTransform != null)
            {
                var mining = playerTransform.GetComponent<PlayerMiningController>();
                if (mining != null)
                {
                    mining.SuppressEnterUntilReleased();
                }
            }

            SaveRuntimeController.Instance?.SaveCurrent(AutoSaveReason.Manual);
        }

        private void FinishWarp()
        {
            warpRoutine = null;
            warpRunner = null;
            warpPending = false;
            UiPauseGate.Release(WarpPauseOwner);
        }

        private void OnDisable()
        {
            if (!warpPending)
            {
                return;
            }

            // 연출 도중 장면이 내려가도 이미 낸 비용만 잃지 않게 도착 처리를 마치고 정지를 푼다.
            if (warpRoutine != null)
            {
                StopCoroutine(warpRoutine);
            }

            if (warpRunner != null)
            {
                warpRunner.Abort();
            }

            CompleteArrival();
            FinishWarp();
        }

        private void EnsureRuntimeBindings()
        {
            var currentState = GameBootstrapper.Instance?.State;
            if (GameState.IsComplete(currentState)
                && !ReferenceEquals(gameState, currentState))
            {
                Bind(currentState, playerTransform, elevatorCenter);
            }
            else if (service == null)
            {
                var state = gameState ?? currentState;
                Bind(state, playerTransform, elevatorCenter);
            }

            if (playerTransform == null)
            {
                var movement = FindAnyObjectByType<PlayerMovement>(FindObjectsInactive.Exclude);
                if (movement != null)
                {
                    playerTransform = movement.transform;
                }
            }

            if (elevatorCenter == null)
            {
                elevatorCenter = FindElevatorCenter();
            }

            if (panelBinder == null)
            {
                panelBinder = FindAnyObjectByType<EmergencyEscapePanelBinder>(
                    FindObjectsInactive.Include);
            }

            if (panelBinder != null)
            {
                panelBinder.BindTo(this);
            }

            ResolveCameraFollow();
        }

        private static Transform FindElevatorCenter()
        {
            var elevators = FindObjectsByType<ElevatorController>(FindObjectsInactive.Exclude);
            for (var i = 0; i < elevators.Length; i++)
            {
                var elevator = elevators[i];
                if (elevator == null)
                {
                    continue;
                }

                var boarding = FindChildRecursive(elevator.transform, "BoardingAnchor");
                if (boarding != null)
                {
                    return boarding;
                }

                return elevator.transform;
            }

            var byName = GameObject.Find("BoardingAnchor");
            return byName != null ? byName.transform : null;
        }

        private static Transform FindChildRecursive(Transform root, string name)
        {
            if (root == null)
            {
                return null;
            }

            var children = root.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < children.Length; i++)
            {
                if (children[i] != null && children[i].name == name)
                {
                    return children[i];
                }
            }

            return null;
        }

        private PlayerCameraFollow ResolveCameraFollow()
        {
            if (cameraFollow != null)
            {
                return cameraFollow;
            }

            var main = Camera.main;
            if (main != null)
            {
                cameraFollow = main.GetComponent<PlayerCameraFollow>();
                if (cameraFollow != null)
                {
                    return cameraFollow;
                }
            }

            if (Application.isPlaying)
            {
                cameraFollow = FindAnyObjectByType<PlayerCameraFollow>(FindObjectsInactive.Exclude);
            }

            return cameraFollow;
        }

        private bool TryResolveDestination(
            EmergencyEscapeDestination kind,
            string outpostInstanceId,
            bool forArrival,
            out Vector3 position,
            out string reason)
        {
            position = default;
            reason = string.Empty;

            if (kind == EmergencyEscapeDestination.Elevator)
            {
                if (elevatorCenter == null)
                {
                    elevatorCenter = FindElevatorCenter();
                }

                if (elevatorCenter == null)
                {
                    reason = "이동할 엘리베이터를 찾을 수 없습니다.";
                    return false;
                }

                position = elevatorCenter.position;
                return true;
            }

            if (string.IsNullOrWhiteSpace(outpostInstanceId))
            {
                reason = (ItemDisplayNames.OutpostName + "를 선택해 주세요.");
                return false;
            }

            var buildings = FindObjectsByType<BuildingInstance>(FindObjectsInactive.Exclude);
            for (var i = 0; i < buildings.Length; i++)
            {
                var candidate = buildings[i];
                if (candidate == null
                    || candidate.BuildingId != DataIds.Buildings.OutpostCoreBasic
                    || !string.Equals(candidate.InstanceId, outpostInstanceId, StringComparison.Ordinal))
                {
                    continue;
                }

                // 미리보기는 코어 자체를 화면 중앙에, 실제 이동은 착지 오프셋을 적용한다.
                position = forArrival
                    ? candidate.transform.position + (Vector3)outpostArrivalOffset
                    : candidate.transform.position;
                return true;
            }

            reason = ("선택한 " + ItemDisplayNames.OutpostName + "를 찾을 수 없습니다.");
            return false;
        }

        private void TeleportPlayer(Vector3 target)
        {
            target.z = playerTransform.position.z;
            var body = playerTransform.GetComponent<Rigidbody2D>();
            if (body != null)
            {
                body.linearVelocity = Vector2.zero;
                body.angularVelocity = 0f;
                body.position = target;
            }

            playerTransform.position = target;
        }

        private static string ToReason(EmergencyEscapePaymentFailure failure)
        {
            return failure switch
            {
                EmergencyEscapePaymentFailure.InsufficientGold => "긴급 탈출에는 100G가 필요합니다.",
                EmergencyEscapePaymentFailure.InsufficientEnergy => "긴급 탈출에 필요한 최대 전력의 10%가 부족합니다.",
                _ => "긴급 탈출 비용을 처리할 수 없습니다."
            };
        }
    }
}

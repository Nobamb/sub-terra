using System;
using System.Collections.Generic;
using SubTerra.App.Core;
using SubTerra.App.Core.Data;
using SubTerra.App.Run;
using SubTerra.App.Save;
using SubTerra.App.State;
using SubTerra.App.UI.EmergencyEscape;
using SubTerra.Gameplay.Building;
using SubTerra.Shared;
using UnityEngine;

namespace SubTerra.App.Integration
{
    /// <summary>포탈 목적지 목록, 결제, 플레이어 이동과 선택 UI를 한 경로에서 처리한다.</summary>
    public sealed class EmergencyEscapePortalRuntimeBridge :
        MonoBehaviour,
        IEmergencyEscapePortalPort
    {
        [SerializeField] private Transform playerTransform;
        [SerializeField] private Transform elevatorCenter;
        [SerializeField] private Vector2 outpostArrivalOffset = new(0f, 1f);
        [SerializeField] private EmergencyEscapePanelBinder panelBinder;

        private GameState gameState;
        private EmergencyEscapeService service;

        private void Start()
        {
            if (service == null)
            {
                Bind(GameBootstrapper.Instance?.State, playerTransform, elevatorCenter);
            }

            if (panelBinder == null)
            {
                panelBinder = FindFirstObjectByType<EmergencyEscapePanelBinder>(FindObjectsInactive.Include);
            }

            panelBinder?.BindTo(this);
        }

        public void Bind(GameState state, Transform player, Transform elevator)
        {
            gameState = state;
            playerTransform = player;
            elevatorCenter = elevator;
            service = GameState.IsComplete(state) ? new EmergencyEscapeService(state) : null;
        }

        public void BindPanel(EmergencyEscapePanelBinder binder)
        {
            panelBinder = binder;
            panelBinder?.BindTo(this);
        }

        public bool TryOpenEscapePanel(out string reason)
        {
            reason = string.Empty;
            if (service == null)
            {
                Bind(GameBootstrapper.Instance?.State, playerTransform, elevatorCenter);
            }

            if (service == null || playerTransform == null)
            {
                reason = "긴급 탈출 상태가 준비되지 않았습니다.";
                return false;
            }

            if (panelBinder == null)
            {
                panelBinder = FindFirstObjectByType<EmergencyEscapePanelBinder>(FindObjectsInactive.Include);
                panelBinder?.BindTo(this);
            }

            if (panelBinder == null)
            {
                reason = "긴급 탈출 선택 창이 준비되지 않았습니다.";
                return false;
            }

            var options = GetDestinationOptions();
            if (options.Count == 0)
            {
                reason = "이동할 엘리베이터를 찾을 수 없습니다.";
                return false;
            }

            panelBinder.Open(options, service.CurrentCost);
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

            var buildings = FindObjectsByType<BuildingInstance>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);
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
                    "전진기지 코어 " + (i + 1)));
            }

            return options;
        }

        public bool TryEscapeTo(
            EmergencyEscapeDestination kind,
            string outpostInstanceId,
            out string reason)
        {
            reason = string.Empty;
            if (service == null)
            {
                Bind(GameBootstrapper.Instance?.State, playerTransform, elevatorCenter);
            }

            if (service == null || playerTransform == null)
            {
                reason = "긴급 탈출 상태가 준비되지 않았습니다.";
                return false;
            }

            if (!TryResolveDestination(kind, outpostInstanceId, out var targetPosition, out reason))
            {
                return false;
            }

            if (!service.TrySpend(out _, out var failure))
            {
                reason = ToReason(failure);
                return false;
            }

            TeleportPlayer(targetPosition);
            panelBinder?.Close();
            SaveRuntimeController.Instance?.SaveCurrent(AutoSaveReason.Manual);
            reason = kind == EmergencyEscapeDestination.OutpostCore
                ? "전진기지 코어로 긴급 이동했습니다."
                : "엘리베이터로 긴급 이동했습니다.";
            return true;
        }

        private bool TryResolveDestination(
            EmergencyEscapeDestination kind,
            string outpostInstanceId,
            out Vector3 position,
            out string reason)
        {
            position = default;
            reason = string.Empty;

            if (kind == EmergencyEscapeDestination.Elevator)
            {
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
                reason = "전진기지 코어를 선택해 주세요.";
                return false;
            }

            var buildings = FindObjectsByType<BuildingInstance>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);
            for (var i = 0; i < buildings.Length; i++)
            {
                var candidate = buildings[i];
                if (candidate == null
                    || candidate.BuildingId != DataIds.Buildings.OutpostCoreBasic
                    || !string.Equals(candidate.InstanceId, outpostInstanceId, StringComparison.Ordinal))
                {
                    continue;
                }

                position = candidate.transform.position + (Vector3)outpostArrivalOffset;
                return true;
            }

            reason = "선택한 전진기지 코어를 찾을 수 없습니다.";
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

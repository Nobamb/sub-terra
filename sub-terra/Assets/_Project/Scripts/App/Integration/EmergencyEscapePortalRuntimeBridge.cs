using System;
using SubTerra.App.Core;
using SubTerra.App.Core.Data;
using SubTerra.App.Run;
using SubTerra.App.Save;
using SubTerra.App.State;
using SubTerra.Gameplay.Building;
using SubTerra.Shared;
using UnityEngine;

namespace SubTerra.App.Integration
{
    /// <summary>포탈의 목적지 결정, 결제, 플레이어 이동과 저장을 한 경로에서 처리한다.</summary>
    public sealed class EmergencyEscapePortalRuntimeBridge :
        MonoBehaviour,
        IEmergencyEscapePortalPort
    {
        [SerializeField] private Transform playerTransform;
        [SerializeField] private Transform elevatorCenter;
        [SerializeField] private Vector2 outpostArrivalOffset = new(0f, 1f);

        private GameState gameState;
        private EmergencyEscapeService service;

        private void Start()
        {
            if (service == null)
            {
                Bind(GameBootstrapper.Instance?.State, playerTransform, elevatorCenter);
            }
        }

        public void Bind(GameState state, Transform player, Transform elevator)
        {
            gameState = state;
            playerTransform = player;
            elevatorCenter = elevator;
            service = GameState.IsComplete(state) ? new EmergencyEscapeService(state) : null;
        }

        public bool TryEscape(
            out EmergencyEscapeDestination destination,
            out string reason)
        {
            destination = EmergencyEscapeDestination.Elevator;
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

            var targetPosition = elevatorCenter != null
                ? elevatorCenter.position
                : Vector3.zero;
            if (TryResolveLatestOutpost(out var outpostPosition))
            {
                destination = EmergencyEscapeDestination.OutpostCore;
                targetPosition = outpostPosition;
            }
            else if (elevatorCenter == null)
            {
                reason = "이동할 엘리베이터를 찾을 수 없습니다.";
                return false;
            }

            if (!service.TrySpend(out _, out var failure))
            {
                reason = ToReason(failure);
                return false;
            }

            TeleportPlayer(targetPosition);
            SaveRuntimeController.Instance?.SaveCurrent(AutoSaveReason.Manual);
            reason = destination == EmergencyEscapeDestination.OutpostCore
                ? "전진기지 코어로 긴급 이동했습니다."
                : "엘리베이터로 긴급 이동했습니다.";
            return true;
        }

        private bool TryResolveLatestOutpost(out Vector3 position)
        {
            position = default;
            BuildingInstance selected = null;
            var buildings = FindObjectsByType<BuildingInstance>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (var i = 0; i < buildings.Length; i++)
            {
                var candidate = buildings[i];
                if (candidate == null
                    || candidate.BuildingId != DataIds.Buildings.OutpostCoreBasic
                    || (selected != null && string.CompareOrdinal(candidate.InstanceId, selected.InstanceId) <= 0))
                {
                    continue;
                }

                selected = candidate;
            }

            if (selected != null)
            {
                position = selected.transform.position + (Vector3)outpostArrivalOffset;
                return true;
            }

            var outpost = gameState?.Outpost;
            if (outpost == null
                || outpost.InstalledOutpostIds.Count == 0
                || string.IsNullOrWhiteSpace(outpost.CheckpointId))
            {
                return false;
            }

            position = new Vector3(
                outpost.CheckpointX + 0.5f,
                outpost.CheckpointY + 1f,
                playerTransform.position.z);
            return true;
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

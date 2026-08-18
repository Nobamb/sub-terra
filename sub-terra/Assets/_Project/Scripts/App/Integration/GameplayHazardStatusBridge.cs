using System;
using SubTerra.App.Core.Data;
using SubTerra.App.State;
using SubTerra.App.UI.Hazards;
using SubTerra.Gameplay.Hazards;
using SubTerra.Gameplay.Power;
using SubTerra.Gameplay.Player;
using SubTerra.Gameplay.Structural;
using SubTerra.Shared;
using UnityEngine;
using AppGasRiskLevel = SubTerra.App.State.GasRiskLevel;
using AppStructuralRiskLevel = SubTerra.App.State.StructuralRiskLevel;
using GameplayGasRiskLevel = SubTerra.Gameplay.Hazards.GasRiskLevel;
using GameplayStructuralRiskLevel = SubTerra.Gameplay.Structural.StructuralRiskLevel;

namespace SubTerra.App.Integration
{
    /// <summary>
    /// A의 확정 위험/전력 이벤트를 B 읽기 모델과 GameState로 번역한다.
    /// 임계치 계산은 하지 않고 A가 결정한 enum·snapshot 값만 사용한다.
    /// </summary>
    public sealed class GameplayHazardStatusBridge :
        MonoBehaviour,
        IHazardStatusSource,
        IGameplayEventSink
    {
        [SerializeField] private StructuralIntegritySystem structuralSystem;
        [SerializeField] private GasHazardSystem gasSystem;
        [SerializeField] private GasExposureEffectController gasEffectController;
        [SerializeField] private PowerNetworkSystem powerSystem;
        [SerializeField] private Transform playerTarget;

        private GameState gameState;
        private Vector3Int lastStructuralCell;
        private bool hasStructuralCell;
        private bool hasStructuralStatus;

        public HazardStatusReadModel StructuralStatus { get; private set; } =
            new(HazardSeverity.Safe, "안전", string.Empty);
        public HazardStatusReadModel GasStatus { get; private set; } =
            new(HazardSeverity.Safe, "안전", string.Empty);
        public PowerStatusReadModel PowerStatus { get; private set; } =
            new(false, 0, 0, 0, "전력망 정보 없음");

        public event Action<HazardStatusReadModel> StructuralStatusChanged;
        public event Action<HazardStatusReadModel> GasStatusChanged;
        public event Action<PowerStatusReadModel> PowerStatusChanged;

        private void OnEnable()
        {
            if (structuralSystem != null)
            {
                structuralSystem.StructuralStatusChanged += OnStructuralStatusChanged;
                ResolvePlayerTarget();
                RefreshLocalStructuralStatus(true);
            }

            if (gasEffectController != null)
            {
                gasEffectController.EffectStateChanged += OnGasEffectStateChanged;
                OnGasEffectStateChanged(gasEffectController.CurrentState);
            }
            else if (gasSystem != null)
            {
                gasSystem.ExposureChanged += OnGasExposureChanged;
                OnGasExposureChanged(gasSystem.CurrentExposure);
            }

            if (powerSystem != null)
            {
                powerSystem.NetworkRebuilt += OnPowerNetworkRebuilt;
                OnPowerNetworkRebuilt(powerSystem.CurrentSnapshot);
            }
        }

        private void OnDisable()
        {
            if (structuralSystem != null)
            {
                structuralSystem.StructuralStatusChanged -= OnStructuralStatusChanged;
            }

            if (gasEffectController != null)
            {
                gasEffectController.EffectStateChanged -= OnGasEffectStateChanged;
            }
            else if (gasSystem != null)
            {
                gasSystem.ExposureChanged -= OnGasExposureChanged;
            }

            if (powerSystem != null)
            {
                powerSystem.NetworkRebuilt -= OnPowerNetworkRebuilt;
            }
        }

        private void Update()
        {
            RefreshLocalStructuralStatus(false);
        }

        public void BindGameState(GameState state)
        {
            gameState = state;
            ApplyGameState();
        }

        /// <summary>
        /// Shared 전진기지 이벤트의 연결·활성·원인 값을 HUD에 반영한다.
        /// 충전/정산 가능 여부를 B에서 다시 계산하지 않고 A의 isActive를 그대로 사용한다.
        /// </summary>
        public void Publish(GameplayEventDto gameplayEvent)
        {
            if (gameplayEvent == null
                || gameplayEvent.type != GameplayEventType.OutpostStatusChanged
                || gameplayEvent.outpostStatus == null)
            {
                return;
            }

            ApplyOutpostStatus(gameplayEvent.outpostStatus);
        }

        public void ApplyOutpostStatus(OutpostStatusDto status)
        {
            if (status == null)
            {
                return;
            }

            PowerStatus = new PowerStatusReadModel(
                status.isActive,
                status.totalPowerSupply,
                status.totalPowerConsumption,
                CountActiveFacilities(status),
                status.isActive ? string.Empty : FormatReason(status.inactiveReasonId));
            PowerStatusChanged?.Invoke(PowerStatus);

            if (gameState != null)
            {
                gameState.SetInteractionPrompt(BuildInteractionPrompt(status));
            }
        }

        private void OnStructuralStatusChanged()
        {
            RefreshLocalStructuralStatus(true);
        }

        private void RefreshLocalStructuralStatus(bool force)
        {
            if (structuralSystem == null) return;
            ResolvePlayerTarget();
            if (playerTarget == null)
            {
                if (!force) return;
                ApplyStructuralStatus(new StructuralRiskStatus(
                    Vector3Int.zero,
                    0f,
                    structuralSystem.CurrentRisk,
                    StructuralRiskCause.None,
                    structuralSystem.CurrentRisk == GameplayStructuralRiskLevel.CollapseImminent));
                return;
            }

            Vector3Int cell = structuralSystem.WorldToCell(playerTarget.position);
            if (!force && hasStructuralCell && cell == lastStructuralCell) return;
            lastStructuralCell = cell;
            hasStructuralCell = true;
            ApplyStructuralStatus(structuralSystem.EvaluateStatusAt(cell));
        }

        private void ResolvePlayerTarget()
        {
            if (playerTarget != null) return;
            PlayerMovement movement = FindAnyObjectByType<PlayerMovement>();
            if (movement != null) playerTarget = movement.transform;
        }

        private void ApplyStructuralStatus(StructuralRiskStatus status)
        {
            GameplayStructuralRiskLevel risk = status.Level;
            var severity = risk == GameplayStructuralRiskLevel.CollapseImminent
                ? HazardSeverity.Imminent
                : risk == GameplayStructuralRiskLevel.Danger
                ? HazardSeverity.Critical
                : risk == GameplayStructuralRiskLevel.Caution
                    ? HazardSeverity.Caution
                    : HazardSeverity.Safe;
            var next = new HazardStatusReadModel(
                severity,
                severity == HazardSeverity.Safe ? "안전"
                    : severity == HazardSeverity.Caution ? "주의"
                    : severity == HazardSeverity.Imminent ? "붕괴 임박" : "위험",
                FormatStructuralCause(status.Cause));
            if (hasStructuralStatus
                && StructuralStatus.Severity == next.Severity
                && StructuralStatus.Label == next.Label
                && StructuralStatus.ValueText == next.ValueText)
            {
                return;
            }

            StructuralStatus = next;
            hasStructuralStatus = true;
            gameState?.SetStructuralRisk(ToAppStructuralRisk(risk));
            StructuralStatusChanged?.Invoke(StructuralStatus);
        }

        private static string FormatStructuralCause(StructuralRiskCause cause)
        {
            return cause == StructuralRiskCause.Unsupported ? "천장 아래가 비어 있음"
                : cause == StructuralRiskCause.MiningImpact ? "채굴 충격 누적"
                : cause == StructuralRiskCause.SupportRemoved ? "버팀목 보호 상실"
                : string.Empty;
        }

        private void OnGasExposureChanged(GasExposureState exposure)
        {
            var severity = exposure.Risk == GameplayGasRiskLevel.Critical
                ? HazardSeverity.Critical
                : exposure.Risk == GameplayGasRiskLevel.Caution
                    ? HazardSeverity.Caution
                    : HazardSeverity.Safe;
            var duration = exposure.IsExposed
                ? "잔여 " + Mathf.Max(0f, exposure.RemainingDuration).ToString("0.0") + "초"
                : string.Empty;
            GasStatus = new HazardStatusReadModel(
                severity,
                severity == HazardSeverity.Safe ? "안전"
                    : severity == HazardSeverity.Caution ? "주의" : "위험",
                duration);
            gameState?.SetGasExposure(ToAppGasRisk(exposure.Risk));
            GasStatusChanged?.Invoke(GasStatus);
        }

        private void OnGasEffectStateChanged(GasExposureEffectState effect)
        {
            var severity = effect.Risk == GameplayGasRiskLevel.Critical
                ? HazardSeverity.Critical
                : effect.Risk == GameplayGasRiskLevel.Caution
                    ? HazardSeverity.Caution
                    : HazardSeverity.Safe;
            var value = effect.IsSheltered
                ? "전진기지 정화 보호"
                : effect.IsExposed
                    ? "노출 " + effect.CumulativeExposure.ToString("0.0")
                        + "초 · 한계 " + effect.FailureThreshold.ToString("0.0") + "초"
                    : string.Empty;
            GasStatus = new HazardStatusReadModel(
                severity,
                severity == HazardSeverity.Safe ? "안전"
                    : severity == HazardSeverity.Caution ? "주의" : "위험",
                value);
            gameState?.SetGasExposure(ToAppGasRisk(effect.Risk));
            GasStatusChanged?.Invoke(GasStatus);
        }

        private void OnPowerNetworkRebuilt(PowerNetworkSnapshot snapshot)
        {
            // NetworkSnapshot의 활성 시설 수는 A가 연결 그래프를 계산한 확정 결과다.
            var connected = snapshot.ActiveFacilityCount > 0 || snapshot.Supply > 0;
            var reason = connected
                ? string.Empty
                : "전진기지 코어 또는 케이블 연결을 확인하세요.";
            PowerStatus = new PowerStatusReadModel(
                connected,
                snapshot.Supply,
                snapshot.Demand,
                snapshot.ActiveFacilityCount,
                reason);
            PowerStatusChanged?.Invoke(PowerStatus);
        }

        private void ApplyGameState()
        {
            if (gameState == null)
            {
                return;
            }

            gameState.SetStructuralRisk(StructuralStatus.Severity == HazardSeverity.Imminent
                ? AppStructuralRiskLevel.Imminent
                : StructuralStatus.Severity == HazardSeverity.Critical
                ? AppStructuralRiskLevel.Critical
                : StructuralStatus.Severity == HazardSeverity.Caution
                    ? AppStructuralRiskLevel.Caution
                    : AppStructuralRiskLevel.Safe);
            gameState.SetGasExposure(GasStatus.Severity == HazardSeverity.Critical
                ? AppGasRiskLevel.Hazard
                : GasStatus.Severity == HazardSeverity.Caution
                    ? AppGasRiskLevel.Elevated
                    : AppGasRiskLevel.Safe);
        }

        private static AppStructuralRiskLevel ToAppStructuralRisk(GameplayStructuralRiskLevel risk)
        {
            return risk == GameplayStructuralRiskLevel.CollapseImminent
                ? AppStructuralRiskLevel.Imminent
                : risk == GameplayStructuralRiskLevel.Danger
                ? AppStructuralRiskLevel.Critical
                : risk == GameplayStructuralRiskLevel.Caution
                    ? AppStructuralRiskLevel.Caution
                    : AppStructuralRiskLevel.Safe;
        }

        private static AppGasRiskLevel ToAppGasRisk(GameplayGasRiskLevel risk)
        {
            return risk == GameplayGasRiskLevel.Critical
                ? AppGasRiskLevel.Hazard
                : risk == GameplayGasRiskLevel.Caution
                    ? AppGasRiskLevel.Elevated
                    : AppGasRiskLevel.Safe;
        }

        private static int CountActiveFacilities(OutpostStatusDto status)
        {
            if (status.connectedFacilities == null)
            {
                return 0;
            }

            var count = 0;
            for (var i = 0; i < status.connectedFacilities.Count; i++)
            {
                if (status.connectedFacilities[i] != null
                    && status.connectedFacilities[i].isActive)
                {
                    count++;
                }
            }

            return count;
        }

        private static string BuildInteractionPrompt(OutpostStatusDto status)
        {
            if (!status.isInInteractionRange || status.connectedFacilities == null)
            {
                return string.Empty;
            }

            for (var i = 0; i < status.connectedFacilities.Count; i++)
            {
                var facility = status.connectedFacilities[i];
                if (facility == null || !IsCurrentInteractionFacility(status, facility))
                {
                    continue;
                }

                if (facility.buildingId == DataIds.Buildings.SettlementBasic)
                {
                    return facility.isActive
                        ? "상호작용: 광물 정산"
                        : string.Empty;
                }
            }

            return string.Empty;
        }

        private static bool IsCurrentInteractionFacility(
            OutpostStatusDto status,
            ConnectedFacilityStatusDto facility)
        {
            if (!string.IsNullOrEmpty(status.interactionFacilityInstanceId))
            {
                return facility.instanceId == status.interactionFacilityInstanceId;
            }

            return facility.buildingId == status.interactionFacilityBuildingId;
        }

        private static string FormatReason(string reasonId)
        {
            switch (reasonId)
            {
                case "power_disconnected":
                    return "전력망 미연결";
                case "insufficient_power":
                    return "전력 부족";
                case "out_of_range":
                    return "상호작용 거리 밖";
                case "core_inactive":
                    return "전진기지 코어 비활성";
                default:
                    return string.IsNullOrEmpty(reasonId) ? "원인 정보 없음" : reasonId;
            }
        }
    }
}

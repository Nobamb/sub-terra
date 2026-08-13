using System;
using System.Collections.Generic;
using SubTerra.App.State;
using SubTerra.Gameplay.Hazards;
using SubTerra.Gameplay.Player;
using SubTerra.Shared;
using UnityEngine;

namespace SubTerra.App.Integration
{
    /// <summary>
    /// Gameplay의 가스 판정을 Player 이동, App 전력, 시야 표현과 실패 입력에 연결한다.
    /// Zone 판정은 재계산하지 않고 GasHazardSystem이 보낸 강도만 사용한다.
    /// </summary>
    public sealed class GasExposureEffectController : MonoBehaviour
    {
        [SerializeField] private GasHazardSystem gasSystem;
        [SerializeField] private PlayerMovement playerMovement;
        [SerializeField] private CanvasGroup visionOverlay;
        [SerializeField] private GasExposureEffectSettings settings = new();

        private GasExposureEffectModel model;
        private GameState gameState;
        private IUpgradeEffectProvider upgradeEffects;
        private GasExposureState currentExposure;
        private readonly HashSet<string> shelteringOutpostIds = new(StringComparer.Ordinal);
        private bool isOutpostSheltered;
        private float appliedResistance = -1f;
        private bool lastVisuallyCleared;
        private GasExposureEffectState lastPublishedState;
        private bool hasPublishedState;

        public GasExposureEffectState CurrentState
        {
            get
            {
                EnsureModel();
                return model.CurrentState;
            }
        }

        public event Action<GasExposureEffectState> EffectStateChanged;
        public event Action<GasExposureFailureInputDto> FailureInputRaised;

        private void Awake()
        {
            EnsureModel();
            ApplyState(model.CurrentState, true);
        }

        private void OnEnable()
        {
            EnsureModel();
            if (gasSystem != null)
            {
                gasSystem.ExposureChanged += ApplyExposure;
                ApplyExposure(gasSystem.CurrentExposure);
            }
        }

        private void Update()
        {
            Advance(Time.deltaTime);
        }

        private void OnDisable()
        {
            if (gasSystem != null)
            {
                gasSystem.ExposureChanged -= ApplyExposure;
            }

            model?.Reset();
            currentExposure = default;
            shelteringOutpostIds.Clear();
            isOutpostSheltered = false;
            appliedResistance = -1f;
            lastVisuallyCleared = false;
            playerMovement?.SetHazardSpeedMultiplier(1f);
            SetVisionObscuration(0f);
        }

        public void Bind(GameState state, IUpgradeEffectProvider effects)
        {
            gameState = state;
            upgradeEffects = effects;
            RefreshMitigation(true);
        }

        public void ApplyExposure(GasExposureState exposure)
        {
            EnsureModel();
            currentExposure = exposure;
            RefreshMitigation(true);
        }

        public void ApplyOutpostStatus(OutpostStatusDto status)
        {
            if (status == null)
            {
                return;
            }

            var id = string.IsNullOrEmpty(status.outpostInstanceId)
                ? "outpost-current"
                : status.outpostInstanceId;
            if (status.isActive && status.isInInteractionRange)
            {
                shelteringOutpostIds.Add(id);
            }
            else
            {
                shelteringOutpostIds.Remove(id);
            }

            var sheltered = shelteringOutpostIds.Count > 0;
            if (isOutpostSheltered == sheltered)
            {
                return;
            }

            isOutpostSheltered = sheltered;
            RefreshMitigation(true);
        }

        /// <summary>테스트와 Update가 공유하는 단일 시간 진행 경로.</summary>
        public GasExposureTickResult Advance(float deltaTime)
        {
            EnsureModel();
            RefreshMitigation(false);
            var result = model.Advance(deltaTime);
            if (result.EnergyDrain > 0 && gameState != null)
            {
                gameState.SetCurrentEnergy(gameState.Player.Energy - result.EnergyDrain);
            }

            ApplyState(result.State, false);
            if (result.FailureThresholdCrossed)
            {
                FailureInputRaised?.Invoke(new GasExposureFailureInputDto
                {
                    gasZoneId = result.State.GasZoneId,
                    effectiveIntensity = result.State.EffectiveIntensity,
                    cumulativeExposureSeconds = result.State.CumulativeExposure,
                    severity = GasExposureFailureSeverity.RescueRequired
                });
            }

            return result;
        }

        private void RefreshMitigation(bool forcePublish)
        {
            EnsureModel();
            var resistance = Mathf.Clamp01(upgradeEffects?.GetGasResistance() ?? 0f);
            var cleared = ResolveVisualClearance();
            if (!forcePublish
                && Mathf.Abs(appliedResistance - resistance) < 0.0001f
                && lastVisuallyCleared == cleared)
            {
                return;
            }

            appliedResistance = resistance;
            lastVisuallyCleared = cleared;
            var state = model.SetExposure(currentExposure, resistance, isOutpostSheltered, cleared);
            ApplyState(state, forcePublish);
        }

        private bool ResolveVisualClearance()
        {
            if (playerMovement == null)
            {
                return false;
            }

            return GasVisionClearanceSource.IsCleared(playerMovement.transform.position);
        }

        private void ApplyState(GasExposureEffectState state, bool forcePublish)
        {
            playerMovement?.SetHazardSpeedMultiplier(state.SpeedMultiplier);
            SetVisionObscuration(state.VisionObscuration);
            if (forcePublish || !hasPublishedState || !state.Equals(lastPublishedState))
            {
                lastPublishedState = state;
                hasPublishedState = true;
                EffectStateChanged?.Invoke(state);
            }
        }

        private void SetVisionObscuration(float value)
        {
            if (visionOverlay == null)
            {
                return;
            }

            visionOverlay.alpha = Mathf.Clamp01(value);
            visionOverlay.interactable = false;
            visionOverlay.blocksRaycasts = false;
        }

        private void EnsureModel()
        {
            model ??= new GasExposureEffectModel(settings);
        }
    }
}

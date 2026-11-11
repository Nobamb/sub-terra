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
        [SerializeField] private GasVisionWorldVeil worldVeil;
        [SerializeField] private GasVisionOverlayDriver overlayDriver;
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
#if UNITY_EDITOR || SUBTERRA_BUILD_DEVELOPMENT
        private Coroutine debugGasRoutine;
        private int debugGasSequence;
#endif

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
#if UNITY_EDITOR || SUBTERRA_BUILD_DEVELOPMENT
            if (debugGasRoutine != null)
            {
                StopCoroutine(debugGasRoutine);
                debugGasRoutine = null;
            }
#endif
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
            if (status.isActive && status.isInPurificationRange)
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
            if (result.ExposureTickCount > 0)
            {
                RaiseDamageTicks(result.State.GasZoneId, result.State.EffectiveIntensity,
                    result.State.CumulativeExposure, result.ExposureTickCount);
            }

            return result;
        }

        private void RaiseDamageTicks(string zoneId, float intensity, float exposureSeconds, int tickCount)
        {
            FailureInputRaised?.Invoke(new GasExposureFailureInputDto
            {
                gasZoneId = zoneId,
                effectiveIntensity = intensity,
                cumulativeExposureSeconds = exposureSeconds,
                damage = tickCount * settings.HealthDamagePerTick,
                severity = GasExposureFailureSeverity.Damage
            });
        }

#if UNITY_EDITOR || SUBTERRA_BUILD_DEVELOPMENT
        internal void ScheduleDebugGasDamage(int tickCount)
        {
            if (debugGasRoutine != null) StopCoroutine(debugGasRoutine);
            debugGasRoutine = StartCoroutine(ApplyDebugGasDamage(tickCount, $"debug-gas-{++debugGasSequence}"));
        }

        private System.Collections.IEnumerator ApplyDebugGasDamage(int remaining, string zoneId)
        {
            float interval = settings.TickInterval;
            float exposure = 0f;
            var wait = new WaitForSeconds(interval);
            while (remaining-- > 0)
            {
                yield return wait;
                exposure += interval;
                RaiseDamageTicks(zoneId, 1f, exposure, 1);
            }
            debugGasRoutine = null;
        }
#endif

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
            var clamped = Mathf.Clamp01(value);
            if (overlayDriver == null && visionOverlay != null)
            {
                overlayDriver = visionOverlay.GetComponent<GasVisionOverlayDriver>();
            }

            if (overlayDriver != null)
            {
                overlayDriver.SetOpacity(clamped);
                if (worldVeil != null)
                {
                    worldVeil.SetOpacity(0f);
                }

                return;
            }

            if (worldVeil == null)
            {
                worldVeil = FindFirstObjectByType<GasVisionWorldVeil>();
            }

            if (worldVeil != null)
            {
                worldVeil.SetOpacity(clamped);
                if (visionOverlay != null)
                {
                    visionOverlay.alpha = 0f;
                    visionOverlay.interactable = false;
                    visionOverlay.blocksRaycasts = false;
                }

                return;
            }

            if (visionOverlay == null)
            {
                return;
            }

            visionOverlay.alpha = clamped;
            visionOverlay.interactable = false;
            visionOverlay.blocksRaycasts = false;
        }

        private void EnsureModel()
        {
            model ??= new GasExposureEffectModel(settings);
        }
    }
}

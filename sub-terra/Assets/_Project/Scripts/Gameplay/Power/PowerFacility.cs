using System;
using UnityEngine;

namespace SubTerra.Gameplay.Power
{
    /// <summary>Turns assigned visuals and interaction availability on only while its node has power.</summary>
    [RequireComponent(typeof(PowerNode))]
    public sealed class PowerFacility : MonoBehaviour
    {
        [SerializeField] private PowerNode powerNode;
        [SerializeField] private GameObject[] poweredVisuals = Array.Empty<GameObject>();

        private PowerOnEffect powerOnEffect;

        public bool IsInteractionAvailable { get; private set; }
        public event Action<bool> AvailabilityChanged;
        /// <summary>B-185 켜짐 연출. 처음 켜질 때 만든다(그 전에는 null).</summary>
        public PowerOnEffect PowerOnEffectInstance => powerOnEffect;
        /// <summary>
        /// B-185-1: 실제 게임의 켜짐 판정(코어·엘리베이터 공급 범위 안). GameplayEventBridge가 정한다.
        /// 케이블 용량 기반 PowerNode.IsPowered와는 별개다.
        /// </summary>
        public bool IsInSupplyRange { get; private set; }

        private void Awake()
        {
            if (powerNode == null) powerNode = GetComponent<PowerNode>();
        }

        private void OnEnable()
        {
            if (powerNode == null) powerNode = GetComponent<PowerNode>();
            if (powerNode != null) powerNode.PowerStateChanged += ApplyPowerState;
            ApplyPowerState(powerNode != null && powerNode.IsPowered);
        }

        private void OnDisable()
        {
            if (powerNode != null) powerNode.PowerStateChanged -= ApplyPowerState;
            if (powerOnEffect != null) powerOnEffect.Stop();
        }

        /// <summary>
        /// B-185-1: 범위 판정 결과를 받는다. 꺼짐→켜짐이면 연출을 시작하고 true.
        /// 켜짐→켜짐·켜짐→꺼짐·복원 중(PowerOnEffect.IsSuppressed)·allowEffect false는 연출 없이 상태만 맞춘다.
        /// 상호작용 가능 여부와 켜짐 그림(PowerNode 기준)은 바꾸지 않는다.
        /// </summary>
        public bool SetSupplyRangePowered(bool inRange, bool allowEffect)
        {
            bool turningOn = inRange && !IsInSupplyRange;
            IsInSupplyRange = inRange;
            if (!inRange)
            {
                // 켜짐 → 꺼짐은 연출 없이 바로 끈다. 연출 중이면 즉시 정리한다.
                if (powerOnEffect != null) powerOnEffect.Stop();
                return false;
            }

            if (!turningOn || !allowEffect || PowerOnEffect.IsSuppressed) return false;
            if (!enabled || !gameObject.activeInHierarchy) return false;
            return PlayPowerOnEffect();
        }

        private void ApplyPowerState(PowerNode _, bool powered) => ApplyPowerState(powered);

        private void ApplyPowerState(bool powered)
        {
            foreach (GameObject visual in poweredVisuals)
            {
                if (visual != null) visual.SetActive(powered);
            }
            if (IsInteractionAvailable == powered) return;
            IsInteractionAvailable = powered;
            AvailabilityChanged?.Invoke(IsInteractionAvailable);
        }

        private bool PlayPowerOnEffect()
        {
            try
            {
                if (powerOnEffect == null) powerOnEffect = GetComponent<PowerOnEffect>();
                if (powerOnEffect == null) powerOnEffect = gameObject.AddComponent<PowerOnEffect>();
                powerOnEffect.Bind(this);
                return powerOnEffect.Play(poweredVisuals);
            }
            catch (Exception exception)
            {
                // 연출 실패가 전력·상호작용 상태를 막지 않게 한다.
                Debug.LogException(exception, this);
                return false;
            }
        }
    }
}

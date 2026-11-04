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

        private void Awake()
        {
            if (powerNode == null) powerNode = GetComponent<PowerNode>();
        }

        private void OnEnable()
        {
            if (powerNode == null) powerNode = GetComponent<PowerNode>();
            if (powerNode != null) powerNode.PowerStateChanged += ApplyPowerState;
            // 첫 적용은 연출 없이 바로 켠다.
            ApplyPowerState(powerNode != null && powerNode.IsPowered, false);
        }

        private void OnDisable()
        {
            if (powerNode != null) powerNode.PowerStateChanged -= ApplyPowerState;
            if (powerOnEffect != null) powerOnEffect.Stop();
        }

        private void ApplyPowerState(PowerNode _, bool powered) => ApplyPowerState(powered, true);

        private void ApplyPowerState(bool powered, bool allowEffect)
        {
            bool turningOn = powered && !IsInteractionAvailable;
            foreach (GameObject visual in poweredVisuals)
            {
                if (visual != null) visual.SetActive(powered);
            }
            if (!powered)
            {
                // 켜짐 → 꺼짐은 연출 없이 바로 끈다. 연출 중이면 즉시 정리한다.
                if (powerOnEffect != null) powerOnEffect.Stop();
            }
            else if (turningOn && allowEffect && !PowerOnEffect.IsSuppressed)
            {
                PlayPowerOnEffect();
            }
            if (IsInteractionAvailable == powered) return;
            IsInteractionAvailable = powered;
            AvailabilityChanged?.Invoke(IsInteractionAvailable);
        }

        private void PlayPowerOnEffect()
        {
            try
            {
                if (powerOnEffect == null) powerOnEffect = GetComponent<PowerOnEffect>();
                if (powerOnEffect == null) powerOnEffect = gameObject.AddComponent<PowerOnEffect>();
                powerOnEffect.Bind(this, powerNode);
                powerOnEffect.Play(poweredVisuals);
            }
            catch (Exception exception)
            {
                // 연출 실패가 전력·상호작용 상태를 막지 않게 한다.
                Debug.LogException(exception, this);
            }
        }
    }
}

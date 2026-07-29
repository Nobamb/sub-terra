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

        public bool IsInteractionAvailable { get; private set; }
        public event Action<bool> AvailabilityChanged;

        private void Awake()
        {
            if (powerNode == null) powerNode = GetComponent<PowerNode>();
        }

        private void OnEnable()
        {
            if (powerNode != null) powerNode.PowerStateChanged += ApplyPowerState;
            ApplyPowerState(powerNode != null && powerNode.IsPowered);
        }

        private void OnDisable()
        {
            if (powerNode != null) powerNode.PowerStateChanged -= ApplyPowerState;
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
    }
}

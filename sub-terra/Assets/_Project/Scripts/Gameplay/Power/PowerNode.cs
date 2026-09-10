using System;
using UnityEngine;

namespace SubTerra.Gameplay.Power
{
    /// <summary>A core, generator, or facility connection point in the runtime power graph.</summary>
    public sealed class PowerNode : MonoBehaviour
    {
        [SerializeField] private PowerNetworkSystem network;
        [SerializeField] private bool isPowerSource;
        [SerializeField, Min(0)] private int supply;
        [SerializeField, Min(0)] private int demand;
        [SerializeField] private PowerPriority priority = PowerPriority.Normal;
        [SerializeField] private string entityId;
        [SerializeField] private Transform cablePortAnchor;

        public bool IsPowerSource => isPowerSource;
        public int Supply => supply;
        public int Demand => demand;
        public PowerPriority Priority => priority;
        public string EntityId => entityId;
        public PowerNetworkSystem Network => network;
        /// <summary>Visual cable endpoint. Nodes without a facility port use their own transform.</summary>
        public Vector3 CablePortPosition => cablePortAnchor != null
            ? cablePortAnchor.position
            : transform.position;
        public bool IsPowered { get; private set; }
        public event Action<PowerNode, bool> PowerStateChanged;

        private void OnEnable() => network?.RegisterNode(this);
        private void OnDisable() => network?.UnregisterNode(this);

        public void Configure(PowerNetworkSystem targetNetwork, bool source, int sourceSupply, int powerDemand, PowerPriority nodePriority)
        {
            SetNetwork(targetNetwork);
            isPowerSource = source;
            supply = Mathf.Max(0, sourceSupply);
            demand = Mathf.Max(0, powerDemand);
            priority = nodePriority;
            network?.RequestRebuild();
        }

        /// <summary>Assigns this node to a network after a facility prefab has been instantiated or restored.</summary>
        public void SetNetwork(PowerNetworkSystem targetNetwork)
        {
            if (network == targetNetwork)
            {
                return;
            }

            if (isActiveAndEnabled)
            {
                network?.UnregisterNode(this);
            }

            network = targetNetwork;
            if (isActiveAndEnabled)
            {
                network?.RegisterNode(this);
            }
        }

        public void SetDemand(int nextDemand)
        {
            demand = Mathf.Max(0, nextDemand);
            network?.RequestRebuild();
        }

        public void SetEntityId(string nextEntityId)
        {
            entityId = nextEntityId ?? string.Empty;
            network?.RequestRebuild();
        }

        /// <summary>Assigns the visual socket used by power-cable rendering.</summary>
        public void SetCablePortAnchor(Transform anchor) => cablePortAnchor = anchor;

        internal void SetPowered(bool powered)
        {
            if (IsPowered == powered) return;
            IsPowered = powered;
            PowerStateChanged?.Invoke(this, IsPowered);
        }
    }
}

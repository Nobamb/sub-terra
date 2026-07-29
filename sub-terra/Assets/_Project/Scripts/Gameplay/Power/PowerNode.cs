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

        public bool IsPowerSource => isPowerSource;
        public int Supply => supply;
        public int Demand => demand;
        public PowerPriority Priority => priority;
        public bool IsPowered { get; private set; }
        public event Action<PowerNode, bool> PowerStateChanged;

        private void OnEnable() => network?.RegisterNode(this);
        private void OnDisable() => network?.UnregisterNode(this);

        public void Configure(PowerNetworkSystem targetNetwork, bool source, int sourceSupply, int powerDemand, PowerPriority nodePriority)
        {
            network = targetNetwork;
            isPowerSource = source;
            supply = Mathf.Max(0, sourceSupply);
            demand = Mathf.Max(0, powerDemand);
            priority = nodePriority;
        }

        public void SetDemand(int nextDemand)
        {
            demand = Mathf.Max(0, nextDemand);
            network?.RequestRebuild();
        }

        internal void SetPowered(bool powered)
        {
            if (IsPowered == powered) return;
            IsPowered = powered;
            PowerStateChanged?.Invoke(this, IsPowered);
        }
    }
}

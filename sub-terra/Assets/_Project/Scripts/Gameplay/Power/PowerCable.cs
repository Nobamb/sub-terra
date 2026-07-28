using UnityEngine;

namespace SubTerra.Gameplay.Power
{
    /// <summary>An explicit graph edge. The network recalculates only when this edge changes.</summary>
    public sealed class PowerCable : MonoBehaviour
    {
        [SerializeField] private PowerNetworkSystem network;
        [SerializeField] private PowerNode endpointA;
        [SerializeField] private PowerNode endpointB;

        public PowerNode EndpointA => endpointA;
        public PowerNode EndpointB => endpointB;
        public bool IsValid => endpointA != null && endpointB != null && endpointA != endpointB;

        private void OnEnable() => network?.RegisterCable(this);
        private void OnDisable() => network?.UnregisterCable(this);

        public void Configure(PowerNetworkSystem targetNetwork, PowerNode first, PowerNode second)
        {
            network = targetNetwork;
            endpointA = first;
            endpointB = second;
        }

        public bool Connects(PowerNode node) => endpointA == node || endpointB == node;
        public PowerNode GetOther(PowerNode node) => endpointA == node ? endpointB : endpointB == node ? endpointA : null;
    }
}

using UnityEngine;

namespace SubTerra.Gameplay.Power
{
    /// <summary>An explicit graph edge. The network recalculates only when this edge changes.</summary>
    public sealed class PowerCable : MonoBehaviour
    {
        [SerializeField] private PowerNetworkSystem network;
        [SerializeField] private PowerNode endpointA;
        [SerializeField] private PowerNode endpointB;
        [SerializeField, Min(0.001f)] private float visualWidth = 0.055f;

        private SpriteRenderer cableRenderer;
        private bool rendererResolved;

        public PowerNode EndpointA => endpointA;
        public PowerNode EndpointB => endpointB;
        public bool IsValid => endpointA != null && endpointB != null && endpointA != endpointB;

        private void Awake() => ResolveRenderer();

        private void OnEnable() => network?.RegisterCable(this);
        private void OnDisable() => network?.UnregisterCable(this);
        private void LateUpdate() => RefreshVisual();

        public void Configure(PowerNetworkSystem targetNetwork, PowerNode first, PowerNode second)
        {
            network = targetNetwork;
            endpointA = first;
            endpointB = second;
            RefreshVisual();
        }

        /// <summary>Aligns the visual cable with facility sockets without affecting graph connectivity.</summary>
        public void RefreshVisual()
        {
            ResolveRenderer();

            if (cableRenderer == null || cableRenderer.sprite == null || !IsValid)
            {
                return;
            }

            Vector3 start = endpointA.CablePortPosition;
            Vector3 end = endpointB.CablePortPosition;
            Vector3 direction = end - start;
            float length = direction.magnitude;
            if (length <= 0.0001f)
            {
                cableRenderer.enabled = false;
                return;
            }

            cableRenderer.enabled = true;
            transform.position = (start + end) * 0.5f;
            transform.right = direction / length;

            Vector2 spriteSize = cableRenderer.sprite.bounds.size;
            if (spriteSize.x <= 0.0001f || spriteSize.y <= 0.0001f)
            {
                return;
            }

            transform.localScale = new Vector3(
                length / spriteSize.x,
                visualWidth / spriteSize.y,
                1f);
        }

        private void ResolveRenderer()
        {
            if (rendererResolved)
            {
                return;
            }

            cableRenderer = GetComponent<SpriteRenderer>();
            rendererResolved = true;
        }

        public bool Connects(PowerNode node) => endpointA == node || endpointB == node;
        public PowerNode GetOther(PowerNode node) => endpointA == node ? endpointB : endpointB == node ? endpointA : null;
    }
}

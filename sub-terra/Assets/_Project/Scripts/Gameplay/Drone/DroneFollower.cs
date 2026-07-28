using UnityEngine;

namespace SubTerra.Gameplay.Drone
{
    /// <summary>Follows the player smoothly while preserving a small separation distance.</summary>
    public sealed class DroneFollower : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector2 followOffset = new(-1.2f, 1f);
        [SerializeField, Min(0.01f)] private float smoothTime = 0.25f;
        [SerializeField, Min(0f)] private float minimumDistance = 0.5f;

        private Vector3 velocity;
        public Transform Target => target;

        private void LateUpdate()
        {
            if (target == null) return;
            Vector3 desired = target.position + (Vector3)followOffset;
            Vector3 next = Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime);
            if (Vector2.Distance(next, target.position) < minimumDistance) next = target.position + (Vector3)followOffset.normalized * minimumDistance;
            transform.position = new Vector3(next.x, next.y, transform.position.z);
        }

        public void SetTarget(Transform nextTarget) => target = nextTarget;
    }
}

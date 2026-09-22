using UnityEngine;

namespace SubTerra.Gameplay.Drone
{
    /// <summary>Follows the player smoothly while preserving a small separation distance.</summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class DroneFollower : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private SpriteRenderer visualRenderer;
        [SerializeField] private Vector2 followOffset = new(-1.2f, 1f);
        [SerializeField, Min(0.01f)] private float smoothTime = 0.25f;
        [SerializeField, Min(0f)] private float minimumDistance = 0.5f;
        [SerializeField] private bool unflippedSpriteFacesRight = true;
        [SerializeField, Min(0f)] private float facingDeadZone = 0.01f;

        private Vector3 velocity;
        public Transform Target => target;

        private void Awake()
        {
            if (visualRenderer == null)
            {
                visualRenderer = GetComponent<SpriteRenderer>();
            }
        }

        private void LateUpdate()
        {
            if (target == null) return;
            Vector3 desired = target.position + (Vector3)followOffset;
            UpdateFacing(desired.x - transform.position.x);
            Vector3 next = Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime);
            if (Vector2.Distance(next, target.position) < minimumDistance) next = target.position + (Vector3)followOffset.normalized * minimumDistance;
            transform.position = new Vector3(next.x, next.y, transform.position.z);
        }

        public void SetTarget(Transform nextTarget) => target = nextTarget;

        private void UpdateFacing(float horizontalDelta)
        {
            if (visualRenderer == null || Mathf.Abs(horizontalDelta) <= facingDeadZone)
            {
                return;
            }

            bool facesRight = horizontalDelta > 0f;
            visualRenderer.flipX = unflippedSpriteFacesRight ? !facesRight : facesRight;
        }
    }
}

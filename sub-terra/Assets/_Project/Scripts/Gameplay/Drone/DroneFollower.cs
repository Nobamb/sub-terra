using UnityEngine;

namespace SubTerra.Gameplay.Drone
{
    /// <summary>Follows the player smoothly while preserving a small separation distance.</summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class DroneFollower : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private SpriteRenderer visualRenderer;
        [SerializeField] private Sprite rightFacingSprite;
        [SerializeField] private Vector2 followOffset = new(-1.2f, 1f);
        [SerializeField, Min(0.01f)] private float smoothTime = 0.25f;
        [SerializeField, Min(0f)] private float minimumDistance = 0.5f;
        [SerializeField] private bool unflippedSpriteFacesRight = true;
        [SerializeField, Min(0f)] private float facingDeadZone = 0.01f;

        private Vector3 velocity;
        public Transform Target => target;

        private void Awake()
        {
            CacheVisualRenderer();
            ApplyFacingSprite();
        }

        private void LateUpdate()
        {
            Tick(Time.deltaTime);
        }

        public void Tick(float deltaTime)
        {
            if (target == null) return;
            Vector3 desired = target.position + (Vector3)followOffset;
            // 회피 목표만 바꾼다. 최소 거리를 강제로 맞춰 현재 위치를 반대편으로 순간 이동시키지 않는다.
            Vector2 separation = (Vector2)(transform.position - target.position);
            if (separation.sqrMagnitude < minimumDistance * minimumDistance)
            {
                Vector2 direction = separation.sqrMagnitude > 0.0001f
                    ? separation.normalized : followOffset.normalized;
                desired = target.position + (Vector3)(direction * minimumDistance);
            }
            UpdateFacing(desired.x - transform.position.x);
            if (deltaTime <= 0f) return;
            Vector3 next = Vector3.SmoothDamp(transform.position, desired, ref velocity,
                smoothTime, Mathf.Infinity, deltaTime);
            transform.position = new Vector3(next.x, next.y, transform.position.z);
        }

        public void SetTarget(Transform nextTarget)
        {
            if (target == nextTarget) return;
            target = nextTarget;
            velocity = Vector3.zero;
        }

        private void UpdateFacing(float horizontalDelta)
        {
            if (!CacheVisualRenderer())
            {
                return;
            }

            ApplyFacingSprite();
            if (Mathf.Abs(horizontalDelta) <= facingDeadZone) return;

            bool facesRight = horizontalDelta > 0f;
            visualRenderer.flipX = unflippedSpriteFacesRight ? !facesRight : facesRight;
        }

        private void ApplyFacingSprite()
        {
            if (visualRenderer != null && rightFacingSprite != null && visualRenderer.sprite != rightFacingSprite)
            {
                visualRenderer.sprite = rightFacingSprite;
            }
        }

        private bool CacheVisualRenderer()
        {
            if (visualRenderer == null)
            {
                visualRenderer = GetComponent<SpriteRenderer>();
            }

            return visualRenderer != null;
        }
    }
}

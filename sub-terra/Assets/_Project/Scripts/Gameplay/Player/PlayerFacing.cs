using UnityEngine;

namespace SubTerra.Gameplay.Player
{
    [RequireComponent(typeof(PlayerMovement))]
    public sealed class PlayerFacing : MonoBehaviour
    {
        [SerializeField] private Transform visualRoot;

        private PlayerMovement movement;
        private SpriteRenderer visualRenderer;
        private float lastDirection;

        private void Awake()
        {
            movement = GetComponent<PlayerMovement>();
            CacheVisualRenderer();
            ApplyFacing(force: true);
        }

        private void OnEnable()
        {
            ApplyFacing(force: true);
        }

        private void LateUpdate()
        {
            ApplyFacing(force: false);
        }

        private void CacheVisualRenderer()
        {
            visualRenderer = visualRoot != null
                ? visualRoot.GetComponentInChildren<SpriteRenderer>(includeInactive: true)
                : null;
        }

        private void ApplyFacing(bool force)
        {
            if (movement == null || visualRoot == null)
            {
                return;
            }

            float direction = movement.FacingDirection < 0f ? -1f : 1f;
            if (!force && Mathf.Approximately(lastDirection, direction))
            {
                return;
            }

            lastDirection = direction;
            if (visualRenderer == null)
            {
                CacheVisualRenderer();
            }

            Vector3 scale = visualRoot.localScale;
            scale.x = Mathf.Abs(scale.x);
            visualRoot.localScale = scale;

            if (visualRenderer != null)
            {
                visualRenderer.flipX = direction < 0f;
                return;
            }

            // 커스텀 비주얼에 SpriteRenderer가 없는 경우에도 기존 동작을 유지한다.
            scale.x *= direction;
            visualRoot.localScale = scale;
        }
    }
}

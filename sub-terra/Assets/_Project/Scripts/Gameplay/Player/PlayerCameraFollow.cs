using UnityEngine;

namespace SubTerra.Gameplay.Player
{
    [RequireComponent(typeof(Camera))]
    public sealed class PlayerCameraFollow : MonoBehaviour
    {
        private const float TargetViewportPadding = 0.25f;

        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new(0f, 1f, -10f);
        [SerializeField, Min(0f)] private float smoothTime = 0.18f;
        [SerializeField, Min(0f)] private float teleportDistance = 3f;
        [SerializeField] private CameraBounds2D boundsProvider;

        private Vector3 velocity;
        private Camera controlledCamera;
        private CameraBounds2D cachedBoundsProvider;
        private CameraClampLimits clampLimits;
        private float cachedOrthographicSize = -1f;
        private float cachedAspect = -1f;
        private int cachedBoundsVersion = -1;
        private Vector3 previousTargetPosition;
        private bool hasPreviousTargetPosition;

        public void SetTarget(Transform newTarget, bool snapImmediately = false)
        {
            target = newTarget;
            hasPreviousTargetPosition = false;
            if (snapImmediately)
            {
                SnapToTarget();
            }
        }

        public void SetBoundsProvider(
            CameraBounds2D newBoundsProvider,
            bool snapImmediately = false)
        {
            boundsProvider = newBoundsProvider;
            InvalidateClampCache();
            if (snapImmediately)
            {
                SnapToTarget();
            }
        }

        /// <summary>로드·구조 실패 등 순간이동 뒤 잔류 속도 없이 즉시 정렬합니다.</summary>
        public bool SnapToTarget()
        {
            if (target == null)
            {
                return false;
            }

            EnsureCamera();
            transform.position = Constrain(target.position + offset);
            velocity = Vector3.zero;
            previousTargetPosition = target.position;
            hasPreviousTargetPosition = true;
            return true;
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            Vector3 currentTargetPosition = target.position;
            if (!hasPreviousTargetPosition
                || (teleportDistance > 0f
                    && (currentTargetPosition - previousTargetPosition).sqrMagnitude
                        >= teleportDistance * teleportDistance))
            {
                SnapToTarget();
                return;
            }

            Vector3 targetPosition = target.position + offset;
            Vector3 nextPosition = Vector3.SmoothDamp(
                transform.position,
                targetPosition,
                ref velocity,
                smoothTime);
            nextPosition = KeepTargetVisible(nextPosition, currentTargetPosition);
            Vector3 constrainedPosition = Constrain(nextPosition);
            if (!Mathf.Approximately(constrainedPosition.x, nextPosition.x))
            {
                velocity.x = 0f;
            }

            if (!Mathf.Approximately(constrainedPosition.y, nextPosition.y))
            {
                velocity.y = 0f;
            }

            transform.position = constrainedPosition;
            previousTargetPosition = currentTargetPosition;
        }

        private Vector3 KeepTargetVisible(Vector3 cameraPosition, Vector3 targetPosition)
        {
            EnsureCamera();
            if (controlledCamera == null || !controlledCamera.orthographic)
            {
                return cameraPosition;
            }

            float halfHeight = Mathf.Max(
                0f,
                controlledCamera.orthographicSize - TargetViewportPadding);
            float halfWidth = Mathf.Max(
                0f,
                controlledCamera.orthographicSize * controlledCamera.aspect
                    - TargetViewportPadding);
            cameraPosition.x = Mathf.Clamp(
                cameraPosition.x,
                targetPosition.x - halfWidth,
                targetPosition.x + halfWidth);
            cameraPosition.y = Mathf.Clamp(
                cameraPosition.y,
                targetPosition.y - halfHeight,
                targetPosition.y + halfHeight);
            return cameraPosition;
        }

        private Vector3 Constrain(Vector3 position)
        {
            EnsureCamera();
            if (boundsProvider == null
                || controlledCamera == null
                || !controlledCamera.orthographic)
            {
                return position;
            }

            RefreshClampLimits();
            return clampLimits.Clamp(position);
        }

        private void EnsureCamera()
        {
            if (controlledCamera == null)
            {
                controlledCamera = GetComponent<Camera>();
            }
        }

        private void RefreshClampLimits()
        {
            if (cachedBoundsProvider == boundsProvider
                && cachedBoundsVersion == boundsProvider.Version
                && Mathf.Approximately(cachedOrthographicSize, controlledCamera.orthographicSize)
                && Mathf.Approximately(cachedAspect, controlledCamera.aspect))
            {
                return;
            }

            // Tilemap을 다시 훑지 않고 viewport가 바뀐 경우에만 제한값을 갱신합니다.
            clampLimits = CameraViewportClamp.Calculate(
                boundsProvider.WorldBounds,
                controlledCamera.orthographicSize,
                controlledCamera.aspect);
            cachedBoundsProvider = boundsProvider;
            cachedBoundsVersion = boundsProvider.Version;
            cachedOrthographicSize = controlledCamera.orthographicSize;
            cachedAspect = controlledCamera.aspect;
        }

        private void InvalidateClampCache()
        {
            cachedBoundsProvider = null;
            cachedBoundsVersion = -1;
            cachedOrthographicSize = -1f;
            cachedAspect = -1f;
        }
    }
}

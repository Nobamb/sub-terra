using SubTerra.Shared;
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

        [Header("Screen Shake")]
        [SerializeField, Min(0f)] private float defaultShakeAmplitude = 0.22f;
        [SerializeField, Min(0f)] private float defaultShakeDuration = 0.28f;

        private Vector3 velocity;
        private Camera controlledCamera;
        private CameraBounds2D cachedBoundsProvider;
        private CameraClampLimits clampLimits;
        private float cachedOrthographicSize = -1f;
        private float cachedAspect = -1f;
        private int cachedBoundsVersion = -1;
        private Vector3 previousTargetPosition;
        private bool hasPreviousTargetPosition;
        private float shakeTimeRemaining;
        private float shakeDuration;
        private float shakeAmplitude;

        public bool IsShakeActive => shakeTimeRemaining > 0f
            && !AccessibilityPreferences.ReduceMotion;

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
            shakeTimeRemaining = 0f;
            return true;
        }

        /// <summary>
        /// 구조 붕괴·위험 시 화면 흔들림. 접근성 "화면 진동 억제"가 켜져 있으면 무시한다.
        /// </summary>
        public void RequestShake(float amplitude = -1f, float duration = -1f)
        {
            if (AccessibilityPreferences.ReduceMotion)
            {
                return;
            }

            float nextAmplitude = amplitude > 0f ? amplitude : defaultShakeAmplitude;
            float nextDuration = duration > 0f ? duration : defaultShakeDuration;
            // 더 강한/긴 요청이 오면 덮어쓰고, 약하면 현재를 유지한다.
            if (nextAmplitude >= shakeAmplitude || shakeTimeRemaining <= 0f)
            {
                shakeAmplitude = nextAmplitude;
            }

            shakeDuration = Mathf.Max(shakeDuration, nextDuration);
            shakeTimeRemaining = Mathf.Max(shakeTimeRemaining, nextDuration);
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

            transform.position = constrainedPosition + SampleShakeOffset();
            previousTargetPosition = currentTargetPosition;
        }

        private Vector3 SampleShakeOffset()
        {
            if (shakeTimeRemaining <= 0f || AccessibilityPreferences.ReduceMotion)
            {
                shakeTimeRemaining = 0f;
                shakeAmplitude = 0f;
                return Vector3.zero;
            }

            shakeTimeRemaining = Mathf.Max(0f, shakeTimeRemaining - Time.deltaTime);
            float falloff = shakeDuration > 0f
                ? Mathf.Clamp01(shakeTimeRemaining / shakeDuration)
                : 0f;
            float strength = shakeAmplitude * falloff;
            // 프레임마다 다른 오프셋으로 붕괴/위험 피드백을 준다.
            return new Vector3(
                (Mathf.PerlinNoise(Time.time * 28f, 0.17f) - 0.5f) * 2f * strength,
                (Mathf.PerlinNoise(0.41f, Time.time * 31f) - 0.5f) * 2f * strength,
                0f);
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

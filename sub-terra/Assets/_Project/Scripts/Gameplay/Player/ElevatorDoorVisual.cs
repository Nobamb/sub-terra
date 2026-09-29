using SubTerra.Shared;
using UnityEngine;

namespace SubTerra.Gameplay.Player
{
    /// <summary>Animates the two front shutters without owning travel or boarding rules.</summary>
    [RequireComponent(typeof(ElevatorController))]
    public sealed class ElevatorDoorVisual : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer leftDoor;
        [SerializeField] private SpriteRenderer rightDoor;
        [SerializeField] private SpriteMask doorOpeningMask;
        [SerializeField, Min(0.01f)] private float slideDuration = 0.28f;
        [SerializeField, Min(0.01f)] private float openingDuration = 0.52f;
        [SerializeField, Min(0f)] private float initialOpenDelay = 0.16f;
        [SerializeField] private float openDoorOffset = 1.05f;
        [SerializeField] private float closedDoorOffset = 0.34f;
        [SerializeField] private float doorCenterY = -0.55f;
        [SerializeField] private Vector2 doorScale = new(1.9f, 0.87f);
        [SerializeField] private Vector2 openingMaskSize = new(1.36f, 1.16f);

        private ElevatorController elevator;
        private float closedFraction;
        private float targetFraction;
        private float transitionStartFraction;
        private float transitionElapsed;
        private float transitionDuration;
        private float openingDelayRemaining;
        private bool transitionActive;

        public float ClosedFraction => closedFraction;

        private void OnEnable()
        {
            elevator = GetComponent<ElevatorController>();
            elevator.StateChanged += OnStateChanged;
            ApplyGeometry();

            if (ShouldClose(elevator.State))
            {
                SnapTo(1f);
                return;
            }

            // 정거장 진입 시 닫힌 문을 먼저 보여 준 뒤 열어, 도착 연출이
            // 순간적으로 열린 모습으로 튀지 않도록 한다.
            closedFraction = 1f;
            targetFraction = 0f;
            openingDelayRemaining = initialOpenDelay;
            transitionActive = false;
            ApplyPose();
            if (openingDelayRemaining <= 0f)
            {
                BeginTransition(0f);
            }
        }

        private void OnDisable()
        {
            if (elevator != null)
            {
                elevator.StateChanged -= OnStateChanged;
            }
        }

        private void Update()
        {
            if (openingDelayRemaining > 0f)
            {
                openingDelayRemaining -= Time.unscaledDeltaTime;
                if (openingDelayRemaining > 0f)
                {
                    return;
                }

                BeginTransition(targetFraction);
            }

            if (!transitionActive)
            {
                return;
            }

            transitionElapsed += Time.unscaledDeltaTime;
            float progress = transitionDuration <= 0f
                ? 1f
                : Mathf.Clamp01(transitionElapsed / transitionDuration);
            float eased = SmootherStep(progress);
            closedFraction = Mathf.Lerp(transitionStartFraction, targetFraction, eased);
            ApplyPose();

            if (progress >= 1f)
            {
                closedFraction = targetFraction;
                transitionActive = false;
                ApplyPose();
            }
        }

        private void OnStateChanged(ElevatorTravelState state)
        {
            openingDelayRemaining = 0f;
            BeginTransition(ShouldClose(state) ? 1f : 0f);
        }

        private static bool ShouldClose(ElevatorTravelState state)
        {
            return state == ElevatorTravelState.Calling || state == ElevatorTravelState.Moving;
        }

        private void BeginTransition(float target)
        {
            targetFraction = Mathf.Clamp01(target);
            float distance = Mathf.Abs(targetFraction - closedFraction);
            if (distance <= 0.0001f)
            {
                SnapTo(targetFraction);
                return;
            }

            transitionStartFraction = closedFraction;
            transitionElapsed = 0f;
            float fullDuration = targetFraction > closedFraction
                ? slideDuration
                : openingDuration;
            transitionDuration = Mathf.Max(0.01f, fullDuration * distance);
            transitionActive = true;
        }

        private void SnapTo(float target)
        {
            openingDelayRemaining = 0f;
            targetFraction = Mathf.Clamp01(target);
            closedFraction = targetFraction;
            transitionStartFraction = closedFraction;
            transitionElapsed = 0f;
            transitionDuration = 0f;
            transitionActive = false;
            ApplyPose();
        }

        private static float SmootherStep(float value)
        {
            float t = Mathf.Clamp01(value);
            return t * t * t * (t * (t * 6f - 15f) + 10f);
        }

        private void ApplyGeometry()
        {
            if (leftDoor != null)
            {
                leftDoor.transform.localScale = new Vector3(doorScale.x, doorScale.y, 1f);
            }

            if (rightDoor != null)
            {
                rightDoor.transform.localScale = new Vector3(doorScale.x, doorScale.y, 1f);
            }

            if (doorOpeningMask == null)
            {
                Transform maskTransform = transform.Find("DoorOpeningMask");
                if (maskTransform != null)
                {
                    doorOpeningMask = maskTransform.GetComponent<SpriteMask>();
                }
            }

            if (doorOpeningMask != null)
            {
                doorOpeningMask.transform.localPosition = new Vector3(0f, doorCenterY, 0f);
                doorOpeningMask.transform.localScale =
                    new Vector3(openingMaskSize.x, openingMaskSize.y, 1f);
            }
        }

        private void ApplyPose()
        {
            float offset = Mathf.Lerp(openDoorOffset, closedDoorOffset, closedFraction);
            bool visible = closedFraction > 0.001f;
            if (leftDoor != null)
            {
                leftDoor.transform.localPosition = new Vector3(-offset, doorCenterY, 0f);
                leftDoor.enabled = visible;
            }

            if (rightDoor != null)
            {
                rightDoor.transform.localPosition = new Vector3(offset, doorCenterY, 0f);
                rightDoor.enabled = visible;
            }
        }
    }
}

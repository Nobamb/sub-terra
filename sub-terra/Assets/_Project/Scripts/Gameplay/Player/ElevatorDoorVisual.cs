using SubTerra.Shared;
using UnityEngine;

namespace SubTerra.Gameplay.Player
{
    /// <summary>Animates the doors and cabin while travel rules remain in ElevatorController.</summary>
    [RequireComponent(typeof(ElevatorController))]
    public sealed class ElevatorDoorVisual : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer leftDoor;
        [SerializeField] private SpriteRenderer rightDoor;
        [SerializeField] private Transform artwork;
        [SerializeField] private Transform openingMask;
        [SerializeField, Min(0.01f)] private float slideDuration = 0.28f;
        [SerializeField, Min(0.01f)] private float riseDuration = 0.55f;
        [SerializeField, Min(0f)] private float riseDistance = 1.4f;
        [SerializeField] private float openOffset = 0.9f;
        [SerializeField] private float closedOffset = 0.29f;
        [SerializeField] private float doorCenterY = -0.55f;

        private ElevatorController elevator;
        private float closedFraction;
        private float targetFraction;
        private float riseFraction;
        private float targetRise;
        private Vector3 artworkBasePosition;
        private Vector3 maskBasePosition;
        private Transform riderVisual;
        private Vector3 riderBasePosition;

        public float ClosedFraction => closedFraction;

        private void OnEnable()
        {
            elevator = GetComponent<ElevatorController>();
            elevator.StateChanged += OnStateChanged;
            artworkBasePosition = artwork != null ? artwork.localPosition : Vector3.zero;
            maskBasePosition = openingMask != null ? openingMask.localPosition : Vector3.zero;
            targetFraction = ShouldClose(elevator.State) ? 1f : 0f;
            // A newly loaded Mine scene starts with the car closed, then reveals its rider.
            closedFraction = elevator.State == ElevatorTravelState.Arrived
                ? 1f
                : targetFraction;
            targetRise = elevator.State == ElevatorTravelState.Moving ? 1f : 0f;
            riseFraction = targetRise;
            if (targetRise > 0f)
            {
                CaptureRiderVisual();
            }
            ApplyPose();
        }

        private void OnDisable()
        {
            if (elevator != null)
            {
                elevator.StateChanged -= OnStateChanged;
            }

            RestoreRiderVisual();
            riseFraction = 0f;
            targetRise = 0f;
            closedFraction = 0f;
            targetFraction = 0f;
            ApplyPose();
        }

        private void Update()
        {
            float nextRise = Mathf.MoveTowards(
                riseFraction, targetRise, Time.unscaledDeltaTime / riseDuration);
            bool riseChanged = !Mathf.Approximately(nextRise, riseFraction);
            if (riseChanged)
            {
                riseFraction = nextRise;
            }

            // Never expose the rider while the cabin has a visual travel offset.
            if (riseFraction <= 0.001f && !ShouldClose(elevator.State))
            {
                targetFraction = 0f;
            }

            float nextClosed = Mathf.MoveTowards(
                closedFraction, targetFraction, Time.unscaledDeltaTime / slideDuration);
            bool doorsChanged = !Mathf.Approximately(nextClosed, closedFraction);
            if (doorsChanged)
            {
                closedFraction = nextClosed;
            }

            if (riseChanged || doorsChanged)
            {
                ApplyPose();
            }
        }

        private void OnStateChanged(ElevatorTravelState state)
        {
            targetRise = state == ElevatorTravelState.Moving ? 1f : 0f;
            if (targetRise > 0f)
            {
                CaptureRiderVisual();
            }
            else if (state == ElevatorTravelState.Arrived || state == ElevatorTravelState.Blocked)
            {
                // Gameplay unlocks the rider at this point. Restore its visual offset first.
                riseFraction = 0f;
                RestoreRiderVisual();
                ApplyPose();
            }

            targetFraction = ShouldClose(state) || riseFraction > 0.001f ? 1f : 0f;
        }

        private void CaptureRiderVisual()
        {
            Transform rider = elevator.RiderTransform;
            PlayerAnimationController animation = rider != null
                ? rider.GetComponentInChildren<PlayerAnimationController>()
                : null;
            Transform candidate = animation != null ? animation.transform : null;
            if (candidate == riderVisual)
            {
                return;
            }

            RestoreRiderVisual();
            riderVisual = candidate;
            if (riderVisual != null)
            {
                riderBasePosition = riderVisual.localPosition;
            }
        }

        private void RestoreRiderVisual()
        {
            if (riderVisual != null)
            {
                riderVisual.localPosition = riderBasePosition;
            }

            riderVisual = null;
        }

        private static bool ShouldClose(ElevatorTravelState state)
        {
            return state == ElevatorTravelState.Calling || state == ElevatorTravelState.Moving;
        }

        private void ApplyPose()
        {
            float offset = Mathf.Lerp(openOffset, closedOffset, closedFraction);
            float rise = riseDistance * riseFraction;
            bool visible = closedFraction > 0.001f;
            if (artwork != null)
            {
                artwork.localPosition = artworkBasePosition + Vector3.up * rise;
            }

            if (openingMask != null)
            {
                openingMask.localPosition = maskBasePosition + Vector3.up * rise;
            }

            if (riderVisual != null)
            {
                riderVisual.localPosition = riderBasePosition + Vector3.up * rise;
            }

            if (leftDoor != null)
            {
                leftDoor.transform.localPosition = new Vector3(-offset, doorCenterY + rise, 0f);
                leftDoor.enabled = visible;
            }

            if (rightDoor != null)
            {
                rightDoor.transform.localPosition = new Vector3(offset, doorCenterY + rise, 0f);
                rightDoor.enabled = visible;
            }
        }
    }
}

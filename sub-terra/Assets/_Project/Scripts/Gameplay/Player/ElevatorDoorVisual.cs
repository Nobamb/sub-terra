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
        [SerializeField, Min(0.01f)] private float slideDuration = 0.28f;
        [SerializeField] private float openOffset = 0.9f;
        [SerializeField] private float closedOffset = 0.29f;
        [SerializeField] private float doorCenterY = -0.55f;

        private ElevatorController elevator;
        private float closedFraction;
        private float targetFraction;

        public float ClosedFraction => closedFraction;

        private void OnEnable()
        {
            elevator = GetComponent<ElevatorController>();
            elevator.StateChanged += OnStateChanged;
            targetFraction = ShouldClose(elevator.State) ? 1f : 0f;
            closedFraction = targetFraction;
            ApplyPose();
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
            if (Mathf.Approximately(closedFraction, targetFraction))
            {
                return;
            }

            closedFraction = Mathf.MoveTowards(
                closedFraction, targetFraction, Time.unscaledDeltaTime / slideDuration);
            ApplyPose();
        }

        private void OnStateChanged(ElevatorTravelState state)
        {
            targetFraction = ShouldClose(state) ? 1f : 0f;
        }

        private static bool ShouldClose(ElevatorTravelState state)
        {
            return state == ElevatorTravelState.Calling || state == ElevatorTravelState.Moving;
        }

        private void ApplyPose()
        {
            float offset = Mathf.Lerp(openOffset, closedOffset, closedFraction);
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

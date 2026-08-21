using SubTerra.App.Core;
using SubTerra.App.Save;
using SubTerra.App.Tutorial;
using SubTerra.Gameplay.Player;
using SubTerra.Shared;
using UnityEngine;

namespace SubTerra.App.Integration
{
    public sealed class MineReturnDepartureGate
    {
        private bool observedInside;

        public bool Observe(
            bool arrivedByElevator,
            float playerX,
            float elevatorCenterX,
            float protectedAreaHalfWidth)
        {
            if (!arrivedByElevator)
            {
                return false;
            }

            var isInside = Mathf.Abs(playerX - elevatorCenterX) <= protectedAreaHalfWidth;
            if (isInside)
            {
                observedInside = true;
                return false;
            }

            return observedInside;
        }
    }

    /// <summary>Gameplay 정거장의 목적지를 App 저장·Scene 전환 경계에 연결한다.</summary>
    public sealed class ElevatorTravelBridge : MonoBehaviour, IElevatorTravelPort
    {
        private const float ProtectedAreaHalfWidth = 1.5f;

        private readonly MineReturnDepartureGate mineReturnGate = new();
        private Transform playerTransform;
        private Transform elevatorTransform;
        private bool mineReturnCompleted;

        public ElevatorTravelState State =>
            SaveRuntimeController.Instance?.ElevatorState ?? ElevatorTravelState.Idle;

        private void Update()
        {
            if (mineReturnCompleted)
            {
                return;
            }

            var runtime = SaveRuntimeController.Instance;
            var state = GameBootstrapper.Instance?.State;
            if (runtime == null
                || runtime.ElevatorState != ElevatorTravelState.Arrived
                || state?.Progress?.CurrentObjectiveId != DemoObjectiveIds.ReturnToMine)
            {
                return;
            }

            ResolveTargets();
            if (playerTransform == null
                || elevatorTransform == null
                || !mineReturnGate.Observe(
                    true,
                    playerTransform.position.x,
                    elevatorTransform.position.x,
                    ProtectedAreaHalfWidth))
            {
                return;
            }

            mineReturnCompleted = DemoObjectiveDirector.AdvancePersistedState(
                state,
                DemoProgressSignal.MineReachedByElevator).Advanced;
        }

        public bool TryTravel(ElevatorDestination destination, out string reason)
        {
            var runtime = SaveRuntimeController.Instance;
            if (runtime == null)
            {
                reason = "저장 런타임을 찾을 수 없습니다.";
                return false;
            }

            var state = GameBootstrapper.Instance?.State;
            var succeeded = destination == ElevatorDestination.Mine
                ? runtime.TryStartExploration(out reason)
                : runtime.TryReturnToSurface(out reason);
            if (succeeded && destination == ElevatorDestination.SurfaceBase)
            {
                DemoObjectiveDirector.AdvancePersistedState(
                    state,
                    DemoProgressSignal.SurfaceReachedByElevator);
            }

            return succeeded;
        }

        private void ResolveTargets()
        {
            if (playerTransform == null)
            {
                var movement = FindAnyObjectByType<PlayerMovement>(FindObjectsInactive.Exclude);
                if (movement != null)
                {
                    playerTransform = movement.transform;
                }
            }

            if (elevatorTransform == null)
            {
                var elevator = FindAnyObjectByType<ElevatorController>(FindObjectsInactive.Exclude);
                if (elevator != null)
                {
                    elevatorTransform = elevator.transform;
                }
            }
        }
    }
}

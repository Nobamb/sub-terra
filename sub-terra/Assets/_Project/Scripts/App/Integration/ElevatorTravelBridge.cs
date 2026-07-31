using SubTerra.App.Save;
using SubTerra.Shared;
using UnityEngine;

namespace SubTerra.App.Integration
{
    /// <summary>Gameplay 정거장의 목적지를 App 저장·Scene 전환 경계에 연결한다.</summary>
    public sealed class ElevatorTravelBridge : MonoBehaviour, IElevatorTravelPort
    {
        public ElevatorTravelState State =>
            SaveRuntimeController.Instance?.ElevatorState ?? ElevatorTravelState.Idle;

        public bool TryTravel(ElevatorDestination destination, out string reason)
        {
            var runtime = SaveRuntimeController.Instance;
            if (runtime == null)
            {
                reason = "저장 런타임을 찾을 수 없습니다.";
                return false;
            }

            return destination == ElevatorDestination.Mine
                ? runtime.TryStartExploration(out reason)
                : runtime.TryReturnToSurface(out reason);
        }
    }
}

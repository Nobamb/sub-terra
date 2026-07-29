using SubTerra.App.Outpost;
using SubTerra.Shared;
using UnityEngine;

namespace SubTerra.App.Integration
{
    /// <summary>
    /// Shared Gameplay 이벤트를 OutpostService에 전달한다.
    /// 거리·전력·활성 판정은 DTO 값을 그대로 사용한다.
    /// </summary>
    public sealed class OutpostRuntimeBridge : MonoBehaviour, IGameplayEventSink
    {
        private OutpostService service;

        public void BindTo(OutpostService outpostService)
        {
            service = outpostService;
        }

        public void Publish(GameplayEventDto gameplayEvent)
        {
            if (service == null || gameplayEvent == null)
            {
                return;
            }

            if (gameplayEvent.type == GameplayEventType.OutpostStatusChanged)
            {
                service.ApplyRuntimeStatus(gameplayEvent.outpostStatus);
                return;
            }

            if (gameplayEvent.type != GameplayEventType.OutpostActivated)
            {
                return;
            }

            var status = gameplayEvent.outpostStatus;
            var instanceId = !string.IsNullOrEmpty(gameplayEvent.instanceId)
                ? gameplayEvent.instanceId
                : status?.outpostInstanceId;
            var checkpointId = !string.IsNullOrEmpty(status?.checkpointId)
                ? status.checkpointId
                : gameplayEvent.entityId;
            var x = status?.checkpointX ?? gameplayEvent.x;
            var y = status?.checkpointY ?? gameplayEvent.y;
            service.HandleOutpostInstalled(instanceId, checkpointId, x, y);
        }

        private void OnDisable()
        {
            // Scene 종료나 Runtime 제거 시 열린 패널이 이전 상태를 유지하지 않게 한다.
            service?.ClearRuntimeStatus();
        }
    }
}

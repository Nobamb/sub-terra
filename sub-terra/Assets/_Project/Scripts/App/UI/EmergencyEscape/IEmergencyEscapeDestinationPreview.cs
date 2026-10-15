using SubTerra.Shared;
using UnityEngine;

namespace SubTerra.App.UI.EmergencyEscape
{
    /// <summary>
    /// 긴급 탈출 목적지 선택 중 미리보기 위치를 알려 준다.
    /// 플레이어 위치와 골드/전력은 바꾸지 않는다.
    /// </summary>
    public interface IEmergencyEscapeDestinationPreview
    {
        /// <summary>메인 카메라를 목적지로 옮긴다(구 방식). 터미널 창은 CCTV를 쓰므로 호출하지 않는다.</summary>
        bool TryPreviewDestination(
            EmergencyEscapeDestination kind,
            string outpostInstanceId,
            out string reason);

        void ClearDestinationPreview();

        /// <summary>CCTV가 비출 목적지의 월드 좌표. 카메라·플레이어는 건드리지 않는다.</summary>
        bool TryGetDestinationPosition(
            EmergencyEscapeDestination kind,
            string outpostInstanceId,
            out Vector2 position);
    }
}

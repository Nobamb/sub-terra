using SubTerra.Shared;

namespace SubTerra.App.UI.EmergencyEscape
{
    /// <summary>
    /// 긴급 탈출 목적지 선택 중 카메라만 옮긴다.
    /// 플레이어 위치와 골드/전력은 바꾸지 않는다.
    /// </summary>
    public interface IEmergencyEscapeDestinationPreview
    {
        bool TryPreviewDestination(
            EmergencyEscapeDestination kind,
            string outpostInstanceId,
            out string reason);

        void ClearDestinationPreview();
    }
}

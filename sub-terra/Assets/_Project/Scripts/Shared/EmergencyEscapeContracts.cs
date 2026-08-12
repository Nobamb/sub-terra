using System.Collections.Generic;

namespace SubTerra.Shared
{
    public enum EmergencyEscapeDestination
    {
        Elevator = 0,
        OutpostCore = 1
    }

    /// <summary>긴급 탈출 드롭다운에 표시할 목적지 한 줄.</summary>
    public readonly struct EmergencyEscapeDestinationOption
    {
        public EmergencyEscapeDestination Kind { get; }
        public string InstanceId { get; }
        public string DisplayName { get; }

        public EmergencyEscapeDestinationOption(
            EmergencyEscapeDestination kind,
            string instanceId,
            string displayName)
        {
            Kind = kind;
            InstanceId = instanceId ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
        }
    }

    /// <summary>Gameplay 포탈이 App의 상태·결제·UI 구현을 직접 참조하지 않기 위한 경계.</summary>
    public interface IEmergencyEscapePortalPort
    {
        /// <summary>포탈 탑승 중 E 입력 시 목적지 선택 패널을 연다.</summary>
        bool TryOpenEscapePanel(out string reason);

        /// <summary>엘리베이터와 설치된 전진기지 코어 목록. 엘리베이터를 항상 첫 항목으로 둔다.</summary>
        IReadOnlyList<EmergencyEscapeDestinationOption> GetDestinationOptions();

        /// <summary>선택한 목적지로 비용 지불 후 이동한다.</summary>
        bool TryEscapeTo(
            EmergencyEscapeDestination kind,
            string outpostInstanceId,
            out string reason);
    }
}

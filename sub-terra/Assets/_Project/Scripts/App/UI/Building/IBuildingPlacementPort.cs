using System;
using SubTerra.Shared;

namespace SubTerra.App.UI.Building
{
    public interface IBuildingFacilityMovePort
    {
        event Action<string> MoveStatusChanged;
        bool IsMoveMode { get; }
        void BeginMoveSelection();

        /// <summary>B-181: 이동을 취소한다. playSfx가 true이고 시설을 들고 있었을 때만 취소음을 낸다.</summary>
        void CancelMove(bool playSfx);
    }

    /// <summary>
    /// B의 메뉴가 A의 Preview 수명과 확정 결과만 다루기 위한 포트.
    /// 실제 위치·구조·가스 계산은 A 구현이 수행한다.
    /// </summary>
    public interface IBuildingPlacementPort
    {
        event Action<BuildingPlacementResultDto> PlacementChanged;

        bool BeginPreview(string buildingId);
        void CancelPreview();
    }
}

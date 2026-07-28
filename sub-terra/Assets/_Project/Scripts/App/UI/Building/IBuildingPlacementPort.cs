using System;
using SubTerra.Shared;

namespace SubTerra.App.UI.Building
{
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

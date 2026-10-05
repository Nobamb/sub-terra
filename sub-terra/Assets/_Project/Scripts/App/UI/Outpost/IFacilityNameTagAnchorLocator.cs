using UnityEngine;

namespace SubTerra.App.UI.Outpost
{
    /// <summary>
    /// 시설 인스턴스 ID로 이름표를 올릴 월드 위치(시설 윗면 중앙 바로 위)를 찾는다.
    /// IFacilityWorldLocator 구현이 함께 제공하면 CCTV가 이름표를 표시한다. 표시 전용이다.
    /// </summary>
    public interface IFacilityNameTagAnchorLocator
    {
        bool TryGetNameTagAnchor(string instanceId, out Vector2 anchor);
    }
}

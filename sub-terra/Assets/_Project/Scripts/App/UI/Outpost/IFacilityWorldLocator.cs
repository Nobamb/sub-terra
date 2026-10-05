using UnityEngine;

namespace SubTerra.App.UI.Outpost
{
    /// <summary>
    /// 시설 인스턴스 ID로 실제 월드 위치를 찾는다. 구현은 Gameplay를 아는 Integration 쪽에 둔다.
    /// CCTV 관찰 전용이며 접근·사용 판정에는 쓰지 않는다.
    /// </summary>
    public interface IFacilityWorldLocator
    {
        bool TryGetWorldCenter(string instanceId, out Vector2 center);
    }
}

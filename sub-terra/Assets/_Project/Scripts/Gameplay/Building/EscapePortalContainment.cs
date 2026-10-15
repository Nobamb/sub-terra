using UnityEngine;

namespace SubTerra.Gameplay.Building
{
    /// <summary>
    /// 긴급 탈출 포탈 상호작용 판정. 캐릭터 판정 영역이 포탈 영역 안에 완전히 들어갈 때만 포탈이 대상이 된다.
    /// 순수 계산이라 EditMode에서 경계 조건을 그대로 검사할 수 있다.
    /// </summary>
    public static class EscapePortalContainment
    {
        /// <summary>부동소수 오차로 딱 맞닿은 경계가 밖으로 판정되지 않게 하는 허용치.</summary>
        public const float Tolerance = 0.001f;

        /// <summary>inner의 네 변이 모두 zone 안(경계 포함)에 있으면 true. 한 변이라도 걸치면 false.</summary>
        public static bool IsFullyInside(Bounds inner, Bounds zone)
        {
            return inner.min.x >= zone.min.x - Tolerance
                && inner.max.x <= zone.max.x + Tolerance
                && inner.min.y >= zone.min.y - Tolerance
                && inner.max.y <= zone.max.y + Tolerance;
        }
    }
}

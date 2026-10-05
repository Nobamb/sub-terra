using UnityEngine;

namespace SubTerra.App.UI.FacilityNameTag
{
    /// <summary>
    /// 일반 화면 이름표는 이 레이어에 둔다. CCTV 미리보기 카메라는 이 레이어를 그리지 않아,
    /// 플레이어 접근으로 뜬 이름표가 CCTV 영상에 섞이지 않는다.
    /// 프로젝트 설정(TagManager)을 건드리지 않으려고 쓰이지 않는 기본 레이어(Water)를 쓴다.
    /// </summary>
    public static class FacilityNameTagLayers
    {
        public const int MainScreen = 4;

        public static int CctvCullingMask => ~0 & ~(1 << MainScreen);

        public static bool IsHiddenFromCctv(GameObject target)
        {
            return target != null && (CctvCullingMask & (1 << target.layer)) == 0;
        }
    }
}

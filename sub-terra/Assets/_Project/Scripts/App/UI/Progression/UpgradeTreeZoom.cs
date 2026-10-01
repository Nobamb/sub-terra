using UnityEngine;

namespace SubTerra.App.UI.Progression
{
    /// <summary>
    /// prompt-B 118-1: 업그레이드 트리 확대/축소 계산. 위로 스크롤하면 확대, 아래로 스크롤하면 축소한다.
    /// 좌표는 뷰포트 중심 기준(content.anchoredPosition)이며 EditMode에서 검증할 수 있도록 순수 계산만 둔다.
    /// </summary>
    public static class UpgradeTreeZoom
    {
        public const float MinZoom = 0.7f;
        public const float MaxZoom = 2.2f;
        public const float Step = 1.15f;

        /// <summary>스크롤 한 번(부호만 사용)에 한 단계씩 배율을 바꾼다. 장치마다 다른 스크롤 크기에 영향받지 않는다.</summary>
        public static float NextZoom(float current, float scrollY)
        {
            if (Mathf.Approximately(scrollY, 0f))
            {
                return Mathf.Clamp(current, MinZoom, MaxZoom);
            }

            var next = scrollY > 0f ? current * Step : current / Step;
            // 1배 근처에서 미세한 부동소수 오차가 쌓이지 않게 맞춘다.
            if (Mathf.Abs(next - 1f) < 0.02f)
            {
                next = 1f;
            }

            return Mathf.Clamp(next, MinZoom, MaxZoom);
        }

        /// <summary>pivot(뷰포트 중심 기준 좌표) 아래의 트리 지점이 배율 변경 후에도 같은 자리에 오도록 이동량을 구한다.</summary>
        public static Vector2 PanAround(Vector2 pan, float oldScale, float newScale, Vector2 pivot)
        {
            if (oldScale <= 0f || newScale <= 0f)
            {
                return pan;
            }

            var contentPoint = (pivot - pan) / oldScale;
            return pivot - contentPoint * newScale;
        }

        /// <summary>
        /// 트리가 뷰포트보다 클 때만 넘치는 만큼 이동을 허용한다. 작거나 같으면 가운데로 고정한다.
        /// </summary>
        public static Vector2 ClampPan(Vector2 pan, Vector2 contentSize, float scale, Vector2 viewportSize)
        {
            var halfX = Mathf.Max(0f, (contentSize.x * scale - viewportSize.x) * 0.5f);
            var halfY = Mathf.Max(0f, (contentSize.y * scale - viewportSize.y) * 0.5f);
            return new Vector2(Mathf.Clamp(pan.x, -halfX, halfX), Mathf.Clamp(pan.y, -halfY, halfY));
        }
    }
}

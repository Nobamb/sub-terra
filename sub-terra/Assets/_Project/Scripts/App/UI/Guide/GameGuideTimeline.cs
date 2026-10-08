using UnityEngine;

namespace SubTerra.App.UI.Guide
{
    /// <summary>등장·퇴장 한 순간의 연출 값. 모두 0~1 진행값이고, 위치는 캔버스 단위 오프셋이다.</summary>
    public struct GuideFrame
    {
        /// <summary>책 전체 투명도.</summary>
        public float BookAlpha;
        /// <summary>책 중심의 세로 오프셋(위가 +).</summary>
        public float BookY;
        public float BookScale;
        /// <summary>표지가 열린 정도(0 닫힘, 1 펼침).</summary>
        public float CoverOpen;
        /// <summary>넘어가는 페이지 세 장의 진행(0 오른쪽, 1 왼쪽).</summary>
        public float Flip0;
        public float Flip1;
        public float Flip2;
        /// <summary>책 크기(0)에서 창 크기(1)로의 변형 진행.</summary>
        public float Expand;
        /// <summary>가장자리 청록 빛 강도.</summary>
        public float Glow;
        /// <summary>책 페이지 면의 짙은 정도. 창 패널로 바뀌며 사라진다.</summary>
        public float PageAlpha;
        /// <summary>실제 금속 프레임·패널 레이어 투명도.</summary>
        public float PanelAlpha;
        public float Backdrop;
        public float Tabs;
        public float List;
        public float Detail;

        public float MaxContent => Mathf.Max(Tabs, Mathf.Max(List, Detail));
    }

    /// <summary>
    /// 홀로그램 책 → 가이드 창 연출 시간표(순수 계산).
    /// 첫 등장은 책이 떠올라 바운스하고 표지가 열리며 페이지 세 장이 넘어간 뒤 창으로 변형되고,
    /// 재열기는 페이지를 한 장만 넘겨 절반 길이로 끝난다. 모든 시간은 비스케일 초다.
    /// </summary>
    public static class GameGuideTimeline
    {
        public const float FullOpenDuration = 1.22f;
        public const float QuickOpenDuration = 0.60f;
        public const float CloseDuration = 0.40f;

        private struct Plan
        {
            public float RiseEnd;
            public float RiseStartY;
            public float Peak;
            public float PeakY;
            public float Settle;
            public float SettleY;
            public float CoverStart;
            public float CoverEnd;
            public float FlipStart;
            public float FlipLength;
            public float FlipStagger;
            public int FlipCount;
            public float ExpandStart;
            public float ExpandEnd;
            public float PanelStart;
            public float PanelEnd;
            public float TabsStart;
            public float ListStart;
            public float DetailStart;
            public float ContentLength;
            public float Total;
        }

        private static readonly Plan Full = new Plan
        {
            RiseEnd = 0.38f, RiseStartY = -150f, Peak = 0.20f, PeakY = 22f, Settle = 0.30f, SettleY = -7f,
            CoverStart = 0.36f, CoverEnd = 0.60f,
            FlipStart = 0.58f, FlipLength = 0.14f, FlipStagger = 0.07f, FlipCount = 3,
            ExpandStart = 0.84f, ExpandEnd = 1.04f, PanelStart = 0.96f, PanelEnd = 1.08f,
            TabsStart = 1.04f, ListStart = 1.09f, DetailStart = 1.14f, ContentLength = 0.08f,
            Total = FullOpenDuration
        };

        private static readonly Plan Quick = new Plan
        {
            RiseEnd = 0.17f, RiseStartY = -90f, Peak = 0.09f, PeakY = 9f, Settle = 0.14f, SettleY = -3f,
            CoverStart = 0.14f, CoverEnd = 0.26f,
            FlipStart = 0.24f, FlipLength = 0.10f, FlipStagger = 0.07f, FlipCount = 1,
            ExpandStart = 0.34f, ExpandEnd = 0.48f, PanelStart = 0.42f, PanelEnd = 0.50f,
            TabsStart = 0.48f, ListStart = 0.52f, DetailStart = 0.56f, ContentLength = 0.04f,
            Total = QuickOpenDuration
        };

        public static float OpenDuration(bool full) => full ? FullOpenDuration : QuickOpenDuration;

        public static GuideFrame Open(float t, bool full)
        {
            var p = full ? Full : Quick;
            t = Mathf.Clamp(t, 0f, p.Total);
            var frame = new GuideFrame();

            frame.BookAlpha = Mathf.Clamp01(t / (p.RiseEnd * 0.4f));
            frame.BookY = RiseY(t, p);
            var scaleRise = Smooth(t / p.Peak);
            frame.BookScale = t < p.Peak
                ? Mathf.Lerp(0.35f, 1.07f, scaleRise)
                : Mathf.Lerp(1.07f, 1f, Smooth((t - p.Peak) / (p.RiseEnd - p.Peak)));

            frame.CoverOpen = Smooth(Progress(t, p.CoverStart, p.CoverEnd));
            frame.Flip0 = Smooth(Progress(t, p.FlipStart, p.FlipStart + p.FlipLength));
            frame.Flip1 = p.FlipCount > 1
                ? Smooth(Progress(t, p.FlipStart + p.FlipStagger, p.FlipStart + p.FlipStagger + p.FlipLength))
                : 0f;
            frame.Flip2 = p.FlipCount > 2
                ? Smooth(Progress(t, p.FlipStart + p.FlipStagger * 2f, p.FlipStart + p.FlipStagger * 2f + p.FlipLength))
                : 0f;

            frame.Expand = EaseOut(Progress(t, p.ExpandStart, p.ExpandEnd));
            frame.Glow = Mathf.Clamp01(Progress(t, p.CoverStart, p.ExpandStart))
                * (1f - Smooth(Progress(t, p.PanelStart, p.Total)));
            frame.PanelAlpha = Smooth(Progress(t, p.PanelStart, p.PanelEnd));
            frame.PageAlpha = 1f - Smooth(Progress(t, p.PanelStart + 0.02f, p.PanelEnd + 0.04f));
            frame.Backdrop = Smooth(t / (p.RiseEnd * 0.8f));
            frame.Tabs = Smooth(Progress(t, p.TabsStart, p.TabsStart + p.ContentLength));
            frame.List = Smooth(Progress(t, p.ListStart, p.ListStart + p.ContentLength));
            frame.Detail = Smooth(Progress(t, p.DetailStart, p.DetailStart + p.ContentLength));
            return frame;
        }

        /// <summary>
        /// 닫는 연출. 내용이 먼저 사라지고 → 프레임이 펼친 책 크기로 줄어들며 → 책이 닫히고 작아져 사라진다.
        /// 시작 시점 값(from)보다 커지지 않으므로 등장 도중에 닫아도 값이 튀지 않는다. 페이지 넘김은 반복하지 않는다.
        /// </summary>
        public static GuideFrame Close(float u, GuideFrame from)
        {
            u = Mathf.Clamp(u, 0f, CloseDuration);
            var frame = new GuideFrame();

            var content = 1f - Smooth(Progress(u, 0f, 0.10f));
            frame.Detail = Mathf.Min(from.Detail, content);
            frame.List = Mathf.Min(from.List, 1f - Smooth(Progress(u, 0.01f, 0.11f)));
            frame.Tabs = Mathf.Min(from.Tabs, 1f - Smooth(Progress(u, 0.02f, 0.12f)));

            frame.Expand = Mathf.Min(from.Expand, 1f - EaseIn(Progress(u, 0.08f, 0.26f)));
            frame.PanelAlpha = Mathf.Min(from.PanelAlpha, 1f - Smooth(Progress(u, 0.06f, 0.22f)));
            // 프레임이 줄어드는 동안 책 페이지 면이 다시 드러나 같은 외곽을 이어받는다.
            frame.PageAlpha = Mathf.Max(from.PageAlpha, Smooth(Progress(u, 0.06f, 0.22f)));
            frame.CoverOpen = Mathf.Min(from.CoverOpen, 1f - Smooth(Progress(u, 0.24f, 0.34f)));
            frame.Flip0 = 0f;
            frame.Flip1 = 0f;
            frame.Flip2 = 0f;

            var shrink = Smooth(Progress(u, 0.26f, CloseDuration));
            frame.BookScale = Mathf.Min(from.BookScale, Mathf.Lerp(1f, 0.45f, shrink));
            frame.BookAlpha = Mathf.Min(from.BookAlpha, 1f - Smooth(Progress(u, 0.30f, CloseDuration)));
            frame.BookY = Mathf.Lerp(from.BookY, from.BookY - 36f, shrink);
            frame.Glow = Mathf.Max(from.Glow, 0.85f * Smooth(Progress(u, 0.10f, 0.26f)))
                * (1f - Smooth(Progress(u, 0.30f, CloseDuration)));
            frame.Backdrop = Mathf.Min(from.Backdrop, 1f - Smooth(Progress(u, 0.18f, CloseDuration)));
            return frame;
        }

        /// <summary>닫는 중 다시 열 때, 변형 진행이 현재 값 이상이 되는 가장 이른 등장 시각.</summary>
        public static float OpenTimeForExpand(float expand, bool full)
        {
            var total = OpenDuration(full);
            if (expand <= 0.001f)
            {
                return 0f;
            }

            var low = 0f;
            var high = total;
            for (var i = 0; i < 24; i++)
            {
                var mid = (low + high) * 0.5f;
                if (Open(mid, full).Expand >= expand)
                {
                    high = mid;
                }
                else
                {
                    low = mid;
                }
            }

            return high;
        }

        private static float RiseY(float t, Plan p)
        {
            if (t >= p.RiseEnd)
            {
                return 0f;
            }

            if (t < p.Peak)
            {
                return Mathf.Lerp(p.RiseStartY, p.PeakY, EaseOut(t / p.Peak));
            }

            if (t < p.Settle)
            {
                return Mathf.Lerp(p.PeakY, p.SettleY, Smooth((t - p.Peak) / (p.Settle - p.Peak)));
            }

            return Mathf.Lerp(p.SettleY, 0f, Smooth((t - p.Settle) / (p.RiseEnd - p.Settle)));
        }

        public static float Progress(float t, float start, float end)
        {
            return end <= start ? (t >= end ? 1f : 0f) : Mathf.Clamp01((t - start) / (end - start));
        }

        public static float Smooth(float x)
        {
            x = Mathf.Clamp01(x);
            return x * x * (3f - 2f * x);
        }

        public static float EaseOut(float x)
        {
            x = Mathf.Clamp01(x);
            var inv = 1f - x;
            return 1f - inv * inv * inv;
        }

        public static float EaseIn(float x)
        {
            x = Mathf.Clamp01(x);
            return x * x * x;
        }
    }
}

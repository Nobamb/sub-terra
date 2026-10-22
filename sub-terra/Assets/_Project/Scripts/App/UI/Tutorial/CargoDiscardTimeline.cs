using System;

namespace SubTerra.App.UI.Tutorial
{
    /// <summary>
    /// 화물 버리기 창(B-153) 시간표. 인스펙터에서 조정하며, 페이지 전환 전체는 MaxPageDuration을 넘지 않는다.
    /// </summary>
    [Serializable]
    public sealed class CargoDiscardTiming
    {
        public const float MaxPageDuration = 0.35f;
        public const float MaxCompleteHold = 0.8f;

        /// <summary>이전 페이지 내용이 빠지는 시간(프레임은 그대로).</summary>
        public float pageOutDuration = 0.09f;
        /// <summary>가로 스캔선이 지나가며 새 내용이 들어오는 시간.</summary>
        public float pageInDuration = 0.16f;
        public float gaugeTweenDuration = 0.2f;
        public float quantityBlinkDuration = 0.2f;
        public float clearedFlashDuration = 0.3f;
        /// <summary>공간 확보 완료를 보여 준 뒤 접기 시작까지.</summary>
        public float completeHold = 0.4f;

        public float PageDuration => pageOutDuration + pageInDuration;

        public CargoDiscardTiming Sanitized()
        {
            var copy = (CargoDiscardTiming)MemberwiseClone();
            copy.pageOutDuration = Math.Max(0.02f, copy.pageOutDuration);
            copy.pageInDuration = Math.Max(0.04f, copy.pageInDuration);
            copy.gaugeTweenDuration = Math.Max(0.01f, copy.gaugeTweenDuration);
            copy.quantityBlinkDuration = Math.Max(0.02f, copy.quantityBlinkDuration);
            copy.clearedFlashDuration = Math.Max(0.02f, copy.clearedFlashDuration);
            copy.completeHold = Math.Min(MaxCompleteHold, Math.Max(0.1f, copy.completeHold));
            var page = copy.PageDuration;
            if (page > MaxPageDuration)
            {
                var k = MaxPageDuration / page;
                copy.pageOutDuration *= k;
                copy.pageInDuration *= k;
            }

            return copy;
        }
    }

    /// <summary>
    /// 페이지 전환: 프레임·배경은 그대로 두고 안쪽 내용만 바꾼다(창이 접혔다 펴지지 않는다).
    /// 나가기는 내용이 빠지고, 들어오기는 가로 스캔선이 위에서 아래로 한 번 지나가며 새 내용을 드러낸다.
    /// 그 밖의 표시(게이지 줄어듦, 수량·완료 표시)는 한 번만 바뀌고 반복 깜빡임이 없다.
    /// </summary>
    public static class CargoDiscardTimeline
    {
        public struct PagePose
        {
            public float Content;
            /// <summary>내용이 위에서부터 드러난 비율(0~1).</summary>
            public float Reveal;
            /// <summary>스캔선 위치(0 = 위, 1 = 아래).</summary>
            public float ScanY;
            public float ScanAlpha;
            public bool Finished;
        }

        public static PagePose EvaluatePageOut(float t, CargoDiscardTiming timing)
        {
            var s = timing ?? new CargoDiscardTiming();
            var d = s.pageOutDuration;
            return new PagePose
            {
                Content = 1f - CargoFullPopupTimeline.EaseOutCubic(CargoFullPopupTimeline.Progress(t, 0f, d)),
                Reveal = 1f,
                ScanY = 0f,
                ScanAlpha = CargoFullPopupTimeline.Progress(t, d * 0.5f, d),
                Finished = t >= d
            };
        }

        public static PagePose EvaluatePageIn(float t, CargoDiscardTiming timing)
        {
            var s = timing ?? new CargoDiscardTiming();
            var d = s.pageInDuration;
            var sweep = CargoFullPopupTimeline.EaseOutCubic(CargoFullPopupTimeline.Progress(t, 0f, d));
            return new PagePose
            {
                Content = CargoFullPopupTimeline.Progress(t, 0f, d * 0.4f),
                Reveal = sweep,
                ScanY = sweep,
                ScanAlpha = 1f - CargoFullPopupTimeline.Progress(t, d * 0.75f, d),
                Finished = t >= d
            };
        }

        /// <summary>게이지가 이전 값에서 새 값으로 가는 비율(0~1).</summary>
        public static float GaugeTween(float t, CargoDiscardTiming timing)
        {
            var s = timing ?? new CargoDiscardTiming();
            return CargoFullPopupTimeline.EaseOutCubic(CargoFullPopupTimeline.Progress(t, 0f, s.gaugeTweenDuration));
        }

        /// <summary>수량 숫자가 바뀔 때 한 번 밝아졌다 돌아온다.</summary>
        public static float QuantityBlink(float t, CargoDiscardTiming timing)
        {
            var s = timing ?? new CargoDiscardTiming();
            var d = s.quantityBlinkDuration;
            return CargoFullPopupTimeline.Pulse(t, 0f, d * 0.25f, d);
        }

        /// <summary>부족이 0이 되는 순간 게이지 끝과 문구가 한 번 밝아진다.</summary>
        public static float ClearedFlash(float t, CargoDiscardTiming timing)
        {
            var s = timing ?? new CargoDiscardTiming();
            var d = s.clearedFlashDuration;
            return CargoFullPopupTimeline.Pulse(t, 0f, d * 0.2f, d);
        }
    }
}

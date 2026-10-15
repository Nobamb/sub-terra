using System;

namespace SubTerra.App.UI.PlayerStatus
{
    /// <summary>
    /// 플레이어 머리 위 문구의 시간 상수와 순수 계산. 등장·퇴장 level은 시설 이름표 시간표(FacilityNameTagTimeline)를 그대로 쓴다.
    /// 모든 시간은 unscaled 기준이라 일시정지 중에도 잔류하지 않는다.
    /// </summary>
    public static class PlayerStatusTimeline
    {
        /// <summary>문구가 떠 있는 시간(등장 완료 후). 같은 문구를 다시 받으면 이 값으로 되돌아간다.</summary>
        public const float HoldSeconds = 2.5f;
        /// <summary>위·아래 항목이 자리를 옮길 때 슬라이드 시간.</summary>
        public const float SlideSeconds = 0.18f;
        /// <summary>항목 사이 간격(디자인 픽셀).</summary>
        public const float StackGap = 6f;
        /// <summary>떠 있는 항목(골드 팝업 포함) 최대 개수. 넘치면 가장 오래된 문구부터 퇴장한다.</summary>
        public const int MaxEntries = 4;
        /// <summary>경고 깜박임 횟수. 2회는 밝기 골이 2번 생기는 것.</summary>
        public const int BlinkCount = 2;
        public const float BlinkPeriod = 0.3f;
        public const float FlashDuration = BlinkCount * BlinkPeriod;
        public const float MinFlashAlpha = 0.15f;
        /// <summary>한 프레임에 진행할 최대 시간. 시설 이름표 Tick과 같은 값이라 두 쪽 level이 어긋나지 않는다.</summary>
        public const float MaxStep = 0.05f;

        public static float ClampStep(float deltaSeconds)
        {
            return deltaSeconds < 0f ? 0f : deltaSeconds > MaxStep ? MaxStep : deltaSeconds;
        }

        /// <summary>
        /// 경고 깜박임 알파. 등장 직후 2번 어두워졌다가, FlashDuration 뒤에는 1(불투명)로 고정된다.
        /// 시작과 끝이 모두 1이라 고정될 때 튀지 않는다.
        /// </summary>
        public static float FlashAlpha(float shownFor)
        {
            if (shownFor <= 0f)
            {
                return 1f;
            }

            if (shownFor >= FlashDuration)
            {
                return 1f;
            }

            var wave = 0.5f + 0.5f * (float)Math.Cos(2.0 * Math.PI * shownFor / BlinkPeriod);
            return MinFlashAlpha + (1f - MinFlashAlpha) * wave;
        }

        /// <summary>from에서 to까지 SlideSeconds 동안 부드럽게(smoothstep) 옮겨 간 위치.</summary>
        public static float SlideOffset(float from, float to, float elapsed)
        {
            var k = elapsed <= 0f ? 0f : elapsed >= SlideSeconds ? 1f : elapsed / SlideSeconds;
            k = k * k * (3f - 2f * k);
            return from + (to - from) * k;
        }
    }
}

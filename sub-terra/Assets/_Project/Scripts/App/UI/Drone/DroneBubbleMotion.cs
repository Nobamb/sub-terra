using SubTerra.App.UI.Outpost;
using UnityEngine;

namespace SubTerra.App.UI.Drone
{
    /// <summary>
    /// B-189 드론 말풍선 연출의 시간별 값(순수 계산). MonoBehaviour 없이 EditMode에서 검사한다.
    /// 첫 등장(옛 TV 켜짐): 밝은 가로선이 가운데에서 좌우로 뻗고(살짝 넘침), 그 선이 위아래로 열려 화면이 된다.
    /// 동시에 살짝 아래에서 올라온다. 섬광이 빠지며 어두운 화면이 드러나고, 한 번 깜빡인 뒤 주사선이 한 번 훑는다.
    /// 펼친 뒤 글자를 찍는다. 순서는 전진기지 코어 CCTV 팝업 켜짐(CoreCctvTimeline.Window)과 같고 시간만 짧다.
    /// 대사 변경: 펼침 없이 글자를 지우고 다시 찍으며, 높이는 부드럽게 바뀐다.
    /// 타이핑은 전진기지 코어 CCTV 터미널(CoreCctvTimeline)과 같은 줄 단위 속도를 쓴다.
    /// </summary>
    public static class DroneBubbleMotion
    {
        // B-189 ★ PM이 숫자만 바꿔 본다.
        public const float AppearDuration = 0.22f;
        public const float AppearRisePixels = 8f;
        public const float AppearOvershoot = 1.05f;
        public const float ResizeDuration = 0.16f;

        // B-189 TV 켜짐 ★
        /// <summary>가로선이 좌우로 뻗는 시간. 이후 AppearDuration까지 위아래로 열린다.</summary>
        public const float LineDuration = 0.08f;
        /// <summary>가로선 두께(로컬 단위).</summary>
        public const float LineThickness = 3f;
        /// <summary>가로선 단계의 섬광 세기(0~1). 열리면서 FlashFade 동안 빠진다.</summary>
        public const float FlashPeak = 0.85f;
        public const float FlashFade = 0.12f;
        /// <summary>다 열린 직후 한 번 깜빡이는 시점·길이·세기.</summary>
        public const float FlickerAt = 0.26f;
        public const float FlickerWidth = 0.035f;
        public const float FlickerPeak = 0.2f;
        /// <summary>다 열린 뒤 주사선이 위에서 아래로 한 번 훑는 시간·세기·두께.</summary>
        public const float ScanDuration = 0.24f;
        public const float ScanAlpha = 0.28f;
        public const float ScanThickness = 4f;

        /// <summary>섬광·깜빡임·주사선까지 끝나는 시점.</summary>
        public const float TvEffectEnd = AppearDuration + ScanDuration;

        /// <summary>등장 경과 시간의 가로 폭 비율. 가로선 단계에 0→넘침, 열리는 동안 1로 돌아온다.</summary>
        public static float WidthScale(float elapsed)
        {
            if (elapsed <= 0f)
            {
                return 0f;
            }

            if (elapsed >= AppearDuration)
            {
                return 1f;
            }

            if (elapsed < LineDuration)
            {
                return AppearOvershoot * EaseOut(elapsed / LineDuration);
            }

            var settle = (elapsed - LineDuration) / (AppearDuration - LineDuration);
            return Mathf.Lerp(AppearOvershoot, 1f, Smooth(settle));
        }

        /// <summary>
        /// 등장 경과 시간의 세로 비율. 가로선 단계는 LineThickness 두께, 이후 1까지 열린다.
        /// fullHeight는 다 열린 높이(로컬 단위).
        /// </summary>
        public static float HeightScale(float elapsed, float fullHeight)
        {
            if (elapsed >= AppearDuration)
            {
                return 1f;
            }

            var line = fullHeight <= LineThickness ? 1f : LineThickness / fullHeight;
            if (elapsed < LineDuration)
            {
                return line;
            }

            var open = (elapsed - LineDuration) / (AppearDuration - LineDuration);
            return Mathf.Lerp(line, 1f, EaseOut(open));
        }

        /// <summary>패널 위에 덮는 강조색 섬광 세기. 가로선 단계에 밝고, 열리며 빠진 뒤 한 번 깜빡인다.</summary>
        public static float FlashAlpha(float elapsed)
        {
            if (elapsed < 0f || elapsed >= TvEffectEnd)
            {
                return 0f;
            }

            var flash = elapsed < LineDuration
                ? FlashPeak
                : FlashPeak * (1f - Smooth((elapsed - LineDuration) / FlashFade));
            var flicker = FlickerPeak * Bell(elapsed, FlickerAt, FlickerWidth);
            return Mathf.Max(flash, flicker);
        }

        /// <summary>주사선 위치(0=위, 1=아래). 다 열린 뒤 ScanDuration 동안 내려간다.</summary>
        public static float ScanPosition(float elapsed)
        {
            return Mathf.Clamp01((elapsed - AppearDuration) / ScanDuration);
        }

        /// <summary>주사선 세기. 다 열리기 전·훑기가 끝난 뒤는 0, 양 끝에서 부드럽게 오르내린다.</summary>
        public static float ScanlineAlpha(float elapsed)
        {
            if (elapsed < AppearDuration || elapsed >= TvEffectEnd)
            {
                return 0f;
            }

            var k = ScanPosition(elapsed);
            return ScanAlpha * Mathf.Sin(k * Mathf.PI);
        }

        /// <summary>전진기지 켜짐 터미널과 같은 값: 한 줄을 찍는 시간.</summary>
        public const float TypingLineDuration = CoreCctvTimeline.TerminalLineTyping;
        /// <summary>전진기지 켜짐 터미널과 같은 값: 다음 줄이 찍히기 시작하는 간격.</summary>
        public const float TypingLineInterval = CoreCctvTimeline.TerminalLineInterval;

        /// <summary>프레임 하나에서 진행할 최대 시간. 끊김 뒤 연출이 건너뛰지 않게 한다(CCTV와 같음).</summary>
        public const float MaxStep = 0.05f;

        /// <summary>등장 경과 시간의 세로 오프셋(로컬 단위). 아래(-)에서 0으로 올라온다.</summary>
        public static float RiseOffset(float elapsed)
        {
            var t = Mathf.Clamp01(elapsed / AppearDuration);
            return -AppearRisePixels * (1f - EaseOut(t));
        }

        public static bool IsAppearDone(float elapsed)
        {
            return elapsed >= AppearDuration;
        }

        /// <summary>높이 보간. 경과 시간 0이면 from, ResizeDuration 이후면 to.</summary>
        public static float ResizeHeight(float from, float to, float elapsed)
        {
            var t = ResizeDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / ResizeDuration);
            return Mathf.Lerp(from, to, Smooth(t));
        }

        /// <summary>줄 수만큼 찍는 데 드는 전체 시간.</summary>
        public static float TypingDuration(int lineCount)
        {
            return lineCount <= 0 ? 0f : TypingLineInterval * (lineCount - 1) + TypingLineDuration;
        }

        /// <summary>
        /// 타이핑 경과 시간에 보이는 글자 수. 줄마다 TypingLineInterval 간격으로 시작해
        /// TypingLineDuration 동안 앞에서부터 찍힌다(CoreCctvPopupView.ApplyTerminal과 같은 반올림).
        /// </summary>
        public static int VisibleCharacters(float elapsed, int[] lineCharacterCounts)
        {
            if (lineCharacterCounts == null)
            {
                return 0;
            }

            var visible = 0;
            for (var i = 0; i < lineCharacterCounts.Length; i++)
            {
                var count = Mathf.Max(0, lineCharacterCounts[i]);
                var start = TypingLineInterval * i;
                var k = TypingLineDuration <= 0f ? 1f : Mathf.Clamp01((elapsed - start) / TypingLineDuration);
                visible += Mathf.RoundToInt(count * k);
            }

            return visible;
        }

        public static int TotalCharacters(int[] lineCharacterCounts)
        {
            if (lineCharacterCounts == null)
            {
                return 0;
            }

            var total = 0;
            for (var i = 0; i < lineCharacterCounts.Length; i++)
            {
                total += Mathf.Max(0, lineCharacterCounts[i]);
            }

            return total;
        }

        private static float EaseOut(float k)
        {
            k = Mathf.Clamp01(k);
            var inv = 1f - k;
            return 1f - inv * inv * inv;
        }

        private static float Smooth(float k)
        {
            k = Mathf.Clamp01(k);
            return k * k * (3f - 2f * k);
        }

        /// <summary>center에서 1, ±halfWidth 밖에서 0인 종 모양.</summary>
        private static float Bell(float t, float center, float halfWidth)
        {
            if (halfWidth <= 0f)
            {
                return 0f;
            }

            var d = Mathf.Abs(t - center) / halfWidth;
            return d >= 1f ? 0f : Smooth(1f - d);
        }
    }
}

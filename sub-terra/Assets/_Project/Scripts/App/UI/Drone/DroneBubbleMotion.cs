using SubTerra.App.UI.Outpost;
using UnityEngine;

namespace SubTerra.App.UI.Drone
{
    /// <summary>
    /// B-189 드론 말풍선 연출의 시간별 값(순수 계산). MonoBehaviour 없이 EditMode에서 검사한다.
    /// 첫 등장: 살짝 아래에서 올라오며 가로 0→살짝 넘침→100%로 펼쳐진다. 펼친 뒤 글자를 찍는다.
    /// 대사 변경: 펼침 없이 글자를 지우고 다시 찍으며, 높이는 부드럽게 바뀐다.
    /// 타이핑은 전진기지 코어 CCTV 터미널(CoreCctvTimeline)과 같은 줄 단위 속도를 쓴다.
    /// </summary>
    public static class DroneBubbleMotion
    {
        // B-189 ★ PM이 숫자만 바꿔 본다.
        public const float AppearDuration = 0.22f;
        public const float AppearRisePixels = 8f;
        public const float AppearOvershoot = 1.05f;
        /// <summary>펼침 시간 중 가로 폭이 넘침 최대에 닿는 지점(0~1).</summary>
        public const float AppearOvershootAt = 0.72f;
        public const float ResizeDuration = 0.16f;

        /// <summary>전진기지 켜짐 터미널과 같은 값: 한 줄을 찍는 시간.</summary>
        public const float TypingLineDuration = CoreCctvTimeline.TerminalLineTyping;
        /// <summary>전진기지 켜짐 터미널과 같은 값: 다음 줄이 찍히기 시작하는 간격.</summary>
        public const float TypingLineInterval = CoreCctvTimeline.TerminalLineInterval;

        /// <summary>프레임 하나에서 진행할 최대 시간. 끊김 뒤 연출이 건너뛰지 않게 한다(CCTV와 같음).</summary>
        public const float MaxStep = 0.05f;

        /// <summary>등장 경과 시간의 가로 폭 비율. 0에서 시작해 넘침 뒤 1로 돌아온다.</summary>
        public static float WidthScale(float elapsed)
        {
            if (elapsed <= 0f)
            {
                return 0f;
            }

            var t = Mathf.Clamp01(elapsed / AppearDuration);
            if (t >= 1f)
            {
                return 1f;
            }

            if (t < AppearOvershootAt)
            {
                return AppearOvershoot * EaseOut(t / AppearOvershootAt);
            }

            var settle = (t - AppearOvershootAt) / (1f - AppearOvershootAt);
            return Mathf.Lerp(AppearOvershoot, 1f, Smooth(settle));
        }

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
    }
}

using UnityEngine;

namespace SubTerra.App.UI.Outpost
{
    /// <summary>
    /// 보관함 팝업 Showbox 연출 시간표(순수 계산). View는 시간만 넘기고 값은 여기서 받는다.
    /// 등장 1.1초: 닫힌 상자가 화면 아래에서 가운데로 감속하며 올라옴 → 뚜껑 두 장이 열리며 짧은 청록 발광
    ///   → 광물 아이콘이 하나씩 튀어나와 최대 크기에 이르면 목록의 자원 아이콘으로 날아가 작아지며 안착
    ///   (상자 입구에서 패널이 커지며 정착, 이때부터 입력 가능).
    /// 종료 0.45초: 패널이 상자로 줄어 들어감 → 광물이 역순으로 다시 담김 → 뚜껑 닫힘 → 상자가 아래로 가속하며 사라짐.
    /// 감속은 반동 없는 ease만 쓴다(바운스·반복 출렁임 없음). 광물 연출은 표시 전용이며 수치를 바꾸지 않는다.
    /// </summary>
    public static class StorageBoxTimeline
    {
        public const float OpenDuration = 1.10f;
        public const float CloseDuration = 0.45f;

        // 상자 상승(캔버스 기준 화면 아래 → 가운데)
        public const float BoxStartY = -760f;
        public const float BoxRiseEnd = 0.34f;
        public const float BoxFadeIn = 0.06f;
        public const float BoxFadeStart = 0.74f;
        public const float BoxFadeDuration = 0.16f;

        // 개봉
        public const float LidOpenStart = 0.36f;
        public const float LidOpenDuration = 0.18f;
        public const float GlowStart = 0.38f;
        public const float GlowDuration = 0.36f;

        // 광물 팝
        public const int MaxPops = 5;
        public const float PopStart = 0.44f;
        public const float PopStagger = 0.04f;
        public const float PopDuration = 0.20f;
        public const float PopStartScale = 0.35f;

        // 정점에 이른 광물이 목록의 자원 아이콘으로 날아가 작아지며 안착한다.
        public const float FlyDuration = 0.24f;
        public const float LandFadeDuration = 0.05f;

        // 패널 정착
        public const float PanelStart = 0.70f;
        public const float PanelDuration = 0.30f;
        public const float PanelMinScale = 0.32f;
        public const float HeaderReveal = 0.84f;
        public const float ListReveal = 0.88f;
        public const float FooterReveal = 0.92f;
        public const float RevealDuration = 0.12f;

        // 종료
        public const float CloseContentFade = 0.08f;
        public const float PanelShrinkDuration = 0.16f;
        public const float BoxAppearDuration = 0.08f;
        public const float ReturnStart = 0.04f;
        public const float ReturnStagger = 0.03f;
        public const float ReturnDuration = 0.12f;
        public const float LidCloseStart = 0.24f;
        public const float LidCloseDuration = 0.08f;
        public const float DescentStart = 0.30f;
        public const float BoxVanishStart = 0.40f;

        public static float EaseOutCubic(float x)
        {
            x = Mathf.Clamp01(x);
            var k = 1f - x;
            return 1f - k * k * k;
        }

        public static float EaseInCubic(float x)
        {
            x = Mathf.Clamp01(x);
            return x * x * x;
        }

        /// <summary>start에서 시작해 duration 동안 한 번 올라갔다 내려가는 0~1 값.</summary>
        public static float Pulse(float t, float start, float duration)
        {
            var x = (t - start) / duration;
            if (x <= 0f || x >= 1f)
            {
                return 0f;
            }

            return Mathf.Sin(Mathf.PI * x);
        }

        // ---------- 등장 ----------

        public static float Backdrop(float t)
        {
            return EaseOutCubic(t / 0.3f);
        }

        /// <summary>상자 상승 진행(0 화면 아래 → 1 가운데). 초반 빠르고 가운데 근처에서 감속한다.</summary>
        public static float BoxRise(float t)
        {
            return EaseOutCubic(t / BoxRiseEnd);
        }

        public static float BoxY(float rise)
        {
            return Mathf.Lerp(BoxStartY, 0f, Mathf.Clamp01(rise));
        }

        /// <summary>패널이 덮을 무렵 상자는 살짝 작아지며 사라진다.</summary>
        public static float OpenBoxAlpha(float t)
        {
            return Mathf.Clamp01(t / BoxFadeIn) * (1f - BoxFadeOut(t));
        }

        public static float OpenBoxScale(float t)
        {
            return 1f - 0.15f * BoxFadeOut(t);
        }

        private static float BoxFadeOut(float t)
        {
            return Mathf.Clamp01((t - BoxFadeStart) / BoxFadeDuration);
        }

        /// <summary>뚜껑 열린 정도(0 닫힘 → 1 활짝).</summary>
        public static float OpenLid(float t)
        {
            return EaseOutCubic((t - LidOpenStart) / LidOpenDuration);
        }

        /// <summary>입구의 짧은 청록 발광(0~1). 상자 주변만 밝히고 화면 전체를 덮지 않는다.</summary>
        public static float MouthGlow(float t)
        {
            return Pulse(t, GlowStart, GlowDuration);
        }

        public static int PopCount(int kinds)
        {
            return Mathf.Clamp(kinds, 0, MaxPops);
        }

        public static float PopStartTime(int index)
        {
            return PopStart + index * PopStagger;
        }

        /// <summary>광물 i가 입구에서 정점까지 튀어나온 정도(0 입구 → 1 정점).</summary>
        public static float PopProgress(float t, int index)
        {
            return EaseOutCubic((t - PopStartTime(index)) / PopDuration);
        }

        /// <summary>광물 i가 정점에서 목록 아이콘으로 날아가기 시작하는 시각(정점에 닿는 즉시).</summary>
        public static float FlyStartTime(int index)
        {
            return PopStartTime(index) + PopDuration;
        }

        public static float FlyEndTime(int index)
        {
            return FlyStartTime(index) + FlyDuration;
        }

        /// <summary>정점에서 목록 아이콘까지 날아간 정도(0 정점 → 1 안착). 출발·도착 모두 부드럽게 감속한다.</summary>
        public static float FlyProgress(float t, int index)
        {
            var x = Mathf.Clamp01((t - FlyStartTime(index)) / FlyDuration);
            return x * x * (3f - 2f * x);
        }

        /// <summary>튀어나올 때 PopStartScale → 1(최대 크기), 날아가며 1 → landScale(목록 아이콘 크기).</summary>
        public static float PopScale(float t, int index, float landScale)
        {
            var grow = Mathf.Lerp(PopStartScale, 1f, PopProgress(t, index));
            return Mathf.Lerp(grow, landScale, FlyProgress(t, index));
        }

        /// <summary>안착하기 전까지 보이고, 목록 아이콘이 이어받는 짧은 순간에 사라진다.</summary>
        public static float PopAlpha(float t, int index)
        {
            var start = PopStartTime(index);
            if (t < start)
            {
                return 0f;
            }

            var fadeIn = Mathf.Clamp01((t - start) / 0.05f);
            var fadeOut = Mathf.Clamp01((t - FlyEndTime(index)) / LandFadeDuration);
            return fadeIn * (1f - fadeOut);
        }

        /// <summary>광물 i의 정점(입구 기준). 위쪽 부채꼴로 고르게 퍼진다.</summary>
        public static Vector2 PopApex(int index, int count)
        {
            var spread = count <= 1 ? 0f : Mathf.Lerp(-55f, 55f, index / (float)(count - 1));
            var radians = spread * Mathf.Deg2Rad;
            var distance = 170f + (index % 2) * 34f;
            return new Vector2(Mathf.Sin(radians), Mathf.Cos(radians)) * distance;
        }

        /// <summary>입구에서 정점까지 위로 볼록한 경로. 끝에서 위로 살짝 떠오른다.</summary>
        public static Vector2 PopPosition(float progress, Vector2 apex)
        {
            var p = Mathf.Clamp01(progress);
            var arc = 40f * Mathf.Sin(Mathf.PI * p);
            return apex * p + new Vector2(0f, arc);
        }

        /// <summary>패널이 입구에서 커져 정착한 정도(0 없음 → 1 정착).</summary>
        public static float PanelPresence(float t)
        {
            return EaseOutCubic((t - PanelStart) / PanelDuration);
        }

        public static float PanelScale(float presence)
        {
            return Mathf.Lerp(PanelMinScale, 1f, Mathf.Clamp01(presence));
        }

        public static float PanelAlpha(float presence)
        {
            return Mathf.Clamp01(presence * 1.8f);
        }

        public static float Reveal(float t, float start)
        {
            return EaseOutCubic((t - start) / RevealDuration);
        }

        /// <summary>조작 영역이 드러난 뒤에만 입력을 받는다.</summary>
        public static bool IsInteractive(float t)
        {
            return t >= FooterReveal;
        }

        // ---------- 종료 ----------
        // from: 닫기 요청 시점의 등장 시간(완전히 열렸으면 OpenDuration). 등장 도중 닫아도 그 상태에서 역순으로 이어진다.

        /// <summary>등장 중 광물이 이미 튀어나오기 시작한 개수.</summary>
        public static int PoppedCount(float from, int count)
        {
            var popped = 0;
            for (var i = 0; i < count; i++)
            {
                if (from >= PopStartTime(i))
                {
                    popped++;
                }
            }

            return popped;
        }

        /// <summary>
        /// 종료 시작 시각. 패널·광물이 아직 없으면 담는 단계를 건너뛰어 바로 뚜껑 닫기(또는 하강)부터 시작한다.
        /// </summary>
        public static float CloseStartOffset(float from, int count)
        {
            if (PanelPresence(from) > 0f || PoppedCount(from, count) > 0)
            {
                return 0f;
            }

            return OpenLid(from) > 0f ? LidCloseStart : DescentStart;
        }

        public static float CloseContentAlpha(float t)
        {
            return 1f - Mathf.Clamp01(t / CloseContentFade);
        }

        public static float CloseBackdrop(float t)
        {
            return 1f - Mathf.Clamp01((t - 0.1f) / (CloseDuration - 0.1f));
        }

        /// <summary>패널이 상자 입구로 빨려 들어가는 정도를 반영한 남은 존재감.</summary>
        public static float ClosePanel(float t, float from)
        {
            return PanelPresence(from) * (1f - EaseInCubic(t / PanelShrinkDuration));
        }

        public static float CloseBoxAlpha(float t, float from)
        {
            var appear = Mathf.Max(OpenBoxAlpha(from), Mathf.Clamp01(t / BoxAppearDuration));
            return appear * (1f - Mathf.Clamp01((t - BoxVanishStart) / (CloseDuration - BoxVanishStart)));
        }

        public static float CloseLid(float t, float from)
        {
            return OpenLid(from) * (1f - EaseInCubic((t - LidCloseStart) / LidCloseDuration));
        }

        /// <summary>닫힌 상자가 아래로 가속하며 내려간다(ease-in).</summary>
        public static float CloseBoxY(float t, float from)
        {
            var start = BoxY(BoxRise(from));
            return Mathf.Lerp(start, BoxStartY, EaseInCubic((t - DescentStart) / (CloseDuration - DescentStart)));
        }

        /// <summary>역순 스태거: 마지막에 튀어나온 광물이 먼저 담긴다.</summary>
        public static float ReturnStartTime(int index, int popped)
        {
            return ReturnStart + (popped - 1 - index) * ReturnStagger;
        }

        /// <summary>광물 i가 정점에서 입구로 떨어진 정도(0 정점 → 1 입구).</summary>
        public static float ReturnProgress(float t, int index, int popped)
        {
            return EaseInCubic((t - ReturnStartTime(index, popped)) / ReturnDuration);
        }

        public static float ReturnAlpha(float t, int index, int popped)
        {
            if (index >= popped)
            {
                return 0f;
            }

            var start = ReturnStartTime(index, popped);
            if (t >= start + ReturnDuration)
            {
                return 0f;
            }

            return Mathf.Clamp01((t - (start - 0.04f)) / 0.04f);
        }
    }
}

using UnityEngine;

namespace SubTerra.App.UI.Sell
{
    /// <summary>
    /// 판매 창 연출 시간표(순수 계산). View는 시간만 넘기고 값은 여기서 받는다.
    /// 등장 1.15초: 도는 금화가 하나씩 쌓임 → 닫힌 창(양쪽 끝이 맞닿은 상태)이 멀리서 다가와 금화와 부딪힘
    ///   → 금화가 터져 사라짐 → 양쪽 끝이 좌우로 벌어지며 가운데부터 홀로그램 내부가 퍼져 나타남 → 내용 표시.
    /// 종료 0.36초: 내용이 먼저 흐려지고 양쪽 끝이 다시 맞닿으며 뒤로 물러나 사라진다.
    /// 감속은 반동 없는 ease만 쓴다(과한 출렁임·반복 바운스 없음).
    /// </summary>
    public static class ResourceSellTimeline
    {
        public const float OpenDuration = 1.15f;
        public const float CloseDuration = 0.36f;
        public const float SaleDuration = 0.8f;

        public const float FarScale = 0.5f;
        public const float FarBrightness = 0.25f;

        // 금화 쌓기
        public const int CoinCount = 4;
        public const float CoinStagger = 0.08f;
        public const float CoinDrop = 0.15f;
        public const float CoinSpinTurnsPerSecond = 1.9f;

        // 닫힌 창 접근·충돌
        public const float ApproachStart = 0.14f;
        public const float ImpactTime = 0.56f;
        public const float ImpactWindup = 0.08f;

        // 금화 폭발
        public const int BurstCoinCount = 12;
        public const int BurstSparkCount = 10;
        public const float BurstDuration = 0.4f;

        // 펼침
        public const float UnfoldStart = 0.6f;
        public const float UnfoldDuration = 0.45f;
        public const float HeightSpreadDuration = 0.3f;
        public const float MinInteriorHeight = 0.12f;
        public const float GlowFadeStart = 0.95f;
        public const float HologramSolidStart = 0.7f;
        public const float HologramSolidDuration = 0.42f;

        // 내용
        public const float HeaderReveal = 0.84f;
        public const float ListReveal = 0.88f;
        public const float FooterReveal = 0.92f;
        public const float RevealDuration = 0.14f;
        public const float RowStagger = 0.02f;
        public const int MaxStaggeredRows = 5;

        // 종료
        public const float CloseContentFade = 0.10f;
        public const float FoldStart = 0.04f;
        public const float FoldDuration = 0.2f;
        public const float RecedeStart = 0.14f;
        public const float FadeOutStart = 0.2f;

        // 판매 성공
        public const int MaxStreaks = 5;
        public const float StreakStagger = 0.05f;
        public const float StreakDuration = 0.32f;
        public const float CountStart = 0.28f;
        public const float CountDuration = 0.45f;

        public static float EaseOutCubic(float x)
        {
            x = Mathf.Clamp01(x);
            var k = 1f - x;
            return 1f - k * k * k;
        }

        public static float EaseOutQuart(float x)
        {
            x = Mathf.Clamp01(x);
            var k = 1f - x;
            return 1f - k * k * k * k;
        }

        public static float EaseInQuad(float x)
        {
            x = Mathf.Clamp01(x);
            return x * x;
        }

        public static float EaseInCubic(float x)
        {
            x = Mathf.Clamp01(x);
            return x * x * x;
        }

        public static float EaseInOut(float x)
        {
            x = Mathf.Clamp01(x);
            return x * x * (3f - 2f * x);
        }

        // ---------- 등장: 금화 ----------

        /// <summary>금화 i의 낙하 진행(0 위·투명 → 1 쌓인 자리).</summary>
        public static float CoinDropProgress(float t, int coin)
        {
            return EaseOutCubic((t - coin * CoinStagger) / CoinDrop);
        }

        /// <summary>금화 i가 도는 각도(라디안). 떨어지는 동안과 쌓인 뒤에도 계속 돈다.</summary>
        public static float CoinSpinAngle(float t, int coin)
        {
            return (t - coin * CoinStagger) * CoinSpinTurnsPerSecond * Mathf.PI * 2f + coin * 0.9f;
        }

        /// <summary>도는 금화의 가로 폭 비율. 옆면을 향할 때도 아주 얇게 남는다.</summary>
        public static float CoinSpinWidth(float t, int coin)
        {
            return Mathf.Lerp(0.14f, 1f, Mathf.Abs(Mathf.Cos(CoinSpinAngle(t, coin))));
        }

        /// <summary>쌓인 금화 더미가 보이는 정도. 부딪히는 순간 사라진다(터지는 금화가 이어받는다).</summary>
        public static float StackAlpha(float t)
        {
            return t < ImpactTime ? 1f : 0f;
        }

        /// <summary>부딪히기 직전 더미가 살짝 커지며 눌리는 정도(0~1).</summary>
        public static float ImpactWindupAmount(float t)
        {
            return EaseInQuad((t - (ImpactTime - ImpactWindup)) / ImpactWindup);
        }

        // ---------- 등장: 닫힌 창 접근 ----------

        /// <summary>닫힌 창이 다가오는 진행(0 멀리 → 1 충돌 지점). 부딪힐 때까지 점점 빨라진다.</summary>
        public static float Approach(float t)
        {
            return EaseInQuad((t - ApproachStart) / (ImpactTime - ApproachStart));
        }

        public static float OpenCardScale(float t)
        {
            return Mathf.Lerp(FarScale, 1f, Approach(t));
        }

        public static float OpenBrightness(float t)
        {
            return Mathf.Lerp(FarBrightness, 1f, Approach(t));
        }

        public static float OpenShellAlpha(float t)
        {
            return Mathf.Clamp01((t - ApproachStart) / 0.16f);
        }

        public static float OpenBackdrop(float t)
        {
            return EaseOutCubic(t / 0.3f);
        }

        /// <summary>충돌 순간 번쩍이는 금빛(0~1).</summary>
        public static float ImpactFlash(float t)
        {
            return Pulse(t, ImpactTime - 0.02f, 0.22f);
        }

        // ---------- 등장: 금화 폭발 ----------

        public static bool BurstActive(float t)
        {
            return t >= ImpactTime && t < ImpactTime + BurstDuration;
        }

        public static float BurstProgress(float t)
        {
            return EaseOutCubic((t - ImpactTime) / BurstDuration);
        }

        /// <summary>폭발 조각의 투명도: 처음엔 또렷하고 뒤쪽 절반에서 사라진다.</summary>
        public static float BurstAlpha(float t)
        {
            var x = Mathf.Clamp01((t - ImpactTime) / BurstDuration);
            if (x <= 0f || x >= 1f)
            {
                return 0f;
            }

            return 1f - Mathf.Clamp01((x - 0.35f) / 0.65f);
        }

        // ---------- 등장: 펼침 ----------

        /// <summary>양쪽 끝 사이 벌어진 정도(0 맞닿음 → 1 최종 폭). 반동 없이 감속한다.</summary>
        public static float Gap(float t)
        {
            return EaseOutQuart((t - UnfoldStart) / UnfoldDuration);
        }

        public static float GapLinear(float t)
        {
            return Mathf.Clamp01((t - UnfoldStart) / UnfoldDuration);
        }

        /// <summary>내부 홀로그램의 세로 펼침(가운데 가는 선 → 전체 높이).</summary>
        public static float InteriorHeight(float t)
        {
            return Mathf.Lerp(MinInteriorHeight, 1f, EaseOutCubic((t - UnfoldStart) / HeightSpreadDuration));
        }

        /// <summary>벌어지는 경계를 따라 흐르는 청록 빛. 닫힌 동안은 맞닿은 선이 은은하게 빛나고 정착하면 0.</summary>
        public static float EdgeGlow(float t)
        {
            var moving = Mathf.Sin(Mathf.PI * GapLinear(t)) * 0.95f;
            var idle = t < UnfoldStart ? 0.55f * OpenShellAlpha(t) : 0f;
            var settle = 1f - Mathf.Clamp01((t - GlowFadeStart) / (OpenDuration - GlowFadeStart));
            return Mathf.Max(moving, idle) * settle;
        }

        /// <summary>홀로그램 내부가 단단해지는 정도(0 투명한 빛 → 1 실제 패널).</summary>
        public static float HologramSolid(float t)
        {
            return EaseOutCubic((t - HologramSolidStart) / HologramSolidDuration);
        }

        /// <summary>홀로그램 줄무늬·번쩍임 세기(펼칠 때 강하고 정착하면 0).</summary>
        public static float HologramAmount(float t)
        {
            if (t < UnfoldStart)
            {
                return 0f;
            }

            return 1f - EaseInOut((t - UnfoldStart) / (OpenDuration - UnfoldStart));
        }

        public static float Reveal(float t, float start)
        {
            return EaseOutCubic((t - start) / RevealDuration);
        }

        public static float RowReveal(float t, int row)
        {
            var index = Mathf.Min(row, MaxStaggeredRows);
            return Reveal(t, ListReveal + index * RowStagger);
        }

        // ---------- 종료 ----------

        public static float CloseContentAlpha(float t)
        {
            return 1f - Mathf.Clamp01(t / CloseContentFade);
        }

        /// <summary>종료 때 양쪽 끝 사이 남은 폭(1 → 0). 다시 맞닿는다.</summary>
        public static float CloseGap(float t)
        {
            return 1f - EaseInCubic((t - FoldStart) / FoldDuration);
        }

        public static float CloseFoldLinear(float t)
        {
            return Mathf.Clamp01((t - FoldStart) / FoldDuration);
        }

        public static float CloseInteriorHeight(float t)
        {
            return Mathf.Lerp(MinInteriorHeight, 1f, CloseGap(t));
        }

        public static float CloseEdgeGlow(float t)
        {
            var moving = Mathf.Sin(Mathf.PI * CloseFoldLinear(t)) * 0.7f;
            var idle = EaseInCubic((t - FoldStart) / FoldDuration) * 0.55f;
            return Mathf.Max(moving, idle) * CloseShellAlpha(t);
        }

        public static float Recede(float t)
        {
            return EaseInCubic((t - RecedeStart) / (CloseDuration - RecedeStart));
        }

        public static float CloseCardScale(float t)
        {
            return Mathf.Lerp(1f, FarScale + 0.05f, Recede(t));
        }

        public static float CloseBrightness(float t)
        {
            return Mathf.Lerp(1f, FarBrightness, Recede(t));
        }

        public static float CloseShellAlpha(float t)
        {
            return 1f - Mathf.Clamp01((t - FadeOutStart) / (CloseDuration - FadeOutStart));
        }

        public static float CloseBackdrop(float t)
        {
            return 1f - Mathf.Clamp01((t - CloseContentFade) / (CloseDuration - CloseContentFade));
        }

        // ---------- 판매 성공 ----------

        public static float StreakProgress(float t, int index)
        {
            return EaseInOut((t - index * StreakStagger) / StreakDuration);
        }

        public static float StreakAlpha(float t, int index)
        {
            var x = (t - index * StreakStagger) / StreakDuration;
            if (x <= 0f || x >= 1f)
            {
                return 0f;
            }

            var fadeIn = Mathf.Clamp01(x / 0.15f);
            var fadeOut = Mathf.Clamp01((1f - x) / 0.3f);
            return fadeIn * fadeOut;
        }

        public static float SummaryGlow(float t)
        {
            return Pulse(t, 0.16f, 0.34f);
        }

        public static float TopGoldGlow(float t)
        {
            return Pulse(t, CountStart, 0.4f);
        }

        public static float CountProgress(float t)
        {
            return EaseOutCubic((t - CountStart) / CountDuration);
        }

        public static float RowFlash(float t)
        {
            return 1f - Mathf.Clamp01(t / 0.3f);
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
    }
}

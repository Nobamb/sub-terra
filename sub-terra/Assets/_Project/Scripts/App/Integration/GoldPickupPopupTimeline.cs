using System;
using SubTerra.App.UI.FacilityNameTag;
using SubTerra.App.UI.Sell;
using UnityEngine;

namespace SubTerra.App.Integration
{
    /// <summary>한 줄(기본/추가)의 한 시점 모션. 정착 뒤에는 오프셋 0, 스케일 1로 정확히 돌아온다.</summary>
    public readonly struct GoldPickupLineMotion
    {
        public float OffsetY { get; }
        public float ScaleX { get; }
        public float ScaleY { get; }
        /// <summary>모션블러 세기 0~1. 0이면 잔상은 꺼진다.</summary>
        public float Blur { get; }
        /// <summary>이동 속도 비율 0~1. 잔상 간격·알파가 이 값에 비례한다.</summary>
        public float Speed { get; }
        /// <summary>잔상이 꼬리처럼 남는 방향. 내려올 때 +1(위쪽), 올라갈 때 -1(아래쪽).</summary>
        public float GhostDir { get; }

        public GoldPickupLineMotion(float offsetY, float scaleX, float scaleY, float blur, float speed, float ghostDir = 1f)
        {
            OffsetY = offsetY;
            ScaleX = scaleX;
            ScaleY = scaleY;
            Blur = blur;
            Speed = speed;
            GhostDir = ghostDir;
        }
    }

    /// <summary>
    /// 골드 획득 팝업 시간표(순수 계산). 홀로그램 등장 → 레버 당김·복귀 → 기본·추가 낙하 → 퇴장(등장의 역순)과
    /// 금화 궤적·수량을 맡고, View는 시간을 넘겨 값만 받아 간다.
    /// 등장 기준 1.65초(추가 포함 1.80초). 단위: 시간은 초, 길이는 디자인 px(월드 캔버스 1px = 0.01).
    /// </summary>
    public static class GoldPickupPopupTimeline
    {
        // 레이아웃: 프레임은 본문 폭만 쓰고 레버는 프레임 바깥 오른쪽에 매단다.
        public const float FrameWidth = 200f;
        public const float BaseHeight = 44f;
        public const float BonusRowHeight = 28f;
        public const float FullHeight = BaseHeight + BonusRowHeight;
        public const float EdgeThickness = 1.5f;
        public const float ExpandSeconds = 0.1f;

        // 홀로그램 등장: 아래에서 떠오르며 모서리 빛 → 좌우 펼침
        public const float AppearSeconds = 0.34f;
        public const float SlideRisePx = 22f;
        private const float FlickerEnd = 0.2f;
        private const float FlickerStep = 0.03f;

        // 레버: 프레임이 다 뜬 뒤 당김 → 곧바로 복귀(짧은 탄성 정착 한 번)
        public const float LeverDownStart = 0.36f;
        public const float LeverDownEnd = 0.5f;
        public const float LeverReturnEnd = 0.7f;
        public const float LeverPullDegrees = 140f;
        private const float LeverOvershoot = 0.8f;

        // 낙하·착지: 레버가 올라오기 시작할 때 기본, 그 뒤 추가
        public const float BaseFallStart = 0.5f;
        public const float BonusFallStart = 0.66f;
        public const float FallSeconds = 0.2f;
        public const float BlurClearSeconds = 0.06f;
        public const float SettleSeconds = 0.08f;
        public const float StretchY = 0.34f;
        public const float ShrinkX = 0.07f;
        public const float MainOvershootPx = 4f;
        public const float BonusOvershootPx = 3f;
        public const int MainGhostCount = 4;
        public const int BonusGhostCount = 3;
        public const float MainGhostGap = 11f;
        public const float BonusGhostGap = 7f;
        // 합치기로 추가 줄이 처음 열릴 때 높이 확장이 어느 정도 진행된 뒤 낙하를 시작한다.
        public const float MergeBonusFallDelay = 0.05f;
        private static readonly float[] GhostAlphas = { 0.5f, 0.34f, 0.2f, 0.1f };
        public const float KickSeconds = 0.14f;
        public const float MainKickPx = 2.5f;
        public const float BonusKickPx = 1.8f;

        // 퇴장(등장의 역순): 추가 글자 → 기본 글자가 위로 빠지고 → 프레임이 접히며 가라앉는다.
        public const float BaseOnlyExitStart = 1.15f;
        public const float BonusExitStart = 1.3f;
        public const float ExitSeconds = 0.5f;
        public const float MergeHoldSeconds = 0.45f;
        public const float RollupSeconds = 0.22f;
        public const float RestoreSeconds = 0.18f;
        private const float ExitBonusTextEnd = 0.3f;
        private const float ExitBaseTextStart = 0.12f;
        private const float ExitBaseTextEnd = 0.42f;
        private const float ExitFoldStart = 0.4f;

        // 금화·훑기
        public const int MaxLiveCoins = 24;
        public const float CoinLifetime = 0.55f;
        public const float CoinFadePortion = 0.35f;
        public const float CoinGravity = 900f;
        public const float CoinPopSeconds = 0.07f;
        public const float SweepSeconds = 0.2f;
        public const float EdgeFlashSeconds = 0.14f;
        private const float FanHalfDegrees = 38f;
        private static readonly float[] AngleJitter = { -4f, 2f, -1f, 5f, -3f };

        // ---------- 프레임·레버 ----------

        public static float AppearLevel(float t)
        {
            return Mathf.Clamp01(t / AppearSeconds);
        }

        /// <summary>등장 진행(0~1)과 퇴장 접힘을 합친 프레임 레벨. 퇴장은 등장 레벨을 거꾸로 되짚는다.</summary>
        public static float FrameLevel(float t, float exitProgress)
        {
            return Mathf.Min(AppearLevel(t), 1f - ExitFold(exitProgress));
        }

        /// <summary>시설 이름표 시간표를 읽기 전용으로 재사용한 프레임 값(Spread/FrameAlpha/Flare).</summary>
        public static FacilityNameTagFrame Frame(float level)
        {
            return FacilityNameTagTimeline.Evaluate(Mathf.Clamp01(level), true);
        }

        /// <summary>프레임이 최종 위치보다 아래에 있는 거리(px). 레벨 0에서 SlideRisePx, 1에서 0.</summary>
        public static float Slide(float level)
        {
            return SlideRisePx * (1f - ResourceSellTimeline.EaseOutCubic(level));
        }

        /// <summary>등장 초반 홀로그램이 켜질 때의 깜박임(0.55~1). 끝나면 1로 고정.</summary>
        public static float Flicker(float t)
        {
            if (t <= 0.04f || t >= FlickerEnd)
            {
                return 1f;
            }

            return (int)(t / FlickerStep) % 3 == 1 ? 0.55f : 1f;
        }

        /// <summary>켜지는 밝은 선이 프레임 아래에서 위로 훑는 위치(0~1)와 세기. 퇴장 때는 레벨이 거꾸로 가므로 위에서 아래로 돌아온다.</summary>
        public static float BootScanPosition(float level)
        {
            return ResourceSellTimeline.EaseOutCubic(Mathf.Clamp01((level - 0.25f) / 0.6f));
        }

        public static float BootScanAlpha(float level)
        {
            float k = Mathf.Clamp01((level - 0.25f) / 0.6f);
            return k <= 0f || k >= 1f ? 0f : Mathf.Sin(Mathf.PI * k);
        }

        public static float Height(float expand)
        {
            return BaseHeight + BonusRowHeight * Mathf.Clamp01(expand);
        }

        /// <summary>추가 줄 높이 확장(0~1). 처음부터 추가가 있으면 즉시 1, 합치기로 열리면 약 0.1초 ease-out.</summary>
        public static float ExpandProgress(float t, bool hasBonusRow, bool instant, float openedAt)
        {
            if (!hasBonusRow)
            {
                return 0f;
            }

            return instant ? 1f : ResourceSellTimeline.EaseOutCubic((t - openedAt) / ExpandSeconds);
        }

        /// <summary>레버가 프레임 오른쪽 가장자리까지 펼쳐진 뒤에 나타난다.</summary>
        public static float LeverAlpha(float level)
        {
            FacilityNameTagFrame frame = Frame(level);
            return Mathf.Clamp01((frame.Spread - 0.6f) / 0.4f) * frame.FrameAlpha;
        }

        /// <summary>손잡이를 당긴 각도(0=위로 선 휴식). 하강 후 복귀하며 한 번만 살짝 지나쳤다 정착한다.</summary>
        public static float LeverAngle(float t)
        {
            if (t <= LeverDownStart || t >= LeverReturnEnd)
            {
                return 0f;
            }

            if (t < LeverDownEnd)
            {
                return LeverPullDegrees * ResourceSellTimeline.EaseInOut((t - LeverDownStart) / (LeverDownEnd - LeverDownStart));
            }

            float u = (t - LeverDownEnd) / (LeverReturnEnd - LeverDownEnd);
            return LeverPullDegrees * (1f - EaseOutBack(u));
        }

        /// <summary>
        /// 정면에서 본 레버의 원근. 당길수록 막대는 짧아지고(cos) 손잡이는 관객 쪽으로 다가와 커진다.
        /// 90도를 넘으면 막대가 아래로 뒤집히며 손잡이는 축 아래로 내려온다.
        /// </summary>
        public static float LeverRodScale(float angleDegrees)
        {
            return Mathf.Cos(angleDegrees * Mathf.Deg2Rad);
        }

        public static float LeverBallScale(float angleDegrees)
        {
            return 1f + 0.45f * Mathf.Sin(Mathf.Clamp(Mathf.Abs(angleDegrees), 0f, 180f) * Mathf.Deg2Rad);
        }

        // ---------- 낙하·블러 ----------

        public static float LandTime(float fallStart)
        {
            return fallStart + FallSeconds;
        }

        public static float SettledTime(float fallStart)
        {
            return LandTime(fallStart) + SettleSeconds;
        }

        /// <summary>
        /// 줄 하나의 낙하 모션. startOffset은 슬롯 위쪽 바깥 시작 높이, 낙하는 ease-in, 착지 뒤 overshoot 한 번 후 복귀.
        /// </summary>
        public static GoldPickupLineMotion Line(float t, float fallStart, float startOffset, float overshootPx)
        {
            if (t < fallStart)
            {
                return new GoldPickupLineMotion(startOffset, 1f, 1f, 0f, 0f);
            }

            float land = LandTime(fallStart);
            if (t < land)
            {
                float u = (t - fallStart) / FallSeconds;
                float blur = Mathf.Clamp01(u * 2f);
                return new GoldPickupLineMotion(
                    startOffset * (1f - ResourceSellTimeline.EaseInQuad(u)),
                    1f - ShrinkX * blur,
                    1f + StretchY * blur,
                    blur,
                    u);
            }

            float since = t - land;
            if (since >= SettleSeconds)
            {
                return new GoldPickupLineMotion(0f, 1f, 1f, 0f, 0f);
            }

            float fade = 1f - Mathf.Clamp01(since / BlurClearSeconds);
            return new GoldPickupLineMotion(
                -overshootPx * Mathf.Sin(Mathf.PI * since / SettleSeconds),
                1f - ShrinkX * fade,
                1f + StretchY * fade,
                fade,
                fade);
        }

        /// <summary>
        /// 퇴장 때 줄이 슬롯 위로 빠져나가는 모션(낙하의 시간 역순). x는 이 줄의 퇴장 진행 0~1이며 0이면 낙하 모션을 그대로 쓴다.
        /// </summary>
        public static GoldPickupLineMotion LineRise(float x, float startOffset)
        {
            float u = 1f - Mathf.Clamp01(x);
            float ramp = Mathf.Clamp01(x * 6f);
            float blur = Mathf.Clamp01(u * 2f) * ramp;
            return new GoldPickupLineMotion(
                startOffset * (1f - ResourceSellTimeline.EaseInQuad(u)),
                1f - ShrinkX * blur,
                1f + StretchY * blur,
                blur,
                u * ramp,
                -1f);
        }

        /// <summary>추가 줄이 먼저, 기본 줄이 조금 뒤에 빠진다(등장 순서의 역).</summary>
        public static float TextExit(float exitProgress, bool bonus)
        {
            return bonus
                ? Mathf.Clamp01(exitProgress / ExitBonusTextEnd)
                : Mathf.Clamp01((exitProgress - ExitBaseTextStart) / (ExitBaseTextEnd - ExitBaseTextStart));
        }

        public static float ExitFold(float exitProgress)
        {
            // 퇴장 끝에서 부동소수 오차 없이 등장 시작 상태(레벨 0)로 정확히 돌아가게 한다.
            return exitProgress >= 1f ? 1f : Mathf.Clamp01((exitProgress - ExitFoldStart) / (1f - ExitFoldStart));
        }

        /// <summary>착지 충격으로 프레임이 아래로 살짝 눌렸다 돌아오는 거리(px, 양수=아래).</summary>
        public static float Kick(float sinceLand, float amplitudePx)
        {
            if (sinceLand <= 0f || sinceLand >= KickSeconds)
            {
                return 0f;
            }

            return amplitudePx * Mathf.Sin(Mathf.PI * sinceLand / KickSeconds);
        }

        /// <summary>잔상 i의 간격. 이동 속도에 비례해 벌어진다.</summary>
        public static float GhostOffset(float speed, int ghost, float lineGap)
        {
            return lineGap * Mathf.Clamp01(speed) * (ghost + 1);
        }

        /// <summary>잔상 i의 알파. 뒤로 갈수록 단계적으로 줄고 속도에 비례한다.</summary>
        public static float GhostAlpha(float speed, int ghost)
        {
            int index = Mathf.Clamp(ghost, 0, GhostAlphas.Length - 1);
            return GhostAlphas[index] * Mathf.Clamp01(speed);
        }

        // ---------- 숫자 롤업 ----------

        /// <summary>현재 표시값에서 목표까지 ease-out 롤업. 정수, 단조 증가, 목표 초과 없음, 끝은 정확히 목표.</summary>
        public static int RollValue(int from, int to, float elapsed)
        {
            if (to <= from)
            {
                return to;
            }

            float k = ResourceSellTimeline.EaseOutCubic(elapsed / RollupSeconds);
            if (k >= 1f)
            {
                return to;
            }

            long value = from + (long)Math.Floor((double)((long)to - from) * k);
            return (int)Math.Min(value, to);
        }

        // ---------- 퇴장 ----------

        public static float ExitFloor(bool bonusFromStart)
        {
            return bonusFromStart ? BonusExitStart : BaseOnlyExitStart;
        }

        // ---------- 연출 보조 ----------

        public static float SweepProgress(float sinceStart)
        {
            return Mathf.Clamp01(sinceStart / SweepSeconds);
        }

        public static float EdgeFlash(float sinceLand)
        {
            return ResourceSellTimeline.Pulse(sinceLand, 0f, EdgeFlashSeconds);
        }

        // ---------- 금화 ----------

        public static int FloorLog10(int amount)
        {
            int digits = 0;
            for (int value = Math.Max(amount, 1); value >= 10; value /= 10)
            {
                digits++;
            }

            return digits;
        }

        /// <summary>F(amount) = Clamp(3 + 2 × floor(log10(max(amount, 1))), 3, 8).</summary>
        public static int CoinsForAmount(int amount)
        {
            return Mathf.Clamp(3 + 2 * FloorLog10(amount), 3, 8);
        }

        public static int BaseLandCoins(int baseGold)
        {
            return CoinsForAmount(baseGold);
        }

        public static int BonusLandCoins(int bonusGold)
        {
            return Mathf.Clamp(CoinsForAmount(bonusGold) + 2, 4, 10);
        }

        /// <summary>합치기 분출 수. 유효 증가분이 0 이하면 0.</summary>
        public static int MergeCoins(long effectiveIncrease)
        {
            if (effectiveIncrease <= 0)
            {
                return 0;
            }

            int clamped = (int)Math.Min(effectiveIncrease, int.MaxValue);
            return Mathf.Clamp(CoinsForAmount(clamped) - 1, 2, 5);
        }

        /// <summary>인덱스 기반 결정적 부채꼴 발사. 각도는 위쪽 기준(음수=왼쪽), 속도는 px/초.</summary>
        public static void CoinLaunch(int index, int count, int salt, out float vx, out float vy, out float delay)
        {
            int safeCount = Math.Max(count, 1);
            int i = Math.Max(index, 0);
            float spread = (i + 0.5f) / safeCount * 2f - 1f;
            float angle = (spread * FanHalfDegrees + AngleJitter[(i * 3 + salt) % AngleJitter.Length]) * Mathf.Deg2Rad;
            float speed = 270f + 22f * ((i * 2 + salt) % 4);
            vx = Mathf.Sin(angle) * speed;
            vy = Mathf.Cos(angle) * speed;
            delay = 0.014f * i;
        }

        public static Vector2 CoinPosition(Vector2 origin, float vx, float vy, float t)
        {
            float local = Mathf.Max(0f, t);
            return new Vector2(origin.x + vx * local, origin.y + vy * local - 0.5f * CoinGravity * local * local);
        }

        public static float CoinAlpha(float t)
        {
            float life = Mathf.Clamp01(t / CoinLifetime);
            float fadeStart = 1f - CoinFadePortion;
            return life <= fadeStart ? 1f : Mathf.Clamp01((1f - life) / CoinFadePortion);
        }

        /// <summary>튀어나올 때 작게 시작해 살짝 커졌다 1로 돌아오는 크기 배율.</summary>
        public static float CoinPopScale(float t)
        {
            float k = Mathf.Clamp01(t / CoinPopSeconds);
            return Mathf.Lerp(0.3f, 1f, ResourceSellTimeline.EaseOutCubic(k)) * (1f + 0.25f * Mathf.Sin(Mathf.PI * k));
        }

        private static float EaseOutBack(float u)
        {
            u = Mathf.Clamp01(u);
            float k = u - 1f;
            return 1f + (LeverOvershoot + 1f) * k * k * k + LeverOvershoot * k * k;
        }
    }
}

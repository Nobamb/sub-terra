using System.Globalization;
using UnityEngine;

namespace SubTerra.App.Integration
{
    /// <summary>
    /// 골드 획득 연출의 문구·색·HUD 값 (prompt-B 114, 142).
    /// 팝업 모양과 타이밍은 GoldPickupPopupTimeline이 맡고, 여기는 문구 형식·팔레트·
    /// 금빛 가루·HUD 입자 비행·Pulse 값만 둔다. 모든 값은 시각 전용이며 골드 지급과 무관하다.
    /// </summary>
    public static class GoldPickupPresentation
    {
        public const float CoinFadeInSeconds = 0.05f;
        public const float CoinFadeOutPortion = 0.35f;

        public const int DustCount = 10;
        public const float DustLifetimeMax = 0.55f;
        public const float DustCleanupSeconds = 0.8f;

        // 머리와 팝업 사이 여백.
        public const float TextBaseLift = 0.12f;

        public const int HudParticleCount = 3;
        public const float HudFlightSeconds = 0.42f;
        public const float HudStagger = 0.06f;
        public const float HudParticlePixelSize = 22f;
        public const float HudCurvePixels = 70f;
        public const float PulseSeconds = 0.2f;
        public const float PulsePeak = 1.08f;

        // Gold HUD 아이콘 금화와 같은 계열. 위는 밝은 금, 아래는 주황빛 금.
        public static readonly Color MainTopColor = new Color32(0xFF, 0xE0, 0x7A, 0xFF);
        public static readonly Color MainBottomColor = new Color32(0xF2, 0xA6, 0x2B, 0xFF);
        public static readonly Color BonusColor = new Color32(0xFF, 0xB3, 0x3D, 0xFF);
        public static readonly Color ShadowColor = new Color32(0x2A, 0x16, 0x04, 0xB4);
        public static readonly Color DustBrightColor = new Color32(0xFF, 0xE6, 0x8C, 0xFF);
        public static readonly Color DustDeepColor = new Color32(0xE8, 0x93, 0x1E, 0xFF);

        // 팝업 프레임: 어두운 갈색 반투명 배경 + 금색 얇은 테두리 (청록 사용 금지).
        public static readonly Color FrameFillColor = new Color(0.10f, 0.06f, 0.015f, 0.82f);
        public static readonly Color FrameGlowColor = new Color(1f, 0.72f, 0.20f, 0.20f);
        public static readonly Color FrameEdgeColor = new Color(1f, 0.78f, 0.28f, 0.85f);
        public static readonly Color FrameAccentColor = new Color(1f, 0.90f, 0.55f, 1f);
        public static readonly Color LeverMetalColor = new Color(0.66f, 0.58f, 0.42f, 1f);
        public static readonly Color LeverKnobColor = new Color(1f, 0.80f, 0.26f, 1f);

        public static int BaseGold(int acceptedGold, int acceptedGoldBonus)
        {
            if (acceptedGold <= 0)
            {
                return 0;
            }

            return acceptedGold - ClampBonus(acceptedGold, acceptedGoldBonus);
        }

        public static int ClampBonus(int acceptedGold, int acceptedGoldBonus)
        {
            if (acceptedGold <= 0)
            {
                return 0;
            }

            return Mathf.Clamp(acceptedGoldBonus, 0, acceptedGold);
        }

        /// <summary>천 단위 구분자 없이 정수만 쓴다(HUD Gold 표기와 같다).</summary>
        public static string FormatAmount(int amount)
        {
            return amount.ToString(CultureInfo.InvariantCulture);
        }

        public static string FormatBaseLine(int baseGold)
        {
            return "+" + FormatAmount(baseGold) + " G";
        }

        public static string FormatBonusLine(int bonusGold)
        {
            return "추가 골드 +" + FormatAmount(bonusGold) + " G";
        }

        /// <summary>확정 골드에서 보너스를 뺀 기본 획득량만 표시한다. 보너스는 다시 계산하지 않는다.</summary>
        public static string FormatMainText(int acceptedGold, int acceptedGoldBonus = 0)
        {
            if (acceptedGold <= 0)
            {
                return string.Empty;
            }

            return FormatBaseLine(BaseGold(acceptedGold, acceptedGoldBonus));
        }

        public static string FormatBonusText(int acceptedGold, int acceptedGoldBonus)
        {
            int bonus = ClampBonus(acceptedGold, acceptedGoldBonus);
            return bonus > 0 ? FormatBonusLine(bonus) : string.Empty;
        }

        public static float CoinAlpha(float normalizedLife)
        {
            float life = Mathf.Clamp01(normalizedLife);
            float fadeOutStart = 1f - CoinFadeOutPortion;
            if (life <= fadeOutStart)
            {
                return 1f;
            }

            return Mathf.Clamp01((1f - life) / CoinFadeOutPortion);
        }

        public static float CoinFadeIn(float localTime)
        {
            return CoinFadeInSeconds <= 0f ? 1f : Mathf.Clamp01(localTime / CoinFadeInSeconds);
        }

        /// <summary>회전감을 주기 위한 가로 스케일(동전 뒤집힘). 1에서 0.35 사이를 오간다.</summary>
        public static float CoinSpinScaleX(int index, float localTime)
        {
            float phase = localTime * 14f + Mathf.Max(0, index) * 1.3f;
            return Mathf.Lerp(0.35f, 1f, Mathf.Abs(Mathf.Cos(phase)));
        }

        /// <summary>HUD 입자 i의 발사 시각. 퇴장 시작을 0으로 하고 HudStagger 간격으로 이어진다.</summary>
        public static float HudLaunchTime(int index)
        {
            return Mathf.Max(0, index) * HudStagger;
        }

        /// <summary>HUD 입자 비행 진행도. 점점 빨라져 HUD에 빨려 들어가는 느낌을 준다.</summary>
        public static float HudFlightProgress(float flightElapsed)
        {
            float u = Mathf.Clamp01(flightElapsed / HudFlightSeconds);
            return u * u;
        }

        public static Vector2 EvaluateHudPath(Vector2 start, Vector2 end, float progress, int index)
        {
            float u = Mathf.Clamp01(progress);
            Vector2 delta = end - start;
            Vector2 normal = delta.sqrMagnitude > 0.0001f
                ? new Vector2(-delta.y, delta.x).normalized
                : Vector2.zero;
            float side = (index % 2 == 0 ? 1f : -1f) * (1f - 0.3f * index);
            Vector2 control = (start + end) * 0.5f + normal * HudCurvePixels * side;
            float inv = 1f - u;
            return inv * inv * start + 2f * inv * u * control + u * u * end;
        }

        /// <summary>비행 시간 비율(선형) 기준 알파. 짧게 나타난 뒤 도착 직전까지 유지된다.</summary>
        public static float HudParticleAlpha(float flightElapsed)
        {
            float u = Mathf.Clamp01(flightElapsed / HudFlightSeconds);
            return Mathf.Clamp01(u / 0.12f) * Mathf.Lerp(1f, 0.75f, u);
        }

        /// <summary>1.0 → 1.08 → 1.0 짧은 Pulse.</summary>
        public static float EvaluatePulse(float elapsed)
        {
            if (elapsed <= 0f || elapsed >= PulseSeconds)
            {
                return 1f;
            }

            return 1f + (PulsePeak - 1f) * Mathf.Sin(elapsed / PulseSeconds * Mathf.PI);
        }
    }
}

using UnityEngine;

namespace SubTerra.App.Integration
{
    /// <summary>
    /// 골드 획득 연출의 문구·색·HUD 입자 값. 팝업 시간표는 GoldPickupPopupTimeline,
    /// 상태는 GoldPickupPopupState가 맡는다.
    /// 작은 금빛 가루는 채굴 칸에서 흩어지고, 끝 무렵 작은 입자가 Gold HUD로 날아가 도착 시 Pulse한다.
    /// 모든 값은 시각 전용이며 골드 지급 시점과 무관하다.
    /// </summary>
    public static class GoldPickupPresentation
    {
        public const int DustCount = 10;
        public const float DustLifetimeMax = 0.55f;
        public const float DustCleanupSeconds = 0.8f;

        // 머리와 팝업 아래쪽 사이 여백.
        public const float PopupBaseLift = 0.12f;

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

        /// <summary>확정 골드(AcceptedGold)에서 보너스를 뺀 기본 획득량.</summary>
        public static int BaseGold(int acceptedGold, int acceptedGoldBonus)
        {
            if (acceptedGold <= 0)
            {
                return 0;
            }

            return acceptedGold - ClampBonus(acceptedGold, acceptedGoldBonus);
        }

        /// <summary>확정 보너스(AcceptedGoldBonus)를 0~확정 골드 범위로 제한한다. 다시 계산하지 않는다.</summary>
        public static int ClampBonus(int acceptedGold, int acceptedGoldBonus)
        {
            if (acceptedGold <= 0)
            {
                return 0;
            }

            return Mathf.Clamp(acceptedGoldBonus, 0, acceptedGold);
        }

        /// <summary>기본 줄 문구. 음수는 비우고 0은 "+0 G". 현재 HUD Gold가 천 단위 구분자를 쓰지 않아 똑같이 쓰지 않는다.</summary>
        public static string FormatBase(int baseGold)
        {
            return baseGold < 0 ? string.Empty : "+" + baseGold + " G";
        }

        /// <summary>추가 줄 문구. 0 이하면 줄 자체가 없다.</summary>
        public static string FormatBonus(int bonusGold)
        {
            return bonusGold > 0 ? "추가 골드 +" + bonusGold + " G" : string.Empty;
        }

        /// <summary>확정 골드에서 기본 획득량만 문구로 만든다.</summary>
        public static string FormatMainText(int acceptedGold, int acceptedGoldBonus = 0)
        {
            return acceptedGold <= 0 ? string.Empty : FormatBase(BaseGold(acceptedGold, acceptedGoldBonus));
        }

        public static string FormatBonusText(int acceptedGold, int acceptedGoldBonus)
        {
            return FormatBonus(ClampBonus(acceptedGold, acceptedGoldBonus));
        }

        /// <summary>회전감을 주기 위한 가로 스케일(동전 뒤집힘). 1에서 0.35 사이를 오간다.</summary>
        public static float CoinSpinScaleX(int index, float localTime)
        {
            float phase = localTime * 14f + Mathf.Max(0, index) * 1.3f;
            return Mathf.Lerp(0.35f, 1f, Mathf.Abs(Mathf.Cos(phase)));
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

using UnityEngine;

namespace SubTerra.App.Integration
{
    /// <summary>
    /// 골드 획득 연출의 문구·색·타임라인 (prompt-B 114).
    /// 금화 3장은 채굴 칸에서 짧게 튀었다 떨어지고, 금빛 가루가 함께 흩어진다.
    /// 문구는 살짝 커졌다 돌아온 뒤 천천히 떠오르며 사라진다.
    /// 끝 무렵 작은 금빛 입자가 Gold HUD로 날아가 도착 시 Gold 영역을 짧게 Pulse한다.
    /// 모든 값은 시각 전용이며 골드 지급 시점과 무관하다.
    /// </summary>
    public static class GoldPickupPresentation
    {
        public const int CoinCount = 3;
        public const float CoinStartDrop = 0.04f;
        public const float CoinWorldScale = 0.2f;
        public const float CoinGravity = 11f;
        public const float CoinFadeInSeconds = 0.05f;
        public const float CoinFadeOutPortion = 0.35f;

        // 수직 기준 발사각. 음수는 왼쪽, 양수는 오른쪽.
        private static readonly float[] LaunchAngles = { -24f, 4f, 21f };
        private static readonly float[] LaunchSpeeds = { 3.1f, 3.5f, 2.9f };
        // 0에 가까울수록 앞(큼), 1에 가까울수록 뒤(작음).
        private static readonly float[] Depths = { 0.5f, 0.0f, 0.8f };
        private static readonly float[] SpawnX = { -0.04f, 0f, 0.05f };
        private static readonly float[] Delays = { 0.02f, 0.00f, 0.05f };

        public const int DustCount = 10;
        public const float DustLifetimeMax = 0.55f;
        public const float DustCleanupSeconds = 0.8f;

        public const float MainFontSize = 34f;
        public const float TextPopSeconds = 0.14f;
        public const float TextSettleSeconds = 0.16f;
        public const float TextPopScaleFrom = 0.55f;
        public const float TextPopScalePeak = 1.16f;
        public const float TextFadeInSeconds = 0.08f;
        public const float TextFadeOutSeconds = 0.35f;
        public const float TextDuration = 1.4f;
        public const float TextRise = 0.5f;
        // 머리와 문구 사이 여백. 보너스 줄이 캐릭터에 닿지 않게 한다.
        public const float TextBaseLift = 0.12f;
        public const float BonusDelaySeconds = 0.1f;
        public const int MaxActiveTexts = 3;
        // 단일 줄/보너스 포함 문구의 월드 높이. 연속 획득 시 이전 문구를 이만큼 위로 민다.
        public const float TextLineStep = 0.44f;
        public const float TextBonusLineStep = 0.7f;
        public const float TextStackFollowSpeed = 14f;
        // 초과된 오래된 문구를 빠르게 지우는 시간.
        public const float TextEvictSeconds = 0.15f;

        public const int HudParticleCount = 3;
        public const float HudLaunchSeconds = 0.8f;
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

        /// <summary>확정 골드에서 보너스를 뺀 기본 획득량만 표시한다. 보너스는 다시 계산하지 않는다.</summary>
        public static string FormatMainText(int acceptedGold, int acceptedGoldBonus = 0)
        {
            if (acceptedGold <= 0)
            {
                return string.Empty;
            }

            return "+" + BaseGold(acceptedGold, acceptedGoldBonus) + "G";
        }

        public static string FormatBonusText(int acceptedGold, int acceptedGoldBonus)
        {
            int bonus = ClampBonus(acceptedGold, acceptedGoldBonus);
            return bonus > 0 ? "BONUS +" + bonus + "G" : string.Empty;
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

        public static Vector3 CoinStart(Vector3 origin, int index)
        {
            index = ClampIndex(index);
            return new Vector3(
                origin.x + SpawnX[index],
                origin.y - CoinStartDrop,
                origin.z);
        }

        public static float CoinDelay(int index)
        {
            return Delays[ClampIndex(index)];
        }

        /// <summary>튀어 올랐다가 출발 높이로 돌아오는 전체 비행 시간.</summary>
        public static float CoinFlightDuration(int index)
        {
            GetLaunch(index, out _, out float vy);
            return 2f * vy / CoinGravity;
        }

        public static float CoinScale(int index)
        {
            float depth = Depths[ClampIndex(index)];
            return CoinWorldScale * Mathf.Lerp(1.12f, 0.8f, depth);
        }

        public static int CoinSortingBias(int index)
        {
            return Mathf.RoundToInt((1f - Depths[ClampIndex(index)]) * 8f);
        }

        public static Vector3 EvaluateCoinPosition(Vector3 origin, int index, float localTime)
        {
            GetLaunch(index, out float vx, out float vy);
            float t = Mathf.Max(0f, localTime);
            Vector3 start = CoinStart(origin, index);
            return new Vector3(
                start.x + vx * t,
                start.y + vy * t - 0.5f * CoinGravity * t * t,
                start.z);
        }

        /// <summary>회전감을 주기 위한 가로 스케일(동전 뒤집힘). 1에서 0.35 사이를 오간다.</summary>
        public static float CoinSpinScaleX(int index, float localTime)
        {
            float phase = localTime * 14f + ClampIndex(index) * 1.3f;
            return Mathf.Lerp(0.35f, 1f, Mathf.Abs(Mathf.Cos(phase)));
        }

        public static float MaxCoinLifetime()
        {
            float max = 0f;
            for (var index = 0; index < CoinCount; index++)
            {
                float life = CoinDelay(index) + CoinFlightDuration(index);
                if (life > max)
                {
                    max = life;
                }
            }

            return max;
        }

        private static void GetLaunch(int index, out float vx, out float vy)
        {
            index = ClampIndex(index);
            float angle = LaunchAngles[index] * Mathf.Deg2Rad;
            float speed = LaunchSpeeds[index];
            vx = Mathf.Sin(angle) * speed;
            vy = Mathf.Cos(angle) * speed;
        }

        private static int ClampIndex(int index)
        {
            if (index < 0)
            {
                return 0;
            }

            if (index >= CoinCount)
            {
                return CoinCount - 1;
            }

            return index;
        }

        /// <summary>
        /// 문구 타임라인. 등장 시 0.55→1.16→1.0으로 튀어나오고,
        /// 전체 구간 동안 감속하며 위로 떠오르고, 마지막 0.35초에 사라진다.
        /// </summary>
        public static void EvaluateText(float elapsed, out float alpha, out float yOffset, out float scale)
        {
            if (elapsed <= 0f)
            {
                alpha = 0f;
                yOffset = 0f;
                scale = TextPopScaleFrom;
                return;
            }

            if (elapsed >= TextDuration)
            {
                alpha = 0f;
                yOffset = TextRise;
                scale = 1f;
                return;
            }

            float fadeIn = Mathf.Clamp01(elapsed / TextFadeInSeconds);
            float fadeOut = Mathf.Clamp01((TextDuration - elapsed) / TextFadeOutSeconds);
            alpha = Mathf.Min(fadeIn, SmoothStep(fadeOut));
            yOffset = TextRise * EaseOutCubic(elapsed / TextDuration);
            scale = EvaluatePopScale(elapsed);
        }

        /// <summary>보너스 줄은 주 금액보다 약간 늦게 튀어나와 읽는 순서를 만든다.</summary>
        public static void EvaluateBonus(float elapsed, out float alpha, out float scale)
        {
            float local = elapsed - BonusDelaySeconds;
            if (local <= 0f)
            {
                alpha = 0f;
                scale = TextPopScaleFrom;
                return;
            }

            EvaluateText(elapsed, out float mainAlpha, out _, out _);
            alpha = Mathf.Min(Mathf.Clamp01(local / TextFadeInSeconds), mainAlpha);
            scale = EvaluatePopScale(local);
        }

        public static float EvaluatePopScale(float elapsed)
        {
            if (elapsed <= 0f)
            {
                return TextPopScaleFrom;
            }

            if (elapsed < TextPopSeconds)
            {
                float u = EaseOutCubic(elapsed / TextPopSeconds);
                return Mathf.Lerp(TextPopScaleFrom, TextPopScalePeak, u);
            }

            float settle = elapsed - TextPopSeconds;
            if (settle < TextSettleSeconds)
            {
                return Mathf.Lerp(TextPopScalePeak, 1f, SmoothStep(settle / TextSettleSeconds));
            }

            return 1f;
        }

        /// <summary>연속 획득 시 새 문구 아래에 쌓인 이전 문구의 추가 높이.</summary>
        public static float StackStep(bool hasBonus)
        {
            return hasBonus ? TextBonusLineStep : TextLineStep;
        }

        public static float HudLaunchTime(int index)
        {
            return HudLaunchSeconds + Mathf.Max(0, index) * HudStagger;
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

        private static float EaseOutCubic(float u)
        {
            u = Mathf.Clamp01(u);
            float inv = 1f - u;
            return 1f - inv * inv * inv;
        }

        private static float SmoothStep(float u)
        {
            u = Mathf.Clamp01(u);
            return u * u * (3f - 2f * u);
        }
    }
}

using UnityEngine;

namespace SubTerra.App.Integration
{
    /// <summary>
    /// 골드 블록 채굴 연출의 문구·색·타임라인.
    /// 금화 5장은 같은 칸에서 각기 다른 각도로 포물선 솟구침을 그리고,
    /// 원근(깊이)에 따라 크기가 조금 다르다. 높이가 중간일 때 가장 잘 보인다.
    /// 텍스트는 머리 위에서 0.5초 등장, 1초 유지, 0.5초 하강 소멸이다.
    /// </summary>
    public static class GoldPickupPresentation
    {
        public const int CoinCount = 5;
        public const float CoinStartDrop = 0.08f;
        public const float CoinWorldScale = 0.32f;
        public const float CoinGravity = 6.2f;

        // 수직 기준 발사각. 음수는 왼쪽, 양수는 오른쪽.
        private static readonly float[] LaunchAngles = { -26f, -11f, 5f, 20f, -18f };
        private static readonly float[] LaunchSpeeds = { 3.8f, 4.15f, 4.4f, 4.0f, 3.45f };
        // 0에 가까울수록 앞(큼), 1에 가까울수록 뒤(작음).
        private static readonly float[] Depths = { 0.58f, 0.16f, 0.0f, 0.34f, 0.76f };
        private static readonly float[] SpawnX = { -0.03f, 0.02f, 0f, 0.04f, -0.02f };
        private static readonly float[] Delays = { 0.00f, 0.04f, 0.02f, 0.06f, 0.03f };

        public const float TextFadeInSeconds = 0.5f;
        public const float TextHoldSeconds = 1f;
        public const float TextFadeOutSeconds = 0.5f;
        public const float TextRise = 0.28f;
        public const float TextDuration = TextFadeInSeconds + TextHoldSeconds + TextFadeOutSeconds;

        public static readonly Color FillColor = new Color32(0xFF, 0xFB, 0x19, 0xFF);

        public static string FormatPickupText(int acceptedGold)
        {
            if (acceptedGold <= 0)
            {
                return string.Empty;
            }

            return acceptedGold + "G 골드 획득!";
        }

        public static float CoinAlpha(float normalizedHeight)
        {
            float height = Mathf.Clamp01(normalizedHeight);
            if (height <= 0.5f)
            {
                return height * 2f;
            }

            return (1f - height) * 2f;
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

        public static float CoinFlightDuration(int index)
        {
            GetLaunch(index, out _, out float vy);
            return vy / CoinGravity;
        }

        public static float CoinScale(int index)
        {
            float depth = Depths[ClampIndex(index)];
            return CoinWorldScale * Mathf.Lerp(1.14f, 0.76f, depth);
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

        public static float EvaluateCoinHeightNormalized(int index, float localTime)
        {
            GetLaunch(index, out _, out float vy);
            float peak = vy * vy / (2f * CoinGravity);
            if (peak <= 0.0001f)
            {
                return 1f;
            }

            float t = Mathf.Max(0f, localTime);
            float height = vy * t - 0.5f * CoinGravity * t * t;
            return Mathf.Clamp01(height / peak);
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

        public static void EvaluateText(float elapsed, out float alpha, out float yOffset)
        {
            if (elapsed <= 0f)
            {
                alpha = 0f;
                yOffset = 0f;
                return;
            }

            if (elapsed < TextFadeInSeconds)
            {
                float u = elapsed / TextFadeInSeconds;
                alpha = u;
                yOffset = u * TextRise;
                return;
            }

            if (elapsed < TextFadeInSeconds + TextHoldSeconds)
            {
                alpha = 1f;
                yOffset = TextRise;
                return;
            }

            if (elapsed < TextDuration)
            {
                float u = (elapsed - TextFadeInSeconds - TextHoldSeconds) / TextFadeOutSeconds;
                alpha = 1f - u;
                yOffset = TextRise * (1f - u);
                return;
            }

            alpha = 0f;
            yOffset = 0f;
        }
    }
}

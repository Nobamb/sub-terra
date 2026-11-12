using System;

namespace SubTerra.App.Integration
{
    /// <summary>
    /// 자원 아이콘 홀로그램의 시간표. 상태가 없는 순수 계산이라 EditMode에서 검증한다.
    /// 처음엔 청록으로 덮이고, 스캔 줄이 위에서 아래로 지나가며 원래 색이 드러난다.
    /// 엔진 연료는 스캔이 끝난 뒤에만 따뜻한 후광을 한 번 퍼뜨린다.
    /// 모션 감소 설정은 보지 않는다(작은 UI 연출은 줄이지 않는 프로젝트 결정).
    /// </summary>
    public static class ResourceHologramTimeline
    {
        public const float MinScanSeconds = 0.2f;
        public const float MaxScanSeconds = 0.3f;
        public const float DefaultScanSeconds = 0.22f;
        /// <summary>아이콘이 투명에서 올라오는 시간. 수확 홀로그램 등장(0.26s)과 겹친다.</summary>
        public const float FadeInSeconds = 0.08f;
        public const float FuelHaloSeconds = 0.42f;
        public const float ReleaseSeconds = 0.12f;
        public const float NoiseHz = 24f;

        public static float ClampScan(float seconds)
        {
            return seconds < MinScanSeconds ? MinScanSeconds : seconds > MaxScanSeconds ? MaxScanSeconds : seconds;
        }

        /// <summary>스캔 줄 위치. 0은 아이콘 맨 위, 1은 맨 아래이고 이후에는 1로 고정된다.</summary>
        public static float ScanProgress(float age, float scanSeconds)
        {
            return Clamp01(age / Math.Max(0.0001f, scanSeconds));
        }

        /// <summary>
        /// 위에서 row번째 줄(0=맨 위)이 원래 색으로 드러난 정도 0~1.
        /// 스캔 줄이 줄 중심을 지나는 순간 한 줄 폭 안에서 부드럽게 바뀐다.
        /// </summary>
        public static float RowReveal(float progress, int row, int rows)
        {
            if (rows <= 0)
            {
                return 1f;
            }

            var center = (row + 0.5f) / rows;
            return Clamp01((progress - center) / (0.5f / rows) + 0.5f);
        }

        public static float FadeIn(float age)
        {
            return Clamp01(age / FadeInSeconds);
        }

        /// <summary>연료 후광 진행 0~1. 스캔이 끝나기 전에는 -1(없음), 끝난 뒤 한 번만 0→1로 지나간다.</summary>
        public static float FuelHaloProgress(float age, float scanSeconds)
        {
            var since = age - scanSeconds;
            if (since < 0f || since >= FuelHaloSeconds)
            {
                return -1f;
            }

            return since / FuelHaloSeconds;
        }

        /// <summary>후광 세기. 번쩍 올라왔다가 퍼지며 사라진다.</summary>
        public static float FuelHaloAlpha(float haloProgress)
        {
            if (haloProgress < 0f)
            {
                return 0f;
            }

            var rise = Clamp01(haloProgress / 0.2f);
            var fall = 1f - Clamp01((haloProgress - 0.2f) / 0.8f);
            return rise * fall * fall;
        }

        /// <summary>후광이 퍼지는 배율(아이콘 크기 대비).</summary>
        public static float FuelHaloScale(float haloProgress)
        {
            var k = Clamp01(haloProgress);
            var inv = 1f - k;
            return 1f + 1.1f * (1f - inv * inv * inv);
        }

        /// <summary>가로 줄무늬 노이즈 농도. 줄마다 번갈아 진하고, 24Hz로 조금씩 흔들린다. 같은 입력엔 같은 값.</summary>
        public static float StripeAlpha(int row, float age)
        {
            var baseAlpha = (row & 1) == 0 ? 0.38f : 0.22f;
            return baseAlpha + (Hash(row, (int)(age * NoiseHz)) - 0.5f) * 0.14f;
        }

        /// <summary>덮여 있는 줄의 좌우 흔들림(-1~1). 같은 입력엔 같은 값.</summary>
        public static float Jitter(int row, float age)
        {
            return Hash(row + 31, (int)(age * NoiseHz)) * 2f - 1f;
        }

        public const float PopSeconds = 0.34f;
        public const float BurstSeconds = 0.5f;
        public const float BracketSeconds = 0.22f;

        /// <summary>팝인 배율. 작게 시작해 살짝 튀어 오른 뒤 1로 자리 잡는다.</summary>
        public static float PopScale(float age)
        {
            var t = Clamp01(age / PopSeconds) - 1f;
            var back = 1f + 2.70158f * t * t * t + 1.70158f * t * t;
            return 0.35f + 0.65f * back;
        }

        /// <summary>팝인이 끝난 뒤 위아래로 천천히 떠 있는 정도(-1~1). 그 전에는 0.</summary>
        public static float Bob(float age)
        {
            var t = age - PopSeconds;
            return t <= 0f ? 0f : (float)Math.Sin(t * 3.4) * Clamp01(t / 0.3f);
        }

        /// <summary>스캔이 끝난 순간부터 폭발·충격파가 퍼지는 진행 0~1. 끝나기 전·지난 뒤에는 -1.</summary>
        public static float BurstProgress(float age, float scanSeconds)
        {
            var since = age - scanSeconds;
            return since < 0f || since >= BurstSeconds ? -1f : since / BurstSeconds;
        }

        /// <summary>코너 브래킷이 바깥에서 아이콘 둘레로 모여드는 진행 0~1.</summary>
        public static float BracketProgress(float age)
        {
            var k = Clamp01(age / BracketSeconds);
            var inv = 1f - k;
            return 1f - inv * inv * inv;
        }

        /// <summary>색수차 유령(청록·분홍 어긋난 복사본)이 보이는 정도. 스캔 중 가장 강하고 스캔이 끝난 직후 사라진다.</summary>
        public static float GlitchAmount(float age, float scanSeconds)
        {
            var tail = 0.1f;
            return age >= scanSeconds + tail ? 0f : age <= scanSeconds ? 1f : 1f - (age - scanSeconds) / tail;
        }

        public static float EaseOut(float k)
        {
            var inv = 1f - Clamp01(k);
            return 1f - inv * inv * inv;
        }

        public static float Hash(int a, int b)
        {
            unchecked
            {
                var h = (uint)(a * 73856093) ^ (uint)(b * 19349663);
                h ^= h >> 13;
                h *= 1274126177u;
                h ^= h >> 16;
                return (h & 0xFFFFu) / 65535f;
            }
        }

        private static float Clamp01(float v)
        {
            return v < 0f ? 0f : v > 1f ? 1f : v;
        }
    }
}

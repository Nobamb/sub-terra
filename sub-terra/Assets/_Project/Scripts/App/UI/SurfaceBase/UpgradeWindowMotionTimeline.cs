using UnityEngine;

namespace SubTerra.App.UI.SurfaceBase
{
    /// <summary>업그레이드 창 열기·닫기 한 순간의 연출 값. 진행값은 0~1, 각도는 도, 위치는 캔버스 단위다.</summary>
    public struct UpgradeWindowFrame
    {
        /// <summary>자물쇠가 나타난 정도. 자물쇠·조각 전체 알파에 곱한다.</summary>
        public float Presence;
        public float LockScale;
        public float LockAngle;
        /// <summary>자물쇠 중심의 세로 오프셋(위가 +). 노드 자물쇠처럼 떨어지지 않도록 줄어들지 않는다.</summary>
        public float LockY;
        /// <summary>부서지지 않은 자물쇠(몸통·고리·금)의 알파.</summary>
        public float LockAlpha;
        /// <summary>고리가 들린 높이(자물쇠 단위, 노드 자물쇠 30x34 기준).</summary>
        public float ShackleLift;
        /// <summary>금이 번진 정도.</summary>
        public float Crack;
        /// <summary>부서짐 진행(0 온전, 1 다 흩어져 사라짐). 조각 퍼짐·알파는 이 값에서 나온다.</summary>
        public float Shatter;
        public float ShardSpread;
        public float ShardAlpha;
        /// <summary>창 펼침(카드 scale 배율).</summary>
        public float Expand;
        /// <summary>카드 CanvasGroup 알파 배율.</summary>
        public float WindowAlpha;
        /// <summary>딤 알파 배율.</summary>
        public float Dim;
        /// <summary>자물쇠 뒤 청록 빛.</summary>
        public float Glow;
    }

    /// <summary>
    /// B-167 업그레이드 창 연출 시간표(순수 계산). 큰 청록 금속 자물쇠가 화면 가운데에서 금이 가며 부서지고
    /// 그 자리에서 창이 펼쳐진다. 닫기는 정확한 역순이다. 모든 시간은 비스케일 초다.
    /// </summary>
    public static class UpgradeWindowMotionTimeline
    {
        public const float FullOpenDuration = 1.2f;
        public const float QuickOpenDuration = 0.5f;
        public const float CloseDuration = 0.3f;

        // 노드·썸네일 자물쇠(30x34)의 약 4.5배.
        public const float LockUnit = 4.5f;
        public static readonly Vector2 LockSize = new Vector2(30f, 34f) * LockUnit;
        public const float ShakeDegrees = 8f;
        public const float ShackleKick = 5f;
        public const int ShardCount = 8;

        public const float ShakeStartTime = 0.15f;
        public const float FullCrackStartTime = 0.45f;
        public const float FullShatterTime = 0.75f;
        public const float QuickCrackStartTime = 0.08f;
        public const float QuickShatterTime = 0.2f;
        public const float CloseFoldEndTime = 0.12f;
        public const float RegatherTime = 0.2f;
        public const float CrackClearTime = 0.25f;

        private struct Plan
        {
            public float AppearEnd;
            public float ShakeStart;
            public float ShakeEnd;
            public float UnlockEnd;
            public float CrackStart;
            public float CrackEnd;
            public float ShatterStart;
            public float ShatterEnd;
            public float ExpandStart;
            public float ExpandEnd;
            public float Total;
        }

        private static readonly Plan Full = new Plan
        {
            AppearEnd = ShakeStartTime, ShakeStart = ShakeStartTime, ShakeEnd = FullCrackStartTime,
            UnlockEnd = 0.55f, CrackStart = FullCrackStartTime, CrackEnd = FullShatterTime,
            ShatterStart = FullShatterTime, ShatterEnd = 0.95f, ExpandStart = 0.8f, ExpandEnd = 1.1f,
            Total = FullOpenDuration
        };

        private static readonly Plan Quick = new Plan
        {
            AppearEnd = QuickCrackStartTime, ShakeStart = QuickCrackStartTime, ShakeEnd = QuickCrackStartTime,
            UnlockEnd = 0.14f, CrackStart = QuickCrackStartTime, CrackEnd = QuickShatterTime,
            ShatterStart = QuickShatterTime, ShatterEnd = 0.32f, ExpandStart = 0.22f, ExpandEnd = 0.45f,
            Total = QuickOpenDuration
        };

        public static float OpenDuration(bool full) => full ? FullOpenDuration : QuickOpenDuration;
        public static float CrackStartTime(bool full) => full ? FullCrackStartTime : QuickCrackStartTime;
        public static float ShatterTime(bool full) => full ? FullShatterTime : QuickShatterTime;
        public static float ExpandStartTime(bool full) => full ? Full.ExpandStart : Quick.ExpandStart;

        public static UpgradeWindowFrame Open(float t, bool full)
        {
            var p = full ? Full : Quick;
            t = Mathf.Clamp(t, 0f, p.Total);
            var f = new UpgradeWindowFrame();

            // 나타남: 0 → 1.1 → 1, 아래에서 살짝 떠오른다(내려가지 않음).
            var appear = Progress(t, 0f, p.AppearEnd);
            f.Presence = Smooth(Progress(t, 0f, p.AppearEnd * 0.6f));
            f.LockScale = appear < 0.65f
                ? Mathf.Lerp(0.35f, 1.1f, EaseOut(appear / 0.65f))
                : Mathf.Lerp(1.1f, 1f, Smooth((appear - 0.65f) / 0.35f));
            f.LockY = Mathf.Lerp(-14f, 0f, EaseOut(appear));

            if (full && t > p.ShakeStart && t < p.ShakeEnd)
            {
                // 심층 썸네일 자물쇠와 같은 흔들림 식. 큰 자물쇠라 진폭은 줄였다.
                var s = Progress(t, p.ShakeStart, p.ShakeEnd);
                f.LockAngle = Mathf.Sin(s * Mathf.PI * 8f) * ShakeDegrees * (0.5f + 0.5f * s);
            }

            f.ShackleLift = ShackleKick * EaseOut(Progress(t, p.CrackStart, p.UnlockEnd));
            // 금은 잠금 해제와 함께 천천히 시작해 부서지기 직전 끝까지 번진다.
            f.Crack = Smooth(Progress(t, p.CrackStart, p.CrackEnd));
            f.Shatter = Progress(t, p.ShatterStart, p.ShatterEnd);
            f.Expand = EaseOut(Progress(t, p.ExpandStart, p.ExpandEnd));
            f.WindowAlpha = 0.85f * Smooth(Progress(t, p.ExpandStart, p.ExpandStart + (p.ExpandEnd - p.ExpandStart) * 0.67f))
                + 0.15f * Smooth(Progress(t, p.ExpandEnd, p.Total));
            // 자물쇠가 나타날 때 화면을 먼저 충분히 어둡게 해 뒤 버튼·글자보다 자물쇠가 먼저 읽히게 한다.
            f.Dim = 0.8f * Smooth(Progress(t, 0f, p.AppearEnd)) + 0.2f * Smooth(Progress(t, p.ExpandEnd, p.Total));

            var flash = 1f - Mathf.Clamp01(Mathf.Abs(t - p.ShatterStart) / ((p.ShatterEnd - p.ShatterStart) * 0.35f));
            f.Glow = Mathf.Max(0.45f * f.Crack * (1f - Smooth(f.Shatter * 2f)), flash) * f.Presence;
            Derive(ref f);
            return f;
        }

        /// <summary>
        /// 닫기(정확한 역순): 창이 가운데 점으로 접힘 → 조각이 다시 모여 자물쇠 → 금이 사라짐 → 자물쇠가 흐려짐.
        /// 각 값은 시작값(from)보다 커지지 않아 여는 도중 닫아도 튀지 않는다. 흔들림은 없다.
        /// </summary>
        public static UpgradeWindowFrame Close(float u, UpgradeWindowFrame from)
        {
            u = Mathf.Clamp(u, 0f, CloseDuration);
            var f = new UpgradeWindowFrame();

            f.Expand = Mathf.Min(from.Expand, 1f - EaseIn(Progress(u, 0f, CloseFoldEndTime)));
            f.WindowAlpha = Mathf.Min(from.WindowAlpha, 1f - Smooth(Progress(u, 0.03f, CloseFoldEndTime)));
            f.Shatter = Mathf.Min(from.Shatter, 1f - Smooth(Progress(u, 0.1f, RegatherTime)));
            f.Crack = Mathf.Min(from.Crack, 1f - Smooth(Progress(u, 0.18f, CrackClearTime)));
            f.ShackleLift = Mathf.Min(from.ShackleLift, ShackleKick * (1f - Smooth(Progress(u, 0.19f, CrackClearTime))));

            var fade = Smooth(Progress(u, 0.22f, CloseDuration));
            f.Presence = Mathf.Min(from.Presence, 1f - fade);
            f.LockScale = Mathf.Min(from.LockScale, Mathf.Lerp(from.LockScale, 0.8f, fade));
            f.LockY = from.LockY;
            // 흔들리던 중에 닫으면 그 각도에서 바로 가라앉는다. 다시 흔들지 않는다.
            f.LockAngle = from.LockAngle * (1f - Smooth(Progress(u, 0f, 0.06f)));
            f.Dim = Mathf.Min(from.Dim, 1f - Smooth(Progress(u, 0.04f, CloseDuration)));

            var gather = 1f - Mathf.Clamp01(Mathf.Abs(u - RegatherTime) / 0.06f);
            f.Glow = Mathf.Max(0.45f * f.Crack, from.Shatter > 0f ? gather : 0f) * f.Presence;
            Derive(ref f);
            return f;
        }

        /// <summary>조각 i의 퍼짐 위치(자물쇠 중심 기준, 캔버스 단위)와 회전. 열기·닫기가 같은 길을 쓴다.</summary>
        public static void ShardPose(int index, float spread, out Vector2 position, out float angle)
        {
            var seed = Shards[Mathf.Clamp(index, 0, ShardCount - 1)];
            var start = seed.Start * LockUnit;
            var direction = (seed.Start - new Vector2(0f, -4f)).normalized;
            direction = Quaternion.Euler(0f, 0f, seed.Turn) * direction;
            position = start + (Vector2)direction * (seed.Distance * spread);
            angle = seed.Spin * spread;
        }

        public static Vector2 ShardStart(int index) => Shards[Mathf.Clamp(index, 0, ShardCount - 1)].Start;

        private struct ShardSeed
        {
            public Vector2 Start;
            public float Turn;
            public float Distance;
            public float Spin;
        }

        // 고정 값(시드 167로 한 번 뽑아 고정). 몸통 5조각 + 고리 3조각.
        private static readonly ShardSeed[] Shards =
        {
            new ShardSeed { Start = new Vector2(-8f, -11f), Turn = 14f, Distance = 210f, Spin = -250f },
            new ShardSeed { Start = new Vector2(1f, -12f), Turn = -18f, Distance = 170f, Spin = 190f },
            new ShardSeed { Start = new Vector2(9f, -10f), Turn = 9f, Distance = 235f, Spin = 300f },
            new ShardSeed { Start = new Vector2(-9f, -2f), Turn = -11f, Distance = 250f, Spin = 160f },
            new ShardSeed { Start = new Vector2(8f, -1f), Turn = 17f, Distance = 190f, Spin = -210f },
            new ShardSeed { Start = new Vector2(-8f, 8f), Turn = 21f, Distance = 175f, Spin = 280f },
            new ShardSeed { Start = new Vector2(8f, 9f), Turn = -15f, Distance = 220f, Spin = -170f },
            new ShardSeed { Start = new Vector2(0f, 14f), Turn = 6f, Distance = 200f, Spin = 230f }
        };

        private static void Derive(ref UpgradeWindowFrame f)
        {
            // 부서지기 시작하면 온전한 자물쇠는 빠르게 조각으로 바뀌고, 조각은 흩어지며 흐려진다.
            f.LockAlpha = f.Presence * (1f - Smooth(Progress(f.Shatter, 0f, 0.12f)));
            f.ShardSpread = EaseOut(f.Shatter);
            f.ShardAlpha = f.Presence * Smooth(Progress(f.Shatter, 0f, 0.06f)) * (1f - Smooth(Progress(f.Shatter, 0.5f, 1f)));
        }

        public static float Progress(float t, float start, float end)
        {
            return end <= start ? (t >= end ? 1f : 0f) : Mathf.Clamp01((t - start) / (end - start));
        }

        public static float Smooth(float x)
        {
            x = Mathf.Clamp01(x);
            return x * x * (3f - 2f * x);
        }

        public static float EaseOut(float x)
        {
            x = Mathf.Clamp01(x);
            var inv = 1f - x;
            return 1f - inv * inv * inv;
        }

        public static float EaseIn(float x)
        {
            x = Mathf.Clamp01(x);
            return x * x * x;
        }
    }
}

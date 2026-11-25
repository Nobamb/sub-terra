using UnityEngine;

namespace SubTerra.Gameplay.Player
{
    /// <summary>B-211: 착지 한 번의 판정 결과. 찌그러짐·먼지·흔들림은 이 값만 보고 고른다.</summary>
    public readonly struct PlayerLandingInfo
    {
        public PlayerLandingInfo(float distance, bool ladderExempt, bool stableLanding, bool damaged, bool canActAfter)
        {
            Distance = Mathf.Max(0f, distance);
            LadderExempt = ladderExempt;
            StableLanding = stableLanding;
            Damaged = damaged;
            CanActAfter = canActAfter;
        }

        /// <summary>최고점에서 착지점까지 떨어진 높이(칸).</summary>
        public float Distance { get; }
        /// <summary>사다리를 타고 내려와 닿았다.</summary>
        public bool LadderExempt { get; }
        /// <summary>완충판 위 착지.</summary>
        public bool StableLanding { get; }
        /// <summary>이 착지로 체력이 깎였다.</summary>
        public bool Damaged { get; }
        /// <summary>착지 뒤에도 행동할 수 있다(기절·실패가 아님).</summary>
        public bool CanActAfter { get; }
    }

    public enum PlayerLandingDustSize
    {
        None,
        Small,
        Medium,
        Large
    }

    /// <summary>B-211 점프·착지 손맛 수치. 순수 계산이라 EditMode에서 검사한다.</summary>
    public static class PlayerLandingFeedbackRules
    {
        /// <summary>이보다 낮은 착지는 찌그러짐·먼지가 없다. 착지음 기준과 같다.</summary>
        public const float MinEffectHeight = PlayerMoveSfxGate.MinLandHeight;
        public const float FullSquashHeight = 3f;
        public const float MediumDustHeight = 1f;
        public const float LargeDustHeight = 3f;
        public const float ShakeMinHeight = 3f;
        public const float ShakeFullHeight = 8f;
        public const float ShakeMinAmplitude = 0.05f;
        public const float ShakeMaxAmplitude = 0.10f;
        public const float ShakeDuration = 0.1f;

        public static bool ShouldSquash(in PlayerLandingInfo landing) =>
            landing.CanActAfter && !landing.LadderExempt && landing.Distance >= MinEffectHeight;

        /// <summary>0.3칸에서 light, 3칸 이상에서 heavy. 그 사이는 선형.</summary>
        public static Vector2 LandingSquash(float distance, Vector2 light, Vector2 heavy)
        {
            float t = Mathf.Clamp01((distance - MinEffectHeight) / (FullSquashHeight - MinEffectHeight));
            return Vector2.Lerp(light, heavy, t);
        }

        /// <summary>세기 배율. 0이면 1(효과 없음), 1이면 그대로.</summary>
        public static Vector2 ApplyStrength(Vector2 factor, float strength)
        {
            float s = Mathf.Clamp(strength, 0f, 1.5f);
            return new Vector2(1f + (factor.x - 1f) * s, 1f + (factor.y - 1f) * s);
        }

        /// <summary>시작 1 → 끝 0. 부드럽게 줄고 0 아래로 내려가지 않는다(튕김 없음).</summary>
        public static float RecoverWeight(float elapsed, float duration)
        {
            if (duration <= 0f) return 0f;
            float u = Mathf.Clamp01(elapsed / duration);
            return 1f - u * u * (3f - 2f * u);
        }

        /// <summary>
        /// 세로 배율이 바뀌어도 발바닥이 그대로 있도록 그림 뿌리를 옮길 거리.
        /// footFromPivot은 그림 뿌리(배율 1 기준)에서 발바닥까지의 세로 거리(아래면 음수).
        /// </summary>
        public static float FootAnchorOffset(float footFromPivot, float scaleY) => footFromPivot * (1f - scaleY);

        public static PlayerLandingDustSize ResolveDustSize(in PlayerLandingInfo landing)
        {
            if (landing.LadderExempt) return PlayerLandingDustSize.None;
            PlayerLandingDustSize size;
            if (landing.Damaged) size = PlayerLandingDustSize.Large;
            else if (landing.Distance < MinEffectHeight) return PlayerLandingDustSize.None;
            else if (landing.Distance < MediumDustHeight) size = PlayerLandingDustSize.Small;
            else if (landing.Distance < LargeDustHeight) size = PlayerLandingDustSize.Medium;
            else size = PlayerLandingDustSize.Large;

            // 완충판 위에서는 한 단계 작게.
            return landing.StableLanding ? (PlayerLandingDustSize)((int)size - 1) : size;
        }

        /// <summary>작게 2~3, 중간 4~5, 최대 6~8(피해 착지는 8).</summary>
        public static int ResolveDustCount(PlayerLandingDustSize size, in PlayerLandingInfo landing)
        {
            float d = landing.Distance;
            switch (size)
            {
                case PlayerLandingDustSize.Small:
                    return d < 0.65f ? 2 : 3;
                case PlayerLandingDustSize.Medium:
                    return d < 2.5f ? 4 : 5;
                case PlayerLandingDustSize.Large:
                    if (landing.Damaged) return 8;
                    return 6 + Mathf.Clamp(Mathf.FloorToInt((d - LargeDustHeight) / 2.5f), 0, 2);
                default:
                    return 0;
            }
        }

        /// <summary>
        /// 3칸 이상, 피해 없음, 완충판·사다리 아님, 디버그 억제·줄인 움직임 아님일 때만 아주 짧고 약하게 흔든다.
        /// </summary>
        public static bool TryResolveShake(in PlayerLandingInfo landing, bool suppressed, bool reduceMotion,
            out float amplitude, out float duration)
        {
            amplitude = 0f;
            duration = 0f;
            if (suppressed || reduceMotion || !landing.CanActAfter || landing.Damaged
                || landing.StableLanding || landing.LadderExempt || landing.Distance < ShakeMinHeight)
            {
                return false;
            }

            float t = Mathf.Clamp01((landing.Distance - ShakeMinHeight) / (ShakeFullHeight - ShakeMinHeight));
            amplitude = Mathf.Lerp(ShakeMinAmplitude, ShakeMaxAmplitude, t);
            duration = ShakeDuration;
            return true;
        }
    }
}

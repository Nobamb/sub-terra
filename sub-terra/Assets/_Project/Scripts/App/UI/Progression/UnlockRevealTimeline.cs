using System.Collections.Generic;
using SubTerra.App.Audio;
using SubTerra.Shared.Audio;
using UnityEngine;

namespace SubTerra.App.UI.Progression
{
    /// <summary>자물쇠 한 장면의 자세. Visible이 false면 자물쇠를 끈다.</summary>
    public readonly struct UnlockLockPose
    {
        public readonly Vector2 Offset;
        public readonly float Angle;
        public readonly float Scale;
        public readonly float ShackleLift;
        public readonly float Alpha;
        public readonly bool Visible;

        public UnlockLockPose(Vector2 offset, float angle, float scale, float shackleLift, float alpha, bool visible)
        {
            Offset = offset;
            Angle = angle;
            Scale = scale;
            ShackleLift = shackleLift;
            Alpha = alpha;
            Visible = visible;
        }
    }

    /// <summary>
    /// B-169: 업그레이드 노드와 심층 구역 썸네일이 함께 쓰는 자물쇠 해금 시간표(u: 0~1).
    /// 0~0.2 흔들림 → 0.2~0.3 딸깍(고리 들림) → 0.3~0.65 떨어지며 기울고 흐려짐 → 0.65~1 빛.
    /// 소리는 각 단계를 넘는 프레임에 한 번 낸다(프레임이 튀어도 한 번).
    /// </summary>
    public static class UnlockRevealTimeline
    {
        public const float ShakeEnd = 0.2f;
        public const float ClickEnd = 0.3f;
        public const float FallEnd = 0.65f;

        public const float RattleAt = 0f;
        public const float ClickAt = ShakeEnd;
        public const float GlowAt = FallEnd;

        /// <summary>여러 노드가 한꺼번에 풀릴 때 같은 소리 최소 간격(초).</summary>
        public const float SameCueSeconds = 0.1f;
        /// <summary>한 번에 풀린 노드 중 소리를 내는 노드 수. 나머지는 연출만.</summary>
        public const int MaxAudibleNodes = 3;

        /// <summary>시작 전 진행값. 0에 있는 흔들림 소리를 첫 프레임에 넘기기 위해 0보다 작다.</summary>
        public const float BeforeStart = -1f;

        /// <param name="shackleKick">딸깍 때 고리가 들리는 거리(px)</param>
        /// <param name="drop">떨어지는 거리(px)</param>
        /// <param name="dropAngle">떨어지며 기우는 각도(도)</param>
        /// <param name="drift">떨어지며 옆으로 밀리는 거리(px)</param>
        public static UnlockLockPose SampleLock(float u, float shackleKick, float drop, float dropAngle, float drift)
        {
            if (u < ShakeEnd)
            {
                var p = u / ShakeEnd;
                return new UnlockLockPose(Vector2.zero, Mathf.Sin(p * Mathf.PI * 8f) * 12f * (0.5f + p * 0.5f), 1f, 0f, 1f, true);
            }

            if (u < ClickEnd)
            {
                var p = (u - ShakeEnd) / 0.1f;
                return new UnlockLockPose(Vector2.zero, 0f, 1f + 0.1f * UpgradeTreeTween.Pulse(p), shackleKick * UpgradeTreeTween.EaseOut(p), 1f, true);
            }

            // 썸네일 그림이 이전과 같도록 구간 길이는 리터럴(0.1·0.35)로 나눈다.
            var q = Mathf.Clamp01((u - ClickEnd) / 0.35f);
            var fall = q * q;
            return new UnlockLockPose(
                new Vector2(drift * fall, -drop * fall),
                -dropAngle * fall,
                1f,
                shackleKick,
                1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.35f, 1f, q)),
                u < FallEnd);
        }

        /// <summary>소리가 나는 진행값(u). 해금 소리가 아니면 0.</summary>
        public static float CueTime(SfxId id)
        {
            switch (id)
            {
                case SfxId.UnlockClick:
                    return ClickAt;
                case SfxId.UnlockGlow:
                    return GlowAt;
                default:
                    return RattleAt;
            }
        }

        /// <summary>previous → now 사이에 넘은 소리 시점을 순서대로 담는다.</summary>
        public static void CollectCues(float previous, float now, List<SfxId> cues)
        {
            if (cues == null)
            {
                return;
            }

            if (SfxPlayback.Crossed(previous, now, RattleAt)) cues.Add(SfxId.UnlockRattle);
            if (SfxPlayback.Crossed(previous, now, ClickAt)) cues.Add(SfxId.UnlockClick);
            if (SfxPlayback.Crossed(previous, now, GlowAt)) cues.Add(SfxId.UnlockGlow);
        }
    }
}

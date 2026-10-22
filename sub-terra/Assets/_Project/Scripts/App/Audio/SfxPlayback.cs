using UnityEngine;

namespace SubTerra.App.Audio
{
    /// <summary>한 번의 재생이 어디서 시작해 얼마나 오래, 어떻게 줄어들며 끝나는지 정한 결과.</summary>
    public readonly struct SfxPlaybackPlan
    {
        /// <summary>클립 안에서 재생을 시작할 위치(초).</summary>
        public readonly float StartTime;
        /// <summary>실제로 흐르는 시간(초). 피치가 높으면 클립 길이보다 짧다.</summary>
        public readonly float RealDuration;
        /// <summary>끝에서부터 음량이 0으로 내려가는 시간(초).</summary>
        public readonly float FadeOut;

        public SfxPlaybackPlan(float startTime, float realDuration, float fadeOut)
        {
            StartTime = startTime;
            RealDuration = realDuration;
            FadeOut = fadeOut;
        }
    }

    /// <summary>재생 구간·페이드 계산. 엔진 상태에 기대지 않아 EditMode에서 그대로 검증한다.</summary>
    public static class SfxPlayback
    {
        public const float MinPitch = 0.1f;
        public const float MaxPitch = 3f;

        /// <param name="clipLength">클립 전체 길이(초)</param>
        /// <param name="startTime">재생 시작 위치(초)</param>
        /// <param name="maxDuration">클립 시간 기준 최대 재생 길이(초). 0 이하면 끝까지.</param>
        /// <param name="fadeOut">끝 페이드아웃(초)</param>
        /// <param name="pitch">최종 피치</param>
        /// <param name="realTimeLimit">0보다 크면 이 시간 안에 끝낸다(경보 반복이 다음 소리를 덮지 않게).</param>
        public static SfxPlaybackPlan Plan(
            float clipLength,
            float startTime,
            float maxDuration,
            float fadeOut,
            float pitch,
            float realTimeLimit = 0f)
        {
            pitch = Mathf.Clamp(pitch, MinPitch, MaxPitch);
            var start = Mathf.Clamp(startTime, 0f, Mathf.Max(0f, clipLength - 0.01f));
            var clipSpan = Mathf.Max(0f, clipLength - start);
            if (maxDuration > 0f)
            {
                clipSpan = Mathf.Min(clipSpan, maxDuration);
            }

            var real = clipSpan / pitch;
            if (realTimeLimit > 0f)
            {
                real = Mathf.Min(real, realTimeLimit);
            }

            // 페이드가 길이의 절반을 넘으면 소리가 시작부터 줄어들므로 절반까지만 쓴다.
            var fade = Mathf.Clamp(fadeOut, 0f, real * 0.5f);
            return new SfxPlaybackPlan(start, real, fade);
        }

        /// <summary>경과 시간에 맞는 페이드 배율(0~1).</summary>
        public static float FadeGain(float elapsed, float duration, float fadeOut)
        {
            if (fadeOut <= 0f)
            {
                return 1f;
            }

            var remaining = duration - elapsed;
            return remaining >= fadeOut ? 1f : Mathf.Clamp01(remaining / fadeOut);
        }

        public static bool IsThrottled(float now, float lastPlayedAt, float minInterval)
        {
            return now - lastPlayedAt < minInterval;
        }
    }
}

using UnityEngine;

namespace SubTerra.App.Integration
{
    /// <summary>
    /// 미니맵 전체 투명도. 기본은 반투명(50%)이고 Ctrl+M으로 불투명과 번갈아 바뀐다.
    /// 전환은 0.3초 안에 끝나며 중간에 다시 눌러도 현재 값에서 이어진다. unscaled 시간만 받는다.
    /// </summary>
    public sealed class MinimapOpacity
    {
        public const float TranslucentAlpha = 0.5f;
        public const float OpaqueAlpha = 1f;
        public const float FadeSeconds = 0.3f;

        private float progress;

        /// <summary>true면 불투명을 향하거나 불투명 상태.</summary>
        public bool IsOpaque { get; private set; }
        public bool IsFading => progress > 0f && progress < 1f;
        public float Alpha => Mathf.Lerp(TranslucentAlpha, OpaqueAlpha, MinimapBoardTimeline.EaseOut(progress));

        public bool Toggle()
        {
            IsOpaque = !IsOpaque;
            return IsOpaque;
        }

        public void Advance(float unscaledDeltaTime)
        {
            float step = Mathf.Max(0f, unscaledDeltaTime) / FadeSeconds;
            progress = Mathf.Clamp01(progress + (IsOpaque ? step : -step));
        }

        public void Reset()
        {
            IsOpaque = false;
            progress = 0f;
        }
    }
}

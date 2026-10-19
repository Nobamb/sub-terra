using System;
using UnityEngine;

namespace SubTerra.App.UI.MainMenu
{
    [Serializable]
    public sealed class MainMenuExitSettings
    {
        [Tooltip("선택 카드가 번쩍이는 시간")]
        public float flashDuration = 0.10f;
        [Tooltip("화면이 가로선으로 접히는 시간")]
        public float foldDuration = 0.22f;
        [Tooltip("가로선이 점으로 줄어드는 시간")]
        public float lineCollapseDuration = 0.08f;
    }

    public readonly struct MainMenuExitFrame
    {
        public float CardFlash { get; }
        /// <summary>1이면 화면 전체가 보이고 0이면 가로선 하나로 접힌 상태.</summary>
        public float Open { get; }
        public float LineWidth { get; }
        public float LineAlpha { get; }
        public bool IsComplete { get; }

        public MainMenuExitFrame(float cardFlash, float open, float lineWidth, float lineAlpha, bool isComplete)
        {
            CardFlash = cardFlash;
            Open = open;
            LineWidth = lineWidth;
            LineAlpha = lineAlpha;
            IsComplete = isComplete;
        }
    }

    /// <summary>B-150 이어하기/새 게임 뒤 연출: 카드 번쩍 → TV 꺼지듯 가로선으로 접힘(약 0.4초).</summary>
    public sealed class MainMenuExitTimeline
    {
        private readonly MainMenuExitSettings settings;
        private float elapsed;

        public MainMenuExitTimeline(MainMenuExitSettings settings = null)
        {
            this.settings = settings ?? new MainMenuExitSettings();
        }

        public float TotalDuration => settings.flashDuration + settings.foldDuration + settings.lineCollapseDuration;
        public float Elapsed => elapsed;
        public bool IsComplete => elapsed >= TotalDuration;

        public void Advance(float deltaSeconds)
        {
            if (deltaSeconds > 0f) elapsed = Mathf.Min(elapsed + deltaSeconds, TotalDuration);
        }

        public void Restart() => elapsed = 0f;

        public MainMenuExitFrame Sample()
        {
            var t = elapsed;
            var foldStart = settings.flashDuration * 0.5f;
            var foldEnd = foldStart + settings.foldDuration;
            var flash = 1f - MainMenuBootTimeline.Range(t, 0f, settings.flashDuration);
            var fold = MainMenuBootTimeline.Range(t, foldStart, foldEnd);
            var open = 1f - fold * fold;
            var collapse = MainMenuBootTimeline.Range(t, foldEnd, TotalDuration);
            var lineAlpha = fold <= 0f ? 0f : 1f - collapse * collapse;
            return new MainMenuExitFrame(flash, open, 1f - MainMenuBootTimeline.EaseOut(collapse), lineAlpha, IsComplete);
        }
    }
}

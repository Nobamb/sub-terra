using System;
using System.Collections;
using SubTerra.Shared;
using UnityEngine;

namespace SubTerra.App.UI.EmergencyEscape
{
    /// <summary>
    /// B-197: 광산 타이머 만료 시 지상으로 올라가는 글리치 암전 진행기.
    /// 구출 글리치와 같은 EscapeWarpTimeline/EscapeWarpOverlay/EscapeWarpSettings를 그대로 쓴다.
    /// 화면이 완전히 검은 프레임에서 onBlack(월드 교체·지상 씬 로드·저장)을 돌리고, 그동안 시간표는 멈춘다.
    /// 오버레이는 씬이 바뀌어도 켜짐이 끝날 때까지 남는다. 카메라·플레이어는 건드리지 않는다.
    /// </summary>
    public sealed class MineTimeoutWarpSequence
    {
        private readonly EscapeWarpSettings settings;
        private readonly Func<IEnumerator> onBlack;
        private readonly bool reduceMotion;

        private EscapeWarpOverlay overlay;
        private bool blackRan;
        private bool finished;

        public bool ReduceMotion => reduceMotion;
        public bool IsFinished => finished;
        public bool BlackRan => blackRan;
        public EscapeWarpOverlay Overlay => overlay;

        public MineTimeoutWarpSequence(
            EscapeWarpSettings settings,
            Func<IEnumerator> onBlack,
            bool? reduceMotionOverride = null)
        {
            this.settings = settings ?? new EscapeWarpSettings();
            this.onBlack = onBlack;
            reduceMotion = reduceMotionOverride ?? AccessibilityPreferences.ReduceMotion;
        }

        public IEnumerator Run()
        {
            var timeline = new EscapeWarpTimeline(settings, reduceMotion);
            overlay = new EscapeWarpOverlay(settings);
            overlay.PersistAcrossScenes();
            overlay.Apply(timeline.Evaluate(0f));
            try
            {
                while (!timeline.IsComplete)
                {
                    var step = Mathf.Clamp(Time.unscaledDeltaTime, 0.001f, 0.1f);
                    var frame = timeline.Advance(step);
                    overlay.Apply(frame);

                    if (frame.ShouldTeleport)
                    {
                        // 화면이 완전히 검다. 지상이 열릴 때까지 이 프레임에서 멈춘 채 기다린다.
                        blackRan = true;
                        if (onBlack != null)
                        {
                            var inner = onBlack();
                            while (inner != null && inner.MoveNext())
                            {
                                yield return inner.Current;
                            }
                        }
                    }

                    yield return null;
                }
            }
            finally
            {
                Cleanup();
            }
        }

        /// <summary>연출 도중 중단될 때(코루틴 정지 등) 오버레이만 정리한다.</summary>
        public void Abort()
        {
            Cleanup();
        }

        private void Cleanup()
        {
            if (finished)
            {
                return;
            }

            finished = true;
            if (overlay != null)
            {
                overlay.Destroy();
                overlay = null;
            }
        }
    }
}

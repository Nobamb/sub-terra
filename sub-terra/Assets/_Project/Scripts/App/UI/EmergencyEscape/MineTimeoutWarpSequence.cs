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
        private const float NominalFrameSeconds = 1f / 60f;
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
            EscapeGlitchSfx.Play(reduceMotion);
            var afterBlack = false;
            try
            {
                while (!timeline.IsComplete)
                {
                    // 로드·저장으로 길어진 첫 프레임 간격은 켜짐 연출에서 빼, 글리치가 잘리지 않고 끝까지 재생되게 한다.
                    var step = afterBlack
                        ? NominalFrameSeconds
                        : Mathf.Clamp(Time.unscaledDeltaTime, 0.001f, 0.1f);
                    afterBlack = false;
                    var frame = timeline.Advance(step);
                    overlay.Apply(frame);

                    if (frame.ShouldTeleport)
                    {
                        // 화면이 완전히 검다. 지상이 열릴 때까지 깨끗한 검정으로 기다린다.
                        // 이동 프레임의 켜짐 글리치를 그대로 두면 로드 내내 한 장면으로 굳어 보이므로 지운다.
                        // 글리치는 켜짐이 시작되는 다음 프레임부터 끊김 없이 이어져 가라앉는다.
                        overlay.Apply(new EscapeWarpFrame(
                            frame.Phase, frame.ElapsedSeconds, 1f, 0f, 0, true, frame.IsComplete));
                        blackRan = true;
                        afterBlack = true;
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

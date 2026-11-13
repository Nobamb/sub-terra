using System;
using System.Collections;
using SubTerra.App.UI.EmergencyEscape;
using SubTerra.Gameplay.Player;
using SubTerra.Shared;
using UnityEngine;

namespace SubTerra.App.Integration
{
    public enum EscapeWarpStyle
    {
        /// <summary>비상 탈출(전력 고갈 구출): 암전 + 간단 글리치 → 검은 화면에서 이동 → 켜지면 도착.</summary>
        Glitch = 0,
        /// <summary>긴급 탈출 포탈: 검은 전체화면 + 웜홀에 빨려 듦 → 암전 한가운데서 이동 → 목적지 웜홀에서 나옴.</summary>
        Wormhole = 1
    }

    /// <summary>
    /// 구출(글리치)과 포탈(웜홀)이 같이 쓰는 워프 진행기. 두 흐름 모두 카메라를 움직이지 않는다.
    /// 화면이 완전히 검은 동안 플레이어를 옮기고 카메라를 즉시 맞춰, 다시 켜질 때 이미 목적지에 도착해 있다.
    /// 결제·정지 소유는 호출자가 맡고, 이 클래스는 연출과 "도착 시 한 번" 콜백만 책임진다.
    /// 모션 감소가 켜져 있으면 스타일과 상관없이 페이드 아웃 → 도착 → 페이드 인만 한다.
    /// </summary>
    public sealed class EscapeWarpRunner
    {
        private readonly EscapeWarpSettings settings;
        private readonly EscapeWarpStyle style;
        private readonly PlayerCameraFollow follow;
        private readonly Transform player;
        private readonly Action arrive;
        private readonly bool reduceMotion;

        private EscapeWarpOverlay glitchOverlay;
        private EscapeWormholeWarpOverlay wormholeOverlay;
        private EscapeWarpPlayerProxy playerProxy;
        private bool arrived;
        private bool finished;

        public bool IsFinished => finished;
        public bool HasArrived => arrived;
        public bool ReduceMotion => reduceMotion;
        public EscapeWarpStyle Style => style;

        public EscapeWarpRunner(
            EscapeWarpSettings settings,
            EscapeWarpStyle style,
            PlayerCameraFollow follow,
            Transform player,
            Action arrive,
            bool? reduceMotionOverride = null)
        {
            this.settings = settings ?? new EscapeWarpSettings();
            this.style = style;
            this.follow = follow;
            this.player = player;
            this.arrive = arrive;
            reduceMotion = reduceMotionOverride ?? AccessibilityPreferences.ReduceMotion;
        }

        public static PlayerCameraFollow ResolveFollow()
        {
            var main = Camera.main;
            if (main != null)
            {
                var onMain = main.GetComponent<PlayerCameraFollow>();
                if (onMain != null)
                {
                    return onMain;
                }
            }

            return Application.isPlaying
                ? UnityEngine.Object.FindAnyObjectByType<PlayerCameraFollow>(FindObjectsInactive.Exclude)
                : null;
        }

        public IEnumerator Run()
        {
            // 안쪽 흐름을 직접 펼쳐 돌린다. 코루틴 밖(테스트)에서도 한 번의 MoveNext마다 한 프레임씩 진행된다.
            var inner = reduceMotion || style == EscapeWarpStyle.Glitch
                ? RunDarkWarp()
                : RunWormholeWarp();
            try
            {
                while (inner.MoveNext())
                {
                    yield return inner.Current;
                }
            }
            finally
            {
                Cleanup();
            }
        }

        /// <summary>연출 도중 장면이 내려갈 때. 이미 낸 비용만 잃지 않게 도착 처리를 한 번 하고 정리한다.</summary>
        public void Abort()
        {
            if (finished)
            {
                return;
            }

            RaiseArrive();
            Cleanup();
        }

        // 암전(+글리치) → 완전히 검은 화면에서 이동 → 켜짐. 모션 감소는 글리치만 빠진다.
        private IEnumerator RunDarkWarp()
        {
            var timeline = new EscapeWarpTimeline(settings, reduceMotion);
            glitchOverlay = new EscapeWarpOverlay(settings);
            glitchOverlay.Apply(timeline.Evaluate(0f));

            while (!timeline.IsComplete)
            {
                var step = Mathf.Clamp(Time.unscaledDeltaTime, 0.001f, 0.1f);
                var frame = timeline.Advance(step);
                glitchOverlay.Apply(frame);

                if (frame.ShouldTeleport)
                {
                    // 화면이 완전히 검다. 옮긴 뒤 카메라를 즉시 맞춰 켜질 때 도착 장면이 보이게 한다.
                    ReleasePreview();
                    RaiseArrive();
                    SnapCamera();
                }

                yield return null;
            }
        }

        // 검은 전체화면 + 웜홀에 빨려 듦 → 암전 한가운데서 이동 → 암전이 걷히며 목적지 웜홀에서 나옴.
        private IEnumerator RunWormholeWarp()
        {
            var timeline = new EscapeWormholeWarpTimeline(settings);
            ReleasePreview();
            SnapCamera();
            var view = ResolveViewCamera();
            playerProxy = new EscapeWarpPlayerProxy(player, view);
            wormholeOverlay = new EscapeWormholeWarpOverlay();
            wormholeOverlay.SetPlayerProxy(playerProxy.Sprites);
            // 복제 그림이 같은 자리·같은 크기로 이어받으므로 눈에 띄는 변화는 없다.
            playerProxy.HideRenderers();

            while (true)
            {
                var step = Mathf.Clamp(Time.unscaledDeltaTime, 0.001f, 0.1f);
                var frame = timeline.Advance(step);

                if (frame.ShouldTeleport)
                {
                    RaiseArrive();
                    SnapCamera();
                }

                ApplyWormhole(frame, view, step);
                if (frame.IsComplete)
                {
                    break;
                }

                yield return null;
            }
        }

        private void ApplyWormhole(EscapeWormholeWarpFrame frame, Camera view, float step)
        {
            if (wormholeOverlay == null)
            {
                return;
            }

            var screen = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            var diameter = Screen.height * 0.2f;
            if (view != null)
            {
                var center = playerProxy != null ? playerProxy.WorldCenter : Vector3.zero;
                var projected = view.WorldToScreenPoint(center);
                var edge = view.WorldToScreenPoint(center + Vector3.right);
                screen = new Vector2(projected.x, projected.y);
                diameter = Mathf.Abs(edge.x - projected.x) * settings.wormholeWorldDiameter;
            }

            wormholeOverlay.Apply(frame, screen, diameter, step);
        }

        private Camera ResolveViewCamera()
        {
            if (follow != null)
            {
                var own = follow.GetComponent<Camera>();
                if (own != null)
                {
                    return own;
                }
            }

            return Camera.main;
        }

        private void ReleasePreview()
        {
            if (follow != null)
            {
                follow.ClearPreview(snapImmediately: true);
            }
        }

        /// <summary>카메라를 플레이어에게 즉시 맞춘다(이동 없음, 잔류 속도 제거).</summary>
        private void SnapCamera()
        {
            if (follow != null)
            {
                follow.SnapToTarget();
            }
        }

        private void RaiseArrive()
        {
            if (arrived)
            {
                return;
            }

            arrived = true;
            arrive?.Invoke();
        }

        private void Cleanup()
        {
            if (finished)
            {
                return;
            }

            finished = true;
            if (playerProxy != null)
            {
                playerProxy.Restore();
                playerProxy = null;
            }

            if (glitchOverlay != null)
            {
                glitchOverlay.Destroy();
                glitchOverlay = null;
            }

            if (wormholeOverlay != null)
            {
                wormholeOverlay.Destroy();
                wormholeOverlay = null;
            }
        }
    }
}

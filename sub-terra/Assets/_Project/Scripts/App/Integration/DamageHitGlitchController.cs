using System;
using SubTerra.App.UI.DamageFeedback;
using SubTerra.Gameplay.Player;
using SubTerra.Shared;
using UnityEngine;

namespace SubTerra.App.Integration
{
    /// <summary>
    /// B-207: PlayerSurvivalController.DamageApplied를 받아 낙하·붕괴 피해에만 짧은 붉은 글리치·틴트를 켠다.
    /// 가스(기존 가스 시야만)·산소 0 틱은 켜지 않는다. 기존 흔들림·깜빡임·피격음은 건드리지 않는다.
    /// </summary>
    public sealed class DamageHitGlitchController : MonoBehaviour
    {
        [SerializeField] private DamageHitGlitchSettings settings = new DamageHitGlitchSettings();

        private PlayerSurvivalController survival;
        private DamageHitGlitchTimeline timeline;
        private DamageHitGlitchOverlay overlay;

        public DamageHitGlitchSettings Settings => settings;
        public DamageHitGlitchOverlay Overlay => overlay;
        public bool IsPlaying => timeline != null && timeline.IsRunning;

        public void Bind(PlayerSurvivalController target)
        {
            Unbind();
            survival = target;
            if (survival != null)
            {
                survival.DamageApplied += OnDamageApplied;
            }
        }

        public void Unbind()
        {
            if (survival != null)
            {
                survival.DamageApplied -= OnDamageApplied;
            }

            survival = null;
        }

        /// <summary>테스트와 LateUpdate가 같이 쓰는 한 프레임 진행.</summary>
        public DamageHitGlitchFrame Tick(float now)
        {
            EnsureTimeline();
            var frame = timeline.Evaluate(now, AccessibilityPreferences.ReduceMotion);
            if (overlay != null)
            {
                overlay.Apply(frame);
            }

            return frame;
        }

        /// <summary>재생을 멈추고 오버레이를 지운다.</summary>
        public void Clear()
        {
            if (timeline != null)
            {
                timeline.Stop();
            }

            if (overlay != null)
            {
                overlay.Destroy();
                overlay = null;
            }
        }

        private void OnDamageApplied(RunFailureCause cause, float appliedDamage, bool becameIncapacitated)
        {
            try
            {
                EnsureTimeline();
                if (appliedDamage <= 0f || !timeline.TryStart(cause, Time.unscaledTime))
                {
                    return;
                }

                // 치명타여도 같은 짧은 길이로만 켜고, 위층의 사망 연출이 그대로 이어진다.
                overlay ??= new DamageHitGlitchOverlay(settings);
                Tick(Time.unscaledTime);
            }
            catch (Exception exception)
            {
                Debug.LogError("[DamageHitGlitchController] 피해 글리치를 시작하지 못했습니다: " + exception);
            }
        }

        private void LateUpdate()
        {
            if (timeline != null && timeline.IsRunning)
            {
                Tick(Time.unscaledTime);
            }
        }

        private void OnDisable()
        {
            if (timeline != null)
            {
                timeline.Stop();
            }

            if (overlay != null)
            {
                overlay.Apply(DamageHitGlitchFrame.Inactive);
            }
        }

        private void OnDestroy()
        {
            Unbind();
            Clear();
        }

        private void EnsureTimeline()
        {
            settings ??= new DamageHitGlitchSettings();
            timeline ??= new DamageHitGlitchTimeline(settings);
        }
    }
}

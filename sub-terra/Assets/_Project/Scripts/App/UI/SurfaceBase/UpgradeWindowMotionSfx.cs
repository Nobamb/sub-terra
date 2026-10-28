using System;
using SubTerra.App.Audio;
using SubTerra.Shared.Audio;
using UnityEngine;

namespace SubTerra.App.UI.SurfaceBase
{
    /// <summary>
    /// B-169: 업그레이드 창 자물쇠 연출(B-167) 훅에 소리를 붙인다. 모션 하나에 한 번만 구독하며,
    /// 지상 기지(SurfaceBaseView)와 광산(PanelToggleController)이 모션을 만드는 곳에서 Attach한다.
    /// 훅은 시간표 시점을 넘는 프레임에 한 번씩만 나므로 되감기·열다 닫기에서 지난 소리는 다시 나지 않는다.
    /// 모션 감소면 자물쇠 훅이 없고, 대신 바로 열리고 닫힐 때 다른 창과 같은 TV 켜짐·꺼짐을 낸다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UpgradeWindowMotionSfx : MonoBehaviour
    {
        /// <summary>연타해도 같은 소리는 이 간격 안에 한 번만.</summary>
        public const float SameSoundSeconds = 0.1f;

        private readonly SfxRepeatGate gate = new SfxRepeatGate(SameSoundSeconds);
        private UpgradeWindowMotion motion;

        public UpgradeWindowMotion Motion => motion;

        /// <summary>테스트용 시계(같은 소리 0.1초 제한).</summary>
        public Func<float> Clock
        {
            get => gate.Clock;
            set => gate.Clock = value ?? (() => Time.realtimeSinceStartup);
        }

        public static UpgradeWindowMotionSfx Attach(UpgradeWindowMotion target)
        {
            if (target == null)
            {
                return null;
            }

            try
            {
                var sfx = target.GetComponent<UpgradeWindowMotionSfx>();
                if (sfx == null)
                {
                    sfx = target.gameObject.AddComponent<UpgradeWindowMotionSfx>();
                }

                sfx.Bind(target);
                return sfx;
            }
            catch (Exception exception)
            {
                Debug.LogError("[SubTerra] 업그레이드 창 효과음 연결 실패: " + exception);
                return null;
            }
        }

        public void Bind(UpgradeWindowMotion target)
        {
            if (motion == target)
            {
                return;
            }

            Unbind();
            motion = target;
            if (motion == null)
            {
                return;
            }

            motion.ShakeStarted += OnShakeStarted;
            motion.CrackStarted += OnCrackStarted;
            motion.Shattered += OnShattered;
            motion.CloseStarted += OnCloseStarted;
            motion.FragmentsRegathered += OnFragmentsRegathered;
            motion.OpenCompleted += OnOpenCompleted;
            motion.CloseCompleted += OnCloseCompleted;
        }

        private void Unbind()
        {
            if (motion == null)
            {
                return;
            }

            motion.ShakeStarted -= OnShakeStarted;
            motion.CrackStarted -= OnCrackStarted;
            motion.Shattered -= OnShattered;
            motion.CloseStarted -= OnCloseStarted;
            motion.FragmentsRegathered -= OnFragmentsRegathered;
            motion.OpenCompleted -= OnOpenCompleted;
            motion.CloseCompleted -= OnCloseCompleted;
            motion = null;
        }

        private void OnDestroy()
        {
            Unbind();
        }

        private void OnShakeStarted() => gate.TryPlay(SfxId.UnlockRattle);

        private void OnCrackStarted()
        {
            gate.TryPlay(SfxId.UnlockClick);
            // 다시 열기는 금 가는 시간이 0.12초뿐이라 처음 열기에만 겹친다.
            if (motion != null && motion.IsFullOpen)
            {
                gate.TryPlay(SfxId.UpgradeLockCrack);
            }
        }

        private void OnShattered() => gate.TryPlay(SfxId.UpgradeLockShatter);

        private void OnCloseStarted() => gate.TryPlay(SfxId.UpgradeWindowSuckIn);

        private void OnFragmentsRegathered() => gate.TryPlay(SfxId.UpgradeLockRegather);

        // 연출 끝·즉시 끄고 켜기(CompleteImmediately)는 무음. 모션 감소 경로에서만 TV 소리를 낸다.
        private void OnOpenCompleted()
        {
            if (motion != null && motion.IsReducedMotionTransition)
            {
                WindowTvSfx.Play(SfxId.WindowTvOpen);
            }
        }

        private void OnCloseCompleted()
        {
            if (motion != null && motion.IsReducedMotionTransition)
            {
                WindowTvSfx.Play(SfxId.WindowTvClose);
            }
        }
    }
}

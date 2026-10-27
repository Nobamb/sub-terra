using System;
using SubTerra.App.Audio;
using SubTerra.Shared;
using SubTerra.Shared.Audio;
using UnityEngine;

namespace SubTerra.App.Integration
{
    /// <summary>
    /// B-168: 체력 30% 경고음 판정. 기준은 ExplorationBgmController와 같은 Current &lt;= Maximum × 0.3.
    /// 체력이 줄어 처음 넘을 때 피격음 0.25초 뒤 한 번, 30% 위로 회복해야 다시 낼 수 있다.
    /// 첫 관측(씬 진입·불러오기)과 최대 체력만 바뀐 경우는 무음, 기다리는 동안 사망·실패 처리면 취소한다.
    /// </summary>
    public sealed class LowHealthSfxGate
    {
        public const float LowHealthRatio = 0.3f;
        public const float DelaySeconds = 0.25f;

        private bool observed;
        private bool warned;
        private float lastCurrent;
        private float pendingAt = -1f;

        public bool IsPending => pendingAt >= 0f;

        public void Observe(PlayerHealthReadModel health, float now)
        {
            var low = health.Current <= health.Maximum * LowHealthRatio;
            if (!observed)
            {
                observed = true;
                warned = low;
                lastCurrent = health.Current;
                return;
            }

            var dropped = health.Current < lastCurrent;
            lastCurrent = health.Current;
            if (!low)
            {
                warned = false;
                pendingAt = -1f;
                return;
            }

            if (health.Current <= 0f)
            {
                pendingAt = -1f;
                warned = true;
                return;
            }

            if (!warned && dropped)
            {
                warned = true;
                pendingAt = now + DelaySeconds;
            }
        }

        /// <returns>지금 경고음을 낼 차례면 true.</returns>
        public bool Tick(float now, bool cancelled)
        {
            if (pendingAt < 0f)
            {
                return false;
            }

            if (cancelled)
            {
                pendingAt = -1f;
                return false;
            }

            if (now < pendingAt)
            {
                return false;
            }

            pendingAt = -1f;
            return true;
        }

        public void Reset()
        {
            observed = false;
            warned = false;
            pendingAt = -1f;
        }
    }

    /// <summary>B-168: 체력 변화를 구독해 LowHealthSfxGate가 고른 순간에 LowHealthWarning(World 버스)을 낸다.</summary>
    public sealed class LowHealthWarningAudio : MonoBehaviour
    {
        private readonly LowHealthSfxGate gate = new LowHealthSfxGate();
        private IPlayerHealthSource source;
        private Func<bool> isCancelled = () => false;

        public LowHealthSfxGate Gate => gate;

        public static LowHealthWarningAudio Attach(GameObject host, IPlayerHealthSource health, Func<bool> cancelled)
        {
            if (host == null || health == null)
            {
                return null;
            }

            var audio = host.GetComponent<LowHealthWarningAudio>();
            if (audio == null)
            {
                audio = host.AddComponent<LowHealthWarningAudio>();
            }

            audio.Bind(health, cancelled);
            return audio;
        }

        public void Bind(IPlayerHealthSource health, Func<bool> cancelled)
        {
            Unbind();
            gate.Reset();
            source = health;
            isCancelled = cancelled ?? (() => false);
            Subscribe();
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unbind();
            gate.Reset();
        }

        private void Subscribe()
        {
            if (source == null || !isActiveAndEnabled)
            {
                return;
            }

            source.HealthChanged -= OnHealthChanged;
            source.HealthChanged += OnHealthChanged;
            OnHealthChanged(source.GetHealth());
        }

        private void Unbind()
        {
            if (source != null)
            {
                source.HealthChanged -= OnHealthChanged;
            }
        }

        private void OnHealthChanged(PlayerHealthReadModel health)
        {
            gate.Observe(health, Time.unscaledTime);
        }

        private void Update()
        {
            if (!gate.IsPending)
            {
                return;
            }

            try
            {
                if (gate.Tick(Time.unscaledTime, isCancelled()))
                {
                    SfxPlayer.Play(SfxId.LowHealthWarning);
                }
            }
            catch (Exception exception)
            {
                gate.Reset();
                Debug.LogWarning("[LowHealthWarningAudio] 체력 경고음 갱신 실패: " + exception.Message);
            }
        }
    }
}

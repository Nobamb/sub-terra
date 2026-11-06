using System;
using SubTerra.App.Audio;
using SubTerra.Gameplay.Power;
using SubTerra.Shared.Audio;
using UnityEngine;

namespace SubTerra.App.Integration
{
    /// <summary>
    /// B-186(#292): 시설 전력 켜짐 연출(PowerOnEffect)에 짧은 정전기 소리 하나를 붙인다.
    /// 범위 판정 한 번에 한 번만 오는 BatchStarted만 듣는다. 복원 중에는 연출 자체가 없어 소리도 없다.
    /// </summary>
    public sealed class PowerOnSfx : IDisposable
    {
        public const SfxId PowerOnSfxId = SfxId.PowerOnStatic;
        public const float PowerOnMinGap = 0.25f;               // ★ 연달아 켜질 때 이 시간 안이면 생략
        public const float PowerOnStaticCctvGap = 0.15f;        // ★ CCTV 연결음 직후 이 시간 안이면 줄여서 낸다
        public const float PowerOnStaticDuckedVolume = 0.5f;    // ★ 줄여 낼 때 음량 배율
        public const SfxId CctvConnectSfx = SfxId.UiConfirm;    // CoreCctvPopupView 케이블 연결 성공음

        private readonly Func<SfxId, float> secondsSinceLastPlayed;
        private readonly Func<float> clock;
        private readonly Action<SfxId, float> play;
        private readonly Action<SfxId, float, float> playScaled;
        private float lastPlayed = float.NegativeInfinity;
        private bool bound;

        public PowerOnSfx(Func<SfxId, float> secondsSinceLastPlayed = null, Func<float> clock = null,
            Action<SfxId, float> play = null, Action<SfxId, float, float> playScaled = null)
        {
            this.secondsSinceLastPlayed = secondsSinceLastPlayed ?? SecondsSinceLastPlayed;
            this.clock = clock ?? (() => Time.unscaledTime);
            this.play = play ?? SfxPlayer.Play;
            this.playScaled = playScaled ?? SfxPlayer.Play;
        }

        public void Bind()
        {
            if (bound) return;
            PowerOnEffect.BatchStarted += HandleBatchStarted;
            bound = true;
        }

        public void Unbind()
        {
            if (!bound) return;
            PowerOnEffect.BatchStarted -= HandleBatchStarted;
            bound = false;
        }

        public void HandleBatchStarted(int count, Vector3 center)
        {
            // 불러오기·이어하기는 연출이 시작되지 않지만, 혹시 이벤트가 와도 무음으로 둔다.
            if (count <= 0 || PowerOnEffect.IsSuppressed) return;
            float now = clock();
            if (now - lastPlayed < PowerOnMinGap) return;
            lastPlayed = now;

            // 음량은 count와 무관. CCTV 연결음 직후면 뭉개지지 않게 줄여서 낸다.
            if (secondsSinceLastPlayed(CctvConnectSfx) < PowerOnStaticCctvGap)
                playScaled(PowerOnSfxId, 1f, PowerOnStaticDuckedVolume);
            else
                play(PowerOnSfxId, 1f);
        }

        public void Dispose() => Unbind();

        private static float SecondsSinceLastPlayed(SfxId id)
        {
            var player = SfxPlayer.Instance;
            return player != null ? player.SecondsSinceLastPlayed(id) : float.PositiveInfinity;
        }
    }
}

using System;
using SubTerra.App.Audio;
using SubTerra.App.UI;
using SubTerra.Gameplay.Drone;
using SubTerra.Shared.Audio;
using UnityEngine;

namespace SubTerra.App.Integration
{
    /// <summary>B-184: 허용된 스캔 회차의 초점 번쩍임만 짧게 찍는다. 생략한 표시는 예약하지 않는다.</summary>
    public sealed class DroneScanMarkSfx : IDisposable
    {
        public const SfxId ScanMarkSfx = SfxId.ScanMarkClick;
        public const int ScanMarkMaxPerPulse = 6;
        public const float ScanMarkMinGap = 0.06f;
        public const float ScanMarkPitchStep = 0.015f;
        public const float ScanMarkMaxPitch = 1.1f;

        private readonly Func<bool> settingsOpen;
        private readonly Func<bool> pauseHeld;
        private readonly Func<SfxId, float> secondsSinceLastPlayed;
        private readonly Func<float> clock;
        private DroneSensor sensor;
        private DroneScanPulseView view;
        private bool allowed;
        private int played;
        private float lastClick = float.NegativeInfinity;

        public DroneScanMarkSfx(Func<bool> settingsOpen, Func<bool> pauseHeld = null,
            Func<SfxId, float> secondsSinceLastPlayed = null, Func<float> clock = null)
        {
            this.settingsOpen = settingsOpen;
            this.pauseHeld = pauseHeld ?? (() => UiPauseGate.IsHeld);
            this.secondsSinceLastPlayed = secondsSinceLastPlayed ?? SecondsSinceLastPlayed;
            this.clock = clock ?? (() => Time.unscaledTime);
        }

        public void Bind(DroneSensor target)
        {
            Unbind();
            sensor = target;
            if (sensor != null)
            {
                sensor.ScanPulseStarted += HandlePulseStarted;
                BindView(sensor.ScanPulseView);
            }
        }

        private void BindView(DroneScanPulseView target)
        {
            if (view == target) return;
            if (view != null) view.MarkerFocused -= HandleMarkerFocused;
            view = target;
            if (view != null) view.MarkerFocused += HandleMarkerFocused;
        }

        public void HandlePulseStarted(bool immediatePulse)
        {
            // View는 첫 펄스에서 만들어질 수도 있다. Show 직후 이벤트 시점에 다시 연결한다.
            if (sensor != null) BindView(sensor.ScanPulseView);
            played = 0;
            allowed = DroneScanPingRule.ShouldPlay(immediatePulse,
                secondsSinceLastPlayed(SfxId.ElevatorArriveDing),
                secondsSinceLastPlayed(SfxId.UpgradeLevelUp),
                settingsOpen != null && settingsOpen(), pauseHeld());
        }

        public void HandleMarkerFocused(int order, Vector3 worldPosition)
        {
            if (!allowed || played >= ScanMarkMaxPerPulse || pauseHeld()
                || (settingsOpen != null && settingsOpen())) return;
            float now = clock();
            if (now - lastClick < ScanMarkMinGap) return;
            lastClick = now;
            played++;
            SfxPlayer.Play(ScanMarkSfx, Mathf.Min(ScanMarkMaxPitch, 1f + order * ScanMarkPitchStep));
        }

        public void Unbind()
        {
            if (sensor != null) sensor.ScanPulseStarted -= HandlePulseStarted;
            sensor = null;
            BindView(null);
            allowed = false;
            played = 0;
        }

        public void Dispose() => Unbind();

        private static float SecondsSinceLastPlayed(SfxId id)
        {
            var player = SfxPlayer.Instance;
            return player != null ? player.SecondsSinceLastPlayed(id) : float.PositiveInfinity;
        }
    }
}

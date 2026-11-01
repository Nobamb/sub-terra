using System;
using SubTerra.App.Audio;
using SubTerra.App.UI;
using SubTerra.Gameplay.Drone;
using SubTerra.Shared.Audio;

namespace SubTerra.App.Integration
{
    /// <summary>
    /// B-178: 드론 30초 스캔 링 "핑"을 낼지 정하는 순수 판단. 생략은 그 회차만이며 늦춰서 내지 않는다.
    /// </summary>
    public static class DroneScanPingRule
    {
        /// <summary>광산 도착 "딩"·레벨업 소리가 이 시간 안에 났으면 생략한다.</summary>
        public const float SkipIfRecentSeconds = 0.6f;
        public const float Pitch = 1f;

        public static bool ShouldPlay(
            bool immediatePulse,
            float secondsSinceElevatorDing,
            float secondsSinceLevelUp,
            bool settingsOpen,
            bool pauseHeld)
        {
            // 월드 준비·업그레이드 적용 직후의 즉시 스캔은 도착 "딩"과 같은 순간이라 링만 보인다.
            if (immediatePulse || settingsOpen || pauseHeld)
            {
                return false;
            }

            return secondsSinceElevatorDing >= SkipIfRecentSeconds
                && secondsSinceLevelUp >= SkipIfRecentSeconds;
        }
    }

    /// <summary>B-178: DroneSensor.ScanPulseStarted를 듣고 규칙을 통과한 회차에만 DroneScanPing을 한 번 낸다.</summary>
    public sealed class DroneScanPingSfx : IDisposable
    {
        private readonly Func<bool> settingsOpen;
        private readonly Func<bool> pauseHeld;
        private readonly Func<SfxId, float> secondsSinceLastPlayed;
        private DroneSensor sensor;

        public DroneScanPingSfx(
            Func<bool> settingsOpen,
            Func<bool> pauseHeld = null,
            Func<SfxId, float> secondsSinceLastPlayed = null)
        {
            this.settingsOpen = settingsOpen;
            this.pauseHeld = pauseHeld ?? (() => UiPauseGate.IsHeld);
            this.secondsSinceLastPlayed = secondsSinceLastPlayed ?? SecondsSinceLastPlayedOnPlayer;
        }

        public void Bind(DroneSensor target)
        {
            Unbind();
            sensor = target;
            if (sensor != null)
            {
                sensor.ScanPulseStarted += HandlePulseStarted;
            }
        }

        public void Unbind()
        {
            if (sensor != null)
            {
                sensor.ScanPulseStarted -= HandlePulseStarted;
            }

            sensor = null;
        }

        public void Dispose()
        {
            Unbind();
        }

        public void HandlePulseStarted(bool immediatePulse)
        {
            var play = DroneScanPingRule.ShouldPlay(
                immediatePulse,
                secondsSinceLastPlayed(SfxId.ElevatorArriveDing),
                secondsSinceLastPlayed(SfxId.UpgradeLevelUp),
                settingsOpen != null && settingsOpen(),
                pauseHeld());
            if (play)
            {
                SfxPlayer.Play(SfxId.DroneScanPing, DroneScanPingRule.Pitch);
            }
        }

        private static float SecondsSinceLastPlayedOnPlayer(SfxId id)
        {
            var player = SfxPlayer.Instance;
            return player != null ? player.SecondsSinceLastPlayed(id) : float.PositiveInfinity;
        }
    }
}

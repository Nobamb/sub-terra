using SubTerra.App.UI.Sell;
using UnityEngine;

namespace SubTerra.App.UI.EmergencyEscape
{
    public enum EscapeWormholePhase
    {
        NotStarted = 0,
        /// <summary>화면이 어두워지며 제자리 웜홀이 열리고 캐릭터가 빨려 들어간다.</summary>
        SuckIn = 1,
        /// <summary>완전한 암전. 웜홀이 커지며 빨아들이는 줄기가 모이고, 한 점으로 모이며 번쩍인다.</summary>
        Void = 2,
        /// <summary>암전이 걷히며 목적지 웜홀이 열리고 캐릭터가 나온다.</summary>
        Emerge = 3,
        Completed = 4
    }

    public readonly struct EscapeWormholeWarpFrame
    {
        public EscapeWormholePhase Phase { get; }
        public float ElapsedSeconds { get; }
        /// <summary>검은 전체화면 알파 0~1.</summary>
        public float BlackoutAlpha { get; }
        /// <summary>웜홀 크기 배율. 1이 기본 지름, 암전 동안 1보다 커진다. 0이면 보이지 않는다.</summary>
        public float WormholeScale { get; }
        /// <summary>웜홀 소용돌이 누적 회전(도). 빨려 들어갈 때 +, 나올 때 -.</summary>
        public float WormholeAngle { get; }
        /// <summary>캐릭터 크기 배율 0~1.</summary>
        public float PlayerScale { get; }
        /// <summary>캐릭터가 돌아간 각도(도).</summary>
        public float PlayerSpin { get; }
        /// <summary>웜홀로 빨려 드는 줄기 세기 0~1.</summary>
        public float SuctionStrength { get; }
        /// <summary>줄기 애니메이션용 시간(초). 연출 시작부터의 경과.</summary>
        public float SuctionTime { get; }
        /// <summary>한 점으로 모일 때의 번쩍임 0~1.</summary>
        public float FlashAlpha { get; }
        public bool ShouldTeleport { get; }
        public bool IsComplete { get; }

        public EscapeWormholeWarpFrame(
            EscapeWormholePhase phase,
            float elapsedSeconds,
            float blackoutAlpha,
            float wormholeScale,
            float wormholeAngle,
            float playerScale,
            float playerSpin,
            float suctionStrength,
            float suctionTime,
            float flashAlpha,
            bool shouldTeleport,
            bool isComplete)
        {
            Phase = phase;
            ElapsedSeconds = elapsedSeconds;
            BlackoutAlpha = blackoutAlpha;
            WormholeScale = wormholeScale;
            WormholeAngle = wormholeAngle;
            PlayerScale = playerScale;
            PlayerSpin = playerSpin;
            SuctionStrength = suctionStrength;
            SuctionTime = suctionTime;
            FlashAlpha = flashAlpha;
            ShouldTeleport = shouldTeleport;
            IsComplete = isComplete;
        }
    }

    /// <summary>
    /// 긴급 탈출 포탈 워프: 검은 전체화면 + 웜홀에 빨려 들어가는 연출. 카메라는 움직이지 않는다.
    /// 1) 화면이 어두워지고 제자리 웜홀이 열리며 캐릭터가 줄고 돌며 빨려 든다.
    /// 2) 완전한 암전에서 웜홀이 커지고 빨아들이는 줄기가 모였다가 한 점으로 모이며 번쩍인다. 그 순간 플레이어를 옮긴다.
    /// 3) 암전이 걷히며 목적지(이미 옮겨진 자리)에 웜홀이 열리고 캐릭터가 돌며 나온다.
    /// 플레이어 좌표는 암전 한가운데에서 한 번만 옮겨, 목적지가 켜질 때 이미 도착해 있다.
    /// </summary>
    public sealed class EscapeWormholeWarpTimeline
    {
        /// <summary>웜홀 소용돌이 회전 배율. 기준 속도는 창 열기의 EscapePortalOpenTimeline.SpinSpeedMax.</summary>
        public const float SpinMultiplier = 1.8f;
        /// <summary>빨려 들어가는 동안 캐릭터가 도는 각도(도).</summary>
        public const float PlayerSpinDegrees = 600f;
        /// <summary>빨려 드는 구간이 끝날 때 웜홀이 기본 지름 대비 커져 있는 배율.</summary>
        public const float SuckInEndScale = 1.5f;
        /// <summary>완전 암전 구간에서 웜홀이 커지는 최대 배율.</summary>
        public const float VoidPeakScale = 2.3f;

        private readonly float suckInDuration;
        private readonly float voidDuration;
        private readonly float emergeDuration;
        private float elapsedSeconds;
        private int teleportCount;

        public float SuckInDuration => suckInDuration;
        public float VoidDuration => voidDuration;
        public float EmergeDuration => emergeDuration;
        public float TeleportTime { get; }
        public float TotalDuration { get; }
        public float ElapsedSeconds => elapsedSeconds;
        public int TeleportCount => teleportCount;
        public bool IsComplete => elapsedSeconds >= TotalDuration - 0.0001f;

        public EscapeWormholeWarpTimeline(EscapeWarpSettings settings = null)
        {
            var active = settings ?? new EscapeWarpSettings();
            suckInDuration = Mathf.Max(0.05f, active.suckInDuration);
            voidDuration = Mathf.Max(0.05f, active.voidDuration);
            emergeDuration = Mathf.Max(0.05f, active.emergeDuration);
            TeleportTime = suckInDuration + voidDuration;
            TotalDuration = TeleportTime + emergeDuration;
        }

        public EscapeWormholeWarpFrame Advance(float deltaTime)
        {
            if (deltaTime > 0f)
            {
                elapsedSeconds = Mathf.Min(TotalDuration, elapsedSeconds + deltaTime);
            }

            var crossed = teleportCount == 0 && elapsedSeconds >= TeleportTime - 0.0001f;
            if (crossed)
            {
                teleportCount++;
            }

            var frame = Evaluate(elapsedSeconds, crossed);
            if (crossed)
            {
                // 옮기는 프레임은 완전히 검고 웜홀은 한 점, 캐릭터는 없다. 걷힘은 다음 프레임부터다.
                frame = new EscapeWormholeWarpFrame(
                    frame.Phase, frame.ElapsedSeconds, 1f, 0f, frame.WormholeAngle, 0f, 0f, 0f,
                    frame.SuctionTime, Mathf.Max(frame.FlashAlpha, 0.95f), true, frame.IsComplete);
            }

            return frame;
        }

        public EscapeWormholeWarpFrame Evaluate(float timeSeconds)
        {
            return Evaluate(Mathf.Clamp(timeSeconds, 0f, TotalDuration), false);
        }

        private EscapeWormholeWarpFrame Evaluate(float t, bool shouldTeleport)
        {
            var spinSpeed = EscapePortalOpenTimeline.SpinSpeedMax * SpinMultiplier;

            if (t < suckInDuration - 0.0001f)
            {
                var u = Mathf.Clamp01(t / suckInDuration);
                var open = ResourceSellTimeline.EaseOutCubic(Mathf.Clamp01(u / 0.35f));
                var scale = open * Mathf.Lerp(1f, SuckInEndScale, ResourceSellTimeline.EaseInOut(u));
                // 캐릭터는 웜홀이 열린 뒤(0.25~0.95) 점점 빨라지며 줄고 돈다.
                var p = Mathf.Clamp01((u - 0.25f) / 0.7f);
                return new EscapeWormholeWarpFrame(
                    EscapeWormholePhase.SuckIn, t,
                    ResourceSellTimeline.EaseInOut(u),
                    scale, spinSpeed * t,
                    1f - (p * p), PlayerSpinDegrees * p * p,
                    Mathf.Clamp01((u - 0.2f) / 0.6f) * 0.6f, t,
                    0f, shouldTeleport, false);
            }

            if (t < TeleportTime - 0.0001f)
            {
                var v = Mathf.Clamp01((t - suckInDuration) / voidDuration);
                // 0~0.65: 웜홀이 커지고 줄기가 빨라진다. 0.65~1: 한 점으로 모이며 번쩍인다.
                var grow = ResourceSellTimeline.EaseInOut(Mathf.Clamp01(v / 0.65f));
                var collapse = ResourceSellTimeline.EaseInOut(Mathf.Clamp01((v - 0.65f) / 0.35f));
                var scale = Mathf.Lerp(SuckInEndScale, VoidPeakScale, grow) * (1f - collapse);
                var strength = Mathf.Lerp(0.6f, 1f, grow) * (1f - (collapse * 0.7f));
                var flash = ResourceSellTimeline.EaseOutCubic(Mathf.Clamp01((v - 0.72f) / 0.28f));
                return new EscapeWormholeWarpFrame(
                    EscapeWormholePhase.Void, t,
                    1f, scale, (spinSpeed * suckInDuration) + (spinSpeed * 1.6f * (t - suckInDuration)),
                    0f, 0f, strength, t, flash, shouldTeleport, false);
            }

            if (t < TotalDuration - 0.0001f)
            {
                var local = t - TeleportTime;
                var w = Mathf.Clamp01(local / emergeDuration);
                // 번쩍임이 가라앉으며(0~0.15) 암전이 걷히고(0~0.7), 웜홀이 열리면(0~0.3) 캐릭터가 나온다(0.3~0.75).
                var black = 1f - ResourceSellTimeline.EaseInOut(Mathf.Clamp01(w / 0.7f));
                var open = ResourceSellTimeline.EaseOutCubic(Mathf.Clamp01(w / 0.3f));
                var close = ResourceSellTimeline.EaseInOut(Mathf.Clamp01((w - 0.7f) / 0.3f));
                var p = Mathf.Clamp01((w - 0.3f) / 0.45f);
                var eased = ResourceSellTimeline.EaseOutCubic(p);
                var flash = 1f - ResourceSellTimeline.EaseOutCubic(Mathf.Clamp01(w / 0.15f));
                return new EscapeWormholeWarpFrame(
                    EscapeWormholePhase.Emerge, t,
                    black, open * (1f - close), -spinSpeed * local,
                    eased, -PlayerSpinDegrees * (1f - eased),
                    0f, t, flash, shouldTeleport, false);
            }

            return new EscapeWormholeWarpFrame(
                EscapeWormholePhase.Completed, TotalDuration, 0f, 0f, 0f, 1f, 0f, 0f, TotalDuration, 0f,
                shouldTeleport, true);
        }
    }
}

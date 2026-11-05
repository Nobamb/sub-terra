using System;
using SubTerra.Gameplay.Structural;
using SubTerra.Shared.Audio;
using UnityEngine;

namespace SubTerra.App.Integration
{
    public sealed class UnstableShakeLoopAudio : ProximityLoopAudio
    {
        public const SfxId ShakeLoopSfx = SfxId.UnstableBlockShakeLoop;
        public const float ShakeHearFull = 3f;
        public const float ShakeHearMax = 10f;
        public const float ShakeFadeIn = 0.15f;
        public const float ShakeFadeOut = 0.3f;
        public const float ShakeStartupQuiet = 1f;
        private StructuralCrackOverlay overlay;
        private Transform player;
        private float quietRemaining = ShakeStartupQuiet;
        private bool subscribed;

        public void Configure(StructuralCrackOverlay target, Transform listener, Func<bool> failureHandling,
            Func<bool> playerCanAct, Func<bool> elevatorRiding)
        {
            Unsubscribe();
            overlay = target;
            player = listener;
            ConfigurePlayback(ShakeLoopSfx, failureHandling, playerCanAct, elevatorRiding);
            Clear();
            Subscribe();
        }

        private void OnEnable()
        {
            Clear();
            Subscribe();
        }

        private void Subscribe()
        {
            if (subscribed || overlay == null) return;
            overlay.Cleared += Clear;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (subscribed && overlay != null) overlay.Cleared -= Clear;
            subscribed = false;
        }

        public override void Tick(float deltaTime)
        {
            float target = 0f;
            bool quiet = quietRemaining > 0f;
            if (!IsPaused) quietRemaining = Mathf.Max(0f, quietRemaining - Mathf.Max(0f, deltaTime));
            if (!quiet && overlay != null && player != null
                && overlay.TryGetNearestShakingCell(player.position, out var center))
                target = DistanceLevel(Vector2.Distance(player.position, center));
            TickPlayback(target, deltaTime, ShakeFadeIn, ShakeFadeOut);
        }

        public static float DistanceLevel(float distance) =>
            1f - Mathf.InverseLerp(ShakeHearFull, ShakeHearMax, distance);

        private void Clear()
        {
            quietRemaining = ShakeStartupQuiet;
            StopNow();
        }

        protected override void OnDisable()
        {
            Unsubscribe();
            Clear();
        }

        protected override void OnDestroy() => OnDisable();
    }
}

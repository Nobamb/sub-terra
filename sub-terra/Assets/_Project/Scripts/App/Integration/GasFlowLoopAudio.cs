using System;
using System.Collections.Generic;
using SubTerra.Gameplay.Hazards;
using SubTerra.Shared.Audio;
using UnityEngine;

namespace SubTerra.App.Integration
{
    public sealed class GasFlowLoopAudio : ProximityLoopAudio
    {
        public const SfxId GasFlowSfx = SfxId.GasFlowLoop;
        public const float GasFadeOutLead = 2f;
        public const float GasHearFull = 2f;
        public const float GasHearMax = 14f;
        public const int GasTrackMax = 8;
        private readonly List<GasZone> zones = new List<GasZone>();
        private GasHazardSystem hazards;
        private Transform player;
        private bool subscribed;
        public int TrackedCount => zones.Count;

        public void Configure(GasHazardSystem target, Transform listener, Func<bool> failureHandling,
            Func<bool> playerCanAct, Func<bool> elevatorRiding)
        {
            Unsubscribe();
            Clear();
            hazards = target;
            player = listener;
            ConfigurePlayback(GasFlowSfx, failureHandling, playerCanAct, elevatorRiding);
            Subscribe();
        }

        private void OnEnable() => Subscribe();
        private void Subscribe()
        {
            if (subscribed || hazards == null) return;
            hazards.GasZoneActivated += OnActivated;
            hazards.GasZoneDeactivated += OnDeactivated;
            hazards.ZonesCleared += Clear;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (hazards != null && subscribed)
            {
                hazards.GasZoneActivated -= OnActivated;
                hazards.GasZoneDeactivated -= OnDeactivated;
                hazards.ZonesCleared -= Clear;
            }
            subscribed = false;
        }

        private void OnActivated(GasZone zone)
        {
            // Bind는 기존 존을 수집하지 않는다. 복원은 퍼짐 완료 상태로 이벤트를 보낸다.
            RemoveInactive();
            if (zone != null && !zone.IsSpawnComplete && zones.Count < GasTrackMax && !zones.Contains(zone))
                zones.Add(zone);
        }

        private void OnDeactivated(string id)
        {
            zones.RemoveAll(zone => zone == null || zone.GasZoneId == id);
            if (zones.Count == 0) StopNow();
        }

        private void RemoveInactive()
        {
            zones.RemoveAll(zone => zone == null || !zone.IsActive || !zone.gameObject.activeInHierarchy
                || hazards == null || !ContainsZone(zone));
        }

        private bool ContainsZone(GasZone zone)
        {
            foreach (var active in hazards.ActiveZones)
                if (active == zone) return true;
            return false;
        }

        public override void Tick(float deltaTime)
        {
            RemoveInactive();
            float target = 0f;
            if (player != null)
                foreach (var zone in zones)
                {
                    float distance = Vector2.Distance(player.position, zone.transform.position) - zone.Radius;
                    target = Mathf.Max(target, GasLevel(zone.IsActive, zone.SpawnProgress, zone.RemainingDuration)
                        * DistanceLevel(distance));
                }
            TickPlayback(target, deltaTime, 0f, 0f);
        }

        public static float GasLevel(bool active, float spawnProgress, float remainingDuration)
        {
            if (!active) return 0f;
            if (spawnProgress < 1f) return Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(spawnProgress));
            return Mathf.Clamp01(remainingDuration / GasFadeOutLead);
        }

        public static float DistanceLevel(float edgeDistance) =>
            1f - Mathf.InverseLerp(GasHearFull, GasHearMax, edgeDistance);

        private void Clear()
        {
            zones.Clear();
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

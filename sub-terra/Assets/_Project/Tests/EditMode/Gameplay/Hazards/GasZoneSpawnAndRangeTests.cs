using NUnit.Framework;
using SubTerra.Shared;
using UnityEngine;

namespace SubTerra.Gameplay.Hazards.Tests
{
    public sealed class GasZoneSpawnAndRangeTests
    {
        [Test]
        public void PromptB50_SpawnAnimationBlocksExposureUntilOneSecond()
        {
            var root = new GameObject("GasZoneSpawn");
            try
            {
                var zone = root.AddComponent<GasZone>();
                zone.Activate("gas-spawn", GasType.Toxic, 0.8f, GasVisualRules.GasRadiusBlocks, 12f);

                Assert.That(zone.IsActive, Is.True);
                Assert.That(zone.IsSpawnComplete, Is.False);
                Assert.That(zone.Contains(Vector2.zero), Is.False);

                zone.Tick(0.99f);
                Assert.That(zone.IsSpawnComplete, Is.False);
                Assert.That(zone.Contains(Vector2.zero), Is.False);

                zone.Tick(0.01f);
                Assert.That(zone.IsSpawnComplete, Is.True);
                Assert.That(zone.Contains(Vector2.zero), Is.True);
                Assert.That(zone.RemainingDuration, Is.EqualTo(12f).Within(0.001f));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void PromptB203_DurationEndStartsOneSecondFadeOutWithoutExposure()
        {
            var root = new GameObject("GasZoneFade");
            try
            {
                var zone = root.AddComponent<GasZone>();
                zone.Activate("gas-fade", GasType.Toxic, 0.8f, GasVisualRules.GasRadiusBlocks, 2f, false);

                zone.Tick(1.99f);
                Assert.That(zone.IsActive, Is.True);
                Assert.That(zone.Contains(Vector2.zero), Is.True);

                zone.Tick(0.01f);
                Assert.That(zone.IsActive, Is.False, "저장·피해 판정은 페이드 시작과 함께 끝난다.");
                Assert.That(zone.IsFadingOut, Is.True);
                Assert.That(zone.Contains(Vector2.zero), Is.False);
                Assert.That(root.activeSelf, Is.True);
                Assert.That(zone.VisualProgress, Is.EqualTo(1f).Within(0.001f));

                zone.Tick(GasVisualRules.SpawnDurationSeconds * 0.5f);
                Assert.That(zone.VisualProgress, Is.EqualTo(0.5f).Within(0.001f));
                Assert.That(root.activeSelf, Is.True);
                Assert.That(zone.Contains(Vector2.zero), Is.False);

                zone.Tick(GasVisualRules.SpawnDurationSeconds * 0.5f);
                Assert.That(zone.IsFadingOut, Is.False);
                Assert.That(zone.VisualProgress, Is.EqualTo(0f));
                Assert.That(root.activeSelf, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void PromptB203_FadeOutDoesNotChangeDangerousDuration()
        {
            var root = new GameObject("GasZoneFadeDuration");
            try
            {
                var zone = root.AddComponent<GasZone>();
                zone.Activate("gas-duration", GasType.Toxic, 0.8f, GasVisualRules.GasRadiusBlocks, 3f);

                zone.Tick(GasVisualRules.SpawnDurationSeconds);
                zone.Tick(2.5f);
                Assert.That(zone.RemainingDuration, Is.EqualTo(0.5f).Within(0.001f));
                Assert.That(zone.IsFadingOut, Is.False);

                zone.Tick(0.5f);
                Assert.That(zone.IsFadingOut, Is.True);
                Assert.That(zone.RemainingDuration, Is.EqualTo(0f));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void PromptB203_SystemDeactivatesAtFadeStartAndRestoreSkipsFadingZone()
        {
            var root = new GameObject("GasSystemFade");
            var player = new GameObject("FadePlayer");
            try
            {
                var system = root.AddComponent<GasHazardSystem>();
                string deactivatedId = null;
                system.GasZoneDeactivated += id => deactivatedId = id;
                var zone = system.RestoreGasZone(new GasSnapshotDto
                {
                    gasZoneId = "fade-restore",
                    gasTypeId = GasType.Toxic.ToString(),
                    concentrationLevel = 0.8f,
                    remainingDuration = 1f,
                    isActive = true
                });
                system.SetPlayerTransform(player.transform);
                Assert.That(system.CurrentExposure.IsExposed, Is.True);

                var tick = typeof(GasHazardSystem).GetMethod(
                    "TickZones",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                tick.Invoke(system, new object[] { 1f });
                system.SetPlayerTransform(player.transform);

                Assert.That(zone.IsFadingOut, Is.True);
                Assert.That(deactivatedId, Is.EqualTo("fade-restore"));
                Assert.That(system.CurrentExposure.IsExposed, Is.False);
                Assert.That(zone.IsActive, Is.False, "WorldSnapshotSystem은 IsActive=false 구역을 저장하지 않는다.");
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void PromptB50_GasRangeIsFiveBlocksFromOrigin()
        {
            var root = new GameObject("GasZoneRange");
            try
            {
                var zone = root.AddComponent<GasZone>();
                zone.Activate("gas-range", GasType.Toxic, 0.8f, GasVisualRules.GasRadiusBlocks, 12f, false);

                Assert.That(zone.Radius, Is.EqualTo(5f));
                Assert.That(zone.Contains(new Vector2(5f, 0f)), Is.True);
                Assert.That(zone.Contains(new Vector2(5.05f, 0f)), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void PromptB50_RestoreSkipsSpawnDelay()
        {
            var root = new GameObject("GasSystemRestore");
            var player = new GameObject("Player");
            try
            {
                var system = root.AddComponent<GasHazardSystem>();
                system.RestoreGasZone(new GasSnapshotDto
                {
                    gasZoneId = "restored",
                    gasTypeId = GasType.Toxic.ToString(),
                    concentrationLevel = 0.8f,
                    isActive = true
                });
                system.SetPlayerTransform(player.transform);

                Assert.That(system.CurrentExposure.IsExposed, Is.True);
                Assert.That(system.CurrentExposure.GasZoneId, Is.EqualTo("restored"));
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void PromptB50_LightClearanceSourceRegistersOnlyWhileEnabled()
        {
            var light = new GameObject("LightClear");
            var player = new GameObject("ClearPlayer");
            try
            {
                var source = light.AddComponent<GasVisionClearanceSource>();
                source.SetRadius(GasVisualRules.LightClearRadiusBlocks);
                player.transform.position = new Vector3(5f, 0f, 0f);

                Assert.That(GasVisionClearanceSource.IsCleared(player.transform.position), Is.True);

                light.SetActive(false);
                Assert.That(GasVisionClearanceSource.IsCleared(player.transform.position), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(light);
                Object.DestroyImmediate(player);
            }
        }
    }
}

using System.Linq;
using System.Reflection;
using SubTerra.Gameplay.Drone;
using SubTerra.Gameplay.Hazards;
using SubTerra.Gameplay.Mining;
using SubTerra.Shared;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace SubTerra.Gameplay.Tests.Drone
{
    public sealed class DroneSensorUpgradeTests
    {
        [Test]
        public void FirstPulse_WaitsForRestoredWorld_AndDisablingClearsExistingMarkers()
        {
            using var world = new SensorWorld(3f);
            var cell = new Vector3Int(1, 0, 0);
            world.PlaceMineral(cell, "mineral.copper");
            world.Sensor.SetScanWorldReady(false);
            world.Sensor.TickScanPulse(0f);
            Assert.That(world.Sensor.ScanPulseView, Is.Null);

            world.RemoveTile(cell); // 월드 복원이 저장된 채굴 흔적을 적용한 상황.
            world.Sensor.SetScanWorldReady(true);
            world.Sensor.TickScanPulse(1f);
            Assert.That(world.Sensor.LastPulseTargets, Is.Empty);
            Assert.That(world.Sensor.ScanPulseView.ActiveMarkerCount, Is.Zero);

            world.PlaceMineral(cell, "mineral.iron");
            world.Sensor.TickScanPulse(31f);
            Assert.That(world.Sensor.ScanPulseView.ActiveMarkerCount, Is.EqualTo(1));
            world.Sensor.SetScanWorldReady(false);
            Assert.That(world.Sensor.ScanPulseView.ActiveMarkerCount, Is.Zero);
            Assert.That(world.Sensor.ScanPulseView.IsRingVisible, Is.False);
        }

        [Test]
        public void RemovedMineral_ClearsMarkerBeforePulseExpires()
        {
            using var world = new SensorWorld(3f);
            var cell = new Vector3Int(1, 0, 0);
            world.PlaceMineral(cell, "mineral.copper");
            world.Sensor.TickScanPulse(0f);
            world.Sensor.TickScanPulse(0.7f);
            var view = world.Sensor.ScanPulseView;
            Assert.That(view.TryGetActiveTarget(cell, out _), Is.True);

            world.RemoveTile(cell);
            world.Sensor.TickScanPulse(0.8f);

            Assert.That(view.TryGetActiveTarget(cell, out _), Is.False,
                "Mining, collapse and world restore must not leave an ore marker on an empty cell.");
            Assert.That(view.ActiveMarkerCount, Is.Zero);
        }

        [Test]
        public void MineralReplacedByRock_ClearsOldMarker()
        {
            using var world = new SensorWorld(3f);
            var cell = new Vector3Int(1, 0, 0);
            world.PlaceMineral(cell, "mineral.copper");
            world.Sensor.TickScanPulse(0f);
            world.Sensor.TickScanPulse(0.7f);
            world.PlaceRock(cell);
            world.Sensor.TickScanPulse(0.8f);
            Assert.That(world.Sensor.ScanPulseView.TryGetActiveTarget(cell, out _), Is.False);
        }

        [Test]
        public void ActiveGas_RemainsMarkedAfterSourceTileIsRemoved()
        {
            using var world = new SensorWorld(7f);
            var cell = new Vector3Int(1, 0, 0);
            world.PlaceGas(cell);
            world.Sensor.TickScanPulse(0f);
            world.Sensor.TickScanPulse(0.7f);
            world.RemoveTile(cell);
            world.Sensor.TickScanPulse(0.8f);
            Assert.That(world.Sensor.ScanPulseView.TryGetActiveTarget(cell, out var kind), Is.True);
            Assert.That(kind, Is.EqualTo(DroneScanTargetKind.GasHazard));
        }

        [Test]
        public void ScanRing_ExpandsThenFadesAndExpires()
        {
            var host = new GameObject("ScanRingLifetime");
            try
            {
                var view = host.AddComponent<DroneScanPulseView>();
                view.Show(new DroneScanTarget[0], Vector3.zero, 7, 10f, 0f);
                var root = (GameObject)typeof(DroneScanPulseView)
                    .GetField("visualRoot", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(view);
                var ring = root.transform.Find("DroneScanRangeRing").GetComponent<SpriteRenderer>();
                float initialAlpha = ring.color.a;
                Assert.That(initialAlpha, Is.InRange(0.3f, 0.5f));
                Assert.That(ring.transform.localScale.x, Is.EqualTo(0.15f));
                view.Tick(0.25f);
                Assert.That(ring.transform.localScale.x, Is.InRange(0.16f, 6.99f));
                view.Tick(0.75f);
                Assert.That(ring.color.a, Is.LessThan(initialAlpha));
                Assert.That(ring.transform.localScale.x, Is.EqualTo(7f));
                Assert.That(ring.sortingOrder, Is.GreaterThan(10), "Ring must draw over the terrain.");
                view.Tick(1f);
                Assert.That(view.IsRingVisible, Is.False);
            }
            finally { Object.DestroyImmediate(host); }
        }

        [Test]
        public void Wave_RevealsNearbyTargetsBeforeDistantOnes_ThenFades()
        {
            var host = new GameObject("ScanWaveReveal");
            try
            {
                var view = host.AddComponent<DroneScanPulseView>();
                var near = new DroneScanTarget(Vector3Int.right, Vector3.right, DroneScanTargetKind.Mineral);
                var far = new DroneScanTarget(Vector3Int.right * 3, Vector3.right * 3, DroneScanTargetKind.GoldDrop);
                view.Show(new[] { near, far }, Vector3.zero, 3, 10f, 0f);
                Assert.That(view.TryGetActiveTarget(near.Cell, out _), Is.False);
                Assert.That(view.TryGetActiveTarget(far.Cell, out _), Is.False);
                view.Tick(0.3f);
                Assert.That(view.TryGetActiveTarget(near.Cell, out _), Is.True);
                Assert.That(view.TryGetActiveTarget(far.Cell, out _), Is.False);
                view.Tick(0.7f);
                Assert.That(view.TryGetActiveTarget(far.Cell, out var kind), Is.True);
                Assert.That(kind, Is.EqualTo(DroneScanTargetKind.GoldDrop));
                var root = (GameObject)typeof(DroneScanPulseView).GetField("visualRoot",
                    BindingFlags.Instance | BindingFlags.NonPublic).GetValue(view);
                var marker = root.GetComponentsInChildren<SpriteRenderer>()
                    .Single(renderer => renderer.transform.position.x == 3f);
                float alpha = marker.color.a;
                view.Tick(9f);
                Assert.That(marker.color.a, Is.LessThan(alpha));
                view.Tick(10f);
                Assert.That(view.ActiveMarkerCount, Is.Zero);
            }
            finally { Object.DestroyImmediate(host); }
        }

        [Test]
        public void Pulse_UsesDroneOrigin_AndWaveReachesDiagonalTargets()
        {
            using var world = new SensorWorld(3f);
            world.Sensor.transform.position = new Vector3(-1f, 1f, 0f);
            var cell = new Vector3Int(3, 3, 0);
            world.PlaceMineral(cell, "mineral.iron");
            world.Sensor.TickScanPulse(0f);
            var view = world.Sensor.ScanPulseView;
            var root = (GameObject)typeof(DroneScanPulseView).GetField("visualRoot",
                BindingFlags.Instance | BindingFlags.NonPublic).GetValue(view);
            var ring = root.transform.Find("DroneScanRangeRing");
            Assert.That((Vector2)ring.position, Is.EqualTo((Vector2)world.Sensor.transform.position));
            world.Sensor.TickScanPulse(0.7f);
            Assert.That(view.TryGetActiveTarget(cell, out _), Is.True);
            Assert.That(ring.localScale.x, Is.GreaterThanOrEqualTo(
                Vector2.Distance(world.Sensor.transform.position, world.Sensor.LastPulseTargets.Single().WorldPosition)));
            Assert.That(world.Sensor.EffectiveMineralScanRadius, Is.EqualTo(3));
        }

        [Test]
        public void DroneScanUpgrade_ChangesActualSensorRadius()
        {
            var host = new GameObject("DroneSensorUpgradeTests");
            try
            {
                var sensor = host.AddComponent<DroneSensor>();
                sensor.SetUpgradeEffects(new FixedEffects(7f));
                Assert.That(sensor.EffectiveMineralScanRadius, Is.EqualTo(7));
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void NoUpgrade_DisablesNearbyMineralsAndPulse()
        {
            using var world = new SensorWorld(0f);
            world.PlaceMineral(new Vector3Int(1, 0, 0), "mineral.copper");

            Assert.That(world.Sensor.EffectiveMineralScanRadius, Is.Zero);
            Assert.That(world.Sensor.CaptureContext().NearbyMineralIds, Is.Empty);

            world.Sensor.TickScanPulse(0f);
            Assert.That(world.Sensor.LastPulseTargets, Is.Empty);
            Assert.That(world.Sensor.GetComponent<DroneScanPulseView>(), Is.Null);
        }

        [Test]
        public void LevelOne_ScansMineralsWithinChebyshevRadiusThreeOnly()
        {
            using var world = new SensorWorld(3f);
            world.PlaceMineral(new Vector3Int(3, 3, 0), "mineral.iron");
            world.PlaceMineral(new Vector3Int(4, 0, 0), "mineral.lithium");

            SubTerra.Gameplay.Drone.DroneContextDto context = world.Sensor.CaptureContext();
            Assert.That(context.NearbyMineralIds, Is.EquivalentTo(new[] { "mineral.iron" }));

            world.Sensor.TickScanPulse(0f);
            Assert.That(world.Sensor.LastPulseTargets.Count, Is.EqualTo(1));
            Assert.That(world.Sensor.LastPulseTargets[0].Cell, Is.EqualTo(new Vector3Int(3, 3, 0)));
            Assert.That(world.Sensor.LastPulseTargets[0].Kind, Is.EqualTo(DroneScanTargetKind.Mineral));
        }

        [Test]
        public void LevelTwo_ScansMineralsAndActiveGas_AndPulseUsesThirtyByTenTiming()
        {
            using var world = new SensorWorld(7f);
            world.PlaceMineral(new Vector3Int(7, 0, 0), "mineral.lithium");
            world.PlaceMineral(new Vector3Int(8, 0, 0), "mineral.copper");
            world.PlaceGas(new Vector3Int(5, -5, 0));

            world.Sensor.TickScanPulse(0f);
            Assert.That(world.Sensor.ContextScanInterval, Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(world.Sensor.PulseInterval, Is.EqualTo(30f).Within(0.0001f));
            Assert.That(world.Sensor.PulseDuration, Is.EqualTo(10f).Within(0.0001f));
            Assert.That(world.Sensor.LastPulseTargets, Has.Some.Matches<DroneScanTarget>(
                target => target.Cell == new Vector3Int(7, 0, 0)
                    && target.Kind == DroneScanTargetKind.Mineral));
            Assert.That(world.Sensor.LastPulseTargets, Has.Some.Matches<DroneScanTarget>(
                target => target.Kind == DroneScanTargetKind.GasHazard));
            Assert.That(world.Sensor.LastPulseTargets, Has.None.Matches<DroneScanTarget>(
                target => target.Cell == new Vector3Int(8, 0, 0)));

            DroneScanPulseView view = world.Sensor.GetComponent<DroneScanPulseView>();
            world.Sensor.TickScanPulse(0.7f);
            int initialLightCount = view.ActiveLightCount;
            Assert.That(initialLightCount, Is.GreaterThan(0));
            Assert.That(view.IsRingVisible, Is.True);
            Assert.That(
                view.GetComponentsInChildren<SpriteRenderer>(true)
                    .Count(renderer => renderer.gameObject.name.StartsWith("PrimaryShape")
                        || renderer.gameObject.name.StartsWith("SecondaryShape")),
                Is.Zero);
            Assert.That(
                view.TryGetActiveTarget(new Vector3Int(7, 0, 0), out DroneScanTargetKind kind),
                Is.True);
            Assert.That(kind, Is.EqualTo(DroneScanTargetKind.Mineral));

            world.Sensor.TickScanPulse(10f);
            Assert.That(view.ActiveLightCount, Is.Zero);
            Assert.That(
                view.TryGetActiveTarget(new Vector3Int(7, 0, 0), out _),
                Is.False);
            Assert.That(view.IsRingVisible, Is.False);
            world.Sensor.TickScanPulse(29f);
            Assert.That(view.ActiveLightCount, Is.Zero);
            world.Sensor.TickScanPulse(30f);
            Assert.That(view.ActiveLightCount, Is.EqualTo(initialLightCount));
        }

        [Test]
        public void PromptB100_GoldRockAndOreUseYellowAndOverrideGas()
        {
            using var world = new SensorWorld(7f);
            var goldRock = new Vector3Int(1, 0, 0);
            var goldOre = new Vector3Int(2, 0, 0);
            var ordinaryOre = new Vector3Int(-7, -7, 0);
            world.PlaceGas(goldRock);
            world.PlaceMineral(goldRock, "", 20);
            world.PlaceMineral(goldOre, "mineral.copper", 50);
            world.PlaceMineral(ordinaryOre, "mineral.copper");
            world.Sensor.TickScanPulse(0);
            world.Sensor.TickScanPulse(0.7f);
            Assert.That(world.Sensor.LastPulseTargets.Single(t => t.Cell == goldRock).Kind, Is.EqualTo(DroneScanTargetKind.GoldDrop));
            Assert.That(world.Sensor.LastPulseTargets.Single(t => t.Cell == goldOre).Kind, Is.EqualTo(DroneScanTargetKind.GoldDrop));
            Assert.That(world.Sensor.LastPulseTargets.Single(t => t.Cell == ordinaryOre).Kind, Is.EqualTo(DroneScanTargetKind.Mineral));
            var view = world.Sensor.GetComponent<DroneScanPulseView>();
            var visual = typeof(DroneScanPulseView).GetField("visualRoot", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(view) as GameObject;
            Assert.That(visual.GetComponentsInChildren<UnityEngine.Rendering.Universal.Light2D>(), Is.Empty);
            var markers = visual.GetComponentsInChildren<SpriteRenderer>();
            Assert.That(markers.Count(m => m.name.StartsWith("DroneScanMarker_")
                && m.color.r == 1f && m.color.g == 0.82f && m.color.b == 0.18f
                && m.color.a > 0f && m.color.a <= 0.8f), Is.EqualTo(2));
        }

        [TestCase(3f)]
        [TestCase(7f)]
        public void PromptB111_OrdinaryBlocksAreNotScanTargets(float radius)
        {
            using var world = new SensorWorld(radius);
            var ordinary = new Vector3Int(1, 0, 0);
            var mineral = new Vector3Int(2, 0, 0);
            world.PlaceMineral(ordinary, string.Empty);
            world.PlaceMineral(mineral, "mineral.copper");

            world.Sensor.TickScanPulse(0f);

            Assert.That(world.Sensor.LastPulseTargets.Select(target => target.Cell),
                Is.EquivalentTo(new[] { mineral }));
            Assert.That(world.Sensor.ScanPulseView.TryGetActiveTarget(ordinary, out _), Is.False);
            Assert.That(world.Sensor.ScanPulseView.ActiveLightCount, Is.EqualTo(1));
        }

        private sealed class SensorWorld : System.IDisposable
        {
            private readonly GameObject root;
            private readonly Tilemap tilemap;
            private readonly MiningTileResolver resolver;
            private readonly GasHazardSystem gasSystem;
            private readonly Tile rock;

            public SensorWorld(float scanRadius)
            {
                root = new GameObject("DroneSensorWorld");
                root.AddComponent<Grid>();
                var mapObject = new GameObject("Foreground");
                mapObject.transform.SetParent(root.transform);
                tilemap = mapObject.AddComponent<Tilemap>();
                mapObject.AddComponent<TilemapRenderer>();

                resolver = root.AddComponent<MiningTileResolver>();
                gasSystem = root.AddComponent<GasHazardSystem>();
                SetPrivateField(gasSystem, "foregroundTilemap", tilemap);

                var player = new GameObject("Player");
                player.transform.SetParent(root.transform);
                player.transform.position = tilemap.GetCellCenterWorld(Vector3Int.zero);

                var sensorObject = new GameObject("DroneSensor");
                sensorObject.transform.SetParent(root.transform);
                Sensor = sensorObject.AddComponent<DroneSensor>();
                SetPrivateField(Sensor, "playerTransform", player.transform);
                SetPrivateField(Sensor, "foregroundTilemap", tilemap);
                SetPrivateField(Sensor, "tileResolver", resolver);
                SetPrivateField(Sensor, "gasHazardSystem", gasSystem);
                Sensor.SetUpgradeEffects(new FixedEffects(scanRadius));

                rock = ScriptableObject.CreateInstance<Tile>();
            }

            public DroneSensor Sensor { get; }

            public void RemoveTile(Vector3Int cell) => tilemap.SetTile(cell, null);

            public void PlaceRock(Vector3Int cell)
            {
                resolver.RegisterRuntime(rock,
                    new MiningTileDto("tile.rock", string.Empty, 0, true, 1f, 0f, 0f, false));
                tilemap.SetTile(cell, rock);
            }

            public void PlaceMineral(Vector3Int cell, string mineralId, int gold = 0)
            {
                var mineral = ScriptableObject.CreateInstance<Tile>();
                resolver.RegisterRuntime(
                    mineral,
                    new MiningTileDto("tile.test", mineralId, 1, true, 1f, 0f, 0f, false, goldDrop: gold));
                tilemap.SetTile(cell, mineral);
            }

            public void PlaceGas(Vector3Int cell)
            {
                resolver.RegisterRuntime(
                    rock,
                    new MiningTileDto("tile.gas", string.Empty, 0, true, 1f, 0f, 0f, true));
                tilemap.SetTile(cell, rock);
                GasZone zone = gasSystem.ActivateAt(
                    cell,
                    new MiningTileDto("tile.gas", string.Empty, 0, true, 1f, 0f, 0f, true));
                Assert.That(zone, Is.Not.Null);
            }

            public void Dispose()
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(rock);
            }

            private static void SetPrivateField(object target, string name, object value)
            {
                FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(field, Is.Not.Null, "Missing field: " + name);
                field.SetValue(target, value);
            }
        }

        private sealed class FixedEffects : IUpgradeEffectProvider
        {
            private readonly float scanRadius;
            public FixedEffects(float scanRadius) => this.scanRadius = scanRadius;
            public int GetDrillLevel() => 0;
            public float GetDrillSpeedMultiplier() => 1f;
            public float GetEnergyEfficiencyMultiplier() => 1f;
            public int GetMaximumEnergy(int baseMaximum) => baseMaximum;
            public float GetMaximumCargoWeight(float baseMaximum) => baseMaximum;
            public float GetDroneScanRadius(float baseRadius) => scanRadius;
            public float GetDroneRescuePreservation(float basePreservation) => basePreservation;
            public float GetGasResistance() => 0f;
            public int GetGoldGainBonusPercent() => 0;
            public int GetMiningYieldBonus(string mineralId) => 0;
        }
    }
}

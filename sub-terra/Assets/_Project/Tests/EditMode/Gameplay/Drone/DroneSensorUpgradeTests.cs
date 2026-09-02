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
            int initialLightCount = view.ActiveLightCount;
            Assert.That(initialLightCount, Is.GreaterThan(0));
            Assert.That(view.IsRingVisible, Is.True);

            world.Sensor.TickScanPulse(10f);
            Assert.That(view.ActiveLightCount, Is.Zero);
            Assert.That(view.IsRingVisible, Is.False);
            world.Sensor.TickScanPulse(29f);
            Assert.That(view.ActiveLightCount, Is.Zero);
            world.Sensor.TickScanPulse(30f);
            Assert.That(view.ActiveLightCount, Is.EqualTo(initialLightCount));
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

            public void PlaceMineral(Vector3Int cell, string mineralId)
            {
                var mineral = ScriptableObject.CreateInstance<Tile>();
                resolver.RegisterRuntime(
                    mineral,
                    new MiningTileDto("tile.test", mineralId, 1, true, 1f, 0f, 0f, false));
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
        }
    }
}

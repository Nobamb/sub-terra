using System.Linq;
using System.Reflection;
using UnityEditor;
using NUnit.Framework;
using SubTerra.Gameplay.Drone;
using SubTerra.Gameplay.Mining;
using SubTerra.Shared;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace SubTerra.Gameplay.Tests.Drone
{
    public sealed class SealedGlyphScanTests
    {
        [Test]
        public void LockedSignal_UsesCyanScanKind_NotMineralWhite()
        {
            var root = new GameObject("SealedGlyphScanTests");
            try
            {
                root.AddComponent<Grid>();
                var mapObject = new GameObject("Foreground");
                mapObject.transform.SetParent(root.transform);
                var tilemap = mapObject.AddComponent<Tilemap>();
                mapObject.AddComponent<TilemapRenderer>();
                var resolver = root.AddComponent<MiningTileResolver>();
                var player = new GameObject("Player");
                player.transform.SetParent(root.transform);
                player.transform.position = tilemap.GetCellCenterWorld(Vector3Int.zero);

                var sensorObject = new GameObject("DroneSensor");
                sensorObject.transform.SetParent(root.transform);
                var sensor = sensorObject.AddComponent<DroneSensor>();
                SetField(sensor, "playerTransform", player.transform);
                SetField(sensor, "foregroundTilemap", tilemap);
                SetField(sensor, "tileResolver", resolver);
                sensor.SetUpgradeEffects(new FixedScan(3f));

                var tile = ScriptableObject.CreateInstance<Tile>();
                resolver.RegisterRuntime(
                    tile,
                    new MiningTileDto(
                        "tile.locked.signal",
                        "item.rare.engine_fuel",
                        1,
                        true,
                        1f,
                        1.2f,
                        0f,
                        false,
                        2,
                        3));
                tilemap.SetTile(new Vector3Int(1, 0, 0), tile);

                sensor.TickScanPulse(0f);
                Assert.That(sensor.LastPulseTargets.Count, Is.EqualTo(1));
                Assert.That(sensor.LastPulseTargets[0].Kind, Is.EqualTo(DroneScanTargetKind.SealedGlyph));
                Assert.That(sensor.CaptureContext().NearbyMineralIds, Does.Contain("item.rare.engine_fuel"));

                var view = sensor.GetComponent<DroneScanPulseView>();
                Assert.That(view, Is.Not.Null);
                Assert.That(view.TryGetActiveTarget(new Vector3Int(1, 0, 0), out var kind), Is.True);
                Assert.That(kind, Is.EqualTo(DroneScanTargetKind.SealedGlyph));

                var visualRoot = typeof(DroneScanPulseView)
                    .GetField("visualRoot", BindingFlags.Instance | BindingFlags.NonPublic)
                    .GetValue(view) as GameObject;
                var lights = visualRoot.GetComponentsInChildren<UnityEngine.Rendering.Universal.Light2D>();
                Assert.That(lights.Any(light => ColorsMatch(light.color, new Color(0.18f, 0.92f, 0.88f, 1f))), Is.True);
                var glyph = AssetDatabase.LoadAssetAtPath<Sprite>(
                    "Assets/_Project/Art/Tiles/SealedGlyph/sealed_glyph_awakened_01.png");
                Assert.That(glyph, Is.Not.Null);
                var presentation = root.AddComponent<SealedGlyphScanVisual>();
                presentation.Configure(tilemap, resolver, sensor, glyph);
                TickVisual(presentation);
                var overlay = root.GetComponentsInChildren<SpriteRenderer>(true)
                    .Single(renderer => renderer.gameObject.name == "SealedGlyphOverlay");
                Assert.That(overlay.enabled, Is.True);
                Assert.That(overlay.sprite, Is.SameAs(glyph));
                Assert.That(tilemap.GetTile(new Vector3Int(1, 0, 0)), Is.SameAs(tile));
                view.Tick(sensor.PulseDuration + 0.1f);
                TickVisual(presentation);
                Assert.That(overlay.enabled, Is.False);
                sensor.TickScanPulse(sensor.PulseInterval);
                TickVisual(presentation);
                Assert.That(overlay.enabled, Is.True);
                tilemap.SetTile(new Vector3Int(1, 0, 0), null);
                TickVisual(presentation);
                Assert.That(overlay.enabled, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static bool ColorsMatch(Color actual, Color expected)
        {
            return Mathf.Abs(actual.r - expected.r) < 0.001f
                && Mathf.Abs(actual.g - expected.g) < 0.001f
                && Mathf.Abs(actual.b - expected.b) < 0.001f;
        }

        private static void TickVisual(SealedGlyphScanVisual visual)
        {
            var method = typeof(SealedGlyphScanVisual).GetMethod(
                "LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            method.Invoke(visual, null);
        }

        private static void SetField(object target, string name, object value)
        {
            var field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, name);
            field.SetValue(target, value);
        }

        private sealed class FixedScan : IUpgradeEffectProvider
        {
            private readonly float radius;
            public FixedScan(float radius) => this.radius = radius;
            public int GetDrillLevel() => 2;
            public float GetDrillSpeedMultiplier() => 1f;
            public float GetEnergyEfficiencyMultiplier() => 1f;
            public int GetMaximumEnergy(int baseMaximum) => baseMaximum;
            public float GetMaximumCargoWeight(float baseMaximum) => baseMaximum;
            public float GetDroneScanRadius(float baseRadius) => radius;
            public float GetDroneRescuePreservation(float basePreservation) => basePreservation;
            public float GetGasResistance() => 0f;
            public int GetGoldGainBonusPercent() => 0;
            public int GetMiningYieldBonus(string mineralId) => 0;
        }
    }
}

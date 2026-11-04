using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SubTerra.App.Audio;
using SubTerra.App.Integration;
using SubTerra.Gameplay.Hazards;
using SubTerra.Gameplay.Structural;
using SubTerra.Shared;
using SubTerra.Shared.Audio;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace SubTerra.App.Tests.Audio
{
    public sealed class PromptB184SfxWorldLoopTests
    {
        private readonly List<Object> objects = new List<Object>();
        private readonly List<(SfxId id, float pitch)> played = new List<(SfxId, float)>();
        private readonly List<DroneScanMarkSfx> marks = new List<DroneScanMarkSfx>();
        private float now;
        private SfxLibrary Library => Resources.Load<SfxLibrary>(SfxLibrary.ResourcePath);

        [SetUp]
        public void SetUp()
        {
            now = 10f;
            played.Clear();
            SfxRequests.PlayRequested += Record;
        }

        [TearDown]
        public void TearDown()
        {
            SfxRequests.PlayRequested -= Record;
            foreach (var mark in marks) mark.Dispose();
            marks.Clear();
            for (var i = objects.Count - 1; i >= 0; i--)
                if (objects[i] != null) Object.DestroyImmediate(objects[i]);
            objects.Clear();
        }

        private void Record(SfxId id, float pitch) => played.Add((id, pitch));
        private GameObject NewObject(string name)
        {
            var go = new GameObject(name);
            objects.Add(go);
            return go;
        }

        private static void Set(object target, string field, object value) =>
            target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);

        private static void Disable(MonoBehaviour target)
        {
            // EditMode에서는 SetActive(false)만으로 OnDisable 호출을 가정하지 않는다.
            target.GetType().GetMethod("OnDisable", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(target, null);
        }

        [TestCase(SfxId.ScanMarkClick, 99, "scan_mark_click", 0.4f, 0.1f)]
        [TestCase(SfxId.GasFlowLoop, 100, "gas_flow_loop", 0.45f, 4f)]
        [TestCase(SfxId.UnstableBlockShakeLoop, 101, "unstable_block_shake_loop", 0.5f, 1.5f)]
        public void Slots_AssetAndDefaultsMatch(SfxId id, int number, string file, float volume, float seconds)
        {
            Assert.That((int)id, Is.EqualTo(number));
            var pair = SfxLibraryDefaults.Create().Single(p => p.Entry.Id == id);
            Assert.That(pair.FileName, Is.EqualTo(file));
            Assert.That(pair.Entry.Bus, Is.EqualTo(SfxBus.World));
            Assert.That(pair.Entry.Volume, Is.EqualTo(volume));
            Assert.That(Library.TryGetEntry(id, out var entry), Is.True);
            Assert.That(entry.Clip, Is.Not.Null);
            Assert.That(entry.Clip.name, Is.EqualTo(file));
            Assert.That(entry.Clip.length, Is.EqualTo(seconds).Within(0.025f));
            Assert.That(entry.Bus, Is.EqualTo(SfxBus.World));
            Assert.That(entry.Volume, Is.EqualTo(volume));
        }

        [TestCase("gas_flow_loop")]
        [TestCase("unstable_block_shake_loop")]
        public void LoopImport_IsPcmWithoutNormalize(string file)
        {
            var importer = (AudioImporter)AssetImporter.GetAtPath("Assets/_Project/Audio/SFX/World/" + file + ".wav");
            Assert.That(importer, Is.Not.Null);
            Assert.That(new SerializedObject(importer).FindProperty("m_Normalize").boolValue, Is.False);
            Assert.That(importer.defaultSampleSettings.compressionFormat, Is.EqualTo(AudioCompressionFormat.PCM));
            Assert.That(importer.defaultSampleSettings.loadType, Is.EqualTo(AudioClipLoadType.DecompressOnLoad));
            Assert.That(SfxLibraryDefaults.IsLoopFile(file), Is.False);
        }

        private DroneScanMarkSfx Mark(Func<bool> settings = null, Func<bool> pause = null,
            Func<SfxId, float> recent = null)
        {
            var mark = new DroneScanMarkSfx(settings ?? (() => false), pause ?? (() => false),
                recent ?? (_ => float.PositiveInfinity), () => now);
            marks.Add(mark);
            return mark;
        }

        [Test]
        public void Mark_TenTargets_PlayAtMostSixWithIncreasingPitch()
        {
            var mark = Mark();
            mark.HandlePulseStarted(false);
            for (var i = 0; i < 10; i++)
            {
                mark.HandleMarkerFocused(i, Vector3.right * i);
                now += 0.1f;
            }
            Assert.That(played.Select(p => p.id), Is.EqualTo(Enumerable.Repeat(SfxId.ScanMarkClick, 6)));
            Assert.That(played.Select(p => p.pitch),
                Is.EqualTo(new[] { 1f, 1.015f, 1.03f, 1.045f, 1.06f, 1.075f }).Within(0.0001f));
        }

        [Test]
        public void Mark_CloseFocusIsSkippedWithoutDelayedPlayback_AndPitchIsCapped()
        {
            var mark = Mark();
            mark.HandlePulseStarted(false);
            mark.HandleMarkerFocused(0, Vector3.zero);
            now += 0.03f;
            mark.HandleMarkerFocused(1, Vector3.right);
            Assert.That(played.Count, Is.EqualTo(1));
            now += 0.1f;
            mark.HandleMarkerFocused(20, Vector3.right * 20);
            Assert.That(played.Count, Is.EqualTo(2));
            Assert.That(played.Last().pitch, Is.EqualTo(1.1f));
        }

        [TestCase("immediate")]
        [TestCase("settings")]
        [TestCase("pause")]
        [TestCase("ding")]
        [TestCase("level")]
        public void Mark_SuppressedPulseStaysSilent(string reason)
        {
            var settings = reason == "settings";
            var pause = reason == "pause";
            var mark = Mark(() => settings, () => pause,
                id => reason == "ding" && id == SfxId.ElevatorArriveDing
                    || reason == "level" && id == SfxId.UpgradeLevelUp ? 0.3f : float.PositiveInfinity);
            mark.HandlePulseStarted(reason == "immediate");
            settings = pause = false;
            now += 1f;
            mark.HandleMarkerFocused(0, Vector3.zero);
            Assert.That(played, Is.Empty, "회차 도중 차단이 풀려도 뒤늦게 내지 않는다");
        }

        [TestCase(true, 0f, 12f, 0f)]
        [TestCase(true, 0.5f, 12f, 0.5f)]
        [TestCase(true, 1f, 12f, 1f)]
        [TestCase(true, 1f, 5f, 1f)]
        [TestCase(true, 1f, 1f, 0.5f)]
        [TestCase(true, 1f, 0f, 0f)]
        [TestCase(false, 1f, 12f, 0f)]
        public void GasEnvelope_MatchesSpawnAndLastTwoSeconds(bool active, float spawn, float remaining, float expected)
        {
            Assert.That(GasFlowLoopAudio.GasLevel(active, spawn, remaining), Is.EqualTo(expected).Within(0.0001f));
        }

        [TestCase(-2f, 1f)]
        [TestCase(2f, 1f)]
        [TestCase(8f, 0.5f)]
        [TestCase(14f, 0f)]
        [TestCase(20f, 0f)]
        public void GasDistance_UsesDistanceFromZoneEdge(float distance, float expected)
        {
            Assert.That(GasFlowLoopAudio.DistanceLevel(distance), Is.EqualTo(expected).Within(0.0001f));
        }

        private (GasHazardSystem system, Transform player, GasFlowLoopAudio loop) Gas()
        {
            var grid = NewObject("Grid");
            grid.AddComponent<Grid>();
            var map = NewObject("Foreground").AddComponent<Tilemap>();
            map.transform.SetParent(grid.transform, false);
            var system = NewObject("Gas").AddComponent<GasHazardSystem>();
            Set(system, "foregroundTilemap", map);
            var player = NewObject("Player").transform;
            player.position = map.GetCellCenterWorld(Vector3Int.zero);
            var loop = system.gameObject.AddComponent<GasFlowLoopAudio>();
            loop.Configure(system, player, () => false, () => true, () => false);
            loop.ConfigureForTests(Library, () => false, () => false, () => false);
            return (system, player, loop);
        }

        private static GasZone Burst(GasHazardSystem system, int x = 0) =>
            system.ActivateAt(new Vector3Int(x, 0, 0), new MiningTileDto { containsGas = true });

        [Test]
        public void Gas_RestoredAndAlreadyPresentZonesAreSilent_NewZonesUseMaximumNotSum()
        {
            var f = Gas();
            var restored = f.system.RestoreGasZone(new GasSnapshotDto
                { gasZoneId = "restored", isActive = true, remainingDuration = 12f });
            f.loop.Tick(0.1f);
            Assert.That(f.loop.TrackedCount, Is.Zero);
            Assert.That(f.loop.Level, Is.Zero);
            var first = Burst(f.system);
            first.Tick(0.5f);
            f.loop.Tick(0.1f);
            Assert.That(f.loop.Level, Is.EqualTo(0.5f).Within(0.0001f));
            var second = Burst(f.system, 1);
            second.Tick(1f);
            f.loop.Tick(0.1f);
            Assert.That(f.loop.Level, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(f.loop.TrackedCount, Is.EqualTo(2));
            Assert.That(restored.IsSpawnComplete, Is.True);
            f.loop.Configure(f.system, f.player, () => false, () => true, () => false);
            f.loop.Tick(0.1f);
            Assert.That(f.loop.TrackedCount, Is.Zero, "재바인딩은 이미 있는 가스를 소리로 복원하지 않는다");
            Assert.That(f.loop.Level, Is.Zero);
        }

        [Test]
        public void Gas_TracksAtMostEight_DropsDestroyedZones_AndClearStopsImmediately()
        {
            var f = Gas();
            var zones = Enumerable.Range(0, 10).Select(i => Burst(f.system, i)).ToArray();
            foreach (var zone in zones) zone.Tick(1f);
            f.loop.Tick(0.1f);
            Assert.That(f.loop.TrackedCount, Is.EqualTo(8));
            Object.DestroyImmediate(zones[0].gameObject);
            f.loop.Tick(0.1f);
            Assert.That(f.loop.TrackedCount, Is.EqualTo(7));
            f.system.ClearRestoredZones();
            Assert.That(f.loop.TrackedCount, Is.Zero);
            Assert.That(f.loop.Level, Is.Zero);
            Assert.That(f.loop.Source.volume, Is.Zero);
        }

        [Test]
        public void Gas_DisableStopsAndForgetsTrackedZones()
        {
            var f = Gas();
            Burst(f.system).Tick(1f);
            f.loop.Tick(0.1f);
            Assert.That(f.loop.Level, Is.GreaterThan(0f));
            Disable(f.loop);
            Assert.That(f.loop.TrackedCount, Is.Zero);
            Assert.That(f.loop.Level, Is.Zero);
            Assert.That(f.loop.Source.volume, Is.Zero);
        }

        [TestCase(0f, 1f)]
        [TestCase(2f, 1f)]
        [TestCase(3f, 1f)]
        [TestCase(6.5f, 0.5f)]
        [TestCase(10f, 0f)]
        public void ShakeDistance_HasFullAndMaximumBoundaries(float distance, float expected)
        {
            Assert.That(UnstableShakeLoopAudio.DistanceLevel(distance), Is.EqualTo(expected).Within(0.0001f));
        }

        private (StructuralCrackOverlay overlay, Tilemap source, Transform player, UnstableShakeLoopAudio loop) Shake()
        {
            var grid = NewObject("Grid");
            grid.AddComponent<Grid>();
            var source = NewObject("Source").AddComponent<Tilemap>();
            source.transform.SetParent(grid.transform, false);
            var map = NewObject("Cracks").AddComponent<Tilemap>();
            map.transform.SetParent(grid.transform, false);
            var tile = ScriptableObject.CreateInstance<Tile>();
            objects.Add(tile);
            tile.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.one * 0.5f, 1f);
            objects.Add(tile.sprite);
            source.SetTile(Vector3Int.zero, tile);
            source.SetTile(new Vector3Int(8, 0, 0), tile);
            var overlay = map.gameObject.AddComponent<StructuralCrackOverlay>();
            Set(overlay, "overlayTilemap", map);
            overlay.BindSourceTilemap(source);
            var player = NewObject("Player").transform;
            player.position = source.GetCellCenterWorld(Vector3Int.zero);
            var loop = NewObject("ShakeAudio").AddComponent<UnstableShakeLoopAudio>();
            loop.Configure(overlay, player, () => false, () => true, () => false);
            loop.ConfigureForTests(Library, () => false, () => false, () => false);
            return (overlay, source, player, loop);
        }

        [Test]
        public void Shake_NearestCellIsReadOnly_StartupQuietThenFadeInAndFadeOut()
        {
            var f = Shake();
            f.loop.Tick(0.1f);
            Assert.That(f.loop.Level, Is.Zero);
            f.overlay.SetCell(Vector3Int.zero, StructuralRiskLevel.Danger, 1f, StructuralRiskCause.Unsupported);
            f.overlay.SetCell(new Vector3Int(8, 0, 0), StructuralRiskLevel.Danger, 1f, StructuralRiskCause.MiningImpact);
            Assert.That(f.overlay.ShakingCellCount, Is.EqualTo(2));
            Assert.That(f.overlay.TryGetNearestShakingCell(f.player.position, out var center), Is.True);
            Assert.That(center, Is.EqualTo(f.source.GetCellCenterWorld(Vector3Int.zero)));
            f.loop.Tick(0.4f);
            Assert.That(f.loop.Level, Is.Zero, "처음 0.5초는 무음");
            f.loop.Tick(0.6f);
            f.loop.Tick(0.15f);
            Assert.That(f.loop.Level, Is.EqualTo(1f).Within(0.0001f));
            f.overlay.ClearCell(Vector3Int.zero);
            f.overlay.ClearCell(new Vector3Int(8, 0, 0));
            f.loop.Tick(0.3f);
            Assert.That(f.loop.Level, Is.Zero);
            Assert.That(f.overlay.ShakingCellCount, Is.Zero);
            Assert.That(f.overlay.TryGetNearestShakingCell(f.player.position, out _), Is.False);
        }

        [Test]
        public void Shake_ClearAllStopsImmediatelyAndRestartsQuietPeriod()
        {
            var f = Shake();
            f.overlay.SetCell(Vector3Int.zero, StructuralRiskLevel.Danger, 1f, StructuralRiskCause.Unsupported);
            f.loop.Tick(1.1f);
            f.loop.Tick(0.2f);
            Assert.That(f.loop.Level, Is.GreaterThan(0f));
            f.overlay.ClearAll();
            Assert.That(f.loop.Level, Is.Zero);
            Assert.That(f.loop.Source.volume, Is.Zero);
            f.overlay.SetCell(Vector3Int.zero, StructuralRiskLevel.Danger, 1f, StructuralRiskCause.Unsupported);
            f.loop.Tick(0.5f);
            Assert.That(f.loop.Level, Is.Zero);
        }
    }
}

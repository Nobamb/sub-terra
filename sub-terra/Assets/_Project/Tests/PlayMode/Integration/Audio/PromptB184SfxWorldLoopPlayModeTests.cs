using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SubTerra.App.Audio;
using SubTerra.App.Core;
using SubTerra.App.Integration;
using SubTerra.App.Save;
using SubTerra.App.UI;
using SubTerra.App.UI.MainMenu;
using SubTerra.App.UI.Tutorial;
using SubTerra.Gameplay.Drone;
using SubTerra.Gameplay.Hazards;
using SubTerra.Gameplay.Player;
using SubTerra.Gameplay.Structural;
using SubTerra.Shared;
using SubTerra.Shared.Audio;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace SubTerra.Integration.Tests.Audio
{
    public sealed class PromptB184SfxWorldLoopPlayModeTests
    {
        private GameObject root;
        private SfxPlayer sfx;
        private DroneScanMarkSfx mark;
        private bool bootstrapSmoke;
        private readonly List<(SfxId id, float pitch)> played = new List<(SfxId, float)>();
        private readonly List<Object> assets = new List<Object>();
        private SfxLibrary Library => Resources.Load<SfxLibrary>(SfxLibrary.ResourcePath);

        [SetUp]
        public void SetUp()
        {
            bootstrapSmoke = false;
            root = new GameObject("B184Runtime");
            root.AddComponent<AudioListener>();
            if (SfxPlayer.Instance != null) SfxPlayer.Instance.ReleaseForTests();
            sfx = SfxPlayer.CreateForTests(Library, null, () => false);
            played.Clear();
            SfxRequests.PlayRequested += Record;
        }

        [TearDown]
        public void TearDown()
        {
            SfxRequests.PlayRequested -= Record;
            if (mark != null) mark.Dispose();
            mark = null;
            if (sfx != null) sfx.ReleaseForTests();
            if (root != null) Object.DestroyImmediate(root);
            if (bootstrapSmoke) GameBootstrapper.ResetInstanceForTests();
            foreach (var asset in assets) if (asset != null) Object.DestroyImmediate(asset);
            assets.Clear();
        }

        [UnityTearDown]
        public IEnumerator CleanSmokeScene()
        {
            if (!bootstrapSmoke) yield break;
            GameBootstrapper.ResetInstanceForTests();
            var clean = SceneManager.CreateScene("B184SmokeCleanup-" + Guid.NewGuid().ToString("N"));
            SceneManager.SetActiveScene(clean);
            for (var i = SceneManager.sceneCount - 1; i >= 0; i--)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene != clean && scene.isLoaded) yield return SceneManager.UnloadSceneAsync(scene);
            }
        }

        private void Record(SfxId id, float pitch) => played.Add((id, pitch));
        private GameObject Child(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root.transform, false);
            return go;
        }
        private static void Set(object target, string field, object value) =>
            target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);

        [UnityTest]
        public IEnumerator Bootstrap_NewGame_MineBindsWorldLoopsAndPlaysGas()
        {
            bootstrapSmoke = true;
            GameBootstrapper.ResetInstanceForTests();
            yield return SceneManager.LoadSceneAsync(SceneNames.Bootstrap);
            yield return WaitFor(() => SceneManager.GetActiveScene().name == SceneNames.MainMenu
                && SaveRuntimeController.Instance != null && SaveRuntimeController.Instance.InventoryService != null,
                "Bootstrap runtime and MainMenu");
            var runtime = SaveRuntimeController.Instance;
            Assert.That(runtime.ActiveSlot, Is.Zero);
            var testRoot = Path.GetFullPath(Path.Combine(Application.dataPath,
                "../Temp/B184-smoke-" + Guid.NewGuid().ToString("N")));
            Assert.That(Directory.Exists(testRoot), Is.False, "새 임시 경로만 사용한다");
            var paths = new SavePathPolicy(testRoot);
            var fileSystem = new PhysicalSaveFileSystem();
            var mapper = new SaveDataMapper(new SystemSaveClock());
            var json = new SaveJsonCodec(new SaveMigrationService());
            // 새 게임·자동저장·썸네일 모두 사용자 슬롯 대신 이번 검사 전용 Temp로 보낸다.
            Set(runtime, "saveService", new SaveService(fileSystem, paths, mapper, json));
            Set(runtime, "loadService", new LoadService(fileSystem, paths, mapper, json));
            Set(runtime, "<Thumbnails>k__BackingField", new SaveThumbnailService(paths));
            Assert.That(runtime.StartNewGame(1), Is.True);
            yield return WaitFor(() => SceneManager.GetActiveScene().name == SceneNames.SurfaceBase
                && runtime.IsUiReady, "new-game SurfaceBase");
            Assert.That(runtime.TryStartExploration(out var reason), Is.True, reason);
            yield return WaitFor(() => SceneManager.GetActiveScene().name == SceneNames.Integration
                && runtime.IsUiReady, "mine runtime ready");
            var binder = Object.FindAnyObjectByType<IntegrationRuntimeBinder>();
            Assert.That(binder, Is.Not.Null);
            yield return WaitFor(() => binder.IsUiActivated, "IntegrationRuntimeBinder.IsUiActivated");
            var briefing = Object.FindAnyObjectByType<StartBriefingPopupMotion>();
            if (briefing != null && briefing.isActiveAndEnabled)
            {
                var guidance = Object.FindAnyObjectByType<DemoObjectiveView>();
                Assert.That(guidance, Is.Not.Null);
                yield return WaitFor(() => briefing.IsShown, "start briefing shown");
                Assert.That(guidance.TryCloseGuidance(), Is.True);
                yield return WaitFor(() => Time.timeScale > 0f, "briefing closed");
            }
            Assert.That(binder.GetComponents<GasFlowLoopAudio>().Length, Is.EqualTo(1));
            Assert.That(binder.GetComponents<UnstableShakeLoopAudio>().Length, Is.EqualTo(1));
            var gasFlow = binder.GetComponent<GasFlowLoopAudio>();
            var hazards = Object.FindAnyObjectByType<GasHazardSystem>();
            var movement = Object.FindAnyObjectByType<PlayerMovement>();
            Assert.That(hazards, Is.Not.Null);
            Assert.That(movement, Is.Not.Null);
            var map = (Tilemap)typeof(GasHazardSystem).GetField("foregroundTilemap",
                BindingFlags.Instance | BindingFlags.NonPublic).GetValue(hazards);
            Assert.That(map, Is.Not.Null);
            var zone = hazards.ActivateAt(map.WorldToCell(movement.transform.position),
                new MiningTileDto { containsGas = true });
            Assert.That(zone, Is.Not.Null);
            yield return new WaitForSecondsRealtime(0.5f);
            var elevator = Object.FindAnyObjectByType<ElevatorController>();
            var diagnostic = "spawn=" + zone.SpawnProgress + "; zoneActive=" + zone.IsActive
                + "; tracked=" + gasFlow.TrackedCount + "; level=" + gasFlow.Level
                + "; timeScale=" + Time.timeScale + "; pauseHeld=" + UiPauseGate.IsHeld
                + "; settingsOpen=" + ControlPreferences.IsSettingsOpen
                + "; masterMuted=" + SettingsRuntimeApplier.IsMasterMuted
                + "; sfxMuted=" + SettingsRuntimeApplier.IsMuted(AudioChannel.Sfx)
                + "; binderEnabled=" + binder.isActiveAndEnabled + "; loopEnabled=" + gasFlow.isActiveAndEnabled
                + "; runPhase=" + runtime.RunPhase + "; elevatorRiding=" + (elevator != null && elevator.IsRiding)
                + "; movementCanMove=" + movement.CanMove;
            var currentlyMuted = SettingsRuntimeApplier.IsMasterMuted
                || SettingsRuntimeApplier.IsMuted(AudioChannel.Sfx);
            if (currentlyMuted)
            {
                Assert.That(gasFlow.Source, Is.Null, "현재 음소거 설정에 따른 정상 차단: " + diagnostic);
                // 사용자 설정은 보존하고 이 컴포넌트의 테스트용 음소거 판단만 대체한다.
                gasFlow.ConfigureForTests(Library, () => false, null, null);
                yield return new WaitForSecondsRealtime(0.1f);
            }
            Debug.Log("[B184 smoke] " + diagnostic + "; testMuteBypass=" + currentlyMuted);
            yield return SaveMineScreen();
            Assert.That(gasFlow.Source, Is.Not.Null, diagnostic);
            Assert.That(gasFlow.Source.isPlaying, Is.True);
            Assert.That(gasFlow.Level, Is.GreaterThan(0f));
            Assert.That(gasFlow.Source.clip.name, Is.EqualTo("gas_flow_loop"));
        }

        private static IEnumerator WaitFor(Func<bool> condition, string description)
        {
            var deadline = Time.realtimeSinceStartup + 15f;
            while (!condition() && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(condition(), Is.True, "Timed out: " + description);
        }

        private static IEnumerator SaveMineScreen()
        {
            yield return new WaitForEndOfFrame();
            var texture = ScreenCapture.CaptureScreenshotAsTexture();
            var target = RenderTexture.GetTemporary(Mathf.Max(1, texture.width / 2), Mathf.Max(1, texture.height / 2));
            var previous = RenderTexture.active;
            Graphics.Blit(texture, target);
            RenderTexture.active = target;
            var small = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
            small.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
            small.Apply();
            var output = Path.GetFullPath(Path.Combine(Application.dataPath,
                "../../work_process/MVP2/sound/B-184-verification/mine-smoke.png"));
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            File.WriteAllBytes(output, small.EncodeToPNG());
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(target);
            Object.Destroy(texture);
            Object.Destroy(small);
        }

        [UnityTest]
        public IEnumerator Gas_ActualSourceAdvances_PausesResumes_AndStopsForEachBlocker()
        {
            var grid = Child("Grid");
            grid.AddComponent<Grid>();
            var map = Child("Foreground").AddComponent<Tilemap>();
            map.transform.SetParent(grid.transform, false);
            var hazards = Child("Gas").AddComponent<GasHazardSystem>();
            hazards.enabled = false;
            Set(hazards, "foregroundTilemap", map);
            var player = Child("Player").transform;
            player.position = map.GetCellCenterWorld(Vector3Int.zero);
            bool paused = false, muted = false, failure = false, canAct = true, riding = false, ending = false;
            var loop = Child("GasAudio").AddComponent<GasFlowLoopAudio>();
            loop.Configure(hazards, player, () => failure, () => canAct, () => riding);
            loop.ConfigureForTests(Library, () => muted, () => paused, () => ending);
            var zone = hazards.ActivateAt(Vector3Int.zero, new MiningTileDto { containsGas = true });
            zone.Tick(1f);
            yield return new WaitForSecondsRealtime(0.15f);
            Assert.That(loop.Source.isPlaying, Is.True);
            Assert.That(loop.Source.loop, Is.True);
            Assert.That(loop.Source.spatialBlend, Is.Zero);
            Assert.That(loop.Source.clip.name, Is.EqualTo("gas_flow_loop"));
            Assert.That(loop.GetComponentsInChildren<AudioSource>().Length, Is.EqualTo(1));
            var before = loop.Source.time;
            yield return new WaitForSecondsRealtime(0.12f);
            Assert.That(loop.Source.time, Is.GreaterThan(before));
            paused = true;
            yield return null;
            var held = loop.Source.time;
            yield return new WaitForSecondsRealtime(0.12f);
            Assert.That(loop.Source.isPlaying, Is.False);
            Assert.That(loop.Source.time, Is.EqualTo(held).Within(0.04f));
            paused = false;
            yield return new WaitForSecondsRealtime(0.12f);
            Assert.That(loop.Source.isPlaying, Is.True);
            Assert.That(loop.Source.time, Is.GreaterThan(held));
            muted = true;
            yield return null;
            Assert.That(loop.Source.isPlaying, Is.False);
            Assert.That(loop.Source.volume, Is.Zero);
            muted = false;
            foreach (var reason in new[] { "failure", "cannotAct", "riding", "ending" })
            {
                yield return new WaitForSecondsRealtime(0.1f);
                Assert.That(loop.Source.isPlaying, Is.True, reason + " 시작 전");
                failure = reason == "failure";
                canAct = reason != "cannotAct";
                riding = reason == "riding";
                ending = reason == "ending";
                yield return new WaitForSecondsRealtime(0.15f);
                Assert.That(loop.Source.isPlaying, Is.False, reason);
                Assert.That(loop.Level, Is.Zero, reason);
                failure = riding = ending = false;
                canAct = true;
            }
            yield return new WaitForSecondsRealtime(0.1f);
            loop.gameObject.SetActive(false);
            Assert.That(loop.Source.isPlaying, Is.False);
            Assert.That(loop.TrackedCount, Is.Zero);
            loop.gameObject.SetActive(true);
            yield return null;
            Assert.That(loop.Source.isPlaying, Is.False, "재활성화는 기존 가스를 다시 울리지 않는다");
        }

        [UnityTest]
        public IEnumerator Scan_FocusEventsAreOnceInNearOrder_ClearAndDisableAreSilent()
        {
            var drone = Child("Drone");
            var sensor = drone.AddComponent<DroneSensor>();
            sensor.enabled = false;
            var view = drone.AddComponent<DroneScanPulseView>();
            Set(sensor, "pulseView", view);
            float now = 10f;
            mark = new DroneScanMarkSfx(() => false, () => false, _ => float.PositiveInfinity, () => now);
            mark.Bind(sensor);
            var focused = new List<int>();
            view.MarkerFocused += (order, position) => focused.Add(order);
            var targets = Enumerable.Range(1, 10).Reverse().Select(i => new DroneScanTarget(
                new Vector3Int(i, 0, 0), Vector3.right * i, DroneScanTargetKind.Mineral)).ToArray();
            view.Show(targets, Vector3.zero, 10, 20f, now);
            mark.HandlePulseStarted(false);
            for (var i = 0; i < 60; i++)
            {
                now += 0.02f;
                view.Tick(now);
                yield return null;
            }
            Assert.That(focused, Is.EqualTo(Enumerable.Range(0, 10)));
            Assert.That(played.Count(p => p.id == SfxId.ScanMarkClick), Is.InRange(1, 6));
            var pitches = played.Where(p => p.id == SfxId.ScanMarkClick).Select(p => p.pitch).ToArray();
            Assert.That(pitches, Is.Ordered.Ascending);
            view.Tick(now + 0.5f);
            Assert.That(focused.Count, Is.EqualTo(10));
            played.Clear();
            view.Clear();
            view.Show(targets, Vector3.zero, 10, now + 10f, now);
            mark.HandlePulseStarted(true);
            view.Tick(now + 1f);
            Assert.That(played, Is.Empty, "즉시 스캔은 무음");
            view.Clear();
            view.enabled = false;
            yield return null;
            Assert.That(played, Is.Empty, "Clear와 OnDisable은 소리 이벤트를 만들지 않는다");
        }

        [UnityTest]
        public IEnumerator Shake_ActualLoopStartsAfterQuiet_FadesWhenCellEnds_AndClearStops()
        {
            var grid = Child("Grid");
            grid.AddComponent<Grid>();
            var source = Child("Source").AddComponent<Tilemap>();
            source.transform.SetParent(grid.transform, false);
            var map = Child("Cracks").AddComponent<Tilemap>();
            map.transform.SetParent(grid.transform, false);
            var tile = ScriptableObject.CreateInstance<Tile>();
            var sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.one * 0.5f, 1f);
            assets.Add(sprite);
            assets.Add(tile);
            tile.sprite = sprite;
            source.SetTile(Vector3Int.zero, tile);
            var overlay = map.gameObject.AddComponent<StructuralCrackOverlay>();
            Set(overlay, "overlayTilemap", map);
            overlay.BindSourceTilemap(source);
            var player = Child("Player").transform;
            player.position = source.GetCellCenterWorld(Vector3Int.zero);
            var loop = Child("ShakeAudio").AddComponent<UnstableShakeLoopAudio>();
            loop.Configure(overlay, player, () => false, () => true, () => false);
            loop.ConfigureForTests(Library, () => false, () => false, () => false);
            overlay.SetCell(Vector3Int.zero, StructuralRiskLevel.Danger, 1f, StructuralRiskCause.Unsupported);
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.That(loop.Level, Is.Zero);
            yield return new WaitForSecondsRealtime(0.8f);
            Assert.That(loop.Source.isPlaying, Is.True);
            Assert.That(loop.Source.clip.name, Is.EqualTo("unstable_block_shake_loop"));
            overlay.ClearCell(Vector3Int.zero);
            yield return new WaitForSecondsRealtime(0.4f);
            Assert.That(loop.Source.isPlaying, Is.False);
            overlay.SetCell(Vector3Int.zero, StructuralRiskLevel.Danger, 1f, StructuralRiskCause.MiningImpact);
            yield return new WaitForSecondsRealtime(0.2f);
            Assert.That(loop.Source.isPlaying, Is.True);
            overlay.ClearAll();
            Assert.That(loop.Source.isPlaying, Is.False);
            Assert.That(loop.Source.volume, Is.Zero);
        }
    }
}

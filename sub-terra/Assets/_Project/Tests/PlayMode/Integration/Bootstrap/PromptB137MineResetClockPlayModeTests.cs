using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using SubTerra.App.Core;
using SubTerra.App.Integration;
using SubTerra.App.Save;
using SubTerra.App.State;
using SubTerra.App.Tutorial;
using SubTerra.App.UI;
using SubTerra.App.UI.HUD;
using SubTerra.App.UI.SurfaceBase;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace SubTerra.App.Tests.PlayMode
{
    public sealed class PromptB137MineResetClockPlayModeTests
    {
        private const string PauseOwner = "prompt-b137-test";
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private UiTestEnvironment environment;
        private SaveRuntimeController runtime;
        private MineResetClockOverlay overlay;
        private MineResetClockView view;
        private SaveService memorySave;

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            UiPauseGate.Release(PauseOwner);
            if (environment != null) environment.Dispose();
            yield return null;
        }

        private IEnumerator Boot()
        {
            environment = new UiTestEnvironment();
            GameBootstrapper.ResetInstanceForTests();
            yield return SceneManager.LoadSceneAsync(SceneNames.Bootstrap);
            yield return UiTestWait.Until(() => SaveRuntimeController.Instance != null
                && SaveRuntimeController.Instance.InventoryService != null
                && SceneManager.GetActiveScene().name == SceneNames.MainMenu, "Bootstrap runtime and MainMenu");
            runtime = SaveRuntimeController.Instance;
            Assert.That(runtime.ActiveSlot, Is.Zero);
            var fs = new MemoryFileSystem();
            var paths = new SavePathPolicy(Path.Combine(Path.GetTempPath(), "subterra-b137-memory"));
            var mapper = new SaveDataMapper(new SystemSaveClock());
            var json = new SaveJsonCodec(new SaveMigrationService());
            memorySave = new SaveService(fs, paths, mapper, json);
            typeof(SaveRuntimeController).GetField("saveService", Private).SetValue(runtime, memorySave);
            typeof(SaveRuntimeController).GetField("loadService", Private).SetValue(runtime, new LoadService(fs, paths, mapper, json));
            Assert.That(runtime.StartNewGame(1), Is.True);
            yield return UiTestWait.Until(() => SceneManager.GetActiveScene().name == SceneNames.SurfaceBase
                && runtime.IsUiReady, "memory-slot SurfaceBase");
            var state = GameBootstrapper.Instance.State;
            state.SetDemoProgress(DemoObjectiveIds.MineCopper, 1, false);
            state.SetQuestRewardSettlement(string.Empty, 1);
            yield return LoadMine();
            overlay = runtime.GetComponentInChildren<MineResetClockOverlay>(true);
            view = overlay.GetComponentInChildren<MineResetClockView>(true);
            Assert.That(view.gameObject.activeInHierarchy, Is.True);
            Assert.That(view.Band, Is.EqualTo(MineResetClockBand.Normal));
            Assert.That(view.IsTransitioning, Is.False);
            UiPauseGate.Acquire(PauseOwner);
        }

        private IEnumerator LoadMine()
        {
            yield return SceneManager.LoadSceneAsync(SceneNames.Integration);
            yield return UiTestWait.Until(() =>
            {
                var binder = Object.FindAnyObjectByType<IntegrationRuntimeBinder>();
                return binder != null && binder.IsUiActivated;
            }, "Integration HUD activation");
            yield return UiTestEnvironment.DismissBriefing();
        }

        private void SetRemaining(int seconds)
        {
            runtime.SetMineResetElapsedSeconds(MineResetService.CycleDurationSeconds - seconds);
            overlay.RefreshFromState();
        }

        [UnityTest]
        public IEnumerator Bootstrap_MemorySaveLoad_Reentry_Boundaries_AndSingleExpiry()
        {
            yield return Boot();
            foreach (var seconds in new[] { 1801, 1800, 1799, 301, 300, 299, 61, 60, 59, 1 })
            {
                SetRemaining(seconds);
                Assert.That(view.DisplaySeconds, Is.EqualTo(seconds));
                Assert.That(view.Band, Is.EqualTo(MineResetClockStyle.GetBand(seconds)));
                var natural = seconds == 1800 || seconds == 1799 || seconds == 300
                    || seconds == 299 || seconds == 60 || seconds == 59;
                Assert.That(view.IsTransitioning, Is.EqualTo(natural));
                yield return null;
            }
            SetRemaining(600);
            SetRemaining(599);
            Assert.That(view.DisplaySeconds, Is.EqualTo(599));
            SetRemaining(60);
            SetRemaining(59);
            Assert.That(view.DisplaySeconds, Is.EqualTo(59));

            // 같은 씬에서 불러올 때 자연 경계(301→300)처럼 보이는 값도 즉시 적용되어야 한다.
            SetRemaining(300);
            Assert.That(runtime.SaveCurrent().IsSuccess, Is.True);
            SetRemaining(301);
            var loadService = (LoadService)typeof(SaveRuntimeController).GetField("loadService", Private).GetValue(runtime);
            var load = loadService.Load(1);
            Assert.That(load.IsSuccess, Is.True);
            Assert.That(runtime.RestoreBState(load.State), Is.True);
            Assert.That(view.DisplaySeconds, Is.EqualTo(300));
            Assert.That(view.IsTransitioning, Is.False);
            Assert.That(view.transform.Find("Digits/Digit0/Segment0").GetComponent<UnityEngine.UI.Image>().color,
                Is.EqualTo(MineResetClockStyle.CriticalColor));
            ContinueResult continued = null;
            runtime.BeginContinue(1, result => continued = result);
            yield return UiTestWait.Until(() => continued != null, "memory-slot BeginContinue completion");
            Assert.That(continued.IsSuccess, Is.True);
            yield return UiTestWait.Until(() =>
            {
                var binder = Object.FindAnyObjectByType<IntegrationRuntimeBinder>();
                return binder != null && binder.IsUiActivated;
            }, "loaded Integration HUD activation");
            Assert.That(view.DisplaySeconds, Is.EqualTo(300));
            Assert.That(view.IsTransitioning, Is.False);

            for (var i = 0; i < 3; i++)
            {
                SetRemaining(60);
                view.Advance(0.875f);
                runtime.ToggleMineResetClock();
                Assert.That(view.gameObject.activeSelf, Is.False);
                Assert.That(view.PulseFactor, Is.EqualTo(1f));
                runtime.ToggleMineResetClock();
                Assert.That(view.gameObject.activeSelf, Is.True);
                Assert.That(view.IsTransitioning, Is.False);
                Assert.That(view.PulseFactor, Is.EqualTo(1f));
                UiPauseGate.Release(PauseOwner);
                yield return SceneManager.LoadSceneAsync(SceneNames.SurfaceBase);
                Assert.That(view.gameObject.activeSelf, Is.False);
                yield return LoadMine();
                UiPauseGate.Acquire(PauseOwner);
                Assert.That(view.gameObject.activeSelf, Is.True);
                Assert.That(view.IsTransitioning, Is.False);
                Assert.That(runtime.GetComponentsInChildren<MineResetClockView>(true), Has.Length.EqualTo(1));
            }

            SetRemaining(1);
            var changes = 0;
            GameBootstrapper.Instance.State.MineResetCycleChanged += () => changes++;
            var saves = 0;
            memorySave.Saved += _ => saves++;
            var stateAtExpiry = GameBootstrapper.Instance.State;
            typeof(MineResetCycleState).GetMethod("SetElapsedSeconds", Private)
                .Invoke(stateAtExpiry.MineResetCycle, new object[] { MineResetService.CycleDurationSeconds });
            overlay.RefreshFromState();
            Assert.That(view.DisplaySeconds, Is.Zero);
            Assert.That(view.Band, Is.EqualTo(MineResetClockBand.Final));
            UiPauseGate.Release(PauseOwner);
            yield return UiTestWait.Until(() => SceneManager.GetActiveScene().name == SceneNames.SurfaceBase
                && overlay.transform.Find("TimedResetPopup").gameObject.activeSelf, "existing expiry popup");
            Assert.That(stateAtExpiry.MineResetCycle.ElapsedSeconds, Is.Zero);
            Assert.That(view.gameObject.activeSelf, Is.False);
            Assert.That(changes, Is.EqualTo(1));
            Assert.That(saves, Is.EqualTo(1));
            Assert.That(overlay.transform.Find("TimedResetPopup").GetComponents<MineResetPopupMotion>(), Has.Length.EqualTo(1));
            yield return UiTestWait.Until(() => !overlay.transform.Find("TimedResetPopup/ClockIntro")
                .GetComponent<MineResetClockIntro>().IsPlaying, "existing clock intro completion");
            yield return null;
            Assert.That(changes, Is.EqualTo(1));
            Assert.That(saves, Is.EqualTo(1));
            Assert.That(view.IsTransitioning, Is.False);
            Assert.That(view.PulseFactor, Is.EqualTo(1f));
        }

        [UnityTest, Category("Visual")]
        public IEnumerator Clock_ThreeResolutions_FiveStates_WithHud()
        {
            yield return Boot();
            var evidence = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..",
                "work_process", "MVP2", "mine-reset-clock"));
            foreach (var size in new[] { new Vector2Int(1280, 720), new Vector2Int(1920, 1080), new Vector2Int(2560, 1440) })
            {
                yield return environment.Resolution.Set(size.x, size.y);
                foreach (var name in new[] { "normal", "warning", "final-bright", "final-dark", "zero" })
                {
                    var remaining = name == "normal" ? 9000 : name == "warning" ? 1800 : name == "zero" ? 0 : 60;
                    // 0초 정지 화면은 만료를 호출하지 않고 기존 상태만 관찰한다.
                    typeof(MineResetCycleState).GetMethod("SetElapsedSeconds", Private)
                        .Invoke(GameBootstrapper.Instance.State.MineResetCycle,
                            new object[] { MineResetService.CycleDurationSeconds - remaining });
                    view.ResetObservation();
                    overlay.RefreshFromState();
                    if (name == "final-dark") view.Advance(0.875f);
                    AssertLayout();
                    yield return UiTestWait.Capture(Path.Combine(evidence, $"clock-{size.x}x{size.y}-{name}.png"));
                }
            }
        }

        private void AssertLayout()
        {
            Canvas.ForceUpdateCanvases();
            var clockRect = ScreenRect((RectTransform)view.transform);
            Assert.That(clockRect.xMin, Is.GreaterThanOrEqualTo(0));
            Assert.That(clockRect.xMax, Is.LessThanOrEqualTo(Screen.width));
            Assert.That(clockRect.yMax, Is.LessThanOrEqualTo(Screen.height));
            var hud = Object.FindAnyObjectByType<BasicHudView>();
            Assert.That(hud, Is.Not.Null);
            Assert.That(clockRect.Overlaps(ScreenRect((RectTransform)hud.transform)), Is.False, "clock overlaps HUD");
            var side = Object.FindAnyObjectByType<GameplaySideMenuController>();
            Assert.That(side, Is.Not.Null);
            Assert.That(clockRect.Overlaps(ScreenRect((RectTransform)side.transform.Find("OpenMenuRoot"))),
                Is.False, "clock overlaps side menu");
        }

        private static Rect ScreenRect(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            var canvas = rect.GetComponentInParent<Canvas>().rootCanvas;
            var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            var min = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
            var max = RectTransformUtility.WorldToScreenPoint(camera, corners[2]);
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        private sealed class MemoryFileSystem : ISaveFileSystem
        {
            private readonly Dictionary<string, string> files = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            public bool FileExists(string path) => files.ContainsKey(path);
            public void CreateDirectory(string path) { }
            public void WriteAllText(string path, string contents) { files[path] = contents; }
            public string ReadAllText(string path) => files[path];
            public void DeleteFile(string path) { files.Remove(path); }
            public void MoveFile(string sourcePath, string destinationPath)
            {
                files[destinationPath] = files[sourcePath];
                files.Remove(sourcePath);
            }
        }
    }
}

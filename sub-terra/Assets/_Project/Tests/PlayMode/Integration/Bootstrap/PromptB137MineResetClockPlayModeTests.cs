using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine.EventSystems;
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

        private IEnumerator Boot(bool captureEntry = false)
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
            if (captureEntry)
            {
                yield return environment.Resolution.Set(1920, 1080);
                var loading = SceneManager.LoadSceneAsync(SceneNames.Integration);
                yield return UiTestWait.Until(() =>
                {
                    overlay = runtime.GetComponentInChildren<MineResetClockOverlay>(true);
                    if (overlay == null) return false;
                    view = overlay.GetComponentInChildren<MineResetClockView>(true);
                    return view != null && view.IsPoweredOn;
                }, "entry power target");
                yield return CapturePower("power-on", true);
                yield return UiTestWait.Until(() => loading.isDone, "entry scene loaded");
                yield return UiTestEnvironment.DismissBriefing();
            }
            else
            {
                yield return LoadMine();
                overlay = runtime.GetComponentInChildren<MineResetClockOverlay>(true);
                Assert.That(overlay, Is.Not.Null);
                view = overlay.GetComponentInChildren<MineResetClockView>(true);
            }
            yield return UiTestWait.Until(() => !view.IsPowerAnimating, "clock power-on completion");
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
            var beforeLoadPower = view.PowerProgress;
            Assert.That(runtime.RestoreBState(load.State), Is.True);
            Assert.That(view.PowerProgress, Is.EqualTo(beforeLoadPower), "same-scene load replayed power-on");
            Assert.That(view.DisplaySeconds, Is.EqualTo(300));
            Assert.That(view.IsTransitioning, Is.False);
            Assert.That(view.transform.Find("Window/Content/Readout/Digits/Digit0/Segment0").GetComponent<UnityEngine.UI.Image>().color,
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
                yield return UiTestWait.Until(() => !view.gameObject.activeSelf, "T-toggle power-off completion");
                Assert.That(view.gameObject.activeSelf, Is.False);
                Assert.That(view.PulseFactor, Is.EqualTo(1f));
                runtime.ToggleMineResetClock();
                Assert.That(view.gameObject.activeSelf, Is.True);
                Assert.That(view.IsTransitioning, Is.False);
                Assert.That(view.PulseFactor, Is.EqualTo(1f));
                UiPauseGate.Release(PauseOwner);
                yield return SceneManager.LoadSceneAsync(SceneNames.SurfaceBase);
                yield return UiTestWait.Until(() => !view.gameObject.activeSelf, "surface power-off completion");
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
            yield return UiTestWait.Until(() => !view.gameObject.activeSelf, "expiry power-off completion");
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

        [UnityTest]
        public IEnumerator SurfaceDeparture_PowersOffClockBeforeSurfaceScene_AndExpiryDoesToo()
        {
            yield return Boot();
            UiPauseGate.Release(PauseOwner);

            // 엘리베이터 귀환 경로: 시계가 완전히 꺼질 때까지 광산 Scene이 유지되어야 한다.
            Assert.That(runtime.CreateSurfaceSceneLoader().Load(SceneNames.SurfaceBase), Is.True);
            yield return UiTestWait.Until(() => !view.IsPoweredOn, "departure power target");
            var guard = 0;
            while (view.gameObject.activeSelf && guard++ < 120)
            {
                Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(SceneNames.Integration),
                    "scene changed before the clock powered off");
                yield return null;
            }
            Assert.That(view.gameObject.activeSelf, Is.False);
            yield return UiTestWait.Until(() => SceneManager.GetActiveScene().name == SceneNames.SurfaceBase, "surface arrival");
            Assert.That(view.gameObject.activeSelf, Is.False);

            // 광산 초기화 만료 경로도 같은 순서를 따른다.
            yield return LoadMine();
            Assert.That(view.gameObject.activeSelf, Is.True);
            typeof(MineResetCycleState).GetMethod("SetElapsedSeconds", Private)
                .Invoke(GameBootstrapper.Instance.State.MineResetCycle, new object[] { MineResetService.CycleDurationSeconds });
            guard = 0;
            while (SceneManager.GetActiveScene().name == SceneNames.Integration && guard++ < 600)
            {
                if (view.gameObject.activeSelf) Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(SceneNames.Integration));
                yield return null;
            }
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(SceneNames.SurfaceBase));
            Assert.That(view.gameObject.activeSelf, Is.False, "clock still on after arriving at surface");
        }

        [UnityTest, Category("Visual")]
        public IEnumerator Clock_ThreeResolutions_FiveStates_WithHud()
        {
            yield return Boot();
            var evidence = EvidenceDirectory;
            foreach (var size in new[] { new Vector2Int(1280, 720), new Vector2Int(1920, 1080), new Vector2Int(2560, 1440) })
            {
                yield return environment.Resolution.Set(size.x, size.y);
                foreach (var name in new[] { "normal", "warning", "critical", "final-bright", "final-dark", "zero" })
                {
                    var remaining = name == "normal" ? 9000 : name == "warning" ? 1800 : name == "critical" ? 120 : name == "zero" ? 0 : 60;
                    // 0초 정지 화면은 만료를 호출하지 않고 기존 상태만 관찰한다.
                    typeof(MineResetCycleState).GetMethod("SetElapsedSeconds", Private)
                        .Invoke(GameBootstrapper.Instance.State.MineResetCycle,
                            new object[] { MineResetService.CycleDurationSeconds - remaining });
                    view.ResetObservation();
                    overlay.RefreshFromState();
                    if (name == "final-dark") view.Advance(0.875f);
                    Assert.That(view.PowerProgress, Is.EqualTo(1f));
                    AssertLayout();
                    yield return UiTestWait.Capture(Path.Combine(evidence, $"clock-{size.x}x{size.y}-{name}.png"));
                }
            }
        }

        private static string EvidenceDirectory => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..",
            "work_process", "MVP2", "mine-reset-clock", "b137-1"));

        [UnityTest, Category("Visual")]
        public IEnumerator Power_EntryExitSequences_WarningCriticalAndFinal_WithPauseAndInput()
        {
            yield return Boot(true);
            UiPauseGate.Release(PauseOwner);
            var leaving = SceneManager.LoadSceneAsync(SceneNames.SurfaceBase);
            yield return UiTestWait.Until(() => !view.IsPoweredOn, "surface exit power target");
            yield return CapturePower("power-off", false);
            yield return UiTestWait.Until(() => leaving.isDone, "surface exit scene loaded");
            Assert.That(view.isActiveAndEnabled, Is.False);
            yield return LoadMine();
            yield return UiTestWait.Until(() => !view.IsPowerAnimating, "reentry power completion");
            UiPauseGate.Acquire(PauseOwner);
            Time.timeScale = 0f;
            foreach (var seconds in new[] { 1800, 120, 60 })
            {
                SetRemaining(seconds);
                runtime.ToggleMineResetClock();
                yield return CapturePower("power-off-" + seconds, false);
                Assert.That(view.gameObject.activeSelf, Is.False);
                runtime.ToggleMineResetClock();
                yield return CapturePower("power-on-" + seconds, true);
                Assert.That(view.PowerProgress, Is.EqualTo(1f));
                AssertPowerEndpoint();
            }
            yield return VerifyClickThroughClock();
        }

        private IEnumerator VerifyClickThroughClock()
        {
            var probe = new GameObject("ClockInputProbe", typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.GraphicRaycaster));
            try
            {
                var canvas = probe.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 899;
                var buttonObject = new GameObject("BehindClockButton", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
                buttonObject.transform.SetParent(probe.transform, false);
                var rect = (RectTransform)buttonObject.transform;
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
                rect.sizeDelta = new Vector2(280f, 78f);
                rect.anchoredPosition = new Vector2(0f, -51f);
                buttonObject.GetComponent<UnityEngine.UI.Image>().color = Color.clear;
                var button = buttonObject.GetComponent<UnityEngine.UI.Button>();
                var clicks = 0;
                button.onClick.AddListener(() => clicks++);
                runtime.ToggleMineResetClock();
                yield return UiTestWait.Until(() => !view.gameObject.activeSelf, "input test power-off completion");
                runtime.ToggleMineResetClock();
                Assert.That(view.IsPowerAnimating, Is.True);
                yield return UiTestWait.Click(environment.Mouse, button);
                Assert.That(clicks, Is.EqualTo(1), "click during power-on did not reach the button behind clock");
                yield return UiTestWait.Until(() => !view.IsPowerAnimating, "input test power-on completion");
                yield return UiTestWait.Click(environment.Mouse, button);
                Assert.That(clicks, Is.EqualTo(2), "click after power-on did not reach the button behind clock");
            }
            finally { Object.Destroy(probe); }
        }

        [UnityTest]
        public IEnumerator Power_ReverseOnSceneVisibility_AndRepeatedRefresh_PreserveCurrentProgress()
        {
            yield return Boot();
            Time.timeScale = 0f;
            runtime.ToggleMineResetClock();
            yield return UiTestWait.Until(() => !view.gameObject.activeSelf, "initial test power-off completion");
            runtime.ToggleMineResetClock();
            var trace = new StringBuilder("sample,dt,before,after,expected\n");
            for (var i = 0; i < 20; i++)
            {
                var before = view.PowerProgress;
                SetRemaining(9000 - i);
                overlay.RefreshFromState();
                overlay.RefreshFromState();
                Assert.That(view.PowerProgress, Is.EqualTo(before), "two refreshes or clock values advanced power");
                yield return null;
                var expected = Mathf.Min(1f, before + Mathf.Min(Time.unscaledDeltaTime, 0.05f) / MineResetClockPowerTimeline.OnDuration);
                Assert.That(view.PowerProgress, Is.EqualTo(expected).Within(1e-6f), "power did not advance once by actual unscaled frame delta");
                trace.AppendLine(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                    "{0},{1:F6},{2:F6},{3:F6},{4:F6}", i, Time.unscaledDeltaTime, before, view.PowerProgress, expected));
            }
            File.WriteAllText(Path.Combine(EvidenceDirectory, "power-refresh.csv"), trace.ToString());
            yield return UiTestWait.Until(() => !view.IsPowerAnimating, "test power-on completion");
            runtime.ToggleMineResetClock();
            yield return UiTestWait.Until(() => view.PowerProgress < 0.7f, "partial power-off");
            var p = view.PowerProgress;
            runtime.ToggleMineResetClock();
            Assert.That(view.PowerProgress, Is.EqualTo(p));
            for (var i = 0; i < 20; i++) overlay.RefreshFromState();
            Assert.That(view.PowerProgress, Is.EqualTo(p));
            yield return UiTestWait.Until(() => !view.IsPowerAnimating, "reversed power-on completion");
            AssertPowerEndpoint();
            // 광산에서 매 프레임 표시 여부를 다시 설정하는 런타임을 멈추고 오버레이의 중단 경로를 관찰한다.
            runtime.enabled = false;
            overlay.SetSessionVisible(false);
            yield return UiTestWait.Until(() => !view.gameObject.activeSelf, "visibility power-off completion");
            overlay.SetSessionVisible(true);
            yield return UiTestWait.Until(() => view.PowerProgress > 0.2f, "partial power-on");
            p = view.PowerProgress;
            overlay.SetSessionVisible(false);
            Assert.That(view.PowerProgress, Is.EqualTo(p));
            yield return UiTestWait.Until(() => !view.gameObject.activeSelf, "reversed power-off completion");
            Assert.That(view.isActiveAndEnabled, Is.False);
            Assert.That(runtime.GetComponentsInChildren<MineResetClockView>(true), Has.Length.EqualTo(1));
            runtime.enabled = true;
        }

        private IEnumerator CapturePower(string prefix, bool on)
        {
            var trace = new StringBuilder("sample,realtime,p,width,height,frameAlpha,readoutAlpha\n");
            var window = (RectTransform)view.transform.Find("Window");
            var frame = window.Find("Content/Frame").GetComponent<CanvasGroup>();
            var readout = window.Find("Content/Readout").GetComponent<CanvasGroup>();
            var images = new List<Texture2D>();
            var start = Time.realtimeSinceStartup;
            Directory.CreateDirectory(EvidenceDirectory);
            try
            {
                do
                {
                    yield return new WaitForEndOfFrame();
                    trace.AppendLine(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                        "{0},{1:F5},{2:F5},{3:F3},{4:F3},{5:F5},{6:F5}", images.Count, Time.realtimeSinceStartup - start,
                        view.PowerProgress, window.sizeDelta.x, window.sizeDelta.y, frame.alpha, readout.alpha));
                    AssertClockDoesNotBlockInput();
                    images.Add(ScreenCapture.CaptureScreenshotAsTexture());
                    yield return null;
                } while (view.IsPowerAnimating);
                yield return new WaitForEndOfFrame();
                trace.AppendLine(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                    "{0},{1:F5},{2:F5},{3:F3},{4:F3},{5:F5},{6:F5}", images.Count, Time.realtimeSinceStartup - start,
                    view.PowerProgress, window.sizeDelta.x, window.sizeDelta.y, frame.alpha, readout.alpha));
                images.Add(ScreenCapture.CaptureScreenshotAsTexture());
                File.WriteAllText(Path.Combine(EvidenceDirectory, prefix + ".csv"), trace.ToString());
                for (var i = 0; i < images.Count; i++)
                    File.WriteAllBytes(Path.Combine(EvidenceDirectory, prefix + "-" + i.ToString("D2") + ".png"), images[i].EncodeToPNG());
                Assert.That(images.Count, Is.GreaterThanOrEqualTo(8), prefix + " needs at least eight rendered frames");
            }
            finally
            {
                foreach (var texture in images) Object.Destroy(texture);
            }
            Assert.That(view.PowerProgress, Is.EqualTo(on ? 1f : 0f));
            if (!on) Assert.That(view.gameObject.activeSelf, Is.False);
        }

        private void AssertPowerEndpoint()
        {
            Assert.That(view.PowerProgress, Is.EqualTo(1f));
            Assert.That(((RectTransform)view.transform.Find("Window")).sizeDelta, Is.EqualTo(new Vector2(280f, 78f)));
            Assert.That(view.transform.Find("Window/Content/Readout").GetComponent<CanvasGroup>().alpha, Is.EqualTo(1f));
            foreach (var name in new[] { "PowerBeam", "PowerSpark", "EdgeGlowTop", "EdgeGlowBottom", "FrameFlash" })
                Assert.That(view.transform.Find(name).GetComponent<UnityEngine.UI.Image>().color.a, Is.Zero, name);
            AssertClockDoesNotBlockInput();
        }

        private void AssertClockDoesNotBlockInput()
        {
            foreach (var graphic in view.GetComponentsInChildren<UnityEngine.UI.Graphic>(true))
                Assert.That(graphic.raycastTarget, Is.False, graphic.name);
            var eventSystem = EventSystem.current;
            Assert.That(eventSystem, Is.Not.Null);
            var results = new List<RaycastResult>();
            var point = RectTransformUtility.WorldToScreenPoint(null, view.transform.TransformPoint(new Vector3(0f, -39f, 0f)));
            eventSystem.RaycastAll(new PointerEventData(eventSystem) { position = point }, results);
            foreach (var hit in results)
                Assert.That(hit.gameObject.transform.IsChildOf(view.transform), Is.False, "clock intercepted UI raycast");
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

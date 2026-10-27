using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using SubTerra.App.Core;
using SubTerra.App.Core.Data;
using SubTerra.App.Integration;
using SubTerra.App.Save;
using SubTerra.App.Tests.PlayMode;
using SubTerra.App.Tutorial;
using SubTerra.App.UI;
using SubTerra.App.UI.Building;
using SubTerra.App.UI.HUD;
using SubTerra.App.UI.PlayerStatus;
using SubTerra.App.UI.SurfaceBase;
using SubTerra.Gameplay.Building;
using SubTerra.Gameplay.Player;
using SubTerra.Gameplay.Structural;
using SubTerra.Shared;
using SubTerra.Shared.Audio;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using AppGasRiskLevel = SubTerra.App.State.GasRiskLevel;
using Object = UnityEngine.Object;

namespace SubTerra.App.Tests.PlayMode.Audio
{
    /// <summary>
    /// B-168: 실제 광산 씬에서 가스 맥박·피격·체력 경고·설치 실패·천장 붕괴 소리 연결을 확인한다.
    /// 요청은 SfxRequests.PlayRequested 구독(요청 지점)에서 센다.
    /// </summary>
    public sealed class PromptB168SfxD2PlayModeTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private const string LogPath = "Temp/b168-sfx-trace.log";

        private UiTestEnvironment environment;
        private readonly List<(SfxId Id, float At)> requested = new List<(SfxId, float)>();

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            SfxRequests.PlayRequested -= OnRequested;
            requested.Clear();
            ControlPreferences.IsSettingsOpen = false;
            if (environment != null) environment.Dispose();
            environment = null;
            // 광산 씬이 남으면 뒤에 도는 Gameplay 엘리베이터 검사가 그것을 잡는다.
            var clean = SceneManager.CreateScene("B168Cleanup-" + Guid.NewGuid().ToString("N"));
            SceneManager.SetActiveScene(clean);
            for (var i = SceneManager.sceneCount - 1; i >= 0; i--)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene != clean && scene.isLoaded)
                {
                    yield return SceneManager.UnloadSceneAsync(scene);
                }
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator GasPulse_FollowsVignette_StopsWhileSettingsOpen()
        {
            yield return BootToMineWithMemorySlot();
            var view = Object.FindAnyObjectByType<GasPoisonOverlayView>(FindObjectsInactive.Include);
            Assert.That(view, Is.Not.Null, "가스 화면 연출");
            var pulse = view.GetComponent<GasPulseAudio>();
            Assert.That(pulse, Is.Not.Null, "가스 맥박이 오버레이 옆에 붙어야 한다");

            // 플레이어를 가스 칸으로 옮기지 않고 타임라인 단계만 올린다(소리 연결 확인용).
            view.Timeline.SetLevel(AppGasRiskLevel.Elevated);
            yield return new WaitForSecondsRealtime(2.6f);
            var caution = pulse.PulseCount;
            Log("caution pulses=" + caution + " pitch=" + pulse.Source.pitch);
            Assert.That(caution, Is.GreaterThanOrEqualTo(1), "Caution 2초마다 맥박");
            Assert.That(pulse.Source.pitch, Is.EqualTo(1f).Within(0.01f));

            ControlPreferences.IsSettingsOpen = true;
            yield return new WaitForSecondsRealtime(0.2f);
            Assert.That(pulse.IsPlaying, Is.False, "설정창이 열리면 0.08초 페이드 뒤 멈춘다");
            var paused = pulse.PulseCount;
            yield return new WaitForSecondsRealtime(2.4f);
            Assert.That(pulse.PulseCount, Is.EqualTo(paused), "설정창이 열린 동안 무음");

            ControlPreferences.IsSettingsOpen = false;
            yield return new WaitForSecondsRealtime(2.4f);
            Assert.That(pulse.PulseCount, Is.GreaterThan(paused), "닫으면 다음 맥박부터 다시 난다");

            view.Timeline.SetLevel(AppGasRiskLevel.Hazard);
            var beforeCritical = pulse.PulseCount;
            yield return new WaitForSecondsRealtime(2.6f);
            Log("critical pulses=" + (pulse.PulseCount - beforeCritical) + " pitch=" + pulse.Source.pitch);
            Assert.That(pulse.PulseCount - beforeCritical, Is.GreaterThanOrEqualTo(2), "Critical 1.2초마다");
            Assert.That(pulse.Source.pitch, Is.EqualTo(1.08f).Within(0.01f));

            view.Timeline.SetLevel(AppGasRiskLevel.Safe);
            yield return new WaitForSecondsRealtime(0.2f);
            var left = pulse.PulseCount;
            yield return new WaitForSecondsRealtime(2.5f);
            Assert.That(pulse.PulseCount, Is.EqualTo(left), "가스 밖이면 멈춘다");
        }

        [UnityTest]
        public IEnumerator Hurt_ThenLowHealthWarning_FinalBlowSilent()
        {
            yield return BootToMineWithMemorySlot();
            var failure = Object.FindAnyObjectByType<RunFailureRuntimeController>();
            Assert.That(failure, Is.Not.Null);
            var survival = failure.SurvivalController;
            Assert.That(survival, Is.Not.Null);
            Assert.That(survival.GetComponent<LowHealthWarningAudio>(), Is.Not.Null, "체력 경고가 생존 컨트롤러에 붙어야 한다");

            survival.SetHealthAbsolute(50);
            yield return new WaitForSecondsRealtime(0.9f);
            StartRecording("collapse-impact 50->25");
            Assert.That(survival.ApplyCollapseImpact(), Is.True);
            yield return new WaitForSecondsRealtime(0.6f);
            Assert.That(Count(SfxId.PlayerHurt), Is.EqualTo(1), "무너진 돌 피해는 피격음 한 번");
            Assert.That(Count(SfxId.LowHealthWarning), Is.EqualTo(1), "처음 30% 아래면 경고 한 번");
            var delay = TimeOf(SfxId.LowHealthWarning) - TimeOf(SfxId.PlayerHurt);
            Log("warning delay=" + delay.ToString("F3"));
            Assert.That(delay, Is.InRange(0.2f, 0.4f), "피격음 0.25초 뒤");

            yield return new WaitForSecondsRealtime(0.4f);
            StartRecording("collapse-impact 25->0");
            Assert.That(survival.ApplyCollapseImpact(), Is.True);
            yield return new WaitForSecondsRealtime(0.6f);
            Assert.That(Count(SfxId.PlayerHurt), Is.Zero, "마지막 한 방은 무음");
            Assert.That(Count(SfxId.LowHealthWarning), Is.Zero);
        }

        [UnityTest]
        public IEnumerator PlacementFailure_PlaysToneAndHeadWarning_CollapsePlaysOnce()
        {
            yield return BootToMineWithMemorySlot();
            var menu = Object.FindAnyObjectByType<BuildingMenuBinder>(FindObjectsInactive.Include);
            Assert.That(menu, Is.Not.Null);
            Assert.That(menu.Presenter, Is.Not.Null);
            Assert.That(menu.Presenter.SelectBuilding(DataIds.Buildings.LadderBasic), Is.True, "사다리 선택");
            var placement = Object.FindAnyObjectByType<BuildingPlacementSystem>();
            var player = Object.FindAnyObjectByType<PlayerMovement>();
            Assert.That(placement, Is.Not.Null);
            Assert.That(player, Is.Not.Null);
            var far = placement.WorldToCell(player.transform.position) + new Vector3Int(40, 0, 0);

            StartRecording("place far");
            placement.TryPlaceAt(far);
            yield return new WaitForSecondsRealtime(0.4f);
            var first = Count(SfxId.BuildFailBlocked) + Count(SfxId.BuildFailResource);
            Log("fail tones blocked=" + Count(SfxId.BuildFailBlocked) + " resource=" + Count(SfxId.BuildFailResource));
            Assert.That(first, Is.EqualTo(1), "설치 실패음 한 번");
            Assert.That(Count(SfxId.UiCancelFail), Is.Zero);

            var stack = PlayerStatusWarnings.Current as PlayerHeadMessageStack;
            Assert.That(stack, Is.Not.Null, "머리 위 문구 스택");
            var entry = Find(stack, "building.placement-failed");
            Assert.That(entry, Is.Not.Null, "설치 실패 노란 경고");
            Log("warning text=" + entry.Text + " level=" + entry.Level + " style=" + entry.Style);
            Assert.That(entry.Style, Is.EqualTo(PlayerStatusStyle.Warning));
            Assert.That(entry.Level, Is.GreaterThan(0f), "경고가 화면에 나타난다");

            yield return new WaitForSecondsRealtime(0.2f);
            placement.TryPlaceAt(far);
            yield return new WaitForSecondsRealtime(0.4f);
            Assert.That(Count(SfxId.BuildFailBlocked) + Count(SfxId.BuildFailResource), Is.EqualTo(2), "다시 시도하면 다시 난다");
            menu.Presenter.CancelSelection();

            var feedback = Object.FindAnyObjectByType<StructuralRiskFeedback>(FindObjectsInactive.Include);
            Assert.That(feedback, Is.Not.Null);
            Assert.That(typeof(StructuralRiskFeedback).GetField("structuralSystem", Private).GetValue(feedback),
                Is.Not.Null, "붕괴 이벤트 연결");
            StartRecording("collapse");
            var collapse = new StructuralCollapseEventDto();
            collapse.cells.Add(new CollapseCellDto { x = 1, y = 1 });
            typeof(StructuralRiskFeedback).GetMethod("OnCollapseTriggered", Private).Invoke(feedback, new object[] { collapse });
            yield return null;
            Assert.That(Count(SfxId.CeilingCollapse), Is.EqualTo(1));
        }

        private static PlayerStatusEntry Find(PlayerHeadMessageStack stack, string key)
        {
            var entries = stack.Model.Entries;
            for (var i = 0; i < entries.Count; i++)
            {
                if (entries[i].Key == key) return entries[i];
            }

            return null;
        }

        private IEnumerator BootToMineWithMemorySlot()
        {
            environment = new UiTestEnvironment();
            GameBootstrapper.ResetInstanceForTests();
            yield return SceneManager.LoadSceneAsync(SceneNames.Bootstrap);
            yield return UiTestWait.Until(() => SaveRuntimeController.Instance != null
                && SaveRuntimeController.Instance.InventoryService != null
                && SceneManager.GetActiveScene().name == SceneNames.MainMenu, "Bootstrap runtime and MainMenu", 20f);
            var runtime = SaveRuntimeController.Instance;
            var fs = new MemoryFileSystem();
            var paths = new SavePathPolicy(Path.Combine(Path.GetTempPath(), "subterra-b168-memory"));
            var mapper = new SaveDataMapper(new SystemSaveClock());
            var json = new SaveJsonCodec(new SaveMigrationService());
            typeof(SaveRuntimeController).GetField("saveService", Private)
                .SetValue(runtime, new SaveService(fs, paths, mapper, json));
            typeof(SaveRuntimeController).GetField("loadService", Private)
                .SetValue(runtime, new LoadService(fs, paths, mapper, json));
            Assert.That(runtime.StartNewGame(1), Is.True);
            yield return UiTestWait.Until(() => SceneManager.GetActiveScene().name == SceneNames.SurfaceBase
                && runtime.IsUiReady, "memory-slot SurfaceBase", 20f);
            var state = GameBootstrapper.Instance.State;
            state.SetDemoProgress(DemoObjectiveIds.MineCopper, 1, false);
            state.SetQuestRewardSettlement(string.Empty, 1);
            yield return environment.Resolution.Set(1920, 1080);
            environment.BindInput();
            var surface = Object.FindAnyObjectByType<SurfaceBaseView>();
            var explore = (Button)typeof(SurfaceBaseView).GetField("exploreButton", Private).GetValue(surface);
            yield return UiTestWait.Until(() => explore.IsInteractable(), "explore button ready", 5f);
            yield return Click(environment.Mouse, explore);
            yield return UiTestWait.Until(() => SceneManager.GetActiveScene().name == SceneNames.Integration,
                "mine scene", 20f, "scene");
            yield return UiTestWait.Until(() =>
            {
                var integration = Object.FindAnyObjectByType<IntegrationRuntimeBinder>();
                return integration != null && integration.IsUiActivated;
            }, "IntegrationRuntimeBinder.IsUiActivated", 20f);
            yield return UiTestEnvironment.DismissBriefing();
            yield return UiTestWait.Until(() => !ControlPreferences.IsSettingsOpen && !UiPauseGate.IsHeld,
                "mine ready", 5f, "animation");
        }

        private static IEnumerator Click(Mouse mouse, Button button)
        {
            yield return UiTestWait.FocusGameView();
            var position = (Vector2)button.transform.position;
            InputSystem.QueueStateEvent(mouse, new MouseState { position = position });
            yield return null;
            InputSystem.QueueStateEvent(mouse, new MouseState { position = position, buttons = 1 });
            yield return UiTestWait.Until(() => mouse.leftButton.isPressed, "mouse down", 2f, "input");
            yield return null;
            InputSystem.QueueStateEvent(mouse, new MouseState { position = position });
            yield return UiTestWait.Until(() => !mouse.leftButton.isPressed, "mouse up", 2f, "input");
            yield return null;
        }

        private void StartRecording(string label)
        {
            requested.Clear();
            SfxRequests.PlayRequested -= OnRequested;
            SfxRequests.PlayRequested += OnRequested;
            Log("---- " + label + " frame=" + Time.frameCount + " t=" + Time.unscaledTime.ToString("F3"));
        }

        private void OnRequested(SfxId id, float pitch)
        {
            requested.Add((id, Time.realtimeSinceStartup));
            Log("request frame=" + Time.frameCount + " t=" + Time.unscaledTime.ToString("F3") + " id=" + id);
        }

        private int Count(SfxId id)
        {
            var count = 0;
            for (var i = 0; i < requested.Count; i++)
            {
                if (requested[i].Id == id) count++;
            }

            return count;
        }

        private float TimeOf(SfxId id)
        {
            for (var i = 0; i < requested.Count; i++)
            {
                if (requested[i].Id == id) return requested[i].At;
            }

            return float.NaN;
        }

        private static void Log(string line)
        {
            Directory.CreateDirectory("Temp");
            File.AppendAllText(LogPath, "[B168-TEST] " + TestContext.CurrentContext.Test.Name + " " + line + "\n");
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

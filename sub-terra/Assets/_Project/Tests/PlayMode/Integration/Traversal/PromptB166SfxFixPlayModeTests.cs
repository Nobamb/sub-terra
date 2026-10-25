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
using SubTerra.App.Tutorial;
using SubTerra.App.UI;
using SubTerra.App.UI.EmergencyEscape;
using SubTerra.App.UI.SurfaceBase;
using SubTerra.Gameplay.Building;
using SubTerra.Gameplay.Player;
using SubTerra.Gameplay.Power;
using SubTerra.Shared;
using SubTerra.Shared.Audio;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SubTerra.App.Tests.PlayMode.Traversal
{
    /// <summary>
    /// B-166: 긴급 탈출 성공 뒤 실패음, 엘리베이터 탑승 중 ESC 설정창.
    /// 가상 키보드·마우스 상태를 Input System에 넣어 EventSystem·입력 액션을 실제로 타게 한다.
    /// </summary>
    public sealed class PromptB166SfxFixPlayModeTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private const string LogPath = "Temp/b166-sfx-trace.log";
        private const float SilenceWindow = 1.5f;

        private UiTestEnvironment environment;
        private readonly List<SfxId> requested = new List<SfxId>();
        private EmergencyEscapePanelBinder binder;
        private EmergencyEscapePanelView view;

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            SfxRequests.PlayRequested -= OnRequested;
            requested.Clear();
            ControlPreferences.IsSettingsOpen = false;
            UiPauseGate.Release(EmergencyEscapePanelBinder.PauseOwner);
            if (environment != null) environment.Dispose();
            environment = null;
            // 광산·지상 씬의 플레이어·엘리베이터가 남으면 뒤에 도는 Gameplay 엘리베이터 검사가 그것을 탑승자·이동 포트로 잡는다.
            var clean = SceneManager.CreateScene("B166Cleanup-" + Guid.NewGuid().ToString("N"));
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
        public IEnumerator Escape_Elevator_MouseClick_NoFailSfx() => EscapeSucceedsWithoutFail(false, true);

        [UnityTest]
        public IEnumerator Escape_Elevator_Enter_NoFailSfx() => EscapeSucceedsWithoutFail(false, false);

        [UnityTest]
        public IEnumerator Escape_Outpost_MouseClick_NoFailSfx() => EscapeSucceedsWithoutFail(true, true);

        [UnityTest]
        public IEnumerator Escape_Outpost_Enter_NoFailSfx() => EscapeSucceedsWithoutFail(true, false);

        [UnityTest]
        public IEnumerator Escape_DimmedDestination_MouseClick_PlaysFailOnce()
        {
            environment = new UiTestEnvironment();
            yield return environment.LoadIntegration();
            yield return OpenEscape(false, 0);
            var confirm = ConfirmButton();
            Assert.That(confirm.IsInteractable(), Is.False, "골드가 없으면 이동 버튼이 흐려야 한다");
            StartRecording("dimmed mouse");
            yield return ClickRaw(environment.Mouse, confirm);
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.That(Count(SfxId.UiCancelFail), Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator Elevator_EscWhileRiding_DoesNotOpenSettings_AndArrivesAtSurface()
        {
            yield return BootToMineWithMemorySlot();
            var runtime = SaveRuntimeController.Instance;
            var elevator = Object.FindAnyObjectByType<ElevatorController>();
            var underground = Object.FindAnyObjectByType<UndergroundMenuController>();
            Assert.That(elevator, Is.Not.Null);
            Assert.That(underground, Is.Not.Null);
            yield return Board(elevator);

            yield return UiTestWait.Until(() => elevator.State == ElevatorTravelState.Calling
                || elevator.State == ElevatorTravelState.Moving, "elevator calling", 3f);
            yield return TapEscWhileInMine(elevator, underground, runtime, "calling");
            yield return UiTestWait.Until(() => elevator == null || elevator.State == ElevatorTravelState.Moving,
                "elevator moving", 5f);
            yield return TapEscWhileInMine(elevator, underground, runtime, "moving");
            yield return UiTestWait.Until(() => elevator == null || runtime.ElevatorState == ElevatorTravelState.Moving,
                "runtime travel committed", 8f);
            yield return TapEscWhileInMine(elevator, underground, runtime, "committed");

            yield return UiTestWait.Until(() => SceneManager.GetActiveScene().name == SceneNames.SurfaceBase,
                "surface arrival", 15f);
            Log("surface scene active frame=" + Time.frameCount + " settingsOpen=" + ControlPreferences.IsSettingsOpen);
            Assert.That(ControlPreferences.IsSettingsOpen, Is.False);
            yield return UiTestWait.Until(() => runtime.IsUiReady, "surface ui ready", 10f);
            yield return new WaitForSecondsRealtime(0.3f);
            yield return UiTestWait.Press(environment.Keyboard, Key.Escape);
            Assert.That(ControlPreferences.IsSettingsOpen, Is.True, "지상 도착 뒤에는 ESC로 설정창이 열려야 한다");
            yield return UiTestWait.Press(environment.Keyboard, Key.Escape);
        }

        [UnityTest]
        public IEnumerator Elevator_TravelRejected_EscOpensSettingsAgain()
        {
            // 슬롯 0 격리 런타임은 런타임 이동을 거절하므로 객실이 내려와 Blocked가 된다.
            environment = new UiTestEnvironment();
            yield return environment.LoadIntegration();
            var elevator = Object.FindAnyObjectByType<ElevatorController>();
            var underground = Object.FindAnyObjectByType<UndergroundMenuController>();
            yield return Board(elevator);
            yield return UiTestWait.Until(() => elevator.State == ElevatorTravelState.Calling
                || elevator.State == ElevatorTravelState.Moving, "elevator calling", 3f);
            yield return TapEscWhileInMine(elevator, underground, SaveRuntimeController.Instance, "rejected-ride");
            yield return UiTestWait.Until(() => elevator.State == ElevatorTravelState.Blocked, "travel rejected", 10f);
            Log("rejected state=" + elevator.State + " frame=" + Time.frameCount);
            yield return UiTestWait.Press(environment.Keyboard, Key.Escape);
            Log("esc after rejected state=" + elevator.State + " settingsOpen=" + underground.IsSettingsOpen);
            Assert.That(underground.IsSettingsOpen, Is.True, "탑승이 취소되면 바로 설정창을 열 수 있어야 한다");
            yield return UiTestWait.Press(environment.Keyboard, Key.Escape);
            Assert.That(underground.IsSettingsOpen, Is.False);
        }

        // Enter를 닫기 연출(0.5초)보다 오래 누르면 정지가 풀린 뒤 채굴 키로 읽혀 엘리베이터 칸을 캐려다 실패음이 났다.
        [UnityTest]
        public IEnumerator Escape_Elevator_EnterHeldPastClose_NoFailSfx() => EscapeSucceedsWithoutFail(false, false, 0.9f);

        private IEnumerator EscapeSucceedsWithoutFail(bool outpost, bool mouse, float enterHoldSeconds = 0f)
        {
            environment = new UiTestEnvironment();
            yield return environment.LoadIntegration();
            if (outpost)
            {
                SpawnOutpostCore();
            }

            yield return OpenEscape(outpost, 1000);
            if (outpost)
            {
                yield return UiTestWait.Press(environment.Keyboard, Key.DownArrow);
                Assert.That(view.SelectedDestinationIndex, Is.EqualTo(1), "거점 선택");
            }

            var state = GameBootstrapper.Instance.State;
            var goldBefore = state.Player.Gold;
            var label = (outpost ? "outpost" : "elevator") + " " + (mouse ? "mouse" : "enter")
                + (enterHoldSeconds > 0f ? " held" : string.Empty);
            StartRecording(label);
            if (mouse)
            {
                yield return UiTestWait.Click(environment.Mouse, ConfirmButton());
            }
            else if (enterHoldSeconds > 0f)
            {
                yield return Hold(Key.Enter, enterHoldSeconds);
            }
            else
            {
                yield return UiTestWait.Press(environment.Keyboard, Key.Enter);
            }

            Assert.That(state.Player.Gold, Is.LessThan(goldBefore), "탈출 성공(비용 지불)");
            yield return new WaitForSecondsRealtime(SilenceWindow);
            Log("end " + label + " requests=" + string.Join(",", requested));
            Assert.That(Count(SfxId.UiCancelFail), Is.Zero, label + ": " + string.Join(",", requested));
        }

        private IEnumerator OpenEscape(bool expectOutpost, int gold)
        {
            GameBootstrapper.Instance.State.SetGold(gold);
            var bridge = Object.FindAnyObjectByType<EmergencyEscapePortalRuntimeBridge>();
            Assert.That(bridge, Is.Not.Null);
            Assert.That(bridge.TryOpenEscapePanel(out var reason), Is.True, reason);
            binder = Object.FindAnyObjectByType<EmergencyEscapePanelBinder>();
            view = (EmergencyEscapePanelView)typeof(EmergencyEscapePanelBinder).GetField("view", Private).GetValue(binder);
            yield return UiTestWait.Until(() => binder.IsOpen && !view.IsOpening, "escape panel open", 6f, "animation");
            if (expectOutpost)
            {
                Assert.That(view.Model.Count, Is.GreaterThanOrEqualTo(2), "거점 목적지");
            }
        }

        private Button ConfirmButton() =>
            (Button)typeof(EmergencyEscapePanelBinder).GetField("confirmButton", Private).GetValue(binder);

        private static void SpawnOutpostCore()
        {
            var catalog = GameBootstrapper.Instance.AssignedCatalog as GameDataCatalog;
            Assert.That(catalog, Is.Not.Null);
            Assert.That(catalog.TryGetBuilding(DataIds.Buildings.OutpostCoreBasic, out var data), Is.True);
            var player = Object.FindAnyObjectByType<PlayerMovement>();
            var position = player.transform.position + new Vector3(6f, 0f, 0f);
            var instance = Object.Instantiate(data.RuntimePrefab, position, Quaternion.identity);
            const string id = DataIds.Buildings.OutpostCoreBasic + "-b166-001";
            var building = instance.GetComponent<BuildingInstance>();
            if (building == null) building = instance.AddComponent<BuildingInstance>();
            building.Initialize(id, DataIds.Buildings.OutpostCoreBasic);
            var node = instance.GetComponent<PowerNode>();
            if (node != null)
            {
                node.SetEntityId(id);
                node.SetNetwork(Object.FindAnyObjectByType<PowerNetworkSystem>());
            }
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
            Assert.That(runtime.ActiveSlot, Is.Zero);
            var fs = new MemoryFileSystem();
            var paths = new SavePathPolicy(Path.Combine(Path.GetTempPath(), "subterra-b166-memory"));
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
            yield return ClickThenEsc(explore);
            yield return UiTestWait.Until(() => SceneManager.GetActiveScene().name == SceneNames.Integration,
                "mine scene", 20f, "scene");
            yield return UiTestWait.Until(() =>
            {
                var integration = Object.FindAnyObjectByType<IntegrationRuntimeBinder>();
                return integration != null && integration.IsUiActivated;
            }, "IntegrationRuntimeBinder.IsUiActivated", 20f);
            var underground = Object.FindAnyObjectByType<UndergroundMenuController>();
            if (underground != null && underground.IsSettingsOpen)
            {
                // 광산에 도착한 뒤 읽힌 ESC는 정상 동작이라 닫고 진행한다.
                underground.CloseSettings();
            }

            yield return UiTestWait.Until(() => !ControlPreferences.IsSettingsOpen, "mine settings closed", 5f, "animation");
            yield return UiTestEnvironment.DismissBriefing();
        }

        // 탐사 시작을 누른 바로 다음 프레임에 ESC를 눌러 씬 전환 전 틈을 확인한다.
        private IEnumerator ClickThenEsc(Button button)
        {
            var mouse = environment.Mouse;
            var keyboard = environment.Keyboard;
            yield return UiTestWait.FocusGameView();
            UiTestWait.AssertClickTarget(button);
            var position = (Vector2)button.transform.position;
            InputSystem.QueueStateEvent(mouse, new MouseState { position = position });
            yield return null;
            InputSystem.QueueStateEvent(mouse, new MouseState { position = position, buttons = 1 });
            yield return UiTestWait.Until(() => mouse.leftButton.isPressed, "explore mouse down", 2f, "input");
            yield return null;
            InputSystem.QueueStateEvent(mouse, new MouseState { position = position });
            yield return UiTestWait.Until(() => !mouse.leftButton.isPressed, "explore mouse up", 2f, "input");
            Log("explore click frame=" + Time.frameCount + " scene=" + SceneManager.GetActiveScene().name
                + " runtimeElevator=" + SaveRuntimeController.Instance.ElevatorState);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape));
            for (var i = 0; i < 3; i++)
            {
                yield return null;
                Log("explore+esc frame=" + Time.frameCount + " scene=" + SceneManager.GetActiveScene().name
                    + " settingsOpen=" + ControlPreferences.IsSettingsOpen
                    + " runtimeElevator=" + SaveRuntimeController.Instance.ElevatorState);
                if (SceneManager.GetActiveScene().name == SceneNames.SurfaceBase)
                {
                    Assert.That(ControlPreferences.IsSettingsOpen, Is.False, "탐사 시작 뒤 지상 씬에서 설정창이 열리면 안 된다");
                }
            }

            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return null;
        }

        private IEnumerator Board(ElevatorController elevator)
        {
            MovePlayerInto(elevator);
            yield return new WaitForSecondsRealtime(0.3f);
            Log("board press E state=" + elevator.State + " hasRider=" + elevator.HasRider);
            yield return Tap(Key.E);
            Log("after E state=" + elevator.State + " frame=" + Time.frameCount);
        }

        private static void MovePlayerInto(ElevatorController elevator)
        {
            var anchor = (Transform)typeof(ElevatorController).GetField("boardingAnchor", Private).GetValue(elevator);
            var player = Object.FindAnyObjectByType<PlayerMovement>();
            var target = anchor != null ? anchor.position : elevator.transform.position;
            var body = player.GetComponent<Rigidbody2D>();
            if (body != null)
            {
                body.position = target;
                body.linearVelocity = Vector2.zero;
            }

            player.transform.position = target;
        }

        private IEnumerator TapEscWhileInMine(ElevatorController elevator, UndergroundMenuController underground,
            SaveRuntimeController runtime, string phase)
        {
            if (elevator == null || SceneManager.GetActiveScene().name != SceneNames.Integration)
            {
                Log("esc " + phase + " skipped: scene already changed frame=" + Time.frameCount);
                yield break;
            }

            var stateAtPress = elevator.State;
            yield return Tap(Key.Escape);
            var stillMine = elevator != null && SceneManager.GetActiveScene().name == SceneNames.Integration;
            Log("esc " + phase + " frame=" + Time.frameCount + " elevator=" + stateAtPress
                + " runtime=" + runtime.ElevatorState
                + " settingsOpen=" + (stillMine && underground != null && underground.IsSettingsOpen)
                + " flag=" + ControlPreferences.IsSettingsOpen + " scene=" + SceneManager.GetActiveScene().name);
            if (stillMine)
            {
                Assert.That(underground.IsSettingsOpen, Is.False, phase + " 단계 ESC");
                Assert.That(ControlPreferences.IsSettingsOpen, Is.False, phase + " 단계 ESC");
            }
        }

        private IEnumerator Tap(Key key)
        {
            var keyboard = environment.Keyboard;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(key));
            yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return null;
        }

        private IEnumerator Hold(Key key, float seconds)
        {
            var keyboard = environment.Keyboard;
            yield return UiTestWait.FocusGameView();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(key));
            yield return new WaitForSecondsRealtime(seconds);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return null;
        }

        // 비활성 버튼도 누를 수 있도록 상호작용 검사 없이 클릭한다.
        private static IEnumerator ClickRaw(Mouse mouse, Button button)
        {
            yield return UiTestWait.FocusGameView();
            var position = (Vector2)button.transform.position;
            InputSystem.QueueStateEvent(mouse, new MouseState { position = position });
            yield return null;
            InputSystem.QueueStateEvent(mouse, new MouseState { position = position, buttons = 1 });
            yield return UiTestWait.Until(() => mouse.leftButton.isPressed, "raw mouse down", 2f, "input");
            yield return null;
            InputSystem.QueueStateEvent(mouse, new MouseState { position = position });
            yield return UiTestWait.Until(() => !mouse.leftButton.isPressed, "raw mouse up", 2f, "input");
            yield return null;
        }

        private void StartRecording(string label)
        {
            requested.Clear();
            SfxRequests.PlayRequested -= OnRequested;
            SfxRequests.PlayRequested += OnRequested;
            Log("---- confirm " + label + " frame=" + Time.frameCount + " t=" + Time.unscaledTime.ToString("F3"));
        }

        private void OnRequested(SfxId id, float pitch)
        {
            requested.Add(id);
            Log("request frame=" + Time.frameCount + " t=" + Time.unscaledTime.ToString("F3") + " id=" + id);
        }

        private int Count(SfxId id)
        {
            var count = 0;
            for (var i = 0; i < requested.Count; i++)
            {
                if (requested[i] == id) count++;
            }

            return count;
        }

        private static void Log(string line)
        {
            Directory.CreateDirectory("Temp");
            File.AppendAllText(LogPath, "[B166-TEST] " + TestContext.CurrentContext.Test.Name + " " + line + "\n");
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

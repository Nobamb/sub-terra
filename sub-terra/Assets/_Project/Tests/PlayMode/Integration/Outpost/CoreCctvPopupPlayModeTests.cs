using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using SubTerra.App.Core;
using SubTerra.App.Core.Data;
using SubTerra.App.Integration;
using SubTerra.App.Outpost;
using SubTerra.App.UI.Outpost;
using SubTerra.Gameplay.Building;
using SubTerra.Gameplay.Player;
using SubTerra.Gameplay.Power;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SubTerra.App.Tests.PlayMode
{
    /// <summary>
    /// 전진기지 코어 CCTV 팝업을 실제 Integration Scene에서 검증한다.
    /// 시설은 실제 런타임 프리팹으로 설치하고, 연결 상태와 접근 범위는 실제 GameplayEventBridge가 계산한다.
    /// 연출 시각은 popup.Tick으로 직접 진행해 캡처 비용과 무관하게 일정하다.
    /// </summary>
    public sealed class CoreCctvPopupPlayModeTests
    {
        private const string EvidenceDirectory = "Temp/core-cctv-evidence";
        private const float Frame = 1f / 60f;

        private UiTestEnvironment env;
        private CoreCctvScene scene;

        [SetUp]
        public void SetUp()
        {
            Directory.CreateDirectory(EvidenceDirectory);
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            scene?.Dispose();
            scene = null;
            env?.Dispose();
            env = null;
        }

        // ---- 연결된 시설 0개 / 1개 / 여러 개 ----

        [UnityTest]
        public IEnumerator Open_WithFacilities_PlaysIntroThenShowsLiveCctvOfFirstFacility()
        {
            yield return Load();
            scene.SpawnCore();
            scene.SpawnFacility(DataIds.Buildings.ChargerBasic, new Vector3(5f, 0f, 0f));
            scene.SpawnFacility(DataIds.Buildings.ClinicBasic, new Vector3(-6f, 0f, 0f));
            yield return scene.WaitForStatus();
            var main = MainCameraSnapshot.Take();
            var camerasBefore = Camera.allCamerasCount;

            scene.Open();
            var popup = scene.Popup;
            popup.ManualTick = true;
            Assert.That(popup.State, Is.EqualTo(CoreCctvPopupView.PlayState.Intro));

            // 등장 연출 구간별 상태. 목록과 영상은 터미널·연결 표시 뒤에 나온다.
            yield return Advance(popup, 0.2f);
            Assert.That(popup.ListAlpha, Is.Zero);
            Assert.That(popup.VideoLevel, Is.Zero);
            Assert.That(popup.Rig == null || !popup.Rig.IsRendering, Is.True, "영상이 켜지기 전에는 렌더링하지 않는다.");
            yield return Advance(popup, 0.45f);
            Assert.That(popup.TerminalAlpha, Is.EqualTo(1f));
            yield return Advance(popup, CoreCctvTimeline.DecideTime + 0.04f - popup.Clock);
            Assert.That(popup.ConnectedLabelAlpha, Is.EqualTo(1f));
            Assert.That(popup.TerminalAlpha, Is.Zero);
            yield return Advance(popup, CoreCctvTimeline.IntroDuration(3) - popup.Clock + 0.05f);

            Assert.That(popup.State, Is.EqualTo(CoreCctvPopupView.PlayState.Live));
            Assert.That(popup.Video, Is.EqualTo(CoreCctvPopupView.VideoState.On));
            Assert.That(popup.ConnectedLabelAlpha, Is.Zero, "연결되었습니다는 등장 연출에서만 쓴다.");
            Assert.That(popup.RecAlpha, Is.EqualTo(1f).Within(0.001f));
            Assert.That(popup.ItemCount, Is.EqualTo(2));
            Assert.That(popup.List.SelectedIndex, Is.EqualTo(0), "첫 시설이 자동 선택된다.");

            // 전용 카메라: 하나만 만들고, 메인 카메라는 건드리지 않는다.
            var rig = popup.Rig;
            Assert.That(rig, Is.Not.Null);
            Assert.That(rig.IsRendering, Is.True);
            Assert.That(Camera.allCamerasCount, Is.EqualTo(camerasBefore + 1));
            Assert.That(rig.Camera.CompareTag("MainCamera"), Is.False);
            Assert.That(rig.Camera.GetComponent<AudioListener>(), Is.Null);
            Assert.That(rig.Texture.filterMode, Is.EqualTo(FilterMode.Point));
            main.AssertUnchanged();
            AssertCameraShowsSelected(popup);
            AssertRenderTextureMatchesScreen(popup);
            AssertNoUiCanvasRendersToCamera();
            AssertVideoIsNotBlank(popup);

            yield return Capture("final-layout-1920x1080");
        }

        [UnityTest]
        public IEnumerator Capture_PowerOnSequence()
        {
            yield return Load();
            scene.SpawnCore();
            scene.SpawnFacility(DataIds.Buildings.ChargerBasic, new Vector3(5f, 0f, 0f));
            scene.SpawnFacility(DataIds.Buildings.ClinicBasic, new Vector3(-6f, 0f, 0f));
            yield return scene.WaitForStatus();

            scene.Open();
            var popup = scene.Popup;
            popup.ManualTick = true;
            yield return CaptureSequence(popup, "intro", 1.45f, 1f / 30f);
            Assert.That(popup.State, Is.EqualTo(CoreCctvPopupView.PlayState.Live));
        }

        [UnityTest]
        public IEnumerator Open_WithNoFacilities_KeepsBlackScreenAndShowsOnlyEmptyList()
        {
            yield return Load();
            scene.SpawnCore();
            yield return scene.WaitForStatus();

            scene.Open();
            var popup = scene.Popup;
            popup.ManualTick = true;
            var maxConnected = 0f;
            var maxRec = 0f;
            for (var t = 0f; t < 1.6f; t += Frame)
            {
                popup.Tick(Frame);
                maxConnected = Mathf.Max(maxConnected, popup.ConnectedLabelAlpha);
                maxRec = Mathf.Max(maxRec, popup.RecAlpha);
                yield return null;
            }

            Assert.That(popup.State, Is.EqualTo(CoreCctvPopupView.PlayState.Live));
            Assert.That(maxConnected, Is.Zero, "연결되었습니다는 표시하지 않는다.");
            Assert.That(maxRec, Is.Zero, "REC는 표시하지 않는다.");
            Assert.That(popup.Video, Is.EqualTo(CoreCctvPopupView.VideoState.Off));
            Assert.That(popup.VideoLevel, Is.Zero);
            Assert.That(popup.ItemCount, Is.Zero);
            Assert.That(popup.EmptyLabelAlpha, Is.EqualTo(1f).Within(0.001f));
            Assert.That(popup.TerminalAlpha, Is.Zero);
            Assert.That(popup.Rig == null || !popup.Rig.IsRendering, Is.True, "검은 화면에서는 렌더링하지 않는다.");
            Assert.That(popup.GhostsVisible, Is.False);

            // 이후에도 번쩍이거나 오류 연출을 반복하지 않는다.
            var level = popup.VideoLevel;
            for (var i = 0; i < 60; i++)
            {
                popup.Tick(Frame);
                Assert.That(popup.VideoLevel, Is.EqualTo(level));
                yield return null;
            }

            yield return Capture("empty-1920x1080");
        }

        [UnityTest]
        public IEnumerator Open_WithSingleFacility_ShowsOneItemAndItsLocation()
        {
            yield return Load();
            scene.SpawnCore();
            var charger = scene.SpawnFacility(DataIds.Buildings.ChargerBasic, new Vector3(-7f, 0f, 0f));
            yield return scene.WaitForStatus();

            scene.Open();
            var popup = scene.Popup;
            popup.ManualTick = true;
            yield return Advance(popup, 2f);

            Assert.That(popup.ItemCount, Is.EqualTo(1));
            Assert.That(popup.ScrollbarVisible, Is.False, "항목이 영역을 넘지 않으면 스크롤바는 없다.");
            AssertCameraShowsFacility(popup, charger);
            yield return Capture("single-1920x1080");
        }

        // ---- 목록 스크롤과 키보드 ----

        [UnityTest]
        public IEnumerator ManyFacilities_ScrollsWithWheelAndKeyboard_KeepingSelectionVisible()
        {
            yield return Load();
            scene.SpawnCore();
            for (var i = 0; i < 9; i++)
            {
                scene.SpawnFacility(
                    i % 2 == 0 ? DataIds.Buildings.ChargerBasic : DataIds.Buildings.ClinicBasic,
                    new Vector3(-8f + i * 2f, 0f, 0f));
            }

            yield return scene.WaitForStatus();
            scene.Open();
            var popup = scene.Popup;
            // 실제 입력은 Update에서 읽으므로 이 테스트는 실시간으로 진행한다.
            yield return UiTestWait.Until(
                () => popup.State == CoreCctvPopupView.PlayState.Live && popup.Video == CoreCctvPopupView.VideoState.On,
                "core cctv live");

            Assert.That(popup.ItemCount, Is.EqualTo(9));
            yield return null;
            yield return null;
            Assert.That(popup.ScrollbarVisible, Is.True, "항목이 영역을 넘으면 스크롤바가 나타난다.");
            Assert.That(popup.ListContent.anchoredPosition.y, Is.Zero);
            yield return Capture("scroll-top-1920x1080");

            // 키보드(아래 화살표)로 선택을 옮기면 선택 항목이 항상 보인다.
            for (var step = 1; step < 9; step++)
            {
                yield return UiTestWait.Press(env.Keyboard, Key.DownArrow);
                Assert.That(popup.List.SelectedIndex, Is.EqualTo(step), "아래 입력 " + step);
                AssertItemVisible(popup, step);
            }

            Assert.That(popup.ListContent.anchoredPosition.y, Is.GreaterThan(0f));
            yield return UiTestWait.Until(() => !popup.Rig.IsMoving, "camera move end");
            yield return Capture("scroll-bottom-1920x1080");
            AssertCameraShowsFacility(popup, scene.Spawned[8]);

            for (var step = 7; step >= 0; step--)
            {
                yield return UiTestWait.Press(env.Keyboard, Key.UpArrow);
                Assert.That(popup.List.SelectedIndex, Is.EqualTo(step), "위 입력 " + step);
                AssertItemVisible(popup, step);
            }

            Assert.That(popup.ListContent.anchoredPosition.y, Is.Zero);

            // 마우스 휠: 목록 위에서 굴리면 스크롤된다.
            var rect = ScreenRect((RectTransform)popup.ListViewport);
            var center = rect.center;
            yield return UiTestWait.FocusGameView();
            InputSystem.QueueStateEvent(env.Mouse, new MouseState { position = center });
            yield return null;
            InputSystem.QueueStateEvent(env.Mouse, new MouseState { position = center, scroll = new Vector2(0f, -120f) });
            yield return null;
            yield return null;
            InputSystem.QueueStateEvent(env.Mouse, new MouseState { position = center });
            yield return null;
            Assert.That(popup.ListContent.anchoredPosition.y, Is.GreaterThan(0f), "마우스 휠로 스크롤된다.");
            Assert.That(popup.List.SelectedIndex, Is.Zero, "스크롤은 선택을 바꾸지 않는다.");
        }

        // ---- 같은 종류 시설 / 선택 전환 ----

        [UnityTest]
        public IEnumerator SameKindFacilities_EachItemShowsItsOwnLocation_AndSelectionOnlyMovesTheCctv()
        {
            yield return Load();
            scene.SpawnCore();
            var a = scene.SpawnFacility(DataIds.Buildings.ChargerBasic, new Vector3(-8f, 0f, 0f));
            var b = scene.SpawnFacility(DataIds.Buildings.ChargerBasic, new Vector3(3f, 0f, 0f));
            var c = scene.SpawnFacility(DataIds.Buildings.ChargerBasic, new Vector3(8f, 0f, 0f));
            yield return scene.WaitForStatus();
            var service = scene.Service;
            var operations = 0;
            service.OperationCompleted += _ => operations++;
            var main = MainCameraSnapshot.Take();
            var playerPosition = scene.Player.transform.position;

            scene.Open();
            var popup = scene.Popup;
            popup.ManualTick = true;
            yield return Advance(popup, 2f);
            Assert.That(popup.ItemCount, Is.EqualTo(3));

            // 목록 순서는 인스턴스 ID 순(설치 순서)으로 고정이고, 각 항목은 자기 시설을 가리킨다.
            var order = new[] { a, b, c };
            for (var i = 0; i < order.Length; i++)
            {
                Assert.That(popup.List.Items[i].InstanceId, Is.EqualTo(order[i].GetComponent<BuildingInstance>().InstanceId));
            }

            AssertCameraShowsFacility(popup, a);

            yield return ClickCenter(popup.ItemAt(2).Button);
            Assert.That(popup.SelectedInstanceId, Is.EqualTo(popup.List.Items[2].InstanceId), "선택 강조는 즉시 옮겨진다.");
            Assert.That(popup.ItemAt(2).IsSelected, Is.True);
            Assert.That(popup.Rig.IsMoving, Is.True);
            var ticks = 0;
            var sawGhost = false;
            while (popup.Rig.IsMoving && ticks < 60)
            {
                yield return Capture("switch-" + ticks.ToString("D3"));
                popup.Tick(Frame);
                sawGhost |= popup.GhostsVisible;
                ticks++;
            }

            Assert.That(ticks * Frame, Is.InRange(0.2f, 0.36f), "카메라 이동은 약 0.2~0.35초.");
            Assert.That(sawGhost, Is.True, "이동 중에만 잔상이 있다.");
            Assert.That(popup.GhostsVisible, Is.False, "이동이 끝나면 바로 선명해진다.");
            AssertCameraShowsFacility(popup, c);

            yield return ClickCenter(popup.ItemAt(1).Button);
            yield return Advance(popup, 0.4f);
            AssertCameraShowsFacility(popup, b);

            // 선택은 시설을 사용하거나 플레이어/메인 카메라를 움직이지 않는다.
            Assert.That(operations, Is.Zero);
            Assert.That(Vector2.Distance(scene.Player.transform.position, playerPosition), Is.LessThan(0.1f), "플레이어는 이동하지 않는다.");
            main.AssertUnchanged();
            Assert.That(service.IsFacilityInteraction, Is.True);
        }

        [UnityTest]
        public IEnumerator RapidSelection_FollowsLastClick_ReusesOneCameraAndTexture_AndIgnoresReselect()
        {
            yield return Load();
            scene.SpawnCore();
            scene.SpawnFacility(DataIds.Buildings.ChargerBasic, new Vector3(-8f, 0f, 0f));
            scene.SpawnFacility(DataIds.Buildings.ClinicBasic, new Vector3(-2f, 0f, 0f));
            scene.SpawnFacility(DataIds.Buildings.ChargerBasic, new Vector3(4f, 0f, 0f));
            var last = scene.SpawnFacility(DataIds.Buildings.ClinicBasic, new Vector3(9f, 0f, 0f));
            yield return scene.WaitForStatus();

            scene.Open();
            var popup = scene.Popup;
            popup.ManualTick = true;
            yield return Advance(popup, 2f);
            var texture = popup.Rig.Texture;
            var camera = popup.Rig.Camera;
            var cameras = Camera.allCamerasCount;

            var order = new[] { 3, 1, 2, 0, 3 };
            foreach (var index in order)
            {
                popup.SelectFacility(popup.List.Items[index].InstanceId);
                popup.Tick(Frame * 3f);
                yield return null;
            }

            var lastId = popup.List.Items[3].InstanceId;
            Assert.That(popup.SelectedInstanceId, Is.EqualTo(lastId));
            yield return Advance(popup, 0.5f);
            AssertCameraShowsFacility(popup, last);
            Assert.That(popup.Rig.Texture, Is.SameAs(texture), "렌더 텍스처를 새로 만들지 않는다.");
            Assert.That(popup.Rig.Camera, Is.SameAs(camera));
            Assert.That(Camera.allCamerasCount, Is.EqualTo(cameras));

            // 이미 선택된 항목을 다시 눌러도 이동하지 않는다.
            popup.SelectFacility(lastId);
            Assert.That(popup.Rig.IsMoving, Is.False);
            popup.ItemAt(3).Button.onClick.Invoke();
            Assert.That(popup.Rig.IsMoving, Is.False);
        }

        // ---- 목록 변경 ----

        [UnityTest]
        public IEnumerator FacilitiesChangeWhileOpen_ListFollowsConnectionState()
        {
            yield return Load();
            scene.SpawnCore();
            var first = scene.SpawnFacility(DataIds.Buildings.ChargerBasic, new Vector3(-6f, 0f, 0f));
            var second = scene.SpawnFacility(DataIds.Buildings.ClinicBasic, new Vector3(3f, 0f, 0f));
            yield return scene.WaitForStatus();

            scene.Open();
            var popup = scene.Popup;
            popup.ManualTick = true;
            yield return Advance(popup, 2f);
            Assert.That(popup.ItemCount, Is.EqualTo(2));
            var clock = popup.Clock;

            // 추가: 선택은 유지되고 연출은 다시 시작하지 않는다.
            var added = scene.SpawnFacility(DataIds.Buildings.ChargerBasic, new Vector3(7f, 0f, 0f));
            yield return scene.WaitForStatus();
            yield return Advance(popup, 0.4f);
            Assert.That(popup.ItemCount, Is.EqualTo(3));
            Assert.That(popup.SelectedInstanceId, Is.EqualTo(first.GetComponent<BuildingInstance>().InstanceId));
            Assert.That(popup.Clock, Is.EqualTo(clock), "목록 갱신만으로 등장 연출을 다시 시작하지 않는다.");
            Assert.That(popup.State, Is.EqualTo(CoreCctvPopupView.PlayState.Live));

            // 선택한 시설이 사라지면 남은 시설 중 하나로 넘어가고 카메라도 따라간다.
            var firstId = first.GetComponent<BuildingInstance>().InstanceId;
            Object.Destroy(first);
            yield return scene.WaitForStatus();
            yield return Advance(popup, 0.5f);
            Assert.That(popup.ItemCount, Is.EqualTo(2));
            Assert.That(popup.List.IndexOf(popup.SelectedInstanceId), Is.GreaterThanOrEqualTo(0));
            Assert.That(popup.SelectedInstanceId, Is.Not.EqualTo(firstId));
            var remaining = popup.SelectedInstanceId == second.GetComponent<BuildingInstance>().InstanceId ? second : added;
            AssertCameraShowsFacility(popup, remaining);

            // 전력 범위 밖으로 나가면 연결 해제: 모두 사라지면 검은 화면과 빈 목록.
            scene.Disconnect(second);
            scene.Disconnect(added);
            yield return scene.WaitForStatus();
            yield return Advance(popup, 0.6f);
            Assert.That(popup.ItemCount, Is.Zero);
            Assert.That(popup.Video, Is.EqualTo(CoreCctvPopupView.VideoState.Off));
            Assert.That(popup.RecAlpha, Is.Zero);
            Assert.That(popup.EmptyLabelAlpha, Is.EqualTo(1f).Within(0.001f));
            Assert.That(popup.Rig.IsRendering, Is.False);
            Assert.That(popup.State, Is.EqualTo(CoreCctvPopupView.PlayState.Live));
            yield return Capture("all-removed-1920x1080");

            // 다시 연결되면 목록과 첫 시설의 영상이 켜진다.
            scene.Reconnect(second);
            yield return scene.WaitForStatus();
            yield return Advance(popup, 1f);
            Assert.That(popup.ItemCount, Is.EqualTo(1));
            Assert.That(popup.Video, Is.EqualTo(CoreCctvPopupView.VideoState.On));
            Assert.That(popup.Rig.IsRendering, Is.True);
            AssertCameraShowsFacility(popup, second);
            Assert.That(popup.State, Is.EqualTo(CoreCctvPopupView.PlayState.Live));
        }

        // ---- 닫기 / 범위 이탈 / 재접근 ----

        [UnityTest]
        public IEnumerator CloseWithX_PlaysPowerOff_StopsRendering_AndDoesNotReopenWhileInRange()
        {
            yield return Load();
            scene.SpawnCore();
            scene.SpawnFacility(DataIds.Buildings.ChargerBasic, new Vector3(5f, 0f, 0f));
            yield return scene.WaitForStatus();

            scene.Open();
            var popup = scene.Popup;
            popup.ManualTick = true;
            yield return Advance(popup, 2f);
            var rig = popup.Rig;

            popup.CloseButton.onClick.Invoke();
            Assert.That(popup.State, Is.EqualTo(CoreCctvPopupView.PlayState.Exiting));
            var ticks = 0;
            while (popup.State == CoreCctvPopupView.PlayState.Exiting && ticks < 60)
            {
                yield return Capture("exit-" + ticks.ToString("D3"));
                popup.Tick(Frame);
                ticks++;
            }

            Assert.That(ticks * Frame, Is.InRange(0.18f, 0.4f), "종료 연출은 약 0.2~0.35초.");
            AssertFullyClosed(popup);
            Assert.That(rig.IsRendering, Is.False, "닫힌 동안 CCTV 렌더링을 멈춘다.");

            // 같은 범위 안에 있어도 매 프레임 다시 열리지 않는다.
            Assert.That(scene.Service.IsFacilityInteraction, Is.True, "닫아도 시설 범위에는 남아 있다.");
            for (var i = 0; i < 40; i++)
            {
                yield return null;
                Assert.That(popup.State, Is.EqualTo(CoreCctvPopupView.PlayState.Hidden), "프레임 " + i);
            }

            // 다시 상호작용하면 열린다.
            scene.Open();
            Assert.That(popup.State, Is.EqualTo(CoreCctvPopupView.PlayState.Intro));
            yield return Advance(popup, 2f);
            Assert.That(popup.Video, Is.EqualTo(CoreCctvPopupView.VideoState.On));
            Assert.That(popup.Rig, Is.SameAs(rig), "재열기에도 같은 CCTV 구조를 재사용한다.");
            Assert.That(CountCctvCameras(), Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator CloseDuringIntro_StillClosesCleanly()
        {
            yield return Load();
            scene.SpawnCore();
            scene.SpawnFacility(DataIds.Buildings.ChargerBasic, new Vector3(5f, 0f, 0f));
            yield return scene.WaitForStatus();

            foreach (var closeAt in new[] { 0.08f, 0.25f, 0.6f, 0.95f, 1.1f })
            {
                scene.Open();
                var popup = scene.Popup;
                popup.ManualTick = true;
                yield return Advance(popup, closeAt);
                scene.Binder.ClosePanel();
                Assert.That(popup.State, Is.EqualTo(CoreCctvPopupView.PlayState.Exiting), "닫기 시점 " + closeAt);
                yield return Advance(popup, 0.5f);
                AssertFullyClosed(popup);
            }

            Assert.That(CountCctvCameras(), Is.LessThanOrEqualTo(1));
        }

        [UnityTest]
        public IEnumerator LeavingCoreRange_ClosesPopup_EvenDuringIntro_AndCctvPositionNeverAffectsRange()
        {
            yield return Load();
            scene.SpawnCore();
            scene.SpawnFacility(DataIds.Buildings.ChargerBasic, new Vector3(8f, 0f, 0f));
            yield return scene.WaitForStatus();
            var home = scene.Player.transform.position;

            scene.Open();
            var popup = scene.Popup;
            popup.ManualTick = true;
            yield return Advance(popup, 2f);
            Assert.That(popup.Rig.Position.x, Is.GreaterThan(home.x + 5f), "CCTV는 먼 시설을 보고 있다.");
            yield return Advance(popup, 0.3f);
            Assert.That(popup.State, Is.EqualTo(CoreCctvPopupView.PlayState.Live), "CCTV 카메라 위치는 접근 판정에 영향을 주지 않는다.");
            Assert.That(scene.Service.IsFacilityInteraction, Is.True);

            // 실제 플레이어-코어 거리로 판정한다: 코어에서 멀어지면 닫힌다.
            scene.MovePlayer(new Vector3(12f, 0f, 0f));
            yield return null;
            yield return null;
            Assert.That(scene.Service.IsFacilityInteraction, Is.False);
            Assert.That(popup.State, Is.EqualTo(CoreCctvPopupView.PlayState.Exiting));
            yield return Advance(popup, 0.5f);
            AssertFullyClosed(popup);

            // 등장 도중 이탈.
            scene.MovePlayer(Vector3.zero);
            yield return null;
            yield return null;
            scene.Open();
            popup.ManualTick = true;
            yield return Advance(popup, 0.3f);
            scene.MovePlayer(new Vector3(12f, 0f, 0f));
            yield return null;
            yield return null;
            Assert.That(popup.State, Is.EqualTo(CoreCctvPopupView.PlayState.Exiting));
            yield return Advance(popup, 0.5f);
            AssertFullyClosed(popup);

            // 반복 접근: 들어올 때마다 자동으로 열리지 않고 상호작용으로만 열린다.
            for (var i = 0; i < 3; i++)
            {
                scene.MovePlayer(Vector3.zero);
                yield return null;
                yield return null;
                Assert.That(popup.State, Is.EqualTo(CoreCctvPopupView.PlayState.Hidden), "접근만으로는 열리지 않는다.");
                scene.Open();
                popup.ManualTick = true;
                yield return Advance(popup, 1.5f);
                Assert.That(popup.State, Is.EqualTo(CoreCctvPopupView.PlayState.Live));
                scene.MovePlayer(new Vector3(12f, 0f, 0f));
                yield return null;
                yield return null;
                yield return Advance(popup, 0.5f);
                AssertFullyClosed(popup);
            }

            Assert.That(CountCctvCameras(), Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator EscapeKey_UsesExistingClosePath_ForCctvPopup()
        {
            yield return Load();
            scene.SpawnCore();
            scene.SpawnFacility(DataIds.Buildings.ChargerBasic, new Vector3(5f, 0f, 0f));
            yield return scene.WaitForStatus();
            var menu = Object.FindAnyObjectByType<UndergroundMenuController>();
            Assert.That(menu, Is.Not.Null);

            scene.Open();
            var popup = scene.Popup;
            popup.ManualTick = true;
            yield return Advance(popup, 2f);
            Assert.That(scene.Binder.IsTopWindow(popup.GetComponent<Canvas>()), Is.True);

            menu.HandleCloseTopPopup();
            Assert.That(popup.State, Is.EqualTo(CoreCctvPopupView.PlayState.Exiting));
            yield return Advance(popup, 0.5f);
            AssertFullyClosed(popup);
        }

        // ---- 시간 / 정리 ----

        [UnityTest]
        public IEnumerator PausedTime_StillPlaysIntroAndClose_WithoutBlockingInput()
        {
            yield return Load();
            scene.SpawnCore();
            scene.SpawnFacility(DataIds.Buildings.ChargerBasic, new Vector3(5f, 0f, 0f));
            yield return scene.WaitForStatus();

            Time.timeScale = 0f;
            scene.Open();
            var popup = scene.Popup;
            // Update가 직접 시간을 진행한다(timeScale 0이어도 unscaledDeltaTime 사용).
            var start = Time.realtimeSinceStartup;
            while (popup.State == CoreCctvPopupView.PlayState.Intro && Time.realtimeSinceStartup - start < 6f)
            {
                yield return null;
            }

            Assert.That(popup.State, Is.EqualTo(CoreCctvPopupView.PlayState.Live), "일시정지 중에도 등장 연출이 끝난다.");
            Assert.That(popup.GetComponent<CanvasGroup>().blocksRaycasts, Is.True);
            popup.CloseButton.onClick.Invoke();
            start = Time.realtimeSinceStartup;
            while (popup.State != CoreCctvPopupView.PlayState.Hidden && Time.realtimeSinceStartup - start < 3f)
            {
                yield return null;
            }

            AssertFullyClosed(popup);
            Time.timeScale = 1f;
        }

        [UnityTest]
        public IEnumerator DestroyingPopup_ReleasesCameraAndTexture()
        {
            yield return Load();
            scene.SpawnCore();
            scene.SpawnFacility(DataIds.Buildings.ChargerBasic, new Vector3(5f, 0f, 0f));
            yield return scene.WaitForStatus();

            scene.Open();
            var popup = scene.Popup;
            popup.ManualTick = true;
            yield return Advance(popup, 2f);
            Assert.That(CountCctvCameras(), Is.EqualTo(1));

            Object.Destroy(popup.gameObject);
            yield return null;
            yield return null;

            Assert.That(CountCctvCameras(), Is.Zero, "씬 전환·파괴 시 CCTV 카메라와 렌더 텍스처를 정리한다.");
        }

        // ---- 해상도 ----

        [UnityTest]
        public IEnumerator RepresentativeResolutions_KeepLayoutInsideScreen_AndReadable()
        {
            yield return Load();
            scene.SpawnCore();
            for (var i = 0; i < 6; i++)
            {
                scene.SpawnFacility(
                    i % 2 == 0 ? DataIds.Buildings.ChargerBasic : DataIds.Buildings.ClinicBasic,
                    new Vector3(-7f + i * 3f, 0f, 0f));
            }

            yield return scene.WaitForStatus();
            scene.Open();
            var popup = scene.Popup;
            popup.ManualTick = true;
            yield return Advance(popup, 2f);

            var sizes = new[]
            {
                new Vector2Int(960, 540), new Vector2Int(1280, 720), new Vector2Int(1600, 900),
                new Vector2Int(1920, 1080), new Vector2Int(2560, 1440), new Vector2Int(2560, 1080),
                new Vector2Int(1024, 768)
            };
            foreach (var size in sizes)
            {
                yield return env.Resolution.Set(size.x, size.y);
                yield return Advance(popup, 0.2f);
                yield return null;
                var screen = new Rect(0f, 0f, Screen.width, Screen.height);
                var window = ScreenRect((RectTransform)popup.transform);
                var list = ScreenRect((RectTransform)popup.transform.Find("ContentClip/ContentInner/Body/ListPanel"));
                var cctv = ScreenRect((RectTransform)popup.transform.Find("ContentClip/ContentInner/Body/CctvArea"));
                var label = size.x + "x" + size.y;
                Assert.That(screen.Contains(window.min) && screen.Contains(window.max), Is.True, label + " 창이 화면 안에 있어야 한다: " + window);
                Assert.That(list.Overlaps(cctv), Is.False, label + " 목록과 CCTV가 겹치지 않아야 한다.");
                Assert.That(window.Contains(list.min) && window.Contains(list.max) && window.Contains(cctv.min) && window.Contains(cctv.max), Is.True, label);
                Assert.That(list.width / (list.width + cctv.width), Is.InRange(0.30f, 0.36f), label);
                AssertRenderTextureMatchesScreen(popup);
                AssertCameraShowsSelected(popup);

                // 글자 가독성: 목록 이름과 제목이 화면 픽셀 기준으로 충분히 크다.
                var scale = popup.transform.lossyScale.y;
                var nameFont = popup.ItemAt(0).NameText.fontSize * scale;
                Assert.That(nameFont, Is.GreaterThanOrEqualTo(12f), label + " 목록 글자 크기(px)");
                yield return Capture("resolution-" + label);
            }
        }

        // ---- 환경 ----

        private IEnumerator Load()
        {
            env = new UiTestEnvironment();
            yield return env.LoadIntegration();
            scene = new CoreCctvScene();
            yield return scene.Prepare();
        }

        /// <summary>실제 마우스 입력으로 버튼 중앙을 누른다. 피벗이 가장자리인 목록 항목도 정확히 눌린다.</summary>
        private IEnumerator ClickCenter(Button button)
        {
            yield return UiTestWait.FocusGameView();
            Canvas.ForceUpdateCanvases();
            var position = ScreenRect((RectTransform)button.transform).center;
            var pointer = new PointerEventData(EventSystem.current) { position = position };
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, hits);
            Assert.That(hits, Is.Not.Empty, button.name + " 아래에 레이캐스트 대상이 없다.");
            Assert.That(hits[0].gameObject.GetComponentInParent<Button>(), Is.SameAs(button), button.name + " 위를 다른 UI가 가리고 있다: " + hits[0].gameObject.name);
            env.Mouse.MakeCurrent();
            InputSystem.QueueStateEvent(env.Mouse, new MouseState { position = position });
            yield return null;
            InputSystem.QueueStateEvent(env.Mouse, new MouseState { position = position, buttons = 1 });
            yield return null;
            yield return null;
            InputSystem.QueueStateEvent(env.Mouse, new MouseState { position = position });
            yield return null;
            yield return null;
        }

        private static IEnumerator Advance(CoreCctvPopupView popup, float seconds)
        {
            for (var t = 0f; t < seconds; t += Frame)
            {
                popup.Tick(Mathf.Min(Frame, seconds - t));
                yield return null;
            }
        }

        private static IEnumerator Capture(string name)
        {
            yield return new WaitForEndOfFrame();
            var texture = ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(Path.Combine(EvidenceDirectory, name + ".png"), texture.EncodeToPNG());
            Object.Destroy(texture);
        }

        private static IEnumerator CaptureSequence(CoreCctvPopupView popup, string prefix, float seconds, float step)
        {
            var index = 0;
            for (var t = 0f; t <= seconds + 0.0001f; t += step)
            {
                yield return Capture(prefix + "-" + index.ToString("D3"));
                popup.Tick(step);
                index++;
            }
        }

        private static Rect ScreenRect(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
        }

        private static int CountCctvCameras()
        {
            var count = 0;
            foreach (var camera in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include))
            {
                if (camera.name == CoreCctvCameraRig.CameraName)
                {
                    count++;
                }
            }

            return count;
        }

        private static void AssertFullyClosed(CoreCctvPopupView popup)
        {
            Assert.That(popup.State, Is.EqualTo(CoreCctvPopupView.PlayState.Hidden));
            Assert.That(popup.gameObject.activeSelf, Is.False);
            Assert.That(popup.RecAlpha, Is.Zero);
            Assert.That(popup.VideoLevel, Is.Zero);
            Assert.That(popup.GhostsVisible, Is.False);
            Assert.That(popup.ConnectedLabelAlpha, Is.Zero);
            Assert.That(popup.Video, Is.EqualTo(CoreCctvPopupView.VideoState.Off));
            Assert.That(popup.Rig == null || !popup.Rig.IsRendering, Is.True, "닫힌 뒤 CCTV 렌더링이 남지 않아야 한다.");
        }

        private static void AssertItemVisible(CoreCctvPopupView popup, int index)
        {
            var viewport = ScreenRect((RectTransform)popup.ListViewport);
            var item = ScreenRect(popup.ItemAt(index).Rect);
            Assert.That(item.yMin, Is.GreaterThanOrEqualTo(viewport.yMin - 1f), "항목 " + index + " 아래");
            Assert.That(item.yMax, Is.LessThanOrEqualTo(viewport.yMax + 1f), "항목 " + index + " 위");
        }

        private static void AssertCameraShowsSelected(CoreCctvPopupView popup)
        {
            var locator = new BuildingInstanceFacilityLocator();
            Assert.That(locator.TryGetWorldCenter(popup.SelectedInstanceId, out var center), Is.True);
            var rig = popup.Rig;
            var tolerance = 2f / rig.PixelsPerUnit + 0.001f;
            Assert.That(Vector2.Distance(rig.Camera.transform.position, center), Is.LessThanOrEqualTo(tolerance + 1f / rig.PixelsPerUnit));
        }

        private static void AssertCameraShowsFacility(CoreCctvPopupView popup, GameObject facility)
        {
            var id = facility.GetComponent<BuildingInstance>().InstanceId;
            Assert.That(popup.SelectedInstanceId, Is.EqualTo(id), "선택된 시설");
            Assert.That(popup.Rig.IsMoving, Is.False);
            var locator = new BuildingInstanceFacilityLocator();
            Assert.That(locator.TryGetWorldCenter(id, out var center), Is.True);
            Assert.That(Vector2.Distance(center, facility.transform.position), Is.LessThan(2f), "시설 그림의 중심");
            var rig = popup.Rig;
            var distance = Vector2.Distance(rig.Camera.transform.position, center);
            Assert.That(distance, Is.LessThanOrEqualTo(2f / rig.PixelsPerUnit + 0.002f), "카메라가 시설 위치를 보고 있다.");
            // 시설이 CCTV 화면 중앙 부근에 충분히 보인다.
            var view = rig.Camera.WorldToViewportPoint(facility.transform.position);
            Assert.That(view.x, Is.InRange(0.3f, 0.7f));
            Assert.That(view.y, Is.InRange(0.25f, 0.75f));
        }

        private static void AssertRenderTextureMatchesScreen(CoreCctvPopupView popup)
        {
            var rect = ScreenRect(popup.VideoImage.rectTransform);
            var texture = popup.Rig.Texture;
            Assert.That(texture.width, Is.EqualTo(Mathf.RoundToInt(rect.width)).Within(1), "렌더 텍스처 가로가 화면 픽셀과 같아야 늘어나지 않는다.");
            Assert.That(texture.height, Is.EqualTo(Mathf.RoundToInt(rect.height)).Within(1));
            Assert.That(popup.VideoImage.texture, Is.SameAs(texture));
            var main = Camera.main;
            var mainPpu = main.pixelHeight / (2f * main.orthographicSize);
            Assert.That(popup.Rig.PixelsPerUnit, Is.EqualTo(mainPpu).Within(1f), "메인 화면과 같은 배율이라 그림이 번지거나 늘어나지 않는다.");
            Assert.That(popup.Rig.Camera.orthographicSize * 2f * popup.Rig.PixelsPerUnit, Is.EqualTo(texture.height).Within(0.5f));
            Assert.That(popup.Rig.Camera.aspect, Is.EqualTo(texture.width / (float)texture.height).Within(0.01f));
        }

        private static void AssertNoUiCanvasRendersToCamera()
        {
            // 화면 UI(HUD·팝업·코어 팝업)는 Overlay 캔버스라 어떤 카메라에도 그려지지 않는다.
            foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude))
            {
                if (!canvas.isRootCanvas)
                {
                    continue;
                }

                Assert.That(canvas.renderMode, Is.Not.EqualTo(RenderMode.ScreenSpaceCamera), canvas.name + " 은 카메라에 그려진다.");
            }
        }

        private static void AssertVideoIsNotBlank(CoreCctvPopupView popup)
        {
            var texture = popup.Rig.Texture;
            var previous = RenderTexture.active;
            RenderTexture.active = texture;
            var read = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);
            read.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
            read.Apply();
            RenderTexture.active = previous;
            var pixels = read.GetPixels32();
            Object.Destroy(read);
            var bright = 0;
            for (var i = 0; i < pixels.Length; i += 7)
            {
                if (pixels[i].r + pixels[i].g + pixels[i].b > 90)
                {
                    bright++;
                }
            }

            Assert.That(bright, Is.GreaterThan(40), "CCTV 영상에 실제 월드가 그려진다.");
        }

        private sealed class MainCameraSnapshot
        {
            private Camera camera;
            private Vector3 position;
            private float size;
            private RenderTexture target;
            private bool enabled;
            private int cullingMask;
            private float depth;
            private Rect rect;
            private CameraClearFlags clear;
            private string tag;
            private bool orthographic;

            public static MainCameraSnapshot Take()
            {
                var camera = Camera.main;
                Assert.That(camera, Is.Not.Null, "메인 카메라");
                return new MainCameraSnapshot
                {
                    camera = camera,
                    size = camera.orthographicSize,
                    target = camera.targetTexture,
                    enabled = camera.enabled,
                    cullingMask = camera.cullingMask,
                    depth = camera.depth,
                    rect = camera.rect,
                    clear = camera.clearFlags,
                    tag = camera.tag,
                    orthographic = camera.orthographic,
                    position = camera.transform.position
                };
            }

            public void AssertUnchanged()
            {
                Assert.That(Camera.main, Is.SameAs(camera), "Camera.main이 바뀌지 않는다.");
                Assert.That(camera.orthographicSize, Is.EqualTo(size));
                Assert.That(camera.targetTexture, Is.SameAs(target));
                Assert.That(camera.enabled, Is.EqualTo(enabled));
                Assert.That(camera.cullingMask, Is.EqualTo(cullingMask));
                Assert.That(camera.depth, Is.EqualTo(depth));
                Assert.That(camera.rect, Is.EqualTo(rect));
                Assert.That(camera.clearFlags, Is.EqualTo(clear));
                Assert.That(camera.tag, Is.EqualTo(tag));
                Assert.That(camera.orthographic, Is.EqualTo(orthographic));
                // 메인 카메라는 플레이어를 따라가므로 CCTV 때문에 움직이지 않는지는 위치가 거의 같은지로 본다.
                Assert.That(Vector2.Distance(camera.transform.position, position), Is.LessThan(0.5f), "메인 카메라는 CCTV와 무관하게 플레이어를 따른다.");
            }
        }

        /// <summary>실제 시설 프리팹과 실제 브리지를 쓰는 Scene 준비.</summary>
        private sealed class CoreCctvScene : System.IDisposable
        {
            private readonly List<GameObject> spawned = new List<GameObject>();
            private readonly Dictionary<GameObject, Vector3> homes = new Dictionary<GameObject, Vector3>();
            private int sequence;

            public PlayerMovement Player { get; private set; }
            public PowerNetworkSystem Network { get; private set; }
            public GameDataCatalog Catalog { get; private set; }
            public OutpostPanelBinder Binder { get; private set; }
            public OutpostPanelView View { get; private set; }
            public Vector3 Origin { get; private set; }
            public CoreCctvPopupView Popup => View.CorePopup;
            public IReadOnlyList<GameObject> Spawned => spawned;

            public OutpostService Service
            {
                get
                {
                    var binder = Object.FindAnyObjectByType<IntegrationRuntimeBinder>();
                    var field = typeof(IntegrationRuntimeBinder).GetField(
                        "outpostService",
                        BindingFlags.Instance | BindingFlags.NonPublic);
                    return field != null ? field.GetValue(binder) as OutpostService : null;
                }
            }

            public IEnumerator Prepare()
            {
                Player = Object.FindAnyObjectByType<PlayerMovement>();
                Network = Object.FindAnyObjectByType<PowerNetworkSystem>();
                Catalog = GameBootstrapper.Instance.AssignedCatalog as GameDataCatalog;
                Binder = Object.FindAnyObjectByType<OutpostPanelBinder>(FindObjectsInactive.Include);
                View = Binder.GetComponent<OutpostPanelView>();
                Assert.That(Player, Is.Not.Null, "PlayerMovement");
                Assert.That(Network, Is.Not.Null, "PowerNetworkSystem");
                Assert.That(Catalog, Is.Not.Null, "GameDataCatalog");
                Assert.That(View.CorePopup, Is.Not.Null, "CorePopup");
                Origin = Player.transform.position;
                yield return null;
            }

            public GameObject SpawnCore()
            {
                return Spawn(DataIds.Buildings.OutpostCoreBasic, Vector3.zero);
            }

            public GameObject SpawnFacility(string buildingId, Vector3 offset)
            {
                return Spawn(buildingId, offset);
            }

            /// <summary>플레이어 시작 위치에서 offset만큼 떨어진 곳에 실제 시설 프리팹을 설치한다.</summary>
            private GameObject Spawn(string buildingId, Vector3 offset)
            {
                Assert.That(Catalog.TryGetBuilding(buildingId, out var data), Is.True, buildingId);
                var position = Origin + offset;
                var instance = Object.Instantiate(data.RuntimePrefab, position, Quaternion.identity);
                var id = buildingId + "-t" + (++sequence).ToString("D4");
                var building = instance.GetComponent<BuildingInstance>() ?? instance.AddComponent<BuildingInstance>();
                building.Initialize(id, buildingId);
                var node = instance.GetComponent<PowerNode>();
                if (node != null)
                {
                    node.SetEntityId(id);
                    node.SetNetwork(Network);
                }

                spawned.Add(instance);
                homes[instance] = position;
                return instance;
            }

            /// <summary>전력 범위 밖으로 옮겨 연결을 끊는다(실제 판정은 브리지가 한다).</summary>
            public void Disconnect(GameObject facility)
            {
                facility.transform.position = homes[facility] + new Vector3(0f, 60f, 0f);
                Network.RequestRebuild();
            }

            public void Reconnect(GameObject facility)
            {
                facility.transform.position = homes[facility];
                Network.RequestRebuild();
            }

            public void MovePlayer(Vector3 offsetFromOrigin)
            {
                var body = Player.GetComponent<Rigidbody2D>();
                var target = Origin + offsetFromOrigin;
                Player.transform.position = target;
                if (body != null)
                {
                    body.position = target;
                    body.linearVelocity = Vector2.zero;
                }
            }

            public IEnumerator WaitForStatus()
            {
                Network.RequestRebuild();
                for (var i = 0; i < 4; i++)
                {
                    yield return null;
                }
            }

            public void Open()
            {
                Binder.Presenter.ToggleInteractionPanel();
            }

            public void Dispose()
            {
                for (var i = 0; i < spawned.Count; i++)
                {
                    if (spawned[i] != null)
                    {
                        Object.Destroy(spawned[i]);
                    }
                }

                spawned.Clear();
            }
        }
    }
}

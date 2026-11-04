using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using SubTerra.App.Audio;
using SubTerra.App.Core.Data;
using SubTerra.App.Drone.Dialogue;
using SubTerra.App.Integration;
using SubTerra.App.UI;
using SubTerra.App.UI.Drone;
using SubTerra.App.UI.EmergencyRescue;
using SubTerra.App.UI.Outpost;
using SubTerra.Gameplay.Building;
using SubTerra.Shared;
using SubTerra.Shared.Audio;
using TMPro;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SubTerra.App.Tests.Audio
{
    /// <summary>
    /// B-181: 재사용 대기 창·구조 순간이동·시설 집기/취소·드론 말풍선을 기존 소리로 채운다.
    /// SfxRequests(일반·배율) 기록과 WindowTvSfx 기록으로 확인한다.
    /// </summary>
    public sealed class PromptB181SfxIngameFillTests
    {
        private const string PauseOwner = "b181-test";

        private readonly struct Request
        {
            public Request(SfxId id, float pitch, float scale)
            {
                Id = id;
                Pitch = pitch;
                Scale = scale;
            }

            public SfxId Id { get; }
            public float Pitch { get; }
            public float Scale { get; }
        }

        private readonly List<Request> played = new List<Request>();
        private readonly List<Object> created = new List<Object>();
        private SfxPlayer player;
        private float now;

        [SetUp]
        public void SetUp()
        {
            played.Clear();
            now = 100f;
            SfxRequests.PlayRequested += Record;
            SfxRequests.ScaledPlayRequested += RecordScaled;
            WindowTvSfx.ResetForTests(Resources.Load<SfxLibrary>(SfxLibrary.ResourcePath), () => now, () => false);
        }

        [TearDown]
        public void TearDown()
        {
            SfxRequests.PlayRequested -= Record;
            SfxRequests.ScaledPlayRequested -= RecordScaled;
            WindowTvSfx.ReleaseForTests();
            UiPauseGate.Release(PauseOwner);
            if (player != null)
            {
                player.ReleaseForTests();
                player = null;
            }

            for (var i = created.Count - 1; i >= 0; i--)
            {
                if (created[i] != null) Object.DestroyImmediate(created[i]);
            }

            created.Clear();
        }

        private void Record(SfxId id, float pitch) => played.Add(new Request(id, pitch, 1f));

        private void RecordScaled(SfxId id, float pitch, float scale) => played.Add(new Request(id, pitch, scale));

        private int Count(SfxId id)
        {
            var count = 0;
            foreach (var entry in played)
            {
                if (entry.Id == id) count++;
            }

            return count;
        }

        private Request Last(SfxId id)
        {
            for (var i = played.Count - 1; i >= 0; i--)
            {
                if (played[i].Id == id) return played[i];
            }

            Assert.Fail(id + " 요청이 없다");
            return default;
        }

        private static void InvokePrivate(object target, string name)
        {
            var method = target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.That(method, Is.Not.Null, name);
            method.Invoke(target, null);
        }

        private static string ReadScript(params string[] parts)
        {
            var path = Path.Combine(Application.dataPath, "_Project", "Scripts");
            foreach (var part in parts)
            {
                path = Path.Combine(path, part);
            }

            return File.ReadAllText(path);
        }

        // ---- ★ 상수 ----

        [Test]
        public void StarConstants_UseExistingSlotsAndDefaultValues()
        {
            Assert.That(FacilityCooldownPopupView.CooldownOpenSfx, Is.EqualTo(SfxId.WindowTvOpen));
            Assert.That((int)FacilityCooldownPopupView.CooldownOpenSfx, Is.EqualTo(79));
            Assert.That((int)FacilityCooldownPopupView.CooldownCloseSfx, Is.EqualTo(80));
            Assert.That((int)FacilityCooldownPopupView.CooldownDoneSfx, Is.EqualTo(78));
            Assert.That(FacilityCooldownPopupView.CooldownTvVolume, Is.EqualTo(0.6f));
            Assert.That(FacilityCooldownPopupView.CooldownOpenDelay, Is.EqualTo(0f));

            Assert.That((int)EmergencyRescueRuntimeController.RescueWarpSfx, Is.EqualTo(30));
            Assert.That((int)EmergencyRescueRuntimeController.RescueFailSfx, Is.EqualTo(5));

            Assert.That((int)GameplayBuildingPlacementBridge.MovePickupSfx, Is.EqualTo(11));
            Assert.That((int)GameplayBuildingPlacementBridge.MoveCancelSfx, Is.EqualTo(12));
            Assert.That(GameplayBuildingPlacementBridge.MoveHoloVolume, Is.EqualTo(0.6f));

            Assert.That((int)DroneDialogueSocket.DroneBubbleSfx, Is.EqualTo(13));
            Assert.That(DroneDialogueSocket.DroneUrgentBubbleSfx, Is.EqualTo(SfxId.DroneBubbleUrgent), "B-183: 급한 대사 전용 음원");
            Assert.That(DroneDialogueSocket.DroneBubblePitch, Is.EqualTo(1.35f));
            Assert.That(DroneDialogueSocket.DroneBubblePingGap, Is.EqualTo(0.3f));
            Assert.That(DroneDialogueSocket.DroneBubbleMinGap, Is.EqualTo(1.5f));
            Assert.That(DroneDialogueSocket.DroneBubbleStartupQuiet, Is.EqualTo(1.0f));
        }

        // ---- 음량 배율 ----

        [Test]
        public void ClampVolumeScale_CutsToZeroOne()
        {
            Assert.That(SfxRequests.ClampVolumeScale(0.6f), Is.EqualTo(0.6f));
            Assert.That(SfxRequests.ClampVolumeScale(2f), Is.EqualTo(1f));
            Assert.That(SfxRequests.ClampVolumeScale(-1f), Is.EqualTo(0f));
            Assert.That(SfxRequests.ClampVolumeScale(float.NaN), Is.EqualTo(0f));
        }

        [Test]
        public void SfxPlayer_ScaledPlay_MultipliesFinalVolume_ExistingCallsStayOne()
        {
            player = SfxPlayer.CreateForTests(Resources.Load<SfxLibrary>(SfxLibrary.ResourcePath), () => now, () => false);

            player.PlayNow(SfxId.WorldHoloOpen);
            Assert.That(player.LastVolumeScale, Is.EqualTo(1f), "기존 호출은 1배");
            var full = PlayingVolume(SfxId.WorldHoloOpen);
            Assert.That(full, Is.GreaterThan(0f));

            player.StopAll();
            now += 1f;
            SfxPlayer.Play(SfxId.WorldHoloOpen, 1f, 0.6f);
            Assert.That(player.LastVolumeScale, Is.EqualTo(0.6f));
            Assert.That(PlayingVolume(SfxId.WorldHoloOpen), Is.EqualTo(full * 0.6f).Within(0.0001f));

            player.StopAll();
            now += 1f;
            player.PlayNow(SfxId.WorldHoloOpen, 1f, 3f);
            Assert.That(player.LastVolumeScale, Is.EqualTo(1f), "1 위는 1로 자른다");
            Assert.That(PlayingVolume(SfxId.WorldHoloOpen), Is.EqualTo(full).Within(0.0001f));

            player.StopAll();
            now += 1f;
            player.PlayNow(SfxId.WorldHoloOpen, 1f, -2f);
            Assert.That(player.LastVolumeScale, Is.EqualTo(0f), "0 아래는 0으로 자른다");
        }

        private float PlayingVolume(SfxId id)
        {
            var library = Resources.Load<SfxLibrary>(SfxLibrary.ResourcePath);
            Assert.That(library.TryGetEntry(id, out var entry), Is.True);
            foreach (var source in player.GetComponents<AudioSource>())
            {
                if (source.clip == entry.Clip) return source.volume;
            }

            Assert.Fail(id + " 재생 소스가 없다");
            return 0f;
        }

        [Test]
        public void WindowTvSfx_ScaledPlay_MultipliesSourceVolume()
        {
            Assert.That(WindowTvSfx.Play(SfxId.WindowTvOpen), Is.True);
            Assert.That(WindowTvSfx.LastVolumeScale, Is.EqualTo(1f));
            var full = WindowTvSfx.Source.volume;
            Assert.That(full, Is.GreaterThan(0f));

            Assert.That(WindowTvSfx.Play(SfxId.WindowTvOpen, 0.6f), Is.True);
            Assert.That(WindowTvSfx.LastVolumeScale, Is.EqualTo(0.6f));
            Assert.That(WindowTvSfx.Source.volume, Is.EqualTo(full * 0.6f).Within(0.0001f));

            Assert.That(WindowTvSfx.Play(SfxId.WindowTvOpen, 5f), Is.True);
            Assert.That(WindowTvSfx.Source.volume, Is.EqualTo(full).Within(0.0001f));
        }

        // ---- A. 재사용 대기 창 ----

        private FacilityCooldownPopupView NewCooldownView()
        {
            var canvas = new GameObject("B181CooldownCanvas", typeof(RectTransform), typeof(Canvas));
            created.Add(canvas);
            var view = FacilityCooldownPopupView.Create(canvas.transform, null);
            Assert.That(view, Is.Not.Null);
            return view;
        }

        [Test]
        public void Cooldown_NewOpenPlaysSmallTvOpenOnce_SameTargetAgainSilent()
        {
            var view = NewCooldownView();
            var remaining = 20d;

            Assert.That(view.ShowFacilityCooldown(DataIds.Buildings.ChargerBasic, "charger.a", id => remaining), Is.True);
            Assert.That(WindowTvSfx.StartedCount, Is.EqualTo(1));
            Assert.That(WindowTvSfx.LastId, Is.EqualTo(SfxId.WindowTvOpen));
            Assert.That(WindowTvSfx.LastVolumeScale, Is.EqualTo(0.6f));

            view.Tick(0.05f);
            Assert.That(view.ShowFacilityCooldown(DataIds.Buildings.ChargerBasic, "charger.a", id => remaining), Is.True);
            Assert.That(WindowTvSfx.StartedCount, Is.EqualTo(1), "같은 대상 다시 E는 무음");

            Assert.That(view.ShowFacilityCooldown(DataIds.Buildings.ClinicBasic, "clinic.a", id => remaining), Is.True);
            Assert.That(WindowTvSfx.StartedCount, Is.EqualTo(2), "다른 대상은 새로 켬");
            Assert.That(WindowTvSfx.LastId, Is.EqualTo(SfxId.WindowTvOpen));
        }

        [Test]
        public void Cooldown_CompletionPlaysGlowThenCloseOnce()
        {
            var view = NewCooldownView();
            var remaining = 1.0d;
            view.ShowFacilityCooldown(DataIds.Buildings.ChargerBasic, "charger.a", id => remaining);
            view.Tick(0.05f);
            played.Clear();

            remaining = -1d;
            view.Tick(0.05f);
            Assert.That(view.IsCompleting, Is.True);
            Assert.That(Count(SfxId.UnlockGlow), Is.EqualTo(1), "완료 빛과 함께 반짝 한 번");
            Assert.That(WindowTvSfx.StartedCount, Is.EqualTo(1), "빛이 도는 동안 꺼짐음은 아직");

            for (var i = 0; i < 40; i++)
            {
                view.Tick(0.05f);
            }

            Assert.That(view.IsOpen, Is.False);
            Assert.That(Count(SfxId.UnlockGlow), Is.EqualTo(1));
            Assert.That(WindowTvSfx.StartedCount, Is.EqualTo(2), "78 뒤에 80 한 번");
            Assert.That(WindowTvSfx.LastId, Is.EqualTo(SfxId.WindowTvClose));
            Assert.That(WindowTvSfx.LastVolumeScale, Is.EqualTo(0.6f));
        }

        [Test]
        public void Cooldown_WalkAwayHidePlaysCloseOnce()
        {
            var view = NewCooldownView();
            view.ShowFacilityCooldown(DataIds.Buildings.ChargerBasic, "charger.a", id => 20d);

            view.HideFacilityCooldown();
            view.HideFacilityCooldown();
            for (var i = 0; i < 20; i++)
            {
                view.Tick(0.05f);
            }

            Assert.That(WindowTvSfx.StartedCount, Is.EqualTo(2));
            Assert.That(WindowTvSfx.LastId, Is.EqualTo(SfxId.WindowTvClose));
        }

        [Test]
        public void Cooldown_ExpiredWhileOpenWithoutShortObservation_PlaysCloseOnce()
        {
            var view = NewCooldownView();
            var remaining = 20d;
            view.ShowFacilityCooldown(DataIds.Buildings.ChargerBasic, "charger.a", id => remaining);

            remaining = -1d;
            view.Tick(0.05f);

            Assert.That(view.IsOpen, Is.False);
            Assert.That(Count(SfxId.UnlockGlow), Is.Zero);
            Assert.That(WindowTvSfx.StartedCount, Is.EqualTo(2));
            Assert.That(WindowTvSfx.LastId, Is.EqualTo(SfxId.WindowTvClose));
        }

        [Test]
        public void Cooldown_OnDisableClearStateAndQuietHide_AreSilent()
        {
            var view = NewCooldownView();
            view.ShowFacilityCooldown(DataIds.Buildings.ChargerBasic, "charger.a", id => 20d);
            Assert.That(WindowTvSfx.StartedCount, Is.EqualTo(1));

            InvokePrivate(view, "OnDisable");
            view.HideFacilityCooldown();
            Assert.That(WindowTvSfx.StartedCount, Is.EqualTo(1), "OnDisable·ClearState 뒤에는 꺼짐음 없음");

            view.ShowFacilityCooldown(DataIds.Buildings.ChargerBasic, "charger.a", id => 20d);
            Assert.That(WindowTvSfx.StartedCount, Is.EqualTo(2));
            view.HideFacilityCooldownQuietly();
            view.HideFacilityCooldown();
            for (var i = 0; i < 20; i++)
            {
                view.Tick(0.05f);
            }

            Assert.That(WindowTvSfx.StartedCount, Is.EqualTo(2), "조용히 치우는 경로는 무음");
            Assert.That(played, Is.Empty);
        }

        [Test]
        public void Cooldown_PresenterUnbindUsesQuietHide()
        {
            var presenter = ReadScript("App", "UI", "Outpost", "OutpostPanelPresenter.cs");
            Assert.That(presenter, Does.Contain("HideFacilityCooldown(quiet: true)"));
            Assert.That(presenter, Does.Contain("IFacilityCooldownQuietHide"));
        }

        // ---- B. 구조 순간이동 ----

        [Test]
        public void RescueController_WarpOnceBeforeMove_NoArrivalDing_FailSoundOnEachFailure()
        {
            var source = ReadScript("App", "Integration", "EmergencyRescueRuntimeController.cs");
            var tryRescue = source.Substring(source.IndexOf("private void TryRescue()", StringComparison.Ordinal));
            tryRescue = tryRescue.Substring(0, tryRescue.IndexOf("private static void PlayRescueFailSfx", StringComparison.Ordinal));

            const string warp = "SfxPlayer.Play(RescueWarpSfx);";
            Assert.That(CountOf(tryRescue, warp), Is.EqualTo(1), "성공 시 워프 한 번");
            Assert.That(tryRescue.IndexOf(warp, StringComparison.Ordinal),
                Is.LessThan(tryRescue.IndexOf("MoveToResolvedElevator();", StringComparison.Ordinal)));
            Assert.That(CountOf(source, "SfxPlayer.Play("), Is.EqualTo(1), "워프 외 추가 재생(지연 도착음 등) 없음");
            Assert.That(source, Does.Not.Contain("ElevatorArriveDing"), "엘리베이터를 탄 것이 아니므로 구조 도착 딩 없음");
            Assert.That(source, Does.Not.Contain("RescueArrive"));
            Assert.That(File.Exists(Path.Combine(Application.dataPath, "_Project", "Scripts", "App", "Integration", "EmergencyRescueArrivalSfx.cs")),
                Is.False, "지연 도착음 클래스는 제거");
            Assert.That(CountOf(tryRescue, "PlayRescueFailSfx();"), Is.EqualTo(3), "엘리베이터 못 찾음·비용 바뀜·결제 실패");
            Assert.That(source, Does.Contain("FailureFeedback.Play(RescueFailSfx, RescueFailureKey)"));

            var reset = source.Substring(source.IndexOf("private void ResetEpisode()", StringComparison.Ordinal));
            reset = reset.Substring(0, reset.IndexOf("private static Sprite ResolveIcon", StringComparison.Ordinal));
            Assert.That(reset, Does.Not.Contain("Sfx"), "전력 회복·HideAll은 무음");
        }

        private static int CountOf(string text, string token)
        {
            var count = 0;
            var index = 0;
            while ((index = text.IndexOf(token, index, StringComparison.Ordinal)) >= 0)
            {
                count++;
                index += token.Length;
            }

            return count;
        }

        [Test]
        public void RescueButton_ClickHasNoConfirm_HoverKept_HideAllSilent()
        {
            var canvas = new GameObject("B181RescueCanvas", typeof(RectTransform), typeof(Canvas));
            created.Add(canvas);
            var view = EmergencyRescuePanelView.Create(canvas.transform, null);
            var rescued = 0;
            view.Bind(() => rescued++, () => { }, null);

            var sfx = view.Popup.RescueButton.GetComponent<UiSfxButton>();
            Assert.That(sfx, Is.Not.Null);
            Assert.That(sfx.ClickSfx, Is.EqualTo(SfxId.None));
            Assert.That(sfx.Hover, Is.True);

            view.Popup.RescueButton.onClick.Invoke();
            Assert.That(rescued, Is.EqualTo(1));
            view.HideImmediate();
            view.SetChipVisible(false);
            Assert.That(played, Is.Empty);
        }

        // ---- C. 시설 집기·이동 취소 ----

        private sealed class MoveFixture
        {
            public GameObject Host;
            public GameObject Prefab;
            public BuildingPlacementDefinition Definition;
            public BuildingPlacementSystem System;
            public GameplayBuildingPlacementBridge Bridge;
        }

        private MoveFixture NewMoveFixture()
        {
            var fixture = new MoveFixture
            {
                Host = new GameObject("B181Move"),
                Prefab = new GameObject("B181StorageTemplate"),
                Definition = ScriptableObject.CreateInstance<BuildingPlacementDefinition>(),
            };
            created.Add(fixture.Host);
            created.Add(fixture.Prefab);
            created.Add(fixture.Definition);
            fixture.Host.SetActive(false);
            fixture.Definition.EditorSet(DataIds.Buildings.StorageBasic, fixture.Prefab, Vector2Int.one, false);
            fixture.System = fixture.Host.AddComponent<BuildingPlacementSystem>();
            var systemSettings = new SerializedObject(fixture.System);
            var definitions = systemSettings.FindProperty("restoreDefinitions");
            definitions.arraySize = 1;
            definitions.GetArrayElementAtIndex(0).objectReferenceValue = fixture.Definition;
            systemSettings.ApplyModifiedPropertiesWithoutUndo();
            fixture.Bridge = fixture.Host.AddComponent<GameplayBuildingPlacementBridge>();
            var bridgeSettings = new SerializedObject(fixture.Bridge);
            bridgeSettings.FindProperty("placementSystem").objectReferenceValue = fixture.System;
            bridgeSettings.ApplyModifiedPropertiesWithoutUndo();
            fixture.Host.SetActive(true);
            Assert.That(fixture.System.TryRestoreBuilding(new BuildingSnapshotDto
            {
                instanceId = "storage.b181", buildingTypeId = DataIds.Buildings.StorageBasic,
                footprintWidth = 1, footprintHeight = 1, level = 1, health = 1f
            }), Is.True);
            return fixture;
        }

        [Test]
        public void Move_PickupPlaysSmallHoloOpen_CarryingRightClickCancelPlaysSmallHoloClose()
        {
            var fixture = NewMoveFixture();

            Assert.That(fixture.Bridge.HandleWorldRightClick(Vector3.zero), Is.True);
            Assert.That(fixture.System.IsMoving, Is.True);
            Assert.That(Count(SfxId.WorldHoloOpen), Is.EqualTo(1));
            Assert.That(Last(SfxId.WorldHoloOpen).Scale, Is.EqualTo(0.6f));
            Assert.That(Count(SfxId.WorldHoloClose), Is.Zero);

            Assert.That(fixture.Bridge.HandleWorldRightClick(Vector3.zero), Is.True);
            Assert.That(fixture.System.IsMoving, Is.False);
            Assert.That(Count(SfxId.WorldHoloClose), Is.EqualTo(1));
            Assert.That(Last(SfxId.WorldHoloClose).Scale, Is.EqualTo(0.6f));
            Assert.That(Count(SfxId.WorldHoloOpen), Is.EqualTo(1));
        }

        [Test]
        public void Move_SelectionModeOnlyCancel_IsSilent_ThenClickPickupPlaysOpen()
        {
            var fixture = NewMoveFixture();

            fixture.Bridge.BeginMoveSelection();
            Assert.That(fixture.Bridge.IsMoveMode, Is.True);
            Assert.That(fixture.Bridge.HandleWorldRightClick(Vector3.zero), Is.True);
            Assert.That(fixture.Bridge.IsMoveMode, Is.False);
            Assert.That(played, Is.Empty, "아직 집지 않았으면 무음");

            fixture.Bridge.BeginMoveSelection();
            Assert.That(fixture.Bridge.TryBeginMoveFromWorld(Vector3.zero), Is.True, "이동 버튼 뒤 클릭으로 집기");
            Assert.That(Count(SfxId.WorldHoloOpen), Is.EqualTo(1));
        }

        [Test]
        public void Move_QuietCancelPaths_AreSilent_PlayerCancelMovePlaysClose()
        {
            var fixture = NewMoveFixture();

            fixture.Bridge.HandleWorldRightClick(Vector3.zero);
            played.Clear();
            fixture.Bridge.CancelPreview();
            Assert.That(fixture.System.IsMoving, Is.False);
            Assert.That(played, Is.Empty, "기존 계약 경로(바인딩 해제·선택 교체)는 무음");

            fixture.Bridge.HandleWorldRightClick(Vector3.zero);
            played.Clear();
            fixture.Bridge.BeginMoveSelection();
            Assert.That(played, Is.Empty, "BeginMoveSelection 안의 정리는 무음");

            fixture.Bridge.CancelPreview();
            fixture.Bridge.HandleWorldRightClick(Vector3.zero);
            played.Clear();
            ((SubTerra.App.UI.Building.IBuildingFacilityMovePort)fixture.Bridge).CancelMove(true);
            Assert.That(Count(SfxId.WorldHoloClose), Is.EqualTo(1), "건설창 닫기 경로");
        }

        [Test]
        public void Move_DropSuccessKeepsPlaceSound_NoHoloClose()
        {
            var fixture = NewMoveFixture();
            InvokePrivate(fixture.Bridge, "OnEnable");
            try
            {
                fixture.Bridge.HandleWorldRightClick(Vector3.zero);
                played.Clear();

                Assert.That(fixture.System.TryMoveAt(new Vector3Int(2, 0, 0)).IsSuccess, Is.True);
                Assert.That(Count(BuildingPlacementSfx.ResolvePlaced(DataIds.Buildings.StorageBasic)), Is.EqualTo(1), "설치음 그대로");
                Assert.That(Count(SfxId.WorldHoloClose), Is.Zero);
            }
            finally
            {
                InvokePrivate(fixture.Bridge, "OnDisable");
            }
        }

        [Test]
        public void Move_PlayerCancelEntryPointsUseSoundPath()
        {
            var presenter = ReadScript("App", "UI", "Building", "BuildingMenuPresenter.cs");
            Assert.That(presenter, Does.Contain("movePort.CancelMove(true);"));
            var menu = ReadScript("App", "Integration", "UndergroundMenuController.cs");
            Assert.That(menu, Does.Contain("placement.CancelPreview(true);"));
            var binder = ReadScript("App", "UI", "Building", "BuildingMenuBinder.cs");
            Assert.That(binder, Does.Contain("activePort?.CancelPreview();"), "OnDisable(씬 전환·탑승)은 무음 경로 그대로");
        }

        // ---- D. 드론 말풍선 ----

        private sealed class BubbleClock
        {
            public float Now;
            public float SincePing = float.PositiveInfinity;
            public bool Paused;
        }

        private DroneDialogueSocket NewSocket(BubbleClock clock)
        {
            var go = new GameObject("B181DroneSocket", typeof(RectTransform), typeof(CanvasGroup));
            created.Add(go);
            var textObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(go.transform, false);
            var socket = go.AddComponent<DroneDialogueSocket>();
            var settings = new SerializedObject(socket);
            settings.FindProperty("dialogueText").objectReferenceValue = textObject.GetComponent<TextMeshProUGUI>();
            settings.FindProperty("canvasGroup").objectReferenceValue = go.GetComponent<CanvasGroup>();
            settings.ApplyModifiedPropertiesWithoutUndo();
            socket.ConfigureSfxForTests(() => clock.Now, id => id == SfxId.DroneScanPing ? clock.SincePing : float.PositiveInfinity,
                () => clock.Paused);
            return socket;
        }

        private static DroneDialogueResult Line(string text, bool urgent = false)
        {
            return new DroneDialogueResult("b181." + text, text, false, false, urgent);
        }

        /// <summary>바인딩하고 첫 대사(초기화)를 지나 시작 무음 시간 뒤로 옮긴다.</summary>
        private DroneDialogueSocket BoundSocket(BubbleClock clock)
        {
            var socket = NewSocket(clock);
            socket.SetVisible(true);
            socket.SetDialogue(Line("first"));
            clock.Now += DroneDialogueSocket.DroneBubbleStartupQuiet + 0.1f;
            return socket;
        }

        [Test]
        public void Bubble_NewLinePlaysHighBeepOnce()
        {
            var clock = new BubbleClock { Now = 10f };
            var socket = BoundSocket(clock);
            Assert.That(played, Is.Empty, "첫 대사는 초기화라 무음");

            socket.SetDialogue(Line("new"));

            Assert.That(Count(SfxId.WorldFacilityTagBeep), Is.EqualTo(1));
            Assert.That(Last(SfxId.WorldFacilityTagBeep).Pitch, Is.EqualTo(1.35f).Within(0.0001f));
            Assert.That(Last(SfxId.WorldFacilityTagBeep).Scale, Is.EqualTo(1f), "월드 버스 그대로, 배율 없음");
        }

        [Test]
        public void Bubble_SkippedRightAfterScanPing()
        {
            var clock = new BubbleClock { Now = 10f };
            var socket = BoundSocket(clock);
            clock.SincePing = 0.2f;

            socket.SetDialogue(Line("ping"));
            Assert.That(played, Is.Empty, "핑 0.2초 뒤는 생략");

            clock.SincePing = 0.5f;
            clock.Now += 0.1f;
            socket.SetDialogue(Line("after-ping"));
            Assert.That(Count(SfxId.WorldFacilityTagBeep), Is.EqualTo(1), "생략은 그 회차만");
        }

        [Test]
        public void Bubble_SecondLineWithinMinGap_Skipped_AfterGapPlays()
        {
            var clock = new BubbleClock { Now = 10f };
            var socket = BoundSocket(clock);
            socket.SetDialogue(Line("one"));
            clock.Now += 1.0f;
            socket.SetDialogue(Line("two"));
            Assert.That(Count(SfxId.WorldFacilityTagBeep), Is.EqualTo(1), "1.0초 안 두 번째는 생략");

            clock.Now += 0.6f;
            socket.SetDialogue(Line("three"));
            Assert.That(Count(SfxId.WorldFacilityTagBeep), Is.EqualTo(2));
        }

        [Test]
        public void Bubble_SameTextAgain_Silent()
        {
            var clock = new BubbleClock { Now = 10f };
            var socket = BoundSocket(clock);
            socket.SetDialogue(Line("same"));
            clock.Now += 2f;
            socket.SetDialogue(Line("same"));
            Assert.That(Count(SfxId.WorldFacilityTagBeep), Is.EqualTo(1));
        }

        [Test]
        public void Bubble_FirstLineStartupQuietAndPaused_Silent()
        {
            var clock = new BubbleClock { Now = 10f };
            var socket = NewSocket(clock);
            socket.SetVisible(true);
            socket.SetDialogue(Line("first"));
            clock.Now += 0.5f;
            socket.SetDialogue(Line("startup"));
            Assert.That(played, Is.Empty, "첫 대사·시작 무음 시간");

            clock.Now += 1f;
            clock.Paused = true;
            socket.SetDialogue(Line("paused"));
            Assert.That(played, Is.Empty, "정지·설정창 열림");

            clock.Paused = false;
            clock.Now += 0.1f;
            socket.SetDialogue(Line("resumed"));
            Assert.That(Count(SfxId.WorldFacilityTagBeep), Is.EqualTo(1));
        }

        [Test]
        public void Bubble_RebindStartsQuietAgain()
        {
            var clock = new BubbleClock { Now = 10f };
            var socket = BoundSocket(clock);
            socket.SetVisible(false);
            socket.SetVisible(true);
            socket.SetDialogue(Line("after-load"));
            clock.Now += 0.5f;
            socket.SetDialogue(Line("after-load-2"));
            Assert.That(played, Is.Empty, "불러오기·이어하기 직후");
        }

        [Test]
        public void Bubble_UrgentLinePlaysDedicatedBeepOnce()
        {
            var clock = new BubbleClock { Now = 10f };
            var socket = BoundSocket(clock);
            socket.SetDialogue(Line("urgent", true));
            Assert.That(Count(SfxId.DroneBubbleUrgent), Is.EqualTo(1));
            Assert.That(Last(SfxId.DroneBubbleUrgent).Pitch, Is.EqualTo(1f).Within(0.0001f));
        }

        [Test]
        public void Bubble_UiPauseGateHeld_DefaultGateSilent()
        {
            var clock = new BubbleClock { Now = 10f };
            var socket = BoundSocket(clock);
            socket.ConfigureSfxForTests(() => clock.Now, id => float.PositiveInfinity, null);
            UiPauseGate.Acquire(PauseOwner);
            socket.SetDialogue(Line("held"));
            Assert.That(played, Is.Empty);
        }
    }
}

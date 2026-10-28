using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using SubTerra.App.Audio;
using SubTerra.App.Editor.DataValidation;
using SubTerra.App.Outpost;
using SubTerra.App.Run;
using SubTerra.App.UI.EmergencyEscape;
using SubTerra.App.UI.HUD;
using SubTerra.App.UI.MainMenu;
using SubTerra.App.UI.Outpost;
using SubTerra.App.UI.Sell;
using SubTerra.Shared;
using SubTerra.Shared.Audio;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SubTerra.App.Tests.Audio
{
    /// <summary>B-163: 효과음 B 단계 마무리 칸과 연결 위치. SfxRequests 기록으로 확인한다.</summary>
    public sealed class PromptB163SfxBRestTests
    {
        private const string Copper = "mineral.copper";

        private static readonly (SfxId Id, int Number, string File, float Volume)[] Table =
        {
            (SfxId.StorageClose, 45, "storage_close", 0.7f),
            (SfxId.StoragePartialNotice, 46, "storage_partial_notice", 0.65f),
            (SfxId.Timer5MinAlert, 47, "timer_5min_alert", 0.6f),
            (SfxId.KeysHoloOpen, 48, "keys_holo_open", 0.45f),
            (SfxId.KeysPageTurn, 49, "keys_page_turn", 0.65f),
            (SfxId.PortalClose, 50, "portal_close", 0.55f),
        };

        private readonly List<Object> created = new List<Object>();
        private readonly List<SfxId> played = new List<SfxId>();

        [SetUp]
        public void SetUp()
        {
            played.Clear();
            SfxRequests.PlayRequested += Record;
        }

        [TearDown]
        public void TearDown()
        {
            SfxRequests.PlayRequested -= Record;
            for (var i = created.Count - 1; i >= 0; i--)
            {
                if (created[i] != null) Object.DestroyImmediate(created[i]);
            }

            created.Clear();
        }

        private void Record(SfxId id, float pitch) => played.Add(id);

        private int Count(SfxId id)
        {
            var count = 0;
            foreach (var entry in played)
            {
                if (entry == id) count++;
            }

            return count;
        }

        private GameObject NewCanvas()
        {
            var root = new GameObject("B163Canvas", typeof(RectTransform), typeof(Canvas));
            ((RectTransform)root.transform).sizeDelta = new Vector2(1920f, 1080f);
            created.Add(root);
            return root;
        }

        private static UiSfxButton SfxOf(Component root, string name)
        {
            foreach (var button in root.GetComponentsInChildren<UiSfxButton>(true))
            {
                if (button.name == name) return button;
            }

            Assert.Fail(name + " 버튼에 UiSfxButton이 없다");
            return null;
        }

        private static void AssertClick(UiSfxButton button)
        {
            Assert.That(button.ClickSfx, Is.EqualTo(SfxId.UiClick), button.name);
            Assert.That(button.FailWhenDisabled, Is.False, button.name + ": 막힌 버튼은 무음");
            Assert.That(button.Hover, Is.False, button.name);
        }

        private static void AssertFailOnly(UiSfxButton button)
        {
            Assert.That(button.ClickSfx, Is.EqualTo(SfxId.None), button.name + ": 결과음은 코드가 낸다");
            Assert.That(button.FailWhenDisabled, Is.True, button.name);
        }

        // ---- 칸 ----

        [Test]
        public void Defaults_RestSlotsNumberedFrom45_AllUiBus()
        {
            var defaults = new Dictionary<SfxId, (SfxEntry Entry, string File)>();
            foreach (var pair in SfxLibraryDefaults.Create())
            {
                Assert.That(defaults.ContainsKey(pair.Entry.Id), Is.False, pair.Entry.Id.ToString());
                defaults.Add(pair.Entry.Id, (pair.Entry, pair.FileName));
            }

            var library = AssetDatabase.LoadAssetAtPath<SfxLibrary>(PromptB156SfxLibraryBuilder.AssetPath);
            Assert.That(library, Is.Not.Null);
            var numbers = new HashSet<int>();
            foreach (var row in Table)
            {
                Assert.That((int)row.Id, Is.EqualTo(row.Number), row.Id.ToString());
                Assert.That(numbers.Add(row.Number), Is.True, row.Id.ToString());
                Assert.That(defaults.TryGetValue(row.Id, out var actual), Is.True, row.Id.ToString());
                Assert.That(actual.File, Is.EqualTo(row.File), row.Id.ToString());
                Assert.That(actual.Entry.Bus, Is.EqualTo(SfxBus.UI), row.Id.ToString());
                Assert.That(actual.Entry.Volume, Is.EqualTo(row.Volume), row.Id.ToString());

                Assert.That(library.TryGetEntry(row.Id, out var asset), Is.True, row.Id.ToString());
                Assert.That(asset.Clip, Is.Not.Null, row.Id.ToString());
                Assert.That(asset.Clip.name, Is.EqualTo(row.File), row.Id.ToString());
                Assert.That(asset.Bus, Is.EqualTo(SfxBus.UI), row.Id.ToString());
            }

            Assert.That((int)SfxId.PortalIdleLoop, Is.EqualTo(44), "기존 번호는 그대로");
            Assert.That(Enum.IsDefined(typeof(SfxId), 3), Is.True, "3번은 Obsolete로 비워 둔 채 남는다");
            Assert.That(defaults.ContainsKey((SfxId)3), Is.False);
            Assert.That(SfxLibraryDefaults.Fallbacks.Count, Is.EqualTo(1), "대체 표는 그대로");
        }

        // ---- 판매 창 ----

        private ResourceSellPopupView OpenSellPopup(SellBackend backend)
        {
            var popup = ResourceSellPopupView.Create(NewCanvas().transform, true);
            popup.ManualTick = true;
            popup.Attach(new ResourceSellSession(backend));
            popup.Show();
            popup.Tick(ResourceSellTimeline.OpenDuration + 0.01f);
            played.Clear();
            return popup;
        }

        [Test]
        public void Sell_FailedAdjustedEmptyPlayFailOnce_SoldPlaysNoFail()
        {
            var failed = OpenSellPopup(new SellBackend { Fail = true });
            failed.Session.Adjust(Copper, 1);
            Assert.That(failed.RequestSell().Status, Is.EqualTo(ResourceSellConfirmStatus.Failed));
            Assert.That(Count(SfxId.UiCancelFail), Is.EqualTo(1));

            played.Clear();
            var adjustedBackend = new SellBackend();
            var adjusted = OpenSellPopup(adjustedBackend);
            adjusted.Session.Adjust(Copper, 5);
            adjustedBackend.Owned = 2;
            Assert.That(adjusted.RequestSell().Status, Is.EqualTo(ResourceSellConfirmStatus.Adjusted));
            Assert.That(Count(SfxId.UiCancelFail), Is.EqualTo(1));
            Assert.That(Count(SfxId.SellConfirm), Is.Zero);

            played.Clear();
            var empty = OpenSellPopup(new SellBackend());
            Assert.That(empty.RequestSell().Status, Is.EqualTo(ResourceSellConfirmStatus.Empty));
            Assert.That(Count(SfxId.UiCancelFail), Is.EqualTo(1));

            played.Clear();
            var sold = OpenSellPopup(new SellBackend());
            sold.Session.Adjust(Copper, 2);
            Assert.That(sold.RequestSell().IsSold, Is.True);
            Assert.That(Count(SfxId.UiCancelFail), Is.Zero);
            Assert.That(Count(SfxId.SellConfirm), Is.EqualTo(1), "판매음은 그대로");
        }

        [Test]
        public void Sell_QuantityAndToolButtonsClick_SellButtonFailsOnlyWhenBlocked()
        {
            var popup = OpenSellPopup(new SellBackend());
            foreach (var name in new[] { "Minus", "Plus", "PlusFive", "PlusTen", "Max", "SelectAllButton", "ResetButton" })
            {
                AssertClick(SfxOf(popup, name));
            }

            var sell = SfxOf(popup, "SellButton");
            AssertFailOnly(sell);
            Assert.That(popup.SellButton.Button.interactable, Is.False, "고른 게 없으면 판매 버튼이 막힌다");
            sell.OnPointerClick(null);
            Assert.That(Count(SfxId.UiCancelFail), Is.EqualTo(1));
            Assert.That(popup.CloseButton.Button.GetComponent<UiSfxButton>(), Is.Null, "닫기는 BeginClose가 이미 소리를 낸다");
        }

        // ---- 보관함 ----

        private static OutpostOperationResult Result(OutpostOperationStatus status, bool partial = false)
        {
            return new OutpostOperationResult(status, OutpostOperationKind.Withdraw, Copper, 1, 0, "msg", partial);
        }

        [Test]
        public void Storage_QuantityResult_ConfirmOnSuccess_FailOnLimit_SilentWhenBusy()
        {
            OutpostPanelBinder.PlayQuantitySfx(Result(OutpostOperationStatus.Success));
            Assert.That(Count(SfxId.UiConfirm), Is.EqualTo(1));
            Assert.That(Count(SfxId.UiCancelFail), Is.Zero);

            OutpostPanelBinder.PlayQuantitySfx(Result(OutpostOperationStatus.CapacityExceeded));
            OutpostPanelBinder.PlayQuantitySfx(Result(OutpostOperationStatus.InsufficientQuantity));
            Assert.That(Count(SfxId.UiCancelFail), Is.EqualTo(2));

            played.Clear();
            OutpostPanelBinder.PlayQuantitySfx(Result(OutpostOperationStatus.InvalidRequest), true);
            OutpostPanelBinder.PlayTakeAllSfx(Result(OutpostOperationStatus.InvalidRequest), true);
            OutpostPanelBinder.PlayQuantitySfx(null);
            Assert.That(played, Is.Empty, "처리 중이라 막힌 요청은 무음");
        }

        [Test]
        public void Storage_PartialTakeAllPlaysNoticeOnly_FailPlaysFail()
        {
            OutpostPanelBinder.PlayTakeAllSfx(Result(OutpostOperationStatus.Success, true));
            Assert.That(Count(SfxId.StoragePartialNotice), Is.EqualTo(1));
            Assert.That(Count(SfxId.StorageTakeAll), Is.Zero, "두 소리가 겹치지 않는다");

            played.Clear();
            OutpostPanelBinder.PlayTakeAllSfx(Result(OutpostOperationStatus.Success));
            Assert.That(Count(SfxId.StorageTakeAll), Is.EqualTo(1));
            Assert.That(Count(SfxId.StoragePartialNotice), Is.Zero);

            played.Clear();
            OutpostPanelBinder.PlayTakeAllSfx(Result(OutpostOperationStatus.CapacityExceeded));
            Assert.That(Count(SfxId.UiCancelFail), Is.EqualTo(1));
            Assert.That(Count(SfxId.StorageTakeAll), Is.Zero);
        }

        private StoragePopupView OpenStorage()
        {
            var popup = StoragePopupView.Create(NewCanvas().transform);
            popup.ManualTick = true;
            popup.Show();
            popup.Tick(5f);
            played.Clear();
            return popup;
        }

        [Test]
        public void Storage_BeginCloseOncePlaysCloseOnce_InactiveCloseIsSilent()
        {
            var popup = OpenStorage();
            Assert.That(popup.BeginClose(), Is.True);
            Assert.That(popup.BeginClose(), Is.False);
            Assert.That(Count(SfxId.StorageClose), Is.EqualTo(1));

            played.Clear();
            var inactive = OpenStorage();
            inactive.enabled = false;
            Assert.That(inactive.BeginClose(), Is.True);
            Assert.That(Count(SfxId.StorageClose), Is.Zero, "연출 없이 바로 닫히면 무음");
        }

        [Test]
        public void Storage_PickerAndQuantityButtonsClick_TransferButtonsFailOnlyWhenBlocked()
        {
            var popup = OpenStorage();
            foreach (var name in new[] { "QuantityMinusButton", "Quantity1Button", "QuantityAllButton" })
            {
                AssertClick(SfxOf(popup, name));
            }

            foreach (var name in new[] { "DepositButton", "WithdrawButton", "DepositAllButton", "WithdrawAllButton" })
            {
                AssertFailOnly(SfxOf(popup, name));
            }
        }

        // ---- 5분 알림 ----

        [Test]
        public void FiveMinuteGate_FiresOnlyWhenCrossingIntoCritical()
        {
            var gate = new MineResetFiveMinuteGate();
            Assert.That(gate.Observe(301), Is.False, "첫 관측");
            Assert.That(gate.Observe(300), Is.True, "301 → 300");
            Assert.That(gate.Observe(299), Is.False, "같은 주기 한 번");

            gate = new MineResetFiveMinuteGate();
            gate.Observe(1801);
            Assert.That(gate.Observe(1800), Is.False, "30분 구간은 무음");

            gate = new MineResetFiveMinuteGate();
            Assert.That(gate.Observe(200), Is.False, "첫 관측 200(불러오기)");

            gate = new MineResetFiveMinuteGate();
            gate.Observe(300);
            Assert.That(gate.Observe(299), Is.False, "300 → 299");

            gate = new MineResetFiveMinuteGate();
            gate.Observe(400);
            Assert.That(gate.Observe(250), Is.False, "큰 건너뛰기");

            gate = new MineResetFiveMinuteGate();
            gate.Observe(305);
            Assert.That(gate.Observe(298), Is.True, "프레임이 튀어 몇 초 건너뛰어도 10초 안이면 낸다");
        }

        [Test]
        public void FiveMinuteGate_RearmsAboveFiveMinutes_ResetIsFirstObservation()
        {
            var gate = new MineResetFiveMinuteGate();
            gate.Observe(301);
            Assert.That(gate.Observe(300), Is.True);
            Assert.That(gate.Observe(10800), Is.False, "초기화로 시간이 늘면 무음");
            gate.Observe(301);
            Assert.That(gate.Observe(300), Is.True, "다시 300초를 넘었으면 다시 낸다");

            gate.Observe(301);
            gate.Reset();
            Assert.That(gate.Observe(300), Is.False, "GameState가 바뀌면 처음부터 다시 잰다");
            Assert.That(gate.Observe(60), Is.False, "막판 1분은 째깍 몫");
            Assert.That(MineResetFiveMinuteGate.ToSeconds(300.999), Is.EqualTo(300), "시계 표시와 같은 내림");
            Assert.That(MineResetClockStyle.GetBand(300), Is.EqualTo(MineResetClockBand.Critical));
            Assert.That(MineResetClockStyle.GetBand(301), Is.EqualTo(MineResetClockBand.Warning));
        }

        // ---- 키 조작 창 ----

        [Test]
        public void Keys_OpenHolo_CyclePageTurn_ClosePopupClose()
        {
            var scheme = ControlPreferences.Scheme;
            var settingsOpen = ControlPreferences.IsSettingsOpen;
            var hadPref = PlayerPrefs.HasKey(SettingsRuntimeApplier.PrefControls);
            var pref = PlayerPrefs.GetInt(SettingsRuntimeApplier.PrefControls, 0);
            try
            {
                var panel = ControlSchemePanel.Attach(NewCanvas());
                played.Clear();
                panel.Cycle(1);
                Assert.That(Count(SfxId.KeysPageTurn), Is.Zero, "창이 닫혀 있으면 무음");

                panel.Open();
                Assert.That(Count(SfxId.KeysHoloOpen), Is.Zero, "연출 없이 여는 경로는 무음");
                Assert.That(panel.Close(), Is.True);
                Assert.That(Count(SfxId.UiPopupClose), Is.Zero, "연출 없이 닫는 경로는 무음");

                panel.ForceMotionForTests = true;
                played.Clear();
                panel.Open();
                Assert.That(Count(SfxId.KeysHoloOpen), Is.EqualTo(1));
                Assert.That(played[0], Is.EqualTo(SfxId.KeysHoloOpen), "키캡보다 먼저");

                panel.Cycle(1);
                Assert.That(Count(SfxId.KeysPageTurn), Is.EqualTo(1));

                Assert.That(panel.Close(), Is.True);
                Assert.That(panel.Close(), Is.False);
                Assert.That(Count(SfxId.UiPopupClose), Is.EqualTo(1));
                Assert.That(Count(SfxId.UiConfirm), Is.Zero, "즉시 저장이라 저장음은 없다");
            }
            finally
            {
                ControlPreferences.Scheme = scheme;
                ControlPreferences.IsSettingsOpen = settingsOpen;
                if (hadPref) PlayerPrefs.SetInt(SettingsRuntimeApplier.PrefControls, pref);
                else PlayerPrefs.DeleteKey(SettingsRuntimeApplier.PrefControls);
                PlayerPrefs.Save();
            }
        }

        // ---- 포탈 ----

        private static void SetField(object target, string name, object value)
        {
            var field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, name);
            field.SetValue(target, value);
        }

        private EmergencyEscapePanelBinder NewEscape(FakePort port)
        {
            var host = new GameObject("B163Portal");
            created.Add(host);
            var view = host.AddComponent<EmergencyEscapePanelView>();
            var binder = host.AddComponent<EmergencyEscapePanelBinder>();
            SetField(view, "panelRoot", host);
            SetField(binder, "view", view);
            if (port != null)
            {
                port.Binder = binder;
                binder.BindTo(port);
            }

            return binder;
        }

        [Test]
        public void Portal_CloseWithoutTravelPlaysPortalCloseOnce()
        {
            var port = new FakePort();
            var binder = NewEscape(port);
            binder.Open(port.GetDestinationOptions(), new EmergencyEscapeCost(0, 0));
            Assert.That(binder.IsOpen, Is.True);
            played.Clear();

            binder.Close();
            binder.Close();
            Assert.That(Count(SfxId.PortalClose), Is.EqualTo(1));
        }

        [Test]
        public void Portal_SuccessfulTravelHasNoPortalClose()
        {
            var port = new FakePort { CloseDuringEscape = true };
            var binder = NewEscape(port);
            binder.Open(port.GetDestinationOptions(), new EmergencyEscapeCost(0, 0));
            played.Clear();

            binder.ConfirmSelectedDestination();
            Assert.That(port.Escapes, Is.EqualTo(1));
            Assert.That(Count(SfxId.PortalWarp), Is.EqualTo(1));
            Assert.That(Count(SfxId.PortalClose), Is.Zero, "워프 때는 워프음만");
            Assert.That(Count(SfxId.UiCancelFail), Is.Zero);
            Assert.That(binder.IsOpen, Is.False);
        }

        [Test]
        public void Portal_EachFailurePathPlaysFailOnce()
        {
            // 경로 준비 안 됨
            var noPort = NewEscape(null);
            noPort.ConfirmSelectedDestination();
            Assert.That(Count(SfxId.UiCancelFail), Is.EqualTo(1));

            // 목적지 없음
            played.Clear();
            var emptyPort = new FakePort();
            var empty = NewEscape(emptyPort);
            empty.Open(Array.Empty<EmergencyEscapeDestinationOption>(), new EmergencyEscapeCost(0, 0));
            empty.ConfirmSelectedDestination();
            Assert.That(Count(SfxId.UiCancelFail), Is.EqualTo(1));

            // 흐린 목적지(골드 부족)
            played.Clear();
            var dimPort = new FakePort();
            var dim = NewEscape(dimPort);
            dim.Open(dimPort.GetDestinationOptions(), new EmergencyEscapeCost(10, 0),
                EmergencyEscapePaymentFailure.InsufficientGold);
            dim.ConfirmSelectedDestination();
            Assert.That(dimPort.Escapes, Is.Zero, "비용 처리 경로를 타지 않는다");
            Assert.That(Count(SfxId.UiCancelFail), Is.EqualTo(1));

            // 이동 실패
            played.Clear();
            var failPort = new FakePort { Succeed = false };
            var fail = NewEscape(failPort);
            fail.Open(failPort.GetDestinationOptions(), new EmergencyEscapeCost(0, 0));
            played.Clear();
            fail.ConfirmSelectedDestination();
            Assert.That(failPort.Escapes, Is.EqualTo(1));
            Assert.That(Count(SfxId.UiCancelFail), Is.EqualTo(1));
            Assert.That(Count(SfxId.PortalClose), Is.Zero);
        }

        // ---- 가짜 경계 ----

        private sealed class SellBackend : IResourceSellBackend
        {
            public int Owned = 5;
            public bool Fail;

            public event Action Changed;

            public string Hint => "hint";
            public string BulkRuleNotice => string.Empty;

            public ResourceSellSnapshot Read(bool fresh)
            {
                var lines = Owned > 0
                    ? new[] { new ResourceSellLine(Copper, Copper, Owned, 10, 1f, null) }
                    : Array.Empty<ResourceSellLine>();
                return new ResourceSellSnapshot(lines, 0, Owned, 60f, 0, ResourceSellBonusMode.PerLine);
            }

            public ResourceSellCommitResult Commit(IReadOnlyList<KeyValuePair<string, int>> items)
            {
                if (Fail)
                {
                    return new ResourceSellCommitResult(false, 0, "실패");
                }

                var total = 0;
                foreach (var item in items)
                {
                    Owned -= item.Value;
                    total += item.Value * 10;
                }

                Changed?.Invoke();
                return new ResourceSellCommitResult(true, total, "ok");
            }

            public void Dispose()
            {
            }
        }

        private sealed class FakePort : IEmergencyEscapePortalPort
        {
            public bool Succeed = true;
            public bool CloseDuringEscape;
            public int Escapes;
            public EmergencyEscapePanelBinder Binder;

            public bool TryOpenEscapePanel(out string reason)
            {
                reason = string.Empty;
                return true;
            }

            public IReadOnlyList<EmergencyEscapeDestinationOption> GetDestinationOptions()
            {
                return new[]
                {
                    new EmergencyEscapeDestinationOption(EmergencyEscapeDestination.Elevator, string.Empty, "엘리베이터")
                };
            }

            public bool TryEscapeTo(EmergencyEscapeDestination kind, string outpostInstanceId, out string reason)
            {
                Escapes++;
                // 실제 Runtime Bridge처럼 처리 중에 창을 닫는다.
                if (Succeed && CloseDuringEscape && Binder != null) Binder.Close();
                reason = Succeed ? string.Empty : "실패";
                return Succeed;
            }
        }
    }
}

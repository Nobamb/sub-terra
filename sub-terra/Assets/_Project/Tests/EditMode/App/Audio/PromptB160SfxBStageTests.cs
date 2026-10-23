using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using SubTerra.App.Audio;
using SubTerra.App.Editor.DataValidation;
using SubTerra.App.Integration;
using SubTerra.App.Inventory;
using SubTerra.App.Outpost;
using SubTerra.App.Run;
using SubTerra.App.UI.ControlTerminal;
using SubTerra.App.UI.EmergencyEscape;
using SubTerra.App.UI.Guide;
using SubTerra.App.UI.HUD;
using SubTerra.App.UI.MainMenu;
using SubTerra.App.UI.Outpost;
using SubTerra.App.UI.Sell;
using SubTerra.Shared;
using SubTerra.Shared.Audio;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SubTerra.App.Tests.Audio
{
    /// <summary>B-160: 효과음 B 단계 칸과 연결 위치. SfxRequests 기록으로 확인한다.</summary>
    public sealed class PromptB160SfxBStageTests
    {
        private const string Copper = "mineral.copper";

        private static readonly (SfxId Id, string File, SfxBus Bus)[] Table =
        {
            (SfxId.SellConfirm, "sell_confirm", SfxBus.UI),
            (SfxId.StorageBoxRise, "storage_box_rise", SfxBus.World),
            (SfxId.StorageLidOpen, "storage_lid_open", SfxBus.World),
            (SfxId.StorageTakeAll, "storage_take_all", SfxBus.World),
            (SfxId.GuidePageFlipX3, "guide_page_flip_x3", SfxBus.UI),
            (SfxId.GuidePageFlip, "guide_page_flip_loop", SfxBus.UI),
            (SfxId.TimerTick, "timer_tick", SfxBus.UI),
            (SfxId.TimerTickSingle, "timer_tick_single", SfxBus.UI),
            (SfxId.TimerEnd, "timer_end", SfxBus.UI),
            (SfxId.MinimapBeep, "minimap_beep", SfxBus.UI),
            (SfxId.KeycapPop, "keycap_pop", SfxBus.UI),
            (SfxId.PortalActivate, "portal_activate", SfxBus.World),
            (SfxId.PortalSwitch, "portal_switch", SfxBus.UI),
            (SfxId.PortalWarp, "portal_warp", SfxBus.World),
            (SfxId.GoldLever, "gold_lever", SfxBus.World),
            (SfxId.GoldReelStop, "gold_reel_stop", SfxBus.World),
            (SfxId.GoldCoinPop, "gold_coin_pop", SfxBus.World),
            (SfxId.StorageOrePop, "storage_ore_pop", SfxBus.World),
        };

        private readonly List<Object> created = new List<Object>();
        private readonly List<(SfxId Id, float Pitch)> played = new List<(SfxId, float)>();
        private float now;

        [SetUp]
        public void SetUp()
        {
            now = 10f;
            played.Clear();
            GameGuideState.ResetSessionForTests();
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
            GameGuideState.ResetSessionForTests();
        }

        private void Record(SfxId id, float pitch) => played.Add((id, pitch));

        private int Count(SfxId id)
        {
            var count = 0;
            foreach (var entry in played)
            {
                if (entry.Id == id) count++;
            }

            return count;
        }

        private List<float> Pitches(SfxId id)
        {
            var pitches = new List<float>();
            foreach (var entry in played)
            {
                if (entry.Id == id) pitches.Add(entry.Pitch);
            }

            return pitches;
        }

        private GameObject NewCanvas()
        {
            var root = new GameObject("B160Canvas", typeof(RectTransform), typeof(Canvas));
            ((RectTransform)root.transform).sizeDelta = new Vector2(1920f, 1080f);
            created.Add(root);
            return root;
        }

        // ---- 칸 ----

        [Test]
        public void Defaults_B160SlotsMatchTable_NumberedFrom17_NoAlertBus()
        {
            var defaults = new Dictionary<SfxId, (SfxEntry Entry, string File)>();
            foreach (var pair in SfxLibraryDefaults.Create())
            {
                Assert.That(defaults.ContainsKey(pair.Entry.Id), Is.False, pair.Entry.Id.ToString());
                defaults.Add(pair.Entry.Id, (pair.Entry, pair.FileName));
            }

            var numbers = new HashSet<int>();
            foreach (var row in Table)
            {
                Assert.That(numbers.Add((int)row.Id), Is.True, row.Id.ToString());
                Assert.That((int)row.Id, Is.InRange(17, 34), row.Id.ToString());
                Assert.That(defaults.TryGetValue(row.Id, out var actual), Is.True, row.Id.ToString());
                Assert.That(actual.File, Is.EqualTo(row.File), row.Id.ToString());
                Assert.That(actual.Entry.Bus, Is.EqualTo(row.Bus), row.Id.ToString());
                Assert.That(actual.Entry.Bus, Is.Not.EqualTo(SfxBus.Alert), "Alert는 경보 반복을 끊는다");
            }

            Assert.That((int)SfxId.SellConfirm, Is.EqualTo(17));
            Assert.That((int)SfxId.StorageOrePop, Is.EqualTo(34));
            Assert.That(defaults[SfxId.MinimapBeep].Entry.MaxDuration, Is.EqualTo(0.3f));
            Assert.That(defaults[SfxId.TimerTick].Entry.MaxDuration, Is.EqualTo(0.6f));
            Assert.That(defaults[SfxId.PortalActivate].Entry.MaxDuration, Is.EqualTo(1.3f));
            Assert.That(defaults[SfxId.GoldReelStop].Entry.MaxDuration, Is.EqualTo(0.6f));
            Assert.That(SfxLibraryDefaults.Fallbacks.Count, Is.EqualTo(1), "대체 표는 그대로");
        }

        [Test]
        public void AddressDocument_HasRowForEveryB160File()
        {
            var path = Path.GetFullPath(Path.Combine(Application.dataPath, "../../init/sound-effect-address.md"));
            var text = File.ReadAllText(path);
            foreach (var row in Table)
            {
                Assert.That(text, Does.Contain("| " + row.File + " |"), row.File);
            }
        }

        [Test]
        public void Crossed_IsTrueOnlyForTheStepThatPassesTheMark()
        {
            Assert.That(SfxPlayback.Crossed(0f, 0.3f, 0.36f), Is.False);
            Assert.That(SfxPlayback.Crossed(0.3f, 0.36f, 0.36f), Is.True);
            Assert.That(SfxPlayback.Crossed(0.36f, 0.4f, 0.36f), Is.False);
            Assert.That(SfxPlayback.Crossed(0f, 10f, 0.36f), Is.True, "큰 dt 한 번으로 건너뛰어도 한 번");
        }

        [Test]
        public void Throttle_BlocksWithinInterval_AndResetStartsOver()
        {
            var throttle = new SfxThrottle(0.25f, () => now);
            Assert.That(throttle.TryPass(), Is.True);
            now += 0.1f;
            Assert.That(throttle.TryPass(), Is.False);
            now += 0.2f;
            Assert.That(throttle.TryPass(), Is.True);
            throttle.Reset();
            Assert.That(throttle.TryPass(), Is.True);
        }

        // ---- 판매 ----

        [Test]
        public void Sell_SoldPlaysConfirmOnce_FailedIsSilent()
        {
            var sold = OpenSellPopup(new SellBackend());
            sold.Session.Adjust(Copper, 2);
            Assert.That(sold.RequestSell().IsSold, Is.True);
            Assert.That(Count(SfxId.SellConfirm), Is.EqualTo(1));

            played.Clear();
            var failed = OpenSellPopup(new SellBackend { Fail = true });
            failed.Session.Adjust(Copper, 1);
            Assert.That(failed.RequestSell().IsSold, Is.False);
            Assert.That(Count(SfxId.SellConfirm), Is.Zero);
        }

        private ResourceSellPopupView OpenSellPopup(SellBackend backend)
        {
            var popup = ResourceSellPopupView.Create(NewCanvas().transform, true);
            popup.ManualTick = true;
            popup.Attach(new ResourceSellSession(backend));
            popup.Show();
            popup.Tick(ResourceSellTimeline.OpenDuration + 0.01f);
            return popup;
        }

        // ---- 보관함 ----

        [Test]
        public void Storage_OpenPlaysRiseAndLidOnce_EvenWithOneHugeStep()
        {
            var popup = StoragePopupView.Create(NewCanvas().transform);
            popup.ManualTick = true;

            Assert.That(popup.Show(), Is.True);
            Assert.That(popup.Show(), Is.True, "이미 열린 상태면 다시 내지 않는다");
            Assert.That(Count(SfxId.StorageBoxRise), Is.EqualTo(1));

            popup.Tick(5f);
            popup.Tick(5f);
            Assert.That(Count(SfxId.StorageLidOpen), Is.EqualTo(1));
            Assert.That(Count(SfxId.StorageOrePop), Is.Zero, "튀어나올 광물이 없으면 무음");
        }

        [Test]
        public void Storage_OrePopsOnceWhenCargoExists_LidAtItsMark()
        {
            var popup = StoragePopupView.Create(NewCanvas().transform);
            popup.ManualTick = true;
            var cargo = new InventorySnapshot(9f, 60f, 0, new[]
            {
                new InventoryStackEntry(Copper, "구리", 3, 1.5f, 10),
                new InventoryStackEntry("mineral.iron", "철", 2, 2f, 15)
            });
            popup.SetCargo(cargo, new InventorySnapshot(0f, float.MaxValue, 0, Array.Empty<InventoryStackEntry>()));
            popup.Show();
            Assert.That(popup.PopCount, Is.GreaterThan(0));

            popup.Tick(StorageBoxTimeline.LidOpenStart - 0.01f);
            Assert.That(Count(SfxId.StorageLidOpen), Is.Zero);
            popup.Tick(0.02f);
            Assert.That(Count(SfxId.StorageLidOpen), Is.EqualTo(1));
            for (var i = 0; i < 40; i++) popup.Tick(0.02f);
            Assert.That(Count(SfxId.StorageOrePop), Is.EqualTo(1), "광물마다가 아니라 한 번");
        }

        [Test]
        public void TakeAll_OnlySuccessPlays()
        {
            OutpostPanelBinder.PlayTakeAllSfx(new OutpostOperationResult(
                OutpostOperationStatus.Success, OutpostOperationKind.Deposit, string.Empty, 3, 0, "ok"));
            OutpostPanelBinder.PlayTakeAllSfx(new OutpostOperationResult(
                OutpostOperationStatus.InvalidRequest, OutpostOperationKind.Withdraw, string.Empty, 0, 0, "no"));
            OutpostPanelBinder.PlayTakeAllSfx(null);
            Assert.That(Count(SfxId.StorageTakeAll), Is.EqualTo(1));
        }

        // ---- 가이드 ----

        [Test]
        public void Guide_FirstShowX3Once_ReopenFlipOnce_ResumeSilent()
        {
            var popup = GameGuidePopupView.Create(NewCanvas().transform, null);
            popup.ManualTick = true;

            popup.Show();
            Assert.That(popup.IsFullIntro, Is.True);
            Assert.That(Count(SfxId.GuidePageFlipX3), Is.EqualTo(1));
            Run(popup, GameGuideTimeline.FullOpenDuration + 0.1f);
            Assert.That(Count(SfxId.GuidePageFlipX3), Is.EqualTo(1));
            Assert.That(Count(SfxId.GuidePageFlip), Is.Zero, "첫 등장은 합친 소리 하나");

            popup.BeginClose();
            Run(popup, GameGuideTimeline.CloseDuration + 0.05f);
            popup.Show();
            Assert.That(popup.IsFullIntro, Is.False);
            Run(popup, GameGuideTimeline.FlipStartTime(false) - 0.02f);
            Assert.That(Count(SfxId.GuidePageFlip), Is.Zero);
            Run(popup, GameGuideTimeline.QuickOpenDuration);
            Assert.That(Count(SfxId.GuidePageFlip), Is.EqualTo(1));

            // 닫는 중 다시 열면 이어서 열리며 소리는 없다.
            popup.BeginClose();
            Run(popup, 0.1f);
            popup.Show();
            Run(popup, GameGuideTimeline.QuickOpenDuration + 0.1f);
            Assert.That(Count(SfxId.GuidePageFlip), Is.EqualTo(1));
            Assert.That(Count(SfxId.GuidePageFlipX3), Is.EqualTo(1));
        }

        private static void Run(GameGuidePopupView popup, float seconds)
        {
            for (var t = 0f; t < seconds - 0.0001f; t += 0.02f) popup.Tick(0.02f);
        }

        // ---- 타이머 ----

        [Test]
        public void ClockTick_OncePerSecondInFinalMinute_FirstJumpAndZeroSilent()
        {
            var view = NewClockView();
            view.SetFormattedClock("00:01:01");
            view.SetFormattedClock("00:01:00");
            view.SetFormattedClock("00:00:59");
            Assert.That(Count(SfxId.TimerTickSingle), Is.EqualTo(2), "61→60→59");

            played.Clear();
            view.SetFormattedClock("00:00:10");
            view.SetFormattedClock("00:00:07");
            view.SetFormattedClock("00:00:01");
            view.SetFormattedClock("00:00:00");
            Assert.That(Count(SfxId.TimerTickSingle), Is.Zero, "점프·0초 무음");

            Assert.That(MineResetClockView.IsFinalTick(-1, 59), Is.False, "첫 관측");
            Assert.That(MineResetClockView.IsFinalTick(62, 61), Is.False, "60초 위");
            Assert.That(MineResetClockView.IsFinalTick(2, 1), Is.True);
        }

        [Test]
        public void ClockTick_SilentWhileClockIsOff()
        {
            var view = NewClockView();
            view.SetFormattedClock("00:00:30");
            view.SetPowered(false, true);
            view.SetFormattedClock("00:00:29");
            view.SetFormattedClock("00:00:28");
            Assert.That(Count(SfxId.TimerTickSingle), Is.Zero);
        }

        private MineResetClockView NewClockView()
        {
            var host = new GameObject("B160ClockHost", typeof(RectTransform));
            created.Add(host);
            var overlay = MineResetClockOverlay.Create(host.transform);
            var clock = overlay.transform.Find("ClockRoot");
            clock.gameObject.SetActive(true);
            var view = clock.GetComponent<MineResetClockView>();
            view.SetPowered(true, true);
            played.Clear();
            return view;
        }

        [Test]
        public void ClockIntro_TickAtHandStart_BellAtHandEnd_OnceEach()
        {
            var go = new GameObject("B160Intro", typeof(RectTransform));
            created.Add(go);
            var intro = go.AddComponent<MineResetClockIntro>();
            intro.Play(null);

            intro.Tick(MineResetClockIntroTimeline.HandStart - 0.05f);
            Assert.That(played, Is.Empty);
            intro.Tick(0.1f);
            Assert.That(Count(SfxId.TimerTick), Is.EqualTo(1));
            intro.Tick(MineResetClockIntroTimeline.HandEnd);
            Assert.That(Count(SfxId.TimerEnd), Is.EqualTo(1));

            played.Clear();
            intro.Play(null);
            intro.Tick(10f);
            Assert.That(Count(SfxId.TimerTick), Is.EqualTo(1), "큰 dt 한 번에도 한 번씩");
            Assert.That(Count(SfxId.TimerEnd), Is.EqualTo(1));
            Assert.That(intro.IsPlaying, Is.False);
        }

        // ---- 미니맵 ----

        [Test]
        public void Minimap_EachPressBeepsOnce_PitchPerMode()
        {
            var canvas = NewCanvas();
            var root = new GameObject("ExplorationMinimap", typeof(RectTransform), typeof(CanvasRenderer), typeof(CanvasGroup));
            root.transform.SetParent(canvas.transform, false);
            var map = root.AddComponent<ExplorationMinimap>();

            Assert.That(map.PressMapKey(), Is.EqualTo(MinimapBoardMode.Wide));
            Assert.That(map.PressMapKey(), Is.EqualTo(MinimapBoardMode.Square));
            Assert.That(map.PressMapKey(), Is.EqualTo(MinimapBoardMode.Closed));
            Assert.That(Pitches(SfxId.MinimapBeep), Is.EqualTo(new[] { 1f, 1.15f, 0.85f }));
        }

        // ---- 키캡 ----

        [Test]
        public void Keycaps_PopTwoOrThreeTimesWithRisingPitch_SettledIsSilent()
        {
            Assert.That(ControlTerminalTimeline.CrossedKeyRow(0f, 0.41f), Is.EqualTo(-1));
            Assert.That(ControlTerminalTimeline.CrossedKeyRow(0.41f, 0.47f), Is.EqualTo(1));
            Assert.That(ControlTerminalTimeline.CrossedKeyRow(0f, 5f), Is.EqualTo(ControlTerminalTimeline.RowCount - 1));

            var scheme = ControlPreferences.Scheme;
            var settingsOpen = ControlPreferences.IsSettingsOpen;
            var hadPref = PlayerPrefs.HasKey(SettingsRuntimeApplier.PrefControls);
            var pref = PlayerPrefs.GetInt(SettingsRuntimeApplier.PrefControls, 0);
            try
            {
                var root = NewCanvas();
                var panel = ControlSchemePanel.Attach(root);
                var terminal = panel.Terminal;
                played.Clear();
                terminal.ShowSettled();
                for (var i = 0; i < 100; i++) terminal.Tick(0.01f);
                Assert.That(Count(SfxId.KeycapPop), Is.Zero, "연출 없이 열면 무음");

                terminal.PlayIntro();
                for (var i = 0; i < 120; i++) terminal.Tick(0.01f);
                var pitches = Pitches(SfxId.KeycapPop);
                Assert.That(pitches.Count, Is.InRange(2, 3));
                for (var i = 1; i < pitches.Count; i++) Assert.That(pitches[i], Is.GreaterThan(pitches[i - 1]));
                Assert.That(pitches[pitches.Count - 1], Is.LessThanOrEqualTo(1.12f + 0.0001f));

                played.Clear();
                terminal.PlayIntro();
                for (var i = 0; i < 120; i++) terminal.Tick(0.01f);
                Assert.That(Pitches(SfxId.KeycapPop).Count, Is.InRange(2, 3), "다시 열면 처음부터");
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

        [Test]
        public void Portal_OpenActivatesOnce_StepSwitches_SuccessWarps()
        {
            var host = new GameObject("B160Portal");
            created.Add(host);
            var view = host.AddComponent<EmergencyEscapePanelView>();
            var binder = host.AddComponent<EmergencyEscapePanelBinder>();
            SetField(view, "panelRoot", host);
            SetField(binder, "view", view);
            var port = new FakePort();
            binder.BindTo(port);
            binder.SwitchSfx.Clock = () => now;

            binder.Open(port.GetDestinationOptions(), new EmergencyEscapeCost(0, 0));
            Assert.That(Count(SfxId.PortalActivate), Is.EqualTo(1));
            Assert.That(Count(SfxId.PortalSwitch), Is.Zero, "첫 미리보기는 무음");

            binder.StepSelection(1);
            Assert.That(Count(SfxId.PortalSwitch), Is.EqualTo(1));
            binder.StepSelection(1);
            Assert.That(Count(SfxId.PortalSwitch), Is.EqualTo(1), "끝 항목에서 더 못 가면 무음");
            binder.StepSelection(-1);
            Assert.That(Count(SfxId.PortalSwitch), Is.EqualTo(1), "0.08초 안 연타는 한 번");
            now += 0.1f;
            binder.StepSelection(1);
            Assert.That(Count(SfxId.PortalSwitch), Is.EqualTo(2));

            binder.ConfirmSelectedDestination();
            Assert.That(port.Escapes, Is.EqualTo(1));
            Assert.That(Count(SfxId.PortalWarp), Is.EqualTo(1));
        }

        [Test]
        public void Portal_FailedEscapeHasNoWarp()
        {
            var host = new GameObject("B160PortalFail");
            created.Add(host);
            var view = host.AddComponent<EmergencyEscapePanelView>();
            var binder = host.AddComponent<EmergencyEscapePanelBinder>();
            SetField(view, "panelRoot", host);
            SetField(binder, "view", view);
            var port = new FakePort { Succeed = false };
            binder.BindTo(port);
            binder.Open(port.GetDestinationOptions(), new EmergencyEscapeCost(0, 0));

            binder.ConfirmSelectedDestination();
            Assert.That(port.Escapes, Is.EqualTo(1));
            Assert.That(Count(SfxId.PortalWarp), Is.Zero);
        }

        private static void SetField(object target, string name, object value)
        {
            var field = target.GetType().GetField(name,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, name);
            field.SetValue(target, value);
        }

        // ---- 골드 ----

        [Test]
        public void GoldState_LeverPulledOnceAtLeverDownStart_NotAgainAfterMerge()
        {
            var state = new GoldPickupPopupState(120, 0);
            Assert.That(state.Advance(GoldPickupPopupTimeline.LeverDownStart - 0.01f) & GoldPickupPopupEvents.LeverPulled,
                Is.EqualTo(GoldPickupPopupEvents.None));
            Assert.That(state.Advance(0.02f) & GoldPickupPopupEvents.LeverPulled,
                Is.EqualTo(GoldPickupPopupEvents.LeverPulled));
            state.Merge(30, 0);
            Assert.That(state.Advance(0.5f) & GoldPickupPopupEvents.LeverPulled, Is.EqualTo(GoldPickupPopupEvents.None));
        }

        [Test]
        public void GoldSfx_LeverOnce_ReelTwiceWithBonusPitch_MergeBurstCoinOnce()
        {
            var sfx = new GoldPickupSfx(() => now);
            var state = new GoldPickupPopupState(120, 30);
            for (var i = 0; i < 60; i++)
            {
                now += 0.02f;
                sfx.Handle(state.Advance(0.02f));
            }

            Assert.That(Count(SfxId.GoldLever), Is.EqualTo(1));
            Assert.That(Pitches(SfxId.GoldReelStop), Is.EqualTo(new[] { 1f, GoldPickupSfx.BonusReelPitch }));

            played.Clear();
            now += 1f;
            var merged = new GoldPickupMergeResult(10, 0, false, false);
            sfx.OnMerged(merged);
            now += 0.04f;
            sfx.OnMerged(merged);
            now += 0.04f;
            sfx.OnMerged(merged);
            Assert.That(Count(SfxId.GoldCoinPop), Is.EqualTo(1), "0.1초 안 합치기 3번");
            Assert.That(Count(SfxId.GoldLever) + Count(SfxId.GoldReelStop), Is.Zero, "합쳐도 레버·착지음은 다시 없다");

            sfx.OnMerged(default);
            now += 0.3f;
            sfx.OnMerged(default);
            Assert.That(Count(SfxId.GoldCoinPop), Is.EqualTo(1), "실제로 늘지 않은 합치기는 무음");
        }

        [Test]
        public void GoldSfx_BothLandingsInOneFrame_PlayReelOnceAtBasePitch()
        {
            var sfx = new GoldPickupSfx(() => now);
            sfx.Handle(GoldPickupPopupEvents.BaseLanded | GoldPickupPopupEvents.BonusLanded);
            Assert.That(Pitches(SfxId.GoldReelStop), Is.EqualTo(new[] { 1f }));
            Assert.That(Count(SfxId.GoldCoinPop), Is.EqualTo(1));
        }

        // ---- 가짜 경계 ----

        private sealed class SellBackend : IResourceSellBackend
        {
            private int owned = 5;
            public bool Fail;

            public event Action Changed;

            public string Hint => "hint";
            public string BulkRuleNotice => string.Empty;

            public ResourceSellSnapshot Read(bool fresh)
            {
                var lines = owned > 0
                    ? new[] { new ResourceSellLine(Copper, Copper, owned, 10, 1f, null) }
                    : Array.Empty<ResourceSellLine>();
                return new ResourceSellSnapshot(lines, 0, owned, 60f, 0, ResourceSellBonusMode.PerLine);
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
                    owned -= item.Value;
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
            public int Escapes;

            public bool TryOpenEscapePanel(out string reason)
            {
                reason = string.Empty;
                return true;
            }

            public IReadOnlyList<EmergencyEscapeDestinationOption> GetDestinationOptions()
            {
                return new[]
                {
                    new EmergencyEscapeDestinationOption(EmergencyEscapeDestination.Elevator, string.Empty, "엘리베이터"),
                    new EmergencyEscapeDestinationOption(EmergencyEscapeDestination.OutpostCore, "outpost.1", "전진기지 1")
                };
            }

            public bool TryEscapeTo(EmergencyEscapeDestination kind, string outpostInstanceId, out string reason)
            {
                Escapes++;
                reason = Succeed ? string.Empty : "실패";
                return Succeed;
            }
        }
    }
}

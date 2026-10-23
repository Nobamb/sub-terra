using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using SubTerra.App.Audio;
using SubTerra.App.Editor.DataValidation;
using SubTerra.App.Integration;
using SubTerra.App.Run;
using SubTerra.App.UI;
using SubTerra.App.UI.EmergencyEscape;
using SubTerra.App.UI.RunFailure;
using SubTerra.App.UI.Sell;
using SubTerra.App.UI.Tutorial;
using SubTerra.Shared;
using SubTerra.Shared.Audio;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SubTerra.App.Tests.Audio
{
    /// <summary>B-162: 효과음 C 단계 칸과 연결 위치. SfxRequests 기록으로 확인한다.</summary>
    public sealed class PromptB162SfxCStageTests
    {
        private const string Copper = "mineral.copper";

        private static readonly (SfxId Id, int Number, string File, SfxBus Bus, float Volume)[] Table =
        {
            (SfxId.RunFailGlitch, 35, "run_fail_collapse_glitch", SfxBus.UI, 0.6f),
            (SfxId.RunFailBlackout, 36, "run_fail_blackout", SfxBus.UI, 0.6f),
            (SfxId.RunFailRowTick, 37, "run_fail_row_tick", SfxBus.UI, 0.5f),
            (SfxId.EscapeArrive, 38, "escape_arrive", SfxBus.World, 0.6f),
            (SfxId.BriefingGlitch, 39, "briefing_glitch_tick", SfxBus.UI, 0.5f),
            (SfxId.BriefingGlitchShort, 40, "briefing_glitch_tick_short", SfxBus.UI, 0.45f),
            (SfxId.BriefingClose, 41, "briefing_close", SfxBus.UI, 0.5f),
            (SfxId.SellCoinStack, 42, "sell_open_coin_stack", SfxBus.UI, 0.5f),
            (SfxId.SellCoinBurst, 43, "sell_open_coin_burst", SfxBus.UI, 0.55f),
            (SfxId.PortalIdleLoop, 44, "portal_idle_loop", SfxBus.World, 0.35f),
        };

        private readonly List<Object> created = new List<Object>();
        private readonly List<(SfxId Id, float Pitch)> played = new List<(SfxId, float)>();
        private float now;

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
            for (var i = created.Count - 1; i >= 0; i--)
            {
                if (created[i] != null) Object.DestroyImmediate(created[i]);
            }

            created.Clear();
            UiPauseGate.Release(StartBriefingPopupMotion.PauseOwner);
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
            var root = new GameObject("B162Canvas", typeof(RectTransform), typeof(Canvas));
            ((RectTransform)root.transform).sizeDelta = new Vector2(1920f, 1080f);
            created.Add(root);
            return root;
        }

        // ---- 칸 ----

        [Test]
        public void Defaults_CSlotsMatchTable_NumberedFrom35_NoAlertBus()
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
                Assert.That(actual.Entry.Bus, Is.EqualTo(row.Bus), row.Id.ToString());
                Assert.That(actual.Entry.Bus, Is.Not.EqualTo(SfxBus.Alert), "Alert는 경보 반복을 끊는다");
                Assert.That(actual.Entry.Volume, Is.EqualTo(row.Volume), row.Id.ToString());

                Assert.That(library.TryGetEntry(row.Id, out var asset), Is.True, row.Id.ToString());
                Assert.That(asset.Clip, Is.Not.Null, row.Id.ToString());
                Assert.That(asset.Clip.name, Is.EqualTo(row.File), row.Id.ToString());
                Assert.That(asset.Bus, Is.EqualTo(row.Bus), row.Id.ToString());
            }

            Assert.That(defaults[SfxId.BriefingClose].Entry.MaxDuration, Is.EqualTo(1.0f));
            Assert.That(defaults[SfxId.BriefingClose].Entry.FadeOut, Is.EqualTo(0.3f));
            Assert.That(defaults[SfxId.SellCoinBurst].Entry.MaxDuration, Is.Zero, "동전 터짐은 끝까지");
            Assert.That((int)SfxId.StorageOrePop, Is.EqualTo(34), "기존 번호는 그대로");
            Assert.That(SfxLibraryDefaults.Fallbacks.Count, Is.EqualTo(1), "대체 표는 그대로");
        }

        [Test]
        public void PortalLoopClip_ImportsCompressedInMemory()
        {
            var importer = AssetImporter.GetAtPath("Assets/_Project/Audio/SFX/World/portal_idle_loop.wav") as AudioImporter;
            Assert.That(importer, Is.Not.Null);
            Assert.That(importer.defaultSampleSettings.loadType, Is.EqualTo(AudioClipLoadType.CompressedInMemory));
            var oneShot = AssetImporter.GetAtPath("Assets/_Project/Audio/SFX/UI/run_fail_row_tick.wav") as AudioImporter;
            Assert.That(oneShot.defaultSampleSettings.loadType, Is.EqualTo(AudioClipLoadType.DecompressOnLoad));
        }

        // ---- 탐사 실패 창 ----

        private RunFailurePanelView NewFailureView()
        {
            var canvas = NewCanvas();
            var panel = new GameObject("B162RunFailure", typeof(RectTransform));
            panel.transform.SetParent(canvas.transform, false);
            var view = panel.AddComponent<RunFailurePanelView>();
            view.BuildOrUpdateHierarchy();
            return view;
        }

        private static PlayerRescueResultDto Rescue(int rows)
        {
            var cargo = new List<CargoLossEntryDto>();
            var ids = new[] { "mineral.copper", "mineral.iron", "mineral.silver" };
            for (var i = 0; i < rows; i++)
            {
                cargo.Add(new CargoLossEntryDto { mineralId = ids[i], quantity = 3 + i });
            }

            return new PlayerRescueResultDto
            {
                failureToken = "b162",
                cause = RunFailureCause.StructuralCollapse,
                preservationRatio = 0.6f,
                lostCargo = cargo
            };
        }

        private static void RunReveal(RunFailurePanelView view, float seconds)
        {
            for (var t = 0f; t < seconds - 0.0001f; t += 0.02f) view.AdvanceReveal(0.02f);
        }

        [Test]
        public void Failure_ThreeRowsTickThreeTimes_GlitchOnceOnShow()
        {
            var view = NewFailureView();
            view.Show(Rescue(3));
            Assert.That(Count(SfxId.RunFailGlitch), Is.EqualTo(1));
            Assert.That(Count(SfxId.RunFailRowTick), Is.Zero, "창 글리치 동안은 무음");

            RunReveal(view, view.ActiveRevealTimeline.TotalDuration + 0.1f);
            Assert.That(view.IsRevealComplete, Is.True);
            Assert.That(Count(SfxId.RunFailRowTick), Is.EqualTo(3));
            Assert.That(Count(SfxId.RunFailGlitch), Is.EqualTo(1));
        }

        [Test]
        public void Failure_NoLostCargoIsSilent()
        {
            var view = NewFailureView();
            view.Show(Rescue(0));
            RunReveal(view, 1f);
            Assert.That(Count(SfxId.RunFailGlitch), Is.EqualTo(1));
            Assert.That(Count(SfxId.RunFailRowTick), Is.Zero);
        }

        [Test]
        public void Failure_SkipDoesNotPlayRemainingTicks()
        {
            var view = NewFailureView();
            view.Show(Rescue(3));
            RunReveal(view, view.ActiveRevealTimeline.GetItemStartTime(0) + 0.02f);
            Assert.That(Count(SfxId.RunFailRowTick), Is.EqualTo(1));

            Assert.That(view.ConfirmOrSkip(), Is.False, "첫 입력은 스킵");
            Assert.That(view.IsRevealComplete, Is.True);
            RunReveal(view, 1f);
            Assert.That(Count(SfxId.RunFailRowTick), Is.EqualTo(1));
        }

        private static void Drain(IEnumerator routine)
        {
            var guard = 0;
            while (routine.MoveNext() && guard++ < 10000)
            {
            }
        }

        [TestCase(5f)]
        [TestCase(0.02f)]
        public void Transition_BlackoutOnceAtDarken_PopupOpenOnceAtTvTurnOn(float step)
        {
            var view = NewFailureView();
            view.Show(Rescue(1));
            view.ConfirmOrSkip();
            Assert.That(view.ConfirmOrSkip(), Is.True);
            played.Clear();

            var moves = 0;
            Drain(view.PlayTransitionRoutine(true, () => moves++, () => step));
            Assert.That(moves, Is.EqualTo(1));
            Assert.That(Count(SfxId.RunFailBlackout), Is.EqualTo(1));
            Assert.That(Count(SfxId.UiPopupOpen), Is.EqualTo(1), "큰 dt로 건너뛰어도 한 번");
            Assert.That(Count(SfxId.RunFailGlitch), Is.Zero, "강한 글리치 단계는 무음");
        }

        [Test]
        public void Transition_SurfaceLoadPathHasNoTvTurnOnSound()
        {
            var view = NewFailureView();
            view.Show(Rescue(1));
            view.ConfirmOrSkip();
            view.ConfirmOrSkip();
            played.Clear();

            Drain(view.PlayTransitionRoutine(false, null, () => 0.05f));
            Assert.That(Count(SfxId.RunFailBlackout), Is.EqualTo(1));
            Assert.That(Count(SfxId.UiPopupOpen), Is.Zero, "TV 켜짐은 지상 Scene 로드 경로에서 재생되지 않는다");
        }

        [Test]
        public void TransitionTimeline_CrossedTvTurnOnOnlyOnce()
        {
            var timeline = new RunFailureTransitionTimeline(RunFailureCause.Unknown, null, true);
            var start = timeline.PreSurfaceLoadDuration;
            Assert.That(timeline.CrossedTvTurnOn(0f, start - 0.01f), Is.False);
            Assert.That(timeline.CrossedTvTurnOn(start - 0.01f, start + 0.01f), Is.True);
            Assert.That(timeline.CrossedTvTurnOn(start + 0.01f, timeline.TotalDuration), Is.False);
            Assert.That(timeline.CrossedTvTurnOn(0f, 10f), Is.True);
        }

        // ---- 판매 창 ----

        private ResourceSellPopupView NewSellPopup()
        {
            var popup = ResourceSellPopupView.Create(NewCanvas().transform, true);
            popup.ManualTick = true;
            popup.Attach(new ResourceSellSession(new SellBackend()));
            return popup;
        }

        [Test]
        public void SellTimeline_CoinLandTimesAndPitches()
        {
            var expected = new[] { 0.15f, 0.23f, 0.31f, 0.39f };
            var pitches = new[] { 1f, 1.04f, 1.08f, 1.12f };
            Assert.That(ResourceSellTimeline.CoinCount, Is.EqualTo(4));
            for (var i = 0; i < 4; i++)
            {
                Assert.That(ResourceSellTimeline.CoinLandTime(i), Is.EqualTo(expected[i]).Within(0.0001f));
                Assert.That(ResourceSellTimeline.CoinStackPitch(i), Is.EqualTo(pitches[i]).Within(0.0001f));
                Assert.That(ResourceSellTimeline.CoinDropProgress(ResourceSellTimeline.CoinLandTime(i), i),
                    Is.EqualTo(1f).Within(0.0001f), "착지 시각에 낙하가 끝난다");
            }

            Assert.That(ResourceSellTimeline.ImpactTime, Is.EqualTo(0.56f));
        }

        [TestCase(0.02f)]
        [TestCase(2f)]
        public void Sell_OpenStacksFourCoinsWithRisingPitch_BurstOnce(float step)
        {
            var popup = NewSellPopup();
            popup.Show();
            for (var t = 0f; t < ResourceSellTimeline.OpenDuration + 0.05f; t += step) popup.Tick(step);

            var pitches = Pitches(SfxId.SellCoinStack);
            Assert.That(pitches.Count, Is.EqualTo(4));
            var expected = new[] { 1f, 1.04f, 1.08f, 1.12f };
            for (var i = 0; i < 4; i++) Assert.That(pitches[i], Is.EqualTo(expected[i]).Within(0.0001f));
            Assert.That(Count(SfxId.SellCoinBurst), Is.EqualTo(1));
            Assert.That(Count(SfxId.UiPopupClose), Is.Zero);
        }

        [Test]
        public void Sell_CloseOnce_ReopenWhileOpenIsSilent()
        {
            var popup = NewSellPopup();
            popup.Show();
            popup.Tick(ResourceSellTimeline.OpenDuration + 0.01f);
            played.Clear();

            Assert.That(popup.Show(), Is.True);
            popup.Tick(0.5f);
            Assert.That(Count(SfxId.SellCoinStack), Is.Zero, "이미 열린 창을 다시 열면 무음");

            Assert.That(popup.BeginClose(), Is.True);
            Assert.That(popup.BeginClose(), Is.False);
            Assert.That(Count(SfxId.UiPopupClose), Is.EqualTo(1));
            Assert.That(Count(SfxId.SellConfirm), Is.Zero);
        }

        // ---- 시작 브리핑 ----

        private static object Invoke(object target, string name, params object[] args)
        {
            var method = target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.That(method, Is.Not.Null, name);
            return method.Invoke(target, args);
        }

        private StartBriefingPopupMotion NewMotion(bool compact)
        {
            var root = new GameObject("B162Briefing", typeof(RectTransform));
            root.transform.SetParent(NewCanvas().transform, false);
            ((RectTransform)root.transform).sizeDelta = new Vector2(900f, 450f);
            var motion = root.AddComponent<StartBriefingPopupMotion>();
            if (compact)
            {
                Invoke(motion, "ConfigureCompact", new Vector2(900f, 450f), new Vector2(395f, 155f), null, false,
                    "Unlock", null, 1f);
            }

            // EditMode에서는 Awake/OnEnable이 불리지 않는다.
            Invoke(motion, "Awake");
            Invoke(motion, "OnEnable");
            return motion;
        }

        private static void RunMotion(StartBriefingPopupMotion motion, float seconds)
        {
            for (var t = 0f; t < seconds - 0.0001f; t += 0.02f) Invoke(motion, "Tick", 0.02f);
        }

        [Test]
        public void Briefing_IntroGlitchOnce_ShortGlitches_CloseOnce()
        {
            var motion = NewMotion(false);
            Invoke(motion, "BeginIntro");
            Assert.That(Count(SfxId.BriefingGlitch), Is.EqualTo(1));

            RunMotion(motion, StartBriefingTimeline.IntroDuration + 0.05f);
            Assert.That(motion.IsShown, Is.True);
            Assert.That(Count(SfxId.BriefingGlitch), Is.EqualTo(1));
            Assert.That(Count(SfxId.BriefingGlitchShort), Is.GreaterThanOrEqualTo(2), "화면 글리치 1·2번 버스트");

            var shortsBeforeClose = Count(SfxId.BriefingGlitchShort);
            Assert.That(motion.RequestClose(null), Is.True);
            Assert.That(motion.RequestClose(null), Is.False);
            RunMotion(motion, StartBriefingTimeline.CloseDuration + 0.05f);
            Assert.That(Count(SfxId.BriefingClose), Is.EqualTo(1));
            Assert.That(Count(SfxId.BriefingGlitchShort), Is.EqualTo(shortsBeforeClose), "닫는 중 짧은 글리치는 무음");
        }

        [Test]
        public void Briefing_ShortGlitchThrottledTo015Seconds()
        {
            var motion = NewMotion(false);
            Invoke(motion, "BeginIntro");
            // 0.15초 창 안에서 최대 한 번이므로, 등장 1.28초 동안 짧은 소리는 9번을 넘지 않는다.
            RunMotion(motion, StartBriefingTimeline.IntroDuration);
            Assert.That(Count(SfxId.BriefingGlitchShort),
                Is.LessThanOrEqualTo(Mathf.CeilToInt(StartBriefingTimeline.IntroDuration / StartBriefingPopupMotion.ShortGlitchSfxInterval)));
        }

        [Test]
        public void Briefing_CompactWindowsLikeDeepZoneUnlockAreSilent()
        {
            var motion = NewMotion(true);
            Invoke(motion, "BeginIntro");
            RunMotion(motion, StartBriefingTimeline.IntroDuration + 0.5f);
            Invoke(motion, "RequestCompactClose", (Action)null);
            RunMotion(motion, StartBriefingTimeline.CloseDuration + 0.05f);

            Assert.That(Count(SfxId.BriefingGlitch), Is.Zero);
            Assert.That(Count(SfxId.BriefingGlitchShort), Is.Zero);
            Assert.That(Count(SfxId.BriefingClose), Is.Zero);
        }

        // ---- 긴급 탈출 도착 ----

        private EmergencyEscapePanelBinder NewEscape(FakePort port)
        {
            var host = new GameObject("B162Portal");
            created.Add(host);
            var view = host.AddComponent<EmergencyEscapePanelView>();
            var binder = host.AddComponent<EmergencyEscapePanelBinder>();
            SetField(view, "panelRoot", host);
            SetField(binder, "view", view);
            binder.BindTo(port);
            binder.ArriveSfx.Clock = () => now;
            binder.Open(port.GetDestinationOptions(), new EmergencyEscapeCost(0, 0));
            return binder;
        }

        [Test]
        public void Escape_ArriveOnce035SecondsAfterWarp()
        {
            var binder = NewEscape(new FakePort());
            binder.ConfirmSelectedDestination();
            Assert.That(Count(SfxId.PortalWarp), Is.EqualTo(1));
            Assert.That(Count(SfxId.EscapeArrive), Is.Zero, "워프와 같은 프레임에는 내지 않는다");

            now += 0.3f;
            binder.TickArriveSfx();
            Assert.That(Count(SfxId.EscapeArrive), Is.Zero);
            now += EmergencyEscapePanelBinder.ArriveSfxDelay;
            binder.TickArriveSfx();
            binder.TickArriveSfx();
            Assert.That(Count(SfxId.EscapeArrive), Is.EqualTo(1));
        }

        [Test]
        public void Escape_FailedEscapeHasNoArrive()
        {
            var binder = NewEscape(new FakePort { Succeed = false });
            binder.ConfirmSelectedDestination();
            now += 1f;
            binder.TickArriveSfx();
            Assert.That(Count(SfxId.EscapeArrive), Is.Zero);
        }

        private static void SetField(object target, string name, object value)
        {
            var field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, name);
            field.SetValue(target, value);
        }

        // ---- 포탈 대기음 ----

        [Test]
        public void LoopMath_FullWithinTwoTiles_SilentFromTen_SmoothBetween()
        {
            Assert.That(PortalIdleLoopMath.Gain(0f, 2f, 10f), Is.EqualTo(1f));
            Assert.That(PortalIdleLoopMath.Gain(2f, 2f, 10f), Is.EqualTo(1f));
            Assert.That(PortalIdleLoopMath.Gain(10f, 2f, 10f), Is.EqualTo(0f));
            Assert.That(PortalIdleLoopMath.Gain(30f, 2f, 10f), Is.EqualTo(0f));
            var previous = 1f;
            for (var d = 2.5f; d < 10f; d += 0.5f)
            {
                var gain = PortalIdleLoopMath.Gain(d, 2f, 10f);
                Assert.That(gain, Is.LessThan(previous), "멀수록 작아진다");
                previous = gain;
            }

            var portals = new List<Vector2> { new Vector2(5f, 0f), new Vector2(-1f, 0f) };
            Assert.That(PortalIdleLoopMath.Nearest(Vector2.zero, portals, out var distance), Is.EqualTo(1));
            Assert.That(distance, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(PortalIdleLoopMath.Nearest(Vector2.zero, new List<Vector2>(), out _), Is.EqualTo(-1));
        }

        private PortalIdleLoopAudio NewLoop(Func<bool> muted, Func<bool> paused)
        {
            var host = new GameObject("B162Loop");
            created.Add(host);
            var loop = host.AddComponent<PortalIdleLoopAudio>();
            var library = AssetDatabase.LoadAssetAtPath<SfxLibrary>(PromptB156SfxLibraryBuilder.AssetPath);
            loop.ConfigureForTests(library, muted, paused);
            return loop;
        }

        [Test]
        public void Loop_FollowsNearestOfTwoPortals_WithoutRestart_StopsOutOfRange()
        {
            var loop = NewLoop(() => false, () => false);
            var portals = new List<Vector2> { new Vector2(20f, 0f), new Vector2(1f, 0f) };

            loop.Tick(Vector2.zero, portals, false);
            Assert.That(loop.NearestIndex, Is.EqualTo(1));
            Assert.That(loop.IsActive, Is.True);
            Assert.That(loop.Gain, Is.EqualTo(1f));
            Assert.That(loop.Source.loop, Is.True);
            Assert.That((Vector2)loop.Source.transform.position, Is.EqualTo(new Vector2(1f, 0f)));
            var source = loop.Source;

            // 다른 포탈 쪽으로 이동: 소리 하나가 위치만 옮긴다.
            loop.Tick(new Vector2(16f, 0f), portals, false);
            Assert.That(loop.NearestIndex, Is.EqualTo(0));
            Assert.That(loop.IsActive, Is.True);
            Assert.That(loop.Source, Is.SameAs(source));
            Assert.That((Vector2)loop.Source.transform.position, Is.EqualTo(new Vector2(20f, 0f)));
            Assert.That(loop.Gain, Is.InRange(0.01f, 0.99f));
            Assert.That(loop.GetComponentsInChildren<AudioSource>().Length, Is.EqualTo(1), "겹치지 않게 소스는 하나");

            loop.Tick(new Vector2(50f, 0f), portals, false);
            Assert.That(loop.Gain, Is.Zero);
            Assert.That(loop.IsActive, Is.False, "범위 밖이면 정지");

            loop.Tick(new Vector2(50f, 0f), new List<Vector2>(), false);
            Assert.That(loop.IsActive, Is.False, "포탈이 없으면 정지");
        }

        [Test]
        public void Loop_SuppressedOrMutedStops_PauseDucks()
        {
            var muted = false;
            var paused = false;
            var loop = NewLoop(() => muted, () => paused);
            var portals = new List<Vector2> { new Vector2(1f, 0f) };

            loop.Tick(Vector2.zero, portals, true);
            Assert.That(loop.IsActive, Is.False, "탐사 실패 처리 중에는 정지");

            paused = true;
            loop.Tick(Vector2.zero, portals, false);
            Assert.That(loop.IsActive, Is.True);
            Assert.That(loop.Gain, Is.EqualTo(0.35f).Within(0.0001f), "UI 정지 중에는 작게");

            muted = true;
            loop.Tick(Vector2.zero, portals, false);
            Assert.That(loop.IsActive, Is.False, "효과음 음소거");
        }

        private sealed class SellBackend : IResourceSellBackend
        {
            public event Action Changed;

            public string Hint => "hint";
            public string BulkRuleNotice => string.Empty;

            public ResourceSellSnapshot Read(bool fresh)
            {
                return new ResourceSellSnapshot(
                    new[] { new ResourceSellLine(Copper, Copper, 5, 10, 1f, null) }, 0, 5, 60f, 0,
                    ResourceSellBonusMode.PerLine);
            }

            public ResourceSellCommitResult Commit(IReadOnlyList<KeyValuePair<string, int>> items)
            {
                Changed?.Invoke();
                return new ResourceSellCommitResult(true, 0, "ok");
            }

            public void Dispose()
            {
            }
        }

        private sealed class FakePort : IEmergencyEscapePortalPort
        {
            public bool Succeed = true;

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
                reason = Succeed ? string.Empty : "실패";
                return Succeed;
            }
        }
    }
}

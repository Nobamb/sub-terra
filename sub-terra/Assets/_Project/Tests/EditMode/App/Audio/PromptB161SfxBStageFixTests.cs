using System.Collections.Generic;
using NUnit.Framework;
using SubTerra.App.Audio;
using SubTerra.App.Editor.DataValidation;
using SubTerra.App.UI.Guide;
using SubTerra.App.UI.HUD;
using SubTerra.Shared.Audio;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SubTerra.App.Tests.Audio
{
    /// <summary>B-161: 가이드 넘김·닫힘, 막판 째깍, 만료 연출 째깍 수정. SfxRequests 기록으로 확인한다.</summary>
    public sealed class PromptB161SfxBStageFixTests
    {
        private readonly List<Object> created = new List<Object>();
        private readonly List<SfxId> played = new List<SfxId>();
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

        // ---- 칸 음량 ----

        [TestCase(SfxId.GuidePageFlipX3, 0.8f)]
        [TestCase(SfxId.TimerTick, 0.9f)]
        [TestCase(SfxId.TimerTickSingle, 0.9f)]
        public void Volume_DefaultsAndAssetMatch(SfxId id, float volume)
        {
            SfxEntry defaults = null;
            foreach (var pair in SfxLibraryDefaults.Create())
            {
                if (pair.Entry.Id == id) defaults = pair.Entry;
            }

            Assert.That(defaults, Is.Not.Null);
            Assert.That(defaults.Volume, Is.EqualTo(volume));

            var library = AssetDatabase.LoadAssetAtPath<SfxLibrary>(PromptB156SfxLibraryBuilder.AssetPath);
            Assert.That(library, Is.Not.Null);
            Assert.That(library.TryGetEntry(id, out var entry), Is.True);
            Assert.That(entry.Volume, Is.EqualTo(volume));
            Assert.That(entry.Clip, Is.Not.Null);
        }

        [Test]
        public void Volume_OtherSlotsUnchanged()
        {
            var expected = new Dictionary<SfxId, float>
            {
                { SfxId.GuidePageFlip, 0.5f },
                { SfxId.TimerEnd, 0.6f },
                { SfxId.UiPopupClose, 0.55f },
            };
            foreach (var pair in SfxLibraryDefaults.Create())
            {
                if (expected.TryGetValue(pair.Entry.Id, out var volume))
                {
                    Assert.That(pair.Entry.Volume, Is.EqualTo(volume), pair.Entry.Id.ToString());
                }
            }
        }

        // ---- 가이드 ----

        private GameGuidePopupView NewGuide()
        {
            var root = new GameObject("B161Canvas", typeof(RectTransform), typeof(Canvas));
            ((RectTransform)root.transform).sizeDelta = new Vector2(1920f, 1080f);
            created.Add(root);
            var popup = GameGuidePopupView.Create(root.transform, null);
            popup.ManualTick = true;
            return popup;
        }

        private static void Run(GameGuidePopupView popup, float seconds)
        {
            for (var t = 0f; t < seconds - 0.0001f; t += 0.02f) popup.Tick(0.02f);
        }

        [Test]
        public void Guide_FirstShowX3Once_SecondOpenFlipOnce()
        {
            var popup = NewGuide();
            popup.Show();
            Run(popup, GameGuideTimeline.FullOpenDuration + 0.1f);
            Assert.That(Count(SfxId.GuidePageFlipX3), Is.EqualTo(1));
            Assert.That(Count(SfxId.GuidePageFlip), Is.Zero);

            popup.BeginClose();
            Run(popup, GameGuideTimeline.CloseDuration + 0.05f);
            played.Clear();
            popup.Show();
            Run(popup, GameGuideTimeline.QuickOpenDuration + 0.1f);
            Assert.That(Count(SfxId.GuidePageFlip), Is.EqualTo(1));
            Assert.That(Count(SfxId.GuidePageFlipX3), Is.Zero);
        }

        [Test]
        public void Guide_BeginClosePlaysCloseOnce_EvenWhenCalledTwice()
        {
            var popup = NewGuide();
            popup.Show();
            Run(popup, 0.3f);
            Assert.That(Count(SfxId.UiPopupClose), Is.Zero);

            Assert.That(popup.BeginClose(), Is.True);
            Assert.That(popup.BeginClose(), Is.False);
            Run(popup, 0.1f);
            Assert.That(popup.BeginClose(), Is.False);
            Assert.That(Count(SfxId.UiPopupClose), Is.EqualTo(1));

            // 닫던 중 다시 열기는 무음, 다시 닫으면 한 번 더.
            played.Clear();
            popup.Show();
            Run(popup, 0.1f);
            popup.BeginClose();
            Assert.That(Count(SfxId.UiPopupClose), Is.EqualTo(1));
            Assert.That(Count(SfxId.GuidePageFlip), Is.Zero);
            Assert.That(Count(SfxId.GuidePageFlipX3), Is.Zero);
        }

        [Test]
        public void Guide_HiddenCloseAndHideImmediateAreSilent()
        {
            var popup = NewGuide();
            popup.HideImmediate();
            Assert.That(popup.BeginClose(), Is.False);
            popup.HideImmediate();
            Assert.That(played, Is.Empty);

            popup.Show();
            Run(popup, 0.3f);
            played.Clear();
            popup.HideImmediate();
            Assert.That(popup.State, Is.EqualTo(GameGuidePopupView.PopupState.Hidden));
            Assert.That(played, Is.Empty);
        }

        // ---- 막판 째깍 게이트 ----

        [Test]
        public void Gate_OncePerSecond_61To60To59()
        {
            var gate = new MineResetClockTickGate(() => now);
            Assert.That(gate.TryTick(-1, 61, true), Is.False, "첫 관측");
            Assert.That(gate.TryTick(61, 60, true), Is.True);
            now += 1f;
            Assert.That(gate.TryTick(60, 59, true), Is.True);
        }

        [Test]
        public void Gate_FrameSkipStillTicksOnce()
        {
            var gate = new MineResetClockTickGate(() => now);
            Assert.That(gate.TryTick(10, 8, true), Is.True, "10→8");
            now += 1f;
            Assert.That(gate.TryTick(8, 5, true), Is.True, "3초 감소도 한 번");
        }

        [Test]
        public void Gate_TwoDecrementsWithinHalfSecond_TickOnce()
        {
            var gate = new MineResetClockTickGate(() => now);
            Assert.That(gate.TryTick(30, 29, true), Is.True);
            now += 0.4f;
            Assert.That(gate.TryTick(29, 28, true), Is.False);
            now += 0.55f;
            Assert.That(gate.TryTick(28, 27, true), Is.True, "0.9초가 지나면 다시 난다");
        }

        [Test]
        public void Gate_SilentCases()
        {
            var gate = new MineResetClockTickGate(() => now);
            Assert.That(gate.TryTick(-1, 30, true), Is.False, "첫 관측");
            Assert.That(gate.TryTick(1, 0, true), Is.False, "0초");
            Assert.That(gate.TryTick(20, 21, true), Is.False, "증가");
            Assert.That(gate.TryTick(20, 20, true), Is.False, "같은 값");
            Assert.That(gate.TryTick(62, 61, true), Is.False, "60초 위");
            Assert.That(gate.TryTick(20, 19, false), Is.False, "시계 꺼짐");
        }

        [Test]
        public void Gate_AfterDebugJump_TicksEverySecond()
        {
            var gate = new MineResetClockTickGate(() => now);
            gate.TryTick(1001, 1000, true);
            gate.TryTick(1000, 50, true);
            now += 0.3f;
            Assert.That(gate.TryTick(50, 49, true), Is.True, "49");
            now += 1f;
            Assert.That(gate.TryTick(49, 48, true), Is.True, "48");
        }

        [Test]
        public void ClockView_PowerOffStops_PowerOnResumesNextSecond()
        {
            var view = NewClockView();
            view.SetFormattedClock("00:00:30");
            now += 1f;
            view.SetFormattedClock("00:00:29");
            Assert.That(Count(SfxId.TimerTickSingle), Is.EqualTo(1));

            view.SetPowered(false, true);
            now += 1f;
            view.SetFormattedClock("00:00:28");
            Assert.That(Count(SfxId.TimerTickSingle), Is.EqualTo(1), "끄면 바로 멈춤");

            view.gameObject.SetActive(true);
            view.ResetObservation();
            view.SetPowered(true, true);
            now += 1f;
            view.SetFormattedClock("00:00:27");
            Assert.That(Count(SfxId.TimerTickSingle), Is.EqualTo(1), "다시 켠 첫 관측은 무음");
            now += 1f;
            view.SetFormattedClock("00:00:26");
            Assert.That(Count(SfxId.TimerTickSingle), Is.EqualTo(2), "다음 초부터 이어짐");
        }

        private MineResetClockView NewClockView()
        {
            var host = new GameObject("B161ClockHost", typeof(RectTransform));
            created.Add(host);
            var overlay = MineResetClockOverlay.Create(host.transform);
            var clock = overlay.transform.Find("ClockRoot");
            clock.gameObject.SetActive(true);
            var view = clock.GetComponent<MineResetClockView>();
            view.TickGate.Clock = () => now;
            view.SetPowered(true, true);
            played.Clear();
            return view;
        }

        // ---- 만료 연출 ----

        [Test]
        public void Intro_SmallSteps_TickAtHandStart_BellAtHandEnd_OnceEach()
        {
            var go = new GameObject("B161Intro", typeof(RectTransform));
            created.Add(go);
            var intro = go.AddComponent<MineResetClockIntro>();
            intro.Play(null);

            var t = 0f;
            var tickAt = -1f;
            var bellAt = -1f;
            while (intro.IsPlaying && t < 5f)
            {
                intro.Tick(0.05f);
                t += 0.05f;
                if (tickAt < 0f && Count(SfxId.TimerTick) > 0) tickAt = t;
                if (bellAt < 0f && Count(SfxId.TimerEnd) > 0) bellAt = t;
            }

            Assert.That(Count(SfxId.TimerTick), Is.EqualTo(1));
            Assert.That(Count(SfxId.TimerEnd), Is.EqualTo(1));
            Assert.That(tickAt, Is.InRange(MineResetClockIntroTimeline.HandStart - 0.001f,
                MineResetClockIntroTimeline.HandStart + 0.051f));
            Assert.That(bellAt, Is.InRange(MineResetClockIntroTimeline.HandEnd - 0.001f,
                MineResetClockIntroTimeline.HandEnd + 0.051f));
        }

        [Test]
        public void Intro_OneHugeStep_TickAndBellOnceEach()
        {
            var go = new GameObject("B161IntroBig", typeof(RectTransform));
            created.Add(go);
            var intro = go.AddComponent<MineResetClockIntro>();
            intro.Play(null);
            intro.Tick(10f);
            Assert.That(Count(SfxId.TimerTick), Is.EqualTo(1));
            Assert.That(Count(SfxId.TimerEnd), Is.EqualTo(1));
        }
    }
}

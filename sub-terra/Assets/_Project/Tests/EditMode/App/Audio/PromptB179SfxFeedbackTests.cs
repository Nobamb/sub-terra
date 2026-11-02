using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SubTerra.App.Audio;
using SubTerra.App.UI.ControlTerminal;
using SubTerra.App.UI.MainMenu;
using SubTerra.Shared;
using SubTerra.Shared.Audio;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SubTerra.App.Tests.Audio
{
    public sealed class PromptB179SfxFeedbackTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private readonly List<SfxId> played = new List<SfxId>();
        private GameObject root;
        private ControlSchemePanel panel;

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
            // 패널 닫기의 설정 저장 경로를 실행하지 않고 테스트 뷰만 정리한다.
            if (panel != null) panel.GetType().GetField("isOpen", Private).SetValue(panel, false);
            if (root != null) Object.DestroyImmediate(root);
            panel = null;
        }

        private void Record(SfxId id, float pitch) => played.Add(id);

        [Test]
        public void Slot91_MatchesDefaultsAssetImporter_AndSlot8IsUnchanged()
        {
            Assert.That((int)SfxId.SettingsSliderDrag, Is.EqualTo(91));
            var defaults = SfxLibraryDefaults.Create();
            var row = defaults.First(pair => pair.Entry.Id == SfxId.SettingsSliderDrag);
            Assert.That(row.FileName, Is.EqualTo("settings_slider_drag"));
            Assert.That(row.Entry.Bus, Is.EqualTo(SfxBus.UI));
            Assert.That(row.Entry.Volume, Is.EqualTo(0.55f));
            var library = Resources.Load<SfxLibrary>(SfxLibrary.ResourcePath);
            Assert.That(library.TryGetEntry(SfxId.SettingsSliderDrag, out var entry), Is.True);
            Assert.That(entry.Bus, Is.EqualTo(SfxBus.UI));
            Assert.That(entry.Volume, Is.EqualTo(0.55f));
            Assert.That(entry.Pitch, Is.EqualTo(1f));
            Assert.That(entry.MaxDuration, Is.Zero);
            Assert.That(entry.FadeOut, Is.Zero);
            var path = "Assets/_Project/Audio/SFX/UI/settings_slider_drag.wav";
            Assert.That(AssetDatabase.GetAssetPath(entry.Clip), Is.EqualTo(path));
            Assert.That(entry.Clip.length, Is.EqualTo(0.698f).Within(0.01f));
            var importer = (AudioImporter)AssetImporter.GetAtPath(path);
            Assert.That(importer.forceToMono, Is.True);
            Assert.That(importer.defaultSampleSettings.loadType, Is.EqualTo(AudioClipLoadType.DecompressOnLoad));
            Assert.That(library.TryGetEntry(SfxId.UiScanTick, out var scan), Is.True);
            Assert.That((int)SfxId.UiScanTick, Is.EqualTo(8));
            Assert.That(scan.Volume, Is.EqualTo(0.5f));
            Assert.That(scan.Clip.name, Is.EqualTo("ui_scan_tick"));
            Assert.That(defaults.First(pair => pair.Entry.Id == SfxId.UiScanTick).Entry.Volume, Is.EqualTo(0.5f));
        }

        private ControlTerminalView OpenTerminal()
        {
            root = new GameObject("B179 Controls", typeof(RectTransform), typeof(Canvas));
            ((RectTransform)root.transform).sizeDelta = new Vector2(1920f, 1080f);
            panel = ControlSchemePanel.Attach(root);
            panel.SetDraft(ControlScheme.Classic);
            panel.Open();
            played.Clear();
            return panel.Terminal;
        }

        private static SfxThrottle HoverClock(ControlTerminalView terminal) =>
            (SfxThrottle)typeof(ControlTerminalView).GetField("keyHoverSfx", Private).GetValue(terminal);

        [Test]
        public void Controls_FourButtonsHaveHoverOnly()
        {
            var terminal = OpenTerminal();
            foreach (var name in new[] { "PreviousScheme", "NextScheme", "CloseControls", "CloseControlsX" })
            {
                var button = terminal.Chrome.Find(name).GetComponent<Button>();
                var sound = button.GetComponent<UiSfxButton>();
                Assert.That(sound, Is.Not.Null, name);
                Assert.That(sound.ClickSfx, Is.EqualTo(SfxId.None), name);
                Assert.That(sound.Hover, Is.True, name);
                sound.OnPointerEnter(null);
                Assert.That(played, Is.EqualTo(new[] { SfxId.UiHover }), name);
                played.Clear();
            }
        }

        [Test]
        public void KeysAndMouse_UseSeparateSounds_WithSharedHoverGap_AndDemoIsSilent()
        {
            var terminal = OpenTerminal();
            var now = 10f;
            HoverClock(terminal).Clock = () => now;
            var key = terminal.GetCap("W");
            key.OnPointerEnter(null);
            terminal.GetCap("A").OnPointerEnter(null);
            Assert.That(played, Is.EqualTo(new[] { SfxId.KeycapPop }), "0.06초 안 두 키");
            played.Clear();
            now += 0.07f;
            terminal.GetCap(ControlTerminalBindings.MouseKey).OnPointerEnter(null);
            Assert.That(played, Is.EqualTo(new[] { SfxId.UiClick }));
            played.Clear();
            key.SetDemoDown(true);
            key.SetDemoDown(false);
            terminal.Tick(0.02f);
            Assert.That(played, Is.Empty, "자동 시연과 머무르는 동안 무음");
            key.OnPointerExit(null);
            now += 0.07f;
            key.OnPointerEnter(null);
            Assert.That(played, Is.EqualTo(new[] { SfxId.KeycapPop }));
        }

        [Test]
        public void Keys_IntroOutroHiddenAndNonFunctionAreSilent_SchemeChangeKeepsHover()
        {
            var terminal = OpenTerminal();
            terminal.PlayIntro();
            terminal.GetCap("W").OnPointerEnter(null);
            terminal.GetCap(ControlTerminalBindings.MouseKey).OnPointerEnter(null);
            Assert.That(played, Is.Empty);
            terminal.ShowSettled();
            var inactive = terminal.Caps[0];
            foreach (var cap in terminal.Caps) if (!cap.IsFunctionKey) { inactive = cap; break; }
            Assert.That(inactive.IsFunctionKey, Is.False);
            Assert.That(inactive.RaycastEnabled, Is.False);
            inactive.OnPointerEnter(null);
            Assert.That(played, Is.Empty);
            terminal.PlayOutro();
            terminal.GetCap("W").OnPointerEnter(null);
            terminal.ResetImmediate();
            terminal.GetCap("A").OnPointerEnter(null);
            Assert.That(played, Is.Empty);
            terminal.SetScheme(ControlScheme.WasdMove, false);
            terminal.ShowSettled();
            terminal.GetCap("←").OnPointerEnter(null);
            Assert.That(played, Is.EqualTo(new[] { SfxId.KeycapPop }));
        }

        [Test]
        public void TimedReset_StartsSilentDeparture_BeforeSharedWait()
        {
            var source = File.ReadAllText("Assets/_Project/Scripts/App/Save/SaveRuntimeController.cs");
            var start = source.IndexOf("private IEnumerator ExecuteTimedMineReset(");
            var end = source.IndexOf("private IEnumerator LoadAfterClockOff(", start);
            Assert.That(source.Substring(start, end - start), Does.Contain("BeginDeparture(playSound: false)"));
            Assert.That(source.Substring(end), Does.Contain("overlay.BeginDeparture()"));
        }
    }
}

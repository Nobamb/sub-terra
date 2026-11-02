using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using SubTerra.App.Audio;
using SubTerra.App.UI.ControlTerminal;
using SubTerra.App.UI.MainMenu;
using SubTerra.Shared;
using SubTerra.Shared.Audio;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SubTerra.App.Tests.Audio
{
    public sealed class PromptB1791ControlPanelTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private readonly List<SfxId> played = new List<SfxId>();
        private readonly List<float> pitches = new List<float>();
        private GameObject root;
        private ControlSchemePanel panel;
        private ControlScheme previousScheme;
        private bool previousSettingsOpen;
        private bool hadControls;
        private int previousControls;

        [SetUp]
        public void SetUp()
        {
            previousScheme = ControlPreferences.Scheme;
            previousSettingsOpen = ControlPreferences.IsSettingsOpen;
            hadControls = PlayerPrefs.HasKey(SettingsRuntimeApplier.PrefControls);
            previousControls = PlayerPrefs.GetInt(SettingsRuntimeApplier.PrefControls);
            played.Clear();
            pitches.Clear();
            SfxRequests.PlayRequested += Record;
            root = new GameObject("B1791 Controls", typeof(RectTransform), typeof(Canvas));
            ((RectTransform)root.transform).sizeDelta = new Vector2(1920f, 1080f);
            panel = ControlSchemePanel.Attach(root);
            panel.SetDraft(ControlScheme.Classic);
            panel.Open();
            ClearRequests();
        }

        [TearDown]
        public void TearDown()
        {
            SfxRequests.PlayRequested -= Record;
            // 정리 때문에 별도의 저장을 만들지 않는다.
            if (panel != null) typeof(ControlSchemePanel).GetField("isOpen", Private).SetValue(panel, false);
            if (root != null) Object.DestroyImmediate(root);
            ControlPreferences.Scheme = previousScheme;
            ControlPreferences.IsSettingsOpen = previousSettingsOpen;
            if (hadControls) PlayerPrefs.SetInt(SettingsRuntimeApplier.PrefControls, previousControls);
            else PlayerPrefs.DeleteKey(SettingsRuntimeApplier.PrefControls);
            PlayerPrefs.Save();
        }

        private void Record(SfxId id, float pitch)
        {
            played.Add(id);
            pitches.Add(pitch);
        }

        private void ClearRequests()
        {
            played.Clear();
            pitches.Clear();
        }

        private SfxThrottle HoverClock => (SfxThrottle)typeof(ControlTerminalView)
            .GetField("keyHoverSfx", Private).GetValue(panel.Terminal);

        [TestCase("W")]
        [TestCase("A")]
        [TestCase("Space")]
        public void LitKey_HoverUsesKeycapPopAndIntroRowPitch(string key)
        {
            HoverClock.Clock = () => 10f;
            var cap = panel.Terminal.GetCap(key);
            Assert.That(cap.IsFunctionKey, Is.True);
            cap.OnPointerEnter(null);
            Assert.That(played, Is.EqualTo(new[] { SfxId.KeycapPop }));
            Assert.That(pitches[0], Is.EqualTo(1f + cap.Def.Row * ControlTerminalView.KeycapPitchStep));

            ClearRequests();
            cap.SetDemoDown(true);
            cap.SetDemoDown(false);
            panel.Terminal.Tick(0.02f);
            Assert.That(played, Is.Empty, "자동 시연과 같은 키에 머무는 동안 무음");
        }

        [Test]
        public void Hover_MouseStaysClickAtUnitPitch_AndSharesKeyThrottle()
        {
            var now = 10f;
            HoverClock.Clock = () => now;
            panel.Terminal.GetCap("W").OnPointerEnter(null);
            panel.Terminal.GetCap("A").OnPointerEnter(null);
            panel.Terminal.GetCap(ControlTerminalBindings.MouseKey).OnPointerEnter(null);
            Assert.That(played, Is.EqualTo(new[] { SfxId.KeycapPop }), "0.06초 안에는 키와 마우스가 함께 제한된다");
            ClearRequests();
            now += 0.07f;
            panel.Terminal.GetCap(ControlTerminalBindings.MouseKey).OnPointerEnter(null);
            Assert.That(played, Is.EqualTo(new[] { SfxId.UiClick }));
            Assert.That(pitches[0], Is.EqualTo(1f));
        }

        [Test]
        public void SilentClose_SavesImmediately_StartsOutroOnce_AndRemainsVisibleUntilEnd()
        {
            panel.ForceMotionForTests = true;
            panel.SetDraft(ControlScheme.WasdMove);
            Assert.That(panel.Close(playSound: false), Is.True);
            Assert.That(panel.IsOpen, Is.False);
            Assert.That(panel.IsClosing, Is.True);
            Assert.That(panel.Terminal.IsOutroPlaying, Is.True);
            Assert.That(ControlPreferences.Scheme, Is.EqualTo(ControlScheme.WasdMove));
            Assert.That(PlayerPrefs.GetInt(SettingsRuntimeApplier.PrefControls), Is.EqualTo((int)ControlScheme.WasdMove));
            Assert.That(played, Is.Empty);

            panel.Terminal.Tick(0.05f);
            var alpha = panel.Terminal.Chrome.GetComponent<CanvasGroup>().alpha;
            Assert.That(panel.Close(playSound: false), Is.False);
            Assert.That(panel.Terminal.Chrome.GetComponent<CanvasGroup>().alpha, Is.EqualTo(alpha), "중복 요청이 닫힘 연출을 처음부터 시작하지 않는다");
            Assert.That(root.transform.Find("ControlSchemeOverlay").gameObject.activeSelf, Is.True);
            for (var elapsed = 0.05f; elapsed < ControlTerminalTimeline.OutroDuration + 0.05f; elapsed += 0.05f)
                panel.Terminal.Tick(0.05f);
            Assert.That(panel.Terminal.IsHidden, Is.True);
            Assert.That(panel.IsClosing, Is.False);
            typeof(ControlSchemePanel).GetMethod("Update", Private).Invoke(panel, null);
            Assert.That(root.transform.Find("ControlSchemeOverlay").gameObject.activeSelf, Is.False);
            Assert.That(played, Is.Empty, "설정창에서 같이 닫을 때 키 창 닫힘음은 없다");
        }

        [Test]
        public void DefaultClose_KeepsSingleStandalonePopupSound()
        {
            panel.ForceMotionForTests = true;
            Assert.That(panel.Close(), Is.True);
            Assert.That(panel.Close(), Is.False);
            Assert.That(played, Is.EqualTo(new[] { SfxId.UiPopupClose }));
            Assert.That(panel.Terminal.IsOutroPlaying, Is.True);
        }
    }
}

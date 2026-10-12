using System.Collections;
using NUnit.Framework;
using SubTerra.App.UI.ControlTerminal;
using SubTerra.App.UI.MainMenu;
using SubTerra.Shared;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;

namespace SubTerra.App.Tests.PlayMode
{
    /// <summary>
    /// 조작 단말 라이프사이클·타이밍. 이번 작업에서는 컴파일·정적 검토까지만 하고 실행하지 않았다.
    /// 시간은 timeScale 0에서도 흘러야 하므로 unscaled 기준으로 대기한다.
    /// </summary>
    public sealed class ControlTerminalPlayModeTests
    {
        private GameObject canvas;
        private ControlSchemePanel panel;
        private float previousTimeScale;
        private ControlScheme previousScheme;
        private bool hadPref;
        private int previousPref;

        [SetUp]
        public void SetUp()
        {
            previousTimeScale = Time.timeScale;
            previousScheme = ControlPreferences.Scheme;
            hadPref = PlayerPrefs.HasKey(SettingsRuntimeApplier.PrefControls);
            previousPref = PlayerPrefs.GetInt(SettingsRuntimeApplier.PrefControls, 0);
            Time.timeScale = 0f;
            canvas = new GameObject("ControlTerminalTestCanvas", typeof(RectTransform), typeof(Canvas));
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            panel = ControlSchemePanel.Attach(canvas);
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = previousTimeScale;
            if (canvas != null) Object.Destroy(canvas);
            ControlPreferences.Scheme = previousScheme;
            ControlPreferences.IsSettingsOpen = false;
            if (hadPref) PlayerPrefs.SetInt(SettingsRuntimeApplier.PrefControls, previousPref);
            else PlayerPrefs.DeleteKey(SettingsRuntimeApplier.PrefControls);
            PlayerPrefs.Save();
        }

        private static IEnumerator WaitUnscaled(float seconds)
        {
            var end = Time.realtimeSinceStartup + seconds;
            while (Time.realtimeSinceStartup < end) yield return null;
        }

        [UnityTest]
        public IEnumerator Open_PlaysIntroInUnscaledTimeAndSettlesToBaseValues()
        {
            panel.Open();
            var terminal = panel.Terminal;
            Assert.That(terminal.IsIntroPlaying, Is.True);
            yield return WaitUnscaled(ControlTerminalTimeline.IntroDuration + 0.4f);
            Assert.That(terminal.CurrentPhase, Is.EqualTo(ControlTerminalView.Phase.Idle));
            Assert.That(terminal.KeyboardRoot.anchoredPosition, Is.EqualTo(Vector2.zero));
            foreach (var cap in terminal.Caps)
            {
                Assert.That(cap.IsPopSettled, Is.True, cap.Id);
                Assert.That(cap.Level, Is.EqualTo(0f), cap.Id);
                Assert.That(cap.TopRect.anchoredPosition.y, Is.EqualTo(0f).Within(0.01f), cap.Id);
            }
        }

        [UnityTest]
        public IEnumerator Close_SavesImmediatelyAndOverlayHidesAfterOutro()
        {
            panel.SetDraft(ControlScheme.WasdMove);
            panel.Open();
            yield return WaitUnscaled(0.3f);
            Assert.That(panel.Close(), Is.True);
            Assert.That(panel.IsOpen, Is.False);
            Assert.That(SettingsRuntimeApplier.LoadControlScheme(), Is.EqualTo(ControlScheme.WasdMove));
            Assert.That(panel.Terminal.IsOutroPlaying, Is.True);
            yield return WaitUnscaled(ControlTerminalTimeline.OutroDuration + 0.3f);
            Assert.That(panel.transform.Find("ControlSchemeOverlay").gameObject.activeSelf, Is.False);
            Assert.That(panel.Terminal.IsHidden, Is.True);
        }

        [UnityTest]
        public IEnumerator HoverOverKey_ShowsLineAndOneScreen_AndLeavingCleansUp()
        {
            panel.Open();
            yield return WaitUnscaled(ControlTerminalTimeline.IntroDuration + 0.2f);
            var cap = panel.Terminal.GetCap("A");
            ExecuteEvents.Execute(cap.gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerEnterHandler);
            yield return WaitUnscaled(0.7f);
            Assert.That(panel.Terminal.HoverState.ActiveKey, Is.EqualTo("A"));
            Assert.That(panel.Terminal.ScreenVisible, Is.True);
            ExecuteEvents.Execute(cap.gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerExitHandler);
            yield return WaitUnscaled(0.7f);
            Assert.That(panel.Terminal.HoverState.IsIdle, Is.True);
            Assert.That(panel.Terminal.ScreenVisible, Is.False);
            Assert.That(panel.Terminal.Stage.Clip, Is.Null);
        }

        [UnityTest]
        public IEnumerator AutoDemo_PressesOneKeyWhenIdleAndStopsOnHover()
        {
            panel.Open();
            yield return WaitUnscaled(ControlTerminalTimeline.IntroDuration + ControlTerminalAutoDemo.IdleSeconds + 0.3f);
            int pressed = 0;
            foreach (var cap in panel.Terminal.Caps)
                if (cap.IsDemoDown) pressed++;
            Assert.That(pressed, Is.LessThanOrEqualTo(1));
            var target = panel.Terminal.GetCap("D");
            ExecuteEvents.Execute(target.gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerEnterHandler);
            yield return null;
            yield return null;
            Assert.That(panel.Terminal.AutoDemo.IsBlocked, Is.True);
            foreach (var cap in panel.Terminal.Caps)
                Assert.That(cap.IsDemoDown, Is.False);
        }

        [UnityTest]
        public IEnumerator Disable_CleansHoverStateAndSettingsFlag()
        {
            panel.Open();
            yield return WaitUnscaled(0.3f);
            ExecuteEvents.Execute(panel.Terminal.GetCap("D").gameObject, new PointerEventData(EventSystem.current),
                ExecuteEvents.pointerEnterHandler);
            yield return WaitUnscaled(0.3f);
            panel.enabled = false;
            Assert.That(panel.Terminal.HoverState.IsIdle, Is.True);
            Assert.That(panel.Terminal.IsHidden, Is.True);
            Assert.That(ControlPreferences.IsSettingsOpen, Is.False);
        }
    }
}

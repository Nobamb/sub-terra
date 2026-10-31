using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using SubTerra.App.Audio;
using SubTerra.App.Save;
using SubTerra.App.State;
using SubTerra.App.UI.HUD;
using SubTerra.App.UI.MainMenu;
using SubTerra.Shared.Audio;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SubTerra.App.Tests.PlayMode.Audio
{
    public sealed class PromptB177SfxMenuPlayModeTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private static void Set(object target, string field, object value) =>
            target.GetType().GetField(field, Private).SetValue(target, value);
        private static T Get<T>(object target, string field) =>
            (T)target.GetType().GetField(field, Private).GetValue(target);
        private static object Call(object target, string method, params object[] args) =>
            target.GetType().GetMethod(method, Private).Invoke(target, args);

        [UnityTest]
        public IEnumerator Runtime_SettingsTransitionsSliderDropdownAndToggle()
        {
            var events = new List<SfxId>();
            var pitchValues = new List<float>();
            Action<SfxId, float> record = (id, pitch) => { events.Add(id); pitchValues.Add(pitch); };
            SfxRequests.PlayRequested += record;
            var root = new GameObject("B177 Settings Test", typeof(RectTransform));
            root.SetActive(false);
            try
            {
                var sliderGo = new GameObject("Volume", typeof(RectTransform), typeof(Slider));
                sliderGo.transform.SetParent(root.transform, false);
                var slider = sliderGo.GetComponent<Slider>();
                var toggleGo = new GameObject("Toggle", typeof(RectTransform), typeof(Toggle));
                toggleGo.transform.SetParent(root.transform, false);
                var toggle = toggleGo.GetComponent<Toggle>();
                var dropdownGo = new GameObject("LanguageDropdown", typeof(RectTransform), typeof(TMP_Dropdown));
                dropdownGo.transform.SetParent(root.transform, false);
                var dropdown = dropdownGo.GetComponent<TMP_Dropdown>();
                dropdown.AddOptions(new List<string> { "A", "B" });
                var skin = root.AddComponent<SettingsMenuSkin>();
                Set(skin, "volumeSlider", slider);
                Set(skin, "reduceMotion", toggle);
                events.Clear();
                Assert.That(events, Is.Empty, "비활성 시작");
                root.SetActive(true);
                Assert.That(events, Is.EqualTo(new[] { SfxId.UiPopupOpen }));

                events.Clear(); pitchValues.Clear();
                var now = 100f;
                Get<SfxThrottle>(skin, "volumeTickThrottle").Clock = () => now;
                slider.value = 1f;
                slider.value = 0.5f;
                Assert.That(events, Is.EqualTo(new[] { SfxId.UiScanTick }), "한 프레임 이동·0.07초 제한");
                Assert.That(pitchValues[0], Is.EqualTo(1.1f).Within(1e-5));
                now += 0.08f;
                events.Clear();
                slider.value = 0.51f;
                Assert.That(events, Is.Empty, "5% 칸 내부");
                slider.value = 0.56f;
                Assert.That(events, Is.EqualTo(new[] { SfxId.UiScanTick }));
                Assert.That(pitchValues[pitchValues.Count - 1], Is.InRange(0.9f, 1.1f));
                events.Clear();
                slider.SetValueWithoutNotify(0.9f);
                dropdown.SetValueWithoutNotify(1);
                toggle.SetIsOnWithoutNotify(true);
                skin.RefreshVolumeSoundSteps();
                Assert.That(events, Is.Empty, "프로그램 재표시");
                slider.value = 0.91f;
                Assert.That(events, Is.Empty, "재표시 뒤 같은 칸");
                toggle.isOn = false;
                Assert.That(events, Is.EqualTo(new[] { SfxId.UiClick }));
                events.Clear();
                dropdown.value = 0;
                Assert.That(events, Is.EqualTo(new[] { SfxId.UiClick }));
                events.Clear();
                var trigger = dropdown.GetComponent<EventTrigger>();
                trigger.OnPointerDown(new PointerEventData(EventSystem.current));
                Assert.That(events, Is.EqualTo(new[] { SfxId.UiClick }));

                // 카드가 있어야 닫힘이 다음 프레임까지 유지된다.
                var card = new GameObject("Card", typeof(RectTransform));
                card.transform.SetParent(root.transform, false);
                Set(skin, "card", card.GetComponent<RectTransform>());
                events.Clear();
                skin.PlayCloseAnimation(() => root.SetActive(false));
                skin.PlayCloseAnimation(null);
                Assert.That(events, Is.EqualTo(new[] { SfxId.UiPopupClose }));
                yield return new WaitForSecondsRealtime(0.5f);
                root.SetActive(true);
                events.Clear();
                skin.PlayApplySound();
                skin.PlayCloseAnimation(() => root.SetActive(false));
                Assert.That(events, Is.EqualTo(new[] { SfxId.UiConfirm }));
            }
            finally
            {
                SfxRequests.PlayRequested -= record;
                Object.DestroyImmediate(root);
            }
        }

        [UnityTest]
        public IEnumerator Runtime_SideMenuAndClock_OnlyInputChangesMakeSound()
        {
            var events = new List<SfxId>();
            Action<SfxId, float> record = (id, pitch) => events.Add(id);
            SfxRequests.PlayRequested += record;
            var root = new GameObject("B177 HUD Test", typeof(RectTransform), typeof(CanvasGroup));
            var runtimeHost = new GameObject("B177 Runtime Test");
            runtimeHost.SetActive(false);
            var runtime = runtimeHost.AddComponent<SaveRuntimeController>();
            runtime.enabled = false;
            runtimeHost.SetActive(true);
            try
            {
                var menu = root.AddComponent<GameplaySideMenuController>();
                var open = new GameObject("Open", typeof(RectTransform));
                var closed = new GameObject("Closed", typeof(RectTransform));
                open.transform.SetParent(root.transform, false); closed.transform.SetParent(root.transform, false);
                Set(menu, "openRoot", open.GetComponent<RectTransform>());
                Set(menu, "closedRoot", closed.GetComponent<RectTransform>());
                Set(menu, "inputGroup", root.GetComponent<CanvasGroup>());
                events.Clear();
                Call(menu, "ResetClosed");
                Assert.That(events, Is.Empty);
                menu.Toggle(); menu.Toggle();
                Assert.That(events, Is.EqualTo(new[] { SfxId.UiPopupOpen }));
                yield return new WaitForSecondsRealtime(0.95f);
                events.Clear();
                menu.Toggle(); menu.Toggle();
                Assert.That(events, Is.EqualTo(new[] { SfxId.UiPopupClose }));

                var overlay = MineResetClockOverlay.Create(root.transform);
                var state = GameState.CreateNew();
                Set(runtime, "boundState", state);
                Set(runtime, "activeSlot", 1);
                overlay.SetSessionVisible(true);
                events.Clear();
                Call(overlay, "ToggleClockFromInput");
                Assert.That(events, Is.EqualTo(new[] { SfxId.WindowTvClose }));
                events.Clear();
                Call(overlay, "ToggleClockFromInput");
                Assert.That(events, Is.EqualTo(new[] { SfxId.UiCctvOn }));
                events.Clear();
                overlay.BeginDeparture();
                Call(overlay, "ToggleClockFromInput");
                Assert.That(events, Is.Empty, "출발은 무음");
                overlay.EndDeparture();
                Set(runtime, "activeSlot", 0);
                overlay.RefreshFromState();
                events.Clear();
                Call(overlay, "ToggleClockFromInput");
                Assert.That(events, Is.Empty, "슬롯 없음");
                overlay.SetSessionVisible(false);
                Call(overlay, "ToggleClockFromInput");
                Assert.That(events, Is.Empty, "세션 숨김");

                var main = new GameObject("B177 Invalid New Game", typeof(RectTransform));
                main.transform.SetParent(root.transform, false);
                main.SetActive(false);
                var mainView = main.AddComponent<MainMenuView>();
                var binder = main.AddComponent<MainMenuBinder>();
                binder.enabled = false;
                Set(binder, "view", mainView);
                events.Clear();
                Call(binder, "OnStartNewGame", 0);
                Assert.That(events, Is.EqualTo(new[] { SfxId.UiConfirm, SfxId.UiCancelFail }), "유효하지 않은 슬롯은 저장 없이 실패");
            }
            finally
            {
                SfxRequests.PlayRequested -= record;
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(runtimeHost);
            }
        }
    }
}

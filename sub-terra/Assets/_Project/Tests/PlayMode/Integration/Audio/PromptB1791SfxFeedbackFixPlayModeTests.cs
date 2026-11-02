using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using SubTerra.App.Audio;
using SubTerra.App.UI.MainMenu;
using SubTerra.Shared;
using SubTerra.Shared.Audio;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SubTerra.App.Tests.PlayMode.Audio
{
    public sealed class PromptB1791SfxFeedbackFixPlayModeTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private GameObject root;
        private SettingsMenuSkin skin;
        private Slider slider;
        private SliderDragLoopAudio loop;
        private bool muted;
        private bool paused;
        private readonly List<SfxId> played = new List<SfxId>();
        private bool hadControls;
        private int previousControls;
        private ControlScheme previousScheme;
        private bool previousSettingsOpen;

        private static T Get<T>(object value, string name) => (T)value.GetType().GetField(name, Private).GetValue(value);
        private static void Call(object value, string name, params object[] args) => value.GetType().GetMethod(name, Private).Invoke(value, args);

        [SetUp]
        public void SetUp()
        {
            hadControls = PlayerPrefs.HasKey(SettingsRuntimeApplier.PrefControls);
            previousControls = PlayerPrefs.GetInt(SettingsRuntimeApplier.PrefControls);
            previousScheme = ControlPreferences.Scheme;
            previousSettingsOpen = ControlPreferences.IsSettingsOpen;
            muted = paused = false;
            root = new GameObject("B1791 Settings", typeof(RectTransform), typeof(Canvas));
            root.SetActive(false);
            ((RectTransform)root.transform).sizeDelta = new Vector2(1920f, 1080f);
            var card = new GameObject("SettingsCard", typeof(RectTransform));
            card.transform.SetParent(root.transform, false);
            var child = new GameObject("Volume", typeof(RectTransform), typeof(Slider));
            child.transform.SetParent(card.transform, false);
            slider = child.GetComponent<Slider>();
            skin = root.AddComponent<SettingsMenuSkin>();
            typeof(SettingsMenuSkin).GetField("card", Private).SetValue(skin, card.transform);
            typeof(SettingsMenuSkin).GetField("volumeSlider", Private).SetValue(skin, slider);
            root.SetActive(true);
            loop = root.GetComponent<SliderDragLoopAudio>();
            loop.ConfigureForTests(Resources.Load<SfxLibrary>(SfxLibrary.ResourcePath), () => muted, () => paused);
            played.Clear();
            SfxRequests.PlayRequested += Record;
        }

        [TearDown]
        public void TearDown()
        {
            SfxRequests.PlayRequested -= Record;
            var panel = root != null ? root.GetComponent<ControlSchemePanel>() : null;
            if (panel != null) typeof(ControlSchemePanel).GetField("isOpen", Private).SetValue(panel, false);
            if (root != null) Object.DestroyImmediate(root);
            if (hadControls) PlayerPrefs.SetInt(SettingsRuntimeApplier.PrefControls, previousControls);
            else PlayerPrefs.DeleteKey(SettingsRuntimeApplier.PrefControls);
            PlayerPrefs.Save();
            ControlPreferences.Scheme = previousScheme;
            ControlPreferences.IsSettingsOpen = previousSettingsOpen;
        }

        private void Record(SfxId id, float pitch) => played.Add(id);

        private void Pointer(bool down)
        {
            var data = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left };
            var trigger = slider.GetComponent<EventTrigger>();
            if (down) trigger.OnPointerDown(data);
            else { trigger.OnPointerUp(data); trigger.OnEndDrag(data); }
        }

        [UnityTest]
        public IEnumerator HeadRelease_SchedulesOnlyTailAtHeadEnd_AndNoDuplicateRelease()
        {
            Pointer(true);
            Assert.That(loop.IsPlaying, Is.False, "누르기만 하면 무음");
            slider.value = 0.5f;
            Assert.That(loop.IsHeld, Is.True);
            Assert.That(loop.HeadSource.clip.name, Is.EqualTo("settings_slider_drag_head"));
            Assert.That(loop.LoopSource.loop, Is.True);
            Assert.That(loop.HeadSource.loop || loop.TailSource.loop, Is.False);
            Pointer(false);
            var tailAt = Get<double>(loop, "tailStart");
            Assert.That(tailAt, Is.EqualTo(Get<double>(loop, "loopStart")), "앞 중 놓기는 앞+끝, 반복 0회");
            Pointer(false);
            Assert.That(Get<double>(loop, "tailStart"), Is.EqualTo(tailAt));
            Assert.That(played, Is.Empty, "반복 조각은 SfxPlayer 원샷 요청을 하지 않는다");
            yield return new WaitForSecondsRealtime(0.85f);
            Assert.That(loop.IsPlaying, Is.False);
        }

        [UnityTest]
        public IEnumerator HeldLoop_UsesLiveVolume_ThenReleaseAtNextSampleBoundary()
        {
            Pointer(true);
            slider.value = 0.5f;
            var firstStart = Get<double>(loop, "loopStart");
            yield return new WaitForSecondsRealtime(1.1f);
            Assert.That(loop.IsHeld && loop.LoopSource.isPlaying, Is.True);
            Assert.That(Get<double>(loop, "loopStart"), Is.EqualTo(firstStart), "손이 멈춰도 앞을 다시 시작하지 않는다");
            var library = Resources.Load<SfxLibrary>(SfxLibrary.ResourcePath);
            Assert.That(loop.LoopSource.volume, Is.EqualTo(0.55f * library.GetBusVolume(SfxBus.UI)
                * SettingsRuntimeApplier.EffectiveVolume(AudioChannel.Sfx)).Within(0.0001f));
            Pointer(false);
            var remaining = Get<double>(loop, "tailStart") - AudioSettings.dspTime;
            Assert.That(remaining, Is.InRange(-0.01, 0.17));
            var cycles = (Get<double>(loop, "tailStart") - firstStart)
                / ((double)loop.LoopSource.clip.samples / loop.LoopSource.clip.frequency);
            Assert.That(cycles, Is.EqualTo(System.Math.Round(cycles)).Within(1e-8));
            yield return new WaitForSecondsRealtime(0.4f);
            Assert.That(loop.IsPlaying, Is.False);
        }

        [UnityTest]
        public IEnumerator CancellationPaths_ClearHeldAndTail_AndAllowNewDrag()
        {
            foreach (var reason in new[] { "close", "slider", "mute", "pause", "focus", "suspend", "disable", "missing" })
            {
                Pointer(true);
                slider.value = slider.value < 0.5f ? 0.8f : 0.2f;
                yield return new WaitForSecondsRealtime(0.62f);
                Assert.That(loop.IsHeld, Is.True, reason);
                switch (reason)
                {
                    case "close": skin.PlayCloseAnimation(null); break;
                    case "slider": slider.gameObject.SetActive(false); break;
                    case "mute": muted = true; break;
                    case "pause": paused = true; break;
                    case "focus": Call(skin, "OnApplicationFocus", false); Call(loop, "OnApplicationFocus", false); break;
                    case "suspend": Call(skin, "OnApplicationPause", true); Call(loop, "OnApplicationPause", true); break;
                    case "disable": root.SetActive(false); break;
                    case "missing": loop.ConfigureForTests(ScriptableObject.CreateInstance<SfxLibrary>(), () => false, () => false); break;
                }
                yield return new WaitForSecondsRealtime(0.08f);
                Assert.That(loop.IsHeld || loop.IsPlaying, Is.False, reason);
                Assert.That(loop.TailSource.isPlaying, Is.False, reason + " 끝소리 없이 정지");
                muted = paused = false;
                if (reason == "missing") Object.DestroyImmediate(Get<SfxLibrary>(loop, "library"));
                loop.ConfigureForTests(Resources.Load<SfxLibrary>(SfxLibrary.ResourcePath), () => muted, () => paused);
                Call(loop, "OnApplicationFocus", true);
                Call(loop, "OnApplicationPause", false);
                root.SetActive(false);
                root.SetActive(true);
                slider.gameObject.SetActive(true);
            }
        }

        [UnityTest]
        public IEnumerator SettingsClose_WithKeyPanel_WaitsForOutro_AndMakesOneCloseSound()
        {
            var panel = ControlSchemePanel.Attach(root);
            panel.Open();
            panel.Terminal.ShowSettled();
            played.Clear();
            var finished = false;
            skin.PlayCloseAnimation(() =>
            {
                Assert.That(panel.Terminal.IsHidden, Is.True, "설정창 비활성화 전에 키 창 퇴장 완료");
                finished = true;
                root.SetActive(false);
            });
            skin.PlayCloseAnimation(null);
            Assert.That(panel.IsClosing, Is.True);
            Assert.That(played, Is.EqualTo(new[] { SfxId.UiPopupClose }));
            var deadline = Time.realtimeSinceStartup + 2f;
            while (!finished && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(finished, Is.True);
            Assert.That(root.activeSelf, Is.False);
            Assert.That(played, Is.EqualTo(new[] { SfxId.UiPopupClose }));
        }
    }
}

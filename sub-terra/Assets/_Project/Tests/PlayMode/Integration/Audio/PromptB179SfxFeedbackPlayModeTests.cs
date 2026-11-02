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
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SubTerra.App.Tests.PlayMode.Audio
{
    public sealed class PromptB179SfxFeedbackPlayModeTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private readonly List<SfxId> played = new List<SfxId>();
        private readonly List<float> pitches = new List<float>();
        private readonly List<GameObject> roots = new List<GameObject>();
        private SettingsMenuSkin skin;
        private Slider slider;
        private float now;

        private static void Set(object target, string field, object value) =>
            target.GetType().GetField(field, Private).SetValue(target, value);
        private static T Get<T>(object target, string field) =>
            (T)target.GetType().GetField(field, Private).GetValue(target);
        private static void Call(object target, string method) =>
            target.GetType().GetMethod(method, Private).Invoke(target, null);

        [SetUp]
        public void SetUp()
        {
            played.Clear(); pitches.Clear(); roots.Clear();
            now = 100f;
            SfxRequests.PlayRequested += Record;
        }

        [TearDown]
        public void TearDown()
        {
            SfxRequests.PlayRequested -= Record;
            foreach (var root in roots) if (root != null) Object.DestroyImmediate(root);
            roots.Clear();
        }

        private void Record(SfxId id, float pitch)
        {
            played.Add(id); pitches.Add(pitch);
        }

        private GameObject Root(string name)
        {
            var root = new GameObject(name, typeof(RectTransform));
            roots.Add(root);
            return root;
        }

        private void MakeSlider()
        {
            var root = Root("B179 Settings");
            root.SetActive(false);
            var child = new GameObject("Volume", typeof(RectTransform), typeof(Slider));
            child.transform.SetParent(root.transform, false);
            ((RectTransform)child.transform).sizeDelta = new Vector2(100f, 20f);
            slider = child.GetComponent<Slider>();
            var fill = new GameObject("Fill", typeof(RectTransform));
            fill.transform.SetParent(child.transform, false);
            slider.fillRect = (RectTransform)fill.transform;
            skin = root.AddComponent<SettingsMenuSkin>();
            Set(skin, "volumeSlider", slider);
            root.SetActive(true);
            skin.GetComponent<SliderDragLoopAudio>().ConfigureForTests(
                Resources.Load<SfxLibrary>(SfxLibrary.ResourcePath), () => false, () => false);
            Get<SfxThrottle>(skin, "volumeKeyThrottle").Clock = () => now;
            played.Clear(); pitches.Clear();
        }

        private void Pointer(EventTriggerType type)
        {
            var data = new PointerEventData(EventSystem.current)
            {
                button = PointerEventData.InputButton.Left,
                position = new Vector2(40f, 0f)
            };
            var trigger = slider.GetComponent<EventTrigger>();
            if (type == EventTriggerType.PointerDown) trigger.OnPointerDown(data);
            else if (type == EventTriggerType.PointerUp) trigger.OnPointerUp(data);
            else trigger.OnEndDrag(data);
        }

        [UnityTest]
        public IEnumerator Slider_HeldOverOneSecond_KeepsOneHead_AndNextDragStartsAgain()
        {
            MakeSlider();
            Pointer(EventTriggerType.PointerDown);
            slider.value = 1f;
            slider.value = 0f;
            slider.value = 0.5f;
            var loop = skin.GetComponent<SliderDragLoopAudio>();
            var firstLoopStart = Get<double>(loop, "loopStart");
            yield return new WaitForSecondsRealtime(1.05f);
            now += 1.1f;
            slider.value = 1f;
            slider.value = 0f;
            Assert.That(played, Is.Empty, "드래그는 원샷 풀 대신 전용 AudioSource를 쓴다");
            Assert.That(loop.IsHeld, Is.True);
            Assert.That(Get<double>(loop, "loopStart"), Is.EqualTo(firstLoopStart));
            Pointer(EventTriggerType.PointerUp);
            Pointer(EventTriggerType.EndDrag);
            Pointer(EventTriggerType.PointerDown);
            slider.value = 1f;
            yield return new WaitForSecondsRealtime(0.05f);
            Assert.That(loop.IsHeld, Is.True);
            Assert.That(Get<double>(loop, "loopStart"), Is.GreaterThan(firstLoopStart));
        }

        [UnityTest]
        public IEnumerator Slider_TrackClickBeforeTrigger_IsCountedAsTheSameDrag()
        {
            MakeSlider();
            // 실제 Slider.OnPointerDown → EventTrigger 컴포넌트 순서로 보낸다.
            var data = new PointerEventData(EventSystem.current)
            {
                button = PointerEventData.InputButton.Left,
                position = new Vector2(40f, 0f)
            };
            ExecuteEvents.Execute(slider.gameObject, data, ExecuteEvents.pointerDownHandler);
            Assert.That(slider.value, Is.GreaterThan(0.5f));
            Assert.That(played, Is.Empty);
            var loop = skin.GetComponent<SliderDragLoopAudio>();
            Assert.That(loop.IsHeld, Is.True);
            var firstLoopStart = Get<double>(loop, "loopStart");
            now += 2f;
            slider.value = 0f;
            Assert.That(Get<double>(loop, "loopStart"), Is.EqualTo(firstLoopStart), "트랙 클릭 뒤 계속 끌어도 앞은 한 번");
            yield return null;
        }

        [UnityTest]
        public IEnumerator Slider_NoChangeSilent_NonPointerGapAndDisableRecovery()
        {
            MakeSlider();
            Pointer(EventTriggerType.PointerDown);
            Pointer(EventTriggerType.PointerUp);
            Assert.That(played, Is.Empty);
            slider.value = 0.5f;
            Call(skin, "LateUpdate");
            now += 0.69f;
            slider.value = 0.6f;
            Call(skin, "LateUpdate");
            Assert.That(played.Count, Is.EqualTo(1));
            now += 0.02f;
            slider.value = 0.7f;
            Call(skin, "LateUpdate");
            Assert.That(played.Count, Is.EqualTo(2));
            played.Clear();
            slider.SetValueWithoutNotify(0.8f);
            skin.RefreshVolumeSoundSteps();
            Call(skin, "LateUpdate");
            slider.value = 0.81f;
            Assert.That(played, Is.Empty, "재표시와 같은 5% 칸은 무음");
            Pointer(EventTriggerType.PointerDown);
            slider.value = 0.9f;
            slider.gameObject.SetActive(false);
            yield return null;
            slider.gameObject.SetActive(true);
            played.Clear();
            Pointer(EventTriggerType.PointerDown);
            slider.value = 0.5f;
            Assert.That(played, Is.Empty);
            Assert.That(skin.GetComponent<SliderDragLoopAudio>().IsHeld, Is.True, "슬라이더 비활성 뒤 새 드래그");
            skin.gameObject.SetActive(false);
            skin.gameObject.SetActive(true);
            played.Clear();
            yield return null;
            Assert.That(played, Is.Empty, "재열기만 하면 무음");
            Pointer(EventTriggerType.PointerDown);
            slider.value = 1f;
            Assert.That(played, Is.Empty);
            Assert.That(skin.GetComponent<SliderDragLoopAudio>().IsHeld, Is.True, "PointerUp 놓친 뒤 재열기");
            Assert.That(slider.GetComponent<EventTrigger>().triggers.FindAll(
                entry => entry.eventID == EventTriggerType.PointerDown).Count, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator Clock_VisibleDepartureOnce_OffAndTimedResetSilent_TInputUnchanged()
        {
            var host = Root("B179 Runtime");
            host.SetActive(false);
            var runtime = host.AddComponent<SaveRuntimeController>();
            runtime.enabled = false;
            host.SetActive(true);
            Set(runtime, "boundState", GameState.CreateNew());
            Set(runtime, "activeSlot", 1);
            var overlay = MineResetClockOverlay.Create(Root("B179 HUD").transform);
            overlay.SetSessionVisible(true);
            played.Clear();
            overlay.BeginDeparture();
            overlay.BeginDeparture();
            Assert.That(played, Is.EqualTo(new[] { SfxId.WindowTvClose }));
            played.Clear();
            overlay.EndDeparture();
            Assert.That(played, Is.Empty, "이동 취소 복구는 무음");
            overlay.BeginDeparture(playSound: false);
            overlay.BeginDeparture();
            Assert.That(played, Is.Empty, "만료 이후 공통 대기 경로도 무음");
            overlay.EndDeparture();
            Call(overlay, "ToggleClockFromInput");
            Assert.That(played, Is.EqualTo(new[] { SfxId.WindowTvClose }));
            played.Clear();
            overlay.BeginDeparture();
            Assert.That(played, Is.Empty, "이미 꺼진 시계");
            overlay.EndDeparture();
            Call(overlay, "ToggleClockFromInput");
            Assert.That(played, Is.EqualTo(new[] { SfxId.UiCctvOn }));
            overlay.SetSessionVisible(false);
            played.Clear();
            overlay.BeginDeparture();
            Assert.That(played, Is.Empty, "시계 숨김");
            yield return null;
        }
    }
}

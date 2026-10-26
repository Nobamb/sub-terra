using System.Collections;
using System.Reflection;
using NUnit.Framework;
using SubTerra.App.UI;
using SubTerra.App.UI.Building;
using SubTerra.App.UI.Inventory;
using SubTerra.App.UI.HUD;
using SubTerra.App.UI.Tutorial;
using SubTerra.Shared;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SubTerra.App.Tests.PlayMode
{
    public sealed class Issue213WindowOpenMotionTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private bool originalReduceMotion;
        private float originalTimeScale;
        private GameObject instance;

        [SetUp]
        public void SetUp()
        {
            originalReduceMotion = AccessibilityPreferences.ReduceMotion;
            originalTimeScale = Time.timeScale;
            AccessibilityPreferences.ReduceMotion = false;
        }

        [TearDown]
        public void TearDown()
        {
            if (instance != null) Object.DestroyImmediate(instance);
            AccessibilityPreferences.ReduceMotion = originalReduceMotion;
            Time.timeScale = originalTimeScale;
        }

        [UnityTest]
        public IEnumerator Issue213_RealMenuPrefabsRapidToggleAndPausedOpeningKeepInputAvailable()
        {
#if UNITY_EDITOR
            foreach (var name in new[] { "BuildingMenu", "InventoryPanel" })
            {
                var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/UI/" + name + ".prefab");
                Assert.That(prefab, Is.Not.Null);
                instance = Object.Instantiate(prefab);
                var building = instance.GetComponent<BuildingMenuView>();
                var inventory = instance.GetComponent<InventoryPanelView>();
                void Visible(bool value)
                {
                    if (building != null) building.SetVisible(value);
                    else inventory.SetVisible(value);
                }
                Visible(false);
                var scale = instance.transform.localScale;
                var position = ((RectTransform)instance.transform).anchoredPosition;
                var rect = (RectTransform)instance.transform;
                var center = rect.TransformPoint(rect.rect.center);
                var group = instance.GetComponent<CanvasGroup>();
                if (group == null) group = instance.AddComponent<CanvasGroup>();
                for (var i = 0; i < 3; i++)
                {
                    Visible(true);
                    var motion = instance.GetComponent<TerminalWindowOpenMotion>();
                    Assert.That(motion.IsPlaying, Is.True);
                    motion.Advance(TerminalWindowOpenMotion.Duration * 0.6f);
                    Assert.That(Vector3.Distance(rect.TransformPoint(rect.rect.center), center), Is.LessThan(0.01f));
                    var midway = instance.transform.localScale;
                    Visible(true);
                    Assert.That(instance.transform.localScale, Is.EqualTo(midway), "중복 표시 호출로 재시작하지 않는다.");
                    Assert.That(group.interactable && group.blocksRaycasts, Is.True);
                    Assert.That(Effects(motion).Find("WindowOpenLine").GetComponent<Image>().raycastTarget, Is.False);
                    if (inventory != null) Assert.That(inventory.CloseButton.gameObject.activeInHierarchy, Is.True);
                    Visible(false);
                    Assert.That(motion.IsClosing, Is.True);
                    Assert.That(instance.activeInHierarchy, Is.True, "닫힘을 마치기 전에 루트를 숨기지 않는다.");
                    Assert.That(group.interactable, Is.False);
                    var close = building != null ? building.CloseButton : inventory.CloseButton;
                    Assert.That(close.IsInteractable(), Is.False);
                    motion.Advance(TerminalWindowOpenMotion.CloseDuration + .01f);
                    Assert.That(motion.IsPlaying, Is.False);
                    Assert.That(instance.transform.localScale, Is.EqualTo(scale));
                }
                Time.timeScale = 0f;
                Visible(true);
                yield return new WaitForSecondsRealtime(TerminalWindowOpenMotion.Duration + 0.12f);
                Assert.That(instance.GetComponent<TerminalWindowOpenMotion>().IsPlaying, Is.False);
                Assert.That(instance.transform.localScale, Is.EqualTo(scale));
                Assert.That(((RectTransform)instance.transform).anchoredPosition, Is.EqualTo(position));
                Assert.That(instance.GetComponents<TerminalWindowOpenMotion>(), Has.Length.EqualTo(1));
                Assert.That(Effects(instance.GetComponent<TerminalWindowOpenMotion>()).childCount, Is.EqualTo(3));
                Assert.That(Effects(instance.GetComponent<TerminalWindowOpenMotion>()).gameObject.activeSelf, Is.False);
                Visible(false);
                instance.GetComponent<TerminalWindowOpenMotion>().Advance(TerminalWindowOpenMotion.CloseDuration + .01f);
                Object.DestroyImmediate(instance);
                instance = null;
            }
#else
            Assert.Ignore("실제 프리팹 검사는 Editor 전용입니다.");
            yield break;
#endif
        }

        [Test]
        public void Issue213_SurfaceQuestDetailsDoNotRestartOnPageRefreshAndRestoreOnParentClose()
        {
#if UNITY_EDITOR
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/UI/SurfaceBasePanel.prefab");
            instance = Object.Instantiate(prefab);
            var view = instance.GetComponentInChildren<DemoObjectiveView>(true);
            Assert.That(view, Is.Not.Null);
            var panel = (GameObject)typeof(DemoObjectiveView).GetField("detailsRoot", Private).GetValue(view);
            Assert.That(panel, Is.Not.Null);
            view.gameObject.SetActive(true);
            view.SetDetailsVisible(false);
            var scale = panel.transform.localScale;
            view.SetDetailsVisible(true);
            var motion = panel.GetComponent<TerminalWindowOpenMotion>();
            Assert.That(motion.IsPlaying, Is.True);
            motion.Advance(0.1f);
            var midway = panel.transform.localScale;
            view.SetDetailsVisible(true);
            Assert.That(panel.transform.localScale, Is.EqualTo(midway));
            instance.SetActive(false);
            Assert.That(motion.IsPlaying, Is.False);
            Assert.That(panel.transform.localScale, Is.EqualTo(scale));
            instance.SetActive(true);
            view.gameObject.SetActive(true);
            view.SetDetailsVisible(false);
            view.SetDetailsVisible(true);
            Assert.That(motion.IsPlaying, Is.True);
            view.SetDetailsVisible(false);
            Assert.That(motion.IsClosing, Is.True);
            motion.Advance(TerminalWindowOpenMotion.CloseDuration + .01f);
            Assert.That(motion.IsPlaying, Is.False);
            Assert.That(panel.transform.localScale, Is.EqualTo(scale));
#else
            Assert.Ignore("실제 프리팹 검사는 Editor 전용입니다.");
#endif
        }

        [Test]
        public void Issue241_PointLineContentReverseAndReduceMotionKeepStateAndCloseOnce()
        {
            instance = new GameObject("Issue241_Window", typeof(RectTransform), typeof(CanvasGroup));
            var rect = (RectTransform)instance.transform;
            rect.sizeDelta = new Vector2(500, 600);
            rect.pivot = new Vector2(0, 1);
            var group = instance.GetComponent<CanvasGroup>();
            group.alpha = .7f;
            var scale = rect.localScale;
            var center = rect.TransformPoint(rect.rect.center);
            TerminalWindowOpenMotion.Open(instance);
            var motion = instance.GetComponent<TerminalWindowOpenMotion>();
            motion.Advance(TerminalWindowOpenMotion.Duration * .1f);
            Assert.That(Effects(motion).Find("WindowOpenPoint").GetComponent<Image>().color.a, Is.GreaterThan(0));
            Assert.That(group.alpha, Is.Zero, "점 단계에서는 본문을 숨긴다.");
            motion.Advance(TerminalWindowOpenMotion.Duration * .22f);
            Assert.That(Effects(motion).Find("WindowOpenLine").GetComponent<RectTransform>().rect.width, Is.GreaterThan(0));
            Assert.That(group.alpha, Is.Zero, "선이 펼쳐질 때까지 본문을 숨긴다.");
            motion.Advance(TerminalWindowOpenMotion.Duration * .25f);
            Assert.That(group.alpha, Is.GreaterThan(0));
            Assert.That(Vector3.Distance(rect.TransformPoint(rect.rect.center), center), Is.LessThan(.01f));
            int completed = 0;
            TerminalWindowOpenMotion.Close(instance, () => completed++);
            motion.Advance(.01f);
            var closingScale = rect.localScale;
            TerminalWindowOpenMotion.Open(instance);
            Assert.That(rect.localScale, Is.EqualTo(closingScale), "역전 시 원래 크기로 튀지 않는다.");
            Assert.That(group.interactable, Is.True);
            motion.Advance(TerminalWindowOpenMotion.Duration);
            Assert.That(completed, Is.Zero, "취소된 닫기의 콜백은 실행하지 않는다.");
            TerminalWindowOpenMotion.Close(instance, () => completed++);
            TerminalWindowOpenMotion.Close(instance, () => completed += 10);
            motion.Advance(TerminalWindowOpenMotion.CloseDuration * .88f);
            Assert.That(Effects(motion).Find("WindowOpenPoint").GetComponent<Image>().color.a, Is.GreaterThan(0));
            Assert.That(group.alpha, Is.Zero);
            AccessibilityPreferences.ReduceMotion = true;
            motion.Advance(.001f);
            Assert.That(completed, Is.EqualTo(1));
            Assert.That(rect.localScale, Is.EqualTo(scale));
            Assert.That(group.alpha, Is.EqualTo(.7f));
            Assert.That(group.interactable && group.blocksRaycasts, Is.True);
            TerminalWindowOpenMotion.Open(instance);
            Assert.That(motion.IsPlaying, Is.False);
        }

        [Test]
        public void Issue241_HudBuildingCloseWaitsForAnimation()
        {
#if UNITY_EDITOR
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/UI/BuildingMenu.prefab");
            instance = Object.Instantiate(prefab);
            var view = instance.GetComponent<BuildingMenuView>();
            var host = new GameObject("Issue241_Chrome");
            host.SetActive(false);
            try
            {
                var chrome = host.AddComponent<HudPanelChromeController>();
                typeof(HudPanelChromeController).GetField("buildingMenuRoot", Private).SetValue(chrome, instance);
                typeof(HudPanelChromeController).GetField("buildingMenuView", Private).SetValue(chrome, view);
                var apply = typeof(HudPanelChromeController).GetMethod("ApplyBuildingMenuVisible", Private);
                apply.Invoke(chrome, new object[] { true, false });
                var motion = instance.GetComponent<TerminalWindowOpenMotion>();
                motion.Advance(TerminalWindowOpenMotion.Duration);
                apply.Invoke(chrome, new object[] { false, false });
                Assert.That(instance.activeSelf, Is.True);
                Assert.That(motion.IsClosing, Is.True);
                motion.Advance(TerminalWindowOpenMotion.CloseDuration + .01f);
                Assert.That(instance.activeSelf, Is.False);
            }
            finally { Object.DestroyImmediate(host); }
#else
            Assert.Ignore("실제 프리팹 검사는 Editor 전용입니다.");
#endif
        }

        private static Transform Effects(TerminalWindowOpenMotion motion)
            => (Transform)typeof(TerminalWindowOpenMotion).GetField("effects", Private).GetValue(motion);

        [Test]
        public void Issue213_ReduceMotionAndDisablePreserveOriginalScaleAndInputSettings()
        {
            instance = new GameObject("Issue213_Window", typeof(RectTransform), typeof(CanvasGroup));
            var group = instance.GetComponent<CanvasGroup>();
            group.alpha = 0.7f;
            group.interactable = false;
            group.blocksRaycasts = false;
            var scale = new Vector3(0.8f, 0.9f, 1f);
            instance.transform.localScale = scale;
            AccessibilityPreferences.ReduceMotion = true;
            TerminalWindowOpenMotion.Open(instance);
            Assert.That(instance.transform.localScale, Is.EqualTo(scale));
            var motion = instance.GetComponent<TerminalWindowOpenMotion>();
            Assert.That(motion.IsPlaying, Is.False);
            AccessibilityPreferences.ReduceMotion = false;
            TerminalWindowOpenMotion.Open(instance);
            Assert.That(motion.IsPlaying, Is.True);
            motion.Advance(0.09f);
            motion.enabled = false;
            Assert.That(instance.transform.localScale, Is.EqualTo(scale));
            Assert.That(group.alpha, Is.EqualTo(0.7f));
            Assert.That(group.interactable || group.blocksRaycasts, Is.False);
        }
    }
}

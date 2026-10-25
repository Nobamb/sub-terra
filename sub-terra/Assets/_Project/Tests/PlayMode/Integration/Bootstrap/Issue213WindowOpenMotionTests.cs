using System.Collections;
using System.Reflection;
using NUnit.Framework;
using SubTerra.App.UI;
using SubTerra.App.UI.Building;
using SubTerra.App.UI.Inventory;
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
                    Assert.That(instance.transform.Find("WindowOpenLine").GetComponent<Image>().raycastTarget, Is.False);
                    if (inventory != null) Assert.That(inventory.CloseButton.gameObject.activeInHierarchy, Is.True);
                    Visible(false);
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
                var beams = 0;
                foreach (Transform child in instance.transform) if (child.name == "WindowOpenLine") beams++;
                Assert.That(beams, Is.EqualTo(1));
                Assert.That(instance.transform.Find("WindowOpenLine").gameObject.activeSelf, Is.False);
                Visible(false);
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
            Assert.That(motion.IsPlaying, Is.False);
            Assert.That(panel.transform.localScale, Is.EqualTo(scale));
#else
            Assert.Ignore("실제 프리팹 검사는 Editor 전용입니다.");
#endif
        }

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

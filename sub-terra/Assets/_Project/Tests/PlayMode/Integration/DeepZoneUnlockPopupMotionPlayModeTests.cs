using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using SubTerra.App.UI;
using SubTerra.App.UI.Progression;
using SubTerra.App.UI.Tutorial;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace SubTerra.App.Tests.PlayMode
{
    /// <summary>B-154 심층 해금 창이 시작 브리핑 글리치로 열리고 닫히며, 정지 중에도 재생되고 잔상을 남기지 않는다.</summary>
    public sealed class DeepZoneUnlockPopupMotionPlayModeTests
    {
        private GameObject canvas;
        private ProgressionPanelView view;
        private float previousTimeScale;

        [SetUp]
        public void SetUp()
        {
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            canvas = new GameObject("DeepZoneMotionTestCanvas", typeof(RectTransform), typeof(Canvas));
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var panel = new GameObject("ProgressionPanel", typeof(RectTransform));
            panel.transform.SetParent(canvas.transform, false);
            var resultGo = new GameObject("ResultText", typeof(RectTransform));
            resultGo.transform.SetParent(panel.transform, false);
            var result = resultGo.AddComponent<TextMeshProUGUI>();
            view = panel.AddComponent<ProgressionPanelView>();
            typeof(ProgressionPanelView).GetField("resultText", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(view, result);
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(canvas);
            Time.timeScale = previousTimeScale;
        }

        [UnityTest]
        public IEnumerator OpensWithGlitchAndClosesWithoutBlockingInputWhilePaused()
        {
            Assert.That(DeepZoneUnlockPopupSkin.Load(), Is.Not.Null);
            var started = Time.realtimeSinceStartup;
            view.ShowDeepZoneUnlockPopup();
            var root = canvas.transform.Find(DeepZoneUnlockPopupMotion.RootName).gameObject;
            var transition = root.GetComponent<StartBriefingPopupMotion>();
            var frame = (RectTransform)root.transform.Find("UnlockTransition/Frame");
            var content = root.transform.Find("UnlockContent").GetComponent<CanvasGroup>();
            Assert.That(content.alpha, Is.Zero);
            yield return WaitFor(() => root.transform.Find("UnlockGlitch").gameObject.activeSelf);
            yield return WaitFor(() => frame.localScale.y > 0.05f && frame.localScale.y < 0.99f);
            yield return WaitFor(() => transition.IsShown);
            Assert.That(Time.realtimeSinceStartup - started, Is.LessThan(1.2f));
            Assert.That(content.alpha, Is.EqualTo(1f));
            Assert.That(root.transform.Find("UnlockGlitch").gameObject.activeSelf, Is.False);
            Assert.That(Time.timeScale, Is.Zero);
            Assert.That(PopupWindowSorting.Top, Is.EqualTo(root.GetComponent<Canvas>()));

            root.transform.Find("UnlockContent/CloseButton").GetComponent<Button>().onClick.Invoke();

            Assert.That(view.IsDeepZoneUnlockPopupOpen, Is.False);
            Assert.That(view.TryHideDeepZoneUnlockPopup(), Is.False);
            Assert.That(transition.IsClosing, Is.True);
            Assert.That(root.GetComponent<CanvasGroup>().blocksRaycasts, Is.False);
            yield return WaitFor(() => frame.localScale.y < 0.8f);
            Assert.That(root.activeSelf, Is.True);
            yield return WaitFor(() => !root.activeSelf);
            Assert.That(PopupWindowSorting.Top, Is.Not.EqualTo(root.GetComponent<Canvas>()));
            Assert.That(transition.Phase, Is.EqualTo(BriefingPhase.Hidden));
            Assert.That(Time.timeScale, Is.Zero);
        }

        [UnityTest]
        public IEnumerator RapidOpenCloseReopenLeavesSingleCleanWindow()
        {
            view.ShowDeepZoneUnlockPopup();
            yield return null;
            var root = canvas.transform.Find(DeepZoneUnlockPopupMotion.RootName).gameObject;
            var transition = root.GetComponent<StartBriefingPopupMotion>();
            var layerCount = root.GetComponentsInChildren<RectTransform>(true).Length;

            for (var i = 0; i < 3; i++)
            {
                Assert.That(view.TryHideDeepZoneUnlockPopup(), Is.True);
                Assert.That(transition.IsClosing, Is.True);
                yield return new WaitForSecondsRealtime(0.1f);
                view.ShowDeepZoneUnlockPopup();
                Assert.That(transition.IsClosing, Is.False);
                Assert.That(view.IsDeepZoneUnlockPopupOpen, Is.True);
                yield return null;
            }

            yield return WaitFor(() => transition.IsShown);
            Assert.That(root.GetComponents<StartBriefingPopupMotion>().Length, Is.EqualTo(1));
            Assert.That(root.GetComponentsInChildren<RectTransform>(true).Length, Is.EqualTo(layerCount));
            Assert.That(CountRoots(), Is.EqualTo(1));

            view.TryHideDeepZoneUnlockPopup();
            yield return WaitFor(() => !root.activeSelf);
            AssertNoLeftoverEffects(root);

            // 등장 도중 창이 꺼져도(화면 전환 등) 노이즈 막대·잔상 프레임이 남지 않는다.
            view.ShowDeepZoneUnlockPopup();
            yield return WaitFor(() => root.transform.Find("UnlockGlitch").gameObject.activeSelf);
            root.SetActive(false);
            AssertNoLeftoverEffects(root);
            Assert.That(view.IsDeepZoneUnlockPopupOpen, Is.False);
            view.ShowDeepZoneUnlockPopup();
            yield return WaitFor(() => transition.IsShown);
            Assert.That(view.IsDeepZoneUnlockPopupOpen, Is.True);
        }

        [UnityTest]
        public IEnumerator HeroArtScansDownCountsDepthThenPulsesBlueWhilePaused()
        {
            var skin = DeepZoneUnlockPopupSkin.Load();
            view.ShowDeepZoneUnlockPopup();
            var root = canvas.transform.Find(DeepZoneUnlockPopupMotion.RootName).gameObject;
            var hero = root.transform.Find("UnlockContent/" + DeepZoneUnlockPopupMotion.HeroName);
            var emblem = hero.Find("Emblem").GetComponent<Image>();
            var scan = hero.Find("ScanClip/ScanLine").GetComponent<Image>();
            var counter = root.transform.Find("UnlockContent/Message").GetComponent<TMP_Text>();
            var crystal = hero.Find("CrystalGlow0").GetComponent<Image>();
            var ring = hero.Find("PingRing0").GetComponent<Image>();
            var art = root.GetComponent<DeepZoneUnlockArtMotion>();

            // 창이 열리기 전에는 그림이 드러나지 않는다.
            Assert.That(emblem.fillAmount, Is.Zero);
            yield return WaitFor(() => emblem.fillAmount > 0.2f && emblem.fillAmount < 0.8f);
            Assert.That(scan.enabled, Is.True);
            Assert.That(counter.text, Is.Not.EqualTo(DeepZoneUnlockPopupText.Range(skin.minDepth, skin.maxDepth)));

            yield return WaitFor(() => emblem.fillAmount >= 1f);
            yield return WaitFor(() => art.Time > DeepZoneUnlockArtTimeline.ScanFadeEnd + 0.05f);
            Assert.That(counter.text, Is.EqualTo(DeepZoneUnlockPopupText.Range(skin.minDepth, skin.maxDepth)));
            Assert.That(scan.enabled, Is.False);
            yield return WaitFor(() => ring.enabled);

            // 유지 중 푸른 발광이 일정 주기로 밝아졌다 어두워진다.
            yield return WaitFor(() => art.Time > DeepZoneUnlockArtTimeline.IdleRampEnd);
            var low = 1f;
            var high = 0f;
            var until = Time.realtimeSinceStartup + 3.4f;
            while (Time.realtimeSinceStartup < until)
            {
                low = Mathf.Min(low, crystal.color.a);
                high = Mathf.Max(high, crystal.color.a);
                yield return null;
            }

            Assert.That(high - low, Is.GreaterThan(0.15f));
            Assert.That(Time.timeScale, Is.Zero);

            // 다시 열면 그림은 처음부터 다시 드러난다.
            view.TryHideDeepZoneUnlockPopup();
            yield return WaitFor(() => !root.activeSelf);
            view.ShowDeepZoneUnlockPopup();
            Assert.That(art.IsStarted, Is.False);
            yield return null;
            Assert.That(emblem.fillAmount, Is.LessThan(0.2f));
        }

        private static void AssertNoLeftoverEffects(GameObject root)
        {
            Assert.That(root.transform.Find("UnlockGlitch").gameObject.activeSelf, Is.False);
            foreach (var image in root.transform.Find("UnlockTransition").GetComponentsInChildren<Image>(true))
            {
                Assert.That(image.enabled, Is.False, image.name + " left visible");
            }
        }

        private int CountRoots()
        {
            var count = 0;
            for (var i = 0; i < canvas.transform.childCount; i++)
            {
                if (canvas.transform.GetChild(i).name == DeepZoneUnlockPopupMotion.RootName) count++;
            }

            return count;
        }

        private static IEnumerator WaitFor(Func<bool> condition)
        {
            var deadline = Time.realtimeSinceStartup + 5f;
            while (!condition() && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(condition(), Is.True, "Deep zone popup animation timed out.");
        }
    }
}

using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using SubTerra.App.UI.Tutorial;
using UnityEngine;
using UnityEngine.TestTools;

namespace SubTerra.App.Tests.PlayMode
{
    public sealed class QuestClearPopupMotionPlayModeTests
    {
        private GameObject canvas;
        private GameObject popup;
        private RectTransform check;
        private QuestClearPopupMotion motion;
        private float previousTimeScale;

        [SetUp]
        public void SetUp()
        {
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            canvas = new GameObject("QuestMotionTestCanvas", typeof(RectTransform), typeof(Canvas));
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            popup = new GameObject("QuestClearRewardPanel", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            popup.SetActive(false);
            popup.transform.SetParent(canvas.transform, false);
            popup.GetComponent<RectTransform>().sizeDelta = new Vector2(900f, 450f);
            check = new GameObject("ClaimCheckSign", typeof(RectTransform)).GetComponent<RectTransform>();
            check.SetParent(popup.transform, false);
            motion = popup.AddComponent<QuestClearPopupMotion>();
            typeof(QuestClearPopupMotion).GetField("checkSign", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(motion, check);
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(canvas);
            Time.timeScale = previousTimeScale;
        }

        [UnityTest]
        public IEnumerator FrameSpreadsThenOriginalCheckPopsAndCloseGathersWhilePaused()
        {
            popup.SetActive(true);
            var transition = popup.GetComponent<StartBriefingPopupMotion>();
            var frame = (RectTransform)popup.transform.Find("ClaimTransition/Frame");
            var content = popup.transform.Find("ClaimContent").GetComponent<CanvasGroup>();
            Assert.That(check.localScale, Is.EqualTo(Vector3.zero));
            Assert.That(content.alpha, Is.Zero);
            yield return WaitFor(() => popup.transform.Find("ClaimGlitch").gameObject.activeSelf);
            yield return WaitFor(() => popup.transform.Find("ClaimTransition/SignalLine")
                .GetComponent<UnityEngine.UI.Image>().enabled);
            yield return WaitFor(() => frame.localScale.y > 0.05f && frame.localScale.y < 0.99f);
            Assert.That(check.localScale, Is.EqualTo(Vector3.zero));
            Assert.That(popup.GetComponent<RectTransform>().anchoredPosition, Is.EqualTo(Vector2.zero));
            yield return WaitFor(() => transition.IsShown);

            var peak = 0f;
            var until = Time.realtimeSinceStartup + 0.35f;
            while (Time.realtimeSinceStartup < until)
            {
                peak = Mathf.Max(peak, check.localScale.x);
                yield return null;
            }

            Assert.That(peak, Is.GreaterThan(1.05f));
            Assert.That(check.localScale.x, Is.EqualTo(1f).Within(0.01f));
            Assert.That(content.alpha, Is.EqualTo(1f));
            Assert.That(frame.GetComponent<UnityEngine.UI.Image>().raycastTarget, Is.True);
            Assert.That(Time.timeScale, Is.Zero);
            motion.Close();
            motion.Close();
            Assert.That(transition.IsClosing, Is.True);
            yield return WaitFor(() => frame.localScale.y < 0.8f);
            Assert.That(content.alpha, Is.Zero);
            Assert.That(popup.activeSelf, Is.True);
            yield return WaitFor(() => !popup.activeSelf);
            Assert.That(transition.Phase, Is.EqualTo(BriefingPhase.Hidden));
            Assert.That(Time.timeScale, Is.Zero);
        }

        [UnityTest]
        public IEnumerator CloseDuringIntroAndReopenResetEffectsWithoutDuplicatingLayers()
        {
            popup.SetActive(true);
            yield return null;
            var transition = popup.GetComponent<StartBriefingPopupMotion>();
            var layerCount = popup.GetComponentsInChildren<RectTransform>(true).Length;
            motion.Close();
            Assert.That(transition.IsClosing, Is.True);
            yield return new WaitForSecondsRealtime(0.1f);
            motion.Open();
            Assert.That(motion.IsClosing, Is.False);
            Assert.That(check.localScale, Is.EqualTo(Vector3.zero));
            yield return WaitFor(() => transition.IsShown);
            Assert.That(popup.activeSelf, Is.True);
            Assert.That(popup.GetComponents<StartBriefingPopupMotion>().Length, Is.EqualTo(1));
            Assert.That(popup.GetComponentsInChildren<RectTransform>(true).Length, Is.EqualTo(layerCount));
            motion.Close();
            yield return WaitFor(() => !popup.activeSelf);
            popup.SetActive(true);
            Assert.That(check.localScale, Is.EqualTo(Vector3.zero));
            yield return WaitFor(() => transition.IsShown);
            Assert.That(Time.timeScale, Is.Zero);
        }

        private static IEnumerator WaitFor(Func<bool> condition)
        {
            var deadline = Time.realtimeSinceStartup + 5f;
            while (!condition() && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(condition(), Is.True, "Popup animation timed out.");
        }
    }
}

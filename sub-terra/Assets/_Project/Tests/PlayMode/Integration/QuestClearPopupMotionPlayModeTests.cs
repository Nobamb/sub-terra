using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using SubTerra.App.UI.Tutorial;
using SubTerra.Shared.Audio;
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

        // B-180-1: 완료음은 체크 아이콘이 커지기 시작할 때 한 번, 닫을 때는 브리핑 종료음만.
        [UnityTest]
        public IEnumerator CompletionSfxPlaysWhenCheckAppears_CloseUsesBriefingCloseOnly()
        {
            var played = new System.Collections.Generic.List<SfxId>();
            Action<SfxId, float> record = (id, pitch) => played.Add(id);
            SfxRequests.PlayRequested += record;
            try
            {
                popup.SetActive(true);
                var transition = popup.GetComponent<StartBriefingPopupMotion>();
                yield return WaitFor(() => transition.Phase == BriefingPhase.Intro);
                Assert.That(Count(played, SfxId.QuestReward), Is.Zero, "창이 막 열릴 때는 완료음 없음");

                yield return WaitFor(() => check.localScale.x > 0f);
                Assert.That(Count(played, SfxId.QuestReward), Is.EqualTo(1), "체크가 나타나기 시작할 때 한 번");
                Assert.That(check.localScale.x, Is.LessThan(0.6f), "체크 등장 시작 무렵");
                Assert.That(motion.CheckSfxPlayed, Is.True);

                yield return new WaitForSecondsRealtime(0.5f);
                Assert.That(Count(played, SfxId.QuestReward), Is.EqualTo(1), "표시 중 반복 없음");

                played.Clear();
                motion.Close();
                motion.Close();
                yield return WaitFor(() => !popup.activeSelf);
                Assert.That(Count(played, SfxId.BriefingClose), Is.EqualTo(1), "닫힘은 브리핑 종료음 한 번");
                Assert.That(Count(played, SfxId.GlitchPopupClose), Is.Zero);
                Assert.That(Count(played, SfxId.QuestReward), Is.Zero, "닫을 때 완료음 없음");

                // 다음 완료 표시에서도 같은 순서로 다시 한 번.
                played.Clear();
                popup.SetActive(true);
                yield return WaitFor(() => check.localScale.x > 0f);
                Assert.That(Count(played, SfxId.QuestReward), Is.EqualTo(1));
            }
            finally
            {
                SfxRequests.PlayRequested -= record;
            }
        }

        [UnityTest]
        public IEnumerator CloseBeforeCheckAppears_NoLateCompletionSfx()
        {
            var played = new System.Collections.Generic.List<SfxId>();
            Action<SfxId, float> record = (id, pitch) => played.Add(id);
            SfxRequests.PlayRequested += record;
            try
            {
                popup.SetActive(true);
                yield return null;
                motion.Close();
                yield return WaitFor(() => !popup.activeSelf);
                yield return new WaitForSecondsRealtime(0.3f);
                Assert.That(Count(played, SfxId.QuestReward), Is.Zero, "체크가 나오기 전에 닫으면 완료음 없음");
                Assert.That(motion.CheckSfxPlayed, Is.False);

                // 닫는 중 다시 열기: 새 표시에서 체크가 나올 때 한 번만.
                played.Clear();
                popup.SetActive(true);
                yield return null;
                motion.Close();
                yield return new WaitForSecondsRealtime(0.05f);
                motion.Open();
                yield return WaitFor(() => check.localScale.x > 0f);
                yield return new WaitForSecondsRealtime(0.4f);
                Assert.That(Count(played, SfxId.QuestReward), Is.EqualTo(1));
            }
            finally
            {
                SfxRequests.PlayRequested -= record;
            }
        }

        private static int Count(System.Collections.Generic.List<SfxId> played, SfxId id)
        {
            var count = 0;
            foreach (var value in played) if (value == id) count++;
            return count;
        }

        private static IEnumerator WaitFor(Func<bool> condition)
        {
            var deadline = Time.realtimeSinceStartup + 5f;
            while (!condition() && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(condition(), Is.True, "Popup animation timed out.");
        }
    }
}

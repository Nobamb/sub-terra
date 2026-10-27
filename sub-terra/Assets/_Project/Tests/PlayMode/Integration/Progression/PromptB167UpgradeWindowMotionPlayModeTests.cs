using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using SubTerra.App.UI.SurfaceBase;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace SubTerra.App.Tests.PlayMode
{
    /// <summary>
    /// B-167 실제 지상 기지 Scene(슬롯 0 격리 런타임)에서 업그레이드 창 자물쇠 열기·닫기 연출을 확인하고 화면을 남긴다.
    /// </summary>
    public sealed class PromptB167UpgradeWindowMotionPlayModeTests
    {
        private const string EvidenceDirectory = "Temp/b167-upgrade-evidence";

        private UiTestEnvironment env;

        [SetUp]
        public void SetUp()
        {
            Directory.CreateDirectory(EvidenceDirectory);
            UpgradeWindowMotion.ResetSessionForTests();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (env != null) env.Dispose();
            env = null;
            UpgradeWindowMotion.ResetSessionForTests();
            // 지상 Scene이 남아 뒤 검사에 끼어들지 않게 빈 Scene으로 바꾸고 나머지를 내린다(lessons 10-25).
            var clean = SceneManager.CreateScene("B167Cleanup-" + Guid.NewGuid().ToString("N"));
            SceneManager.SetActiveScene(clean);
            for (var i = SceneManager.sceneCount - 1; i >= 0; i--)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene != clean && scene.isLoaded)
                {
                    yield return SceneManager.UnloadSceneAsync(scene);
                }
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator UpgradeButton_OpensWithLockShatter_ClosesInReverse_AndReopensQuickWhilePaused()
        {
            env = new UiTestEnvironment();
            yield return env.LoadSurfaceBase(() =>
                Object.FindAnyObjectByType<SurfaceBaseBinder>()?.IsBound == true);
            var view = Object.FindAnyObjectByType<SurfaceBaseView>();
            Assert.That(view, Is.Not.Null);
            var button = view.transform.Find("SurfaceBaseContent/UpgradeButton").GetComponent<UnityEngine.UI.Button>();
            var close = view.transform.Find("SurfaceBaseContent/UpgradeModal/ProgressionPanel/CloseUpgradeButton")
                .GetComponent<UnityEngine.UI.Button>();
            var modal = view.transform.Find("SurfaceBaseContent/UpgradeModal").gameObject;
            var motion = view.UpgradeMotion;
            var log = new List<string>();
            motion.ShakeStarted += () => log.Add("Shake");
            motion.CrackStarted += () => log.Add("Crack");
            motion.Shattered += () => log.Add("Shattered");
            motion.OpenCompleted += () => log.Add("OpenCompleted");
            motion.CloseStarted += () => log.Add("CloseStarted");
            motion.FragmentsRegathered += () => log.Add("Regathered");
            motion.CloseCompleted += () => log.Add("CloseCompleted");

            yield return Capture("before-open");
            button.onClick.Invoke();
            Assert.That(motion.IsOpening, Is.True);
            Assert.That(motion.IsFullOpen, Is.True);
            yield return null;
            var fxCanvas = modal.transform.Find("UpgradeWindowLockFx").GetComponent<Canvas>();
            var cardCanvas = motion.Card.GetComponent<Canvas>();
            Assert.That(cardCanvas, Is.Not.Null);
            Assert.That(fxCanvas.sortingOrder, Is.GreaterThan(cardCanvas.sortingOrder), "자물쇠는 카드 앞에 그린다.");

            var started = Time.realtimeSinceStartup;
            foreach (var at in new[] { 0.08f, 0.3f, 0.6f, 0.72f, 0.8f, 0.9f, 1.05f })
            {
                yield return WaitUntilElapsed(started, at);
                yield return Capture("open-full-" + ((int)(at * 100f)).ToString("D3"));
            }

            // 화면 저장이 프레임을 늦추므로(한 프레임 최대 0.05초 진행) 실제 시간 상한은 넉넉히 둔다.
            yield return WaitUntil(() => motion.IsSettled, 4f, "처음 열기 끝");
            Assert.That(motion.Card.localScale, Is.EqualTo(Vector3.one));
            Assert.That(motion.Card.GetComponent<CanvasGroup>().interactable, Is.True);
            Assert.That(motion.EffectsVisible, Is.False);
            yield return Capture("open-settled");
            Assert.That(log, Is.EqualTo(new[] { "Shake", "Crack", "Shattered", "OpenCompleted" }));

            log.Clear();
            close.onClick.Invoke();
            Assert.That(view.IsUpgradeVisible, Is.False);
            started = Time.realtimeSinceStartup;
            foreach (var at in new[] { 0.06f, 0.15f, 0.23f })
            {
                yield return WaitUntilElapsed(started, at);
                yield return Capture("close-" + ((int)(at * 100f)).ToString("D3"));
            }

            yield return WaitUntil(() => !modal.activeSelf, 2f, "닫기 끝");
            Assert.That(log, Is.EqualTo(new[] { "CloseStarted", "Regathered", "CloseCompleted" }));

            // 다시 열기는 흔들림 없이 짧고, 게임이 멈춰 있어도 재생된다.
            log.Clear();
            Time.timeScale = 0f;
            button.onClick.Invoke();
            Assert.That(motion.IsFullOpen, Is.False);
            started = Time.realtimeSinceStartup;
            foreach (var at in new[] { 0.1f, 0.2f, 0.3f })
            {
                yield return WaitUntilElapsed(started, at);
                yield return Capture("reopen-" + ((int)(at * 100f)).ToString("D3"));
            }

            yield return WaitUntil(() => motion.IsSettled, 2f, "다시 열기 끝");
            Assert.That(log, Is.EqualTo(new[] { "Crack", "Shattered", "OpenCompleted" }));
            Time.timeScale = 1f;
        }

        [UnityTest]
        public IEnumerator MineUKey_OpensWithSameLockShatter_AndClosesAfterMotion()
        {
            env = new UiTestEnvironment();
            yield return env.LoadIntegration();
            var panels = Object.FindAnyObjectByType<SubTerra.App.UI.PanelToggleController>();
            Assert.That(panels, Is.Not.Null);
            var card = Object.FindAnyObjectByType<SubTerra.App.UI.Progression.ProgressionPanelView>(FindObjectsInactive.Include);
            Assert.That(card, Is.Not.Null);
            var root = card.gameObject;
            var log = new List<string>();

            yield return Capture("mine-before-open");
            panels.ToggleUpgrade();
            var motion = root.GetComponent<UpgradeWindowMotion>();
            Assert.That(motion, Is.Not.Null);
            motion.ShakeStarted += () => log.Add("Shake");
            motion.CrackStarted += () => log.Add("Crack");
            motion.Shattered += () => log.Add("Shattered");
            motion.OpenCompleted += () => log.Add("OpenCompleted");
            Assert.That(motion.IsOpening, Is.True);
            Assert.That(motion.IsFullOpen, Is.True, "광산에서 처음 열어도 처음 열기 연출이다.");
            yield return null;
            var cardCanvas = root.GetComponent<Canvas>();
            var fxCanvas = motion.EffectsRoot.GetComponent<Canvas>();
            Assert.That(motion.EffectsRoot.parent, Is.Not.SameAs(root.transform));
            Assert.That(fxCanvas.sortingOrder, Is.GreaterThan(cardCanvas.sortingOrder), "자물쇠는 카드 앞에 그린다.");

            var started = Time.realtimeSinceStartup;
            foreach (var at in new[] { 0.08f, 0.3f, 0.6f, 0.8f, 0.95f })
            {
                yield return WaitUntilElapsed(started, at);
                yield return Capture("mine-open-" + ((int)(at * 100f)).ToString("D3"));
            }

            yield return WaitUntil(() => motion.IsSettled, 4f, "광산 처음 열기 끝");
            Assert.That(root.transform.localScale, Is.EqualTo(Vector3.one));
            Assert.That(motion.EffectsVisible, Is.False);
            yield return Capture("mine-open-settled");
            Assert.That(log, Is.EqualTo(new[] { "Shake", "Crack", "Shattered", "OpenCompleted" }));

            panels.CloseUpgrade();
            Assert.That(panels.IsVisible(SubTerra.App.UI.RuntimePanelId.Upgrade), Is.False);
            Assert.That(root.activeSelf, Is.True);
            started = Time.realtimeSinceStartup;
            foreach (var at in new[] { 0.06f, 0.15f, 0.23f })
            {
                yield return WaitUntilElapsed(started, at);
                yield return Capture("mine-close-" + ((int)(at * 100f)).ToString("D3"));
            }

            yield return WaitUntil(() => !root.activeSelf, 2f, "광산 닫기 끝");
            Assert.That(motion.EffectsVisible, Is.False);
        }

        private static IEnumerator WaitUntilElapsed(float started, float seconds)
        {
            while (Time.realtimeSinceStartup - started < seconds)
            {
                yield return null;
            }
        }

        private static IEnumerator WaitUntil(Func<bool> condition, float timeout, string label)
        {
            var start = Time.realtimeSinceStartup;
            while (!condition())
            {
                if (Time.realtimeSinceStartup - start > timeout)
                {
                    Assert.Fail(label + " 시간 초과");
                }

                yield return null;
            }
        }

        private static IEnumerator Capture(string name)
        {
            yield return new WaitForEndOfFrame();
            var texture = ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(Path.Combine(EvidenceDirectory, name + ".png"), texture.EncodeToPNG());
            Object.Destroy(texture);
        }
    }
}

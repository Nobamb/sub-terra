using System.Collections;
using NUnit.Framework;
using SubTerra.App.Progression;
using SubTerra.App.UI.Progression;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace SubTerra.App.Tests.PlayMode
{
    /// <summary>
    /// prompt-B 155: 업그레이드 창이 게임을 멈춘 상태(timeScale 0)에서도 해금 연출이 비스케일 시간으로 끝나고,
    /// 해금 창이 닫힌 뒤에만 시작하며, 도중에 창이 꺼지면 끝 상태로 맞춰지는지 확인한다.
    /// </summary>
    public sealed class PromptB155DeepZoneThumbnailPlayModeTests
    {
        private GameObject canvas;
        private DeepZoneDisplaySettings settings;
        private float previousTimeScale;

        [SetUp]
        public void SetUp()
        {
            previousTimeScale = Time.timeScale;
            canvas = new GameObject("B155Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            settings = ScriptableObject.CreateInstance<DeepZoneDisplaySettings>();
            settings.EditorSet(new[]
            {
                new DeepZoneDisplayLevel(36, 40, true, string.Empty),
                new DeepZoneDisplayLevel(41, 75, false, "준비 중")
            });
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = previousTimeScale;
            Object.Destroy(canvas);
            Object.Destroy(settings);
        }

        private DeepZoneThumbnailStrip NewStrip()
        {
            var holder = new GameObject("Holder", typeof(RectTransform));
            holder.transform.SetParent(canvas.transform, false);
            ((RectTransform)holder.transform).sizeDelta = new Vector2(700f, 110f);
            return DeepZoneThumbnailStrip.Create((RectTransform)holder.transform, null);
        }

        [UnityTest]
        public IEnumerator Reveal_WaitsForPopup_PlaysWithUnscaledTime_Once()
        {
            Time.timeScale = 0f;
            var popupOpen = true;
            var strip = NewStrip();
            strip.RevealBlocked = () => popupOpen;
            strip.Apply(settings, new ZoneAccessResult(false, false, string.Empty));
            strip.Apply(settings, new ZoneAccessResult(true, true, string.Empty));

            yield return null;
            yield return null;
            Assert.That(strip.IsRevealing, Is.False, "해금 창이 열려 있는 동안은 재생하지 않는다");
            Assert.That(strip.HasPendingReveal, Is.True);

            popupOpen = false;
            yield return null;
            Assert.That(strip.IsRevealing, Is.True);

            var timeout = Time.realtimeSinceStartup + 3f;
            while (strip.IsRevealing && Time.realtimeSinceStartup < timeout)
            {
                yield return null;
            }

            Assert.That(strip.IsRevealing, Is.False, "timeScale 0에서도 약 1초 안에 끝나야 한다");
            var state = strip.GetCellState(0);
            Assert.That(state.Unlocked, Is.True);
            Assert.That(state.LockVisible, Is.False);
            Assert.That(strip.HasPendingReveal, Is.False);

            strip.Apply(settings, new ZoneAccessResult(true, true, string.Empty));
            yield return null;
            Assert.That(strip.IsRevealing, Is.False, "같은 해금으로는 다시 재생하지 않는다");
        }

        [UnityTest]
        public IEnumerator DisablingMidReveal_SnapsToUnlockedEndState()
        {
            var strip = NewStrip();
            strip.Apply(settings, new ZoneAccessResult(false, false, string.Empty));
            strip.Apply(settings, new ZoneAccessResult(true, true, string.Empty));
            yield return null;
            Assert.That(strip.IsRevealing, Is.True);
            yield return null;

            strip.gameObject.SetActive(false);
            Assert.That(strip.IsRevealing, Is.False);
            var state = strip.GetCellState(0);
            Assert.That(state.Unlocked, Is.True);
            Assert.That(state.LockVisible, Is.False);
            Assert.That(state.Condition, Is.EqualTo("해금됨"));
        }
    }
}

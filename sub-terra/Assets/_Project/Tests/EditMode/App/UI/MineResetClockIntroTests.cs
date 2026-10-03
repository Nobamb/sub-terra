using NUnit.Framework;
using SubTerra.App.UI.HUD;
using SubTerra.App.UI.SurfaceBase;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.Tests.UI
{
    /// <summary>prompt-B 126: 3시간 만료 알림 직전 시계 연출의 시간표·구조·입력 범위.</summary>
    public sealed class MineResetClockIntroTests
    {
        private GameObject host;
        private MineResetClockOverlay overlay;
        private Transform popup;

        [SetUp]
        public void SetUp()
        {
            host = new GameObject("ClockIntroTestHost", typeof(RectTransform));
            overlay = MineResetClockOverlay.Create(host.transform);
            popup = overlay.transform.Find("TimedResetPopup");
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(host);
        }

        [Test]
        public void Timeline_StartsAtZeroScale_PeaksAtOnePointTwo_AndSettlesAtOne()
        {
            var start = MineResetClockIntroTimeline.Evaluate(0f);
            Assert.That(start.Scale, Is.Zero);

            var peak = MineResetClockIntroTimeline.Evaluate(MineResetClockIntroTimeline.PopEnd);
            Assert.That(peak.Scale, Is.EqualTo(1.2f).Within(1e-4f));

            var settled = MineResetClockIntroTimeline.Evaluate(MineResetClockIntroTimeline.SettleEnd);
            Assert.That(settled.Scale, Is.EqualTo(1f).Within(1e-4f));
        }

        [Test]
        public void Timeline_HandTurnsOnceClockwise_ThenWholeClockShakes_ThenFades()
        {
            Assert.That(MineResetClockIntroTimeline.Evaluate(0.05f).HandAngle, Is.Zero);
            Assert.That(MineResetClockIntroTimeline.Evaluate(MineResetClockIntroTimeline.HandEnd).HandAngle,
                Is.EqualTo(-360f).Within(1e-3f));

            var previous = 0f;
            for (var t = MineResetClockIntroTimeline.HandStart; t <= MineResetClockIntroTimeline.HandEnd; t += 0.02f)
            {
                var angle = MineResetClockIntroTimeline.Evaluate(t).HandAngle;
                Assert.That(angle, Is.LessThanOrEqualTo(previous + 1e-4f), "시계 방향(음수 방향)으로만 돈다");
                previous = angle;
            }

            // 회전 중에는 시계가 흔들리지 않고, 회전이 끝난 뒤에만 좌우로 흔들린다.
            Assert.That(MineResetClockIntroTimeline.Evaluate(0.5f).ShakeX, Is.Zero);
            var sign = 0;
            var flips = 0;
            for (var t = MineResetClockIntroTimeline.HandEnd; t <= MineResetClockIntroTimeline.ShakeEnd; t += 0.005f)
            {
                var x = MineResetClockIntroTimeline.Evaluate(t).ShakeX;
                var s = x > 0.5f ? 1 : (x < -0.5f ? -1 : 0);
                if (s != 0 && s != sign)
                {
                    if (sign != 0) flips++;
                    sign = s;
                }
            }

            Assert.That(flips, Is.InRange(3, 5), "좌우로 두세 번 흔들린다");
            Assert.That(MineResetClockIntroTimeline.Evaluate(MineResetClockIntroTimeline.ShakeEnd).Alpha, Is.EqualTo(1f).Within(1e-4f));
            Assert.That(MineResetClockIntroTimeline.Evaluate(MineResetClockIntroTimeline.Duration).Alpha, Is.Zero);
        }

        [Test]
        public void Timeline_DurationIsAboutOneToOnePointThreeSeconds()
        {
            Assert.That(MineResetClockIntroTimeline.Duration, Is.InRange(1f, 1.3f));
        }

        [Test]
        public void Clock_HasRingAndSingleHandOnly_WithSharedCenter()
        {
            var clock = popup.Find("ClockIntro/Clock");
            Assert.That(clock, Is.Not.Null);
            var ring = (RectTransform)clock.Find("ClockRing");
            var hand = (RectTransform)clock.Find("ClockHand");
            Assert.That(ring, Is.Not.Null);
            Assert.That(hand, Is.Not.Null);
            // 시침 회전축(피벗)과 테두리 중심이 모두 시계 정중앙이다.
            Assert.That(hand.pivot, Is.EqualTo(new Vector2(0.5f, 0.5f)));
            Assert.That(hand.anchoredPosition, Is.EqualTo(ring.anchoredPosition));
            Assert.That(hand.sizeDelta, Is.EqualTo(ring.sizeDelta));
            Assert.That(clock.Find("ClockMinuteHand"), Is.Null);
            Assert.That(clock.Find("ClockSecondHand"), Is.Null);
            foreach (var text in clock.GetComponentsInChildren<TMPro.TMP_Text>(true))
            {
                Assert.Fail("시계에는 숫자·글자가 없어야 한다: " + text.name);
            }
        }

        [Test]
        public void Apply_RotatesOnlyTheHand_AndShakesTheWholeClock()
        {
            var intro = popup.Find("ClockIntro").GetComponent<MineResetClockIntro>();
            var clock = (RectTransform)popup.Find("ClockIntro/Clock");
            var hand = (RectTransform)popup.Find("ClockIntro/Clock/ClockHand");
            var ring = (RectTransform)popup.Find("ClockIntro/Clock/ClockRing");

            intro.Apply(0.5f);
            Assert.That(clock.localRotation, Is.EqualTo(Quaternion.identity));
            Assert.That(ring.localRotation, Is.EqualTo(Quaternion.identity));
            Assert.That(hand.localEulerAngles.z, Is.Not.EqualTo(0f).Within(1f));

            intro.Apply(0.84f);
            Assert.That(clock.anchoredPosition.x, Is.Not.Zero);
            Assert.That(clock.localRotation, Is.EqualTo(Quaternion.identity));
        }

        [Test]
        public void Clock_RaycastsAreOff_SoOnlyTheBlockerStopsClicks()
        {
            foreach (var graphic in popup.Find("ClockIntro").GetComponentsInChildren<Graphic>(true))
            {
                Assert.That(graphic.raycastTarget, Is.False, graphic.name);
            }

            Assert.That(popup.GetComponent<Image>().raycastTarget, Is.True);
            Assert.That(popup.GetComponent<Image>().canvasRenderer.cullTransparentMesh, Is.False,
                "알파가 0이어도 딤 블로커가 클릭을 막는다");
        }

        [Test]
        public void EditMode_ShowSkipsClockAndOpensCard_RepeatedShowKeepsSingleBlocker()
        {
            overlay.ShowTimedResetPopup(true);
            overlay.ShowTimedResetPopup(false);
            overlay.ShowTimedResetPopup(true);

            Assert.That(popup.gameObject.activeSelf, Is.True);
            Assert.That(popup.Find("Card").gameObject.activeSelf, Is.True);
            Assert.That(popup.Find("ClockIntro").gameObject.activeSelf, Is.False);
            Assert.That(popup.GetComponents<MineResetPopupMotion>(), Has.Length.EqualTo(1));
            Assert.That(popup.GetComponents<MineResetClockIntro>(), Is.Empty);

            Assert.That(overlay.TryClosePopup(popup.GetComponent<Canvas>()), Is.True);
            Assert.That(popup.gameObject.activeSelf, Is.False);
            Assert.That(popup.Find("Card").gameObject.activeSelf, Is.True, "닫은 뒤에도 다음 표시에 쓸 카드가 켜진 채 남는다");
        }

        [Test]
        public void ClockIntro_PlayFinishesOnce_AndStopNeverInvokesCallback()
        {
            var intro = popup.Find("ClockIntro").GetComponent<MineResetClockIntro>();
            popup.gameObject.SetActive(true);
            var calls = 0;
            intro.Play(() => calls++);
            Assert.That(intro.IsPlaying, Is.True);
            intro.Stop();
            Assert.That(intro.IsPlaying, Is.False);
            Assert.That(calls, Is.Zero);
            Assert.That(intro.gameObject.activeSelf, Is.False);
        }
    }
}

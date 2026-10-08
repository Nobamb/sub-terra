using System;
using System.Collections.Generic;
using NUnit.Framework;
using SubTerra.App.Core.Data;
using SubTerra.App.Integration;
using SubTerra.App.Inventory;
using SubTerra.App.Outpost;
using SubTerra.App.State;
using SubTerra.App.UI.HUD;
using SubTerra.App.UI.Outpost;
using SubTerra.Shared;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.Tests.Outpost
{
    public sealed class FacilityCooldownPopupTests
    {
        [TestCase(197, "03:17")]
        [TestCase(1, "00:01")]
        [TestCase(300, "05:00")]
        [TestCase(59, "00:59")]
        [TestCase(0, "00:00")]
        [TestCase(-5, "00:00")]
        [TestCase(3600, "1:00:00")]
        [TestCase(3661, "1:01:01")]
        public void Format_UsesMinuteSecondOrHourForm(int seconds, string expected)
        {
            Assert.That(FacilityCooldownFormat.Format(seconds), Is.EqualTo(expected));
        }

        [TestCase(0.2, 1)]
        [TestCase(1.0, 1)]
        [TestCase(1.5, 2)]
        [TestCase(0.0, 0)]
        [TestCase(-3.0, 0)]
        [TestCase(300.0, 300)]
        public void GetCooldownDisplaySeconds_RoundsUpAndKeepsOneSecondWhileRemaining(double remaining, int expected)
        {
            Assert.That(OutpostService.GetCooldownDisplaySeconds(remaining), Is.EqualTo(expected));
        }

        [TestCase(0.2, "충전기 재사용까지 1초 남았습니다.")]
        [TestCase(59.4, "충전기 재사용까지 1분 남았습니다.")]
        [TestCase(300d, "충전기 재사용까지 5분 남았습니다.")]
        public void FormatFacilityCooldownMessage_KeepsExistingWording(double remaining, string expected)
        {
            Assert.That(
                OutpostService.FormatFacilityCooldownMessage(DataIds.Buildings.ChargerBasic, remaining),
                Is.EqualTo(expected));
        }

        [Test]
        public void Gather_IsZeroAboveThirtySeconds_AndRisesToOneAtZero()
        {
            Assert.That(FacilityCooldownPopupTimeline.Gather01(31d), Is.EqualTo(0f));
            Assert.That(FacilityCooldownPopupTimeline.Gather01(30d), Is.EqualTo(0f).Within(0.0001f));
            Assert.That(FacilityCooldownPopupTimeline.Gather01(29d), Is.GreaterThan(0f));
            Assert.That(FacilityCooldownPopupTimeline.Gather01(6d), Is.GreaterThan(FacilityCooldownPopupTimeline.Gather01(12d)));
            Assert.That(FacilityCooldownPopupTimeline.Gather01(0d), Is.EqualTo(1f));
        }

        [Test]
        public void FinalWindow_AndParticleSpeed_ChangeAtFiveSeconds()
        {
            Assert.That(FacilityCooldownPopupTimeline.IsFinalWindow(6d), Is.False);
            Assert.That(FacilityCooldownPopupTimeline.IsFinalWindow(5d), Is.True);
            Assert.That(FacilityCooldownPopupTimeline.IsFinalWindow(1d), Is.True);
            Assert.That(FacilityCooldownPopupTimeline.IsFinalWindow(0d), Is.False);
            Assert.That(FacilityCooldownPopupTimeline.ParticleSpeed(6d), Is.EqualTo(1f));
            Assert.That(FacilityCooldownPopupTimeline.ParticleSpeed(5d), Is.EqualTo(FacilityCooldownPopupTimeline.FinalParticleSpeedScale));
        }

        [Test]
        public void BlueTint_IsCappedAtThirtyFivePercent()
        {
            for (var remaining = 30d; remaining >= 0d; remaining -= 0.5d)
            {
                Assert.That(FacilityCooldownPopupTimeline.BlueTint01(remaining), Is.LessThanOrEqualTo(0.35f + 0.0001f));
            }
        }

        [Test]
        public void OuterGlow_StartsAtThirtySeconds_ReachesPartialAtFive_AndFullAtZero()
        {
            Assert.That(FacilityCooldownPopupTimeline.OuterGlow01(31d), Is.EqualTo(0f));
            Assert.That(FacilityCooldownPopupTimeline.OuterGlow01(30d), Is.EqualTo(0f));
            Assert.That(FacilityCooldownPopupTimeline.OuterGlow01(29d), Is.GreaterThan(0f));
            Assert.That(FacilityCooldownPopupTimeline.OuterGlow01(5d),
                Is.EqualTo(FacilityCooldownPopupTimeline.OuterGlowAtFinalWindow).Within(0.0001f));
            Assert.That(FacilityCooldownPopupTimeline.OuterGlow01(0d), Is.EqualTo(1f));

            var previous = 0f;
            for (var remaining = 30d; remaining >= 0d; remaining -= 0.25d)
            {
                var level = FacilityCooldownPopupTimeline.OuterGlow01(remaining);
                Assert.That(level, Is.GreaterThanOrEqualTo(previous - 0.0001f), "남은 " + remaining);
                previous = level;
            }
        }

        [Test]
        public void InnerGlow_OnlyInFinalFiveSeconds_AndStaysFaint()
        {
            Assert.That(FacilityCooldownPopupTimeline.InnerGlow01(6d), Is.EqualTo(0f));
            Assert.That(FacilityCooldownPopupTimeline.InnerGlow01(5d), Is.EqualTo(0f).Within(0.0001f));
            Assert.That(FacilityCooldownPopupTimeline.InnerGlow01(2.5d), Is.GreaterThan(0f));
            for (var remaining = 5d; remaining >= 0d; remaining -= 0.25d)
            {
                Assert.That(FacilityCooldownPopupTimeline.InnerGlow01(remaining),
                    Is.LessThanOrEqualTo(FacilityCooldownPopupTimeline.InnerGlowMax + 0.0001f));
            }

            Assert.That(FacilityCooldownPopupTimeline.InnerGlowMax, Is.LessThan(1f));
        }

        [Test]
        public void Flash_FillsEdgesFirstThenCenter()
        {
            Assert.That(FacilityCooldownPopupTimeline.FlashEdge01(0f), Is.EqualTo(0f));
            Assert.That(FacilityCooldownPopupTimeline.FlashCore01(0f), Is.EqualTo(0f));
            Assert.That(FacilityCooldownPopupTimeline.FlashEdge01(0.3f),
                Is.GreaterThan(FacilityCooldownPopupTimeline.FlashCore01(0.3f)));
            Assert.That(FacilityCooldownPopupTimeline.FlashEdge01(1f), Is.EqualTo(1f));
            Assert.That(FacilityCooldownPopupTimeline.FlashCore01(1f), Is.EqualTo(1f));
        }

        [Test]
        public void View_Completion_FlashesThenTurnsOffTv_AndKeepsFlashDuringCollapse()
        {
            var fixture = CreateViewFixture(out var remaining);
            remaining = 1.0d;
            fixture.View.ShowFacilityCooldown(DataIds.Buildings.ChargerBasic, "charger.a", id => remaining);
            for (var i = 0; i < 12; i++)
            {
                fixture.View.Tick(0.05f);
            }

            remaining = -1d;
            fixture.View.Tick(0.05f);
            Assert.That(fixture.View.IsCompleting, Is.True);
            var midFlash = ImageAlpha(fixture.View, "FlashCore");
            for (var i = 0; i < 3; i++)
            {
                fixture.View.Tick(0.05f);
            }

            Assert.That(ImageAlpha(fixture.View, "FlashCore"), Is.GreaterThan(midFlash));

            // 섬광이 끝나면 TV 꺼짐이 시작되고, 접히는 동안 섬광은 유지된다.
            for (var i = 0; i < 3; i++)
            {
                fixture.View.Tick(0.05f);
            }

            Assert.That(fixture.View.IsCompleting, Is.False);
            Assert.That(fixture.View.IsOpen, Is.False);
            Assert.That(fixture.View.gameObject.activeSelf, Is.True);
            Assert.That(ImageAlpha(fixture.View, "FlashCore"), Is.GreaterThan(0.5f));

            for (var i = 0; i < 20; i++)
            {
                fixture.View.Tick(0.05f);
            }

            Assert.That(fixture.View.gameObject.activeSelf, Is.False);
            Assert.That(ImageAlpha(fixture.View, "FlashCore"), Is.EqualTo(0f));
            Assert.That(ImageAlpha(fixture.View, "OuterGlow"), Is.EqualTo(0f));
            fixture.Dispose();
        }

        [Test]
        public void OuterGlow_HasNoStallAroundFiveSeconds_AndRisesInSmallSteps()
        {
            // 5초 지점에서 기울기가 끊기지 않아야 한다(밝아지다 멈췄다 다시 오르는 느낌 방지).
            var before = FacilityCooldownPopupTimeline.OuterGlow01(5.0d) - FacilityCooldownPopupTimeline.OuterGlow01(5.5d);
            var after = FacilityCooldownPopupTimeline.OuterGlow01(4.5d) - FacilityCooldownPopupTimeline.OuterGlow01(5.0d);
            Assert.That(before, Is.GreaterThan(0f));
            Assert.That(after / before, Is.InRange(0.5f, 4f));

            // 한 프레임(0.05초)에 변하는 폭은 항상 작다.
            for (var remaining = 30d; remaining > 0d; remaining -= 0.05d)
            {
                var step = FacilityCooldownPopupTimeline.OuterGlow01(remaining - 0.05d)
                    - FacilityCooldownPopupTimeline.OuterGlow01(remaining);
                Assert.That(step, Is.LessThan(0.02f), "남은 " + remaining);
            }
        }

        [Test]
        public void InnerEdgeGlow_StartsAtThirtySeconds_WithSmallerAreaThanAtFiveSeconds()
        {
            Assert.That(FacilityCooldownPopupTimeline.InnerEdgeGlow01(31d), Is.EqualTo(0f));
            Assert.That(FacilityCooldownPopupTimeline.InnerEdgeGlow01(29d), Is.GreaterThan(0f));
            Assert.That(FacilityCooldownPopupTimeline.InnerEdgeGlow01(0d),
                Is.EqualTo(FacilityCooldownPopupTimeline.InnerEdgeMax));
            Assert.That(FacilityCooldownPopupTimeline.InnerEdgeMax, Is.LessThan(1f));

            Assert.That(FacilityCooldownPopupTimeline.InnerEdgeSize01(30d), Is.EqualTo(0f));
            Assert.That(FacilityCooldownPopupTimeline.InnerEdgeSize01(5d),
                Is.EqualTo(FacilityCooldownPopupTimeline.InnerEdgeSizeAtFinalWindow).Within(0.0001f));
            Assert.That(FacilityCooldownPopupTimeline.InnerEdgeSize01(29d),
                Is.LessThan(FacilityCooldownPopupTimeline.InnerEdgeSize01(5d) * 0.2f));
            Assert.That(FacilityCooldownPopupTimeline.InnerEdgeSize01(0d), Is.EqualTo(1f));

            var level = 0f;
            var size = 0f;
            for (var remaining = 30d; remaining >= 0d; remaining -= 0.25d)
            {
                var nextLevel = FacilityCooldownPopupTimeline.InnerEdgeGlow01(remaining);
                var nextSize = FacilityCooldownPopupTimeline.InnerEdgeSize01(remaining);
                Assert.That(nextLevel, Is.GreaterThanOrEqualTo(level - 0.0001f), "level " + remaining);
                Assert.That(nextSize, Is.GreaterThanOrEqualTo(size - 0.0001f), "size " + remaining);
                level = nextLevel;
                size = nextSize;
            }
        }

        [Test]
        public void View_OuterGlowAndInnerEdgeAppearAtThirtySecondsWhileWideInnerGlowWaitsForFinalWindow()
        {
            var fixture = CreateViewFixture(out var remaining);
            remaining = 20d;
            fixture.View.ShowFacilityCooldown(DataIds.Buildings.ChargerBasic, "charger.a", id => remaining);
            for (var i = 0; i < 10; i++)
            {
                fixture.View.Tick(0.05f);
            }

            Assert.That(ImageAlpha(fixture.View, "OuterGlow"), Is.GreaterThan(0f));
            Assert.That(ImageAlpha(fixture.View, "MoteOut0"), Is.GreaterThanOrEqualTo(0f));
            Assert.That(ImageAlpha(fixture.View, "InnerEdgeGlow"), Is.GreaterThan(0f));
            Assert.That(ImageAlpha(fixture.View, "InnerGlow"), Is.EqualTo(0f));
            Assert.That(ImageAlpha(fixture.View, "MoteIn0"), Is.EqualTo(0f));

            var outerBefore = ImageAlpha(fixture.View, "OuterGlow");
            var edgeBefore = ImageAlpha(fixture.View, "InnerEdgeGlow");
            var edgeThicknessBefore = FindImage(fixture.View, "InnerEdgeGlow").pixelsPerUnitMultiplier;
            remaining = 2d;
            for (var i = 0; i < 30; i++)
            {
                fixture.View.Tick(0.05f);
            }

            Assert.That(ImageAlpha(fixture.View, "OuterGlow"), Is.GreaterThan(outerBefore));
            Assert.That(ImageAlpha(fixture.View, "InnerEdgeGlow"), Is.GreaterThan(edgeBefore));
            // 배율이 작을수록 띠가 두껍다. 5초 근처에는 20초보다 넓은 영역이 밝다.
            Assert.That(FindImage(fixture.View, "InnerEdgeGlow").pixelsPerUnitMultiplier,
                Is.LessThan(edgeThicknessBefore));
            Assert.That(ImageAlpha(fixture.View, "InnerGlow"), Is.GreaterThan(0f));
            Assert.That(ImageAlpha(fixture.View, "InnerGlow"), Is.LessThan(0.35f));
            fixture.Dispose();
        }

        [Test]
        public void View_InnerEdgeGlow_IsInsideWindowMaskAndHasNoCenterFill()
        {
            var fixture = CreateViewFixture(out _);
            var window = fixture.View.transform.Find("Stage/Window");
            var edge = FindImage(fixture.View, "InnerEdgeGlow");
            Assert.That(edge, Is.Not.Null);
            Assert.That(edge.transform.IsChildOf(window), Is.True);
            Assert.That(edge.fillCenter, Is.False);
            Assert.That(edge.type, Is.EqualTo(Image.Type.Sliced));
            fixture.Dispose();
        }

        [Test]
        public void View_Timer_UsesSevenSegmentDigitsWithoutText()
        {
            var fixture = CreateViewFixture(out var remaining);
            remaining = 197d;
            fixture.View.ShowFacilityCooldown(DataIds.Buildings.ChargerBasic, "charger.a", id => remaining);

            var digits = fixture.View.transform.Find("Stage/Window/Content/Readout/Digits");
            Assert.That(digits, Is.Not.Null);
            Assert.That(fixture.View.transform.Find("Stage/Window/Content/Readout/Time"), Is.Null);

            // 03:17 → 0, 3, 1, 7 네 칸과 콜론 하나.
            var expected = new[] { "0", "3", "1", "7" };
            var digitMasks = new[] { SevenSegmentGlyph.GetMask(0), SevenSegmentGlyph.GetMask(3),
                SevenSegmentGlyph.GetMask(1), SevenSegmentGlyph.GetMask(7) };
            for (var i = 0; i < expected.Length; i++)
            {
                var slot = digits.Find("Digit" + i);
                Assert.That(slot.gameObject.activeSelf, Is.True, "Digit" + i);
                for (var j = 0; j < 7; j++)
                {
                    var lit = (digitMasks[i] & (1 << j)) != 0;
                    var alpha = slot.Find("Segment" + j).GetComponent<Image>().color.a;
                    Assert.That(alpha, lit ? Is.EqualTo(1f) : Is.LessThan(0.2f), expected[i] + " segment " + j);
                }
            }

            Assert.That(digits.Find("Digit4").gameObject.activeSelf, Is.False);
            Assert.That(digits.Find("Colon0").gameObject.activeSelf, Is.True);
            Assert.That(digits.Find("Colon1").gameObject.activeSelf, Is.False);
            fixture.Dispose();
        }

        [Test]
        public void View_Timer_ShowsHourFormAndStaysCentered()
        {
            var fixture = CreateViewFixture(out var remaining);
            remaining = 3661d;
            fixture.View.ShowFacilityCooldown(DataIds.Buildings.ChargerBasic, "charger.a", id => remaining);

            Assert.That(fixture.View.TimeText, Is.EqualTo("1:01:01"));
            var digits = fixture.View.transform.Find("Stage/Window/Content/Readout/Digits");
            var minX = float.MaxValue;
            var maxX = float.MinValue;
            foreach (Transform slot in digits)
            {
                if (!slot.gameObject.activeSelf)
                {
                    continue;
                }

                var x = ((RectTransform)slot).anchoredPosition.x;
                minX = Mathf.Min(minX, x);
                maxX = Mathf.Max(maxX, x);
            }

            Assert.That(minX + maxX, Is.EqualTo(0f).Within(0.01f));
            Assert.That(digits.Find("Colon1").gameObject.activeSelf, Is.True);
            fixture.Dispose();
        }

        [Test]
        public void View_OuterGlowAndParticlesLiveOutsideTheClippedWindow()
        {
            var fixture = CreateViewFixture(out _);
            var window = fixture.View.transform.Find("Stage/Window");
            Assert.That(window, Is.Not.Null);
            foreach (var name in new[] { "OuterGlow", "MoteOut0", "MoteOut19" })
            {
                var image = FindImage(fixture.View, name);
                Assert.That(image, Is.Not.Null, name);
                Assert.That(image.transform.IsChildOf(window), Is.False, name + "가 창 마스크 안에 있음");
            }

            Assert.That(FindImage(fixture.View, "InnerGlow").transform.IsChildOf(window), Is.True);
            fixture.Dispose();
        }

        private static Image FindImage(FacilityCooldownPopupView view, string name)
        {
            foreach (var image in view.GetComponentsInChildren<Image>(true))
            {
                if (image.name == name)
                {
                    return image;
                }
            }

            return null;
        }

        private static float ImageAlpha(FacilityCooldownPopupView view, string name)
        {
            var image = FindImage(view, name);
            Assert.That(image, Is.Not.Null, name);
            return image.color.a;
        }

        [Test]
        public void TimerReuse_ResetsOrReducesClinicAndChargerCooldowns()
        {
            var session = new DeveloperDebugCommandSession(DeveloperDebugCommandRegistry.CreateIsolated());
            var state = GameState.CreateNew();
            var charger = DataIds.Buildings.ChargerBasic + "-0001";
            var clinic = DataIds.Buildings.ClinicBasic + "-0002";
            var other = "storage-0003";
            var context = new DeveloperDebugCommandContext(state, null, null, null);

            void Seed()
            {
                state.Outpost.TryRestore(
                    new List<OutpostStorageEntryState>(),
                    new List<string>(),
                    string.Empty,
                    0,
                    0,
                    new List<FacilityCooldownState>
                    {
                        new FacilityCooldownState(charger, 300d),
                        new FacilityCooldownState(clinic, 300d),
                        new FacilityCooldownState(other, 300d)
                    });
            }

            double Remaining(string id) =>
                state.Outpost.TryGetFacilityCooldownRemaining(id, out var value) ? value : 0d;

            Seed();
            session.Execute("timer reuse hp", context);
            Assert.That(Remaining(clinic), Is.EqualTo(0d));
            Assert.That(Remaining(charger), Is.EqualTo(300d));

            Seed();
            session.Execute("timer reuse energy", context);
            Assert.That(Remaining(charger), Is.EqualTo(0d));
            Assert.That(Remaining(clinic), Is.EqualTo(300d));

            Seed();
            session.Execute("timer reuse", context);
            Assert.That(Remaining(charger), Is.EqualTo(0d));
            Assert.That(Remaining(clinic), Is.EqualTo(0d));
            Assert.That(Remaining(other), Is.EqualTo(300d));

            Seed();
            session.Execute("timer reuse 3m", context);
            Assert.That(Remaining(charger), Is.EqualTo(120d).Within(0.0001d));
            Assert.That(Remaining(clinic), Is.EqualTo(120d).Within(0.0001d));
            Assert.That(Remaining(other), Is.EqualTo(300d));

            Seed();
            session.Execute("timer reuse hp 10s", context);
            Assert.That(Remaining(clinic), Is.EqualTo(290d).Within(0.0001d));
            Assert.That(Remaining(charger), Is.EqualTo(300d));

            session.Execute("timer reuse energy 10s", context);
            Assert.That(Remaining(charger), Is.EqualTo(290d).Within(0.0001d));

            session.Execute("timer reuse energy 10 s", context);
            Assert.That(Remaining(charger), Is.EqualTo(280d).Within(0.0001d));

            session.Execute("timer reuse hp 10m", context);
            Assert.That(Remaining(clinic), Is.EqualTo(0d));
        }

        [Test]
        public void TimerReuse_BadArguments_AreRejectedAndMineTimerStaysDefault()
        {
            var session = new DeveloperDebugCommandSession(DeveloperDebugCommandRegistry.CreateIsolated());
            var state = GameState.CreateNew();
            var context = new DeveloperDebugCommandContext(state, null, null, null);

            Assert.That(session.Execute("timer reuse hp energy", context), Does.Contain("인자 오류"));
            Assert.That(session.Execute("timer reuse 0s", context), Does.Contain("인자 오류"));
            Assert.That(session.Execute("timer reuse -5s", context), Does.Contain("인자 오류"));
            Assert.That(session.Execute("timer reuse hp banana", context), Does.Contain("인자 오류"));
            Assert.That(session.Execute("timer reuse", context), Does.Contain("대기 중인 시설 없음"));
            Assert.That(session.Execute("timer", context), Does.Contain("timer 인자 오류"));
        }

        [Test]
        public void Pose_OnFoldsFromLineToFullFrame_AndOffUsesSameCurveInReverse()
        {
            var start = FacilityCooldownPopupTimeline.Evaluate(0f);
            Assert.That(start.WindowWidth01, Is.EqualTo(0f));
            Assert.That(start.ReadoutAlpha, Is.EqualTo(0f));

            var middle = FacilityCooldownPopupTimeline.Evaluate(0.6f);
            Assert.That(middle.WindowWidth01, Is.EqualTo(1f));
            Assert.That(middle.WindowHeight01, Is.GreaterThan(0.2f));
            Assert.That(middle.WindowHeight01, Is.LessThan(1f));

            var full = FacilityCooldownPopupTimeline.Evaluate(1f);
            Assert.That(full.WindowHeight01, Is.EqualTo(1f));
            Assert.That(full.ReadoutAlpha, Is.EqualTo(1f));
        }

        [Test]
        public void Advance_RespectsOnAndOffDurations()
        {
            var on = FacilityCooldownPopupTimeline.Advance(0f, true, FacilityCooldownPopupTimeline.OnDuration);
            Assert.That(on, Is.EqualTo(1f).Within(0.0001f));
            var off = FacilityCooldownPopupTimeline.Advance(1f, false, FacilityCooldownPopupTimeline.OffDuration);
            Assert.That(off, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(FacilityCooldownPopupTimeline.Advance(0f, false, 1f), Is.EqualTo(0f));
        }

        [Test]
        public void Presenter_CooldownTap_ShowsPopupOnceAndKeepsServicePanelClosed()
        {
            var system = CreateChargeSystem();
            var view = new CooldownRecordingView();
            var presenter = new OutpostPanelPresenter(view);
            system.State.SetEnergy(10, 100);
            system.Service.ApplyRuntimeStatus(ChargerStatus("charger.a"));
            presenter.Bind(system.Service);

            presenter.ToggleInteractionPanel();
            presenter.DismissInteractionPanel();
            system.State.SetCurrentEnergy(10);
            presenter.ToggleInteractionPanel();

            Assert.That(system.State.Player.Energy, Is.EqualTo(10));
            Assert.That(view.CooldownShowCount, Is.EqualTo(1));
            Assert.That(view.LastCooldownBuildingId, Is.EqualTo(DataIds.Buildings.ChargerBasic));
            Assert.That(view.LastCooldownInstanceId, Is.EqualTo("charger.a"));
            Assert.That(view.LastRemaining, Is.EqualTo(OutpostService.FacilityUseCooldownSeconds).Within(0.0001d));
            Assert.That(view.Visible, Is.False);
            Assert.That(view.TemporaryMessage, Is.Null.Or.Empty);
        }

        [Test]
        public void Presenter_RepeatedTapOnSameTarget_KeepsSingleTargetAndDoesNotRecharge()
        {
            var system = CreateChargeSystem();
            var view = new CooldownRecordingView();
            var presenter = new OutpostPanelPresenter(view);
            system.State.SetEnergy(10, 100);
            system.Service.ApplyRuntimeStatus(ChargerStatus("charger.a"));
            presenter.Bind(system.Service);
            presenter.ToggleInteractionPanel();
            presenter.DismissInteractionPanel();

            presenter.ToggleInteractionPanel();
            var showsAfterFirst = view.CooldownShowCount;
            var hidesAfterFirst = view.HideCount;

            presenter.ToggleInteractionPanel();

            Assert.That(view.CooldownShowCount, Is.EqualTo(showsAfterFirst + 1));
            Assert.That(view.LastCooldownInstanceId, Is.EqualTo("charger.a"));
            Assert.That(view.HideCount, Is.EqualTo(hidesAfterFirst));
        }

        [Test]
        public void Presenter_LeavingRange_HidesCooldownPopup()
        {
            var system = CreateChargeSystem();
            var view = new CooldownRecordingView();
            var presenter = new OutpostPanelPresenter(view);
            system.State.SetEnergy(10, 100);
            system.Service.ApplyRuntimeStatus(ChargerStatus("charger.a"));
            presenter.Bind(system.Service);
            presenter.ToggleInteractionPanel();
            presenter.DismissInteractionPanel();
            presenter.ToggleInteractionPanel();

            system.Service.ApplyRuntimeStatus(OutOfRangeStatus());

            Assert.That(view.HideCount, Is.GreaterThanOrEqualTo(1));
        }

        [Test]
        public void Presenter_TargetChange_HidesPreviousTarget()
        {
            var system = CreateChargeSystem();
            var view = new CooldownRecordingView();
            var presenter = new OutpostPanelPresenter(view);
            system.State.SetEnergy(10, 100);
            system.Service.ApplyRuntimeStatus(ChargerStatus("charger.a"));
            presenter.Bind(system.Service);
            presenter.ToggleInteractionPanel();
            presenter.DismissInteractionPanel();
            presenter.ToggleInteractionPanel();
            var hideBefore = view.HideCount;

            system.Service.ApplyRuntimeStatus(ChargerStatus("charger.b"));

            Assert.That(view.HideCount, Is.GreaterThan(hideBefore));
        }

        [Test]
        public void Presenter_ExpiredCooldown_UsesNormalChargeOnceThenBlocksAgain()
        {
            var system = CreateChargeSystem();
            var view = new CooldownRecordingView();
            var presenter = new OutpostPanelPresenter(view);
            system.State.SetEnergy(10, 100);
            system.Service.ApplyRuntimeStatus(ChargerStatus("charger.a"));
            presenter.Bind(system.Service);
            presenter.ToggleInteractionPanel();
            presenter.DismissInteractionPanel();

            system.State.SetCurrentEnergy(10);
            system.State.AddMineResetElapsed(OutpostService.FacilityUseCooldownSeconds);
            presenter.ToggleInteractionPanel();
            Assert.That(system.State.Player.Energy, Is.EqualTo(100));
            Assert.That(view.CooldownShowCount, Is.EqualTo(0));

            presenter.DismissInteractionPanel();
            system.State.SetCurrentEnergy(10);
            presenter.ToggleInteractionPanel();

            Assert.That(system.State.Player.Energy, Is.EqualTo(10));
            Assert.That(view.CooldownShowCount, Is.EqualTo(1));
        }

        [Test]
        public void Presenter_WithoutCooldownInterface_FallsBackToThreeSecondMessage()
        {
            var system = CreateChargeSystem();
            var view = new PlainRecordingView();
            var presenter = new OutpostPanelPresenter(view);
            system.State.SetEnergy(10, 100);
            system.Service.ApplyRuntimeStatus(ChargerStatus("charger.a"));
            presenter.Bind(system.Service);
            presenter.ToggleInteractionPanel();
            presenter.DismissInteractionPanel();

            presenter.ToggleInteractionPanel();

            Assert.That(view.TemporaryMessage, Is.EqualTo(
                OutpostService.FormatFacilityCooldownMessage(
                    DataIds.Buildings.ChargerBasic,
                    OutpostService.FacilityUseCooldownSeconds)));
            Assert.That(view.TemporaryMessageDuration, Is.EqualTo(3f));
        }

        [Test]
        public void View_Show_DisplaysRealRemainingWithoutPausingProvider()
        {
            var fixture = CreateViewFixture(out var remaining);
            remaining = 197d;

            Assert.That(fixture.View.ShowFacilityCooldown(DataIds.Buildings.ChargerBasic, "charger.a", id => remaining), Is.True);

            Assert.That(fixture.View.IsOpen, Is.True);
            Assert.That(fixture.View.TimeText, Does.Contain("03:17"));
            Assert.That(fixture.View.NameText, Is.Not.Empty);
            Assert.That(fixture.View.StageGroup.blocksRaycasts, Is.False);
            Assert.That(fixture.View.StageGroup.interactable, Is.False);
            foreach (var graphic in fixture.View.GetComponentsInChildren<Graphic>(true))
            {
                Assert.That(graphic.raycastTarget, Is.False, graphic.name);
            }
        }

        [Test]
        public void View_OpeningNearTwelveSeconds_StartsAtMatchingGlowAndCounts()
        {
            var fixture = CreateViewFixture(out var remaining);
            remaining = 12d;
            fixture.View.ShowFacilityCooldown(DataIds.Buildings.ChargerBasic, "charger.a", id => remaining);

            Assert.That(fixture.View.GatherValue, Is.EqualTo(FacilityCooldownPopupTimeline.Gather01(12d)).Within(0.0001f));
            Assert.That(fixture.View.TimeText, Does.Contain("00:12"));
        }

        [Test]
        public void View_RemainingUpdatesOncePerDisplayedSecond()
        {
            var fixture = CreateViewFixture(out var remaining);
            remaining = 196.4d;
            fixture.View.ShowFacilityCooldown(DataIds.Buildings.ChargerBasic, "charger.a", id => remaining);
            Assert.That(fixture.View.DisplaySeconds, Is.EqualTo(197));

            remaining = 195.9d;
            fixture.View.Tick(0.1f);

            Assert.That(fixture.View.DisplaySeconds, Is.EqualTo(196));
            Assert.That(fixture.View.TimeText, Does.Contain("03:16"));
        }

        [Test]
        public void View_SameTargetWhileOpen_IsNoOpAndDoesNotRestartOpening()
        {
            var fixture = CreateViewFixture(out var remaining);
            remaining = 100d;
            fixture.View.ShowFacilityCooldown(DataIds.Buildings.ChargerBasic, "charger.a", id => remaining);
            fixture.View.Tick(0.2f);
            var progress = fixture.View.Progress;

            Assert.That(fixture.View.ShowFacilityCooldown(DataIds.Buildings.ChargerBasic, "charger.a", id => remaining), Is.True);

            Assert.That(fixture.View.Progress, Is.EqualTo(progress).Within(0.0001f));
            Assert.That(fixture.View.IsOpen, Is.True);
        }

        [Test]
        public void View_ReopenWhileClosing_ResumesFromCurrentProgress()
        {
            var fixture = CreateViewFixture(out var remaining);
            remaining = 100d;
            fixture.View.ShowFacilityCooldown(DataIds.Buildings.ChargerBasic, "charger.a", id => remaining);
            fixture.View.Tick(0.3f);
            fixture.View.HideFacilityCooldown();
            fixture.View.Tick(0.1f);
            var closing = fixture.View.Progress;
            Assert.That(closing, Is.GreaterThan(0f));

            fixture.View.ShowFacilityCooldown(DataIds.Buildings.ChargerBasic, "charger.a", id => remaining);
            fixture.View.Tick(0.01f);

            Assert.That(fixture.View.IsOpen, Is.True);
            Assert.That(fixture.View.Progress, Is.GreaterThanOrEqualTo(closing));
        }

        [Test]
        public void View_Hide_ClosesAndDeactivatesAfterExit()
        {
            var fixture = CreateViewFixture(out var remaining);
            remaining = 100d;
            fixture.View.ShowFacilityCooldown(DataIds.Buildings.ChargerBasic, "charger.a", id => remaining);
            fixture.View.Tick(0.5f);

            fixture.View.HideFacilityCooldown();
            for (var i = 0; i < 20; i++)
            {
                fixture.View.Tick(0.05f);
            }

            Assert.That(fixture.View.IsOpen, Is.False);
            Assert.That(fixture.View.Progress, Is.EqualTo(0f));
            Assert.That(fixture.View.gameObject.activeSelf, Is.False);
            Assert.That(fixture.View.InstanceId, Is.Empty);
        }

        [Test]
        public void View_Expiry_AfterObservedShortWait_PlaysCompletionThenCloses()
        {
            var fixture = CreateViewFixture(out var remaining);
            remaining = 1.0d;
            fixture.View.ShowFacilityCooldown(DataIds.Buildings.ChargerBasic, "charger.a", id => remaining);
            fixture.View.Tick(0.1f);

            remaining = -1d;
            fixture.View.Tick(0.1f);
            Assert.That(fixture.View.IsCompleting, Is.True);
            Assert.That(fixture.View.TimeText, Does.Contain("00:00"));

            for (var i = 0; i < 40 && fixture.View.gameObject.activeSelf; i++)
            {
                fixture.View.Tick(0.05f);
            }

            Assert.That(fixture.View.IsCompleting, Is.False);
            Assert.That(fixture.View.gameObject.activeSelf, Is.False);
        }

        [Test]
        public void View_SuddenDisappearance_ClosesWithoutCompletion()
        {
            var fixture = CreateViewFixture(out var remaining);
            remaining = 200d;
            fixture.View.ShowFacilityCooldown(DataIds.Buildings.ChargerBasic, "charger.a", id => remaining);
            fixture.View.Tick(0.1f);

            remaining = -1d;
            fixture.View.Tick(0.1f);

            Assert.That(fixture.View.IsCompleting, Is.False);
            Assert.That(fixture.View.IsOpen, Is.False);
        }

        [Test]
        public void View_TargetChange_RetargetsWithoutRestartingTv()
        {
            var fixture = CreateViewFixture(out var remaining);
            remaining = 100d;
            fixture.View.ShowFacilityCooldown(DataIds.Buildings.ChargerBasic, "charger.a", id => remaining);
            fixture.View.Tick(0.5f);
            var progress = fixture.View.Progress;

            fixture.View.ShowFacilityCooldown(DataIds.Buildings.ClinicBasic, "clinic.a", id => remaining);

            Assert.That(fixture.View.InstanceId, Is.EqualTo("clinic.a"));
            Assert.That(fixture.View.BuildingId, Is.EqualTo(DataIds.Buildings.ClinicBasic));
            Assert.That(fixture.View.Progress, Is.GreaterThanOrEqualTo(progress));
        }

        [Test]
        public void View_TurnOffPath_ReleasesProviderAndStopsPolling()
        {
            // OnDisable/OnDestroy는 EditMode에서 호출되지 않으므로, 실행되는 종료 경로(Hide→TurnOff)로 검증한다.
            // 비활성화·파괴 직후의 정리는 PlayMode 테스트(FacilityCooldownPopupPlayModeTests)가 담당한다.
            var fixture = CreateViewFixture(out var remaining);
            var calls = 0;
            remaining = 100d;
            fixture.View.ShowFacilityCooldown(DataIds.Buildings.ChargerBasic, "charger.a", id => { calls++; return remaining; });
            fixture.View.HideFacilityCooldown();
            for (var i = 0; i < 40; i++)
            {
                fixture.View.Tick(0.05f);
            }

            Assert.That(fixture.View.gameObject.activeSelf, Is.False);
            Assert.That(fixture.View.InstanceId, Is.Empty);
            Assert.That(fixture.View.Progress, Is.EqualTo(0f));
            var callsAfterOff = calls;
            fixture.View.Tick(0.05f);
            Assert.That(calls, Is.EqualTo(callsAfterOff));
            fixture.Dispose();
        }

        [Test]
        public void View_PlateIsSolidTranslucent_AndNoCyanHazeObjectExists()
        {
            var fixture = CreateViewFixture(out _);
            var plate = null as Image;
            foreach (var image in fixture.View.GetComponentsInChildren<Image>(true))
            {
                Assert.That(image.name.Contains("FrameGlow"), Is.False, "흐린 청록 번짐 배경이 남아 있음");
                if (image.name == "Plate")
                {
                    plate = image;
                }
            }

            Assert.That(plate, Is.Not.Null);
            Assert.That(plate.sprite, Is.Null);
            Assert.That(plate.color.a, Is.GreaterThan(0.75f));
            Assert.That(plate.color.a, Is.LessThan(0.95f));
            fixture.Dispose();
        }

        [Test]
        public void View_HideDuringCompletion_CancelsLapAndDoesNotReopen()
        {
            var fixture = CreateViewFixture(out var remaining);
            remaining = 1.0d;
            fixture.View.ShowFacilityCooldown(DataIds.Buildings.ChargerBasic, "charger.a", id => remaining);
            fixture.View.Tick(0.1f);
            remaining = -1d;
            fixture.View.Tick(0.1f);
            Assert.That(fixture.View.IsCompleting, Is.True);

            fixture.View.HideFacilityCooldown();
            Assert.That(fixture.View.IsCompleting, Is.False);
            Assert.That(fixture.View.IsOpen, Is.False);

            remaining = 50d;
            for (var i = 0; i < 40; i++)
            {
                fixture.View.Tick(0.05f);
            }

            Assert.That(fixture.View.IsCompleting, Is.False);
            Assert.That(fixture.View.IsOpen, Is.False);
            Assert.That(fixture.View.gameObject.activeSelf, Is.False);
            fixture.Dispose();
        }

        [Test]
        public void View_ReadsSavedRemainingFromState_NotAnIndependentTimer()
        {
            var system = CreateChargeSystem();
            system.State.SetEnergy(10, 100);
            system.Service.ApplyRuntimeStatus(ChargerStatus("charger.a"));
            Assert.That(system.Service.TryCharge().IsSuccess, Is.True);
            var fixture = CreateViewFixture(out _);
            var state = system.State;

            fixture.View.ShowFacilityCooldown(DataIds.Buildings.ChargerBasic, "charger.a",
                id => state.Outpost.TryGetFacilityCooldownRemaining(id, out var r) ? r : -1d);
            fixture.View.Tick(0.5f);
            Assert.That(fixture.View.DisplaySeconds, Is.EqualTo(300));

            system.State.AddMineResetElapsed(120d);
            fixture.View.Tick(0.1f);

            Assert.That(fixture.View.DisplaySeconds, Is.EqualTo(180));
            Assert.That(fixture.View.TimeText, Does.Contain("03:00"));
            fixture.Dispose();
        }

        private static ViewFixture CreateViewFixture(out double remaining)
        {
            var canvasObject = new GameObject("CooldownTestCanvas", typeof(RectTransform), typeof(Canvas));
            var view = FacilityCooldownPopupView.Create(canvasObject.transform, null);
            remaining = 0d;
            return new ViewFixture(canvasObject, view);
        }

        private sealed class ViewFixture
        {
            public ViewFixture(GameObject canvas, FacilityCooldownPopupView view)
            {
                Canvas = canvas;
                View = view;
            }

            public GameObject Canvas { get; }
            public FacilityCooldownPopupView View { get; }

            public void Dispose()
            {
                if (Canvas != null)
                {
                    UnityEngine.Object.DestroyImmediate(Canvas);
                }
            }
        }

        private static ChargeSystem CreateChargeSystem()
        {
            var catalog = new InMemoryMineralCatalog();
            catalog.Register(DataIds.Minerals.Copper, 1.5f, 10, "구리");
            var state = GameState.CreateNew();
            var inventory = new InventoryService(catalog, 100f, state);
            var service = new OutpostService(inventory, catalog, state);
            return new ChargeSystem(service, state);
        }

        private static OutpostStatusDto ChargerStatus(string instanceId)
        {
            return new OutpostStatusDto
            {
                isActive = false,
                isInInteractionRange = true,
                interactionFacilityInstanceId = instanceId,
                interactionFacilityBuildingId = DataIds.Buildings.ChargerBasic,
                connectedFacilities = new List<ConnectedFacilityStatusDto>
                {
                    new ConnectedFacilityStatusDto
                    {
                        instanceId = instanceId,
                        buildingId = DataIds.Buildings.ChargerBasic,
                        isActive = true,
                        inactiveReasonId = string.Empty
                    }
                }
            };
        }

        private static OutpostStatusDto OutOfRangeStatus()
        {
            return new OutpostStatusDto
            {
                isActive = false,
                isInInteractionRange = false,
                interactionFacilityInstanceId = string.Empty,
                interactionFacilityBuildingId = string.Empty,
                connectedFacilities = new List<ConnectedFacilityStatusDto>()
            };
        }

        private sealed class ChargeSystem
        {
            public ChargeSystem(OutpostService service, GameState state)
            {
                Service = service;
                State = state;
            }

            public OutpostService Service { get; }
            public GameState State { get; }
        }

        private class PlainRecordingView : IOutpostPanelView
        {
            public bool Visible;
            public string TemporaryMessage;
            public float TemporaryMessageDuration;

            public void SetVisible(bool visible) => Visible = visible;
            public void SetMode(OutpostPanelMode mode) { }
            public void SetPower(float supply, float consumption, bool active, string inactiveReasonId) { }
            public void SetFacilities(IReadOnlyList<OutpostFacilityReadModel> facilities) { }
            public void SetCargo(string playerCargo, string storageCargo) { }
            public void SetSettlementCargo(string cargo) { }
            public void SetCheckpoint(string checkpoint) { }
            public void SetSelectedMineral(string summary) { }
            public void SetMineralOptions(IReadOnlyList<OutpostMineralOption> options, string selectedMineralId) { }
            public void ClearMineralSearch() { }
            public void SetResult(string message, bool isError) { }

            public void ShowTemporaryMessage(string message, float durationSeconds)
            {
                TemporaryMessage = message;
                TemporaryMessageDuration = durationSeconds;
            }

            public void SetTutorialVisible(bool visible) { }
            public void SetBusy(bool busy) { }
        }

        private sealed class CooldownRecordingView : PlainRecordingView, IFacilityCooldownPopupView
        {
            public int CooldownShowCount;
            public int HideCount;
            public string LastCooldownBuildingId;
            public string LastCooldownInstanceId;
            public double LastRemaining;

            public bool ShowFacilityCooldown(string buildingId, string instanceId, Func<string, double> remainingProvider)
            {
                CooldownShowCount++;
                LastCooldownBuildingId = buildingId;
                LastCooldownInstanceId = instanceId;
                LastRemaining = remainingProvider(instanceId);
                return true;
            }

            public void HideFacilityCooldown()
            {
                HideCount++;
            }
        }
    }
}

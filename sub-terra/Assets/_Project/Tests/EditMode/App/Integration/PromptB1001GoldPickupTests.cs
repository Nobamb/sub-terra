using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using SubTerra.App.Editor.DataValidation;
using SubTerra.App.Integration;
using SubTerra.App.UI.FacilityNameTag;
using SubTerra.App.UI.HUD;
using SubTerra.Gameplay.Mining;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.Tests.Integration
{
    public sealed class PromptB1001GoldPickupTests
    {
        private readonly List<Object> created = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            for (var index = created.Count - 1; index >= 0; index--)
            {
                if (created[index] != null)
                {
                    Object.DestroyImmediate(created[index]);
                }
            }

            created.Clear();
            // EditMode에서는 OnDestroy가 불리지 않아 연출 루트가 남을 수 있다.
            GameObject leftover;
            while ((leftover = GameObject.Find("GoldPickupWorldRoot")) != null)
            {
                Object.DestroyImmediate(leftover);
            }
        }

        [Test]
        public void PickupText_SplitsConfirmedBaseAndBonusWithoutRecalculating()
        {
            Assert.That(GoldPickupPresentation.BaseGold(35, 15), Is.EqualTo(20));
            Assert.That(GoldPickupPresentation.ClampBonus(35, 15), Is.EqualTo(15));
            Assert.That(GoldPickupPresentation.BaseGold(35, 15) + GoldPickupPresentation.ClampBonus(35, 15), Is.EqualTo(35));
            Assert.That(GoldPickupPresentation.ClampBonus(5, 9), Is.EqualTo(5), "확정 골드를 넘는 보너스는 확정 골드로 제한");
            Assert.That(GoldPickupPresentation.BaseGold(5, 9), Is.EqualTo(0));
            Assert.That(GoldPickupPresentation.ClampBonus(10, -4), Is.EqualTo(0));

            Assert.That(GoldPickupPresentation.FormatMainText(120), Is.EqualTo("+120 G"));
            Assert.That(GoldPickupPresentation.FormatMainText(35, 15), Is.EqualTo("+20 G"));
            Assert.That(GoldPickupPresentation.FormatBonusText(35, 15), Is.EqualTo("추가 골드 +15 G"));
            Assert.That(GoldPickupPresentation.FormatBonusText(20, 0), Is.EqualTo(string.Empty));
            Assert.That(GoldPickupPresentation.FormatMainText(0), Is.EqualTo(string.Empty));
            Assert.That(GoldPickupPresentation.FormatMainText(-3), Is.EqualTo(string.Empty));
            Assert.That(GoldPickupPresentation.FormatBonusText(0, 5), Is.EqualTo(string.Empty));
            Assert.That(GoldPickupPresentation.FormatBase(-1), Is.EqualTo(string.Empty));
            Assert.That(GoldPickupPresentation.FormatBase(1234567), Is.EqualTo("+1234567 G"), "HUD Gold처럼 천 단위 구분자 없음");
            Assert.That(GoldPickupPresentation.FormatBonus(30), Is.EqualTo("추가 골드 +30 G"));
            Assert.That(GoldPickupPresentation.FormatBonus(-2), Is.EqualTo(string.Empty));
            Assert.That(GoldPickupPresentation.FormatBase(7), Does.Not.Contain("BONUS"));
        }

        [Test]
        public void PickupColors_UseWarmGoldPaletteNotCyan()
        {
            Color[] colors =
            {
                GoldPickupPresentation.MainTopColor,
                GoldPickupPresentation.MainBottomColor,
                GoldPickupPresentation.BonusColor
            };
            for (var index = 0; index < colors.Length; index++)
            {
                Assert.That(colors[index].r, Is.GreaterThan(0.9f));
                Assert.That(colors[index].b, Is.LessThan(colors[index].g));
                Assert.That(colors[index].b, Is.LessThan(0.5f));
            }
        }

        [Test]
        public void HudPulse_PeaksWithinRequestedRangeAndReturns()
        {
            Assert.That(GoldPickupPresentation.EvaluatePulse(0f), Is.EqualTo(1f));
            float peak = GoldPickupPresentation.EvaluatePulse(GoldPickupPresentation.PulseSeconds * 0.5f);
            Assert.That(peak, Is.InRange(1.05f, 1.1f));
            Assert.That(GoldPickupPresentation.EvaluatePulse(GoldPickupPresentation.PulseSeconds), Is.EqualTo(1f));
            Assert.That(GoldPickupPresentation.HudParticleCount, Is.InRange(2, 4));

            var start = new Vector2(0f, 0f);
            var end = new Vector2(300f, 200f);
            Assert.That(GoldPickupPresentation.EvaluateHudPath(start, end, 0f, 0), Is.EqualTo(start));
            Assert.That(Vector2.Distance(GoldPickupPresentation.EvaluateHudPath(start, end, 1f, 1), end), Is.LessThan(0.001f));
        }

        [Test]
        public void Popup_IsOneMainScreenHologramThatNeverBlocksRaycasts()
        {
            Vfx vfx = NewVfx(withCoin: false);
            vfx.Component.Play(35, Vector3.zero, Vector3.up, 15);

            Assert.That(vfx.Component.ActivePopupCount, Is.EqualTo(1));
            GoldPickupPopupVisual visual = vfx.Component.PopupVisual;
            Assert.That(visual.MainLabel, Is.EqualTo("+20 G"));
            Assert.That(visual.BonusLabel, Is.EqualTo("추가 골드 +15 G"));

            GameObject root = visual.Root;
            Assert.That(root.layer, Is.EqualTo(FacilityNameTagLayers.MainScreen));
            Assert.That(FacilityNameTagLayers.IsHiddenFromCctv(root), Is.True, "CCTV 카메라에는 보이지 않는다.");
            var canvas = root.GetComponent<Canvas>();
            Assert.That(canvas.renderMode, Is.EqualTo(RenderMode.WorldSpace));
            Assert.That(canvas.sortingOrder, Is.GreaterThan(FacilityNameTagVisual.WorldSortingOrder), "시설 이름표보다 위");
            Assert.That(root.transform.localScale.x, Is.EqualTo(FacilityNameTagVisual.WorldPerPixel));
            Assert.That(root.GetComponentInChildren<GraphicRaycaster>(true), Is.Null);

            foreach (Graphic graphic in root.GetComponentsInChildren<Graphic>(true))
            {
                Assert.That(graphic.raycastTarget, Is.False, graphic.name);
            }

            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                Assert.That(child.gameObject.layer, Is.EqualTo(FacilityNameTagLayers.MainScreen), child.name);
            }

            Assert.That(root.GetComponent<CanvasGroup>().blocksRaycasts, Is.False);
            Assert.That(root.GetComponentsInChildren<RectMask2D>(true).Length, Is.GreaterThanOrEqualTo(2), "슬롯 마스크");
            Assert.That(FrameBracketCount(root), Is.EqualTo(8), "모서리 브래킷 8개");
        }

        [Test]
        public void Popup_FrameWidthDoesNotDependOnAmountAndLeverHangsOutsideRight()
        {
            Vfx small = NewVfx(withCoin: false);
            Vfx large = NewVfx(withCoin: false);
            small.Component.Play(5, Vector3.zero, Vector3.up);
            large.Component.Play(int.MaxValue, Vector3.zero, Vector3.up, 1234567);
            small.Component.Tick(GoldPickupPopupTimeline.AppearSeconds + 0.05f);
            large.Component.Tick(GoldPickupPopupTimeline.AppearSeconds + 0.05f);

            Vector2 smallSize = small.Component.PopupVisual.FrameRect.sizeDelta;
            Vector2 largeSize = large.Component.PopupVisual.FrameRect.sizeDelta;
            Assert.That(smallSize.x, Is.EqualTo(GoldPickupPopupTimeline.FrameWidth));
            Assert.That(largeSize.x, Is.EqualTo(smallSize.x));
            Assert.That(small.Component.PopupVisual.CurrentHeight, Is.EqualTo(44f));
            Assert.That(largeSize.y, Is.EqualTo(72f));
            Assert.That(
                small.Component.PopupVisual.LeverMount.position.x - small.Component.PopupVisual.Root.transform.position.x,
                Is.EqualTo(large.Component.PopupVisual.LeverMount.position.x - large.Component.PopupVisual.Root.transform.position.x).Within(0.0001f),
                "긴 숫자도 레버 위치를 바꾸지 않는다.");

            GoldPickupPopupVisual visual = small.Component.PopupVisual;
            var corners = new Vector3[4];
            visual.FrameRect.GetWorldCorners(corners);
            float frameRight = Mathf.Max(corners[2].x, corners[3].x);
            var leverCorners = new Vector3[4];
            ((RectTransform)visual.LeverMount.Find("Plate")).GetWorldCorners(leverCorners);
            float plateLeft = Mathf.Min(leverCorners[0].x, leverCorners[1].x);
            Assert.That(plateLeft, Is.GreaterThan(frameRight - 0.0001f), "레버 판은 팝업 프레임 바깥 오른쪽에 있다.");
            Assert.That(visual.LeverMount.position.x, Is.GreaterThan(visual.Root.transform.position.x));
            Assert.That(visual.MainLineRect.position.x, Is.EqualTo(visual.Root.transform.position.x).Within(0.0001f), "본문은 플레이어 중앙에 맞춘다.");
        }

        [Test]
        public void Popup_RisesFromBelowWhileLeverWaitsUpThenSettlesInPlace()
        {
            Vfx vfx = NewVfx(withCoin: false);
            vfx.Component.Play(20, Vector3.zero, Vector3.up);
            GoldPickupPopupVisual visual = vfx.Component.PopupVisual;
            Assert.That(visual.BodyRect.anchoredPosition.y, Is.LessThan(-GoldPickupPopupTimeline.SlideRisePx * 0.9f), "처음엔 아래에 있다.");

            float previousY = visual.BodyRect.anchoredPosition.y;
            for (var step = 0; step < 6; step++)
            {
                vfx.Component.Tick(GoldPickupPopupTimeline.AppearSeconds / 6f);
                float y = visual.BodyRect.anchoredPosition.y;
                Assert.That(y, Is.GreaterThanOrEqualTo(previousY - 0.0001f), "등장 동안 위로만 올라간다.");
                Assert.That(visual.LeverAngleDegrees, Is.EqualTo(0f), "팝업이 뜨는 동안 레버는 올라가 있다.");
                previousY = y;
            }

            Assert.That(visual.BodyRect.anchoredPosition.y, Is.EqualTo(0f).Within(0.001f), "다 뜨면 제자리");
            Assert.That(visual.FrameRect.sizeDelta.x, Is.EqualTo(GoldPickupPopupTimeline.FrameWidth).Within(0.001f));
            Assert.That(visual.MainLineRect.anchoredPosition.y, Is.EqualTo(40f), "글자는 아직 슬롯 위에서 대기");
        }

        [Test]
        public void Popup_LeverPullsTowardViewerThenReturnsWhileStayingMounted()
        {
            Vfx vfx = NewVfx(withCoin: false);
            vfx.Component.Play(20, Vector3.zero, Vector3.up);
            GoldPickupPopupVisual visual = vfx.Component.PopupVisual;
            Transform mount = visual.LeverMount;

            vfx.Component.Tick(GoldPickupPopupTimeline.LeverDownStart - 0.01f);
            Vector3 mountLocal = mount.localPosition;
            float restBallY = visual.LeverBall.anchoredPosition.y;
            Assert.That(visual.LeverRod.localScale.y, Is.EqualTo(1f).Within(0.001f));
            Assert.That(visual.LeverBall.localScale.x, Is.EqualTo(1f).Within(0.001f));

            vfx.Component.Tick(GoldPickupPopupTimeline.LeverDownEnd - GoldPickupPopupTimeline.LeverDownStart + 0.01f);
            Assert.That(visual.LeverAngleDegrees, Is.EqualTo(GoldPickupPopupTimeline.LeverPullDegrees).Within(2f), "손잡이가 끝까지 당겨짐");
            Assert.That(visual.LeverBall.anchoredPosition.y, Is.LessThan(restBallY - 20f), "손잡이가 아래로 내려옴");
            Assert.That(visual.LeverRod.localScale.y, Is.LessThan(0f), "막대가 짧아지다 뒤집힘(원근)");
            Assert.That(visual.LeverBall.localScale.x, Is.GreaterThan(1.2f), "관객 쪽으로 다가와 커짐");
            Assert.That(mount.localPosition, Is.EqualTo(mountLocal), "축은 프레임에 고정");

            vfx.Component.Tick(GoldPickupPopupTimeline.LeverReturnEnd - GoldPickupPopupTimeline.LeverDownEnd + 0.02f);
            Assert.That(visual.LeverAngleDegrees, Is.EqualTo(0f), "복귀 뒤 정확히 원위치");
            Assert.That(visual.LeverBall.anchoredPosition.y, Is.EqualTo(restBallY).Within(0.001f));
            Assert.That(visual.LeverBall.localScale, Is.EqualTo(Vector3.one));
            Assert.That(mount.localPosition, Is.EqualTo(mountLocal));
            Assert.That(mount.GetComponentInParent<Button>(), Is.Null, "클릭 버튼이 아니다.");
            Assert.That(mount.GetComponentInChildren<Selectable>(true), Is.Null);
        }

        [Test]
        public void Popup_LinesFallInStaggerThenSettleExactlyWithGhostsRemoved()
        {
            Vfx vfx = NewVfx(withCoin: false);
            vfx.Component.Play(35, Vector3.zero, Vector3.up, 15);
            GoldPickupPopupVisual visual = vfx.Component.PopupVisual;

            vfx.Component.Tick(GoldPickupPopupTimeline.BaseFallStart - 0.01f);
            Assert.That(visual.MainLineRect.anchoredPosition.y, Is.EqualTo(40f), "레버가 올라오기 전엔 대기");
            Assert.That(visual.ActiveMainGhostCount, Is.EqualTo(0));

            vfx.Component.Tick(0.01f + GoldPickupPopupTimeline.FallSeconds * 0.5f);
            Assert.That(visual.MainLineRect.anchoredPosition.y, Is.GreaterThan(0f).And.LessThan(40f), "기본 줄 낙하 중");
            Assert.That(visual.MainLineRect.localScale.y, Is.GreaterThan(1f));
            Assert.That(visual.MainLineRect.localScale.x, Is.LessThan(1f));
            Assert.That(visual.ActiveMainGhostCount, Is.EqualTo(GoldPickupPopupTimeline.MainGhostCount));
            Assert.That(visual.BonusLineRect.anchoredPosition.y, Is.EqualTo(24f), "추가 줄은 기본보다 늦게 시작");
            Assert.That(visual.ActiveBonusGhostCount, Is.EqualTo(0));

            float toSettled = GoldPickupPopupTimeline.SettledTime(GoldPickupPopupTimeline.BaseFallStart)
                - (GoldPickupPopupTimeline.BaseFallStart + GoldPickupPopupTimeline.FallSeconds * 0.5f) + 0.01f;
            vfx.Component.Tick(toSettled);
            Assert.That(visual.MainLineRect.anchoredPosition, Is.EqualTo(Vector2.zero), "기본 줄 정착");
            Assert.That(visual.MainLineRect.localScale, Is.EqualTo(Vector3.one));
            Assert.That(visual.ActiveMainGhostCount, Is.EqualTo(0), "블러가 0이면 잔상 비활성");
            Assert.That(visual.BonusLineRect.anchoredPosition.y, Is.GreaterThan(0f), "추가 줄 낙하 중");
            Assert.That(visual.ActiveBonusGhostCount, Is.GreaterThan(0));
            Assert.That(visual.ActiveBonusGhostCount, Is.LessThanOrEqualTo(GoldPickupPopupTimeline.BonusGhostCount));

            vfx.Component.Tick(0.25f);
            Assert.That(visual.BonusLineRect.anchoredPosition, Is.EqualTo(Vector2.zero));
            Assert.That(visual.BonusLineRect.localScale, Is.EqualTo(Vector3.one));
            Assert.That(visual.ActiveBonusGhostCount, Is.EqualTo(0));
        }

        [Test]
        public void Popup_WithoutBonusSkipsBonusRowSecondBurstAndSweep()
        {
            Vfx vfx = NewVfx(withCoin: true);
            vfx.Component.Play(20, Vector3.zero, Vector3.up);
            vfx.Component.Tick(GoldPickupPopupTimeline.LandTime(GoldPickupPopupTimeline.BaseFallStart) + 0.01f);
            int baseCoins = GoldPickupPopupTimeline.BaseLandCoins(20);
            Assert.That(vfx.Component.ActiveCoinCount, Is.EqualTo(baseCoins));
            Assert.That(vfx.Component.PopupVisual.BonusRowActive, Is.False);
            Assert.That(vfx.Component.PopupVisual.BonusLabel, Is.EqualTo(string.Empty));

            vfx.Component.Tick(0.2f);
            Assert.That(vfx.Component.ActiveCoinCount, Is.LessThanOrEqualTo(baseCoins), "두 번째 분출 없음");
        }

        [Test]
        public void Popup_CoinsBurstPerLandingAndNeverExceedTwentyFour()
        {
            Vfx vfx = NewVfx(withCoin: true);
            vfx.Component.Play(35, Vector3.zero, Vector3.up, 15);
            vfx.Component.Tick(GoldPickupPopupTimeline.LandTime(GoldPickupPopupTimeline.BaseFallStart) + 0.01f);
            Assert.That(vfx.Component.ActiveCoinCount, Is.EqualTo(GoldPickupPopupTimeline.BaseLandCoins(20)));
            vfx.Component.Tick(0.16f);
            Assert.That(
                vfx.Component.ActiveCoinCount,
                Is.EqualTo(GoldPickupPopupTimeline.BaseLandCoins(20) + GoldPickupPopupTimeline.BonusLandCoins(15)));

            for (var merge = 0; merge < 12; merge++)
            {
                vfx.Component.Play(1000, Vector3.zero, Vector3.up);
                Assert.That(vfx.Component.ActiveCoinCount, Is.LessThanOrEqualTo(GoldPickupPopupTimeline.MaxLiveCoins));
            }

            Assert.That(vfx.Component.ActiveCoinCount, Is.EqualTo(GoldPickupPopupTimeline.MaxLiveCoins));
            Assert.That(vfx.Component.ActivePopupCount, Is.EqualTo(1));
        }

        [Test]
        public void Popup_MergeKeepsOnePopupAndSumsConfirmedValues()
        {
            Vfx vfx = NewVfx(withCoin: false);
            vfx.Component.Play(35, Vector3.zero, Vector3.up, 15);
            vfx.Component.Tick(0.4f);
            vfx.Component.Play(23, Vector3.zero, Vector3.up, 3);

            Assert.That(vfx.Component.ActivePopupCount, Is.EqualTo(1));
            GoldPickupPopupState state = vfx.Component.PopupState;
            Assert.That((state.TotalBase, state.TotalBonus), Is.EqualTo((40, 18)));
            Assert.That(vfx.Component.PopupVisual.MainLabel, Is.EqualTo("+20 G"), "표시값은 롤업으로 올라간다.");

            vfx.Component.Tick(0.5f);
            Assert.That(vfx.Component.PopupVisual.MainLabel, Is.EqualTo("+40 G"));
            Assert.That(vfx.Component.PopupVisual.BonusLabel, Is.EqualTo("추가 골드 +18 G"));
            Assert.That(vfx.Component.PopupVisual.MainLineRect.anchoredPosition, Is.EqualTo(Vector2.zero), "롤업은 위치를 흔들지 않는다.");
        }

        [Test]
        public void Popup_MergeOpeningBonusExpandsHeightOnce()
        {
            Vfx vfx = NewVfx(withCoin: false);
            vfx.Component.Play(20, Vector3.zero, Vector3.up);
            vfx.Component.Tick(0.8f);
            GoldPickupPopupVisual visual = vfx.Component.PopupVisual;
            Assert.That(visual.CurrentHeight, Is.EqualTo(44f));
            Assert.That(visual.BonusRowActive, Is.False);

            vfx.Component.Play(30, Vector3.zero, Vector3.up, 30);
            vfx.Component.Tick(0.05f);
            Assert.That(visual.CurrentHeight, Is.GreaterThan(44f).And.LessThan(72f));
            vfx.Component.Tick(0.1f);
            Assert.That(visual.CurrentHeight, Is.EqualTo(72f));
            Assert.That(visual.BonusRowActive, Is.True);
            Assert.That(visual.BonusLabel, Is.EqualTo("추가 골드 +30 G"));

            Assert.That(visual.LeverAngleDegrees, Is.EqualTo(0f));
            vfx.Component.Play(10, Vector3.zero, Vector3.up, 10);
            vfx.Component.Tick(0.05f);
            Assert.That(visual.CurrentHeight, Is.EqualTo(72f), "이미 추가 줄이 있으면 높이는 그대로");
            Assert.That(visual.LeverAngleDegrees, Is.EqualTo(0f), "합치기에는 레버를 다시 당기지 않는다.");
        }

        [Test]
        public void Popup_ExitReversesAppearAndCleansUp()
        {
            Vfx vfx = NewVfx(withCoin: true);
            vfx.Component.Play(20, Vector3.zero, Vector3.up);
            GoldPickupPopupVisual visual = vfx.Component.PopupVisual;
            vfx.Component.Tick(1.0f);
            Assert.That(vfx.Component.PopupState.IsExiting, Is.False);
            Assert.That(visual.BodyRect.anchoredPosition.y, Is.EqualTo(0f).Within(0.001f));
            Assert.That(visual.MainLineRect.anchoredPosition, Is.EqualTo(Vector2.zero));

            // 퇴장 진행 약 0.3: 기본 글자는 빠지는 중이고 프레임은 아직 그대로다.
            vfx.Component.Tick(GoldPickupPopupTimeline.BaseOnlyExitStart - 1.0f + 0.3f * GoldPickupPopupTimeline.ExitSeconds);
            Assert.That(vfx.Component.PopupState.IsExiting, Is.True);
            Assert.That(visual.MainLineRect.anchoredPosition.y, Is.GreaterThan(0f), "글자가 슬롯 위로 빠져나간다.");
            Assert.That(visual.ActiveMainGhostCount, Is.GreaterThan(0), "올라가면서 모션블러");
            Assert.That(visual.FrameRect.sizeDelta.x, Is.EqualTo(GoldPickupPopupTimeline.FrameWidth).Within(0.001f), "글자가 먼저 빠지고 프레임은 그대로");

            vfx.Component.Tick(0.45f * GoldPickupPopupTimeline.ExitSeconds);
            Assert.That(visual.FrameRect.sizeDelta.x, Is.LessThan(GoldPickupPopupTimeline.FrameWidth), "프레임이 접힌다.");
            Assert.That(visual.BodyRect.anchoredPosition.y, Is.LessThan(0f), "아래로 가라앉는다.");

            vfx.Component.Tick(0.4f);
            Assert.That(vfx.Component.ActivePopupCount, Is.EqualTo(0));
            Assert.That(vfx.Component.ActiveCoinCount, Is.EqualTo(0));
            Assert.That(visual.IsAlive, Is.False, "프레임·레버·잔상·금화가 남지 않는다.");
        }

        [Test]
        public void Vfx_PlaySpawnsDustThenExpires()
        {
            Vfx vfx = NewVfx(withCoin: true);
            vfx.Component.Play(20, Vector3.zero, Vector3.up);
            Assert.That(vfx.Component.ActiveDustCount, Is.EqualTo(1));
            vfx.Component.Tick(GoldPickupPresentation.DustCleanupSeconds + 0.1f);
            Assert.That(vfx.Component.ActiveDustCount, Is.EqualTo(0));
        }

        [Test]
        public void Vfx_HudParticlesStartAtExitPulseGoldAndRestoreTransform()
        {
            HudRig rig = NewHudRig();
            rig.Vfx.Play(20, Vector3.zero, Vector3.up);

            rig.Vfx.Tick(GoldPickupPopupTimeline.BaseOnlyExitStart - 0.02f);
            Assert.That(rig.Vfx.ActiveHudParticleCount, Is.EqualTo(0), "퇴장 전에는 HUD 입자가 없다.");
            rig.Vfx.Tick(0.04f);
            Assert.That(rig.Vfx.ActiveHudParticleCount, Is.EqualTo(GoldPickupPresentation.HudParticleCount));

            bool pulsed = false;
            for (var step = 0; step < 200; step++)
            {
                rig.Vfx.Tick(0.02f);
                pulsed |= rig.Vfx.IsPulsing && rig.GoldRect.localScale.x > 1f;
            }

            Assert.That(pulsed, Is.True);
            Assert.That(rig.Vfx.ActiveHudParticleCount, Is.EqualTo(0));
            Assert.That(rig.Vfx.IsPulsing, Is.False);
            Assert.That(rig.GoldRect.localScale, Is.EqualTo(Vector3.one));
            Assert.That(rig.GoldRect.anchoredPosition, Is.EqualTo(new Vector2(60f, -154f)));
        }

        [Test]
        public void Vfx_MergeDuringExitDoesNotLaunchHudParticlesTwice()
        {
            HudRig rig = NewHudRig();
            rig.Vfx.Play(20, Vector3.zero, Vector3.up);
            rig.Vfx.Tick(GoldPickupPopupTimeline.BaseOnlyExitStart - 0.02f);
            rig.Vfx.Tick(0.04f);
            GoldPickupPopupState state = rig.Vfx.PopupState;
            Assert.That(state.HudLaunched, Is.True);

            rig.Vfx.Play(5, Vector3.zero, Vector3.up);
            Assert.That(state.IsExiting, Is.False, "합치기로 퇴장이 취소된다.");

            int maxParticles = 0;
            bool exitedAgain = false;
            for (var step = 0; step < 300; step++)
            {
                rig.Vfx.Tick(0.01f);
                exitedAgain |= state.IsExiting;
                maxParticles = Mathf.Max(maxParticles, rig.Vfx.ActiveHudParticleCount);
            }

            Assert.That(exitedAgain, Is.True);
            Assert.That(maxParticles, Is.LessThanOrEqualTo(GoldPickupPresentation.HudParticleCount), "같은 팝업의 HUD 비행은 중복 발사하지 않는다.");
            Assert.That(rig.Vfx.ActivePopupCount, Is.EqualTo(0));
            Assert.That(rig.GoldRect.localScale, Is.EqualTo(Vector3.one));
        }

        [Test]
        public void FailedMining_ClearsPendingGoldWithoutPlaying()
        {
            Vfx vfx = NewVfx(withCoin: true);
            var mining = vfx.Host.AddComponent<MiningSystem>();
            vfx.Component.BindTo(mining, vfx.Host.transform);
            vfx.Component.SetPendingGold(50, 10);
            Assert.That(vfx.Component.PendingGold, Is.EqualTo(50));

            mining.TryStartMining(Vector3Int.zero);
            Assert.That(vfx.Component.PendingGold, Is.EqualTo(0));
            Assert.That(vfx.Component.ActivePopupCount, Is.EqualTo(0));
            Assert.That(vfx.Component.ActiveCoinCount, Is.EqualTo(0));
        }

        [Test]
        public void AssetsAndWiring_UseDedicatedFontAndVfx()
        {
            Assert.That(File.Exists(Path.Combine(Application.dataPath, "_Project/Art/FX/gold_coin_01.png")), Is.True);
            Assert.That(File.Exists(Path.Combine(Application.dataPath, "_Project/Fonts/SeoulAlrimTTF-Heavy.ttf")), Is.True);

            var hud = File.ReadAllText(Path.Combine(Application.dataPath, "_Project/Prefabs/UI/HUDCanvas.prefab"));
            var scene = File.ReadAllText(
                Path.Combine(Application.dataPath, "_Project/Scenes/App/Mine_Demo_Integration.unity"));
            var binder = File.ReadAllText(
                Path.Combine(Application.dataPath, "_Project/Scripts/App/Integration/IntegrationRuntimeBinder.cs"));

            Assert.That(hud, Does.Contain("GoldPickupVfx"));
            Assert.That(scene, Does.Match(@"goldPickupVfx: \{fileID: [1-9]"));
            Assert.That(binder, Does.Contain("goldPickupVfx.SetPendingGold"));
            Assert.That(binder, Does.Contain("goldPickupVfx.BindTo"));

            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(PromptB1001GoldPickupBuilder.FontSdfPath);
            Assert.That(font, Is.Not.Null);
            // 천 단위 구분자는 쓰지 않으므로(HUD Gold와 동일) 쉼표 글리프는 필요 없다.
            foreach (char glyph in "+G0123456789 추가골드")
            {
                Assert.That(font.characterLookupTable.ContainsKey(glyph), Is.True, "missing glyph " + glyph);
            }

        }

        private Vfx NewVfx(bool withCoin)
        {
            var host = new GameObject("GoldPickupHost");
            created.Add(host);
            var vfx = host.AddComponent<GoldPickupVfx>();
            if (withCoin)
            {
                var texture = new Texture2D(8, 8);
                texture.SetPixel(0, 0, Color.yellow);
                texture.Apply();
                created.Add(texture);
                SetPrivate(vfx, "coinSprite", Sprite.Create(texture, new Rect(0f, 0f, 8f, 8f), new Vector2(0.5f, 0.5f), 8f));
            }

            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(PromptB1001GoldPickupBuilder.FontSdfPath);
            if (font != null)
            {
                SetPrivate(vfx, "pickupFont", font);
            }

            return new Vfx { Host = host, Component = vfx };
        }

        private HudRig NewHudRig()
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(PromptB1001GoldPickupBuilder.FontSdfPath);
            Assume.That(font, Is.Not.Null);
            Vfx vfx = NewVfx(withCoin: true);
            var cameraObject = new GameObject("MainCamera", typeof(Camera)) { tag = "MainCamera" };
            created.Add(cameraObject);
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
            cameraObject.GetComponent<Camera>().orthographic = true;

            var canvasObject = new GameObject("HudCanvas", typeof(RectTransform), typeof(Canvas));
            created.Add(canvasObject);
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var hudObject = new GameObject("BasicHUD", typeof(RectTransform));
            hudObject.transform.SetParent(canvasObject.transform, false);
            var hudView = hudObject.AddComponent<BasicHudView>();
            var goldObject = new GameObject("GoldText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            goldObject.transform.SetParent(hudObject.transform, false);
            var goldText = goldObject.GetComponent<TextMeshProUGUI>();
            goldText.font = font;
            var goldRect = goldText.rectTransform;
            goldRect.pivot = new Vector2(0f, 1f);
            goldRect.sizeDelta = new Vector2(146f, 31f);
            goldRect.anchoredPosition = new Vector2(60f, -154f);
            SetPrivate(hudView, "goldText", goldText);
            vfx.Component.SetHudView(hudView);
            return new HudRig { Vfx = vfx.Component, GoldRect = goldRect };
        }

        private static int FrameBracketCount(GameObject root)
        {
            var count = 0;
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == "BracketH" || child.name == "BracketV")
                {
                    count++;
                }
            }

            return count;
        }

        private static void SetPrivate(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(
                fieldName,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, fieldName);
            field.SetValue(target, value);
        }

        private sealed class Vfx
        {
            public GameObject Host;
            public GoldPickupVfx Component;
        }

        private sealed class HudRig
        {
            public GoldPickupVfx Vfx;
            public RectTransform GoldRect;
        }
    }
}

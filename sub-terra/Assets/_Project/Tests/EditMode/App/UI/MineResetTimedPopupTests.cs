using System.Reflection;
using NUnit.Framework;
using SubTerra.App.Core;
using SubTerra.App.Save;
using SubTerra.App.State;
using SubTerra.App.UI.HUD;
using SubTerra.App.UI.SurfaceBase;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SubTerra.App.Tests.UI
{
    /// <summary>prompt-B 124: 3시간 만료 알림(TimedResetPopup)의 레이어·입력 범위.</summary>
    public sealed class MineResetTimedPopupTests
    {
        private GameObject host;
        private MineResetClockOverlay overlay;
        private Transform popup;

        [SetUp]
        public void SetUp()
        {
            host = new GameObject("TimedPopupTestHost", typeof(RectTransform));
            overlay = MineResetClockOverlay.Create(host.transform);
            popup = overlay.transform.Find("TimedResetPopup");
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(host);
        }

        [Test]
        public void Popup_StartsHiddenAndUsesSharedMotion()
        {
            Assert.That(popup, Is.Not.Null);
            Assert.That(popup.gameObject.activeSelf, Is.False);
            Assert.That(popup.GetComponent<MineResetPopupMotion>(), Is.Not.Null);
            Assert.That(popup.GetComponent<Canvas>().overrideSorting, Is.True);
        }

        [Test]
        public void SkinAsset_ReferencesExistingArtOnly()
        {
            var skin = Resources.Load<MineResetTimedPopupSkin>(MineResetTimedPopupSkin.ResourcePath);
            Assert.That(skin, Is.Not.Null, "Run SubTerra/UI/Build Prompt-B 124 Timed Reset Popup Skin.");
            Assert.That(skin.frame, Is.Not.Null);
            Assert.That(skin.panel, Is.Not.Null);
            Assert.That(skin.cave, Is.Not.Null);
            Assert.That(skin.hexMine, Is.Not.Null);
            Assert.That(skin.hexBorder, Is.Not.Null);
            Assert.That(skin.buttonConfirm, Is.Not.Null);
            Assert.That(skin.buttonConfirmHover, Is.Not.Null);
            Assert.That(skin.caveGlows, Has.Length.EqualTo(3));
            Assert.That(skin.font, Is.Not.Null);
        }

        [Test]
        public void Popup_OnlyBlockerAndOkReceiveRaycasts()
        {
            var targets = new System.Collections.Generic.List<string>();
            foreach (var graphic in popup.GetComponentsInChildren<Graphic>(true))
            {
                if (graphic.raycastTarget)
                {
                    targets.Add(graphic.name);
                }
            }

            Assert.That(targets, Is.EquivalentTo(new[] { "TimedResetPopup", "OkButton" }));
        }

        [Test]
        public void Popup_HasOnlyConfirmButton_AndNoClockPlateOrCloseX()
        {
            var buttons = popup.GetComponentsInChildren<Button>(true);
            Assert.That(buttons, Has.Length.EqualTo(1));
            Assert.That(buttons[0].name, Is.EqualTo("OkButton"));
            Assert.That(popup.Find("Card/Content/CloseButton"), Is.Null);
            Assert.That(popup.Find("Card/Content/TimerPlate"), Is.Null);
            Assert.That(popup.Find("Card/Content/ClockIcon"), Is.Null);
        }

        [Test]
        public void Background_FillsInsideFrameOnly_WithoutDistortingCaveArt()
        {
            var clip = (RectTransform)popup.Find("Card/Body/Background");
            Assert.That(clip.GetComponent<RectMask2D>(), Is.Not.Null);
            var card = (RectTransform)popup.Find("Card");
            Assert.That(clip.sizeDelta.x, Is.LessThanOrEqualTo(card.sizeDelta.x));
            Assert.That(clip.sizeDelta.y, Is.LessThanOrEqualTo(card.sizeDelta.y));

            foreach (var name in new[] { "CaveBottom", "CaveTop" })
            {
                var strip = (RectTransform)clip.Find(name);
                Assert.That(Mathf.Abs(strip.localScale.x), Is.EqualTo(Mathf.Abs(strip.localScale.y)).Within(1e-4f), name + " keeps aspect");
                Assert.That(Mathf.Abs(strip.localScale.x) * strip.sizeDelta.x, Is.GreaterThanOrEqualTo(clip.sizeDelta.x), name + " covers the width");
            }

            Assert.That(clip.Find("TextShade"), Is.Not.Null);
        }

        [Test]
        public void Hex_IsAboutOnePointFourTimesLargerThanConfirmPopup()
        {
            var hex = popup.Find("Card/Hex");
            // 확인창과 같은 카드 비율로 줄인 기존 알림 육각형은 0.52 배였다.
            Assert.That(hex.localScale.x / 0.52f, Is.InRange(1.3f, 1.5f));
            Assert.That(hex.localScale.x, Is.EqualTo(hex.localScale.y));
        }

        [Test]
        public void ShowThenClose_UsesCommittedTextKeysAndOnlyHidesPopup()
        {
            overlay.ShowTimedResetPopup(true);
            Assert.That(popup.gameObject.activeSelf, Is.True);
            var title = popup.Find("Card/Content/Title").GetComponent<TMP_Text>();
            var highlight = popup.Find("Card/Content/Highlight").GetComponent<TMP_Text>();
            var body = popup.Find("Card/Content/Body").GetComponent<TMP_Text>();
            var footer = popup.Find("Card/Content/Footer").GetComponent<TMP_Text>();
            Assert.That(title.text, Is.EqualTo("탐사 시간 종료"));
            Assert.That(highlight.text, Is.EqualTo("새로운 광산 구역이 생성되었습니다."));
            Assert.That(body.text, Is.EqualTo(
                "탐사 시간 3시간이 만료되어 지상 기지로 이동했습니다.\n채굴한 타일과 설치한 지하 시설이 초기화되었습니다."));
            // 비용은 MineResetService 기준 값을 그대로 쓴다.
            Assert.That(footer.text, Is.EqualTo("다음 유료 초기화 비용: " + MineResetService.BaseFeeGold + " G"));

            // Edit Mode에서는 모션 없이 즉시 닫힌다. 닫기는 창만 숨긴다.
            var closed = overlay.TryClosePopup(popup.GetComponent<Canvas>());
            Assert.That(closed, Is.True);
            Assert.That(popup.gameObject.activeSelf, Is.False);
            Assert.That(overlay.TryClosePopup(popup.GetComponent<Canvas>()), Is.False);
        }

        [TestCase(SceneNames.SurfaceBase, true)]
        [TestCase(SceneNames.MainMenu, false)]
        public void RuntimeTicks_KeepCompletedPopupOnSurfaceOnly(string sceneName, bool remainsVisible)
        {
            var scene = SceneManager.GetActiveScene();
            var previousSceneName = scene.name;
            var runtimeHost = new GameObject("TimedPopupRuntimeTestHost");
            try
            {
                scene.name = sceneName;
                var runtime = runtimeHost.AddComponent<SaveRuntimeController>();
                var flags = BindingFlags.Instance | BindingFlags.NonPublic;
                var state = GameState.CreateNew();
                typeof(SaveRuntimeController).GetField("boundState", flags).SetValue(runtime, state);
                typeof(SaveRuntimeController).GetField("activeSlot", flags).SetValue(runtime, 1);
                typeof(SaveRuntimeController).GetField("mineResetClockOverlay", flags).SetValue(runtime, overlay);

                overlay.SetSessionVisible(false);
                overlay.ShowTimedResetPopup(true);
                var tick = typeof(SaveRuntimeController).GetMethod("TickMineResetCycle", flags);
                for (var i = 0; i < 3; i++)
                {
                    tick.Invoke(runtime, null);
                    Assert.That(popup.gameObject.activeSelf, Is.EqualTo(remainsVisible));
                }

                Assert.That(overlay.transform.Find("ClockRoot").gameObject.activeSelf, Is.False);
                Assert.That(state.MineResetCycle.ElapsedSeconds, Is.Zero);
                if (remainsVisible)
                {
                    Assert.That(overlay.TryClosePopup(popup.GetComponent<Canvas>()), Is.True);
                    Assert.That(popup.gameObject.activeSelf, Is.False);
                }
            }
            finally
            {
                Object.DestroyImmediate(runtimeHost);
                scene.name = previousSceneName;
            }
        }
    }
}

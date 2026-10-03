using NUnit.Framework;
using SubTerra.App.UI.HUD;
using SubTerra.App.UI.SurfaceBase;
using TMPro;
using UnityEngine;
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
            Assert.That(skin.closeButton, Is.Not.Null);
            Assert.That(skin.caveGlows, Has.Length.EqualTo(3));
            Assert.That(skin.font, Is.Not.Null);
        }

        [Test]
        public void Popup_OnlyBlockerOkAndCloseReceiveRaycasts()
        {
            var targets = new System.Collections.Generic.List<string>();
            foreach (var graphic in popup.GetComponentsInChildren<Graphic>(true))
            {
                if (graphic.raycastTarget)
                {
                    targets.Add(graphic.name);
                }
            }

            Assert.That(targets, Is.EquivalentTo(new[] { "TimedResetPopup", "OkButton", "CloseButton" }));
        }

        [Test]
        public void Popup_HasOnlyConfirmAndCloseButtons()
        {
            var buttons = popup.GetComponentsInChildren<Button>(true);
            var names = new System.Collections.Generic.List<string>();
            foreach (var button in buttons)
            {
                names.Add(button.name);
            }

            Assert.That(names, Is.EquivalentTo(new[] { "OkButton", "CloseButton" }));
            var close = popup.Find("Card/Content/CloseButton");
            Assert.That(((RectTransform)close).sizeDelta, Is.EqualTo(new Vector2(40f, 40f)));
        }

        [Test]
        public void ShowThenClose_UsesCommittedTextKeysAndOnlyHidesPopup()
        {
            overlay.ShowTimedResetPopup(true);
            Assert.That(popup.gameObject.activeSelf, Is.True);
            var body = popup.Find("Card/Content/Body").GetComponent<TMP_Text>();
            Assert.That(body.text, Does.Contain("\n"));
            Assert.That(body.text, Is.Not.Empty);

            // Edit Mode에서는 모션 없이 즉시 닫힌다. 닫기는 창만 숨긴다.
            var closed = overlay.TryClosePopup(popup.GetComponent<Canvas>());
            Assert.That(closed, Is.True);
            Assert.That(popup.gameObject.activeSelf, Is.False);
            Assert.That(overlay.TryClosePopup(popup.GetComponent<Canvas>()), Is.False);
        }
    }
}

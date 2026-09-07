using System.Reflection;
using NUnit.Framework;
using SubTerra.App.Integration;
using SubTerra.App.UI.Progression;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.Tests.UI
{
    /// <summary>prompt-B 90: 심층 해금 팝업 닫기 버튼과 ESC가 같은 Hide 경로를 탄다.</summary>
    public sealed class PromptB90DeepZoneUnlockPopupTests
    {
        private GameObject canvasRoot;
        private ProgressionPanelView view;

        [SetUp]
        public void SetUp()
        {
            canvasRoot = new GameObject("PromptB90Canvas", typeof(RectTransform), typeof(Canvas));
            var panel = new GameObject("ProgressionPanel", typeof(RectTransform));
            panel.transform.SetParent(canvasRoot.transform, false);
            var resultGo = new GameObject("ResultText", typeof(RectTransform));
            resultGo.transform.SetParent(panel.transform, false);
            var result = resultGo.AddComponent<TextMeshProUGUI>();
            view = panel.AddComponent<ProgressionPanelView>();
            SetPrivateField(view, "resultText", result);
        }

        [TearDown]
        public void TearDown()
        {
            if (canvasRoot != null)
            {
                Object.DestroyImmediate(canvasRoot);
            }
        }

        [Test]
        public void ShowPopup_CreatesCloseButtonLabeledClose()
        {
            view.ShowDeepZoneUnlockPopup();

            Assert.That(view.IsDeepZoneUnlockPopupOpen, Is.True);
            var close = FindCloseButton();
            Assert.That(close, Is.Not.Null);
            var label = close.GetComponentInChildren<TMP_Text>(true);
            Assert.That(label, Is.Not.Null);
            Assert.That(label.text, Is.EqualTo("닫기"));
            Assert.That(
                close.GetComponentInParent<DeepZoneUnlockPopupEscClose>(true),
                Is.Not.Null);
        }

        [Test]
        public void CloseButton_HidesPopup()
        {
            view.ShowDeepZoneUnlockPopup();
            var close = FindCloseButton();
            Assert.That(close, Is.Not.Null);

            close.onClick.Invoke();

            Assert.That(view.IsDeepZoneUnlockPopupOpen, Is.False);
        }

        [Test]
        public void EscapePath_MatchesCloseButton_AndDoesNotOpenSettings()
        {
            view.ShowDeepZoneUnlockPopup();
            Assert.That(view.IsDeepZoneUnlockPopupOpen, Is.True);

            var menuHost = new GameObject("UndergroundMenu");
            var menu = menuHost.AddComponent<UndergroundMenuController>();

            try
            {
                menu.HandleEscape();

                Assert.That(view.IsDeepZoneUnlockPopupOpen, Is.False);
                Assert.That(menu.IsSettingsOpen, Is.False);
                Assert.That(view.TryHideDeepZoneUnlockPopup(), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(menuHost);
            }
        }

        [Test]
        public void HideAndTryHide_UseTheSameClosedState()
        {
            view.ShowDeepZoneUnlockPopup();
            Assert.That(view.TryHideDeepZoneUnlockPopup(), Is.True);
            Assert.That(view.IsDeepZoneUnlockPopupOpen, Is.False);

            view.ShowDeepZoneUnlockPopup();
            view.HideDeepZoneUnlockPopup();
            Assert.That(view.IsDeepZoneUnlockPopupOpen, Is.False);
            Assert.That(view.TryHideDeepZoneUnlockPopup(), Is.False);
        }

        private Button FindCloseButton()
        {
            var popup = canvasRoot.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < popup.Length; i++)
            {
                if (popup[i] != null && popup[i].name == "CloseButton")
                {
                    return popup[i].GetComponent<Button>();
                }
            }

            return null;
        }

        private static void SetPrivateField(object target, string name, object value)
        {
            var field = target.GetType().GetField(
                name,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, "Missing field: " + name);
            field.SetValue(target, value);
        }
    }
}

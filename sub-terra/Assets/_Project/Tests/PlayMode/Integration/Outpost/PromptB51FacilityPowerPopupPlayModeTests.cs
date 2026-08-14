using System.Collections;
using NUnit.Framework;
using SubTerra.App.UI.Outpost;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;

namespace SubTerra.App.Tests.PlayMode.Integration.Outpost
{
    public sealed class PromptB51FacilityPowerPopupPlayModeTests
    {
        [UnityTest]
        public IEnumerator Popup_IsCenteredAndHidesAfterRequestedDuration()
        {
            var canvasObject = new GameObject("PromptB51_Canvas", typeof(Canvas));
            var viewObject = new GameObject("PromptB51_View");
            viewObject.transform.SetParent(canvasObject.transform, false);
            var view = viewObject.AddComponent<OutpostPanelView>();

            view.ShowTemporaryMessage("충전기 사용불가, 전력망 미연결", 0.05f);
            yield return null;

            var popup = canvasObject.transform.Find("FacilityPowerWarning_Runtime") as RectTransform;
            Assert.That(popup, Is.Not.Null);
            Assert.That(popup.anchorMin, Is.EqualTo(new Vector2(0.5f, 0.5f)));
            Assert.That(popup.anchorMax, Is.EqualTo(new Vector2(0.5f, 0.5f)));
            Assert.That(popup.anchoredPosition, Is.EqualTo(Vector2.zero));
            Assert.That(popup.gameObject.activeSelf, Is.True);
            Assert.That(
                popup.GetComponentInChildren<TMP_Text>(true).text,
                Is.EqualTo("충전기 사용불가, 전력망 미연결"));

            yield return new WaitForSecondsRealtime(0.08f);
            Assert.That(popup.gameObject.activeSelf, Is.False);

            Object.Destroy(canvasObject);
            yield return null;
        }
    }
}

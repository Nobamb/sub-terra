using NUnit.Framework;
using SubTerra.App.UI;
using SubTerra.App.UI.Tutorial;
using UnityEditor;
using UnityEngine;

namespace SubTerra.App.Tests.Tutorial
{
    public sealed class QuestDetailsPopupSortTests
    {
        [Test]
        public void OpeningDetails_RisesAboveAnExistingPopup()
        {
            var root = new GameObject("Root", typeof(RectTransform), typeof(Canvas));
            var popup = new GameObject("ExistingPopup", typeof(RectTransform), typeof(Canvas));
            popup.transform.SetParent(root.transform, false);
            var popupCanvas = popup.GetComponent<Canvas>();
            var details = new GameObject("QuestDetailsPanel", typeof(RectTransform), typeof(Canvas));
            details.SetActive(false);
            details.transform.SetParent(root.transform, false);
            var view = new GameObject("DemoObjectiveView").AddComponent<DemoObjectiveView>();
            view.transform.SetParent(root.transform, false);
            Assign(view, "detailsRoot", details);

            try
            {
                PopupWindowSorting.BringToFront(popupCanvas);
                view.SetDetailsVisible(true);

                var detailsCanvas = details.GetComponent<Canvas>();
                Assert.That(details.activeInHierarchy, Is.True);
                Assert.That(detailsCanvas.overrideSorting, Is.True);
                Assert.That(detailsCanvas.sortingOrder, Is.GreaterThan(popupCanvas.sortingOrder));

                view.SetDetailsVisible(false);
                Assert.That(details.activeSelf, Is.False);

                PopupWindowSorting.BringToFront(popupCanvas);
                view.SetDetailsVisible(true);
                Assert.That(detailsCanvas.sortingOrder, Is.GreaterThan(popupCanvas.sortingOrder));
            }
            finally
            {
                PopupWindowSorting.Remove(details.GetComponent<Canvas>());
                PopupWindowSorting.Remove(popupCanvas);
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ClaimOpenedAfterDetails_RisesAboveTheDetailWindow()
        {
            var root = new GameObject("Root", typeof(RectTransform), typeof(Canvas));
            var details = new GameObject("QuestDetailsPanel", typeof(RectTransform), typeof(Canvas));
            details.SetActive(false);
            details.transform.SetParent(root.transform, false);
            var claim = new GameObject("QuestClearRewardPanel", typeof(RectTransform), typeof(Canvas));
            claim.SetActive(false);
            claim.transform.SetParent(root.transform, false);
            var view = new GameObject("DemoObjectiveView").AddComponent<DemoObjectiveView>();
            view.transform.SetParent(root.transform, false);
            Assign(view, "detailsRoot", details);
            Assign(view, "claimRoot", claim);

            try
            {
                view.SetDetailsVisible(true);
                view.SetClaimVisible(true);

                var claimCanvas = claim.GetComponent<Canvas>();
                Assert.That(claim.activeInHierarchy, Is.True);
                Assert.That(claimCanvas.overrideSorting, Is.True);
                Assert.That(claimCanvas.sortingOrder, Is.GreaterThan(details.GetComponent<Canvas>().sortingOrder));
            }
            finally
            {
                PopupWindowSorting.Remove(details.GetComponent<Canvas>());
                PopupWindowSorting.Remove(claim.GetComponent<Canvas>());
                Object.DestroyImmediate(root);
            }
        }

        private static void Assign(DemoObjectiveView view, string field, Object value)
        {
            var serialized = new SerializedObject(view);
            serialized.FindProperty(field).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}

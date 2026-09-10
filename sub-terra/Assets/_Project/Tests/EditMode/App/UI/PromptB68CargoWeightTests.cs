using NUnit.Framework;
using SubTerra.App.Editor.DataValidation;
using SubTerra.App.UI.Inventory;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SubTerra.App.Tests.UI
{
    public sealed class PromptB68CargoWeightTests
    {
        private const string PrefabPath =
            "Assets/_Project/Prefabs/UI/InventoryPanel.prefab";

        [Test]
        public void InventoryPanel_HasVisibleWeightHelpAndHoverDescription()
        {
            PromptB68InventoryWeightBuilder.Build();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Assert.That(prefab, Is.Not.Null);

            var icon = prefab.transform.Find("PanelRoot/WeightHelpIcon");
            var tooltip = prefab.transform.Find("PanelRoot/WeightTooltip");
            var cargo = prefab.transform.Find("PanelRoot/CargoSummaryText");
            var close = prefab.transform.Find("CloseButton");
            Assert.That(icon, Is.Not.Null);
            Assert.That(tooltip, Is.Not.Null);
            Assert.That(cargo, Is.Not.Null);
            Assert.That(close, Is.Not.Null);
            Assert.That(tooltip.gameObject.activeSelf, Is.False);

            var hover = icon.GetComponent<InventoryWeightTooltip>();
            Assert.That(hover, Is.Not.Null);
            Assert.That(hover.TooltipRoot, Is.EqualTo(tooltip.gameObject));

            var iconImage = icon.GetComponent<Image>();
            var iconLabel = icon.GetComponentInChildren<TMP_Text>(true);
            Assert.That(iconImage, Is.Not.Null);
            Assert.That(iconImage.raycastTarget, Is.True);
            Assert.That(iconImage.color.a, Is.EqualTo(1f));
            Assert.That(iconLabel, Is.Not.Null);
            Assert.That(iconLabel.text, Is.EqualTo("?"));
            Assert.That(iconLabel.color, Is.Not.EqualTo(iconImage.color));

            var iconRect = (RectTransform)icon;
            var cargoRect = (RectTransform)cargo;
            var closeRect = (RectTransform)close;
            var panelWidth = ((RectTransform)prefab.transform).sizeDelta.x;
            var cargoRight = cargoRect.anchoredPosition.x + cargoRect.sizeDelta.x;
            var iconLeft = panelWidth + iconRect.anchoredPosition.x - iconRect.sizeDelta.x;
            var iconRight = panelWidth + iconRect.anchoredPosition.x;
            var closeLeft = panelWidth + closeRect.anchoredPosition.x - closeRect.sizeDelta.x;
            Assert.That(cargoRight, Is.LessThan(iconLeft), "중량 텍스트와 도움말 아이콘이 겹칩니다.");
            Assert.That(iconRight, Is.LessThan(closeLeft), "도움말 아이콘이 닫기 버튼에 가려집니다.");

            var description = tooltip.GetComponentInChildren<TMP_Text>(true);
            Assert.That(description.text, Does.Contain("이동 속도"));
            Assert.That(description.text, Does.Contain("더 이상 자원을 채굴할 수 없습니다"));
            Assert.That(description.text, Does.Contain("75%"));
            Assert.That(description.text, Does.Contain("1.5배"));
        }

        [Test]
        public void WeightHelpIcon_ShowsOnlyWhileHovered()
        {
            var icon = new GameObject("WeightHelpIcon", typeof(RectTransform));
            var tooltip = new GameObject("WeightTooltip");
            tooltip.SetActive(false);
            try
            {
                var hover = icon.AddComponent<InventoryWeightTooltip>();
                var serialized = new SerializedObject(hover);
                serialized.FindProperty("tooltipRoot").objectReferenceValue = tooltip;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                hover.OnPointerEnter((PointerEventData)null);
                Assert.That(tooltip.activeSelf, Is.True);

                hover.OnPointerExit((PointerEventData)null);
                Assert.That(tooltip.activeSelf, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(icon);
                Object.DestroyImmediate(tooltip);
            }
        }
    }
}

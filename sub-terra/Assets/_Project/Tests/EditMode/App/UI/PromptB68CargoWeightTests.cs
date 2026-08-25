using NUnit.Framework;
using SubTerra.App.Editor.DataValidation;
using SubTerra.App.UI.Inventory;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SubTerra.App.Tests.UI
{
    public sealed class PromptB68CargoWeightTests
    {
        private const string PrefabPath =
            "Assets/_Project/Prefabs/UI/InventoryPanel.prefab";

        [Test]
        public void InventoryPanel_HasWeightHelpIconAndHoverDescription()
        {
            PromptB68InventoryWeightBuilder.Build();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Assert.That(prefab, Is.Not.Null);

            var icon = prefab.transform.Find("PanelRoot/WeightHelpIcon");
            var tooltip = prefab.transform.Find("PanelRoot/WeightTooltip");
            Assert.That(icon, Is.Not.Null);
            Assert.That(tooltip, Is.Not.Null);
            Assert.That(tooltip.gameObject.activeSelf, Is.False);

            var hover = icon.GetComponent<InventoryWeightTooltip>();
            Assert.That(hover, Is.Not.Null);
            Assert.That(hover.TooltipRoot, Is.EqualTo(tooltip.gameObject));

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

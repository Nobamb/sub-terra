using System;
using System.Linq;
using NUnit.Framework;
using SubTerra.App.Editor.DataValidation;
using SubTerra.App.Outpost;
using SubTerra.App.UI.Outpost;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.Tests.Outpost
{
    public sealed class OutpostStaticStructureTests
    {
        [Test]
        public void H_S01_ServiceContract_DoesNotExposePhysicsOrGameplayImplementationTypes()
        {
            var publicMethods = typeof(OutpostService).GetMethods(
                System.Reflection.BindingFlags.Instance
                | System.Reflection.BindingFlags.Public
                | System.Reflection.BindingFlags.DeclaredOnly);
            var exposedTypes = publicMethods
                .SelectMany(method => method.GetParameters().Select(parameter => parameter.ParameterType))
                .Concat(publicMethods.Select(method => method.ReturnType))
                .ToArray();

            Assert.That(exposedTypes.Any(type => type.Namespace != null
                && type.Namespace.StartsWith("SubTerra.Gameplay", StringComparison.Ordinal)), Is.False);
            Assert.That(exposedTypes.Any(type => typeof(UnityEngine.Object).IsAssignableFrom(type)), Is.False);
        }

        [Test]
        public void H_S02_S03_OutpostState_HasSeparateStorageAndNoUnityObjectFields()
        {
            var state = new OutpostState();
            Assert.That(state.Storage, Is.Not.Null);
            Assert.That(state.Storage.Count, Is.Zero);

            var fields = typeof(OutpostState).GetFields(
                System.Reflection.BindingFlags.Instance
                | System.Reflection.BindingFlags.Public
                | System.Reflection.BindingFlags.NonPublic);
            Assert.That(fields.Any(field => typeof(UnityEngine.Object).IsAssignableFrom(field.FieldType)), Is.False);
        }

        [Test]
        public void H_OutpostPanelPrefab_ExistsWithRequiredReferences()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Project/Prefabs/UI/OutpostPanel.prefab");

            Assert.That(prefab, Is.Not.Null);
            Assert.That(prefab.GetComponent<OutpostPanelBinder>(), Is.Not.Null);
            var view = prefab.GetComponent<OutpostPanelView>();
            Assert.That(view, Is.Not.Null);
            Assert.That(view.HasRequiredReferences(), Is.True);
        }

        [Test]
        public void PromptB52_OutpostPanel_HasSeparateFacilityModesAndScrollableLists()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Project/Prefabs/UI/OutpostPanel.prefab");

            Assert.That(prefab, Is.Not.Null);
            var panelRoot = prefab.transform.Find("PanelRoot");
            Assert.That(panelRoot, Is.Not.Null);
            Assert.That(panelRoot.Find("CoreRoot"), Is.Not.Null);
            Assert.That(panelRoot.Find("ChargerRoot"), Is.Not.Null);
            Assert.That(panelRoot.Find("SettlementRoot"), Is.Not.Null);
            Assert.That(panelRoot.Find("StorageRoot"), Is.Not.Null);
            Assert.That(panelRoot.Find("TransactionRoot"), Is.Not.Null);

            var scrollRects = prefab.GetComponentsInChildren<ScrollRect>(true);
            Assert.That(scrollRects.Any(scroll => scroll.name == "FacilitiesScroll"), Is.True);
            Assert.That(scrollRects.Any(scroll => scroll.name == "SettlementCargoScroll"), Is.True);
            Assert.That(scrollRects.All(scroll => !scroll.horizontal && scroll.vertical), Is.True);
        }

        [Test]
        public void PromptB69_OutpostPanel_HasSearchableMineralDropdown()
        {
            PromptB69StoragePickerBuilder.Build();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Project/Prefabs/UI/OutpostPanel.prefab");

            Assert.That(prefab, Is.Not.Null);
            var transaction = prefab.transform.Find("PanelRoot/TransactionRoot");
            Assert.That(transaction, Is.Not.Null);
            Assert.That(transaction.Find("Select_mineral.copper"), Is.Null);
            Assert.That(transaction.Find("Select_mineral.iron"), Is.Null);
            Assert.That(transaction.Find("Select_mineral.lithium"), Is.Null);

            var picker = transaction.Find("MineralPicker");
            Assert.That(picker, Is.Not.Null);
            Assert.That(picker.Find("SearchInput"), Is.Not.Null);
            Assert.That(picker.Find("CaptionButton"), Is.Not.Null);
            Assert.That(picker.Find("OptionsPanel"), Is.Not.Null);
            Assert.That(picker.GetComponent<OutpostMineralPickerView>(), Is.Not.Null);
            Assert.That(picker.GetComponent<OutpostMineralPickerView>().HasRequiredReferences(), Is.True);
            Assert.That(prefab.GetComponent<OutpostPanelView>().HasRequiredReferences(), Is.True);
        }

        [Test]
        public void PromptB70_OutpostPanel_HasTopRightCloseButton()
        {
            PromptB70FacilityPanelCloseBuilder.Build();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Project/Prefabs/UI/OutpostPanel.prefab");

            Assert.That(prefab, Is.Not.Null);
            var panelRoot = prefab.transform.Find("PanelRoot") as RectTransform;
            var close = panelRoot != null
                ? panelRoot.Find("CloseButton") as RectTransform
                : null;
            Assert.That(panelRoot, Is.Not.Null);
            Assert.That(close, Is.Not.Null);
            Assert.That(close.GetComponent<Button>(), Is.Not.Null);

            var label = close.GetComponentInChildren<TMP_Text>(true);
            Assert.That(label, Is.Not.Null);
            Assert.That(label.text, Is.EqualTo("×"));
            Assert.That(close.anchorMin, Is.EqualTo(new Vector2(1f, 1f)));
            Assert.That(close.anchorMax, Is.EqualTo(new Vector2(1f, 1f)));
            Assert.That(close.pivot, Is.EqualTo(new Vector2(1f, 1f)));
            Assert.That(close.anchoredPosition.x, Is.LessThan(0f));
            Assert.That(close.anchoredPosition.y, Is.LessThanOrEqualTo(0f));

            var title = panelRoot.Find("Title") as RectTransform;
            Assert.That(title, Is.Not.Null);
            var panelWidth = ((RectTransform)prefab.transform).sizeDelta.x;
            var titleRight = title.anchoredPosition.x + title.sizeDelta.x;
            var closeLeft = panelWidth + close.anchoredPosition.x - close.sizeDelta.x;
            Assert.That(titleRight, Is.LessThan(closeLeft), "제목과 X 버튼이 겹치면 안 된다.");

            var view = prefab.GetComponent<OutpostPanelView>();
            Assert.That(view, Is.Not.Null);
            Assert.That(view.CloseButton, Is.EqualTo(close.GetComponent<Button>()));
            Assert.That(view.HasRequiredReferences(), Is.True);
        }
    }
}

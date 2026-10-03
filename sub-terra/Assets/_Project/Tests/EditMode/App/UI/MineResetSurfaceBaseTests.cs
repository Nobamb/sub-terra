using System.IO;
using System.Reflection;
using NUnit.Framework;
using SubTerra.App.Editor.DataValidation;
using SubTerra.App.Save;
using SubTerra.App.UI.SurfaceBase;
using SubTerra.Shared.Localization;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace SubTerra.App.Tests.UI
{
    public sealed class MineResetSurfaceBaseTests
    {
        [Test]
        public void SurfaceBasePrefab_HasResetButtonAndInactiveConfirmationModal()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                MineResetSurfaceBaseLayoutBuilder.SurfaceBasePrefabPath);
            Assert.That(prefab, Is.Not.Null);

            var content = prefab.transform.Find("SurfaceBaseContent");
            var reset = content.Find("ResetMineButton") as RectTransform;
            var message = content.Find("MessageText") as RectTransform;
            var confirm = prefab.transform.Find("ResetMineConfirm");

            Assert.That(reset, Is.Not.Null);
            Assert.That(reset.GetComponent<Button>(), Is.Not.Null);
            Assert.That(reset.anchoredPosition.y,
                Is.EqualTo(-282f).Within(0.5f));
            Assert.That(reset.sizeDelta, Is.EqualTo(new Vector2(530f, 112f)));
            Assert.That(message.anchoredPosition.y,
                Is.EqualTo(-375f).Within(0.5f));
            Assert.That(confirm, Is.Not.Null);
            Assert.That(confirm.gameObject.activeSelf, Is.False);
            Assert.That(confirm.Find("ResetMineCard/Title").GetComponent<TMP_Text>(), Is.Not.Null);
            Assert.That(confirm.Find("ResetMineCard/Body").GetComponent<TMP_Text>(), Is.Not.Null);
            Assert.That(confirm.Find("ResetMineCard/ConfirmButton").GetComponent<Button>(), Is.Not.Null);
            Assert.That(confirm.Find("ResetMineCard/CancelButton").GetComponent<Button>(), Is.Not.Null);
            var card = confirm.Find("ResetMineCard") as RectTransform;
            Assert.That(card.anchoredPosition, Is.EqualTo(Vector2.zero));
            Assert.That(confirm.GetComponent<Canvas>().sortingOrder, Is.EqualTo(700));
            Assert.That(card.GetComponent<SubTerra.App.UI.PopupWindowDrag>(), Is.Null);
            Assert.That(card.GetComponentInChildren<SubTerra.App.UI.Tutorial.QuestClearPopupMotion>(true), Is.Null);
            Assert.That(card.GetComponent<UnityEngine.UI.Image>().color, Is.EqualTo(Color.white));
            Assert.That(AssetDatabase.GetAssetPath(card.GetComponent<UnityEngine.UI.Image>().sprite),
                Is.EqualTo(MineResetSurfaceBaseLayoutBuilder.FramePath));
            var metal = card.Find("MetalPanel").GetComponent<UnityEngine.UI.RawImage>();
            Assert.That(AssetDatabase.GetAssetPath(metal.texture), Is.EqualTo(MineResetSurfaceBaseLayoutBuilder.MetalPath));
            var close = card.Find("Close") as RectTransform;
            Assert.That(close.parent, Is.SameAs(card));
            Assert.That(close.sizeDelta, Is.EqualTo(new Vector2(40f, 40f)));
            Assert.That(AssetDatabase.GetAssetPath(close.GetComponent<UnityEngine.UI.Image>().sprite),
                Is.EqualTo(MineResetSurfaceBaseLayoutBuilder.ClosePath));
            foreach (var name in new[] { "Title", "Body", "ConfirmButton", "CancelButton", "Close" })
            {
                var rect = card.Find(name) as RectTransform;
                Assert.That(Mathf.Abs(rect.anchoredPosition.x) + rect.sizeDelta.x / 2f,
                    Is.LessThan(metal.rectTransform.sizeDelta.x / 2f), name);
                Assert.That(Mathf.Abs(rect.anchoredPosition.y - metal.rectTransform.anchoredPosition.y) + rect.sizeDelta.y / 2f,
                    Is.LessThan(metal.rectTransform.sizeDelta.y / 2f), name);
            }
            foreach (var name in new[] { "ConfirmButton", "CancelButton" })
            {
                var button = card.Find(name).GetComponent<UnityEngine.UI.Button>();
                Assert.That(button.transition, Is.EqualTo(Selectable.Transition.SpriteSwap));
                Assert.That(button.GetComponent<UnityEngine.UI.Image>().color, Is.EqualTo(Color.white));
                Assert.That(AssetDatabase.GetAssetPath(button.GetComponent<UnityEngine.UI.Image>().sprite),
                    Is.EqualTo(MineResetSurfaceBaseLayoutBuilder.ButtonOffPath));
                Assert.That(AssetDatabase.GetAssetPath(button.spriteState.highlightedSprite),
                    Is.EqualTo(MineResetSurfaceBaseLayoutBuilder.ButtonOnPath));
            }
            Assert.That(prefab.GetComponent<SurfaceBaseView>().HasRequiredReferences(), Is.True);
        }

        [TestCase(GameLanguage.Korean, 500)]
        [TestCase(GameLanguage.Korean, 1000)]
        [TestCase(GameLanguage.English, 1000)]
        [TestCase(GameLanguage.English, int.MaxValue)]
        public void Confirmation_DisplaysQuotedFeeBalanceAndTimerWithinFrame(GameLanguage language, int fee)
        {
            var previous = LocalizationService.Current;
            var instance = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(
                MineResetSurfaceBaseLayoutBuilder.SurfaceBasePrefabPath));
            try
            {
                LocalizationService.SetLanguage(language);
                var view = instance.GetComponent<SurfaceBaseView>();
                view.SetMineResetConfirmVisible(true, int.MaxValue, fee);
                var body = instance.transform.Find("ResetMineConfirm/ResetMineCard/Body").GetComponent<TMP_Text>();
                Assert.That(body.text, Is.EqualTo(string.Format(LocalizationService.Get("mine_reset.confirm.body"),
                    int.MaxValue, int.MaxValue - fee, fee)));
                Assert.That(body.textWrappingMode, Is.EqualTo(TextWrappingModes.Normal));
                Assert.That(body.overflowMode, Is.Not.EqualTo(TextOverflowModes.Overflow));
                Assert.That(body.alignment, Is.EqualTo(TextAlignmentOptions.MidlineLeft));
                body.ForceMeshUpdate();
                Assert.That(body.isTextOverflowing, Is.False);
                Assert.That(body.preferredHeight, Is.LessThan(body.rectTransform.rect.height));
                Assert.That(instance.transform.Find("SurfaceBaseContent/ResetMineButton/FeeLabel").GetComponent<TMP_Text>().text,
                    Is.EqualTo(fee + "G"));
            }
            finally
            {
                Object.DestroyImmediate(instance);
                LocalizationService.SetLanguage(previous);
            }
        }

        [Test]
        public void Confirmation_SpriteSwapAndCloseRespectBusyState()
        {
            var instance = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(
                MineResetSurfaceBaseLayoutBuilder.SurfaceBasePrefabPath));
            try
            {
                var view = instance.GetComponent<SurfaceBaseView>();
                typeof(SurfaceBaseView).GetMethod("OnEnable", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(view, null);
                var card = instance.transform.Find("ResetMineConfirm/ResetMineCard");
                view.SetMineResetConfirmVisible(true, 3000, 500);
                var yes = card.Find("ConfirmButton").GetComponent<UnityEngine.UI.Button>();
                var image = yes.GetComponent<UnityEngine.UI.Image>();
                yes.OnPointerEnter(new PointerEventData(null));
                Assert.That(image.overrideSprite, Is.SameAs(yes.spriteState.highlightedSprite));
                yes.OnPointerExit(new PointerEventData(null));
                Assert.That(image.overrideSprite, Is.SameAs(image.sprite));
                var cancelled = 0;
                view.ResetMineCancelled += () => { cancelled++; view.SetMineResetConfirmVisible(false); };
                view.SetMineResetBusy(true);
                foreach (var path in new[] { "ResetMineConfirm/ResetMineCard/ConfirmButton", "ResetMineConfirm/ResetMineCard/CancelButton",
                    "ResetMineConfirm/ResetMineCard/Close", "SurfaceBaseContent/ResetMineButton" })
                    Assert.That(instance.transform.Find(path).GetComponent<UnityEngine.UI.Button>().interactable, Is.False);
                var close = card.Find("Close").GetComponent<UnityEngine.UI.Button>();
                close.OnPointerClick(new PointerEventData(null));
                Assert.That(cancelled, Is.Zero);
                view.SetMineResetBusy(false);
                close.OnPointerClick(new PointerEventData(null));
                Assert.That(cancelled, Is.EqualTo(1));
                Assert.That(view.IsMineResetConfirmVisible, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void ResetFee_UpdatesSeparateCostWithoutDuplicatingItInLocalizedTitle()
        {
            var previous = LocalizationService.Current;
            var instance = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(
                MineResetSurfaceBaseLayoutBuilder.SurfaceBasePrefabPath));
            try
            {
                var view = instance.GetComponent<SurfaceBaseView>();
                var reset = instance.transform.Find("SurfaceBaseContent/ResetMineButton");
                var label = reset.Find("Label").GetComponent<TMP_Text>();
                var fee = reset.Find("FeeLabel").GetComponent<TMP_Text>();
                LocalizationService.SetLanguage(GameLanguage.Korean);
                view.SetMineResetButtonFee(875);
                Assert.That(label.text, Is.EqualTo("새 광산 초기화"));
                Assert.That(fee.text, Is.EqualTo("875G"));
                LocalizationService.SetLanguage(GameLanguage.English);
                view.SetMineResetButtonFee(1250);
                Assert.That(label.text, Is.EqualTo("New Mine"));
                Assert.That(fee.text, Is.EqualTo("1250G"));
                view.SetMineResetButtonFee(-1);
                Assert.That(fee.text, Is.EqualTo("0G"));
            }
            finally
            {
                Object.DestroyImmediate(instance);
                LocalizationService.SetLanguage(previous);
            }
        }

        [Test]
        public void ResetUi_UsesLocalizedLabelsAndPresenterDoesNotMutateGold()
        {
            var previous = LocalizationService.Current;
            try
            {
                LocalizationService.SetLanguage(GameLanguage.Korean);
                Assert.That(LocalizationService.Get("mine_reset.button"), Does.Contain("{0}G"));
                Assert.That(LocalizationService.Get("mine_reset.confirm.body"), Does.Contain("{2}G"));
                Assert.That(LocalizationService.Get("mine_reset.success"), Does.Contain("{0}G"));

                LocalizationService.SetLanguage(GameLanguage.English);
                Assert.That(LocalizationService.Get("mine_reset.button"), Is.EqualTo("New Mine ({0}G)"));
                Assert.That(LocalizationService.Get("mine_reset.fail.surface"), Does.Contain("Surface Base"));
            }
            finally
            {
                LocalizationService.SetLanguage(previous);
            }

            var presenterPath = Path.Combine(
                Application.dataPath,
                "_Project",
                "Scripts",
                "App",
                "UI",
                "SurfaceBase",
                "SurfaceBasePresenter.cs");
            var presenterText = File.ReadAllText(presenterPath);
            Assert.That(presenterText, Does.Not.Contain("SetGold"));
            Assert.That(presenterText, Does.Contain("MineResetService.GetFeeGold"));
        }

        [Test]
        public void BuilderScope_IsLimitedToSurfaceBasePrefabAndScene()
        {
            var builderPath = Path.Combine(
                Application.dataPath,
                "_Project",
                "Editor",
                "DataValidation",
                "MineResetSurfaceBaseLayoutBuilder.cs");
            var text = File.ReadAllText(builderPath);

            Assert.That(text, Does.Contain("SurfaceBasePanel.prefab"));
            Assert.That(text, Does.Contain("SurfaceBase.unity"));
            Assert.That(text, Does.Not.Contain("MainMenuPanel.prefab"));
            Assert.That(text, Does.Not.Contain("InventoryPanel.prefab"));
            Assert.That(text, Does.Not.Contain("EconomySellRow.prefab"));
        }

        [Test]
        public void Fee_IsSingleSharedConstant()
        {
            Assert.That(MineResetService.FeeGold, Is.EqualTo(500));
        }
    }
}

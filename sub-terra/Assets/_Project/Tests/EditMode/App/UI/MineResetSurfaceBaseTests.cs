using System.IO;
using NUnit.Framework;
using SubTerra.App.Editor.DataValidation;
using SubTerra.App.Save;
using SubTerra.App.UI.SurfaceBase;
using SubTerra.Shared.Localization;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

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
                Is.EqualTo(MineResetSurfaceBaseLayoutBuilder.ResetButtonY).Within(0.5f));
            Assert.That(reset.sizeDelta, Is.EqualTo(new Vector2(320f, 48f)));
            Assert.That(message.anchoredPosition.y,
                Is.EqualTo(MineResetSurfaceBaseLayoutBuilder.MessageY).Within(0.5f));
            Assert.That(confirm, Is.Not.Null);
            Assert.That(confirm.gameObject.activeSelf, Is.False);
            Assert.That(confirm.Find("ResetMineCard/Title").GetComponent<TMP_Text>(), Is.Not.Null);
            Assert.That(confirm.Find("ResetMineCard/Body").GetComponent<TMP_Text>(), Is.Not.Null);
            Assert.That(confirm.Find("ResetMineCard/ConfirmButton").GetComponent<Button>(), Is.Not.Null);
            Assert.That(confirm.Find("ResetMineCard/CancelButton").GetComponent<Button>(), Is.Not.Null);
            Assert.That(prefab.GetComponent<SurfaceBaseView>().HasRequiredReferences(), Is.True);
        }

        [Test]
        public void ResetUi_UsesLocalizedLabelsAndPresenterDoesNotMutateGold()
        {
            var previous = LocalizationService.Current;
            try
            {
                LocalizationService.SetLanguage(GameLanguage.Korean);
                Assert.That(LocalizationService.Get("mine_reset.button"), Does.Contain("500G"));
                Assert.That(LocalizationService.Get("mine_reset.confirm.body"), Does.Contain("{0}"));
                Assert.That(LocalizationService.Get("mine_reset.success"), Does.Contain("500G"));

                LocalizationService.SetLanguage(GameLanguage.English);
                Assert.That(LocalizationService.Get("mine_reset.button"), Is.EqualTo("New Mine (500G)"));
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
            Assert.That(presenterText, Does.Contain("MineResetService.FeeGold"));
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

using System.Reflection;
using NUnit.Framework;
using SubTerra.App.State;
using SubTerra.App.UI.SurfaceBase;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.Tests.UI
{
    public sealed class PromptB117SurfaceBaseTests
    {
        private const string PrefabPath = "Assets/_Project/Prefabs/UI/SurfaceBasePanel.prefab";

        [Test]
        public void Header_ConsumesCurrentStateWithoutChangingIt()
        {
            var instance = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath));
            try
            {
                var state = GameState.CreateNew();
                state.SetGold(875);
                state.SetCargoWeight(96.4f);
                var binder = instance.GetComponent<SurfaceBaseBinder>();
                var flags = BindingFlags.Instance | BindingFlags.NonPublic;
                typeof(SurfaceBaseBinder).GetField("boundCycleState", flags).SetValue(binder, state);
                typeof(SurfaceBaseBinder).GetMethod("RefreshResources", flags).Invoke(binder, null);
                var content = instance.transform.Find("SurfaceBaseContent");
                Assert.That(content.Find("CargoText").GetComponent<TMP_Text>().text, Does.Contain("96.4"));
                Assert.That(content.Find("GoldText").GetComponent<TMP_Text>().text, Does.Contain("875G"));
                state.SetGold(375);
                typeof(SurfaceBaseBinder).GetMethod("OnCreditsChanged", flags).Invoke(binder, new object[] { 375 });
                Assert.That(content.Find("GoldText").GetComponent<TMP_Text>().text, Does.Contain("375G"));
                Assert.That(state.Player.Cargo, Is.EqualTo(96.4f));
                Assert.That(state.Player.Gold, Is.EqualTo(375));
            }
            finally { Object.DestroyImmediate(instance); }
        }

        [Test]
        public void PrimaryActions_HaveUnblockedRaycastGraphicsAndFeedback()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            var content = prefab.transform.Find("SurfaceBaseContent");
            foreach (var name in new[] { "ExploreButton", "OpenSellButton", "UpgradeButton",
                "ResetMineButton", "SettingsButton", "QuitButton" })
            {
                var button = content.Find(name).GetComponent<Button>();
                Assert.That(button, Is.Not.Null, name);
                Assert.That(button.targetGraphic.raycastTarget, Is.True, name);
                Assert.That(button.GetComponent<SurfaceBaseButtonFeedback>(), Is.Not.Null, name);
                foreach (var graphic in button.GetComponentsInChildren<Graphic>(true))
                    if (graphic != button.targetGraphic) Assert.That(graphic.raycastTarget, Is.False, name + "/" + graphic.name);
            }
            foreach (var name in new[] { "TopFrame", "CargoIcon", "GoldIcon" })
                Assert.That(content.Find(name).GetComponent<Image>().raycastTarget, Is.False, name);
            Assert.That(prefab.transform.Find("SurfaceBackground").GetComponent<Image>().raycastTarget, Is.False);
        }

        [Test]
        public void ActionButtons_HaveDimIconsParticleRootAndResetSpin()
        {
            var content = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath).transform.Find("SurfaceBaseContent");
            foreach (var name in new[] { "ExploreButton", "OpenSellButton", "UpgradeButton", "ResetMineButton" })
            {
                var button = content.Find(name);
                var serialized = new SerializedObject(button.GetComponent<SurfaceBaseButtonFeedback>());
                var icon = (Image)serialized.FindProperty("icon").objectReferenceValue;
                Assert.That(icon, Is.Not.Null, name);
                Assert.That(icon.color.r, Is.LessThan(0.6f), name);
                Assert.That(serialized.FindProperty("particleRoot").objectReferenceValue, Is.Not.Null, name);
                Assert.That(serialized.FindProperty("spinIconOnHover").boolValue, Is.EqualTo(name == "ResetMineButton"), name);
                Assert.That(serialized.FindProperty("glow").objectReferenceValue != null,
                    Is.EqualTo(name == "ExploreButton" || name == "ResetMineButton"), name);
            }
        }

        [Test]
        public void ResetButton_KeepsIconLabelAndFeeInOneCenteredRow()
        {
            var reset = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath).transform
                .Find("SurfaceBaseContent/ResetMineButton");
            var row = reset.GetComponent<HorizontalLayoutGroup>();
            Assert.That(row, Is.Not.Null);
            Assert.That(row.childAlignment, Is.EqualTo(TextAnchor.MiddleCenter));
            foreach (var name in new[] { "Frame", "HoverFrame", "InnerLight", "HoverParticles" })
                Assert.That(reset.Find(name).GetComponent<LayoutElement>().ignoreLayout, Is.True, name);
            Assert.That(reset.Find("Icon").GetSiblingIndex(), Is.LessThan(reset.Find("Label").GetSiblingIndex()));
            Assert.That(reset.Find("Label").GetSiblingIndex(), Is.LessThan(reset.Find("FeeLabel").GetSiblingIndex()));
        }

        [Test]
        public void HeaderGroups_SitInsideRightFramePanel()
        {
            var content = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath).transform.Find("SurfaceBaseContent");
            float X(string name) { var r = (RectTransform)content.Find(name); return r.anchorMin.x * 1920f + r.anchoredPosition.x; }
            Assert.That(X("CargoIcon"), Is.GreaterThan(1240f));
            Assert.That(X("GoldIcon"), Is.GreaterThan(X("CargoIcon") + 150f));
            Assert.That(X("QuitButton") + 36f, Is.LessThanOrEqualTo(1815f));
            Assert.That(X("SettingsButton") - 36f, Is.GreaterThan(X("GoldText") + 145f));
        }

        [Test]
        public void IconSpin_EasesInHoldsAndEasesOutOverHalfSecond()
        {
            Assert.That(SurfaceBaseButtonFeedback.SpinTurn(0f), Is.EqualTo(0f).Within(1e-4f));
            Assert.That(SurfaceBaseButtonFeedback.SpinTurn(0.2f), Is.EqualTo(1f / 3f).Within(1e-4f));
            Assert.That(SurfaceBaseButtonFeedback.SpinTurn(0.3f), Is.EqualTo(2f / 3f).Within(1e-4f));
            Assert.That(SurfaceBaseButtonFeedback.SpinTurn(0.5f), Is.EqualTo(1f).Within(1e-4f));
            float previous = 0f;
            for (float t = 0f; t <= 0.5f; t += 0.01f)
            {
                float turn = SurfaceBaseButtonFeedback.SpinTurn(t);
                Assert.That(turn, Is.GreaterThanOrEqualTo(previous - 1e-5f));
                previous = turn;
            }
        }

        [Test]
        public void UpgradeCloseButton_AndSettings_DoNotLeaveModalBlockingScreen()
        {
            var instance = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath));
            try
            {
                var view = instance.GetComponent<SurfaceBaseView>();
                typeof(SurfaceBaseView).GetMethod("OnEnable", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(view, null);
                view.UpgradeCloseClicked += () => view.SetUpgradeVisible(false);
                view.SetUpgradeVisible(true);
                Assert.That(view.IsUpgradeVisible, Is.True);
                instance.transform.Find("SurfaceBaseContent/UpgradeModal/ProgressionPanel/CloseUpgradeButton")
                    .GetComponent<Button>().onClick.Invoke();
                Assert.That(view.IsUpgradeVisible, Is.False);
                view.SetUpgradeVisible(true);
                view.SetSettingsVisible(true);
                Assert.That(view.IsUpgradeVisible, Is.False);
                Assert.That(instance.transform.Find("SettingsPanel").gameObject.activeSelf, Is.True);
                view.SetSettingsVisible(false);
                Assert.That(view.IsUpgradeVisible, Is.False);
            }
            finally { Object.DestroyImmediate(instance); }
        }
    }
}

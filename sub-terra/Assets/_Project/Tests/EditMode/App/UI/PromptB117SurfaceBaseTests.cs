using System.Reflection;
using NUnit.Framework;
using SubTerra.App.State;
using SubTerra.App.Tutorial;
using SubTerra.App.UI;
using SubTerra.App.UI.SurfaceBase;
using SubTerra.App.UI.Tutorial;
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
        public void Issue249_InventoryReusesMinePrefabAndKeepsResetInsideScreen()
        {
            var root = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            var content = root.transform.Find("SurfaceBaseContent");
            var modal = content.Find("InventoryModal");
            var inventory = modal.GetComponentInChildren<SubTerra.App.UI.Inventory.InventoryPanelView>(true);
            Assert.That(modal.gameObject.activeSelf, Is.False);
            Assert.That(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(inventory.gameObject),
                Is.EqualTo("Assets/_Project/Prefabs/UI/InventoryPanel.prefab"));
            Assert.That(((RectTransform)inventory.transform).anchoredPosition, Is.EqualTo(Vector2.zero));
            var view = new SerializedObject(root.GetComponent<SurfaceBaseView>());
            Assert.That(view.FindProperty("inventoryPanel").objectReferenceValue, Is.EqualTo(inventory));
            Assert.That(view.FindProperty("inventoryButton").objectReferenceValue,
                Is.EqualTo(content.Find("InventoryButton").GetComponent<Button>()));
            var binder = new SerializedObject(root.GetComponent<SurfaceBaseBinder>());
            Assert.That(binder.FindProperty("inventoryBinder").objectReferenceValue,
                Is.EqualTo(inventory.GetComponent<SubTerra.App.UI.Inventory.InventoryPanelBinder>()));
            var reset = (RectTransform)content.Find("ResetMineButton");
            Assert.That(reset.anchoredPosition.y - 177f / 2f, Is.GreaterThan(-540f));
        }

        [Test]
        public void Issue209_SurfaceOKeyUsesQuitPolicyAndDefersWhileSaving()
        {
            const string editModeInput = "RUN_PLAYER_UPDATES_IN_EDIT_MODE";
            var inputSettings = UnityEngine.InputSystem.InputSystem.settings;
            var hadEditModeInput = (bool)inputSettings.GetType()
                .GetMethod("IsFeatureEnabled", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(inputSettings, new object[] { editModeInput });
            inputSettings.SetInternalFeatureFlag(editModeInput, true);
            var previousKeyboard = UnityEngine.InputSystem.Keyboard.current;
            var keyboard = UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Keyboard>();
            var runtimeRoot = new GameObject("QuitShortcutRuntime");
            var host = new GameObject("SurfaceQuitShortcut");
            var runtime = runtimeRoot.AddComponent<SubTerra.App.Save.SaveRuntimeController>();
            var instanceProperty = typeof(SubTerra.App.Save.SaveRuntimeController).GetProperty("Instance");
            var previousRuntime = instanceProperty.GetValue(null);
            var view = host.AddComponent<SurfaceBaseView>();
            var binder = host.AddComponent<SurfaceBaseBinder>();
            var textRoot = new GameObject("Message", typeof(RectTransform));
            textRoot.transform.SetParent(host.transform);
            var message = textRoot.AddComponent<TextMeshProUGUI>();
            try
            {
                instanceProperty.SetValue(null, runtime);
                typeof(SubTerra.App.Save.SaveRuntimeController).GetField("saveInProgress", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(runtime, true); // 실제 종료·저장 없이 기존 종료 정책까지 입력이 도달하는지 확인한다.
                typeof(SurfaceBaseView).GetField("messageText", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(view, message);
                typeof(SurfaceBaseBinder).GetField("view", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(binder, view);
                typeof(SurfaceBaseBinder).GetField("settings", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(binder, new SubTerra.App.UI.MainMenu.SettingsSession());
                // Input System은 첫 조회부터 프레임별 누름 상태를 추적하므로 입력 전에 초기화한다.
                _ = keyboard.oKey.wasPressedThisFrame;
                UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard, new UnityEngine.InputSystem.LowLevel.KeyboardState());
                UnityEngine.InputSystem.InputSystem.Update();
                UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard,
                    new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.O));
                UnityEngine.InputSystem.InputSystem.Update();
                keyboard.MakeCurrent();
                Assert.That(keyboard.oKey.wasPressedThisFrame, Is.True, "검사용 키보드에서 O 눌림이 전달되어야 한다.");
                Assert.That(UiPauseGate.IsHeld, Is.False, "검사 시작 시 UI 일시정지가 없어야 한다.");
                typeof(SurfaceBaseBinder).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(binder, null);
                Assert.That(message.text, Is.EqualTo("저장 중입니다. 잠시 후 다시 시도하세요."));
            }
            finally
            {
                Object.DestroyImmediate(host);
                Object.DestroyImmediate(runtimeRoot);
                instanceProperty.SetValue(null, previousRuntime);
                UnityEngine.InputSystem.InputSystem.RemoveDevice(keyboard);
                if (previousKeyboard != null) previousKeyboard.MakeCurrent();
                inputSettings.SetInternalFeatureFlag(editModeInput, hadEditModeInput);
            }
        }

        [Test]
        public void QuestIcon_UsesSurfaceSetAssetWithoutChangingLayout()
        {
            var root = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            var quest = (RectTransform)root.transform.Find("SurfaceBaseContent/QuestButton");
            var icon = quest.Find("Icon").GetComponent<Image>();
            Assert.That(AssetDatabase.GetAssetPath(icon.sprite),
                Is.EqualTo("Assets/_Project/Art/UI/SurfaceBase/Icons/icon-quest.png"));
            Assert.That(icon.preserveAspect, Is.True);
            Assert.That(icon.GetComponent<LayoutElement>().preferredWidth, Is.EqualTo(50f));
            Assert.That(icon.rectTransform.sizeDelta.y, Is.EqualTo(50f));
            Assert.That(quest.sizeDelta, Is.EqualTo(new Vector2(386f, 98f)));
            Assert.That(quest.anchoredPosition, Is.EqualTo(new Vector2(-213f, -145f)));
        }

        [Test]
        public void QuestActions_UseEqualSizeAndSpacing()
        {
            var content = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath).transform.Find("SurfaceBaseContent");
            var quest = (RectTransform)content.Find("QuestButton");
            var sell = (RectTransform)content.Find("OpenSellButton");
            var upgrade = (RectTransform)content.Find("UpgradeButton");
            var inventory = (RectTransform)content.Find("InventoryButton");
            Assert.That(quest.sizeDelta, Is.EqualTo(sell.sizeDelta));
            Assert.That(upgrade.sizeDelta, Is.EqualTo(sell.sizeDelta));
            Assert.That(inventory.sizeDelta, Is.EqualTo(sell.sizeDelta));
            Assert.That(quest.anchoredPosition.y, Is.EqualTo(upgrade.anchoredPosition.y));
            Assert.That(sell.anchoredPosition.y, Is.EqualTo(inventory.anchoredPosition.y));
            Assert.That(quest.anchoredPosition.x, Is.EqualTo(sell.anchoredPosition.x));
            Assert.That(upgrade.anchoredPosition.x, Is.EqualTo(inventory.anchoredPosition.x));
            Assert.That(upgrade.anchoredPosition.x - quest.anchoredPosition.x, Is.GreaterThan(quest.sizeDelta.x));
            Assert.That(quest.anchoredPosition.y - sell.anchoredPosition.y, Is.GreaterThan(quest.sizeDelta.y));
            Assert.That(quest.GetComponentInChildren<TMP_Text>(true).text, Is.EqualTo("퀘스트"));
            Assert.That(quest.GetComponent<SurfaceBaseButtonFeedback>(), Is.Not.Null);
            Assert.That(quest.GetComponent<Button>().targetGraphic.raycastTarget, Is.True);
        }

        [Test]
        public void SurfaceQuest_ReusesDetailsWithoutChangingProgressOrRewards()
        {
            var instance = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath));
            try
            {
                var panel = instance.GetComponent<SurfaceBaseQuestPanel>();
                // EditMode에서는 일반 MonoBehaviour의 활성화 콜백을 실행하지 않으므로 명시한다.
                var lifecycle = BindingFlags.Instance | BindingFlags.NonPublic;
                typeof(SurfaceBaseQuestPanel).GetMethod("OnDisable", lifecycle).Invoke(panel, null);
                typeof(SurfaceBaseQuestPanel).GetMethod("OnEnable", lifecycle).Invoke(panel, null);
                var questRoot = instance.transform.Find("SurfaceQuestRoot");
                var details = questRoot.Find("QuestDetailsPanel").gameObject;
                var view = questRoot.GetComponent<DemoObjectiveView>();
                var state = GameState.CreateNew();
                state.SetGold(321);
                state.SetDemoProgress(DemoObjectiveIds.PlaceLadder, 7, false);
                state.SetQuestRewardSettlement(DemoObjectiveIds.PlaceSupportInDanger, 6);
                Assert.That(panel.Open(state), Is.True);
                Assert.That(panel.IsOpen, Is.True);
                Assert.That(details.activeInHierarchy, Is.True);
                var title = (TMP_Text)new SerializedObject(view).FindProperty("detailsTitleText").objectReferenceValue;
                Assert.That(title.text, Is.EqualTo(DemoObjectiveCatalog.All[7].Title));
                view.OnDetailsNextClicked();
                Assert.That(title.text, Is.EqualTo(DemoObjectiveCatalog.All[8].Title));
                view.OnDetailsPrevClicked();
                Assert.That(title.text, Is.EqualTo(DemoObjectiveCatalog.All[7].Title));
                var detailsMotion = details.GetComponent<TerminalWindowOpenMotion>();
                detailsMotion.Advance(1f);
                view.OnDetailsDismissClicked();
                Assert.That(panel.IsOpen, Is.False);
                Assert.That(details.activeInHierarchy, Is.True, "The parent must survive the closing animation.");
                Assert.That(detailsMotion.IsClosing, Is.True);
                detailsMotion.Advance(1f);
                Assert.That(details.activeInHierarchy, Is.False);
                Assert.That(panel.Open(state), Is.True);
                Assert.That(panel.TryCloseTopWindow(PopupWindowSorting.Top), Is.True);
                Assert.That(panel.IsOpen, Is.False);
                Assert.That(state.Progress.CurrentObjectiveId, Is.EqualTo(DemoObjectiveIds.PlaceLadder));
                Assert.That(state.Progress.CompletedObjectives, Is.EqualTo(7));
                Assert.That(state.Progress.PendingQuestRewardId, Is.EqualTo(DemoObjectiveIds.PlaceSupportInDanger));
                Assert.That(state.Progress.QuestRewardSettledCount, Is.EqualTo(6));
                Assert.That(state.Player.Gold, Is.EqualTo(321));
                Assert.That(instance.activeSelf, Is.True);
            }
            finally
            {
                var panel = instance.GetComponent<SurfaceBaseQuestPanel>();
                if (panel != null)
                    typeof(SurfaceBaseQuestPanel).GetMethod("OnDisable", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(panel, null);
                Object.DestroyImmediate(instance);
            }
        }

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
        public void HeaderResourceText_IsTwentyThreePoint()
        {
            var content = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath).transform.Find("SurfaceBaseContent");
            Assert.That(content.Find("CargoText").GetComponent<TMP_Text>().fontSize, Is.EqualTo(23f));
            Assert.That(content.Find("GoldText").GetComponent<TMP_Text>().fontSize, Is.EqualTo(23f));
        }

        [Test]
        public void UpgradeModal_KeepsCardCanvasAboveDimmer()
        {
            var instance = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath));
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            try
            {
                var view = instance.GetComponent<SurfaceBaseView>();
                var modal = instance.transform.Find("SurfaceBaseContent/UpgradeModal").gameObject;
                modal.SetActive(true);
                var drag = modal.GetComponentInChildren<PopupWindowDrag>(true);
                // Edit Mode에서는 SetActive만으로 OnEnable이 돌지 않는다.
                typeof(PopupWindowDrag).GetMethod("OnEnable", flags).Invoke(drag, null);
                view.SetUpgradeVisible(true);
                var dimmer = modal.GetComponent<Canvas>();
                var card = drag.GetComponent<Canvas>();
                Assert.That(card, Is.Not.Null);
                Assert.That(card.overrideSorting, Is.True);
                Assert.That(card.sortingOrder, Is.GreaterThan(dimmer.sortingOrder));
            }
            finally
            {
                var drag = instance.GetComponentInChildren<PopupWindowDrag>(true);
                if (drag != null)
                {
                    typeof(PopupWindowDrag).GetMethod("OnDisable", flags).Invoke(drag, null);
                }

                instance.GetComponent<SurfaceBaseView>().SetUpgradeVisible(false);
                Object.DestroyImmediate(instance);
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

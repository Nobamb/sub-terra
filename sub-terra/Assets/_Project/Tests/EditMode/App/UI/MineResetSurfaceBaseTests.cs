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
            Assert.That(confirm.Find("ResetMineCard/Description").GetComponent<TMP_Text>(), Is.Not.Null);
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

            // X 버튼·장문 Body·장식용 빈 버튼이 남아 있지 않다. 닫기는 취소 버튼만 쓴다.
            Assert.That(card.Find("Close"), Is.Null);
            Assert.That(card.Find("Body"), Is.Null);
            Assert.That(card.GetComponentsInChildren<Button>(true).Length, Is.EqualTo(2));

            foreach (var name in new[] { "Title", "Description", "CostPanel", "ResetRow", "KeepRow", "TimerRow", "ConfirmButton", "CancelButton" })
            {
                var rect = card.Find(name) as RectTransform;
                Assert.That(rect, Is.Not.Null, name);
                Assert.That(Mathf.Abs(rect.anchoredPosition.x) + rect.sizeDelta.x / 2f,
                    Is.LessThan(metal.rectTransform.sizeDelta.x / 2f), name);
                Assert.That(Mathf.Abs(rect.anchoredPosition.y - metal.rectTransform.anchoredPosition.y) + rect.sizeDelta.y / 2f,
                    Is.LessThan(metal.rectTransform.sizeDelta.y / 2f), name);
            }

            // 비용 패널: 기존 골드 아이콘과 비용·잔액 텍스트.
            var panel = card.Find("CostPanel");
            Assert.That(AssetDatabase.GetAssetPath(panel.Find("AmountRow/GoldIcon").GetComponent<Image>().sprite),
                Is.EqualTo(MineResetSurfaceBaseLayoutBuilder.GoldIconPath));
            Assert.That(panel.Find("BalanceText").GetComponent<TMP_Text>(), Is.Not.Null);
            var costSize = panel.Find("AmountRow/CostText").GetComponent<TMP_Text>().fontSize;
            var titleSize = card.Find("Title").GetComponent<TMP_Text>().fontSize;
            Assert.That(costSize, Is.GreaterThan(titleSize));
            Assert.That(costSize, Is.LessThanOrEqualTo(titleSize * 1.4f));

            // 세 안내 행: 아이콘·제목·설명의 시작 x와 행 간격이 같다.
            var rows = new[] { "ResetRow", "KeepRow", "TimerRow" };
            var iconPaths = new[]
            {
                MineResetSurfaceBaseLayoutBuilder.ResetIconPath,
                MineResetSurfaceBaseLayoutBuilder.KeepIconPath,
                MineResetSurfaceBaseLayoutBuilder.TimerIconPath
            };
            var first = card.Find(rows[0]);
            for (var i = 0; i < rows.Length; i++)
            {
                var row = card.Find(rows[i]) as RectTransform;
                Assert.That(AssetDatabase.GetAssetPath(row.Find("Icon").GetComponent<Image>().sprite), Is.EqualTo(iconPaths[i]));
                foreach (var column in new[] { "Icon", "Title", "Desc" })
                {
                    Assert.That(LeftEdge(row.Find(column)), Is.EqualTo(LeftEdge(first.Find(column))).Within(0.01f), rows[i] + "/" + column);
                }

                Assert.That(row.anchoredPosition.y,
                    Is.EqualTo(MineResetSurfaceBaseLayoutBuilder.RowFirstY - MineResetSurfaceBaseLayoutBuilder.RowPitch * i)
                        .Within(0.01f));
            }

            // 하단 버튼: 왼쪽 취소, 오른쪽 실행. 높이와 y가 같다.
            var confirmRect = card.Find("ConfirmButton") as RectTransform;
            var cancelRect = card.Find("CancelButton") as RectTransform;
            Assert.That(cancelRect.anchoredPosition.y, Is.EqualTo(confirmRect.anchoredPosition.y));
            Assert.That(cancelRect.sizeDelta.y, Is.EqualTo(confirmRect.sizeDelta.y));
            Assert.That(cancelRect.anchoredPosition.x + cancelRect.sizeDelta.x / 2f,
                Is.LessThan(confirmRect.anchoredPosition.x - confirmRect.sizeDelta.x / 2f));

            // 호버 효과는 다른 팝업과 같은 MenuSpriteButtonSkin(off/on 페이드).
            foreach (var name in new[] { "ConfirmButton", "CancelButton" })
            {
                var button = card.Find(name).GetComponent<UnityEngine.UI.Button>();
                Assert.That(button.GetComponent<SubTerra.App.UI.MainMenu.MenuSpriteButtonSkin>(), Is.Not.Null, name);
                Assert.That(button.transition, Is.EqualTo(Selectable.Transition.None));
                Assert.That(button.GetComponent<UnityEngine.UI.Image>().color, Is.EqualTo(Color.white));
                Assert.That(AssetDatabase.GetAssetPath(button.GetComponent<UnityEngine.UI.Image>().sprite),
                    Is.EqualTo(MineResetSurfaceBaseLayoutBuilder.ButtonOffPath));
                var overlay = button.transform.Find(name == "ConfirmButton" ? "AccentRoot/HoverOverlay" : "HoverOverlay")
                    .GetComponent<Image>();
                Assert.That(AssetDatabase.GetAssetPath(overlay.sprite),
                    Is.EqualTo(MineResetSurfaceBaseLayoutBuilder.ButtonOnPath));
                Assert.That(overlay.raycastTarget, Is.False);
            }
            Assert.That(prefab.GetComponent<SurfaceBaseView>().HasRequiredReferences(), Is.True);
        }

        private static float LeftEdge(Transform transform)
        {
            var rect = (RectTransform)transform;
            return rect.anchoredPosition.x - rect.sizeDelta.x / 2f;
        }

        [TestCase(GameLanguage.Korean, 500, 750)]
        [TestCase(GameLanguage.Korean, 1000, 1000)]
        [TestCase(GameLanguage.English, 1000, 4000)]
        [TestCase(GameLanguage.English, int.MaxValue, int.MaxValue)]
        public void Confirmation_AffordableShowsCostBalanceAndFitsFrame(GameLanguage language, int fee, int gold)
        {
            var previous = LocalizationService.Current;
            var instance = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(
                MineResetSurfaceBaseLayoutBuilder.SurfaceBasePrefabPath));
            try
            {
                LocalizationService.SetLanguage(language);
                var view = instance.GetComponent<SurfaceBaseView>();
                view.SetMineResetConfirmVisible(true, gold, fee);
                var card = instance.transform.Find("ResetMineConfirm/ResetMineCard");
                var cost = card.Find("CostPanel/AmountRow/CostText").GetComponent<TMP_Text>();
                var balance = card.Find("CostPanel/BalanceText").GetComponent<TMP_Text>();
                Assert.That(cost.text, Is.EqualTo(fee + " G"));
                Assert.That(balance.text, Is.EqualTo(string.Format(LocalizationService.Get("mine_reset.confirm.balance"),
                    gold, gold - fee)));
                Assert.That(card.Find("Description").GetComponent<TMP_Text>().text,
                    Is.EqualTo(LocalizationService.Get("mine_reset.confirm.desc")));
                var yes = card.Find("ConfirmButton").GetComponent<Button>();
                Assert.That(yes.GetComponentInChildren<TMP_Text>().text,
                    Is.EqualTo(string.Format(LocalizationService.Get("mine_reset.confirm.create"), fee)));
                Assert.That(yes.interactable, Is.True);
                Assert.That(card.Find("ConfirmButton/AccentRoot").gameObject.activeSelf, Is.True);
                Assert.That(instance.transform.Find("SurfaceBaseContent/ResetMineButton/FeeLabel").GetComponent<TMP_Text>().text,
                    Is.EqualTo(fee + "G"));
                AssertTextsFit(card);
            }
            finally
            {
                Object.DestroyImmediate(instance);
                LocalizationService.SetLanguage(previous);
            }
        }

        [TestCase(GameLanguage.Korean)]
        [TestCase(GameLanguage.English)]
        public void Confirmation_InsufficientGoldShowsShortageAndBlocksConfirm(GameLanguage language)
        {
            var previous = LocalizationService.Current;
            var instance = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(
                MineResetSurfaceBaseLayoutBuilder.SurfaceBasePrefabPath));
            try
            {
                LocalizationService.SetLanguage(language);
                var view = instance.GetComponent<SurfaceBaseView>();
                view.SetMineResetConfirmVisible(true, 300, 500);
                var card = instance.transform.Find("ResetMineConfirm/ResetMineCard");
                var balance = card.Find("CostPanel/BalanceText").GetComponent<TMP_Text>();
                Assert.That(balance.text, Is.EqualTo(string.Format(LocalizationService.Get("mine_reset.confirm.shortage"),
                    300, 200)));
                Assert.That(balance.text, Does.Not.Contain("-"));
                Assert.That(card.Find("CostPanel/AmountRow/CostText").GetComponent<TMP_Text>().text, Is.EqualTo("500 G"));
                var yes = card.Find("ConfirmButton").GetComponent<Button>();
                Assert.That(yes.interactable, Is.False);
                Assert.That(card.Find("ConfirmButton/AccentRoot").gameObject.activeSelf, Is.False);
                // 취소는 부족 상태에서도 열려 있어야 한다.
                Assert.That(card.Find("CancelButton").GetComponent<Button>().interactable, Is.True);

                // busy 해제가 부족 상태의 실행 버튼을 다시 켜지 않는다.
                view.SetMineResetBusy(true);
                view.SetMineResetBusy(false);
                Assert.That(yes.interactable, Is.False);
                AssertTextsFit(card);

                view.SetMineResetConfirmVisible(true, 500, 500);
                Assert.That(yes.interactable, Is.True);
                Assert.That(card.Find("CostPanel/BalanceText").GetComponent<TMP_Text>().text,
                    Is.EqualTo(string.Format(LocalizationService.Get("mine_reset.confirm.balance"), 500, 0)));
            }
            finally
            {
                Object.DestroyImmediate(instance);
                LocalizationService.SetLanguage(previous);
            }
        }

        private static void AssertTextsFit(Transform card)
        {
            // 비용 줄은 HorizontalLayoutGroup이 크기를 정하므로 먼저 레이아웃을 확정한다.
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)card.Find("CostPanel/AmountRow"));
            foreach (var text in card.GetComponentsInChildren<TMP_Text>(false))
            {
                text.ForceMeshUpdate();
                // 자동 크기 텍스트는 최소 크기에서도 한 줄 폭·높이 안에 들어와야 한다.
                Assert.That(text.isTextOverflowing, Is.False, text.name + ": " + text.text);
            }
        }

        [Test]
        public void Confirmation_ConfirmAndCancelInvokeExistingEventsOnceAndHonorBusy()
        {
            var instance = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(
                MineResetSurfaceBaseLayoutBuilder.SurfaceBasePrefabPath));
            try
            {
                var view = instance.GetComponent<SurfaceBaseView>();
                var enable = typeof(SurfaceBaseView).GetMethod("OnEnable", BindingFlags.Instance | BindingFlags.NonPublic);
                // 중복 호출에도 리스너가 두 번 붙지 않는다.
                enable.Invoke(view, null);
                enable.Invoke(view, null);
                var card = instance.transform.Find("ResetMineConfirm/ResetMineCard");
                view.SetMineResetConfirmVisible(true, 3000, 500);
                var confirmed = 0;
                var cancelled = 0;
                view.ResetMineConfirmed += () => confirmed++;
                view.ResetMineCancelled += () => cancelled++;
                var yes = card.Find("ConfirmButton").GetComponent<Button>();
                var no = card.Find("CancelButton").GetComponent<Button>();

                view.SetMineResetBusy(true);
                Assert.That(yes.interactable, Is.False);
                Assert.That(no.interactable, Is.False);
                Assert.That(instance.transform.Find("SurfaceBaseContent/ResetMineButton").GetComponent<Button>().interactable, Is.False);
                yes.OnPointerClick(new PointerEventData(null));
                no.OnPointerClick(new PointerEventData(null));
                Assert.That(confirmed, Is.Zero);
                Assert.That(cancelled, Is.Zero);

                view.SetMineResetBusy(false);
                yes.OnPointerClick(new PointerEventData(null));
                Assert.That(confirmed, Is.EqualTo(1));
                no.OnPointerClick(new PointerEventData(null));
                Assert.That(cancelled, Is.EqualTo(1));
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
                Assert.That(LocalizationService.Get("mine_reset.confirm.balance"), Does.Contain("{1} G"));
                Assert.That(LocalizationService.Get("mine_reset.confirm.create"), Does.Contain("{0} G"));
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

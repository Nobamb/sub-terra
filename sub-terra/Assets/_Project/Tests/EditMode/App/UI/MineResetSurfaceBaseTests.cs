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
        private const string Content = "ResetMineConfirm/ResetMineCard/Content/";

        [Test]
        public void SurfaceBasePrefab_HasResetButtonAndInactiveLayeredPopup()
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
            Assert.That(reset.anchoredPosition.y, Is.EqualTo(-282f).Within(0.5f));
            Assert.That(reset.sizeDelta, Is.EqualTo(new Vector2(530f, 112f)));
            Assert.That(message.anchoredPosition.y, Is.EqualTo(-375f).Within(0.5f));
            Assert.That(confirm, Is.Not.Null);
            Assert.That(confirm.gameObject.activeSelf, Is.False);
            Assert.That(confirm.GetComponent<Canvas>().sortingOrder, Is.EqualTo(700));
            Assert.That(confirm.GetComponent<MineResetPopupMotion>(), Is.Not.Null);

            var card = confirm.Find("ResetMineCard") as RectTransform;
            Assert.That(card.anchoredPosition, Is.EqualTo(Vector2.zero));
            Assert.That(card.sizeDelta, Is.EqualTo(MineResetSurfaceBaseLayoutBuilder.CardSize));
            Assert.That(card.GetComponent<SubTerra.App.UI.PopupWindowDrag>(), Is.Null);
            Assert.That(card.GetComponentInChildren<SubTerra.App.UI.Tutorial.QuestClearPopupMotion>(true), Is.Null);

            // 컨셉 이미지를 한 장으로 깔지 않고 레이어별로 나눈다.
            Assert.That(card.Find("Body").GetComponent<RectMask2D>(), Is.Not.Null);
            AssertSprite(card, "Body/Panel", MineResetSurfaceBaseLayoutBuilder.PanelPath);
            AssertSprite(card, "Body/Cave", MineResetSurfaceBaseLayoutBuilder.CavePath);
            AssertSprite(card, "Body/Frame", MineResetSurfaceBaseLayoutBuilder.FramePath);
            AssertSprite(card, "Body/FrameFlash", MineResetSurfaceBaseLayoutBuilder.FrameGlowPath);
            AssertSprite(card, "Hex/HexScale/HexMine", MineResetSurfaceBaseLayoutBuilder.HexMinePath);
            AssertSprite(card, "Hex/HexScale/HexBorder", MineResetSurfaceBaseLayoutBuilder.HexBorderPath);
            AssertSprite(card, "Hex/HexScale/HexBorderGlow", MineResetSurfaceBaseLayoutBuilder.HexBorderGlowPath);
            AssertSprite(card, "Hex/HexScale/HexCrystalGlow", MineResetSurfaceBaseLayoutBuilder.HexCrystalGlowPath);
            AssertSprite(card, "Hex/HexRings", MineResetSurfaceBaseLayoutBuilder.HexRingsPath);
            AssertSprite(card, "Content/CostPanel", MineResetSurfaceBaseLayoutBuilder.CostPlatePath);
            AssertSprite(card, "Content/CostPanel/AmountRow/GoldIcon", MineResetSurfaceBaseLayoutBuilder.GoldIconPath);
            AssertSprite(card, "Content/ResetRow", MineResetSurfaceBaseLayoutBuilder.InfoPlatePath);
            AssertSprite(card, "Content/ResetRow/Icon", MineResetSurfaceBaseLayoutBuilder.ResetIconPath);
            AssertSprite(card, "Content/KeepRow/Icon", MineResetSurfaceBaseLayoutBuilder.KeepIconPath);
            AssertSprite(card, "Content/TimerRow", MineResetSurfaceBaseLayoutBuilder.TimerPlatePath);
            AssertSprite(card, "Content/TimerRow/Group/Icon", MineResetSurfaceBaseLayoutBuilder.TimerIconPath);
            Assert.That(card.Find("Body/CaveGlows").childCount, Is.EqualTo(MineResetSurfaceBaseLayoutBuilder.CaveGlowPaths.Length));
            Assert.That(card.Find("Content").GetComponent<CanvasGroup>(), Is.Not.Null);

            // 그림은 원본 비율 그대로(찌그러짐 없음). 육각 입구는 특히 확인한다.
            foreach (var image in card.GetComponentsInChildren<Image>(true))
            {
                if (image.sprite == null || image.preserveAspect) continue;
                // 등장용 가로 빛 줄은 일부러 길게 늘인다.
                if (image.name == "ScanLine" || image.name.StartsWith("Edge")) continue;
                var rect = image.rectTransform.rect;
                if (image.rectTransform.anchorMin != image.rectTransform.anchorMax) continue;
                var spriteAspect = image.sprite.rect.width / image.sprite.rect.height;
                Assert.That(rect.width / rect.height, Is.EqualTo(spriteAspect).Within(spriteAspect * 0.02f), image.name);
            }

            // X 버튼 없이 취소·실행 두 버튼만. 발광·장식 레이어는 입력을 가로채지 않는다.
            Assert.That(card.Find("Close"), Is.Null);
            var buttons = card.GetComponentsInChildren<Button>(true);
            Assert.That(buttons.Length, Is.EqualTo(2));
            foreach (var graphic in card.GetComponentsInChildren<Graphic>(true))
            {
                var isButtonFace = graphic.GetComponent<Button>() != null;
                Assert.That(graphic.raycastTarget, Is.EqualTo(isButtonFace), graphic.name);
            }

            // 주요 영역은 창 안에 있다.
            foreach (var name in new[] { "Title", "Description", "TitleDivider", "CostPanel", "ResetRow", "KeepRow", "TimerRow", "ConfirmButton", "CancelButton" })
            {
                var rect = card.Find("Content/" + name) as RectTransform;
                Assert.That(rect, Is.Not.Null, name);
                Assert.That(Mathf.Abs(rect.anchoredPosition.x) + rect.sizeDelta.x / 2f, Is.LessThan(card.sizeDelta.x / 2f), name);
                Assert.That(Mathf.Abs(rect.anchoredPosition.y) + rect.sizeDelta.y / 2f, Is.LessThan(card.sizeDelta.y / 2f), name);
            }

            // 위→아래 순서: 제목, 설명, 육각형, 비용, 초기화·유지(좌우), 탐사 시간, 버튼
            var y = new System.Func<string, float>(path => ((RectTransform)card.Find(path)).anchoredPosition.y);
            Assert.That(y("Content/Title"), Is.GreaterThan(y("Content/Description")));
            Assert.That(y("Content/Description"), Is.GreaterThan(y("Hex")));
            Assert.That(y("Hex"), Is.GreaterThan(y("Content/CostPanel")));
            Assert.That(y("Content/CostPanel"), Is.GreaterThan(y("Content/ResetRow")));
            Assert.That(y("Content/ResetRow"), Is.EqualTo(y("Content/KeepRow")));
            Assert.That(((RectTransform)card.Find("Content/ResetRow")).anchoredPosition.x, Is.LessThan(0f));
            Assert.That(((RectTransform)card.Find("Content/KeepRow")).anchoredPosition.x, Is.GreaterThan(0f));
            Assert.That(y("Content/ResetRow"), Is.GreaterThan(y("Content/TimerRow")));
            Assert.That(y("Content/TimerRow"), Is.GreaterThan(y("Content/CancelButton")));

            // 중앙 육각 이미지의 존재감: 창 너비의 30% 이상
            var hexMine = (RectTransform)card.Find("Hex/HexScale/HexMine");
            Assert.That(hexMine.sizeDelta.x, Is.GreaterThan(card.sizeDelta.x * 0.3f));

            // 하단 버튼: 왼쪽 취소, 오른쪽 실행. 같은 높이·y, 같은 hover 오버레이 방식.
            var confirmRect = card.Find("Content/ConfirmButton") as RectTransform;
            var cancelRect = card.Find("Content/CancelButton") as RectTransform;
            Assert.That(cancelRect.anchoredPosition.y, Is.EqualTo(confirmRect.anchoredPosition.y));
            Assert.That(cancelRect.anchoredPosition.x + cancelRect.sizeDelta.x / 2f,
                Is.LessThan(confirmRect.anchoredPosition.x - confirmRect.sizeDelta.x / 2f));
            foreach (var name in new[] { "ConfirmButton", "CancelButton" })
            {
                var button = card.Find("Content/" + name).GetComponent<Button>();
                Assert.That(button.GetComponent<SubTerra.App.UI.MainMenu.MenuSpriteButtonSkin>(), Is.Not.Null, name);
                Assert.That(button.transition, Is.EqualTo(Selectable.Transition.None));
                var overlay = button.transform.Find(name == "ConfirmButton" ? "AccentRoot/HoverOverlay" : "HoverOverlay")
                    .GetComponent<Image>();
                Assert.That(overlay.raycastTarget, Is.False);
                Assert.That(overlay.color.a, Is.Zero);
            }

            AssertSprite(card, "Content/ConfirmButton/AccentRoot/AccentGlow", MineResetSurfaceBaseLayoutBuilder.ButtonConfirmPath);
            AssertSprite(card, "Content/CancelButton", MineResetSurfaceBaseLayoutBuilder.ButtonCancelPath);
            Assert.That(prefab.GetComponent<SurfaceBaseView>().HasRequiredReferences(), Is.True);
        }

        [Test]
        public void Popup_MotionReferencesAreAllWiredAndPrefabRestsFullyOpen()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                MineResetSurfaceBaseLayoutBuilder.SurfaceBasePrefabPath);
            var confirm = prefab.transform.Find("ResetMineConfirm");
            var motion = new SerializedObject(confirm.GetComponent<MineResetPopupMotion>());
            var property = motion.GetIterator();
            property.NextVisible(true);
            while (property.NextVisible(false))
            {
                if (property.propertyType == SerializedPropertyType.ObjectReference)
                {
                    Assert.That(property.objectReferenceValue, Is.Not.Null, property.name);
                }
                else if (property.isArray && property.propertyType == SerializedPropertyType.Generic)
                {
                    Assert.That(property.arraySize, Is.GreaterThan(0), property.name);
                    for (var i = 0; i < property.arraySize; i++)
                    {
                        Assert.That(property.GetArrayElementAtIndex(i).objectReferenceValue, Is.Not.Null, property.name + i);
                    }
                }
            }

            Assert.That(new SerializedObject(prefab.GetComponent<SurfaceBaseView>())
                .FindProperty("resetMineConfirmMotion").objectReferenceValue, Is.Not.Null);

            // 저장 상태는 완전히 열린 정지 화면: 중간 상태가 남지 않는다.
            var card = (RectTransform)confirm.Find("ResetMineCard");
            Assert.That(((RectTransform)card.Find("Body")).sizeDelta, Is.EqualTo(card.sizeDelta));
            Assert.That(card.Find("Content").GetComponent<CanvasGroup>().alpha, Is.EqualTo(1f));
            Assert.That(card.Find("Hex/HexScale").localScale, Is.EqualTo(Vector3.one));
            Assert.That(card.localScale, Is.EqualTo(Vector3.one));
            Assert.That(confirm.GetComponent<CanvasGroup>().alpha, Is.EqualTo(1f));
        }

        private static void AssertSprite(Transform card, string path, string spritePath)
        {
            var target = card.Find(path);
            Assert.That(target, Is.Not.Null, path);
            var image = target.GetComponent<Image>();
            Assert.That(image, Is.Not.Null, path);
            Assert.That(AssetDatabase.GetAssetPath(image.sprite), Is.EqualTo(spritePath), path);
        }

        [TestCase(GameLanguage.Korean, 500, 750)]
        [TestCase(GameLanguage.Korean, 1000, 1000)]
        [TestCase(GameLanguage.Korean, 500, 3000)]
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
                var cost = instance.transform.Find(Content + "CostPanel/AmountRow/CostText").GetComponent<TMP_Text>();
                var balance = instance.transform.Find(Content + "CostPanel/BalanceText").GetComponent<TMP_Text>();
                Assert.That(cost.text, Is.EqualTo(fee.ToString("N0", System.Globalization.CultureInfo.InvariantCulture) + " G"));
                Assert.That(balance.text, Is.EqualTo(SurfaceBaseView.FormatGold("mine_reset.confirm.balance", gold, gold - fee)));
                Assert.That(instance.transform.Find(Content + "Description").GetComponent<TMP_Text>().text,
                    Is.EqualTo(LocalizationService.Get("mine_reset.confirm.desc")));
                var yes = instance.transform.Find(Content + "ConfirmButton").GetComponent<Button>();
                Assert.That(yes.GetComponentInChildren<TMP_Text>().text,
                    Is.EqualTo(SurfaceBaseView.FormatGold("mine_reset.confirm.create", fee)));
                Assert.That(yes.interactable, Is.True);
                Assert.That(instance.transform.Find(Content + "ConfirmButton/AccentRoot").gameObject.activeSelf, Is.True);
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

        [Test]
        public void Confirmation_KoreanTextsMatchConceptAndResetRules()
        {
            var previous = LocalizationService.Current;
            var instance = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(
                MineResetSurfaceBaseLayoutBuilder.SurfaceBasePrefabPath));
            try
            {
                LocalizationService.SetLanguage(GameLanguage.Korean);
                instance.GetComponent<SurfaceBaseView>().SetMineResetConfirmVisible(true, 3000, 500);
                var root = instance.transform;
                Assert.That(root.Find(Content + "Title").GetComponent<TMP_Text>().text, Is.EqualTo("새 광산 구역"));
                Assert.That(root.Find(Content + "CostPanel/AmountRow/CostText").GetComponent<TMP_Text>().text, Is.EqualTo("500 G"));
                Assert.That(root.Find(Content + "CostPanel/BalanceText").GetComponent<TMP_Text>().text,
                    Is.EqualTo("보유 3,000 G → 이용 후 2,500 G"));
                Assert.That(root.Find(Content + "ResetRow/Desc").GetComponent<TMP_Text>().text,
                    Is.EqualTo("채굴한 타일 · 지하 시설 · 붕괴 · 가스"));
                Assert.That(root.Find(Content + "KeepRow/Desc").GetComponent<TMP_Text>().text,
                    Is.EqualTo("업그레이드 · 심층 해금 · 보유 광물"));
                // 탐사 시간 문구는 실제 주기(MineResetService)에서 계산한다.
                var hours = Mathf.RoundToInt((float)(MineResetService.CycleDurationSeconds / 3600d));
                Assert.That(root.Find(Content + "TimerRow/Group/Desc").GetComponent<TMP_Text>().text,
                    Is.EqualTo(hours + "시간으로 다시 시작"));
                Assert.That(root.Find(Content + "ConfirmButton/Label").GetComponent<TMP_Text>().text, Is.EqualTo("새 광산 생성 · 500 G"));
                Assert.That(root.Find(Content + "CancelButton/Label").GetComponent<TMP_Text>().text, Is.EqualTo("취소"));
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
                var balance = instance.transform.Find(Content + "CostPanel/BalanceText").GetComponent<TMP_Text>();
                Assert.That(balance.text, Is.EqualTo(SurfaceBaseView.FormatGold("mine_reset.confirm.shortage", 300, 200)));
                Assert.That(balance.text, Does.Not.Contain("-"));
                Assert.That(instance.transform.Find(Content + "CostPanel/AmountRow/CostText").GetComponent<TMP_Text>().text, Is.EqualTo("500 G"));
                var yes = instance.transform.Find(Content + "ConfirmButton").GetComponent<Button>();
                Assert.That(yes.interactable, Is.False);
                Assert.That(instance.transform.Find(Content + "ConfirmButton/AccentRoot").gameObject.activeSelf, Is.False);
                // 취소는 부족 상태에서도 열려 있어야 한다.
                Assert.That(instance.transform.Find(Content + "CancelButton").GetComponent<Button>().interactable, Is.True);

                // busy 해제가 부족 상태의 실행 버튼을 다시 켜지 않는다.
                view.SetMineResetBusy(true);
                view.SetMineResetBusy(false);
                Assert.That(yes.interactable, Is.False);
                AssertTextsFit(card);

                view.SetMineResetConfirmVisible(true, 500, 500);
                Assert.That(yes.interactable, Is.True);
                Assert.That(balance.text, Is.EqualTo(SurfaceBaseView.FormatGold("mine_reset.confirm.balance", 500, 0)));
            }
            finally
            {
                Object.DestroyImmediate(instance);
                LocalizationService.SetLanguage(previous);
            }
        }

        private static void AssertTextsFit(Transform card)
        {
            // 비용·탐사 시간 줄은 HorizontalLayoutGroup이 크기를 정하므로 먼저 레이아웃을 확정한다.
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)card.Find("Content/CostPanel/AmountRow"));
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)card.Find("Content/TimerRow/Group"));
            foreach (var text in card.GetComponentsInChildren<TMP_Text>(false))
            {
                text.ForceMeshUpdate();
                // 자동 크기 텍스트는 최소 크기에서도 한 줄 폭·높이 안에 들어와야 한다.
                Assert.That(text.isTextOverflowing, Is.False, text.name + ": " + text.text);
            }

            var amount = (RectTransform)card.Find("Content/CostPanel/AmountRow");
            var group = (RectTransform)card.Find("Content/TimerRow/Group");
            Assert.That(LayoutUtility.GetPreferredWidth(amount), Is.LessThanOrEqualTo(amount.rect.width + 0.5f));
            Assert.That(LayoutUtility.GetPreferredWidth(group), Is.LessThanOrEqualTo(group.rect.width + 0.5f));
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
                view.SetMineResetConfirmVisible(true, 3000, 500);
                var confirmed = 0;
                var cancelled = 0;
                view.ResetMineConfirmed += () => confirmed++;
                view.ResetMineCancelled += () => cancelled++;
                var yes = instance.transform.Find(Content + "ConfirmButton").GetComponent<Button>();
                var no = instance.transform.Find(Content + "CancelButton").GetComponent<Button>();

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
        public void Motion_CloseHidesFromViewImmediatelyAndReopenRestartsCleanly()
        {
            var instance = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(
                MineResetSurfaceBaseLayoutBuilder.SurfaceBasePrefabPath));
            try
            {
                var confirm = instance.transform.Find("ResetMineConfirm");
                confirm.gameObject.SetActive(true);
                var motion = confirm.GetComponent<MineResetPopupMotion>();
                var closed = 0;

                // 숨김 상태에서 닫기 요청은 즉시 완료된다.
                motion.ResetHidden();
                motion.PlayClose(() => closed++);
                Assert.That(closed, Is.EqualTo(1));

                motion.PlayOpen();
                Assert.That(motion.IsClosing, Is.False);
                // 열자마자(0초)는 가로 빛만 있고 본문은 보이지 않으며 조작도 막힌다.
                var card = confirm.Find("ResetMineCard");
                Assert.That(card.Find("Content").GetComponent<CanvasGroup>().alpha, Is.Zero);
                Assert.That(card.Find("Content").GetComponent<CanvasGroup>().interactable, Is.False);
                Assert.That(((RectTransform)card.Find("Body")).sizeDelta.y, Is.EqualTo(MineResetPopupTimeline.RevealMinHeight));
                // 텍스트 레이어는 크기를 바꾸지 않는다(찌그러짐 없음).
                Assert.That(card.Find("Content").localScale, Is.EqualTo(Vector3.one));

                motion.PlayClose(() => closed++);
                Assert.That(motion.IsClosing, Is.True);
                Assert.That(confirm.GetComponent<CanvasGroup>().interactable, Is.False);
                Assert.That(closed, Is.EqualTo(1));
                // 닫는 중 두 번째 닫기 요청은 무시된다.
                motion.PlayClose(() => closed += 10);

                // 닫는 중 다시 열면 이전 닫기 콜백은 버리고 처음부터 연다.
                motion.PlayOpen();
                Assert.That(motion.IsClosing, Is.False);
                Assert.That(motion.OpenTime, Is.Zero);
                Assert.That(confirm.GetComponent<CanvasGroup>().interactable, Is.True);
                Assert.That(closed, Is.EqualTo(1));

                motion.SnapOpen();
                Assert.That(card.Find("Content").GetComponent<CanvasGroup>().alpha, Is.EqualTo(1f));
                Assert.That(card.Find("Hex/HexScale").localScale, Is.EqualTo(Vector3.one));
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
                Assert.That(LocalizationService.Get("mine_reset.confirm.balance"), Does.Contain("{1:N0} G"));
                Assert.That(LocalizationService.Get("mine_reset.confirm.create"), Does.Contain("{0:N0} G"));
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
            Assert.That(text, Does.Not.Contain("_SDF.asset\", ImportAssetOptions"));
        }

        [Test]
        public void Fee_IsSingleSharedConstant()
        {
            Assert.That(MineResetService.FeeGold, Is.EqualTo(500));
        }
    }
}

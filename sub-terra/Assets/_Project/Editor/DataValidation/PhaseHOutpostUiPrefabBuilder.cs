using System.Collections.Generic;
using System.IO;
using SubTerra.App.Core.Data;
using SubTerra.App.UI.Outpost;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SubTerra.App.Editor.DataValidation
{
    /// <summary>시설별 역할이 분리된 전진기지 상호작용 패널을 생성한다.</summary>
    public static class PhaseHOutpostUiPrefabBuilder
    {
        private const string PrefabPath = "Assets/_Project/Prefabs/UI/OutpostPanel.prefab";
        private const string InputActionsPath = "Assets/Settings/InputSystem_Actions.inputactions";
        private const string BuildFlagPath = "Temp/subterra-build-phaseh-outpost.flag";
        private const string BuildResultPath = "Temp/subterra-build-phaseh-outpost.done";

        [InitializeOnLoadMethod]
        private static void WatchBuildFlag()
        {
            EditorApplication.update += PollBuildFlag;
        }

        private static void PollBuildFlag()
        {
            if (!File.Exists(BuildFlagPath))
            {
                return;
            }

            File.Delete(BuildFlagPath);
            File.WriteAllText(BuildResultPath, BuildPrefab());
        }

        [MenuItem("SubTerra/UI/Build Prompt-B 52 Facility Interaction Panel")]
        public static void BuildFromMenu()
        {
            Debug.Log("[SubTerra] " + BuildPrefab());
        }

        public static string BuildPrefab()
        {
            var root = new GameObject("OutpostPanel", typeof(RectTransform));
            var rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.sizeDelta = new Vector2(760f, 680f);

            var panelRoot = new GameObject("PanelRoot", typeof(RectTransform), typeof(Image));
            panelRoot.transform.SetParent(root.transform, false);
            StretchFull(panelRoot.GetComponent<RectTransform>());
            panelRoot.GetComponent<Image>().color = new Color(0.045f, 0.07f, 0.1f, 0.97f);

            var title = CreateText(
                panelRoot.transform,
                "Title",
                new Vector2(24f, -18f),
                new Vector2(710f, 42f),
                30f,
                string.Empty);
            title.fontStyle = FontStyles.Bold;

            var coreRoot = CreateLayer(panelRoot.transform, "CoreRoot");
            var power = CreateText(coreRoot.transform, "PowerText",
                new Vector2(24f, -70f), new Vector2(710f, 36f), 22f, "전력 상태 없음");
            var checkpoint = CreateText(coreRoot.transform, "CheckpointText",
                new Vector2(24f, -112f), new Vector2(710f, 34f), 18f, "체크포인트 없음");
            CreateText(coreRoot.transform, "FacilitiesLabel",
                new Vector2(24f, -158f), new Vector2(710f, 32f), 20f, "주변 활성 시설");
            var facilities = CreateScrollText(
                coreRoot.transform,
                "FacilitiesScroll",
                new Vector2(24f, -196f),
                new Vector2(712f, 390f),
                "활성화된 주변 시설 없음");

            var chargerRoot = CreateLayer(panelRoot.transform, "ChargerRoot");
            var chargerMessage = CreateText(chargerRoot.transform, "ChargerMessage",
                new Vector2(80f, -180f), new Vector2(600f, 170f), 25f,
                "연결된 전력망을 확인했습니다.\n플레이어 전력을 최대치까지 충전합니다.");
            chargerMessage.alignment = TextAlignmentOptions.Center;

            var settlementRoot = CreateLayer(panelRoot.transform, "SettlementRoot");
            CreateText(settlementRoot.transform, "SettlementLabel",
                new Vector2(24f, -76f), new Vector2(710f, 32f), 20f,
                "판매할 자원 · Surface Base와 동일한 단가 적용");
            var settlementCargo = CreateScrollText(
                settlementRoot.transform,
                "SettlementCargoScroll",
                new Vector2(24f, -116f),
                new Vector2(712f, 190f),
                "판매할 자원이 없습니다.");

            var storageRoot = CreateLayer(panelRoot.transform, "StorageRoot");
            var playerCargo = CreateText(storageRoot.transform, "PlayerCargoText",
                new Vector2(24f, -82f), new Vector2(710f, 84f), 18f, "보유 자원: 비어 있음");
            playerCargo.textWrappingMode = TextWrappingModes.Normal;
            var storageCargo = CreateText(storageRoot.transform, "StorageCargoText",
                new Vector2(24f, -180f), new Vector2(710f, 84f), 18f, "보관 자원: 비어 있음");
            storageCargo.textWrappingMode = TextWrappingModes.Normal;

            var binder = root.AddComponent<OutpostPanelBinder>();
            var view = root.AddComponent<OutpostPanelView>();
            var operationButtons = new List<Button>();

            var transactionRoot = CreateLayer(panelRoot.transform, "TransactionRoot");
            var selected = CreateText(transactionRoot.transform, "SelectedMineralText",
                new Vector2(24f, -326f), new Vector2(710f, 36f), 18f, "자원을 선택하세요.");
            CreateMineralButton(transactionRoot.transform, binder, DataIds.Minerals.Copper, "구리",
                new Vector2(24f, -374f));
            CreateMineralButton(transactionRoot.transform, binder, DataIds.Minerals.Iron, "철",
                new Vector2(148f, -374f));
            CreateMineralButton(transactionRoot.transform, binder, DataIds.Minerals.Lithium, "리튬",
                new Vector2(272f, -374f));

            var quantity = CreateInputField(transactionRoot.transform, "QuantityInput",
                new Vector2(24f, -426f), new Vector2(150f, 40f), "직접 입력");
            quantity.text = "1";
            var quantityOne = CreateButton(transactionRoot.transform, "Quantity1Button",
                new Vector2(190f, -426f), new Vector2(86f, 40f), "1개");
            var quantityFive = CreateButton(transactionRoot.transform, "Quantity5Button",
                new Vector2(288f, -426f), new Vector2(86f, 40f), "5개");
            var quantityTen = CreateButton(transactionRoot.transform, "Quantity10Button",
                new Vector2(386f, -426f), new Vector2(86f, 40f), "10개");
            UnityEventTools.AddPersistentListener(quantityOne.onClick, binder.SetQuantityOne);
            UnityEventTools.AddPersistentListener(quantityFive.onClick, binder.SetQuantityFive);
            UnityEventTools.AddPersistentListener(quantityTen.onClick, binder.SetQuantityTen);

            var storageActionsRoot = CreateLayer(transactionRoot.transform, "StorageActionsRoot");
            var deposit = CreateButton(storageActionsRoot.transform, "DepositButton",
                new Vector2(24f, -482f), new Vector2(220f, 44f), "선택 수량 보관");
            var withdraw = CreateButton(storageActionsRoot.transform, "WithdrawButton",
                new Vector2(260f, -482f), new Vector2(220f, 44f), "선택 수량 꺼내기");
            UnityEventTools.AddPersistentListener(deposit.onClick, binder.Deposit);
            UnityEventTools.AddPersistentListener(withdraw.onClick, binder.Withdraw);
            operationButtons.Add(deposit);
            operationButtons.Add(withdraw);

            var settlementActionsRoot = CreateLayer(transactionRoot.transform, "SettlementActionsRoot");
            var sellSelected = CreateButton(settlementActionsRoot.transform, "SellSelectedButton",
                new Vector2(24f, -482f), new Vector2(220f, 44f), "선택 수량 판매");
            var sellAll = CreateButton(settlementActionsRoot.transform, "SellAllButton",
                new Vector2(260f, -482f), new Vector2(220f, 44f), "모두 판매");
            UnityEventTools.AddPersistentListener(sellSelected.onClick, binder.SellSelected);
            UnityEventTools.AddPersistentListener(sellAll.onClick, binder.SettlePlayerCargo);
            operationButtons.Add(sellSelected);
            operationButtons.Add(sellAll);

            var result = CreateText(panelRoot.transform, "ResultText",
                new Vector2(24f, -548f), new Vector2(710f, 58f), 19f, string.Empty);
            result.textWrappingMode = TextWrappingModes.Normal;

            var tutorialRoot = CreateTutorial(panelRoot.transform, binder);

            var viewObject = new SerializedObject(view);
            viewObject.FindProperty("panelRoot").objectReferenceValue = panelRoot;
            viewObject.FindProperty("titleText").objectReferenceValue = title;
            viewObject.FindProperty("coreRoot").objectReferenceValue = coreRoot;
            viewObject.FindProperty("chargerRoot").objectReferenceValue = chargerRoot;
            viewObject.FindProperty("settlementRoot").objectReferenceValue = settlementRoot;
            viewObject.FindProperty("storageRoot").objectReferenceValue = storageRoot;
            viewObject.FindProperty("transactionRoot").objectReferenceValue = transactionRoot;
            viewObject.FindProperty("storageActionsRoot").objectReferenceValue = storageActionsRoot;
            viewObject.FindProperty("settlementActionsRoot").objectReferenceValue = settlementActionsRoot;
            viewObject.FindProperty("powerText").objectReferenceValue = power;
            viewObject.FindProperty("facilitiesText").objectReferenceValue = facilities;
            viewObject.FindProperty("playerCargoText").objectReferenceValue = playerCargo;
            viewObject.FindProperty("storageCargoText").objectReferenceValue = storageCargo;
            viewObject.FindProperty("settlementCargoText").objectReferenceValue = settlementCargo;
            viewObject.FindProperty("checkpointText").objectReferenceValue = checkpoint;
            viewObject.FindProperty("selectedMineralText").objectReferenceValue = selected;
            viewObject.FindProperty("resultText").objectReferenceValue = result;
            viewObject.FindProperty("tutorialRoot").objectReferenceValue = tutorialRoot;
            var buttonsProperty = viewObject.FindProperty("operationButtons");
            buttonsProperty.arraySize = operationButtons.Count;
            for (var i = 0; i < operationButtons.Count; i++)
            {
                buttonsProperty.GetArrayElementAtIndex(i).objectReferenceValue = operationButtons[i];
            }
            viewObject.ApplyModifiedPropertiesWithoutUndo();

            var binderObject = new SerializedObject(binder);
            binderObject.FindProperty("view").objectReferenceValue = view;
            binderObject.FindProperty("quantityInput").objectReferenceValue = quantity;
            binderObject.FindProperty("inputActions").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
            binderObject.ApplyModifiedPropertiesWithoutUndo();

            coreRoot.SetActive(false);
            chargerRoot.SetActive(false);
            settlementRoot.SetActive(false);
            storageRoot.SetActive(false);
            transactionRoot.SetActive(false);
            panelRoot.SetActive(false);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            var prefabView = prefab != null ? prefab.GetComponent<OutpostPanelView>() : null;
            return "OutpostPanel prefab=" + (prefab != null)
                + " refs=" + (prefabView != null && prefabView.HasRequiredReferences());
        }

        private static GameObject CreateLayer(Transform parent, string name)
        {
            var layer = new GameObject(name, typeof(RectTransform));
            layer.transform.SetParent(parent, false);
            StretchFull(layer.GetComponent<RectTransform>());
            return layer;
        }

        private static GameObject CreateTutorial(Transform parent, OutpostPanelBinder binder)
        {
            var tutorialRoot = new GameObject("TutorialRoot", typeof(RectTransform), typeof(Image));
            tutorialRoot.transform.SetParent(parent, false);
            var rect = tutorialRoot.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 20f);
            rect.sizeDelta = new Vector2(710f, 64f);
            tutorialRoot.GetComponent<Image>().color = new Color(0.11f, 0.2f, 0.25f, 1f);
            CreateText(tutorialRoot.transform, "TutorialText",
                new Vector2(14f, -10f), new Vector2(570f, 44f), 15f,
                "시설 가까이에서 E키를 눌러 각 시설의 기능을 사용합니다.");
            var dismiss = CreateButton(tutorialRoot.transform, "DismissButton",
                new Vector2(590f, -13f), new Vector2(100f, 38f), "확인");
            UnityEventTools.AddPersistentListener(dismiss.onClick, binder.DismissTutorial);
            tutorialRoot.SetActive(false);
            return tutorialRoot;
        }

        private static TextMeshProUGUI CreateScrollText(
            Transform parent,
            string name,
            Vector2 position,
            Vector2 size,
            string value)
        {
            var root = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            root.transform.SetParent(parent, false);
            var rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = rootRect.anchorMax = new Vector2(0f, 1f);
            rootRect.pivot = new Vector2(0f, 1f);
            rootRect.anchoredPosition = position;
            rootRect.sizeDelta = size;
            root.GetComponent<Image>().color = new Color(0.025f, 0.04f, 0.06f, 0.72f);

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            viewport.transform.SetParent(root.transform, false);
            var viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = new Vector2(14f, 12f);
            viewportRect.offsetMax = new Vector2(-28f, -12f);

            var content = new GameObject(
                "Content",
                typeof(RectTransform),
                typeof(ContentSizeFitter),
                typeof(TextMeshProUGUI));
            content.transform.SetParent(viewport.transform, false);
            var contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = Vector2.zero;

            var text = content.GetComponent<TextMeshProUGUI>();
            ApplyTextStyle(text, 18f, value);
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Overflow;
            var fitter = content.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scrollbarRoot = new GameObject(
                "Scrollbar Vertical",
                typeof(RectTransform),
                typeof(Image),
                typeof(Scrollbar));
            scrollbarRoot.transform.SetParent(root.transform, false);
            var scrollbarRect = scrollbarRoot.GetComponent<RectTransform>();
            scrollbarRect.anchorMin = new Vector2(1f, 0f);
            scrollbarRect.anchorMax = Vector2.one;
            scrollbarRect.pivot = Vector2.one;
            scrollbarRect.offsetMin = new Vector2(-18f, 4f);
            scrollbarRect.offsetMax = new Vector2(-4f, -4f);
            scrollbarRoot.GetComponent<Image>().color = new Color(0.08f, 0.12f, 0.16f, 0.9f);
            var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handle.transform.SetParent(scrollbarRoot.transform, false);
            StretchFull(handle.GetComponent<RectTransform>());
            handle.GetComponent<Image>().color = new Color(0.3f, 0.62f, 0.72f, 1f);
            var scrollbar = scrollbarRoot.GetComponent<Scrollbar>();
            scrollbar.handleRect = handle.GetComponent<RectTransform>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;

            var scroll = root.GetComponent<ScrollRect>();
            scroll.viewport = viewportRect;
            scroll.content = contentRect;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 28f;
            scroll.verticalScrollbar = scrollbar;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
            scroll.verticalScrollbarSpacing = 4f;
            return text;
        }

        private static void CreateMineralButton(
            Transform parent,
            OutpostPanelBinder binder,
            string mineralId,
            string label,
            Vector2 position)
        {
            var button = CreateButton(parent, "Select_" + mineralId, position, new Vector2(110f, 38f), label);
            button.gameObject.AddComponent<OutpostMineralSelectButton>().EditorSet(mineralId, binder);
        }

        private static TMP_InputField CreateInputField(
            Transform parent,
            string name,
            Vector2 position,
            Vector2 size,
            string placeholderValue)
        {
            var root = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
            root.transform.SetParent(parent, false);
            var rect = root.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            root.GetComponent<Image>().color = new Color(0.12f, 0.16f, 0.2f, 1f);

            var viewport = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));
            viewport.transform.SetParent(root.transform, false);
            var viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = new Vector2(8f, 4f);
            viewportRect.offsetMax = new Vector2(-8f, -4f);

            var placeholder = CreateText(viewport.transform, "Placeholder", Vector2.zero, size, 17f, placeholderValue);
            placeholder.color = new Color(1f, 1f, 1f, 0.45f);
            StretchFull(placeholder.rectTransform);
            var text = CreateText(viewport.transform, "Text", Vector2.zero, size, 17f, string.Empty);
            StretchFull(text.rectTransform);

            var input = root.GetComponent<TMP_InputField>();
            input.textViewport = viewportRect;
            input.textComponent = text;
            input.placeholder = placeholder;
            input.contentType = TMP_InputField.ContentType.IntegerNumber;
            return input;
        }

        private static TextMeshProUGUI CreateText(
            Transform parent,
            string name,
            Vector2 position,
            Vector2 size,
            float fontSize,
            string value)
        {
            var root = new GameObject(name, typeof(RectTransform));
            root.transform.SetParent(parent, false);
            var rect = root.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var text = root.AddComponent<TextMeshProUGUI>();
            ApplyTextStyle(text, fontSize, value);
            return text;
        }

        private static void ApplyTextStyle(TextMeshProUGUI text, float fontSize, string value)
        {
            var fontAsset = KoreanFontAssetUtility.GetOrCreateKoreanFontAsset();
            if (fontAsset != null)
            {
                text.font = fontAsset;
            }

            text.text = value;
            text.fontSize = fontSize;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.TopLeft;
            text.raycastTarget = false;
        }

        private static Button CreateButton(
            Transform parent,
            string name,
            Vector2 position,
            Vector2 size,
            string label)
        {
            var root = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            root.transform.SetParent(parent, false);
            var rect = root.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            root.GetComponent<Image>().color = new Color(0.15f, 0.28f, 0.38f, 1f);

            var text = CreateText(root.transform, "Label", Vector2.zero, size, 17f, label);
            StretchFull(text.rectTransform);
            text.alignment = TextAlignmentOptions.Center;
            return root.GetComponent<Button>();
        }

        private static void StretchFull(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}

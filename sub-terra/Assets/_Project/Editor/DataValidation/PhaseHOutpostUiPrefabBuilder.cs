using System.Collections.Generic;
using System.IO;
using SubTerra.App.Core.Data;
using SubTerra.App.UI.Outpost;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.Editor.DataValidation
{
    /// <summary>Phase H 전진기지 패널을 Editor API로 생성하고 참조를 연결한다.</summary>
    public static class PhaseHOutpostUiPrefabBuilder
    {
        private const string PrefabPath = "Assets/_Project/Prefabs/UI/OutpostPanel.prefab";
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

        [MenuItem("SubTerra/UI/Build Phase H Outpost UI")]
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
                30,
                "전진기지");
            title.fontStyle = FontStyles.Bold;

            var power = CreateText(panelRoot.transform, "PowerText",
                new Vector2(24f, -70f), new Vector2(710f, 36f), 22, "전력 상태 없음");
            var facilities = CreateText(panelRoot.transform, "FacilitiesText",
                new Vector2(24f, -112f), new Vector2(710f, 105f), 18, "연결된 시설 없음");
            facilities.textWrappingMode = TextWrappingModes.Normal;

            var playerCargo = CreateText(panelRoot.transform, "PlayerCargoText",
                new Vector2(24f, -230f), new Vector2(710f, 46f), 18, "플레이어: 비어 있음");
            var storageCargo = CreateText(panelRoot.transform, "StorageCargoText",
                new Vector2(24f, -278f), new Vector2(710f, 46f), 18, "보관함: 비어 있음");
            var checkpoint = CreateText(panelRoot.transform, "CheckpointText",
                new Vector2(24f, -326f), new Vector2(710f, 34f), 18, "체크포인트 없음");
            var selected = CreateText(panelRoot.transform, "SelectedMineralText",
                new Vector2(24f, -372f), new Vector2(280f, 34f), 18, "선택: 없음");

            var binder = root.AddComponent<OutpostPanelBinder>();
            var view = root.AddComponent<OutpostPanelView>();
            var operationButtons = new List<Button>();

            CreateMineralButton(panelRoot.transform, binder, DataIds.Minerals.Copper, "구리",
                new Vector2(320f, -368f));
            CreateMineralButton(panelRoot.transform, binder, DataIds.Minerals.Iron, "철",
                new Vector2(430f, -368f));
            CreateMineralButton(panelRoot.transform, binder, DataIds.Minerals.Lithium, "리튬",
                new Vector2(540f, -368f));

            var quantity = CreateInputField(panelRoot.transform, "QuantityInput",
                new Vector2(24f, -418f), new Vector2(150f, 38f), "수량");
            var deposit = CreateButton(panelRoot.transform, "DepositButton",
                new Vector2(188f, -418f), new Vector2(150f, 38f), "보관");
            var withdraw = CreateButton(panelRoot.transform, "WithdrawButton",
                new Vector2(350f, -418f), new Vector2(150f, 38f), "꺼내기");
            var charge = CreateButton(panelRoot.transform, "ChargeButton",
                new Vector2(512f, -418f), new Vector2(150f, 38f), "충전");
            operationButtons.Add(deposit);
            operationButtons.Add(withdraw);
            operationButtons.Add(charge);
            UnityEventTools.AddPersistentListener(deposit.onClick, binder.Deposit);
            UnityEventTools.AddPersistentListener(withdraw.onClick, binder.Withdraw);
            UnityEventTools.AddPersistentListener(charge.onClick, binder.Charge);

            var settlePlayer = CreateButton(panelRoot.transform, "SettlePlayerButton",
                new Vector2(24f, -474f), new Vector2(310f, 42f), "플레이어 화물 정산");
            var settleStorage = CreateButton(panelRoot.transform, "SettleStorageButton",
                new Vector2(350f, -474f), new Vector2(312f, 42f), "보관함 정산");
            operationButtons.Add(settlePlayer);
            operationButtons.Add(settleStorage);
            UnityEventTools.AddPersistentListener(settlePlayer.onClick, binder.SettlePlayerCargo);
            UnityEventTools.AddPersistentListener(settleStorage.onClick, binder.SettleStorage);

            var result = CreateText(panelRoot.transform, "ResultText",
                new Vector2(24f, -530f), new Vector2(710f, 54f), 18, string.Empty);

            var tutorialRoot = new GameObject("TutorialRoot", typeof(RectTransform), typeof(Image));
            tutorialRoot.transform.SetParent(panelRoot.transform, false);
            var tutorialRect = tutorialRoot.GetComponent<RectTransform>();
            tutorialRect.anchorMin = tutorialRect.anchorMax = new Vector2(0.5f, 0f);
            tutorialRect.pivot = new Vector2(0.5f, 0f);
            tutorialRect.anchoredPosition = new Vector2(0f, 22f);
            tutorialRect.sizeDelta = new Vector2(710f, 72f);
            tutorialRoot.GetComponent<Image>().color = new Color(0.11f, 0.2f, 0.25f, 1f);
            CreateText(tutorialRoot.transform, "TutorialText",
                new Vector2(14f, -12f), new Vector2(570f, 48f), 16,
                "첫 전진기지가 설치되었습니다. 충전·보관·정산 후 체크포인트가 자동 저장됩니다.");
            var dismiss = CreateButton(tutorialRoot.transform, "DismissButton",
                new Vector2(590f, -17f), new Vector2(100f, 38f), "확인");
            UnityEventTools.AddPersistentListener(dismiss.onClick, binder.DismissTutorial);
            tutorialRoot.SetActive(false);

            var viewObject = new SerializedObject(view);
            viewObject.FindProperty("panelRoot").objectReferenceValue = panelRoot;
            viewObject.FindProperty("powerText").objectReferenceValue = power;
            viewObject.FindProperty("facilitiesText").objectReferenceValue = facilities;
            viewObject.FindProperty("playerCargoText").objectReferenceValue = playerCargo;
            viewObject.FindProperty("storageCargoText").objectReferenceValue = storageCargo;
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
            binderObject.ApplyModifiedPropertiesWithoutUndo();

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

        private static void CreateMineralButton(
            Transform parent,
            OutpostPanelBinder binder,
            string mineralId,
            string label,
            Vector2 position)
        {
            var button = CreateButton(parent, "Select_" + mineralId, position, new Vector2(100f, 34f), label);
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

            var placeholder = CreateText(viewport.transform, "Placeholder", Vector2.zero, size, 17, placeholderValue);
            placeholder.color = new Color(1f, 1f, 1f, 0.45f);
            StretchFull(placeholder.rectTransform);
            var text = CreateText(viewport.transform, "Text", Vector2.zero, size, 17, string.Empty);
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
            text.text = value;
            text.fontSize = fontSize;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.TopLeft;
            text.raycastTarget = false;
            return text;
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

            var text = CreateText(root.transform, "Label", Vector2.zero, size, 17, label);
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

#if UNITY_EDITOR
using System;
using SubTerra.App.Integration;
using SubTerra.App.UI.EmergencyEscape;
using SubTerra.App.UI.HUD;
using SubTerra.App.UI.Inventory;
using SubTerra.Gameplay.Building;
using SubTerra.Gameplay.Power;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SubTerra.App.Editor.DataValidation
{
    /// <summary>
    /// prompt-B 47-1: 긴급 탈출 포탈 2x2 시각화와 E키 목적지 선택 창만 수정한다.
    /// </summary>
    public static class PromptB47_1EmergencyEscapePortalBuilder
    {
        public const string IntegrationScenePath =
            "Assets/_Project/Scenes/App/Mine_Demo_Integration.unity";
        public const string PortalPrefabPath =
            "Assets/_Project/Prefabs/Gameplay/Buildings/EmergencyEscapePortal.prefab";
        public const string PanelPrefabPath =
            "Assets/_Project/Prefabs/UI/EmergencyEscapePanel.prefab";

        private const string InputActionsPath =
            "Assets/Settings/InputSystem_Actions.inputactions";

        [MenuItem("SubTerra/UI/Build Prompt-B 47-1 Emergency Escape Portal")]
        public static void BuildFromMenu()
        {
            Debug.Log("[SubTerra] " + Build());
        }

        public static string Build()
        {
            BuildPortalPrefab();
            BuildPanelPrefab();
            WireIntegrationScene();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return "Prompt-B 47-1 portal 2x2 visual + destination panel wired.";
        }

        private static GameObject BuildPortalPrefab()
        {
            var root = new GameObject(
                "EmergencyEscapePortal",
                typeof(BoxCollider2D),
                typeof(BuildingInstance),
                typeof(PowerNode),
                typeof(EmergencyEscapePortal));
            try
            {
                // 2x2 footprint 중심에 배치되므로 로컬 원점 기준 2x2 네모로 맞춘다.
                var zone = root.GetComponent<BoxCollider2D>();
                zone.isTrigger = true;
                zone.size = new Vector2(2f, 2f);
                zone.offset = Vector2.zero;

                var power = root.GetComponent<PowerNode>();
                power.Configure(null, false, 0, 30, PowerPriority.Critical);

                var sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
                CreateVisual(
                    root.transform,
                    "OuterFrame",
                    sprite,
                    Vector3.zero,
                    new Vector2(2f, 2f),
                    new Color(0.1f, 0.8f, 0.95f, 0.92f),
                    5);
                CreateVisual(
                    root.transform,
                    "PortalField",
                    sprite,
                    new Vector3(0f, 0f, -0.01f),
                    new Vector2(1.6f, 1.6f),
                    new Color(0.08f, 0.16f, 0.35f, 0.78f),
                    6);

                var portalSo = new SerializedObject(root.GetComponent<EmergencyEscapePortal>());
                portalSo.FindProperty("inputActions").objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
                portalSo.FindProperty("powerNode").objectReferenceValue = power;
                portalSo.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, PortalPrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }

            return AssetDatabase.LoadAssetAtPath<GameObject>(PortalPrefabPath);
        }

        /// <summary>스프라이트 bounds를 기준으로 실제 월드 크기가 size가 되도록 스케일한다.</summary>
        private static void CreateVisual(
            Transform parent,
            string name,
            Sprite sprite,
            Vector3 localPosition,
            Vector2 size,
            Color color,
            int sortingOrder)
        {
            var visual = new GameObject(name, typeof(SpriteRenderer));
            visual.transform.SetParent(parent, false);
            visual.transform.localPosition = localPosition;
            var renderer = visual.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.drawMode = SpriteDrawMode.Simple;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;

            if (sprite != null)
            {
                var bounds = sprite.bounds.size;
                var scaleX = bounds.x > 0.0001f ? size.x / bounds.x : size.x;
                var scaleY = bounds.y > 0.0001f ? size.y / bounds.y : size.y;
                visual.transform.localScale = new Vector3(scaleX, scaleY, 1f);
            }
            else
            {
                visual.transform.localScale = new Vector3(size.x, size.y, 1f);
            }
        }

        private static GameObject BuildPanelPrefab()
        {
            var root = new GameObject("EmergencyEscapePanel", typeof(RectTransform));
            var rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.sizeDelta = new Vector2(520f, 360f);

            var panelRoot = new GameObject("PanelRoot", typeof(RectTransform), typeof(Image));
            panelRoot.transform.SetParent(root.transform, false);
            StretchFull(panelRoot.GetComponent<RectTransform>());
            panelRoot.GetComponent<Image>().color = new Color(0.04f, 0.07f, 0.11f, 0.97f);

            var title = CreateText(
                panelRoot.transform,
                "Title",
                new Vector2(24f, -18f),
                new Vector2(470f, 40f),
                28,
                "긴급 탈출 포탈");
            title.fontStyle = FontStyles.Bold;

            CreateText(
                panelRoot.transform,
                "DestinationLabel",
                new Vector2(24f, -78f),
                new Vector2(470f, 28f),
                18,
                "이동 목적지");

            var dropdown = CreateDropdown(
                panelRoot.transform,
                "DestinationDropdown",
                new Vector2(24f, -118f),
                new Vector2(470f, 44f));

            var cost = CreateText(
                panelRoot.transform,
                "CostText",
                new Vector2(24f, -180f),
                new Vector2(470f, 32f),
                20,
                "비용: 100G + 전력 10");

            var result = CreateText(
                panelRoot.transform,
                "ResultText",
                new Vector2(24f, -220f),
                new Vector2(470f, 40f),
                17,
                string.Empty);

            var confirm = CreateButton(
                panelRoot.transform,
                "ConfirmButton",
                new Vector2(24f, -280f),
                new Vector2(220f, 48f),
                "이동");
            var close = CreateButton(
                panelRoot.transform,
                "CloseButton",
                new Vector2(274f, -280f),
                new Vector2(220f, 48f),
                "닫기");

            var view = root.AddComponent<EmergencyEscapePanelView>();
            var binder = root.AddComponent<EmergencyEscapePanelBinder>();

            var viewSo = new SerializedObject(view);
            viewSo.FindProperty("panelRoot").objectReferenceValue = panelRoot;
            viewSo.FindProperty("destinationDropdown").objectReferenceValue = dropdown;
            viewSo.FindProperty("costText").objectReferenceValue = cost;
            viewSo.FindProperty("resultText").objectReferenceValue = result;
            viewSo.FindProperty("confirmButton").objectReferenceValue = confirm;
            viewSo.FindProperty("closeButton").objectReferenceValue = close;
            viewSo.ApplyModifiedPropertiesWithoutUndo();

            var binderSo = new SerializedObject(binder);
            binderSo.FindProperty("view").objectReferenceValue = view;
            binderSo.FindProperty("confirmButton").objectReferenceValue = confirm;
            binderSo.FindProperty("closeButton").objectReferenceValue = close;
            binderSo.ApplyModifiedPropertiesWithoutUndo();

            panelRoot.SetActive(false);
            PrefabUtility.SaveAsPrefabAsset(root, PanelPrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            return AssetDatabase.LoadAssetAtPath<GameObject>(PanelPrefabPath);
        }

        private static void WireIntegrationScene()
        {
            var scene = EditorSceneManager.OpenScene(IntegrationScenePath, OpenSceneMode.Additive);
            try
            {
                var canvas = FindInSceneByName(scene, "HUDCanvas");
                var applicationRoot = FindInSceneByName(scene, "ApplicationRoot");
                var player = FindInSceneByName(scene, "Player");
                var elevator = FindInSceneByName(scene, "StartElevatorStation");
                var elevatorCenter = elevator != null
                    ? FindChild(elevator.transform, "BoardingAnchor")
                    : null;
                if (canvas == null || applicationRoot == null || player == null || elevatorCenter == null)
                {
                    throw new InvalidOperationException("Integration escape panel wiring is incomplete.");
                }

                var panelParent = canvas.transform.Find("PanelLayout");
                if (panelParent == null)
                {
                    panelParent = canvas.transform;
                }

                var existing = FindChild(panelParent, "EmergencyEscapePanel");
                if (existing != null)
                {
                    UnityEngine.Object.DestroyImmediate(existing.gameObject);
                }

                var panelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PanelPrefabPath);
                if (panelPrefab == null)
                {
                    throw new InvalidOperationException("EmergencyEscapePanel prefab is missing.");
                }

                var panelInstance = (GameObject)PrefabUtility.InstantiatePrefab(panelPrefab, panelParent);
                panelInstance.name = "EmergencyEscapePanel";
                var panelRect = panelInstance.GetComponent<RectTransform>();
                panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0.5f);
                panelRect.pivot = new Vector2(0.5f, 0.5f);
                panelRect.anchoredPosition = Vector2.zero;
                panelRect.sizeDelta = new Vector2(520f, 360f);
                // 다른 HUD 패널보다 앞에 보이도록 최상단 sibling으로 올린다.
                panelInstance.transform.SetAsLastSibling();

                var binder = panelInstance.GetComponent<EmergencyEscapePanelBinder>();
                var escapeBridge = applicationRoot.GetComponent<EmergencyEscapePortalRuntimeBridge>()
                    ?? applicationRoot.AddComponent<EmergencyEscapePortalRuntimeBridge>();
                var bridgeSo = new SerializedObject(escapeBridge);
                bridgeSo.FindProperty("playerTransform").objectReferenceValue = player.transform;
                bridgeSo.FindProperty("elevatorCenter").objectReferenceValue = elevatorCenter;
                bridgeSo.FindProperty("panelBinder").objectReferenceValue = binder;
                bridgeSo.ApplyModifiedPropertiesWithoutUndo();

                PreserveInventoryPanelReferences(scene);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static void PreserveInventoryPanelReferences(Scene scene)
        {
            var controller = FindInScene<HudPanelChromeController>(scene);
            var inventoryView = FindInScene<InventoryPanelView>(scene);
            var inventoryRoot = inventoryView != null ? inventoryView.gameObject : null;
            var close = inventoryRoot != null
                ? FindChild(inventoryRoot.transform, "CloseButton")?.GetComponent<Button>()
                : null;
            if (controller == null || inventoryView == null || close == null)
            {
                return;
            }

            var controllerSo = new SerializedObject(controller);
            controllerSo.FindProperty("inventoryPanelView").objectReferenceValue = inventoryView;
            controllerSo.FindProperty("inventoryPanelRoot").objectReferenceValue = inventoryRoot;
            controllerSo.FindProperty("inventoryCloseButton").objectReferenceValue = close;
            controllerSo.ApplyModifiedPropertiesWithoutUndo();
        }

        private static TMP_Dropdown CreateDropdown(
            Transform parent,
            string name,
            Vector2 position,
            Vector2 size)
        {
            var root = new GameObject(
                name,
                typeof(RectTransform),
                typeof(Image),
                typeof(TMP_Dropdown));
            root.transform.SetParent(parent, false);
            var rect = root.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            root.GetComponent<Image>().color = new Color(0.12f, 0.16f, 0.22f, 1f);

            var label = CreateText(root.transform, "Label", Vector2.zero, size, 18, "엘리베이터");
            StretchFull(label.rectTransform);
            label.alignment = TextAlignmentOptions.Left;
            label.margin = new Vector4(12f, 0f, 28f, 0f);

            var arrow = CreateText(root.transform, "Arrow", Vector2.zero, size, 18, "▼");
            StretchFull(arrow.rectTransform);
            arrow.alignment = TextAlignmentOptions.Right;
            arrow.margin = new Vector4(0f, 0f, 12f, 0f);
            arrow.raycastTarget = false;

            var template = new GameObject(
                "Template",
                typeof(RectTransform),
                typeof(Image),
                typeof(ScrollRect));
            template.transform.SetParent(root.transform, false);
            var templateRect = template.GetComponent<RectTransform>();
            templateRect.anchorMin = new Vector2(0f, 0f);
            templateRect.anchorMax = new Vector2(1f, 0f);
            templateRect.pivot = new Vector2(0.5f, 1f);
            templateRect.anchoredPosition = new Vector2(0f, 2f);
            templateRect.sizeDelta = new Vector2(0f, 140f);
            template.GetComponent<Image>().color = new Color(0.08f, 0.1f, 0.14f, 1f);

            var viewport = new GameObject(
                "Viewport",
                typeof(RectTransform),
                typeof(Image),
                typeof(Mask));
            viewport.transform.SetParent(template.transform, false);
            var viewportRect = viewport.GetComponent<RectTransform>();
            StretchFull(viewportRect);
            viewport.GetComponent<Image>().color = Color.white;
            viewport.GetComponent<Mask>().showMaskGraphic = false;

            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);
            var contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0f, 36f);

            var item = new GameObject(
                "Item",
                typeof(RectTransform),
                typeof(Toggle));
            item.transform.SetParent(content.transform, false);
            var itemRect = item.GetComponent<RectTransform>();
            itemRect.anchorMin = new Vector2(0f, 0.5f);
            itemRect.anchorMax = new Vector2(1f, 0.5f);
            itemRect.pivot = new Vector2(0.5f, 0.5f);
            itemRect.sizeDelta = new Vector2(0f, 36f);

            var itemBg = new GameObject("Item Background", typeof(RectTransform), typeof(Image));
            itemBg.transform.SetParent(item.transform, false);
            StretchFull(itemBg.GetComponent<RectTransform>());
            itemBg.GetComponent<Image>().color = new Color(0.12f, 0.18f, 0.24f, 1f);

            var itemCheck = new GameObject("Item Checkmark", typeof(RectTransform), typeof(Image));
            itemCheck.transform.SetParent(item.transform, false);
            var checkRect = itemCheck.GetComponent<RectTransform>();
            checkRect.anchorMin = new Vector2(0f, 0.5f);
            checkRect.anchorMax = new Vector2(0f, 0.5f);
            checkRect.pivot = new Vector2(0f, 0.5f);
            checkRect.anchoredPosition = new Vector2(8f, 0f);
            checkRect.sizeDelta = new Vector2(16f, 16f);
            itemCheck.GetComponent<Image>().color = new Color(0.3f, 0.85f, 1f, 1f);

            var itemLabel = CreateText(
                item.transform,
                "Item Label",
                Vector2.zero,
                new Vector2(100f, 36f),
                17,
                "Option");
            StretchFull(itemLabel.rectTransform);
            itemLabel.margin = new Vector4(30f, 0f, 8f, 0f);
            itemLabel.alignment = TextAlignmentOptions.Left;

            var toggle = item.GetComponent<Toggle>();
            toggle.targetGraphic = itemBg.GetComponent<Image>();
            toggle.graphic = itemCheck.GetComponent<Image>();
            toggle.isOn = true;

            var scroll = template.GetComponent<ScrollRect>();
            scroll.content = contentRect;
            scroll.viewport = viewportRect;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            var dropdown = root.GetComponent<TMP_Dropdown>();
            dropdown.template = templateRect;
            dropdown.captionText = label;
            dropdown.itemText = itemLabel;
            dropdown.options.Clear();
            dropdown.options.Add(new TMP_Dropdown.OptionData("엘리베이터"));
            dropdown.RefreshShownValue();
            template.SetActive(false);
            return dropdown;
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
            root.GetComponent<Image>().color = new Color(0.15f, 0.3f, 0.4f, 1f);

            var text = CreateText(root.transform, "Label", Vector2.zero, size, 18, label);
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

        private static Transform FindChild(Transform root, string name)
        {
            if (root == null)
            {
                return null;
            }

            foreach (var child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == name)
                {
                    return child;
                }
            }

            return null;
        }

        private static GameObject FindInSceneByName(Scene scene, string name)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                var found = FindChild(root.transform, name);
                if (found != null)
                {
                    return found.gameObject;
                }
            }

            return null;
        }

        private static T FindInScene<T>(Scene scene) where T : Component
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                var component = root.GetComponentInChildren<T>(true);
                if (component != null)
                {
                    return component;
                }
            }

            return null;
        }
    }
}
#endif

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

                PromptB46EmergencyEscapePortalBuilder.ApplyPortalArtwork(root);

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
            // prompt-B 147: 드롭다운 창은 터미널 창으로 교체되었다. 기존 프리팹이 있으면 자식만 다시 만든다.
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(PanelPrefabPath);
            if (existing == null)
            {
                var seed = new GameObject("EmergencyEscapePanel", typeof(RectTransform));
                seed.AddComponent<EmergencyEscapePanelView>();
                seed.AddComponent<EmergencyEscapePanelBinder>();
                PrefabUtility.SaveAsPrefabAsset(seed, PanelPrefabPath);
                UnityEngine.Object.DestroyImmediate(seed);
            }

            PromptB147EscapePortalTerminalBuilder.Build();
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

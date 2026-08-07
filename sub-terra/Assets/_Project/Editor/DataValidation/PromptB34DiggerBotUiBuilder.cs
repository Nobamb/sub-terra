using System.IO;
using System.Linq;
using System.Text;
using SubTerra.App.UI;
using SubTerra.App.UI.Drone;
using SubTerra.App.UI.HUD;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SubTerra.App.Editor.DataValidation
{
    /// <summary>
    /// prompt-B 34: 드론 말풍선 표시 유지 + digger-bot 하단 창 가시성/Tab·클릭 토글/X 닫기.
    /// </summary>
    public static class PromptB34DiggerBotUiBuilder
    {
        public const string IntegrationScenePath =
            "Assets/_Project/Scenes/App/Mine_Demo_Integration.unity";
        private const string DialoguePrefabPath =
            "Assets/_Project/Prefabs/UI/DroneDialoguePanel.prefab";
        private const string CompositePrefabPath =
            "Assets/_Project/Prefabs/UI/DroneAnalysisUI.prefab";

        private const float DiggerBottomY = 24f;
        private const float DiggerWidth = 760f;
        private const float DiggerHeight = 240f;
        private const float DialogueFontSize = DroneDialoguePanelView.PanelDialogueFontSize;

        [MenuItem("SubTerra/UI/Build Digger-Bot Speech + Panel (prompt-B 34)")]
        public static void BuildFromMenu()
        {
            var report = Build();
            Debug.Log("[SubTerra] " + report);
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            Directory.CreateDirectory(Path.Combine(projectRoot, "Temp"));
            File.WriteAllText(
                Path.Combine(projectRoot, "Temp", "prompt-b34-digger-bot-ui.txt"),
                report);
        }

        public static string Build()
        {
            var sb = new StringBuilder();
            sb.AppendLine("prompt-B 34 Digger-Bot Speech + Panel");
            sb.AppendLine(UpdateDialoguePrefab());
            sb.AppendLine(UpdateCompositePrefab());
            sb.AppendLine(UpdateIntegrationScene());
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return sb.ToString();
        }

        private static string UpdateDialoguePrefab()
        {
            var root = PrefabUtility.LoadPrefabContents(DialoguePrefabPath);
            try
            {
                ApplyDiggerPanelLayout(root.transform);
                EnsureXCloseButton(root.transform);
                ApplyLargeDialogueFont(root.transform);
                WirePanelViewCloseButton(root);
                PrefabUtility.SaveAsPrefabAsset(root, DialoguePrefabPath);
                return "DroneDialoguePanel layout+X+font=" + DialogueFontSize;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static string UpdateCompositePrefab()
        {
            var root = PrefabUtility.LoadPrefabContents(CompositePrefabPath);
            try
            {
                // 부모는 전체 스트레치(자식 배치 기준). 크기를 0으로 두면 자식 absolute size는 유지된다.
                var rect = root.GetComponent<RectTransform>();
                if (rect != null)
                {
                    rect.anchorMin = Vector2.zero;
                    rect.anchorMax = Vector2.one;
                    rect.pivot = new Vector2(0.5f, 0.5f);
                    rect.anchoredPosition = Vector2.zero;
                    rect.sizeDelta = Vector2.zero;
                }

                var dialogue = root.transform.Find("DroneDialoguePanel");
                if (dialogue != null)
                {
                    ApplyDiggerPanelLayout(dialogue);
                    EnsureXCloseButton(dialogue);
                    ApplyLargeDialogueFont(dialogue);
                    WirePanelViewCloseButton(dialogue.gameObject);
                }

                PrefabUtility.SaveAsPrefabAsset(root, CompositePrefabPath);
                return "DroneAnalysisUI stretch parent + digger child";
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static string UpdateIntegrationScene()
        {
            var previous = SceneManager.GetActiveScene().path;
            var scene = EditorSceneManager.OpenScene(IntegrationScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                return "FAIL: scene open " + IntegrationScenePath;
            }

            var canvas = FindInScene<Canvas>(scene, "HUDCanvas");
            if (canvas == null)
            {
                return "FAIL: HUDCanvas missing";
            }

            // 부모 합성 패널(DiggerBotPanel)은 항상 활성(Binder/말풍선 유지) + stretch.
            var composite = FindInSceneTransform(scene, "DiggerBotPanel")
                ?? FindInSceneTransform(scene, "DroneAnalysisUI");
            if (composite != null)
            {
                var rect = composite as RectTransform ?? composite.GetComponent<RectTransform>();
                if (rect != null)
                {
                    rect.anchorMin = Vector2.zero;
                    rect.anchorMax = Vector2.one;
                    rect.pivot = new Vector2(0.5f, 0.5f);
                    rect.anchoredPosition = Vector2.zero;
                    rect.sizeDelta = Vector2.zero;
                    EditorUtility.SetDirty(rect);
                }

                composite.gameObject.SetActive(true);

                // PanelToggleController가 digger 호스트를 끄는 레거시 CloseButton 제거.
                foreach (var btn in composite.GetComponentsInChildren<Button>(true))
                {
                    if (btn.name != "CloseButton")
                    {
                        continue;
                    }

                    // 패널 내부 ×만 남기고, 합성 루트에 붙은 중복 CloseButton 제거.
                    if (btn.transform.parent == composite)
                    {
                        Object.DestroyImmediate(btn.gameObject);
                    }
                }
            }

            var digger = FindInSceneTransform(scene, "DroneDialoguePanel");
            if (digger == null)
            {
                return "FAIL: DroneDialoguePanel missing";
            }

            ApplyDiggerPanelLayout(digger);
            EnsureXCloseButton(digger);
            ApplyLargeDialogueFont(digger);
            WirePanelViewCloseButton(digger.gameObject);
            // 시작 시 창은 닫힘. Tab/드론 클릭으로 연다.
            digger.gameObject.SetActive(false);

            // 우측 단독 추천 창은 비활성 유지(통합 창 사용).
            var reason = FindInSceneTransform(scene, "DroneReasonPanel");
            if (reason != null)
            {
                reason.gameObject.SetActive(false);
            }

            // 드론 재오픈 버튼 완전 제거.
            var openDiggerTf = canvas.transform.Find("OpenDiggerBotButton");
            if (openDiggerTf != null)
            {
                Object.DestroyImmediate(openDiggerTf.gameObject);
            }

            // PanelToggleController가 digger 호스트를 시작 시 끄지 않도록 해제.
            ClearPanelToggleDigger(scene, composite != null ? composite.gameObject : null);

            var droneRuntime = FindInSceneTransform(scene, "DiggerBot_Runtime");
            var viewSocket = droneRuntime != null
                ? droneRuntime.Find("ViewSocket")
                : FindInSceneTransform(scene, "ViewSocket");
            if (viewSocket != null)
            {
                viewSocket.gameObject.SetActive(true);
            }

            var chrome = canvas.GetComponent<HudPanelChromeController>();
            if (chrome == null)
            {
                chrome = canvas.gameObject.AddComponent<HudPanelChromeController>();
            }

            var diggerView = digger.GetComponent<DroneDialoguePanelView>()
                ?? digger.GetComponentInChildren<DroneDialoguePanelView>(true);
            var diggerClose = diggerView != null && diggerView.CloseButton != null
                ? diggerView.CloseButton
                : digger.GetComponentsInChildren<Button>(true)
                    .FirstOrDefault(b => b.name == "CloseButton");

            // 바인더는 합성 루트에 있을 수 있다.
            var binder = composite != null
                ? composite.GetComponent<DroneUiBinder>()
                : null;
            if (binder == null)
            {
                binder = FindInScene<DroneUiBinder>(scene, null);
            }

            if (binder != null)
            {
                var binderSo = new SerializedObject(binder);
                if (viewSocket != null)
                {
                    var socket = viewSocket.GetComponent<DroneDialogueSocket>();
                    if (socket != null)
                    {
                        binderSo.FindProperty("worldDialogueSocket").objectReferenceValue =
                            socket;
                    }
                }

                if (diggerView != null)
                {
                    binderSo.FindProperty("dialogueView").objectReferenceValue = diggerView;
                }

                binderSo.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(binder);
            }

            var chromeSo = new SerializedObject(chrome);
            chromeSo.FindProperty("diggerBotView").objectReferenceValue = diggerView;
            chromeSo.FindProperty("diggerBotRoot").objectReferenceValue = digger.gameObject;
            chromeSo.FindProperty("diggerCloseButton").objectReferenceValue = diggerClose;
            chromeSo.FindProperty("diggerOpenButton").objectReferenceValue = null;
            chromeSo.FindProperty("diggerHostRoot").objectReferenceValue =
                composite != null ? composite.gameObject : null;
            chromeSo.FindProperty("diggerBotOpen").boolValue = false;
            if (droneRuntime != null)
            {
                chromeSo.FindProperty("diggerBotWorldTarget").objectReferenceValue =
                    droneRuntime;
            }

            chromeSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(chrome);
            EditorUtility.SetDirty(canvas.gameObject);
            EditorUtility.SetDirty(digger.gameObject);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            if (!string.IsNullOrEmpty(previous)
                && previous != IntegrationScenePath
                && File.Exists(previous))
            {
                EditorSceneManager.OpenScene(previous, OpenSceneMode.Single);
            }

            return "IntegrationScene diggerY=" + DiggerBottomY
                + " font=" + DialogueFontSize
                + " worldTarget=" + (droneRuntime != null)
                + " socket=" + (viewSocket != null)
                + " closeX=" + (diggerClose != null)
                + " openBtnRemoved=True hostAlwaysOn=True";
        }

        private static void ClearPanelToggleDigger(Scene scene, GameObject diggerHost)
        {
            var toggles = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<PanelToggleController>(true));
            foreach (var toggle in toggles)
            {
                var so = new SerializedObject(toggle);
                var panels = so.FindProperty("panels");
                if (panels == null || !panels.isArray)
                {
                    continue;
                }

                for (var i = 0; i < panels.arraySize; i++)
                {
                    var entry = panels.GetArrayElementAtIndex(i);
                    var idProp = entry.FindPropertyRelative("panelId");
                    var rootProp = entry.FindPropertyRelative("panelRoot");
                    var visProp = entry.FindPropertyRelative("visibleOnStart");
                    if (idProp == null || rootProp == null)
                    {
                        continue;
                    }

                    // RuntimePanelId.DiggerBot == 4
                    if (idProp.enumValueIndex != 4)
                    {
                        continue;
                    }

                    rootProp.objectReferenceValue = null;
                    if (visProp != null)
                    {
                        visProp.boolValue = false;
                    }
                }

                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(toggle);
            }

            if (diggerHost != null)
            {
                diggerHost.SetActive(true);
            }
        }

        private static void ApplyDiggerPanelLayout(Transform diggerRoot)
        {
            var rect = diggerRoot as RectTransform ?? diggerRoot.GetComponent<RectTransform>();
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, DiggerBottomY);
            rect.sizeDelta = new Vector2(DiggerWidth, DiggerHeight);
            EditorUtility.SetDirty(rect);

            var panelRoot = diggerRoot.Find("PanelRoot") as RectTransform;
            if (panelRoot != null)
            {
                panelRoot.anchorMin = Vector2.zero;
                panelRoot.anchorMax = Vector2.one;
                panelRoot.offsetMin = Vector2.zero;
                panelRoot.offsetMax = Vector2.zero;
                panelRoot.gameObject.SetActive(true);
                EditorUtility.SetDirty(panelRoot);
            }

            // 대사 영역: 더 큰 글자를 위해 높이 확보.
            var dialogue = diggerRoot.Find("PanelRoot/DialogueText") as RectTransform
                ?? diggerRoot.GetComponentsInChildren<TMP_Text>(true)
                    .FirstOrDefault(t => t.name == "DialogueText")
                    ?.rectTransform;
            if (dialogue != null)
            {
                dialogue.anchorMin = new Vector2(0f, 1f);
                dialogue.anchorMax = new Vector2(0f, 1f);
                dialogue.pivot = new Vector2(0f, 1f);
                dialogue.anchoredPosition = new Vector2(20f, -44f);
                dialogue.sizeDelta = new Vector2(DiggerWidth - 40f, 72f);
                EditorUtility.SetDirty(dialogue);
            }
        }

        private static void ApplyLargeDialogueFont(Transform diggerRoot)
        {
            var dialogue = diggerRoot.GetComponentsInChildren<TMP_Text>(true)
                .FirstOrDefault(t => t.name == "DialogueText");
            if (dialogue == null)
            {
                return;
            }

            dialogue.fontSize = DialogueFontSize;
            dialogue.enableAutoSizing = false;
            dialogue.textWrappingMode = TextWrappingModes.Normal;
            EditorUtility.SetDirty(dialogue);
        }

        private static void EnsureXCloseButton(Transform diggerRoot)
        {
            var panelRoot = diggerRoot.Find("PanelRoot") ?? diggerRoot;
            var existing = panelRoot.GetComponentsInChildren<Button>(true)
                .FirstOrDefault(b => b.name == "CloseButton");
            Button button;
            if (existing != null)
            {
                button = existing;
            }
            else
            {
                var go = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
                go.transform.SetParent(panelRoot, false);
                button = go.GetComponent<Button>();
                var image = go.GetComponent<Image>();
                image.color = new Color(0.22f, 0.18f, 0.18f, 0.95f);
                button.targetGraphic = image;
            }

            var rect = button.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-10f, -8f);
            rect.sizeDelta = new Vector2(40f, 36f);

            var label = button.GetComponentInChildren<TMP_Text>(true);
            if (label == null)
            {
                var labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
                labelGo.transform.SetParent(button.transform, false);
                label = labelGo.GetComponent<TextMeshProUGUI>();
                var labelRect = label.rectTransform;
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = Vector2.zero;
                labelRect.offsetMax = Vector2.zero;
            }

            label.text = "×";
            label.fontSize = 22f;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
            EditorUtility.SetDirty(button.gameObject);
            EditorUtility.SetDirty(label);
        }

        private static void WirePanelViewCloseButton(GameObject diggerRoot)
        {
            var view = diggerRoot.GetComponent<DroneDialoguePanelView>()
                ?? diggerRoot.GetComponentInChildren<DroneDialoguePanelView>(true);
            if (view == null)
            {
                return;
            }

            var close = diggerRoot.GetComponentsInChildren<Button>(true)
                .FirstOrDefault(b => b.name == "CloseButton");
            var so = new SerializedObject(view);
            so.FindProperty("closeButton").objectReferenceValue = close;
            // panelRoot 누락 시 복구.
            if (so.FindProperty("panelRoot").objectReferenceValue == null)
            {
                var panel = diggerRoot.transform.Find("PanelRoot");
                if (panel != null)
                {
                    so.FindProperty("panelRoot").objectReferenceValue = panel.gameObject;
                }
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(view);
        }

        private static Button EnsureSideToggleButton(
            Transform canvas,
            string name,
            Vector2 anchoredPos,
            Vector2 size,
            string label)
        {
            var existing = canvas.Find(name);
            GameObject go;
            if (existing != null)
            {
                go = existing.gameObject;
            }
            else
            {
                go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
                go.transform.SetParent(canvas, false);
            }

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = size;

            var image = go.GetComponent<Image>();
            image.color = new Color(0.12f, 0.16f, 0.22f, 0.92f);

            var button = go.GetComponent<Button>();
            button.targetGraphic = image;

            var labelTmp = go.GetComponentInChildren<TMP_Text>(true);
            if (labelTmp == null)
            {
                var labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
                labelGo.transform.SetParent(go.transform, false);
                labelTmp = labelGo.GetComponent<TextMeshProUGUI>();
                var lr = labelTmp.rectTransform;
                lr.anchorMin = Vector2.zero;
                lr.anchorMax = Vector2.one;
                lr.offsetMin = Vector2.zero;
                lr.offsetMax = Vector2.zero;
            }

            labelTmp.text = label;
            labelTmp.fontSize = 16f;
            labelTmp.alignment = TextAlignmentOptions.Center;
            labelTmp.raycastTarget = false;
            return button;
        }

        private static T FindInScene<T>(Scene scene, string name)
            where T : Component
        {
            return scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<T>(true))
                .FirstOrDefault(item => name == null || item.name == name);
        }

        private static Transform FindInSceneTransform(Scene scene, string name)
        {
            return scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .FirstOrDefault(item => item.name == name);
        }
    }
}

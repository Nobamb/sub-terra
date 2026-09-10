using System;
using System.Linq;
using SubTerra.App.Integration;
using SubTerra.App.UI;
using SubTerra.App.UI.HUD;
using SubTerra.App.UI.Outpost;
using SubTerra.App.UI.EmergencyEscape;
using SubTerra.App.UI.SurfaceBase;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SubTerra.App.Editor.DataValidation
{
    public static class PromptB89UndergroundMenuBuilder
    {
        public const string ScenePath = "Assets/_Project/Scenes/App/Mine_Demo_Integration.unity";
        private const string SurfacePrefabPath = "Assets/_Project/Prefabs/UI/SurfaceBasePanel.prefab";

        [MenuItem("SubTerra/UI/Build Prompt-B 89 Underground Menu")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode first.");
            var previous = SceneManager.GetActiveScene();
            var scene = SceneManager.GetSceneByPath(ScenePath);
            bool opened = !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                var chrome = Find<HudPanelChromeController>(scene);
                var bar = chrome.GetComponentsInChildren<RectTransform>(true)
                    .Single(t => t.name == "PanelShortcutBar");
                var controller = chrome.GetComponent<UndergroundMenuController>();
                if (controller == null) controller = chrome.gameObject.AddComponent<UndergroundMenuController>();
                var old = chrome.transform.Find("UndergroundSettings");
                if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);

                // 원본은 읽기만 하고 복제본에서 설정창 외 지상 전용 UI와 Binder를 제거한다.
                var source = AssetDatabase.LoadAssetAtPath<GameObject>(SurfacePrefabPath);
                var host = UnityEngine.Object.Instantiate(source, chrome.transform);
                host.name = "UndergroundSettings";
                UnityEngine.Object.DestroyImmediate(host.GetComponent<SurfaceBaseBinder>());
                UnityEngine.Object.DestroyImmediate(host.GetComponent<Image>());
                var view = host.GetComponent<SurfaceBaseView>();
                var serialized = new SerializedObject(view);
                var settingsRoot = (GameObject)serialized.FindProperty("settingsRoot").objectReferenceValue;
                for (int i = host.transform.childCount - 1; i >= 0; i--)
                {
                    var child = host.transform.GetChild(i);
                    if (child.gameObject != settingsRoot) UnityEngine.Object.DestroyImmediate(child.gameObject);
                }
                // 삭제한 지상 버튼 참조를 명시적으로 비워 MissingReference를 남기지 않는다.
                serialized.Update();
                var property = serialized.GetIterator();
                while (property.NextVisible(true))
                    if (property.propertyType == SerializedPropertyType.ObjectReference
                        && property.objectReferenceValue == null) property.objectReferenceValue = null;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                var rect = (RectTransform)host.transform;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = rect.offsetMax = Vector2.zero;
                host.SetActive(true);
                view.SetSettingsVisible(false);

                Set(controller, "settingsView", view);
                Set(controller, "settingsRoot", settingsRoot);
                Set(controller, "chrome", chrome);
                Set(controller, "panels", Find<PanelToggleController>(scene));
                Set(controller, "outpost", Find<OutpostPanelBinder>(scene));
                Set(controller, "escape", Find<EmergencyEscapePanelBinder>(scene));
                var template = bar.GetComponentsInChildren<Button>(true).First();
                var settingsButton = MakeButton(bar, template, "SettingsShortcut", "설정(Esc)");
                var quitButton = MakeButton(bar, template, "QuitShortcut", "게임 종료(O)");
                UnityEventTools.AddPersistentListener(settingsButton.onClick, controller.OpenSettings);
                UnityEventTools.AddPersistentListener(quitButton.onClick, controller.RequestQuit);
                // 기존 우측 세로 바의 버튼 아래에 같은 크기로 이어 붙인다.
                var existing = bar.GetComponentsInChildren<Button>(true)
                    .Where(b => b != settingsButton && b != quitButton)
                    .Select(b => (RectTransform)b.transform).OrderByDescending(r => r.anchoredPosition.y).ToArray();
                var last = existing.Last();
                float step = existing.Length > 1
                    ? existing[0].anchoredPosition.y - existing[1].anchoredPosition.y
                    : last.rect.height + 8f;
                ((RectTransform)settingsButton.transform).anchoredPosition = last.anchoredPosition - new Vector2(0, step);
                ((RectTransform)quitButton.transform).anchoredPosition = last.anchoredPosition - new Vector2(0, step * 2);
                bar.sizeDelta = new Vector2(180f, -last.anchoredPosition.y + step * 2 + last.rect.height);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            finally
            {
                if (opened) EditorSceneManager.CloseScene(scene, true);
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            }
        }

        private static Button MakeButton(Transform parent, Button template, string name, string label)
        {
            var old = parent.Find(name);
            if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
            var button = UnityEngine.Object.Instantiate(template, parent);
            button.name = name;
            button.onClick = new Button.ButtonClickedEvent();
            button.GetComponentInChildren<TMP_Text>(true).text = label;
            button.gameObject.SetActive(true);
            UiKeyboardSubmitGuard.ConfigurePointerPreferredButton(button);
            return button;
        }

        private static T Find<T>(Scene scene) where T : Component => scene.GetRootGameObjects()
            .SelectMany(r => r.GetComponentsInChildren<T>(true)).FirstOrDefault();

        private static void Set(UnityEngine.Object target, string name, UnityEngine.Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(name).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}

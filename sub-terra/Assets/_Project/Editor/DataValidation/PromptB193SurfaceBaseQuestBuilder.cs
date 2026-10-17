using System;
using System.Linq;
using SubTerra.App.UI.SurfaceBase;
using SubTerra.App.UI.Tutorial;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SubTerra.App.Editor.DataValidation
{
    /// <summary>지하의 기존 상세창을 지상 프리팹에 복제한다. 원본 씬은 저장하지 않는다.</summary>
    public static class PromptB193SurfaceBaseQuestBuilder
    {
        [MenuItem("SubTerra/UI/Build Issue 193 Surface Quest Button")]
        public static void Build()
        {
            var scene = SceneManager.GetSceneByPath(PromptB53QuestUiBuilder.IntegrationScenePath);
            var closeScene = !scene.IsValid() || !scene.isLoaded;
            if (closeScene) scene = EditorSceneManager.OpenScene(PromptB53QuestUiBuilder.IntegrationScenePath, OpenSceneMode.Additive);
            GameObject panel = null;
            try
            {
                var source = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<DemoObjectiveView>(true)).Single();
                panel = PrefabUtility.LoadPrefabContents(PromptB117SurfaceBaseBuilder.PrefabPath);
                PromptB117SurfaceBaseBuilder.ApplyMainControls(panel);
                var previous = panel.transform.Find("SurfaceQuestRoot");
                if (previous != null) UnityEngine.Object.DestroyImmediate(previous.gameObject);
                var root = UnityEngine.Object.Instantiate(source.gameObject, panel.transform, false);
                root.name = "SurfaceQuestRoot";
                root.SetActive(false);
                foreach (var binder in root.GetComponentsInChildren<TutorialDirectorBinder>(true))
                    UnityEngine.Object.DestroyImmediate(binder);
                foreach (var debug in root.GetComponentsInChildren<DemoObjectiveDebugTools>(true))
                    UnityEngine.Object.DestroyImmediate(debug);
                foreach (Transform child in root.transform.Cast<Transform>().ToArray())
                    if (child.name != "QuestDetailsPanel") UnityEngine.Object.DestroyImmediate(child.gameObject);
                if (root.transform.Find("QuestDetailsPanel") == null)
                    throw new InvalidOperationException("기존 퀘스트 상세창을 찾을 수 없습니다.");

                var rect = (RectTransform)root.transform;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = rect.offsetMax = Vector2.zero;
                rect.localScale = Vector3.one;
                var canvas = root.GetComponent<Canvas>() ?? root.AddComponent<Canvas>();
                if (root.GetComponent<UnityEngine.UI.GraphicRaycaster>() == null)
                    root.AddComponent<UnityEngine.UI.GraphicRaycaster>();
                var view = root.GetComponent<DemoObjectiveView>();
                var serializedView = new SerializedObject(view);
                var property = serializedView.GetIterator();
                while (property.NextVisible(true))
                {
                    if (property.propertyType != SerializedPropertyType.ObjectReference) continue;
                    var reference = property.objectReferenceValue;
                    var target = reference is Component component ? component.transform
                        : reference is GameObject go ? go.transform : null;
                    if (target != null && target != root.transform && !target.IsChildOf(root.transform))
                        property.objectReferenceValue = null;
                }
                serializedView.FindProperty("tutorialCanvas").objectReferenceValue = canvas;
                serializedView.ApplyModifiedPropertiesWithoutUndo();
                view.SetDetailsVisible(false);
                var controller = panel.GetComponent<SurfaceBaseQuestPanel>() ?? panel.AddComponent<SurfaceBaseQuestPanel>();
                var serialized = new SerializedObject(controller);
                serialized.FindProperty("questButton").objectReferenceValue = panel.transform
                    .Find("SurfaceBaseContent/QuestButton").GetComponent<UnityEngine.UI.Button>();
                serialized.FindProperty("questView").objectReferenceValue = view;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(panel, PromptB117SurfaceBaseBuilder.PrefabPath);
            }
            finally
            {
                if (panel != null) PrefabUtility.UnloadPrefabContents(panel);
                if (closeScene && scene.IsValid() && scene.isLoaded) EditorSceneManager.CloseScene(scene, true);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[SubTerra] Issue 193 surface quest button built.");
        }

        public static void BuildAndVerify()
        {
            Build();
            var tests = AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType("SubTerra.App.Tests.UI.PromptB117SurfaceBaseTests"))
                .First(type => type != null);
            foreach (var name in new[] { "QuestActions_UseEqualSizeAndSpacing", "SurfaceQuest_ReusesDetailsWithoutChangingProgressOrRewards" })
            {
                tests.GetMethod(name).Invoke(Activator.CreateInstance(tests), null);
                Debug.Log("[SubTerra] PASS: " + name);
            }
        }
    }
}

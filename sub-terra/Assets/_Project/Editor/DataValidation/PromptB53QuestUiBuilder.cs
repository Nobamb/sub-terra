using SubTerra.App.UI.Tutorial;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SubTerra.App.Editor.DataValidation
{
    /// <summary>prompt-B 53 퀘스트 요약 클릭 영역과 중앙 상세창만 연결한다.</summary>
    public static class PromptB53QuestUiBuilder
    {
        public const string IntegrationScenePath =
            "Assets/_Project/Scenes/App/Mine_Demo_Integration.unity";

        [MenuItem("SubTerra/UI/Build Prompt-B 53 Quest Details")]
        public static void BuildFromMenu()
        {
            Debug.Log("[SubTerra] " + Build());
        }

        public static string Build()
        {
            var scene = SceneManager.GetSceneByPath(IntegrationScenePath);
            var closeAfterBuild = !scene.IsValid() || !scene.isLoaded;
            if (closeAfterBuild)
            {
                scene = EditorSceneManager.OpenScene(
                    IntegrationScenePath,
                    OpenSceneMode.Additive);
            }

            try
            {
                var root = FindInScene(scene, "DemoObjectiveRoot");
                if (root == null)
                {
                    throw new System.InvalidOperationException(
                        "Mine_Demo_Integration: DemoObjectiveRoot가 없습니다.");
                }

                var view = root.GetComponent<DemoObjectiveView>();
                if (view == null)
                {
                    throw new System.InvalidOperationException(
                        "DemoObjectiveRoot: DemoObjectiveView가 없습니다.");
                }

                var font = FindFont(root.transform);
                var summaryButton = EnsureSummaryButton(root.transform);
                var details = EnsureDetailsPanel(root.transform, font);

                var serialized = new SerializedObject(view);
                Assign(serialized, "detailsRoot", details.Root);
                Assign(serialized, "detailsTitleText", details.Title);
                Assign(serialized, "detailsBodyText", details.Body);
                Assign(serialized, "detailsNextActionText", details.NextAction);
                serialized.ApplyModifiedPropertiesWithoutUndo();

                Wire(summaryButton, view.OnObjectiveDetailsClicked);
                Wire(details.CloseButton, view.OnDetailsDismissClicked);
                details.Root.SetActive(false);

                EditorUtility.SetDirty(view);
                EditorUtility.SetDirty(summaryButton);
                EditorUtility.SetDirty(details.CloseButton);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene))
                {
                    throw new System.InvalidOperationException(
                        "Mine_Demo_Integration 저장에 실패했습니다.");
                }

                return "Prompt-B 53 quest details built: " + IntegrationScenePath;
            }
            finally
            {
                if (closeAfterBuild && scene.IsValid() && scene.isLoaded)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        private static Button EnsureSummaryButton(Transform root)
        {
            var existing = root.Find("QuestSummaryButton");
            GameObject go;
            if (existing == null)
            {
                go = new GameObject(
                    "QuestSummaryButton",
                    typeof(RectTransform),
                    typeof(Image),
                    typeof(Button));
                go.transform.SetParent(root, false);
            }
            else
            {
                go = existing.gameObject;
                if (go.GetComponent<Image>() == null)
                {
                    go.AddComponent<Image>();
                }

                if (go.GetComponent<Button>() == null)
                {
                    go.AddComponent<Button>();
                }
            }

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(12f, -280f);
            rect.sizeDelta = new Vector2(452f, 142f);
            go.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.01f);
            go.transform.SetAsFirstSibling();
            return go.GetComponent<Button>();
        }

        private static DetailsReferences EnsureDetailsPanel(
            Transform root,
            TMP_FontAsset font)
        {
            var existing = root.Find("QuestDetailsPanel");
            GameObject panel;
            if (existing == null)
            {
                panel = new GameObject(
                    "QuestDetailsPanel",
                    typeof(RectTransform),
                    typeof(Image));
                panel.transform.SetParent(root, false);
            }
            else
            {
                panel = existing.gameObject;
                if (panel.GetComponent<Image>() == null)
                {
                    panel.AddComponent<Image>();
                }
            }

            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(720f, 360f);
            panel.GetComponent<Image>().color = new Color(0.035f, 0.055f, 0.085f, 0.97f);

            var title = EnsureText(
                panel.transform,
                "QuestDetailsTitle",
                new Vector2(28f, -26f),
                new Vector2(620f, 52f),
                24f,
                TextAlignmentOptions.TopLeft,
                font);
            var body = EnsureText(
                panel.transform,
                "QuestDetailsBody",
                new Vector2(28f, -92f),
                new Vector2(664f, 150f),
                18f,
                TextAlignmentOptions.TopLeft,
                font);
            var next = EnsureText(
                panel.transform,
                "QuestDetailsNextAction",
                new Vector2(28f, -260f),
                new Vector2(664f, 64f),
                17f,
                TextAlignmentOptions.TopLeft,
                font);
            var close = EnsureCloseButton(panel.transform, font);

            panel.transform.SetAsLastSibling();
            return new DetailsReferences(panel, title, body, next, close);
        }

        private static TMP_Text EnsureText(
            Transform parent,
            string name,
            Vector2 position,
            Vector2 size,
            float fontSize,
            TextAlignmentOptions alignment,
            TMP_FontAsset font)
        {
            var existing = parent.Find(name);
            GameObject go;
            if (existing == null)
            {
                go = new GameObject(name, typeof(RectTransform));
                go.transform.SetParent(parent, false);
            }
            else
            {
                go = existing.gameObject;
            }

            var text = go.GetComponent<TextMeshProUGUI>();
            if (text == null)
            {
                text = go.AddComponent<TextMeshProUGUI>();
            }

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            text.font = font;
            text.fontSize = fontSize;
            text.color = Color.white;
            text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.raycastTarget = false;
            return text;
        }

        private static Button EnsureCloseButton(Transform parent, TMP_FontAsset font)
        {
            var existing = parent.Find("QuestDetailsCloseButton");
            GameObject go;
            if (existing == null)
            {
                go = new GameObject(
                    "QuestDetailsCloseButton",
                    typeof(RectTransform),
                    typeof(Image),
                    typeof(Button));
                go.transform.SetParent(parent, false);
            }
            else
            {
                go = existing.gameObject;
                if (go.GetComponent<Image>() == null)
                {
                    go.AddComponent<Image>();
                }

                if (go.GetComponent<Button>() == null)
                {
                    go.AddComponent<Button>();
                }
            }

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-18f, -18f);
            rect.sizeDelta = new Vector2(44f, 44f);
            go.GetComponent<Image>().color = new Color(0.38f, 0.12f, 0.14f, 1f);

            var label = EnsureText(
                go.transform,
                "Label",
                Vector2.zero,
                new Vector2(44f, 44f),
                24f,
                TextAlignmentOptions.Center,
                font);
            var labelRect = label.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.pivot = new Vector2(0.5f, 0.5f);
            labelRect.anchoredPosition = Vector2.zero;
            labelRect.sizeDelta = Vector2.zero;
            label.text = "X";
            return go.GetComponent<Button>();
        }

        private static void Wire(Button button, UnityEngine.Events.UnityAction action)
        {
            while (button.onClick.GetPersistentEventCount() > 0)
            {
                UnityEventTools.RemovePersistentListener(button.onClick, 0);
            }

            UnityEventTools.AddPersistentListener(button.onClick, action);
        }

        private static TMP_FontAsset FindFont(Transform root)
        {
            var sourceTransform = root.Find("ObjectiveTitle");
            var source = sourceTransform != null
                ? sourceTransform.GetComponent<TMP_Text>()
                : null;
            if (source == null || source.font == null)
            {
                throw new System.InvalidOperationException(
                    "ObjectiveTitle의 TMP 폰트 참조가 없습니다.");
            }

            return source.font;
        }

        private static GameObject FindInScene(Scene scene, string name)
        {
            var roots = scene.GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                var transforms = roots[i].GetComponentsInChildren<Transform>(true);
                for (var j = 0; j < transforms.Length; j++)
                {
                    if (transforms[j].name == name)
                    {
                        return transforms[j].gameObject;
                    }
                }
            }

            return null;
        }

        private static void Assign(SerializedObject serialized, string name, Object value)
        {
            var property = serialized.FindProperty(name);
            if (property == null)
            {
                throw new System.InvalidOperationException(
                    "DemoObjectiveView 직렬화 필드 누락: " + name);
            }

            property.objectReferenceValue = value;
        }

        private readonly struct DetailsReferences
        {
            public GameObject Root { get; }
            public TMP_Text Title { get; }
            public TMP_Text Body { get; }
            public TMP_Text NextAction { get; }
            public Button CloseButton { get; }

            public DetailsReferences(
                GameObject root,
                TMP_Text title,
                TMP_Text body,
                TMP_Text nextAction,
                Button closeButton)
            {
                Root = root;
                Title = title;
                Body = body;
                NextAction = nextAction;
                CloseButton = closeButton;
            }
        }
    }
}

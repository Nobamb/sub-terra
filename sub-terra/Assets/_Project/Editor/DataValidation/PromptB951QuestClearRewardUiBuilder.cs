using SubTerra.App.Tutorial;
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
    /// <summary>prompt-B 95-1 퀘스트 클리어 보상 팝업만 Integration Scene에 연결한다.</summary>
    public static class PromptB951QuestClearRewardUiBuilder
    {
        public const string IntegrationScenePath =
            "Assets/_Project/Scenes/App/Mine_Demo_Integration.unity";

        [MenuItem("SubTerra/UI/Build Prompt-B 95-1 Quest Clear Reward Popup")]
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
                var claim = EnsureClaimPanel(root.transform, font);
                RelabelCapacityButtons(root.transform, font);
                ApplyQuestPopupSort(root.transform.Find("QuestDetailsPanel"));
                ApplyQuestPopupSort(claim.Root.transform);

                var serialized = new SerializedObject(view);
                Assign(serialized, "claimRoot", claim.Root);
                Assign(serialized, "claimTitleText", claim.Title);
                Assign(serialized, "claimQuestTitleText", claim.QuestTitle);
                Assign(serialized, "claimRewardText", claim.Reward);
                Assign(serialized, "claimHintText", claim.Hint);
                serialized.ApplyModifiedPropertiesWithoutUndo();

                Wire(claim.CloseButton, view.OnClaimConfirmClicked);
                Wire(claim.ConfirmButton, view.OnClaimConfirmClicked);

                claim.Root.SetActive(false);

                EditorUtility.SetDirty(view);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene))
                {
                    throw new System.InvalidOperationException(
                        "Mine_Demo_Integration 저장에 실패했습니다.");
                }

                return "Prompt-B 95-1 quest clear reward popup built: " + IntegrationScenePath;
            }
            finally
            {
                if (closeAfterBuild && scene.IsValid() && scene.isLoaded)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        private static ClaimRefs EnsureClaimPanel(Transform root, TMP_FontAsset font)
        {
            var panel = EnsurePanel(root, "QuestClearRewardPanel", new Vector2(720f, 400f));
            var title = EnsureText(
                panel.transform,
                "ClaimTitle",
                new Vector2(28f, -24f),
                new Vector2(620f, 44f),
                26f,
                TextAlignmentOptions.TopLeft,
                font);
            title.text = "퀘스트 클리어";
            var quest = EnsureText(
                panel.transform,
                "ClaimQuestTitle",
                new Vector2(28f, -78f),
                new Vector2(664f, 40f),
                22f,
                TextAlignmentOptions.TopLeft,
                font);
            var reward = EnsureText(
                panel.transform,
                "ClaimReward",
                new Vector2(28f, -128f),
                new Vector2(664f, 80f),
                20f,
                TextAlignmentOptions.TopLeft,
                font);
            var hint = EnsureText(
                panel.transform,
                "ClaimHint",
                new Vector2(28f, -220f),
                new Vector2(664f, 48f),
                16f,
                TextAlignmentOptions.TopLeft,
                font);
            hint.text = "닫으면 보상이 지급됩니다.";
            var close = EnsureButton(
                panel.transform,
                "ClaimCloseButton",
                "X",
                new Vector2(1f, 1f),
                new Vector2(-18f, -18f),
                new Vector2(44f, 44f),
                new Color(0.38f, 0.12f, 0.14f, 1f),
                font);
            var confirm = EnsureButton(
                panel.transform,
                "ClaimConfirmButton",
                "확인",
                new Vector2(0.5f, 0f),
                new Vector2(0f, 28f),
                new Vector2(240f, 52f),
                new Color(0.16f, 0.32f, 0.22f, 1f),
                font);
            panel.transform.SetAsLastSibling();
            return new ClaimRefs(panel, title, quest, reward, hint, close, confirm);
        }

        private static void ApplyQuestPopupSort(Transform panel)
        {
            if (panel == null)
            {
                return;
            }

            var canvas = panel.GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = panel.gameObject.AddComponent<Canvas>();
            }

            if (panel.GetComponent<GraphicRaycaster>() == null)
            {
                panel.gameObject.AddComponent<GraphicRaycaster>();
            }

            var wasActive = panel.gameObject.activeSelf;
            panel.gameObject.SetActive(true);
            canvas.overrideSorting = true;
            canvas.sortingOrder = UiLayerPriority.QuestPopup;
            canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.TexCoord1
                | AdditionalCanvasShaderChannels.Normal
                | AdditionalCanvasShaderChannels.Tangent;
            var serialized = new SerializedObject(canvas);
            serialized.FindProperty("m_OverrideSorting").boolValue = true;
            serialized.FindProperty("m_SortingOrder").intValue = UiLayerPriority.QuestPopup;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            panel.gameObject.SetActive(wasActive);
        }

        private static void RelabelCapacityButtons(Transform root, TMP_FontAsset font)
        {
            var capacity = root.Find("QuestRewardCapacityPanel");
            if (capacity == null)
            {
                return;
            }

            EnsureButton(
                capacity,
                "CapacityDumpButton",
                "기존 자원 버리기",
                new Vector2(0.5f, 0f),
                new Vector2(-160f, 28f),
                new Vector2(280f, 52f),
                new Color(0.16f, 0.32f, 0.22f, 1f),
                font);
            EnsureButton(
                capacity,
                "CapacityForfeitButton",
                "퀘스트 보상 버리기",
                new Vector2(0.5f, 0f),
                new Vector2(160f, 28f),
                new Vector2(280f, 52f),
                new Color(0.38f, 0.12f, 0.14f, 1f),
                font);
        }

        private static GameObject EnsurePanel(Transform root, string name, Vector2 size)
        {
            var existing = root.Find(name);
            GameObject panel;
            if (existing == null)
            {
                panel = new GameObject(name, typeof(RectTransform), typeof(Image));
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
            rect.sizeDelta = size;
            panel.GetComponent<Image>().color = new Color(0.035f, 0.055f, 0.085f, 0.97f);
            return panel;
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

        private static Button EnsureButton(
            Transform parent,
            string name,
            string label,
            Vector2 anchor,
            Vector2 position,
            Vector2 size,
            Color color,
            TMP_FontAsset font)
        {
            var existing = parent.Find(name);
            GameObject go;
            if (existing == null)
            {
                go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
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
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            go.GetComponent<Image>().color = color;

            var text = EnsureText(
                go.transform,
                "Label",
                Vector2.zero,
                size,
                20f,
                TextAlignmentOptions.Center,
                font);
            var labelRect = text.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.pivot = new Vector2(0.5f, 0.5f);
            labelRect.anchoredPosition = Vector2.zero;
            labelRect.sizeDelta = Vector2.zero;
            text.text = label;
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

        private static void Assign(SerializedObject serialized, string name, UnityEngine.Object value)
        {
            var property = serialized.FindProperty(name);
            if (property == null)
            {
                throw new System.InvalidOperationException(
                    "DemoObjectiveView 직렬화 필드 누락: " + name);
            }

            property.objectReferenceValue = value;
        }

        private readonly struct ClaimRefs
        {
            public GameObject Root { get; }
            public TMP_Text Title { get; }
            public TMP_Text QuestTitle { get; }
            public TMP_Text Reward { get; }
            public TMP_Text Hint { get; }
            public Button CloseButton { get; }
            public Button ConfirmButton { get; }

            public ClaimRefs(
                GameObject root,
                TMP_Text title,
                TMP_Text questTitle,
                TMP_Text reward,
                TMP_Text hint,
                Button closeButton,
                Button confirmButton)
            {
                Root = root;
                Title = title;
                QuestTitle = questTitle;
                Reward = reward;
                Hint = hint;
                CloseButton = closeButton;
                ConfirmButton = confirmButton;
            }
        }
    }
}

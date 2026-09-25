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
        private const string Art = "Assets/_Project/Art/UI/Gameplay/Quest/Clear/";
        private const string Icons = "Assets/_Project/Art/Icons/";
        public const string IntegrationScenePath =
            "Assets/_Project/Scenes/App/Mine_Demo_Integration.unity";

        [MenuItem("SubTerra/UI/Build Prompt-B 95-1 Quest Clear Reward Popup")]
        public static void BuildFromMenu()
        {
            Debug.Log("[SubTerra] " + Build());
        }

        public static string Build()
        {
            ImportArt();
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
                ApplyQuestPopupSort(claim.Root.transform);

                var serialized = new SerializedObject(view);
                Assign(serialized, "claimRoot", claim.Root);
                Assign(serialized, "claimTitleText", claim.Title);
                Assign(serialized, "claimQuestTitleText", claim.QuestTitle);
                Assign(serialized, "claimRewardText", claim.Reward);
                Assign(serialized, "claimHintText", claim.Hint);
                Assign(serialized, "claimProgressText", claim.Progress);
                Assign(serialized, "claimRewardRow", claim.RewardRow);
                serialized.ApplyModifiedPropertiesWithoutUndo();

                Wire(claim.CloseButton, view.OnClaimConfirmClicked);

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
            var panel = EnsurePanel(root, "QuestClearRewardPanel", new Vector2(900f, 450f));
            var background = panel.GetComponent<Image>();
            background.sprite = SpriteAt("quest-clear-popup-frame.png");
            background.color = Color.white;
            background.raycastTarget = true;
            var motion = panel.GetComponent<QuestClearPopupMotion>();
            if (motion == null) motion = panel.AddComponent<QuestClearPopupMotion>();
            var title = EnsureText(
                panel.transform,
                "ClaimTitle",
                new Vector2(125f, -90f),
                new Vector2(365f, 40f),
                28f,
                TextAlignmentOptions.MidlineLeft,
                font);
            title.text = "퀘스트 클리어";
            title.color = new Color(0.4f, 1f, 1f, 1f);
            var quest = EnsureText(
                panel.transform,
                "ClaimQuestTitle",
                new Vector2(246f, -174f),
                new Vector2(555f, 42f),
                29f,
                TextAlignmentOptions.TopLeft,
                font);
            var reward = EnsureText(
                panel.transform,
                "ClaimReward",
                new Vector2(225f, -320f),
                new Vector2(550f, 40f),
                19f,
                TextAlignmentOptions.TopLeft,
                font);
            var hint = EnsureText(
                panel.transform,
                "ClaimHint",
                new Vector2(520f, -270f),
                new Vector2(273f, 20f),
                12f,
                TextAlignmentOptions.TopRight,
                font);
            hint.text = "닫으면 보상이 지급됩니다.";
            hint.color = new Color(0.62f, 0.78f, 0.82f, 1f);
            var progress = EnsureText(panel.transform, "ClaimProgress", new Vector2(550f, -100f),
                new Vector2(180f, 34f), 18f, TextAlignmentOptions.MidlineRight, font);
            progress.color = new Color(0.45f, 1f, 1f, 1f);
            var oldMission = panel.transform.Find("ClaimMission");
            if (oldMission != null) Object.DestroyImmediate(oldMission.gameObject);
            var description = EnsureText(panel.transform, "ClaimDescription", new Vector2(248f, -226f),
                new Vector2(545f, 34f), 18f, TextAlignmentOptions.TopLeft, font);
            description.text = "퀘스트 목표를 달성했습니다.";
            description.color = new Color(0.83f, 0.94f, 0.96f, 1f);
            var rewardLabel = EnsureText(panel.transform, "ClaimRewardLabel", new Vector2(104f, -324f),
                new Vector2(100f, 38f), 22f, TextAlignmentOptions.TopLeft, font);
            rewardLabel.text = "보상";
            rewardLabel.color = new Color(0.4f, 1f, 1f, 1f);
            var check = EnsureImage(panel.transform, "ClaimCheckSign", SpriteAt("check-sign.png"),
                new Vector2(0f, 1f), new Vector2(104f, -171f), new Vector2(106f, 106f));
            var oldRewardIcon = panel.transform.Find("ClaimRewardIcon");
            if (oldRewardIcon != null) Object.DestroyImmediate(oldRewardIcon.gameObject);
            var rewardRow = EnsureRect(panel.transform, "ClaimRewardRow",
                new Vector2(0f, 1f), new Vector2(225f, -315f), new Vector2(570f, 52f));
            EnsureRewardItem(rewardRow, "Copper", "icon_copper.png", font);
            EnsureRewardItem(rewardRow, "Iron", "icon_iron.png", font);
            EnsureRewardItem(rewardRow, "Lithium", "icon_lithium.png", font);
            EnsureRewardItem(rewardRow, "Gold", "quest-icon-gold.png", font);
            reward.gameObject.SetActive(false);
            var close = EnsureImage(panel.transform, "ClaimCloseButton", SpriteAt("x-button.png"),
                new Vector2(1f, 1f), new Vector2(-102f, -111f), new Vector2(36f, 36f));
            close.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            close.raycastTarget = true;
            var closeButton = close.GetComponent<Button>();
            if (closeButton == null) closeButton = close.gameObject.AddComponent<Button>();
            closeButton.transition = Selectable.Transition.None;
            if (close.GetComponent<QuestClearCloseHover>() == null)
                close.gameObject.AddComponent<QuestClearCloseHover>();
            var oldLabel = close.transform.Find("Label");
            if (oldLabel != null) Object.DestroyImmediate(oldLabel.gameObject);
            var confirm = panel.transform.Find("ClaimConfirmButton");
            if (confirm != null) Object.DestroyImmediate(confirm.gameObject);
            var glint = EnsureImage(panel.transform, "ClaimBorderGlint", null,
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(62f, 5f));
            glint.color = new Color(0.2f, 1f, 0.95f, 0.95f);
            glint.raycastTarget = false;
            glint.gameObject.SetActive(false);
            var motionSerialized = new SerializedObject(motion);
            Assign(motionSerialized, "checkSign", check.rectTransform);
            Assign(motionSerialized, "borderGlint", glint.rectTransform);
            Assign(motionSerialized, "borderGlintImage", glint);
            motionSerialized.ApplyModifiedPropertiesWithoutUndo();
            panel.transform.SetAsLastSibling();
            return new ClaimRefs(panel, title, quest, reward, hint, progress, rewardRow, closeButton);
        }

        private static RectTransform EnsureRect(Transform parent, string name,
            Vector2 anchor, Vector2 position, Vector2 size)
        {
            var child = parent.Find(name);
            var go = child != null ? child.gameObject : new GameObject(name, typeof(RectTransform));
            if (child == null) go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        private static void EnsureRewardItem(RectTransform row, string name, string iconFile, TMP_FontAsset font)
        {
            var item = EnsureRect(row, name, new Vector2(0f, 1f), Vector2.zero, new Vector2(145f, 52f));
            var iconPath = iconFile == "quest-icon-gold.png"
                ? "Assets/_Project/Art/UI/Gameplay/Quest/" + iconFile
                : Icons + iconFile;
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
            if (sprite == null) throw new System.InvalidOperationException("Missing reward icon: " + iconPath);
            var icon = EnsureImage(item, "Icon", sprite, new Vector2(0f, 1f),
                new Vector2(0f, -3f), new Vector2(44f, 44f));
            icon.preserveAspect = true;
            var label = EnsureText(item, "Amount", new Vector2(50f, -9f),
                new Vector2(95f, 32f), 18f, TextAlignmentOptions.MidlineLeft, font);
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.fontStyle = FontStyles.Bold;
        }

        private static Image EnsureImage(Transform parent, string name, Sprite sprite,
            Vector2 anchor, Vector2 position, Vector2 size)
        {
            var child = parent.Find(name);
            var go = child != null ? child.gameObject : new GameObject(name, typeof(RectTransform), typeof(Image));
            if (child == null) go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.color = Color.white;
            image.raycastTarget = false;
            return image;
        }

        private static Sprite SpriteAt(string name)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Art + name);
            if (sprite == null) throw new System.InvalidOperationException("Missing quest clear art: " + name);
            return sprite;
        }

        private static void ImportArt()
        {
            var files = new[] { "quest-clear-popup-frame.png", "x-button.png", "check-sign.png" };
            foreach (var file in files)
            {
                var path = Art + file;
                AssetDatabase.ImportAsset(path);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.maxTextureSize = 2048;
                importer.SaveAndReimport();
            }
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
            TMP_Text source = null;
            foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
            {
                if (text.name == "ObjectiveTitle")
                {
                    source = text;
                    break;
                }
            }
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
            public TMP_Text Progress { get; }
            public RectTransform RewardRow { get; }
            public Button CloseButton { get; }

            public ClaimRefs(
                GameObject root,
                TMP_Text title,
                TMP_Text questTitle,
                TMP_Text reward,
                TMP_Text hint,
                TMP_Text progress,
                RectTransform rewardRow,
                Button closeButton)
            {
                Root = root;
                Title = title;
                QuestTitle = questTitle;
                Reward = reward;
                Hint = hint;
                Progress = progress;
                RewardRow = rewardRow;
                CloseButton = closeButton;
            }
        }
    }
}

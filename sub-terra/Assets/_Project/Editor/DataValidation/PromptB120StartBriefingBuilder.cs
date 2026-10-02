using System;
using SubTerra.App.Tutorial;
using SubTerra.App.UI;
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
    /// <summary>
    /// prompt-B 120 시작 브리핑(GuidancePanel)만 다시 만든다.
    /// 대상: Mine_Demo_Integration의 DemoObjectiveRoot/GuidancePanel 하나. 다른 패널·Prefab은 건드리지 않는다.
    /// </summary>
    public static class PromptB120StartBriefingBuilder
    {
        public const string IntegrationScenePath =
            "Assets/_Project/Scenes/App/Mine_Demo_Integration.unity";
        public const string FramePath =
            "Assets/_Project/Art/UI/Gameplay/Quest/Clear/quest-clear-popup-frame.png";
        private const string SettingsArt = "Assets/_Project/Art/UI/MainMenu/Settings/";

        // 프레임 그림(1774x887)의 보이는 외곽선과 가로 구분선 위치를 이 창 크기로 환산한 값.
        public static readonly Vector2 WindowSize = new Vector2(1700f, 850f);
        public static readonly Vector2 FrameHalfExtent = new Vector2(783f, 311f);
        private const float TitleY = 236f;
        private const float BodyY = 13f;
        private const float ButtonY = -222f;

        private static readonly Color Cyan = new Color(0.20f, 1f, 0.96f, 1f);

        [MenuItem("SubTerra/UI/Build Prompt-B 120 Start Briefing")]
        public static void BuildFromMenu()
        {
            Debug.Log("[SubTerra] " + Build());
        }

        public static string Build()
        {
            if (EditorApplication.isPlaying)
            {
                throw new InvalidOperationException("Stop Play Mode first.");
            }

            var scene = SceneManager.GetSceneByPath(IntegrationScenePath);
            if (scene.IsValid() && scene.isLoaded && scene.isDirty)
            {
                throw new InvalidOperationException("Mine_Demo_Integration에 저장되지 않은 변경이 있습니다.");
            }

            var closeAfter = !scene.IsValid() || !scene.isLoaded;
            if (closeAfter)
            {
                scene = EditorSceneManager.OpenScene(IntegrationScenePath, OpenSceneMode.Additive);
            }

            try
            {
                var root = FindInScene(scene, "DemoObjectiveRoot");
                if (root == null)
                {
                    throw new InvalidOperationException("DemoObjectiveRoot가 없습니다.");
                }

                var view = root.GetComponent<DemoObjectiveView>();
                var guidance = root.transform.Find("GuidancePanel") as RectTransform;
                if (view == null || guidance == null)
                {
                    throw new InvalidOperationException("DemoObjectiveView 또는 GuidancePanel이 없습니다.");
                }

                var font = ResolveFont(root.transform, guidance);
                Rebuild(guidance, view, font);

                EditorUtility.SetDirty(view);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene))
                {
                    throw new InvalidOperationException("Mine_Demo_Integration 저장에 실패했습니다.");
                }

                return "Prompt-B 120 start briefing built: " + IntegrationScenePath;
            }
            finally
            {
                if (closeAfter && scene.IsValid() && scene.isLoaded)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        private static void Rebuild(RectTransform guidance, DemoObjectiveView view, TMP_FontAsset font)
        {
            // 모달이라 드래그 이동·팝업 정렬 목록 등록 대상이 아니다.
            RemoveComponent<PopupWindowDrag>(guidance.gameObject);
            RemoveComponent<PopupWindowRaycastGate>(guidance.gameObject);
            var oldMotion = guidance.GetComponent<StartBriefingPopupMotion>();
            if (oldMotion != null)
            {
                UnityEngine.Object.DestroyImmediate(oldMotion);
            }

            for (var i = guidance.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.DestroyImmediate(guidance.GetChild(i).gameObject);
            }

            Stretch(guidance);
            var backdrop = guidance.GetComponent<Image>();
            backdrop.sprite = null;
            backdrop.color = new Color(0.01f, 0.02f, 0.04f, 0f);
            backdrop.raycastTarget = true;
            var rootGroup = guidance.GetComponent<CanvasGroup>();
            if (rootGroup == null)
            {
                rootGroup = guidance.gameObject.AddComponent<CanvasGroup>();
            }

            rootGroup.alpha = 1f;
            rootGroup.interactable = true;
            rootGroup.blocksRaycasts = true;

            var frameSprite = AssetDatabase.LoadAssetAtPath<Sprite>(FramePath);
            if (frameSprite == null)
            {
                throw new InvalidOperationException("프레임 그림이 없습니다: " + FramePath);
            }

            var window = NewRect(guidance, "Window");
            window.sizeDelta = WindowSize;

            // 프레임·장식 효과 레이어(글리치 대상). 본문 레이어보다 아래에 둔다.
            var glow = NewImage(window, "FrameGlow", frameSprite, Cyan);
            Fill(glow.rectTransform);
            var ghosts = new[]
            {
                NewImage(window, "FrameGhostA", frameSprite, Cyan),
                NewImage(window, "FrameGhostB", frameSprite, Color.white)
            };
            foreach (var ghost in ghosts)
            {
                Fill(ghost.rectTransform);
            }

            var frame = NewImage(window, "Frame", frameSprite, Color.white);
            Fill(frame.rectTransform);

            var edgeBars = new Image[10];
            for (var i = 0; i < edgeBars.Length; i++)
            {
                edgeBars[i] = NewImage(window, "EdgeNoise" + i, null, Cyan);
            }

            var signal = NewImage(window, "SignalLine", null, Cyan);
            var motes = new Image[8];
            for (var i = 0; i < motes.Length; i++)
            {
                motes[i] = NewImage(window, "SignalMote" + i, null, Cyan);
                motes[i].rectTransform.sizeDelta = new Vector2(10f, 10f);
            }

            // 본문 레이어: 알파만 바뀌고 위치·색·왜곡은 건드리지 않는다.
            var contentRect = NewRect(window, "Content");
            Fill(contentRect);
            var content = contentRect.gameObject.AddComponent<CanvasGroup>();

            var title = NewText(contentRect, "GuidanceTitle", font, 36f, FontStyles.Bold,
                new Color(0.4f, 1f, 1f, 1f), TextAlignmentOptions.MidlineLeft);
            Place(title.rectTransform, new Vector2(-680f + 550f, TitleY), new Vector2(1100f, 60f));
            title.text = DemoObjectiveCatalog.IntroductionGuidanceTitle;
            title.characterSpacing = 4f;

            var body = NewText(contentRect, "GuidanceBody", font, 30f, FontStyles.Normal,
                new Color(0.88f, 0.96f, 0.98f, 1f), TextAlignmentOptions.Center);
            Place(body.rectTransform, new Vector2(0f, BodyY), new Vector2(1360f, 292f));
            body.lineSpacing = 8f;
            body.textWrappingMode = TextWrappingModes.Normal;
            body.overflowMode = TextOverflowModes.Overflow;
            body.text = DemoObjectiveCatalog.IntroductionGuidanceBody;

            var buttonRect = NewRect(contentRect, "DismissButton");
            Place(buttonRect, new Vector2(0f, ButtonY), new Vector2(380f, 78f));
            var button = buttonRect.gameObject.AddComponent<Button>();
            buttonRect.gameObject.AddComponent<Image>();
            var navigation = button.navigation;
            navigation.mode = Navigation.Mode.None;
            button.navigation = navigation;
            PromptB117SurfaceBaseBuilder.StyleButton(
                button,
                SettingsArt + "button-active-off.png",
                SettingsArt + "button-active-on.png",
                new Vector2(380f, 78f));
            var label = NewText(buttonRect, "Label", font, 30f, FontStyles.Bold,
                new Color(0.9f, 0.99f, 1f, 1f), TextAlignmentOptions.Center);
            Fill(label.rectTransform);
            label.text = DemoObjectiveCatalog.IntroductionGuidanceConfirmLabel;
            label.transform.SetAsLastSibling();
            while (button.onClick.GetPersistentEventCount() > 0)
            {
                UnityEventTools.RemovePersistentListener(button.onClick, 0);
            }

            UnityEventTools.AddPersistentListener(button.onClick, view.OnDismissClicked);

            // 화면 글리치 레이어: 등장 직후 0.45초만 켜지는 전체 화면 오버레이. 입력은 받지 않는다.
            var screenRoot = NewRect(guidance, "ScreenGlitch");
            Fill(screenRoot);
            var slices = new RawImage[3];
            for (var i = 0; i < slices.Length; i++)
            {
                var sliceRect = NewRect(screenRoot, "TearSlice" + i);
                sliceRect.anchorMin = sliceRect.anchorMax = new Vector2(0.5f, 0.5f);
                slices[i] = sliceRect.gameObject.AddComponent<RawImage>();
                slices[i].raycastTarget = false;
            }

            var screenBars = new Image[8];
            for (var i = 0; i < screenBars.Length; i++)
            {
                screenBars[i] = NewImage(screenRoot, "NoiseBar" + i, null, Cyan);
            }

            screenRoot.gameObject.SetActive(false);

            var motion = guidance.gameObject.AddComponent<StartBriefingPopupMotion>();
            var motionSo = new SerializedObject(motion);
            Assign(motionSo, "window", window);
            motionSo.FindProperty("windowSize").vector2Value = WindowSize;
            motionSo.FindProperty("frameHalfExtent").vector2Value = FrameHalfExtent;
            Assign(motionSo, "frame", frame.rectTransform);
            Assign(motionSo, "frameImage", frame);
            Assign(motionSo, "frameGlow", glow);
            AssignArray(motionSo, "ghostFrames", ghosts);
            AssignArray(motionSo, "edgeBars", edgeBars);
            Assign(motionSo, "signalLine", signal);
            AssignArray(motionSo, "signalMotes", motes);
            Assign(motionSo, "screenGlitchRoot", screenRoot);
            AssignArray(motionSo, "screenBars", screenBars);
            AssignArray(motionSo, "tearSlices", slices);
            Assign(motionSo, "backdrop", backdrop);
            Assign(motionSo, "content", content);
            Assign(motionSo, "rootGroup", rootGroup);
            motionSo.ApplyModifiedPropertiesWithoutUndo();

            var viewSo = new SerializedObject(view);
            Assign(viewSo, "guidanceRoot", guidance.gameObject);
            Assign(viewSo, "guidanceTitleText", title);
            Assign(viewSo, "guidanceBodyText", body);
            Assign(viewSo, "guidanceCanvasGroup", rootGroup);
            Assign(viewSo, "guidanceMotion", motion);
            viewSo.ApplyModifiedPropertiesWithoutUndo();

            // 시작 시에는 닫혀 있고, 첫 목표가 열릴 때 Presenter가 켠다.
            guidance.gameObject.SetActive(false);
        }

        private static TMP_FontAsset ResolveFont(Transform root, Transform guidance)
        {
            var body = guidance.Find("GuidanceBody");
            var text = body != null ? body.GetComponent<TMP_Text>() : null;
            if (text != null && text.font != null)
            {
                return text.font;
            }

            var title = root.Find("ObjectiveTitle");
            text = title != null ? title.GetComponent<TMP_Text>() : null;
            if (text == null || text.font == null)
            {
                var button = root.Find("QuestSummaryButton");
                var nested = button != null ? button.Find("ObjectiveTitle") : null;
                text = nested != null ? nested.GetComponent<TMP_Text>() : null;
            }

            if (text == null || text.font == null)
            {
                throw new InvalidOperationException("브리핑에 쓸 한글 TMP 폰트를 찾지 못했습니다.");
            }

            return text.font;
        }

        private static GameObject FindInScene(Scene scene, string name)
        {
            var roots = scene.GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                var found = FindRecursive(roots[i].transform, name);
                if (found != null)
                {
                    return found.gameObject;
                }
            }

            return null;
        }

        private static Transform FindRecursive(Transform parent, string name)
        {
            if (parent.name == name)
            {
                return parent;
            }

            for (var i = 0; i < parent.childCount; i++)
            {
                var found = FindRecursive(parent.GetChild(i), name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static void RemoveComponent<T>(GameObject target) where T : Component
        {
            var component = target.GetComponent<T>();
            if (component != null)
            {
                UnityEngine.Object.DestroyImmediate(component);
            }
        }

        private static RectTransform NewRect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            return rect;
        }

        private static Image NewImage(Transform parent, string name, Sprite sprite, Color color)
        {
            var rect = NewRect(parent, name);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            if (sprite != null)
            {
                image.type = Image.Type.Simple;
                image.preserveAspect = false;
            }

            return image;
        }

        private static TextMeshProUGUI NewText(
            Transform parent,
            string name,
            TMP_FontAsset font,
            float size,
            FontStyles style,
            Color color,
            TextAlignmentOptions alignment)
        {
            var rect = NewRect(parent, name);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font;
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.alignment = alignment;
            text.enableAutoSizing = false;
            text.raycastTarget = false;
            return text;
        }

        private static void Place(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void Fill(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void Stretch(RectTransform rect) => Fill(rect);

        private static void Assign(SerializedObject serialized, string field, UnityEngine.Object value)
        {
            var property = serialized.FindProperty(field);
            if (property == null)
            {
                throw new InvalidOperationException("직렬화 필드가 없습니다: " + field);
            }

            property.objectReferenceValue = value;
        }

        private static void AssignArray<T>(SerializedObject serialized, string field, T[] values)
            where T : UnityEngine.Object
        {
            var property = serialized.FindProperty(field);
            if (property == null)
            {
                throw new InvalidOperationException("직렬화 필드가 없습니다: " + field);
            }

            property.arraySize = values.Length;
            for (var i = 0; i < values.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
        }
    }
}

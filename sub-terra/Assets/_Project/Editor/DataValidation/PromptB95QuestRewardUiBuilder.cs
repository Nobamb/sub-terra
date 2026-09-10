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
    /// <summary>prompt-B 95 퀘스트 상세 로그·보상 표시와 화물 부족 선택/버리기 팝업만 연결한다.</summary>
    public static class PromptB95QuestRewardUiBuilder
    {
        public const string IntegrationScenePath =
            "Assets/_Project/Scenes/App/Mine_Demo_Integration.unity";

        [MenuItem("SubTerra/UI/Build Prompt-B 95 Quest Reward Log")]
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
                var details = EnsureDetailsPanel(root.transform, font);
                var capacity = EnsureCapacityPanel(root.transform, font);
                var dump = EnsureDumpPanel(root.transform, font);

                var serialized = new SerializedObject(view);
                Assign(serialized, "detailsRoot", details.Root);
                Assign(serialized, "detailsTitleText", details.Title);
                Assign(serialized, "detailsBodyText", details.Body);
                Assign(serialized, "detailsNextActionText", details.NextAction);
                Assign(serialized, "detailsRewardText", details.Reward);
                Assign(serialized, "detailsStatusText", details.Status);
                Assign(serialized, "detailsIndexText", details.Index);
                Assign(serialized, "detailsPrevButton", details.PrevButton);
                Assign(serialized, "detailsNextButton", details.NextButton);
                Assign(serialized, "capacityRoot", capacity.Root);
                Assign(serialized, "capacityTitleText", capacity.Title);
                Assign(serialized, "capacityBodyText", capacity.Body);
                Assign(serialized, "dumpRoot", dump.Root);
                Assign(serialized, "dumpSummaryText", dump.Summary);
                Assign(serialized, "dumpCopperText", dump.CopperLabel);
                Assign(serialized, "dumpIronText", dump.IronLabel);
                Assign(serialized, "dumpLithiumText", dump.LithiumLabel);
                Assign(serialized, "dumpCopperOneButton", dump.CopperOne);
                Assign(serialized, "dumpCopperAllButton", dump.CopperAll);
                Assign(serialized, "dumpIronOneButton", dump.IronOne);
                Assign(serialized, "dumpIronAllButton", dump.IronAll);
                Assign(serialized, "dumpLithiumOneButton", dump.LithiumOne);
                Assign(serialized, "dumpLithiumAllButton", dump.LithiumAll);
                serialized.ApplyModifiedPropertiesWithoutUndo();

                Wire(details.CloseButton, view.OnDetailsDismissClicked);
                Wire(details.PrevButton, view.OnDetailsPrevClicked);
                Wire(details.NextButton, view.OnDetailsNextClicked);
                Wire(capacity.DumpButton, view.OnCapacityDumpClicked);
                Wire(capacity.ForfeitButton, view.OnCapacityForfeitClicked);
                Wire(dump.CloseButton, view.OnDumpClosedClicked);
                Wire(dump.CopperOne, view.OnDumpCopperOneClicked);
                Wire(dump.CopperAll, view.OnDumpCopperAllClicked);
                Wire(dump.IronOne, view.OnDumpIronOneClicked);
                Wire(dump.IronAll, view.OnDumpIronAllClicked);
                Wire(dump.LithiumOne, view.OnDumpLithiumOneClicked);
                Wire(dump.LithiumAll, view.OnDumpLithiumAllClicked);

                details.Root.SetActive(false);
                capacity.Root.SetActive(false);
                dump.Root.SetActive(false);

                EditorUtility.SetDirty(view);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene))
                {
                    throw new System.InvalidOperationException(
                        "Mine_Demo_Integration 저장에 실패했습니다.");
                }

                return "Prompt-B 95 quest reward log built: " + IntegrationScenePath;
            }
            finally
            {
                if (closeAfterBuild && scene.IsValid() && scene.isLoaded)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        private static DetailsRefs EnsureDetailsPanel(Transform root, TMP_FontAsset font)
        {
            var panel = EnsurePanel(root, "QuestDetailsPanel", new Vector2(860f, 560f));
            var title = EnsureText(
                panel.transform,
                "QuestDetailsTitle",
                new Vector2(84f, -26f),
                new Vector2(680f, 48f),
                24f,
                TextAlignmentOptions.TopLeft,
                font);
            var status = EnsureText(
                panel.transform,
                "QuestDetailsStatus",
                new Vector2(84f, -78f),
                new Vector2(680f, 32f),
                18f,
                TextAlignmentOptions.TopLeft,
                font);
            var body = EnsureText(
                panel.transform,
                "QuestDetailsBody",
                new Vector2(84f, -118f),
                new Vector2(692f, 160f),
                18f,
                TextAlignmentOptions.TopLeft,
                font);
            var next = EnsureText(
                panel.transform,
                "QuestDetailsNextAction",
                new Vector2(84f, -286f),
                new Vector2(692f, 56f),
                17f,
                TextAlignmentOptions.TopLeft,
                font);
            var reward = EnsureText(
                panel.transform,
                "QuestDetailsReward",
                new Vector2(84f, -350f),
                new Vector2(692f, 56f),
                18f,
                TextAlignmentOptions.TopLeft,
                font);
            var index = EnsureText(
                panel.transform,
                "QuestDetailsIndex",
                new Vector2(280f, -500f),
                new Vector2(300f, 40f),
                20f,
                TextAlignmentOptions.Center,
                font);
            var close = EnsureButton(
                panel.transform,
                "QuestDetailsCloseButton",
                "X",
                new Vector2(1f, 1f),
                new Vector2(-18f, -18f),
                new Vector2(44f, 44f),
                new Color(0.38f, 0.12f, 0.14f, 1f),
                font);
            var prev = EnsureButton(
                panel.transform,
                "QuestDetailsPrevButton",
                "<",
                new Vector2(0f, 0.5f),
                new Vector2(18f, 0f),
                new Vector2(52f, 72f),
                new Color(0.12f, 0.22f, 0.32f, 1f),
                font);
            var nextButton = EnsureButton(
                panel.transform,
                "QuestDetailsNextButton",
                ">",
                new Vector2(1f, 0.5f),
                new Vector2(-18f, 0f),
                new Vector2(52f, 72f),
                new Color(0.12f, 0.22f, 0.32f, 1f),
                font);
            panel.transform.SetAsLastSibling();
            return new DetailsRefs(
                panel,
                title,
                body,
                next,
                reward,
                status,
                index,
                close,
                prev,
                nextButton);
        }

        private static CapacityRefs EnsureCapacityPanel(Transform root, TMP_FontAsset font)
        {
            var panel = EnsurePanel(root, "QuestRewardCapacityPanel", new Vector2(720f, 360f));
            var title = EnsureText(
                panel.transform,
                "CapacityTitle",
                new Vector2(28f, -24f),
                new Vector2(664f, 48f),
                24f,
                TextAlignmentOptions.TopLeft,
                font);
            var body = EnsureText(
                panel.transform,
                "CapacityBody",
                new Vector2(28f, -84f),
                new Vector2(664f, 140f),
                18f,
                TextAlignmentOptions.TopLeft,
                font);
            var dump = EnsureButton(
                panel.transform,
                "CapacityDumpButton",
                "화물 비우기",
                new Vector2(0.5f, 0f),
                new Vector2(-150f, 28f),
                new Vector2(240f, 52f),
                new Color(0.16f, 0.32f, 0.22f, 1f),
                font);
            var forfeit = EnsureButton(
                panel.transform,
                "CapacityForfeitButton",
                "보상 포기",
                new Vector2(0.5f, 0f),
                new Vector2(150f, 28f),
                new Vector2(240f, 52f),
                new Color(0.38f, 0.12f, 0.14f, 1f),
                font);
            panel.transform.SetAsLastSibling();
            return new CapacityRefs(panel, title, body, dump, forfeit);
        }

        private static DumpRefs EnsureDumpPanel(Transform root, TMP_FontAsset font)
        {
            var panel = EnsurePanel(root, "QuestRewardDumpPanel", new Vector2(720f, 480f));
            EnsureText(
                panel.transform,
                "DumpTitle",
                new Vector2(28f, -22f),
                new Vector2(600f, 40f),
                24f,
                TextAlignmentOptions.TopLeft,
                font).text = "화물 버리기";
            var summary = EnsureText(
                panel.transform,
                "DumpSummary",
                new Vector2(28f, -68f),
                new Vector2(664f, 40f),
                16f,
                TextAlignmentOptions.TopLeft,
                font);
            var copper = EnsureDumpRow(panel.transform, "DumpCopper", -120f, font);
            var iron = EnsureDumpRow(panel.transform, "DumpIron", -188f, font);
            var lithium = EnsureDumpRow(panel.transform, "DumpLithium", -256f, font);
            var close = EnsureButton(
                panel.transform,
                "DumpCloseButton",
                "돌아가기",
                new Vector2(0.5f, 0f),
                new Vector2(0f, 28f),
                new Vector2(240f, 52f),
                new Color(0.18f, 0.22f, 0.3f, 1f),
                font);
            panel.transform.SetAsLastSibling();
            return new DumpRefs(
                panel,
                summary,
                copper.Label,
                iron.Label,
                lithium.Label,
                copper.One,
                copper.All,
                iron.One,
                iron.All,
                lithium.One,
                lithium.All,
                close);
        }

        private static DumpRowRefs EnsureDumpRow(
            Transform parent,
            string name,
            float y,
            TMP_FontAsset font)
        {
            var existing = parent.Find(name);
            GameObject row;
            if (existing == null)
            {
                row = new GameObject(name, typeof(RectTransform));
                row.transform.SetParent(parent, false);
            }
            else
            {
                row = existing.gameObject;
            }

            var rect = row.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(28f, y);
            rect.sizeDelta = new Vector2(664f, 60f);

            var label = EnsureText(
                row.transform,
                "Label",
                new Vector2(0f, 0f),
                new Vector2(280f, 60f),
                18f,
                TextAlignmentOptions.MidlineLeft,
                font);
            var one = EnsureButton(
                row.transform,
                "DumpOne",
                "1개 버리기",
                new Vector2(1f, 0.5f),
                new Vector2(-170f, 0f),
                new Vector2(150f, 44f),
                new Color(0.28f, 0.18f, 0.12f, 1f),
                font);
            var all = EnsureButton(
                row.transform,
                "DumpAll",
                "전부 버리기",
                new Vector2(1f, 0.5f),
                new Vector2(-8f, 0f),
                new Vector2(150f, 44f),
                new Color(0.32f, 0.14f, 0.12f, 1f),
                font);
            return new DumpRowRefs(label, one, all);
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

        private readonly struct DetailsRefs
        {
            public GameObject Root { get; }
            public TMP_Text Title { get; }
            public TMP_Text Body { get; }
            public TMP_Text NextAction { get; }
            public TMP_Text Reward { get; }
            public TMP_Text Status { get; }
            public TMP_Text Index { get; }
            public Button CloseButton { get; }
            public Button PrevButton { get; }
            public Button NextButton { get; }

            public DetailsRefs(
                GameObject root,
                TMP_Text title,
                TMP_Text body,
                TMP_Text nextAction,
                TMP_Text reward,
                TMP_Text status,
                TMP_Text index,
                Button closeButton,
                Button prevButton,
                Button nextButton)
            {
                Root = root;
                Title = title;
                Body = body;
                NextAction = nextAction;
                Reward = reward;
                Status = status;
                Index = index;
                CloseButton = closeButton;
                PrevButton = prevButton;
                NextButton = nextButton;
            }
        }

        private readonly struct CapacityRefs
        {
            public GameObject Root { get; }
            public TMP_Text Title { get; }
            public TMP_Text Body { get; }
            public Button DumpButton { get; }
            public Button ForfeitButton { get; }

            public CapacityRefs(
                GameObject root,
                TMP_Text title,
                TMP_Text body,
                Button dumpButton,
                Button forfeitButton)
            {
                Root = root;
                Title = title;
                Body = body;
                DumpButton = dumpButton;
                ForfeitButton = forfeitButton;
            }
        }

        private readonly struct DumpRefs
        {
            public GameObject Root { get; }
            public TMP_Text Summary { get; }
            public TMP_Text CopperLabel { get; }
            public TMP_Text IronLabel { get; }
            public TMP_Text LithiumLabel { get; }
            public Button CopperOne { get; }
            public Button CopperAll { get; }
            public Button IronOne { get; }
            public Button IronAll { get; }
            public Button LithiumOne { get; }
            public Button LithiumAll { get; }
            public Button CloseButton { get; }

            public DumpRefs(
                GameObject root,
                TMP_Text summary,
                TMP_Text copperLabel,
                TMP_Text ironLabel,
                TMP_Text lithiumLabel,
                Button copperOne,
                Button copperAll,
                Button ironOne,
                Button ironAll,
                Button lithiumOne,
                Button lithiumAll,
                Button closeButton)
            {
                Root = root;
                Summary = summary;
                CopperLabel = copperLabel;
                IronLabel = ironLabel;
                LithiumLabel = lithiumLabel;
                CopperOne = copperOne;
                CopperAll = copperAll;
                IronOne = ironOne;
                IronAll = ironAll;
                LithiumOne = lithiumOne;
                LithiumAll = lithiumAll;
                CloseButton = closeButton;
            }
        }

        private readonly struct DumpRowRefs
        {
            public TMP_Text Label { get; }
            public Button One { get; }
            public Button All { get; }

            public DumpRowRefs(TMP_Text label, Button one, Button all)
            {
                Label = label;
                One = one;
                All = all;
            }
        }
    }
}

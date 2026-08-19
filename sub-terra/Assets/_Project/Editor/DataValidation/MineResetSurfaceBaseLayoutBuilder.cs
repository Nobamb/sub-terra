using SubTerra.App.UI.SurfaceBase;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.Editor.DataValidation
{
    /// <summary>새 광산 버튼과 확인 모달을 Surface Base 프리팹에만 적용한다.</summary>
    public static class MineResetSurfaceBaseLayoutBuilder
    {
        public const string SurfaceBasePrefabPath =
            "Assets/_Project/Prefabs/UI/SurfaceBasePanel.prefab";
        public const string SurfaceBaseScenePath =
            "Assets/_Project/Scenes/App/SurfaceBase.unity";
        public const float ResetButtonY = 8f;
        public const float MessageY = -48f;

        [MenuItem("SubTerra/UI/Build Mine Reset (SurfaceBase only)")]
        public static void BuildFromMenu()
        {
            Debug.Log("[SubTerra] " + Build());
        }

        public static string Build()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(SurfaceBasePrefabPath) == null)
            {
                return "SKIP: SurfaceBase prefab missing";
            }

            var root = PrefabUtility.LoadPrefabContents(SurfaceBasePrefabPath);
            try
            {
                var content = root.transform.Find("SurfaceBaseContent") ?? root.transform;
                var resetButton = EnsureButton(
                    content,
                    "ResetMineButton",
                    new Vector2(0f, ResetButtonY),
                    new Vector2(320f, 48f),
                    "새 광산 초기화 (500G)");

                PlaceCentered(content.Find("MessageText") as RectTransform, MessageY, 720f, 32f);

                var confirmRoot = EnsureConfirmModal(root.transform,
                    out var title,
                    out var body,
                    out var yes,
                    out var no);

                var view = root.GetComponent<SurfaceBaseView>();
                if (view == null)
                {
                    view = root.AddComponent<SurfaceBaseView>();
                }

                var so = new SerializedObject(view);
                so.FindProperty("resetMineButton").objectReferenceValue = resetButton;
                so.FindProperty("resetMineConfirmRoot").objectReferenceValue = confirmRoot;
                so.FindProperty("resetMineConfirmTitleText").objectReferenceValue = title;
                so.FindProperty("resetMineConfirmBodyText").objectReferenceValue = body;
                so.FindProperty("resetMineConfirmYesButton").objectReferenceValue = yes;
                so.FindProperty("resetMineConfirmNoButton").objectReferenceValue = no;
                so.ApplyModifiedPropertiesWithoutUndo();

                confirmRoot.SetActive(false);
                PrefabUtility.SaveAsPrefabAsset(root, SurfaceBasePrefabPath);
                AssetDatabase.SaveAssets();
                return "SurfaceBasePrefab mine reset controls";
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static GameObject EnsureConfirmModal(
            Transform root,
            out TMP_Text title,
            out TMP_Text body,
            out Button yes,
            out Button no)
        {
            var existing = root.Find("ResetMineConfirm");
            var modal = existing != null
                ? existing.gameObject
                : new GameObject(
                    "ResetMineConfirm",
                    typeof(RectTransform),
                    typeof(Canvas),
                    typeof(GraphicRaycaster),
                    typeof(Image));
            if (existing == null)
            {
                modal.transform.SetParent(root, false);
            }

            var modalRect = modal.GetComponent<RectTransform>();
            Stretch(modalRect);
            var backdrop = modal.GetComponent<Image>();
            backdrop.color = new Color(0.01f, 0.015f, 0.025f, 0.88f);
            backdrop.raycastTarget = true;
            var canvas = modal.GetComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = 700;

            var card = EnsureChild(modal.transform, "ResetMineCard", typeof(Image));
            PlaceCentered(card, 0f, 720f, 480f);
            var cardImage = card.GetComponent<Image>();
            cardImage.color = new Color(0.06f, 0.095f, 0.11f, 1f);
            cardImage.raycastTarget = true;

            title = EnsureText(
                card,
                "Title",
                new Vector2(0f, 180f),
                new Vector2(640f, 52f),
                30f,
                "새 광산 구역");
            body = EnsureText(
                card,
                "Body",
                new Vector2(0f, 30f),
                new Vector2(640f, 250f),
                20f,
                "이용료 500G를 내고 지하를 새로 배치합니다.\n"
                + "캔 타일, 지하 시설, 붕괴와 가스 상태가 사라집니다.\n"
                + "업그레이드, 심층 해금, 보유 광물, 남은 골드는 유지됩니다.\n"
                + "현재 골드 {0} → {1}");
            body.alignment = TextAlignmentOptions.MidlineLeft;

            yes = EnsureButton(
                card,
                "ConfirmButton",
                new Vector2(-130f, -178f),
                new Vector2(220f, 56f),
                "확인");
            no = EnsureButton(
                card,
                "CancelButton",
                new Vector2(130f, -178f),
                new Vector2(220f, 56f),
                "취소");
            modal.transform.SetAsLastSibling();
            return modal;
        }

        private static Button EnsureButton(
            Transform parent,
            string name,
            Vector2 position,
            Vector2 size,
            string label)
        {
            var rect = EnsureChild(parent, name, typeof(Image), typeof(Button));
            Place(rect, position, size);
            var image = rect.GetComponent<Image>();
            image.color = new Color(0.12f, 0.36f, 0.31f, 1f);
            image.raycastTarget = true;
            var button = rect.GetComponent<Button>();
            button.targetGraphic = image;
            EnsureText(rect, "Label", Vector2.zero, size - new Vector2(20f, 8f), 20f, label);
            return button;
        }

        private static RectTransform EnsureChild(Transform parent, string name, params System.Type[] components)
        {
            var found = parent.Find(name) as RectTransform;
            if (found == null)
            {
                var types = new System.Type[components.Length + 1];
                types[0] = typeof(RectTransform);
                components.CopyTo(types, 1);
                var go = new GameObject(name, types);
                go.transform.SetParent(parent, false);
                found = go.GetComponent<RectTransform>();
            }

            for (var i = 0; i < components.Length; i++)
            {
                if (found.GetComponent(components[i]) == null)
                {
                    found.gameObject.AddComponent(components[i]);
                }
            }

            return found;
        }

        private static TMP_Text EnsureText(
            Transform parent,
            string name,
            Vector2 position,
            Vector2 size,
            float fontSize,
            string value)
        {
            var rect = EnsureChild(parent, name, typeof(TextMeshProUGUI));
            Place(rect, position, size);
            var text = rect.GetComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = fontSize;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            return text;
        }

        private static void PlaceCentered(RectTransform rect, float y, float width, float height)
        {
            if (rect != null)
            {
                Place(rect, new Vector2(0f, y), new Vector2(width, height));
            }
        }

        private static void Place(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}

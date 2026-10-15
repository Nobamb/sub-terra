using SubTerra.App.UI.HUD;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.Editor.DataValidation
{
    /// <summary>
    /// Prompt-B 146: 가스 중독 화면 연출(가장자리 비네팅 + 전체 필터)을 HUDCanvas.prefab에 넣는다.
    /// 대상은 HUDCanvas.prefab뿐이다. 오버레이는 캔버스의 첫 번째 자식이라 HUD 요소보다 뒤에 그려지고,
    /// 팝업·이름표 캔버스(정렬 순서 100 이상)보다도 아래에 있다. Image는 모두 raycastTarget을 끈다.
    /// </summary>
    public static class PromptB146GasPoisonOverlayBuilder
    {
        public const string HudCanvasPath = "Assets/_Project/Prefabs/UI/HUDCanvas.prefab";
        public const string RootName = "GasPoisonOverlay";
        public const string FilterName = "GasPoisonFilter";
        public const string StreakName = "GasPoisonStreaks";
        public const string VignetteName = "GasPoisonVignette";

        [MenuItem("SubTerra/UI/Build Prompt-B 146 Gas Poison Overlay")]
        public static void Build()
        {
            if (EditorApplication.isPlaying)
            {
                throw new System.InvalidOperationException("Stop Play Mode first.");
            }

            var root = PrefabUtility.LoadPrefabContents(HudCanvasPath);
            try
            {
                var old = root.transform.Find(RootName);
                if (old != null)
                {
                    Object.DestroyImmediate(old.gameObject);
                }

                var overlay = NewStretched(root.transform, RootName);
                overlay.SetSiblingIndex(0);

                // 필터 → 방사 줄무늬 → 비네팅 순으로 쌓는다. 모두 빈 칸 클릭을 막지 않는다.
                var filter = NewImage(overlay, FilterName);
                var streaks = NewImage(overlay, StreakName);
                var vignette = NewImage(overlay, VignetteName);

                var view = overlay.gameObject.AddComponent<GasPoisonOverlayView>();
                var so = new SerializedObject(view);
                so.FindProperty("vignetteImage").objectReferenceValue = vignette;
                so.FindProperty("filterImage").objectReferenceValue = filter;
                so.FindProperty("streakImage").objectReferenceValue = streaks;
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, HudCanvasPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            AssetDatabase.SaveAssets();
        }

        private static Image NewImage(Transform parent, string name)
        {
            var rect = NewStretched(parent, name);
            var image = rect.gameObject.AddComponent<Image>();
            image.raycastTarget = false;
            image.color = new Color(1f, 1f, 1f, 0f);
            image.enabled = false;
            return image;
        }

        private static RectTransform NewStretched(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
            return rect;
        }
    }
}

using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.Editor.DataValidation
{
    /// <summary>
    /// Applies only the Surface Base settings-modal layout.
    /// This intentionally does not open scenes or modify other UI prefabs.
    /// </summary>
    public static class SurfaceBaseSettingsModalLayoutBuilder
    {
        private const string SurfaceBasePrefabPath =
            "Assets/_Project/Prefabs/UI/SurfaceBasePanel.prefab";
        private const string MainMenuPrefabPath =
            "Assets/_Project/Prefabs/UI/MainMenuPanel.prefab";

        private const float CardWidth = 680f;
        private const float CardHeight = 800f;
        private const float DropdownHeight = 36f;
        // Three option rows. Longer lists remain scrollable instead of covering other rows.
        private const float DropdownPopupHeight = 84f;

        [MenuItem("SubTerra/UI/Build Surface Base Settings Modal Layout")]
        public static void BuildFromMenu()
        {
            Debug.Log("[SubTerra] " + BuildSurfaceBase());
        }

        [MenuItem("SubTerra/UI/Build Settings Modal Layout (Main + Surface Base)")]
        public static void BuildAllFromMenu()
        {
            Debug.Log("[SubTerra] " + BuildAll());
        }

        public static string BuildSurfaceBase()
        {
            return BuildPrefab(SurfaceBasePrefabPath, "Surface Base");
        }

        public static string BuildAll()
        {
            return BuildPrefab(SurfaceBasePrefabPath, "Surface Base") + "\n" +
                   BuildPrefab(MainMenuPrefabPath, "Main Menu");
        }

        private static string BuildPrefab(string prefabPath, string label)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) == null)
            {
                return "SKIP: " + label + " settings prefab missing";
            }

            var root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                var settingsPanel = FindChild(root.transform, "SettingsPanel") as RectTransform;
                if (settingsPanel == null)
                {
                    return "SKIP: " + label + " SettingsPanel missing";
                }

                ConfigureFullScreenBlocker(settingsPanel);
                ConfigureCard(settingsPanel);
                LayoutSettingsRows(settingsPanel);
                ConfigureDropdown(
                    FindChild(settingsPanel, "ResolutionDropdown")?.GetComponent<TMP_Dropdown>(),
                    opensUpward: false);
                ConfigureDropdown(
                    FindChild(settingsPanel, "LanguageDropdown")?.GetComponent<TMP_Dropdown>(),
                    opensUpward: false);
                ConfigureDropdown(
                    FindChild(settingsPanel, "FrameRateDropdown")?.GetComponent<TMP_Dropdown>(),
                    opensUpward: true);

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                AssetDatabase.SaveAssets();
                return label + " settings modal layout applied";
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void ConfigureFullScreenBlocker(RectTransform settingsPanel)
        {
            settingsPanel.anchorMin = Vector2.zero;
            settingsPanel.anchorMax = Vector2.one;
            settingsPanel.pivot = new Vector2(0.5f, 0.5f);
            settingsPanel.offsetMin = Vector2.zero;
            settingsPanel.offsetMax = Vector2.zero;

            var image = settingsPanel.GetComponent<Image>();
            if (image == null)
            {
                image = settingsPanel.gameObject.AddComponent<Image>();
            }

            image.color = new Color(0.005f, 0.012f, 0.02f, 0.82f);
            image.raycastTarget = true;
            EditorUtility.SetDirty(settingsPanel);
            EditorUtility.SetDirty(image);
        }

        private static void ConfigureCard(RectTransform settingsPanel)
        {
            var card = settingsPanel.Find("SettingsCard") as RectTransform;
            if (card == null)
            {
                var cardObject = new GameObject("SettingsCard", typeof(RectTransform), typeof(Image));
                cardObject.transform.SetParent(settingsPanel, false);
                card = cardObject.GetComponent<RectTransform>();
            }

            card.anchorMin = new Vector2(0.5f, 0.5f);
            card.anchorMax = new Vector2(0.5f, 0.5f);
            card.pivot = new Vector2(0.5f, 0.5f);
            card.anchoredPosition = Vector2.zero;
            card.sizeDelta = new Vector2(CardWidth, CardHeight);
            card.SetAsFirstSibling();

            var image = card.GetComponent<Image>();
            image.color = new Color(0.035f, 0.07f, 0.105f, 1f);
            image.raycastTarget = true;
            EditorUtility.SetDirty(card);
            EditorUtility.SetDirty(image);
        }

        private static void LayoutSettingsRows(Transform settingsPanel)
        {
            Place(settingsPanel, "SettingsTitle", 350f, 44f, 520f);
            Place(settingsPanel, "MasterVolumeLabel", 290f, 26f, 500f);
            Place(settingsPanel, "MasterVolume", 248f, 30f, 460f);
            Place(settingsPanel, "ResolutionLabel", 184f, 24f, 320f);
            Place(settingsPanel, "ResolutionDropdown", 146f, DropdownHeight, 320f);
            Place(settingsPanel, "ReduceMotionGroup", -4f, 34f, 460f);
            Place(settingsPanel, "LanguageLabel", -58f, 24f, 320f);
            Place(settingsPanel, "LanguageDropdown", -96f, DropdownHeight, 320f);
            Place(settingsPanel, "FrameRateLabel", -176f, 24f, 320f);
            Place(settingsPanel, "FrameRateDropdown", -214f, DropdownHeight, 320f);
            Place(settingsPanel, "BgmHint", -272f, 30f, 560f);
            Place(settingsPanel, "SettingsApply", -330f, 42f, 132f, -150f);
            Place(settingsPanel, "SettingsCancel", -330f, 42f, 132f, 0f);
            Place(settingsPanel, "SettingsDefaults", -330f, 42f, 132f, 150f);
        }

        private static void Place(
            Transform parent,
            string name,
            float y,
            float height,
            float width,
            float x = 0f)
        {
            var rect = FindChild(parent, name) as RectTransform;
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
            EditorUtility.SetDirty(rect);
        }

        private static void ConfigureDropdown(TMP_Dropdown dropdown, bool opensUpward)
        {
            if (dropdown == null || dropdown.template == null)
            {
                return;
            }

            var template = dropdown.template;
            template.anchorMin = new Vector2(0f, opensUpward ? 1f : 0f);
            template.anchorMax = new Vector2(1f, opensUpward ? 1f : 0f);
            template.pivot = new Vector2(0.5f, opensUpward ? 0f : 1f);
            template.anchoredPosition = new Vector2(0f, opensUpward ? 4f : -4f);
            template.sizeDelta = new Vector2(0f, DropdownPopupHeight);

            var templateImage = template.GetComponent<Image>();
            if (templateImage != null)
            {
                templateImage.color = new Color(0.055f, 0.095f, 0.14f, 1f);
                templateImage.raycastTarget = true;
                EditorUtility.SetDirty(templateImage);
            }

            var scroll = template.GetComponent<ScrollRect>();
            if (scroll != null)
            {
                scroll.vertical = true;
                scroll.horizontal = false;
                scroll.movementType = ScrollRect.MovementType.Clamped;
                EditorUtility.SetDirty(scroll);
            }

            ConfigureScrollableOptionTemplate(template);

            EditorUtility.SetDirty(template);
            EditorUtility.SetDirty(dropdown);
        }

        private static void ConfigureScrollableOptionTemplate(RectTransform template)
        {
            var viewport = template.Find("Viewport") as RectTransform;
            var content = viewport != null ? viewport.Find("Content") as RectTransform : null;
            var item = content != null ? content.Find("Item") as RectTransform : null;
            if (viewport == null || content == null || item == null)
            {
                return;
            }

            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.offsetMin = Vector2.zero;
            viewport.offsetMax = Vector2.zero;

            // TMP_Dropdown positions every cloned option from the top edge.
            // Keeping these templates top-aligned is required for the options to render.
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(0f, 28f);

            item.anchorMin = new Vector2(0f, 1f);
            item.anchorMax = new Vector2(1f, 1f);
            item.pivot = new Vector2(0.5f, 1f);
            item.anchoredPosition = Vector2.zero;
            item.sizeDelta = new Vector2(0f, 28f);

            EditorUtility.SetDirty(viewport);
            EditorUtility.SetDirty(content);
            EditorUtility.SetDirty(item);
        }

        private static Transform FindChild(Transform parent, string name)
        {
            if (parent == null)
            {
                return null;
            }

            foreach (var child in parent.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == name)
                {
                    return child;
                }
            }

            return null;
        }
    }
}

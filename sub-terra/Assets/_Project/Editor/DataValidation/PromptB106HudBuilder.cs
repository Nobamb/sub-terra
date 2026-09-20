using System;
using System.Linq;
using SubTerra.App.UI.HUD;
using SubTerra.App.UI.Hazards;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SubTerra.App.Editor.DataValidation
{
    public static class PromptB106HudBuilder
    {
        public const string Art = "Assets/_Project/Art/UI/Gameplay/HUD/";
        public const string Prefab = "Assets/_Project/Prefabs/UI/BasicHUD.prefab";
        public const string Scene = "Assets/_Project/Scenes/App/Mine_Demo_Integration.unity";

        [MenuItem("SubTerra/UI/Build Prompt-B 106 Player HUD")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode first.");
            foreach (string file in new[] { "HUD-frame", "hp-empty", "energy-empty", "hp-bar", "energy-bar", "bar-frame", "hud-icons" })
            {
                string path = Art + file + ".png";
                AssetDatabase.ImportAsset(path);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.maxTextureSize = 4096;
                importer.SaveAndReimport();
            }
            var existing = SceneManager.GetSceneByPath(Scene);
            if (existing.isLoaded && existing.isDirty) throw new InvalidOperationException("Integration scene has unsaved changes.");
            var root = PrefabUtility.LoadPrefabContents(Prefab);
            try
            {
                Layout(root.GetComponent<BasicHudView>());
                PrefabUtility.SaveAsPrefabAsset(root, Prefab);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            bool opened = !existing.isLoaded;
            var scene = opened ? EditorSceneManager.OpenScene(Scene, OpenSceneMode.Additive) : existing;
            try
            {
                var hud = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<BasicHudView>(true)).Single();
                Layout(hud);
                var hazard = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<HazardHudView>(true)).Single();
                var so = new SerializedObject(hazard);
                var power = (TMP_Text)so.FindProperty("powerText").objectReferenceValue;
                // HUDCanvas의 기존 전력망 텍스트/바인딩을 유지한 채 프레임 하단에 맞춘다.
                Place(power.rectTransform, 68, 221, 360, 30);
                power.fontSize = 17;
                power.enableAutoSizing = true;
                power.fontSizeMin = 13;
                power.fontSizeMax = 17;
                power.raycastTarget = false;
                PrefabUtility.RecordPrefabInstancePropertyModifications(power);
                PrefabUtility.RecordPrefabInstancePropertyModifications(power.rectTransform);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
        }

        private static void Layout(BasicHudView view)
        {
            var root = (RectTransform)view.transform;
            Place(root, 12, 12, 430, 248);
            var oldBackground = root.GetComponent<UnityEngine.UI.Image>();
            if (oldBackground != null) oldBackground.enabled = false;
            var background = Picture(root, "HudFrame", "HUD-frame", 0, 0, 430, 248);
            background.transform.SetAsFirstSibling();
            var health = Gauge(root, "HealthGauge", "hp", 122, 20);
            var energy = Gauge(root, "EnergyGauge", "energy", 122, 65);
            Set(view, "healthGauge", health);
            Set(view, "energyGauge", energy);
            Label(view.HealthText, 126, 19, 278, 34, 22, TextAlignmentOptions.Center);
            Label(view.EnergyText, 126, 64, 278, 34, 22, TextAlignmentOptions.Center);
            view.HealthText.text = "100 / 100";
            view.EnergyText.text = "100 / 100";
            StaticLabel(root, view.EnergyText, "HealthLabel", "체력", 61, 22, 57, 30);
            StaticLabel(root, view.EnergyText, "EnergyLabel", "전력", 61, 67, 57, 30);
            Label(view.DepthText, 60, 119, 146, 31, 21);
            Label(view.GoldText, 60, 154, 146, 31, 21);
            Label(view.CargoText, 257, 119, 159, 31, 20);
            Label(view.UnsettledValueText, 257, 154, 159, 31, 19);
            view.DepthText.text = "깊이 0m"; view.GoldText.text = "골드 0G";
            view.CargoText.text = "화물 0"; view.UnsettledValueText.text = "미정산 0G";
            // 컨셉 바깥의 기존 안내는 제거하지 않고 호출 시 표시되는 보조 행으로 보존한다.
            Label(view.BuildingSelectionText, 16, 251, 404, 22, 15);
            Label(view.InteractionPromptText, 450, 12, 404, 22, 15);
            for (int i = 0; i < 7; i++)
            {
                float[] xs = { 22, 22, 22, 22, 219, 219, 22 };
                float[] ys = { 23, 68, 122, 157, 122, 157, 211 };
                Icon(root, i, xs[i], ys[i]);
            }
            Line(root, "TopSeparator", 18, 108, 394, 1);
            Line(root, "BottomSeparator", 18, 196, 394, 1);
            Line(root, "ColumnSeparator", 208, 117, 1, 70);
            EditorUtility.SetDirty(view);
            PrefabUtility.RecordPrefabInstancePropertyModifications(view);
            PrefabUtility.RecordPrefabInstancePropertyModifications(root);
        }

        private static HudGaugeView Gauge(Transform parent, string name, string kind, float x, float y)
        {
            var rect = Rect(parent, name);
            Place(rect, x, y, 288, 38);
            Picture(rect, "Empty", kind + "-empty", 0, 0, 288, 38);
            var fill = Picture(rect, "Fill", kind + "-bar", 0, 0, 288, 38);
            fill.type = UnityEngine.UI.Image.Type.Filled;
            fill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
            fill.fillOrigin = 0;
            fill.fillAmount = 1f;
            Picture(rect, "Frame", "bar-frame", 0, 0, 288, 38).transform.SetAsLastSibling();
            var gauge = rect.GetComponent<HudGaugeView>();
            if (gauge == null) gauge = rect.gameObject.AddComponent<HudGaugeView>();
            Set(gauge, "fill", fill);
            return gauge;
        }

        private static void Icon(Transform root, int index, float x, float y)
        {
            var rect = Rect(root, "HudIcon" + index);
            Place(rect, x, y, 28, 28);
            var image = rect.GetComponent<UnityEngine.UI.RawImage>();
            if (image == null) image = rect.gameObject.AddComponent<UnityEngine.UI.RawImage>();
            image.texture = AssetDatabase.LoadAssetAtPath<Texture2D>(Art + "hud-icons.png");
            // 생성된 단일 아틀라스의 실제 아이콘 경계. 투명 여백을 제외하고 UV로 참조한다.
            float[] left = { 15, 345, 615, 907, 1210, 1545, 1864 };
            float[] right = { 291, 553, 838, 1173, 1490, 1773, 2040 };
            float width = image.texture.width;
            float height = image.texture.height;
            image.uvRect = new Rect(left[index] / width, (height - 503) / height,
                (right[index] - left[index]) / width, 294 / height);
            image.raycastTarget = false;
        }

        private static void Line(Transform root, string name, float x, float y, float w, float h)
        {
            var rect = Rect(root, name); Place(rect, x, y, w, h);
            var image = rect.GetComponent<UnityEngine.UI.Image>();
            if (image == null) image = rect.gameObject.AddComponent<UnityEngine.UI.Image>();
            image.color = new Color(0, 0.78f, 0.87f, 0.5f);
            image.raycastTarget = false;
        }

        private static void StaticLabel(Transform parent, TextMeshProUGUI template, string name, string text, float x, float y, float w, float h)
        {
            var rect = Rect(parent, name);
            var label = rect.GetComponent<TextMeshProUGUI>();
            if (label == null) label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.font = template.font;
            label.text = text;
            Label(label, x, y, w, h, 21);
        }

        private static void Label(TMP_Text text, float x, float y, float w, float h, float size,
            TextAlignmentOptions alignment = TextAlignmentOptions.MidlineLeft)
        {
            Place(text.rectTransform, x, y, w, h);
            text.fontSize = size; text.enableAutoSizing = false;
            text.color = Color.white; text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.raycastTarget = false;
            text.transform.SetAsLastSibling();
            PrefabUtility.RecordPrefabInstancePropertyModifications(text);
            PrefabUtility.RecordPrefabInstancePropertyModifications(text.rectTransform);
        }

        private static UnityEngine.UI.Image Picture(Transform parent, string name, string file, float x, float y, float w, float h)
        {
            var rect = Rect(parent, name); Place(rect, x, y, w, h);
            var image = rect.GetComponent<UnityEngine.UI.Image>();
            if (image == null) image = rect.gameObject.AddComponent<UnityEngine.UI.Image>();
            image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Art + file + ".png");
            image.color = Color.white; image.raycastTarget = false;
            return image;
        }

        private static RectTransform Rect(Transform parent, string name)
        {
            var child = parent.Find(name) as RectTransform;
            if (child == null)
            {
                child = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
                child.SetParent(parent, false);
            }
            return child;
        }

        private static void Place(RectTransform rect, float x, float y, float w, float h)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(w, h);
        }

        private static void Set(UnityEngine.Object target, string field, UnityEngine.Object value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}

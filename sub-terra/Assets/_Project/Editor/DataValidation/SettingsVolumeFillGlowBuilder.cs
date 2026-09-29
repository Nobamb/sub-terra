using System;
using System.IO;
using System.Linq;
using SubTerra.App.UI.MainMenu;
using SubTerra.App.UI.SurfaceBase;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SubTerra.App.Editor.DataValidation
{
    /// <summary>설정 창 마스터 음량 슬라이더의 필 글로우만 갱신한다. 다른 설정 창 레이아웃은 건드리지 않는다.</summary>
    public static class SettingsVolumeFillGlowBuilder
    {
        public const string TexturePath = PromptB104SettingsMenuBuilder.SliderFillGlowPath;
        public const int TextureWidth = 128;
        public const int TextureHeight = 64;
        public const int CapRadius = TextureHeight / 2;
        public const float PeakAlpha = 0.6f;
        // 필 양 끝 바깥으로 살짝만 번지도록 좌우 8px, 위아래 20px씩 확장
        public static readonly Vector2 GlowSizeDelta = new Vector2(16f, 48f);
        private static readonly Color32 GlowCyan = new Color32(74, 224, 242, 255);

        [MenuItem("SubTerra/UI/Apply Settings Volume Fill Glow")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode first.");
            WriteTexture();

            foreach (string path in new[] { PromptB104SettingsMenuBuilder.MainPrefab, PromptB104SettingsMenuBuilder.SurfacePrefab })
            {
                var prefab = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    Component view = prefab.GetComponent<MainMenuView>();
                    if (view == null) view = prefab.GetComponent<SurfaceBaseView>();
                    ApplyTo(view);
                    PrefabUtility.SaveAsPrefabAsset(prefab, path);
                }
                finally { PrefabUtility.UnloadPrefabContents(prefab); }
            }

            var scene = SceneManager.GetSceneByPath(PromptB104SettingsMenuBuilder.ScenePath);
            bool opened = !scene.IsValid() || !scene.isLoaded;
            if (!opened && scene.isDirty) throw new InvalidOperationException("Save existing Integration scene edits before running the builder.");
            if (opened) scene = EditorSceneManager.OpenScene(PromptB104SettingsMenuBuilder.ScenePath, OpenSceneMode.Additive);
            try
            {
                var view = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<SurfaceBaseView>(true))
                    .Single(v => v.name == "UndergroundSettings");
                ApplyTo(view);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Integration save failed.");
            }
            finally
            {
                if (opened) EditorSceneManager.CloseScene(scene, true);
            }
            Debug.Log("[SettingsVolumeFillGlow] Fill glow updated in two prefabs and UndergroundSettings only.");
        }

        public static void ApplyTo(Component view)
        {
            var slider = (Slider)new SerializedObject(view).FindProperty("masterVolumeSlider").objectReferenceValue;
            if (slider == null || slider.fillRect == null) throw new InvalidOperationException("masterVolumeSlider fill missing: " + view.name);
            ApplyTo(slider);
        }

        public static Image ApplyTo(Slider slider)
        {
            var fillGlow = slider.fillRect.Find("FillGlow") as RectTransform;
            if (fillGlow == null)
                fillGlow = new GameObject("FillGlow", typeof(RectTransform)).GetComponent<RectTransform>();
            fillGlow.SetParent(slider.fillRect, false);
            fillGlow.anchorMin = new Vector2(0f, 0.5f);
            fillGlow.anchorMax = new Vector2(1f, 0.5f);
            fillGlow.pivot = new Vector2(0.5f, 0.5f);
            fillGlow.anchoredPosition = Vector2.zero;
            fillGlow.sizeDelta = GlowSizeDelta;
            fillGlow.localScale = Vector3.one;
            fillGlow.localRotation = Quaternion.identity;
            fillGlow.SetAsLastSibling();

            var image = fillGlow.GetComponent<Image>();
            if (image == null) image = fillGlow.gameObject.AddComponent<Image>();
            image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(TexturePath);
            // 슬라이스: 가운데는 필 길이만큼 늘리고 양 끝은 둥근 캡 모양을 유지
            image.type = Image.Type.Sliced;
            image.fillCenter = true;
            image.preserveAspect = false;
            image.pixelsPerUnitMultiplier = CapRadius / (GlowSizeDelta.y * 0.5f);
            image.color = Color.white;
            image.raycastTarget = false;
            foreach (var oldFx in fillGlow.GetComponents<Shadow>())
                UnityEngine.Object.DestroyImmediate(oldFx);
            EditorUtility.SetDirty(image);
            return image;
        }

        /// <summary>캡슐형 글로우: 중심선에서 멀어질수록 가우시안으로 투명해져 가장자리에서 0이 된다.</summary>
        public static void WriteTexture()
        {
            var texture = new Texture2D(TextureWidth, TextureHeight, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            var pixels = new Color32[TextureWidth * TextureHeight];
            float floor = Mathf.Exp(-4f);
            for (int y = 0; y < TextureHeight; y++)
            {
                float dy = y + 0.5f - TextureHeight * 0.5f;
                for (int x = 0; x < TextureWidth; x++)
                {
                    float px = x + 0.5f;
                    float dx = px - Mathf.Clamp(px, CapRadius, TextureWidth - CapRadius);
                    float t = Mathf.Sqrt(dx * dx + dy * dy) / CapRadius;
                    float falloff = t >= 1f ? 0f : (Mathf.Exp(-4f * t * t) - floor) / (1f - floor);
                    var color = GlowCyan;
                    color.a = (byte)Mathf.RoundToInt(Mathf.Clamp01(falloff * PeakAlpha) * 255f);
                    pixels[y * TextureWidth + x] = color;
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            File.WriteAllBytes(TexturePath, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(TexturePath);

            var importer = (TextureImporter)AssetImporter.GetAtPath(TexturePath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spriteBorder = new Vector4(CapRadius, 0f, CapRadius, 0f);
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }
    }
}

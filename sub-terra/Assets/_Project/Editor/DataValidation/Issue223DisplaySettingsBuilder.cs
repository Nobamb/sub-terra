using SubTerra.App.Progression;
using UnityEditor;
using UnityEngine;

namespace SubTerra.App.Editor.DataValidation
{
    public static class Issue223DisplaySettingsBuilder
    {
        public const string AssetPath = "Assets/_Project/Resources/DeepZoneDisplaySettings.asset";
        public const string ArtFolder = "Assets/_Project/Art/UI/DeepZone";

        [MenuItem("SubTerra/UI/Create Deep Zone Display Settings")]
        public static void EnsureAsset()
        {
            var existing = AssetDatabase.LoadAssetAtPath<DeepZoneDisplaySettings>(AssetPath);
            if (existing != null)
            {
                AssignMissingArt(existing);
                return;
            }

            var settings = ScriptableObject.CreateInstance<DeepZoneDisplaySettings>();
            settings.EditorSet(new[]
            {
                new DeepZoneDisplayLevel(36, 40, true, string.Empty),
                new DeepZoneDisplayLevel(41, 75, false, "Lv.1 해금 · Demo 목표 13개 완료"),
                new DeepZoneDisplayLevel(76, 100, false, "Lv.2 해금 · 골드/희귀 자원 조건 준비 중")
            });
            AssetDatabase.CreateAsset(settings, AssetPath);
            AssignMissingArt(settings);
            AssetDatabase.SaveAssetIfDirty(settings);
        }

        public static string ThumbnailPath(int levelIndex) => ArtFolder + "/deep-zone-lv" + (levelIndex + 1) + ".png";
        public static string MineralMaskPath(int levelIndex) => ArtFolder + "/deep-zone-lv" + (levelIndex + 1) + "-minerals.png";

        /// <summary>
        /// B-155: 비어 있는 썸네일·광물 마스크 칸만 정해진 경로의 PNG로 채운다.
        /// 깊이·문구와 이미 지정된 sprite는 건드리지 않는다.
        /// </summary>
        public static bool AssignMissingArt(DeepZoneDisplaySettings settings)
        {
            if (settings == null)
            {
                return false;
            }

            var changed = false;
            for (var i = 0; i < settings.Levels.Count; i++)
            {
                var level = settings.Levels[i];
                if (level == null)
                {
                    continue;
                }

                var thumbnail = level.Thumbnail != null ? level.Thumbnail : LoadSprite(ThumbnailPath(i));
                var mask = level.MineralMask != null ? level.MineralMask : LoadSprite(MineralMaskPath(i));
                if (thumbnail == level.Thumbnail && mask == level.MineralMask)
                {
                    continue;
                }

                level.EditorSetArt(thumbnail, mask);
                changed = true;
            }

            if (changed)
            {
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssetIfDirty(settings);
            }

            return changed;
        }

        private static Sprite LoadSprite(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                return null;
            }

            EnsureImportSettings(importer);
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        // 권장 Import: Sprite(Single), Point, 압축 없음, Full Rect. 같은 이름으로 덮어써도 .meta가 남아 그대로 유지된다.
        private static void EnsureImportSettings(TextureImporter importer)
        {
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            if (importer.textureType == TextureImporterType.Sprite
                && importer.spriteImportMode == SpriteImportMode.Single
                && importer.filterMode == FilterMode.Point
                && importer.textureCompression == TextureImporterCompression.Uncompressed
                && importer.mipmapEnabled == false
                && settings.spriteMeshType == SpriteMeshType.FullRect)
            {
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }

        public static void BuildAndValidate()
        {
            EnsureAsset();
            TestValidationRunner.Batch();
        }
    }
}

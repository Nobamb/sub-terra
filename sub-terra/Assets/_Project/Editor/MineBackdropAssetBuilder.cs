using SubTerra.App.UI;
using UnityEditor;
using UnityEngine;

namespace SubTerra.Editor
{
    public static class MineBackdropAssetBuilder
    {
        private const string ImagePath = "Assets/_Project/Art/Backgrounds/mine-cavern-copper.png";
        private const string IronPath = "Assets/_Project/Art/Backgrounds/mine-cavern-iron.png";
        private const string LithiumPath = "Assets/_Project/Art/Backgrounds/mine-cavern-lithium.png";
        private const string SettingsPath = "Assets/_Project/Resources/MineBackdropSettings.asset";

        public static void BuildAndValidate()
        {
            BuildAssets();
            SubTerra.App.Editor.DataValidation.TestValidationRunner.Batch();
        }

        [MenuItem("SubTerra/Visuals/Build Mine Backdrop Settings")]
        public static void Build()
        {
            BuildAssets();
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        public static void BuildAssets()
        {
            foreach (var path in new[] { ImagePath, IronPath, LithiumPath }) ImportImage(path);
            var config = AssetDatabase.LoadAssetAtPath<MineBackdropSettings>(SettingsPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<MineBackdropSettings>();
                AssetDatabase.CreateAsset(config, SettingsPath);
            }
            config.image = AssetDatabase.LoadAssetAtPath<Texture2D>(ImagePath);
            config.ironImage = AssetDatabase.LoadAssetAtPath<Texture2D>(IronPath);
            config.lithiumImage = AssetDatabase.LoadAssetAtPath<Texture2D>(LithiumPath);
            // Read the authoritative mine distribution instead of maintaining separate theme depths.
            var distribution = AssetDatabase.LoadAssetAtPath<ScriptableObject>("Assets/_Project/Data/World/MineLayerDistribution.asset");
            if (distribution == null) throw new System.InvalidOperationException("Mine distribution missing.");
            var data = new SerializedObject(distribution);
            config.mineralSurfaceY = data.FindProperty("topY").intValue + 1f;
            var bands = data.FindProperty("bands");
            for (int i = 0; i < bands.arraySize; i++)
            {
                var band = bands.GetArrayElementAtIndex(i);
                var contents = band.FindPropertyRelative("contents");
                for (int j = 0; j < contents.arraySize; j++)
                {
                    int kind = contents.GetArrayElementAtIndex(j).FindPropertyRelative("kind").intValue;
                    if (kind == 3) config.ironDepth = band.FindPropertyRelative("minDepth").intValue;
                    if (kind == 4) config.lithiumDepth = band.FindPropertyRelative("minDepth").intValue;
                }
            }
            MigrateB187Defaults(config);
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssetIfDirty(config);
            Debug.Log("Mine backdrop depth settings built.");
        }

        // 세로형 구 그림 기준 값일 때만 가로형(B-187) 기본값으로 올린다. PM이 직접 고친 값은 덮지 않는다.
        private static void MigrateB187Defaults(MineBackdropSettings config)
        {
            var oldTint = new Color(.65f, .7f, .75f, 1f);
            if (Mathf.Approximately(config.tint.r, oldTint.r) && Mathf.Approximately(config.tint.g, oldTint.g)
                && Mathf.Approximately(config.tint.b, oldTint.b)) config.tint = MineBackdropSettings.DefaultTint;
            if (Mathf.Approximately(config.visibleImageHeight, .22f) || Mathf.Approximately(config.visibleImageHeight, .5f)) config.visibleImageHeight = .65f;
            if (Mathf.Approximately(config.depthTravelWorld, 160f) || Mathf.Approximately(config.depthTravelWorld, 80f)) config.depthTravelWorld = 60f;
            if (Mathf.Approximately(config.horizontalTravelWorld, 160f)) config.horizontalTravelWorld = 80f;
            // 첫 반짝임 기본값(0.08/0.30/0.25)일 때만 더 또렷한 값으로 올린다.
            if (Mathf.Approximately(config.lithiumPulseBoost, .08f)) config.lithiumPulseBoost = .2f;
            if (Mathf.Approximately(config.copperPulseStrength, .3f)) config.copperPulseStrength = .45f;
            if (Mathf.Approximately(config.ironPulseStrength, .25f)) config.ironPulseStrength = .4f;
        }

        private static void ImportImage(string path)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Default;
            importer.mipmapEnabled = false;
            importer.isReadable = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.SaveAndReimport();
        }
    }
}

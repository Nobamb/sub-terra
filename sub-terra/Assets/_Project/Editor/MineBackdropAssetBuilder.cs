using SubTerra.App.UI;
using UnityEditor;
using UnityEngine;

namespace SubTerra.Editor
{
    public static class MineBackdropAssetBuilder
    {
        private const string ImagePath = "Assets/_Project/Art/Backgrounds/mine-cavern-backdrop.png";
        private const string SettingsPath = "Assets/_Project/Resources/MineBackdropSettings.asset";

        [MenuItem("SubTerra/Visuals/Build Mine Backdrop Settings")]
        public static void Build()
        {
            AssetDatabase.ImportAsset(ImagePath, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(ImagePath);
            importer.textureType = TextureImporterType.Default;
            importer.mipmapEnabled = false;
            importer.isReadable = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.SaveAndReimport();
            var config = AssetDatabase.LoadAssetAtPath<MineBackdropSettings>(SettingsPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<MineBackdropSettings>();
                AssetDatabase.CreateAsset(config, SettingsPath);
            }
            config.image = AssetDatabase.LoadAssetAtPath<Texture2D>(ImagePath);
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssetIfDirty(config);
            Debug.Log("Mine backdrop settings built.");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }
    }
}

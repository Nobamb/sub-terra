using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SubTerra.App.Editor.DataValidation
{
    /// <summary>지상 퀘스트 아이콘만 교체한다. 버튼 배치나 다른 UI는 재생성하지 않는다.</summary>
    public static class PromptB195SurfaceQuestIconBuilder
    {
        public const string IconPath = "Assets/_Project/Art/UI/SurfaceBase/Icons/icon-quest.png";

        [MenuItem("SubTerra/UI/Apply Issue 195 Surface Quest Icon")]
        public static void Build()
        {
            AssetDatabase.ImportAsset(IconPath, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(IconPath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.maxTextureSize = 512;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
            var root = PrefabUtility.LoadPrefabContents(PromptB117SurfaceBaseBuilder.PrefabPath);
            try
            {
                var icon = root.transform.Find("SurfaceBaseContent/QuestButton/Icon")
                    .GetComponent<UnityEngine.UI.Image>();
                icon.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(IconPath);
                if (icon.sprite == null) throw new InvalidOperationException("퀘스트 아이콘을 읽지 못했습니다.");
                PrefabUtility.SaveAsPrefabAsset(root, PromptB117SurfaceBaseBuilder.PrefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            AssetDatabase.SaveAssets();
        }

        public static void BuildAndVerify()
        {
            Build();
            var type = AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType("SubTerra.App.Tests.UI.PromptB117SurfaceBaseTests"))
                .First(candidate => candidate != null);
            foreach (var name in new[] { "QuestActions_UseEqualSizeAndSpacing", "QuestIcon_UsesSurfaceSetAssetWithoutChangingLayout" })
            {
                type.GetMethod(name).Invoke(Activator.CreateInstance(type), null);
                Debug.Log("[SubTerra] PASS: " + name);
            }
        }
    }
}

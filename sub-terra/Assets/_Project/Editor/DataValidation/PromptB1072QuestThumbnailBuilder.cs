using System;
using SubTerra.App.Tutorial;
using SubTerra.App.UI.Tutorial;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SubTerra.App.Editor.DataValidation
{
    /// <summary>상세 퀘스트의 썸네일 참조만 갱신한다. 다른 UI 레이아웃은 저장하지 않는다.</summary>
    public static class PromptB1072QuestThumbnailBuilder
    {
        public const string Art = PromptB107QuestUiBuilder.Art + "Thumbnails/";

        public static string PathFor(string objectiveId) =>
            Art + objectiveId.Substring("demo.quest.".Length) + ".png";

        [MenuItem("SubTerra/UI/Apply Prompt-B 107-2 Quest Thumbnails")]
        public static void Apply()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode first.");
            var scene = SceneManager.GetSceneByPath(PromptB107QuestUiBuilder.IntegrationScenePath);
            if (scene.IsValid() && scene.isLoaded && scene.isDirty)
                throw new InvalidOperationException("Integration 씬에 저장되지 않은 변경이 있습니다.");
            Import();
            bool closeAfter = !scene.IsValid() || !scene.isLoaded;
            if (closeAfter)
                scene = EditorSceneManager.OpenScene(PromptB107QuestUiBuilder.IntegrationScenePath, OpenSceneMode.Additive);
            try
            {
                QuestThumbnailView target = null;
                foreach (var root in scene.GetRootGameObjects())
                {
                    var candidate = root.GetComponentInChildren<QuestThumbnailView>(true);
                    if (candidate != null) target = candidate;
                }
                if (target == null) throw new InvalidOperationException("QuestThumbnailView missing.");
                Configure(target);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Integration save failed.");
            }
            finally
            {
                if (closeAfter) EditorSceneManager.CloseScene(scene, true);
            }
        }

        public static void Configure(QuestThumbnailView view)
        {
            var serialized = new SerializedObject(view);
            var entries = serialized.FindProperty("entries");
            entries.arraySize = DemoObjectiveIds.Ordered.Length;
            for (int i = 0; i < DemoObjectiveIds.Ordered.Length; i++)
            {
                var id = DemoObjectiveIds.Ordered[i];
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(PathFor(id));
                if (sprite == null) throw new InvalidOperationException("Thumbnail missing: " + id);
                var entry = entries.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("objectiveId").stringValue = id;
                entry.FindPropertyRelative("primary").objectReferenceValue = sprite;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(view);
        }

        private static void Import()
        {
            foreach (var id in DemoObjectiveIds.Ordered)
            {
                var path = PathFor(id);
                AssetDatabase.ImportAsset(path);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.textureCompression = TextureImporterCompression.CompressedHQ;
                importer.maxTextureSize = 2048;
                importer.filterMode = FilterMode.Bilinear;
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                importer.SetTextureSettings(settings);
                importer.SaveAndReimport();
            }
        }
    }
}

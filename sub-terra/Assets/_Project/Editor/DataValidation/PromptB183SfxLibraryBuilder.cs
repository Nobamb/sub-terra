using System;
using System.IO;
using SubTerra.Shared.Audio;
using UnityEditor;

namespace SubTerra.App.Editor.DataValidation
{
    /// <summary>B-183 라이브러리 갱신. 기존 칸 중 16번 음량만 바꾼다.</summary>
    public static class PromptB183SfxLibraryBuilder
    {
        private const string Flag = "Temp/subterra-build-b183-audio.flag";

        [InitializeOnLoadMethod]
        private static void WatchFlag()
        {
            EditorApplication.update -= Poll;
            EditorApplication.update += Poll;
        }

        private static void Poll()
        {
            if (!File.Exists(Flag) || EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode) return;
            File.Delete(Flag);
            try
            {
                Build();
                File.WriteAllText("Temp/subterra-build-b183-audio.done", "PASS");
            }
            catch (Exception exception)
            {
                File.WriteAllText("Temp/subterra-build-b183-audio.done", exception.ToString());
            }
        }

        [MenuItem("SubTerra/Audio/Apply Prompt-B 183 Menu Drone Sfx")]
        public static void Build()
        {
            PromptB156SfxLibraryBuilder.EnsureAsset();
            var library = AssetDatabase.LoadAssetAtPath<SubTerra.App.Audio.SfxLibrary>(PromptB156SfxLibraryBuilder.AssetPath);
            var serialized = new SerializedObject(library);
            var entries = serialized.FindProperty("entries");
            for (var i = 0; i < entries.arraySize; i++)
            {
                var entry = entries.GetArrayElementAtIndex(i);
                if (entry.FindPropertyRelative("id").intValue == (int)SfxId.UiSlotSelect)
                    entry.FindPropertyRelative("volume").floatValue = 0.65f;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(library);
            AssetDatabase.SaveAssetIfDirty(library);
            AssetDatabase.ImportAsset("Assets/_Project/Audio/SFX/UI/empty_slot_noise_loop.wav", ImportAssetOptions.ForceUpdate);
        }
    }
}

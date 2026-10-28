using System.IO;
using SubTerra.App.Audio;
using UnityEditor;
using UnityEngine;

namespace SubTerra.App.Editor.DataValidation
{
    /// <summary>
    /// B-156: Resources/SfxLibrary.asset을 만들고, 비어 있는 클립 칸만 Audio/SFX 아래 같은 이름의 파일로 채운다.
    /// 이미 있는 칸의 음량·피치·재생 구간은 덮어쓰지 않는다. 음원 파일은 읽기만 한다.
    /// </summary>
    public static class PromptB156SfxLibraryBuilder
    {
        public const string AssetPath = "Assets/_Project/Resources/SfxLibrary.asset";
        private static readonly string[] Extensions = { ".wav", ".ogg", ".mp3" };

        [MenuItem("SubTerra/Audio/Create Or Refresh Sfx Library")]
        public static void EnsureAsset()
        {
            var library = AssetDatabase.LoadAssetAtPath<SfxLibrary>(AssetPath);
            if (library == null)
            {
                library = CreateNew();
            }
            else
            {
                AddMissingEntries(library);
            }

            if (AssignMissingClips(library))
            {
                EditorUtility.SetDirty(library);
                AssetDatabase.SaveAssetIfDirty(library);
            }
        }

        private static SfxLibrary CreateNew()
        {
            var library = ScriptableObject.CreateInstance<SfxLibrary>();
            foreach (var pair in SfxLibraryDefaults.Create())
            {
                library.EditorAddEntry(pair.Entry);
            }

            var folder = Path.GetDirectoryName(AssetPath).Replace('\\', '/');
            if (!AssetDatabase.IsValidFolder(folder))
            {
                AssetDatabase.CreateFolder("Assets/_Project", "Resources");
            }

            AssetDatabase.CreateAsset(library, AssetPath);
            return library;
        }

        // 뒤 단계에서 SfxId가 늘면 기존 에셋에 빈 칸만 덧붙인다.
        private static void AddMissingEntries(SfxLibrary library)
        {
            var added = false;
            foreach (var pair in SfxLibraryDefaults.Create())
            {
                if (!library.TryGetEntry(pair.Entry.Id, out _))
                {
                    library.EditorAddEntry(pair.Entry);
                    added = true;
                }
            }

            if (added)
            {
                EditorUtility.SetDirty(library);
            }
        }

        public static bool AssignMissingClips(SfxLibrary library)
        {
            if (library == null)
            {
                return false;
            }

            var changed = false;
            foreach (var pair in SfxLibraryDefaults.Create())
            {
                if (!library.TryGetEntry(pair.Entry.Id, out var entry) || entry.Clip != null)
                {
                    continue;
                }

                var clip = LoadClip(pair.Entry.Bus, pair.FileName);
                if (clip != null)
                {
                    entry.EditorSetClip(clip);
                    changed = true;
                }
            }

            return changed;
        }

        private static AudioClip LoadClip(SfxBus bus, string fileName)
        {
            for (var i = 0; i < Extensions.Length; i++)
            {
                var path = SfxLibraryDefaults.ClipRoot + "/" + SfxLibraryDefaults.FolderOf(bus) + "/" + fileName + Extensions[i];
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (clip != null)
                {
                    return clip;
                }
            }

            return null;
        }
    }

    /// <summary>Audio/SFX 아래 파일의 import 설정(모노, 로드 시 압축 해제(반복음은 압축 유지), Vorbis 70%)을 맞추고, 새 파일이 들어오면 라이브러리를 다시 채운다.</summary>
    public sealed class PromptB156SfxPostprocessor : AssetPostprocessor
    {
        private static bool refreshQueued;

        private void OnPreprocessAudio()
        {
            if (!assetPath.StartsWith(SfxLibraryDefaults.ClipRoot + "/"))
            {
                return;
            }

            var importer = (AudioImporter)assetImporter;
            importer.forceToMono = true;
            var settings = importer.defaultSampleSettings;
            // B-162: 20초 반복음은 풀어 두면 메모리를 크게 차지하므로 압축된 채 둔다.
            var isLoop = SfxLibraryDefaults.IsLoopFile(Path.GetFileNameWithoutExtension(assetPath));
            settings.loadType = isLoop ? AudioClipLoadType.CompressedInMemory : AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = 0.7f;
            settings.preloadAudioData = true;
            importer.defaultSampleSettings = settings;
        }

        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            if (refreshQueued || !HasSfxAsset(importedAssets))
            {
                return;
            }

            refreshQueued = true;
            EditorApplication.delayCall += () =>
            {
                refreshQueued = false;
                PromptB156SfxLibraryBuilder.EnsureAsset();
            };
        }

        private static bool HasSfxAsset(string[] paths)
        {
            for (var i = 0; i < paths.Length; i++)
            {
                if (paths[i].StartsWith(SfxLibraryDefaults.ClipRoot + "/"))
                {
                    return true;
                }
            }

            return false;
        }
    }
}

using System;
using System.IO;
using System.Linq;
using SubTerra.App.Editor.DataValidation;
using SubTerra.Gameplay.Player;
using UnityEditor;
using UnityEngine;

namespace SubTerra.Editor
{
    // B-175: 점프 새 그림 3묶음을 가져오고 Player 프리팹 배열·설정만 바꾼다. 기존 Frames/Jump 10장은 지우지 않는다.
    public static class JumpAnimationBuilder
    {
        private const string Frames = "Assets/_Project/Art/Characters/Player/Frames";
        private const string Source = "../work_process/images/player-jump-20261029/";
        private const string PlayerPrefab = "Assets/_Project/Prefabs/Gameplay/Player/Player.prefab";

        private static readonly (string Folder, string Prefix, string Runtime, string Property, int Count)[] Groups =
        {
            ("JumpReady", "jump_ready", "jump-ready", "jumpReadyFrames", 8),
            ("JumpRise", "jump_rise", "jump-rise", "jumpRiseFrames", 24),
            ("JumpFall", "jump_fall", "jump-fall", "jumpFallFrames", 24)
        };

        public static string FramePath(string folder, string prefix, int number) =>
            Frames + "/" + folder + "/" + prefix + "_" + number.ToString("D2") + ".png";

        [MenuItem("SubTerra/Player/Build B-175 Jump")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play Mode before replacing player artwork.");
            // 입력 전체를 먼저 확인한 뒤 쓴다.
            var drawings = Groups.Select(group => Enumerable.Range(1, group.Count).Select(i =>
                ReadDrawing(Source + "runtime-frames/" + group.Runtime + "-" + i.ToString("D2") + ".png"))
                .ToArray()).ToArray();
            var bobOffsets = ReadBobOffsets();

            var sprites = new Sprite[Groups.Length][];
            for (int g = 0; g < Groups.Length; g++)
            {
                var group = Groups[g];
                if (!AssetDatabase.IsValidFolder(Frames + "/" + group.Folder))
                    AssetDatabase.CreateFolder(Frames, group.Folder);
                sprites[g] = new Sprite[group.Count];
                for (int i = 0; i < group.Count; i++)
                {
                    string path = FramePath(group.Folder, group.Prefix, i + 1);
                    WriteAtomic(path, drawings[g][i]);
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                    ConfigureImporter(path);
                    sprites[g][i] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                    if (sprites[g][i] == null)
                        throw new FileNotFoundException("Jump sprite import failed.", path);
                }
            }

            var root = PrefabUtility.LoadPrefabContents(PlayerPrefab);
            try
            {
                var animation = new SerializedObject(root.GetComponentInChildren<PlayerAnimationController>(true));
                for (int g = 0; g < Groups.Length; g++)
                {
                    var frames = animation.FindProperty(Groups[g].Property);
                    frames.arraySize = sprites[g].Length;
                    for (int i = 0; i < sprites[g].Length; i++)
                        frames.GetArrayElementAtIndex(i).objectReferenceValue = sprites[g][i];
                }
                // 이전 10장은 파일만 남기고 참조를 끊는다.
                animation.FindProperty("jumpFrames").arraySize = 0;

                var settings = new PlayerJumpAnimationSettings { fallBobOffsets = bobOffsets };
                var property = animation.FindProperty("jumpAnimation");
                foreach (var field in typeof(PlayerJumpAnimationSettings).GetFields())
                {
                    var value = property.FindPropertyRelative(field.Name);
                    if (field.FieldType == typeof(int)) value.intValue = (int)field.GetValue(settings);
                    else if (field.FieldType == typeof(float)) value.floatValue = (float)field.GetValue(settings);
                    else
                    {
                        value.arraySize = bobOffsets.Length;
                        for (int i = 0; i < bobOffsets.Length; i++)
                            value.GetArrayElementAtIndex(i).floatValue = bobOffsets[i];
                    }
                }
                animation.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefab);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            PromptB140GameGuideSkinBuilder.Build();
            Debug.Log("[B-175] Jump ready 8, rise 24, fall 24 drawings applied.");
        }

        private static byte[] ReadDrawing(string path)
        {
            var bytes = File.ReadAllBytes(path);
            var texture = new Texture2D(2, 2);
            try
            {
                if (!texture.LoadImage(bytes) || texture.width != 256 || texture.height != 256)
                    throw new InvalidDataException("B-175 requires approved 256x256 drawings: " + path);
            }
            finally { UnityEngine.Object.DestroyImmediate(texture); }
            return bytes;
        }

        private static float[] ReadBobOffsets()
        {
            // measurements.json의 "fall_bob_offsets_px": [ ... ] 한 줄 배열만 읽는다.
            string json = File.ReadAllText(Source + "measurements.json");
            const string Key = "\"fall_bob_offsets_px\": [";
            int start = json.IndexOf(Key, StringComparison.Ordinal);
            if (start < 0) throw new InvalidDataException("measurements.json has no fall_bob_offsets_px.");
            start += Key.Length;
            int end = json.IndexOf(']', start);
            return json.Substring(start, end - start)
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(value => float.Parse(value.Trim(), System.Globalization.CultureInfo.InvariantCulture))
                .ToArray();
        }

        private static void WriteAtomic(string path, byte[] bytes)
        {
            if (File.Exists(path) && File.ReadAllBytes(path).SequenceEqual(bytes)) return;
            // 에디터가 PNG를 메모리 매핑 중이어도 기존 .meta를 보존하며 교체한다.
            string temporary = path + ".b175.tmp";
            File.WriteAllBytes(temporary, bytes);
            if (File.Exists(path)) File.Replace(temporary, path, null);
            else File.Move(temporary, path);
        }

        private static void ConfigureImporter(string path)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 256f;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteAlignment = (int)SpriteAlignment.Center;
            settings.spritePivot = new Vector2(.5f, .5f);
            importer.SetTextureSettings(settings);
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }
    }
}

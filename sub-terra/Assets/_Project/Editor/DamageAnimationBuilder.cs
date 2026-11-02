using System;
using System.IO;
using System.Linq;
using SubTerra.Gameplay.Player;
using UnityEditor;
using UnityEngine;

namespace SubTerra.Editor
{
    // B-176: 다침 사이 동작 4장을 가져오고 Player 프리팹 damageFrames 8장·fps만 바꾼다. 기존 damage_01~04는 건드리지 않는다.
    public static class DamageAnimationBuilder
    {
        private const string Folder = "Assets/_Project/Art/Characters/Player/Frames/Damage/";
        private const string Source = "../work_process/images/player-damage-8f-20261029/runtime-frames/";
        private const string PlayerPrefab = "Assets/_Project/Prefabs/Gameplay/Player/Player.prefab";
        private const float DamageFps = 20f;

        private static readonly (string Runtime, string Asset)[] NewFrames =
        {
            ("damage-1-2", "damage_1_2"),
            ("damage-2-3", "damage_2_3"),
            ("damage-3-4", "damage_3_4"),
            ("damage-4-idle", "damage_4_idle")
        };

        [MenuItem("SubTerra/Player/Build B-176 Damage")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play Mode before replacing player artwork.");
            // 입력 전체를 먼저 확인한 뒤 쓴다.
            var drawings = NewFrames.Select(frame => ReadDrawing(Source + frame.Runtime + ".png")).ToArray();
            for (int i = 0; i < NewFrames.Length; i++)
            {
                string path = Folder + NewFrames[i].Asset + ".png";
                WriteAtomic(path, drawings[i]);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                ConfigureImporter(path);
            }

            var sprites = PlayerAnimationAssetBuilder.DamageFrameNames.Select(name =>
            {
                string path = Folder + name + ".png";
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite == null) throw new FileNotFoundException("Damage sprite import failed.", path);
                return sprite;
            }).ToArray();

            var root = PrefabUtility.LoadPrefabContents(PlayerPrefab);
            try
            {
                var animation = new SerializedObject(root.GetComponentInChildren<PlayerAnimationController>(true));
                var frames = animation.FindProperty("damageFrames");
                frames.arraySize = sprites.Length;
                for (int i = 0; i < sprites.Length; i++)
                    frames.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
                animation.FindProperty("damageFps").floatValue = DamageFps;
                animation.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefab);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            PlayerAnimationAssetBuilder.ApplyDamagePreviewClip();
            AssetDatabase.SaveAssets();
            Debug.Log("[B-176] Damage 8 frames at 20fps applied.");
        }

        private static byte[] ReadDrawing(string path)
        {
            var bytes = File.ReadAllBytes(path);
            var texture = new Texture2D(2, 2);
            try
            {
                if (!texture.LoadImage(bytes) || texture.width != 256 || texture.height != 256)
                    throw new InvalidDataException("B-176 requires approved 256x256 drawings: " + path);
            }
            finally { UnityEngine.Object.DestroyImmediate(texture); }
            return bytes;
        }

        private static void WriteAtomic(string path, byte[] bytes)
        {
            if (File.Exists(path) && File.ReadAllBytes(path).SequenceEqual(bytes)) return;
            // 에디터가 PNG를 메모리 매핑 중이어도 기존 .meta를 보존하며 교체한다.
            string temporary = path + ".b176.tmp";
            File.WriteAllBytes(temporary, bytes);
            if (File.Exists(path)) File.Replace(temporary, path, null);
            else File.Move(temporary, path);
        }

        // 기존 damage_01~04 메타와 같은 설정.
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

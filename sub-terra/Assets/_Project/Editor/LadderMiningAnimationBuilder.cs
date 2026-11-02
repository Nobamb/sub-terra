using System;
using System.IO;
using System.Linq;
using SubTerra.Gameplay.Player;
using UnityEditor;
using UnityEngine;

namespace SubTerra.Editor
{
    public static class LadderMiningAnimationBuilder
    {
        private const string Folder = "Assets/_Project/Art/Characters/Player/Frames/LadderMining";
        private const string PlayerPrefab = "Assets/_Project/Prefabs/Gameplay/Player/Player.prefab";

        [MenuItem("SubTerra/Player/Build B-174 Ladder Mining")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play Mode before replacing player artwork.");
            var drawings = Enumerable.Range(1, 24).Select(i => File.ReadAllBytes(
                "../work_process/images/player-ladder-mining-24f-20261029/runtime-frames/ladder-mining-"
                    + i.ToString("D2") + ".png")).ToArray();
            foreach (var bytes in drawings)
            {
                var texture = new Texture2D(2, 2);
                try
                {
                    if (!texture.LoadImage(bytes) || texture.width != 256 || texture.height != 256)
                        throw new InvalidDataException("B-174 requires 24 approved 256x256 drawings.");
                }
                finally { UnityEngine.Object.DestroyImmediate(texture); }
            }
            if (!AssetDatabase.IsValidFolder(Folder))
                AssetDatabase.CreateFolder("Assets/_Project/Art/Characters/Player/Frames", "LadderMining");
            var sprites = new Sprite[24];
            for (int i = 0; i < drawings.Length; i++)
            {
                string path = Folder + "/ladder_mining_" + (i + 1).ToString("D2") + ".png";
                if (!File.Exists(path) || !File.ReadAllBytes(path).SequenceEqual(drawings[i]))
                {
                    string temporary = path + ".b174.tmp";
                    File.WriteAllBytes(temporary, drawings[i]);
                    if (File.Exists(path)) File.Replace(temporary, path, null);
                    else File.Move(temporary, path);
                }
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
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
                sprites[i] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }
            var root = PrefabUtility.LoadPrefabContents(PlayerPrefab);
            try
            {
                var animation = new SerializedObject(root.GetComponentInChildren<PlayerAnimationController>(true));
                var frames = animation.FindProperty("ladderMiningFrames");
                frames.arraySize = sprites.Length;
                for (int i = 0; i < sprites.Length; i++)
                    frames.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
                var settings = new PlayerLadderMiningAnimationSettings();
                var property = animation.FindProperty("ladderMiningAnimation");
                foreach (var field in typeof(PlayerLadderMiningAnimationSettings).GetFields())
                {
                    var value = property.FindPropertyRelative(field.Name);
                    if (field.FieldType == typeof(int)) value.intValue = (int)field.GetValue(settings);
                    else value.floatValue = (float)field.GetValue(settings);
                }
                animation.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefab);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
    }
}

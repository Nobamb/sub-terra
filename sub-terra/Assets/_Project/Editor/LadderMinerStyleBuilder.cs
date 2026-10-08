using System;
using SubTerra.Gameplay.Player;
using UnityEditor;
using UnityEngine;

namespace SubTerra.Editor
{
    /// <summary>Swaps only the five ladder sprite references; movement and other animation states are retained.</summary>
    public static class LadderMinerStyleBuilder
    {
        public const string NeutralSpritePath =
            "Assets/_Project/Art/Characters/Player/Frames/LadderBack/ladder_back_neutral_v2.png";
        public const string OriginalSpritePath =
            "Assets/_Project/Art/Characters/Player/Frames/LadderBack/ladder_back_01.png";
        private const string PlayerPrefabPath = "Assets/_Project/Prefabs/Gameplay/Player/Player.prefab";
        // Opaque neutral height is 957px versus the standing miner's 226px at 256 PPU.
        public const float StylePixelsPerUnit = 1084f;

        public static string StyleFramePath(int index) => index == 0 ? NeutralSpritePath
            : "Assets/_Project/Art/Characters/Player/Frames/LadderBack/ladder_back_style_" + (index + 1).ToString("D2") + ".png";

        [MenuItem("SubTerra/Player/Apply Miner Style Ladder Five Frames")]
        public static void ApplyFiveFrames() => Apply(styled: true);

        [MenuItem("SubTerra/Player/Restore Original Ladder Five Frames")]
        public static void RestoreOriginalFrames() => Apply(styled: false);

        private static void Apply(bool styled)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play Mode before applying ladder artwork.");

            var sprites = new Sprite[5];
            for (int index = 0; index < sprites.Length; index++)
            {
                string spritePath = styled ? StyleFramePath(index)
                    : "Assets/_Project/Art/Characters/Player/Frames/LadderBack/ladder_back_" + (index + 1).ToString("D2") + ".png";
                AssetDatabase.ImportAsset(spritePath, ImportAssetOptions.ForceSynchronousImport);
                if (styled)
                {
                    var importer = AssetImporter.GetAtPath(spritePath) as TextureImporter;
                    if (importer == null) throw new InvalidOperationException("Missing ladder texture: " + spritePath);
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.spritePixelsPerUnit = StylePixelsPerUnit;
                    var settings = new TextureImporterSettings();
                    importer.ReadTextureSettings(settings);
                    settings.spriteMeshType = SpriteMeshType.FullRect;
                    settings.spriteAlignment = (int)SpriteAlignment.Center;
                    settings.spritePivot = new Vector2(0.5f, 0.5f);
                    importer.SetTextureSettings(settings);
                    importer.alphaIsTransparency = true;
                    importer.mipmapEnabled = false;
                    importer.wrapMode = TextureWrapMode.Clamp;
                    importer.filterMode = FilterMode.Bilinear;
                    importer.npotScale = TextureImporterNPOTScale.None;
                    importer.maxTextureSize = 2048;
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.SaveAndReimport();
                }
                sprites[index] = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
                if (sprites[index] == null) throw new InvalidOperationException("Cannot load ladder sprite: " + spritePath);
            }
            GameObject contents = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            try
            {
                var animation = contents.GetComponentInChildren<PlayerAnimationController>(true);
                if (animation == null) throw new InvalidOperationException("Player animation is missing.");
                var serialized = new SerializedObject(animation);
                var frames = serialized.FindProperty("ladderFrames");
                if (frames == null || frames.arraySize != 5)
                    throw new InvalidOperationException("The approved five-frame ladder set is required.");
                for (int index = 0; index < sprites.Length; index++)
                    frames.GetArrayElementAtIndex(index).objectReferenceValue = sprites[index];
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(contents, PlayerPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
            Debug.Log("[Ladder Miner Style] Five frames applied. Styled=" + styled);
        }
    }
}

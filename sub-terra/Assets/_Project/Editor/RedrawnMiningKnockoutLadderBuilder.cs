using System;
using System.IO;
using System.Linq;
using SubTerra.App.Editor.DataValidation;
using SubTerra.Gameplay.Player;
using UnityEditor;
using UnityEngine;

namespace SubTerra.Editor
{
    public static class RedrawnMiningKnockoutLadderBuilder
    {
        private const string Frames = "Assets/_Project/Art/Characters/Player/Frames/";
        private const string PlayerPrefab = "Assets/_Project/Prefabs/Gameplay/Player/Player.prefab";
        private const string Flag = "Temp/subterra-build-b172.flag";
        private const string Done = "Temp/subterra-build-b172.done";

        [InitializeOnLoadMethod]
        private static void Watch() => EditorApplication.update += Poll;

        private static void Poll()
        {
            if (!File.Exists(Flag) || EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode)
                return;
            File.Delete(Flag);
            try
            {
                Build();
                File.WriteAllText(Done, "PASS " + DateTime.Now.ToString("o"));
            }
            catch (Exception error)
            {
                File.WriteAllText(Done, "FAIL " + error);
                Debug.LogException(error);
            }
        }

        [MenuItem("SubTerra/Player/Build B-172 Mining Knockout Ladder")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play Mode before replacing player artwork.");
            // 입력 전체를 먼저 확인한다. 기존 01~08 및 사다리 .meta는 그대로 둔다.
            var mining = ReadDrawings("player-mining-24f-20261029", "mining", 24);
            var knockout = ReadDrawings("player-knockout-48f-20261029", "knockout", 48);
            var ladder = ReadDrawings("player-ladder-5f-20261029", "ladder", 5);
            AssetDatabase.StartAssetEditing();
            try
            {
                WriteDrawings(mining, i => Frames + "Mining/mining_" + (i + 1).ToString("D2") + ".png");
                WriteDrawings(knockout, i => Frames + "Knockout/knockout_" + (i + 1).ToString("D2") + ".png");
                WriteDrawings(ladder, LadderMinerStyleBuilder.StyleFramePath);
            }
            finally { AssetDatabase.StopAssetEditing(); }
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            ConfigureDrawings("Mining", "mining", 24);
            ConfigureDrawings("Knockout", "knockout", 48);
            LadderMinerStyleBuilder.ApplyFiveFrames();

            var root = PrefabUtility.LoadPrefabContents(PlayerPrefab);
            try
            {
                var animation = root.GetComponentInChildren<PlayerAnimationController>(true);
                var serialized = new SerializedObject(animation);
                SetFrames(serialized, "miningFrames", "Mining", "mining", 24);
                SetFrames(serialized, "knockoutFrames", "Knockout", "knockout", 48);
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefab);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            PlayerAnimationAssetBuilder.ApplyRedrawnActionPreviewClips();
            PromptB140GameGuideSkinBuilder.Build();
            Debug.Log("[B-172] Mining 24, knockout 48, ladder 5 drawings applied.");
        }

        private static byte[][] ReadDrawings(string directory, string prefix, int count)
        {
            var drawings = new byte[count][];
            for (int i = 0; i < count; i++)
            {
                string path = "../work_process/images/" + directory + "/runtime-frames/" + prefix
                    + "-" + (i + 1).ToString("D2") + ".png";
                drawings[i] = File.ReadAllBytes(path);
                var texture = new Texture2D(2, 2);
                try
                {
                    if (!texture.LoadImage(drawings[i]) || texture.width != texture.height
                        || (prefix == "ladder" ? texture.width != 1254 : texture.width < 256))
                        throw new InvalidDataException("Unexpected approved drawing canvas: " + path);
                }
                finally { UnityEngine.Object.DestroyImmediate(texture); }
            }
            return drawings;
        }

        private static void WriteDrawings(byte[][] drawings, Func<int, string> path)
        {
            for (int i = 0; i < drawings.Length; i++)
            {
                string target = path(i);
                // 에디터가 PNG를 메모리 매핑 중이어도 기존 .meta를 보존하며 교체한다.
                string temporary = target + ".b172.tmp";
                if (File.Exists(target) && File.ReadAllBytes(target).SequenceEqual(drawings[i]))
                {
                    if (File.Exists(temporary)) File.Delete(temporary);
                    continue;
                }
                File.WriteAllBytes(temporary, drawings[i]);
                if (File.Exists(target)) File.Replace(temporary, target, null);
                else File.Move(temporary, target);
            }
        }

        private static void ConfigureDrawings(string folder, string prefix, int count)
        {
            for (int i = 1; i <= count; i++)
            {
                string path = Frames + folder + "/" + prefix + "_" + i.ToString("D2") + ".png";
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

        private static void SetFrames(SerializedObject animation, string property, string folder, string prefix, int count)
        {
            var frames = animation.FindProperty(property);
            frames.arraySize = count;
            for (int i = 0; i < count; i++)
                frames.GetArrayElementAtIndex(i).objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(
                    Frames + folder + "/" + prefix + "_" + (i + 1).ToString("D2") + ".png");
        }
    }
}

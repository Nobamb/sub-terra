using System;
using System.IO;
using SubTerra.App.UI.Guide;
using SubTerra.Gameplay.Player;
using UnityEditor;
using UnityEngine;

namespace SubTerra.Editor
{
    // 사용자가 제공한 전신 프레임을 등록한다. 신체 부위 변형은 하지 않는다.
    public static class RedrawnWalkCycleBuilder
    {
        private const string Art = "Assets/_Project/Art/Characters/Player/";
        private const string Approved = "../work_process/images/player-walk-40f-20261028/runtime-frames";

        [MenuItem("SubTerra/Build Redrawn Walk Cycle")]
        public static void Build() => Run(false, false);
        [MenuItem("SubTerra/Preview Redrawn Walk Cycle")]
        public static void BuildPreview() => Run(true, false);
        public static void BuildAndValidate() => Run(false, true);

        private static void Run(bool previewOnly, bool validate)
        {
            try
            {
                var drawings = new byte[PlayerAnimationController.WalkFrameCount][];
                // 모든 입력을 확인한 뒤 출력한다. 불완전한 입력으로 프레임 일부만 바꾸지 않는다.
                for (int i = 0; i < drawings.Length; i++)
                {
                    string source = Path.Combine(Approved, "walk-" + (i + 1).ToString("D2") + ".png");
                    drawings[i] = File.ReadAllBytes(source);
                    var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    try
                    {
                        if (!texture.LoadImage(drawings[i]) || texture.width != 256 || texture.height != 256)
                            throw new InvalidDataException("Approved walk drawing must be 256x256: " + source);
                    }
                    finally { UnityEngine.Object.DestroyImmediate(texture); }
                }
                string output = previewOnly ? Path.GetFullPath(Approved + "/builder-preview") : Art + "Frames/Walk";
                Directory.CreateDirectory(output);
                for (int i = 0; i < drawings.Length; i++)
                    File.WriteAllBytes(Path.Combine(output, "walk_" + (i + 1).ToString("D2") + ".png"), drawings[i]);
                if (!previewOnly)
                {
                    AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                    PlayerAnimationAssetBuilder.ApplySmoothWalkCycle();
                    UpdateGuideSprites();
                    for (int i = 41; i <= 47; i++)
                        AssetDatabase.DeleteAsset(Art + "Frames/Walk/walk_" + i.ToString("D2") + ".png");
                    PlayerAnimationAssetBuilder.ValidateRuntimeTransition();
                }
                Debug.Log("Redrawn walk cycle: " + drawings.Length + " whole drawings, "
                    + PlayerAnimationController.WalkFrameRate + " fps.");
                if (Application.isBatchMode)
                {
                    if (validate) SubTerra.App.Editor.DataValidation.TestValidationRunner.Batch();
                    else EditorApplication.Exit(0);
                }
            }
            catch (Exception error)
            {
                Debug.LogException(error);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                else throw;
            }
        }

        private static void UpdateGuideSprites()
        {
            var skin = AssetDatabase.LoadAssetAtPath<GameGuideSkin>("Assets/_Project/Resources/UI/GameGuideSkin.asset");
            if (skin == null) throw new FileNotFoundException("GameGuideSkin asset is missing.");
            int index = skin.sprites.FindIndex(entry => entry.key != null && entry.key.StartsWith("player.walk.", StringComparison.Ordinal));
            skin.sprites.RemoveAll(entry => entry.key != null && entry.key.StartsWith("player.walk.", StringComparison.Ordinal));
            if (index < 0) index = 0;
            for (int i = 1; i <= PlayerAnimationController.WalkFrameCount; i++)
                skin.sprites.Insert(index + i - 1, new GuideSpriteEntry
                {
                    key = "player.walk." + i,
                    sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Art + "Frames/Walk/walk_" + i.ToString("D2") + ".png")
                });
            skin.ResetLookup();
            EditorUtility.SetDirty(skin);
            AssetDatabase.SaveAssetIfDirty(skin);
        }
    }
}

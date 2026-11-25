using System;
using System.Collections.Generic;
using System.IO;
using SubTerra.Gameplay.Player;
using UnityEditor;
using UnityEngine;

namespace SubTerra.App.Editor.DataValidation
{
    /// <summary>
    /// 프롬프트 B-210: 새 모션 4종(가스 피해·사다리 오르기·사다리 피격·사다리 채굴) import 설정과
    /// Player.prefab 남자 기본 그림·세트별 값 연결, 남/여 PlayerAnimationSet 갱신(B-208 빌더 재사용).
    /// Player.prefab과 두 세트 에셋만 저장한다. 다른 프리팹·씬은 열거나 저장하지 않는다.
    /// </summary>
    public static class PromptB210PlayerMotionsBuilder
    {
        public const string MaleFramesRoot = "Assets/_Project/Art/Characters/Player/Frames";
        public const string FemaleFramesRoot = PromptB208PlayerAnimationSetBuilder.FemaleFramesRoot;
        public static readonly string[] MotionFolders = { "GasDamage", "LadderClimb", "LadderHit", "LadderMiningV2" };

        public const int MaleGasDamageCount = 34;
        public const int MaleLadderCount = 32;
        public const int MaleLadderHitCount = 25;
        public const int MaleLadderMiningCount = 25;
        public const float LadderCycleDistance = 2f;

        // 발이 가장 높이 올라간 장(1부터). 반 바퀴 간격으로 둔다. 미리보기로 확인한 값.
        public static readonly int[] MaleLadderStepFrames = { 9, 25 };
        public static readonly int[] FemaleLadderStepFrames = { 8, 20 };

        // 시작 1–8(드릴 뻗기), 작업 9↔18 핑퐁, 마무리 19–25(등으로 돌아오기). 남녀 새 그림이 같은 구성이다.
        public static readonly PlayerLadderMiningFrameRanges LadderMiningRanges = new PlayerLadderMiningFrameRanges(8, 9, 18, 19, 25);

        [MenuItem("SubTerra/MVP2/Build Prompt-B 210 Player New Motions")]
        public static void BuildFromMenu()
        {
            try
            {
                Build();
                Validate();
                Debug.Log("[SubTerra] B-210 player motions imported and linked (Player.prefab, male/female animation sets).");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        public static void Build()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            ConfigureImports(MaleFramesRoot);
            ConfigureImports(FemaleFramesRoot);

            var root = PrefabUtility.LoadPrefabContents(PromptB208PlayerAnimationSetBuilder.PlayerPrefabPath);
            try
            {
                var visual = root.transform.Find("VisualRoot");
                var animation = visual != null ? visual.GetComponent<PlayerAnimationController>() : null;
                if (animation == null)
                {
                    throw new MissingReferenceException("Player prefab VisualRoot has no PlayerAnimationController.");
                }

                var serialized = new SerializedObject(animation);
                Write(serialized, "ladderFrames", LoadMale("LadderClimb", "ladder_climb", MaleLadderCount));
                Write(serialized, "ladderMiningFrames", LoadMale("LadderMiningV2", "ladder_mining_v2", MaleLadderMiningCount));
                Write(serialized, "gasDamageFrames", LoadMale("GasDamage", "gas_damage", MaleGasDamageCount));
                Write(serialized, "ladderHitFrames", LoadMale("LadderHit", "ladder_hit", MaleLadderHitCount));
                var steps = serialized.FindProperty("ladderStepFrames");
                steps.arraySize = MaleLadderStepFrames.Length;
                for (var i = 0; i < MaleLadderStepFrames.Length; i++)
                {
                    steps.GetArrayElementAtIndex(i).intValue = MaleLadderStepFrames[i];
                }

                var ranges = serialized.FindProperty("ladderMiningRanges");
                ranges.FindPropertyRelative("startLastFrame").intValue = LadderMiningRanges.startLastFrame;
                ranges.FindPropertyRelative("workFirstFrame").intValue = LadderMiningRanges.workFirstFrame;
                ranges.FindPropertyRelative("workLastFrame").intValue = LadderMiningRanges.workLastFrame;
                ranges.FindPropertyRelative("finishFirstFrame").intValue = LadderMiningRanges.finishFirstFrame;
                ranges.FindPropertyRelative("finishLastFrame").intValue = LadderMiningRanges.finishLastFrame;
                serialized.FindProperty("ladderCycleDistance").floatValue = LadderCycleDistance;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, PromptB208PlayerAnimationSetBuilder.PlayerPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            // 남자 세트는 프리팹 배열을 복사하고, 여자 세트는 여자 폴더에서 다시 채운다.
            PromptB208PlayerAnimationSetBuilder.Build();
        }

        public static void Validate()
        {
            var errors = CollectErrors();
            errors.AddRange(PromptB208PlayerAnimationSetBuilder.CollectErrors());
            if (errors.Count > 0)
            {
                throw new InvalidOperationException("B-210 player motion validation failed:\n" + string.Join("\n", errors));
            }
        }

        public static List<string> CollectErrors()
        {
            var errors = new List<string>();
            var male = AssetDatabase.LoadAssetAtPath<PlayerAnimationSet>(PromptB208PlayerAnimationSetBuilder.MaleSetPath);
            var female = AssetDatabase.LoadAssetAtPath<PlayerAnimationSet>(PromptB208PlayerAnimationSetBuilder.FemaleSetPath);
            if (male == null || female == null)
            {
                errors.Add("Player animation set asset missing.");
                return errors;
            }

            CheckFrames(errors, "Male gasDamageFrames", male.gasDamageFrames, MaleGasDamageCount, MaleFramesRoot + "/GasDamage/");
            CheckFrames(errors, "Male ladderFrames", male.ladderFrames, MaleLadderCount, MaleFramesRoot + "/LadderClimb/");
            CheckFrames(errors, "Male ladderHitFrames", male.ladderHitFrames, MaleLadderHitCount, MaleFramesRoot + "/LadderHit/");
            CheckFrames(errors, "Male ladderMiningFrames", male.ladderMiningFrames, MaleLadderMiningCount, MaleFramesRoot + "/LadderMiningV2/");
            CheckSteps(errors, "Male", male);
            CheckRanges(errors, "Male", male);

            // 여자 새 필드가 비어 있으면 지금 그림으로 대신 돌므로 오류가 아니라 경고만 한다.
            if (Count(female.gasDamageFrames) == 0 || Count(female.ladderHitFrames) == 0)
            {
                Debug.LogWarning("[SubTerra] Female set has empty B-210 fields; runtime falls back to damageFrames.");
            }
            else
            {
                CheckSteps(errors, "Female", female);
                CheckRanges(errors, "Female", female);
            }

            return errors;
        }

        private static void CheckFrames(List<string> errors, string label, Sprite[] frames, int count, string folder)
        {
            if (Count(frames) != count)
            {
                errors.Add(label + " count " + Count(frames) + " != " + count);
                return;
            }

            for (var i = 0; i < frames.Length; i++)
            {
                var path = frames[i] != null ? AssetDatabase.GetAssetPath(frames[i]) : string.Empty;
                if (!path.StartsWith(folder, StringComparison.Ordinal))
                {
                    errors.Add(label + "[" + i + "] is not in " + folder + ": " + path);
                }
            }
        }

        private static void CheckSteps(List<string> errors, string label, PlayerAnimationSet set)
        {
            var steps = set.ladderStepFrames;
            if (steps == null || steps.Length == 0)
            {
                errors.Add(label + " ladderStepFrames is empty.");
                return;
            }

            foreach (var step in steps)
            {
                if (step < 1 || step > Count(set.ladderFrames))
                {
                    errors.Add(label + " ladderStepFrames has " + step + " outside 1.." + Count(set.ladderFrames));
                }
            }
        }

        private static void CheckRanges(List<string> errors, string label, PlayerAnimationSet set)
        {
            var ranges = set.ladderMiningRanges;
            if (!ranges.IsSet || ranges.finishLastFrame > Count(set.ladderMiningFrames)
                || ranges.workFirstFrame <= ranges.startLastFrame || ranges.finishFirstFrame <= ranges.workLastFrame)
            {
                errors.Add(label + " ladderMiningRanges is invalid for " + Count(set.ladderMiningFrames) + " frames.");
            }
        }

        private static int Count(Sprite[] frames) => frames != null ? frames.Length : 0;

        private static void Write(SerializedObject animation, string field, Sprite[] frames)
        {
            var property = animation.FindProperty(field);
            property.arraySize = frames.Length;
            for (var i = 0; i < frames.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = frames[i];
            }
        }

        private static Sprite[] LoadMale(string folder, string prefix, int count)
        {
            var frames = new Sprite[count];
            for (var i = 0; i < count; i++)
            {
                var path = MaleFramesRoot + "/" + folder + "/" + prefix + "_" + (i + 1).ToString("00") + ".png";
                frames[i] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (frames[i] == null)
                {
                    throw new FileNotFoundException("B-210 male sprite is missing.", path);
                }
            }

            return frames;
        }

        // 기존 256 플레이어 그림과 같은 설정(Sprite 한 장, PPU 256, 중앙 피벗, FullRect, Bilinear, 무압축, Clamp).
        private static void ConfigureImports(string framesRoot)
        {
            foreach (var folder in MotionFolders)
            {
                var path = framesRoot + "/" + folder;
                if (!AssetDatabase.IsValidFolder(path))
                {
                    throw new DirectoryNotFoundException("B-210 frame folder missing: " + path);
                }

                foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { path }))
                {
                    var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    if (!assetPath.EndsWith(".png", StringComparison.Ordinal)
                        || !(AssetImporter.GetAtPath(assetPath) is TextureImporter importer))
                    {
                        continue;
                    }

                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.spritePixelsPerUnit = 256f;
                    var settings = new TextureImporterSettings();
                    importer.ReadTextureSettings(settings);
                    settings.spriteAlignment = (int)SpriteAlignment.Center;
                    settings.spritePivot = new Vector2(0.5f, 0.5f);
                    settings.spriteMeshType = SpriteMeshType.FullRect;
                    importer.SetTextureSettings(settings);
                    importer.mipmapEnabled = false;
                    importer.alphaIsTransparency = true;
                    importer.isReadable = false;
                    importer.wrapMode = TextureWrapMode.Clamp;
                    importer.filterMode = FilterMode.Bilinear;
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.maxTextureSize = 2048;
                    importer.SaveAndReimport();
                }
            }
        }
    }
}

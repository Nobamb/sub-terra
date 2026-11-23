using System;
using System.Collections.Generic;
using System.IO;
using SubTerra.Gameplay.Player;
using SubTerra.Shared;
using UnityEditor;
using UnityEngine;

namespace SubTerra.App.Editor.DataValidation
{
    /// <summary>
    /// 프롬프트 B-208: 여자 캐릭터 프레임 import 설정, 남/여 PlayerAnimationSet 에셋, Player.prefab 연결.
    /// 남자 세트는 프리팹에 이미 연결된 그림 배열을 그대로 복사한다(남자 원본 PNG·meta는 건드리지 않는다).
    /// 다른 프리팹·씬은 열거나 저장하지 않는다.
    /// </summary>
    public static class PromptB208PlayerAnimationSetBuilder
    {
        public const string PlayerPrefabPath = "Assets/_Project/Prefabs/Gameplay/Player/Player.prefab";
        public const string FemaleFramesRoot = "Assets/_Project/Art/Characters/Player/Female/Frames";
        public const string SetsFolder = "Assets/_Project/Art/Characters/Player/AnimationSets";
        public const string MaleSetPath = SetsFolder + "/PlayerAnimationSet_Male.asset";
        public const string FemaleSetPath = SetsFolder + "/PlayerAnimationSet_Female.asset";
        private const float FramePixelsPerUnit = 256f;
        // 남자 사다리는 1254px 캔버스·PPU 1084다. 같은 캔버스를 256으로 줄인 여자 그림이 같은 월드 크기가 되게 맞춘다.
        public const float LadderPixelsPerUnit = 256f * 1084f / 1254f;

        public static readonly string[] DamageNames =
        {
            "damage_01", "damage_1_2", "damage_02", "damage_2_3",
            "damage_03", "damage_3_4", "damage_04", "damage_4_idle"
        };

        public static readonly string[] LadderNames =
        {
            "ladder_back_neutral_v2", "ladder_back_style_02", "ladder_back_style_03",
            "ladder_back_style_04", "ladder_back_style_05"
        };

        /// <summary>B-208 표의 여자 장수. 빠진 장은 복제하지 않는다.</summary>
        public static readonly IReadOnlyDictionary<string, int> FemaleCounts = new Dictionary<string, int>
        {
            { "idleFrames", 1 }, { "walkFrames", 39 }, { "miningFrames", 23 }, { "ladderMiningFrames", 23 },
            { "jumpReadyFrames", 7 }, { "jumpRiseFrames", 23 }, { "jumpFallFrames", 23 },
            { "knockoutFrames", 47 }, { "ladderFrames", 5 }, { "damageFrames", 8 }
        };

        private static readonly string[] FrameFields =
        {
            "idleFrames", "walkFrames", "jumpReadyFrames", "jumpRiseFrames", "jumpFallFrames",
            "ladderFrames", "miningFrames", "ladderMiningFrames", "damageFrames", "knockoutFrames"
        };

        [MenuItem("SubTerra/MVP2/Build Prompt-B 208 Player Animation Sets")]
        public static void BuildFromMenu()
        {
            try
            {
                Build();
                Validate();
                Debug.Log("[SubTerra] B-208 player animation sets built and linked to Player.prefab.");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        public static void Build()
        {
            ConfigureFemaleImports();
            EnsureFolder(SetsFolder);
            var female = CreateOrLoad(FemaleSetPath);
            FillFemale(female);
            var male = CreateOrLoad(MaleSetPath);

            var root = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            try
            {
                var visual = root.transform.Find("VisualRoot");
                var animation = visual != null ? visual.GetComponent<PlayerAnimationController>() : null;
                if (animation == null)
                {
                    throw new MissingReferenceException("Player prefab VisualRoot has no PlayerAnimationController.");
                }

                var serialized = new SerializedObject(animation);
                FillMaleFromPrefab(male, serialized);
                serialized.FindProperty("maleAnimationSet").objectReferenceValue = male;
                serialized.FindProperty("femaleAnimationSet").objectReferenceValue = female;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            EditorUtility.SetDirty(male);
            EditorUtility.SetDirty(female);
            AssetDatabase.SaveAssetIfDirty(male);
            AssetDatabase.SaveAssetIfDirty(female);
        }

        /// <summary>여자 세트가 표 장수와 같고, 여자 폴더 그림만 쓰고, 프리팹에 남/여 세트가 연결됐는지 본다.</summary>
        public static void Validate()
        {
            var errors = CollectErrors();
            if (errors.Count > 0)
            {
                throw new InvalidOperationException("B-208 player animation set validation failed:\n" + string.Join("\n", errors));
            }
        }

        public static List<string> CollectErrors()
        {
            var errors = new List<string>();
            var female = AssetDatabase.LoadAssetAtPath<PlayerAnimationSet>(FemaleSetPath);
            var male = AssetDatabase.LoadAssetAtPath<PlayerAnimationSet>(MaleSetPath);
            if (female == null || male == null)
            {
                errors.Add("Player animation set asset missing.");
                return errors;
            }

            if (female.gender != PlayerGender.Female) errors.Add("Female set gender is not Female.");
            if (male.gender != PlayerGender.Male) errors.Add("Male set gender is not Male.");
            foreach (var pair in FemaleCounts)
            {
                var frames = Frames(female, pair.Key);
                var count = frames != null ? frames.Length : 0;
                if (count != pair.Value)
                {
                    errors.Add("Female " + pair.Key + " count " + count + " != " + pair.Value);
                    continue;
                }

                for (var i = 0; i < count; i++)
                {
                    var path = frames[i] != null ? AssetDatabase.GetAssetPath(frames[i]) : string.Empty;
                    if (!path.StartsWith(FemaleFramesRoot + "/", StringComparison.Ordinal))
                    {
                        errors.Add("Female " + pair.Key + "[" + i + "] is not a female frame: " + path);
                    }
                }
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            var animation = prefab != null ? prefab.GetComponentInChildren<PlayerAnimationController>(true) : null;
            if (animation == null || animation.FemaleAnimationSet != female || animation.MaleAnimationSet != male)
            {
                errors.Add("Player.prefab is not linked to the male/female animation sets.");
            }

            return errors;
        }

        public static Sprite[] Frames(PlayerAnimationSet set, string field)
        {
            switch (field)
            {
                case "idleFrames": return set.idleFrames;
                case "walkFrames": return set.walkFrames;
                case "jumpReadyFrames": return set.jumpReadyFrames;
                case "jumpRiseFrames": return set.jumpRiseFrames;
                case "jumpFallFrames": return set.jumpFallFrames;
                case "ladderFrames": return set.ladderFrames;
                case "miningFrames": return set.miningFrames;
                case "ladderMiningFrames": return set.ladderMiningFrames;
                case "damageFrames": return set.damageFrames;
                case "knockoutFrames": return set.knockoutFrames;
                default: throw new ArgumentOutOfRangeException(nameof(field), field, null);
            }
        }

        private static void FillFemale(PlayerAnimationSet set)
        {
            set.gender = PlayerGender.Female;
            set.idleFrames = Load("Idle", new[] { "player_idle_01" });
            set.walkFrames = Load("Walk", Numbered("walk", 39));
            set.jumpReadyFrames = Load("JumpReady", Numbered("jump_ready", 7));
            set.jumpRiseFrames = Load("JumpRise", Numbered("jump_rise", 23));
            set.jumpFallFrames = Load("JumpFall", Numbered("jump_fall", 23));
            set.ladderFrames = Load("LadderBack", LadderNames);
            set.miningFrames = Load("Mining", Numbered("mining", 23));
            set.ladderMiningFrames = Load("LadderMining", Numbered("ladder_mining", 23));
            set.damageFrames = Load("Damage", DamageNames);
            set.knockoutFrames = Load("Knockout", Numbered("knockout", 47));
            EditorUtility.SetDirty(set);
        }

        private static void FillMaleFromPrefab(PlayerAnimationSet set, SerializedObject animation)
        {
            set.gender = PlayerGender.Male;
            set.idleFrames = Read(animation, "idleFrames");
            set.walkFrames = Read(animation, "walkFrames");
            set.jumpReadyFrames = Read(animation, "jumpReadyFrames");
            set.jumpRiseFrames = Read(animation, "jumpRiseFrames");
            set.jumpFallFrames = Read(animation, "jumpFallFrames");
            set.ladderFrames = Read(animation, "ladderFrames");
            set.miningFrames = Read(animation, "miningFrames");
            set.ladderMiningFrames = Read(animation, "ladderMiningFrames");
            set.damageFrames = Read(animation, "damageFrames");
            set.knockoutFrames = Read(animation, "knockoutFrames");
            foreach (var field in FrameFields)
            {
                var frames = Frames(set, field);
                if (frames == null || frames.Length == 0 || Array.IndexOf(frames, null) >= 0)
                {
                    throw new InvalidOperationException("Player.prefab male " + field + " is empty or has a missing sprite.");
                }
            }

            EditorUtility.SetDirty(set);
        }

        private static Sprite[] Read(SerializedObject animation, string field)
        {
            var property = animation.FindProperty(field);
            var frames = new Sprite[property.arraySize];
            for (var i = 0; i < frames.Length; i++)
            {
                frames[i] = property.GetArrayElementAtIndex(i).objectReferenceValue as Sprite;
            }

            return frames;
        }

        private static string[] Numbered(string prefix, int count)
        {
            var names = new string[count];
            for (var i = 0; i < count; i++)
            {
                names[i] = prefix + "_" + (i + 1).ToString("00");
            }

            return names;
        }

        private static Sprite[] Load(string folder, IReadOnlyList<string> names)
        {
            var frames = new Sprite[names.Count];
            for (var i = 0; i < names.Count; i++)
            {
                var path = FemaleFramesRoot + "/" + folder + "/" + names[i] + ".png";
                frames[i] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (frames[i] == null)
                {
                    throw new FileNotFoundException("Female player sprite is missing.", path);
                }
            }

            return frames;
        }

        private static void ConfigureFemaleImports()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { FemaleFramesRoot }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(".png", StringComparison.Ordinal)
                    || !(AssetImporter.GetAtPath(path) is TextureImporter importer))
                {
                    continue;
                }

                // 남자 같은 모션 PNG와 같은 설정(Sprite 한 장, 중앙 피벗, FullRect, Bilinear, 무압축).
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = path.Contains("/LadderBack/") ? LadderPixelsPerUnit : FramePixelsPerUnit;
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

        private static PlayerAnimationSet CreateOrLoad(string path)
        {
            var set = AssetDatabase.LoadAssetAtPath<PlayerAnimationSet>(path);
            if (set != null)
            {
                return set;
            }

            set = ScriptableObject.CreateInstance<PlayerAnimationSet>();
            AssetDatabase.CreateAsset(set, path);
            return set;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}

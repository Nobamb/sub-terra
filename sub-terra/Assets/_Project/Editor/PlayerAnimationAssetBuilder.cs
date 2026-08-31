using System.Collections.Generic;
using System.IO;
using System;
using System.Reflection;
using SubTerra.Gameplay.Player;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace SubTerra.Editor
{
    public static class PlayerAnimationAssetBuilder
    {
        private const string PlayerRoot = "Assets/_Project/Art/Characters/Player";
        private const string FramesRoot = PlayerRoot + "/Frames";
        private const string AnimationsRoot = PlayerRoot + "/Animations";
        private const string ControllerPath = PlayerRoot + "/PlayerAnimator.controller";
        private const string PlayerPrefabPath = "Assets/_Project/Prefabs/Gameplay/Player/Player.prefab";

        private readonly struct AnimationDefinition
        {
            public AnimationDefinition(string stateName, string folder, string prefix, int frameCount, float frameRate, bool loop)
            {
                StateName = stateName;
                Folder = folder;
                Prefix = prefix;
                FrameCount = frameCount;
                FrameRate = frameRate;
                Loop = loop;
            }

            public string StateName { get; }
            public string Folder { get; }
            public string Prefix { get; }
            public int FrameCount { get; }
            public float FrameRate { get; }
            public bool Loop { get; }
        }

        private static readonly AnimationDefinition[] Definitions =
        {
            new("Idle", "Idle", "player_idle", 1, 4f, true),
            new("Walk", "Walk", "walk", 10, 10f, true),
            new("Jump", "Jump", "jump", 10, 12f, false),
            new("Ladder", "Ladder", "ladder", 8, 8f, true),
            new("LadderDown", "LadderDown", "ladder_down", 8, 8f, true),
            new("Mining", "Mining", "mining", 8, 10f, true),
            new("Damage", "Damage", "damage", 4, 10f, false),
            new("Knockout", "Knockout", "knockout", 8, 8f, false)
        };

        [MenuItem("SubTerra/Build Player Animation Assets")]
        public static void Build()
        {
            EnsureFolder(PlayerRoot);
            EnsureFolder(AnimationsRoot);
            ConfigureFrameImports();

            var clips = new Dictionary<string, AnimationClip>();
            foreach (var definition in Definitions)
            {
                clips.Add(definition.StateName, CreateOrUpdateClip(definition));
            }

            var controller = CreateController(clips);
            ApplyToPlayerPrefab(controller, clips["Idle"]);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Player animation assets were built successfully.");
        }

        [MenuItem("SubTerra/Validate Player Animation Runtime")]
        public static void ValidateRuntimeTransition()
        {
            GameObject player = null;
            try
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
                if (prefab == null)
                {
                    throw new FileNotFoundException("Player prefab is missing.", PlayerPrefabPath);
                }

                player = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                var visualRoot = player != null ? player.transform.Find("VisualRoot") : null;
                var renderer = visualRoot != null ? visualRoot.GetComponent<SpriteRenderer>() : null;
                var animator = visualRoot != null ? visualRoot.GetComponent<Animator>() : null;
                var animationController = visualRoot != null ? visualRoot.GetComponent<PlayerAnimationController>() : null;
                var movement = player != null ? player.GetComponent<PlayerMovement>() : null;
                if (renderer == null || animator == null || animationController == null || movement == null)
                {
                    throw new MissingReferenceException("Player animation runtime components are incomplete.");
                }

                InvokePrivate(movement, "Awake");
                InvokePrivate(animationController, "Awake");
                animator.enabled = false;
                var idleSprite = renderer.sprite;
                if (idleSprite == null)
                {
                    throw new MissingReferenceException("Idle sprite was not applied to Player VisualRoot.");
                }

                var groundedField = typeof(PlayerMovement).GetField(
                    "<IsGrounded>k__BackingField",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                groundedField?.SetValue(movement, true);
                movement.SetMoveInput(1f);
                InvokePrivate(animationController, "LateUpdate");
                if (renderer.sprite == null || renderer.sprite == idleSprite)
                {
                    throw new InvalidOperationException("Walk input did not replace the Idle sprite.");
                }

                Debug.Log("Player animation runtime validation passed: movement input changed Idle to Walk.");
            }
            finally
            {
                if (player != null)
                {
                    UnityEngine.Object.DestroyImmediate(player);
                }
            }
        }

        private static void ConfigureFrameImports()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { FramesRoot }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(".png"))
                {
                    continue;
                }

                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null)
                {
                    continue;
                }

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 256f;
                var spriteSettings = new TextureImporterSettings();
                importer.ReadTextureSettings(spriteSettings);
                spriteSettings.spriteMeshType = SpriteMeshType.FullRect;
                importer.SetTextureSettings(spriteSettings);
                importer.mipmapEnabled = false;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Bilinear;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
        }

        private static AnimationClip CreateOrUpdateClip(AnimationDefinition definition)
        {
            var clipPath = AnimationsRoot + "/Player" + definition.StateName + ".anim";
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            if (clip == null)
            {
                clip = new AnimationClip();
                AssetDatabase.CreateAsset(clip, clipPath);
            }

            clip.frameRate = definition.FrameRate;
            clip.ClearCurves();
            var keyframes = new ObjectReferenceKeyframe[definition.FrameCount];
            for (var index = 0; index < definition.FrameCount; index++)
            {
                var spritePath = FramesRoot + "/" + definition.Folder + "/" + definition.Prefix + "_" + (index + 1).ToString("D2") + ".png";
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
                if (sprite == null)
                {
                    throw new FileNotFoundException("Player animation sprite is missing.", spritePath);
                }

                keyframes[index] = new ObjectReferenceKeyframe
                {
                    time = index / definition.FrameRate,
                    value = sprite
                };
            }

            var binding = new EditorCurveBinding
            {
                path = string.Empty,
                type = typeof(SpriteRenderer),
                propertyName = "m_Sprite"
            };
            AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = definition.Loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip);
            return clip;
        }

        private static AnimatorController CreateController(IReadOnlyDictionary<string, AnimationClip> clips)
        {
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) != null)
            {
                AssetDatabase.DeleteAsset(ControllerPath);
            }

            var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            var stateMachine = controller.layers[0].stateMachine;
            foreach (var definition in Definitions)
            {
                var state = stateMachine.AddState(definition.StateName);
                state.motion = clips[definition.StateName];
                if (definition.StateName == "Idle")
                {
                    stateMachine.defaultState = state;
                }
            }

            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static void ApplyToPlayerPrefab(RuntimeAnimatorController controller, AnimationClip idleClip)
        {
            var prefabRoot = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            try
            {
                var visualRoot = prefabRoot.transform.Find("VisualRoot");
                if (visualRoot == null)
                {
                    throw new MissingReferenceException("Player prefab is missing VisualRoot.");
                }

                var spriteRenderer = visualRoot.GetComponent<SpriteRenderer>();
                if (spriteRenderer == null)
                {
                    spriteRenderer = visualRoot.gameObject.AddComponent<SpriteRenderer>();
                }

                spriteRenderer.sprite = idleClip != null
                    ? AssetDatabase.LoadAssetAtPath<Sprite>(FramesRoot + "/Idle/player_idle_01.png")
                    : null;
                spriteRenderer.drawMode = SpriteDrawMode.Simple;
                spriteRenderer.color = Color.white;

                var animator = visualRoot.GetComponent<Animator>();
                if (animator == null)
                {
                    animator = visualRoot.gameObject.AddComponent<Animator>();
                }

                animator.runtimeAnimatorController = controller;
                animator.enabled = true;
                var animationController = visualRoot.GetComponent<PlayerAnimationController>();
                if (animationController == null)
                {
                    animationController = visualRoot.gameObject.AddComponent<PlayerAnimationController>();
                }

                animationController.ConfigureFrames(
                    spriteRenderer,
                    prefabRoot.GetComponent<PlayerMovement>(),
                    LoadFrames("Idle"),
                    LoadFrames("Walk"),
                    LoadFrames("Jump"),
                    LoadFrames("Ladder"),
                    LoadFrames("LadderDown"),
                    LoadFrames("Mining"),
                    LoadFrames("Damage"),
                    LoadFrames("Knockout"));

                PrefabUtility.SaveAsPrefabAsset(prefabRoot, PlayerPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        private static Sprite[] LoadFrames(string stateName)
        {
            foreach (var definition in Definitions)
            {
                if (definition.StateName != stateName)
                {
                    continue;
                }

                var frames = new Sprite[definition.FrameCount];
                for (var index = 0; index < definition.FrameCount; index++)
                {
                    var spritePath = FramesRoot + "/" + definition.Folder + "/"
                        + definition.Prefix + "_" + (index + 1).ToString("D2") + ".png";
                    frames[index] = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
                    if (frames[index] == null)
                    {
                        throw new FileNotFoundException("Player animation sprite is missing.", spritePath);
                    }
                }

                return frames;
            }

            throw new System.ArgumentOutOfRangeException(nameof(stateName), stateName, "Unknown player animation state.");
        }

        private static void EnsureFolder(string assetPath)
        {
            if (AssetDatabase.IsValidFolder(assetPath))
            {
                return;
            }

            var parent = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
            var folderName = Path.GetFileName(assetPath);
            if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(folderName))
            {
                throw new DirectoryNotFoundException(assetPath);
            }

            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, folderName);
        }

        private static void InvokePrivate(object target, string methodName)
        {
            var method = target.GetType().GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (method == null)
            {
                throw new MissingMethodException(target.GetType().FullName, methodName);
            }

            method.Invoke(target, null);
        }
    }
}

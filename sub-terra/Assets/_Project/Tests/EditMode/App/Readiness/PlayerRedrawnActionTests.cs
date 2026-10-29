using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using SubTerra.App.UI.Guide;
using SubTerra.Gameplay.Player;
using SubTerra.Shared;
using UnityEditor;
using UnityEngine;

namespace SubTerra.App.Tests.EditMode.Readiness
{
    public sealed class PlayerRedrawnActionTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private const string Art = "Assets/_Project/Art/Characters/Player/Frames/";
        private GameObject player;
        private PlayerMovement movement;
        private PlayerAnimationController animation;
        private SpriteRenderer renderer;
        private SerializedObject fields;

        [SetUp]
        public void SetUp()
        {
            player = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Project/Prefabs/Gameplay/Player/Player.prefab"));
            movement = player.GetComponent<PlayerMovement>();
            animation = player.GetComponentInChildren<PlayerAnimationController>();
            renderer = animation.GetComponent<SpriteRenderer>();
            Invoke(movement, "Awake");
            Invoke(animation, "Awake");
            typeof(PlayerMovement).GetField("<IsGrounded>k__BackingField", Private).SetValue(movement, true);
            fields = new SerializedObject(animation);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(player);

        [TestCase("miningFrames", "Mining", "mining", 24, "player-mining-24f-20261029")]
        [TestCase("knockoutFrames", "Knockout", "knockout", 48, "player-knockout-48f-20261029")]
        public void Prefab_HasEveryApprovedUniqueDrawing(string field, string folder, string prefix, int count, string source)
        {
            var frames = fields.FindProperty(field);
            Assert.That(frames.arraySize, Is.EqualTo(count));
            var hashes = new HashSet<string>();
            for (int i = 1; i <= count; i++)
            {
                var sprite = (Sprite)frames.GetArrayElementAtIndex(i - 1).objectReferenceValue;
                Assert.That(sprite, Is.Not.Null);
                string path = Art + folder + "/" + prefix + "_" + i.ToString("D2") + ".png";
                Assert.That(AssetDatabase.GetAssetPath(sprite), Is.EqualTo(path));
                byte[] bytes = File.ReadAllBytes(path);
                CollectionAssert.AreEqual(File.ReadAllBytes("../work_process/images/" + source
                    + "/runtime-frames/" + prefix + "-" + i.ToString("D2") + ".png"), bytes);
                using (var sha = System.Security.Cryptography.SHA256.Create())
                    hashes.Add(System.Convert.ToBase64String(sha.ComputeHash(bytes)));
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                Assert.That(importer.spritePixelsPerUnit, Is.EqualTo(256f));
                Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Bilinear));
                Assert.That(importer.mipmapEnabled, Is.False);
                Assert.That(importer.textureCompression, Is.EqualTo(TextureImporterCompression.Uncompressed));
                AssertTransparentMargin(bytes, 2);
            }
            Assert.That(hashes.Count, Is.EqualTo(count));
        }

        [Test]
        public void Guide_UsesAllMiningAndSameLadderFramesAndProneKnockout()
        {
            var skin = AssetDatabase.LoadAssetAtPath<GameGuideSkin>("Assets/_Project/Resources/UI/GameGuideSkin.asset");
            for (int i = 1; i <= 24; i++)
                Assert.That(skin.Get("player.mine." + i), Is.EqualTo(fields.FindProperty("miningFrames")
                    .GetArrayElementAtIndex(i - 1).objectReferenceValue));
            for (int i = 1; i <= 5; i++)
                Assert.That(skin.Get("player.ladder." + i), Is.EqualTo(fields.FindProperty("ladderFrames")
                    .GetArrayElementAtIndex(i - 1).objectReferenceValue));
            Assert.That(skin.Get("player.knockout.1"), Is.EqualTo(fields.FindProperty("knockoutFrames")
                .GetArrayElementAtIndex(47).objectReferenceValue));
        }

        [TestCase(RunFailureCause.GasExposure)]
        [TestCase(RunFailureCause.StructuralCollapse)]
        [TestCase(RunFailureCause.Fall)]
        public void Knockout_PlaysTwoSecondsThenBreathesOnlyForGas_RescueResets(RunFailureCause cause)
        {
            var survival = player.GetComponent<PlayerSurvivalController>();
            if (survival == null) survival = player.AddComponent<PlayerSurvivalController>();
            Invoke(survival, "Awake");
            animation.BindSurvival(survival);
            survival.State.TryApplyDamage(cause, 999, Time.unscaledTime, 0f, true, out var incapacitated);
            Assert.That(incapacitated, Is.True);
            Invoke(animation, "HandleFailureRequested", new RunFailureInputDto { cause = cause });
            Invoke(animation, "LateUpdate");
            Assert.That(fields.FindProperty("knockoutFps").floatValue, Is.EqualTo(24f));
            Assert.That(fields.FindProperty("knockoutBreathingFps").floatValue, Is.EqualTo(12f));
            foreach (var sample in new[] { 0.001f, 0.501f, 1.501f, 1.999f, 2.001f, 2.501f, 3.001f })
            {
                typeof(PlayerAnimationController).GetField("stateStartedAt", Private)
                    .SetValue(animation, Time.unscaledTime - sample);
                Invoke(animation, "Play", "Knockout");
                int expected = cause == RunFailureCause.GasExposure && sample >= 2f
                    ? 36 + Mathf.FloorToInt((sample - 2f) * 12f) % 12
                    : Mathf.Min(47, Mathf.FloorToInt(sample * 24f));
                Assert.That(renderer.sprite, Is.EqualTo(fields.FindProperty("knockoutFrames")
                    .GetArrayElementAtIndex(expected).objectReferenceValue), "time " + sample);
            }
            survival.RestoreAfterRescue();
            Invoke(animation, "LateUpdate");
            Assert.That(renderer.sprite, Is.EqualTo(fields.FindProperty("idleFrames").GetArrayElementAtIndex(0).objectReferenceValue));
            Assert.That(typeof(PlayerAnimationController).GetField("knockoutCause", Private).GetValue(animation),
                Is.EqualTo(RunFailureCause.Unknown));
        }

        [Test]
        public void LadderFacing_StaysPositiveDuringGrace_ResumesDirectionOnExitAndMining()
        {
            var facing = player.GetComponent<PlayerFacing>();
            Invoke(facing, "Awake");
            movement.EnterLadder();
            movement.SetMoveInput(-1f);
            Invoke(animation, "LateUpdate");
            Invoke(facing, "LateUpdate");
            Assert.That(animation.IsShowingLadderVisual, Is.True);
            Assert.That(renderer.transform.localScale.x, Is.GreaterThan(0f));
            movement.ExitLadder();
            Invoke(animation, "LateUpdate");
            Invoke(facing, "LateUpdate");
            Assert.That(renderer.transform.localScale.x, Is.GreaterThan(0f), "사다리 그림 유지 유예");
            typeof(PlayerAnimationController).GetField("ladderVisualGraceUntil", Private).SetValue(animation, 0f);
            Invoke(animation, "LateUpdate");
            Invoke(facing, "LateUpdate");
            Assert.That(renderer.transform.localScale.x, Is.LessThan(0f));
            movement.SetMoveInput(0f);
            movement.EnterLadder();
            animation.SetMining(true);
            Invoke(animation, "LateUpdate");
            Invoke(facing, "LateUpdate");
            Assert.That(animation.IsShowingLadderVisual, Is.False);
            Assert.That(renderer.transform.localScale.x, Is.LessThan(0f));
        }

        [Test]
        public void Mining_CompletionSurvivesSetFalse_MovementInterruptsFinish()
        {
            animation.SetMining(true);
            animation.NotifyMiningCompleted();
            animation.SetMining(false);
            Invoke(animation, "LateUpdate");
            Assert.That(typeof(PlayerAnimationController).GetField("currentState", Private).GetValue(animation), Is.EqualTo("Mining"));
            movement.SetMoveInput(1f);
            Invoke(animation, "LateUpdate");
            Assert.That(typeof(PlayerAnimationController).GetField("currentState", Private).GetValue(animation), Is.EqualTo("Walk"));
        }

        private static void Invoke(object target, string name, params object[] args) =>
            target.GetType().GetMethod(name, Private).Invoke(target, args);

        private static void AssertTransparentMargin(byte[] bytes, int margin)
        {
            var texture = new Texture2D(2, 2);
            try
            {
                texture.LoadImage(bytes);
                var pixels = texture.GetPixels32();
                for (int y = 0; y < texture.height; y++)
                for (int x = 0; x < texture.width; x++)
                    if (x < margin || y < margin || x >= texture.width - margin || y >= texture.height - margin)
                        Assert.That(pixels[y * texture.width + x].a, Is.EqualTo(0), "그림 가장자리 잘림");
            }
            finally { Object.DestroyImmediate(texture); }
        }
    }
}

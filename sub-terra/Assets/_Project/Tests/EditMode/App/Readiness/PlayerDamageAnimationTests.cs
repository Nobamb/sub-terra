using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using SubTerra.Gameplay.Player;
using SubTerra.Shared;
using UnityEditor;
using UnityEngine;

namespace SubTerra.App.Tests.EditMode.Readiness
{
    // B-176: 다침 8장 재생·끝난 뒤 전환·사다리 위 다침 방향.
    public sealed class PlayerDamageAnimationTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private const string Art = "Assets/_Project/Art/Characters/Player/Frames/Damage/";
        private const string Runtime = "../work_process/images/player-damage-8f-20261029/runtime-frames/";
        private static readonly string[] Order =
            { "damage_01", "damage_1_2", "damage_02", "damage_2_3", "damage_03", "damage_3_4", "damage_04", "damage_4_idle" };
        // 기존 4장은 파일·GUID를 바꾸지 않는다.
        private static readonly Dictionary<string, string> ExistingGuids = new()
        {
            { "damage_01", "63c487259e5d9ee48a01f2a58768e44f" },
            { "damage_02", "1c0dbdafb70f95240816921f05b5c58a" },
            { "damage_03", "1c9becf97ca21134f8836c1ed99e889a" },
            { "damage_04", "d865b2e9d5d7e2d49aca39aca0256df7" }
        };
        private static readonly Dictionary<string, string> NewRuntime = new()
        {
            { "damage_1_2", "damage-1-2" }, { "damage_2_3", "damage-2-3" },
            { "damage_3_4", "damage-3-4" }, { "damage_4_idle", "damage-4-idle" }
        };

        private GameObject player;
        private PlayerMovement movement;
        private PlayerAnimationController animation;
        private PlayerFacing facing;
        private SpriteRenderer renderer;
        private Sprite[] frames;
        private float health;

        [SetUp]
        public void SetUp()
        {
            player = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Project/Prefabs/Gameplay/Player/Player.prefab"));
            movement = player.GetComponent<PlayerMovement>();
            animation = player.GetComponentInChildren<PlayerAnimationController>();
            facing = player.GetComponent<PlayerFacing>();
            renderer = animation.GetComponent<SpriteRenderer>();
            frames = (Sprite[])Get(animation, "damageFrames");
            Invoke(movement, "Awake");
            Invoke(animation, "Awake");
            Invoke(facing, "Awake");
            Set(movement, "<IsGrounded>k__BackingField", true);
            health = 100f;
            Set(animation, "previousHealth", health);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(player);

        [Test]
        public void Prefab_HasEightDamageFramesInOrder_ExistingGuidsKept_NewDrawingsAligned()
        {
            Assert.That(frames.Length, Is.EqualTo(8));
            var hashes = new HashSet<string>();
            for (int i = 0; i < Order.Length; i++)
            {
                string path = Art + Order[i] + ".png";
                Assert.That(AssetDatabase.GetAssetPath(frames[i]), Is.EqualTo(path));
                if (ExistingGuids.TryGetValue(Order[i], out var guid))
                {
                    Assert.That(AssetDatabase.AssetPathToGUID(path), Is.EqualTo(guid));
                    continue;
                }

                byte[] bytes = File.ReadAllBytes(path);
                CollectionAssert.AreEqual(File.ReadAllBytes(Runtime + NewRuntime[Order[i]] + ".png"), bytes);
                hashes.Add(System.Convert.ToBase64String(bytes));
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                Assert.That(importer.spritePixelsPerUnit, Is.EqualTo(256f));
                Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Bilinear));
                Assert.That(importer.mipmapEnabled, Is.False);
                Assert.That(importer.textureCompression, Is.EqualTo(TextureImporterCompression.Uncompressed));
                Assert.That(frames[i].pivot, Is.EqualTo(new Vector2(128, 128)));
                var texture = new Texture2D(2, 2);
                try
                {
                    Assert.That(texture.LoadImage(bytes), Is.True);
                    Assert.That(texture.width, Is.EqualTo(256));
                    Assert.That(texture.height, Is.EqualTo(256));
                    var pixels = texture.GetPixels32();
                    int lowestRow = 256, noise = 0;
                    for (int y = 0; y < 256; y++)
                    for (int x = 0; x < 256; x++)
                    {
                        byte alpha = pixels[y * 256 + x].a;
                        if (alpha >= 32) lowestRow = Mathf.Min(lowestRow, y);
                        if (alpha > 0 && alpha < 8) noise++;
                    }
                    Assert.That(255 - lowestRow, Is.EqualTo(242).Within(2), path + " 발끝(위에서)");
                    Assert.That(noise, Is.Zero, path + " 알파 1~7 잡티");
                }
                finally { Object.DestroyImmediate(texture); }
            }
            Assert.That(hashes.Count, Is.EqualTo(4));
        }

        [Test]
        public void Damage_PlaysEightFramesEveryFiftyMs_AndEndsAfterPointFour()
        {
            float duration = Hit();
            Assert.That(duration, Is.EqualTo(.4f).Within(.001f));
            Tick();
            Assert.That(State, Is.EqualTo("Damage"));
            for (int i = 0; i < 8; i++)
            {
                At(i * .05f + .025f, duration);
                Assert.That(renderer.sprite, Is.EqualTo(frames[i]), "frame " + (i + 1));
            }
            At(.39f, duration);
            Assert.That(State, Is.EqualTo("Damage"));
            Assert.That(renderer.sprite, Is.EqualTo(frames[7]), "마지막 4-idle 장도 보인다");
            At(.401f, duration);
            Assert.That(State, Is.EqualTo("Idle"));
        }

        [Test]
        public void HitDuringDamage_ExtendsOnly_DoesNotRestartDrawing()
        {
            float duration = Hit();
            Tick();
            At(.225f, duration);
            Assert.That(renderer.sprite, Is.EqualTo(frames[4]));
            float startedAt = (float)Get(animation, "stateStartedAt");
            Hit();
            Tick();
            Assert.That(State, Is.EqualTo("Damage"));
            Assert.That((float)Get(animation, "stateStartedAt"), Is.EqualTo(startedAt));
            Assert.That(renderer.sprite, Is.EqualTo(frames[4]));
        }

        [TestCase("idle", "Idle")]
        [TestCase("walk", "Walk")]
        [TestCase("air", "Jump")]
        [TestCase("ladder", "LadderIdle")]
        [TestCase("mining", "Mining")]
        public void DamageEnd_GoesStraightToCurrentAction(string situation, string expected)
        {
            if (situation == "mining") animation.SetMining(true);
            float duration = Hit();
            Tick();
            Assert.That(State, Is.EqualTo("Damage"));
            if (situation == "walk") movement.SetMoveInput(1f);
            else if (situation == "air")
            {
                Set(movement, "jumpUsedUntilLand", true);
                Set(movement, "<IsGrounded>k__BackingField", false);
            }
            else if (situation == "ladder") movement.EnterLadder();
            At(.2f, duration);
            Assert.That(State, Is.EqualTo("Damage"), "다침 중에는 입력·물리와 무관하게 다침 그림");
            At(.401f, duration);
            Assert.That(State, Is.EqualTo(expected));
        }

        [Test]
        public void LadderDamage_LeftInput_DoesNotFlip_AndReturnsToLadderUnflipped()
        {
            movement.EnterLadder();
            Tick();
            float duration = Hit();
            movement.SetMoveInput(-1f);
            Tick();
            Assert.That(State, Is.EqualTo("Damage"));
            Assert.That(renderer.transform.localScale.x, Is.GreaterThan(0f));
            At(.2f, duration);
            Assert.That(renderer.transform.localScale.x, Is.GreaterThan(0f));
            movement.SetMoveInput(0f);
            At(.401f, duration);
            Assert.That(animation.IsShowingLadderVisual, Is.True);
            Assert.That(renderer.transform.localScale.x, Is.GreaterThan(0f));
        }

        [Test]
        public void LadderMiningLeft_Damage_KeepsLeftFlip_ThenResumesLadderMining()
        {
            movement.EnterLadder();
            animation.SetLadderMining(-1);
            Tick();
            Assert.That(State, Is.EqualTo("LadderMining"));
            Assert.That(renderer.transform.localScale.x, Is.LessThan(0f));
            float duration = Hit();
            Tick();
            Assert.That(State, Is.EqualTo("Damage"));
            Assert.That(renderer.transform.localScale.x, Is.LessThan(0f));
            At(.401f, duration);
            Assert.That(State, Is.EqualTo("LadderMining"));
            Assert.That(renderer.transform.localScale.x, Is.LessThan(0f));
        }

        [TestCase(-1f)]
        [TestCase(1f)]
        public void GroundDamage_FollowsMovementDirection(float direction)
        {
            Hit();
            movement.SetMoveInput(direction);
            Tick();
            Assert.That(State, Is.EqualTo("Damage"));
            Assert.That(Mathf.Sign(renderer.transform.localScale.x), Is.EqualTo(direction));
        }

        [Test]
        public void Knockout_ReplacesDamageImmediately()
        {
            Hit();
            Tick();
            Assert.That(State, Is.EqualTo("Damage"));
            Invoke(animation, "HandleFailureRequested", new RunFailureInputDto { cause = RunFailureCause.GasExposure });
            Assert.That(State, Is.EqualTo("Knockout"));
            Assert.That((float)Get(animation, "damageUntil"), Is.Zero);
        }

        private string State => (string)Get(animation, "currentState");

        // 체력 감소 이벤트로 다침을 시작하고 남은 다침 시간을 돌려준다.
        private float Hit()
        {
            health -= 10f;
            Invoke(animation, "HandleHealthChanged", new PlayerHealthReadModel(health, 100));
            return (float)Get(animation, "damageUntil") - Time.unscaledTime;
        }

        // 다침 시작 뒤 elapsed초가 지난 것처럼 시작·끝 시각을 당긴 뒤 한 틱 돈다.
        private void At(float elapsed, float duration)
        {
            float start = Time.unscaledTime - elapsed;
            if (State == "Damage") Set(animation, "stateStartedAt", start);
            Set(animation, "damageUntil", start + duration);
            Tick();
        }

        private void Tick()
        {
            Invoke(animation, "LateUpdate");
            Invoke(facing, "LateUpdate");
        }

        private static void Set(object target, string name, object value) => target.GetType().GetField(name, Private).SetValue(target, value);
        private static object Get(object target, string name) => target.GetType().GetField(name, Private).GetValue(target);
        private static void Invoke(object target, string name, params object[] args) => target.GetType().GetMethod(name, Private).Invoke(target, args);
    }
}

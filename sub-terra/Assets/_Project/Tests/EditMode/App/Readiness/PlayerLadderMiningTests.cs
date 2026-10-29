using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using SubTerra.Gameplay.Mining;
using SubTerra.Gameplay.Player;
using SubTerra.Shared;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace SubTerra.App.Tests.EditMode.Readiness
{
    public sealed class PlayerLadderMiningTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private GameObject player;
        private GameObject world;
        private Tile tile;
        private Tilemap map;
        private MiningSystem mining;
        private PlayerMovement movement;
        private PlayerMiningController controller;
        private PlayerAnimationController animation;
        private PlayerFacing facing;
        private SpriteRenderer renderer;
        private bool wasSettingsOpen;

        [SetUp]
        public void SetUp()
        {
            wasSettingsOpen = ControlPreferences.IsSettingsOpen;
            ControlPreferences.IsSettingsOpen = false;
            world = new GameObject("B174 test world", typeof(Grid));
            var tiles = new GameObject("Tiles", typeof(Tilemap));
            tiles.transform.SetParent(world.transform);
            map = tiles.GetComponent<Tilemap>();
            var resolver = world.AddComponent<MiningTileResolver>();
            mining = world.AddComponent<MiningSystem>();
            Set(mining, "foregroundTilemap", map);
            Set(mining, "tileResolver", resolver);
            tile = ScriptableObject.CreateInstance<Tile>();
            resolver.RegisterRuntime(tile, new MiningTileDto("tile.b174", "", 0, true, 1f, 5f, 0f, false));
            foreach (var offset in new[] { Vector3Int.left, Vector3Int.right, Vector3Int.up, Vector3Int.down,
                new Vector3Int(1, 1, 0), new Vector3Int(-1, -1, 0) }) map.SetTile(offset, tile);
            player = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Project/Prefabs/Gameplay/Player/Player.prefab"));
            player.transform.position = new Vector3(.5f, .5f, 0f);
            movement = player.GetComponent<PlayerMovement>();
            animation = player.GetComponentInChildren<PlayerAnimationController>();
            controller = player.GetComponent<PlayerMiningController>();
            if (controller == null) controller = player.AddComponent<PlayerMiningController>();
            facing = player.GetComponent<PlayerFacing>();
            renderer = animation.GetComponent<SpriteRenderer>();
            Invoke(movement, "Awake");
            player.GetComponent<Rigidbody2D>().position = new Vector2(.5f, .5f);
            Invoke(animation, "Awake");
            Invoke(controller, "Awake");
            Invoke(facing, "Awake");
            Set(controller, "miningSystem", mining);
            Invoke(controller, "BindAnimationEvents");
            Set(movement, "<IsGrounded>k__BackingField", true);
            movement.EnterLadder();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(player);
            Object.DestroyImmediate(world);
            Object.DestroyImmediate(tile);
            ControlPreferences.IsSettingsOpen = wasSettingsOpen;
        }

        [TestCase(0, 1, false)]
        [TestCase(0, -1, false)]
        [TestCase(-1, 0, true)]
        [TestCase(1, 0, true)]
        public void LadderKeyboard_StartsOnlyHorizontalTargets(int x, int y, bool expected)
        {
            StartTarget(new Vector2(x, y));
            Assert.That(mining.IsMining, Is.EqualTo(expected));
            Assert.That(mining.LastFailure, Is.EqualTo(MiningFailureReason.None));
        }

        [TestCase(0, 1, false)]
        [TestCase(0, -1, false)]
        [TestCase(1, 1, false)]
        [TestCase(-1, -1, false)]
        [TestCase(0, 0, false)]
        [TestCase(-1, 0, true)]
        [TestCase(1, 0, true)]
        public void LadderPointer_RejectsOtherRowsAndOwnCell(int x, int y, bool expected)
        {
            Set(controller, "pendingPointerTarget", true);
            Set(controller, "pendingWorldPoint", (Vector2)map.GetCellCenterWorld(new Vector3Int(x, y, 0)));
            StartTarget(Vector2.zero);
            Assert.That(mining.IsMining, Is.EqualTo(expected));
        }

        [Test]
        public void LadderEnter_DoesNotFallBackToDiagonalTarget()
        {
            map.SetTile(Vector3Int.right, null);
            StartTarget(Vector2.zero);
            Assert.That(mining.IsMining, Is.False);
        }

        [TestCase(1)]
        [TestCase(-1)]
        public void GroundKeyboard_PreservesVerticalMining(int y)
        {
            movement.ExitLadder();
            StartTarget(new Vector2(0, y));
            Assert.That(mining.IsMining, Is.True);
            Invoke(animation, "LateUpdate");
            Assert.That(State, Is.EqualTo("Mining"));
        }

        [TestCase(-1)]
        [TestCase(1)]
        public void Facing_UsesActiveCellAgainstPlayerCell_CompletionRestoresUnflippedLadder(int side)
        {
            movement.SetMoveInput(-side);
            movement.SetMoveInput(0f);
            StartTarget(new Vector2(side, 0));
            Invoke(animation, "LateUpdate");
            Invoke(facing, "LateUpdate");
            Assert.That(State, Is.EqualTo("LadderMining"));
            Assert.That(Mathf.Sign(renderer.transform.localScale.x), Is.EqualTo(side));
            Assert.That(movement.FacingDirection, Is.EqualTo(-side));
            mining.TickMining(10f);
            var sequence = (PlayerLadderMiningAnimationSequence)Get(animation, "ladderMiningSequence");
            Assert.That(sequence.FrameIndex, Is.EqualTo(16));
            sequence.Tick(8f / 60f);
            Invoke(animation, "LateUpdate");
            Invoke(facing, "LateUpdate");
            Assert.That(animation.IsShowingLadderVisual, Is.True);
            Assert.That(renderer.transform.localScale.x, Is.GreaterThan(0));
        }

        [TestCase("up")]
        [TestCase("down")]
        [TestCase("jump")]
        [TestCase("exit")]
        [TestCase("ground-to-ladder")]
        public void Traversal_CancelsMiningAndAnimationImmediately(string action)
        {
            if (action == "ground-to-ladder") movement.ExitLadder();
            StartTarget(Vector2.right);
            if (action == "up") movement.SetVerticalMoveInput(1f);
            else if (action == "down") movement.SetVerticalMoveInput(-1f);
            else if (action == "jump")
            {
                Set(movement, "jumpUsedUntilLand", true);
                Set(movement, "<IsGrounded>k__BackingField", false);
            }
            else if (action == "ground-to-ladder") movement.EnterLadder();
            else movement.ExitLadder();
            Invoke(controller, "Update");
            Invoke(animation, "LateUpdate");
            Assert.That(mining.IsMining, Is.False);
            Assert.That(State, Is.Not.EqualTo("LadderMining").And.Not.EqualTo("Mining"));
        }

        [Test]
        public void ReleasedButton_KeepsMining_SettingsCancellationReversesAndSameSideRestartsAtThirteen()
        {
            StartTarget(Vector2.left);
            var sequence = (PlayerLadderMiningAnimationSequence)Get(animation, "ladderMiningSequence");
            sequence.Tick(.6f);
            Invoke(controller, "Update");
            Assert.That(mining.IsMining, Is.True, "입력을 떼도 블록이 깨질 때까지 유지");
            ControlPreferences.IsSettingsOpen = true;
            Invoke(controller, "Update");
            Assert.That(mining.IsMining, Is.False);
            Assert.That(sequence.IsActive, Is.True, "일반 취소는 역재생");
            ControlPreferences.IsSettingsOpen = false;
            sequence.Tick(.1f);
            StartTarget(Vector2.left);
            Assert.That(sequence.FrameIndex, Is.EqualTo(12));
        }

        [Test]
        public void Damage_ResumesThirteen_KnockoutInterrupts()
        {
            StartTarget(Vector2.right);
            var sequence = (PlayerLadderMiningAnimationSequence)Get(animation, "ladderMiningSequence");
            Set(animation, "damageUntil", Time.unscaledTime + .35f);
            Invoke(animation, "LateUpdate");
            Assert.That(State, Is.EqualTo("Damage"));
            Set(animation, "damageUntil", 0f);
            Invoke(animation, "LateUpdate");
            Assert.That(State, Is.EqualTo("LadderMining"));
            Assert.That(sequence.FrameIndex, Is.EqualTo(12));
            Invoke(animation, "HandleFailureRequested", new RunFailureInputDto { cause = RunFailureCause.GasExposure });
            Assert.That(State, Is.EqualTo("Knockout"));
            Assert.That(sequence.IsActive, Is.False);
        }

        [Test]
        public void Prefab_Has24UniqueAlignedDrawingsAndSettings()
        {
            var fields = new SerializedObject(animation);
            var frames = fields.FindProperty("ladderMiningFrames");
            Assert.That(frames.arraySize, Is.EqualTo(24));
            var hashes = new HashSet<string>();
            for (int i = 0; i < frames.arraySize; i++)
            {
                var sprite = (Sprite)frames.GetArrayElementAtIndex(i).objectReferenceValue;
                Assert.That(sprite, Is.Not.Null);
                string path = AssetDatabase.GetAssetPath(sprite);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                Assert.That(importer.spritePixelsPerUnit, Is.EqualTo(256f));
                Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Bilinear));
                Assert.That(importer.mipmapEnabled, Is.False);
                Assert.That(importer.textureCompression, Is.EqualTo(TextureImporterCompression.Uncompressed));
                Assert.That(sprite.pivot, Is.EqualTo(new Vector2(128, 128)));
                byte[] bytes = File.ReadAllBytes(path);
                CollectionAssert.AreEqual(File.ReadAllBytes("../work_process/images/player-ladder-mining-24f-20261029/runtime-frames/ladder-mining-"
                    + (i+1).ToString("D2") + ".png"), bytes);
                hashes.Add(System.Convert.ToBase64String(bytes));
                var texture = new Texture2D(2, 2);
                try
                {
                    texture.LoadImage(bytes);
                    Assert.That(texture.width, Is.EqualTo(256));
                    Assert.That(texture.height, Is.EqualTo(256));
                    int footInset = 256;
                    var pixels = texture.GetPixels32();
                    for (int y = 0; y < 256; y++)
                    for (int x = 0; x < 256; x++)
                        if (pixels[y*256+x].a >= 32) footInset = Mathf.Min(footInset, y);
                    Assert.That(footInset, Is.InRange(14, 18));
                }
                finally { Object.DestroyImmediate(texture); }
            }
            Assert.That(hashes.Count, Is.EqualTo(24));
            var settings = fields.FindProperty("ladderMiningAnimation");
            Assert.That(settings.FindPropertyRelative("startFps").floatValue, Is.EqualTo(24f));
            Assert.That(settings.FindPropertyRelative("workFps").floatValue, Is.EqualTo(12f));
            Assert.That(settings.FindPropertyRelative("finishFps").floatValue, Is.EqualTo(60f));
        }

        private string State => (string)Get(animation, "currentState");
        private void StartTarget(Vector2 direction)
        {
            Set(controller, "pendingDirection", direction);
            Invoke(controller, "TryStartPendingTarget");
        }
        private static void Set(object target, string name, object value) => target.GetType().GetField(name, Private).SetValue(target, value);
        private static object Get(object target, string name) => target.GetType().GetField(name, Private).GetValue(target);
        private static void Invoke(object target, string name, params object[] args) => target.GetType().GetMethod(name, Private).Invoke(target, args);
    }
}

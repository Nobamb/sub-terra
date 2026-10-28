using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using SubTerra.App.UI.Guide;
using SubTerra.Gameplay.Player;
using SubTerra.Shared.Audio;
using UnityEditor;
using UnityEngine;

namespace SubTerra.App.Tests.EditMode.Readiness
{
    public sealed class PlayerWalkCycleTests
    {
        [Test]
        public void GuideWalk_UsesTheSameFortyFrames()
        {
            var skin = AssetDatabase.LoadAssetAtPath<GameGuideSkin>("Assets/_Project/Resources/UI/GameGuideSkin.asset");
            Assert.That(skin, Is.Not.Null);
            Assert.That(GuideDemoLibrary.PlayerWalkFrameCount, Is.EqualTo(PlayerAnimationController.WalkFrameCount));
            Assert.That(GuideDemoLibrary.PlayerWalkFrameRate, Is.EqualTo(PlayerAnimationController.WalkFrameRate));
            for (int i = 1; i <= 40; i++)
                Assert.That(skin.Get("player.walk." + i), Is.EqualTo(AssetDatabase.LoadAssetAtPath<Sprite>(
                    $"Assets/_Project/Art/Characters/Player/Frames/Walk/walk_{i:D2}.png")));
            for (int i = 41; i <= 47; i++)
            {
                Assert.That(skin.Get("player.walk." + i), Is.Null);
                Assert.That(File.Exists($"Assets/_Project/Art/Characters/Player/Frames/Walk/walk_{i:D2}.png"), Is.False);
            }
            foreach (var clip in GuideDemoLibrary.All)
            foreach (var key in clip.UsedSpriteKeys())
                if (key.StartsWith("player.walk.")) Assert.That(skin.Get(key), Is.Not.Null, key);
        }

        [Test]
        public void WalkCycle_PreservesFortyDrawingsAndOneSecondTiming()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Gameplay/Player/Player.prefab");
            var visual = Object.Instantiate(prefab.transform.Find("VisualRoot").gameObject);
            try
            {
                var controller = visual.GetComponent<PlayerAnimationController>();
                var renderer = visual.GetComponent<SpriteRenderer>();
                var fields = new SerializedObject(controller);
                var frames = fields.FindProperty("walkFrames");
                Assert.That(frames.arraySize, Is.EqualTo(40));
                Assert.That(PlayerAnimationController.WalkFrameRate, Is.EqualTo(40f));
                var hashes = new HashSet<string>();
                var play = typeof(PlayerAnimationController).GetMethod("Play", BindingFlags.Instance | BindingFlags.NonPublic);
                var start = typeof(PlayerAnimationController).GetField("stateStartedAt", BindingFlags.Instance | BindingFlags.NonPublic);
                play.Invoke(controller, new object[] { "Walk" });
                for (int i = 0; i < 40; i++)
                {
                    var sprite = (Sprite)frames.GetArrayElementAtIndex(i).objectReferenceValue;
                    Assert.That(sprite, Is.Not.Null);
                    string path = AssetDatabase.GetAssetPath(sprite);
                    CollectionAssert.AreEqual(File.ReadAllBytes(
                        $"../work_process/images/player-walk-40f-20261028/runtime-frames/walk-{i + 1:D2}.png"),
                        File.ReadAllBytes(path), "채택된 전신 원화와 런타임 일치 " + (i + 1));
                    using (var sha = System.Security.Cryptography.SHA256.Create())
                        hashes.Add(System.Convert.ToBase64String(sha.ComputeHash(File.ReadAllBytes(path))));
                    start.SetValue(controller, Time.unscaledTime - (i / PlayerAnimationController.WalkFrameRate + .001f));
                    play.Invoke(controller, new object[] { "Walk" });
                    Assert.That(renderer.sprite, Is.EqualTo(sprite), "frame " + (i + 1));
                }
                Assert.That(hashes.Count, Is.EqualTo(40), "서로 다른 원화 40장을 모두 사용한다.");
                float duration = 40f / PlayerAnimationController.WalkFrameRate;
                Assert.That(duration, Is.EqualTo(1f).Within(.001f));
                start.SetValue(controller, Time.unscaledTime - duration - .001f);
                play.Invoke(controller, new object[] { "Walk" });
                Assert.That(renderer.sprite, Is.EqualTo(frames.GetArrayElementAtIndex(0).objectReferenceValue));
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/_Project/Art/Characters/Player/Animations/PlayerWalk.anim");
                Assert.That(clip.frameRate, Is.EqualTo(PlayerAnimationController.WalkFrameRate));
                Assert.That(AnimationUtility.GetAnimationClipSettings(clip).stopTime, Is.EqualTo(duration).Within(.001f));
                Assert.That(AnimationUtility.GetAnimationClipSettings(clip).loopTime, Is.True);
                var keys = AnimationUtility.GetObjectReferenceCurve(clip, AnimationUtility.GetObjectReferenceCurveBindings(clip)[0]);
                Assert.That(keys.Length, Is.EqualTo(40));
                for (int i = 0; i < keys.Length; i++)
                {
                    Assert.That(keys[i].time, Is.EqualTo(i / 40f).Within(.0001f));
                    Assert.That(keys[i].value, Is.EqualTo(frames.GetArrayElementAtIndex(i).objectReferenceValue));
                }
                play.Invoke(controller, new object[] { "Idle" });
                Assert.That(renderer.sprite, Is.EqualTo(fields.FindProperty("idleFrames").GetArrayElementAtIndex(0).objectReferenceValue));
                play.Invoke(controller, new object[] { "Walk" });
                Assert.That(renderer.sprite, Is.EqualTo(frames.GetArrayElementAtIndex(0).objectReferenceValue), "다시 걸으면 첫 그림부터 시작");
            }
            finally { Object.DestroyImmediate(visual); }
        }

        [Test]
        public void WalkDrawings_KeepGroundLineAndCharacterHeight()
        {
            for (int i = 1; i <= 40; i++)
            {
                var texture = new Texture2D(2, 2);
                try
                {
                    texture.LoadImage(File.ReadAllBytes($"Assets/_Project/Art/Characters/Player/Frames/Walk/walk_{i:D2}.png"));
                    Assert.That(texture.width, Is.EqualTo(256));
                    Assert.That(texture.height, Is.EqualTo(256));
                    Color32[] pixels = texture.GetPixels32();
                    int bottom = 256, top = -1, left = 256, right = -1;
                    for (int y = 0; y < 256; y++)
                    for (int x = 0; x < 256; x++)
                    {
                        if (pixels[y * 256 + x].a < 32) continue;
                        bottom = Mathf.Min(bottom, y); top = Mathf.Max(top, y);
                        left = Mathf.Min(left, x); right = Mathf.Max(right, x);
                    }
                    Assert.That(bottom, Is.InRange(14, 18), "발 기준점 " + i);
                    Assert.That(top - bottom + 1, Is.InRange(218, 236), "캐릭터 높이 " + i);
                    Assert.That(left, Is.GreaterThan(4), "왼쪽 잘림 " + i);
                    Assert.That(right, Is.LessThan(251), "오른쪽 잘림 " + i);
                    var importer = (TextureImporter)AssetImporter.GetAtPath($"Assets/_Project/Art/Characters/Player/Frames/Walk/walk_{i:D2}.png");
                    Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite));
                    Assert.That(importer.spriteImportMode, Is.EqualTo(SpriteImportMode.Single));
                    Assert.That(importer.spritePixelsPerUnit, Is.EqualTo(256f));
                    Assert.That(importer.spritePivot, Is.EqualTo(new Vector2(.5f, .5f)));
                    Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Bilinear));
                    Assert.That(importer.mipmapEnabled, Is.False);
                    Assert.That(importer.textureCompression, Is.EqualTo(TextureImporterCompression.Uncompressed));
                }
                finally { Object.DestroyImmediate(texture); }
            }
        }

        [TestCase(1f)]
        [TestCase(-1f)]
        public void WalkCycle_BriefGroundContactLossKeepsPhase_RealJumpAndFallStillSwitch(float direction)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Gameplay/Player/Player.prefab");
            var player = Object.Instantiate(prefab);
            try
            {
                const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
                var movement = player.GetComponent<PlayerMovement>();
                var controller = player.GetComponentInChildren<PlayerAnimationController>();
                var renderer = controller.GetComponent<SpriteRenderer>();
                typeof(PlayerMovement).GetMethod("Awake", flags).Invoke(movement, null);
                typeof(PlayerAnimationController).GetMethod("Awake", flags).Invoke(controller, null);
                var grounded = typeof(PlayerMovement).GetField("<IsGrounded>k__BackingField", flags);
                var lateUpdate = typeof(PlayerAnimationController).GetMethod("LateUpdate", flags);
                var start = typeof(PlayerAnimationController).GetField("stateStartedAt", flags);
                var frames = new SerializedObject(controller);
                movement.SetMoveInput(direction);
                grounded.SetValue(movement, true);
                lateUpdate.Invoke(controller, null);
                start.SetValue(controller, Time.unscaledTime - .326f);
                lateUpdate.Invoke(controller, null);
                var phase = start.GetValue(controller);
                var walkSprite = renderer.sprite;

                // 실제 광산의 타일 경계에서 한 물리 틱 동안 접지가 빠지는 상황.
                grounded.SetValue(movement, false);
                lateUpdate.Invoke(controller, null);
                Assert.That(renderer.sprite, Is.EqualTo(walkSprite), "짧은 접지 누락에 Jump 첫 그림이 끼어들지 않는다");
                Assert.That(start.GetValue(controller), Is.EqualTo(phase), "걷기 타이머를 다시 시작하지 않는다");
                grounded.SetValue(movement, true);
                lateUpdate.Invoke(controller, null);
                Assert.That(start.GetValue(controller), Is.EqualTo(phase));

                grounded.SetValue(movement, false);
                typeof(PlayerAnimationController).GetField("lastGroundedAt", flags)
                    .SetValue(controller, Time.unscaledTime - .1f);
                lateUpdate.Invoke(controller, null);
                Assert.That(renderer.sprite, Is.EqualTo(frames.FindProperty("jumpFrames").GetArrayElementAtIndex(0).objectReferenceValue),
                    "발판에서 실제로 떨어지면 Jump로 전환한다");

                grounded.SetValue(movement, true);
                lateUpdate.Invoke(controller, null);
                grounded.SetValue(movement, false);
                typeof(PlayerMovement).GetField("jumpUsedUntilLand", flags).SetValue(movement, true);
                lateUpdate.Invoke(controller, null);
                Assert.That(renderer.sprite, Is.EqualTo(frames.FindProperty("jumpFrames").GetArrayElementAtIndex(0).objectReferenceValue),
                    "점프 입력으로 뜨면 유예 없이 바로 전환한다");

                grounded.SetValue(movement, true);
                lateUpdate.Invoke(controller, null);
                movement.SetMoveInput(0f);
                lateUpdate.Invoke(controller, null);
                Assert.That(renderer.sprite, Is.EqualTo(frames.FindProperty("idleFrames").GetArrayElementAtIndex(0).objectReferenceValue));
            }
            finally { Object.DestroyImmediate(player); }
        }

        [TestCase(1f)]
        [TestCase(-1f)]
        public void WalkFootsteps_PlayTwicePerCycleAtTheSameContactFrames(float direction)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Gameplay/Player/Player.prefab");
            var player = Object.Instantiate(prefab);
            int tick = -1;
            var contacts = new List<int>();
            var pitches = new List<float>();
            System.Action<SfxId, float> onSound = (id, pitch) =>
            {
                if (id != SfxId.Footstep) return;
                contacts.Add(tick);
                pitches.Add(pitch);
            };
            float previousTimeScale = Time.timeScale;
            try
            {
                Time.timeScale = 1f;
                var movement = player.GetComponent<PlayerMovement>();
                var visual = player.transform.Find("VisualRoot");
                var controller = visual.GetComponent<PlayerAnimationController>();
                var facing = player.GetComponent<PlayerFacing>();
                const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
                typeof(PlayerMovement).GetMethod("Awake", flags).Invoke(movement, null);
                typeof(PlayerAnimationController).GetMethod("Awake", flags).Invoke(controller, null);
                typeof(PlayerFacing).GetMethod("Awake", flags).Invoke(facing, null);
                typeof(PlayerMovement).GetField("<IsGrounded>k__BackingField", flags).SetValue(movement, true);
                movement.SetMoveInput(direction);
                player.GetComponent<Rigidbody2D>().linearVelocity = new Vector2(direction * 6f, 0f);
                typeof(PlayerFacing).GetMethod("LateUpdate", flags).Invoke(facing, null);
                Assert.That(Mathf.Sign(visual.localScale.x), Is.EqualTo(direction));
                var configured = new SerializedObject(controller).FindProperty("footstepFrames");
                Assert.That(configured.arraySize, Is.EqualTo(2));
                Assert.That(configured.GetArrayElementAtIndex(0).intValue, Is.EqualTo(1));
                Assert.That(configured.GetArrayElementAtIndex(1).intValue, Is.EqualTo(21));
                var play = typeof(PlayerAnimationController).GetMethod("Play", flags);
                var start = typeof(PlayerAnimationController).GetField("stateStartedAt", flags);
                play.Invoke(controller, new object[] { "Walk" });
                SfxRequests.PlayRequested += onSound;
                for (tick = 0; tick < 80; tick++)
                {
                    start.SetValue(controller, Time.unscaledTime - (tick / 40f + .001f));
                    play.Invoke(controller, new object[] { "Walk" });
                    play.Invoke(controller, new object[] { "Walk" });
                }
                CollectionAssert.AreEqual(new[] { 1, 21, 41, 61 }, contacts);
                CollectionAssert.AreEqual(new[] { 1f, .96f, 1f, .96f }, pitches);
            }
            finally
            {
                SfxRequests.PlayRequested -= onSound;
                Time.timeScale = previousTimeScale;
                Object.DestroyImmediate(player);
            }
        }
    }
}

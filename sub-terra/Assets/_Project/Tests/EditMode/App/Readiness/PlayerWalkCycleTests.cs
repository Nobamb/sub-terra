using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using SubTerra.App.UI.Guide;
using SubTerra.Gameplay.Player;
using UnityEditor;
using UnityEngine;

namespace SubTerra.App.Tests.EditMode.Readiness
{
    public sealed class PlayerWalkCycleTests
    {
        [Test]
        public void GuideWalk_UsesTheSameSixteenDrawings()
        {
            var skin = AssetDatabase.LoadAssetAtPath<GameGuideSkin>("Assets/_Project/Resources/UI/GameGuideSkin.asset");
            Assert.That(skin, Is.Not.Null);
            Assert.That(GuideDemoLibrary.PlayerWalkFrameCount, Is.EqualTo(PlayerAnimationController.WalkFrameCount));
            Assert.That(GuideDemoLibrary.PlayerWalkFrameRate, Is.EqualTo(PlayerAnimationController.WalkFrameRate));
            for (int i = 1; i <= 16; i++)
                Assert.That(skin.Get("player.walk." + i), Is.EqualTo(AssetDatabase.LoadAssetAtPath<Sprite>(
                    $"Assets/_Project/Art/Characters/Player/Frames/Walk/walk_{i:D2}.png")));
            Assert.That(skin.Get("player.walk.17"), Is.Null);
            foreach (var clip in GuideDemoLibrary.All)
            foreach (var key in clip.UsedSpriteKeys())
                if (key.StartsWith("player.walk.")) Assert.That(skin.Get(key), Is.Not.Null, key);
        }

        [Test]
        public void WalkCycle_UsesSixteenDistinctDrawingsAndLoopsInOneSecond()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Gameplay/Player/Player.prefab");
            var visual = Object.Instantiate(prefab.transform.Find("VisualRoot").gameObject);
            try
            {
                var controller = visual.GetComponent<PlayerAnimationController>();
                var renderer = visual.GetComponent<SpriteRenderer>();
                var fields = new SerializedObject(controller);
                var frames = fields.FindProperty("walkFrames");
                Assert.That(frames.arraySize, Is.EqualTo(16));
                Assert.That(PlayerAnimationController.WalkFrameRate, Is.EqualTo(16f));
                var hashes = new HashSet<string>();
                var play = typeof(PlayerAnimationController).GetMethod("Play", BindingFlags.Instance | BindingFlags.NonPublic);
                var start = typeof(PlayerAnimationController).GetField("stateStartedAt", BindingFlags.Instance | BindingFlags.NonPublic);
                play.Invoke(controller, new object[] { "Walk" });
                for (int i = 0; i < 16; i++)
                {
                    var sprite = (Sprite)frames.GetArrayElementAtIndex(i).objectReferenceValue;
                    Assert.That(sprite, Is.Not.Null);
                    string path = AssetDatabase.GetAssetPath(sprite);
                    CollectionAssert.AreEqual(File.ReadAllBytes(
                        $"../work_process/images/player-walk-whole-redraw-20261027/approved-one-second/walk-{i + 1:D2}.png"),
                        File.ReadAllBytes(path), "채택된 전신 원화와 런타임 일치 " + (i + 1));
                    using (var sha = System.Security.Cryptography.SHA256.Create())
                        hashes.Add(System.Convert.ToBase64String(sha.ComputeHash(File.ReadAllBytes(path))));
                    start.SetValue(controller, Time.unscaledTime - (i / PlayerAnimationController.WalkFrameRate + .001f));
                    play.Invoke(controller, new object[] { "Walk" });
                    Assert.That(renderer.sprite, Is.EqualTo(sprite), "frame " + (i + 1));
                }
                Assert.That(hashes.Count, Is.EqualTo(16), "새로 그린 열여섯 자세를 모두 사용한다.");
                float duration = 16f / PlayerAnimationController.WalkFrameRate;
                Assert.That(duration, Is.EqualTo(1f));
                start.SetValue(controller, Time.unscaledTime - duration - .001f);
                play.Invoke(controller, new object[] { "Walk" });
                Assert.That(renderer.sprite, Is.EqualTo(frames.GetArrayElementAtIndex(0).objectReferenceValue));
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/_Project/Art/Characters/Player/Animations/PlayerWalk.anim");
                Assert.That(clip.frameRate, Is.EqualTo(PlayerAnimationController.WalkFrameRate));
                Assert.That(AnimationUtility.GetAnimationClipSettings(clip).stopTime, Is.EqualTo(duration).Within(.001f));
                play.Invoke(controller, new object[] { "Idle" });
                Assert.That(renderer.sprite, Is.EqualTo(fields.FindProperty("idleFrames").GetArrayElementAtIndex(0).objectReferenceValue));
            }
            finally { Object.DestroyImmediate(visual); }
        }

        [Test]
        public void WalkDrawings_KeepGroundLineAndCharacterHeight()
        {
            for (int i = 1; i <= 16; i++)
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
                }
                finally { Object.DestroyImmediate(texture); }
            }
        }
    }
}

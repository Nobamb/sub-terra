using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using SubTerra.Gameplay.Player;
using UnityEditor;
using UnityEngine;

namespace SubTerra.App.Tests.EditMode.Readiness
{
    public sealed class PlayerWalkCycleTests
    {
        [Test]
        public void WalkCycle_UsesAllSixteenDistinctFramesAndLoopsAtOneSecond()
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
                var hashes = new HashSet<string>();
                var play = typeof(PlayerAnimationController).GetMethod("Play", BindingFlags.Instance | BindingFlags.NonPublic);
                var start = typeof(PlayerAnimationController).GetField("stateStartedAt", BindingFlags.Instance | BindingFlags.NonPublic);
                play.Invoke(controller, new object[] { "Walk" });
                for (int i = 0; i < 16; i++)
                {
                    var sprite = (Sprite)frames.GetArrayElementAtIndex(i).objectReferenceValue;
                    Assert.That(sprite, Is.Not.Null);
                    string path = AssetDatabase.GetAssetPath(sprite);
                    using (var sha = System.Security.Cryptography.SHA256.Create())
                        hashes.Add(System.Convert.ToBase64String(sha.ComputeHash(File.ReadAllBytes(path))));
                    start.SetValue(controller, Time.unscaledTime - (i / 16f + .001f));
                    play.Invoke(controller, new object[] { "Walk" });
                    Assert.That(renderer.sprite, Is.EqualTo(sprite), "frame " + (i + 1));
                }
                Assert.That(hashes.Count, Is.EqualTo(16), "중복 그림을 오래 유지하는 구성을 다시 넣지 않는다.");
                start.SetValue(controller, Time.unscaledTime - 1.001f);
                play.Invoke(controller, new object[] { "Walk" });
                Assert.That(renderer.sprite, Is.EqualTo(frames.GetArrayElementAtIndex(0).objectReferenceValue));
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/_Project/Art/Characters/Player/Animations/PlayerWalk.anim");
                Assert.That(clip.frameRate, Is.EqualTo(16));
                Assert.That(AnimationUtility.GetAnimationClipSettings(clip).stopTime, Is.EqualTo(1).Within(.001f));
            }
            finally { Object.DestroyImmediate(visual); }
        }
    }
}

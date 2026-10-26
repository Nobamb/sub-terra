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
        public void WalkBake_KneesKeepTheirWidthAndHipSeamStaysAttached()
        {
            System.Type baker = null;
            foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
                if ((baker = assembly.GetType("SubTerra.Editor.SmoothWalkCycleBaker")) != null) break;
            Assert.That(baker, Is.Not.Null);
            var poseType = baker.GetNestedType("LegPose", BindingFlags.NonPublic);
            var deform = poseType.GetMethod("Deform");
            foreach (var near in new[] { true, false })
            for (int i = 0; i < 16; i++)
            {
                float phase = i / 16f;
                float bob = 1.5f - 3.5f * Mathf.Cos(phase * 4 * Mathf.PI);
                var pose = System.Activator.CreateInstance(poseType, new object[] { near, phase, bob });
                var knee = new Vector2(near ? 119 : 150, 61);
                var left = (Vector2)deform.Invoke(pose, new object[] { knee - Vector2.right * 5f });
                var right = (Vector2)deform.Invoke(pose, new object[] { knee + Vector2.right * 5f });
                Assert.That(Vector2.Distance(left, right), Is.GreaterThan(9.5f),
                    $"무릎 접합부가 눌리면 안 된다: near={near}, frame={i + 1}");
                var hem = new Vector2(near ? 119 : 144, 87);
                Assert.That(Vector2.Distance((Vector2)deform.Invoke(pose, new object[] { hem }),
                    hem + Vector2.up * bob), Is.LessThan(.001f));
            }
        }

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

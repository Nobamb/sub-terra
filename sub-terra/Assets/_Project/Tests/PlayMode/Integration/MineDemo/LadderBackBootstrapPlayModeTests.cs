#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using SubTerra.App.Core;
using SubTerra.App.Save;
using SubTerra.Gameplay.Player;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SubTerra.App.Tests.PlayMode.MineDemo
{
    public sealed class LadderBackBootstrapPlayModeTests
    {
        private const string Frames = "Assets/_Project/Art/Characters/Player/Frames/LadderBack/";
        private PlayerMovement movement;
        private SpriteRenderer visual;
        private Sprite[] frames;

        [UnityTest]
        public IEnumerator Bootstrap_NewGame_ClimbUsesImportedBackFramesOnly()
        {
            // 실제 세이브를 건드리지 않도록 전용 실행 인자를 필수로 요구한다.
            var args = Environment.GetCommandLineArgs();
            var saveIndex = Array.IndexOf(args, "-subterra-save-root");
            if (saveIndex < 0 || saveIndex + 1 >= args.Length)
                Assert.Ignore("Run with -subterra-save-root pointing to an isolated test directory.");
            var testRoot = Path.GetFullPath(args[saveIndex + 1]);
            var tempRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "../Temp"));
            Assert.That(testRoot.StartsWith(tempRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase), Is.True);
            Assert.That(Directory.Exists(testRoot), Is.False, "Use a fresh save directory for each run.");

            GameBootstrapper.ResetInstanceForTests();
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/Bootstrap/Bootstrap.unity");
            yield return WaitFor(() => SceneManager.GetActiveScene().name == "MainMenu");
            yield return null;
            Assert.That(SaveRuntimeController.Instance, Is.Not.Null);
            Assert.That(SaveRuntimeController.Instance.StartNewGame(1), Is.True);
            yield return WaitFor(() => SceneManager.GetActiveScene().name == "SurfaceBase");
            yield return null;
            yield return null;
            Assert.That(SaveRuntimeController.Instance.TryStartExploration(out var reason), Is.True, reason);
            yield return WaitFor(() => SceneManager.GetActiveScene().name == "Mine_Demo_Integration");
            yield return WaitFor(() => SaveRuntimeController.Instance.IsUiReady);
            Debug.Log("[Ladder Bootstrap QA] Integration UI ready.");
            movement = UnityEngine.Object.FindFirstObjectByType<PlayerMovement>();
            Assert.That(movement, Is.Not.Null);
            var input = movement.GetComponent<PlayerController>();
            if (input != null) input.enabled = false;
            movement.SetCanMove(true);
            visual = movement.transform.Find("VisualRoot").GetComponent<SpriteRenderer>();
            frames = new Sprite[3];
            for (var i = 0; i < frames.Length; i++)
            {
                frames[i] = AssetDatabase.LoadAssetAtPath<Sprite>(Frames + "ladder_back_0" + (i + 1) + ".png");
                Assert.That(frames[i], Is.Not.Null);
                Assert.That(frames[i].texture.width, Is.EqualTo(1254));
                Assert.That(frames[i].pixelsPerUnit, Is.EqualTo(1254));
            }

            // 실제 5칸 시설 프리팹으로 연결과 물리 등반을 검증한다.
            var ladderPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Gameplay/Traversal/Ladder_Buildable.prefab");
            Assert.That(ladderPrefab, Is.Not.Null);
            var origin = movement.transform.position + Vector3.up * 4f;
            UnityEngine.Object.Instantiate(ladderPrefab, origin, Quaternion.identity);
            var body = movement.GetComponent<Rigidbody2D>();
            body.position = origin;
            body.linearVelocity = Vector2.zero;
            Physics2D.SyncTransforms();
            yield return new WaitForFixedUpdate();
            yield return null;
            Assert.That(movement.IsTouchingLadder, Is.True);
            Assert.That(visual.sprite, Is.SameAs(frames[0]));
            Debug.Log("[Ladder Bootstrap QA] Neutral imported frame selected.");
            var visualPosition = visual.transform.localPosition;
            yield return Travel(1f, "ascending");
            movement.SetVerticalMoveInput(0f);
            yield return new WaitForFixedUpdate();
            yield return null;
            Assert.That(visual.sprite, Is.SameAs(frames[0]));
            yield return Travel(-1f, "descending");
            Debug.Log("[Ladder Bootstrap QA] Ascending and descending selected all three frames.");
            Assert.That(visual.transform.localPosition, Is.EqualTo(visualPosition), "VisualRoot must not bob.");
            movement.SetVerticalMoveInput(0f);
            body.position += Vector2.right * 2f;
            Physics2D.SyncTransforms();
            yield return new WaitForFixedUpdate();
            yield return new WaitForSeconds(0.15f);
            Assert.That(movement.IsClimbing, Is.False);
            Assert.That(Array.IndexOf(frames, visual.sprite), Is.EqualTo(-1), "Normal sprite must be restored on exit.");
        }

        private IEnumerator Travel(float direction, string label)
        {
            var observed = new HashSet<Sprite>();
            var startY = movement.Position.y;
            movement.SetVerticalMoveInput(direction);
            var deadline = Time.realtimeSinceStartup + 4f;
            while (Mathf.Abs(movement.Position.y - startY) < 1.7f && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
                Assert.That(Array.IndexOf(frames, visual.sprite), Is.GreaterThanOrEqualTo(0), "Walk/parts must not appear during climb.");
                observed.Add(visual.sprite);
            }
            Assert.That(Mathf.Abs(movement.Position.y - startY), Is.GreaterThanOrEqualTo(1.7f));
            Assert.That(observed.Count, Is.EqualTo(3), "Both crossing poses and neutral must be displayed.");
        }

        private static IEnumerator WaitFor(Func<bool> condition)
        {
            var deadline = Time.realtimeSinceStartup + 20f;
            while (!condition() && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(condition(), Is.True, "Bootstrap scene flow timed out.");
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            GameBootstrapper.ResetInstanceForTests();
            if (SaveRuntimeController.Instance != null) UnityEngine.Object.Destroy(SaveRuntimeController.Instance.gameObject);
            yield return null;
        }
    }
}
#endif

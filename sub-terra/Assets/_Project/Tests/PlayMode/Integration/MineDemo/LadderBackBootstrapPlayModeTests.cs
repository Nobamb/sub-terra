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
            // 최신 main의 시작 브리핑은 물리를 일시 정지한다. 실제 닫기 경로로 해제한다.
            var briefing = UnityEngine.Object.FindAnyObjectByType<SubTerra.App.UI.Tutorial.StartBriefingPopupMotion>();
            if (briefing != null && briefing.isActiveAndEnabled)
            {
                var view = UnityEngine.Object.FindAnyObjectByType<SubTerra.App.UI.Tutorial.DemoObjectiveView>();
                Assert.That(view, Is.Not.Null);
                yield return WaitFor(() => briefing.IsShown);
                Assert.That(view.TryCloseGuidance(), Is.True);
                yield return WaitFor(() => Time.timeScale > 0f);
            }
            Assert.That(Time.timeScale, Is.GreaterThan(0f), "Gameplay must be resumed before physics checks.");
            movement = UnityEngine.Object.FindAnyObjectByType<PlayerMovement>();
            Assert.That(movement, Is.Not.Null);
            var input = movement.GetComponent<PlayerController>();
            if (input != null) input.enabled = false;
            movement.SetCanMove(true);
            visual = movement.transform.Find("VisualRoot").GetComponent<SpriteRenderer>();
            var configuredFrames = new SerializedObject(
                visual.GetComponent<PlayerAnimationController>()).FindProperty("ladderFrames");
            Assert.That(configuredFrames.arraySize, Is.EqualTo(5));
            frames = new Sprite[5];
            bool styled = AssetDatabase.GetAssetPath(configuredFrames.GetArrayElementAtIndex(0).objectReferenceValue)
                .EndsWith("ladder_back_neutral_v2.png");
            for (var i = 0; i < frames.Length; i++)
            {
                frames[i] = configuredFrames.GetArrayElementAtIndex(i).objectReferenceValue as Sprite;
                Assert.That(frames[i], Is.Not.Null);
                string framePath = AssetDatabase.GetAssetPath(frames[i]);
                if (i == 0)
                    Assert.That(framePath, Is.EqualTo(Frames + "ladder_back_neutral_v2.png")
                        .Or.EqualTo(Frames + "ladder_back_01.png"));
                else
                    Assert.That(framePath, Is.EqualTo(Frames + "ladder_back_" + (styled ? "style_" : "") + "0" + (i + 1) + ".png"));
                Assert.That(frames[i].texture.width, Is.EqualTo(1254));
                Assert.That(frames[i].pixelsPerUnit, Is.EqualTo(styled ? 1084f : 1254f));
            }

            // 실제 5칸 시설 프리팹으로 연결과 물리 등반을 검증한다.
            var ladderPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Gameplay/Traversal/Ladder_Buildable.prefab");
            Assert.That(ladderPrefab, Is.Not.Null);
            var origin = movement.transform.position + Vector3.up * 4f;
            var ladder = UnityEngine.Object.Instantiate(ladderPrefab, origin, Quaternion.identity);
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
            Debug.Log("[Ladder Bootstrap QA] Ascending and descending selected all five frames.");
            Assert.That(visual.transform.localPosition, Is.EqualTo(visualPosition), "VisualRoot must not bob.");
            movement.SetVerticalMoveInput(1f);
            yield return new WaitForSeconds(1.5f);
            var playerCollider = movement.GetComponent<Collider2D>();
            var ladderCollider = ladder.GetComponent<Collider2D>();
            Assert.That(playerCollider.bounds.max.y, Is.LessThanOrEqualTo(ladderCollider.bounds.max.y + 0.001f));
            Assert.That(body.linearVelocityY, Is.EqualTo(0f).Within(0.001f));
            Assert.That(movement.IsClimbing, Is.True);
            Assert.That(visual.sprite, Is.SameAs(frames[0]), "Top stop must settle to the neutral pose.");
            movement.SetVerticalMoveInput(0f);
            body.position += Vector2.right * 2f;
            Physics2D.SyncTransforms();
            yield return new WaitForFixedUpdate();
            yield return new WaitForSeconds(0.15f);
            Assert.That(movement.IsClimbing, Is.False);
            Assert.That(Array.IndexOf(frames, visual.sprite), Is.EqualTo(-1), "Normal sprite must be restored on exit.");

            // 실제 Trigger 위에서 낙하 진입: 입력 없이 지원 높이까지 자동 하강해야 한다.
            body.position = new Vector2(origin.x, ladderCollider.bounds.max.y + 0.2f);
            body.linearVelocity = Vector2.down * 5f;
            Physics2D.SyncTransforms();
            yield return WaitFor(() => movement.IsApproachingLadderFromAbove && body.linearVelocityY < 0f);
            yield return null;
            Assert.That(movement.IsApproachingLadderFromAbove, Is.True);
            Assert.That(body.linearVelocityY, Is.LessThan(0f));
            Assert.That(Array.IndexOf(frames, visual.sprite), Is.EqualTo(-1), "Unsupported entry must not grasp thin air.");
            yield return new WaitForSeconds(0.3f);
            Assert.That(movement.IsApproachingLadderFromAbove, Is.False);
            Assert.That(playerCollider.bounds.max.y, Is.EqualTo(ladderCollider.bounds.max.y).Within(0.001f));
            Assert.That(body.linearVelocityY, Is.EqualTo(0f).Within(0.001f));
            Assert.That(visual.sprite, Is.SameAs(frames[0]));
            movement.SetVerticalMoveInput(-1f);
            yield return new WaitForFixedUpdate();
            Assert.That(body.linearVelocityY, Is.LessThan(0f), "Supported entry must return control to descent input.");
        }

        private IEnumerator Travel(float direction, string label)
        {
            var observed = new HashSet<Sprite>();
            var startY = movement.Position.y;
            movement.SetVerticalMoveInput(direction);
            var deadline = Time.realtimeSinceStartup + 4f;
            while (Mathf.Abs(movement.Position.y - startY) < 2.1f && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
                Assert.That(Array.IndexOf(frames, visual.sprite), Is.GreaterThanOrEqualTo(0), "Walk/parts must not appear during climb.");
                observed.Add(visual.sprite);
            }
            Assert.That(Mathf.Abs(movement.Position.y - startY), Is.GreaterThanOrEqualTo(2.1f), label);
            Assert.That(observed.Count, Is.EqualTo(5), "Both intermediate/full poses and neutral must be displayed.");
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

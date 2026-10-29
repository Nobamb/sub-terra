#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using SubTerra.App.Core;
using SubTerra.App.Save;
using SubTerra.App.Tests.PlayMode;
using SubTerra.App.Tutorial;
using SubTerra.App.UI.RunFailure;
using SubTerra.App.UI.Guide;
using SubTerra.App.UI.ControlTerminal;
using SubTerra.Gameplay.Mining;
using SubTerra.Gameplay.Player;
using SubTerra.Shared;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace SubTerra.App.Tests.PlayMode.MineDemo
{
    public sealed class PlayerRedrawnActionPlayModeTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private const string Evidence = "Temp/b172-play-evidence/";
        private UiTestEnvironment environment;

        private sealed class MemoryFileSystem : ISaveFileSystem
        {
            private readonly Dictionary<string, string> files = new Dictionary<string, string>();
            public bool FileExists(string path) => files.ContainsKey(path);
            public string ReadAllText(string path) => files[path];
            public void WriteAllText(string path, string content) => files[path] = content;
            public void CreateDirectory(string path) { }
            public void DeleteFile(string path) => files.Remove(path);
            public void MoveFile(string source, string destination)
            {
                files[destination] = files[source];
                files.Remove(source);
            }
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.timeScale = 1f;
            if (environment != null) environment.Dispose();
            environment = null;
            var clean = SceneManager.CreateScene("B172Cleanup-" + Guid.NewGuid().ToString("N"));
            SceneManager.SetActiveScene(clean);
            for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
                if (SceneManager.GetSceneAt(i) != clean && SceneManager.GetSceneAt(i).isLoaded)
                    yield return SceneManager.UnloadSceneAsync(SceneManager.GetSceneAt(i));
            yield return null;
        }

        [UnityTest]
        public IEnumerator Bootstrap_MiningCompletionGasBreathingRescueAndLadderFacing()
        {
            environment = new UiTestEnvironment();
            GameBootstrapper.ResetInstanceForTests();
            yield return SceneManager.LoadSceneAsync(SceneNames.Bootstrap);
            yield return UiTestWait.Until(() => SaveRuntimeController.Instance != null
                && SaveRuntimeController.Instance.InventoryService != null
                && SceneManager.GetActiveScene().name == SceneNames.MainMenu, "B172 Bootstrap", 20f);
            var runtime = SaveRuntimeController.Instance;
            // 실제 Bootstrap 흐름을 사용하되 테스트 저장은 메모리에만 쓴다.
            var fs = new MemoryFileSystem();
            var paths = new SavePathPolicy(Path.GetFullPath("Temp/b172-memory"));
            var mapper = new SaveDataMapper(new SystemSaveClock());
            var json = new SaveJsonCodec(new SaveMigrationService());
            typeof(SaveRuntimeController).GetField("saveService", Private)
                .SetValue(runtime, new SaveService(fs, paths, mapper, json));
            typeof(SaveRuntimeController).GetField("loadService", Private)
                .SetValue(runtime, new LoadService(fs, paths, mapper, json));
            Assert.That(runtime.StartNewGame(1), Is.True);
            yield return UiTestWait.Until(() => SceneManager.GetActiveScene().name == SceneNames.SurfaceBase
                && runtime.IsUiReady, "B172 new game", 20f);
            GameBootstrapper.Instance.State.SetDemoProgress(DemoObjectiveIds.MineCopper, 1, false);
            GameBootstrapper.Instance.State.SetQuestRewardSettlement(string.Empty, 1);
            Assert.That(runtime.TryStartExploration(out var reason), Is.True, reason);
            yield return UiTestWait.Until(() => SceneManager.GetActiveScene().name == SceneNames.Integration
                && runtime.IsUiReady, "B172 exploration", 20f);
            yield return environment.Resolution.Set(960, 540);
            environment.BindInput();
            yield return UiTestEnvironment.DismissBriefing();

            var movement = Object.FindAnyObjectByType<PlayerMovement>();
            var animation = movement.GetComponentInChildren<PlayerAnimationController>();
            var renderer = animation.GetComponent<SpriteRenderer>();
            var input = movement.GetComponent<PlayerController>();
            if (input != null) input.enabled = false;
            var body = movement.GetComponent<Rigidbody2D>();
            body.simulated = false;
            movement.enabled = false;
            movement.SetMoveInput(0f);
            movement.SetVerticalMoveInput(0f);
            typeof(PlayerMovement).GetField("<IsGrounded>k__BackingField", Private).SetValue(movement, true);
            var fields = new SerializedObject(animation);
            Assert.That(fields.FindProperty("miningFrames").arraySize, Is.EqualTo(24));
            Assert.That(fields.FindProperty("knockoutFrames").arraySize, Is.EqualTo(48));

            var miningController = movement.GetComponent<PlayerMiningController>();
            var mining = (MiningSystem)typeof(PlayerMiningController).GetField("miningSystem", Private).GetValue(miningController);
            bool started = false;
            var spawnCell = mining.ForegroundTilemap.WorldToCell(movement.Position);
            foreach (var cell in mining.ForegroundTilemap.cellBounds.allPositionsWithin)
            {
                if (Mathf.Abs(cell.x - spawnCell.x) > 6 || Mathf.Abs(cell.y - spawnCell.y) > 6) continue;
                if (!mining.ForegroundTilemap.HasTile(cell) || !mining.TryStartMining(cell)) continue;
                if (mining.EffectiveDuration > 3f) { mining.CancelMining(); continue; }
                Vector2 origin = (Vector2)mining.ForegroundTilemap.GetCellCenterWorld(cell) + Vector2.left;
                body.position = origin;
                movement.transform.position = origin;
                var worldCamera = Camera.main;
                var follow = worldCamera.GetComponent<PlayerCameraFollow>();
                if (follow != null) follow.enabled = false;
                worldCamera.orthographicSize = 1.5f;
                worldCamera.transform.position = new Vector3(origin.x, origin.y + 0.1f, -10f);
                started = true;
                break;
            }
            Assert.That(started, Is.True, "실제 광산에서 채굴 가능한 블록");
            int completionFrame = -1;
            Action<MiningProgressState> observeCompletion = progress =>
            {
                if (progress.Phase == MiningPhase.Completed)
                    completionFrame = ((PlayerMiningAnimationSequence)typeof(PlayerAnimationController)
                        .GetField("miningSequence", Private).GetValue(animation)).FrameIndex;
            };
            mining.ProgressChanged += observeCompletion;
            var seen = new HashSet<Sprite>();
            var captured = new HashSet<int>();
            float deadline = Time.realtimeSinceStartup + mining.EffectiveDuration + 2f;
            while (Time.realtimeSinceStartup < deadline)
            {
                seen.Add(renderer.sprite);
                foreach (int index in new[] { 0, 6, 16, 23 })
                    if (!captured.Contains(index) && renderer.sprite == fields.FindProperty("miningFrames")
                        .GetArrayElementAtIndex(index).objectReferenceValue)
                    {
                        captured.Add(index);
                        yield return UiTestWait.Capture(Evidence + "mining-" + (index + 1).ToString("D2") + ".png");
                    }
                yield return null;
            }
            mining.ProgressChanged -= observeCompletion;
            Assert.That(mining.IsMining, Is.False);
            Assert.That(completionFrame, Is.EqualTo(14), "실제 파괴 이벤트 안에서 대기 없이 드릴 회수 시작");
            Assert.That(seen.Contains((Sprite)fields.FindProperty("miningFrames").GetArrayElementAtIndex(23).objectReferenceValue), Is.True,
                "SetMining(false) 이후에도 24번까지 마무리");
            yield return UiTestWait.Capture(Evidence + "mine-finished.png");

            var survival = movement.GetComponent<PlayerSurvivalController>();
            if (survival == null) survival = Object.FindAnyObjectByType<PlayerSurvivalController>();
            animation.BindSurvival(survival);
            survival.RestoreAfterRescue();
            Assert.That(survival.ApplyGasFailure(new GasExposureFailureInputDto
            {
                gasZoneId = "b172-animation-check", severity = GasExposureFailureSeverity.RescueRequired
            }), Is.True);
            // 팝업이 원화를 가리지 않도록 캡처에서만 숨긴다. 생존 상태는 그대로 검사한다.
            yield return new WaitForSecondsRealtime(2.05f);
            var failureView = Object.FindAnyObjectByType<RunFailurePanelView>();
            if (failureView != null) failureView.Hide();
            var breathing = new HashSet<Sprite>();
            deadline = Time.realtimeSinceStartup + 1.05f;
            while (Time.realtimeSinceStartup < deadline)
            {
                breathing.Add(renderer.sprite);
                yield return null;
            }
            Assert.That(breathing.Count, Is.GreaterThan(1), "가스 쓰러지기 뒤 숨쉬기 반복");
            foreach (var sprite in breathing)
            {
                bool inBreathing = false;
                for (int i = 36; i < 48; i++)
                    inBreathing |= sprite == fields.FindProperty("knockoutFrames").GetArrayElementAtIndex(i).objectReferenceValue;
                Assert.That(inBreathing, Is.True);
            }
            yield return UiTestWait.Capture(Evidence + "gas-breathing.png");
            survival.RestoreAfterRescue();
            yield return null;
            Assert.That(renderer.sprite, Is.EqualTo(fields.FindProperty("idleFrames").GetArrayElementAtIndex(0).objectReferenceValue));

            movement.EnterLadder();
            movement.SetMoveInput(-1f);
            yield return null;
            yield return null;
            Assert.That(animation.IsShowingLadderVisual, Is.True);
            Assert.That(renderer.transform.localScale.x, Is.GreaterThan(0f));
            Assert.That(movement.FacingDirection, Is.LessThan(0f));
            yield return UiTestWait.Capture(Evidence + "ladder-facing-left.png");
            movement.ExitLadder();
            yield return new WaitForSecondsRealtime(0.15f);
            Assert.That(animation.IsShowingLadderVisual, Is.False);
            Assert.That(renderer.transform.localScale.x, Is.LessThan(0f));

            var canvasObject = new GameObject("B172 demo capture", typeof(RectTransform), typeof(Canvas));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32767;
            var skin = Resources.Load<GameGuideSkin>(GameGuideSkin.ResourcePath);
            var font = Object.FindAnyObjectByType<TMPro.TMP_Text>().font;
            var stage = (GameGuideStageView)typeof(GameGuideStageView).GetMethod("Create", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { canvasObject.transform, "Stage", 0f, 0f, 960f, 360f, font, new GuideSprites(skin, null) });
            Assert.That(GuideDemoLibrary.TryGet("mine", out var guideMine), Is.True);
            Assert.That(GuideDemoLibrary.TryGet("ladder", out var guideLadder), Is.True);
            foreach (var clip in new[] { guideMine, guideLadder,
                ControlTerminalDemoClips.Get(ControlKeyFunction.MineRight), ControlTerminalDemoClips.Get(ControlKeyFunction.MoveUp) })
            {
                stage.Bind(clip, true);
                stage.Show(1.4f);
                yield return null;
                yield return UiTestWait.Capture(Evidence + "demo-" + clip.Id.Replace('.', '-') + ".png");
            }
            Object.Destroy(canvasObject);
        }
    }
}
#endif

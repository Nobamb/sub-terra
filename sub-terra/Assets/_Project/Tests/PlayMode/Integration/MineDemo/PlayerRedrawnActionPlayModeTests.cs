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
        public IEnumerator Bootstrap_LadderMiningDirectionsReleaseCompletionAndCancellation()
        {
            environment = new UiTestEnvironment();
            GameBootstrapper.ResetInstanceForTests();
            yield return SceneManager.LoadSceneAsync(SceneNames.Bootstrap);
            yield return UiTestWait.Until(() => SaveRuntimeController.Instance != null
                && SaveRuntimeController.Instance.InventoryService != null
                && SceneManager.GetActiveScene().name == SceneNames.MainMenu, "B174 Bootstrap", 20f);
            var runtime = SaveRuntimeController.Instance;
            var fs = new MemoryFileSystem();
            var paths = new SavePathPolicy(Path.GetFullPath("Temp/b174-memory"));
            var mapper = new SaveDataMapper(new SystemSaveClock());
            var json = new SaveJsonCodec(new SaveMigrationService());
            Set(runtime, "saveService", new SaveService(fs, paths, mapper, json));
            Set(runtime, "loadService", new LoadService(fs, paths, mapper, json));
            Assert.That(runtime.StartNewGame(1), Is.True);
            yield return UiTestWait.Until(() => SceneManager.GetActiveScene().name == SceneNames.SurfaceBase
                && runtime.IsUiReady, "B174 new game", 20f);
            GameBootstrapper.Instance.State.SetDemoProgress(DemoObjectiveIds.MineCopper, 1, false);
            GameBootstrapper.Instance.State.SetQuestRewardSettlement(string.Empty, 1);
            Assert.That(runtime.TryStartExploration(out var reason), Is.True, reason);
            yield return UiTestWait.Until(() => SceneManager.GetActiveScene().name == SceneNames.Integration
                && runtime.IsUiReady, "B174 exploration", 20f);
            yield return environment.Resolution.Set(960, 540);
            environment.BindInput();
            yield return UiTestEnvironment.DismissBriefing();
            yield return UiTestWait.FocusGameView();

            var movement = Object.FindAnyObjectByType<PlayerMovement>();
            var animation = movement.GetComponentInChildren<PlayerAnimationController>();
            var renderer = animation.GetComponent<SpriteRenderer>();
            var controller = movement.GetComponent<PlayerMiningController>();
            var mining = (MiningSystem)Get(controller, "miningSystem");
            var map = mining.ForegroundTilemap;
            var resolver = (MiningTileResolver)Get(mining, "tileResolver");
            var input = movement.GetComponent<PlayerController>();
            input.enabled = false;
            var body = movement.GetComponent<Rigidbody2D>();
            body.simulated = false;
            movement.enabled = false;
            movement.SetMoveInput(0f);
            movement.SetVerticalMoveInput(0f);
            var playerCell = map.WorldToCell(movement.Position) + new Vector3Int(5, -6, 0);
            var origin = map.GetCellCenterWorld(playerCell);
            Sprite rockSprite = null;
            foreach (var cell in map.cellBounds.allPositionsWithin)
                if (map.GetSprite(cell) != null) { rockSprite = map.GetSprite(cell); break; }
            var tile = ScriptableObject.CreateInstance<UnityEngine.Tilemaps.Tile>();
            tile.sprite = rockSprite;
            resolver.RegisterRuntime(tile, new MiningTileDto("tile.b174.visual", "", 0, true, 1f, 2f, 0f, false));
            for (int x = -2; x <= 2; x++)
            for (int y = -2; y <= 2; y++) map.SetTile(playerCell + new Vector3Int(x, y, 0), null);
            body.position = origin;
            movement.transform.position = origin;
            Set(movement, "<IsGrounded>k__BackingField", false);
            var ladderPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Gameplay/Traversal/Ladder.prefab");
            LadderZone ladder = null;
            for (int i = -1; i <= 1; i++)
            {
                var segment = Object.Instantiate(ladderPrefab, origin + Vector3.up * i, Quaternion.identity);
                if (i == 0) ladder = segment.GetComponent<LadderZone>();
            }
            movement.EnterLadder(ladder);
            var camera = Camera.main;
            var follow = camera.GetComponent<PlayerCameraFollow>();
            if (follow != null) follow.enabled = false;
            camera.orthographicSize = 2.3f;
            camera.transform.position = new Vector3(origin.x, origin.y + .1f, -10f);
            yield return null;
            yield return null;
            Assert.That(animation.IsShowingLadderVisual, Is.True);
            yield return UiTestWait.Capture("Temp/b174-play-evidence/ladder-idle.png");
            var fields = new SerializedObject(animation);
            var frames = fields.FindProperty("ladderMiningFrames");
            Assert.That(frames.arraySize, Is.EqualTo(24));
            int initialFrame = Time.frameCount;
            foreach (int side in new[] { 1, -1 })
            {
                map.SetTile(playerCell + new Vector3Int(side, 0, 0), tile);
                movement.SetMoveInput(-side);
                movement.SetMoveInput(0f);
                Set(controller, "pendingDirection", new Vector2(side, 0));
                Set(controller, "pendingPointerTarget", false);
                Invoke(controller, "TryStartPendingTarget");
                Assert.That(mining.IsMining, Is.True);
                var sequence = (PlayerLadderMiningAnimationSequence)Get(animation, "ladderMiningSequence");
                Assert.That(sequence.FrameIndex, Is.Zero);
                yield return null;
                yield return null;
                Assert.That(Mathf.Sign(renderer.transform.localScale.x), Is.EqualTo(side), "ActiveCell로 방향 결정");
                yield return UiTestWait.Capture("Temp/b174-play-evidence/" + side + "-start.png");
                yield return UiTestWait.Until(() => sequence.FrameIndex >= 12 && sequence.FrameIndex <= 15,
                    "B174 work loop", 3f);
                Assert.That(mining.IsMining, Is.True, "버튼을 누르지 않아도 계속 채굴");
                var seen = new HashSet<int>();
                float deadline = Time.realtimeSinceStartup + .6f;
                while (Time.realtimeSinceStartup < deadline)
                {
                    seen.Add(sequence.FrameIndex);
                    yield return null;
                }
                CollectionAssert.AreEquivalent(new[] { 12, 13, 14, 15 }, seen);
                yield return UiTestWait.Capture("Temp/b174-play-evidence/" + side + "-work.png");
                int completionFrame = -1;
                Action<MiningProgressState> observe = progress =>
                {
                    if (progress.Phase == MiningPhase.Completed) completionFrame = sequence.FrameIndex;
                };
                mining.ProgressChanged += observe;
                mining.TickMining(10f);
                mining.ProgressChanged -= observe;
                Assert.That(completionFrame, Is.EqualTo(16), "완료 이벤트 안에서 즉시 17번");
                yield return UiTestWait.Capture("Temp/b174-play-evidence/" + side + "-finish.png");
                yield return UiTestWait.Until(() => animation.IsShowingLadderVisual, "B174 ladder return", 2f);
                Assert.That(renderer.transform.localScale.x, Is.GreaterThan(0f));
            }
            Assert.That(Time.frameCount - initialFrame, Is.GreaterThan(20), "실제 프레임 진행");
            foreach (var direction in new[] { Vector2.up, Vector2.down })
            {
                Set(controller, "pendingDirection", direction);
                Invoke(controller, "TryStartPendingTarget");
                Assert.That(mining.IsMining, Is.False);
            }
            foreach (var action in new[] { "up", "down", "exit", "jump", "settings" })
            {
                movement.EnterLadder(ladder);
                map.SetTile(playerCell + Vector3Int.right, tile);
                Set(controller, "pendingDirection", Vector2.right);
                Invoke(controller, "TryStartPendingTarget");
                yield return null;
                if (action == "up") movement.SetVerticalMoveInput(1f);
                else if (action == "down") movement.SetVerticalMoveInput(-1f);
                else if (action == "exit") movement.ExitLadder();
                else if (action == "jump") Set(movement, "jumpUsedUntilLand", true);
                else ControlPreferences.IsSettingsOpen = true;
                yield return null;
                yield return null;
                Assert.That(mining.IsMining, Is.False, action);
                var sequence = (PlayerLadderMiningAnimationSequence)Get(animation, "ladderMiningSequence");
                Assert.That(sequence.IsActive, Is.EqualTo(action == "settings"), action);
                movement.SetVerticalMoveInput(0f);
                Set(movement, "jumpUsedUntilLand", false);
                ControlPreferences.IsSettingsOpen = false;
                yield return new WaitForSecondsRealtime(.35f);
            }
            movement.EnterLadder(ladder);
            map.SetTile(playerCell + Vector3Int.right, tile);
            Set(controller, "pendingDirection", Vector2.right);
            Invoke(controller, "TryStartPendingTarget");
            var survival = movement.GetComponent<PlayerSurvivalController>();
            if (survival == null) survival = Object.FindAnyObjectByType<PlayerSurvivalController>();
            animation.BindSurvival(survival);
            Assert.That(survival.ApplyGasFailure(new GasExposureFailureInputDto
            {
                gasZoneId = "b174-animation-check", severity = GasExposureFailureSeverity.RescueRequired
            }), Is.True);
            yield return null;
            Assert.That(Get(animation, "currentState"), Is.EqualTo("Knockout"));
            Object.Destroy(tile);
        }

        private static object Get(object target, string name) => target.GetType().GetField(name, Private).GetValue(target);
        private static void Set(object target, string name, object value) => target.GetType().GetField(name, Private).SetValue(target, value);
        private static void Invoke(object target, string name) => target.GetType().GetMethod(name, Private).Invoke(target, null);

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

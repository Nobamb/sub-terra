using System;
using System.IO;
using System.Text;
using SubTerra.Gameplay.Player;
using SubTerra.App.Core;
using SubTerra.App.Save;
using SubTerra.App.Integration;
using SubTerra.App.UI.Tutorial;
using SubTerra.App.Tutorial;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace SubTerra.Editor
{
    // Bootstrap Play 안의 임시 평지에서 실제 Player prefab 이동/렌더를 확인한다.
    // 새 게임/슬롯/세이브 API는 호출하지 않는다.
    [InitializeOnLoad]
    public static class FemaleLocomotionReviewSmoke
    {
        private const string Key = "SubTerra.FemaleLocomotionSmoke";
        private static PlayerMovement movement;
        private static SpriteRenderer renderer;
        private static FemaleLocomotionReviewWindow review;
        private static Camera camera;
        private static GameObject root;
        private static RenderTexture renderTexture;
        private static Texture2D floorTexture;
        private static Sprite floorSprite;
        private static Material material;
        private static readonly StringBuilder Trace = new();
        private static float startTime;
        private static int capture;
        private static int movieFrame;
        private static float lastMovieTime = -1;
        private static string output;
        private static double deadline;
        private static bool ready;
        private static bool corrected;
        private static float phaseStartedAt;
        private static bool mineRequested;
        private static int mineStage;
        private static PlayerController mineInput;
        private static double mineCaptureAt;

        static FemaleLocomotionReviewSmoke()
        {
            if (SessionState.GetBool(Key, false))
            {
                deadline = EditorApplication.timeSinceStartup + 90;
                EditorApplication.update += Tick;
            }
        }

        public static void Run() => Begin(false);
        public static void RunWithMine() => Begin(true);
        public static void RunMineOnly() => Begin(true, true);

        private static void Begin(bool includeMine, bool onlyMine = false)
        {
            output = Path.GetFullPath(Path.Combine(Application.dataPath, "../../work_process/issue266-distance-ground-20261104" + (onlyMine ? "-mine-retry" : "")));
            Directory.CreateDirectory(output);
            if (includeMine)
            {
                string saveRoot = ""; var args = Environment.GetCommandLineArgs();
                for (int i = 0; i < args.Length - 1; i++) if (args[i] == "-subterra-save-root") saveRoot = Path.GetFullPath(args[i + 1]);
                if (!saveRoot.StartsWith(output + Path.DirectorySeparatorChar + "isolated-save-", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Mine review requires a dedicated isolated save root inside its output folder.");
            }
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/Bootstrap/Bootstrap.unity");
            SessionState.SetString(Key + ".output", output);
            SessionState.SetBool(Key, true);
            SessionState.SetBool(Key + ".mine", includeMine);
            SessionState.SetBool(Key + ".mineOnly", onlyMine);
            ready = false; corrected = false; mineRequested = false; mineStage = 0;
            capture = 0; movieFrame = 0; lastMovieTime = -1;
            deadline = EditorApplication.timeSinceStartup + 90;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            EditorApplication.isPlaying = true;
        }

        private static void Tick()
        {
            try
            {
                if (!SessionState.GetBool(Key, false)) return;
                output = SessionState.GetString(Key + ".output", "");
                if (EditorApplication.timeSinceStartup > deadline)
                    throw new TimeoutException("Play review timed out; mineStage=" + mineStage + "; scene=" + SceneManager.GetActiveScene().name
                        + "; timeScale=" + Time.timeScale + "; uiReady=" + (SaveRuntimeController.Instance != null && SaveRuntimeController.Instance.IsUiReady));
                if (!EditorApplication.isPlaying || EditorApplication.isCompiling) return;
                if (mineRequested) { TickMine(); return; }
                if (!ready)
                {
                    if (SceneManager.GetActiveScene().name != SceneNames.MainMenu) return;
                    if (SessionState.GetBool(Key + ".mineOnly", false))
                    {
                        Trace.Clear(); Trace.AppendLine("time,mode,playerX,speed,grounded,pose,soleX,soleY,groundY,offsetY,maleIdleFootY,travel,image");
                        renderTexture = new RenderTexture(640, 384, 24);
                        BeginMine(); return;
                    }
                    CreateFixture(); ready = true; return;
                }
                float elapsed = Time.time - startTime;
                float phaseTime = Time.time - phaseStartedAt;
                string mode = corrected ? "distance-ground" : "time-original";
                float input = phaseTime < 0.7f ? 0 : phaseTime < 2.2f ? 1 : phaseTime < 2.6f ? 0 : phaseTime < 4.1f ? -1 : 0;
                movement.SetMoveInput(input);
                renderer.flipX = input < 0 || (input == 0 && movement.FacingDirection < 0);
                if (elapsed - lastMovieTime >= 0.05f)
                {
                    string file = $"movie-{movieFrame++:D3}.png";
                    Capture(file);
                    var sole = review.ContactWorldPoint;
                    Trace.AppendLine(FormattableString.Invariant($"{elapsed:F4},{mode},{movement.Position.x:F5},{movement.HorizontalSpeed:F5},{movement.IsGrounded},{review.ContactPose},{sole.x:F5},{sole.y:F5},{review.GroundY:F5},{review.GroundOffset:F5},{review.MaleReferenceFootY:F5},{review.TravelDistance:F5},{file}"));
                    lastMovieTime = elapsed;
                }
                float[] moments = { 0.6f, 0.75f, 0.84f, 1.4f, 2.5f, 2.65f, 2.74f, 3.5f, 4.5f };
                if (capture < moments.Length && phaseTime >= moments[capture])
                {
                    Capture($"{mode}-{capture++:D2}.png");
                }
                if (phaseTime >= 4.6f)
                {
                    if (capture != moments.Length) throw new Exception("Incomplete capture sequence");
                    if (!corrected)
                    {
                        corrected = true; phaseStartedAt = Time.time; capture = 0;
                        var body = movement.GetComponent<Rigidbody2D>();
                        body.position = new Vector2(1000, 1000); body.linearVelocity = Vector2.zero;
                        movement.SetMoveInput(0); renderer.flipX = false;
                        review.ConfigureReview(true, true);
                        return;
                    }
                    review.StopReview();
                    if (review.IsReviewActive) throw new Exception("Trial did not stop");
                    File.WriteAllText(Path.Combine(output, "movement-trace.csv"), Trace.ToString());
                    if (SessionState.GetBool(Key + ".mine", false)) { BeginMine(); return; }
                    Finish(true, "Before/after fixture movement and sprite/transform restoration passed. Mine scene was not requested.");
                }
            }
            catch (Exception e) { Finish(false, e.ToString()); }
        }

        private static void CreateFixture()
        {
            root = new GameObject("FemaleLocomotionReviewFixture") { hideFlags = HideFlags.DontSave };
            root.SetActive(false);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Gameplay/Player/Player.prefab");
            if (prefab == null) throw new Exception("Player prefab missing");
            var player = Object.Instantiate(prefab, root.transform);
            player.transform.position = new Vector3(1000, 1000, 0);
            foreach (var component in player.GetComponentsInChildren<MonoBehaviour>(true))
                if (!(component is PlayerMovement) && !(component is PlayerAnimationController)) component.enabled = false;
            movement = player.GetComponent<PlayerMovement>();
            var animation = player.GetComponentInChildren<PlayerAnimationController>(true);
            renderer = animation.GetComponent<SpriteRenderer>();
            if (renderer == null) renderer = player.GetComponentInChildren<SpriteRenderer>(true);
            foreach (var t in player.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 31;
            floorTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            floorTexture.SetPixels(new[] { Color.gray, Color.gray, Color.gray, Color.gray }); floorTexture.Apply();
            floorSprite = Sprite.Create(floorTexture, new Rect(0, 0, 2, 2), Vector2.one * 0.5f, 2);
            var ground = new GameObject("TemporaryGround"); ground.transform.SetParent(root.transform); ground.layer = 31;
            ground.transform.position = new Vector3(1000, 999.25f, 0);
            ground.transform.localScale = new Vector3(60, 0.5f, 1);
            ground.AddComponent<BoxCollider2D>();
            var floorRenderer = ground.AddComponent<SpriteRenderer>(); floorRenderer.sprite = floorSprite;
            var wall = new GameObject("TemporaryWall"); wall.transform.SetParent(root.transform); wall.layer = 31;
            wall.transform.position = new Vector3(1004, 1000.25f, 0); wall.transform.localScale = new Vector3(0.25f, 1.5f, 1);
            wall.AddComponent<BoxCollider2D>(); wall.AddComponent<SpriteRenderer>().sprite = floorSprite;
            material = new Material(Shader.Find("Sprites/Default")) { hideFlags = HideFlags.HideAndDontSave };
            renderer.sharedMaterial = material; floorRenderer.sharedMaterial = material;
            var cameraObject = new GameObject("TemporaryReviewCamera"); cameraObject.transform.SetParent(root.transform);
            camera = cameraObject.AddComponent<Camera>(); camera.enabled = false; camera.orthographic = true;
            camera.orthographicSize = 1.1f; camera.cullingMask = 1 << 31; camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.08f, 0.1f, 0.12f); camera.transform.position = new Vector3(1000, 1000, -10);
            renderTexture = new RenderTexture(640, 384, 24); camera.targetTexture = renderTexture;
            root.SetActive(true);
            review = ScriptableObject.CreateInstance<FemaleLocomotionReviewWindow>();
            review.StartReview(animation);
            review.ConfigureReview(false, false);
            Time.timeScale = 1;
            startTime = Time.time;
            phaseStartedAt = Time.time;
            Trace.Clear(); Trace.AppendLine("time,mode,playerX,speed,grounded,pose,soleX,soleY,groundY,offsetY,maleIdleFootY,travel,image");
        }

        private static void Capture(string file)
        {
            if (!mineRequested) camera.transform.position = new Vector3(movement.Position.x, 1000, -10);
            Sprite before = renderer.sprite;
            var beforePosition = renderer.transform.position;
            camera.Render();
            if (renderer.sprite != before) throw new Exception("Render sprite was not restored");
            if (Vector3.Distance(beforePosition, renderer.transform.position) > 0.00001f) throw new Exception("Visual transform was not restored");
            var previous = RenderTexture.active;
            var png = new Texture2D(640, 384, TextureFormat.RGB24, false);
            try
            {
                RenderTexture.active = renderTexture;
                png.ReadPixels(new Rect(0, 0, 640, 384), 0, 0); png.Apply();
                File.WriteAllBytes(Path.Combine(output, file), png.EncodeToPNG());
            }
            finally { RenderTexture.active = previous; Object.DestroyImmediate(png); }
        }

        private static void BeginMine()
        {
            if (review != null) Object.DestroyImmediate(review);
            if (root != null) Object.DestroyImmediate(root);
            root = null; review = null; mineRequested = true; mineStage = 0;
            deadline = EditorApplication.timeSinceStartup + 120;
        }

        private static void TickMine()
        {
            var runtime = SaveRuntimeController.Instance;
            if (runtime == null) return;
            if (mineStage == 0)
            {
                if (SceneManager.GetActiveScene().name != SceneNames.MainMenu || runtime.InventoryService == null) return;
                if (!runtime.StartNewGame(1)) throw new Exception("Isolated new game failed");
                mineStage = 1; return;
            }
            if (mineStage == 1)
            {
                if (SceneManager.GetActiveScene().name != SceneNames.SurfaceBase || !runtime.IsUiReady) return;
                // 이동 검사의 관심사가 아닌 최초 퀘스트 안내는 기존 통합 검사와 같은 진행 데이터로 건너뛴다.
                var state = GameBootstrapper.Instance.State;
                state.SetDemoProgress(DemoObjectiveIds.MineCopper, 1, false);
                state.SetQuestRewardSettlement(string.Empty, 1);
                if (!runtime.TryStartExploration(out string reason)) throw new Exception("Exploration failed: " + reason);
                mineStage = 2; return;
            }
            if (mineStage == 2)
            {
                if (SceneManager.GetActiveScene().name != SceneNames.Integration) return;
                var binder = Object.FindAnyObjectByType<IntegrationRuntimeBinder>();
                if (binder == null || !binder.IsUiActivated) return;
                var briefing = Object.FindAnyObjectByType<StartBriefingPopupMotion>();
                if (briefing != null && briefing.isActiveAndEnabled)
                {
                    if (briefing.IsShown)
                    {
                        var objectives = Object.FindAnyObjectByType<DemoObjectiveView>();
                        if (objectives != null) objectives.OnDismissClicked();
                    }
                    return;
                }
                if (Time.timeScale <= 0 || !runtime.IsUiReady) return;
                movement = Object.FindAnyObjectByType<PlayerMovement>();
                if (movement == null || !movement.CanMove) return;
                mineInput = movement.GetComponent<PlayerController>();
                if (mineInput != null) mineInput.enabled = false;
                var animation = movement.GetComponentInChildren<PlayerAnimationController>();
                renderer = animation.GetComponent<SpriteRenderer>();
                review = ScriptableObject.CreateInstance<FemaleLocomotionReviewWindow>();
                review.StartReview(animation); review.ConfigureReview(true, true);
                camera = Camera.main;
                if (camera == null) throw new Exception("Mine camera missing");
                startTime = Time.time; mineCaptureAt = 0; mineStage = 3; capture = 0;
                return;
            }
            float elapsed = Time.time - startTime;
            movement.SetMoveInput(elapsed < 0.3f ? 0 : elapsed < 0.75f ? 1 : elapsed < 1f ? 0 : elapsed < 1.45f ? -1 : 0);
            if (EditorApplication.timeSinceStartup - mineCaptureAt >= 0.15)
            {
                var previousTarget = camera.targetTexture; camera.targetTexture = renderTexture;
                string file = $"mine-{capture++:D2}.png";
                try
                {
                    Capture(file);
                    var sole = review.ContactWorldPoint;
                    Trace.AppendLine(FormattableString.Invariant($"{elapsed:F4},mine,{movement.Position.x:F5},{movement.HorizontalSpeed:F5},{movement.IsGrounded},{review.ContactPose},{sole.x:F5},{sole.y:F5},{review.GroundY:F5},{review.GroundOffset:F5},{review.MaleReferenceFootY:F5},{review.TravelDistance:F5},{file}"));
                }
                finally { camera.targetTexture = previousTarget; }
                mineCaptureAt = EditorApplication.timeSinceStartup;
            }
            if (elapsed < 1.8f) return;
            movement.SetMoveInput(0);
            if (mineInput != null) mineInput.enabled = true;
            Finish(true, "Before/after fixture and isolated Bootstrap -> SurfaceBase -> actual Mine Play completed. Sprite and VisualRoot restoration passed. This does not validate other female motions or input/UI walkthrough.");
        }

        private static void Finish(bool success, string message)
        {
            SessionState.SetBool(Key, false); EditorApplication.update -= Tick;
            if (movement != null) movement.SetMoveInput(0);
            if (mineInput != null) mineInput.enabled = true;
            if (review != null) { review.StopReview(); Object.DestroyImmediate(review); }
            if (root != null) Object.DestroyImmediate(root);
            if (renderTexture != null) { renderTexture.Release(); Object.DestroyImmediate(renderTexture); }
            if (floorSprite != null) Object.DestroyImmediate(floorSprite);
            if (floorTexture != null) Object.DestroyImmediate(floorTexture);
            if (material != null) Object.DestroyImmediate(material);
            File.WriteAllText(Path.Combine(output, "movement-trace.csv"), Trace.ToString());
            File.WriteAllText(Path.Combine(output, "result.txt"), (success ? "PASS\n" : "FAIL\n") + message);
            if (success) Debug.Log("[FemaleMotionReview] " + message); else Debug.LogError(message);
            EditorApplication.isPlaying = false;
            EditorApplication.delayCall += () => { if (Application.isBatchMode) EditorApplication.Exit(success ? 0 : 1); };
        }
    }
}

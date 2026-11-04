using System;
using System.Collections.Generic;
using System.IO;
using SubTerra.Gameplay.Player;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace SubTerra.Editor
{
    // Play 중 렌더링만 교체한다. 원본 프리팹/프레임/세이브를 저장하지 않는다.
    public sealed class FemaleLocomotionReviewWindow : EditorWindow
    {
        private const float PixelsPerUnit = 667.4285714f;
        private static readonly Vector2 Pivot = new(0.4592634f, 0.50390625f);
        private static readonly string[] WalkNames =
            { "key-01", "middle-01", "key-02", "middle-02", "key-03", "middle-03", "key-04", "middle-04" };
        private readonly List<Texture2D> textures = new();
        private readonly List<Sprite> sprites = new();
        private readonly Dictionary<Sprite, Vector2> solePixels = new();
        private readonly HashSet<Sprite> originalIdle = new();
        private readonly HashSet<Sprite> originalWalk = new();
        private SpriteRenderer target;
        private PlayerMovement movement;
        private Collider2D bodyCollider;
        private readonly RaycastHit2D[] groundHits = new RaycastHit2D[8];
        private Sprite idle;
        private Sprite[] entry;
        private Sprite[] walk;
        private Sprite originalDuringRender;
        private Vector3 originalRenderPosition;
        private Camera renderingCamera;
        private int sampledFrame = -1;
        private bool lastWasWalk;
        private float enteredWalkAt;
        private float cycleSeconds = 1f;
        private float entrySeconds = 0.16f;
        private bool distancePlayback = true;
        private bool alignGround = true;
        // Review annotation: ~260px near-foot travel per half cycle at 667.43 PPU.
        private float cycleDistanceLocal = 0.78f;
        private float entryDistanceLocal = 0.12f;
        private float travelDistance;
        private float previousPlayerX;
        private bool previousFlip;
        private Sprite maleIdleReference;
        private Vector2 maleIdleSole;
        private string status = "Bootstrap에서 Play → 광산 진입 후 시험을 켜세요.";
        private string shownPose = "꺼짐";
        private float horizontalSpeed;

        public bool IsReviewActive => target != null;
        public string ShownPose => shownPose;
        public Vector3 ContactWorldPoint { get; private set; }
        public string ContactPose { get; private set; } = "";
        public float GroundOffset { get; private set; }
        public float GroundY { get; private set; } = float.NaN;
        public float MaleReferenceFootY { get; private set; }
        public float TravelDistance => travelDistance;
        public void ConfigureReview(bool byDistance, bool groundAlignment)
        {
            distancePlayback = byDistance; alignGround = groundAlignment;
            sampledFrame = -1; lastWasWalk = false; travelDistance = 0;
            if (movement != null) previousPlayerX = movement.Position.x;
        }

        [MenuItem("SubTerra/Review/Female Idle Walk (Play Only)")]
        public static void Open() => GetWindow<FemaleLocomotionReviewWindow>("여자 걷기 시험");

        private void OnEnable()
        {
            Camera.onPreCull += BeforeCamera;
            Camera.onPostRender += AfterCamera;
            RenderPipelineManager.beginCameraRendering += BeforeSrp;
            RenderPipelineManager.endCameraRendering += AfterSrp;
            EditorApplication.playModeStateChanged += OnPlayMode;
            AssemblyReloadEvents.beforeAssemblyReload += StopReview;
        }

        private void OnDisable()
        {
            StopReview();
            Camera.onPreCull -= BeforeCamera;
            Camera.onPostRender -= AfterCamera;
            RenderPipelineManager.beginCameraRendering -= BeforeSrp;
            RenderPipelineManager.endCameraRendering -= AfterSrp;
            EditorApplication.playModeStateChanged -= OnPlayMode;
            AssemblyReloadEvents.beforeAssemblyReload -= StopReview;
        }

        private void OnPlayMode(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode) StopReview();
        }

        private void OnGUI()
        {
            EditorGUILayout.HelpBox("대기·걷기만 시험 표시합니다. 점프·채굴·사다리 등은 원래 모션입니다. 정식 여자 세트 적용이 아닙니다.", MessageType.Info);
            EditorGUI.BeginChangeCheck();
            distancePlayback = EditorGUILayout.Toggle("이동 거리에 맞춰 재생", distancePlayback);
            alignGround = EditorGUILayout.Toggle("바닥 높이에 발 맞춤", alignGround);
            if (distancePlayback)
            {
                cycleDistanceLocal = EditorGUILayout.Slider("한 주기 이동 거리(검토값)", cycleDistanceLocal, 0.3f, 2f);
                entryDistanceLocal = EditorGUILayout.Slider("진입 이동 거리", entryDistanceLocal, 0.04f, 0.3f);
            }
            else
            {
                cycleSeconds = EditorGUILayout.Slider("걷기 한 주기(초)", cycleSeconds, 0.2f, 2f);
                entrySeconds = EditorGUILayout.Slider("진입 시간(초)", entrySeconds, 0.04f, 0.4f);
            }
            if (EditorGUI.EndChangeCheck()) { sampledFrame = -1; lastWasWalk = false; }
            using (new EditorGUI.DisabledScope(!EditorApplication.isPlaying || IsReviewActive))
                if (GUILayout.Button("대기·걷기 시험 켜기")) TryStart();
            using (new EditorGUI.DisabledScope(!IsReviewActive))
                if (GUILayout.Button("시험 끄기 · 원래 표시 복원")) StopReview();
            EditorGUILayout.LabelField("표시", shownPose);
            EditorGUILayout.LabelField("실제 가로 속도", horizontalSpeed.ToString("F2") + " unit/s");
            EditorGUILayout.LabelField("발 높이 보정", GroundOffset.ToString("F3") + " unit");
            EditorGUILayout.HelpBox(status, MessageType.None);
            EditorGUILayout.LabelField("확인: 출발/정지/좌우 전환, 발의 가로 이동과 지면 접점.", EditorStyles.wordWrappedLabel);
        }

        private void TryStart()
        {
            try
            {
                var found = FindObjectsByType<PlayerAnimationController>();
                if (found.Length != 1) throw new InvalidOperationException("플레이어가 하나인 광산 화면에서 실행하세요.");
                StartReview(found[0]);
            }
            catch (Exception e) { StopReview(); status = e.Message; Debug.LogError("[FemaleMotionReview] " + e.Message); }
        }

        public void StartReview(PlayerAnimationController controller)
        {
            StopReview();
            if (!EditorApplication.isPlaying) throw new InvalidOperationException("Play 중에만 사용할 수 있습니다.");
            if (controller == null) throw new ArgumentNullException(nameof(controller));
            try
            {
                var serialized = new SerializedObject(controller);
                var rendererProperty = serialized.FindProperty("spriteRenderer");
                target = rendererProperty.objectReferenceValue as SpriteRenderer;
                if (target == null) throw new InvalidOperationException("플레이어 SpriteRenderer 참조가 없습니다.");
                ReadFrameSet(serialized.FindProperty("idleFrames"), originalIdle);
                ReadFrameSet(serialized.FindProperty("walkFrames"), originalWalk);
                if (originalIdle.Count == 0 || originalWalk.Count == 0) throw new InvalidOperationException("원래 대기·걷기 그림이 없습니다.");
                movement = controller.GetComponentInParent<PlayerMovement>();
                if (movement == null) throw new InvalidOperationException("PlayerMovement 참조가 없습니다.");
                bodyCollider = movement.GetComponent<Collider2D>();
                foreach (var sprite in originalIdle) { maleIdleReference = sprite; break; }
                var referenceTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                try
                {
                    if (!referenceTexture.LoadImage(File.ReadAllBytes(AssetDatabase.GetAssetPath(maleIdleReference)), false))
                        throw new InvalidDataException("남자 대기 원본 PNG를 읽을 수 없습니다.");
                    maleIdleSole = FindBottomSole(referenceTexture);
                }
                finally { DestroyImmediate(referenceTexture); }
                string art = Path.GetFullPath(Path.Combine(Application.dataPath, "../../work_process/images/issue266"));
                string aligned = Path.Combine(art, "female-motion-alignment-review-20261103");
                idle = LoadSprite(Path.Combine(aligned, "damage/pose-01.png"));
                entry = new[] {
                    LoadSprite(Path.Combine(art, "female-walk-entry-review-20261103/entry-01.png")),
                    LoadSprite(Path.Combine(art, "female-walk-entry-review-20261103/entry-02.png")) };
                walk = new Sprite[WalkNames.Length];
                for (int i = 0; i < walk.Length; i++) walk[i] = LoadSprite(Path.Combine(aligned, "walk/" + WalkNames[i] + ".png"));
                sampledFrame = -1; lastWasWalk = false;
                previousPlayerX = movement.Position.x; previousFlip = target.flipX; travelDistance = 0;
                status = "Play 한정 시험 중. 프리팹·씬·세이브를 변경하지 않습니다. 단위 격자와 비교해 발 미끄러짐을 보세요.";
            }
            catch { StopReview(); throw; }
        }

        private static void ReadFrameSet(SerializedProperty property, HashSet<Sprite> result)
        {
            for (int i = 0; i < property.arraySize; i++)
                if (property.GetArrayElementAtIndex(i).objectReferenceValue is Sprite sprite) result.Add(sprite);
        }

        private Sprite LoadSprite(string path)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave, filterMode = FilterMode.Bilinear };
            textures.Add(texture);
            if (!texture.LoadImage(File.ReadAllBytes(path), false) || texture.width != 1024 || texture.height != 1024)
                throw new InvalidDataException("1024px 검토용 PNG를 확인하세요: " + Path.GetFileName(path));
            var sprite = Sprite.Create(texture, new Rect(0, 0, 1024, 1024), Pivot, PixelsPerUnit, 0, SpriteMeshType.FullRect);
            sprite.name = Path.GetFileNameWithoutExtension(path);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            sprites.Add(sprite);
            solePixels[sprite] = FindBottomSole(texture);
            return sprite;
        }

        private static Vector2 FindBottomSole(Texture2D texture)
        {
            // Lowest opaque sole pixels are a diagnostic point, not an IK contact.
            var pixels = texture.GetPixels32();
            for (int y = 0; y < texture.height / 2; y++)
            {
                float sum = 0; int count = 0;
                for (int x = 0; x < texture.width; x++) if (pixels[y * texture.width + x].a >= 180) { sum += x + 0.5f; count++; }
                if (count > 0) return new Vector2(sum / count, y + 0.5f);
            }
            throw new InvalidDataException("Sole diagnostic point missing");
        }

        private void BeforeSrp(ScriptableRenderContext context, Camera camera) => BeforeCamera(camera);
        private void AfterSrp(ScriptableRenderContext context, Camera camera) => AfterCamera(camera);

        private void BeforeCamera(Camera camera)
        {
            if (!EditorApplication.isPlaying || target == null || !target.enabled || renderingCamera != null) return;
            if (camera.cameraType != CameraType.Game) return;
            Sprite source = target.sprite;
            bool isWalk = originalWalk.Contains(source), isIdle = originalIdle.Contains(source);
            if (!isWalk && !isIdle)
            {
                lastWasWalk = false; previousPlayerX = movement.Position.x;
                ContactPose = ""; GroundY = float.NaN; shownPose = "범위 밖 · 원래 모션"; return;
            }
            horizontalSpeed = movement.HorizontalSpeed;
            // 벽을 밀 때는 멈추고 감속 중에는 실제 이동을 따라간다.
            if (distancePlayback) { isWalk = horizontalSpeed > 0.02f; isIdle = !isWalk; }
            float scaleX = Mathf.Max(0.001f, Mathf.Abs(target.transform.lossyScale.x));
            float cycleWorld = cycleDistanceLocal * scaleX;
            float entryWorld = entryDistanceLocal * scaleX;
            if (sampledFrame != Time.frameCount)
            {
                float delta = Mathf.Abs(movement.Position.x - previousPlayerX);
                bool restart = isWalk && (!lastWasWalk || previousFlip != target.flipX || delta > Mathf.Max(2f, cycleWorld * 2));
                if (restart) { enteredWalkAt = Time.time; travelDistance = 0; }
                else if (isWalk) travelDistance += delta;
                else travelDistance = 0;
                previousPlayerX = movement.Position.x; previousFlip = target.flipX;
                lastWasWalk = isWalk; sampledFrame = Time.frameCount;
            }
            float elapsed = Mathf.Max(0f, Time.time - enteredWalkAt);
            Sprite candidate = idle;
            bool inEntry = distancePlayback ? travelDistance < entryWorld : elapsed < entrySeconds;
            if (isWalk)
            {
                if (inEntry)
                    candidate = entry[Mathf.Min(1, Mathf.FloorToInt((distancePlayback ? travelDistance / entryWorld : elapsed / entrySeconds) * 2))];
                else
                    candidate = walk[Mathf.FloorToInt((distancePlayback ? (travelDistance - entryWorld) / cycleWorld : (elapsed - entrySeconds) / cycleSeconds) * walk.Length) % walk.Length];
            }
            originalDuringRender = source; originalRenderPosition = target.transform.position;
            renderingCamera = camera; target.sprite = candidate;
            var localSole = (solePixels[candidate] - candidate.pivot) / candidate.pixelsPerUnit;
            if (target.flipX) localSole.x = -localSole.x;
            if (target.flipY) localSole.y = -localSole.y;
            ContactWorldPoint = target.transform.TransformPoint(localSole);
            MaleReferenceFootY = target.transform.TransformPoint((maleIdleSole - maleIdleReference.pivot) / maleIdleReference.pixelsPerUnit).y;
            GroundOffset = 0; GroundY = float.NaN;
            if (movement.IsGrounded && bodyCollider != null)
            {
                var bounds = bodyCollider.bounds;
                var filter = new ContactFilter2D().NoFilter(); filter.useTriggers = false;
                int count = Physics2D.Raycast(bounds.center, Vector2.down, filter, groundHits, bounds.extents.y + 0.3f);
                float closest = float.PositiveInfinity;
                for (int i = 0; i < count; i++)
                {
                    var hit = groundHits[i];
                    if (hit.collider == null || hit.transform.IsChildOf(movement.transform) || hit.normal.y < 0.5f || hit.distance >= closest) continue;
                    closest = hit.distance; GroundY = hit.point.y;
                }
                if (alignGround && !float.IsNaN(GroundY))
                {
                    GroundOffset = Mathf.Clamp(GroundY - ContactWorldPoint.y, -0.15f, 0.15f);
                    target.transform.position += Vector3.up * GroundOffset;
                    ContactWorldPoint += Vector3.up * GroundOffset;
                }
            }
            ContactPose = (isIdle ? "idle/" : inEntry ? "entry/" : "walk/") + candidate.name;
            shownPose = isIdle ? "대기 참고" : (inEntry ? "진입 · " : "걷기 · ") + candidate.name;
            Repaint();
        }

        private void AfterCamera(Camera camera)
        {
            if (camera != renderingCamera) return;
            RestoreRender();
        }

        private void RestoreRender()
        {
            if (target != null && renderingCamera != null)
            {
                target.sprite = originalDuringRender;
                target.transform.position = originalRenderPosition;
            }
            originalDuringRender = null; renderingCamera = null;
        }

        public void StopReview()
        {
            RestoreRender();
            target = null; movement = null; bodyCollider = null;
            maleIdleReference = null; idle = null; entry = null; walk = null;
            originalIdle.Clear(); originalWalk.Clear();
            foreach (var sprite in sprites) if (sprite != null) DestroyImmediate(sprite);
            foreach (var texture in textures) if (texture != null) DestroyImmediate(texture);
            sprites.Clear(); textures.Clear(); solePixels.Clear(); lastWasWalk = false; shownPose = "꺼짐"; ContactPose = "";
            status = "시험 꺼짐. 원래 모션을 표시합니다.";
        }
    }
}

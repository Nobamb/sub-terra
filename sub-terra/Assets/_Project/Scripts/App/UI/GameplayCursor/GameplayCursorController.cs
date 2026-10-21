using System;
using SubTerra.App.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SubTerra.App.UI.GameplayCursor
{
    /// <summary>
    /// 인게임(지상 기지·탐사) 커서(B-151). OS 커서를 숨기고 최상위 오버레이에 드릴/집게 스프라이트를 마우스 위치에 그린다.
    /// Bootstrap 루트 아래에 붙어 씬 전환에도 유지되며, 메인 메뉴·지상·탐사 화면에서 사용한다.
    /// 클릭은 가로채지 않는다(GraphicRaycaster 없음, raycastTarget=false).
    /// </summary>
    public sealed class GameplayCursorController : MonoBehaviour
    {
        public const string ObjectName = "GameplayCursor";
        public const int SortingOrder = 32_767;
        private const int ParticlePoolSize = 8;

        private struct Particle
        {
            public RectTransform Rect;
            public Image Image;
            public Vector2 Pos;
            public Vector2 Vel;
            public float Life;
            public float MaxLife;
            public bool Active;
        }

        private static Sprite glowSprite;
        private static Sprite ringSprite;

        [SerializeField] private GameplayCursorSkin skin;

        private GameplayCursorModel model;
        private GameObject canvasRoot;
        private RectTransform hotspot;
        private RectTransform visualRect;
        private Image visual;
        private Image glow;
        private Image ring;
        private RectTransform particleRoot;
        private Particle[] particles;
        private float emitCarry;
        private float jitterTime;
        private bool customActive;
        private bool missingSkinLogged;
        private Sprite appliedSprite;
        private uint randomState = 0x9E3779B9u;

        public bool CustomCursorActive => customActive;
        public GameplayCursorModel Model => model;
        public RectTransform Hotspot => hotspot;
        public Image Visual => visual;
        public Image Glow => glow;
        public Image Ring => ring;
        public Canvas Canvas => canvasRoot != null ? canvasRoot.GetComponent<Canvas>() : null;
        public int ActiveParticleCount
        {
            get
            {
                var count = 0;
                if (particles != null)
                    for (var i = 0; i < particles.Length; i++)
                        if (particles[i].Active) count++;
                return count;
            }
        }

        /// <summary>Bootstrap 루트 아래에 커서를 한 번만 붙인다.</summary>
        public static GameplayCursorController AttachTo(Transform root)
        {
            if (root == null) return null;
            var existing = root.GetComponentInChildren<GameplayCursorController>(true);
            if (existing != null) return existing;
            var go = new GameObject(ObjectName);
            go.transform.SetParent(root, false);
            return go.AddComponent<GameplayCursorController>();
        }

        /// <summary>커스텀 커서를 켜는 씬: Bootstrap 이후 메인 메뉴·지상 기지·탐사.</summary>
        public static bool IsGameplayScene(string sceneName)
        {
            return string.Equals(sceneName, SceneNames.MainMenu, StringComparison.Ordinal)
                || string.Equals(sceneName, SceneNames.SurfaceBase, StringComparison.Ordinal)
                || string.Equals(sceneName, SceneNames.Integration, StringComparison.Ordinal);
        }

        public void SetSkin(GameplayCursorSkin value)
        {
            skin = value;
            model = null;
            appliedSprite = null;
        }

        private void OnEnable()
        {
            SceneManager.activeSceneChanged += OnActiveSceneChanged;
            try
            {
                SetCustomCursorActive(IsGameplayScene(SceneManager.GetActiveScene().name));
            }
            catch (Exception e)
            {
                Debug.LogError("[GameplayCursor] 커서 초기화 실패: " + e.Message);
                SetCustomCursorActive(false);
            }
        }

        private void OnDisable()
        {
            SceneManager.activeSceneChanged -= OnActiveSceneChanged;
            SetCustomCursorActive(false);
        }

        private void OnDestroy()
        {
            if (customActive && Application.isPlaying) UnityEngine.Cursor.visible = true;
        }

        private void OnApplicationFocus(bool focus)
        {
            // 포커스를 되찾을 때 OS가 커서를 다시 보이게 한 경우를 정리한다.
            if (focus && customActive) UnityEngine.Cursor.visible = false;
        }

        private void OnActiveSceneChanged(Scene previous, Scene next)
        {
            SetCustomCursorActive(IsGameplayScene(next.name));
        }

        public void SetCustomCursorActive(bool active)
        {
            if (active && !EnsureBuilt()) active = false;
            customActive = active;
            if (canvasRoot != null) canvasRoot.SetActive(active);
            if (Application.isPlaying) UnityEngine.Cursor.visible = !active;
            if (!active) ClearParticles();
        }

        /// <summary>오버레이 계층을 만든다. 스킨이 없으면 OS 커서를 유지한다.</summary>
        public bool EnsureBuilt()
        {
            if (skin == null) skin = GameplayCursorSkin.Load();
            if (skin == null || skin.drillFrames == null || skin.drillFrames.Length == 0 || skin.drillFrames[0] == null)
            {
                if (!missingSkinLogged) Debug.LogWarning("[GameplayCursor] 커서 스킨이 없어 OS 커서를 사용합니다.");
                missingSkinLogged = true;
                return false;
            }

            if (model == null) model = new GameplayCursorModel(skin.tuning);
            if (canvasRoot != null) return true;

            canvasRoot = new GameObject("GameplayCursorCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(CanvasGroup));
            canvasRoot.transform.SetParent(transform, false);
            var canvas = canvasRoot.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = SortingOrder;
            var scaler = canvasRoot.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            var group = canvasRoot.GetComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;

            hotspot = NewRect("Hotspot", (RectTransform)canvasRoot.transform);
            hotspot.anchorMin = hotspot.anchorMax = Vector2.zero;
            hotspot.pivot = new Vector2(0.5f, 0.5f);
            hotspot.sizeDelta = Vector2.zero;

            var tuning = skin.tuning;
            ring = NewImage("SwapRing", hotspot, RingSprite(), tuning.glowColor);
            ring.rectTransform.sizeDelta = Vector2.one * skin.drillCellSize * 0.9f;
            ring.enabled = false;
            glow = NewImage("TipGlow", hotspot, GlowSprite(), tuning.glowColor);
            glow.rectTransform.sizeDelta = Vector2.one * tuning.glowSize;
            glow.enabled = false;
            visual = NewImage("Visual", hotspot, skin.drillFrames[0], Color.white);
            visual.preserveAspect = true;
            visualRect = visual.rectTransform;
            particleRoot = NewRect("TipParticles", hotspot);
            particleRoot.sizeDelta = Vector2.zero;
            // 빛은 그림 위 끝에 겹쳐야 하므로 드릴 뒤가 아닌 앞에 둔다.
            glow.transform.SetAsLastSibling();
            particleRoot.SetAsLastSibling();
            BuildParticles();
            ApplySprite(skin.drillFrames[0], skin.drillCellSize);
            return true;
        }

        private void LateUpdate()
        {
            if (!customActive) return;
            var mouse = Mouse.current;
            if (mouse == null) return;
            // timeScale과 무관하게 매 프레임 실제 마우스 좌표로 맞춘다.
            Apply(GameplayCursorState.Read(mouse.leftButton.isPressed), Time.unscaledDeltaTime, mouse.position.ReadValue());
        }

        /// <summary>한 프레임 연출을 반영한다. 핫스팟은 항상 마우스 좌표 그대로이며 떨림은 그림에만 준다.</summary>
        public void Apply(GameplayCursorInput input, float deltaTime, Vector2 screenPosition)
        {
            if (!EnsureBuilt()) return;
            model.Tick(deltaTime, input);
            hotspot.position = new Vector3(screenPosition.x, screenPosition.y, 0f);

            var claw = model.Sheet == GameplayCursorSheet.Claw && skin.clawFrames != null && skin.clawFrames.Length > 0;
            var frames = claw ? skin.clawFrames : skin.drillFrames;
            var index = Mathf.Clamp(model.FrameIndex, 0, frames.Length - 1);
            ApplySprite(frames[index] != null ? frames[index] : skin.drillFrames[0], claw ? skin.clawCellSize : skin.drillCellSize);

            jitterTime += Mathf.Max(0f, deltaTime);
            var amplitude = model.JitterAmplitude;
            var frequency = skin.tuning.jitterFrequency;
            visualRect.anchoredPosition = amplitude <= 0f
                ? Vector2.zero
                : new Vector2(
                    (Mathf.PerlinNoise(jitterTime * frequency, 0.31f) - 0.5f) * 2f * amplitude,
                    (Mathf.PerlinNoise(0.73f, jitterTime * frequency) - 0.5f) * 2f * amplitude);
            visualRect.localScale = Vector3.one * model.PulseScale;

            var tuning = skin.tuning;
            ring.enabled = model.RingAlpha > 0.001f;
            if (ring.enabled)
            {
                ring.color = WithAlpha(tuning.glowColor, model.RingAlpha * 0.9f);
                ring.rectTransform.localScale = Vector3.one * Mathf.Lerp(1.15f, 0.7f, model.RingAlpha);
            }

            glow.enabled = model.GlowLevel > 0.001f;
            if (glow.enabled)
            {
                var breathe = 0.85f + 0.15f * Mathf.Sin(jitterTime * 6f);
                glow.color = WithAlpha(tuning.glowColor, tuning.glowAlpha * model.GlowLevel * breathe);
            }

            UpdateParticles(Mathf.Max(0f, deltaTime));
        }

        private void ApplySprite(Sprite sprite, float cellSize)
        {
            if (sprite == appliedSprite) return;
            appliedSprite = sprite;
            visual.sprite = sprite;
            var size = sprite.rect.size;
            // 스프라이트 피벗(드릴·집게 끝)을 RectTransform 피벗으로 써서 끝이 정확히 클릭 지점에 오게 한다.
            visualRect.pivot = new Vector2(sprite.pivot.x / size.x, sprite.pivot.y / size.y);
            visualRect.sizeDelta = new Vector2(cellSize, cellSize * size.y / Mathf.Max(1f, size.x));
        }

        private void BuildParticles()
        {
            particles = new Particle[ParticlePoolSize];
            for (var i = 0; i < particles.Length; i++)
            {
                var image = NewImage("Particle" + i, particleRoot, GlowSprite(), skin.tuning.glowColor);
                image.gameObject.SetActive(false);
                particles[i].Image = image;
                particles[i].Rect = image.rectTransform;
            }
        }

        private void UpdateParticles(float dt)
        {
            if (particles == null) return;
            var tuning = skin.tuning;
            if (model.ParticlesActive && dt > 0f)
            {
                emitCarry += tuning.particlesPerSecond * dt;
                while (emitCarry >= 1f)
                {
                    emitCarry -= 1f;
                    Emit(tuning);
                }
            }
            else emitCarry = 0f;

            for (var i = 0; i < particles.Length; i++)
            {
                if (!particles[i].Active) continue;
                particles[i].Life += dt;
                if (particles[i].Life >= particles[i].MaxLife)
                {
                    particles[i].Active = false;
                    particles[i].Image.gameObject.SetActive(false);
                    continue;
                }

                particles[i].Pos += particles[i].Vel * dt;
                particles[i].Rect.anchoredPosition = particles[i].Pos;
                var fade = 1f - particles[i].Life / particles[i].MaxLife;
                particles[i].Image.color = WithAlpha(tuning.glowColor, 0.75f * fade);
            }
        }

        private void Emit(GameplayCursorTuning tuning)
        {
            for (var i = 0; i < particles.Length; i++)
            {
                if (particles[i].Active) continue;
                var angle = Mathf.Lerp(0f, Mathf.PI * 2f, NextRandom());
                var speed = Mathf.Lerp(8f, 20f, NextRandom());
                particles[i].Active = true;
                particles[i].Life = 0f;
                particles[i].MaxLife = Mathf.Lerp(0.35f, 0.65f, NextRandom());
                particles[i].Pos = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 2f;
                particles[i].Vel = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed;
                particles[i].Rect.sizeDelta = Vector2.one * Mathf.Lerp(tuning.particleSize.x, tuning.particleSize.y, NextRandom());
                particles[i].Rect.anchoredPosition = particles[i].Pos;
                particles[i].Image.color = WithAlpha(tuning.glowColor, 0.75f);
                particles[i].Image.gameObject.SetActive(true);
                return;
            }
        }

        private void ClearParticles()
        {
            emitCarry = 0f;
            if (particles == null) return;
            for (var i = 0; i < particles.Length; i++)
            {
                particles[i].Active = false;
                if (particles[i].Image != null) particles[i].Image.gameObject.SetActive(false);
            }
        }

        // 연출 전용 xorshift. 전역 Random 상태(게임 결정성)에 영향을 주지 않는다.
        private float NextRandom()
        {
            randomState ^= randomState << 13;
            randomState ^= randomState >> 17;
            randomState ^= randomState << 5;
            return (randomState & 0xFFFFFF) / (float)0x1000000;
        }

        private static RectTransform NewRect(string name, RectTransform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static Image NewImage(string name, RectTransform parent, Sprite sprite, Color color)
        {
            var rect = NewRect(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = Mathf.Clamp01(alpha);
            return color;
        }

        private static Sprite GlowSprite()
        {
            if (glowSprite != null) return glowSprite;
            glowSprite = ProceduralSprite(32, d => { var a = Mathf.Clamp01(1f - d); return a * a; });
            return glowSprite;
        }

        private static Sprite RingSprite()
        {
            if (ringSprite != null) return ringSprite;
            ringSprite = ProceduralSprite(64, d => Mathf.Clamp01(1f - Mathf.Abs(d - 0.82f) / 0.12f));
            return ringSprite;
        }

        private static Sprite ProceduralSprite(int n, Func<float, float> alphaAtDistance)
        {
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            for (var y = 0; y < n; y++)
                for (var x = 0; x < n; x++)
                {
                    var dx = (x + 0.5f) / n * 2f - 1f;
                    var dy = (y + 0.5f) / n * 2f - 1f;
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alphaAtDistance(Mathf.Sqrt(dx * dx + dy * dy))));
                }
            tex.Apply();
            var sprite = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }
    }
}

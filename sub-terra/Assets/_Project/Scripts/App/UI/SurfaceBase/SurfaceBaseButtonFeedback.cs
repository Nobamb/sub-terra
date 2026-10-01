using UnityEngine;
using UnityEngine.EventSystems;

namespace SubTerra.App.UI.SurfaceBase
{
    /// <summary>기지 버튼의 밝기 전환, 내부 빛 이동, 호버 파티클, 아이콘 회전. 게임 상태는 변경하지 않는다.</summary>
    [RequireComponent(typeof(UnityEngine.UI.Button))]
    public sealed class SurfaceBaseButtonFeedback : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
    {
        public const float BlendSeconds = 0.2f;
        public const float SpinSeconds = 0.5f;
        private const float IconIdleBrightness = 0.45f;
        private const int ParticlePoolSize = 48;

        private struct Particle
        {
            public RectTransform Rect;
            public UnityEngine.UI.Image Image;
            public Vector2 Pos;
            public Vector2 Vel;
            public float Life;
            public float MaxLife;
            public float BaseAlpha;
            public bool Active;
        }

        [SerializeField] private UnityEngine.UI.Image plate;
        [SerializeField] private UnityEngine.UI.Image highlight;
        [SerializeField] private UnityEngine.UI.Image sweep;
        [SerializeField] private UnityEngine.UI.Image icon;
        [SerializeField] private UnityEngine.UI.Image glow;
        [SerializeField] private Color glowColor = new Color(0.75f, 0.95f, 1f, 1f);
        [SerializeField] private float hoverBoost;
        [SerializeField] private RectTransform particleRoot;
        [SerializeField] private Sprite particleSprite;
        [SerializeField] private bool spinIconOnHover;

        private static Sprite softGlowSprite;
        private UnityEngine.UI.Button button;
        private Particle[] particles;
        private bool hovered;
        private bool selected;
        private bool wasActive;
        private float blend;
        private float sweepTime;
        private float spinTime = -1f;
        private float emitCarry;

        public float HighlightAmount => blend;
        public float IconBrightness => icon != null ? icon.color.r : 1f;

        private void Awake() => button = GetComponent<UnityEngine.UI.Button>();
        public void OnPointerEnter(PointerEventData eventData) => hovered = true;
        public void OnPointerExit(PointerEventData eventData) => hovered = false;
        public void OnSelect(BaseEventData eventData) => selected = true;
        public void OnDeselect(BaseEventData eventData) => selected = false;

        private void OnEnable() => Apply(true);

        private void Update()
        {
            if (button == null) button = GetComponent<UnityEngine.UI.Button>();
            float dt = Time.unscaledDeltaTime;
            bool enabled = button.IsInteractable();
            bool active = enabled && (hovered || selected);
            float target = active ? 1f : 0f;
            blend = Mathf.MoveTowards(blend, target, dt / BlendSeconds);
            if (active && !wasActive) OnActivated();
            wasActive = active;
            Apply(enabled);
            if (active) sweepTime += dt;
            else sweepTime = 0f;
            UpdateSpin(dt);
            if (active) Emit(dt);
            UpdateParticles(dt);
        }

        private void OnDisable()
        {
            hovered = selected = wasActive = false;
            blend = sweepTime = emitCarry = 0f;
            spinTime = -1f;
            if (icon != null) icon.rectTransform.localRotation = Quaternion.identity;
            if (particles != null)
                for (int i = 0; i < particles.Length; i++)
                {
                    particles[i].Active = false;
                    if (particles[i].Image != null) particles[i].Image.gameObject.SetActive(false);
                }
            Apply(true);
        }

        /// <summary>한 바퀴 회전의 진행도(0~1). 0.2초 가속, 0.1초 최고속, 0.2초 감속으로 속도가 끊기지 않는다.</summary>
        public static float SpinTurn(float seconds)
        {
            float t = Mathf.Clamp(seconds, 0f, SpinSeconds);
            if (t < 0.2f)
            {
                float u = t / 0.2f;
                return u * u / 3f;
            }
            if (t < 0.3f) return 1f / 3f + (t - 0.2f) / 0.1f / 3f;
            float v = (t - 0.3f) / 0.2f;
            return 2f / 3f + (1f - (1f - v) * (1f - v)) / 3f;
        }

        private void OnActivated()
        {
            if (spinIconOnHover && icon != null && spinTime < 0f) spinTime = 0f;
            for (int i = 0; i < 10; i++) Spawn();
        }

        private void UpdateSpin(float dt)
        {
            if (spinTime < 0f || icon == null) return;
            spinTime += dt;
            if (spinTime >= SpinSeconds)
            {
                spinTime = -1f;
                icon.rectTransform.localRotation = Quaternion.identity;
                return;
            }
            icon.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -360f * SpinTurn(spinTime));
        }

        private void Apply(bool enabled)
        {
            if (plate != null)
            {
                float brightness = enabled ? 0.82f : 0.4f;
                plate.color = new Color(brightness, brightness, brightness, 1f);
            }
            if (highlight != null) highlight.color = new Color(1f, 1f, 1f, blend);
            if (icon != null)
            {
                float v = Mathf.Lerp(IconIdleBrightness, 1f, blend);
                icon.color = new Color(v, v, v, 1f);
            }
            if (glow != null)
            {
                if (glow.sprite == null) glow.sprite = SoftGlow();
                var c = glowColor;
                c.a = blend * 0.34f;
                glow.color = c;
            }
            if (sweep == null) return;
            var parent = sweep.rectTransform.parent as RectTransform;
            float phase = Mathf.Repeat(sweepTime / 1.8f, 1f);
            sweep.rectTransform.anchoredPosition = new Vector2(
                Mathf.Lerp(-parent.rect.width * 0.6f, parent.rect.width * 0.6f, phase), 0f);
            var color = sweep.color;
            color.a = blend * (0.13f + 0.27f * hoverBoost) * Mathf.Sin(phase * Mathf.PI);
            sweep.color = color;
        }

        // 설정창(SettingsMenuSkin)의 청록 입자와 같은 크기·수명·상승 속도를 쓴다.
        private void Emit(float dt)
        {
            if (particleRoot == null) return;
            emitCarry += Mathf.Clamp(particleRoot.rect.width * 0.06f, 16f, 48f) * dt;
            while (emitCarry >= 1f)
            {
                emitCarry -= 1f;
                Spawn();
            }
        }

        private void EnsurePool()
        {
            if (particles != null || particleRoot == null) return;
            particles = new Particle[ParticlePoolSize];
            for (int i = 0; i < particles.Length; i++)
            {
                var go = new GameObject("P_" + i, typeof(RectTransform), typeof(UnityEngine.UI.Image));
                go.transform.SetParent(particleRoot, false);
                var rect = (RectTransform)go.transform;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
                var image = go.GetComponent<UnityEngine.UI.Image>();
                image.raycastTarget = false;
                image.preserveAspect = true;
                image.sprite = particleSprite != null ? particleSprite : SoftGlow();
                go.SetActive(false);
                particles[i].Rect = rect;
                particles[i].Image = image;
            }
        }

        private void Spawn()
        {
            EnsurePool();
            if (particles == null) return;
            for (int i = 0; i < particles.Length; i++)
            {
                if (particles[i].Active) continue;
                var half = particleRoot.rect.size * 0.5f;
                float size = Random.Range(2.2f, 4.4f);
                particles[i].Active = true;
                particles[i].Pos = new Vector2(
                    Random.Range(-0.92f, 0.92f) * half.x,
                    Random.Range(-0.85f, 0.2f) * half.y);
                particles[i].Vel = new Vector2(Random.Range(-14f, 14f), Random.Range(22f, 48f));
                particles[i].Life = 0f;
                particles[i].MaxLife = Random.Range(0.45f, 0.85f);
                particles[i].BaseAlpha = Random.Range(0.25f, 1f);
                particles[i].Rect.sizeDelta = new Vector2(size, size);
                particles[i].Rect.anchoredPosition = particles[i].Pos;
                var c = Color.Lerp(new Color(0.29f, 0.88f, 0.95f), Color.white, Random.value * 0.35f);
                particles[i].Image.color = new Color(c.r, c.g, c.b, particles[i].BaseAlpha);
                particles[i].Image.gameObject.SetActive(true);
                return;
            }
        }

        private void UpdateParticles(float dt)
        {
            if (particles == null || dt <= 0f) return;
            for (int i = 0; i < particles.Length; i++)
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
                particles[i].Vel.y += 12f * dt;
                particles[i].Rect.anchoredPosition = particles[i].Pos;
                var c = particles[i].Image.color;
                c.a = Mathf.Lerp(particles[i].BaseAlpha, 0f, particles[i].Life / particles[i].MaxLife);
                particles[i].Image.color = c;
            }
        }

        // 버튼 안쪽이 고르게 환해지도록 가장자리가 부드럽게 사라지는 사각형에 가까운 그라데이션.
        private static Sprite SoftGlow()
        {
            if (softGlowSprite != null) return softGlowSprite;
            const int n = 64;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dx = Mathf.Abs((x + 0.5f) / n * 2f - 1f);
                    float dy = Mathf.Abs((y + 0.5f) / n * 2f - 1f);
                    float d = Mathf.Pow(dx * dx * dx + dy * dy * dy, 1f / 3f);
                    float a = Mathf.Clamp01(1f - d);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a * a * (3f - 2f * a)));
                }
            tex.Apply();
            softGlowSprite = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
            softGlowSprite.hideFlags = HideFlags.HideAndDontSave;
            return softGlowSprite;
        }
    }
}

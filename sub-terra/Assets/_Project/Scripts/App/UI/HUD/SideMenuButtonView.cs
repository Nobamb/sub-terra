using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SubTerra.App.UI.HUD
{
    /// <summary>제공된 두 이미지의 알파만 0.3초 동안 교차 전환하고, 호버 시 청록 파티클을 올린다.</summary>
    public sealed class SideMenuButtonView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public const float HoverSeconds = 0.3f;
        [SerializeField] private Image normal;
        [SerializeField] private Image hover;
        [SerializeField] private Sprite particleSprite;
        private Button button;
        private bool pointed;
        private float alpha;
        private float emitTimer;
        private RectTransform particleRoot;
        private Sprite fallbackParticleSprite;
        private UIParticle[] pool;
        public int ActiveParticleCount { get; private set; }

        private struct UIParticle
        {
            public RectTransform Rect;
            public Image Img;
            public Vector2 Pos;
            public Vector2 Vel;
            public float Life;
            public float MaxLife;
            public float BaseAlpha;
            public bool Active;
        }

        private void Awake() => button = GetComponent<Button>();

        private void OnEnable()
        {
            if (Application.isPlaying) EnsurePool();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            pointed = true;
            Burst(8);
        }

        public void OnPointerExit(PointerEventData eventData) => pointed = false;

        private void OnDisable()
        {
            pointed = false;
            alpha = 0f;
            emitTimer = 0f;
            Apply();
            ClearParticles();
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            float target = pointed && button != null && button.IsInteractable() ? 1f : 0f;
            alpha = Mathf.MoveTowards(alpha, target, dt / HoverSeconds);
            Apply();
            if (pointed && target > 0f)
            {
                emitTimer += dt;
                if (emitTimer >= 0.045f)
                {
                    emitTimer = 0f;
                    Burst(3);
                }
            }
            else
            {
                emitTimer = 0f;
            }

            UpdateParticles(dt);
        }

        private void Apply()
        {
            if (normal != null) normal.color = new Color(1f, 1f, 1f, 1f - alpha);
            if (hover != null) hover.color = new Color(1f, 1f, 1f, alpha);
        }

        private void Burst(int count)
        {
            if (!Application.isPlaying) return;
            var origin = (RectTransform)transform;
            for (int i = 0; i < count; i++)
            {
                Vector3 local = new Vector3(
                    Random.Range(-origin.rect.width * 0.32f, origin.rect.width * 0.32f),
                    Random.Range(-origin.rect.height * 0.18f, origin.rect.height * 0.12f),
                    0f);
                Spawn(origin.TransformPoint(local), new Vector2(Random.Range(-14f, 14f), Random.Range(22f, 48f)));
            }
        }

        private Sprite ParticleSpriteOrFallback()
        {
            if (particleSprite != null) return particleSprite;
            if (fallbackParticleSprite != null) return fallbackParticleSprite;
            const int size = 16;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];
            float mid = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(mid, mid)) / (mid + 0.01f);
                    float a = Mathf.Clamp01(1f - d);
                    a *= a;
                    pixels[y * size + x] = new Color(1f, 1f, 1f, a);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            tex.hideFlags = HideFlags.HideAndDontSave;
            fallbackParticleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            fallbackParticleSprite.hideFlags = HideFlags.HideAndDontSave;
            return fallbackParticleSprite;
        }

        private void EnsurePool()
        {
            if (pool != null && pool.Length > 0) return;
            var container = new GameObject("HoverParticles", typeof(RectTransform));
            container.transform.SetParent(transform, false);
            particleRoot = container.GetComponent<RectTransform>();
            particleRoot.anchorMin = Vector2.zero;
            particleRoot.anchorMax = Vector2.one;
            particleRoot.offsetMin = Vector2.zero;
            particleRoot.offsetMax = Vector2.zero;
            particleRoot.SetAsLastSibling();
            var group = container.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;

            var sprite = ParticleSpriteOrFallback();
            pool = new UIParticle[24];
            for (int i = 0; i < pool.Length; i++)
            {
                var go = new GameObject("P_" + i, typeof(RectTransform), typeof(Image));
                go.transform.SetParent(particleRoot, false);
                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
                var img = go.GetComponent<Image>();
                img.raycastTarget = false;
                img.preserveAspect = true;
                img.sprite = sprite;
                go.SetActive(false);
                pool[i] = new UIParticle { Rect = rt, Img = img };
            }
        }

        private void Spawn(Vector3 worldPos, Vector2 vel)
        {
            EnsurePool();
            if (particleRoot == null || pool == null) return;
            particleRoot.SetAsLastSibling();
            var sprite = ParticleSpriteOrFallback();
            Vector2 localPos = particleRoot.InverseTransformPoint(worldPos);
            for (int i = 0; i < pool.Length; i++)
            {
                if (pool[i].Active) continue;
                pool[i].Active = true;
                pool[i].Img.gameObject.SetActive(true);
                pool[i].Img.sprite = sprite;
                pool[i].Img.preserveAspect = true;
                float size = Random.Range(2.2f, 4.4f);
                pool[i].Rect.sizeDelta = new Vector2(size, size);
                pool[i].Pos = localPos;
                pool[i].Rect.anchoredPosition = localPos;
                pool[i].Vel = vel;
                pool[i].Life = 0f;
                pool[i].MaxLife = Random.Range(0.45f, 0.85f);
                pool[i].BaseAlpha = Random.Range(0.25f, 1f);
                Color c = Color.Lerp(new Color(0.29f, 0.88f, 0.95f), Color.white, Random.value * 0.35f);
                pool[i].Img.color = new Color(c.r, c.g, c.b, pool[i].BaseAlpha);
                return;
            }
        }

        private void UpdateParticles(float dt)
        {
            if (pool == null) return;
            int active = 0;
            for (int i = 0; i < pool.Length; i++)
            {
                if (!pool[i].Active) continue;
                pool[i].Life += dt;
                if (pool[i].Life >= pool[i].MaxLife)
                {
                    pool[i].Active = false;
                    pool[i].Img.gameObject.SetActive(false);
                    continue;
                }

                pool[i].Pos += pool[i].Vel * dt;
                pool[i].Vel.y += 12f * dt;
                pool[i].Rect.anchoredPosition = pool[i].Pos;
                float fade = Mathf.Lerp(pool[i].BaseAlpha, 0f, pool[i].Life / pool[i].MaxLife);
                var c = pool[i].Img.color;
                pool[i].Img.color = new Color(c.r, c.g, c.b, fade);
                active++;
            }

            ActiveParticleCount = active;
        }

        private void ClearParticles()
        {
            if (pool == null) return;
            for (int i = 0; i < pool.Length; i++)
            {
                if (!pool[i].Active) continue;
                pool[i].Active = false;
                if (pool[i].Img != null) pool[i].Img.gameObject.SetActive(false);
            }

            ActiveParticleCount = 0;
        }
    }
}

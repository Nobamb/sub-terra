using UnityEngine;
using UnityEngine.SceneManagement;

namespace SubTerra.Gameplay.Player
{
    /// <summary>
    /// B-211: 착지한 발밑 양옆으로 흙먼지 몇 알이 퍼지며 살짝 떠올랐다가 사라진다.
    /// 그림 파일 없이 부드러운 원 스프라이트를 코드로 만들고, 정해진 개수만 돌려 써 연속 착지에도 쌓이지 않는다.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerMovement))]
    public sealed class PlayerLandingDust : MonoBehaviour
    {
        private const int DotTextureSize = 16;
        private static Sprite dotSprite;

        [SerializeField] private Color dustColor = new(0x6B / 255f, 0x56 / 255f, 0x44 / 255f, 0.55f);
        [SerializeField, Range(0.1f, 0.35f)] private float lifetimeSeconds = 0.32f;
        [SerializeField, Range(4, 32)] private int maxParticles = 24;
        [Tooltip("최대 크기 먼지 한 알의 지름(칸).")]
        [SerializeField, Min(0.01f)] private float particleSize = 0.17f;
        [SerializeField, Min(0f)] private float spreadSpeed = 2.4f;
        [SerializeField, Min(0f)] private float riseSpeed = 0.6f;
        [Tooltip("발 가운데에서 먼지가 생기는 가로 거리(칸). 장화 폭보다 밖에서 생겨야 캐릭터 그림에 가리지 않는다.")]
        [SerializeField, Min(0f)] private float spawnHalfWidth = 0.15f;

        private struct Particle
        {
            public SpriteRenderer Renderer;
            public Vector2 Position;
            public Vector2 Velocity;
            public float Age;
            public float Life;
            public float Size;
            public bool Alive;
        }

        private PlayerMovement movement;
        private SpriteRenderer characterRenderer;
        private Transform poolRoot;
        private Particle[] particles;
        private int nextSlot;
        private uint randomState = 0x9E3779B9u;
        private bool subscribed;

        public int ActiveCount
        {
            get
            {
                int count = 0;
                if (particles == null) return 0;
                for (int i = 0; i < particles.Length; i++)
                {
                    if (particles[i].Alive) count++;
                }

                return count;
            }
        }

        public int PoolSize => particles != null ? particles.Length : 0;
        public int LastBurstCount { get; private set; }
        public PlayerLandingDustSize LastBurstSize { get; private set; }

        private void Awake()
        {
            movement = GetComponent<PlayerMovement>();
            var animation = GetComponentInChildren<PlayerAnimationController>(true);
            characterRenderer = animation != null
                ? animation.GetComponent<SpriteRenderer>()
                : GetComponentInChildren<SpriteRenderer>(true);
        }

        private void OnEnable()
        {
            if (movement != null && !subscribed)
            {
                movement.Landed += OnLanded;
                subscribed = true;
            }
        }

        private void OnDisable()
        {
            if (movement != null && subscribed)
            {
                movement.Landed -= OnLanded;
            }

            subscribed = false;
            HideAll();
        }

        private void OnDestroy()
        {
            if (poolRoot != null)
            {
                if (Application.isPlaying) Destroy(poolRoot.gameObject);
                else DestroyImmediate(poolRoot.gameObject);
                poolRoot = null;
            }
        }

        private void OnLanded(PlayerLandingInfo landing)
        {
            var size = PlayerLandingFeedbackRules.ResolveDustSize(landing);
            int count = PlayerLandingFeedbackRules.ResolveDustCount(size, landing);
            LastBurstSize = size;
            LastBurstCount = count;
            if (count <= 0 || !movement.TryGetBodyBounds(out var bounds))
            {
                return;
            }

            EnsurePool();
            if (particles == null) return;

            float tier = size switch
            {
                PlayerLandingDustSize.Small => 0.65f,
                PlayerLandingDustSize.Medium => 0.82f,
                _ => 1f
            };
            var foot = new Vector2(bounds.center.x, bounds.min.y + 0.03f);
            for (int i = 0; i < count; i++)
            {
                float side = (i & 1) == 0 ? -1f : 1f;
                float lane = (i >> 1) / Mathf.Max(1f, (count - 1) >> 1);
                ref var p = ref particles[nextSlot];
                nextSlot = (nextSlot + 1) % particles.Length;
                p.Position = foot + new Vector2(side * (spawnHalfWidth + 0.06f * Next01()), 0.01f + 0.03f * Next01());
                p.Velocity = new Vector2(
                    side * spreadSpeed * tier * Mathf.Lerp(0.45f, 1f, Mathf.Clamp01(lane * 0.6f + Next01() * 0.4f)),
                    riseSpeed * tier * Mathf.Lerp(0.5f, 1.1f, Next01()));
                p.Age = 0f;
                p.Life = lifetimeSeconds * Mathf.Lerp(0.75f, 1f, Next01());
                p.Size = particleSize * tier * Mathf.Lerp(0.75f, 1.1f, Next01());
                p.Alive = true;
                ApplyVisual(ref p);
            }
        }

        private void LateUpdate()
        {
            if (particles == null)
            {
                return;
            }

            float dt = Time.deltaTime;
            for (int i = 0; i < particles.Length; i++)
            {
                ref var p = ref particles[i];
                if (!p.Alive) continue;
                p.Age += dt;
                if (p.Age >= p.Life)
                {
                    p.Alive = false;
                    if (p.Renderer != null) p.Renderer.enabled = false;
                    continue;
                }

                // 옆으로는 빨리 멈추고, 위로는 조금 떠오르다 멈춘다.
                p.Velocity.x *= Mathf.Exp(-6f * dt);
                p.Velocity.y *= Mathf.Exp(-5f * dt);
                p.Position += p.Velocity * dt;
                ApplyVisual(ref p);
            }
        }

        private void ApplyVisual(ref Particle p)
        {
            if (p.Renderer == null) return;
            float t = Mathf.Clamp01(p.Age / Mathf.Max(0.0001f, p.Life));
            float grow = 1f - (1f - t) * (1f - t);
            var transformRef = p.Renderer.transform;
            transformRef.position = new Vector3(p.Position.x, p.Position.y, transform.position.z);
            float scale = p.Size * (0.7f + 0.6f * grow);
            transformRef.localScale = new Vector3(scale, scale, 1f);
            var color = dustColor;
            color.a *= 1f - Edge(0.2f, 1f, t);
            p.Renderer.color = color;
            p.Renderer.enabled = true;
        }

        private void EnsurePool()
        {
            if (particles != null && poolRoot != null)
            {
                return;
            }

            var sprite = DotSprite();
            var root = new GameObject("PlayerLandingDust");
            if (gameObject.scene.IsValid())
            {
                SceneManager.MoveGameObjectToScene(root, gameObject.scene);
            }

            poolRoot = root.transform;
            particles = new Particle[Mathf.Clamp(maxParticles, 4, 32)];
            int layerId = characterRenderer != null ? characterRenderer.sortingLayerID : 0;
            // 캐릭터와 같은 재질이라 광산 조명도 똑같이 받는다.
            var material = characterRenderer != null ? characterRenderer.sharedMaterial : null;
            // 캐릭터 바로 뒤, 지형 위.
            int order = characterRenderer != null ? characterRenderer.sortingOrder - 1 : 4;
            for (int i = 0; i < particles.Length; i++)
            {
                var dot = new GameObject("Dust" + i);
                dot.transform.SetParent(poolRoot, false);
                var renderer = dot.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                if (material != null) renderer.sharedMaterial = material;
                renderer.sortingLayerID = layerId;
                renderer.sortingOrder = order;
                renderer.enabled = false;
                particles[i].Renderer = renderer;
            }
        }

        private void HideAll()
        {
            if (particles == null) return;
            for (int i = 0; i < particles.Length; i++)
            {
                particles[i].Alive = false;
                if (particles[i].Renderer != null) particles[i].Renderer.enabled = false;
            }
        }

        // 결정적 xorshift. 같은 착지 순서면 같은 모양이다.
        private float Next01()
        {
            randomState ^= randomState << 13;
            randomState ^= randomState >> 17;
            randomState ^= randomState << 5;
            return (randomState & 0xFFFFFF) / (float)0x1000000;
        }

        // 0(edge0 이하) → 1(edge1 이상) 부드러운 계단. Mathf.SmoothStep(from, to, t)은 from~to 보간이라 쓰지 않는다.
        private static float Edge(float edge0, float edge1, float x)
        {
            float u = Mathf.Clamp01((x - edge0) / (edge1 - edge0));
            return u * u * (3f - 2f * u);
        }

        private static Sprite DotSprite()
        {
            if (dotSprite != null)
            {
                return dotSprite;
            }

            var texture = new Texture2D(DotTextureSize, DotTextureSize, TextureFormat.RGBA32, false)
            {
                name = "PlayerLandingDustDot",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave
            };
            var pixels = new Color32[DotTextureSize * DotTextureSize];
            float radius = DotTextureSize * 0.5f;
            for (int y = 0; y < DotTextureSize; y++)
            {
                for (int x = 0; x < DotTextureSize; x++)
                {
                    float dx = x + 0.5f - radius;
                    float dy = y + 0.5f - radius;
                    float d = Mathf.Sqrt(dx * dx + dy * dy) / radius;
                    // 가운데는 진하고 가장자리는 부드럽게 사라지는 원.
                    float alpha = 1f - Edge(0.45f, 1f, d);
                    pixels[y * DotTextureSize + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            dotSprite = Sprite.Create(texture, new Rect(0f, 0f, DotTextureSize, DotTextureSize),
                new Vector2(0.5f, 0.5f), DotTextureSize);
            dotSprite.name = "PlayerLandingDustDot";
            dotSprite.hideFlags = HideFlags.DontSave;
            return dotSprite;
        }
    }
}

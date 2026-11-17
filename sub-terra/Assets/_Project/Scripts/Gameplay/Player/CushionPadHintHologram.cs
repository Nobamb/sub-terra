using System;
using SubTerra.Shared;
using SubTerra.Shared.Audio;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SubTerra.Gameplay.Player
{
    /// <summary>
    /// 완충판 안내 홀로그램을 광산 입장마다 한 번만 보이게 하는 세션 상태.
    /// 씬이 새로 로드될 때(광산 입장·재입장) 초기화한다. 같은 입장 안에서는 다시 올라가도 나오지 않는다.
    /// </summary>
    public static class CushionPadHintSession
    {
        public static bool Consumed { get; private set; }

        public static void Consume() => Consumed = true;

        public static void Reset() => Consumed = false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Initialize()
        {
            Consumed = false;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode == LoadSceneMode.Single) Reset();
        }
    }

    /// <summary>
    /// 힌트를 언제 보일지만 정하는 순수 로직. 판 위에 잠깐 서 있은 뒤 입장당 한 번 보이고,
    /// 높이를 바꾸거나 판에서 내려오면 사라진다.
    /// </summary>
    public sealed class CushionPadHintLogic
    {
        public const float StandDelaySeconds = 0.35f;

        private float standSeconds;

        public bool Visible { get; private set; }

        /// <returns>이번 호출에서 처음 나타났으면 true(소리용).</returns>
        public bool Tick(bool onPad, float deltaTime)
        {
            if (!onPad)
            {
                standSeconds = 0f;
                Visible = false;
                return false;
            }

            if (Visible || CushionPadHintSession.Consumed) return false;
            standSeconds += Mathf.Max(0f, deltaTime);
            if (standSeconds < StandDelaySeconds) return false;

            Visible = true;
            CushionPadHintSession.Consume();
            return true;
        }

        /// <summary>높이를 한 번 바꾸면 스스로 사라지고 이번 입장에서는 다시 나오지 않는다.</summary>
        public void NotifyAdjusted()
        {
            Visible = false;
            CushionPadHintSession.Consume();
        }
    }

    /// <summary>
    /// 캐릭터 위(↑)·아래(↓)에 입체적인 청록색 방향키 홀로그램을 띄운다. 그림은 코드로 그린 키캡이며
    /// 프리팹·씬을 건드리지 않는다. 모션 감소면 누름 애니메이션 없이 정지 그림만 보인다.
    /// </summary>
    public sealed class CushionPadHintHologram : MonoBehaviour
    {
        private const int KeySize = 40;
        private const float KeyPixelsPerUnit = 80f; // 40px / 80 = 0.5 유닛(기존 크기 유지)
        private const float CycleSeconds = 1.6f;
        private const float PressSeconds = 0.4f;
        private const float PressDepth = 0.05f;
        private const float AboveGap = 0.45f;
        private const float BelowGap = 0.4f;
        private const int SortingOrder = 40;

        // 누름 파티클(청록 점). 키를 누르는 순간 키 테두리에서 바깥으로 튀며 사라진다.
        private const int ParticlesPerPress = 8;
        private const int ParticlePoolSize = 24;
        private const float ParticleLifeMin = 0.35f;
        private const float ParticleLifeMax = 0.55f;
        private const float ParticleSpeedMin = 0.55f;
        private const float ParticleSpeedMax = 1.1f;
        private const float ParticleDrag = 2.5f;
        private const float ParticleRimX = 0.2f;
        private const float ParticleRimY = 0.16f;
        private static readonly Color ParticleStartColor = new Color(0.78f, 1f, 1f, 1f);
        private static readonly Color ParticleEndColor = new Color(0.2f, 0.9f, 0.88f, 0f);

        // 위·아래 키와 파티클은 반드시 한 장의 텍스처(아틀라스)를 공유한다.
        // 같은 머티리얼의 스프라이트가 서로 다른 텍스처를 쓰면 한 배치로 묶여 한쪽 그림으로 통일돼 보인다.
        private const int MoteSize = 4;
        private const int AtlasWidth = KeySize * 2 + 16;

        private static Sprite upSprite;
        private static Sprite downSprite;
        private static Sprite moteSprite;
        // 키마다 머티리얼을 따로 둬서 서로 다른 그림이 한 배치로 묶일 여지를 없앤다.
        private static Material upMaterial;
        private static Material downMaterial;
        private static Material moteMaterial;

        private sealed class Mote
        {
            public SpriteRenderer Renderer;
            public Vector2 Position;
            public Vector2 Velocity;
            public float Age;
            public float Life;
            public float Scale;
            public bool Alive;
        }

        private readonly CushionPadHintLogic logic = new CushionPadHintLogic();
        private readonly System.Collections.Generic.List<Mote> motes = new System.Collections.Generic.List<Mote>();
        private readonly System.Random random = new System.Random(199);
        private PlayerMovement movement;
        private Transform root;
        private SpriteRenderer upRenderer;
        private SpriteRenderer downRenderer;
        private bool upWasPressed;
        private bool downWasPressed;

        public bool IsVisible => logic.Visible;
        public Transform KeyRoot => root;
        public SpriteRenderer UpKey => upRenderer;
        public SpriteRenderer DownKey => downRenderer;
        public int ActiveParticleCount { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            upSprite = null;
            downSprite = null;
            moteSprite = null;
            upMaterial = null;
            downMaterial = null;
            moteMaterial = null;
        }

        private void Awake()
        {
            movement = GetComponent<PlayerMovement>();
        }

        private void OnEnable()
        {
            if (movement == null) movement = GetComponent<PlayerMovement>();
            if (movement != null) movement.PlatformHeightAdjusted += OnAdjusted;
        }

        private void OnDisable()
        {
            if (movement != null) movement.PlatformHeightAdjusted -= OnAdjusted;
            Hide();
        }

        private void OnDestroy()
        {
            if (root != null) Destroy(root.gameObject);
        }

        private void LateUpdate()
        {
            try
            {
                Advance(Time.unscaledDeltaTime, Time.unscaledTime);
            }
            catch (Exception exception)
            {
                Debug.LogError("[CushionPadHint] 갱신 실패: " + exception.Message, this);
                enabled = false;
            }
        }

        /// <summary>테스트가 시간을 직접 넣을 수 있게 분리한 한 틱.</summary>
        public void Advance(float deltaTime, float time)
        {
            bool onPad = movement != null && movement.IsOnAdjustablePlatform
                && !ControlPreferences.IsSettingsOpen;
            if (logic.Tick(onPad, deltaTime))
            {
                SfxRequests.Play(SfxId.KeysHoloOpen);
            }

            if (!logic.Visible)
            {
                Hide();
                return;
            }

            EnsureBuilt();
            root.gameObject.SetActive(true);
            ApplyPress(time);
            UpdateParticles(deltaTime);
        }

        private void OnAdjusted(float delta)
        {
            logic.NotifyAdjusted();
            Hide();
        }

        private void Hide()
        {
            if (root == null || !root.gameObject.activeSelf) return;
            // 숨길 때 남은 파티클도 정리해, 다시 나타날 때 옛 점이 보이지 않게 한다.
            for (int i = 0; i < motes.Count; i++) KillMote(motes[i]);
            ActiveParticleCount = 0;
            upWasPressed = false;
            downWasPressed = false;
            root.gameObject.SetActive(false);
        }

        /// <summary>캐릭터 위·아래에 키를 놓고, 누름 애니메이션(모션 감소면 생략)을 적용한다.</summary>
        private void ApplyPress(float time)
        {
            if (!movement.TryGetBodyBounds(out Bounds bounds)) return;
            bool upPressed = false;
            bool downPressed = false;
            if (!AccessibilityPreferences.ReduceMotion)
            {
                float phase = Mathf.Repeat(time, CycleSeconds);
                upPressed = phase < PressSeconds;
                downPressed = phase >= CycleSeconds * 0.5f && phase < CycleSeconds * 0.5f + PressSeconds;
            }

            SetPressed(upRenderer, bounds.center.x, bounds.max.y + AboveGap, upPressed);
            SetPressed(downRenderer, bounds.center.x, bounds.min.y - BelowGap, downPressed);

            // 눌리는 순간(안 눌림 → 눌림)에만 한 번 터뜨린다. 모션 감소면 눌림이 없어 파티클도 없다.
            if (upPressed && !upWasPressed) EmitBurst(upRenderer.transform.position, 1f);
            if (downPressed && !downWasPressed) EmitBurst(downRenderer.transform.position, -1f);
            upWasPressed = upPressed;
            downWasPressed = downPressed;
        }

        /// <summary>키 테두리에서 바깥(위 키는 위쪽, 아래 키는 아래쪽)으로 청록 점을 튀긴다.</summary>
        private void EmitBurst(Vector3 center, float side)
        {
            for (int i = 0; i < ParticlesPerPress; i++)
            {
                Mote mote = RentMote();
                if (mote == null) return;

                float angle = (float)(random.NextDouble() * Math.PI);
                var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle) * side);
                float speed = Mathf.Lerp(ParticleSpeedMin, ParticleSpeedMax, (float)random.NextDouble());
                mote.Position = new Vector2(center.x + direction.x * ParticleRimX, center.y + direction.y * ParticleRimY);
                mote.Velocity = direction * speed;
                mote.Life = Mathf.Lerp(ParticleLifeMin, ParticleLifeMax, (float)random.NextDouble());
                mote.Scale = Mathf.Lerp(0.8f, 1.6f, (float)random.NextDouble());
                mote.Age = 0f;
                mote.Alive = true;
                ApplyMote(mote);
                mote.Renderer.gameObject.SetActive(true);
            }
        }

        private Mote RentMote()
        {
            for (int i = 0; i < motes.Count; i++)
            {
                if (!motes[i].Alive) return motes[i];
            }

            if (motes.Count >= ParticlePoolSize) return null;
            var moteObject = new GameObject("KeyParticle");
            moteObject.transform.SetParent(root, false);
            var renderer = moteObject.AddComponent<SpriteRenderer>();
            renderer.sprite = moteSprite;
            renderer.sharedMaterial = moteMaterial;
            renderer.sortingOrder = SortingOrder + 1;
            moteObject.SetActive(false);
            var mote = new Mote { Renderer = renderer };
            motes.Add(mote);
            return mote;
        }

        private void UpdateParticles(float deltaTime)
        {
            int alive = 0;
            float dt = Mathf.Max(0f, deltaTime);
            for (int i = 0; i < motes.Count; i++)
            {
                Mote mote = motes[i];
                if (!mote.Alive) continue;
                mote.Age += dt;
                if (mote.Age >= mote.Life)
                {
                    KillMote(mote);
                    continue;
                }

                mote.Position += mote.Velocity * dt;
                mote.Velocity *= Mathf.Max(0f, 1f - ParticleDrag * dt);
                ApplyMote(mote);
                alive++;
            }

            ActiveParticleCount = alive;
        }

        private static void ApplyMote(Mote mote)
        {
            float t = Mathf.Clamp01(mote.Age / mote.Life);
            mote.Renderer.transform.position = new Vector3(mote.Position.x, mote.Position.y, 0f);
            mote.Renderer.transform.localScale = Vector3.one * (mote.Scale * (1f - 0.5f * t));
            Color color = Color.Lerp(ParticleStartColor, ParticleEndColor, t);
            color.a = (1f - t) * (1f - t);
            mote.Renderer.color = color;
        }

        private static void KillMote(Mote mote)
        {
            mote.Alive = false;
            if (mote.Renderer != null) mote.Renderer.gameObject.SetActive(false);
        }

        private static void SetPressed(SpriteRenderer renderer, float x, float y, bool pressed)
        {
            // 눌린 키는 살짝 내려가고 밝아진다. 모션 감소 때는 항상 평상시 모습이다.
            renderer.transform.position = new Vector3(x, y - (pressed ? PressDepth : 0f), 0f);
            renderer.color = pressed ? Color.white : new Color(0.85f, 0.85f, 0.85f, 0.9f);
        }

        private void EnsureBuilt()
        {
            if (root != null) return;
            EnsureSprites();
            root = new GameObject("CushionPadHint").transform;
            root.SetParent(null, false);
            upRenderer = CreateKey("KeyUp", upSprite, upMaterial);
            downRenderer = CreateKey("KeyDown", downSprite, downMaterial);
        }

        private SpriteRenderer CreateKey(string name, Sprite sprite, Material keyMaterial)
        {
            var keyObject = new GameObject(name);
            keyObject.transform.SetParent(root, false);
            var renderer = keyObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sharedMaterial = keyMaterial;
            renderer.sortingOrder = SortingOrder;
            return renderer;
        }

        private static void EnsureSprites()
        {
            if (upSprite != null && downSprite != null && moteSprite != null
                && upMaterial != null && downMaterial != null && moteMaterial != null) return;
            BuildAtlasSprites();
            var shader = Shader.Find("Sprites/Default");
            upMaterial = CreateMaterial(shader, "CushionPadHintUp");
            downMaterial = CreateMaterial(shader, "CushionPadHintDown");
            moteMaterial = CreateMaterial(shader, "CushionPadHintMote");
        }

        // 키캡 모양(팔각 윗면·옆면·색)은 Shared의 HologramKeycapPainter와 긴급탈출 R키가 함께 쓴다.
        // 화살표 글리프: 높이 14px, 머리 9줄 + 줄기 5줄. 글리프 y 0이 아래, 13이 꼭짓점 쪽이다.
        private const int GlyphBottomY = 15;
        private const int GlyphHeight = 14;
        private const int GlyphHeadRows = 9;

        private static Material CreateMaterial(Shader shader, string name)
        {
            return shader != null ? new Material(shader) { name = name } : null;
        }

        /// <summary>
        /// 청록색 홀로그램 키캡. 모서리를 크게 깎은 팔각 윗면 + 아래로 두께가 보이는 옆면,
        /// 좌상단은 밝고 우하단은 어두운 모따기 면, 안쪽으로 파인 판, 좌우 면이 나뉜 입체 화살표.
        /// 위 키는 꼭짓점이 위로, 아래 키는 꼭짓점이 아래로 향한다.
        /// </summary>
        private static void BuildAtlasSprites()
        {
            var texture = new Texture2D(AtlasWidth, KeySize, TextureFormat.RGBA32, false)
            {
                name = "CushionPadHintAtlas",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            // 왼쪽 40px 위 키, 그 오른쪽 40px 아래 키, 맨 끝에 파티클 점.
            var pixels = new Color32[AtlasWidth * KeySize];
            for (int y = 0; y < KeySize; y++)
            {
                for (int x = 0; x < KeySize; x++)
                {
                    pixels[y * AtlasWidth + x] = ShadePixel(x, y, pointUp: true);
                    pixels[y * AtlasWidth + KeySize + x] = ShadePixel(x, y, pointUp: false);
                }
            }

            // 모서리를 깎은 4x4 점.
            var white = new Color32(255, 255, 255, 255);
            int moteX = KeySize * 2 + 4;
            for (int y = 0; y < MoteSize; y++)
            {
                for (int x = 0; x < MoteSize; x++)
                {
                    bool corner = (x == 0 || x == MoteSize - 1) && (y == 0 || y == MoteSize - 1);
                    if (!corner) pixels[y * AtlasWidth + moteX + x] = white;
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            var half = new Vector2(0.5f, 0.5f);
            upSprite = Sprite.Create(texture, new Rect(0, 0, KeySize, KeySize), half, KeyPixelsPerUnit, 0, SpriteMeshType.FullRect);
            downSprite = Sprite.Create(texture, new Rect(KeySize, 0, KeySize, KeySize), half, KeyPixelsPerUnit, 0, SpriteMeshType.FullRect);
            moteSprite = Sprite.Create(texture, new Rect(moteX, 0, MoteSize, MoteSize), half, KeyPixelsPerUnit, 0, SpriteMeshType.FullRect);
        }

        private static Color32 ShadePixel(int x, int y, bool pointUp)
        {
            HologramPixel pixel = HologramKeycapPainter.Shade(x, y, (gx, gy) => GlyphLevel(gx, gy, pointUp));
            return new Color32(pixel.R, pixel.G, pixel.B, pixel.A);
        }

        /// <summary>0 없음, 1 글리프 둘레 빛, 2 글리프 왼쪽(밝은 면), 3 글리프 오른쪽(어두운 면).</summary>
        private static int GlyphLevel(int x, int y, bool pointUp)
        {
            if (InGlyph(x, y, pointUp)) return x + 0.5f < KeySize * 0.5f ? 2 : 3;
            for (int oy = -1; oy <= 1; oy++)
            {
                for (int ox = -1; ox <= 1; ox++)
                {
                    if ((ox != 0 || oy != 0) && InGlyph(x + ox, y + oy, pointUp)) return 1;
                }
            }

            return 0;
        }

        /// <summary>머리(삼각형)와 줄기(사각형)로 된 화살표. 위 키는 꼭짓점이 위, 아래 키는 꼭짓점이 아래.</summary>
        private static bool InGlyph(int x, int y, bool pointUp)
        {
            int row = y - GlyphBottomY;
            if (row < 0 || row >= GlyphHeight) return false;
            if (!pointUp) row = GlyphHeight - 1 - row;

            float half;
            if (row >= GlyphHeight - GlyphHeadRows) half = 0.5f + (GlyphHeight - 1 - row) * 0.75f;
            else half = 2.5f;
            return Mathf.Abs(x + 0.5f - KeySize * 0.5f) <= half;
        }
    }
}

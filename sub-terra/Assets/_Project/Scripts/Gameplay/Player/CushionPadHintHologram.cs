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

        // 키캡 그림 치수(텍스처 좌표, y가 위쪽). 윗면 위에 두께(Depth)만큼 내려간 옆면이 붙는다.
        private const int FaceX0 = 3;
        private const int FaceX1 = 36;
        private const int FaceY0 = 8;
        private const int FaceY1 = 36;
        private const int Depth = 5;
        private const int Chamfer = 6;
        // 화살표 글리프: 높이 14px, 머리 9줄 + 줄기 5줄. 글리프 y 0이 아래, 13이 꼭짓점 쪽이다.
        private const int GlyphBottomY = 15;
        private const int GlyphHeight = 14;
        private const int GlyphHeadRows = 9;

        /// <summary>
        /// 청록색 홀로그램 키캡. 모서리를 크게 깎은 팔각 윗면 + 아래로 두께가 보이는 옆면,
        /// 좌상단은 밝고 우하단은 어두운 모따기 면, 안쪽으로 파인 판, 좌우 면이 나뉜 입체 화살표.
        /// 위 키는 꼭짓점이 위로, 아래 키는 꼭짓점이 아래로 향한다.
        /// </summary>
        private static Material CreateMaterial(Shader shader, string name)
        {
            return shader != null ? new Material(shader) { name = name } : null;
        }

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
            if (InsideCap(x, y, FaceY0, FaceY1)) return ShadeTopFace(x, y, pointUp);
            if (InsideCap(x, y, FaceY0 - Depth, FaceY1 - Depth)) return ShadeSide(x, y);
            return new Color32(0, 0, 0, 0);
        }

        /// <summary>모서리를 45도로 깎은 팔각형 안쪽인지.</summary>
        private static bool InsideCap(int x, int y, int y0, int y1)
        {
            if (x < FaceX0 || x > FaceX1 || y < y0 || y > y1) return false;
            int dx = Mathf.Min(x - FaceX0, FaceX1 - x);
            int dy = Mathf.Min(y - y0, y1 - y);
            return dx + dy >= Chamfer;
        }

        private static Color32 ShadeSide(int x, int y)
        {
            // 윗면 바로 아래로 보이는 두께. 아래 가장자리는 빛나는 선, 오른쪽으로 갈수록 어둡다.
            if (!InsideCap(x, y - 1, FaceY0 - Depth, FaceY1 - Depth)) return new Color32(48, 208, 214, 255);
            float t = Mathf.InverseLerp(FaceX0, FaceX1, x);
            return LerpColor(new Color32(16, 118, 132, 238), new Color32(6, 62, 78, 238), t);
        }

        private static Color32 ShadeTopFace(int x, int y, bool pointUp)
        {
            int dl = x - FaceX0;
            int dr = FaceX1 - x;
            int db = y - FaceY0;
            int dt = FaceY1 - y;
            const float diagonal = 0.7071f;
            // 가장자리까지 거리와 가장 가까운 면(0 위·1 왼·2 아래·3 오른·4 좌상·5 우상·6 좌하·7 우하).
            float[] distances =
            {
                dt, dl, db, dr,
                (dl + dt - Chamfer) * diagonal, (dr + dt - Chamfer) * diagonal,
                (dl + db - Chamfer) * diagonal, (dr + db - Chamfer) * diagonal
            };
            int nearest = 0;
            for (int i = 1; i < distances.Length; i++)
            {
                if (distances[i] < distances[nearest]) nearest = i;
            }

            float edge = distances[nearest];
            if (edge < 1f) return new Color32(72, 246, 240, 255);
            if (edge < 3.5f)
            {
                // 모따기 면 명암: 위·왼쪽은 밝고 아래·오른쪽은 어둡다. 우상·좌하 모서리는 중간.
                if (nearest == 0 || nearest == 1 || nearest == 4) return new Color32(64, 198, 202, 248);
                if (nearest == 2 || nearest == 3 || nearest == 7) return new Color32(14, 92, 108, 248);
                return new Color32(38, 146, 156, 248);
            }

            if (edge < 4.5f) return new Color32(4, 40, 50, 240);

            // 안쪽 판: 위가 약간 밝은 세로 그라데이션 + 가로 주사선(홀로그램 느낌).
            float t = Mathf.InverseLerp(FaceY0, FaceY1, y);
            Color32 face = LerpColor(new Color32(8, 54, 64, 216), new Color32(18, 92, 104, 216), t);
            if (y % 3 == 0) face.a = (byte)(face.a * 0.82f);

            switch (GlyphLevel(x, y, pointUp))
            {
                case 2: return new Color32(206, 255, 250, 255);
                case 3: return new Color32(112, 216, 220, 255);
                case 1: return Over(face, new Color32(40, 232, 226, 120));
                default: return face;
            }
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

        private static Color32 LerpColor(Color32 a, Color32 b, float t)
        {
            return new Color32(
                (byte)Mathf.RoundToInt(Mathf.Lerp(a.r, b.r, t)),
                (byte)Mathf.RoundToInt(Mathf.Lerp(a.g, b.g, t)),
                (byte)Mathf.RoundToInt(Mathf.Lerp(a.b, b.b, t)),
                (byte)Mathf.RoundToInt(Mathf.Lerp(a.a, b.a, t)));
        }

        /// <summary>straight alpha 두 색을 알파 가중으로 합성한다(색만 평균 내면 경계가 튄다).</summary>
        private static Color32 Over(Color32 dst, Color32 src)
        {
            float sa = src.a / 255f;
            float da = dst.a / 255f;
            float oa = sa + da * (1f - sa);
            if (oa <= 0f) return new Color32(0, 0, 0, 0);
            float r = (src.r * sa + dst.r * da * (1f - sa)) / oa;
            float g = (src.g * sa + dst.g * da * (1f - sa)) / oa;
            float b = (src.b * sa + dst.b * da * (1f - sa)) / oa;
            return new Color32((byte)Mathf.RoundToInt(r), (byte)Mathf.RoundToInt(g), (byte)Mathf.RoundToInt(b), (byte)Mathf.RoundToInt(oa * 255f));
        }
    }
}

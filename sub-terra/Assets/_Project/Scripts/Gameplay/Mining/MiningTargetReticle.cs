using SubTerra.Shared;
using UnityEngine;

namespace SubTerra.Gameplay.Mining
{
    public enum MiningTargetSource
    {
        Keyboard = 0,
        Pointer = 1
    }

    /// <summary>
    /// 과녁이 키보드 목표와 마우스 호버 중 어느 쪽을 따를지 정하는 순수 로직.
    /// 가장 최근 입력이 이긴다. 걷기만으로는 바뀌지 않는다.
    /// </summary>
    public sealed class MiningTargetPriority
    {
        public MiningTargetSource Source { get; private set; } = MiningTargetSource.Keyboard;

        /// <summary>엔터를 새로 누르거나 채굴 방향키를 새로 눌렀을 때.</summary>
        public void NoteKeyboardInput() => Source = MiningTargetSource.Keyboard;

        /// <summary>마우스가 움직이거나 버튼을 눌렀을 때.</summary>
        public void NotePointerInput() => Source = MiningTargetSource.Pointer;

        public void Reset() => Source = MiningTargetSource.Keyboard;
    }

    /// <summary>
    /// 과녁의 위치(목표 변경 시 미끄러짐)와 조임 정도를 계산하는 순수 로직.
    /// ReduceMotion이면 미끄러짐·조임 없이 목표 칸에 바로 붙은 정지 꺾쇠다.
    /// </summary>
    public sealed class MiningReticleMotion
    {
        public const float SlideSeconds = 0.1f;
        public const float PinchPeriodSeconds = 1.8f;
        public const float PinchDepth = 0.05f;

        private Vector2 from;
        private Vector2 to;
        private float slideElapsed = SlideSeconds;
        private float pinchTime;

        public bool Visible { get; private set; }
        public Vector2 Position { get; private set; }
        public Vector2 Target => to;
        /// <summary>꺾쇠가 칸 안쪽으로 조여든 거리(월드 단위).</summary>
        public float Inset { get; private set; }

        public void SetTarget(Vector2 center, bool reduceMotion)
        {
            if (!Visible || reduceMotion)
            {
                Snap(center);
                Visible = true;
                return;
            }

            if ((center - to).sqrMagnitude < 0.0001f) return;
            from = Position;
            to = center;
            slideElapsed = 0f;
        }

        public void Hide()
        {
            Visible = false;
            Inset = 0f;
            pinchTime = 0f;
            slideElapsed = SlideSeconds;
        }

        public void Tick(float deltaTime, bool reduceMotion)
        {
            if (!Visible) return;
            float dt = Mathf.Max(0f, deltaTime);
            if (reduceMotion)
            {
                Snap(to);
                Inset = 0f;
                pinchTime = 0f;
                return;
            }

            if (slideElapsed < SlideSeconds)
            {
                slideElapsed = Mathf.Min(SlideSeconds, slideElapsed + dt);
                float t = Mathf.SmoothStep(0f, 1f, slideElapsed / SlideSeconds);
                Position = Vector2.Lerp(from, to, t);
            }

            pinchTime += dt;
            Inset = PinchDepth * (0.5f - 0.5f * Mathf.Cos(pinchTime / PinchPeriodSeconds * Mathf.PI * 2f));
        }

        private void Snap(Vector2 center)
        {
            from = center;
            to = center;
            Position = center;
            slideElapsed = SlideSeconds;
        }
    }

    /// <summary>
    /// 캘 칸의 네 모서리에 얇은 청록 ㄱ자 꺾쇠를 그리는 월드 오브젝트 하나.
    /// 그림은 코드로 만든 한 장의 텍스처를 네 방향으로 뒤집어 쓰며, 프리팹·씬을 건드리지 않는다.
    /// 소리는 내지 않는다.
    /// </summary>
    public sealed class MiningTargetReticle : MonoBehaviour
    {
        // 지형(0~10대)·가스(20~23) 위, 건설 미리보기(49~50)와 균열(90~92)·스캔(94~95) 아래.
        public const int SortingOrder = 24;
        private const int ArmPixels = 8;
        private const int ThicknessPixels = 2;
        private const float PixelsPerUnit = 32f;
        private static readonly Color ReticleColor = new Color(0.3f, 0.95f, 0.92f, 0.8f);

        private static Sprite cornerSprite;
        private static Material cornerMaterial;

        private readonly MiningReticleMotion motion = new MiningReticleMotion();
        private readonly SpriteRenderer[] corners = new SpriteRenderer[4];
        private Transform root;
        private Vector2 cellSize = Vector2.one;

        public bool IsVisible => root != null && root.gameObject.activeSelf && motion.Visible;
        public Vector2 CurrentCenter => motion.Position;
        public Vector2 TargetCenter => motion.Target;
        public float Inset => motion.Inset;
        public SpriteRenderer[] Corners => corners;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            cornerSprite = null;
            cornerMaterial = null;
        }

        private void OnDisable() => Present(false, default, Vector2.one, 0f);

        private void OnDestroy()
        {
            if (root != null) Destroy(root.gameObject);
        }

        /// <summary>매 프레임 한 번. 목표가 없으면 숨기고, 있으면 그 칸 중심으로 보낸다.</summary>
        public void Present(bool hasTarget, Vector3 cellCenter, Vector2 size, float deltaTime)
        {
            if (!hasTarget)
            {
                motion.Hide();
                if (root != null && root.gameObject.activeSelf) root.gameObject.SetActive(false);
                return;
            }

            bool reduce = AccessibilityPreferences.ReduceMotion;
            EnsureBuilt();
            cellSize = new Vector2(Mathf.Max(0.01f, size.x), Mathf.Max(0.01f, size.y));
            motion.SetTarget(cellCenter, reduce);
            motion.Tick(deltaTime, reduce);
            if (!root.gameObject.activeSelf) root.gameObject.SetActive(true);
            Apply();
        }

        private void Apply()
        {
            Vector2 half = cellSize * 0.5f;
            Vector2 center = motion.Position;
            float inset = motion.Inset;
            float hx = Mathf.Max(0f, half.x - inset);
            float hy = Mathf.Max(0f, half.y - inset);
            // 0=좌하 1=우하 2=좌상 3=우상. 원본 그림은 좌하 모서리이고 나머지는 뒤집어 쓴다.
            for (int i = 0; i < corners.Length; i++)
            {
                bool right = (i & 1) != 0;
                bool top = (i & 2) != 0;
                Transform t = corners[i].transform;
                t.position = new Vector3(center.x + (right ? hx : -hx), center.y + (top ? hy : -hy), 0f);
                t.localScale = new Vector3(cellSize.x, cellSize.y, 1f);
            }
        }

        private void EnsureBuilt()
        {
            if (root != null) return;
            EnsureSprite();
            root = new GameObject("MiningTargetReticle").transform;
            root.SetParent(null, false);
            for (int i = 0; i < corners.Length; i++)
            {
                var corner = new GameObject("Corner" + i);
                corner.transform.SetParent(root, false);
                var renderer = corner.AddComponent<SpriteRenderer>();
                renderer.sprite = cornerSprite;
                renderer.sharedMaterial = cornerMaterial;
                renderer.color = ReticleColor;
                renderer.flipX = (i & 1) != 0;
                renderer.flipY = (i & 2) != 0;
                renderer.sortingOrder = SortingOrder;
                corners[i] = renderer;
            }

            root.gameObject.SetActive(false);
        }

        private static void EnsureSprite()
        {
            if (cornerSprite != null && cornerMaterial != null) return;
            var texture = new Texture2D(ArmPixels, ArmPixels, TextureFormat.RGBA32, false)
            {
                name = "MiningTargetReticleCorner",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            var pixels = new Color32[ArmPixels * ArmPixels];
            for (int y = 0; y < ArmPixels; y++)
            {
                for (int x = 0; x < ArmPixels; x++)
                {
                    bool arm = x < ThicknessPixels || y < ThicknessPixels;
                    pixels[y * ArmPixels + x] = arm ? new Color32(255, 255, 255, 255) : new Color32(0, 0, 0, 0);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            // 피벗을 좌하 모서리에 둬서 뒤집어도 칸의 모서리에 그대로 붙는다.
            cornerSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, ArmPixels, ArmPixels),
                Vector2.zero,
                PixelsPerUnit);
            Shader shader = Shader.Find("Sprites/Default");
            cornerMaterial = shader != null ? new Material(shader) { name = "MiningTargetReticle" } : null;
        }
    }
}

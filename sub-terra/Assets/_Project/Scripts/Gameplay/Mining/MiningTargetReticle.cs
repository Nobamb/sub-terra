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
    /// 과녁의 위치(목표 변경 시 미끄러짐), 조임, 등장(락온) 연출을 계산하는 순수 로직.
    /// ReduceMotion이면 미끄러짐·조임 없이 목표 칸에 바로 붙은 정지 꺾쇠다.
    /// 등장은 숨김→표시 때 한 번 재생하는 로컬 연출이라 ReduceMotion과 무관하게 재생한다.
    /// 등장: 꺾쇠가 멀리서 조여 들어오며 진해지고, 조준선은 조금 늦게 커진 채 돌며 맞춰진다.
    /// 재조준: 이미 보이는 중 칸이 바뀌면 미끄러짐 위에 더 작은 같은 모양의 맥동만 얹는다.
    /// </summary>
    public sealed class MiningReticleMotion
    {
        public const float SlideSeconds = 0.1f;
        public const float PinchPeriodSeconds = 1.8f;
        public const float PinchDepth = 0.05f;

        public const float AppearSeconds = 0.34f;
        /// <summary>등장 시작 때 꺾쇠가 칸 중심에서 벌어진 배수(1 = 칸 모서리).</summary>
        public const float AppearStartSpread = 1.85f;
        /// <summary>조준선은 등장 진행도의 이 비율만큼 늦게 시작한다.</summary>
        public const float CrosshairDelay = 0.15f;
        public const float CrosshairStartScale = 2.4f;
        public const float CrosshairStartTurnDegrees = 60f;

        public const float RetargetSeconds = 0.16f;
        public const float RetargetSpread = 1.22f;
        public const float RetargetCrosshairScale = 1.5f;
        public const float RetargetTurnDegrees = 45f;

        /// <summary>칸 안쪽 틴트의 평상시 세기(0~1). 등장·재조준 때만 이보다 밝아졌다 돌아온다.</summary>
        public const float FillSettledAlpha = 0.5f;

        private Vector2 from;
        private Vector2 to;
        private float slideElapsed = SlideSeconds;
        private float pinchTime;
        private float appearElapsed = AppearSeconds;
        private float retargetElapsed = RetargetSeconds;

        public bool Visible { get; private set; }
        public Vector2 Position { get; private set; }
        public Vector2 Target => to;
        /// <summary>꺾쇠가 칸 안쪽으로 조여든 거리(월드 단위).</summary>
        public float Inset { get; private set; }

        /// <summary>등장 진행도 0~1.</summary>
        public float AppearProgress => Mathf.Clamp01(appearElapsed / AppearSeconds);
        public bool IsAppearing => appearElapsed < AppearSeconds;

        /// <summary>꺾쇠 위치 배수. 등장 시작 1.85 → 1, 재조준 시 1.22 → 1 (ease-out).</summary>
        public float BracketSpread =>
            Mathf.Lerp(AppearStartSpread, 1f, EaseOut(AppearProgress)) * Mathf.Lerp(RetargetSpread, 1f, EaseOut(RetargetProgress));

        /// <summary>꺾쇠 알파 배수 0 → 1. 재조준에서는 변하지 않는다.</summary>
        public float BracketAlpha => Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(AppearProgress / 0.35f));

        public float CrosshairScale =>
            Mathf.Lerp(CrosshairStartScale, 1f, EaseOut(CrosshairProgress))
            * Mathf.Lerp(RetargetCrosshairScale, 1f, EaseOut(RetargetProgress));

        /// <summary>조준선이 아직 돌아야 할 각도(도). 끝나면 0.</summary>
        public float CrosshairTurn =>
            Mathf.Lerp(CrosshairStartTurnDegrees, 0f, EaseOut(CrosshairProgress))
            + Mathf.Lerp(RetargetTurnDegrees, 0f, EaseOut(RetargetProgress));

        public float CrosshairAlpha => Mathf.Clamp01(CrosshairProgress / 0.35f);

        /// <summary>칸 틴트 세기. 등장 중 1까지 올랐다가 평상시 값으로 가라앉고, 재조준 때 살짝 밝아진다.</summary>
        public float FillAlpha
        {
            get
            {
                float t = AppearProgress;
                float appear = t < 0.4f
                    ? t / 0.4f
                    : Mathf.Lerp(1f, FillSettledAlpha, Mathf.SmoothStep(0f, 1f, (t - 0.4f) / 0.6f));
                float bump = 0.4f * (1f - EaseOut(RetargetProgress));
                return Mathf.Clamp01(appear + bump * (t >= 1f ? 1f : 0f));
            }
        }

        private float RetargetProgress => Mathf.Clamp01(retargetElapsed / RetargetSeconds);
        private float CrosshairProgress => Mathf.Clamp01((AppearProgress - CrosshairDelay) / (1f - CrosshairDelay));

        public void SetTarget(Vector2 center, bool reduceMotion)
        {
            if (!Visible || reduceMotion)
            {
                // 숨은 상태에서 처음 켜질 때만 등장을 처음부터 재생한다. 모션 감소 중의 칸 이동은 그대로 붙는다.
                if (!Visible)
                {
                    appearElapsed = 0f;
                    retargetElapsed = RetargetSeconds;
                }

                Snap(center);
                Visible = true;
                return;
            }

            if ((center - to).sqrMagnitude < 0.0001f) return;
            from = Position;
            to = center;
            slideElapsed = 0f;
            retargetElapsed = 0f;
        }

        public void Hide()
        {
            Visible = false;
            Inset = 0f;
            pinchTime = 0f;
            slideElapsed = SlideSeconds;
            appearElapsed = AppearSeconds;
            retargetElapsed = RetargetSeconds;
        }

        public void Tick(float deltaTime, bool reduceMotion)
        {
            if (!Visible) return;
            float dt = Mathf.Max(0f, deltaTime);
            if (appearElapsed < AppearSeconds) appearElapsed = Mathf.Min(AppearSeconds, appearElapsed + dt);
            if (retargetElapsed < RetargetSeconds) retargetElapsed = Mathf.Min(RetargetSeconds, retargetElapsed + dt);
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

        // 1 - (1 - t)^3
        private static float EaseOut(float t)
        {
            float inverse = 1f - Mathf.Clamp01(t);
            return 1f - inverse * inverse * inverse;
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
    /// 캘 칸에 띄우는 각진 청록 홀로그램 과녁 하나: 네 모서리 꺾쇠, 한가운데 다이아 조준선, 칸 안쪽 틴트.
    /// 그림은 코드로 만들고(<see cref="MiningTargetReticleArt"/>), 프리팹·씬을 건드리지 않는다. 소리는 내지 않는다.
    /// 루트는 칸 중심에 둔다. 등장·재조준은 꺾쇠 위치 배수와 각 부품의 알파·스케일·회전에만 걸고,
    /// 느린 조임(Inset)은 꺾쇠 위치에만 건다. 둘은 서로를 읽지 않는다.
    /// </summary>
    public sealed class MiningTargetReticle : MonoBehaviour
    {
        // 지형(0~10대)·가스(20~23) 위, 건설 미리보기(49~50)와 균열(90~92)·스캔(94~95) 아래.
        public const int FillSortingOrder = 23;
        public const int SortingOrder = 24;
        public const int CrosshairSortingOrder = SortingOrder + 1;
        public const float BracketFinalAlpha = 0.95f;
        public const float CrosshairFinalAlpha = 0.95f;

        private static Sprite cornerSprite;
        private static Sprite crosshairSprite;
        private static Sprite fillSprite;
        // 텍스처가 다른 스프라이트가 한 머티리얼을 공유하면 같은 배치로 묶여 첫 텍스처로 다 그려진다.
        // 부품마다 머티리얼을 따로 두고 mainTexture를 직접 넣는다.
        private static Material cornerMaterial;
        private static Material crosshairMaterial;
        private static Material fillMaterial;

        private readonly MiningReticleMotion motion = new MiningReticleMotion();
        private readonly SpriteRenderer[] corners = new SpriteRenderer[4];
        private SpriteRenderer crosshair;
        private SpriteRenderer fill;
        private Transform root;
        private Vector2 cellSize = Vector2.one;

        public bool IsVisible => root != null && root.gameObject.activeSelf && motion.Visible;
        public Vector2 CurrentCenter => motion.Position;
        public Vector2 TargetCenter => motion.Target;
        public float Inset => motion.Inset;
        public float BracketSpread => motion.BracketSpread;
        public float BracketAlpha => motion.BracketAlpha;
        public float CrosshairScale => motion.CrosshairScale;
        public float CrosshairTurn => motion.CrosshairTurn;
        public SpriteRenderer[] Corners => corners;
        public SpriteRenderer Crosshair => crosshair;
        public SpriteRenderer Fill => fill;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            cornerSprite = null;
            crosshairSprite = null;
            fillSprite = null;
            cornerMaterial = null;
            crosshairMaterial = null;
            fillMaterial = null;
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
            float hx = Mathf.Max(0f, half.x - inset) * motion.BracketSpread;
            float hy = Mathf.Max(0f, half.y - inset) * motion.BracketSpread;
            root.position = new Vector3(center.x, center.y, 0f);

            var bracketColor = new Color(1f, 1f, 1f, BracketFinalAlpha * motion.BracketAlpha);
            // 0=좌하 1=우하 2=좌상 3=우상. 원본 그림은 좌하 모서리이고 나머지는 뒤집어 쓴다.
            for (int i = 0; i < corners.Length; i++)
            {
                bool right = (i & 1) != 0;
                bool top = (i & 2) != 0;
                Transform t = corners[i].transform;
                t.localPosition = new Vector3(right ? hx : -hx, top ? hy : -hy, 0f);
                t.localScale = new Vector3(cellSize.x, cellSize.y, 1f);
                corners[i].color = bracketColor;
            }

            Transform cross = crosshair.transform;
            cross.localPosition = Vector3.zero;
            cross.localRotation = Quaternion.Euler(0f, 0f, motion.CrosshairTurn);
            float crossScale = motion.CrosshairScale;
            cross.localScale = new Vector3(cellSize.x * crossScale, cellSize.y * crossScale, 1f);
            crosshair.color = new Color(1f, 1f, 1f, CrosshairFinalAlpha * motion.CrosshairAlpha);

            Transform tint = fill.transform;
            tint.localPosition = Vector3.zero;
            tint.localScale = new Vector3(cellSize.x, cellSize.y, 1f);
            fill.color = new Color(1f, 1f, 1f, motion.FillAlpha);
        }

        private void EnsureBuilt()
        {
            if (root != null) return;
            EnsureSprites();
            root = new GameObject("MiningTargetReticle").transform;
            root.SetParent(null, false);

            fill = NewRenderer("Tint", fillSprite, fillMaterial, FillSortingOrder);
            for (int i = 0; i < corners.Length; i++)
            {
                SpriteRenderer corner = NewRenderer("Corner" + i, cornerSprite, cornerMaterial, SortingOrder);
                corner.flipX = (i & 1) != 0;
                corner.flipY = (i & 2) != 0;
                corners[i] = corner;
            }

            crosshair = NewRenderer("Crosshair", crosshairSprite, crosshairMaterial, CrosshairSortingOrder);
            root.gameObject.SetActive(false);
        }

        private SpriteRenderer NewRenderer(string objectName, Sprite sprite, Material material, int order)
        {
            var child = new GameObject(objectName);
            child.transform.SetParent(root, false);
            var renderer = child.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sharedMaterial = material;
            renderer.color = new Color(1f, 1f, 1f, 0f);
            renderer.sortingOrder = order;
            return renderer;
        }

        private static void EnsureSprites()
        {
            if (cornerSprite != null && crosshairSprite != null && fillSprite != null
                && cornerMaterial != null && crosshairMaterial != null && fillMaterial != null)
            {
                return;
            }

            cornerSprite = MiningTargetReticleArt.BuildCornerSprite();
            crosshairSprite = MiningTargetReticleArt.BuildCrosshairSprite();
            fillSprite = MiningTargetReticleArt.BuildFillSprite();
            Shader shader = Shader.Find("Sprites/Default");
            cornerMaterial = BuildMaterial(shader, "MiningTargetReticleCorner", cornerSprite);
            crosshairMaterial = BuildMaterial(shader, "MiningTargetReticleCrosshair", crosshairSprite);
            fillMaterial = BuildMaterial(shader, "MiningTargetReticleTint", fillSprite);
        }

        private static Material BuildMaterial(Shader shader, string materialName, Sprite sprite)
        {
            if (shader == null) return null;
            return new Material(shader) { name = materialName, mainTexture = sprite.texture, hideFlags = HideFlags.HideAndDontSave };
        }
    }
}

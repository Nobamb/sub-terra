using System.Collections.Generic;
using SubTerra.Gameplay.Power;
using UnityEngine;

namespace SubTerra.Gameplay.Building
{
    /// <summary>
    /// B-214: 한 번의 주기 연출이 쓰는 자리·정렬·무작위. 연출 루트는 시설 루트의 자식이며,
    /// 부모 크기를 상쇄해 자식의 월드 크기가 그대로 월드 단위가 되게 한다(B-206과 같은 방식).
    /// </summary>
    internal sealed class IdleFxContext
    {
        public Transform Root;
        public Transform EffectRoot;
        public SpriteRenderer Artwork;
        public Rect World;
        public int LayerId;
        public int ArtworkOrder;
        public int Order;
        public System.Random Rng;
        public readonly List<Object> Owned = new();

        public Vector3 Point(Vector2 ratio) => new(
            Mathf.LerpUnclamped(World.xMin, World.xMax, ratio.x),
            Mathf.LerpUnclamped(World.yMin, World.yMax, ratio.y),
            Root.position.z);

        public float Width => World.width;
        public float Height => World.height;

        public SpriteRenderer NewSprite(string name, Sprite sprite, int orderOffset, Color color, bool hidden = true)
        {
            var go = new GameObject(name) { layer = Root.gameObject.layer };
            go.transform.SetParent(EffectRoot, false);
            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingLayerID = LayerId;
            renderer.sortingOrder = Order + orderOffset;
            renderer.enabled = !hidden;
            return renderer;
        }

        public static void Put(SpriteRenderer renderer, Vector3 position, Vector2 size, float degrees, Color color)
        {
            Transform t = renderer.transform;
            t.position = position;
            t.localScale = new Vector3(size.x, size.y, 1f);
            t.rotation = Quaternion.Euler(0f, 0f, degrees);
            renderer.color = color;
            renderer.enabled = color.a > 0.004f;
        }

        public float Range(float min, float max) => Mathf.Lerp(min, max, (float)Rng.NextDouble());
    }

    internal abstract class IdleFx
    {
        /// <summary>오브젝트를 만든다. 이 시설 그림으로는 그릴 수 없으면 false(그 주기는 건너뛴다).</summary>
        public abstract bool Begin(IdleFxContext c);

        /// <summary>t는 경과 초, u는 0~1 진행, amount는 중단 때 1→0으로 줄어드는 세기.</summary>
        public abstract void Apply(IdleFxContext c, float t, float u, float amount);

        public virtual void End(IdleFxContext c) { }

        protected static Color Alpha(Color color, float alpha)
        {
            color.a = Mathf.Clamp01(alpha);
            return color;
        }

        protected static float Bell(float x) => Mathf.Sin(Mathf.PI * Mathf.Clamp01(x));
    }

    /// <summary>조명: 등불 위에 어두운 덧그림을 겹쳐 2~3번 깜빡이고, 후광 알파도 같이 낮춘다.</summary>
    internal sealed class LightIdleFx : IdleFx
    {
        private SpriteRenderer overlay;
        private FacilityVisualDim dim;

        public override bool Begin(IdleFxContext c)
        {
            overlay = c.NewSprite("LanternDim", FacilityIdleSprites.Glow, 0, Color.black);
            dim = FacilityVisualDim.GetOrAdd(c.Root.gameObject);
            return true;
        }

        public override void Apply(IdleFxContext c, float t, float u, float amount)
        {
            float brightness = Mathf.Lerp(1f, FacilityIdleAnchors.LightBrightness(u), amount);
            float dark = (1f - brightness) / (1f - FacilityIdleAnchors.LightMinBrightness);
            float size = FacilityIdleAnchors.LightOverlayWidth * c.Width;
            IdleFxContext.Put(overlay, c.Point(FacilityIdleAnchors.LightLantern), new Vector2(size, size), 0f,
                new Color(0.02f, 0.02f, 0.06f, dark * FacilityIdleAnchors.LightOverlayMaxAlpha));
            if (dim != null) dim.Multiplier = brightness;
        }

        public override void End(IdleFxContext c)
        {
            if (dim != null) dim.Multiplier = 1f;
        }
    }

    /// <summary>
    /// 충전기·보건소 공통: 시설 위에 팝업 아이콘이 속이 빈 채로 톡 나타나고, 아래에서 위로 액체가 차오르며
    /// 천천히 떠오르다가 가득 차면 번쩍이며 원래 아이콘으로 돌아온 뒤 옅어진다. 차오름 프레임은 미리 구운 시트다.
    /// 시설별 분위기(정전기 / 회복)는 ApplyAura에서 덧그린다.
    /// </summary>
    internal abstract class IconFillIdleFx : IdleFx
    {
        protected SpriteRenderer Icon, Aura, Flash, Ring;
        protected abstract Sprite IconFrame(int frame);
        protected abstract Color Tint { get; }
        protected abstract Vector2 Anchor { get; }
        protected abstract float FillEnd { get; }
        protected abstract float Duration { get; }

        public override bool Begin(IdleFxContext c)
        {
            if (IconFrame(0) == null || IconFrame(FacilityIdleAnchors.IconFillFrames - 1) == null) return false;
            Aura = c.NewSprite("IconAura", FacilityIdleSprites.Glow, 0, Tint);
            Ring = c.NewSprite("IconRing", FacilityIdleSprites.Ring, 1, Tint);
            Icon = c.NewSprite("IconFill", IconFrame(0), 3, Color.white);
            Flash = c.NewSprite("IconFlash", FacilityIdleSprites.Glow, 4, Color.white);
            BeginAura(c);
            return true;
        }

        protected virtual void BeginAura(IdleFxContext c) { }

        /// <summary>아이콘 중심·크기·세기·차오름 정도를 받아 시설별 분위기를 덧그린다.</summary>
        protected virtual void ApplyAura(IdleFxContext c, Vector3 center, float size, float t, float alpha, float level) { }

        public override void Apply(IdleFxContext c, float t, float u, float amount)
        {
            float size = FacilityIdleAnchors.IconWidth * c.Width;
            float appear = Mathf.Clamp01(t / FacilityIdleAnchors.IconAppearSeconds);
            float fadeOut = Mathf.Clamp01((Duration - t) / FacilityIdleAnchors.IconFadeSeconds);
            float alpha = Mathf.Clamp01(appear * 2f) * fadeOut * amount;
            float level = FacilityIdleAnchors.IconFillLevel(t, FillEnd);
            float complete = (t - FillEnd) / FacilityIdleAnchors.IconCompleteSeconds;
            bool completing = complete >= 0f && complete <= 1f;

            // 톡 나타남(살짝 넘쳤다 제자리) + 가득 찬 순간 한 번 튀어 오름.
            float scale = BackOut(appear);
            if (completing) scale *= 1f + 0.16f * Mathf.Sin(Mathf.PI * complete) * (1f - complete * 0.5f);
            float rise = FacilityIdleAnchors.IconRise * Smooth(Mathf.Clamp01(t / Duration));
            Vector3 center = c.Point(Anchor) + new Vector3(0f, FacilityIdleAnchors.IconLift + rise, 0f);

            Icon.sprite = IconFrame(FacilityIdleAnchors.IconFillFrame(level));
            IdleFxContext.Put(Icon, center, new Vector2(size * scale, size * scale), 0f, Alpha(Color.white, alpha));

            // 뒤쪽 빛은 차오를수록 밝아진다.
            float auraAlpha = (0.18f + 0.42f * level) * alpha;
            IdleFxContext.Put(Aura, center, new Vector2(size * 2.1f, size * 2.1f), 0f, Alpha(Tint, auraAlpha));

            float burst = completing ? 1f - complete : 0f;
            IdleFxContext.Put(Flash, center, new Vector2(size * 1.6f * scale, size * 1.6f * scale), 0f,
                Alpha(Color.white, 0.75f * burst * burst * amount));
            float ringSize = Mathf.Lerp(0.7f, 2.6f, 1f - (1f - Mathf.Clamp01(complete)) * (1f - Mathf.Clamp01(complete))) * size;
            IdleFxContext.Put(Ring, center, new Vector2(ringSize, ringSize), 0f,
                Alpha(Tint, completing ? 0.85f * Mathf.Pow(1f - complete, 1.5f) * amount : 0f));

            ApplyAura(c, center, size * scale, t, alpha, level);
        }

        protected static float Smooth(float x) => x * x * (3f - 2f * x);

        private static float BackOut(float x)
        {
            const float s = 1.9f;
            x = Mathf.Clamp01(x) - 1f;
            return 1f + x * x * ((s + 1f) * x + s);
        }

        /// <summary>정해진 순번에 대한 0~1 무작위(같은 연출 안에서는 늘 같은 값).</summary>
        protected static float Hash(int seed, int index, int salt)
        {
            unchecked
            {
                uint h = (uint)(seed * 73856093) ^ (uint)(index * 19349663) ^ (uint)(salt * 83492791);
                h ^= h >> 13; h *= 0x5bd1e995; h ^= h >> 15;
                return (h & 0xFFFFFF) / (float)0x1000000;
            }
        }
    }

    /// <summary>
    /// 충전기: 충전 완료창의 번개 아이콘이 차양 위에 빈 채로 나타나, 둘레에 청록 정전기가 탁탁 튀는 동안
    /// 아래에서 위로 청록 액체가 차올라 원래 번개로 돌아온다. 번개 화면도 같이 빛나고, 가득 차면 정전기가 사방으로 터진다.
    /// </summary>
    internal sealed class ChargerIdleFx : IconFillIdleFx
    {
        private const int SparkSlots = 6, BurstCount = 6;
        private const float SparkCycle = 0.17f;              // 정전기 한 줄기가 다시 튀는 간격
        private readonly List<SpriteRenderer> sparks = new();
        private readonly List<SpriteRenderer> sparkGlows = new();
        private readonly List<SpriteRenderer> bursts = new();
        private SpriteRenderer screenGlow;
        private int seed;

        protected override Sprite IconFrame(int frame) => FacilityIdleSprites.ChargerIcon(frame);
        protected override Color Tint => PowerOnEffect.Cyan;
        protected override Vector2 Anchor => FacilityIdleAnchors.ChargerIcon;
        protected override float FillEnd => FacilityIdleAnchors.ChargerFillEnd;
        protected override float Duration => FacilityIdleAnchors.ChargerSeconds;

        protected override void BeginAura(IdleFxContext c)
        {
            seed = c.Rng.Next(1, int.MaxValue);
            screenGlow = c.NewSprite("ScreenGlow", FacilityIdleSprites.Glow, 0, PowerOnEffect.Cyan);
            for (int i = 0; i < SparkSlots; i++)
            {
                sparkGlows.Add(c.NewSprite("SparkGlow", FacilityIdleSprites.Glow, 2, PowerOnEffect.Cyan));
                sparks.Add(c.NewSprite("SparkBolt", FacilityIdleSprites.Bolt, 5, PowerOnEffect.CyanCore));
            }
            for (int i = 0; i < BurstCount; i++) bursts.Add(c.NewSprite("BurstBolt", FacilityIdleSprites.Bolt, 5, PowerOnEffect.CyanCore));
        }

        protected override void ApplyAura(IdleFxContext c, Vector3 center, float size, float t, float alpha, float level)
        {
            // 정전기: 아이콘 둘레 아무 자리에서 짧게 튀고 사라지기를 되풀이한다(차오르는 동안만).
            bool charging = t >= 0.08f && t <= FillEnd + 0.05f;
            for (int i = 0; i < sparks.Count; i++)
            {
                float local = t + i * (SparkCycle / SparkSlots) * 1.7f;
                int generation = Mathf.FloorToInt(local / SparkCycle);
                float phase = local / SparkCycle - generation;
                bool on = charging && phase < 0.55f && Hash(seed, i, generation) > 0.18f;
                float angle = Hash(seed, i, generation * 3 + 1) * Mathf.PI * 2f;
                float radius = size * Mathf.Lerp(0.42f, 0.6f, Hash(seed, i, generation * 3 + 2));
                Vector3 position = center + new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius * 0.9f, 0f);
                float flicker = Mathf.FloorToInt(t * 45f + i) % 2 == 0 ? 1f : 0.55f;
                float length = size * Mathf.Lerp(0.32f, 0.48f, Hash(seed, i, generation * 3 + 3));
                // 번개 스프라이트는 세로로 뻗으므로 바깥쪽을 향하게 돌린다.
                float degrees = angle * Mathf.Rad2Deg - 90f + Mathf.Lerp(-25f, 25f, Hash(seed, i, generation + 7));
                IdleFxContext.Put(sparks[i], position, new Vector2(length * 0.62f, length), degrees,
                    Alpha(Color.white, on ? flicker * alpha : 0f));
                IdleFxContext.Put(sparkGlows[i], position, new Vector2(length * 1.3f, length * 1.3f), 0f,
                    Alpha(PowerOnEffect.Cyan, on ? 0.6f * alpha : 0f));
            }

            // 가득 차는 순간 정전기가 사방으로 터져 나간다.
            float b = (t - FillEnd) / 0.32f;
            bool bursting = b >= 0f && b <= 1f;
            for (int i = 0; i < bursts.Count; i++)
            {
                float angle = (i / (float)bursts.Count + Hash(seed, i, 99) * 0.08f) * Mathf.PI * 2f;
                float distance = size * Mathf.Lerp(0.45f, 1.05f, Smooth(Mathf.Clamp01(b)));
                Vector3 position = center + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * distance;
                float length = size * 0.5f * (1f - 0.4f * Mathf.Clamp01(b));
                IdleFxContext.Put(bursts[i], position, new Vector2(length * 0.62f, length), angle * Mathf.Rad2Deg - 90f,
                    Alpha(Color.white, bursting ? (1f - b) * alpha : 0f));
            }

            // 충전기 번개 화면도 차오르는 만큼 깜빡이며 밝아진다.
            float width = c.Width;
            float screenFlicker = Mathf.FloorToInt(t * 24f) % 3 == 0 ? 0.65f : 1f;
            IdleFxContext.Put(screenGlow, c.Point(FacilityIdleAnchors.ChargerScreen), new Vector2(width * 0.5f, width * 0.5f), 0f,
                Alpha(PowerOnEffect.Cyan, 0.55f * level * screenFlicker * alpha));
        }
    }

    /// <summary>
    /// 보건소: 회복창의 하트 아이콘이 탑 위에 빈 채로 나타나, 심장 박동처럼 두근거리며 아래에서 위로 붉은 액체가 차오른다.
    /// 둘레에서 회복 십자(+)가 피어오르고, 십자 간판도 박동에 맞춰 빛난다. 가득 차면 붉은 고리가 퍼진다.
    /// </summary>
    internal sealed class ClinicIdleFx : IconFillIdleFx
    {
        private static readonly Color Red = new(1f, 0.28f, 0.34f, 1f);
        private static readonly Color HealPink = new(1f, 0.78f, 0.82f, 1f);
        private const int PlusCount = 6;
        private const float PlusCycle = 0.95f;
        private static readonly float[] Beats = { 0.55f, 0.78f, 1.2f, 1.43f };   // 두근(쿵-쿵) 두 번
        private readonly List<SpriteRenderer> pluses = new();
        private SpriteRenderer crossGlow;
        private int seed;

        protected override Sprite IconFrame(int frame) => FacilityIdleSprites.ClinicIcon(frame);
        protected override Color Tint => Red;
        protected override Vector2 Anchor => FacilityIdleAnchors.ClinicTower;
        protected override float FillEnd => FacilityIdleAnchors.ClinicFillEnd;
        protected override float Duration => FacilityIdleAnchors.ClinicSeconds;

        protected override void BeginAura(IdleFxContext c)
        {
            seed = c.Rng.Next(1, int.MaxValue);
            crossGlow = c.NewSprite("CrossGlow", FacilityIdleSprites.Glow, 0, Red);
            for (int i = 0; i < PlusCount; i++) pluses.Add(c.NewSprite("HealPlus", FacilityIdleSprites.Plus, 2, HealPink));
        }

        /// <summary>박동 세기(0~1). 박자마다 빠르게 올라갔다 천천히 가라앉는다.</summary>
        private static float Beat(float t)
        {
            float value = 0f;
            foreach (float beat in Beats)
            {
                float d = t - beat;
                if (d >= 0f && d < 0.3f) value = Mathf.Max(value, d < 0.06f ? d / 0.06f : 1f - (d - 0.06f) / 0.24f);
            }
            return value;
        }

        public override void Apply(IdleFxContext c, float t, float u, float amount)
        {
            base.Apply(c, t, u, amount);
            // 하트가 박동마다 살짝 커진다(아이콘 크기는 base에서 정한 값에 곱한다).
            float pulse = 1f + 0.08f * Beat(t);
            Icon.transform.localScale = Vector3.Scale(Icon.transform.localScale, new Vector3(pulse, pulse, 1f));
            Aura.transform.localScale = Vector3.Scale(Aura.transform.localScale, new Vector3(pulse, pulse, 1f));
        }

        protected override void ApplyAura(IdleFxContext c, Vector3 center, float size, float t, float alpha, float level)
        {
            // 회복 십자: 하트 아래쪽 둘레에서 피어올라 위로 떠가며 옅어진다.
            bool healing = t >= 0.15f && t <= FacilityIdleAnchors.ClinicSeconds - 0.3f;
            for (int i = 0; i < pluses.Count; i++)
            {
                float local = t + i * (PlusCycle / PlusCount);
                int generation = Mathf.FloorToInt(local / PlusCycle);
                float phase = local / PlusCycle - generation;
                float x = Mathf.Lerp(-0.75f, 0.75f, Hash(seed, i, generation)) * size;
                float y = Mathf.Lerp(-0.45f, 0.75f, phase) * size;
                float s = size * Mathf.Lerp(0.16f, 0.26f, Hash(seed, i, generation + 31)) * Mathf.Min(1f, phase * 4f);
                float fade = Mathf.Sin(Mathf.PI * phase);
                IdleFxContext.Put(pluses[i], center + new Vector3(x, y, 0f), new Vector2(s, s), 0f,
                    Alpha(HealPink, healing ? 0.9f * fade * alpha : 0f));
            }

            float width = c.Width;
            float cross = width * 0.22f * (1f + 0.25f * Beat(t));
            IdleFxContext.Put(crossGlow, c.Point(FacilityIdleAnchors.ClinicCross), new Vector2(cross, cross), 0f,
                Alpha(Red, (0.2f + 0.5f * Beat(t)) * alpha));
        }
    }

    /// <summary>
    /// 보관함: 사용자 영상(storage_open.mp4)의 배경을 지운 프레임을 시설 그림 자리에 그대로 재생한다.
    /// 자물쇠가 빛나며 풀리고 → 뚜껑이 열려 청록 빛이 새어 나오고 → 닫힌 뒤 다시 잠긴다.
    /// 첫 프레임은 원래 그림과 같은 자리·크기라 바꿔 끼워도 튀지 않는다. 재생 중에는 원래 Artwork를 가린다.
    /// </summary>
    internal sealed class StorageIdleFx : IdleFx
    {
        private SpriteRenderer artwork, video;

        public override bool Begin(IdleFxContext c)
        {
            artwork = c.Artwork;
            if (artwork == null || artwork.drawMode != SpriteDrawMode.Simple || artwork.sprite == null) return false;
            Sprite first = FacilityIdleSprites.StorageOpenFrame(0, artwork.sprite);
            if (first == null) return false;

            video = c.NewSprite("StorageOpenVideo", first, 0, Color.white, false);
            video.sharedMaterial = artwork.sharedMaterial;
            // 원래 그림과 같은 정렬 값에 두고 원래 그림은 가린다(두 겹으로 보이지 않게).
            video.sortingOrder = artwork.sortingOrder;
            video.flipX = artwork.flipX;
            artwork.forceRenderingOff = true;
            return true;
        }

        public override void Apply(IdleFxContext c, float t, float u, float amount)
        {
            if (video == null || artwork == null) return;
            if (amount < 1f)
            {
                // 중단되면 바로 원래 그림으로 돌아간다(영상 프레임은 반투명으로 섞을 수 없다).
                artwork.forceRenderingOff = false;
                video.enabled = false;
                return;
            }

            Sprite frame = FacilityIdleSprites.StorageOpenFrame(FacilityIdleAnchors.StorageOpenFrame(t), artwork.sprite);
            if (frame != null) video.sprite = frame;
            // 연출 루트는 부모 크기를 상쇄하므로 그림의 월드 위치·크기를 그대로 따라간다.
            Transform target = artwork.transform;
            video.transform.SetPositionAndRotation(target.position, target.rotation);
            video.transform.localScale = target.lossyScale;
            video.color = artwork.color;
            video.enabled = true;
        }

        public override void End(IdleFxContext c)
        {
            if (artwork != null) artwork.forceRenderingOff = false;
        }
    }

    /// <summary>
    /// 자원 정산기: 화면 안에서 구리·철·리튬 원석 도트가 톡 튀어나와 섬광과 함께 8bit 금화로 바뀌고,
    /// 금화가 통통 뛰며 회전하다 깜빡이며 사라진다(마리오 월드 느낌). 도트는 Aseprite로 그린 시트(SettlementCoinSheet)다.
    /// </summary>
    internal sealed class SettlementIdleFx : IdleFx
    {
        private const int Slots = 3;
        private const float SlotSpacing = 0.31f;     // 화면 폭 대비 가로 간격
        private const float ItemSize = 0.27f;        // 화면 폭 대비 도트 한 칸 크기
        private const float AppearStart = 0.05f, AppearStep = 0.08f, AppearPop = 0.2f;
        private const float ConvertStart = 0.55f, ConvertStep = 0.2f, PoofFrame = 0.06f;
        private const float HopSeconds = 0.4f, HopHeight = 0.5f, SpinFrame = 0.08f;
        private const float BlinkStart = 1.2f, BlinkStep = 0.04f;

        private static readonly Color PanelColor = new(0.03f, 0.21f, 0.23f, 1f);
        private SpriteRenderer panel;
        private readonly SpriteRenderer[] ores = new SpriteRenderer[Slots];
        private readonly SpriteRenderer[] coins = new SpriteRenderer[Slots];
        private readonly SpriteRenderer[] poofs = new SpriteRenderer[Slots];

        public override bool Begin(IdleFxContext c)
        {
            if (!FacilityIdleSprites.HasSettlementSheet) return false;
            panel = c.NewSprite("ScreenPanel", FacilityIdleSprites.Square, 0, PanelColor);
            for (int i = 0; i < Slots; i++)
            {
                ores[i] = c.NewSprite("Ore" + i, FacilityIdleSprites.Ore(i), 1, Color.white);
                coins[i] = c.NewSprite("Coin" + i, FacilityIdleSprites.CoinFrame(0), 2, Color.white);
                poofs[i] = c.NewSprite("Poof" + i, FacilityIdleSprites.Poof(0), 3, Color.white);
            }
            return true;
        }

        public override void Apply(IdleFxContext c, float t, float u, float amount)
        {
            float screen = FacilityIdleAnchors.SettlementScreenWidth * c.Width;
            float size = screen * ItemSize;
            float pixel = size / 16f;
            Vector3 center = c.Point(FacilityIdleAnchors.SettlementScreen);
            // 중단되면 도트가 깜빡이지 않고 그대로 옅어진다.
            Color tint = new(1f, 1f, 1f, amount);

            float panelFade = Mathf.Min(Mathf.Clamp01(t / 0.1f), Mathf.Clamp01((FacilityIdleAnchors.SettlementSeconds - t) / 0.1f)) * amount;
            IdleFxContext.Put(panel, c.Point(FacilityIdleAnchors.SettlementPanelCenter),
                new Vector2(FacilityIdleAnchors.SettlementPanelSize.x * c.Width, FacilityIdleAnchors.SettlementPanelSize.y * c.Height), 0f,
                Alpha(PanelColor, panelFade));

            for (int i = 0; i < Slots; i++)
            {
                Vector3 home = center + new Vector3((i - 1) * SlotSpacing * screen, 0f, 0f);
                float appear = t - (AppearStart + AppearStep * i);
                float convert = t - (ConvertStart + ConvertStep * i);

                // 1) 원석: 톡 튀어나오며 커졌다 제자리, 제자리에서 도트 한 칸씩 위아래로 까딱.
                bool oreOn = appear >= 0f && convert < 0f;
                float pop = Pop(appear / AppearPop);
                float bob = Mathf.FloorToInt(t * 6f + i) % 2 == 0 ? 0f : pixel;
                Set(ores[i], oreOn, home + new Vector3(0f, bob, 0f), size * pop, tint);

                // 2) 섬광: 변환 순간 3프레임.
                int poofFrame = Mathf.FloorToInt(convert / PoofFrame);
                bool poofOn = convert >= 0f && poofFrame < 3;
                if (poofOn) poofs[i].sprite = FacilityIdleSprites.Poof(poofFrame);
                Set(poofs[i], poofOn, home, size * 1.3f, tint);

                // 3) 금화: 통통 뛰며 4프레임으로 회전하고, 마지막에 깜빡이며 사라진다.
                float coinT = convert - PoofFrame;
                bool coinOn = coinT >= 0f;
                float hop = coinT < HopSeconds ? Mathf.Sin(Mathf.PI * Mathf.Clamp01(coinT / HopSeconds)) * HopHeight * size : 0f;
                hop = Mathf.Round(hop / pixel) * pixel;
                int frame = Mathf.FloorToInt(Mathf.Max(0f, coinT) / SpinFrame) % 4;
                if (coinT > HopSeconds + 0.2f && Mathf.FloorToInt(t * 5f + i) % 4 == 0) frame = 4;   // 가끔 반짝
                bool blinkHidden = t > BlinkStart && Mathf.FloorToInt((t - BlinkStart) / BlinkStep) % 2 == 1;
                if (coinOn) coins[i].sprite = FacilityIdleSprites.CoinFrame(frame);
                Set(coins[i], coinOn && !blinkHidden, home + new Vector3(0f, hop, 0f), size, tint);
            }
        }

        private static void Set(SpriteRenderer renderer, bool on, Vector3 position, float size, Color tint)
        {
            IdleFxContext.Put(renderer, position, new Vector2(size, size), 0f, on ? tint : Alpha(tint, 0f));
        }

        // 톡 튀어나오기: 0 → 1.25 → 1을 도트 느낌으로 8단계씩 끊어서.
        private static float Pop(float x)
        {
            if (x <= 0f) return 0f;
            if (x >= 1f) return 1f;
            float smooth = x < 0.6f ? x / 0.6f * 1.25f : Mathf.Lerp(1.25f, 1f, (x - 0.6f) / 0.4f);
            return Mathf.Round(smooth * 8f) / 8f;
        }
    }

    /// <summary>전진기지: 테두리 LED glow, 돔 꼭대기 와이파이(점 → 안쪽 호 → 바깥 호), 중심에서 퍼지는 청록 고리 파형.</summary>
    internal sealed class OutpostIdleFx : IdleFx
    {
        private static readonly float[] WifiOn = { 0.10f, 0.35f, 0.60f, 0.85f };
        private const float WifiDotSize = 0.035f, WifiArcSize = 0.32f;   // 보이는 폭 비율
        private const int RingCount = 3;
        private const float RingStep = 0.35f, RingStart = 0.1f, RingLife = 1.1f;

        private readonly List<SpriteRenderer> leds = new();
        private readonly List<SpriteRenderer> wifi = new();
        private readonly List<SpriteRenderer> rings = new();

        public override bool Begin(IdleFxContext c)
        {
            foreach (Vector2 _ in FacilityIdleAnchors.OutpostLeds)
                leds.Add(c.NewSprite("LedGlow", FacilityIdleSprites.Glow, 0, PowerOnEffect.Cyan));
            wifi.Add(c.NewSprite("WifiDot", FacilityIdleSprites.Disc, 1, PowerOnEffect.Cyan));
            for (int i = 0; i < 3; i++) wifi.Add(c.NewSprite("WifiArc" + i, FacilityIdleSprites.Arc(i), 1, PowerOnEffect.Cyan));
            for (int i = 0; i < RingCount; i++) rings.Add(c.NewSprite("Wave" + i, FacilityIdleSprites.Ring, 0, PowerOnEffect.Cyan));
            return true;
        }

        public override void Apply(IdleFxContext c, float t, float u, float amount)
        {
            float width = c.Width;
            Vector2[] points = FacilityIdleAnchors.OutpostLeds;
            for (int i = 0; i < leds.Count; i++)
            {
                // 아래쪽 LED부터 위로 차례로 켜지고 같이 옅어진다.
                float start = points[i].y * 0.6f;
                float s = (t - start) / 1.2f;
                bool on = s >= 0f && s <= 1f;
                float size = width * 0.075f;
                IdleFxContext.Put(leds[i], c.Point(points[i]), new Vector2(size, size), 0f,
                    Alpha(PowerOnEffect.Cyan, on ? 0.8f * Bell(s) * amount : 0f));
            }

            // 와이파이는 켜진 뒤 유지되다가 끝에서 함께 옅어진다.
            Vector3 top = c.Point(FacilityIdleAnchors.OutpostDome);
            top.y += FacilityIdleAnchors.OutpostWifiRise;
            float fadeOut = Mathf.Clamp01((1f - u) / 0.3f);
            for (int i = 0; i < wifi.Count; i++)
            {
                float lit = Mathf.Clamp01((t - WifiOn[i]) / 0.12f);
                // 호 스프라이트는 점 위치를 중심으로 위쪽 호만 그려 두었으므로 같은 크기로 겹치면 안에서 밖으로 놓인다.
                float size = (i == 0 ? WifiDotSize : WifiArcSize) * width;
                IdleFxContext.Put(wifi[i], top, new Vector2(size, size), 0f,
                    Alpha(PowerOnEffect.Cyan, lit * fadeOut * amount));
            }

            Vector3 center = c.Point(FacilityIdleAnchors.OutpostCenter);
            for (int i = 0; i < rings.Count; i++)
            {
                float s = (t - (RingStart + RingStep * i)) / RingLife;
                bool on = s >= 0f && s <= 1f;
                float grow = 1f - Mathf.Pow(1f - Mathf.Clamp01(s), 2f);
                float size = Mathf.Lerp(0.12f, 1.25f, grow) * width;   // 시설보다 살짝 더 크게 퍼진다
                float fade = Mathf.Pow(1f - Mathf.Clamp01(s), 1.5f);
                IdleFxContext.Put(rings[i], center, new Vector2(size, size * 0.72f), 0f,
                    Alpha(PowerOnEffect.Cyan, on ? 0.75f * fade * amount : 0f));
            }
        }
    }
}

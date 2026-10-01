using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SubTerra.App.UI.Progression
{
    /// <summary>
    /// prompt-B 118-1: 업그레이드 노드에 포인터를 올렸을 때 가운데의 은은한 청록 빛과 아이콘별 연출을 재생한다.
    /// 표시만 담당하며 업그레이드 상태는 바꾸지 않는다. 모든 시간은 unscaledDeltaTime이고,
    /// 연출 이미지는 전부 raycastTarget=false라 노드 클릭을 가리지 않는다.
    /// </summary>
    public sealed class UpgradeNodeHoverFx : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public const float FadeSeconds = 0.18f;

        private static readonly Color GlowCyan = new Color(0.25f, 0.92f, 0.96f, 1f);
        private static readonly Color SparkCyan = new Color(0.62f, 0.98f, 1f, 1f);
        private static readonly Color HealRed = new Color(1f, 0.26f, 0.3f, 1f);
        private static readonly Color HandTint = new Color(0.84f, 1f, 0.84f, 1f);
        private const int FrontPoolSize = 40;
        private const int BackPoolSize = 6;

        private sealed class Particle
        {
            public RectTransform Rect;
            public Image Image;
            public Vector2 Pos;
            public Vector2 Vel;
            public float Gravity;
            public float Life;
            public float MaxLife;
            public float Spin;
            public float Angle;
            public float SizeFrom;
            public float SizeTo;
            public float FadeFrom;
            public Color Color;
            public bool Active;
        }

        [SerializeField] private UpgradeIconFxKind kind;
        [SerializeField] private Image icon;
        [SerializeField] private Image hoverGlow;
        [SerializeField] private Image blindHoverGlow;
        [SerializeField] private RectTransform backLayer;
        [SerializeField] private RectTransform frontLayer;
        [Tooltip("플러그 점등 / 열린 상자 몸통 / 입을 막는 손")]
        [SerializeField] private Image overlayA;
        [Tooltip("상자 뚜껑")]
        [SerializeField] private Image overlayB;
        [Tooltip("파편·동전·불꽃·체력(+) 입자")]
        [SerializeField] private Sprite particleSprite;
        [SerializeField] private Sprite ringSprite;
        [SerializeField] private Sprite glowSprite;
        [Tooltip("상자·드론에 들어가는 화물(구리·철·리튬)")]
        [SerializeField] private Sprite[] itemSprites = Array.Empty<Sprite>();

        private readonly List<Particle> front = new List<Particle>();
        private readonly List<Particle> back = new List<Particle>();
        private Image[] items;
        private Image tintGlow;
        private bool hovered;
        private float blend;
        private float fxTime = -1f;
        private float pulseTime;
        private float sparkTimer;
        private float ringTimer;
        private bool burstA;
        private bool burstB;
        private int plusSpawned;
        private bool poseDirty;
        private bool restCaptured;
        private Vector2 iconRest;

        public UpgradeIconFxKind Kind => kind;
        public bool IsHovered => hovered;
        public float HoverAmount => blend;
        public bool IsPlaying => fxTime >= 0f;
        public Image HoverGlow => hoverGlow;

        public void Configure(UpgradeIconFxKind value)
        {
            kind = value;
        }

        private void Awake()
        {
            CaptureRest();
        }

        private void OnDisable()
        {
            hovered = false;
            blend = 0f;
            fxTime = -1f;
            RestorePose();
            ApplyGlow();
            DeactivateAll(front);
            DeactivateAll(back);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            BeginHover();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            EndHover();
        }

        public void BeginHover()
        {
            hovered = true;
            if (fxTime < 0f || fxTime >= UpgradeIconFx.SequenceDuration(kind))
            {
                fxTime = 0f;
                burstA = burstB = false;
                plusSpawned = 0;
                sparkTimer = 0.05f;
                ringTimer = 0f;
            }
        }

        public void EndHover()
        {
            hovered = false;
        }

        private void Update()
        {
            Tick(Time.unscaledDeltaTime);
        }

        /// <summary>한 프레임 진행. 테스트에서 시간을 직접 넣어 확인할 수 있게 공개한다.</summary>
        public void Tick(float dt)
        {
            dt = Mathf.Max(0f, dt);
            blend = Mathf.MoveTowards(blend, hovered ? 1f : 0f, dt / FadeSeconds);
            pulseTime += dt;
            ApplyGlow();

            if (!IconShown())
            {
                // 미해금(아이콘 숨김) 노드는 가운데 빛만 보여 준다.
                if (fxTime >= 0f)
                {
                    fxTime = -1f;
                    RestorePose();
                }
            }
            else
            {
                var running = fxTime >= 0f && fxTime < UpgradeIconFx.SequenceDuration(kind);
                if (fxTime >= 0f && (hovered || blend > 0f || running))
                {
                    var previous = fxTime;
                    fxTime += dt;
                    Evaluate(previous, fxTime, dt);
                }
                else if (fxTime >= 0f)
                {
                    fxTime = -1f;
                    RestorePose();
                }
            }

            UpdateParticles(front, dt);
            UpdateParticles(back, dt);
        }

        private bool IconShown()
        {
            return icon != null && icon.enabled && icon.sprite != null && icon.gameObject.activeInHierarchy;
        }

        private void ApplyGlow()
        {
            var amount = blend * (0.34f + 0.08f * Mathf.Sin(pulseTime * 3.4f));
            if (hoverGlow != null)
            {
                UpgradeTreeTween.SetColor(hoverGlow, GlowCyan, amount);
            }

            if (blindHoverGlow != null)
            {
                UpgradeTreeTween.SetColor(blindHoverGlow, GlowCyan, amount * 0.8f);
            }
        }

        private float Size
        {
            get
            {
                var layer = frontLayer != null ? frontLayer : icon != null ? icon.rectTransform : null;
                var h = layer != null ? layer.rect.height : 0f;
                return h > 1f ? h : 60f;
            }
        }

        private float IconAlpha => icon != null ? icon.color.a : 1f;

        // ------------------------------------------------------------------ 연출 본체

        private void Evaluate(float previous, float t, float dt)
        {
            CaptureRest();
            poseDirty = true;
            var size = Size;
            var iconRect = icon.rectTransform;
            iconRect.anchoredPosition = iconRest;
            iconRect.localScale = Vector3.one;
            iconRect.localRotation = Quaternion.identity;
            icon.canvasRenderer.SetAlpha(1f);
            SetTint(Color.clear, 0f, 1.6f);

            switch (kind)
            {
                case UpgradeIconFxKind.DrillSpeed:
                {
                    var thrust = UpgradeIconFx.DrillThrust(t);
                    iconRect.anchoredPosition = iconRest + new Vector2(0f, -0.2f * size * thrust);
                    iconRect.localScale = new Vector3(1f + 0.04f * thrust, 1f - 0.06f * thrust, 1f);
                    break;
                }
                case UpgradeIconFxKind.DrillEfficiency:
                {
                    var lit = UpgradeIconFx.PowerOn(t) * blend;
                    SetOverlay(overlayA, lit * IconAlpha, Vector2.zero, 0f, Color.white);
                    SetTint(GlowCyan, 0.42f * lit, 1.5f);
                    if (hovered && t > 0.24f)
                    {
                        EmitSparks(dt, new Vector2(-0.12f, 0.36f), 0.16f, 0.09f, 0.2f);
                    }

                    break;
                }
                case UpgradeIconFxKind.MaximumCargo:
                {
                    // 연출 동안은 닫힌 원본 아이콘을 숨기고 몸통+뚜껑 두 장으로 그린다.
                    var playing = t < UpgradeIconFx.LidCloseEnd;
                    var visible = playing ? IconAlpha : 0f;
                    var open = UpgradeIconFx.LidOpen(t);
                    icon.canvasRenderer.SetAlpha(playing ? 0f : 1f);
                    SetOverlay(overlayA, visible, Vector2.zero, 0f, Color.white);
                    SetOverlay(overlayB, visible, new Vector2(-0.06f * size * open, 0.3f * size * open), 24f * open, Color.white);
                    AnimateItems(t, UpgradeIconFx.CargoItemStarts, new Vector2(0f, 0.1f), 0.5f, size);
                    break;
                }
                case UpgradeIconFxKind.CargoYield:
                case UpgradeIconFxKind.CargoGold:
                {
                    var coins = kind == UpgradeIconFxKind.CargoGold;
                    if (t < 0.3f)
                    {
                        var punch = 1f + 0.1f * UpgradeTreeTween.Pulse(t / 0.3f);
                        iconRect.localScale = new Vector3(punch, punch, 1f);
                    }

                    if (!burstA)
                    {
                        burstA = true;
                        Burst(coins ? 16 : 15, coins, size);
                    }

                    if (!burstB && t >= 0.32f)
                    {
                        burstB = true;
                        Burst(coins ? 9 : 10, coins, size);
                    }

                    break;
                }
                case UpgradeIconFxKind.DroneScan:
                {
                    SetTint(GlowCyan, blend * (0.3f + 0.12f * Mathf.Sin(t * 5f)), 1.5f);
                    if (hovered)
                    {
                        ringTimer -= dt;
                        if (ringTimer <= 0f)
                        {
                            ringTimer = 0.5f;
                            SpawnRing(size);
                        }
                    }

                    break;
                }
                case UpgradeIconFxKind.DroneRescue:
                {
                    iconRect.anchoredPosition = iconRest + new Vector2(0f, 0.04f * size * Mathf.Sin(t * 6f) * blend);
                    AnimateItems(t, UpgradeIconFx.DroneItemStarts, new Vector2(0f, 0.02f), 0.5f, size);
                    break;
                }
                case UpgradeIconFxKind.GasResistance:
                {
                    var cover = UpgradeIconFx.EaseOut(blend);
                    var tint = HandTint;
                    SetOverlay(overlayA, IconAlpha * Mathf.Clamp01(blend * 1.6f),
                        new Vector2(0f, -0.4f * size * (1f - cover)), 0f, tint);
                    break;
                }
                case UpgradeIconFxKind.MaximumEnergy:
                {
                    SetTint(GlowCyan, blend * (0.48f + 0.12f * Mathf.Sin(t * 9f)), 1.7f);
                    if (hovered)
                    {
                        EmitSparks(dt, Vector2.zero, -0.5f, 0.07f, 0.15f);
                    }

                    break;
                }
                case UpgradeIconFxKind.MaximumHealth:
                {
                    if (t < 0.7f)
                    {
                        var turn = UpgradeIconFx.SpinTurn(t * (UpgradeIconFx.SpinSeconds / 0.7f));
                        iconRect.localRotation = Quaternion.Euler(0f, 360f * turn, 0f);
                        iconRect.anchoredPosition = iconRest + new Vector2(0f, 0.15f * size * UpgradeTreeTween.Pulse(t / 0.7f));
                    }

                    Heal(t, size);
                    break;
                }
                case UpgradeIconFxKind.HealthRegeneration:
                {
                    if (t < UpgradeIconFx.SpinSeconds)
                    {
                        iconRect.localRotation = Quaternion.Euler(0f, 0f, -360f * UpgradeIconFx.SpinTurn(t));
                    }

                    Heal(t, size);
                    break;
                }
            }
        }

        private void Heal(float t, float size)
        {
            SetTint(HealRed, 0.5f * UpgradeTreeTween.Pulse(t / 0.8f), 1.5f);
            while (plusSpawned < 5 && t >= 0.08f + 0.12f * plusSpawned)
            {
                var x = (plusSpawned % 2 == 0 ? -1f : 1f) * UnityEngine.Random.Range(0.18f, 0.42f) * size;
                var pos = new Vector2(x, UnityEngine.Random.Range(-0.25f, 0.05f) * size);
                var plusSize = UnityEngine.Random.Range(0.15f, 0.22f) * size;
                Spawn(front, frontLayer, particleSprite, HealRed, pos, new Vector2(0f, 0.75f * size), 0f,
                    0.6f, plusSize, plusSize * 0.8f, 0f, 0f, 0.45f);
                plusSpawned++;
            }
        }

        private void Burst(int count, bool coins, float size)
        {
            for (var i = 0; i < count; i++)
            {
                var angle = UnityEngine.Random.Range(15f, 165f) * Mathf.Deg2Rad;
                var speed = UnityEngine.Random.Range(1.6f, 3.1f) * size;
                var vel = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed;
                var start = new Vector2(UnityEngine.Random.Range(-0.15f, 0.15f) * size, -0.02f * size);
                var bit = (coins ? UnityEngine.Random.Range(0.17f, 0.24f) : UnityEngine.Random.Range(0.12f, 0.2f)) * size;
                Spawn(front, frontLayer, particleSprite, Color.white, start, vel, -6.5f * size,
                    UnityEngine.Random.Range(0.5f, 0.8f), bit, bit * 0.85f,
                    UnityEngine.Random.Range(-420f, 420f), UnityEngine.Random.Range(0f, 360f), coins ? 0.35f : 0.5f);
            }
        }

        private void EmitSparks(float dt, Vector2 center, float radius, float minGap, float maxGap)
        {
            sparkTimer -= dt;
            if (sparkTimer > 0f)
            {
                return;
            }

            sparkTimer = UnityEngine.Random.Range(minGap, maxGap);
            var size = Size;
            Vector2 pos;
            float angle;
            if (radius >= 0f)
            {
                // 플러그 단자 부근: 지정 위치 주변 사각 영역.
                pos = new Vector2(
                    (center.x + UnityEngine.Random.Range(-radius, radius * 2f)) * size,
                    (center.y + UnityEngine.Random.Range(-radius * 0.6f, radius)) * size);
                angle = UnityEngine.Random.Range(0f, 360f);
            }
            else
            {
                // 아이콘 둘레의 고리 위.
                var a = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
                var r = UnityEngine.Random.Range(0.42f, 0.6f) * size;
                pos = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
                angle = a * Mathf.Rad2Deg + 90f + UnityEngine.Random.Range(-30f, 30f);
            }

            var length = UnityEngine.Random.Range(0.3f, 0.45f) * size;
            Spawn(front, frontLayer, particleSprite, SparkCyan, pos, Vector2.zero, 0f,
                UnityEngine.Random.Range(0.1f, 0.17f), length, length * 1.05f, 0f, angle, 0.3f);
        }

        private void SpawnRing(float size)
        {
            Spawn(back, backLayer, ringSprite, GlowCyan, new Vector2(0f, -0.02f * size), Vector2.zero, 0f,
                0.95f, 0.3f * size, 2f * size, 0f, 0f, 0f);
        }

        private void AnimateItems(float t, float[] starts, Vector2 target, float startHeight, float size)
        {
            EnsureItems();
            for (var i = 0; i < items.Length; i++)
            {
                var item = items[i];
                if (item == null)
                {
                    continue;
                }

                var phase = i < starts.Length ? UpgradeIconFx.ItemPhase(t, starts[i]) : -1f;
                if (phase < 0f)
                {
                    item.enabled = false;
                    continue;
                }

                item.enabled = true;
                var from = new Vector2((i - 1) * 0.3f * size, startHeight * size);
                var to = target * size;
                var fall = UpgradeIconFx.EaseIn(phase);
                var rect = item.rectTransform;
                rect.anchoredPosition = Vector2.Lerp(from, to, fall);
                var s = Mathf.Lerp(0.44f, 0.24f, phase) * size;
                rect.sizeDelta = new Vector2(s, s);
                rect.localRotation = Quaternion.Euler(0f, 0f, (i - 1) * -14f * (1f - phase));
                UpgradeTreeTween.SetColor(item, Color.white, UpgradeIconFx.ItemAlpha(phase) * IconAlpha);
            }
        }

        // ------------------------------------------------------------------ 보조

        private void CaptureRest()
        {
            if (restCaptured || icon == null)
            {
                return;
            }

            iconRest = icon.rectTransform.anchoredPosition;
            restCaptured = true;
        }

        private void RestorePose()
        {
            if (!poseDirty)
            {
                return;
            }

            poseDirty = false;
            if (icon != null && restCaptured)
            {
                icon.rectTransform.anchoredPosition = iconRest;
                icon.rectTransform.localScale = Vector3.one;
                icon.rectTransform.localRotation = Quaternion.identity;
            }

            if (icon != null)
            {
                icon.canvasRenderer.SetAlpha(1f);
            }

            SetOverlay(overlayA, 0f, Vector2.zero, 0f, Color.white);
            SetOverlay(overlayB, 0f, Vector2.zero, 0f, Color.white);
            SetTint(Color.clear, 0f, 1.6f);
            if (items != null)
            {
                for (var i = 0; i < items.Length; i++)
                {
                    if (items[i] != null)
                    {
                        items[i].enabled = false;
                    }
                }
            }
        }

        private static void SetOverlay(Image overlay, float alpha, Vector2 offset, float rotation, Color color)
        {
            if (overlay == null)
            {
                return;
            }

            overlay.enabled = alpha > 0.001f;
            UpgradeTreeTween.SetColor(overlay, color, alpha);
            overlay.rectTransform.anchoredPosition = offset;
            overlay.rectTransform.localRotation = Quaternion.Euler(0f, 0f, rotation);
        }

        private void SetTint(Color color, float alpha, float scale)
        {
            if (alpha <= 0.001f && tintGlow == null)
            {
                return;
            }

            if (tintGlow == null)
            {
                if (backLayer == null || glowSprite == null)
                {
                    return;
                }

                tintGlow = NewImage("TintGlow", backLayer, glowSprite);
                tintGlow.rectTransform.SetAsFirstSibling();
            }

            var size = Size * scale;
            tintGlow.rectTransform.sizeDelta = new Vector2(size, size);
            tintGlow.enabled = alpha > 0.001f;
            UpgradeTreeTween.SetColor(tintGlow, color, alpha);
        }

        private void EnsureItems()
        {
            if (items != null)
            {
                return;
            }

            items = new Image[itemSprites.Length];
            for (var i = 0; i < itemSprites.Length; i++)
            {
                if (frontLayer == null || itemSprites[i] == null)
                {
                    continue;
                }

                items[i] = NewImage("Item" + i, frontLayer, itemSprites[i]);
                items[i].enabled = false;
            }
        }

        private void Spawn(
            List<Particle> pool,
            RectTransform layer,
            Sprite sprite,
            Color color,
            Vector2 pos,
            Vector2 vel,
            float gravity,
            float life,
            float sizeFrom,
            float sizeTo,
            float spin,
            float angle,
            float fadeFrom)
        {
            if (layer == null || sprite == null)
            {
                return;
            }

            var capacity = pool == back ? BackPoolSize : FrontPoolSize;
            Particle particle = null;
            for (var i = 0; i < pool.Count; i++)
            {
                if (!pool[i].Active)
                {
                    particle = pool[i];
                    break;
                }
            }

            if (particle == null)
            {
                if (pool.Count >= capacity)
                {
                    return;
                }

                var image = NewImage("Fx" + pool.Count, layer, sprite);
                particle = new Particle { Image = image, Rect = image.rectTransform };
                pool.Add(particle);
            }

            particle.Image.sprite = sprite;
            particle.Pos = pos;
            particle.Vel = vel;
            particle.Gravity = gravity;
            particle.Life = 0f;
            particle.MaxLife = Mathf.Max(0.01f, life);
            particle.Spin = spin;
            particle.Angle = angle;
            particle.SizeFrom = sizeFrom;
            particle.SizeTo = sizeTo;
            particle.FadeFrom = Mathf.Clamp01(fadeFrom);
            particle.Color = color;
            particle.Active = true;
            particle.Image.gameObject.SetActive(true);
            DrawParticle(particle);
        }

        private static void UpdateParticles(List<Particle> pool, float dt)
        {
            for (var i = 0; i < pool.Count; i++)
            {
                var particle = pool[i];
                if (!particle.Active)
                {
                    continue;
                }

                particle.Life += dt;
                if (particle.Life >= particle.MaxLife)
                {
                    particle.Active = false;
                    particle.Image.gameObject.SetActive(false);
                    continue;
                }

                particle.Vel.y += particle.Gravity * dt;
                particle.Pos += particle.Vel * dt;
                particle.Angle += particle.Spin * dt;
                DrawParticle(particle);
            }
        }

        private static void DrawParticle(Particle particle)
        {
            var u = particle.Life / particle.MaxLife;
            var size = Mathf.Lerp(particle.SizeFrom, particle.SizeTo, UpgradeIconFx.EaseOut(u));
            particle.Rect.anchoredPosition = particle.Pos;
            particle.Rect.sizeDelta = new Vector2(size, size);
            particle.Rect.localRotation = Quaternion.Euler(0f, 0f, particle.Angle);
            var alpha = u <= particle.FadeFrom ? 1f : 1f - (u - particle.FadeFrom) / (1f - particle.FadeFrom);
            UpgradeTreeTween.SetColor(particle.Image, particle.Color, particle.Color.a * Mathf.Clamp01(alpha));
        }

        private static void DeactivateAll(List<Particle> pool)
        {
            for (var i = 0; i < pool.Count; i++)
            {
                pool[i].Active = false;
                if (pool[i].Image != null)
                {
                    pool[i].Image.gameObject.SetActive(false);
                }
            }
        }

        private static Image NewImage(string name, RectTransform parent, Sprite sprite)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }
    }
}

using System;
using SubTerra.App.UI.FacilityNameTag;
using SubTerra.App.UI.PlayerStatus;
using UnityEngine;

namespace SubTerra.App.Integration
{
    /// <summary>자원 아이콘 스프라이트를 위에서 아래로 가로 줄 단위로 자른 조각. 스캔이 줄마다 색을 바꾸는 데 쓴다.</summary>
    internal sealed class ResourceIconSlices
    {
        public const int MaxRows = 10;

        public Sprite Source;
        public Sprite[] Rows;
        /// <summary>줄(위=0)의 왼쪽 아래 기준 위치와 높이(스프라이트 단위, 아이콘 아래 중앙이 원점).</summary>
        public Vector2[] Origins;
        public float[] RowHeights;
        /// <summary>원본 스프라이트를 통째로 그릴 때, 바운딩 박스 아래 중앙이 원점에 오게 하는 위치.</summary>
        public Vector2 FullOrigin;
        public float Width;
        public float Height;
        private bool owned;

        public int Count => Rows != null ? Rows.Length : 0;

        public static ResourceIconSlices Build(Sprite source)
        {
            var slices = new ResourceIconSlices { Source = source };
            var ppu = Mathf.Max(0.01f, source.pixelsPerUnit);
            var tex = source.textureRect;
            slices.Width = tex.width / ppu;
            slices.Height = tex.height / ppu;
            var bounds = source.bounds;
            slices.FullOrigin = new Vector2(-bounds.center.x, -bounds.min.y);
            var heightPx = Mathf.RoundToInt(tex.height);
            var rotated = source.packed && source.packingRotation != SpritePackingRotation.None;
            var rows = rotated ? 1 : Mathf.Clamp(heightPx / 2, 1, MaxRows);
            slices.Rows = new Sprite[rows];
            slices.Origins = new Vector2[rows];
            slices.RowHeights = new float[rows];
            if (rows == 1)
            {
                // 한 줄이면 원본을 그대로 쓴다.
                slices.Rows[0] = source;
                slices.Origins[0] = slices.FullOrigin;
                slices.RowHeights[0] = slices.Height;
                return slices;
            }

            slices.owned = true;
            for (var k = 0; k < rows; k++)
            {
                var lower = Mathf.RoundToInt(heightPx * (rows - 1 - k) / (float)rows);
                var upper = Mathf.RoundToInt(heightPx * (rows - k) / (float)rows);
                var rect = new Rect(tex.x, tex.y + lower, tex.width, Mathf.Max(1, upper - lower));
                var row = Sprite.Create(source.texture, rect, new Vector2(0.5f, 0f), ppu, 0, SpriteMeshType.FullRect);
                row.hideFlags = HideFlags.HideAndDontSave;
                slices.Rows[k] = row;
                slices.Origins[k] = new Vector2(0f, lower / ppu);
                slices.RowHeights[k] = rect.height / ppu;
            }

            return slices;
        }

        public void Dispose()
        {
            if (!owned || Rows == null)
            {
                return;
            }

            foreach (var row in Rows)
            {
                if (row == null)
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    UnityEngine.Object.Destroy(row);
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(row);
                }
            }

            Rows = null;
        }
    }

    /// <summary>
    /// 수확 홀로그램 위에 뜨는 자원 아이콘 연출 하나. 스프라이트 렌더러만 쓰고 MainScreen 레이어에 둬 CCTV에 안 찍힌다.
    ///
    /// 구성(아래에서 위로):
    ///  - 프로젝터: 문구 프레임 윗면의 타원 바닥 링과, 거기서 아이콘으로 퍼지는 깜박이는 빔, 떠오르는 데이터 입자.
    ///  - 아이콘: 팝인(작게 시작해 튀어 오름) 뒤 둥실 떠 있다. 처음엔 청록 덮개+가로 줄무늬+색수차 유령(청록·분홍)으로
    ///    글리치하고, 두꺼운 스캔 줄(글로우 띠 포함)이 위에서 아래로 지나가며 줄마다 원래 색이 드러난다.
    ///  - 둘레 코너 브래킷 8개가 바깥에서 모여 맞물린다.
    ///  - 스캔이 끝나는 순간 섬광, 충격파 고리, 방사형 불꽃이 터지고, 이후엔 가끔 얇은 광택 줄이 훑고 지나간다.
    ///  - 엔진 연료: 더 큰 아이콘, 금빛 광선 8줄(천천히 회전), 따뜻한 후광 1회, 크고 금색인 충격파·불꽃, 반짝임 3개.
    /// 위치·표시 여부는 ResourcePickupVfx가 항목(PlayerStatusEntry)에 맞춰 정한다. 모션 감소 설정은 보지 않는다.
    /// </summary>
    public sealed class ResourceHologramIcon
    {
        private const int SortingBase = 122;
        private const int MoteCount = 14;
        private const int BurstMax = 20;
        private const int RayCount = 8;
        private static readonly Color Teal = new Color(0.25f, 0.95f, 0.92f, 1f);
        private static readonly Color CoverIcon = new Color(0.45f, 1f, 0.95f, 1f);
        private static readonly Color ScanColor = new Color(0.82f, 1f, 0.98f, 1f);
        private static readonly Color ShockTeal = new Color(0.45f, 1f, 0.93f, 1f);
        private static readonly Color Cyan = new Color(0.1f, 1f, 1f, 1f);
        private static readonly Color Magenta = new Color(1f, 0.25f, 0.85f, 1f);
        private static readonly Color Warm = new Color(1f, 0.72f, 0.2f, 1f);
        private static readonly Color WarmLight = new Color(1f, 0.92f, 0.55f, 1f);

        private readonly GameObject root;
        private readonly Transform pivot;
        private readonly Transform body;
        private readonly SpriteRenderer[] rowIcons = new SpriteRenderer[ResourceIconSlices.MaxRows];
        private readonly SpriteRenderer[] rowPlates = new SpriteRenderer[ResourceIconSlices.MaxRows];
        private readonly SpriteRenderer[] ghosts = new SpriteRenderer[2];
        private readonly SpriteRenderer scanLine;
        private readonly SpriteRenderer scanGlow;
        private readonly SpriteRenderer sweep;
        private readonly SpriteRenderer beam;
        private readonly SpriteRenderer baseRing;
        private readonly SpriteRenderer baseGlow;
        private readonly SpriteRenderer shock;
        private readonly SpriteRenderer flash;
        private readonly SpriteRenderer[] brackets = new SpriteRenderer[8];
        private readonly SpriteRenderer[] motes = new SpriteRenderer[MoteCount];
        private readonly SpriteRenderer[] burst = new SpriteRenderer[BurstMax];
        private SpriteRenderer halo;
        private SpriteRenderer[] sparks = Array.Empty<SpriteRenderer>();
        private SpriteRenderer[] rays = Array.Empty<SpriteRenderer>();
        private ResourceIconSlices slices;
        private float scanSeconds;
        private float scale = 1f;
        private float displayW;
        private float displayH;
        private float gap;
        private float releaseAge;
        private bool haloStarted;
        private bool burstStarted;

        internal ResourceHologramIcon(Transform parent)
        {
            root = new GameObject("ResourceHologramIcon") { layer = FacilityNameTagLayers.MainScreen };
            root.transform.SetParent(parent, false);
            pivot = NewTransform("Pivot", root.transform);
            body = NewTransform("Body", pivot);

            beam = NewRenderer("Beam", root.transform, ResourceHologramSprites.Beam, SortingBase - 2);
            baseGlow = NewRenderer("BaseGlow", root.transform, ResourceHologramSprites.RadialGlow, SortingBase - 2);
            baseRing = NewRenderer("BaseRing", root.transform, ResourceHologramSprites.Ring, SortingBase - 1);
            for (var i = 0; i < ghosts.Length; i++)
            {
                ghosts[i] = NewRenderer("Ghost" + i, body, null, SortingBase - 1);
            }

            for (var i = 0; i < ResourceIconSlices.MaxRows; i++)
            {
                rowIcons[i] = NewRenderer("Row" + i, body, null, SortingBase);
                rowPlates[i] = NewRenderer("Cover" + i, body, ResourceHologramSprites.Pixel, SortingBase + 1);
            }

            scanGlow = NewRenderer("ScanGlow", body, ResourceHologramSprites.Pixel, SortingBase + 2);
            scanLine = NewRenderer("ScanLine", body, ResourceHologramSprites.Pixel, SortingBase + 3);
            sweep = NewRenderer("Sweep", body, ResourceHologramSprites.Pixel, SortingBase + 3);
            for (var i = 0; i < brackets.Length; i++)
            {
                brackets[i] = NewRenderer("Bracket" + i, root.transform, ResourceHologramSprites.Pixel, SortingBase + 4);
            }

            for (var i = 0; i < motes.Length; i++)
            {
                motes[i] = NewRenderer("Mote" + i, root.transform, ResourceHologramSprites.Pixel, SortingBase + 5);
            }

            shock = NewRenderer("Shock", root.transform, ResourceHologramSprites.Ring, SortingBase + 5);
            flash = NewRenderer("Flash", root.transform, ResourceHologramSprites.RadialGlow, SortingBase + 5);
            for (var i = 0; i < burst.Length; i++)
            {
                burst[i] = NewRenderer("Burst" + i, root.transform, ResourceHologramSprites.Pixel, SortingBase + 6);
            }

            root.SetActive(false);
        }

        public bool IsActive { get; private set; }
        public bool IsRare { get; private set; }
        public bool IsReleasing { get; private set; }
        public float Age { get; private set; }
        public float Alpha { get; private set; }
        public PlayerStatusEntry Owner { get; private set; }
        /// <summary>붙을 때 항목이 준 장식 버전. 항목의 버전이 달라지면 이 아이콘은 물러난다.</summary>
        public int Version { get; private set; }
        public int HaloPlayCount { get; private set; }
        public int BurstPlayCount { get; private set; }
        /// <summary>아이콘 본체 배율을 거치기 전, 연출 전체가 놓이는 루트(문구 프레임 윗면 중앙).</summary>
        public Transform Root => root.transform;
        /// <summary>아이콘 본체(아이콘 크기에 맞춘 배율). 연료가 더 큰지 비교할 때 쓴다.</summary>
        public Transform Body => body;
        public float PopScaleNow => pivot.localScale.x;
        public int RowCount => slices != null ? slices.Count : 0;
        public float ScanSeconds => scanSeconds;
        public float ScanProgress => ResourceHologramTimeline.ScanProgress(Age, scanSeconds);
        public bool HaloVisible => halo != null && halo.enabled;
        public Color HaloColor => halo != null ? halo.color : Color.clear;
        public bool ScanLineVisible => scanLine.enabled;
        public bool BeamVisible => beam.enabled;
        public bool BaseRingVisible => baseRing.enabled;
        public bool ShockVisible => shock.enabled;
        public bool FlashVisible => flash.enabled;
        public bool GhostVisible => ghosts[0].enabled && ghosts[1].enabled;
        public Color ShockColor => shock.color;
        public int VisibleBracketCount => Count(brackets);
        public int VisibleMoteCount => Count(motes);
        public int VisibleBurstCount => Count(burst);
        public int VisibleSparkCount => Count(sparks);
        public int VisibleRayCount => Count(rays);

        public Color RowColor(int row) => rowIcons[row].color;
        /// <summary>청록 덮개(줄무늬)가 아직 이 줄에 남아 있는가.</summary>
        public bool RowCovered(int row) => rowPlates[row].enabled;
        public Vector3 RowOffset(int row) => rowIcons[row].transform.localPosition;

        private static int Count(SpriteRenderer[] list)
        {
            var n = 0;
            foreach (var item in list)
            {
                if (item != null && item.enabled)
                {
                    n++;
                }
            }

            return n;
        }

        internal void Begin(
            ResourceIconSlices source, float worldSize, float gapWorld, bool rare, PlayerStatusEntry owner, int version, float scan)
        {
            slices = source;
            Owner = owner;
            Version = version;
            IsRare = rare;
            IsActive = true;
            IsReleasing = false;
            releaseAge = 0f;
            Age = 0f;
            Alpha = 0f;
            haloStarted = false;
            burstStarted = false;
            HaloPlayCount = 0;
            BurstPlayCount = 0;
            gap = gapWorld;
            scanSeconds = ResourceHologramTimeline.ClampScan(scan);
            scale = worldSize / Mathf.Max(0.001f, Mathf.Max(source.Width, source.Height));
            displayW = source.Width * scale;
            displayH = source.Height * scale;
            body.localScale = Vector3.one * scale;
            body.localPosition = new Vector3(0f, -displayH * 0.5f, 0f);
            pivot.localPosition = new Vector3(0f, gap + displayH * 0.5f, 0f);
            root.SetActive(true);

            for (var i = 0; i < ResourceIconSlices.MaxRows; i++)
            {
                var used = i < source.Count;
                rowIcons[i].enabled = used;
                rowPlates[i].enabled = used;
                if (!used)
                {
                    continue;
                }

                rowIcons[i].sprite = source.Rows[i];
                var plate = rowPlates[i].transform;
                var pixelSize = ResourceHologramSprites.Pixel.bounds.size;
                plate.localScale = new Vector3(source.Width / pixelSize.x, source.RowHeights[i] / pixelSize.y, 1f);
                plate.localPosition = new Vector3(0f, source.Origins[i].y + source.RowHeights[i] * 0.5f, 0f);
            }

            foreach (var ghost in ghosts)
            {
                ghost.sprite = source.Source;
            }

            if (rare)
            {
                EnsureRareDetails();
            }

            if (halo != null)
            {
                halo.enabled = false;
            }

            SetAll(sparks, false);
            SetAll(rays, false);
            Apply(0f);
        }

        internal void StartRelease()
        {
            if (!IsReleasing)
            {
                IsReleasing = true;
                releaseAge = 0f;
            }
        }

        internal void Hide()
        {
            IsActive = false;
            IsReleasing = false;
            Owner = null;
            root.SetActive(false);
        }

        internal void SetPosition(Vector3 worldPosition)
        {
            root.transform.position = worldPosition;
        }

        /// <summary>시간을 진행하고 그린다. visibility는 수확 홀로그램이 보이는 정도(0~1)다.</summary>
        internal void Advance(float deltaSeconds, float visibility)
        {
            if (!IsActive)
            {
                return;
            }

            Age += Mathf.Max(0f, deltaSeconds);
            var envelope = Mathf.Min(ResourceHologramTimeline.FadeIn(Age), Mathf.Clamp01(visibility));
            if (IsReleasing)
            {
                releaseAge += Mathf.Max(0f, deltaSeconds);
                if (releaseAge >= ResourceHologramTimeline.ReleaseSeconds)
                {
                    Hide();
                    return;
                }

                envelope *= 1f - releaseAge / ResourceHologramTimeline.ReleaseSeconds;
            }

            Apply(envelope);
        }

        private void Apply(float alpha)
        {
            Alpha = alpha;
            if (slices == null)
            {
                return;
            }

            var progress = ScanProgress;
            var bob = ResourceHologramTimeline.Bob(Age) * displayH * 0.04f;
            var pop = ResourceHologramTimeline.PopScale(Age);
            pivot.localScale = Vector3.one * pop;
            var center = new Vector3(0f, gap + displayH * 0.5f + bob, 0f);
            pivot.localPosition = center;

            ApplyBody(alpha, progress);
            ApplyProjector(alpha, center);
            ApplyBrackets(alpha, center);
            ApplyBurst(alpha, center);
            if (IsRare)
            {
                ApplyRare(alpha, center);
            }
        }

        /// <summary>아이콘 본체: 줄 단위 색 드러남, 줄무늬 덮개, 색수차 유령, 스캔 줄과 글로우 띠, 스캔 뒤 광택 줄.</summary>
        private void ApplyBody(float alpha, float progress)
        {
            var rows = slices.Count;
            var jitterUnits = slices.Width * 0.08f;
            for (var r = 0; r < rows; r++)
            {
                var reveal = ResourceHologramTimeline.RowReveal(progress, r, rows);
                var cover = 1f - reveal;
                // 원래 색은 흰색 곱이라 그대로 보인다. 스캔 뒤에도 불투명에 가깝게 남겨 색이 묻히지 않게 한다.
                var color = Color.Lerp(CoverIcon, Color.white, reveal);
                var shimmer = 0.94f + 0.06f * Mathf.Sin(Age * 9f + r * 0.7f);
                // 방금 드러난 줄은 잠깐 더 밝게 번쩍인다.
                var edge = reveal > 0f && reveal < 1f ? 1f : 0f;
                color = Color.Lerp(color, ScanColor, edge * 0.55f);
                color.a = Mathf.Lerp(0.55f, shimmer, reveal) * alpha;
                rowIcons[r].color = color;
                rowIcons[r].transform.localPosition = new Vector3(
                    slices.Origins[r].x + cover * ResourceHologramTimeline.Jitter(r, Age) * jitterUnits,
                    slices.Origins[r].y,
                    0f);

                rowPlates[r].enabled = cover > 0.001f;
                var stripe = Mathf.Max(0f, ResourceHologramTimeline.StripeAlpha(r, Age));
                rowPlates[r].color = new Color(Teal.r, Teal.g, Teal.b, cover * stripe * alpha);
            }

            // 색수차 유령: 청록·분홍 복사본이 좌우로 어긋나 떨리다가 스캔이 끝나면 합쳐져 사라진다.
            var glitch = ResourceHologramTimeline.GlitchAmount(Age, scanSeconds);
            var step = (int)(Age * ResourceHologramTimeline.NoiseHz);
            var shift = slices.Width * (0.07f + 0.08f * ResourceHologramTimeline.Hash(step, 5)) * glitch;
            for (var i = 0; i < ghosts.Length; i++)
            {
                var ghost = ghosts[i];
                ghost.enabled = glitch > 0.001f && alpha > 0.001f;
                if (!ghost.enabled)
                {
                    continue;
                }

                var dir = i == 0 ? -1f : 1f;
                var lift = (ResourceHologramTimeline.Hash(step, 9 + i) - 0.5f) * slices.Height * 0.06f * glitch;
                ghost.transform.localPosition = new Vector3(slices.FullOrigin.x + dir * shift, slices.FullOrigin.y + lift, 0f);
                var tint = i == 0 ? Cyan : Magenta;
                tint.a = 0.5f * glitch * alpha;
                ghost.color = tint;
            }

            // 스캔 줄(밝은 심지)과 그 뒤를 따르는 글로우 띠.
            scanLine.enabled = progress > 0f && progress < 1f;
            scanGlow.enabled = scanLine.enabled;
            if (scanLine.enabled)
            {
                var size = ResourceHologramSprites.Pixel.bounds.size;
                var y = slices.Height * (1f - progress);
                scanLine.transform.localScale = new Vector3(slices.Width * 1.3f / size.x, slices.Height * 0.06f / size.y, 1f);
                scanLine.transform.localPosition = new Vector3(0f, y, 0f);
                scanLine.color = new Color(1f, 1f, 1f, alpha);
                var bandTop = Mathf.Min(slices.Height * 1.05f, y + slices.Height * 0.3f);
                var band = Mathf.Max(0.0001f, bandTop - y);
                scanGlow.transform.localScale = new Vector3(slices.Width * 1.15f / size.x, band / size.y, 1f);
                scanGlow.transform.localPosition = new Vector3(0f, y + band * 0.5f, 0f);
                scanGlow.color = new Color(Teal.r, Teal.g, Teal.b, 0.42f * alpha);
            }

            // 스캔 뒤: 가끔 얇은 광택 줄이 위에서 아래로 훑는다.
            var idle = Age - scanSeconds - 0.45f;
            var sweepK = idle < 0f ? -1f : (idle % 1.5f) / 0.24f;
            sweep.enabled = sweepK >= 0f && sweepK <= 1f && alpha > 0.001f;
            if (sweep.enabled)
            {
                var size = ResourceHologramSprites.Pixel.bounds.size;
                sweep.transform.localScale = new Vector3(slices.Width * 1.05f / size.x, slices.Height * 0.04f / size.y, 1f);
                sweep.transform.localPosition = new Vector3(0f, slices.Height * (1f - sweepK), 0f);
                sweep.color = new Color(ScanColor.r, ScanColor.g, ScanColor.b, 0.32f * Mathf.Sin(sweepK * Mathf.PI) * alpha);
            }
        }

        /// <summary>프로젝터: 바닥 링과 글로우, 프레임에서 아이콘으로 퍼지는 빔, 떠오르는 데이터 입자.</summary>
        private void ApplyProjector(float alpha, Vector3 center)
        {
            var width = Mathf.Max(displayW, displayH);
            var intro = ResourceHologramTimeline.EaseOut(Age / 0.25f);
            var pulse = 0.78f + 0.22f * Mathf.Sin(Age * 6.5f);

            baseRing.enabled = alpha > 0.001f;
            var ringWidth = width * (0.35f + 0.8f * intro);
            baseRing.transform.localScale = new Vector3(ringWidth, ringWidth * 0.26f, 1f);
            baseRing.transform.localPosition = new Vector3(0f, 0.012f, 0f);
            baseRing.color = new Color(ScanColor.r, ScanColor.g, ScanColor.b, 0.85f * pulse * alpha);
            baseGlow.enabled = baseRing.enabled;
            var glowWidth = width * (0.5f + 0.9f * intro);
            baseGlow.transform.localScale = new Vector3(glowWidth, glowWidth * 0.34f, 1f);
            baseGlow.transform.localPosition = new Vector3(0f, 0.01f, 0f);
            baseGlow.color = new Color(Teal.r, Teal.g, Teal.b, 0.5f * alpha);

            beam.enabled = alpha > 0.001f;
            var beamSize = beam.sprite.bounds.size;
            var beamHeight = center.y;
            var flicker = 0.85f + 0.3f * ResourceHologramTimeline.Hash((int)(Age * 30f), 21);
            beam.transform.localScale = new Vector3(width * 1.25f / beamSize.x, beamHeight / beamSize.y, 1f);
            beam.transform.localPosition = Vector3.zero;
            var beamStrength = Age < scanSeconds ? 0.5f : 0.3f;
            beam.color = new Color(Teal.r, Teal.g, Teal.b, beamStrength * flicker * alpha * intro);

            // 데이터 입자: 바닥에서 떠올라 위로 갈수록 퍼지며 사라진다. 처음 0.9초는 모두, 이후엔 일부만.
            var pixel = ResourceHologramSprites.Pixel.bounds.size;
            var visible = Age < 0.9f ? MoteCount : MoteCount / 2;
            for (var i = 0; i < motes.Length; i++)
            {
                var mote = motes[i];
                mote.enabled = i < visible && alpha > 0.001f;
                if (!mote.enabled)
                {
                    continue;
                }

                var seed = ResourceHologramTimeline.Hash(i, 3);
                var t = (Age * (0.55f + 0.5f * seed) + seed * 3f) % 1f;
                var spread = (ResourceHologramTimeline.Hash(i, 7) - 0.5f) * width * (0.35f + 0.65f * t);
                var dot = width * (0.03f + 0.025f * seed);
                mote.transform.localPosition = new Vector3(spread, t * center.y * 1.05f, 0f);
                mote.transform.localScale = new Vector3(dot / pixel.x, dot * 1.6f / pixel.y, 1f);
                mote.color = new Color(ScanColor.r, ScanColor.g, ScanColor.b, Mathf.Sin(t * Mathf.PI) * 0.85f * alpha);
            }
        }

        /// <summary>아이콘 둘레 코너 브래킷 8개: 바깥에서 모여 맞물리며 흰색 → 청록으로 가라앉는다.</summary>
        private void ApplyBrackets(float alpha, Vector3 center)
        {
            var k = ResourceHologramTimeline.BracketProgress(Age);
            var pad = width() * 0.1f + width() * 0.35f * (1f - k);
            var halfW = displayW * 0.5f + pad;
            var halfH = displayH * 0.5f + pad;
            var length = width() * 0.26f;
            var thick = Mathf.Max(0.008f, width() * 0.035f);
            var pixel = ResourceHologramSprites.Pixel.bounds.size;
            var settle = Mathf.Clamp01(Age / 0.4f);
            var color = Color.Lerp(Color.white, Teal, settle);
            color.a = Mathf.Lerp(1f, 0.7f, settle) * (0.85f + 0.15f * Mathf.Sin(Age * 5f)) * alpha;
            for (var c = 0; c < 4; c++)
            {
                var sx = (c & 1) == 1 ? 1f : -1f;
                var sy = (c & 2) == 2 ? 1f : -1f;
                var corner = center + new Vector3(sx * halfW, sy * halfH, 0f);
                var horizontal = brackets[c * 2];
                var vertical = brackets[c * 2 + 1];
                horizontal.enabled = vertical.enabled = alpha > 0.001f;
                horizontal.transform.localScale = new Vector3(length / pixel.x, thick / pixel.y, 1f);
                horizontal.transform.localPosition = corner + new Vector3(-sx * length * 0.5f, 0f, 0f);
                vertical.transform.localScale = new Vector3(thick / pixel.x, length / pixel.y, 1f);
                vertical.transform.localPosition = corner + new Vector3(0f, -sy * length * 0.5f, 0f);
                horizontal.color = vertical.color = color;
            }
        }

        private float width()
        {
            return Mathf.Max(displayW, displayH);
        }

        /// <summary>스캔이 끝나는 순간: 섬광, 충격파 고리, 방사형 불꽃. 연료는 금색에 더 크고 불꽃이 많다.</summary>
        private void ApplyBurst(float alpha, Vector3 center)
        {
            var k = ResourceHologramTimeline.BurstProgress(Age, scanSeconds);
            var active = k >= 0f && alpha > 0.001f;
            if (active && !burstStarted)
            {
                burstStarted = true;
                BurstPlayCount++;
            }

            var main = IsRare ? Warm : Teal;
            var light = IsRare ? WarmLight : ShockTeal;
            var reach = width() * (IsRare ? 1.5f : 1f);
            var fade = 1f - k;

            shock.enabled = active;
            flash.enabled = active && k < 0.35f;
            if (active)
            {
                var d = reach * (0.7f + 1.9f * ResourceHologramTimeline.EaseOut(k));
                shock.transform.localScale = new Vector3(d, d, 1f);
                shock.transform.localPosition = center;
                shock.color = new Color(light.r, light.g, light.b, 0.7f * fade * fade * alpha);
            }

            if (flash.enabled)
            {
                var d = width() * (IsRare ? 2.6f : 1.9f);
                flash.transform.localScale = new Vector3(d, d, 1f);
                flash.transform.localPosition = center;
                flash.color = new Color(1f, 1f, 1f, (IsRare ? 0.85f : 0.5f) * (1f - k / 0.35f) * alpha);
            }

            var count = IsRare ? BurstMax : 12;
            var pixel = ResourceHologramSprites.Pixel.bounds.size;
            for (var i = 0; i < burst.Length; i++)
            {
                var spark = burst[i];
                spark.enabled = active && i < count;
                if (!spark.enabled)
                {
                    continue;
                }

                var angle = (i / (float)count) * Mathf.PI * 2f + (ResourceHologramTimeline.Hash(i, 2) - 0.5f) * 0.5f;
                var speed = 0.65f + 0.7f * ResourceHologramTimeline.Hash(i, 4);
                var distance = reach * (0.45f + 1.1f * speed * ResourceHologramTimeline.EaseOut(k));
                var length = reach * 0.16f * speed * fade + 0.004f;
                var dir = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
                spark.transform.localPosition = center + dir * distance;
                spark.transform.localRotation = Quaternion.Euler(0f, 0f, angle * Mathf.Rad2Deg);
                spark.transform.localScale = new Vector3(length / pixel.x, Mathf.Max(0.006f, reach * 0.018f) / pixel.y, 1f);
                spark.color = new Color(
                    Mathf.Lerp(light.r, main.r, k), Mathf.Lerp(light.g, main.g, k), Mathf.Lerp(light.b, main.b, k), fade * alpha);
            }
        }

        /// <summary>엔진 연료 전용: 금빛 광선, 따뜻한 후광 1회, 반짝임 3개.</summary>
        private void ApplyRare(float alpha, Vector3 center)
        {
            // 후광은 스캔이 끝난 뒤에만, 한 번. 청록이면 홀로그램에 묻히므로 금색·주황으로 둔다.
            var haloProgress = ResourceHologramTimeline.FuelHaloProgress(Age, scanSeconds);
            var haloAlpha = ResourceHologramTimeline.FuelHaloAlpha(haloProgress) * alpha;
            halo.enabled = haloProgress >= 0f && haloAlpha > 0.001f;
            if (halo.enabled)
            {
                if (!haloStarted)
                {
                    haloStarted = true;
                    HaloPlayCount++;
                }

                var diameter = width() * 1.8f * ResourceHologramTimeline.FuelHaloScale(haloProgress);
                var size = halo.sprite.bounds.size;
                halo.transform.localScale = new Vector3(diameter / size.x, diameter / size.y, 1f);
                halo.transform.localPosition = center;
                halo.color = new Color(Warm.r, Warm.g, Warm.b, 0.9f * haloAlpha);
            }

            // 금빛 광선: 스캔이 끝나면 아이콘 뒤에서 퍼져 나와 천천히 돈다.
            var rayFade = Mathf.Clamp01((Age - scanSeconds) / 0.2f);
            var pixel = ResourceHologramSprites.Pixel.bounds.size;
            for (var i = 0; i < rays.Length; i++)
            {
                var ray = rays[i];
                ray.enabled = rayFade > 0f && alpha > 0.001f;
                if (!ray.enabled)
                {
                    continue;
                }

                var angle = i * (360f / rays.Length) + Age * 24f;
                var length = width() * (0.8f + 0.3f * ((i & 1) == 0 ? 1f : 0f));
                var rad = angle * Mathf.Deg2Rad;
                ray.transform.localPosition = center + new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f) * length * 0.5f;
                ray.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
                ray.transform.localScale = new Vector3(length / pixel.x, width() * 0.045f / pixel.y, 1f);
                var shimmer = 0.75f + 0.25f * Mathf.Sin(Age * 6f + i * 1.3f);
                ray.color = new Color(WarmLight.r, WarmLight.g, WarmLight.b, 0.3f * shimmer * rayFade * alpha);
            }

            // 반짝임 3개. 색이 드러나기 시작할 때부터 아이콘 둘레에서 깜박인다.
            var sparkFade = Mathf.Clamp01((Age - scanSeconds * 0.5f) / 0.1f);
            var radius = width() * 0.72f;
            for (var i = 0; i < sparks.Length; i++)
            {
                var spark = sparks[i];
                spark.enabled = sparkFade > 0f && alpha > 0.001f;
                if (!spark.enabled)
                {
                    continue;
                }

                var angle = (90f + i * 120f) * Mathf.Deg2Rad + Age * 1.6f;
                spark.transform.localPosition = center + new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f);
                var twinkle = 0.35f + 0.65f * Mathf.Abs(Mathf.Sin(Age * 7f + i * 2.1f));
                spark.color = new Color(WarmLight.r, WarmLight.g, WarmLight.b, twinkle * sparkFade * alpha);
            }
        }

        private void EnsureRareDetails()
        {
            if (halo == null)
            {
                halo = NewRenderer("FuelHalo", root.transform, ResourceHologramSprites.RadialGlow, SortingBase - 1);
                sparks = new SpriteRenderer[3];
                for (var i = 0; i < sparks.Length; i++)
                {
                    sparks[i] = NewRenderer("FuelSpark" + i, root.transform, ResourceHologramSprites.Pixel, SortingBase + 7);
                }

                rays = new SpriteRenderer[RayCount];
                for (var i = 0; i < rays.Length; i++)
                {
                    rays[i] = NewRenderer("FuelRay" + i, root.transform, ResourceHologramSprites.Pixel, SortingBase - 1);
                }
            }

            // 월드 약 0.035 크기의 반짝임 점. 아이콘 크기와 무관하게 같은 크기를 쓴다.
            var size = ResourceHologramSprites.Pixel.bounds.size;
            const float dot = 0.035f;
            foreach (var spark in sparks)
            {
                spark.transform.localScale = new Vector3(dot / size.x, dot / size.y, 1f);
                spark.enabled = false;
            }
        }

        private static void SetAll(SpriteRenderer[] list, bool value)
        {
            foreach (var item in list)
            {
                item.enabled = value;
            }
        }

        private static Transform NewTransform(string name, Transform parent)
        {
            var go = new GameObject(name) { layer = FacilityNameTagLayers.MainScreen };
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        private static SpriteRenderer NewRenderer(string name, Transform parent, Sprite sprite, int order)
        {
            var go = new GameObject(name) { layer = FacilityNameTagLayers.MainScreen };
            go.transform.SetParent(parent, false);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = order;
            return renderer;
        }

        internal void Destroy()
        {
            if (root == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(root);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }
    }
}

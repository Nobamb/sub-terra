using System;
using SubTerra.App.Core.Data;
using SubTerra.App.UI.FacilityNameTag;
using SubTerra.Gameplay.Mining;
using SubTerra.Shared;
using UnityEngine;

namespace SubTerra.App.Integration
{
    /// <summary>지급 확정된 자원만 회수 표시한다. 보상·충돌·소리·채굴 파편을 만들지 않는다.</summary>
    public sealed class ResourcePickupVfx : MonoBehaviour
    {
        public const int Capacity = 8;
        [SerializeField, Min(0.05f)] private float normalDuration = 0.42f;
        [SerializeField, Min(0.05f)] private float rareDuration = 0.7f;
        private sealed class Flight
        {
            public GameObject Root;
            public SpriteRenderer Icon, Halo;
            public SpriteRenderer[] Sparks = Array.Empty<SpriteRenderer>();
            public Vector3 Start;
            public float Age, Duration;
            public bool Rare, Reduced, Active;
        }
        private static readonly Color RareColor = new Color(0.3f, 0.94f, 1f, 1f);
        private readonly Flight[] flights = new Flight[Capacity];
        private MiningSystem mining;
        private Transform player;
        private Func<string, Sprite> iconResolver;
        private string pendingId;
        private int pendingQuantity, next;
        public bool ManualTick { get; set; }
        public int ActiveCount { get { var n = 0; foreach (var f in flights) if (f != null && f.Active) n++; return n; } }
        public int AllocatedCount { get { var n = 0; foreach (var f in flights) if (f != null) n++; return n; } }

        public void BindTo(MiningSystem source, Transform target, Func<string, Sprite> resolveIcon)
        {
            Unbind();
            mining = source;
            player = target;
            iconResolver = resolveIcon;
            if (mining == null) return;
            mining.TileMined += OnMined;
            mining.ProgressChanged += OnProgress;
        }

        public void Unbind()
        {
            if (mining != null)
            {
                mining.TileMined -= OnMined;
                mining.ProgressChanged -= OnProgress;
            }
            mining = null;
            player = null;
            iconResolver = null;
            pendingId = null;
            pendingQuantity = 0;
            foreach (var f in flights) if (f != null) Hide(f);
        }

        public void QueueAccepted(string itemId, int quantity)
        {
            pendingId = quantity > 0 ? itemId : null;
            pendingQuantity = Mathf.Max(0, quantity);
        }

        private void OnProgress(MiningProgressState state)
        {
            if (state.Phase == MiningPhase.Failed || state.Phase == MiningPhase.Cancelled)
                QueueAccepted(null, 0);
        }

        private void OnMined(Vector3Int cell, MiningTileDto tile)
        {
            var id = pendingId;
            var quantity = pendingQuantity;
            QueueAccepted(null, 0);
            if (!string.Equals(id, tile.mineralId, StringComparison.Ordinal)) return;
            var map = mining != null ? mining.ForegroundTilemap : null;
            var origin = map != null ? map.GetCellCenterWorld(cell) : (Vector3)cell + new Vector3(0.5f, 0.5f, 0f);
            PlayAccepted(id, quantity, origin);
        }

        public bool PlayAccepted(string itemId, int quantity, Vector3 origin)
        {
            if (!isActiveAndEnabled || player == null || quantity <= 0 || string.IsNullOrEmpty(itemId)) return false;
            var sprite = iconResolver != null ? iconResolver(itemId) : null;
            if (sprite == null) return false;
            var f = flights[next];
            if (f == null) flights[next] = f = CreateFlight();
            next = (next + 1) % Capacity;
            f.Rare = itemId == DataIds.RareItems.EngineFuel;
            if (f.Rare && f.Halo == null) CreateRareDetails(f);
            f.Reduced = AccessibilityPreferences.ReduceMotion;
            f.Start = f.Reduced ? TargetPosition : origin;
            f.Duration = f.Reduced ? 0.16f : Mathf.Max(0.05f, f.Rare ? rareDuration : normalDuration);
            f.Age = 0f;
            f.Active = true;
            f.Icon.sprite = sprite;
            f.Icon.transform.localScale = Vector3.one * ((f.Rare ? 0.23f : 0.16f)
                / Mathf.Max(0.001f, Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y)));
            f.Root.SetActive(true);
            Apply(f);
            return true;
        }

        private Vector3 TargetPosition => player.position + Vector3.up * 0.12f;
        private void Update() { if (!ManualTick) Tick(Time.deltaTime); }
        private void OnDisable() => Unbind();
        private void OnDestroy()
        {
            Unbind();
            foreach (var f in flights)
                if (f != null && f.Root != null)
                {
                    if (Application.isPlaying) Destroy(f.Root);
                    else DestroyImmediate(f.Root);
                }
        }

        public void Tick(float deltaTime)
        {
            foreach (var f in flights)
            {
                if (f == null || !f.Active) continue;
                f.Age += Mathf.Max(0f, deltaTime);
                if (player == null || f.Age >= f.Duration
                    || (TargetPosition - f.Start).sqrMagnitude > 64f)
                { Hide(f); continue; }
                Apply(f);
            }
        }

        private void Apply(Flight f)
        {
            var p = Mathf.Clamp01(f.Age / f.Duration);
            var alpha = Mathf.Clamp01(Mathf.Min(p / 0.08f, (1f - p) / 0.2f));
            var position = Vector3.Lerp(f.Start, TargetPosition, p * p);
            if (!f.Reduced) position.y += Mathf.Sin(p * Mathf.PI) * (f.Rare ? 0.32f : 0.2f);
            f.Root.transform.position = position;
            f.Icon.color = new Color(1f, 1f, 1f, alpha);
            if (f.Halo != null)
            {
                f.Halo.enabled = f.Rare;
                f.Halo.color = new Color(RareColor.r, RareColor.g, RareColor.b, alpha * 0.55f);
                f.Halo.transform.localRotation = Quaternion.Euler(0f, 0f, 45f + (f.Reduced ? 0f : p * 90f));
            }
            for (var i = 0; i < f.Sparks.Length; i++)
            {
                var s = f.Sparks[i];
                s.enabled = f.Rare;
                var angle = i * Mathf.PI * 2f / f.Sparks.Length + (f.Reduced ? 0f : p * 3f);
                s.transform.localPosition = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * 0.19f;
                s.color = new Color(RareColor.r, RareColor.g, RareColor.b, alpha * 0.7f);
            }
        }

        private Flight CreateFlight()
        {
            var root = new GameObject("ResourcePickupFlight");
            root.transform.SetParent(transform, false);
            var f = new Flight { Root = root };
            f.Icon = NewRenderer("ResourceIcon", root.transform, null, 122);
            return f;
        }

        private static void CreateRareDetails(Flight f)
        {
            f.Sparks = new SpriteRenderer[3];
            f.Halo = NewRenderer("RareHalo", f.Root.transform, GoldPickupPopupSprites.RoundRect(12, 1.5f), 121);
            f.Halo.transform.localScale = Vector3.one * (0.34f / f.Halo.sprite.bounds.size.x);
            for (var i = 0; i < f.Sparks.Length; i++)
            {
                f.Sparks[i] = NewRenderer("RareSpark", f.Root.transform, GoldPickupPopupSprites.RoundRect(1, 0f), 123);
                f.Sparks[i].transform.localScale = Vector3.one * (0.025f / f.Sparks[i].sprite.bounds.size.x);
            }
        }

        private static SpriteRenderer NewRenderer(string name, Transform parent, Sprite sprite, int order)
        {
            var root = new GameObject(name) { layer = FacilityNameTagLayers.MainScreen };
            root.transform.SetParent(parent, false);
            var renderer = root.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = order;
            return renderer;
        }

        private static void Hide(Flight f) { f.Active = false; f.Root.SetActive(false); }
    }
}

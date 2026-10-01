using System.Collections.Generic;
using SubTerra.Shared;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace SubTerra.Gameplay.Mining
{
    /// <summary>Bounded, reusable texture fragments driven only by accepted mining progress.</summary>
    public sealed class MiningDebrisVisual : MonoBehaviour
    {
        private const int Capacity = 64;
        private sealed class Chip
        {
            public SpriteRenderer Renderer;
            public Vector2 Velocity;
            public float Age;
            public float Lifetime;
            public float Spin;
            public float Size;
        }

        private MiningSystem mining;
        private Transform miner;
        private Tilemap map;
        private GameObject root;
        private readonly Chip[] chips = new Chip[Capacity];
        private readonly Dictionary<Sprite, Sprite[]> fragments = new();
        // Visual randomness must not consume Unity's gameplay random sequence.
        private readonly System.Random random = new();
        private int nextChip;
        private Sprite[] activeFragments;
        private Color activeTint;
        private Vector3 contact;
        private Vector2 outward;
        private float contactClock;

        public void Configure(MiningSystem system, Transform source)
        {
            Unsubscribe();
            mining = system;
            miner = source;
            map = system != null ? system.ForegroundTilemap : null;
            if (isActiveAndEnabled) Subscribe();
        }

        private void OnEnable() => Subscribe();
        private void Subscribe()
        {
            if (mining == null) return;
            mining.ProgressChanged -= OnProgress;
            mining.TileMined -= OnMined;
            mining.ProgressChanged += OnProgress;
            mining.TileMined += OnMined;
        }

        private void Unsubscribe()
        {
            if (mining == null) return;
            mining.ProgressChanged -= OnProgress;
            mining.TileMined -= OnMined;
        }

        private void OnDisable()
        {
            Unsubscribe();
            activeFragments = null;
            foreach (Chip chip in chips)
                if (chip != null && chip.Renderer != null) chip.Renderer.enabled = false;
        }

        private void OnDestroy()
        {
            Unsubscribe();
            if (root != null) DestroyOwned(root);
            foreach (Sprite[] variants in fragments.Values)
                foreach (Sprite sprite in variants)
                    if (sprite != null) DestroyOwned(sprite);
        }

        private static void DestroyOwned(Object value)
        {
            if (Application.isPlaying) Destroy(value);
            else DestroyImmediate(value);
        }

        private void OnProgress(MiningProgressState state)
        {
            if (state.Phase != MiningPhase.Mining)
            {
                activeFragments = null;
                return;
            }
            if (state.Progress > 0f || map == null || miner == null) return;
            Sprite sprite = map.GetSprite(mining.ActiveCell);
            activeFragments = GetFragments(sprite);
            TileBase tile = map.GetTile(mining.ActiveCell);
            activeTint = tile is Tile colored ? colored.color : Color.white;
            Vector3 center = map.GetCellCenterWorld(mining.ActiveCell);
            Vector2 delta = miner.position - center;
            outward = Mathf.Abs(delta.x) > Mathf.Abs(delta.y)
                ? new Vector2(Mathf.Sign(delta.x), 0f) : new Vector2(0f, Mathf.Sign(delta.y));
            if (outward == Vector2.zero) outward = Vector2.up;
            Vector3 halfCell = map.CellToWorld(Vector3Int.one) - map.CellToWorld(Vector3Int.zero);
            contact = center + new Vector3(outward.x * Mathf.Abs(halfCell.x) * 0.49f,
                outward.y * Mathf.Abs(halfCell.y) * 0.49f, 0f);
            contactClock = 0f;
            Emit(2, false);
        }

        private void OnMined(Vector3Int cell, MiningTileDto definition)
        {
            // This event is published only after the tile and reward transaction succeeded.
            if (activeFragments != null && cell == mining.ActiveCell) Emit(14, true);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (mining != null && mining.IsMining && activeFragments != null)
            {
                contactClock += dt;
                if (contactClock >= 0.075f)
                {
                    contactClock %= 0.075f;
                    Emit(2, false);
                }
            }
            foreach (Chip chip in chips)
            {
                if (chip == null || !chip.Renderer.enabled) continue;
                chip.Age += dt;
                if (chip.Age >= chip.Lifetime)
                {
                    chip.Renderer.enabled = false;
                    continue;
                }
                chip.Velocity += Vector2.down * (5.5f * dt);
                chip.Renderer.transform.position += (Vector3)(chip.Velocity * dt);
                chip.Renderer.transform.Rotate(0f, 0f, chip.Spin * dt);
                Color tint = chip.Renderer.color;
                tint.a = 1f - Mathf.SmoothStep(0f, 1f, chip.Age / chip.Lifetime);
                chip.Renderer.color = tint;
            }
        }

        private Sprite[] GetFragments(Sprite source)
        {
            if (source == null) return null;
            if (fragments.TryGetValue(source, out Sprite[] cached)) return cached;
            cached = new Sprite[4];
            Rect rect = source.rect;
            // Keep the original texture and crop small patches; no texture readback or imports.
            float side = Mathf.Max(1f, Mathf.Min(rect.width, rect.height) * 0.15f);
            for (int i = 0; i < cached.Length; i++)
            {
                float x = rect.x + (rect.width - side) * (i % 2 == 0 ? 0.28f : 0.72f);
                float y = rect.y + (rect.height - side) * (i < 2 ? 0.28f : 0.72f);
                cached[i] = Sprite.Create(source.texture, new Rect(x, y, side, side),
                    new Vector2(0.5f, 0.5f), source.pixelsPerUnit, 0, SpriteMeshType.FullRect);
                cached[i].name = source.name + " mining fragment " + i;
            }
            fragments.Add(source, cached);
            return cached;
        }

        private void Emit(int count, bool burst)
        {
            if (activeFragments == null) return;
            if (root == null)
            {
                root = new GameObject("Mining debris pool");
                // World-space fragments remain at the mined face when the miner moves.
                root.transform.SetParent(map.transform, false);
            }
            TilemapRenderer terrain = map.GetComponent<TilemapRenderer>();
            for (int i = 0; i < count; i++)
            {
                int index = nextChip++ % Capacity;
                Chip chip = chips[index];
                if (chip == null)
                {
                    var part = new GameObject("Rock fragment", typeof(SpriteRenderer));
                    part.transform.SetParent(root.transform, false);
                    chip = chips[index] = new Chip { Renderer = part.GetComponent<SpriteRenderer>() };
                }
                Sprite sprite = activeFragments[random.Next(activeFragments.Length)];
                chip.Renderer.sprite = sprite;
                chip.Renderer.color = activeTint;
                chip.Renderer.sortingLayerID = terrain != null ? terrain.sortingLayerID : 0;
                chip.Renderer.sortingOrder = terrain != null ? terrain.sortingOrder + 2 : 2;
                chip.Renderer.enabled = true;
                chip.Size = burst ? Range(0.035f, 0.085f) : Range(0.018f, 0.038f);
                chip.Renderer.transform.localScale = Vector3.one * (chip.Size / sprite.bounds.size.x);
                chip.Renderer.transform.position = contact + new Vector3(Range(-0.06f, 0.06f), Range(-0.06f, 0.06f));
                chip.Renderer.transform.rotation = Quaternion.Euler(0f, 0f, Range(0f, 360f));
                chip.Age = 0f;
                chip.Lifetime = burst ? Range(0.32f, 0.65f) : Range(0.18f, 0.35f);
                chip.Spin = Range(-260f, 260f);
                chip.Velocity = outward * Range(0.6f, burst ? 2.2f : 1.1f)
                    + new Vector2(Range(-0.65f, 0.65f), Range(0.45f, 1.65f));
            }
        }

        private float Range(float minimum, float maximum) => Mathf.Lerp(minimum, maximum, (float)random.NextDouble());
    }
}

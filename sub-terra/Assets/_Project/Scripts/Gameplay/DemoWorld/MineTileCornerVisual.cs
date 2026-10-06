using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace SubTerra.Gameplay.DemoWorld
{
    /// <summary>Clips only convex air-facing corners; logical tiles and colliders stay intact.</summary>
    [RequireComponent(typeof(Tilemap), typeof(TilemapRenderer))]
    public sealed class MineTileCornerVisual : MonoBehaviour
    {
        private Tilemap map;
        private TilemapRenderer mapRenderer;
        private Material originalMaterial;
        private Material material;
        private Texture2D cornerTexture;
        private BoundsInt bounds;
        private bool rebuild = true;
        private readonly HashSet<Vector3Int> dirty = new();
        private static readonly int CornerMap = Shader.PropertyToID("_CornerMap");
        private static readonly int CornerBounds = Shader.PropertyToID("_CornerBounds");
        private static readonly int WorldToCell = Shader.PropertyToID("_WorldToCell");
        private static readonly int CornerRadius = Shader.PropertyToID("_CornerRadius");

        private void OnEnable()
        {
            map = GetComponent<Tilemap>();
            mapRenderer = GetComponent<TilemapRenderer>();
            Shader shader = Resources.Load<Shader>("MineRoundedCorners");
            if (shader == null || map.layoutGrid == null
                || map.layoutGrid.cellLayout != GridLayout.CellLayout.Rectangle) return;
            originalMaterial = mapRenderer.sharedMaterial;
            material = new Material(shader) { name = "Mine exposed corner material" };
            if (originalMaterial != null)
            {
                material.CopyPropertiesFromMaterial(originalMaterial);
                material.SetFloat("_UseLighting", originalMaterial.shader.name.Contains("Sprite-Lit") ? 1f : 0f);
            }
            // Sprite material copying must not override the terrain shader's authored radius.
            material.SetFloat(CornerRadius, shader.GetPropertyDefaultFloatValue(shader.FindPropertyIndex("_CornerRadius")));
            mapRenderer.sharedMaterial = material;
            rebuild = true;
            Tilemap.tilemapTileChanged += OnTilesChanged;
            RefreshCorners();
        }

        private void OnDisable()
        {
            Tilemap.tilemapTileChanged -= OnTilesChanged;
            if (mapRenderer != null && mapRenderer.sharedMaterial == material)
                mapRenderer.sharedMaterial = originalMaterial;
            if (material != null) Destroy(material);
            if (cornerTexture != null) Destroy(cornerTexture);
            material = null;
            cornerTexture = null;
            dirty.Clear();
        }

        private void LateUpdate()
        {
            if (material != null) RefreshCorners();
        }

        private void OnTilesChanged(Tilemap changedMap, Tilemap.SyncTile[] changes)
        {
            if (changedMap != map || rebuild) return;
            if (changes.Length > 64) { rebuild = true; return; }
            foreach (var change in changes)
            {
                dirty.Add(change.position);
                dirty.Add(change.position + Vector3Int.left);
                dirty.Add(change.position + Vector3Int.right);
                dirty.Add(change.position + Vector3Int.up);
                dirty.Add(change.position + Vector3Int.down);
            }
        }

        private void RefreshCorners()
        {
            Vector3 origin = map.CellToLocal(Vector3Int.zero);
            Vector3 pitch = map.CellToLocal(Vector3Int.one) - origin;
            material.SetMatrix(WorldToCell, Matrix4x4.Scale(new Vector3(1f / pitch.x, 1f / pitch.y, 1f))
                * Matrix4x4.Translate(-origin) * map.transform.worldToLocalMatrix);
            BoundsInt currentBounds = map.cellBounds;
            if (currentBounds.size.x <= 0 || currentBounds.size.y <= 0) return;
            if (rebuild || currentBounds != bounds || cornerTexture == null)
            {
                bounds = currentBounds;
                if (cornerTexture != null) Destroy(cornerTexture);
                cornerTexture = new Texture2D(bounds.size.x, bounds.size.y, TextureFormat.RGBA32, false, true)
                { name = "Mine exposed corners", filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
                var pixels = new Color32[bounds.size.x * bounds.size.y];
                for (int y = bounds.yMin; y < bounds.yMax; y++)
                    for (int x = bounds.xMin; x < bounds.xMax; x++)
                        pixels[(y - bounds.yMin) * bounds.size.x + x - bounds.xMin] = GetExposedCorners(map, new Vector3Int(x, y, 0));
                cornerTexture.SetPixels32(pixels);
                material.SetTexture(CornerMap, cornerTexture);
                material.SetVector(CornerBounds, new Vector4(bounds.xMin, bounds.yMin, bounds.size.x, bounds.size.y));
                rebuild = false;
            }
            else if (dirty.Count == 0) return;
            else
            {
                foreach (Vector3Int cell in dirty)
                    if (bounds.Contains(cell))
                        cornerTexture.SetPixel(cell.x - bounds.xMin, cell.y - bounds.yMin, GetExposedCorners(map, cell));
            }
            cornerTexture.Apply(false, false);
            dirty.Clear();
        }

        public static Color32 GetExposedCorners(Tilemap tilemap, Vector3Int cell)
        {
            if (!tilemap.HasTile(cell)) return new Color32(0, 0, 0, 0);
            bool left = !tilemap.HasTile(cell + Vector3Int.left);
            bool right = !tilemap.HasTile(cell + Vector3Int.right);
            bool up = !tilemap.HasTile(cell + Vector3Int.up);
            bool down = !tilemap.HasTile(cell + Vector3Int.down);
            return new Color32((byte)(left && down ? 255 : 0), (byte)(right && down ? 255 : 0),
                (byte)(left && up ? 255 : 0), (byte)(right && up ? 255 : 0));
        }
    }
}

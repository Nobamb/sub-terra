using System.Collections.Generic;
using UnityEngine;

namespace SubTerra.Gameplay.Building
{
    /// <summary>One geometry rule for authored facilities, restored instances and placement previews.</summary>
    public static class FacilityGroundedVisual
    {
        private static readonly Dictionary<Sprite, Bounds> meshBounds = new();
        private static Sprite chargerArtwork;
        private static bool chargerArtworkLoaded;
        private static Sprite storageArtwork;
        private static bool storageArtworkLoaded;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCache()
        {
            meshBounds.Clear();
            chargerArtwork = storageArtwork = null;
            chargerArtworkLoaded = storageArtworkLoaded = false;
        }

        public static Sprite ResolveArtwork(Sprite original, string buildingId)
        {
            if (buildingId == "building.charger.basic")
            {
                if (!chargerArtworkLoaded)
                {
                    chargerArtwork = Resources.Load<Sprite>("Facilities/ChargerGrounded");
                    chargerArtworkLoaded = true;
                }
                return chargerArtwork != null ? chargerArtwork : original;
            }
            if (buildingId == "building.storage.basic")
            {
                if (!storageArtworkLoaded)
                {
                    storageArtwork = Resources.Load<Sprite>("Facilities/StorageGrounded");
                    storageArtworkLoaded = true;
                }
                return storageArtwork != null ? storageArtwork : original;
            }
            return original;
        }

        public static bool TryGetGeometry(Sprite sprite, string buildingId, Vector2Int footprint,
            out Vector3 position, out Vector3 scale)
        {
            position = Vector3.zero;
            scale = Vector3.one;
            if (sprite == null) return false;
            bool charger = buildingId == "building.charger.basic";
            bool storage = buildingId == "building.storage.basic";
            bool outpost = buildingId == "building.outpost_core.basic";
            if (!charger && !storage && !outpost) return false;
            bool largeCharger = charger && footprint.x != 1;
            // Explicit legacy 1x2 saves keep their narrow occupied area; new outposts use 2x2.
            float width = outpost ? footprint.x == 1 ? 0.92f : 1.8f : largeCharger ? 1.8f : 0.96f;
            float height = outpost ? 1.78f : largeCharger ? 1.82f : 0.96f;
            int rows = outpost || largeCharger ? 2 : 1;
            bool hasFoundation = FacilityFoundationVisual.Supports(buildingId);
            float contactHeight = hasFoundation ? FacilityFoundationVisual.ArtworkContactHeight : -0.08f;
            height = Mathf.Min(height, rows - Mathf.Max(0f, contactHeight) - 0.02f);
            Bounds bounds = GetVisibleBounds(sprite);
            float factor = Mathf.Min(width / Mathf.Max(bounds.size.x, 0.0001f),
                height / Mathf.Max(bounds.size.y, 0.0001f));
            scale = new Vector3(factor, factor, 1f);
            // Visible feet sit on the shared plate; facilities outside the pilot keep their original shallow contact.
            position = new Vector3(-bounds.center.x * factor, -rows * 0.5f + contactHeight - bounds.min.y * factor, 0f);
            return true;
        }

        public static Bounds GetVisibleBounds(Sprite sprite)
        {
            if (meshBounds.TryGetValue(sprite, out Bounds bounds)) return bounds;
            // Measured opaque bounds (alpha >= 64) of the unchanged source PNGs.
            // Sprite vertices can include transparent margins. Never align those margins to the floor.
            Rect pixels = default;
            Texture2D texture = sprite.texture;
            if (texture.name == "charger_basic_cartoon_v2" && texture.width == 1240 && texture.height == 1269)
                pixels = Rect.MinMaxRect(184f, 79f, 1073f, 1227f);
            else if (texture.name == "storage_basic_cartoon_v2" && texture.width == 1312 && texture.height == 1199)
                pixels = Rect.MinMaxRect(74f, 96f, 1257f, 1098f);
            else if (texture.name == "ChargerGrounded" && texture.width == 1240 && texture.height == 1269)
                pixels = Rect.MinMaxRect(173f, 84f, 1067f, 1220f);
            else if (texture.name == "StorageGrounded" && texture.width == 1315 && texture.height == 1196)
                pixels = Rect.MinMaxRect(57f, 89f, 1258f, 1101f);
            else if (texture.name == "OutpostCoreTall" && texture.width == 948 && texture.height == 1659)
                pixels = Rect.MinMaxRect(58f, 55f, 891f, 1596f);
            else if (texture.name == "outpost_core_cartoon_v3" && texture.width == 1292 && texture.height == 1218)
                // PNG top-left bounds (74,0)-(1216,1132), converted to Unity bottom-left pixels.
                pixels = Rect.MinMaxRect(74f, 86f, 1216f, 1218f);
            if (pixels.width > 0f)
            {
                Vector2 min = (Vector2.Max(pixels.min, sprite.rect.min) - sprite.rect.min - sprite.pivot) / sprite.pixelsPerUnit;
                Vector2 max = (Vector2.Min(pixels.max, sprite.rect.max) - sprite.rect.min - sprite.pivot) / sprite.pixelsPerUnit;
                bounds = new Bounds((min + max) * 0.5f, max - min);
            }
            else
            {
                Vector2[] vertices = sprite.vertices;
                bounds = vertices.Length > 0 ? new Bounds(vertices[0], Vector3.zero) : sprite.bounds;
                foreach (var vertex in vertices) bounds.Encapsulate(vertex);
            }
            meshBounds[sprite] = bounds;
            return bounds;
        }

        public static FacilityFoundationVisual ApplyFoundation(Transform root, SpriteRenderer source,
            string buildingId, Vector2Int footprint)
        {
            if (!FacilityFoundationVisual.Supports(buildingId)) return null;
            if (!TryGetGeometry(source.sprite, buildingId, footprint, out _, out var scale)) return null;
            bool large = buildingId == "building.charger.basic" && footprint.x != 1;
            Transform baseRoot = root.Find("FacilityFoundation");
            if (baseRoot == null)
            {
                baseRoot = new GameObject("FacilityFoundation").transform;
                baseRoot.SetParent(root, false);
            }
            var foundation = baseRoot.GetComponent<FacilityFoundationVisual>() ?? baseRoot.gameObject.AddComponent<FacilityFoundationVisual>();
            foundation.Configure(source, large ? 2 : 1, large ? 2 : 1, meshBounds[source.sprite].size.x * scale.x);
            return foundation;
        }

        public static void Apply(Transform root, string buildingId, Vector2Int footprint)
        {
            Transform artwork = root.Find("VisualRoot/Artwork") ?? root.Find("VisualRoot/Diamond");
            if (artwork == null || !artwork.TryGetComponent<SpriteRenderer>(out var renderer)) return;
            renderer.sprite = ResolveArtwork(renderer.sprite, buildingId);
            if (!TryGetGeometry(renderer.sprite, buildingId, footprint, out var position, out var scale)) return;
            artwork.localPosition = position;
            artwork.localScale = scale;
            ApplyFoundation(root, renderer, buildingId, footprint);
            // Replace only the old painted base, keeping the power-port hierarchy and colliders active.
            if (FacilityFoundationVisual.Supports(buildingId))
            {
                Transform oldBase = root.Find("MVP_Grounding/FoundationTile");
                if (oldBase != null)
                    foreach (var oldRenderer in oldBase.GetComponentsInChildren<Renderer>(true))
                        oldRenderer.enabled = false;
            }
            Transform powered = root.Find("PoweredVisualRoot");
            if (powered != null) powered.localPosition = position + Vector3.Scale(renderer.sprite.bounds.center, scale);
            if (buildingId == "building.charger.basic")
            {
                Transform grounding = root.Find("MVP_Grounding");
                if (grounding != null) grounding.localPosition = footprint.x != 1 ? new Vector3(0f, -0.5f, 0f) : Vector3.zero;
            }
        }
    }
}

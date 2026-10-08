using UnityEngine;

namespace SubTerra.Gameplay.Building
{
    /// <summary>Shared visual-only ground contact. Does not own occupancy, physics or power ports.</summary>
    public sealed class FacilityFoundationVisual : MonoBehaviour
    {
        public const float ArtworkContactHeight = -0.04f;
        [SerializeField] private int rows = 1;
        [SerializeField] private float plateWidth;
        [SerializeField] private Material sourceMaterial;
        [SerializeField] private int sortingLayer;
        [SerializeField] private int sortingOrder;
        [SerializeField] private bool integratedBase;
        private SpriteRenderer[] parts;
        private Color tint = Color.white;
        private static Sprite solidSprite;
        private static Sprite bevelSprite;
        private static Sprite shadowSprite;
        private static Texture2D solidTexture;
        private static Texture2D shadowTexture;
        private static readonly Color[] palette =
        {
            new(0f, 0f, 0f, 0.28f), new(0.055f, 0.07f, 0.08f),
            new(0.25f, 0.29f, 0.33f), new(0.60f, 0.65f, 0.68f),
            new(0.95f, 0.56f, 0.09f), new(0.95f, 0.56f, 0.09f)
        };

        public static bool Supports(string id) => id == "building.storage.basic" || id == "building.charger.basic"
            || id == "building.light.basic" || id == "building.settlement.basic";

        public void Configure(SpriteRenderer source, int footprintRows, int footprintWidth, float visibleWidth)
        {
            float nextWidth = Mathf.Min(footprintWidth - 0.04f, visibleWidth + 0.06f);
            bool nextIntegratedBase = source.sprite != null && (source.sprite.texture.name == "ChargerGrounded"
                || source.sprite.texture.name == "StorageGrounded" || source.sprite.texture.name == "light_basic_cartoon_v3"
                || source.sprite.texture.name == "settlement_console_cartoon_v3");
            bool changed = rows != footprintRows || !Mathf.Approximately(plateWidth, nextWidth)
                || sourceMaterial != source.sharedMaterial || sortingLayer != source.sortingLayerID
                || sortingOrder != source.sortingOrder || integratedBase != nextIntegratedBase;
            rows = footprintRows;
            plateWidth = nextWidth;
            sourceMaterial = source.sharedMaterial;
            sortingLayer = source.sortingLayerID;
            sortingOrder = source.sortingOrder;
            integratedBase = nextIntegratedBase;
            // Editor builders save only this configuration, never transient procedural sprites.
            if (Application.isPlaying && (changed || parts == null)) BuildVisuals();
        }

        private void OnEnable()
        {
            if (Application.isPlaying && plateWidth > 0f) BuildVisuals();
        }

        public void BuildVisuals()
        {
            EnsureSprites();
            if (parts == null)
            {
                string[] names = { "ContactShadow", "PlateOutline", "PlateBody", "TopEdge", "LeftClamp", "RightClamp" };
                parts = new SpriteRenderer[names.Length];
                for (int i = 0; i < parts.Length; i++)
                {
                    var part = new GameObject(names[i]) { hideFlags = HideFlags.DontSave };
                    part.transform.SetParent(transform, false);
                    parts[i] = part.AddComponent<SpriteRenderer>();
                }
            }
            float groundY = -rows * 0.5f;
            SetPart(0, shadowSprite, new Vector2(0f, groundY - 0.005f), new Vector2(plateWidth * 0.96f, 0.05f), -4);
            SetPart(1, bevelSprite, new Vector2(0f, groundY + 0.005f), new Vector2(plateWidth, 0.07f), -3);
            SetPart(2, bevelSprite, new Vector2(0f, groundY + 0.008f), new Vector2(plateWidth - 0.026f, 0.05f), -2);
            SetPart(3, solidSprite, new Vector2(0f, groundY + 0.031f), new Vector2(plateWidth * 0.82f, 0.01f), -1);
            float clampWidth = Mathf.Min(0.09f, plateWidth * 0.085f);
            SetPart(4, bevelSprite, new Vector2(-plateWidth * 0.40f, groundY + 0.012f), new Vector2(clampWidth, 0.035f), -1);
            SetPart(5, bevelSprite, new Vector2(plateWidth * 0.40f, groundY + 0.012f), new Vector2(clampWidth, 0.035f), -1);
            SetTint(tint);
            // The replacement artwork already contains a solid level chassis. Keep only its contact shadow.
            for (int i = 0; i < parts.Length; i++) parts[i].enabled = !integratedBase || i == 0;
        }

        private void SetPart(int index, Sprite sprite, Vector2 position, Vector2 size, int orderOffset)
        {
            var renderer = parts[index];
            renderer.sprite = sprite;
            renderer.sharedMaterial = sourceMaterial;
            renderer.sortingLayerID = sortingLayer;
            renderer.sortingOrder = sortingOrder + orderOffset;
            renderer.transform.localPosition = position;
            renderer.transform.localScale = new Vector3(size.x / sprite.bounds.size.x, size.y / sprite.bounds.size.y, 1f);
        }

        public void SetTint(Color color)
        {
            tint = color;
            if (parts == null) return;
            for (int i = 0; i < parts.Length; i++) parts[i].color = palette[i] * tint;
        }

        private static void EnsureSprites()
        {
            if (solidSprite != null) return;
            // Unity 6000.5 rejects OverrideGeometry on these generated sprites.
            // Encode the small chamfer in alpha and share the white center texel for straight edges.
            solidTexture = new Texture2D(32, 16, TextureFormat.RGBA32, false)
            { name = "FacilityFoundationSolid", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.HideAndDontSave };
            var platePixels = new Color[32 * 16];
            for (int y = 0; y < 16; y++)
                for (int x = 0; x < 32; x++)
                {
                    float edgeX = Mathf.Min(x + 0.5f, 31.5f - x);
                    float edgeY = Mathf.Min(y + 0.5f, 15.5f - y);
                    platePixels[y * 32 + x] = new Color(1f, 1f, 1f, edgeX / 3f + edgeY / 3f >= 1f ? 1f : 0f);
                }
            solidTexture.SetPixels(platePixels);
            solidTexture.Apply(false, true);
            solidSprite = Sprite.Create(solidTexture, new Rect(16, 8, 1, 1), new Vector2(0.5f, 0.5f), 1f, 0, SpriteMeshType.FullRect);
            bevelSprite = Sprite.Create(solidTexture, new Rect(0, 0, 32, 16), new Vector2(0.5f, 0.5f), 32f, 0, SpriteMeshType.FullRect);
            shadowTexture = new Texture2D(64, 16, TextureFormat.RGBA32, false)
            { name = "FacilityContactShadow", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.HideAndDontSave };
            var pixels = new Color[64 * 16];
            for (int y = 0; y < 16; y++)
                for (int x = 0; x < 64; x++)
                {
                    float dx = x / 63f * 2f - 1f;
                    float dy = y / 15f * 2f - 1f;
                    pixels[y * 64 + x] = new Color(1f, 1f, 1f, Mathf.Pow(Mathf.Max(0f, 1f - dx * dx - dy * dy), 2f));
                }
            shadowTexture.SetPixels(pixels);
            shadowTexture.Apply(false, true);
            shadowSprite = Sprite.Create(shadowTexture, new Rect(0, 0, 64, 16), new Vector2(0.5f, 0.5f), 64f);
            solidSprite.hideFlags = bevelSprite.hideFlags = shadowSprite.hideFlags = HideFlags.HideAndDontSave;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSprites()
        {
            Release(solidSprite); Release(bevelSprite); Release(shadowSprite);
            Release(solidTexture); Release(shadowTexture);
            solidSprite = bevelSprite = shadowSprite = null;
            solidTexture = shadowTexture = null;
        }

        private static void Release(Object value)
        {
            if (value == null) return;
            if (Application.isPlaying) Destroy(value); else DestroyImmediate(value);
        }
    }
}

using UnityEngine;
using UnityEngine.Tilemaps;

namespace SubTerra.Gameplay.Building
{
    /// <summary>Visual-only grid preview. It never creates a building or spends resources.</summary>
    public sealed class BuildingPlacementPreview : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Color validColor = new(0.2f, 0.9f, 0.35f, 0.55f);
        [SerializeField] private Color invalidColor = new(0.95f, 0.2f, 0.2f, 0.55f);

        private void Awake()
        {
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        }

        public void Configure(Sprite sprite)
        {
            if (spriteRenderer != null) spriteRenderer.sprite = sprite;
        }

        public void SetCell(Tilemap tilemap, Vector3Int cell, bool isValid)
        {
            transform.position = tilemap != null ? tilemap.GetCellCenterWorld(cell) : cell;
            if (spriteRenderer != null) spriteRenderer.color = isValid ? validColor : invalidColor;
            gameObject.SetActive(true);
        }

        public void Hide() => gameObject.SetActive(false);
    }
}

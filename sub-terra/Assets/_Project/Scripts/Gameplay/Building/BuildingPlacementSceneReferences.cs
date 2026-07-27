using UnityEngine;
using UnityEngine.Tilemaps;

namespace SubTerra.Gameplay.Building
{
    /// <summary>Small test-scene reference holder used by the optional mouse input adapter.</summary>
    public sealed class BuildingPlacementSceneReferences : MonoBehaviour
    {
        [SerializeField] private Tilemap terrainTilemap;
        public Tilemap TerrainTilemap => terrainTilemap;
    }
}

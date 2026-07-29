using SubTerra.Gameplay.Mining;
using SubTerra.Shared;
using UnityEngine;

namespace SubTerra.Gameplay.Hazards
{
    /// <summary>Links A-2 mining completion to A-4 gas zone activation.</summary>
    [RequireComponent(typeof(GasHazardSystem))]
    public sealed class MiningGasBridge : MonoBehaviour
    {
        [SerializeField] private MiningSystem miningSystem;
        [SerializeField] private GasHazardSystem gasHazardSystem;

        private void Awake()
        {
            if (gasHazardSystem == null) gasHazardSystem = GetComponent<GasHazardSystem>();
        }

        private void OnEnable()
        {
            if (miningSystem != null) miningSystem.TileMined += OnTileMined;
        }

        private void OnDisable()
        {
            if (miningSystem != null) miningSystem.TileMined -= OnTileMined;
        }

        private void OnTileMined(Vector3Int cell, MiningTileDto tile)
        {
            if (tile.containsGas) gasHazardSystem?.ActivateAt(cell, tile);
        }
    }
}

using SubTerra.Gameplay.Mining;
using SubTerra.Shared;
using UnityEngine;

namespace SubTerra.Gameplay.Structural
{
    /// <summary>Connects the A-2 mining event to the A-3 structural simulation without App dependencies.</summary>
    [RequireComponent(typeof(StructuralIntegritySystem))]
    public sealed class MiningStructuralBridge : MonoBehaviour
    {
        [SerializeField] private MiningSystem miningSystem;
        [SerializeField] private StructuralIntegritySystem structuralIntegritySystem;

        private void Awake()
        {
            if (structuralIntegritySystem == null)
                structuralIntegritySystem = GetComponent<StructuralIntegritySystem>();
        }

        private void OnEnable()
        {
            if (miningSystem != null)
                miningSystem.TileMined += OnTileMined;
        }

        private void OnDisable()
        {
            if (miningSystem != null)
                miningSystem.TileMined -= OnTileMined;
        }

        private void OnTileMined(Vector3Int cell, MiningTileDto tile)
        {
            structuralIntegritySystem?.NotifyTileMined(cell, tile);
        }
    }
}

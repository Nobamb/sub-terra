using UnityEngine;

namespace SubTerra.Gameplay.Building
{
    /// <summary>Editor test-scene wallet. Production scenes replace this with an App-owned adapter.</summary>
    public sealed class BuildingTestResourceWallet : MonoBehaviour, IBuildingResourceWallet
    {
        [SerializeField] private int remainingPlacements = 3;
        public bool CanAfford(string buildingId) => remainingPlacements > 0 && !string.IsNullOrWhiteSpace(buildingId);

        public bool TrySpend(string buildingId)
        {
            if (!CanAfford(buildingId)) return false;
            remainingPlacements--;
            return true;
        }
    }
}

using SubTerra.Shared;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace SubTerra.App.Core.Data
{
    /// <summary>
    /// B가 소유하는 채굴 타일 정적 정의.
    /// 타일 에셋 연결은 여기서 관리하고 A에는 Unity 비의존 DTO를 제공한다.
    /// </summary>
    [CreateAssetMenu(fileName = "MiningTileData", menuName = "SubTerra/Data/Mining Tile", order = 20)]
    public sealed class MiningTileData : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private TileBase tileAsset;
        [SerializeField] private bool isMineable = true;
        [SerializeField] private float durability = 1f;
        [SerializeField] private float miningTime = 1f;
        [SerializeField, Min(0)] private int requiredDrillLevel;
        [SerializeField, Min(0)] private int energyCost = 1;
        [SerializeField] private MineralData rewardMineral;
        [SerializeField] private int quantity;
        [SerializeField] private float structuralImpact;
        [SerializeField] private bool containsGas;

        public string Id => id;
        public TileBase TileAsset => tileAsset;
        public bool IsMineable => isMineable;
        public float Durability => durability;
        public float MiningTime => miningTime;
        public int RequiredDrillLevel => requiredDrillLevel;
        public int EnergyCost => energyCost;
        public MineralData RewardMineral => rewardMineral;
        public string MineralId => rewardMineral != null ? rewardMineral.Id : string.Empty;
        public int Quantity => quantity;
        public float StructuralImpact => structuralImpact;
        public bool ContainsGas => containsGas;

        /// <summary>Shared 경계에는 ID와 수치만 전달하고 Unity Object 참조는 노출하지 않는다.</summary>
        public MiningTileDto ToDto()
        {
            return new MiningTileDto(
                id,
                MineralId,
                quantity,
                isMineable,
                durability,
                miningTime,
                structuralImpact,
                containsGas,
                requiredDrillLevel,
                energyCost);
        }

#if UNITY_EDITOR
        public void EditorSet(
            string permanentId,
            TileBase asset,
            bool mineable,
            float tileDurability,
            float timeToMine,
            MineralData mineral,
            int rewardQuantity,
            float impact,
            bool gas,
            int minimumDrillLevel = 0,
            int miningEnergyCost = 1)
        {
            id = permanentId;
            tileAsset = asset;
            isMineable = mineable;
            durability = tileDurability;
            miningTime = timeToMine;
            requiredDrillLevel = minimumDrillLevel;
            energyCost = miningEnergyCost;
            rewardMineral = mineral;
            quantity = rewardQuantity;
            structuralImpact = impact;
            containsGas = gas;
        }
#endif
    }
}

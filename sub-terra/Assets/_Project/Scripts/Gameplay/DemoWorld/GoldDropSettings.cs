using SubTerra.Shared;
using UnityEngine;

namespace SubTerra.Gameplay.DemoWorld
{
    [CreateAssetMenu(menuName = "SubTerra/World/Gold Drop Settings")]
    public sealed class GoldDropSettings : ScriptableObject
    {
        [SerializeField, Range(0, 100)] private int rockChancePercent = 1;
        [SerializeField, Range(0, 100)] private int gasChancePercent = 10;
        [SerializeField, Range(0, 100)] private int oreChancePercent = 5;
        [SerializeField, Min(1)] private int rockGold = 20;
        [SerializeField, Min(1)] private int gasGold = 50;
        [SerializeField, Min(1)] private int orePriceMultiplier = 5;
        [SerializeField] private ScriptableObject mineralCatalog;

        public bool IsGold(long seed, int x, int y, MineLayerCellKind kind)
        {
            int chance = kind switch
            {
                MineLayerCellKind.Rock => rockChancePercent,
                MineLayerCellKind.GasPocket => gasChancePercent,
                MineLayerCellKind.Copper or MineLayerCellKind.Iron or MineLayerCellKind.Lithium => oreChancePercent,
                _ => 0
            };
            // 광맥 RNG와 독립된 좌표 해시이므로 기존 광맥과 세이브 버전은 바뀌지 않는다.
            unchecked
            {
                ulong h = (ulong)seed ^ ((ulong)(uint)x << 32) ^ (uint)y ^ 0x474F4C4454494C45UL;
                h = (h ^ (h >> 30)) * 0xBF58476D1CE4E5B9UL;
                h = (h ^ (h >> 27)) * 0x94D049BB133111EBUL;
                h ^= h >> 31;
                return h % 100 < (ulong)Mathf.Clamp(chance, 0, 100);
            }
        }

        public MiningTileDto CreateVariant(MiningTileDto source)
        {
            if (!source.isMineable) return source;
            int grant;
            if (!string.IsNullOrEmpty(source.mineralId))
            {
                if (mineralCatalog == null || !(mineralCatalog is IMineralPriceProvider prices)
                    || !prices.TryGetMineralUnitPrice(source.mineralId, out int price) || price <= 0)
                    throw new System.InvalidOperationException("골드 드롭 광물 단가 참조가 없습니다: " + source.mineralId);
                grant = (int)System.Math.Min(int.MaxValue, (long)price * Mathf.Max(1, orePriceMultiplier));
            }
            else grant = source.containsGas ? Mathf.Max(1, gasGold) : Mathf.Max(1, rockGold);
            source.tileId = MineLayerTileIds.BaseTileId(source.tileId) + ".gold";
            source.goldDrop = grant;
            return source;
        }
    }
}

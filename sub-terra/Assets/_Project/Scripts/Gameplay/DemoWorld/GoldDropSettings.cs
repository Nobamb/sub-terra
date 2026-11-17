using SubTerra.Shared;
using UnityEngine;

namespace SubTerra.Gameplay.DemoWorld
{
    [CreateAssetMenu(menuName = "SubTerra/World/Gold Drop Settings")]
    public sealed class GoldDropSettings : ScriptableObject
    {
        [SerializeField, Range(0, 100)] private int chancePercent = 1;
        private const int GoldPerMeter = 100;

        public bool IsGold(long seed, int x, int y, MineLayerCellKind kind)
        {
            bool eligible = kind switch
            {
                MineLayerCellKind.Rock or MineLayerCellKind.GasPocket or MineLayerCellKind.Copper
                    or MineLayerCellKind.Iron or MineLayerCellKind.Lithium => true,
                _ => false
            };
            if (!eligible) return false;
            // 광맥 RNG와 독립된 좌표 해시이므로 기존 광맥과 세이브 버전은 바뀌지 않는다.
            unchecked
            {
                ulong h = (ulong)seed ^ ((ulong)(uint)x << 32) ^ (uint)y ^ 0x474F4C4454494C45UL;
                h = (h ^ (h >> 30)) * 0xBF58476D1CE4E5B9UL;
                h = (h ^ (h >> 27)) * 0x94D049BB133111EBUL;
                h ^= h >> 31;
                return h % 100 < (ulong)Mathf.Clamp(chancePercent, 0, 100);
            }
        }

        public MiningTileDto CreateVariant(MiningTileDto source)
        {
            if (!source.isMineable) return source;
            source.tileId = MineLayerTileIds.BaseTileId(source.tileId) + ".gold";
            source.goldDrop = GoldPerMeter;
            return source;
        }
    }
}

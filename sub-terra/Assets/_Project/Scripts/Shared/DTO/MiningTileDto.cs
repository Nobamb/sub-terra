using System;

namespace SubTerra.Shared
{
    /// <summary>
    /// 채굴 타일의 Unity 비의존 계약.
    /// TileBase나 ScriptableObject 참조를 넣지 않아 Gameplay와 App이 안전하게 공유한다.
    /// </summary>
    [Serializable]
    public struct MiningTileDto
    {
        public string tileId;
        public string mineralId;
        public int quantity;
        public bool isMineable;
        public float durability;
        public float miningTime;
        public float structuralImpact;
        public bool containsGas;

        public MiningTileDto(
            string tileId,
            string mineralId,
            int quantity,
            bool isMineable,
            float durability,
            float miningTime,
            float structuralImpact,
            bool containsGas)
        {
            this.tileId = tileId;
            this.mineralId = mineralId;
            this.quantity = quantity;
            this.isMineable = isMineable;
            this.durability = durability;
            this.miningTime = miningTime;
            this.structuralImpact = structuralImpact;
            this.containsGas = containsGas;
        }
    }
}

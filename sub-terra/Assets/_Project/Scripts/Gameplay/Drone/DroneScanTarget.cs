using UnityEngine;

namespace SubTerra.Gameplay.Drone
{
    public enum DroneScanTargetKind
    {
        Mineral = 0,
        GasHazard = 1,
        GoldDrop = 2,
        SealedGlyph = 3
    }

    /// <summary>한 번의 스캔 펄스가 월드에 강조할 타일 좌표와 종류.</summary>
    public readonly struct DroneScanTarget
    {
        public DroneScanTarget(Vector3Int cell, Vector3 worldPosition, DroneScanTargetKind kind,
            string mineralId = "", bool hasGold = false)
        {
            Cell = cell;
            WorldPosition = worldPosition;
            Kind = kind;
            MineralId = mineralId ?? string.Empty;
            HasGold = hasGold || kind == DroneScanTargetKind.GoldDrop;
        }

        public Vector3Int Cell { get; }
        public Vector3 WorldPosition { get; }
        public DroneScanTargetKind Kind { get; }
        public string MineralId { get; }
        public bool HasGold { get; }
    }
}

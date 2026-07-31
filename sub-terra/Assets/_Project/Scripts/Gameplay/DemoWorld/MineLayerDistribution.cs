using System;
using System.Collections.Generic;
using UnityEngine;

namespace SubTerra.Gameplay.DemoWorld
{
    public enum MineLayerCellKind
    {
        Rock,
        BoundaryRock,
        Copper,
        Iron,
        Lithium,
        GasPocket,
        LockedSignal
    }

    [Serializable]
    public sealed class MineLayerContentWeight
    {
        [SerializeField] private MineLayerCellKind kind;
        [SerializeField, Min(0f)] private float weight;

        public MineLayerContentWeight(MineLayerCellKind kind, float weight)
        {
            this.kind = kind;
            this.weight = weight;
        }

        public MineLayerCellKind Kind => kind;
        public float Weight => weight;
    }

    [Serializable]
    public sealed class MineLayerBandDefinition
    {
        [SerializeField, Min(1)] private int minDepth;
        [SerializeField, Min(1)] private int maxDepth;
        [SerializeField] private List<MineLayerContentWeight> contents = new();

        public MineLayerBandDefinition(
            int minDepth,
            int maxDepth,
            params MineLayerContentWeight[] contents)
        {
            this.minDepth = minDepth;
            this.maxDepth = maxDepth;
            this.contents.AddRange(contents);
        }

        public int MinDepth => minDepth;
        public int MaxDepth => maxDepth;
        public IReadOnlyList<MineLayerContentWeight> Contents => contents;
        public bool Contains(int depth) => depth >= minDepth && depth <= maxDepth;
    }

    [CreateAssetMenu(
        fileName = "MineLayerDistribution",
        menuName = "SubTerra/World/Mine Layer Distribution")]
    public sealed class MineLayerDistribution : ScriptableObject
    {
        [SerializeField, Min(5)] private int width = 81;
        [SerializeField, Min(1)] private int depth = 40;
        [SerializeField] private int minX = -40;
        [SerializeField] private int topY = -2;
        [SerializeField, Range(0.1f, 0.15f)] private float contentRatio = 0.1f;
        [SerializeField, Range(2, 5)] private int minVeinSize = 2;
        [SerializeField, Range(2, 5)] private int maxVeinSize = 5;
        [SerializeField, Min(1)] private int generatorVersion = 1;
        [SerializeField] private int protectedRouteX = -9;
        [SerializeField] private int signalX = 14;
        [SerializeField, Range(36, 40)] private int signalDepth = 38;
        [SerializeField, Min(0)] private int surfaceBoundaryHeight = 7;
        [SerializeField] private List<MineLayerBandDefinition> bands = new()
        {
            new MineLayerBandDefinition(
                1,
                15,
                new MineLayerContentWeight(MineLayerCellKind.Copper, 1f)),
            new MineLayerBandDefinition(
                16,
                35,
                new MineLayerContentWeight(MineLayerCellKind.Iron, 0.7f),
                new MineLayerContentWeight(MineLayerCellKind.GasPocket, 0.3f)),
            new MineLayerBandDefinition(
                36,
                40,
                new MineLayerContentWeight(MineLayerCellKind.Lithium, 1f))
        };

        public int Width => width;
        public int Depth => depth;
        public int MinX => minX;
        public int MaxX => minX + width - 1;
        public int TopY => topY;
        public float ContentRatio => contentRatio;
        public int MinVeinSize => minVeinSize;
        public int MaxVeinSize => maxVeinSize;
        public int GeneratorVersion => generatorVersion;
        public int ProtectedRouteX => protectedRouteX;
        public int SignalX => signalX;
        public int SignalDepth => signalDepth;
        public int SurfaceBoundaryHeight => surfaceBoundaryHeight;
        public IReadOnlyList<MineLayerBandDefinition> Bands => bands;

        public MineLayerBandDefinition GetBand(int depthValue)
        {
            foreach (MineLayerBandDefinition band in bands)
            {
                if (band != null && band.Contains(depthValue))
                {
                    return band;
                }
            }

            return null;
        }
    }

    public static class MineLayerTileIds
    {
        public const string Rock = "tile.rock.normal";
        public const string BoundaryRock = "tile.rock.boundary";
        public const string Copper = "tile.copper";
        public const string Iron = "tile.iron";
        public const string Lithium = "tile.lithium";
        public const string GasPocket = "tile.gas-pocket";
        public const string LockedSignal = "tile.locked.signal";

        public static string For(MineLayerCellKind kind)
        {
            return kind switch
            {
                MineLayerCellKind.BoundaryRock => BoundaryRock,
                MineLayerCellKind.Copper => Copper,
                MineLayerCellKind.Iron => Iron,
                MineLayerCellKind.Lithium => Lithium,
                MineLayerCellKind.GasPocket => GasPocket,
                MineLayerCellKind.LockedSignal => LockedSignal,
                _ => Rock
            };
        }
    }
}

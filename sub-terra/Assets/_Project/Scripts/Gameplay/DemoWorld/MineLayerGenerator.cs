using System;
using System.Collections.Generic;
using UnityEngine;

namespace SubTerra.Gameplay.DemoWorld
{
    public readonly struct MineLayerCell
    {
        public MineLayerCell(int x, int y, int depth, MineLayerCellKind kind)
        {
            X = x;
            Y = y;
            Depth = depth;
            Kind = kind;
        }

        public int X { get; }
        public int Y { get; }
        public int Depth { get; }
        public MineLayerCellKind Kind { get; }
        public string TileId => MineLayerTileIds.For(Kind);
        public bool IsMineable => Kind != MineLayerCellKind.BoundaryRock
            && Kind != MineLayerCellKind.LockedSignal;
    }

    public sealed class MineLayerLayout
    {
        private static readonly Vector2Int[] Directions =
        {
            Vector2Int.left,
            Vector2Int.right,
            Vector2Int.up,
            Vector2Int.down
        };

        private readonly MineLayerCellKind[] cells;
        private readonly HashSet<int> protectedRoute;

        internal MineLayerLayout(
            long seed,
            int generatorVersion,
            MineLayerDistribution distribution,
            MineLayerCellKind[] cells,
            HashSet<int> protectedRoute)
        {
            Seed = seed;
            GeneratorVersion = generatorVersion;
            Width = distribution.Width;
            Depth = distribution.Depth;
            MinX = distribution.MinX;
            TopY = distribution.TopY;
            ProtectedRouteStartX = distribution.ProtectedRouteX;
            SignalCell = new Vector2Int(
                distribution.SignalX,
                distribution.TopY - distribution.SignalDepth + 1);
            this.cells = cells;
            this.protectedRoute = protectedRoute;
        }

        public long Seed { get; }
        public int GeneratorVersion { get; }
        public int Width { get; }
        public int Depth { get; }
        public int MinX { get; }
        public int MaxX => MinX + Width - 1;
        public int TopY { get; }
        public int ProtectedRouteStartX { get; }
        public Vector2Int SignalCell { get; }
        public int CellCount => cells.Length;

        public MineLayerCell GetCell(int x, int depth)
        {
            if (x < MinX || x > MaxX || depth < 1 || depth > Depth)
            {
                throw new ArgumentOutOfRangeException();
            }

            int index = ToIndex(x, depth);
            return new MineLayerCell(x, TopY - depth + 1, depth, cells[index]);
        }

        public bool IsProtectedRouteCell(int x, int depth)
        {
            return x >= MinX
                && x <= MaxX
                && depth >= 1
                && depth <= Depth
                && protectedRoute.Contains(ToIndex(x, depth));
        }

        public IEnumerable<MineLayerCell> EnumerateCells()
        {
            for (int depth = 1; depth <= Depth; depth++)
            {
                for (int x = MinX; x <= MaxX; x++)
                {
                    yield return GetCell(x, depth);
                }
            }
        }

        public ulong ComputeStableHash()
        {
            const ulong offset = 14695981039346656037UL;
            const ulong prime = 1099511628211UL;
            ulong hash = offset;
            for (int index = 0; index < cells.Length; index++)
            {
                hash ^= (byte)cells[index];
                hash *= prime;
            }

            return hash;
        }

        public bool HasSafeRouteToSignal()
        {
            int startIndex = ToIndex(ProtectedRouteStartX, 1);
            int signalDepth = TopY - SignalCell.y + 1;
            int targetIndex = ToIndex(SignalCell.x, signalDepth);
            var visited = new bool[cells.Length];
            var queue = new Queue<int>();
            visited[startIndex] = true;
            queue.Enqueue(startIndex);

            while (queue.Count > 0)
            {
                int current = queue.Dequeue();
                if (current == targetIndex)
                {
                    return true;
                }

                int currentDepth = current / Width + 1;
                int currentX = MinX + current % Width;
                foreach (Vector2Int direction in Directions)
                {
                    int nextX = currentX + direction.x;
                    int nextDepth = currentDepth - direction.y;
                    if (nextX < MinX || nextX > MaxX || nextDepth < 1 || nextDepth > Depth)
                    {
                        continue;
                    }

                    int next = ToIndex(nextX, nextDepth);
                    if (visited[next] || !IsSafePassage(cells[next]))
                    {
                        continue;
                    }

                    visited[next] = true;
                    queue.Enqueue(next);
                }
            }

            return false;
        }

        private int ToIndex(int x, int depth) => (depth - 1) * Width + x - MinX;

        private static bool IsSafePassage(MineLayerCellKind kind)
        {
            return kind != MineLayerCellKind.BoundaryRock
                && kind != MineLayerCellKind.GasPocket;
        }
    }

    public sealed class MineLayerGenerator
    {
        private static readonly Vector2Int[] Directions =
        {
            Vector2Int.left,
            Vector2Int.right,
            Vector2Int.up,
            Vector2Int.down
        };

        public MineLayerLayout Generate(long seed, MineLayerDistribution distribution)
        {
            Validate(distribution);
            var random = new LocalRandom(seed, distribution.GeneratorVersion);
            var cells = new MineLayerCellKind[distribution.Width * distribution.Depth];
            var protectedRoute = BuildProtectedRoute(distribution);
            FillBoundaries(cells, distribution);
            PlaceSignal(cells, distribution);

            foreach (MineLayerBandDefinition band in distribution.Bands)
            {
                int bandRows = band.MaxDepth - band.MinDepth + 1;
                int target = Mathf.RoundToInt(
                    distribution.Width * bandRows * distribution.ContentRatio);
                if (band.Contains(distribution.SignalDepth))
                {
                    target--;
                }

                PlaceBandContent(
                    cells,
                    protectedRoute,
                    distribution,
                    band,
                    target,
                    ref random);
            }

            return new MineLayerLayout(
                seed,
                distribution.GeneratorVersion,
                distribution,
                cells,
                protectedRoute);
        }

        private static void Validate(MineLayerDistribution distribution)
        {
            if (distribution == null)
            {
                throw new ArgumentNullException(nameof(distribution));
            }

            if (distribution.Width < 5
                || distribution.Depth != 40
                || distribution.MinVeinSize < 2
                || distribution.MaxVeinSize > 5
                || distribution.MinVeinSize > distribution.MaxVeinSize)
            {
                throw new InvalidOperationException("40m 지층 또는 광맥 크기 설정이 유효하지 않습니다.");
            }

            int expectedDepth = 1;
            foreach (MineLayerBandDefinition band in distribution.Bands)
            {
                if (band == null
                    || band.MinDepth != expectedDepth
                    || band.MaxDepth < band.MinDepth
                    || band.Contents.Count == 0)
                {
                    throw new InvalidOperationException("깊이 band는 1m부터 겹침 없이 이어져야 합니다.");
                }

                expectedDepth = band.MaxDepth + 1;
            }

            if (expectedDepth != distribution.Depth + 1)
            {
                throw new InvalidOperationException("깊이 band가 40m 전체를 덮지 않습니다.");
            }
        }

        private static HashSet<int> BuildProtectedRoute(MineLayerDistribution distribution)
        {
            var route = new HashSet<int>();
            for (int depth = 1; depth <= distribution.SignalDepth; depth++)
            {
                route.Add(ToIndex(distribution.ProtectedRouteX, depth, distribution));
            }

            int min = Math.Min(distribution.ProtectedRouteX, distribution.SignalX);
            int max = Math.Max(distribution.ProtectedRouteX, distribution.SignalX);
            for (int x = min; x <= max; x++)
            {
                route.Add(ToIndex(x, distribution.SignalDepth, distribution));
            }

            return route;
        }

        private static void FillBoundaries(
            MineLayerCellKind[] cells,
            MineLayerDistribution distribution)
        {
            for (int depth = 1; depth <= distribution.Depth; depth++)
            {
                cells[ToIndex(distribution.MinX, depth, distribution)] =
                    MineLayerCellKind.BoundaryRock;
                cells[ToIndex(distribution.MaxX, depth, distribution)] =
                    MineLayerCellKind.BoundaryRock;
            }

            for (int x = distribution.MinX; x <= distribution.MaxX; x++)
            {
                cells[ToIndex(x, distribution.Depth, distribution)] =
                    MineLayerCellKind.BoundaryRock;
            }
        }

        private static void PlaceSignal(
            MineLayerCellKind[] cells,
            MineLayerDistribution distribution)
        {
            cells[ToIndex(
                distribution.SignalX,
                distribution.SignalDepth,
                distribution)] = MineLayerCellKind.LockedSignal;
        }

        private static void PlaceBandContent(
            MineLayerCellKind[] cells,
            HashSet<int> protectedRoute,
            MineLayerDistribution distribution,
            MineLayerBandDefinition band,
            int target,
            ref LocalRandom random)
        {
            var anchors = new List<int>();
            for (int depth = band.MinDepth; depth <= band.MaxDepth; depth++)
            {
                for (int x = distribution.MinX + 1; x < distribution.MaxX; x++)
                {
                    int index = ToIndex(x, depth, distribution);
                    if (!protectedRoute.Contains(index) && cells[index] == MineLayerCellKind.Rock)
                    {
                        anchors.Add(index);
                    }
                }
            }

            Shuffle(anchors, ref random);
            int placed = 0;
            int clusterIndex = 0;
            int attempts = 0;
            int maxAttempts = anchors.Count * 16;
            while (placed < target && attempts++ < maxAttempts)
            {
                int remaining = target - placed;
                int size = SelectClusterSize(
                    remaining,
                    distribution.MinVeinSize,
                    distribution.MaxVeinSize,
                    ref random);
                MineLayerCellKind kind = SelectKind(band, clusterIndex, ref random);
                int anchor = anchors[random.Next(anchors.Count)];
                if (!TryBuildCluster(
                        anchor,
                        size,
                        kind,
                        cells,
                        protectedRoute,
                        distribution,
                        band,
                        ref random,
                        out List<int> cluster))
                {
                    continue;
                }

                foreach (int index in cluster)
                {
                    cells[index] = kind;
                }

                clusterIndex++;
                placed += cluster.Count;
            }

            if (placed != target)
            {
                throw new InvalidOperationException(
                    $"{band.MinDepth}~{band.MaxDepth}m 지층의 목표 분포를 유한 시도 안에 생성하지 못했습니다.");
            }
        }

        private static int SelectClusterSize(
            int remaining,
            int minimum,
            int maximum,
            ref LocalRandom random)
        {
            if (remaining <= maximum)
            {
                return remaining;
            }

            int size = random.Next(minimum, maximum + 1);
            if (remaining - size == 1)
            {
                size = size == maximum ? size - 1 : size + 1;
            }

            return size;
        }

        private static MineLayerCellKind SelectKind(
            MineLayerBandDefinition band,
            int clusterIndex,
            ref LocalRandom random)
        {
            if (clusterIndex < band.Contents.Count)
            {
                return band.Contents[clusterIndex].Kind;
            }

            float total = 0f;
            foreach (MineLayerContentWeight content in band.Contents)
            {
                total += Mathf.Max(0f, content.Weight);
            }

            float roll = random.NextFloat() * total;
            foreach (MineLayerContentWeight content in band.Contents)
            {
                roll -= Mathf.Max(0f, content.Weight);
                if (roll <= 0f)
                {
                    return content.Kind;
                }
            }

            return band.Contents[band.Contents.Count - 1].Kind;
        }

        private static bool TryBuildCluster(
            int anchor,
            int size,
            MineLayerCellKind kind,
            MineLayerCellKind[] cells,
            HashSet<int> protectedRoute,
            MineLayerDistribution distribution,
            MineLayerBandDefinition band,
            ref LocalRandom random,
            out List<int> cluster)
        {
            int anchorDepth = anchor / distribution.Width + 1;
            int anchorX = distribution.MinX + anchor % distribution.Width;
            if (protectedRoute.Contains(anchor)
                || cells[anchor] != MineLayerCellKind.Rock
                || !band.Contains(anchorDepth)
                || !HasNoAdjacentContent(
                    anchorX,
                    anchorDepth,
                    kind,
                    cells,
                    new HashSet<int>(),
                    distribution))
            {
                cluster = new List<int>();
                return false;
            }

            cluster = new List<int>(size) { anchor };
            var clusterSet = new HashSet<int> { anchor };

            while (cluster.Count < size)
            {
                var options = new List<int>();
                foreach (int current in cluster)
                {
                    int depth = current / distribution.Width + 1;
                    int x = distribution.MinX + current % distribution.Width;
                    int directionOffset = random.Next(Directions.Length);
                    for (int directionIndex = 0; directionIndex < Directions.Length; directionIndex++)
                    {
                        Vector2Int direction =
                            Directions[(directionIndex + directionOffset) % Directions.Length];
                        int nextX = x + direction.x;
                        int nextDepth = depth - direction.y;
                        if (nextX <= distribution.MinX
                            || nextX >= distribution.MaxX
                            || !band.Contains(nextDepth))
                        {
                            continue;
                        }

                        int next = ToIndex(nextX, nextDepth, distribution);
                        if (!clusterSet.Contains(next)
                            && !protectedRoute.Contains(next)
                            && cells[next] == MineLayerCellKind.Rock
                            && HasNoAdjacentContent(
                                nextX,
                                nextDepth,
                                kind,
                                cells,
                                clusterSet,
                                distribution))
                        {
                            options.Add(next);
                        }
                    }
                }

                if (options.Count == 0)
                {
                    cluster.Clear();
                    return false;
                }

                int selected = options[random.Next(options.Count)];
                cluster.Add(selected);
                clusterSet.Add(selected);
            }

            return true;
        }

        private static bool HasNoAdjacentContent(
            int x,
            int depth,
            MineLayerCellKind kind,
            MineLayerCellKind[] cells,
            HashSet<int> cluster,
            MineLayerDistribution distribution)
        {
            foreach (Vector2Int direction in Directions)
            {
                int nextX = x + direction.x;
                int nextDepth = depth - direction.y;
                if (nextX < distribution.MinX
                    || nextX > distribution.MaxX
                    || nextDepth < 1
                    || nextDepth > distribution.Depth)
                {
                    continue;
                }

                int next = ToIndex(nextX, nextDepth, distribution);
                if (cluster.Contains(next))
                {
                    continue;
                }

                MineLayerCellKind adjacent = cells[next];
                if (adjacent != MineLayerCellKind.Rock
                    && adjacent != MineLayerCellKind.BoundaryRock
                    && adjacent == kind)
                {
                    return false;
                }
            }

            return true;
        }

        private static int ToIndex(
            int x,
            int depth,
            MineLayerDistribution distribution)
        {
            return (depth - 1) * distribution.Width + x - distribution.MinX;
        }

        private static void Shuffle(List<int> values, ref LocalRandom random)
        {
            for (int index = values.Count - 1; index > 0; index--)
            {
                int other = random.Next(index + 1);
                (values[index], values[other]) = (values[other], values[index]);
            }
        }

        private struct LocalRandom
        {
            private ulong state;

            public LocalRandom(long seed, int version)
            {
                state = unchecked((ulong)seed)
                    ^ (unchecked((ulong)(uint)version) * 0x9E3779B97F4A7C15UL);
                if (state == 0)
                {
                    state = 0xD1B54A32D192ED03UL;
                }
            }

            public int Next(int exclusiveMax) => Next(0, exclusiveMax);

            public int Next(int inclusiveMin, int exclusiveMax)
            {
                if (exclusiveMax <= inclusiveMin)
                {
                    return inclusiveMin;
                }

                ulong value = NextUInt64();
                return inclusiveMin + (int)(value % (uint)(exclusiveMax - inclusiveMin));
            }

            public float NextFloat()
            {
                return (NextUInt64() >> 40) * (1f / (1 << 24));
            }

            private ulong NextUInt64()
            {
                ulong value = state;
                value ^= value >> 12;
                value ^= value << 25;
                value ^= value >> 27;
                state = value;
                return value * 2685821657736338717UL;
            }
        }
    }
}

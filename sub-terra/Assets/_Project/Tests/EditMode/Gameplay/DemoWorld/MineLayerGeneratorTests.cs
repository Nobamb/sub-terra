using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace SubTerra.Gameplay.DemoWorld.Tests
{
    public sealed class MineLayerGeneratorTests
    {
        private MineLayerDistribution distribution;
        private MineLayerGenerator generator;

        [SetUp]
        public void SetUp()
        {
            distribution = ScriptableObject.CreateInstance<MineLayerDistribution>();
            generator = new MineLayerGenerator();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(distribution);
        }

        [Test]
        public void SameSeedAndVersion_ProducesSameTileHash()
        {
            MineLayerLayout first = generator.Generate(41027, distribution);
            MineLayerLayout second = generator.Generate(41027, distribution);

            Assert.That(first.ComputeStableHash(), Is.EqualTo(second.ComputeStableHash()));
        }

        [Test]
        public void DifferentSeeds_ChangeGeneratedDistribution()
        {
            MineLayerLayout first = generator.Generate(41027, distribution);
            MineLayerLayout second = generator.Generate(41028, distribution);

            Assert.That(first.ComputeStableHash(), Is.Not.EqualTo(second.ComputeStableHash()));
        }

        [Test]
        public void FixedSeedSample_MeetsRatiosBandsVeinSizesAndSafeRoute()
        {
            for (int seed = 0; seed < 128; seed++)
            {
                MineLayerLayout layout = generator.Generate(seed, distribution);
                List<MineLayerCell> cells = layout.EnumerateCells().ToList();
                int contentCount = cells.Count(IsContent);
                float rockRatio = 1f - contentCount / (float)cells.Count;

                Assert.That(
                    rockRatio,
                    Is.InRange(0.85f, 0.9f),
                    $"seed {seed}: rock ratio");
                AssertBandKinds(cells, seed);
                Assert.That(
                    cells.Count(cell => cell.Kind == MineLayerCellKind.Copper),
                    Is.GreaterThan(0),
                    $"seed {seed}: copper minimum");
                Assert.That(
                    cells.Count(cell => cell.Kind == MineLayerCellKind.Iron),
                    Is.GreaterThan(0),
                    $"seed {seed}: iron minimum");
                Assert.That(
                    cells.Count(cell => cell.Kind == MineLayerCellKind.GasPocket),
                    Is.GreaterThan(0),
                    $"seed {seed}: gas minimum");
                Assert.That(
                    cells.Count(cell => cell.Kind == MineLayerCellKind.Lithium),
                    Is.GreaterThan(0),
                    $"seed {seed}: lithium minimum");
                Assert.That(
                    cells.Count(cell => cell.Kind == MineLayerCellKind.LockedSignal),
                    Is.EqualTo(1),
                    $"seed {seed}: locked signal");
                AssertVeinSizes(layout, MineLayerCellKind.Copper, seed);
                AssertVeinSizes(layout, MineLayerCellKind.Iron, seed);
                AssertVeinSizes(layout, MineLayerCellKind.Lithium, seed);
                AssertVeinSizes(layout, MineLayerCellKind.GasPocket, seed);
                Assert.That(layout.HasSafeRouteToSignal(), Is.True, $"seed {seed}: safe route");

                foreach (MineLayerCell cell in cells.Where(
                             cell => layout.IsProtectedRouteCell(cell.X, cell.Depth)))
                {
                    Assert.That(
                        cell.Kind,
                        cell.X == distribution.SignalX
                            && cell.Depth == distribution.SignalDepth
                                ? Is.EqualTo(MineLayerCellKind.LockedSignal)
                                : Is.EqualTo(MineLayerCellKind.Rock),
                        $"seed {seed}: protected route at {cell.X},{cell.Depth}");
                }
            }
        }

        [Test]
        public void LeftRightAndBottomBoundaries_AreUnmineableBoundaryIds()
        {
            MineLayerLayout layout = generator.Generate(9, distribution);
            for (int depth = 1; depth <= distribution.Depth; depth++)
            {
                AssertBoundary(layout.GetCell(distribution.MinX, depth));
                AssertBoundary(layout.GetCell(distribution.MaxX, depth));
            }

            for (int x = distribution.MinX; x <= distribution.MaxX; x++)
            {
                AssertBoundary(layout.GetCell(x, distribution.Depth));
            }
        }

        private static bool IsContent(MineLayerCell cell)
        {
            return cell.Kind != MineLayerCellKind.Rock
                && cell.Kind != MineLayerCellKind.BoundaryRock;
        }

        private static void AssertBandKinds(IEnumerable<MineLayerCell> cells, int seed)
        {
            foreach (MineLayerCell cell in cells.Where(IsContent))
            {
                if (cell.Depth <= 15)
                {
                    Assert.That(
                        cell.Kind,
                        Is.EqualTo(MineLayerCellKind.Copper),
                        $"seed {seed}: upper");
                }
                else if (cell.Depth <= 35)
                {
                    Assert.That(
                        cell.Kind,
                        Is.EqualTo(MineLayerCellKind.Iron)
                            .Or.EqualTo(MineLayerCellKind.GasPocket),
                        $"seed {seed}: middle");
                }
                else
                {
                    Assert.That(
                        cell.Kind,
                        Is.EqualTo(MineLayerCellKind.Lithium)
                            .Or.EqualTo(MineLayerCellKind.LockedSignal),
                        $"seed {seed}: deep");
                }
            }
        }

        private void AssertVeinSizes(
            MineLayerLayout layout,
            MineLayerCellKind kind,
            int seed)
        {
            var visited = new HashSet<(int x, int depth)>();
            for (int depth = 1; depth <= distribution.Depth; depth++)
            {
                for (int x = distribution.MinX + 1; x < distribution.MaxX; x++)
                {
                    if (layout.GetCell(x, depth).Kind != kind
                        || !visited.Add((x, depth)))
                    {
                        continue;
                    }

                    int size = FloodSize(layout, kind, x, depth, visited);
                    Assert.That(
                        size,
                        Is.InRange(distribution.MinVeinSize, distribution.MaxVeinSize),
                        $"seed {seed}: {kind} vein size");
                }
            }
        }

        private int FloodSize(
            MineLayerLayout layout,
            MineLayerCellKind kind,
            int startX,
            int startDepth,
            HashSet<(int x, int depth)> visited)
        {
            var queue = new Queue<(int x, int depth)>();
            queue.Enqueue((startX, startDepth));
            int size = 0;
            while (queue.Count > 0)
            {
                (int x, int depth) = queue.Dequeue();
                size++;
                foreach ((int dx, int dd) in new[] { (-1, 0), (1, 0), (0, -1), (0, 1) })
                {
                    int nextX = x + dx;
                    int nextDepth = depth + dd;
                    if (nextX <= distribution.MinX
                        || nextX >= distribution.MaxX
                        || nextDepth < 1
                        || nextDepth > distribution.Depth
                        || layout.GetCell(nextX, nextDepth).Kind != kind
                        || !visited.Add((nextX, nextDepth)))
                    {
                        continue;
                    }

                    queue.Enqueue((nextX, nextDepth));
                }
            }

            return size;
        }

        private static void AssertBoundary(MineLayerCell cell)
        {
            Assert.That(cell.Kind, Is.EqualTo(MineLayerCellKind.BoundaryRock));
            Assert.That(cell.TileId, Is.EqualTo(MineLayerTileIds.BoundaryRock));
            Assert.That(cell.IsMineable, Is.False);
        }
    }
}

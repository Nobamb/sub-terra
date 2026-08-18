using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace SubTerra.Gameplay.Structural.Tests
{
    public sealed class DeterministicCollapseSelectorTests
    {
        [Test]
        public void Select_SameSeedAndCandidateState_ReturnsSameCells()
        {
            var candidates = new List<Vector3Int>
            {
                new(-2, 3, 0),
                new(-1, 3, 0),
                new(0, 3, 0),
                new(1, 3, 0),
                new(2, 3, 0)
            };

            List<Vector3Int> first = DeterministicCollapseSelector.Select(candidates, 7123L, 3);
            candidates.Reverse();
            List<Vector3Int> second = DeterministicCollapseSelector.Select(candidates, 7123L, 3);

            Assert.That(second, Is.EqualTo(first));
        }

        [Test]
        public void Select_DoesNotMutateSourceCandidates()
        {
            var candidates = new List<Vector3Int>
            {
                new(3, 1, 0),
                new(1, 1, 0),
                new(2, 1, 0)
            };
            var original = new List<Vector3Int>(candidates);

            DeterministicCollapseSelector.Select(candidates, 99L, 2);

            Assert.That(candidates, Is.EqualTo(original));
        }

        [Test]
        public void Select_PrioritizesScoreThenActionDistanceThenHeight()
        {
            var candidates = new List<StructuralCollapseCandidate>
            {
                new(new Vector3Int(8, 2, 0), 120f),
                new(new Vector3Int(1, 2, 0), 120f),
                new(new Vector3Int(1, 4, 0), 120f),
                new(new Vector3Int(0, 9, 0), 100f)
            };

            List<Vector3Int> selected = DeterministicCollapseSelector.Select(
                candidates,
                Vector3Int.zero,
                77L,
                3);

            Assert.That(selected[0], Is.EqualTo(new Vector3Int(1, 2, 0)));
            Assert.That(selected[1], Is.EqualTo(new Vector3Int(1, 4, 0)));
            Assert.That(selected[2], Is.EqualTo(new Vector3Int(8, 2, 0)));
        }
    }
}

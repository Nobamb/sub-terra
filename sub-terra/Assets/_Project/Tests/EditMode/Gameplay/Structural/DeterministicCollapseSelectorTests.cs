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
    }
}

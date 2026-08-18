using System.Collections.Generic;
using UnityEngine;

namespace SubTerra.Gameplay.Structural
{
    public readonly struct StructuralCollapseCandidate
    {
        public Vector3Int Cell { get; }
        public float Score { get; }

        public StructuralCollapseCandidate(Vector3Int cell, float score)
        {
            Cell = cell;
            Score = score;
        }
    }

    /// <summary>점수·행동 근접도·높이를 우선하고 Seed는 완전 동점에만 사용하는 선택기.</summary>
    public static class DeterministicCollapseSelector
    {
        public static List<Vector3Int> Select(
            IReadOnlyList<StructuralCollapseCandidate> candidates,
            Vector3Int actionCell,
            long worldSeed,
            int maximumCount)
        {
            var ordered = new List<StructuralCollapseCandidate>(candidates);
            ordered.Sort((left, right) => Compare(left, right, actionCell, worldSeed));

            int count = Mathf.Clamp(maximumCount, 0, ordered.Count);
            var selected = new List<Vector3Int>(count);
            for (int i = 0; i < count; i++)
            {
                selected.Add(ordered[i].Cell);
            }

            return selected;
        }

        /// <summary>기존 호출 호환. 점수가 같을 때만 좌표/Seed 정렬을 사용한다.</summary>
        public static List<Vector3Int> Select(
            IReadOnlyList<Vector3Int> candidates,
            long worldSeed,
            int maximumCount)
        {
            var scored = new List<StructuralCollapseCandidate>(candidates.Count);
            for (int i = 0; i < candidates.Count; i++)
            {
                scored.Add(new StructuralCollapseCandidate(candidates[i], 0f));
            }

            return Select(scored, Vector3Int.zero, worldSeed, maximumCount);
        }

        private static int Compare(
            StructuralCollapseCandidate left,
            StructuralCollapseCandidate right,
            Vector3Int actionCell,
            long seed)
        {
            int scoreOrder = right.Score.CompareTo(left.Score);
            if (scoreOrder != 0) return scoreOrder;

            int leftDistance = Manhattan(left.Cell, actionCell);
            int rightDistance = Manhattan(right.Cell, actionCell);
            int distanceOrder = leftDistance.CompareTo(rightDistance);
            if (distanceOrder != 0) return distanceOrder;

            int heightOrder = right.Cell.y.CompareTo(left.Cell.y);
            if (heightOrder != 0) return heightOrder;

            ulong leftHash = Hash(seed, left.Cell);
            ulong rightHash = Hash(seed, right.Cell);
            int hashOrder = leftHash.CompareTo(rightHash);
            if (hashOrder != 0) return hashOrder;

            int xOrder = left.Cell.x.CompareTo(right.Cell.x);
            return xOrder != 0 ? xOrder : left.Cell.z.CompareTo(right.Cell.z);
        }

        private static int Manhattan(Vector3Int left, Vector3Int right)
        {
            return Mathf.Abs(left.x - right.x)
                + Mathf.Abs(left.y - right.y)
                + Mathf.Abs(left.z - right.z);
        }

        private static ulong Hash(long seed, Vector3Int cell)
        {
            unchecked
            {
                ulong value = (ulong)seed ^ 14695981039346656037UL;
                value = (value ^ (uint)cell.x) * 1099511628211UL;
                value = (value ^ (uint)cell.y) * 1099511628211UL;
                value = (value ^ (uint)cell.z) * 1099511628211UL;
                value ^= value >> 32;
                value *= 0xd6e8feb86659fd93UL;
                return value ^ (value >> 32);
            }
        }
    }
}

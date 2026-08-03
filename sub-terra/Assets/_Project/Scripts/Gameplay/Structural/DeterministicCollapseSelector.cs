using System.Collections.Generic;
using UnityEngine;

namespace SubTerra.Gameplay.Structural
{
    /// <summary>전역 난수 상태를 건드리지 않고 Seed와 좌표만으로 붕괴 후보를 고른다.</summary>
    public static class DeterministicCollapseSelector
    {
        public static List<Vector3Int> Select(
            IReadOnlyList<Vector3Int> candidates,
            long worldSeed,
            int maximumCount)
        {
            var ordered = new List<Vector3Int>(candidates);
            ordered.Sort((left, right) => Compare(left, right, worldSeed));
            if (ordered.Count > maximumCount)
            {
                ordered.RemoveRange(maximumCount, ordered.Count - maximumCount);
            }

            return ordered;
        }

        private static int Compare(Vector3Int left, Vector3Int right, long seed)
        {
            ulong leftHash = Hash(seed, left);
            ulong rightHash = Hash(seed, right);
            int hashOrder = leftHash.CompareTo(rightHash);
            if (hashOrder != 0) return hashOrder;

            int yOrder = left.y.CompareTo(right.y);
            return yOrder != 0 ? yOrder : left.x.CompareTo(right.x);
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

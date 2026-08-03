using System;
using System.Collections.Generic;
using SubTerra.Shared;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace SubTerra.Gameplay.Structural
{
    /// <summary>
    /// 채굴 변경점 주변만 재계산하고 확정된 균열·부분 붕괴 결과를 외부에 전달한다.
    /// </summary>
    public sealed class StructuralIntegritySystem : MonoBehaviour
    {
        [SerializeField] private Tilemap foregroundTilemap;
        [SerializeField] private StructuralCrackOverlay crackOverlay;
        [SerializeField] private StructuralRiskSettings riskSettings;
        [SerializeField, Min(1)] private int scanRadius = 3;
        [SerializeField, Min(1)] private int maximumCollapseTiles = 3;
        [SerializeField, Min(1f)] private float miningImpactMultiplier = 100f;
        [SerializeField] private long worldSeed = 20260731L;
        [SerializeField] private TileBase[] protectedTiles = Array.Empty<TileBase>();
        [SerializeField] private Vector3Int[] protectedCells = Array.Empty<Vector3Int>();
        [SerializeField] private StructuralSupport[] supports = Array.Empty<StructuralSupport>();

        private readonly Dictionary<Vector3Int, float> accumulatedImpact = new();
        private readonly Dictionary<Vector3Int, StructuralRiskLevel> evaluatedRegions = new();
        private StructuralRiskSettings runtimeSettings;

        public StructuralRiskLevel CurrentRisk { get; private set; } = StructuralRiskLevel.Stable;
        public long WorldSeed => worldSeed;
        public event Action<StructuralRiskLevel> RiskChanged;
        public event Action<StructuralCollapseEventDto> CollapseTriggered;

        public void ConfigureWorldSeed(long seed) => worldSeed = seed;

        public void NotifyTileMined(Vector3Int cell, MiningTileDto tile)
        {
            float impact = Mathf.Max(0f, tile.structuralImpact) * miningImpactMultiplier;
            accumulatedImpact.TryGetValue(cell, out float currentImpact);
            accumulatedImpact[cell] = currentImpact + impact;
            Reevaluate(cell, true);
        }

        public void RegisterSupport(StructuralSupport support)
        {
            if (support == null || Array.IndexOf(supports, support) >= 0) return;
            Array.Resize(ref supports, supports.Length + 1);
            supports[^1] = support;
            support.AvailabilityChanged += OnSupportAvailabilityChanged;
            ReevaluateAffectedBySupport(support);
        }

        public void UnregisterSupport(StructuralSupport support)
        {
            int index = Array.IndexOf(supports, support);
            if (index < 0) return;
            support.AvailabilityChanged -= OnSupportAvailabilityChanged;
            supports[index] = supports[^1];
            Array.Resize(ref supports, supports.Length - 1);
            ReevaluateAffectedBySupport(support);
        }

        public void RegisterProtectedCell(Vector3Int cell)
        {
            if (Array.IndexOf(protectedCells, cell) >= 0) return;
            Array.Resize(ref protectedCells, protectedCells.Length + 1);
            protectedCells[^1] = cell;
        }

        public StructuralRiskLevel EvaluateAt(Vector3Int center)
        {
            int unsupportedTiles = FindUnsupportedCeilingTiles(center).Count;
            int supportStrength = GetSupportStrength(center);
            float impact = GetAccumulatedImpact(center);
            return StructuralRiskEvaluator.Evaluate(
                impact,
                unsupportedTiles,
                supportStrength,
                Settings);
        }

        private StructuralRiskSettings Settings
        {
            get
            {
                if (riskSettings != null) return riskSettings;
                if (runtimeSettings == null)
                {
                    runtimeSettings = ScriptableObject.CreateInstance<StructuralRiskSettings>();
                    runtimeSettings.hideFlags = HideFlags.HideAndDontSave;
                }

                return runtimeSettings;
            }
        }

        private void Reevaluate(Vector3Int center, bool allowCollapse)
        {
            List<Vector3Int> candidates = FindUnsupportedCeilingTiles(center);
            StructuralRiskLevel risk = StructuralRiskEvaluator.Evaluate(
                GetAccumulatedImpact(center),
                candidates.Count,
                GetSupportStrength(center),
                Settings);
            evaluatedRegions[center] = risk;
            crackOverlay?.UpdateRegion(center, scanRadius, candidates, risk);
            UpdateCurrentRisk();
            if (!allowCollapse || risk != StructuralRiskLevel.CollapseImminent) return;

            StructuralCollapseEventDto collapse = CollapseUnsupportedCeiling(candidates);
            if (collapse.cells.Count == 0) return;

            CollapseTriggered?.Invoke(collapse);
            candidates = FindUnsupportedCeilingTiles(center);
            evaluatedRegions[center] = StructuralRiskEvaluator.Evaluate(
                GetAccumulatedImpact(center),
                candidates.Count,
                GetSupportStrength(center),
                Settings);
            crackOverlay?.UpdateRegion(center, scanRadius, candidates, evaluatedRegions[center]);
            UpdateCurrentRisk();
        }

        private List<Vector3Int> FindUnsupportedCeilingTiles(Vector3Int center)
        {
            var cells = new List<Vector3Int>();
            if (foregroundTilemap == null) return cells;

            for (int x = center.x - scanRadius; x <= center.x + scanRadius; x++)
            for (int y = center.y + 1; y <= center.y + scanRadius; y++)
            {
                var cell = new Vector3Int(x, y, center.z);
                if (!foregroundTilemap.HasTile(cell)
                    || foregroundTilemap.HasTile(cell + Vector3Int.down)
                    || IsProtected(cell)
                    || GetSupportStrength(cell) > 0)
                {
                    continue;
                }

                cells.Add(cell);
            }

            cells.Sort((left, right) =>
            {
                int height = right.y.CompareTo(left.y);
                return height != 0 ? height : left.x.CompareTo(right.x);
            });
            return cells;
        }

        private StructuralCollapseEventDto CollapseUnsupportedCeiling(
            IReadOnlyList<Vector3Int> candidates)
        {
            List<Vector3Int> selected = DeterministicCollapseSelector.Select(
                candidates,
                worldSeed,
                Mathf.Min(maximumCollapseTiles, candidates.Count));
            var collapse = new StructuralCollapseEventDto
            {
                worldSeed = worldSeed,
                severity = ResolveSeverity(selected.Count)
            };
            foreach (Vector3Int cell in selected)
            {
                foregroundTilemap.SetTile(cell, null);
                collapse.cells.Add(new CollapseCellDto { x = cell.x, y = cell.y });
            }

            foregroundTilemap.RefreshAllTiles();
            TilemapCollider2D collider = foregroundTilemap.GetComponent<TilemapCollider2D>();
            if (collider != null && collider.hasTilemapChanges)
                collider.ProcessTilemapChanges();
            return collapse;
        }

        private StructuralCollapseSeverity ResolveSeverity(int collapsedCount)
        {
            if (collapsedCount >= maximumCollapseTiles) return StructuralCollapseSeverity.Severe;
            return collapsedCount > 1
                ? StructuralCollapseSeverity.Major
                : StructuralCollapseSeverity.Minor;
        }

        private bool IsProtected(Vector3Int cell)
        {
            if (Array.IndexOf(protectedCells, cell) >= 0) return true;
            TileBase tile = foregroundTilemap.GetTile(cell);
            return tile != null && Array.IndexOf(protectedTiles, tile) >= 0;
        }

        private float GetAccumulatedImpact(Vector3Int center)
        {
            float total = 0f;
            foreach (KeyValuePair<Vector3Int, float> pair in accumulatedImpact)
            {
                if (Mathf.Abs(pair.Key.x - center.x) <= scanRadius
                    && Mathf.Abs(pair.Key.y - center.y) <= scanRadius)
                    total += pair.Value;
            }

            return total;
        }

        private int GetSupportStrength(Vector3Int cell)
        {
            if (foregroundTilemap == null) return 0;
            Vector3 worldPosition = foregroundTilemap.GetCellCenterWorld(cell);
            int total = 0;
            foreach (StructuralSupport support in supports)
            {
                if (support != null && support.isActiveAndEnabled && support.Supports(worldPosition))
                    total += support.Strength;
            }

            return total;
        }

        private void OnSupportAvailabilityChanged(StructuralSupport support)
        {
            ReevaluateAffectedBySupport(support);
        }

        private void ReevaluateAffectedBySupport(StructuralSupport support)
        {
            if (support == null || foregroundTilemap == null || accumulatedImpact.Count == 0) return;

            var affectedCenters = new List<Vector3Int>();
            foreach (Vector3Int center in accumulatedImpact.Keys)
            {
                Vector3 world = foregroundTilemap.GetCellCenterWorld(center);
                if (Vector2.Distance(world, support.transform.position) <= support.Radius + scanRadius)
                    affectedCenters.Add(center);
            }

            foreach (Vector3Int center in affectedCenters) Reevaluate(center, false);
        }

        private void UpdateCurrentRisk()
        {
            StructuralRiskLevel highest = StructuralRiskLevel.Stable;
            foreach (StructuralRiskLevel risk in evaluatedRegions.Values)
            {
                if (risk > highest) highest = risk;
            }

            if (CurrentRisk == highest) return;
            CurrentRisk = highest;
            RiskChanged?.Invoke(highest);
        }

        private void OnDestroy()
        {
            foreach (StructuralSupport support in supports)
            {
                if (support != null) support.AvailabilityChanged -= OnSupportAvailabilityChanged;
            }

            if (runtimeSettings == null) return;
            if (Application.isPlaying) Destroy(runtimeSettings);
            else DestroyImmediate(runtimeSettings);
        }
    }
}

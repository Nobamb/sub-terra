using System;
using System.Collections.Generic;
using SubTerra.Shared;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace SubTerra.Gameplay.Structural
{
    /// <summary>
    /// 채굴 변경점 주변만 재계산하고 확정된 균열·부분 붕괴 결과를 외부에 전달한다.
    /// 위험 원인이 해소되면(비지지 천장 제거·버팀목 설치) 누적 충격을 지우고 안정 상태로 복귀한다.
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
            // 채굴 지점 + 알려진 모든 영향 구역을 다시 평가해 고착된 위험 단계를 해소한다.
            Reevaluate(cell, true);
            RefreshAllKnownRegions(cell);
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
            // 현재 비지지 구조가 없으면 과거 충격과 무관하게 안정으로 본다.
            if (unsupportedTiles == 0)
            {
                return StructuralRiskLevel.Stable;
            }

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
            StructuralRiskLevel risk = ResolveRisk(center, candidates);
            evaluatedRegions[center] = risk;
            crackOverlay?.UpdateRegion(center, scanRadius, candidates, risk);

            if (!allowCollapse || risk != StructuralRiskLevel.CollapseImminent)
            {
                UpdateCurrentRisk();
                return;
            }

            StructuralCollapseEventDto collapse = CollapseUnsupportedCeiling(candidates);
            if (collapse.cells.Count == 0)
            {
                UpdateCurrentRisk();
                return;
            }

            CollapseTriggered?.Invoke(collapse);
            candidates = FindUnsupportedCeilingTiles(center);
            risk = ResolveRisk(center, candidates);
            evaluatedRegions[center] = risk;
            crackOverlay?.UpdateRegion(center, scanRadius, candidates, risk);
            UpdateCurrentRisk();
        }

        /// <summary>
        /// 비지지 천장이 없으면 누적 충격을 제거하고 Stable을 반환한다.
        /// 위험이 한 번 뜬 뒤 원인 블록을 제거해도 단계가 남는 문제를 막는다.
        /// </summary>
        private StructuralRiskLevel ResolveRisk(Vector3Int center, List<Vector3Int> candidates)
        {
            if (candidates == null || candidates.Count == 0)
            {
                ClearImpactNear(center);
                return StructuralRiskLevel.Stable;
            }

            return StructuralRiskEvaluator.Evaluate(
                GetAccumulatedImpact(center),
                candidates.Count,
                GetSupportStrength(center),
                Settings);
        }

        private void RefreshAllKnownRegions(Vector3Int primaryCenter)
        {
            var centers = new List<Vector3Int>(accumulatedImpact.Count + evaluatedRegions.Count);
            foreach (Vector3Int key in accumulatedImpact.Keys)
            {
                if (key != primaryCenter)
                {
                    centers.Add(key);
                }
            }

            foreach (Vector3Int key in evaluatedRegions.Keys)
            {
                if (key == primaryCenter || centers.Contains(key))
                {
                    continue;
                }

                centers.Add(key);
            }

            for (int i = 0; i < centers.Count; i++)
            {
                // 연쇄 붕괴는 1차 채굴 지점에서만 허용한다.
                Reevaluate(centers[i], false);
            }
        }

        private void ClearImpactNear(Vector3Int center)
        {
            if (accumulatedImpact.Count == 0)
            {
                return;
            }

            var remove = new List<Vector3Int>();
            foreach (Vector3Int key in accumulatedImpact.Keys)
            {
                if (Mathf.Abs(key.x - center.x) <= scanRadius
                    && Mathf.Abs(key.y - center.y) <= scanRadius)
                {
                    remove.Add(key);
                }
            }

            for (int i = 0; i < remove.Count; i++)
            {
                accumulatedImpact.Remove(remove[i]);
            }
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
            if (support == null || foregroundTilemap == null)
            {
                return;
            }

            // 버팀목 설치/제거 시 충격 이력이 없어도 주변 위험 구역을 다시 본다.
            var affectedCenters = new HashSet<Vector3Int>();
            foreach (Vector3Int center in accumulatedImpact.Keys)
            {
                Vector3 world = foregroundTilemap.GetCellCenterWorld(center);
                if (Vector2.Distance(world, support.transform.position) <= support.Radius + scanRadius)
                    affectedCenters.Add(center);
            }

            foreach (Vector3Int center in evaluatedRegions.Keys)
            {
                Vector3 world = foregroundTilemap.GetCellCenterWorld(center);
                if (Vector2.Distance(world, support.transform.position) <= support.Radius + scanRadius)
                    affectedCenters.Add(center);
            }

            // 영향 중심이 없으면 버팀목 위치 기준으로 한 번 평가한다.
            if (affectedCenters.Count == 0)
            {
                Vector3Int supportCell = foregroundTilemap.WorldToCell(support.transform.position);
                affectedCenters.Add(supportCell);
            }

            foreach (Vector3Int center in affectedCenters)
            {
                Reevaluate(center, false);
            }

            UpdateCurrentRisk();
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

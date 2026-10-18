using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SubTerra.App.Core;
using SubTerra.App.Core.Data;
using SubTerra.App.Progression;
using SubTerra.Gameplay.DemoWorld;
using SubTerra.Gameplay.Hazards;
using SubTerra.Gameplay.Mining;
using SubTerra.Gameplay.Snapshot;
using SubTerra.Gameplay.Structural;
using SubTerra.Shared;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace SubTerra.App.Tests.PlayMode.MineDemo
{
    /// <summary>#160: 실제 생성 타일/흔들림 그림과 채굴 거래·가스·월드 복원의 연결을 추적한다.</summary>
    public sealed class Issue160MiningIdentityPlayModeTests
    {
        [UnityTest]
        public IEnumerator GeneratedTiles_10To40m_KeepIdentityRewardsAndGas_BeforeAndAfterWorldRestore()
        {
            using (var env = new UiTestEnvironment())
            {
                yield return env.LoadIntegration();
                // 좌표/보상 경로를 분리해 조사한다. 일반 이동 입력이나 진행도 해금 검사가 아니다.
                Assert.That(env.Save.Upgrades.TryRestore(new[] {
                    new UpgradeLevelState(DataIds.Upgrades.DrillSpeed, 2) }), Is.True);
                Assert.That(env.Save.Upgrades.TryRestoreUnlockedZones(new[] { DataIds.Zones.Deep }), Is.True);
                Time.timeScale = 0f;
                var mining = Object.FindAnyObjectByType<MiningSystem>();
                var resolver = Object.FindAnyObjectByType<MiningTileResolver>();
                var generator = Object.FindAnyObjectByType<MineLayerTilemapGenerator>();
                var world = Object.FindAnyObjectByType<WorldSnapshotSystem>();
                var gas = Object.FindAnyObjectByType<GasHazardSystem>();
                var feedback = Object.FindAnyObjectByType<StructuralCrackOverlay>();
                Assert.That(mining, Is.Not.Null);
                Assert.That(resolver, Is.Not.Null);
                Assert.That(generator, Is.Not.Null);
                Assert.That(world, Is.Not.Null);
                Assert.That(gas, Is.Not.Null);
                Assert.That(feedback, Is.Not.Null);
                var map = mining.ForegroundTilemap;
                var identities = new Dictionary<Vector3Int, Identity>();
                foreach (var generated in generator.CurrentLayout.EnumerateCells())
                {
                    if (generated.Depth < 10 || generated.Depth > 40) continue;
                    var cell = new Vector3Int(generated.X, generated.Y, 0);
                    var tile = map.GetTile(cell);
                    if (tile == null) continue;
                    Assert.That(resolver.TryResolve(tile, out var dto), Is.True, cell.ToString());
                    Assert.That(MineLayerTileIds.BaseTileId(dto.tileId), Is.EqualTo(generated.TileId), cell.ToString());
                    Assert.That(map.GetSprite(cell), Is.Not.Null, dto.tileId);
                    if (generated.Kind == MineLayerCellKind.Lithium)
                    {
                        Assert.That(dto.mineralId, Is.EqualTo(DataIds.Minerals.Lithium));
                        Assert.That(dto.quantity, Is.GreaterThan(0));
                        Assert.That(dto.containsGas, Is.True);
                    }
                    identities.Add(cell, new Identity(generated.Depth, dto, map.GetSprite(cell)));
                }
                Assert.That(identities.Values.Any(x => x.Depth >= 36 && x.Dto.mineralId == DataIds.Minerals.Lithium), Is.True);
                // 일반/금 변형과 10~15,16~35,36~40m 구간을 각각 포함한다.
                var groups = identities.Where(x => IsSample(x.Value.Dto))
                    .GroupBy(x => (x.Value.Depth <= 15 ? 0 : x.Value.Depth <= 35 ? 1 : 2,
                        x.Value.Dto.tileId)).ToArray();
                var mined = new HashSet<Vector3Int>();
                var events = new List<MiningTileDto>();
                mining.TileMined += (_, dto) => events.Add(dto);
                foreach (var group in groups)
                {
                    var pair = group.First();
                    MineAndAssert(pair.Key, pair.Value, env, mining, resolver, gas, feedback, events);
                    mined.Add(pair.Key);
                }
                var snapshot = world.CaptureSnapshot();
                var quantities = env.Save.InventoryService.GetSnapshot().Stacks.ToDictionary(x => x.MineralId, x => x.Quantity);
                Assert.That(world.RestoreSnapshot(snapshot), Is.True, world.LastRestoreFailureReason);
                foreach (var pair in identities)
                {
                    if (mined.Contains(pair.Key))
                    {
                        Assert.That(map.HasTile(pair.Key), Is.False, "저장된 채굴 셀이 복원 뒤 되살아나면 안 된다.");
                        continue;
                    }
                    Assert.That(resolver.TryResolve(map.GetTile(pair.Key), out var dto), Is.True, pair.Key.ToString());
                    Assert.That(dto.tileId, Is.EqualTo(pair.Value.Dto.tileId), pair.Key.ToString());
                    Assert.That(dto.mineralId, Is.EqualTo(pair.Value.Dto.mineralId));
                    Assert.That(dto.quantity, Is.EqualTo(pair.Value.Dto.quantity));
                    Assert.That(dto.goldDrop, Is.EqualTo(pair.Value.Dto.goldDrop));
                    Assert.That(dto.requiredDrillLevel, Is.EqualTo(pair.Value.Dto.requiredDrillLevel));
                    Assert.That(dto.containsGas, Is.EqualTo(pair.Value.Dto.containsGas));
                    Assert.That(map.GetSprite(pair.Key), Is.SameAs(pair.Value.Sprite));
                }
                foreach (var pair in quantities)
                    Assert.That(env.Save.InventoryService.State.GetQuantity(pair.Key), Is.EqualTo(pair.Value), "월드 복원 자체는 보상을 추가하지 않는다.");
                var afterRestore = 0;
                foreach (var group in groups)
                {
                    var remaining = group.Where(x => !mined.Contains(x.Key)).ToArray();
                    if (remaining.Length == 0) continue;
                    var pair = remaining[0];
                    MineAndAssert(pair.Key, pair.Value, env, mining, resolver, gas, feedback, events);
                    afterRestore++;
                }
                Debug.Log($"[Issue160] seed={generator.WorldSeed}; audited={identities.Count}; beforeRestore={groups.Length}; afterRestore={afterRestore}; all tile/clone/reward/gas assertions passed.");
            }
        }

        private static bool IsSample(MiningTileDto dto) => dto.mineralId == DataIds.Minerals.Lithium
            || MineLayerTileIds.BaseTileId(dto.tileId) == MineLayerTileIds.GasPocket
            || MineLayerTileIds.BaseTileId(dto.tileId) == MineLayerTileIds.Rock;

        private static void MineAndAssert(Vector3Int cell, Identity identity, UiTestEnvironment env,
            MiningSystem mining, MiningTileResolver resolver, GasHazardSystem gas,
            StructuralCrackOverlay feedback, List<MiningTileDto> events)
        {
            var map = mining.ForegroundTilemap;
            var tile = map.GetTile(cell);
            Assert.That(resolver.TryResolve(tile, out var dto), Is.True);
            Assert.That(dto.tileId, Is.EqualTo(identity.Dto.tileId));
            Assert.That(dto.mineralId, Is.EqualTo(identity.Dto.mineralId));
            Assert.That(dto.quantity, Is.EqualTo(identity.Dto.quantity));
            Assert.That(dto.containsGas, Is.EqualTo(identity.Dto.containsGas));
            feedback.SetCell(cell, StructuralRiskLevel.Danger, 0.6f, StructuralRiskCause.Unsupported);
            Assert.That(map.GetTile(cell), Is.SameAs(tile), "표시 처리는 실제 논리 타일을 바꾸지 않는다.");
            var clone = feedback.transform.Find($"StructuralShake_{cell.x}_{cell.y}");
            Assert.That(clone, Is.Not.Null);
            Assert.That(clone.GetComponent<SpriteRenderer>().sprite, Is.SameAs(identity.Sprite));
            Assert.That(clone.GetComponent<SpriteRenderer>().sortingOrder, Is.LessThan(map.GetComponent<TilemapRenderer>().sortingOrder));
            var inventory = env.Save.InventoryService;
            var before = string.IsNullOrEmpty(dto.mineralId) ? 0 : inventory.State.GetQuantity(dto.mineralId);
            var state = GameBootstrapper.Instance.State;
            var goldBefore = state.Player.Gold;
            var zones = gas.ActiveZones.Count;
            var eventCount = events.Count;
            state.SetCurrentEnergy(state.Player.MaxEnergy);
            Assert.That(mining.TryMineInstant(cell), Is.True, $"{cell} {dto.tileId} failure={mining.LastFailure}");
            Assert.That(map.HasTile(cell), Is.False);
            Assert.That(events.Count, Is.EqualTo(eventCount + 1));
            Assert.That(events[events.Count - 1].tileId, Is.EqualTo(identity.Dto.tileId));
            Assert.That(state.Player.Gold - goldBefore, Is.EqualTo(identity.Dto.goldDrop), "테스트에는 골드 증가 업그레이드가 없다.");
            var bonus = env.Save.Progression.Effects.GetMiningYieldBonus(dto.mineralId);
            if (!string.IsNullOrEmpty(dto.mineralId))
                Assert.That(inventory.State.GetQuantity(dto.mineralId) - before, Is.EqualTo(dto.quantity + bonus));
            Assert.That(gas.ActiveZones.Count - zones, Is.EqualTo(dto.containsGas ? 1 : 0));
            if (dto.containsGas)
                Assert.That(gas.ActiveZones[gas.ActiveZones.Count - 1].transform.position, Is.EqualTo(map.GetCellCenterWorld(cell)));
            feedback.ClearCell(cell);
            Assert.That(feedback.HasShakeVisual(cell), Is.False);
            Assert.That(mining.TryMineInstant(cell), Is.False, "빈 셀 재채굴로 중복 지급/가스가 생기면 안 된다.");
            Assert.That(events.Count, Is.EqualTo(eventCount + 1));
            Assert.That(gas.ActiveZones.Count - zones, Is.EqualTo(dto.containsGas ? 1 : 0));
            Assert.That(state.Player.Gold - goldBefore, Is.EqualTo(identity.Dto.goldDrop));
            if (!string.IsNullOrEmpty(dto.mineralId))
                Assert.That(inventory.State.GetQuantity(dto.mineralId) - before, Is.EqualTo(dto.quantity + bonus));
            Debug.Log($"[Issue160] cell={cell} depth={identity.Depth} tile={dto.tileId} sprite={identity.Sprite.name} mineral={dto.mineralId} quantity={dto.quantity + bonus} gas={dto.containsGas}");
        }

        private readonly struct Identity
        {
            public readonly int Depth;
            public readonly MiningTileDto Dto;
            public readonly Sprite Sprite;
            public Identity(int depth, MiningTileDto dto, Sprite sprite) { Depth = depth; Dto = dto; Sprite = sprite; }
        }
    }
}

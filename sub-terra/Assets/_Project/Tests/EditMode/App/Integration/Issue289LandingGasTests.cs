using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using SubTerra.Gameplay.Hazards;
using SubTerra.Gameplay.Integration;
using SubTerra.Gameplay.Mining;
using SubTerra.Gameplay.Structural;
using SubTerra.Shared;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace SubTerra.App.Tests.Integration
{
    /// <summary>
    /// B-193: 오버레이가 나중에 준비돼도 배율 콜백·착지 구독이 붙고,
    /// 같은 묶음의 아래 칸이 먼저 떨어져도 남은 바닥 위에 가스가 한 번 퍼진다.
    /// </summary>
    public sealed class Issue289LandingGasTests
    {
        private readonly List<Object> created = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            for (var i = created.Count - 1; i >= 0; i--)
            {
                if (created[i] != null) Object.DestroyImmediate(created[i]);
            }

            created.Clear();
        }

        [Test]
        public void LateOverlay_StillGetsDamageMultiplierAndLandingGas()
        {
            var rig = CreateRig(connectOverlay: false);
            var receiver = new RecordingReceiver();
            var multiplierCalls = new List<TileBase>();

            // 오버레이가 아직 없는 시점에 브리지가 켜지고 콜백이 연결된다.
            Invoke(rig.Bridge, "OnEnable");
            rig.Bridge.SetCollapseDamageReceiver(receiver, tile =>
            {
                multiplierCalls.Add(tile);
                return 1.5f;
            });

            // 이후에 오버레이가 준비된다.
            Set(rig.Structural, "crackOverlay", rig.Overlay);
            Invoke(rig.Structural, "Update");

            DropAndLand(rig, rig.GasTile, new Vector3Int(0, 3, 0));

            Assert.That(receiver.Multipliers, Is.EqualTo(new[] { 1.5f }), "배율 콜백이 접촉 피해까지 전달되어야 한다");
            Assert.That(multiplierCalls, Has.Count.EqualTo(1));
            Assert.That(multiplierCalls[0], Is.SameAs(rig.GasTile));
            Assert.That(rig.Gas.ActiveZones, Has.Count.EqualTo(1), "착지 구독이 버려지지 않아야 한다");
            Invoke(rig.Bridge, "OnDisable");
        }

        [Test]
        public void LateMultiplierCallback_IsReadAtContactTime()
        {
            var rig = CreateRig(connectOverlay: true);
            var receiver = new RecordingReceiver();
            rig.Structural.BindCollapseDamageReceiver(receiver);

            rig.Map.SetTile(new Vector3Int(0, 3, 0), rig.RockTile);
            rig.Overlay.PlayCollapse(new Vector3Int(0, 3, 0), rig.Map, 0.4f);
            // 낙하가 시작된 뒤에 콜백이 붙어도 접촉 시점에는 반영된다.
            rig.Structural.BindCollapseDamageReceiver(receiver, _ => 2f);
            Advance(rig, 0.5f);

            Assert.That(receiver.Multipliers, Is.EqualTo(new[] { 2f }));
        }

        [Test]
        public void UnresolvableTile_KeepsDefaultDamageMultiplier()
        {
            var rig = CreateRig(connectOverlay: true);
            var receiver = new RecordingReceiver();
            rig.Structural.BindCollapseDamageReceiver(receiver, _ => 0f);

            rig.Map.SetTile(new Vector3Int(0, 3, 0), rig.RockTile);
            rig.Overlay.PlayCollapse(new Vector3Int(0, 3, 0), rig.Map, 0.4f);
            Advance(rig, 0.5f);

            Assert.That(receiver.Multipliers, Is.EqualTo(new[] { 1f }));
        }

        [Test]
        public void StackedBlocksFallingTogether_StillSpreadGasOnTheRemainingFloor()
        {
            var rig = CreateRig(connectOverlay: true);
            Invoke(rig.Bridge, "OnEnable");
            var upper = new Vector3Int(0, 4, 0);
            var lower = new Vector3Int(0, 3, 0);
            rig.Map.SetTile(upper, rig.GasTile);
            rig.Map.SetTile(lower, rig.RockTile);
            rig.Map.CompressBounds();

            // 위 칸이 먼저 떨어지기 시작해 아래 칸을 바닥으로 잡고, 곧 아래 칸도 같이 떨어진다.
            rig.Overlay.PlayCollapse(upper, rig.Map, 0.4f);
            rig.Map.SetTile(upper, null);
            rig.Overlay.PlayCollapse(lower, rig.Map, 0.4f);
            rig.Map.SetTile(lower, null);
            Advance(rig, 0.5f);

            Assert.That(rig.Gas.ActiveZones, Has.Count.EqualTo(1));
            Assert.That(
                rig.Gas.ActiveZones[0].transform.position,
                Is.EqualTo(rig.Map.GetCellCenterWorld(Vector3Int.up)),
                "가스는 실제로 남은 바닥 위에 퍼져야 한다");
            Invoke(rig.Bridge, "OnDisable");
        }

        [Test]
        public void FallingForeverWithoutFloor_SpreadsNoGas()
        {
            var rig = CreateRig(connectOverlay: true, withFloor: false);
            Invoke(rig.Bridge, "OnEnable");

            DropAndLand(rig, rig.GasTile, new Vector3Int(0, 3, 0));

            Assert.That(rig.Gas.ActiveZones, Is.Empty);
            Invoke(rig.Bridge, "OnDisable");
        }

        [Test]
        public void RockTile_SpreadsNoGas_AndGasTileDoes()
        {
            var rig = CreateRig(connectOverlay: true);
            Invoke(rig.Bridge, "OnEnable");

            DropAndLand(rig, rig.RockTile, new Vector3Int(0, 3, 0));
            Assert.That(rig.Gas.ActiveZones, Is.Empty);

            DropAndLand(rig, rig.GasTile, new Vector3Int(0, 3, 0));
            Assert.That(rig.Gas.ActiveZones, Has.Count.EqualTo(1));
            Invoke(rig.Bridge, "OnDisable");
        }

        [Test]
        public void WorldClearDuringFall_SpreadsNoGas()
        {
            var rig = CreateRig(connectOverlay: true);
            Invoke(rig.Bridge, "OnEnable");
            var cell = new Vector3Int(0, 3, 0);
            rig.Map.SetTile(cell, rig.GasTile);
            rig.Overlay.PlayCollapse(cell, rig.Map, 0.4f);
            rig.Map.SetTile(cell, null);
            Advance(rig, 0.1f);

            rig.Structural.ClearRuntimeRiskState();
            Advance(rig, 1f);

            Assert.That(rig.Gas.ActiveZones, Is.Empty);
            Invoke(rig.Bridge, "OnDisable");
        }

        private static void DropAndLand(Rig rig, TileBase tile, Vector3Int cell)
        {
            rig.Map.SetTile(cell, tile);
            rig.Map.CompressBounds();
            rig.Overlay.PlayCollapse(cell, rig.Map, 0.4f);
            rig.Map.SetTile(cell, null);
            Advance(rig, 0.2f);
            Advance(rig, 0.5f);
        }

        private static void Advance(Rig rig, float seconds)
        {
            Invoke(rig.Overlay, "UpdateFallingVisuals", seconds);
        }

        private Rig CreateRig(bool connectOverlay, bool withFloor = true)
        {
            var host = new GameObject("Issue289LandingRig");
            host.SetActive(false);
            created.Add(host);
            var grid = new GameObject("Grid", typeof(Grid));
            grid.transform.SetParent(host.transform);
            var mapObject = new GameObject("Foreground", typeof(Tilemap));
            mapObject.transform.SetParent(grid.transform);
            var map = mapObject.GetComponent<Tilemap>();

            var rock = ScriptableObject.CreateInstance<Tile>();
            var gas = ScriptableObject.CreateInstance<Tile>();
            var floor = ScriptableObject.CreateInstance<Tile>();
            var settings = ScriptableObject.CreateInstance<StructuralRiskSettings>();
            created.Add(rock);
            created.Add(gas);
            created.Add(floor);
            created.Add(settings);

            var overlay = host.AddComponent<StructuralCrackOverlay>();
            var structural = host.AddComponent<StructuralIntegritySystem>();
            var resolver = host.AddComponent<MiningTileResolver>();
            var mining = host.AddComponent<MiningSystem>();
            var hazards = host.AddComponent<GasHazardSystem>();
            var bridge = host.AddComponent<GameplayEventBridge>();
            Set(structural, "foregroundTilemap", map);
            Set(structural, "crackOverlay", connectOverlay ? overlay : null);
            Set(structural, "riskSettings", settings);
            Set(mining, "tileResolver", resolver);
            Set(hazards, "foregroundTilemap", map);
            Set(bridge, "miningSystem", mining);
            Set(bridge, "structuralSystem", structural);
            Set(bridge, "gasHazardSystem", hazards);
            resolver.RegisterRuntime(rock, new MiningTileDto { tileId = "tile.rock.normal" });
            resolver.RegisterRuntime(gas, new MiningTileDto
            {
                tileId = "tile.gas-pocket",
                containsGas = true,
                structuralImpact = 0.9f
            });
            if (withFloor) map.SetTile(Vector3Int.zero, floor);
            else map.SetTile(new Vector3Int(5, 5, 0), floor);
            map.CompressBounds();

            return new Rig
            {
                Map = map,
                Overlay = overlay,
                Structural = structural,
                Bridge = bridge,
                Gas = hazards,
                RockTile = rock,
                GasTile = gas
            };
        }

        private static void Set(object target, string name, object value)
        {
            var field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, "Missing field: " + name);
            field.SetValue(target, value);
        }

        private static void Invoke(object target, string name, params object[] args)
        {
            var method = target.GetType().GetMethod(
                name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.That(method, Is.Not.Null, "Missing method: " + name);
            method.Invoke(target, args.Length == 0 ? null : args);
        }

        private sealed class Rig
        {
            public Tilemap Map;
            public StructuralCrackOverlay Overlay;
            public StructuralIntegritySystem Structural;
            public GameplayEventBridge Bridge;
            public GasHazardSystem Gas;
            public Tile RockTile;
            public Tile GasTile;
        }

        private sealed class RecordingReceiver : ICollapseDamageReceiver
        {
            public readonly List<float> Multipliers = new List<float>();

            public bool IsCollapseContact(float fromX, float fromY, float toX, float toY) => true;

            public bool ApplyCollapseImpact(float damageMultiplier = 1f)
            {
                Multipliers.Add(damageMultiplier);
                return true;
            }
        }
    }
}

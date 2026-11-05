using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SubTerra.App.Core.Data;
using SubTerra.Gameplay.Building;
using SubTerra.Gameplay.Integration;
using SubTerra.Gameplay.Power;
using SubTerra.Shared;
using UnityEngine;

namespace SubTerra.App.Tests.Integration
{
    /// <summary>
    /// B-185-1: 켜짐 연출은 실제 게임 켜짐 판정(GameplayEventBridge 공급 범위)을 따른다.
    /// 케이블 용량 기반 PowerNode.IsPowered가 아니라 코어·엘리베이터 반경 안으로 들어오는 순간이 기준이다.
    /// </summary>
    public sealed class PromptB185RangePowerOnEffectTests
    {
        private readonly List<(PowerFacility facility, bool isBatch)> started = new();
        private readonly List<int> batches = new();
        private GameObject root;
        private GameObject elevator;
        private PowerNetworkSystem network;
        private GameplayEventBridge bridge;
        private bool bridgeEnabled;
        private bool previousReduceMotion;

        [SetUp]
        public void SetUp()
        {
            previousReduceMotion = AccessibilityPreferences.ReduceMotion;
            AccessibilityPreferences.ReduceMotion = false;
            root = new GameObject("B185_1_Root");
            network = root.AddComponent<PowerNetworkSystem>();
            bridge = root.AddComponent<GameplayEventBridge>();
            SetField(bridge, "powerNetworkSystem", network);
            PowerOnEffect.Started += OnStarted;
            PowerOnEffect.BatchStarted += OnBatchStarted;
        }

        [TearDown]
        public void TearDown()
        {
            PowerOnEffect.Started -= OnStarted;
            PowerOnEffect.BatchStarted -= OnBatchStarted;
            if (bridgeEnabled) Call(bridge, "OnDisable");
            bridgeEnabled = false;
            while (PowerOnEffect.IsSuppressed) PowerOnEffect.EndSuppression();
            PowerOnEffect.FlushPendingStarts();
            if (elevator != null) Object.DestroyImmediate(elevator);
            if (root != null) Object.DestroyImmediate(root);
            AccessibilityPreferences.ReduceMotion = previousReduceMotion;
            started.Clear();
            batches.Clear();
        }

        [Test]
        public void CoreFirst_ThenChargerPlacedInRange_PlaysEffectOnce()
        {
            EnableBridge();
            CreateCore("core-1", Vector3.zero);
            Assert.That(started, Is.Empty, "코어 자체는 연출 대상이 아님");

            var charger = CreateFacility("charger-1", DataIds.Buildings.ChargerBasic, new Vector3(4f, 0f, 0f));

            Assert.That(charger.IsInSupplyRange, Is.True);
            Assert.That(charger.GetComponent<PowerNode>().IsPowered, Is.False, "케이블 없이 범위만으로 켜지는 실제 게임 조건");
            Assert.That(charger.PowerOnEffectInstance, Is.Not.Null);
            Assert.That(charger.PowerOnEffectInstance.IsPlaying, Is.True);
            Assert.That(started.Select(entry => entry.facility), Is.EqualTo(new[] { charger }));
            Assert.That(started[0].isBatch, Is.False);
            Assert.That(batches, Is.EqualTo(new[] { 1 }));
            Assert.That(FacilityStatuses().Single().isActive, Is.True, "연출 기준과 HUD/CCTV 켜짐 판정이 같다");
        }

        [Test]
        public void ChargerFirst_ThenCorePlaced_PlaysEffectWhenRangeAppears()
        {
            EnableBridge();
            var charger = CreateFacility("charger-1", DataIds.Buildings.ChargerBasic, new Vector3(4f, 0f, 0f));
            Assert.That(charger.IsInSupplyRange, Is.False);
            Assert.That(started, Is.Empty, "범위가 없으면 연출 없음");
            Assert.That(FacilityStatuses().Single().isActive, Is.False);

            CreateCore("core-1", Vector3.zero);

            Assert.That(charger.IsInSupplyRange, Is.True);
            Assert.That(charger.PowerOnEffectInstance.IsPlaying, Is.True);
            Assert.That(started.Select(entry => entry.facility), Is.EqualTo(new[] { charger }));
            Assert.That(batches, Is.EqualTo(new[] { 1 }));
            Assert.That(FacilityStatuses().Single().isActive, Is.True);
        }

        [Test]
        public void ChargerOutsideRange_NoEffect_EvenWhenCableConnected()
        {
            EnableBridge();
            var core = CreateCore("core-1", Vector3.zero, 100);
            var charger = CreateFacility("charger-1", DataIds.Buildings.ChargerBasic, new Vector3(10.5f, 0f, 0f));
            var cable = new GameObject("Cable").AddComponent<PowerCable>();
            cable.transform.SetParent(root.transform);
            cable.Configure(network, core, charger.GetComponent<PowerNode>());
            network.RegisterCable(cable);

            Assert.That(charger.GetComponent<PowerNode>().IsPowered, Is.True);
            Assert.That(charger.IsInSupplyRange, Is.False);
            Assert.That(charger.PowerOnEffectInstance, Is.Null);
            Assert.That(started, Is.Empty, "실제 게임 판정(범위 밖=꺼짐)과 같게 연출 없음");
            Assert.That(FacilityStatuses().Single().isActive, Is.False);
        }

        [Test]
        public void AllFiveFacilities_TurnOnTogether_EachPlays_BatchOnce()
        {
            EnableBridge();
            var ids = new[]
            {
                DataIds.Buildings.ChargerBasic,
                DataIds.Buildings.ClinicBasic,
                DataIds.Buildings.SettlementBasic,
                DataIds.Buildings.LightBasic,
                DataIds.Buildings.StorageBasic
            };
            var facilities = ids.Select((id, i) => CreateFacility("f-" + i, id, new Vector3(i * 1.5f - 3f, 2f, 0f))).ToArray();
            Assert.That(started, Is.Empty);

            CreateCore("core-1", Vector3.zero);

            Assert.That(facilities.All(facility => facility.PowerOnEffectInstance.IsPlaying), Is.True);
            Assert.That(started.Select(entry => entry.facility), Is.EquivalentTo(facilities));
            Assert.That(started.All(entry => entry.isBatch), Is.True);
            Assert.That(batches, Is.EqualTo(new[] { 5 }), "여러 개가 한 번에 켜져도 BatchStarted는 한 번");
        }

        [Test]
        public void RebuildAgain_MoveOut_MoveIn_OnlyOffToOnPlays()
        {
            EnableBridge();
            CreateCore("core-1", Vector3.zero);
            var charger = CreateFacility("charger-1", DataIds.Buildings.ChargerBasic, new Vector3(4f, 0f, 0f));
            PowerOnEffect effect = charger.PowerOnEffectInstance;
            effect.Tick(0.1f);
            started.Clear();
            batches.Clear();

            network.Rebuild();
            Assert.That(started, Is.Empty, "켜짐→켜짐은 연출 없음");
            Assert.That(effect.IsPlaying, Is.True, "진행 중인 연출을 다시 시작하지 않는다");

            charger.transform.position = new Vector3(30f, 0f, 0f);
            network.Rebuild();
            Assert.That(charger.IsInSupplyRange, Is.False);
            Assert.That(effect.IsPlaying, Is.False, "켜짐→꺼짐은 바로 정리");
            Assert.That(started, Is.Empty);

            charger.transform.position = new Vector3(4f, 0f, 0f);
            network.Rebuild();
            Assert.That(effect.IsPlaying, Is.True);
            Assert.That(started, Has.Count.EqualTo(1));
            Assert.That(batches, Is.EqualTo(new[] { 1 }));
        }

        [Test]
        public void ElevatorRange_ChargerPlacedNearElevator_PlaysEffect()
        {
            EnableBridge();
            elevator = new GameObject("B185_1_Elevator");
            elevator.transform.position = new Vector3(-40f, 0f, 0f);
            bridge.SetElevatorPowerOrigin(elevator.transform);

            var charger = CreateFacility("charger-1", DataIds.Buildings.ChargerBasic, new Vector3(-36f, 0f, 0f));

            Assert.That(charger.PowerOnEffectInstance.IsPlaying, Is.True);
            Assert.That(started, Has.Count.EqualTo(1));
            Assert.That(batches, Is.EqualTo(new[] { 1 }));
        }

        [Test]
        public void ElevatorOriginWiring_IsQuiet()
        {
            EnableBridge();
            var charger = CreateFacility("charger-1", DataIds.Buildings.ChargerBasic, new Vector3(-36f, 0f, 0f));
            elevator = new GameObject("B185_1_Elevator");
            elevator.transform.position = new Vector3(-40f, 0f, 0f);

            bridge.SetElevatorPowerOrigin(elevator.transform);
            network.Rebuild();

            Assert.That(charger.IsInSupplyRange, Is.True);
            Assert.That(charger.PowerOnEffectInstance, Is.Null, "엘리베이터 배선은 씬 준비 단계라 연출 없음");
            Assert.That(started, Is.Empty);
            Assert.That(batches, Is.Empty);
        }

        [Test]
        public void SuppressedRestore_TurnsOnQuietly_AndStaysQuietAfterwards()
        {
            EnableBridge();
            PowerOnEffect.BeginSuppression();
            PowerFacility charger;
            try
            {
                CreateCore("core-1", Vector3.zero);
                charger = CreateFacility("charger-1", DataIds.Buildings.ChargerBasic, new Vector3(4f, 0f, 0f));
            }
            finally
            {
                PowerOnEffect.EndSuppression();
            }

            network.Rebuild();

            Assert.That(charger.IsInSupplyRange, Is.True);
            Assert.That(charger.PowerOnEffectInstance, Is.Null, "이어하기·탐사 진입 복원은 연출 없음");
            Assert.That(started, Is.Empty);
            Assert.That(batches, Is.Empty);
        }

        [Test]
        public void BridgeEnabledAfterFacilitiesExist_IsQuiet()
        {
            CreateCore("core-1", Vector3.zero);
            var charger = CreateFacility("charger-1", DataIds.Buildings.ChargerBasic, new Vector3(4f, 0f, 0f));
            Assert.That(charger.IsInSupplyRange, Is.False, "브리지가 꺼져 있으면 아직 판정 전");

            EnableBridge();
            network.Rebuild();

            Assert.That(charger.IsInSupplyRange, Is.True);
            Assert.That(charger.PowerOnEffectInstance, Is.Null, "켜질 때 맞춘 상태는 연출 없음");
            Assert.That(started, Is.Empty);
        }

        [Test]
        public void NonTargetFacility_NeverPlays()
        {
            EnableBridge();
            CreateCore("core-1", Vector3.zero);
            var portal = CreateFacility("portal-1", "building.test.not_power_effect", new Vector3(3f, 0f, 0f));

            Assert.That(portal.IsInSupplyRange, Is.False);
            Assert.That(portal.PowerOnEffectInstance, Is.Null);
            Assert.That(started, Is.Empty);
        }

        private void OnStarted(PowerFacility facility, bool isBatch) => started.Add((facility, isBatch));
        private void OnBatchStarted(int count, Vector3 center) => batches.Add(count);

        private void EnableBridge()
        {
            // EditMode에서는 OnEnable이 자동 호출되지 않는다. 실제 구독 경로를 그대로 쓴다.
            Call(bridge, "OnEnable");
            bridgeEnabled = true;
        }

        private PowerNode CreateCore(string instanceId, Vector3 position, int supply = 5)
        {
            var coreObject = new GameObject(instanceId);
            coreObject.transform.SetParent(root.transform);
            coreObject.transform.position = position;
            coreObject.AddComponent<BuildingInstance>().Initialize(instanceId, DataIds.Buildings.OutpostCoreBasic);
            var node = coreObject.AddComponent<PowerNode>();
            node.SetEntityId(instanceId);
            node.Configure(network, true, supply, 0, PowerPriority.Critical);
            network.RegisterNode(node);
            return node;
        }

        private PowerFacility CreateFacility(string instanceId, string buildingId, Vector3 position)
        {
            var facilityObject = new GameObject(instanceId);
            facilityObject.transform.SetParent(root.transform);
            facilityObject.transform.position = position;
            facilityObject.AddComponent<BuildingInstance>().Initialize(instanceId, buildingId);
            var facility = facilityObject.AddComponent<PowerFacility>();
            var node = facilityObject.GetComponent<PowerNode>();
            node.SetEntityId(instanceId);
            // 실제 설치처럼 시설이 망에 등록되는 순간의 Rebuild가 판정을 일으킨다.
            node.Configure(network, false, 0, 30, PowerPriority.Normal);
            network.RegisterNode(node);
            return facility;
        }

        private List<ConnectedFacilityStatusDto> FacilityStatuses()
        {
            var method = typeof(GameplayEventBridge).GetMethod(
                "BuildFacilityStatuses",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            return (List<ConnectedFacilityStatusDto>)method.Invoke(bridge, null);
        }

        private static void Call(object target, string methodName)
        {
            var method = target.GetType().GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.That(method, Is.Not.Null, methodName);
            method.Invoke(target, null);
        }

        private static void SetField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, fieldName);
            field.SetValue(target, value);
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using SubTerra.App.Core;
using SubTerra.App.Economy;
using SubTerra.App.Integration;
using SubTerra.App.Inventory;
using SubTerra.App.Save;
using SubTerra.App.State;
using SubTerra.Gameplay.Building;
using SubTerra.Gameplay.Snapshot;
using SubTerra.Shared;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.Tilemaps;

namespace SubTerra.App.Tests.PlayMode.MineDemo
{
    /// <summary>
    /// Phase M 최소 플레이 루프 — 실제 BuildingPlacementSystem·WorldSnapshotSystem·Economy 경로.
    /// </summary>
    public sealed class MineDemoIntegrationPlayModeTests
    {
        [UnityTest]
        public IEnumerator M_F01_RewardReceiver_DrivesInventoryAndGameState()
        {
            GameBootstrapper.ResetInstanceForTests();
            var bootGo = new GameObject("M_F01_Boot");
            var boot = bootGo.AddComponent<GameBootstrapper>();
            Assert.That(boot.Initialize(new NullCatalog(), new EmptySave(), new NoOpSceneLoader()), Is.True);

            var catalog = new InMemoryMineralCatalog();
            catalog.Register("mineral.copper", 1.5f, 10, "Copper");
            var inventory = new InventoryService(catalog, 100f, boot.State);
            IMiningRewardReceiver receiver = inventory;

            receiver.AddMineral("mineral.copper", 1);

            Assert.That(inventory.State.GetQuantity("mineral.copper"), Is.EqualTo(1));
            Assert.That(inventory.CurrentWeight, Is.EqualTo(1.5f).Within(0.0001f));
            Assert.That(boot.State.GetInventory().CargoWeight, Is.EqualTo(1.5f).Within(0.0001f));
            Assert.That(boot.State.GetInventory().UnsettledValue, Is.EqualTo(10f).Within(0.0001f));

            Object.Destroy(bootGo);
            GameBootstrapper.ResetInstanceForTests();
            yield return null;
        }

        [UnityTest]
        public IEnumerator M_F02_BuildingPlacementSystem_FailNoSpend_SuccessOnceWithWallet()
        {
            // 실제 A BuildingPlacementSystem + B EconomyService IResourceWallet 경로.
            var catalog = new InMemoryMineralCatalog();
            catalog.Register("mineral.copper", 1f, 10, "Copper");
            var state = GameState.CreateNew();
            var inventory = new InventoryService(catalog, 100f, state);
            inventory.TryAddMineral("mineral.copper", 3);
            IResourceWallet wallet = new EconomyService(inventory, catalog, state);

            var host = new GameObject("M_F02_Placement");
            var tilemapGo = new GameObject("Terrain");
            tilemapGo.transform.SetParent(host.transform);
            var terrain = tilemapGo.AddComponent<Tilemap>();
            // RequiresGround=false: 타일 지면 없이 지갑·점유만 검증
            var placement = host.AddComponent<BuildingPlacementSystem>();
            var buildingRoot = new GameObject("RuntimeBuildings");
            buildingRoot.transform.SetParent(host.transform);

            var prefab = new GameObject("SupportPrefab");
            prefab.SetActive(false);
            var definition = ScriptableObject.CreateInstance<BuildingPlacementDefinition>();
            SetField(definition, "buildingId", "building.support.basic");
            SetField(definition, "runtimePrefab", prefab);
            SetField(definition, "footprint", new Vector2Int(1, 1));
            SetField(definition, "requiresGround", false);
            SetCosts(definition, "mineral.copper", 2);

            SetField(placement, "terrainTilemap", terrain);
            SetField(placement, "buildingRoot", buildingRoot.transform);
            SetField(placement, "restoreDefinitions", new[] { definition });

            placement.SetResourceWallet(wallet);
            placement.Select(definition);

            // 비용 부족: 보유 3이지만 요구를 10으로 올려 실패 유도
            SetCosts(definition, "mineral.copper", 10);
            var failCell = new Vector3Int(0, 0, 0);
            var fail = placement.TryPlaceAt(failCell);
            Assert.That(fail.IsSuccess, Is.False);
            Assert.That(fail.Failure, Is.EqualTo(BuildingPlacementFailure.CannotAfford));
            Assert.That(inventory.State.GetQuantity("mineral.copper"), Is.EqualTo(3), "실패 시 미차감");
            Assert.That(buildingRoot.transform.childCount, Is.EqualTo(0));

            // 성공 1회
            SetCosts(definition, "mineral.copper", 2);
            placement.Select(definition);
            var ok = placement.TryPlaceAt(new Vector3Int(1, 0, 0));
            Assert.That(ok.IsSuccess, Is.True, "valid place should succeed");
            Assert.That(inventory.State.GetQuantity("mineral.copper"), Is.EqualTo(1));
            Assert.That(buildingRoot.transform.childCount, Is.EqualTo(1));

            // 같은 셀 재배치 실패(점유) — 추가 차감 없음
            placement.Select(definition);
            var again = placement.TryPlaceAt(new Vector3Int(1, 0, 0));
            Assert.That(again.IsSuccess, Is.False);
            Assert.That(inventory.State.GetQuantity("mineral.copper"), Is.EqualTo(1));

            Object.Destroy(host);
            Object.Destroy(prefab);
            Object.Destroy(definition);
            yield return null;
        }

        [UnityTest]
        public IEnumerator M_F04_WorldSnapshotSystem_CaptureRestore_RestoresBuildingWithoutDoubleReward()
        {
            // 실제 WorldSnapshotSystem + BuildingPlacementSystem.restoreDefinitions 경로.
            var catalog = new InMemoryMineralCatalog();
            catalog.Register("mineral.copper", 1f, 10, "Copper");
            var state = GameState.CreateNew();
            var inventory = new InventoryService(catalog, 100f, state);
            inventory.TryAddMineral("mineral.copper", 10);
            IResourceWallet wallet = new EconomyService(inventory, catalog, state);
            IMiningRewardReceiver rewardReceiver = inventory;

            var host = new GameObject("M_F04_Snapshot");
            host.SetActive(false);
            var tilemapGo = new GameObject("Foreground");
            tilemapGo.transform.SetParent(host.transform);
            var tilemap = tilemapGo.AddComponent<Tilemap>();
            tilemapGo.AddComponent<TilemapRenderer>();

            var buildingRoot = new GameObject("RuntimeBuildings");
            buildingRoot.transform.SetParent(host.transform);

            var prefab = new GameObject("SupportPrefab");
            prefab.SetActive(false);
            var definition = ScriptableObject.CreateInstance<BuildingPlacementDefinition>();
            SetField(definition, "buildingId", "building.support.basic");
            SetField(definition, "runtimePrefab", prefab);
            SetField(definition, "footprint", new Vector2Int(1, 1));
            SetField(definition, "requiresGround", false);
            SetCosts(definition, "mineral.copper", 1);

            var placement = host.AddComponent<BuildingPlacementSystem>();
            SetField(placement, "terrainTilemap", tilemap);
            SetField(placement, "buildingRoot", buildingRoot.transform);
            SetField(placement, "restoreDefinitions", new[] { definition });
            placement.SetResourceWallet(wallet);

            var mining = host.AddComponent<SubTerra.Gameplay.Mining.MiningSystem>();
            SetField(mining, "foregroundTilemap", tilemap);
            SetField(mining, "rewardReceiverBehaviour", null);

            var snapshot = host.AddComponent<WorldSnapshotSystem>();
            SetField(snapshot, "foregroundTilemap", tilemap);
            SetField(snapshot, "miningSystem", mining);
            SetField(snapshot, "buildingPlacementSystem", placement);
            SetField(snapshot, "worldSeed", 42L);

            host.SetActive(true);
            yield return null;
            typeof(WorldSnapshotSystem)
                .GetMethod("OnEnable", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.Invoke(snapshot, null);

            placement.Select(definition);
            var placed = placement.TryPlaceAt(new Vector3Int(2, 1, 0));
            Assert.That(placed.IsSuccess, Is.True, "place failure: " + placed.Failure);
            Assert.That(inventory.State.GetQuantity("mineral.copper"), Is.EqualTo(9));
            Assert.That(buildingRoot.transform.childCount, Is.EqualTo(1));

            // 실제 핸들러로 캡처 딕셔너리에 기록 (Scene에서는 OnEnable 구독이 동일 메서드를 연결).
            typeof(WorldSnapshotSystem)
                .GetMethod("OnBuildingPlaced", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.Invoke(snapshot, new object[] { placed });

            IWorldSnapshotProvider provider = snapshot;
            var captured = provider.CaptureSnapshot();
            Assert.That(captured, Is.Not.Null);
            Assert.That(captured.worldSeed, Is.EqualTo(42L));
            Assert.That(captured.buildings, Is.Not.Null);
            Assert.That(captured.buildings.Count, Is.EqualTo(1));
            Assert.That(captured.buildings[0].buildingTypeId, Is.EqualTo("building.support.basic"));

            // 새 런타임 상태로 복원 시뮬레이션: 건물 제거 후 Restore
            foreach (Transform child in buildingRoot.transform)
            {
                Object.Destroy(child.gameObject);
            }

            yield return null;
            Assert.That(buildingRoot.transform.childCount, Is.EqualTo(0));

            var copperBeforeRestore = inventory.State.GetQuantity("mineral.copper");
            provider.RestoreSnapshot(captured);
            yield return null;

            // 시설 복원 (restoreDefinitions로 타입 해석)
            Assert.That(buildingRoot.transform.childCount, Is.EqualTo(1), "building restored via restoreDefinitions");
            // 복원은 지갑/보상을 건드리지 않음
            Assert.That(inventory.State.GetQuantity("mineral.copper"), Is.EqualTo(copperBeforeRestore));

            // 중복 복원: 같은 instanceId는 재생성하지 않음
            provider.RestoreSnapshot(captured);
            yield return null;
            Assert.That(buildingRoot.transform.childCount, Is.EqualTo(1), "no duplicate building on second restore");
            Assert.That(inventory.State.GetQuantity("mineral.copper"), Is.EqualTo(copperBeforeRestore));

            // 보상 경로와 분리: AddMineral은 명시 호출 시에만
            rewardReceiver.AddMineral("mineral.copper", 1);
            Assert.That(inventory.State.GetQuantity("mineral.copper"), Is.EqualTo(copperBeforeRestore + 1));

            Object.Destroy(host);
            Object.Destroy(prefab);
            Object.Destroy(definition);
            yield return null;
        }

        [UnityTest]
        public IEnumerator M_ActivationGate_RefusesUiUntilOrderComplete_AndSaveReadyUnlocks()
        {
            // 이전 플레이 잔여 전역 인스턴스가 Start 코루틴을 오염시키지 않게 정리한다.
            GameBootstrapper.ResetInstanceForTests();
            if (SaveRuntimeController.Instance != null)
            {
                Object.Destroy(SaveRuntimeController.Instance.gameObject);
            }

            yield return null;

            var gate = new IntegrationActivationGate();
            Assert.That(gate.TryActivateUi(), Is.False);

            gate.MarkStateReady();
            Assert.That(gate.TryActivateUi(), Is.False);
            gate.MarkWorldRestored();
            Assert.That(gate.TryActivateUi(), Is.False);
            gate.MarkDerivedRecalculated();
            Assert.That(gate.TryActivateUi(), Is.True);
            Assert.That(gate.TryActivateUi(), Is.False);

            // Bootstrap/SaveRuntime 없이 binder Start는 경고 후 종료한다.
            LogAssert.Expect(
                LogType.Warning,
                new System.Text.RegularExpressions.Regex("Integration scene opened without the Bootstrap runtime"));

            var host = new GameObject("M_Gate_BinderHost");
            var binder = host.AddComponent<IntegrationRuntimeBinder>();
            // Awake + Start 한 프레임
            yield return null;
            yield return null;

            Assert.That(binder is IIntegrationRestoreListener, Is.True);
            Assert.That(binder.ActivationGate, Is.Not.Null);
            var listener = (IIntegrationRestoreListener)binder;

            // 순서 미완료 시 ActivateUi 거부
            Assert.That(binder.ActivateUi(), Is.False);
            Assert.That(binder.IsUiActivated, Is.False);

            listener.NotifyWorldRestored();
            Assert.That(binder.ActivationGate.IsWorldRestored, Is.False, "state 없이 world 무시");

            binder.ActivationGate.MarkStateReady();
            listener.NotifyWorldRestored();
            Assert.That(binder.ActivationGate.IsWorldRestored, Is.True);
            Assert.That(binder.ActivateUi(), Is.False, "derived 전 UI 거부");

            listener.NotifyDerivedRecalculated();
            Assert.That(binder.ActivationGate.CanActivateUi, Is.True);
            // 게이트가 채워지면 ActivateUi 허용 (강제 MarkReady 우회가 아님)
            Assert.That(binder.ActivateUi(), Is.True);
            Assert.That(binder.IsUiActivated, Is.True);

            Object.Destroy(host);
            GameBootstrapper.ResetInstanceForTests();
            yield return null;
        }

        [UnityTest]
        public IEnumerator M_Activation_SurvivesMissingOptionalPanelBinders_AndRestoresInput()
        {
            // 씬 재생성으로 inventory/outpost 패널 직렬 참조가 비어도
            // HUD·deferred input 활성은 동일하게 성공해야 한다 (작업자 환경 차이 방지).
            GameBootstrapper.ResetInstanceForTests();
            if (SaveRuntimeController.Instance != null)
            {
                Object.Destroy(SaveRuntimeController.Instance.gameObject);
            }

            yield return null;

            var bootGo = new GameObject("M_Env_Boot");
            var boot = bootGo.AddComponent<GameBootstrapper>();
            Assert.That(
                boot.Initialize(new NullCatalog(), new EmptySave(), new NoOpSceneLoader()),
                Is.True);

            var runtimeGo = new GameObject("M_Env_SaveRuntime");
            var runtime = runtimeGo.AddComponent<SaveRuntimeController>();
            yield return null;
            runtime.SetReady(true);

            var playerGo = new GameObject("M_Env_Player");
            var movement = playerGo.AddComponent<SubTerra.Gameplay.Player.PlayerMovement>();
            movement.enabled = true;

            var hudGo = new GameObject("HUDCanvas");
            var canvasGroup = hudGo.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 1f;

            // inactive로 추가해 Awake/Start 타이밍을 테스트가 제어한다.
            var host = new GameObject("M_Env_BinderHost");
            host.SetActive(false);
            var binder = host.AddComponent<IntegrationRuntimeBinder>();
            // 의도적으로 optional 패널 참조를 비운다 (stale/missing 직렬화 재현).
            SetField(binder, "inventoryPanelBinder", null);
            SetField(binder, "outpostPanelBinder", null);
            SetField(binder, "progressionPanelBinder", null);
            SetField(binder, "hudCanvasGroup", canvasGroup);
            SetField(binder, "deferredInputBehaviours", new Behaviour[] { movement });
            SetField(binder, "playerMovement", movement);
            SetField(binder, "runtime", runtime);
            SetField(binder, "bootstrap", boot);

            host.SetActive(true);
            // Awake: HUD/입력 비활성
            yield return null;
            // Start 코루틴 진행 (IsUiReady 이미 true)
            yield return null;
            yield return null;

            Assert.That(binder.AreContractsWired, Is.True, "WireContracts must complete without optional panels");
            Assert.That(binder.IsUiActivated, Is.True, "UI must activate with IsUiReady");
            Assert.That(canvasGroup.alpha, Is.EqualTo(1f).Within(0.001f), "HUD must become visible");
            Assert.That(movement.enabled, Is.True, "deferred movement must re-enable");

            // 수동 경로: 참조 재탐색이 비어 있지 않은지 확인.
            binder.ResolveSceneReferences();
            Assert.That(
                GetField<CanvasGroup>(binder, "hudCanvasGroup"),
                Is.Not.Null,
                "HUD CanvasGroup must resolve for consistent visibility");
            Assert.That(
                GetField<Behaviour[]>(binder, "deferredInputBehaviours"),
                Is.Not.Null.And.Not.Empty,
                "deferred inputs must resolve for consistent movement/shortcuts");

            Object.Destroy(host);
            Object.Destroy(hudGo);
            Object.Destroy(playerGo);
            Object.Destroy(runtimeGo);
            Object.Destroy(bootGo);
            GameBootstrapper.ResetInstanceForTests();
            yield return null;
        }

        private sealed class NoOpSceneLoader : ISceneLoader
        {
            public bool Load(string sceneName) => true;
        }

        private static void SetField(object target, string name, object value)
        {
            var field = target.GetType().GetField(
                name,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, "Missing field: " + name);
            field.SetValue(target, value);
        }

        private static T GetField<T>(object target, string name)
        {
            var field = target.GetType().GetField(
                name,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, "Missing field: " + name);
            return (T)field.GetValue(target);
        }

        private static void SetCosts(BuildingPlacementDefinition definition, string itemId, int quantity)
        {
            var costEntryType = typeof(BuildingPlacementDefinition)
                .GetNestedType("CostEntry", BindingFlags.NonPublic);
            Assert.That(costEntryType, Is.Not.Null);
            var entry = System.Activator.CreateInstance(costEntryType);
            costEntryType.GetField("itemId").SetValue(entry, itemId);
            costEntryType.GetField("quantity").SetValue(entry, quantity);

            var listType = typeof(List<>).MakeGenericType(costEntryType);
            var list = System.Activator.CreateInstance(listType);
            listType.GetMethod("Add").Invoke(list, new[] { entry });
            SetField(definition, "costs", list);
        }
    }
}

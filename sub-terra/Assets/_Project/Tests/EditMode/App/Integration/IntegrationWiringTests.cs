using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using SubTerra.App.Core;
using SubTerra.App.Economy;
using SubTerra.App.Integration;
using SubTerra.App.Inventory;
using SubTerra.App.State;
using SubTerra.Gameplay.Building;
using SubTerra.Gameplay.Snapshot;
using SubTerra.Shared;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace SubTerra.App.Tests.Integration
{
    /// <summary>M-S01~S05 및 Shared 5경계·복원 게이트 단위/정적 검증.</summary>
    public sealed class IntegrationWiringTests
    {
        private const string IntegrationPath =
            "Assets/_Project/Scenes/App/Mine_Demo_Integration.unity";

        [Test]
        public void M_S01_IntegrationScene_HasCanonicalHierarchy()
        {
            var scene = EditorSceneManager.OpenScene(IntegrationPath, OpenSceneMode.Additive);
            try
            {
                Assert.That(FindRoot(scene, "GameplayRoot"), Is.Not.Null);
                Assert.That(FindRoot(scene, "ApplicationRoot"), Is.Not.Null);
                Assert.That(FindRoot(scene, "HUDCanvas"), Is.Not.Null);
                Assert.That(FindInScene<EventSystem>(scene), Is.Not.Null);

                var grid = FindInSceneByName(scene, "Grid");
                Assert.That(grid, Is.Not.Null);

                var tilemapNames = new HashSet<string>();
                foreach (var tm in grid.GetComponentsInChildren<Tilemap>(true))
                {
                    tilemapNames.Add(tm.gameObject.name);
                }

                Assert.That(tilemapNames, Does.Contain("BackgroundTilemap"));
                Assert.That(tilemapNames, Does.Contain("ForegroundTilemap"));
                Assert.That(tilemapNames, Does.Contain("HazardTilemap"));
                Assert.That(tilemapNames, Does.Contain("BuildingTilemap"));
                Assert.That(FindInSceneByName(scene, "RuntimeBuildings"), Is.Not.Null);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void M_S02_IntegrationScene_WiresFiveSharedBoundaries()
        {
            var scene = EditorSceneManager.OpenScene(IntegrationPath, OpenSceneMode.Additive);
            try
            {
                var binder = FindInScene<IntegrationRuntimeBinder>(scene);
                Assert.That(binder, Is.Not.Null);

                var so = new SerializedObject(binder);
                Assert.That(
                    so.FindProperty("buildingPlacementSystem").objectReferenceValue,
                    Is.Not.Null,
                    "IResourceWallet target BuildingPlacementSystem");
                Assert.That(
                    so.FindProperty("hudBinder").objectReferenceValue,
                    Is.Not.Null);
                Assert.That(
                    so.FindProperty("worldSnapshotProviderBehaviour").objectReferenceValue,
                    Is.Not.Null,
                    "IWorldSnapshotProvider");
                Assert.That(
                    so.FindProperty("droneSensor").objectReferenceValue
                    ?? so.FindProperty("droneContextAdapter").objectReferenceValue,
                    Is.Not.Null,
                    "IDroneContextProvider");

                // Mining reward + event sink: A producers point at binder (IMiningRewardReceiver / IGameplayEventSink)
                var mining = FindComponentByTypeName(scene, "SubTerra.Gameplay.Mining.MiningSystem");
                Assert.That(mining, Is.Not.Null);
                var miningSo = new SerializedObject(mining);
                Assert.That(
                    miningSo.FindProperty("rewardReceiverBehaviour").objectReferenceValue,
                    Is.EqualTo(binder));

                var bridge = FindComponentByTypeName(
                    scene,
                    "SubTerra.Gameplay.Integration.GameplayEventBridge");
                Assert.That(bridge, Is.Not.Null);
                var bridgeSo = new SerializedObject(bridge);
                Assert.That(
                    bridgeSo.FindProperty("eventSinkBehaviour").objectReferenceValue,
                    Is.EqualTo(binder));

                Assert.That(binder is IMiningRewardReceiver, Is.True);
                Assert.That(binder is IGameplayEventSink, Is.True);

                // 건설 배치 경로·복원 정의
                var placement = FindInScene<BuildingPlacementSystem>(scene);
                Assert.That(placement, Is.Not.Null);
                var placementSo = new SerializedObject(placement);
                var restoreDefs = placementSo.FindProperty("restoreDefinitions");
                Assert.That(restoreDefs, Is.Not.Null);
                Assert.That(restoreDefs.arraySize, Is.GreaterThan(0), "restoreDefinitions must be populated for continue restore");

                Assert.That(
                    so.FindProperty("buildingUiBinder").objectReferenceValue,
                    Is.Not.Null,
                    "BuildingUiIntegrationBinder must be wired");
                Assert.That(
                    so.FindProperty("placementBridge").objectReferenceValue
                    ?? FindInScene<GameplayBuildingPlacementBridge>(scene),
                    Is.Not.Null,
                    "GameplayBuildingPlacementBridge required for construction path");
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void M_S04_EventSystem_IsSingleInIntegrationScene()
        {
            var scene = EditorSceneManager.OpenScene(IntegrationPath, OpenSceneMode.Additive);
            try
            {
                var count = 0;
                foreach (var root in scene.GetRootGameObjects())
                {
                    count += root.GetComponentsInChildren<EventSystem>(true).Length;
                }

                Assert.That(count, Is.EqualTo(1));
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void M_S05_IntegrationScene_HasNoMissingScripts()
        {
            var scene = EditorSceneManager.OpenScene(IntegrationPath, OpenSceneMode.Additive);
            try
            {
                var missing = 0;
                foreach (var root in scene.GetRootGameObjects())
                {
                    foreach (var mb in root.GetComponentsInChildren<Transform>(true))
                    {
                        foreach (var c in mb.GetComponents<Component>())
                        {
                            if (c == null)
                            {
                                missing++;
                            }
                        }
                    }
                }

                Assert.That(missing, Is.EqualTo(0));
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void M_ContractRegistry_ReportsFiveBoundariesWhenBound()
        {
            var registry = new IntegrationContractRegistry();
            Assert.That(registry.AreAllConnected, Is.False);

            var catalog = new InMemoryMineralCatalog();
            catalog.Register("mineral.copper", 1f, 10, "Copper");
            var state = GameState.CreateNew();
            var inventory = new InventoryService(catalog, 50f, state);
            var economy = new EconomyService(inventory, catalog, state);
            var sink = new RecordingSink();
            var world = new FakeWorldSnapshot();
            var drone = new FakeDroneProvider();

            registry.Bind(inventory, economy, sink, world, drone);

            Assert.That(registry.ConnectedBoundaryCount, Is.EqualTo(5));
            Assert.That(registry.AreAllConnected, Is.True);
            Assert.That(registry.MiningRewardReceiver, Is.SameAs(inventory));
            Assert.That(registry.ResourceWallet, Is.SameAs(economy));
            Assert.That(registry.GameplayEventSink, Is.SameAs(sink));
            Assert.That(registry.WorldSnapshotProvider, Is.SameAs(world));
            Assert.That(registry.DroneContextProvider, Is.SameAs(drone));
        }

        [Test]
        public void M_EventFanOut_DeliversToAllSinks()
        {
            var a = new RecordingSink();
            var b = new RecordingSink();
            var fanOut = new IntegrationEventFanOut(a, b);

            fanOut.Publish(new GameplayEventDto
            {
                type = GameplayEventType.TileMined,
                reasonId = "mineral.copper",
                quantity = 1
            });

            Assert.That(fanOut.PublishCount, Is.EqualTo(1));
            Assert.That(a.Events.Count, Is.EqualTo(1));
            Assert.That(b.Events.Count, Is.EqualTo(1));
            Assert.That(a.Events[0].reasonId, Is.EqualTo("mineral.copper"));
        }

        [Test]
        public void M_ActivationGate_EnforcesRestoreOrderBeforeUi()
        {
            var gate = new IntegrationActivationGate();
            Assert.That(gate.TryActivateUi(), Is.False);

            gate.MarkWorldRestored();
            Assert.That(gate.IsWorldRestored, Is.False, "World restore requires state first");

            gate.MarkStateReady();
            gate.MarkWorldRestored();
            Assert.That(gate.TryActivateUi(), Is.False, "Derived recalculation still required");

            gate.MarkDerivedRecalculated();
            Assert.That(gate.CanActivateUi, Is.True);
            Assert.That(gate.TryActivateUi(), Is.True);
            Assert.That(gate.IsUiActivated, Is.True);
            Assert.That(gate.TryActivateUi(), Is.False, "Second activate is rejected");
        }

        [Test]
        public void M_F01_MiningRewardReceiver_UpdatesInventoryWeightAndValue()
        {
            var catalog = new InMemoryMineralCatalog();
            catalog.Register("mineral.copper", 1.5f, 10, "Copper");
            var state = GameState.CreateNew();
            var inventory = new InventoryService(catalog, 100f, state);

            IMiningRewardReceiver receiver = inventory;
            receiver.AddMineral("mineral.copper", 2);

            Assert.That(inventory.State.GetQuantity("mineral.copper"), Is.EqualTo(2));
            Assert.That(inventory.CurrentWeight, Is.EqualTo(3f).Within(0.0001f));
            Assert.That(state.GetInventory().CargoWeight, Is.EqualTo(3f).Within(0.0001f));
            Assert.That(state.GetInventory().UnsettledValue, Is.EqualTo(20));
        }

        [Test]
        public void M_F02_BuildingPlacementSystem_WithSetResourceWallet_FailNoSpend_SuccessOnce()
        {
            // 실제 BuildingPlacementSystem + EconomyService 지갑 경로 (통합 배선과 동일).
            var catalog = new InMemoryMineralCatalog();
            catalog.Register("mineral.copper", 1f, 10, "Copper");
            var state = GameState.CreateNew();
            var inventory = new InventoryService(catalog, 100f, state);
            inventory.TryAddMineral("mineral.copper", 3);
            IResourceWallet wallet = new EconomyService(inventory, catalog, state);

            var host = new GameObject("M_F02_EditPlacement");
            var tilemapGo = new GameObject("Terrain");
            tilemapGo.transform.SetParent(host.transform);
            var terrain = tilemapGo.AddComponent<Tilemap>();
            var buildingRoot = new GameObject("RuntimeBuildings");
            buildingRoot.transform.SetParent(host.transform);

            var prefab = new GameObject("SupportPrefab");
            var definition = ScriptableObject.CreateInstance<BuildingPlacementDefinition>();
            SetPrivateField(definition, "buildingId", "building.support.basic");
            SetPrivateField(definition, "runtimePrefab", prefab);
            SetPrivateField(definition, "footprint", new Vector2Int(1, 1));
            SetPrivateField(definition, "requiresGround", false);
            SetDefinitionCosts(definition, "mineral.copper", 10);

            var placement = host.AddComponent<BuildingPlacementSystem>();
            SetPrivateField(placement, "terrainTilemap", terrain);
            SetPrivateField(placement, "buildingRoot", buildingRoot.transform);
            SetPrivateField(placement, "restoreDefinitions", new[] { definition });
            placement.SetResourceWallet(wallet);
            placement.Select(definition);

            var fail = placement.TryPlaceAt(new Vector3Int(0, 0, 0));
            Assert.That(fail.IsSuccess, Is.False);
            Assert.That(fail.Failure, Is.EqualTo(BuildingPlacementFailure.CannotAfford));
            Assert.That(inventory.State.GetQuantity("mineral.copper"), Is.EqualTo(3));
            Assert.That(buildingRoot.transform.childCount, Is.EqualTo(0));

            SetDefinitionCosts(definition, "mineral.copper", 2);
            placement.Select(definition);
            var ok = placement.TryPlaceAt(new Vector3Int(1, 0, 0));
            Assert.That(ok.IsSuccess, Is.True);
            Assert.That(inventory.State.GetQuantity("mineral.copper"), Is.EqualTo(1));
            Assert.That(buildingRoot.transform.childCount, Is.EqualTo(1));

            Object.DestroyImmediate(host);
            Object.DestroyImmediate(prefab);
            Object.DestroyImmediate(definition);
        }

        [Test]
        public void M_F04_WorldSnapshotSystem_CaptureRestore_UsesRestoreDefinitions_NoDoubleBuilding()
        {
            var catalog = new InMemoryMineralCatalog();
            catalog.Register("mineral.copper", 1f, 10, "Copper");
            var state = GameState.CreateNew();
            var inventory = new InventoryService(catalog, 100f, state);
            inventory.TryAddMineral("mineral.copper", 5);
            IResourceWallet wallet = new EconomyService(inventory, catalog, state);

            // host 비활성 상태에서 컴포넌트·참조를 채운 뒤 활성화해 OnEnable 구독이 유효 참조를 잡게 한다.
            var host = new GameObject("M_F04_EditSnapshot");
            host.SetActive(false);
            var tilemapGo = new GameObject("Foreground");
            tilemapGo.transform.SetParent(host.transform);
            var tilemap = tilemapGo.AddComponent<Tilemap>();
            var buildingRoot = new GameObject("RuntimeBuildings");
            buildingRoot.transform.SetParent(host.transform);

            var prefab = new GameObject("SupportPrefab");
            var definition = ScriptableObject.CreateInstance<BuildingPlacementDefinition>();
            SetPrivateField(definition, "buildingId", "building.support.basic");
            SetPrivateField(definition, "runtimePrefab", prefab);
            SetPrivateField(definition, "footprint", new Vector2Int(1, 1));
            SetPrivateField(definition, "requiresGround", false);
            SetDefinitionCosts(definition, "mineral.copper", 1);

            var placement = host.AddComponent<BuildingPlacementSystem>();
            SetPrivateField(placement, "terrainTilemap", tilemap);
            SetPrivateField(placement, "buildingRoot", buildingRoot.transform);
            SetPrivateField(placement, "restoreDefinitions", new[] { definition });
            placement.SetResourceWallet(wallet);

            var snapshot = host.AddComponent<WorldSnapshotSystem>();
            SetPrivateField(snapshot, "foregroundTilemap", tilemap);
            SetPrivateField(snapshot, "buildingPlacementSystem", placement);
            SetPrivateField(snapshot, "worldSeed", 7L);

            host.SetActive(true);
            // 비활성 중 주입한 참조로 이벤트 구독을 다시 건다 (실제 Scene에서는 Inspector 직렬화 후 OnEnable).
            InvokePrivate(snapshot, "OnEnable");

            var wired = GetPrivateField(snapshot, "buildingPlacementSystem");
            Assert.That(wired, Is.SameAs(placement), "snapshot must reference placement for capture");

            var eventHits = 0;
            placement.BuildingPlaced += _ => eventHits++;

            placement.Select(definition);
            var placeResult = placement.TryPlaceAt(new Vector3Int(3, 0, 0));
            Assert.That(placeResult.IsSuccess, Is.True, "place failure: " + placeResult.Failure);
            Assert.That(eventHits, Is.GreaterThan(0), "BuildingPlaced must fire on success");
            Assert.That(inventory.State.GetQuantity("mineral.copper"), Is.EqualTo(4));
            Assert.That(buildingRoot.transform.childCount, Is.EqualTo(1));

            // 이벤트 구독이 EditMode에서 누락돼도 동일 핸들러 경로를 실제 메서드로 구동한다.
            InvokePrivate(snapshot, "OnBuildingPlaced", placeResult);

            IWorldSnapshotProvider provider = snapshot;
            var captured = provider.CaptureSnapshot();
            Assert.That(captured.buildings, Is.Not.Null);
            Assert.That(
                captured.buildings.Count,
                Is.EqualTo(1),
                "WorldSnapshotSystem must record building for capture");
            Assert.That(captured.buildings[0].buildingTypeId, Is.EqualTo("building.support.basic"));

            while (buildingRoot.transform.childCount > 0)
            {
                Object.DestroyImmediate(buildingRoot.transform.GetChild(0).gameObject);
            }

            var copper = inventory.State.GetQuantity("mineral.copper");
            provider.RestoreSnapshot(captured);
            Assert.That(buildingRoot.transform.childCount, Is.EqualTo(1), "restoreDefinitions enables facility restore");
            Assert.That(inventory.State.GetQuantity("mineral.copper"), Is.EqualTo(copper), "restore must not re-spend");

            provider.RestoreSnapshot(captured);
            Assert.That(buildingRoot.transform.childCount, Is.EqualTo(1), "second restore must not duplicate");

            Object.DestroyImmediate(host);
            Object.DestroyImmediate(prefab);
            Object.DestroyImmediate(definition);
        }

        private static GameObject FindRoot(Scene scene, string name)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name == name)
                {
                    return root;
                }
            }

            return null;
        }

        private static T FindInScene<T>(Scene scene) where T : Component
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                var component = root.GetComponentInChildren<T>(true);
                if (component != null)
                {
                    return component;
                }
            }

            return null;
        }

        private static GameObject FindInSceneByName(Scene scene, string name)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name == name)
                    {
                        return t.gameObject;
                    }
                }
            }

            return null;
        }

        private static MonoBehaviour FindComponentByTypeName(Scene scene, string fullTypeName)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var mb in root.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (mb != null && mb.GetType().FullName == fullTypeName)
                    {
                        return mb;
                    }
                }
            }

            return null;
        }

        private sealed class RecordingSink : IGameplayEventSink
        {
            public List<GameplayEventDto> Events { get; } = new List<GameplayEventDto>();

            public void Publish(GameplayEventDto gameplayEvent)
            {
                if (gameplayEvent != null)
                {
                    Events.Add(gameplayEvent);
                }
            }
        }

        private sealed class FakeWorldSnapshot : IWorldSnapshotProvider
        {
            public WorldSnapshotDto CaptureSnapshot() => new WorldSnapshotDto();
            public void RestoreSnapshot(WorldSnapshotDto snapshot) { }
        }

        private sealed class FakeDroneProvider : IDroneContextProvider
        {
            public DroneContextDto CreateContext()
            {
                return new DroneContextDto
                {
                    depth = 0,
                    currentEnergy = 100,
                    returnPathAvailable = true
                };
            }
        }

        private static void SetPrivateField(object target, string name, object value)
        {
            var field = target.GetType().GetField(
                name,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, "Missing field: " + name);
            field.SetValue(target, value);
        }

        private static object GetPrivateField(object target, string name)
        {
            var field = target.GetType().GetField(
                name,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, "Missing field: " + name);
            return field.GetValue(target);
        }

        private static void InvokePrivate(object target, string methodName, params object[] args)
        {
            var method = target.GetType().GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.That(method, Is.Not.Null, "Missing method: " + methodName);
            method.Invoke(target, args.Length == 0 ? null : args);
        }

        private static void SetDefinitionCosts(
            BuildingPlacementDefinition definition,
            string itemId,
            int quantity)
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
            SetPrivateField(definition, "costs", list);
        }
    }
}

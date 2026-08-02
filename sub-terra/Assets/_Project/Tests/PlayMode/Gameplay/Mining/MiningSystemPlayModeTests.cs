using System.Collections;
using NUnit.Framework;
using SubTerra.Gameplay.Mining;
using SubTerra.Gameplay.Player;
using SubTerra.Shared;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using UnityEngine.Tilemaps;

namespace SubTerra.Gameplay.Mining.Tests
{
    public sealed class MiningSystemPlayModeTests
    {
        private sealed class RewardReceiver : MonoBehaviour, IMiningRewardReceiver
        {
            public int Calls; public string MineralId; public int Quantity;
            public void AddMineral(string mineralId, int quantity) { Calls++; MineralId = mineralId; Quantity = quantity; }
        }

        private sealed class MiningTransaction : MonoBehaviour, IMiningTransaction
        {
            public int Energy = 100;
            public int CommitCalls;
            public MiningCommitStatus CommitStatus = MiningCommitStatus.Success;

            public bool CanAffordEnergy(int energyCost) => Energy >= energyCost;

            public MiningCommitResult TryCommitMining(string mineralId, int quantity, int energyCost)
            {
                CommitCalls++;
                if (CommitStatus != MiningCommitStatus.Success)
                {
                    return new MiningCommitResult(CommitStatus);
                }

                Energy -= energyCost;
                return MiningCommitResult.Success();
            }
        }

        private sealed class UpgradeEffects : MonoBehaviour, IUpgradeEffectProvider
        {
            public int DrillLevel;
            public float DrillSpeed = 1f;
            public float EnergyEfficiency = 1f;

            public int GetDrillLevel() => DrillLevel;
            public float GetDrillSpeedMultiplier() => DrillSpeed;
            public float GetEnergyEfficiencyMultiplier() => EnergyEfficiency;
            public int GetMaximumEnergy(int baseMaximum) => baseMaximum;
            public float GetMaximumCargoWeight(float baseMaximum) => baseMaximum;
            public float GetDroneScanRadius(float baseRadius) => baseRadius;
            public float GetDroneRescuePreservation(float basePreservation) => basePreservation;
            public float GetGasResistance() => 0f;
        }

        [Test]
        public void CompletionRemovesTileAndPaysRewardOnlyOnce()
        {
            GameObject root = new("MiningTest");
            GameObject gridObject = new("Grid"); gridObject.transform.SetParent(root.transform); gridObject.AddComponent<Grid>();
            GameObject tilemapObject = new("Tilemap"); tilemapObject.transform.SetParent(gridObject.transform);
            Tilemap tilemap = tilemapObject.AddComponent<Tilemap>();
            MiningTileResolver resolver = root.AddComponent<MiningTileResolver>();
            RewardReceiver receiver = root.AddComponent<RewardReceiver>();
            MiningSystem system = root.AddComponent<MiningSystem>();

            SetPrivate(system, "foregroundTilemap", tilemap);
            SetPrivate(system, "tileResolver", resolver);
            SetPrivate(system, "rewardReceiverBehaviour", receiver);
            Tile tile = ScriptableObject.CreateInstance<Tile>();
            resolver.RegisterRuntime(tile, new MiningTileDto("tile.copper", "mineral.copper", 2, true, 1f, 0.2f, 0f, false));
            Vector3Int cell = new(1, 2, 0); tilemap.SetTile(cell, tile);

            Assert.IsTrue(system.TryStartMining(cell));
            system.TickMining(0.2f);
            system.TickMining(1f);

            Assert.IsNull(tilemap.GetTile(cell));
            Assert.AreEqual(1, receiver.Calls);
            Assert.AreEqual("mineral.copper", receiver.MineralId);
            Assert.AreEqual(2, receiver.Quantity);
            Assert.AreEqual(0, system.SpawnedResourceDropCount);
            Object.DestroyImmediate(root); Object.DestroyImmediate(tile);
        }

#if UNITY_EDITOR
        [Test]
        public void ResolverRebuildsSerializedEntriesWhenRuntimeCacheIsLost()
        {
            var root = new GameObject("ResolverCacheTest");
            var resolver = root.AddComponent<MiningTileResolver>();
            var tile = ScriptableObject.CreateInstance<Tile>();
            var definition = new MiningTileDto(
                "tile.rock.normal", string.Empty, 0, true, 1f, 0.35f, 0.05f, false);
            resolver.EditorSetEntries(new TileBase[] { tile }, new[] { definition });

            Assert.IsTrue(resolver.TryResolve(tile, out _));
            var lookup = (System.Collections.IDictionary)GetPrivate(resolver, "lookup");
            lookup.Clear();
            SetPrivate(resolver, "initialized", true);

            Assert.IsTrue(resolver.TryResolve(tile, out var restored));
            Assert.AreEqual("tile.rock.normal", restored.tileId);

            Object.DestroyImmediate(root);
            Object.DestroyImmediate(tile);
        }
#endif

        [Test]
        public void E_F01_TileDurationDrillLevelAndUpgradeEffects_AreApplied()
        {
            CreateSystem(out var root, out var tilemap, out var resolver, out var system);
            var transaction = root.AddComponent<MiningTransaction>();
            var upgrades = root.AddComponent<UpgradeEffects>();
            system.SetRuntimeServices(transaction, upgrades);
            var tile = ScriptableObject.CreateInstance<Tile>();
            resolver.RegisterRuntime(tile, new MiningTileDto(
                "tile.iron", "mineral.iron", 1, true, 1f, 2f, 0f, false, 2, 10));
            var cell = new Vector3Int(1, 0, 0);
            tilemap.SetTile(cell, tile);

            upgrades.DrillLevel = 1;
            Assert.IsFalse(system.TryStartMining(cell));
            Assert.AreEqual(MiningFailureReason.DrillLevelTooLow, system.LastFailure);

            upgrades.DrillLevel = 2;
            upgrades.DrillSpeed = 2f;
            upgrades.EnergyEfficiency = 2f;
            Assert.IsTrue(system.TryStartMining(cell));
            Assert.AreEqual(1f, system.EffectiveDuration, 0.0001f);
            Assert.AreEqual(5, system.RequiredEnergy);
            system.TickMining(0.99f);
            Assert.IsNotNull(tilemap.GetTile(cell));
            system.TickMining(0.01f);
            Assert.IsNull(tilemap.GetTile(cell));
            Assert.AreEqual(95, transaction.Energy);

            Object.DestroyImmediate(root);
            Object.DestroyImmediate(tile);
        }

        [Test]
        public void E_F02_CancelAndMidProgressEnergyLoss_PreserveTileAndChargeNothing()
        {
            CreateSystem(out var root, out var tilemap, out var resolver, out var system);
            var transaction = root.AddComponent<MiningTransaction>();
            system.SetRuntimeServices(transaction, null);
            var tile = ScriptableObject.CreateInstance<Tile>();
            resolver.RegisterRuntime(tile, new MiningTileDto(
                "tile.copper", "mineral.copper", 1, true, 1f, 1f, 0f, false, 0, 5));
            var cell = Vector3Int.zero;
            tilemap.SetTile(cell, tile);

            Assert.IsTrue(system.TryStartMining(cell));
            system.TickMining(0.4f);
            system.CancelMining();
            Assert.IsNotNull(tilemap.GetTile(cell));
            Assert.AreEqual(100, transaction.Energy);
            Assert.AreEqual(0, transaction.CommitCalls);

            Assert.IsTrue(system.TryStartMining(cell));
            transaction.Energy = 4;
            system.TickMining(0.1f);
            Assert.AreEqual(MiningFailureReason.InsufficientEnergy, system.LastFailure);
            Assert.IsNotNull(tilemap.GetTile(cell));
            Assert.AreEqual(0, transaction.CommitCalls);

            Object.DestroyImmediate(root);
            Object.DestroyImmediate(tile);
        }

        [Test]
        public void E_F03_RepeatedCompletion_CommitsAndRemovesExactlyOnce()
        {
            CreateSystem(out var root, out var tilemap, out var resolver, out var system);
            var transaction = root.AddComponent<MiningTransaction>();
            system.SetRuntimeServices(transaction, null);
            var tile = ScriptableObject.CreateInstance<Tile>();
            resolver.RegisterRuntime(tile, new MiningTileDto(
                "tile.copper", "mineral.copper", 1, true, 1f, 0.1f, 0f, false, 0, 2));
            var cell = Vector3Int.zero;
            tilemap.SetTile(cell, tile);

            Assert.IsTrue(system.TryMineInstant(cell));
            Assert.IsFalse(system.TryMineInstant(cell));
            Assert.AreEqual(1, transaction.CommitCalls);
            Assert.AreEqual(98, transaction.Energy);
            Assert.IsNull(tilemap.GetTile(cell));
            Assert.AreEqual(0, system.SpawnedResourceDropCount);

            Object.DestroyImmediate(root);
            Object.DestroyImmediate(tile);
        }

        [Test]
        public void E_F05_FullInventoryResult_PreservesTileAndEnergy()
        {
            CreateSystem(out var root, out var tilemap, out var resolver, out var system);
            var transaction = root.AddComponent<MiningTransaction>();
            transaction.CommitStatus = MiningCommitStatus.InventoryFull;
            system.SetRuntimeServices(transaction, null);
            var tile = ScriptableObject.CreateInstance<Tile>();
            resolver.RegisterRuntime(tile, new MiningTileDto(
                "tile.lithium", "mineral.lithium", 1, true, 1f, 0.1f, 0f, false, 0, 3));
            var cell = Vector3Int.zero;
            tilemap.SetTile(cell, tile);

            Assert.IsFalse(system.TryMineInstant(cell));
            Assert.AreEqual(MiningFailureReason.InventoryFull, system.LastFailure);
            Assert.IsNotNull(tilemap.GetTile(cell));
            Assert.AreEqual(100, transaction.Energy);
            Assert.AreEqual(1, transaction.CommitCalls);

            Object.DestroyImmediate(root);
            Object.DestroyImmediate(tile);
        }

        [UnityTest]
        public IEnumerator E_F03_KeyboardAndMouse_UseTheSameTimedCompletionPath()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            var mouse = InputSystem.AddDevice<Mouse>();
            var cameraObject = new GameObject("MiningInputCamera");
            var camera = cameraObject.AddComponent<Camera>();
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
            camera.orthographic = true;

            CreateSystem(out var root, out var tilemap, out var resolver, out var system);
            var player = new GameObject("MiningInputPlayer");
            player.SetActive(false);
            player.AddComponent<Rigidbody2D>().gravityScale = 0f;
            var movement = player.AddComponent<PlayerMovement>();
            var controller = player.AddComponent<PlayerMiningController>();
            var input = ScriptableObject.CreateInstance<InputActionAsset>();
            var map = input.AddActionMap("Player");
            var action = map.AddAction("Attack", InputActionType.Button);
            action.AddBinding("<Keyboard>/enter");
            action.AddBinding("<Mouse>/leftButton");
            SetPrivate(controller, "miningSystem", system);
            SetPrivate(controller, "inputActions", input);
            SetPrivate(controller, "reach", 1.35f);

            var tile = ScriptableObject.CreateInstance<Tile>();
            resolver.RegisterRuntime(tile, new MiningTileDto(
                "tile.copper", "mineral.copper", 1, true, 1f, 0.25f, 0f, false));
            var cell = tilemap.WorldToCell(new Vector3(1.35f, -0.5f, 0f));
            var completions = 0;
            system.TileMined += (_, _) => completions++;
            player.SetActive(true);
            yield return null;
            action.Disable();
            Assert.IsFalse(action.enabled, "The regression must not rely on the shared Attack action.");

            tilemap.SetTile(cell, tile);
            SetButtonState(keyboard.enterKey, 1f);
            Assert.IsTrue(keyboard.enterKey.isPressed, "The Enter press was not applied.");
            InvokePrivate(controller, "Update");
            Assert.IsTrue(
                system.IsMining,
                $"Mining did not start. ControllerActive={controller.isActiveAndEnabled}, "
                + $"Pending={GetPrivate(controller, "startPending")}, "
                + $"Failure={system.LastFailure}, Position={movement.Position}, "
                + $"Facing={movement.FacingDirection}, Cell={cell}");
            Assert.IsNotNull(tilemap.GetTile(cell), "Enter must not bypass mining duration.");
            SetButtonState(keyboard.enterKey, 0f);
            InvokePrivate(controller, "Update");
            Assert.IsTrue(system.IsMining, "Releasing Enter must not cancel mining.");
            yield return new WaitForSeconds(0.3f);

            Assert.IsNull(tilemap.GetTile(cell));
            Assert.AreEqual(1, completions);

            tilemap.SetTile(cell, tile);
            var screen = (Vector2)camera.WorldToScreenPoint(tilemap.GetCellCenterWorld(cell));
            InputSystem.QueueStateEvent(mouse, new MouseState { position = screen });
            InputSystem.Update();
            SetButtonState(mouse.leftButton, 1f);
            InvokePrivate(controller, "Update");
            Assert.IsTrue(
                system.IsMining,
                $"Mouse mining did not start. Button={mouse.leftButton.isPressed}, "
                + $"Pointer={mouse.position.ReadValue()}, Screen={screen}, "
                + $"Pending={GetPrivate(controller, "startPending")}, "
                + $"Failure={system.LastFailure}");
            Assert.IsNotNull(tilemap.GetTile(cell), "Mouse click must not bypass mining duration.");
            SetButtonState(mouse.leftButton, 0f);
            InvokePrivate(controller, "Update");
            Assert.IsTrue(system.IsMining, "Releasing the mouse button must not cancel mining.");
            yield return new WaitForSeconds(0.3f);

            Assert.IsNull(tilemap.GetTile(cell));
            Assert.AreEqual(2, completions);

            Object.DestroyImmediate(player);
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(cameraObject);
            Object.DestroyImmediate(tile);
            Object.DestroyImmediate(input);
            InputSystem.RemoveDevice(mouse);
            InputSystem.RemoveDevice(keyboard);
        }

        [Test]
        public void MiningDoesNotStartWithoutPower()
        {
            GameObject root = new("MiningTest");
            MiningSystem system = root.AddComponent<MiningSystem>();
            system.SetMiningPowerAvailable(false);
            Assert.IsFalse(system.TryStartMining(Vector3Int.zero));
            Object.DestroyImmediate(root);
        }

        [Test]
        public void InstantMiningRemovesTerrainAndSpawnsMineralFromTheCell()
        {
            GameObject root = new("MiningTest");
            GameObject gridObject = new("Grid"); gridObject.transform.SetParent(root.transform); gridObject.AddComponent<Grid>();
            GameObject tilemapObject = new("Tilemap"); tilemapObject.transform.SetParent(gridObject.transform);
            Tilemap tilemap = tilemapObject.AddComponent<Tilemap>();
            MiningTileResolver resolver = root.AddComponent<MiningTileResolver>();
            MiningSystem system = root.AddComponent<MiningSystem>();

            SetPrivate(system, "foregroundTilemap", tilemap);
            SetPrivate(system, "tileResolver", resolver);
            Tile tile = ScriptableObject.CreateInstance<Tile>();
            resolver.RegisterRuntime(tile, new MiningTileDto(
                "tile.iron",
                "mineral.iron",
                1,
                true,
                1f,
                0.2f,
                0f,
                false));
            Vector3Int cell = new(2, -3, 0);
            tilemap.SetTile(cell, tile);

            Assert.IsTrue(system.TryMineInstant(cell));

            Assert.IsNull(tilemap.GetTile(cell));
            Assert.AreEqual(1, system.SpawnedResourceDropCount);
            Transform drop = system.transform.Find("MinedResourceDrops/MinedResource_mineral_iron");
            Assert.IsNotNull(drop);
            Assert.AreEqual(
                tilemap.GetCellCenterWorld(cell) + Vector3.up * 0.2f,
                drop.position);
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(tile);
        }

        [Test]
        public void InstantMiningAlsoRemovesOrdinaryRockWithoutCreatingAResource()
        {
            GameObject root = new("MiningTest");
            GameObject gridObject = new("Grid"); gridObject.transform.SetParent(root.transform); gridObject.AddComponent<Grid>();
            GameObject tilemapObject = new("Tilemap"); tilemapObject.transform.SetParent(gridObject.transform);
            Tilemap tilemap = tilemapObject.AddComponent<Tilemap>();
            MiningTileResolver resolver = root.AddComponent<MiningTileResolver>();
            MiningSystem system = root.AddComponent<MiningSystem>();

            SetPrivate(system, "foregroundTilemap", tilemap);
            SetPrivate(system, "tileResolver", resolver);
            Tile tile = ScriptableObject.CreateInstance<Tile>();
            resolver.RegisterRuntime(tile, new MiningTileDto(
                "tile.rock.normal",
                string.Empty,
                0,
                true,
                1f,
                0.2f,
                0f,
                false));
            Vector3Int cell = new(0, -2, 0);
            tilemap.SetTile(cell, tile);

            Assert.IsTrue(system.TryMineInstant(cell));

            Assert.IsNull(tilemap.GetTile(cell));
            Assert.AreEqual(0, system.SpawnedResourceDropCount);
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(tile);
        }

        private static void SetPrivate(object target, string field, object value)
        {
            target.GetType().GetField(field, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(target, value);
        }

        private static object GetPrivate(object target, string field)
        {
            return target.GetType().GetField(
                field,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(target);
        }

        private static void InvokePrivate(object target, string method)
        {
            target.GetType().GetMethod(
                method,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(target, null);
        }

        private static void SetButtonState(ButtonControl button, float value)
        {
            using (StateEvent.From(button.device, out var eventPtr))
            {
                button.WriteValueIntoEvent(value, eventPtr);
                InputSystem.QueueEvent(eventPtr);
            }

            InputSystem.Update();
        }

        private static void CreateSystem(
            out GameObject root,
            out Tilemap tilemap,
            out MiningTileResolver resolver,
            out MiningSystem system)
        {
            root = new GameObject("MiningTest");
            var gridObject = new GameObject("Grid");
            gridObject.transform.SetParent(root.transform);
            gridObject.AddComponent<Grid>();
            var tilemapObject = new GameObject("Tilemap");
            tilemapObject.transform.SetParent(gridObject.transform);
            tilemap = tilemapObject.AddComponent<Tilemap>();
            resolver = root.AddComponent<MiningTileResolver>();
            system = root.AddComponent<MiningSystem>();
            SetPrivate(system, "foregroundTilemap", tilemap);
            SetPrivate(system, "tileResolver", resolver);
        }
    }
}

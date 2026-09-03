using System.Collections.Generic;
using NUnit.Framework;
using SubTerra.App.Core;
using SubTerra.App.Core.Data;
using SubTerra.App.Inventory;
using SubTerra.App.Outpost;
using SubTerra.App.Progression;
using SubTerra.App.Save;
using SubTerra.App.State;
using SubTerra.App.Tutorial;
using SubTerra.App.UI.Building;
using SubTerra.Gameplay.Player;
using SubTerra.Shared;
using UnityEditor;
using UnityEngine;

namespace SubTerra.App.Tests
{
    public sealed class PromptB86ClinicTests
    {
        [Test]
        public void PromptB86_CatalogHasClinicAfterChargerWithMatchingCostAndPower()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<GameDataCatalog>(
                "Assets/_Project/Data/Catalog/GameDataCatalog.asset");

            Assert.That(catalog, Is.Not.Null);
            Assert.That(catalog.TryGetBuilding(DataIds.Buildings.ClinicBasic, out var clinic), Is.True);
            Assert.That(clinic.DisplayName, Is.EqualTo("보건소"));
            Assert.That(clinic.PowerDraw, Is.EqualTo(3));
            Assert.That(clinic.BuildCosts.Count, Is.EqualTo(1));
            Assert.That(clinic.BuildCosts[0].ItemId, Is.EqualTo(DataIds.Minerals.Copper));
            Assert.That(clinic.BuildCosts[0].Quantity, Is.EqualTo(3));
            Assert.That(IndexOf(catalog.Buildings, DataIds.Buildings.ClinicBasic),
                Is.EqualTo(IndexOf(catalog.Buildings, DataIds.Buildings.ChargerBasic) + 1));
        }

        [Test]
        public void PromptB86_BuildingMenuPlacesClinicImmediatelyBelowCharger()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Project/Prefabs/UI/BuildingMenu.prefab");
            Assert.That(prefab, Is.Not.Null);

            var charger = Find(prefab.transform, "Select_" + DataIds.Buildings.ChargerBasic);
            var clinic = Find(prefab.transform, "Select_" + DataIds.Buildings.ClinicBasic);
            var storage = Find(prefab.transform, "Select_" + DataIds.Buildings.StorageBasic);

            Assert.That(charger, Is.Not.Null);
            Assert.That(clinic, Is.Not.Null);
            Assert.That(storage, Is.Not.Null);
            Assert.That(charger.GetComponent<BuildingMenuEntryButton>().BuildingId,
                Is.EqualTo(DataIds.Buildings.ChargerBasic));
            Assert.That(clinic.GetComponent<BuildingMenuEntryButton>().BuildingId,
                Is.EqualTo(DataIds.Buildings.ClinicBasic));
            Assert.That(charger.GetComponent<RectTransform>().anchoredPosition.y,
                Is.GreaterThan(clinic.GetComponent<RectTransform>().anchoredPosition.y));
            Assert.That(clinic.GetComponent<RectTransform>().anchoredPosition.y,
                Is.GreaterThan(storage.GetComponent<RectTransform>().anchoredPosition.y));
        }

        [Test]
        public void PromptB86_TryHealRestoresHealthAndFullHealthStillSucceeds()
        {
            var health = new FakeHealthCommand(true);
            var service = CreateService(health);
            service.ApplyRuntimeStatus(ClinicStatus(true));

            var restored = service.TryHeal();

            Assert.That(restored.IsSuccess, Is.True);
            Assert.That(restored.Kind, Is.EqualTo(OutpostOperationKind.Heal));
            Assert.That(restored.Message, Is.EqualTo("체력 회복이 완료되었습니다."));
            Assert.That(health.CallCount, Is.EqualTo(1));

            health.Changed = false;
            var alreadyFull = service.TryHeal();

            Assert.That(alreadyFull.IsSuccess, Is.True);
            Assert.That(alreadyFull.Message, Is.EqualTo("이미 체력이 최대입니다."));
            Assert.That(health.CallCount, Is.EqualTo(2));
        }

        [Test]
        public void PromptB86_DisconnectedClinicFailsWithoutCallingHealthCommand()
        {
            var health = new FakeHealthCommand(true);
            var service = CreateService(health);
            service.ApplyRuntimeStatus(ClinicStatus(false));

            Assert.That(service.TryGetPowerDisconnectedInteractionMessage(out var message), Is.True);
            Assert.That(message, Is.EqualTo(
                "보건소 사용불가, 전력망 미연결\n"
                + " 엘레베이터 또는 전진기지 코어 근처에서 전력망 연결이 가능합니다."));

            var result = service.TryHeal();

            Assert.That(result.Status, Is.EqualTo(OutpostOperationStatus.FacilityUnavailable));
            Assert.That(health.CallCount, Is.Zero);
        }

        [Test]
        public void PromptB86_PlayerSurvivalControllerImplementsSharedRestoreCommand()
        {
            var player = new GameObject("ClinicHealthPlayer");
            player.transform.position = new Vector3(0.5f, 0.5f, 0f);
            var host = new GameObject("ClinicHealthController");
            var settings = ScriptableObject.CreateInstance<PlayerSurvivalSettings>();
            var controller = host.AddComponent<PlayerSurvivalController>();
            controller.Configure(settings, player.transform);

            Assert.That(controller.ApplyCollapse(new StructuralCollapseEventDto
            {
                worldSeed = 86,
                severity = StructuralCollapseSeverity.Severe,
                cells = new List<CollapseCellDto>
                {
                    new CollapseCellDto { x = 0, y = 0 }
                }
            }), Is.True);

            IPlayerHealthCommand command = controller;
            Assert.That(command.RestoreFull(), Is.True);
            Assert.That(controller.State.Health, Is.EqualTo(controller.State.MaximumHealth));
            Assert.That(command.RestoreFull(), Is.False);

            Object.DestroyImmediate(host);
            Object.DestroyImmediate(player);
            Object.DestroyImmediate(settings);
        }

        [Test]
        public void PromptB86_QuestRequiresNearClinicAndSuccessfulHeal()
        {
            var director = new DemoObjectiveDirector();
            director.BindGameState(GameState.CreateNew());
            director.RestoreFromProgress(new ProgressState(
                10,
                false,
                DemoObjectiveIds.InstallOutpostCore,
                false));

            director.OnGameplayEvent(Placed(DataIds.Buildings.OutpostCoreBasic, 0, -15));
            director.OnGameplayEvent(Placed(DataIds.Buildings.ChargerBasic, 10, -15));
            director.OnOutpostOperationCompleted(Success(OutpostOperationKind.Charge));
            Assert.That(director.CurrentObjectiveId, Is.EqualTo(DemoObjectiveIds.HealNearOutpost));

            director.OnGameplayEvent(Placed(DataIds.Buildings.ClinicBasic, 11, -15));
            director.OnOutpostOperationCompleted(Success(OutpostOperationKind.Heal));
            Assert.That(director.CurrentObjectiveId, Is.EqualTo(DemoObjectiveIds.HealNearOutpost));

            director.OnGameplayEvent(Placed(DataIds.Buildings.ClinicBasic, 10, -15));
            director.OnOutpostOperationCompleted(Success(OutpostOperationKind.Heal));
            Assert.That(director.CurrentObjectiveId, Is.EqualTo(DemoObjectiveIds.UnlockDeepZone));
            Assert.That(director.CompletedCount, Is.EqualTo(13));
        }

        [TestCase(DemoObjectiveIds.InstallOutpostCore, 10)]
        [TestCase(DemoObjectiveIds.ChargeNearOutpost, 11)]
        public void PromptB86_MigrationKeepsObjectivesBeforeOrAtCharger(
            string objectiveId,
            int completed)
        {
            var data = LegacySave(objectiveId, completed, false);

            Assert.That(new SaveMigrationService().TryMigrate(data),
                Is.EqualTo(SaveMigrationStatus.Migrated));
            Assert.That(data.progress.currentObjectiveId, Is.EqualTo(objectiveId));
            Assert.That(data.progress.completedObjectives, Is.EqualTo(completed));
        }

        [Test]
        public void PromptB86_MigrationMarksClinicCompleteWithoutRewindingLaterObjective()
        {
            var codec = new SaveJsonCodec(new SaveMigrationService());
            var legacyJson = codec.Serialize(LegacySave(DemoObjectiveIds.UnlockDeepZone, 12, false));

            Assert.That(codec.TryDeserialize(legacyJson, out var migrated),
                Is.EqualTo(SaveMigrationStatus.Migrated));
            Assert.That(migrated.progress.currentObjectiveId,
                Is.EqualTo(DemoObjectiveIds.UnlockDeepZone));
            Assert.That(migrated.progress.completedObjectives, Is.EqualTo(13));

            var currentJson = codec.Serialize(migrated);
            Assert.That(codec.TryDeserialize(currentJson, out var roundTripped),
                Is.EqualTo(SaveMigrationStatus.Current));
            Assert.That(roundTripped.progress.currentObjectiveId,
                Is.EqualTo(DemoObjectiveIds.UnlockDeepZone));
            Assert.That(roundTripped.progress.completedObjectives, Is.EqualTo(13));
        }

        [Test]
        public void PromptB86_MigrationNormalizesCompletedDemoToEighteen()
        {
            var data = LegacySave(DemoObjectiveIds.DemoEnd, 17, true);

            Assert.That(new SaveMigrationService().TryMigrate(data),
                Is.EqualTo(SaveMigrationStatus.Migrated));
            Assert.That(data.progress.currentObjectiveId, Is.EqualTo(DemoObjectiveIds.DemoEnd));
            Assert.That(data.progress.completedObjectives, Is.EqualTo(18));
            Assert.That(DemoObjectiveIds.RequiredCount, Is.EqualTo(18));
            Assert.That(DeepZoneUnlockRule.Mvp.RequiredCompletedObjectives, Is.EqualTo(13));
        }

        private static OutpostService CreateService(IPlayerHealthCommand health)
        {
            var catalog = new InMemoryMineralCatalog();
            var state = GameState.CreateNew();
            return new OutpostService(
                new InventoryService(catalog, 100f, state),
                catalog,
                state,
                healthCommand: health);
        }

        private static OutpostStatusDto ClinicStatus(bool active)
        {
            return new OutpostStatusDto
            {
                isActive = false,
                isInInteractionRange = true,
                interactionFacilityInstanceId = "clinic.1",
                interactionFacilityBuildingId = DataIds.Buildings.ClinicBasic,
                connectedFacilities = new List<ConnectedFacilityStatusDto>
                {
                    new ConnectedFacilityStatusDto
                    {
                        instanceId = "clinic.1",
                        buildingId = DataIds.Buildings.ClinicBasic,
                        isActive = active,
                        inactiveReasonId = active ? string.Empty : "power_disconnected"
                    }
                }
            };
        }

        private static GameplayEventDto Placed(string buildingId, int x, int y)
        {
            return new GameplayEventDto
            {
                type = GameplayEventType.BuildingPlaced,
                entityId = buildingId,
                x = x,
                y = y,
                buildingPlacement = new BuildingPlacementResultDto
                {
                    state = BuildingPlacementState.Placed,
                    buildingId = buildingId,
                    x = x,
                    y = y
                }
            };
        }

        private static OutpostOperationResult Success(OutpostOperationKind kind)
        {
            return new OutpostOperationResult(
                OutpostOperationStatus.Success,
                kind,
                string.Empty,
                0,
                0,
                string.Empty);
        }

        private static GameSaveData LegacySave(string objectiveId, int completed, bool complete)
        {
            var data = new GameSaveData
            {
                saveVersion = 2,
                targetSceneName = SceneNames.Integration
            };
            data.progress.currentObjectiveId = objectiveId;
            data.progress.completedObjectives = completed;
            data.progress.isDemoComplete = complete;
            SaveDataValidator.NormalizeMissingCollections(data);
            return data;
        }

        private static int IndexOf(IReadOnlyList<BuildingData> buildings, string id)
        {
            for (var i = 0; i < buildings.Count; i++)
            {
                if (buildings[i] != null && buildings[i].Id == id)
                {
                    return i;
                }
            }

            return -1;
        }

        private static Transform Find(Transform root, string name)
        {
            var children = root.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < children.Length; i++)
            {
                if (children[i].name == name)
                {
                    return children[i];
                }
            }

            return null;
        }

        private sealed class FakeHealthCommand : IPlayerHealthCommand
        {
            public bool Changed;
            public int CallCount;

            public FakeHealthCommand(bool changed)
            {
                Changed = changed;
            }

            public bool RestoreFull()
            {
                CallCount++;
                return Changed;
            }
        }
    }
}

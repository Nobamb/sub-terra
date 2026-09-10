using System;
using System.Collections.Generic;
using NUnit.Framework;
using SubTerra.App.Core.Data;
using SubTerra.App.State;
using SubTerra.App.UI.Building;
using SubTerra.Shared;
using UnityEditor;

namespace SubTerra.App.Tests.UI
{
    public sealed class BuildingMenuPresenterTests
    {
        private GameDataCatalog catalog;
        private RecordingBuildingMenuView view;
        private FakeWallet wallet;
        private FakePlacementPort placement;
        private GameState state;
        private BuildingMenuPresenter presenter;

        [SetUp]
        public void SetUp()
        {
            catalog = AssetDatabase.LoadAssetAtPath<GameDataCatalog>(
                "Assets/_Project/Data/Catalog/GameDataCatalog.asset");
            Assert.That(catalog, Is.Not.Null);
            Assert.That(catalog.ValidateAll().IsValid, Is.True);

            view = new RecordingBuildingMenuView();
            wallet = new FakeWallet { CanAffordValue = true };
            placement = new FakePlacementPort();
            state = GameState.CreateNew();
            presenter = new BuildingMenuPresenter(view);
            presenter.Bind(catalog, wallet, null, placement, state);
        }

        [TearDown]
        public void TearDown()
        {
            presenter?.Unbind();
        }

        [Test]
        public void G_S02_ListUsesBuildingDataDescriptionCostAndPower()
        {
            Assert.That(view.Items.Count, Is.EqualTo(catalog.Buildings.Count));
            var support = FindItem(DataIds.Buildings.SupportBasic);
            Assert.That(support.Description, Is.Not.Empty);
            Assert.That(support.PowerDraw, Is.EqualTo(
                FindData(DataIds.Buildings.SupportBasic).PowerDraw));
            Assert.That(support.Costs, Is.Not.Empty);
        }

        [Test]
        public void PromptB45_OutpostCoreDescriptionExplainsGasSafeZoneAndCheckpoint()
        {
            var core = FindItem(DataIds.Buildings.OutpostCoreBasic);

            Assert.That(core.Description, Does.Contain("유독 가스 정화 안전지대"));
            Assert.That(core.Description, Does.Contain("탐사 체크포인트"));
        }

        [Test]
        public void G_S05_PrefabsContainBuildingAndAccessibleHazardViews()
        {
            var menu = AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(
                "Assets/_Project/Prefabs/UI/BuildingMenu.prefab");
            var hud = AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(
                "Assets/_Project/Prefabs/UI/HUDCanvas.prefab");

            Assert.That(menu, Is.Not.Null);
            Assert.That(menu.GetComponent<BuildingMenuView>().HasRequiredReferences(), Is.True);
            Assert.That(menu.GetComponent<BuildingMenuBinder>().HasRequiredReferences(), Is.True);
            Assert.That(menu.GetComponentsInChildren<BuildingMenuEntryButton>(true).Length, Is.EqualTo(9));
            Assert.That(hud, Is.Not.Null);
            Assert.That(hud.GetComponent<SubTerra.App.UI.Hazards.HazardHudView>()
                .HasRequiredReferences(), Is.True);
        }

        [Test]
        public void G_F01_SelectAndCancel_StartsAndEndsPreviewOnce()
        {
            Assert.That(presenter.SelectBuilding(DataIds.Buildings.SupportBasic), Is.True);
            Assert.That(placement.BeginCount, Is.EqualTo(1));
            Assert.That(state.GetBuildingSelection().HasSelection, Is.True);

            presenter.CancelSelection();

            Assert.That(placement.CancelCount, Is.EqualTo(1));
            Assert.That(presenter.SelectedBuildingId, Is.Empty);
            Assert.That(state.GetBuildingSelection().HasSelection, Is.False);
            Assert.That(view.LastAvailability.PlacementState, Is.EqualTo(BuildingPlacementState.None));
        }

        [Test]
        public void G_F02_ValidLocationButInsufficientResources_ShowsCostReason()
        {
            wallet.CanAffordValue = false;
            Assert.That(presenter.SelectBuilding(DataIds.Buildings.SupportBasic), Is.True);

            placement.Emit(new BuildingPlacementResultDto
            {
                state = BuildingPlacementState.Valid,
                buildingId = DataIds.Buildings.SupportBasic
            });

            Assert.That(view.LastAvailability.CanPlace, Is.False);
            Assert.That(view.LastAvailability.CanAfford, Is.False);
            Assert.That(view.LastAvailability.Message, Does.Contain("자원이 부족"));
            Assert.That(wallet.TrySpendCount, Is.Zero);
        }

        [Test]
        public void G_F03_PlacementFailure_ClearsSelectionWithoutSpending()
        {
            Assert.That(presenter.SelectBuilding(DataIds.Buildings.SupportBasic), Is.True);

            placement.Emit(new BuildingPlacementResultDto
            {
                state = BuildingPlacementState.Failed,
                buildingId = DataIds.Buildings.SupportBasic,
                reasonId = "occupied"
            });

            Assert.That(presenter.SelectedBuildingId, Is.Empty);
            Assert.That(state.GetBuildingSelection().HasSelection, Is.False);
            Assert.That(wallet.TrySpendCount, Is.Zero);
            Assert.That(view.StatusMessage, Does.Contain("차지한 위치"));
        }

        [TestCase("out_of_range", "너무 먼")]
        [TestCase("outside_allowed_area", "허용되지 않은 구역")]
        public void F_F02_PlacementLimitFailure_ShowsImmediateReason(
            string reasonId,
            string expectedMessage)
        {
            Assert.That(presenter.SelectBuilding(DataIds.Buildings.SupportBasic), Is.True);

            placement.Emit(new BuildingPlacementResultDto
            {
                state = BuildingPlacementState.Failed,
                buildingId = DataIds.Buildings.SupportBasic,
                reasonId = reasonId
            });

            Assert.That(view.StatusMessage, Does.Contain(expectedMessage));
            Assert.That(wallet.TrySpendCount, Is.Zero);
        }

        [Test]
        public void G_F04_DuplicateSuccessEvent_DoesNotTriggerUiPaymentOrRestoreSelection()
        {
            Assert.That(presenter.SelectBuilding(DataIds.Buildings.SupportBasic), Is.True);
            var success = new BuildingPlacementResultDto
            {
                state = BuildingPlacementState.Placed,
                buildingId = DataIds.Buildings.SupportBasic,
                instanceId = "support-0001"
            };

            placement.Emit(success);
            placement.Emit(success);

            Assert.That(wallet.TrySpendCount, Is.Zero,
                "결제는 A의 생성 성공 경로에서만 호출되며 성공 UI 이벤트가 다시 결제하면 안 됩니다.");
            Assert.That(presenter.SelectedBuildingId, Is.Empty);
            Assert.That(state.GetBuildingSelection().HasSelection, Is.False);
            Assert.That(view.SuccessMessageCount, Is.EqualTo(1));
        }

        [Test]
        public void G_S03_CanPlaceRequiresBothLocationAndAffordability()
        {
            Assert.That(presenter.SelectBuilding(DataIds.Buildings.SupportBasic), Is.True);
            wallet.CanAffordValue = true;
            placement.Emit(new BuildingPlacementResultDto
            {
                state = BuildingPlacementState.Invalid,
                buildingId = DataIds.Buildings.SupportBasic,
                reasonId = "missing_ground"
            });
            Assert.That(view.LastAvailability.CanPlace, Is.False);

            placement.Emit(new BuildingPlacementResultDto
            {
                state = BuildingPlacementState.Valid,
                buildingId = DataIds.Buildings.SupportBasic
            });
            Assert.That(view.LastAvailability.CanPlace, Is.True);

            wallet.CanAffordValue = false;
            placement.Emit(new BuildingPlacementResultDto
            {
                state = BuildingPlacementState.Valid,
                buildingId = DataIds.Buildings.SupportBasic,
                x = 1
            });
            Assert.That(view.LastAvailability.CanPlace, Is.False);
        }

        private BuildingMenuItemReadModel FindItem(string id)
        {
            for (var i = 0; i < view.Items.Count; i++)
            {
                if (view.Items[i].BuildingId == id)
                {
                    return view.Items[i];
                }
            }

            Assert.Fail("Missing item: " + id);
            return null;
        }

        private BuildingData FindData(string id)
        {
            Assert.That(catalog.TryGetBuilding(id, out var data), Is.True);
            return data;
        }

        private sealed class FakeWallet : IResourceWallet
        {
            public bool CanAffordValue;
            public int TrySpendCount;

            public bool CanAfford(IReadOnlyList<ItemCostDto> costs)
            {
                return CanAffordValue;
            }

            public bool TrySpend(IReadOnlyList<ItemCostDto> costs)
            {
                TrySpendCount++;
                return true;
            }
        }

        private sealed class FakePlacementPort : IBuildingPlacementPort
        {
            public event Action<BuildingPlacementResultDto> PlacementChanged;

            public int BeginCount;
            public int CancelCount;

            public bool BeginPreview(string buildingId)
            {
                BeginCount++;
                return true;
            }

            public void CancelPreview()
            {
                CancelCount++;
            }

            public void Emit(BuildingPlacementResultDto result)
            {
                PlacementChanged?.Invoke(result);
            }
        }

        private sealed class RecordingBuildingMenuView : IBuildingMenuView
        {
            public IReadOnlyList<BuildingMenuItemReadModel> Items { get; private set; } =
                Array.Empty<BuildingMenuItemReadModel>();
            public BuildingAvailabilityReadModel LastAvailability { get; private set; }
            public string StatusMessage { get; private set; } = string.Empty;
            public int SuccessMessageCount { get; private set; }

            public void SetBuildingList(IReadOnlyList<BuildingMenuItemReadModel> items)
            {
                Items = items;
            }

            public void SetSelection(BuildingMenuItemReadModel item) { }
            public void ClearSelection() { }

            public void SetAvailability(BuildingAvailabilityReadModel availability)
            {
                LastAvailability = availability;
            }

            public void SetStatusMessage(string message)
            {
                StatusMessage = message ?? string.Empty;
                if (StatusMessage.Contains("완료"))
                {
                    SuccessMessageCount++;
                }
            }

            public void SetVisible(bool visible) { }
        }
    }
}

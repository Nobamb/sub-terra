using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using SubTerra.App.Core.Data;
using SubTerra.App.Inventory;
using SubTerra.App.Outpost;
using SubTerra.App.State;
using SubTerra.App.UI.Outpost;
using SubTerra.Shared;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace SubTerra.App.Tests.PlayMode
{
    public sealed class FacilityCooldownPopupPlayModeTests
    {
        private GameObject canvas;

        [SetUp]
        public void SetUp()
        {
            canvas = new GameObject("CooldownPlayCanvas", typeof(RectTransform), typeof(Canvas));
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        }

        [TearDown]
        public void TearDown()
        {
            if (canvas != null)
            {
                UnityEngine.Object.DestroyImmediate(canvas);
            }
        }

        [UnityTest]
        public IEnumerator Deactivate_StopsProviderPolling_ClearsState_AndSilencesEffects()
        {
            var view = FacilityCooldownPopupView.Create(canvas.transform, null);
            var calls = 0;
            double remaining = 100d;
            view.ShowFacilityCooldown(DataIds.Buildings.ChargerBasic, "charger.a", id => { calls++; return remaining; });
            yield return null;
            yield return null;
            Assert.That(calls, Is.GreaterThan(0));

            view.gameObject.SetActive(false);
            var callsAtOff = calls;
            for (var i = 0; i < 5; i++)
            {
                yield return null;
            }

            Assert.That(calls, Is.EqualTo(callsAtOff));
            Assert.That(view.InstanceId, Is.Empty);
            Assert.That(view.Progress, Is.EqualTo(0f));
            foreach (var image in view.GetComponentsInChildren<Image>(true))
            {
                if (image.name.StartsWith("Mote") || image.name.StartsWith("Flow") || image.name.StartsWith("Flash")
                    || image.name == "OuterGlow" || image.name == "InnerGlow")
                {
                    Assert.That(image.color.a, Is.EqualTo(0f), image.name);
                }

                Assert.That(image.raycastTarget, Is.False, image.name);
            }

            Assert.That(view.StageGroup.blocksRaycasts, Is.False);
            Assert.That(view.StageGroup.interactable, Is.False);
        }

        [UnityTest]
        public IEnumerator Destroy_ReleasesPopup_AndProviderIsNotInvokedAfterwards()
        {
            var view = FacilityCooldownPopupView.Create(canvas.transform, null);
            var calls = 0;
            view.ShowFacilityCooldown(DataIds.Buildings.ChargerBasic, "charger.a", id => { calls++; return 100d; });
            yield return null;
            var callsBeforeDestroy = calls;

            UnityEngine.Object.Destroy(view.gameObject);
            for (var i = 0; i < 3; i++)
            {
                yield return null;
            }

            Assert.That(view == null, Is.True);
            Assert.That(calls, Is.EqualTo(callsBeforeDestroy));
        }

        [UnityTest]
        public IEnumerator OutpostHost_RetargetingBetweenFacilities_KeepsOneRuntimePopupAndNoTvRestart()
        {
            var host = new GameObject("OutpostPanelHost", typeof(RectTransform));
            host.transform.SetParent(canvas.transform, false);
            var panel = host.AddComponent<OutpostPanelView>();
            double remaining = 200d;
            Func<string, double> provider = id => remaining;

            Assert.That(panel.ShowFacilityCooldown(DataIds.Buildings.ChargerBasic, "charger.a", provider), Is.True);
            yield return null;
            yield return null;
            var popup = canvas.GetComponentInChildren<FacilityCooldownPopupView>(true);
            Assert.That(popup, Is.Not.Null);
            var progressBefore = popup.Progress;

            Assert.That(panel.ShowFacilityCooldown(DataIds.Buildings.ClinicBasic, "clinic.a", provider), Is.True);
            yield return null;
            Assert.That(panel.ShowFacilityCooldown(DataIds.Buildings.ChargerBasic, "charger.a", provider), Is.True);
            yield return null;

            var runtimeCount = 0;
            foreach (var found in canvas.GetComponentsInChildren<FacilityCooldownPopupView>(true))
            {
                if (found.name == "FacilityCooldownPopup_Runtime")
                {
                    runtimeCount++;
                }
            }

            Assert.That(runtimeCount, Is.EqualTo(1));
            Assert.That(popup.InstanceId, Is.EqualTo("charger.a"));
            Assert.That(popup.IsOpen, Is.True);
            Assert.That(popup.Progress, Is.GreaterThanOrEqualTo(progressBefore));
        }

        [UnityTest]
        public IEnumerator Presenter_ExpiryWhileCompleting_ChargesExactlyOnceThenBlocksSameFrame()
        {
            var catalog = new InMemoryMineralCatalog();
            catalog.Register(DataIds.Minerals.Copper, 1.5f, 10, "구리");
            var state = GameState.CreateNew();
            var inventory = new InventoryService(catalog, 100f, state);
            var service = new OutpostService(inventory, catalog, state);
            var popup = FacilityCooldownPopupView.Create(canvas.transform, null);
            var view = new CompositePanelView(popup);
            var presenter = new OutpostPanelPresenter(view);
            state.SetEnergy(10, 100);
            service.ApplyRuntimeStatus(ChargerStatus("charger.a"));
            presenter.Bind(service);

            presenter.ToggleInteractionPanel();
            Assert.That(state.Player.Energy, Is.EqualTo(100));
            presenter.DismissInteractionPanel();

            state.SetCurrentEnergy(10);
            presenter.ToggleInteractionPanel();
            Assert.That(popup.IsOpen, Is.True);

            state.AddMineResetElapsed(OutpostService.FacilityUseCooldownSeconds - 1.5d);
            yield return null;
            yield return null;
            state.AddMineResetElapsed(1.5d);
            yield return null;
            Assert.That(popup.IsCompleting || !popup.IsOpen, Is.True);

            state.SetCurrentEnergy(10);
            presenter.DismissInteractionPanel();
            presenter.ToggleInteractionPanel();
            Assert.That(state.Player.Energy, Is.EqualTo(100));
            Assert.That(popup.IsCompleting, Is.False);

            presenter.DismissInteractionPanel();
            state.SetCurrentEnergy(10);
            presenter.ToggleInteractionPanel();
            Assert.That(state.Player.Energy, Is.EqualTo(10));
            Assert.That(popup.IsOpen, Is.True);
            Assert.That(popup.InstanceId, Is.EqualTo("charger.a"));
        }

        private static OutpostStatusDto ChargerStatus(string instanceId)
        {
            return new OutpostStatusDto
            {
                isActive = false,
                isInInteractionRange = true,
                interactionFacilityInstanceId = instanceId,
                interactionFacilityBuildingId = DataIds.Buildings.ChargerBasic,
                connectedFacilities = new List<ConnectedFacilityStatusDto>
                {
                    new ConnectedFacilityStatusDto
                    {
                        instanceId = instanceId,
                        buildingId = DataIds.Buildings.ChargerBasic,
                        isActive = true,
                        inactiveReasonId = string.Empty
                    }
                }
            };
        }

        /// <summary>실제 쿨타임 팝업으로 위임하는 테스트용 패널 View. 나머지 표시는 기록만 한다.</summary>
        private sealed class CompositePanelView : IOutpostPanelView, IFacilityCooldownPopupView
        {
            private readonly FacilityCooldownPopupView popup;

            public CompositePanelView(FacilityCooldownPopupView popup)
            {
                this.popup = popup;
            }

            public bool ShowFacilityCooldown(string buildingId, string instanceId, Func<string, double> remainingProvider)
            {
                return popup.ShowFacilityCooldown(buildingId, instanceId, remainingProvider);
            }

            public void HideFacilityCooldown()
            {
                popup.HideFacilityCooldown();
            }

            public void SetVisible(bool visible) { }
            public void SetMode(OutpostPanelMode mode) { }
            public void SetPower(float supply, float consumption, bool active, string inactiveReasonId) { }
            public void SetFacilities(IReadOnlyList<OutpostFacilityReadModel> facilities) { }
            public void SetCargo(string playerCargo, string storageCargo) { }
            public void SetSettlementCargo(string cargo) { }
            public void SetCheckpoint(string checkpoint) { }
            public void SetSelectedMineral(string summary) { }
            public void SetMineralOptions(IReadOnlyList<OutpostMineralOption> options, string selectedMineralId) { }
            public void ClearMineralSearch() { }
            public void SetResult(string message, bool isError) { }
            public void ShowTemporaryMessage(string message, float durationSeconds) { }
            public void SetTutorialVisible(bool visible) { }
            public void SetBusy(bool busy) { }
        }
    }
}

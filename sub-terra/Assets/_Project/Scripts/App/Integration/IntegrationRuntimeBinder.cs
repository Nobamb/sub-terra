using System.Collections;
using SubTerra.App.Core;
using SubTerra.App.Core.Data;
using SubTerra.App.Inventory;
using SubTerra.App.Outpost;
using SubTerra.App.Save;
using SubTerra.App.State;
using SubTerra.App.UI.HUD;
using SubTerra.App.UI.Inventory;
using SubTerra.App.UI.Progression;
using SubTerra.App.UI.Tutorial;
using SubTerra.Gameplay.Building;
using SubTerra.Gameplay.Drone;
using SubTerra.Gameplay.Mining;
using SubTerra.Gameplay.Player;
using SubTerra.Shared;
using UnityEngine;

namespace SubTerra.App.Integration
{
    /// <summary>
    /// Mine_Demo_Integration Scene 로컬 binder.
    /// Bootstrap 전역 서비스를 중복 생성하지 않고 Shared 5경계를 A Runtime에 연결한다.
    /// 이어하기 시 HUD/입력은 SaveRuntime이 월드 복원·파생 재계산을 끝낸 뒤에만 활성화한다.
    /// </summary>
    public sealed class IntegrationRuntimeBinder :
        MonoBehaviour,
        IMiningRewardReceiver,
        IMiningTransaction,
        IGameplayEventSink,
        IIntegrationRestoreListener
    {
        [SerializeField] private BuildingPlacementSystem buildingPlacementSystem;
        [SerializeField] private HudBinder hudBinder;
        [SerializeField] private GameplayHazardStatusBridge hazardBridge;
        [SerializeField] private GasExposureEffectController gasEffectController;
        [SerializeField] private OutpostRuntimeBridge outpostBridge;
        [SerializeField] private BuildingUiIntegrationBinder buildingUiBinder;
        [SerializeField] private InventoryPanelBinder inventoryPanelBinder;
        [SerializeField] private ProgressionPanelBinder progressionPanelBinder;
        [SerializeField] private GameplayBuildingPlacementBridge placementBridge;
        [SerializeField] private DroneContextProviderAdapter droneContextAdapter;
        [SerializeField] private DroneSensor droneSensor;
        [SerializeField] private MonoBehaviour worldSnapshotProviderBehaviour;
        [SerializeField] private CanvasGroup hudCanvasGroup;
        [SerializeField] private Behaviour[] deferredInputBehaviours;
        [SerializeField] private TutorialDirectorBinder tutorialDirector;
        [SerializeField] private MiningSystem miningSystem;
        [SerializeField] private PlayerMovement playerMovement;
        [SerializeField] private MiningProgressHud miningProgressHud;

        private SaveRuntimeController runtime;
        private GameBootstrapper bootstrap;
        private OutpostService outpostService;
        private IntegrationEventFanOut eventFanOut;
        private IntegrationContractRegistry contracts;
        private IntegrationActivationGate activationGate;
        private bool contractsWired;
        private bool uiActivated;
        private bool inventorySpeedBound;

        public IntegrationContractRegistry Contracts => contracts;
        public IntegrationActivationGate ActivationGate => activationGate;
        public bool AreContractsWired => contractsWired;
        public bool IsUiActivated => uiActivated;

        private void Awake()
        {
            activationGate = new IntegrationActivationGate();
            contracts = new IntegrationContractRegistry();
            // 복원 전 HUD/입력을 끈다. 게이트가 열릴 때만 ActivateUi가 성공한다.
            SetHudVisible(false);
            SetDeferredInputEnabled(false);
        }

        private IEnumerator Start()
        {
            runtime = SaveRuntimeController.Instance;
            bootstrap = GameBootstrapper.Instance;
            if (runtime == null || bootstrap == null)
            {
                Debug.LogWarning(
                    "[SubTerra] Integration scene opened without the Bootstrap runtime.");
                yield break;
            }

            WireContracts();

            // ContinueRoutine은 월드 복원·재계산 후 IsUiReady=true.
            // 새 게임 탐사는 SurfaceBase 진입 시 이미 true인 경우가 많다.
            const int maximumWaitFrames = 300;
            var waited = 0;
            while (runtime != null && !runtime.IsUiReady && waited < maximumWaitFrames)
            {
                waited++;
                yield return null;
            }

            if (runtime == null || !runtime.IsUiReady)
            {
                // 이어하기 실패·타임아웃 시 HUD를 강제 열지 않는다.
                Debug.LogWarning(
                    "[SubTerra] Integration UI kept disabled: IsUiReady never became true.");
                yield break;
            }

            // SaveRuntime이 복원 순서를 끝냈거나, 새 세션으로 이미 준비된 상태.
            // NotifyWorldRestored 없이도 IsUiReady=true면 순서가 보장된 것으로 본다.
            if (!activationGate.IsDerivedRecalculated)
            {
                activationGate.MarkReadyForNewSession();
            }

            ActivateUi();
        }

        /// <summary>
        /// Shared 5경계를 Bootstrap 서비스와 Scene A 컴포넌트에 연결한다.
        /// Inventory/Economy는 SaveRuntimeController가 소유하며 여기서 새로 만들지 않는다.
        /// </summary>
        public void WireContracts()
        {
            if (contractsWired || runtime == null || bootstrap == null)
            {
                return;
            }

            runtime.EnsureGameplayServices();
            runtime.InventoryService?.BindGameState(bootstrap.State);

            if (miningSystem != null)
            {
                miningSystem.SetRuntimeServices(this, runtime.Progression?.Effects);
            }

            BindCargoSpeed();

            // IResourceWallet: A BuildingPlacementSystem → B EconomyService
            if (buildingPlacementSystem != null && runtime.Economy != null)
            {
                buildingPlacementSystem.SetResourceWallet(runtime.Economy);
            }

            // 전진기지 Consumer (Scene 로컬 서비스, 전역 중복 생성 아님)
            if (outpostBridge != null && runtime.InventoryService != null)
            {
                var catalog = bootstrap.AssignedCatalog as GameDataCatalog;
                IMineralCatalogLookup mineralLookup = catalog != null
                    ? (IMineralCatalogLookup)new GameDataCatalogMineralLookup(catalog)
                    : new InMemoryMineralCatalog();
                outpostService = new OutpostService(
                    runtime.InventoryService,
                    mineralLookup,
                    bootstrap.State);
                outpostBridge.BindTo(outpostService);
                runtime.BindAutoSaveEvents(
                    runtime.Economy,
                    runtime.Progression,
                    outpostService);
            }

            hazardBridge?.BindGameState(bootstrap.State);
            if (gasEffectController != null)
            {
                gasEffectController.FailureInputRaised += OnGasFailureInputRaised;
                gasEffectController.EffectStateChanged += OnGasEffectStateChanged;
                gasEffectController.Bind(bootstrap.State, runtime.Progression?.Effects);
            }

            var dataCatalog = bootstrap.AssignedCatalog as GameDataCatalog;
            if (placementBridge != null && runtime.Economy != null)
            {
                placementBridge.BindWallet(runtime.Economy, dataCatalog);
            }

            if (buildingUiBinder != null)
            {
                buildingUiBinder.BindTo(
                    runtime.Economy,
                    runtime.InventoryService,
                    bootstrap.State);
            }

            // IDroneContextProvider: A DroneSensor → B adapter
            if (droneContextAdapter != null && droneSensor != null)
            {
                droneContextAdapter.BindTo(droneSensor);
            }

            inventoryPanelBinder?.BindTo(runtime.InventoryService);
            progressionPanelBinder?.BindTo(
                runtime.Progression,
                () => bootstrap?.State?.Progress?.CompletedObjectives ?? 0);

            droneSensor?.SetUpgradeEffects(runtime.Progression?.Effects);

            eventFanOut = new IntegrationEventFanOut();
            if (hazardBridge != null)
            {
                eventFanOut.Add(hazardBridge);
            }

            if (outpostBridge != null)
            {
                eventFanOut.Add(outpostBridge);
            }

            if (tutorialDirector == null)
            {
                tutorialDirector = FindFirstObjectByType<TutorialDirectorBinder>();
            }

            if (tutorialDirector != null)
            {
                eventFanOut.Add(tutorialDirector);
                tutorialDirector.BindTo(
                    bootstrap.State,
                    runtime.InventoryService,
                    runtime.Economy,
                    runtime.Progression,
                    outpostService);
            }

            var worldProvider = worldSnapshotProviderBehaviour as IWorldSnapshotProvider
                ?? runtime.Resolve();

            contracts.Bind(
                miningRewardReceiver: this,
                resourceWallet: runtime.Economy,
                gameplayEventSink: this,
                worldSnapshotProvider: worldProvider,
                droneContextProvider: ResolveDroneProvider());

            contractsWired = true;
            activationGate.MarkStateReady();
        }

        /// <summary>SaveRuntime Continue 경로에서 월드 복원 직후 호출한다.</summary>
        public void NotifyWorldRestored()
        {
            if (activationGate == null)
            {
                activationGate = new IntegrationActivationGate();
            }

            activationGate.MarkWorldRestored();
        }

        /// <summary>SaveRuntime Continue 경로에서 파생 재계산 직후 호출한다.</summary>
        public void NotifyDerivedRecalculated()
        {
            if (activationGate == null)
            {
                activationGate = new IntegrationActivationGate();
            }

            activationGate.MarkDerivedRecalculated();
        }

        /// <summary>
        /// 게이트가 열렸을 때만 HUD/입력을 켠다. 강제 MarkReady 우회 경로 없음.
        /// </summary>
        public bool ActivateUi()
        {
            if (uiActivated)
            {
                return true;
            }

            if (activationGate == null)
            {
                activationGate = new IntegrationActivationGate();
            }

            // SaveRuntime IsUiReady가 true이면 복원 순서가 끝난 것으로 인정해 게이트를 채운다.
            // 게이트가 비어 있고 IsUiReady도 아니면 강제 MarkReady 우회 없음.
            if (!activationGate.IsDerivedRecalculated
                && runtime != null
                && runtime.IsUiReady)
            {
                activationGate.MarkReadyForNewSession();
            }

            if (!activationGate.TryActivateUi())
            {
                return false;
            }

            if (hudBinder != null && bootstrap != null)
            {
                hudBinder.BindTo(bootstrap.State);
            }

            miningProgressHud?.BindTo(
                miningSystem,
                playerMovement != null ? playerMovement.transform : null);

            SetHudVisible(true);
            SetDeferredInputEnabled(true);
            uiActivated = true;
            return true;
        }

        public void AddMineral(string mineralId, int quantity)
        {
            runtime ??= SaveRuntimeController.Instance;
            runtime?.InventoryService?.AddMineral(mineralId, quantity);
        }

        public bool CanAffordEnergy(int energyCost)
        {
            var state = bootstrap != null ? bootstrap.State : GameBootstrapper.Instance?.State;
            return state != null && state.Player.Energy >= Mathf.Max(0, energyCost);
        }

        public MiningCommitResult TryCommitMining(
            string mineralId,
            int quantity,
            int energyCost)
        {
            runtime ??= SaveRuntimeController.Instance;
            bootstrap ??= GameBootstrapper.Instance;
            var inventory = runtime?.InventoryService;
            var state = bootstrap?.State;
            if (inventory == null || state == null)
            {
                return new MiningCommitResult(MiningCommitStatus.DependencyMissing);
            }

            var cost = Mathf.Max(0, energyCost);
            if (state.Player.Energy < cost)
            {
                return new MiningCommitResult(MiningCommitStatus.InsufficientEnergy);
            }

            var hasReward = !string.IsNullOrEmpty(mineralId) || quantity != 0;
            if (hasReward)
            {
                if (string.IsNullOrEmpty(mineralId) || quantity <= 0)
                {
                    return new MiningCommitResult(MiningCommitStatus.InvalidReward);
                }

                // 전량 수락을 먼저 확정한다. 실패하면 전력과 월드 타일은 그대로 남는다.
                var reward = inventory.TryAddMineralExact(mineralId, quantity);
                if (reward.Status != InventoryMutationStatus.Success)
                {
                    return new MiningCommitResult(
                        reward.Status == InventoryMutationStatus.CapacityFull
                            ? MiningCommitStatus.InventoryFull
                            : MiningCommitStatus.InvalidReward);
                }
            }

            state.SetCurrentEnergy(state.Player.Energy - cost);
            return MiningCommitResult.Success();
        }

        private void BindCargoSpeed()
        {
            if (inventorySpeedBound || runtime?.InventoryService == null || playerMovement == null)
            {
                return;
            }

            runtime.InventoryService.InventoryChanged += OnInventoryChangedForMovement;
            inventorySpeedBound = true;
            ApplyCargoSpeed(runtime.InventoryService.GetSnapshot());
        }

        private void OnInventoryChangedForMovement(InventorySnapshot snapshot)
        {
            ApplyCargoSpeed(snapshot);
        }

        private void ApplyCargoSpeed(InventorySnapshot snapshot)
        {
            playerMovement?.SetCargoSpeedMultiplier(
                CargoSpeedPolicy.Evaluate(snapshot.CurrentWeight, snapshot.MaxCapacity));
        }

        private void OnDestroy()
        {
            if (inventorySpeedBound && runtime?.InventoryService != null)
            {
                runtime.InventoryService.InventoryChanged -= OnInventoryChangedForMovement;
            }

            inventorySpeedBound = false;
            if (gasEffectController != null)
            {
                gasEffectController.FailureInputRaised -= OnGasFailureInputRaised;
                gasEffectController.EffectStateChanged -= OnGasEffectStateChanged;
            }
        }

        public void Publish(GameplayEventDto gameplayEvent)
        {
            var state = GameBootstrapper.Instance?.State;
            if (state == null || gameplayEvent == null)
            {
                return;
            }

            if (gameplayEvent.type == GameplayEventType.StructuralRiskChanged)
            {
                state.SetStructuralRisk(ToStructuralRisk(gameplayEvent.structuralIntegrity));
            }
            else if (gameplayEvent.type == GameplayEventType.GasTriggered)
            {
                // 효과 컨트롤러가 있으면 저항·전진기지 보호가 반영된 값만 RunState에 기록한다.
                if (gasEffectController == null)
                {
                    state.SetGasExposure(ToGasRisk(gameplayEvent.gasRisk));
                }
            }
            else if (gameplayEvent.type == GameplayEventType.OutpostStatusChanged)
            {
                gasEffectController?.ApplyOutpostStatus(gameplayEvent.outpostStatus);
            }

            eventFanOut?.Publish(gameplayEvent);
        }

        private SubTerra.Shared.IDroneContextProvider ResolveDroneProvider()
        {
            if (droneContextAdapter != null && droneContextAdapter.HasRequiredReferences())
            {
                return droneContextAdapter;
            }

            if (droneSensor != null)
            {
                return droneSensor;
            }

            return droneContextAdapter;
        }

        private void OnGasFailureInputRaised(GasExposureFailureInputDto input)
        {
            Publish(new GameplayEventDto
            {
                type = GameplayEventType.GasExposureThreshold,
                entityId = "player",
                instanceId = input?.gasZoneId ?? string.Empty,
                reasonId = "gas_exposure_threshold",
                gasExposureFailure = input
            });
        }

        private void OnGasEffectStateChanged(
            SubTerra.Gameplay.Hazards.GasExposureEffectState effect)
        {
            droneSensor?.SetAppliedGasRisk(effect.Risk);
        }

        private void SetHudVisible(bool visible)
        {
            if (hudCanvasGroup == null)
            {
                return;
            }

            hudCanvasGroup.alpha = visible ? 1f : 0f;
            hudCanvasGroup.interactable = visible;
            hudCanvasGroup.blocksRaycasts = visible;
        }

        private void SetDeferredInputEnabled(bool enabled)
        {
            if (deferredInputBehaviours == null)
            {
                return;
            }

            for (var i = 0; i < deferredInputBehaviours.Length; i++)
            {
                if (deferredInputBehaviours[i] != null)
                {
                    deferredInputBehaviours[i].enabled = enabled;
                }
            }
        }

        private static StructuralRiskLevel ToStructuralRisk(float integrity)
        {
            if (integrity <= 0.25f)
            {
                return StructuralRiskLevel.Critical;
            }

            return integrity <= 0.75f
                ? StructuralRiskLevel.Caution
                : StructuralRiskLevel.Safe;
        }

        private static GasRiskLevel ToGasRisk(float risk)
        {
            if (risk >= 0.7f)
            {
                return GasRiskLevel.Hazard;
            }

            return risk >= 0.3f
                ? GasRiskLevel.Elevated
                : GasRiskLevel.Safe;
        }
    }
}

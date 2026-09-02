using System;
using System.Collections;
using System.Collections.Generic;
using SubTerra.App.Core;
using SubTerra.App.Core.Data;
using SubTerra.App.Inventory;
using SubTerra.App.Outpost;
using SubTerra.App.Save;
using SubTerra.App.State;
using SubTerra.App.UI.HUD;
using SubTerra.App.UI.Inventory;
using SubTerra.App.UI.Outpost;
using SubTerra.App.UI.Progression;
using SubTerra.App.UI.Tutorial;
using SubTerra.Gameplay.Building;
using SubTerra.Gameplay.Drone;
using SubTerra.Gameplay.Integration;
using SubTerra.Gameplay.Mining;
using SubTerra.Gameplay.Player;
using SubTerra.Shared;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

namespace SubTerra.App.Integration
{
    /// <summary>
    /// Mine_Demo_Integration Scene 로컬 binder.
    /// Bootstrap 전역 서비스를 중복 생성하지 않고 Shared 5경계를 A Runtime에 연결한다.
    /// 이어하기 시 HUD/입력은 SaveRuntime이 월드 복원·파생 재계산을 끝낸 뒤에만 활성화한다.
    /// 씬 재생성·머지로 직렬화 참조가 비거나 stale 이어도 런타임 탐색으로 동일 환경을 복구한다.
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
        [SerializeField] private GameplayDepthStatusBridge depthBridge;
        [SerializeField] private DepthDarknessOverlayController depthDarknessOverlay;
        [SerializeField] private GasExposureEffectController gasEffectController;
        [SerializeField] private OutpostRuntimeBridge outpostBridge;
        [SerializeField] private GameplayEventBridge gameplayEventBridge;
        [SerializeField] private BuildingUiIntegrationBinder buildingUiBinder;
        [SerializeField] private InventoryPanelBinder inventoryPanelBinder;
        [SerializeField] private OutpostPanelBinder outpostPanelBinder;
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
        [SerializeField] private RunFailureRuntimeController runFailureController;
        private EmergencyRescueRuntimeController emergencyRescueController;

        private SaveRuntimeController runtime;
        private GameBootstrapper bootstrap;
        private OutpostService outpostService;
        private IntegrationEventFanOut eventFanOut;
        private IntegrationContractRegistry contracts;
        private IntegrationActivationGate activationGate;
        private bool contractsWired;
        private bool uiActivated;
        private bool inventorySpeedBound;
        private bool droneReadingsBound;

        public IntegrationContractRegistry Contracts => contracts;
        public IntegrationActivationGate ActivationGate => activationGate;
        public bool AreContractsWired => contractsWired;
        public bool IsUiActivated => uiActivated;

        private void Awake()
        {
            activationGate = new IntegrationActivationGate();
            contracts = new IntegrationContractRegistry();
            // 복원 전 HUD/입력을 끈다. 게이트가 열릴 때만 ActivateUi가 성공한다.
            // 참조가 비어 있어도 끄기 대상(HUD/입력)을 씬에서 먼저 찾는다.
            ResolveSceneReferences();
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
                    "[SubTerra] Integration scene opened without the Bootstrap runtime. " +
                    "Play from Bootstrap.unity (or enable Bootstrap Play Mode Start Scene) " +
                    "so HUD/input activate consistently on every machine.");
                yield break;
            }

            // 개별 바인딩 실패가 Start 코루틴을 죽여 HUD/입력이 영구 비활성 되는 것을 막는다.
            try
            {
                WireContracts();
            }
            catch (Exception ex)
            {
                Debug.LogError(
                    "[SubTerra] WireContracts failed; continuing toward UI activation if ready. " +
                    ex);
            }

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
            if (activationGate != null && !activationGate.IsDerivedRecalculated)
            {
                activationGate.MarkReadyForNewSession();
            }

            try
            {
                ActivateUi();
            }
            catch (Exception ex)
            {
                Debug.LogError(
                    "[SubTerra] ActivateUi failed; forcing HUD/input enable as last resort. " +
                    ex);
                ForceEnableHudAndInput();
            }
        }

        /// <summary>
        /// 직렬화 참조가 null/missing/stale 이어도 씬 내 동일 타입 컴포넌트로 채운다.
        /// 작업자·머지 결과와 무관하게 Integration 진입 환경을 맞춘다.
        /// </summary>
        public void ResolveSceneReferences()
        {
            buildingPlacementSystem = Resolve(buildingPlacementSystem);
            hudBinder = Resolve(hudBinder);
            hazardBridge = Resolve(hazardBridge);
            depthBridge = Resolve(depthBridge);
            depthDarknessOverlay = Resolve(depthDarknessOverlay, FindObjectsInactive.Include);
            gasEffectController = Resolve(gasEffectController);
            outpostBridge = Resolve(outpostBridge);
            gameplayEventBridge = Resolve(gameplayEventBridge);
            buildingUiBinder = Resolve(buildingUiBinder);
            inventoryPanelBinder = Resolve(inventoryPanelBinder, FindObjectsInactive.Include);
            outpostPanelBinder = Resolve(outpostPanelBinder, FindObjectsInactive.Include);
            progressionPanelBinder = Resolve(progressionPanelBinder, FindObjectsInactive.Include);
            placementBridge = Resolve(placementBridge);
            droneContextAdapter = Resolve(droneContextAdapter);
            droneSensor = Resolve(droneSensor);
            tutorialDirector = Resolve(tutorialDirector, FindObjectsInactive.Include);
            miningSystem = Resolve(miningSystem);
            playerMovement = Resolve(playerMovement);
            miningProgressHud = Resolve(miningProgressHud, FindObjectsInactive.Include);
            runFailureController = Resolve(runFailureController);
            emergencyRescueController = Resolve(emergencyRescueController, FindObjectsInactive.Include);

            if (worldSnapshotProviderBehaviour == null)
            {
                worldSnapshotProviderBehaviour = FindWorldSnapshotProviderBehaviour();
            }

            if (hudCanvasGroup == null)
            {
                hudCanvasGroup = ResolveHudCanvasGroup();
            }

            ResolveDeferredInputBehaviours();
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

            ResolveSceneReferences();

            TryStep("EnsureGameplayServices", () => runtime.EnsureGameplayServices());
            TryStep(
                "BindGameState",
                () =>
                {
                    if (runtime.InventoryService != null)
                    {
                        runtime.InventoryService.BindGameState(bootstrap.State);
                    }
                });

            TryStep(
                "MiningRuntimeServices",
                () =>
                {
                    if (miningSystem != null)
                    {
                        miningSystem.SetRuntimeServices(
                            this,
                            runtime.Progression != null ? runtime.Progression.Effects : null,
                            runtime);
                        miningSystem.SetCellProtectionPredicate(
                            buildingPlacementSystem != null
                                ? buildingPlacementSystem.IsGroundSupportingBuilding
                                : null);
                    }
                });

            TryStep("BindCargoSpeed", BindCargoSpeed);
            TryStep("BindDroneReadings", BindDroneReadings);

            // IResourceWallet: A BuildingPlacementSystem → B EconomyService
            TryStep(
                "ResourceWallet",
                () =>
                {
                    if (buildingPlacementSystem != null && runtime.Economy != null)
                    {
                        buildingPlacementSystem.SetResourceWallet(runtime.Economy);
                    }
                });

            // 전진기지 Consumer (Scene 로컬 서비스, 전역 중복 생성 아님)
            TryStep(
                "OutpostService",
                () =>
                {
                    if (outpostBridge == null || runtime.InventoryService == null)
                    {
                        return;
                    }

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
                });

            TryStep(
                "HazardBridge",
                () =>
                {
                    if (hazardBridge != null)
                    {
                        hazardBridge.BindGameState(bootstrap.State);
                    }
                });

            // 플레이어 Y → Run.Depth → HUD 깊이 텍스트 실시간 반영.
            TryStep("DepthBridge", BindDepthBridge);
            TryStep("DepthDarkness", BindDepthDarkness);
            TryStep("FacilityProximityLabel", EnsureFacilityProximityLabel);

            TryStep(
                "GasEffect",
                () =>
                {
                    if (gasEffectController == null)
                    {
                        return;
                    }

                    gasEffectController.FailureInputRaised += OnGasFailureInputRaised;
                    gasEffectController.EffectStateChanged += OnGasEffectStateChanged;
                    gasEffectController.Bind(
                        bootstrap.State,
                        runtime.Progression != null ? runtime.Progression.Effects : null);
                });

            TryStep(
                "RunFailure",
                () =>
                {
                    if (runFailureController == null)
                    {
                        return;
                    }

                    runFailureController.Bind(runtime, bootstrap.State);
                    if (gameplayEventBridge != null)
                    {
                        gameplayEventBridge.SetCollapseDamageReceiver(
                            runFailureController.SurvivalController);
                    }
                    if (hudBinder != null)
                    {
                        hudBinder.BindHealthSource(runFailureController.SurvivalController);
                    }
                    runFailureController.PlayerRescued += OnPlayerRescued;
                });

            TryStep(
                "EmergencyRescue",
                () =>
                {
                    if (emergencyRescueController == null)
                    {
                        emergencyRescueController = gameObject.AddComponent<EmergencyRescueRuntimeController>();
                    }

                    emergencyRescueController.Bind(
                        runtime,
                        bootstrap.State,
                        playerMovement != null ? playerMovement.transform : null,
                        hudBinder);
                });

            var dataCatalog = bootstrap.AssignedCatalog as GameDataCatalog;
            TryStep(
                "PlacementBridge",
                () =>
                {
                    if (placementBridge != null && runtime.Economy != null)
                    {
                        placementBridge.BindWallet(runtime.Economy, dataCatalog);
                    }
                });

            TryStep(
                "BuildingUi",
                () =>
                {
                    if (buildingUiBinder != null)
                    {
                        buildingUiBinder.BindTo(
                            runtime.Economy,
                            runtime.InventoryService,
                            bootstrap.State);
                    }
                });

            // IDroneContextProvider: A DroneSensor → B adapter
            TryStep(
                "DroneAdapter",
                () =>
                {
                    if (droneContextAdapter != null && droneSensor != null)
                    {
                        droneContextAdapter.BindTo(droneSensor);
                    }
                });

            // UnityEngine.Object의 null 조건부 연산자는 파괴된 객체를 걸러내지 못한다.
            // 씬 레이아웃 재생성으로 직렬화 참조가 stale 상태여도 활성화가 계속되게 한다.
            TryStep(
                "InventoryPanel",
                () =>
                {
                    if (inventoryPanelBinder != null && runtime.InventoryService != null)
                    {
                        inventoryPanelBinder.BindTo(runtime.InventoryService);
                    }
                });

            TryStep(
                "ProgressionPanel",
                () =>
                {
                    if (progressionPanelBinder == null)
                    {
                        return;
                    }

                    progressionPanelBinder.BindTo(
                        runtime.Progression,
                        () => bootstrap != null
                            && bootstrap.State != null
                            && bootstrap.State.Progress != null
                            ? bootstrap.State.Progress.CompletedObjectives
                            : 0);
                });

            // 인벤토리 변경 시 업그레이드 구매 가능 표시를 즉시 갱신한다.
            TryStep(
                "InventoryProgressionHook",
                () =>
                {
                    if (runtime.InventoryService == null)
                    {
                        return;
                    }

                    runtime.InventoryService.InventoryChanged -= OnInventoryChangedForProgression;
                    runtime.InventoryService.InventoryChanged += OnInventoryChangedForProgression;
                });

            TryStep(
                "DroneUpgradeEffects",
                () =>
                {
                    if (droneSensor != null && runtime.Progression != null)
                    {
                        droneSensor.SetUpgradeEffects(runtime.Progression.Effects);
                    }
                });

            eventFanOut = new IntegrationEventFanOut();
            if (hazardBridge != null)
            {
                eventFanOut.Add(hazardBridge);
            }

            if (outpostBridge != null)
            {
                eventFanOut.Add(outpostBridge);
            }

            if (runFailureController != null)
            {
                eventFanOut.Add(runFailureController);
            }

            if (tutorialDirector == null)
            {
                tutorialDirector = Resolve<TutorialDirectorBinder>(null);
            }

            TryStep(
                "TutorialDirector",
                () =>
                {
                    if (tutorialDirector == null)
                    {
                        return;
                    }

                    eventFanOut.Add(tutorialDirector);
                    tutorialDirector.BindTo(
                        bootstrap.State,
                        runtime.InventoryService,
                        runtime.Economy,
                        runtime.Progression,
                        outpostService);
                });

            // GameplayEventBridge가 초기 전력 스냅샷을 발행하기 전에 모든
            // 전진기지 소비자와 팬아웃을 준비한다. SetInteractionOrigin은
            // 준비가 끝난 뒤 현재 거리 상태를 즉시 다시 발행한다.
            if (gameplayEventBridge == null)
            {
                gameplayEventBridge = Resolve<GameplayEventBridge>(null);
            }

            TryStep(
                "EventSink",
                () =>
                {
                    if (gameplayEventBridge != null)
                    {
                        gameplayEventBridge.SetEventSink(this);
                    }
                });

            TryStep("OutpostPanelUi", BindOutpostPanelUi);

            var worldProvider = worldSnapshotProviderBehaviour as IWorldSnapshotProvider
                ?? runtime.Resolve();

            TryStep(
                "ContractsBind",
                () =>
                {
                    contracts.Bind(
                        miningRewardReceiver: this,
                        resourceWallet: runtime.Economy,
                        gameplayEventSink: this,
                        worldSnapshotProvider: worldProvider,
                        droneContextProvider: ResolveDroneProvider());
                });

            // 부분 실패가 있어도 게이트·UI 활성은 계속 진행한다. 환경 차이를 줄이기 위함.
            contractsWired = true;
            if (activationGate == null)
            {
                activationGate = new IntegrationActivationGate();
            }

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

            ResolveSceneReferences();

            if (hudBinder != null && bootstrap != null)
            {
                try
                {
                    hudBinder.BindTo(bootstrap.State);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("[SubTerra] HudBinder.BindTo failed: " + ex.Message);
                }
            }

            // I 키 인벤토리 패널: 전역 InventoryService에 구독만 연결 (중복 생성 없음).
            try
            {
                BindInventoryPanelUi();
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[SubTerra] BindInventoryPanelUi failed: " + ex.Message);
            }

            if (miningProgressHud != null)
            {
                try
                {
                    miningProgressHud.BindTo(
                        miningSystem,
                        playerMovement != null ? playerMovement.transform : null);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("[SubTerra] MiningProgressHud.BindTo failed: " + ex.Message);
                }
            }

            SetHudVisible(true);
            SetDeferredInputEnabled(true);
            uiActivated = true;
            return true;
        }

        private void BindInventoryPanelUi()
        {
            if (runtime == null || runtime.InventoryService == null)
            {
                return;
            }

            if (inventoryPanelBinder == null)
            {
                inventoryPanelBinder = Resolve<InventoryPanelBinder>(null, FindObjectsInactive.Include);
            }

            if (inventoryPanelBinder != null)
            {
                inventoryPanelBinder.BindTo(runtime.InventoryService);
            }
        }

        private void BindOutpostPanelUi()
        {
            if (outpostService == null)
            {
                return;
            }

            if (gameplayEventBridge == null)
            {
                gameplayEventBridge = Resolve<GameplayEventBridge>(null);
            }

            var elevator = Resolve<ElevatorController>(null);
            if (gameplayEventBridge != null)
            {
                gameplayEventBridge.SetElevatorPowerOrigin(
                    elevator != null ? elevator.transform : null);
                gameplayEventBridge.SetInteractionOrigin(
                    playerMovement != null ? playerMovement.transform : null);
            }

            if (outpostPanelBinder == null)
            {
                outpostPanelBinder = Resolve<OutpostPanelBinder>(null, FindObjectsInactive.Include);
            }

            if (outpostPanelBinder != null)
            {
                outpostPanelBinder.SetPrimaryInteractionClaim(
                    () => elevator != null && elevator.TryClaimInteractionPriority());
                outpostPanelBinder.BindTo(outpostService);
            }
        }

        public void AddMineral(string mineralId, int quantity)
        {
            if (runtime == null)
            {
                runtime = SaveRuntimeController.Instance;
            }

            if (runtime != null && runtime.InventoryService != null)
            {
                runtime.InventoryService.AddMineral(mineralId, quantity);
            }
        }

        public bool CanAffordEnergy(int energyCost)
        {
            var state = bootstrap != null
                ? bootstrap.State
                : GameBootstrapper.Instance != null
                    ? GameBootstrapper.Instance.State
                    : null;
            return state != null && state.Player.Energy >= Mathf.Max(0, energyCost);
        }

        public MiningCommitResult TryCommitMining(
            string mineralId,
            int quantity,
            int energyCost)
        {
            if (runtime == null)
            {
                runtime = SaveRuntimeController.Instance;
            }

            if (bootstrap == null)
            {
                bootstrap = GameBootstrapper.Instance;
            }

            var inventory = runtime != null ? runtime.InventoryService : null;
            var state = bootstrap != null ? bootstrap.State : null;
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
            if (inventorySpeedBound
                || runtime == null
                || runtime.InventoryService == null
                || playerMovement == null)
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
            if (playerMovement == null || snapshot == null)
            {
                return;
            }

            playerMovement.SetCargoSpeedMultiplier(
                CargoSpeedPolicy.Evaluate(snapshot.CurrentWeight, snapshot.MaxCapacity));
            playerMovement.SetCargoJumpMultiplier(
                CargoLoadEffectPolicy.EvaluateJumpMultiplier(
                    snapshot.CurrentWeight,
                    snapshot.MaxCapacity));

            var survival = runFailureController != null
                ? runFailureController.SurvivalController
                : null;
            if (survival != null)
            {
                survival.SetCargoFallImpactMultiplier(
                    CargoLoadEffectPolicy.EvaluateFallImpactMultiplier(
                        snapshot.CurrentWeight,
                        snapshot.MaxCapacity));
            }
        }

        private void OnInventoryChangedForProgression(InventorySnapshot _)
        {
            if (progressionPanelBinder == null || progressionPanelBinder.Presenter == null)
            {
                return;
            }

            progressionPanelBinder.Presenter.Refresh();
        }

        private void OnDestroy()
        {
            if (inventorySpeedBound && runtime != null && runtime.InventoryService != null)
            {
                runtime.InventoryService.InventoryChanged -= OnInventoryChangedForMovement;
            }

            if (runtime != null && runtime.InventoryService != null)
            {
                runtime.InventoryService.InventoryChanged -= OnInventoryChangedForProgression;
            }

            inventorySpeedBound = false;
            if (droneReadingsBound && bootstrap != null && bootstrap.State != null)
            {
                bootstrap.State.EnergyChanged -= OnEnergyChangedForDrone;
            }

            if (droneReadingsBound && runtime != null && runtime.InventoryService != null)
            {
                runtime.InventoryService.InventoryChanged -= OnInventoryChangedForDrone;
            }

            droneReadingsBound = false;
            if (gasEffectController != null)
            {
                gasEffectController.FailureInputRaised -= OnGasFailureInputRaised;
                gasEffectController.EffectStateChanged -= OnGasEffectStateChanged;
            }

            if (runFailureController != null)
            {
                if (gameplayEventBridge != null)
                {
                    gameplayEventBridge.SetCollapseDamageReceiver(null);
                }
                if (hudBinder != null)
                {
                    hudBinder.BindHealthSource(null);
                }
                runFailureController.PlayerRescued -= OnPlayerRescued;
                runFailureController.Unbind();
            }

            if (emergencyRescueController != null)
            {
                emergencyRescueController.Unbind();
            }
        }

        /// <summary>
        /// 시설 근접 시설명 말풍선. 씬에 없어도 ApplicationRoot에 런타임 생성한다.
        /// </summary>
        private void EnsureFacilityProximityLabel()
        {
            var controller = GetComponent<FacilityProximityLabelController>();
            if (controller == null)
            {
                controller = Resolve<FacilityProximityLabelController>(null);
            }

            if (controller == null)
            {
                controller = gameObject.AddComponent<FacilityProximityLabelController>();
            }

            if (playerMovement != null)
            {
                controller.SetPlayer(playerMovement.transform);
            }

            // 런타임 생성 말풍선이 LiberationSans로 한글이 깨지지 않게 HUD 한글 폰트를 넘긴다.
            var koreanFont = FacilityProximityLabelController.ResolveKoreanFont();
            if (FacilityProximityLabelController.IsKoreanFont(koreanFont))
            {
                controller.SetFont(koreanFont);
            }
        }

        /// <summary>
        /// 플레이어 위치를 깊이(m)로 변환해 GameState에 연결한다.
        /// Scene에 브리지가 없으면 ApplicationRoot에 런타임 생성한다.
        /// </summary>
        private void BindDepthBridge()
        {
            if (bootstrap == null || bootstrap.State == null)
            {
                return;
            }

            if (depthBridge == null)
            {
                depthBridge = Resolve<GameplayDepthStatusBridge>(null);
            }

            if (depthBridge == null)
            {
                depthBridge = gameObject.AddComponent<GameplayDepthStatusBridge>();
            }

            // DroneSensor와 동일한 지표면 기준을 사용해 드론 문맥·HUD 깊이를 맞춘다.
            if (droneSensor != null)
            {
                depthBridge.SetSurfaceY(droneSensor.SurfaceY);
            }

            if (playerMovement != null)
            {
                depthBridge.SetPlayer(playerMovement.transform);
            }

            depthBridge.BindGameState(bootstrap.State);
        }

        private void BindDepthDarkness()
        {
            if (bootstrap == null || bootstrap.State == null || playerMovement == null)
            {
                return;
            }

            if (depthDarknessOverlay == null)
            {
                depthDarknessOverlay = Resolve<DepthDarknessOverlayController>(
                    null,
                    FindObjectsInactive.Include);
            }

            if (depthDarknessOverlay == null)
            {
                if (hudCanvasGroup == null)
                {
                    hudCanvasGroup = ResolveHudCanvasGroup();
                }

                if (hudCanvasGroup == null)
                {
                    return;
                }

                var overlayObject = new GameObject(
                    "DepthDarknessOverlay",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image),
                    typeof(CanvasGroup));
                var rect = overlayObject.GetComponent<RectTransform>();
                rect.SetParent(hudCanvasGroup.transform, false);
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                rect.SetAsFirstSibling();
                depthDarknessOverlay = overlayObject.AddComponent<DepthDarknessOverlayController>();
            }

            depthDarknessOverlay.SetTerrainTilemap(FindForegroundTilemap());
            depthDarknessOverlay.SetDroneSensor(droneSensor);
            depthDarknessOverlay.Bind(bootstrap.State, playerMovement.transform);
        }

        private static Tilemap FindForegroundTilemap()
        {
            var maps = FindObjectsByType<Tilemap>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (var i = 0; i < maps.Length; i++)
            {
                var map = maps[i];
                if (map != null && map.name == DepthDarknessBlockVisual.ForegroundTilemapName)
                {
                    return map;
                }
            }

            return null;
        }

        private void BindDroneReadings()
        {
            if (droneReadingsBound
                || droneSensor == null
                || bootstrap == null
                || bootstrap.State == null
                || runtime == null
                || runtime.InventoryService == null)
            {
                return;
            }

            bootstrap.State.EnergyChanged += OnEnergyChangedForDrone;
            runtime.InventoryService.InventoryChanged += OnInventoryChangedForDrone;
            droneReadingsBound = true;
            SyncDroneReadings(runtime.InventoryService.GetSnapshot());
        }

        private void OnEnergyChangedForDrone(EnergyReadModel _)
        {
            if (runtime != null && runtime.InventoryService != null)
            {
                SyncDroneReadings(runtime.InventoryService.GetSnapshot());
            }
        }

        private void OnInventoryChangedForDrone(InventorySnapshot snapshot)
        {
            SyncDroneReadings(snapshot);
        }

        private void SyncDroneReadings(InventorySnapshot snapshot)
        {
            if (droneSensor == null || bootstrap == null || bootstrap.State == null || snapshot == null)
            {
                return;
            }

            droneSensor.SetAppStateReadings(
                bootstrap.State.Player.Energy,
                Mathf.RoundToInt(snapshot.UnsettledValue),
                snapshot.CurrentWeight,
                snapshot.MaxCapacity);
        }

        public void Publish(GameplayEventDto gameplayEvent)
        {
            var state = GameBootstrapper.Instance != null
                ? GameBootstrapper.Instance.State
                : null;
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
                if (gasEffectController != null)
                {
                    gasEffectController.ApplyOutpostStatus(gameplayEvent.outpostStatus);
                }
            }

            if (eventFanOut != null)
            {
                eventFanOut.Publish(gameplayEvent);
            }
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
                instanceId = input != null ? input.gasZoneId ?? string.Empty : string.Empty,
                reasonId = "gas_exposure_threshold",
                gasExposureFailure = input
            });
        }

        private void OnGasEffectStateChanged(
            SubTerra.Gameplay.Hazards.GasExposureEffectState effect)
        {
            if (droneSensor != null)
            {
                droneSensor.SetAppliedGasRisk(effect.Risk);
            }

            // 실제 가스 노출과 활성 전진기지 보호가 동시에 확정된 경우만 퀘스트에 전달한다.
            if (effect.IsExposed && effect.IsSheltered && eventFanOut != null)
            {
                eventFanOut.Publish(new GameplayEventDto
                {
                    type = GameplayEventType.GasPurified,
                    entityId = "player",
                    instanceId = effect.GasZoneId,
                    reasonId = "outpost_shelter"
                });
            }
        }

        private void OnPlayerRescued(PlayerRescueResultDto rescue)
        {
            if (rescue == null)
            {
                return;
            }

            if (eventFanOut != null)
            {
                eventFanOut.Publish(new GameplayEventDto
                {
                    type = GameplayEventType.PlayerRescued,
                    entityId = "player",
                    instanceId = rescue.failureToken,
                    reasonId = rescue.cause.ToString(),
                    x = rescue.returnX,
                    y = rescue.returnY,
                    playerRescue = rescue
                });
            }
        }

        private void SetHudVisible(bool visible)
        {
            if (hudCanvasGroup == null)
            {
                hudCanvasGroup = ResolveHudCanvasGroup();
            }

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
            ResolveDeferredInputBehaviours();
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

        /// <summary>
        /// ActivateUi 본문 예외 시에도 최소한 HUD/입력은 동일하게 켠다.
        /// 게이트를 우회하지 않은 경로에서만 호출한다(게이트는 이미 통과했거나 실패 복구).
        /// </summary>
        private void ForceEnableHudAndInput()
        {
            ResolveSceneReferences();
            SetHudVisible(true);
            SetDeferredInputEnabled(true);
            uiActivated = true;
        }

        private void ResolveDeferredInputBehaviours()
        {
            if (HasAnyValidDeferredInput())
            {
                return;
            }

            var resolved = new List<Behaviour>(4);
            if (playerMovement == null)
            {
                playerMovement = Resolve<PlayerMovement>(null);
            }

            if (playerMovement != null)
            {
                resolved.Add(playerMovement);
            }

            var miningController = FindAnyObjectByType<PlayerMiningController>(
                FindObjectsInactive.Exclude);
            if (miningController != null)
            {
                resolved.Add(miningController);
            }

            var playerController = FindAnyObjectByType<PlayerController>(
                FindObjectsInactive.Exclude);
            if (playerController != null)
            {
                resolved.Add(playerController);
            }

            if (resolved.Count > 0)
            {
                deferredInputBehaviours = resolved.ToArray();
            }
        }

        private bool HasAnyValidDeferredInput()
        {
            if (deferredInputBehaviours == null || deferredInputBehaviours.Length == 0)
            {
                return false;
            }

            for (var i = 0; i < deferredInputBehaviours.Length; i++)
            {
                if (deferredInputBehaviours[i] != null)
                {
                    return true;
                }
            }

            return false;
        }

        private static CanvasGroup ResolveHudCanvasGroup()
        {
            var hudRoot = GameObject.Find("HUDCanvas");
            if (hudRoot != null)
            {
                var group = hudRoot.GetComponent<CanvasGroup>();
                if (group != null)
                {
                    return group;
                }
            }

            return FindAnyObjectByType<CanvasGroup>(FindObjectsInactive.Include);
        }

        private static MonoBehaviour FindWorldSnapshotProviderBehaviour()
        {
            var behaviours = FindObjectsByType<MonoBehaviour>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (var i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IWorldSnapshotProvider)
                {
                    return behaviours[i];
                }
            }

            return null;
        }

        /// <summary>
        /// UnityEngine.Object는 C# null 조건부(?. / ??=)가 파괴·Missing 객체를 걸러내지 못한다.
        /// Unity 오버로드 == null 로 판정한 뒤 씬에서 재탐색한다.
        /// </summary>
        private static T Resolve<T>(
            T current,
            FindObjectsInactive inactive = FindObjectsInactive.Exclude)
            where T : UnityEngine.Object
        {
            if (current != null)
            {
                return current;
            }

            return FindAnyObjectByType<T>(inactive);
        }

        private static void TryStep(string stepName, Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                Debug.LogWarning(
                    "[SubTerra] Integration wire step '" + stepName + "' failed: " + ex.Message);
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

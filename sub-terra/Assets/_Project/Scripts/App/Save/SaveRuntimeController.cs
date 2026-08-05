using System;
using System.Collections;
using System.IO;
using SubTerra.App.Core;
using SubTerra.App.Core.Data;
using SubTerra.App.Drone;
using SubTerra.App.Drone.Dialogue;
using SubTerra.App.Economy;
using SubTerra.App.Inventory;
using SubTerra.App.Outpost;
using SubTerra.App.Progression;
using SubTerra.App.State;
using SubTerra.App.UI.MainMenu;
using SubTerra.Shared;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SubTerra.App.Save
{
    /// <summary>
    /// Bootstrap 수명에 붙어 슬롯 선택, 이어하기, 자동 저장과 종료 저장을 실제 Scene에 연결한다.
    /// 세이브 원문과 전체 저장 경로는 로그로 남기지 않는다.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public sealed class SaveRuntimeController : MonoBehaviour,
        IRestoredStateReceiver,
        IWorldSnapshotResolver,
        IDerivedStateRecalculator,
        ILoadedUiGate
    {
        public static SaveRuntimeController Instance { get; private set; }

        [SerializeField] private DroneAnalysisSettings droneSettings;
        [SerializeField, Min(1f)] private float periodicDirtySeconds = 30f;

        private SaveService saveService;
        private LoadService loadService;
        private AutoSaveCoordinator autoSave;
        private AutoSaveEventBinder eventBinder;
        private InventoryState inventory;
        private UpgradeState upgrades;
        private InventoryService inventoryService;
        private EconomyService economy;
        private CraftingService crafting;
        private ProgressionService progression;
        private TemplateDialogueGenerator dialogueGenerator;
        private GameState boundState;
        private int activeSlot;
        private bool dirty;
        private bool pendingInitialSave;
        private bool uiReady;
        private bool saveInProgress;
        private float nextPeriodicSaveAt;
        private SaveResult lastSaveResult;
        private ContinueResult lastContinueResult;
        private readonly ExplorationStartGuard explorationGuard = new ExplorationStartGuard();
        private ElevatorTravelSession elevatorTravel;
        private RunLifecycleService runLifecycle;
        private OutpostStatusDto latestOutpostStatus;
        private string pendingInitialScene = SceneNames.SurfaceBase;

        public const int MineElevatorEnergyCost = 5;

        public LoadService Loader => loadService;
        public int ActiveSlot => activeSlot;
        public bool IsUiReady => uiReady;
        public bool IsDirty => dirty;
        public bool IsSaveInProgress => saveInProgress;
        public SaveResult LastSaveResult => lastSaveResult;
        public ContinueResult LastContinueResult => lastContinueResult;
        public InventoryState Inventory => inventory;
        public UpgradeState Upgrades => upgrades;
        public InventoryService InventoryService => inventoryService;
        public EconomyService Economy => economy;
        public CraftingService Crafting => crafting;
        public ProgressionService Progression => progression;
        public ExplorationStartGuard ExplorationGuard => explorationGuard;
        public ElevatorTravelState ElevatorState =>
            elevatorTravel?.State ?? ElevatorTravelState.Idle;
        public RunLifecyclePhase RunPhase => runLifecycle?.Phase ?? RunLifecyclePhase.Ready;
        public RunReturnTarget LastReturnTarget { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            var migrations = new SaveMigrationService();
            var paths = new SavePathPolicy(ResolveSaveRoot());
            var mapper = new SaveDataMapper(new SystemSaveClock());
            var json = new SaveJsonCodec(migrations);
            var fileSystem = new PhysicalSaveFileSystem();
            saveService = new SaveService(fileSystem, paths, mapper, json);
            loadService = new LoadService(fileSystem, paths, mapper, json);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private IEnumerator Start()
        {
            // GameBootstrapper.Start의 MainMenu 전환이 끝난 다음 기본 런타임 상태를 잡는다.
            yield return null;
            var bootstrap = GameBootstrapper.Instance;
            if (bootstrap != null && GameState.IsComplete(bootstrap.State))
            {
                ApplyRuntimeState(
                    bootstrap.State,
                    new InventoryState(),
                    new UpgradeState(),
                    null);
            }

#if UNITY_EDITOR || SUBTERRA_BUILD_DEVELOPMENT || SUBTERRA_BUILD_QA
            yield return RunDevelopmentSmokeCommand();
#endif
        }

        private void Update()
        {
            if (!dirty
                || autoSave == null
                || Time.unscaledTime < nextPeriodicSaveAt)
            {
                return;
            }

            dirty = false;
            nextPeriodicSaveAt = Time.unscaledTime + periodicDirtySeconds;
            _ = autoSave.RequestAsync(AutoSaveReason.PeriodicDirty);
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            UnbindStateDirtyEvents();
            eventBinder?.Dispose();
            autoSave?.Dispose();
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                SaveCurrent(AutoSaveReason.QuitRequested);
            }
        }

        private void OnApplicationQuit()
        {
            SaveCurrent(AutoSaveReason.QuitRequested);
        }

        /// <summary>
        /// 새 게임 시작. confirmOverwrite가 false이고 슬롯에 세이브가 있으면 거부한다.
        /// 기본 첫 Scene은 Surface Base이며, 탐사는 TryStartExploration으로만 진입한다.
        /// </summary>
        public bool StartNewGame(int slotId, bool confirmOverwrite = false)
        {
            if (!IsAllowedSlot(slotId))
            {
                return false;
            }

            var metadata = loadService.GetSlotMetadata(slotId);
            var eligibility = SlotContinuePolicy.FromMetadata(metadata);
            if (SlotContinuePolicy.RequiresOverwriteConfirm(eligibility) && !confirmOverwrite)
            {
                // 기존 슬롯 침묵 덮어쓰기 금지. UI 확인 후 confirmOverwrite=true로 재호출.
                return false;
            }

            var state = GameState.CreateNew();
            if (!ApplyRuntimeState(
                    state,
                    new InventoryState(),
                    new UpgradeState(),
                    null))
            {
                return false;
            }

            ActivateSlot(slotId);
            pendingInitialSave = true;
            pendingInitialScene = SceneNames.SurfaceBase;
            uiReady = false;
            explorationGuard.Reset();
            elevatorTravel?.Reset();
            if (!new UnitySceneLoader().Load(SceneNames.SurfaceBase))
            {
                pendingInitialSave = false;
                return false;
            }

            return true;
        }

        /// <summary>
        /// Surface Base에서 탐사(Integration/Mine)로 진입.
        /// 유효 슬롯·State가 없으면 로드하지 않고, 연타 시 Scene 로드는 한 번만 시도한다.
        /// </summary>
        public bool TryStartExploration(out string reason)
        {
            if (!TryElevatorTravel(
                SceneNames.Integration,
                MineElevatorEnergyCost,
                AutoSaveReason.Manual,
                out reason))
            {
                return false;
            }

            if (runLifecycle == null)
            {
                reason = "Run 수명주기 서비스가 준비되지 않았습니다.";
                return false;
            }

            return runLifecycle.TryBeginExploration(out reason);
        }

        /// <summary>Mine 정거장에서 Surface Base로 비상 귀환한다. 귀환에는 전력을 차감하지 않는다.</summary>
        public bool TryReturnToSurface(out string reason)
        {
            if (runLifecycle == null)
            {
                reason = "Run 수명주기 서비스가 준비되지 않았습니다.";
                return false;
            }

            if (!runLifecycle.TryPrepareNormalReturn(
                    latestOutpostStatus,
                    out var returnTarget,
                    out reason))
            {
                return false;
            }

            if (!TryElevatorTravel(
                SceneNames.SurfaceBase,
                0,
                AutoSaveReason.SurfaceReturn,
                out reason))
            {
                runLifecycle.AbortPendingReturn();
                return false;
            }

            if (!runLifecycle.CompleteNormalReturn(out reason))
            {
                runLifecycle.AbortPendingReturn();
                return false;
            }

            LastReturnTarget = returnTarget;
            return true;
        }

        public void ReportOutpostStatus(OutpostStatusDto status)
        {
            latestOutpostStatus = status;
        }

        /// <summary>Surface Base UI가 판매·제작·업그레이드 서비스를 쓸 수 있게 보장한다.</summary>
        public void EnsureGameplayServices()
        {
            if (boundState == null || inventory == null || upgrades == null)
            {
                return;
            }

            if (economy != null && progression != null && inventoryService != null)
            {
                return;
            }

            RebuildGameplayServices(boundState, inventory, upgrades);
        }

        public void RequestQuit()
        {
            if (saveInProgress)
            {
                return;
            }

            if (dirty && activeSlot > 0)
            {
                SaveCurrent(AutoSaveReason.QuitRequested);
            }

            // Editor Play Mode: Application.Quit만으로는 재생이 멈추지 않는다.
            // App 어셈블리는 UnityEditor를 참조하지 않으므로 리플렉션으로 isPlaying을 끈다.
            // Player 빌드: Application.Quit 경로.
            if (Application.isEditor)
            {
                StopEditorPlayMode();
            }
            else
            {
                Application.Quit();
            }
        }

        /// <summary>Editor 전용 종료. 플레이어 빌드에서는 no-op에 가깝다(타입 없음).</summary>
        private static void StopEditorPlayMode()
        {
            var editorApplication = System.Type.GetType(
                "UnityEditor.EditorApplication, UnityEditor");
            if (editorApplication == null)
            {
                Application.Quit();
                return;
            }

            var isPlaying = editorApplication.GetProperty(
                "isPlaying",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            if (isPlaying != null && isPlaying.CanWrite)
            {
                isPlaying.SetValue(null, false);
                return;
            }

            Application.Quit();
        }

        /// <summary>
        /// Unity Scene 활성화 완료를 기다린 뒤 월드를 복원하는 실제 런타임 이어하기 경로.
        /// </summary>
        public void BeginContinue(
            int slotId,
            Action<ContinueResult> completed = null)
        {
            StartCoroutine(ContinueRoutine(slotId, completed));
        }

        public ContinueResult Continue(int slotId)
        {
            if (!IsAllowedSlot(slotId))
            {
                lastContinueResult = new ContinueResult(
                    ContinueStatus.LoadFailed,
                    new LoadResult(LoadStatus.InvalidSlot, slotId));
                return lastContinueResult;
            }

            var service = new ContinueService(
                loadService,
                this,
                new UnitySceneLoader(),
                this,
                this,
                this);
            lastContinueResult = service.Continue(slotId);
            if (lastContinueResult.IsSuccess)
            {
                ActivateSlot(slotId);
                dirty = false;
            }

            return lastContinueResult;
        }

        public SaveResult SaveCurrent(AutoSaveReason reason = AutoSaveReason.Manual)
        {
            if (activeSlot == 0)
            {
                return new SaveResult(SaveStatus.InvalidSlot, activeSlot);
            }

            saveInProgress = true;
            try
            {
                var context = CaptureContext();
                lastSaveResult = saveService.Save(activeSlot, context);
                if (lastSaveResult.IsSuccess)
                {
                    dirty = false;
                }

                return lastSaveResult;
            }
            finally
            {
                saveInProgress = false;
            }
        }

        /// <summary>경제·진행·전진기지 성공 이벤트를 현재 슬롯 자동 저장에 연결한다.</summary>
        public void BindAutoSaveEvents(
            EconomyService economy = null,
            ProgressionService progression = null,
            OutpostService outpost = null)
        {
            eventBinder?.Dispose();
            eventBinder = autoSave == null
                ? null
                : new AutoSaveEventBinder(autoSave, economy, progression, outpost);
        }

        public void NotifyAutoSave(AutoSaveReason reason)
        {
            if (autoSave != null)
            {
                dirty = false;
                _ = autoSave.RequestAsync(reason);
            }
        }

        public bool RestoreBState(RestoredSaveState state)
        {
            return state != null
                && ApplyRuntimeState(
                    state.GameState,
                    state.Inventory,
                    state.Upgrades,
                    state.Drone);
        }

        public IWorldSnapshotProvider Resolve()
        {
            var behaviours = FindObjectsByType<MonoBehaviour>(
                FindObjectsInactive.Exclude);
            for (var i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IWorldSnapshotProvider provider)
                {
                    return provider;
                }
            }

            return null;
        }

        public bool Recalculate()
        {
            // A WorldSnapshotSystem.RestoreSnapshot이 전력망 재계산을 요청한 뒤 반환한다.
            return Resolve() != null;
        }

        public void SetReady(bool ready)
        {
            uiReady = ready;
        }

        private bool ApplyRuntimeState(
            GameState state,
            InventoryState restoredInventory,
            UpgradeState restoredUpgrades,
            DroneSaveData restoredDrone)
        {
            var bootstrap = GameBootstrapper.Instance;
            if (bootstrap == null
                || !bootstrap.TryReplaceState(state)
                || restoredInventory == null
                || restoredUpgrades == null)
            {
                return false;
            }

            UnbindStateDirtyEvents();
            inventory = restoredInventory;
            upgrades = restoredUpgrades;
            elevatorTravel = new ElevatorTravelSession(state);
            runLifecycle = new RunLifecycleService(state);
            latestOutpostStatus = null;
            LastReturnTarget = default;
            dialogueGenerator = CreateDialogueGenerator();
            if (restoredDrone != null
                && !DroneSaveRestorer.TryRestore(restoredDrone, dialogueGenerator))
            {
                return false;
            }

            RebuildGameplayServices(state, restoredInventory, restoredUpgrades);
            BindStateDirtyEvents(state);
            return true;
        }

        private void RebuildGameplayServices(
            GameState state,
            InventoryState inventoryState,
            UpgradeState upgradeState)
        {
            eventBinder?.Dispose();
            eventBinder = null;

            var catalog = GameBootstrapper.Instance?.AssignedCatalog as GameDataCatalog;
            IMineralCatalogLookup mineralLookup = catalog != null
                ? (IMineralCatalogLookup)new GameDataCatalogMineralLookup(catalog)
                : new InMemoryMineralCatalog();
            IUpgradeCatalog upgradeCatalog = catalog != null
                ? (IUpgradeCatalog)new GameDataUpgradeCatalog(catalog)
                : null;

            inventoryService = new InventoryService(mineralLookup, inventoryState, state);
            economy = new EconomyService(inventoryService, mineralLookup, state);
            crafting = new CraftingService(economy);
            progression = upgradeCatalog != null
                ? new ProgressionService(upgradeState, upgradeCatalog, economy)
                : null;

            if (autoSave != null)
            {
                BindAutoSaveEvents(economy, progression, null);
            }
        }

        private TemplateDialogueGenerator CreateDialogueGenerator()
        {
            if (droneSettings == null)
            {
                droneSettings = ScriptableObject.CreateInstance<DroneAnalysisSettings>();
            }

            var catalog =
                GameBootstrapper.Instance?.AssignedCatalog as GameDataCatalog;
            return new TemplateDialogueGenerator(
                catalog != null ? catalog.Dialogues : Array.Empty<DialogueTemplateData>(),
                new UnityRealtimeDroneClock(),
                droneSettings);
        }

        private SaveCaptureContext CaptureContext()
        {
            return new SaveCaptureContext(
                GameBootstrapper.Instance?.State,
                inventory,
                upgrades,
                dialogueGenerator,
                Resolve(),
                SceneManager.GetActiveScene().name,
                Application.version);
        }

        private bool TryElevatorTravel(
            string destinationScene,
            int energyCost,
            AutoSaveReason arrivalSaveReason,
            out string reason)
        {
            reason = string.Empty;
            if (activeSlot == 0 || boundState == null || !GameState.IsComplete(boundState))
            {
                reason = "유효한 슬롯과 게임 상태가 없어 엘리베이터를 이용할 수 없습니다.";
                return false;
            }

            elevatorTravel ??= new ElevatorTravelSession(boundState);
            if (!elevatorTravel.TryCall(
                    destinationScene,
                    energyCost,
                    isExitClear: true,
                    out var callFailure))
            {
                reason = DescribeElevatorFailure(callFailure);
                return false;
            }

            if (!elevatorTravel.TryDepart(new UnitySceneLoader(), out var departFailure))
            {
                reason = DescribeElevatorFailure(departFailure);
                return false;
            }

            // 목적지 Scene 시스템의 Start가 끝난 다음 프레임에 저장해 생성 seed와 위치를 함께 잡는다.
            StartCoroutine(SaveAfterElevatorArrival(arrivalSaveReason));
            return true;
        }

        private IEnumerator SaveAfterElevatorArrival(AutoSaveReason reason)
        {
            yield return null;
            if (activeSlot > 0)
            {
                NotifyAutoSave(reason);
            }
        }

        private static string DescribeElevatorFailure(ElevatorTravelFailure failure)
        {
            return failure switch
            {
                ElevatorTravelFailure.Busy => "엘리베이터가 이미 이동 중입니다.",
                ElevatorTravelFailure.InsufficientEnergy => "엘리베이터 전력이 부족합니다. (필요 전력 5)",
                ElevatorTravelFailure.BlockedExit => "도착 지점이 막혀 이동할 수 없습니다.",
                ElevatorTravelFailure.SceneLoadFailed => "목적지 Scene 로드에 실패했습니다.",
                ElevatorTravelFailure.InvalidDestination => "엘리베이터 목적지가 올바르지 않습니다.",
                _ => "엘리베이터를 이용할 수 없습니다."
            };
        }

        private void ActivateSlot(int slotId)
        {
            eventBinder?.Dispose();
            eventBinder = null;
            autoSave?.Dispose();
            activeSlot = slotId;
            autoSave = new AutoSaveCoordinator(saveService, CaptureContext, slotId);
            nextPeriodicSaveAt = Time.unscaledTime + periodicDirtySeconds;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == SceneNames.SurfaceBase
                && GameState.IsComplete(boundState))
            {
                // 지상 기지는 다음 탐사를 준비하는 안전 구역이므로 도착 즉시 완충한다.
                boundState.SetCurrentEnergy(boundState.Player.MaxEnergy);
            }

            if (scene.name == SceneNames.Integration)
            {
                explorationGuard.Complete();
            }

            if (!pendingInitialSave || scene.name != pendingInitialScene)
            {
                return;
            }

            pendingInitialSave = false;
            uiReady = true;
            lastSaveResult = SaveCurrent(AutoSaveReason.Manual);
        }

        private IEnumerator ContinueRoutine(
            int slotId,
            Action<ContinueResult> completed)
        {
            uiReady = false;
            if (!IsAllowedSlot(slotId))
            {
                CompleteContinue(
                    new ContinueResult(
                        ContinueStatus.LoadFailed,
                        new LoadResult(LoadStatus.InvalidSlot, slotId)),
                    completed);
                yield break;
            }

            var load = loadService.Load(slotId);
            if (!load.IsSuccess || load.State == null)
            {
                CompleteContinue(
                    new ContinueResult(ContinueStatus.LoadFailed, load),
                    completed);
                yield break;
            }

            if (!RestoreBState(load.State))
            {
                CompleteContinue(
                    new ContinueResult(ContinueStatus.StateRestoreFailed, load),
                    completed);
                yield break;
            }

            if (!new UnitySceneLoader().Load(load.State.TargetSceneName))
            {
                CompleteContinue(
                    new ContinueResult(ContinueStatus.SceneLoadFailed, load),
                    completed);
                yield break;
            }

            const int maximumSceneWaitFrames = 300;
            var waitedFrames = 0;
            while (SceneManager.GetActiveScene().name != load.State.TargetSceneName
                && waitedFrames < maximumSceneWaitFrames)
            {
                waitedFrames++;
                yield return null;
            }

            if (SceneManager.GetActiveScene().name != load.State.TargetSceneName)
            {
                CompleteContinue(
                    new ContinueResult(ContinueStatus.SceneLoadFailed, load),
                    completed);
                yield break;
            }

            if (ContinueService.RequiresWorldRestore(load.State.TargetSceneName))
            {
                var world = Resolve();
                if (world == null)
                {
                    CompleteContinue(
                        new ContinueResult(ContinueStatus.WorldProviderMissing, load),
                        completed);
                    yield break;
                }

                try
                {
                    world.RestoreSnapshot(load.State.World);
                }
                catch
                {
                    CompleteContinue(
                        new ContinueResult(ContinueStatus.WorldRestoreFailed, load),
                        completed);
                    yield break;
                }

                // Integration binder 게이트: 월드 복원 완료 신호 (HUD는 아직 비활성).
                NotifyIntegrationWorldRestored();

                if (!Recalculate())
                {
                    CompleteContinue(
                        new ContinueResult(ContinueStatus.RecalculationFailed, load),
                        completed);
                    yield break;
                }

                NotifyIntegrationDerivedRecalculated();
            }
            else
            {
                // SurfaceBase 등 월드 복원 불필요 Scene은 게이트를 바로 통과 가능하게 둔다.
                NotifyIntegrationWorldRestored();
                NotifyIntegrationDerivedRecalculated();
            }

            ActivateSlot(slotId);
            dirty = false;
            uiReady = true;
            CompleteContinue(
                new ContinueResult(ContinueStatus.Success, load),
                completed);
        }

        /// <summary>
        /// Integration Scene binder에 복원 단계 완료를 알린다.
        /// App→Integration 순환 참조 없이 IIntegrationRestoreListener로 탐색한다.
        /// </summary>
        private static void NotifyIntegrationWorldRestored()
        {
            var behaviours = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude);
            for (var i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IIntegrationRestoreListener listener)
                {
                    listener.NotifyWorldRestored();
                }
            }
        }

        private static void NotifyIntegrationDerivedRecalculated()
        {
            var behaviours = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude);
            for (var i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IIntegrationRestoreListener listener)
                {
                    listener.NotifyDerivedRecalculated();
                }
            }
        }

        private void CompleteContinue(
            ContinueResult result,
            Action<ContinueResult> completed)
        {
            lastContinueResult = result;
            completed?.Invoke(result);
        }

        private void BindStateDirtyEvents(GameState state)
        {
            if (state == null)
            {
                return;
            }

            boundState = state;
            state.EnergyChanged += OnPersistentStateChanged;
            state.CreditsChanged += OnPersistentStateChanged;
            state.InventoryChanged += OnPersistentStateChanged;
            state.DepthChanged += OnPersistentStateChanged;
            state.StructuralRiskChanged += OnPersistentStateChanged;
            state.GasExposureChanged += OnPersistentStateChanged;
        }

        private void UnbindStateDirtyEvents()
        {
            var state = boundState;
            if (state == null)
            {
                return;
            }

            state.EnergyChanged -= OnPersistentStateChanged;
            state.CreditsChanged -= OnPersistentStateChanged;
            state.InventoryChanged -= OnPersistentStateChanged;
            state.DepthChanged -= OnPersistentStateChanged;
            state.StructuralRiskChanged -= OnPersistentStateChanged;
            state.GasExposureChanged -= OnPersistentStateChanged;
            boundState = null;
        }

        private void OnPersistentStateChanged<T>(T _)
        {
            dirty = true;
        }

        private static bool IsAllowedSlot(int slotId)
        {
            return slotId >= SavePathPolicy.MinimumSlot
                && slotId <= SavePathPolicy.MaximumSlot;
        }

        private static string ResolveSaveRoot()
        {
#if UNITY_EDITOR || SUBTERRA_BUILD_DEVELOPMENT || SUBTERRA_BUILD_QA
            var args = Environment.GetCommandLineArgs();
            for (var i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == "-subterra-save-root"
                    && !string.IsNullOrWhiteSpace(args[i + 1]))
                {
                    return Path.GetFullPath(args[i + 1]);
                }
            }
#endif
            return Application.persistentDataPath;
        }

#if UNITY_EDITOR || SUBTERRA_BUILD_DEVELOPMENT || SUBTERRA_BUILD_QA
        private IEnumerator RunDevelopmentSmokeCommand()
        {
            var command = GetCommandLineValue("-subterra-save-smoke");
            if (string.IsNullOrEmpty(command))
            {
                yield break;
            }

            var slotText = GetCommandLineValue("-subterra-save-slot");
            var slot = int.TryParse(slotText, out var parsed) ? parsed : 1;
            var success = false;
            if (command == "new")
            {
                // 스모크는 빈 슬롯 전제. 확인 플래그 true로 명시 덮어쓰기 경로를 허용한다.
                success = StartNewGame(slot, confirmOverwrite: true);
                var waitFrames = 0;
                while (success
                    && pendingInitialSave
                    && waitFrames < 300)
                {
                    waitFrames++;
                    yield return null;
                }

                success = success
                    && !pendingInitialSave
                    && lastSaveResult != null
                    && lastSaveResult.IsSuccess
                    && SceneManager.GetActiveScene().name == SceneNames.SurfaceBase;
                if (success)
                {
                    var state = GameBootstrapper.Instance.State;
                    state.SetGold(321);
                    state.SetDepth(12);
                    success = SaveCurrent().IsSuccess;
                }
            }
            else if (command == "continue")
            {
                ContinueResult result = null;
                var finished = false;
                BeginContinue(
                    slot,
                    value =>
                    {
                        result = value;
                        finished = true;
                    });
                var waitFrames = 0;
                while (!finished && waitFrames < 300)
                {
                    waitFrames++;
                    yield return null;
                }

                var state = GameBootstrapper.Instance?.State;
                success = finished
                    && result != null
                    && result.IsSuccess
                    && state != null
                    && state.Player.Gold == 321
                    && state.Run.Depth == 12;
            }

            yield return null;
            Application.Quit(success ? 0 : 2);
        }

        private static string GetCommandLineValue(string name)
        {
            var args = Environment.GetCommandLineArgs();
            for (var i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == name)
                {
                    return args[i + 1];
                }
            }

            return string.Empty;
        }
#endif
    }
}

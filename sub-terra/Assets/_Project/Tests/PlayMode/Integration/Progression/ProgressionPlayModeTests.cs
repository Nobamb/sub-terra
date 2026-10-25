using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using NUnit.Framework;
using SubTerra.App.Core;
using SubTerra.App.Core.Data;
using SubTerra.App.Integration;
using SubTerra.App.Inventory;
using SubTerra.App.Progression;
using SubTerra.App.Save;
using SubTerra.App.State;
using SubTerra.App.UI.Progression;
using SubTerra.Gameplay.Player;
using SubTerra.Shared;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;

namespace SubTerra.App.Tests.PlayMode.Integration.Progression
{
    /// <summary>구매 성공 프레임에 UI와 Gameplay Provider가 같은 레벨을 읽는지 검증.</summary>
    public sealed class ProgressionPlayModeTests
    {
        private sealed class Catalog : IUpgradeCatalog
        {
            private readonly UpgradeData[] entries;

            public Catalog(params UpgradeData[] data)
            {
                entries = data;
            }

            public IReadOnlyList<UpgradeData> Upgrades => entries;

            public bool TryGetUpgrade(string upgradeId, out UpgradeData found)
            {
                foreach (var entry in entries)
                {
                    if (entry != null && entry.Id == upgradeId)
                    {
                        found = entry;
                        return true;
                    }
                }

                found = null;
                return false;
            }
        }

        private sealed class Wallet : IResourceWallet
        {
            public int Copper = 3;

            public bool CanAfford(IReadOnlyList<ItemCostDto> costs)
            {
                return costs != null && costs.Count == 1 && Copper >= costs[0].Quantity;
            }

            public bool TrySpend(IReadOnlyList<ItemCostDto> costs)
            {
                if (!CanAfford(costs))
                {
                    return false;
                }

                Copper -= costs[0].Quantity;
                return true;
            }
        }

        private sealed class View : IProgressionPanelView, IProgressionPurchaseFeedbackView
        {
            public UpgradeSnapshot Selected;
            public UpgradeSnapshot SelectedAtFeedback;
            public string Message;
            public bool Busy;

            public void SetUpgradeList(IReadOnlyList<UpgradeSnapshot> upgrades) { }
            public void SetSelectedUpgrade(UpgradeSnapshot upgrade) => Selected = upgrade;
            public void SetPurchaseResult(string message, string detail) => Message = message;
            public void SetDeepZoneAccess(ZoneAccessResult access) { }
            public void SetBusy(bool busy) => Busy = busy;
            public void SetVisible(bool visible) { }
            public void OnPurchaseCompleted(ProgressionPurchaseResult result) => SelectedAtFeedback = Selected;
        }

        [Test]
        public void CargoYieldPurchase_UpdatesSelectionBeforeSuccessFeedback()
        {
            var data = ScriptableObject.CreateInstance<UpgradeData>();
            var view = new View();
            var presenter = new ProgressionPanelPresenter(view);
            try
            {
                data.EditorSet(DataIds.Upgrades.CargoYield, "채굴 수확량", 1,
                    new List<UpgradeLevelDefinition>
                    {
                        new UpgradeLevelDefinition(1, 1f,
                            new List<ItemCostEntry> { new ItemCostEntry(DataIds.Minerals.Copper, 1) },
                            new List<MineralBonusEntry> { new MineralBonusEntry(DataIds.Minerals.Copper, 1) })
                    });
                var wallet = new Wallet();
                var service = new ProgressionService(new UpgradeState(), new Catalog(data), wallet);
                presenter.Bind(service);
                Assert.That(presenter.SelectUpgrade(data.Id), Is.True);

                var result = presenter.RequestPurchase();

                Assert.That(result.IsSuccess, Is.True);
                Assert.That(view.SelectedAtFeedback.CurrentLevel, Is.EqualTo(1));
                Assert.That(view.Selected.CurrentLevel, Is.EqualTo(1));
                Assert.That(service.Effects.GetMiningYieldBonus(DataIds.Minerals.Copper), Is.EqualTo(1));
                Assert.That(wallet.Copper, Is.EqualTo(2));
            }
            finally
            {
                presenter.Unbind();
                Object.DestroyImmediate(data);
            }
        }

        [Test]
        public void PurchaseCompleted_ObservesRuntimeHealthAndRegeneration_WithoutSceneReload()
        {
            GameBootstrapper.ResetInstanceForTests();
            Assert.That(SaveRuntimeController.Instance, Is.Null);
            var bootstrapObject = new GameObject("Issue147_Bootstrap");
            var bootstrap = bootstrapObject.AddComponent<GameBootstrapper>();
            bootstrap.enabled = false;
            var state = GameState.CreateNew();
            bootstrap.TryReplaceState(state);
            var runtimeObject = new GameObject("Issue147_Runtime");
            var runtime = runtimeObject.AddComponent<SaveRuntimeController>();
            runtime.enabled = false;
            var host = new GameObject("Issue147_Survival");
            var survival = host.AddComponent<PlayerSurvivalController>();
            var controller = host.AddComponent<RunFailureRuntimeController>();
            var settings = ScriptableObject.CreateInstance<PlayerSurvivalSettings>();
            var health = CreateUpgrade(DataIds.Upgrades.MaximumHealth, 30f);
            var regeneration = CreateUpgrade(DataIds.Upgrades.HealthRegeneration, 0.3f);
            try
            {
                survival.Configure(settings, host.transform);
                var service = new ProgressionService(
                    new UpgradeState(), new Catalog(health, regeneration), new Wallet());
                typeof(SaveRuntimeController).GetField("progression", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(runtime, service);
                typeof(SaveRuntimeController).GetField("inventoryService", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(runtime, new InventoryService(new InMemoryMineralCatalog(), 50f, state));
                typeof(RunFailureRuntimeController).GetField("survivalController", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(controller, survival);
                controller.Bind(runtime, state);
                var completed = 0;
                service.PurchaseCompleted += result =>
                {
                    completed++;
                    Assert.That(result.IsSuccess, Is.True);
                    Assert.That(survival.State.MaximumHealth, Is.EqualTo(130));
                    if (result.UpgradeId == DataIds.Upgrades.HealthRegeneration)
                    {
                        Assert.That(survival.State.AdvanceRegeneration(1f), Is.True);
                        Assert.That(survival.State.Health, Is.EqualTo(120.3f).Within(0.001f));
                    }
                };

                Assert.That(service.TryPurchase(health.Id).IsSuccess, Is.True);
                Assert.That(survival.State.Health, Is.EqualTo(130f));
                survival.State.TryApplyDamage(RunFailureCause.Fall, 10, 0f, 0f, false, out _);
                Assert.That(service.TryPurchase(regeneration.Id).IsSuccess, Is.True);
                Assert.That(completed, Is.EqualTo(2));
            }
            finally
            {
                controller.Unbind();
                Object.DestroyImmediate(host);
                Object.DestroyImmediate(runtimeObject);
                Object.DestroyImmediate(bootstrapObject);
                Object.DestroyImmediate(settings);
                Object.DestroyImmediate(health);
                Object.DestroyImmediate(regeneration);
                GameBootstrapper.ResetInstanceForTests();
            }
        }

        private static UpgradeData CreateUpgrade(string id, float effect)
        {
            var data = ScriptableObject.CreateInstance<UpgradeData>();
            data.EditorSet(id, id, 1, new List<UpgradeLevelDefinition>
            {
                new UpgradeLevelDefinition(1, effect, new List<ItemCostEntry>
                {
                    new ItemCostEntry(DataIds.Minerals.Copper, 1)
                })
            });
            return data;
        }

        [UnityTest]
        public IEnumerator Issue211_MemoryContinueRebindsCurrentServiceAndUpdatesPurchaseImmediately()
        {
            using var env = new UiTestEnvironment();
            // 기존 격리 지상 로더로 시작하며 사용자 슬롯은 읽거나 쓰지 않는다.
            yield return env.LoadSurfaceBase(() =>
                Object.FindAnyObjectByType<SubTerra.App.UI.SurfaceBase.SurfaceBaseBinder>()?.IsBound == true,
                sceneLoadTimeout: 90f, configureResolution: false);
            var runtime = env.Save;
            Assert.That(runtime.ActiveSlot, Is.Zero);
            var fs = new Issue211MemoryFileSystem();
            var paths = new SavePathPolicy(Path.Combine(Path.GetTempPath(), "subterra-issue211-memory"));
            var mapper = new SaveDataMapper(new SystemSaveClock());
            var codec = new SaveJsonCodec(new SaveMigrationService());
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            typeof(SaveRuntimeController).GetField("saveService", flags)
                .SetValue(runtime, new SaveService(fs, paths, mapper, codec));
            typeof(SaveRuntimeController).GetField("loadService", flags)
                .SetValue(runtime, new LoadService(fs, paths, mapper, codec));
            // 새 게임의 썸네일 삭제도 실제 사용자 경로에 닿지 않도록 격리한다.
            typeof(SaveRuntimeController).GetField("<Thumbnails>k__BackingField", flags)
                .SetValue(runtime, new SaveThumbnailService(paths));
            Assert.That(runtime.StartNewGame(1), Is.True);
            yield return UiTestWait.Until(SurfaceReady, "새 게임 지상 연결", 90f);
            GameBootstrapper.Instance.State.SetDemoProgress(SubTerra.App.Tutorial.DemoObjectiveIds.MineCopper, 1, false);
            GameBootstrapper.Instance.State.SetQuestRewardSettlement(string.Empty, 1);
            runtime.InventoryService.SetMaximumCapacity(10000f);
            Assert.That(runtime.InventoryService.TryAddMineral(DataIds.Minerals.Copper, 200).AcceptedQuantity, Is.EqualTo(200));
            Assert.That(runtime.InventoryService.TryAddMineral(DataIds.Minerals.Iron, 100).AcceptedQuantity, Is.EqualTo(100));
            PurchaseAndCheck(DataIds.Upgrades.DrillSpeed);
            Assert.That(runtime.SaveCurrent().IsSuccess, Is.True);
            var outgoingService = runtime.Progression;
            var outgoingHandle = SceneManager.GetActiveScene().handle;
            ContinueResult continued = null;
            runtime.BeginContinue(1, result => continued = result);
            yield return UiTestWait.Until(() => continued != null, "메모리 슬롯 이어하기", 90f);
            Assert.That(continued.IsSuccess, Is.True, continued.Status.ToString());
            yield return UiTestWait.Until(SurfaceReady, "이어하기 지상 연결", 90f);
            Assert.That(SceneManager.GetActiveScene().handle, Is.Not.EqualTo(outgoingHandle));
            Assert.That(runtime.Progression, Is.Not.SameAs(outgoingService));
            Assert.That(runtime.Progression.TryGetSnapshot(DataIds.Upgrades.DrillSpeed, out var restored), Is.True);
            Assert.That(restored.CurrentLevel, Is.EqualTo(1));
            PurchaseAndCheck(DataIds.Upgrades.DrillSpeed);
            PurchaseAndCheck(DataIds.Upgrades.MaximumEnergy);
            Assert.That(runtime.SaveCurrent().IsSuccess, Is.True);

            // 그래픽 없는 검사에서는 URP/CCTV 렌더만 제외한다. 씬·서비스·구매 경로는 유지한다.
            var disabledForHeadless = new List<Behaviour>();
            SceneManager.sceneLoaded += PrepareHeadlessScene;
            try
            {
                var serviceBeforeTravel = runtime.Progression;
                Object.FindAnyObjectByType<SubTerra.App.UI.SurfaceBase.SurfaceBaseView>().SetUpgradeVisible(false);
                Assert.That(runtime.TryStartExploration(out var enterReason), Is.True, enterReason);
                yield return UiTestWait.Until(() => SceneManager.GetActiveScene().name == SceneNames.Integration
                    && Object.FindAnyObjectByType<IntegrationRuntimeBinder>()?.IsUiActivated == true,
                    "광산 UI 연결", 90f);
                yield return UiTestEnvironment.DismissBriefing();
                Assert.That(runtime.Progression, Is.SameAs(serviceBeforeTravel));
                var mineOwner = Object.FindAnyObjectByType<IntegrationRuntimeBinder>();
                var minePanel = (ProgressionPanelBinder)typeof(IntegrationRuntimeBinder)
                    .GetField("progressionPanelBinder", flags).GetValue(mineOwner);
                Assert.That(minePanel.gameObject.activeInHierarchy, Is.False, "광산 업그레이드 창을 열지 않은 경로를 확인한다.");
                var minePresenter = minePanel.Presenter;
                Assert.That(minePresenter.IsBound, Is.True);
                Assert.That(runtime.TryReturnToSurface(out var returnReason), Is.True, returnReason);
                yield return UiTestWait.Until(SurfaceReady, "실제 귀환 후 지상 연결", 90f);
                Assert.That(minePresenter.IsBound, Is.False, "파괴된 비활성 창의 구독이 남으면 다음 구매 갱신을 중단시킨다.");
                Assert.That(runtime.Progression, Is.SameAs(serviceBeforeTravel));
                PurchaseAndCheck(DataIds.Upgrades.MaximumHealth);
            }
            finally
            {
                SceneManager.sceneLoaded -= PrepareHeadlessScene;
                foreach (var behaviour in disabledForHeadless)
                    if (behaviour != null) behaviour.enabled = true;
            }

            void PrepareHeadlessScene(Scene scene, LoadSceneMode mode)
            {
                if (SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null
                    || scene.name != SceneNames.Integration) return;
                foreach (var camera in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
                    if (camera.enabled)
                    {
                        disabledForHeadless.Add(camera);
                        camera.enabled = false;
                    }
                foreach (var binder in Object.FindObjectsByType<SubTerra.App.UI.EmergencyEscape.EmergencyEscapePanelBinder>(FindObjectsSortMode.None))
                    if (binder.enabled)
                    {
                        disabledForHeadless.Add(binder);
                        binder.enabled = false;
                    }
            }

            bool SurfaceReady() => SceneManager.GetActiveScene().name == SceneNames.SurfaceBase
                && Object.FindAnyObjectByType<SubTerra.App.UI.SurfaceBase.SurfaceBaseBinder>()?.IsBound == true
                && runtime.IsUiReady;

            void PurchaseAndCheck(string id)
            {
                var surface = Object.FindAnyObjectByType<SubTerra.App.UI.SurfaceBase.SurfaceBaseView>();
                var binder = surface.GetComponentInChildren<ProgressionPanelBinder>(true);
                var panel = binder.GetComponent<ProgressionPanelView>();
                surface.SetUpgradeVisible(true);
                Assert.That(binder.SelectUpgrade(id), Is.True);
                Assert.That(runtime.Progression.TryGetSnapshot(id, out var before), Is.True);
                var purchase = (UnityEngine.UI.Button)typeof(ProgressionPanelView).GetField("purchaseButton", flags).GetValue(panel);
                Assert.That(purchase.interactable, Is.True, id);
                typeof(ProgressionPanelBinder).GetField("tracePurchases", flags).SetValue(binder, true);
                LogAssert.Expect(LogType.Log, new System.Text.RegularExpressions.Regex(
                    @"\[UpgradePurchaseTrace\] phase=before .*currentService=True .*boundLevel=" + before.CurrentLevel));
                purchase.onClick.Invoke();
                Assert.That(runtime.Progression.LastPurchaseResult.IsSuccess, Is.True, id);
                Assert.That(runtime.Progression.TryGetSnapshot(id, out var after), Is.True);
                Assert.That(after.CurrentLevel, Is.EqualTo(before.CurrentLevel + 1), id);
                var label = (TMPro.TMP_Text)typeof(UpgradeTreeNodeView).GetField("levelLabel", flags)
                    .GetValue(panel.TreeView.FindNode(id));
                Assert.That(label.text, Is.EqualTo(after.IsMaximumLevel ? "MAX" : "Lv." + after.CurrentLevel + "/" + after.MaximumLevel));
                if (id == DataIds.Upgrades.MaximumEnergy)
                    Assert.That(GameBootstrapper.Instance.State.Player.MaxEnergy, Is.EqualTo(100 + (int)after.CurrentEffectValue));
                if (id == DataIds.Upgrades.MaximumHealth)
                    Assert.That(((IPlayerHealthUpgradeProvider)runtime.Progression.Effects).GetMaximumHealth(100),
                        Is.EqualTo(100 + (int)after.CurrentEffectValue));
            }
        }

        private sealed class Issue211MemoryFileSystem : ISaveFileSystem
        {
            private readonly Dictionary<string, string> files = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase);
            public bool FileExists(string path) => files.ContainsKey(path);
            public void CreateDirectory(string path) { }
            public void WriteAllText(string path, string contents) => files[path] = contents;
            public string ReadAllText(string path) => files[path];
            public void DeleteFile(string path) => files.Remove(path);
            public void MoveFile(string sourcePath, string destinationPath)
            {
                files[destinationPath] = files[sourcePath];
                files.Remove(sourcePath);
            }
        }

        [UnityTest]
        public IEnumerator Issue211_SurfaceScenePurchaseUpdatesLevelsAndEffectsWithoutLeaving()
        {
            using var env = new UiTestEnvironment();
            yield return env.LoadSurfaceBase(() =>
            {
                var surface = Object.FindAnyObjectByType<SubTerra.App.UI.SurfaceBase.SurfaceBaseBinder>();
                return surface != null && surface.IsBound;
            }, sceneLoadTimeout: 90f, configureResolution: false);
            var surfaceView = Object.FindAnyObjectByType<SubTerra.App.UI.SurfaceBase.SurfaceBaseView>();
            var binder = surfaceView.GetComponentInChildren<ProgressionPanelBinder>(true);
            var panel = binder.GetComponent<ProgressionPanelView>();
            Assert.That(binder.IsBound, Is.True);
            env.Save.InventoryService.SetMaximumCapacity(10000f);
            env.Save.InventoryService.TryAddMineral(DataIds.Minerals.Copper, 200);
            env.Save.InventoryService.TryAddMineral(DataIds.Minerals.Iron, 100);
            surfaceView.SetUpgradeVisible(true);
            var purchase = (UnityEngine.UI.Button)typeof(ProgressionPanelView)
                .GetField("purchaseButton", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(panel);
            var ids = new[] { DataIds.Upgrades.DrillSpeed, DataIds.Upgrades.DrillSpeed,
                DataIds.Upgrades.MaximumEnergy, DataIds.Upgrades.MaximumHealth,
                DataIds.Upgrades.MaximumCargo, DataIds.Upgrades.CargoYield };
            foreach (var id in ids)
            {
                Assert.That(binder.SelectUpgrade(id), Is.True);
                Assert.That(env.Save.Progression.TryGetSnapshot(id, out var before), Is.True);
                Assert.That(before.IsUnlocked, Is.True, before.LockedReason);
                Assert.That(purchase.interactable, Is.True, id);
                purchase.onClick.Invoke();
                Assert.That(env.Save.Progression.LastPurchaseResult.IsSuccess, Is.True, id);
                Assert.That(env.Save.Progression.TryGetSnapshot(id, out var after), Is.True);
                Assert.That(after.CurrentLevel, Is.EqualTo(before.CurrentLevel + 1), id);
                var node = panel.TreeView.FindNode(id);
                var label = (TMPro.TMP_Text)typeof(UpgradeTreeNodeView)
                    .GetField("levelLabel", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(node);
                Assert.That(label.text, Is.EqualTo(after.IsMaximumLevel ? "MAX" : "Lv." + after.CurrentLevel + "/" + after.MaximumLevel), id);
                if (id == DataIds.Upgrades.MaximumEnergy)
                    Assert.That(GameBootstrapper.Instance.State.Player.MaxEnergy, Is.EqualTo(100 + (int)after.CurrentEffectValue));
                if (id == DataIds.Upgrades.MaximumHealth)
                    Assert.That(((IPlayerHealthUpgradeProvider)env.Save.Progression.Effects).GetMaximumHealth(100),
                        Is.EqualTo(100 + (int)after.CurrentEffectValue));
                if (id == DataIds.Upgrades.CargoYield)
                    Assert.That(env.Save.Progression.Effects.GetMiningYieldBonus(DataIds.Minerals.Copper), Is.EqualTo(1));
                if (id == DataIds.Upgrades.MaximumCargo)
                    Assert.That(env.Save.InventoryService.MaxCapacity, Is.EqualTo(50f + after.CurrentEffectValue));
                yield return null;
            }
            surfaceView.SetUpgradeVisible(false);
            surfaceView.SetUpgradeVisible(true);
            Assert.That(binder.IsBound, Is.True);
            Assert.That(env.Save.ActiveSlot, Is.Zero);
        }

        [Test]
        public void Issue211_PurchaseTracePreservesSubscriberExceptionAndCommittedLevel()
        {
            var host = new GameObject("Issue211_DiagnosticBinder");
            var data = CreateUpgrade(DataIds.Upgrades.DrillSpeed, 0.25f);
            try
            {
                var binder = host.AddComponent<ProgressionPanelBinder>();
                var service = new ProgressionService(new UpgradeState(), new Catalog(data), new Wallet());
                binder.BindTo(service);
                Assert.That(binder.SelectUpgrade(data.Id), Is.True);
                typeof(ProgressionPanelBinder).GetField("tracePurchases", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(binder, true);
                var failure = new System.InvalidOperationException("검사용 구독자 예외");
                service.UpgradeChanged += _ => throw failure;
                LogAssert.Expect(LogType.Log, new System.Text.RegularExpressions.Regex(
                    @"\[UpgradePurchaseTrace\] phase=before .*boundLevel=0"));
                LogAssert.Expect(LogType.Log, new System.Text.RegularExpressions.Regex(
                    @"\[UpgradePurchaseTrace\] phase=exception .*boundLevel=1 .*outcome=InvalidOperationException"));
                Assert.That(Assert.Throws<System.InvalidOperationException>(() => binder.PurchaseSelected()), Is.SameAs(failure));
                Assert.That(service.TryGetSnapshot(data.Id, out var snapshot), Is.True);
                Assert.That(snapshot.CurrentLevel, Is.EqualTo(1));
                Assert.That(binder.Presenter.IsBusy, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(host);
                Object.DestroyImmediate(data);
            }
        }

        [UnityTest]
        public IEnumerator Purchase_UpdatesUiAndProviderInSameFrame()
        {
            var data = ScriptableObject.CreateInstance<UpgradeData>();
            data.EditorSet(
                DataIds.Upgrades.DrillSpeed,
                "드릴 속도",
                1,
                new List<UpgradeLevelDefinition>
                {
                    new UpgradeLevelDefinition(
                        1,
                        0.25f,
                        new List<ItemCostEntry>
                        {
                            new ItemCostEntry(DataIds.Minerals.Copper, 2)
                        })
                });
            var state = new UpgradeState();
            var wallet = new Wallet();
            var service = new ProgressionService(state, new Catalog(data), wallet);
            var view = new View();
            var presenter = new ProgressionPanelPresenter(view);
            presenter.Bind(service);
            Assert.That(presenter.SelectUpgrade(DataIds.Upgrades.DrillSpeed), Is.True);

            var result = presenter.RequestPurchase();

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(view.Selected.CurrentLevel, Is.EqualTo(1));
            Assert.That(view.Selected.CurrentEffectValue, Is.EqualTo(0.25f).Within(0.0001f));
            Assert.That(service.Effects.GetDrillSpeedMultiplier(), Is.EqualTo(1.25f).Within(0.0001f));
            Assert.That(wallet.Copper, Is.EqualTo(1));
            Assert.That(view.Message, Does.Contain("완료"));
            Assert.That(view.Busy, Is.False);

            Object.Destroy(data);
            yield return null;
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using SubTerra.App.Audio;
using SubTerra.App.Core.Data;
using SubTerra.App.Tests.PlayMode;
using SubTerra.App.UI;
using SubTerra.App.UI.HUD;
using SubTerra.App.UI.Progression;
using SubTerra.App.UI.SurfaceBase;
using SubTerra.App.UI.Tutorial;
using SubTerra.Shared;
using SubTerra.Shared.Audio;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace SubTerra.App.Tests.PlayMode.Audio
{
    /// <summary>
    /// B-169 실제 지상 기지·광산 Scene(슬롯 0 격리 런타임)에서 노드 해금 연출·소리, 창 TV 소리, 업그레이드 창 자물쇠 소리를 확인하고 화면을 남긴다.
    /// </summary>
    public sealed class PromptB169UnlockAndTvPlayModeTests
    {
        private const string EvidenceDirectory = "../work_process/images/b169-unlock-evidence";
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

        private static readonly SfxId[] LockSounds =
        {
            SfxId.UnlockRattle, SfxId.UnlockClick, SfxId.UpgradeLockCrack, SfxId.UpgradeLockShatter,
            SfxId.UpgradeWindowSuckIn, SfxId.UpgradeLockRegather, SfxId.UnlockGlow
        };

        private readonly List<SfxId> requests = new List<SfxId>();
        private UiTestEnvironment env;
        private bool previousReduceMotion;

        [SetUp]
        public void SetUp()
        {
            Directory.CreateDirectory(EvidenceDirectory);
            previousReduceMotion = AccessibilityPreferences.ReduceMotion;
            AccessibilityPreferences.ReduceMotion = false;
            UpgradeWindowMotion.ResetSessionForTests();
            requests.Clear();
            SfxRequests.PlayRequested += Record;
            // 실제 라이브러리·시계를 쓰고 음소거만 끈다(설정 파일에 기대지 않게).
            WindowTvSfx.ResetForTests(null, null, () => false);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            SfxRequests.PlayRequested -= Record;
            AccessibilityPreferences.ReduceMotion = previousReduceMotion;
            Time.timeScale = 1f;
            if (env != null) env.Dispose();
            env = null;
            WindowTvSfx.ReleaseForTests();
            UpgradeWindowMotion.ResetSessionForTests();
            // 실제 Scene이 남아 뒤 검사에 끼어들지 않게 빈 Scene으로 바꾸고 나머지를 내린다(lessons 10-25).
            var clean = SceneManager.CreateScene("B169Cleanup-" + Guid.NewGuid().ToString("N"));
            SceneManager.SetActiveScene(clean);
            for (var i = SceneManager.sceneCount - 1; i >= 0; i--)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene != clean && scene.isLoaded)
                {
                    yield return SceneManager.UnloadSceneAsync(scene);
                }
            }

            yield return null;
        }

        private void Record(SfxId id, float pitch) => requests.Add(id);

        [UnityTest]
        public IEnumerator Surface_PurchaseUnlock_ShowsLockThenFallsWithSounds_AndUpgradeWindowLockSounds()
        {
            env = new UiTestEnvironment();
            yield return env.LoadSurfaceBase(() =>
                Object.FindAnyObjectByType<SurfaceBaseBinder>()?.IsBound == true);
            Assert.That(WindowTvSfx.StartedCount, Is.Zero, "씬을 여는 동안 창 TV 소리 없음");
            Assert.That(LockOnly(), Is.Empty, "씬을 여는 동안 자물쇠 소리 없음");

            var runtime = env.Save;
            runtime.InventoryService.SetMaximumCapacity(10000f);
            runtime.InventoryService.TryAddMineral(DataIds.Minerals.Copper, 600);
            runtime.InventoryService.TryAddMineral(DataIds.Minerals.Iron, 300);

            var view = Object.FindAnyObjectByType<SurfaceBaseView>();
            var upgradeButton = view.transform.Find("SurfaceBaseContent/UpgradeButton").GetComponent<UnityEngine.UI.Button>();
            var closeButton = view.transform.Find("SurfaceBaseContent/UpgradeModal/ProgressionPanel/CloseUpgradeButton")
                .GetComponent<UnityEngine.UI.Button>();
            var motion = view.UpgradeMotion;
            Assert.That(motion.GetComponents<UpgradeWindowMotionSfx>().Length, Is.EqualTo(1), "소리는 한 번만 붙는다");

            // 처음 열기: 덜그럭 → 딸깍+치직 → 쨍그랑.
            upgradeButton.onClick.Invoke();
            yield return UiTestWait.Until(() => motion.IsSettled, "처음 열기 끝", 4f);
            Assert.That(LockOnly(), Is.EqualTo(new[] { SfxId.UnlockRattle, SfxId.UnlockClick, SfxId.UpgradeLockCrack, SfxId.UpgradeLockShatter }));
            Assert.That(WindowTvSfx.StartedCount, Is.Zero, "업그레이드 창은 TV 소리를 쓰지 않는다");

            // 구매 → 새로 풀린 노드: 자물쇠가 보인 채로 시작해 흔들림 → 딸깍 → 떨어짐 → 빛.
            requests.Clear();
            var binder = view.GetComponentInChildren<ProgressionPanelBinder>(true);
            var panel = binder.GetComponent<ProgressionPanelView>();
            Assert.That(binder.SelectUpgrade(DataIds.Upgrades.DrillSpeed), Is.True);
            var purchase = (UnityEngine.UI.Button)typeof(ProgressionPanelView).GetField("purchaseButton", Private).GetValue(panel);
            purchase.onClick.Invoke();
            var result = runtime.Progression.LastPurchaseResult;
            Assert.That(result.IsSuccess, Is.True, result.Status.ToString());
            var unlocked = result.NewlyUnlockedUpgradeIds;
            Assert.That(unlocked.Count, Is.GreaterThan(0), "드릴 속도 Lv.1로 주변 노드가 풀린다");
            var nodes = new List<UpgradeTreeNodeView>();
            var log = new StringBuilder("unlocked=" + unlocked.Count);
            foreach (var id in unlocked)
            {
                var node = panel.TreeView.FindNode(id);
                Assert.That(node, Is.Not.Null, id);
                nodes.Add(node);
                Assert.That(node.IsRevealPending, Is.True, id + " 해금 대기");
                Assert.That(node.IsLockVisible, Is.True, id + " 구매 직후 자물쇠가 보인다(B-169 원인 수정)");
                Assert.That(node.BlindAlpha, Is.EqualTo(1f), id + " 블라인드 유지");
                log.Append(' ').Append(id);
            }

            Debug.Log("[B169] " + log);
            yield return Capture("purchase-000");
            var started = Time.realtimeSinceStartup;
            foreach (var at in new[] { 0.3f, 0.42f, 0.55f, 0.75f, 1f, 1.4f, 1.9f })
            {
                yield return UiTestWait.Until(() => Time.realtimeSinceStartup - started >= at, "시각 " + at, 5f);
                yield return Capture("purchase-" + ((int)(at * 100f)).ToString("D3"));
            }

            yield return UiTestWait.Until(() => nodes.TrueForAll(n => !n.IsRevealPending && !n.IsUnlockPlaying), "해금 연출 끝", 6f);
            foreach (var node in nodes)
            {
                Assert.That(node.BlindAlpha, Is.EqualTo(0f), node.UpgradeId);
                Assert.That(node.IsLockVisible, Is.False, node.UpgradeId);
            }

            var audible = Mathf.Min(unlocked.Count, UnlockRevealTimeline.MaxAudibleNodes);
            Assert.That(requests.Count, Is.GreaterThan(0));
            Assert.That(requests[0], Is.EqualTo(SfxId.UpgradeLevelUp), "레벨업이 먼저");
            Assert.That(Count(SfxId.UnlockRattle), Is.EqualTo(audible));
            Assert.That(Count(SfxId.UnlockClick), Is.EqualTo(audible));
            Assert.That(Count(SfxId.UnlockGlow), Is.EqualTo(audible));

            // 닫기: 슈욱 → 착. 다시 열면 해금 연출이 또 나오지 않는다.
            requests.Clear();
            closeButton.onClick.Invoke();
            yield return UiTestWait.Until(() => motion.State == UpgradeWindowMotion.MotionState.Hidden, "닫기 끝", 3f);
            Assert.That(LockOnly(), Is.EqualTo(new[] { SfxId.UpgradeWindowSuckIn, SfxId.UpgradeLockRegather }));

            requests.Clear();
            upgradeButton.onClick.Invoke();
            yield return UiTestWait.Until(() => motion.IsSettled, "다시 열기 끝", 3f);
            yield return WaitSeconds(0.6f);
            Assert.That(LockOnly(), Is.EqualTo(new[] { SfxId.UnlockClick, SfxId.UpgradeLockShatter }), "다시 열기는 딸깍 → 쨍그랑만");
            foreach (var node in nodes) Assert.That(node.IsUnlockPlaying, Is.False, "다시 열면 해금 연출 없음");

            // 캡처 비용으로 중간 자세를 건너뛰지 않도록 시각 비교만 같은 노드에서 수동 진행한다.
            var preview = nodes[0];
            preview.ManualTick = true;
            preview.SetRevealPending(true);
            preview.BeginUnlock(null);
            var previewSteps = new[] { 0.275f, 0.245f, 0.2f, 0.28f };
            for (var i = 0; i < previewSteps.Length; i++)
            {
                preview.StepUnlock(previewSteps[i]);
                yield return Capture("issue188-stage-" + i);
            }
            preview.ManualTick = false;
        }

        [UnityTest]
        public IEnumerator Mine_WindowTvSounds_UpgradeUKeySounds_AndReduceMotion()
        {
            env = new UiTestEnvironment();
            yield return env.LoadIntegration();
            Assert.That(WindowTvSfx.StartedCount, Is.Zero, "광산을 여는 동안 창 TV 소리 없음");
            Assert.That(LockOnly(), Is.Empty);

            var hud = Object.FindAnyObjectByType<HudPanelChromeController>();
            Assert.That(hud, Is.Not.Null);
            var inventoryView = Object.FindAnyObjectByType<SubTerra.App.UI.Inventory.InventoryPanelView>(FindObjectsInactive.Include);
            var buildingView = Object.FindAnyObjectByType<SubTerra.App.UI.Building.BuildingMenuView>(FindObjectsInactive.Include);
            var questView = Object.FindAnyObjectByType<DemoObjectiveView>(FindObjectsInactive.Include);
            Assert.That(inventoryView, Is.Not.Null);
            Assert.That(buildingView, Is.Not.Null);
            Assert.That(questView, Is.Not.Null);
            AssertNoClickSoundOnWindowButtons();

            // 인벤토리
            hud.OpenInventoryPanel();
            AssertTv(1, SfxId.WindowTvOpen, "인벤토리 열기");
            AssertVolume(SfxId.WindowTvOpen);
            yield return Capture("mine-inventory-open-start");
            yield return WaitSeconds(0.3f);
            hud.CloseInventoryPanel();
            AssertTv(2, SfxId.WindowTvClose, "인벤토리 닫기");
            yield return WaitSeconds(0.3f);
            hud.CloseInventoryPanel();
            AssertTv(2, SfxId.WindowTvClose, "닫힌 인벤토리를 다시 닫으면 무음");

            // 건설창
            hud.OpenBuildingMenu();
            AssertTv(3, SfxId.WindowTvOpen, "건설창 열기");
            yield return WaitSeconds(0.3f);
            hud.CloseBuildingMenu();
            AssertTv(4, SfxId.WindowTvClose, "건설창 닫기");
            yield return WaitSeconds(0.3f);

            // 퀘스트 상세: 페이지 넘김은 무음.
            questView.OnObjectiveDetailsClicked();
            AssertTv(5, SfxId.WindowTvOpen, "퀘스트 상세 열기");
            yield return WaitSeconds(0.3f);
            questView.OnDetailsNextClicked();
            questView.OnDetailsPrevClicked();
            yield return null;
            AssertTv(5, SfxId.WindowTvOpen, "퀘스트 페이지 넘김은 무음");
            questView.OnDetailsDismissClicked();
            AssertTv(6, SfxId.WindowTvClose, "퀘스트 상세 닫기");
            yield return WaitSeconds(0.3f);

            // 빠르게 열고 닫기: 끊고 다시 내며 창 크기가 원래대로 남는다.
            var inventoryRect = (RectTransform)inventoryView.transform;
            var scale = inventoryRect.localScale;
            var position = inventoryRect.anchoredPosition;
            var interrupted = WindowTvSfx.InterruptedCount;
            hud.OpenInventoryPanel();
            yield return null;
            hud.CloseInventoryPanel();
            yield return null;
            hud.OpenInventoryPanel();
            AssertTv(9, SfxId.WindowTvOpen, "연타");
            // 직전 닫기 소리(0.56초)가 아직 나는 중이면 첫 열기도 끊은 것으로 센다.
            Assert.That(WindowTvSfx.InterruptedCount, Is.GreaterThanOrEqualTo(interrupted + 2), "앞 소리를 끊고 다시");
            Assert.That(WindowTvSfx.Source.clip.name, Is.EqualTo("window_tv_open"));
            yield return WaitSeconds(0.4f);
            Assert.That(inventoryRect.localScale, Is.EqualTo(scale));
            Assert.That(Vector2.Distance(inventoryRect.anchoredPosition, position), Is.LessThan(0.01f));
            hud.CloseInventoryPanel();
            AssertTv(10, SfxId.WindowTvClose, "연타 뒤 닫기");
            yield return WaitSeconds(0.3f);

            // 광산 U키 경로(PanelToggleController): 지상과 같은 자물쇠 소리.
            requests.Clear();
            var panels = Object.FindAnyObjectByType<PanelToggleController>();
            panels.ToggleUpgrade();
            var card = Object.FindAnyObjectByType<ProgressionPanelView>(FindObjectsInactive.Include);
            var motion = card.GetComponentInParent<UpgradeWindowMotion>(true);
            Assert.That(motion, Is.Not.Null);
            Assert.That(motion.GetComponents<UpgradeWindowMotionSfx>().Length, Is.EqualTo(1));
            yield return UiTestWait.Until(() => motion.IsSettled, "광산 처음 열기 끝", 4f);
            Assert.That(LockOnly(), Is.EqualTo(new[] { SfxId.UnlockRattle, SfxId.UnlockClick, SfxId.UpgradeLockCrack, SfxId.UpgradeLockShatter }));
            requests.Clear();
            panels.CloseUpgrade();
            yield return UiTestWait.Until(() => motion.State == UpgradeWindowMotion.MotionState.Hidden, "광산 닫기 끝", 3f);
            Assert.That(LockOnly(), Is.EqualTo(new[] { SfxId.UpgradeWindowSuckIn, SfxId.UpgradeLockRegather }));

            // 모션 감소: 창은 바로 열리고 닫히며 TV 소리만 난다. 업그레이드 창도 자물쇠 대신 TV 소리.
            AccessibilityPreferences.ReduceMotion = true;
            requests.Clear();
            hud.OpenInventoryPanel();
            AssertTv(11, SfxId.WindowTvOpen, "모션 감소 인벤토리 열기");
            hud.CloseInventoryPanel();
            AssertTv(12, SfxId.WindowTvClose, "모션 감소 인벤토리 닫기");
            panels.ToggleUpgrade();
            Assert.That(motion.IsSettled, Is.True, "모션 감소는 바로 열림");
            AssertTv(13, SfxId.WindowTvOpen, "모션 감소 업그레이드 열기");
            panels.CloseUpgrade();
            AssertTv(14, SfxId.WindowTvClose, "모션 감소 업그레이드 닫기");
            Assert.That(LockOnly(), Is.Empty, "모션 감소면 자물쇠 소리 없음");
            yield return null;
        }

        // ---------- 도우미 ----------

        private void AssertNoClickSoundOnWindowButtons()
        {
            var offenders = new StringBuilder();
            foreach (var button in Object.FindObjectsByType<UiSfxButton>(FindObjectsInactive.Include))
            {
                var path = PathOf(button.transform);
                var related = path.Contains("Inventory") || path.Contains("Building") || path.Contains("Quest") || path.Contains("Objective");
                if (related && button.ClickSfx != SfxId.None)
                {
                    offenders.Append(path).Append('=').Append(button.ClickSfx).Append("; ");
                }
            }

            Assert.That(offenders.ToString(), Is.Empty, "세 창 열기·닫기 버튼 클릭음은 None");
        }

        private static string PathOf(Transform t)
        {
            var path = t.name;
            while (t.parent != null)
            {
                t = t.parent;
                path = t.name + "/" + path;
            }

            return path;
        }

        private static void AssertTv(int started, SfxId last, string label)
        {
            Assert.That(WindowTvSfx.StartedCount, Is.EqualTo(started), label);
            Assert.That(WindowTvSfx.LastId, Is.EqualTo(last), label);
        }

        private static void AssertVolume(SfxId id)
        {
            var library = SfxPlayer.Instance != null ? SfxPlayer.Instance.Library : Resources.Load<SfxLibrary>(SfxLibrary.ResourcePath);
            Assert.That(library.TryGetEntry(id, out var entry), Is.True);
            var expected = entry.Volume * library.GetBusVolume(entry.Bus)
                * SubTerra.App.UI.MainMenu.SettingsRuntimeApplier.EffectiveVolume(SubTerra.App.UI.MainMenu.AudioChannel.Sfx);
            Assert.That(WindowTvSfx.Source.volume, Is.EqualTo(expected).Within(0.0001f), "칸 × 버스 × 효과음 설정");
            Assert.That(WindowTvSfx.Source.ignoreListenerPause, Is.True);
            Debug.Log("[B169] " + id + " volume=" + WindowTvSfx.Source.volume.ToString("F3"));
        }

        private List<SfxId> LockOnly()
        {
            var result = new List<SfxId>();
            foreach (var id in requests)
            {
                if (Array.IndexOf(LockSounds, id) >= 0) result.Add(id);
            }

            return result;
        }

        private int Count(SfxId id)
        {
            var count = 0;
            foreach (var value in requests)
            {
                if (value == id) count++;
            }

            return count;
        }

        private static IEnumerator WaitSeconds(float seconds)
        {
            var until = Time.realtimeSinceStartup + seconds;
            while (Time.realtimeSinceStartup < until)
            {
                yield return null;
            }
        }

        private static IEnumerator Capture(string name)
        {
            yield return new WaitForEndOfFrame();
            var texture = ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(Path.Combine(EvidenceDirectory, name + ".png"), texture.EncodeToPNG());
            Object.Destroy(texture);
        }
    }
}

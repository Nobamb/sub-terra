using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SubTerra.App.Integration;
using SubTerra.App.Save;
using SubTerra.App.UI;
using SubTerra.App.UI.Guide;
using SubTerra.App.UI.HUD;
using SubTerra.Gameplay.Hazards;
using SubTerra.Gameplay.Player;
using SubTerra.Shared;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SubTerra.App.Tests.PlayMode
{
    /// <summary>B-140: 실제 Integration 씬·실제 입력 경로(G·X·마우스)로 게임 가이드 창을 확인한다.</summary>
    public sealed class PromptB140GameGuidePlayModeTests
    {
        private const string EvidenceFolder = "../../work_process/MVP2/guide-ui/evidence";
        private UiTestEnvironment environment;

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.timeScale = 1f;
            GameGuideState.ResetSessionForTests();
            if (environment != null) environment.Dispose();
            yield return null;
        }

        private IEnumerator Load(int width = 1920, int height = 1080)
        {
            GameGuideState.ResetSessionForTests();
            environment = new UiTestEnvironment();
            yield return environment.LoadIntegration(sceneLoadTimeout: 90f);
            if (width != 1920 || height != 1080)
            {
                yield return environment.Resolution.Set(width, height);
            }
        }

        private static HudPanelChromeController Chrome()
        {
            var chrome = Object.FindAnyObjectByType<HudPanelChromeController>();
            Assert.That(chrome, Is.Not.Null);
            return chrome;
        }

        private static GameGuidePopupView Popup()
        {
            return Object.FindAnyObjectByType<GameGuidePopupView>(FindObjectsInactive.Include);
        }

        private static void AssertPointerHitsGuide(string message)
        {
            Canvas.ForceUpdateCanvases();
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current)
                { position = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f) }, hits);
            Assert.That(hits, Is.Not.Empty, message);
            Assert.That(hits[0].gameObject.GetComponentInParent<GameGuidePopupView>(), Is.Not.Null, message);
        }

        private IEnumerator WaitOpen(GameGuidePopupView popup)
        {
            yield return UiTestWait.Until(() => popup.State == GameGuidePopupView.PopupState.Open, "guide open", 4f, "animation");
        }

        private IEnumerator WaitHidden(GameGuidePopupView popup)
        {
            yield return UiTestWait.Until(() => popup.State == GameGuidePopupView.PopupState.Hidden && !popup.gameObject.activeSelf,
                "guide hidden", 3f, "animation");
        }

        [UnityTest]
        public IEnumerator GuideAndControlTerminal_MiningCompletionFrames()
        {
            yield return Load(1280, 720);
            var chrome = Chrome();
            chrome.OpenGameGuide();
            var popup = Popup();
            yield return WaitOpen(popup);
            popup.GuideState.SelectCard("ctrl.mine");
            yield return UiTestWait.Until(() => popup.DetailStage.Clip != null && popup.DetailStage.Clip.Id == "mine",
                "mining guide demo", 2f);
            yield return new WaitForSecondsRealtime(0.25f);
            popup.DetailStage.Stop();
            foreach (var offset in new[] { -0.01f, 0f, 0.15f, 0.17f })
            {
                popup.DetailStage.Show(1.333f + offset);
                yield return UiTestWait.Capture("Temp/guide-mining-evidence/guide-" + (offset + 0.01f).ToString("F2") + ".png");
            }
            chrome.CloseGameGuide();
            yield return WaitHidden(popup);

            var canvas = new GameObject("MiningControlsPreview", typeof(RectTransform), typeof(Canvas));
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var panel = SubTerra.App.UI.MainMenu.ControlSchemePanel.Attach(canvas);
            try
            {
                panel.Open();
                yield return UiTestWait.Until(() => panel.Terminal.CurrentPhase == SubTerra.App.UI.ControlTerminal.ControlTerminalView.Phase.Idle,
                    "control terminal open", 3f);
                var cap = panel.Terminal.GetCap("Enter");
                ExecuteEvents.Execute(cap.gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerEnterHandler);
                yield return UiTestWait.Until(() => panel.Terminal.ScreenVisible && panel.Terminal.Stage.Clip != null,
                    "mining controls demo", 2f);
                yield return new WaitForSecondsRealtime(0.5f);
                panel.Terminal.Stage.Stop();
                foreach (var offset in new[] { -0.01f, 0f, 0.15f, 0.17f })
                {
                    panel.Terminal.Stage.Show(17f / 12f + offset);
                    yield return UiTestWait.Capture("Temp/guide-mining-evidence/controls-" + (offset + 0.01f).ToString("F2") + ".png");
                }
            }
            finally { Object.Destroy(canvas); }
        }

        [UnityTest]
        public IEnumerator Guide_OpensAndClosesThroughExistingPaths()
        {
            yield return Load();
            var chrome = Chrome();
            var scaleBefore = Time.timeScale;

            // G 키(기존 열기 경로).
            yield return UiTestWait.Press(environment.Keyboard, Key.G);
            Assert.That(chrome.IsGameGuideOpen, Is.True);
            var popup = Popup();
            Assert.That(popup, Is.Not.Null);
            Assert.That(popup.IsFullIntro, Is.True, "첫 열기는 전체 연출");
            yield return WaitOpen(popup);
            Assert.That(Time.timeScale, Is.Zero);
            Assert.That(UiPauseGate.IsHeldBy(UiPauseGate.MineGuide), Is.True);
            Assert.That(popup.TabButtons.Count, Is.EqualTo(3));
            Assert.That(popup.TitleString, Is.EqualTo("SUB-TERRA 게임 가이드"));
            Assert.That(popup.IsDemoPlaying, Is.True);
            var legacy = chrome.GetComponentInChildren<GameGuidePanelView>(true);
            Assert.That(legacy.IsOpen, Is.True);

            // X 버튼(새 창의 닫기)으로 닫기.
            yield return UiTestWait.Click(environment.Mouse, popup.CloseButton.Button);
            Assert.That(chrome.IsGameGuideOpen, Is.False);
            yield return WaitHidden(popup);
            Assert.That(Time.timeScale, Is.EqualTo(scaleBefore));
            Assert.That(popup.IsDemoPlaying, Is.False, "닫은 뒤 시연 재생 중단");
            Assert.That(popup.BlocksInput, Is.False);

            // 재열기: 짧은 연출 + 선택 상태 유지.
            popup.GuideState.SelectCard("ctrl.mine");
            yield return UiTestWait.Press(environment.Keyboard, Key.G);
            Assert.That(popup.IsFullIntro, Is.False, "재열기는 짧은 연출");
            yield return WaitOpen(popup);
            Assert.That(popup.GuideState.CurrentCardId, Is.EqualTo("ctrl.mine"));

            // X 키(기존 최상위 팝업 닫기 경로).
            yield return UiTestWait.Press(environment.Keyboard, Key.X);
            Assert.That(chrome.IsGameGuideOpen, Is.False);
            yield return WaitHidden(popup);
        }

        [UnityTest]
        public IEnumerator Guide_OpeningDurationsFollowTheTimeline()
        {
            yield return Load();
            var chrome = Chrome();
            chrome.OpenGameGuide();
            var popup = Popup();
            var started = Time.realtimeSinceStartup;
            yield return WaitOpen(popup);
            var firstOpen = Time.realtimeSinceStartup - started;
            Assert.That(firstOpen, Is.InRange(0.9f, 2.0f), "첫 등장 약 1~1.3초(프레임 지터 포함)");

            chrome.CloseGameGuide();
            started = Time.realtimeSinceStartup;
            yield return WaitHidden(popup);
            var closeTime = Time.realtimeSinceStartup - started;
            Assert.That(closeTime, Is.InRange(0.25f, 0.9f), "닫기 약 0.3~0.5초");

            chrome.OpenGameGuide();
            started = Time.realtimeSinceStartup;
            yield return WaitOpen(popup);
            var reopen = Time.realtimeSinceStartup - started;
            Assert.That(reopen, Is.LessThan(firstOpen * 0.8f), "재열기는 더 짧다");
            Assert.That(reopen, Is.InRange(0.4f, 1.1f));
        }

        [UnityTest]
        public IEnumerator Guide_ClosesWhileOpeningAndSurvivesRapidToggling()
        {
            yield return Load();
            var chrome = Chrome();
            chrome.OpenGameGuide();
            var popup = Popup();
            yield return UiTestWait.Delay(0.35f, "mid-intro");
            Assert.That(popup.State, Is.EqualTo(GameGuidePopupView.PopupState.Opening));
            AssertPointerHitsGuide("등장 중 창 영역 클릭이 게임으로 통과하지 않는다");
            chrome.CloseGameGuide();
            Assert.That(popup.IsClosing, Is.True, "등장 도중에도 닫기 요청을 받는다");
            yield return UiTestWait.Delay(0.12f, "mid-close");
            AssertPointerHitsGuide("닫는 중에도 창 영역 클릭이 게임으로 통과하지 않는다");
            yield return WaitHidden(popup);

            for (var i = 0; i < 12; i++)
            {
                chrome.ToggleGameGuide();
                yield return null;
            }

            yield return UiTestWait.Delay(0.2f, "settle toggles");
            Assert.That(Object.FindObjectsByType<GameGuidePopupView>(FindObjectsInactive.Include).Length, Is.EqualTo(1),
                "창이 여러 개 생기지 않는다");
            if (chrome.IsGameGuideOpen)
            {
                yield return WaitOpen(popup);
                Assert.That(popup.GetComponentsInChildren<GameGuideStageView>(true).Count(s => s.IsPlaying), Is.EqualTo(1));
                chrome.CloseGameGuide();
            }

            yield return WaitHidden(popup);
            Canvas.ForceUpdateCanvases();
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current)
                { position = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f) }, hits);
            Assert.That(hits.Any(h => h.gameObject.GetComponentInParent<GameGuidePopupView>() != null), Is.False,
                "닫은 뒤 투명 패널이 입력을 막지 않는다");
        }

        [UnityTest]
        public IEnumerator Guide_AnimationRunsWhileGameIsPaused_AndDoesNotChangeTimeScale()
        {
            yield return Load();
            var chrome = Chrome();
            chrome.OpenGameGuide();
            Assert.That(UiPauseGate.Acquire("b140-test"), Is.True);
            Assert.That(Time.timeScale, Is.Zero);
            var popup = Popup();
            yield return WaitOpen(popup);
            Assert.That(Time.timeScale, Is.Zero, "기존 정지 상태를 그대로 둔다");
            chrome.CloseGameGuide();
            yield return WaitHidden(popup);
            Assert.That(UiPauseGate.IsHeldBy(UiPauseGate.MineGuide), Is.False);
            Assert.That(UiPauseGate.Release("b140-test"), Is.True);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
        }

        [UnityTest]
        public IEnumerator Issue352_MineMenusPauseWorldAndPreventOtherPanelsOpening()
        {
            yield return Load();
            var chrome = Chrome();
            var menu = Object.FindAnyObjectByType<UndergroundMenuController>();
            var panels = Object.FindAnyObjectByType<PanelToggleController>();
            var survival = Object.FindAnyObjectByType<PlayerSurvivalController>();
            var body = Object.FindAnyObjectByType<PlayerMovement>().GetComponent<Rigidbody2D>();
            survival.RestoreOxygen(40f);
            body.linearVelocityY = -1f;
            var position = body.position;

            chrome.OpenGameGuide();
            yield return WaitOpen(Popup());
            Assert.That(UiPauseGate.IsHeldBy(UiPauseGate.MineGuide), Is.True);
            Assert.That(body.position, Is.EqualTo(position));
            Assert.That(body.linearVelocityY, Is.EqualTo(-1f));
            Assert.That(survival.OxygenRemaining, Is.EqualTo(40f));
            menu.HandleEscape();
            panels.ToggleUpgrade();
            chrome.OpenDiggerBot();
            chrome.OpenInventoryPanel();
            chrome.OpenBuildingMenu();
            Assert.That(menu.IsSettingsOpen, Is.False);
            Assert.That(panels.IsVisible(RuntimePanelId.Upgrade), Is.False);
            Assert.That(chrome.IsDiggerBotOpen || chrome.IsInventoryPanelOpen || chrome.IsBuildingMenuOpen, Is.False);
            yield return UiTestWait.Press(environment.Keyboard, Key.G);
            yield return WaitHidden(Popup());
            Assert.That(Time.timeScale, Is.EqualTo(1f));

            menu.OpenSettings();
            Assert.That(menu.IsSettingsOpen, Is.True);
            Assert.That(UiPauseGate.IsHeldBy(UiPauseGate.MineSettings), Is.True);
            chrome.OpenGameGuide();
            panels.ToggleUpgrade();
            Assert.That(chrome.IsGameGuideOpen, Is.False);
            Assert.That(panels.IsVisible(RuntimePanelId.Upgrade), Is.False);
            yield return UiTestWait.Press(environment.Keyboard, Key.Escape);
            yield return UiTestWait.Until(() => !UiPauseGate.IsHeld, "settings close releases pause");
            Assert.That(menu.IsSettingsOpen, Is.False);

            panels.ToggleUpgrade();
            Assert.That(panels.IsVisible(RuntimePanelId.Upgrade), Is.True);
            Assert.That(UiPauseGate.IsHeldBy(UiPauseGate.MineUpgrade), Is.True);
            menu.OpenSettings();
            chrome.OpenGameGuide();
            Assert.That(menu.IsSettingsOpen || chrome.IsGameGuideOpen, Is.False);
            yield return UiTestWait.Press(environment.Keyboard, Key.U);
            yield return UiTestWait.Until(() => !UiPauseGate.IsHeld, "upgrade close releases pause");
            Assert.That(panels.IsVisible(RuntimePanelId.Upgrade), Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
        }

        [UnityTest]
        public IEnumerator Issue352_MineMenusFreezeClockAndGasDamage()
        {
            yield return Load();
            var runtime = environment.Save;
            Assert.That(runtime.ActiveSlot, Is.Zero);
            var slot = typeof(SaveRuntimeController).GetField("activeSlot", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(slot, Is.Not.Null);
            var chrome = Chrome();
            var menu = Object.FindAnyObjectByType<UndergroundMenuController>();
            var panels = Object.FindAnyObjectByType<PanelToggleController>();
            var survival = Object.FindAnyObjectByType<PlayerSurvivalController>();
            var gas = Object.FindAnyObjectByType<GasHazardSystem>();
            var effects = Object.FindAnyObjectByType<GasExposureEffectController>();
            var position = Object.FindAnyObjectByType<PlayerMovement>().Position;
            var zone = gas.RestoreGasZone(new GasSnapshotDto
            {
                gasZoneId = "issue352-test-gas",
                gasTypeId = GasType.Toxic.ToString(),
                concentrationLevel = 1f,
                remainingDuration = 20f,
                x = Mathf.RoundToInt(position.x),
                y = Mathf.RoundToInt(position.y),
                isActive = true
            });
            Assert.That(gas.CurrentExposure.IsExposed && !effects.CurrentState.IsSheltered, Is.True);
            var ticks = 0;
            void CountDamage(GasExposureFailureInputDto _) => ticks++;
            effects.FailureInputRaised += CountDamage;
            try
            {
                // Allow the real clock to tick without creating an autosave coordinator or writing a slot.
                slot.SetValue(runtime, 1);
                var menus = new (System.Action open, System.Action close, string owner)[]
                {
                    (chrome.OpenGameGuide, chrome.CloseGameGuide, UiPauseGate.MineGuide),
                    (menu.OpenSettings, menu.CloseSettings, UiPauseGate.MineSettings),
                    (panels.ToggleUpgrade, panels.CloseUpgrade, UiPauseGate.MineUpgrade)
                };
                foreach (var (open, close, owner) in menus)
                {
                    open();
                    Assert.That(UiPauseGate.IsHeldBy(owner), Is.True, owner);
                    var timer = runtime.MineResetRemainingSeconds;
                    var gasTime = zone.RemainingDuration;
                    var health = survival.State.Health;
                    var damageTicks = ticks;
                    yield return new WaitForSecondsRealtime(1.15f);
                    Assert.That(runtime.MineResetRemainingSeconds, Is.EqualTo(timer), owner);
                    Assert.That(zone.RemainingDuration, Is.EqualTo(gasTime), owner);
                    Assert.That(ticks, Is.EqualTo(damageTicks), owner);
                    Assert.That(survival.State.Health, Is.EqualTo(health), owner);

                    close();
                    yield return UiTestWait.Until(() => !UiPauseGate.IsHeld, owner + " closes");
                    yield return UiTestWait.Until(() => runtime.MineResetRemainingSeconds < timer
                        && zone.RemainingDuration < gasTime, owner + " resumes clock and gas");
                }

                var ticksBeforeResume = ticks;
                var healthBeforeResume = survival.State.Health;
                yield return UiTestWait.Until(() => ticks > ticksBeforeResume && survival.State.Health < healthBeforeResume,
                    "gas damage resumes");
            }
            finally
            {
                slot.SetValue(runtime, 0);
                effects.FailureInputRaised -= CountDamage;
            }
        }

        [UnityTest]
        public IEnumerator Guide_ClicksTabsCardsFilterAndFirstExploreCard()
        {
            yield return Load();
            var chrome = Chrome();
            chrome.OpenGameGuide();
            var popup = Popup();
            yield return WaitOpen(popup);
            var state = popup.GuideState;

            // 첫 탐사 안내 접기·펼치기(실제 클릭).
            Assert.That(popup.FirstExploreHeight, Is.GreaterThan(200f));
            yield return UiTestWait.Click(environment.Mouse, popup.FirstToggle.Button);
            yield return UiTestWait.Until(() => popup.FirstExploreHeight < 100f, "first explore collapsed", 2f, "animation");
            Assert.That(state.FirstExploreExpanded, Is.False);

            // 카드 선택.
            var card = popup.Cards(GuideTabKind.Controls).First(c => c.Def.Id == "ctrl.ladder");
            yield return UiTestWait.Click(environment.Mouse, card.Button);
            Assert.That(state.CurrentCardId, Is.EqualTo("ctrl.ladder"));
            yield return UiTestWait.Until(() => popup.ShownCardId == "ctrl.ladder", "detail shows ladder", 1f);
            Assert.That(popup.DetailStage.Clip.Id, Is.EqualTo("ladder"));

            // 탭 전환.
            yield return UiTestWait.Click(environment.Mouse, popup.TabButtons[1].Button);
            Assert.That(state.Tab, Is.EqualTo(GuideTabKind.Mechanics));
            yield return UiTestWait.Click(environment.Mouse, popup.TabButtons[2].Button);
            Assert.That(state.Tab, Is.EqualTo(GuideTabKind.Resources));

            // 필터.
            yield return UiTestWait.Click(environment.Mouse, popup.FilterButtons[1].Button);
            Assert.That(state.Filter, Is.EqualTo(GuideFilter.Resources));
            Assert.That(popup.Cards(GuideTabKind.Resources).Count(c => c.gameObject.activeSelf), Is.EqualTo(5));
            yield return UiTestWait.Click(environment.Mouse, popup.FilterButtons[2].Button);
            Assert.That(popup.Cards(GuideTabKind.Resources).Count(c => c.gameObject.activeSelf), Is.EqualTo(10));
            yield return UiTestWait.Click(environment.Mouse, popup.FilterButtons[0].Button);

            // 관련 안내 링크(상세의 버튼) → 해당 탭·카드로 이동.
            var storage = popup.Cards(GuideTabKind.Resources).First(c => c.Def.Id == "res.copper");
            yield return UiTestWait.Click(environment.Mouse, storage.Button);
            var link = popup.GetComponentsInChildren<GameGuideButton>(false).First(b => b.name == "Related_mech.cargo");
            yield return UiTestWait.Click(environment.Mouse, link.Button);
            Assert.That(state.Tab, Is.EqualTo(GuideTabKind.Mechanics));
            Assert.That(state.CurrentCardId, Is.EqualTo("mech.cargo"));

            // 목록 스크롤과 상세 스크롤은 서로 영향을 주지 않는다.
            state.SelectTab(GuideTabKind.Controls);
            yield return null;
            var list = popup.ListScroll(GuideTabKind.Controls);
            var detail = popup.DetailScroll;
            Canvas.ForceUpdateCanvases();
            var detailBefore = detail.content.anchoredPosition.y;
            list.verticalNormalizedPosition = 0f;
            yield return null;
            Assert.That(list.content.anchoredPosition.y, Is.GreaterThan(1f), "목록이 스크롤됨");
            Assert.That(detail.content.anchoredPosition.y, Is.EqualTo(detailBefore).Within(0.01f), "상세는 그대로");
            var listBefore = list.content.anchoredPosition.y;
            detail.verticalNormalizedPosition = 0f;
            yield return null;
            Assert.That(list.content.anchoredPosition.y, Is.EqualTo(listBefore).Within(0.01f), "목록은 그대로");

            chrome.CloseGameGuide();
            yield return WaitHidden(popup);
        }

        [UnityTest]
        public IEnumerator Guide_ContentUsesRealCatalogData()
        {
            yield return Load();
            var chrome = Chrome();
            chrome.OpenGameGuide();
            var popup = Popup();
            yield return WaitOpen(popup);

            popup.GuideState.SelectCard("res.copper");
            yield return null;
            var text = GuideDetailBuilder.ToPlainText(popup.ShownDetail);
            Assert.That(text, Does.Contain("개당 판매가 10G"), "카탈로그 가격");
            Assert.That(text, Does.Contain("개당 무게 1.5"));
            Assert.That(text, Does.Contain("업그레이드 재료"));

            popup.GuideState.SelectCard("fac.storage");
            yield return null;
            text = GuideDetailBuilder.ToPlainText(popup.ShownDetail);
            Assert.That(text, Does.Contain("전력이 필요 없는 시설"), "보관함 powerDraw 0");
            Assert.That(text, Does.Contain("철 2"), "건설 재료");

            popup.GuideState.SelectCard("fac.charger");
            yield return null;
            Assert.That(GuideDetailBuilder.ToPlainText(popup.ShownDetail), Does.Contain("전력망 연결이 필요한 시설"));

            popup.GuideState.SelectCard("mech.grid");
            yield return null;
            Assert.That(GuideDetailBuilder.ToPlainText(popup.ShownDetail), Does.Contain("전력이 필요 없는 시설"));

            // 카드 아이콘은 실제 카탈로그 아이콘(흰 사각형이 아님).
            popup.GuideState.SelectTab(GuideTabKind.Resources);
            yield return null;
            foreach (var card in popup.Cards(GuideTabKind.Resources))
            {
                var icon = card.transform.Find("Icon").GetComponent<Image>();
                Assert.That(icon.sprite, Is.Not.Null, card.Def.Id);
                Assert.That(icon.enabled, Is.True, card.Def.Id);
            }

            chrome.CloseGameGuide();
            yield return WaitHidden(popup);
        }

        [UnityTest]
        public IEnumerator Guide_FitsRepresentativeResolutions_WithoutClippedText()
        {
            yield return Load();
            foreach (var size in new[] { new Vector2Int(1280, 720), new Vector2Int(1920, 1080), new Vector2Int(2560, 1440) })
            {
                yield return environment.Resolution.Set(size.x, size.y);
                yield return null;
                var chrome = Chrome();
                chrome.OpenGameGuide();
                var popup = Popup();
                yield return WaitOpen(popup);
                yield return null;
                Canvas.ForceUpdateCanvases();
                var canvasRect = (RectTransform)popup.transform;
                var corners = new Vector3[4];
                popup.CardRect.GetWorldCorners(corners);
                var screenMin = RectTransformUtility.WorldToScreenPoint(null, corners[0]);
                var screenMax = RectTransformUtility.WorldToScreenPoint(null, corners[2]);
                Assert.That(screenMin.x, Is.GreaterThanOrEqualTo(-1f), size + " 왼쪽");
                Assert.That(screenMin.y, Is.GreaterThanOrEqualTo(-1f), size + " 아래");
                Assert.That(screenMax.x, Is.LessThanOrEqualTo(Screen.width + 1f), size + " 오른쪽");
                Assert.That(screenMax.y, Is.LessThanOrEqualTo(Screen.height + 1f), size + " 위");
                Assert.That(canvasRect.rect.width, Is.GreaterThan(1f));
                var shotFolder = Path.GetFullPath(Path.Combine(Application.dataPath, EvidenceFolder));
                Directory.CreateDirectory(shotFolder);
                yield return UiTestWait.Capture(Path.Combine(shotFolder, "guide-res-" + size.x + "x" + size.y + ".png"));

                var problems = new List<string>();
                foreach (var tab in new[] { GuideTabKind.Controls, GuideTabKind.Mechanics, GuideTabKind.Resources })
                {
                    popup.GuideState.SelectTab(tab);
                    foreach (var def in GameGuideCatalog.ForTab(tab))
                    {
                        popup.GuideState.SelectCard(def.Id);
                        yield return null;
                        Canvas.ForceUpdateCanvases();
                        foreach (var text in popup.GetComponentsInChildren<TMP_Text>(false))
                        {
                            if (text.GetComponentInParent<GameGuideStageView>() != null) continue;
                            if (text.textWrappingMode != TextWrappingModes.NoWrap || text.enableAutoSizing
                                || text.overflowMode == TextOverflowModes.Ellipsis) continue;
                            var preferred = text.GetPreferredValues(text.text).x;
                            if (preferred > text.rectTransform.rect.width + 2f && text.rectTransform.rect.width > 1f)
                                problems.Add(size + " " + def.Id + " '" + text.text + "'");
                        }
                    }
                }

                Assert.That(problems.Distinct().ToArray(), Is.Empty, string.Join("\n", problems.Distinct().Take(8)));
                chrome.CloseGameGuide();
                yield return WaitHidden(popup);
            }
        }

        [UnityTest]
        public IEnumerator Guide_CaptureEvidence()
        {
            yield return Load();
            var chrome = Chrome();
            var folder = Path.GetFullPath(Path.Combine(Application.dataPath, EvidenceFolder));
            Directory.CreateDirectory(folder);
            chrome.OpenGameGuide();
            var popup = Popup();
            yield return WaitOpen(popup);
            yield return UiTestWait.Delay(0.4f, "demo plays");
            yield return UiTestWait.Capture(Path.Combine(folder, "guide-1-controls-expanded.png"));

            popup.GuideState.SetFirstExplore(false);
            popup.GuideState.SelectCard("ctrl.interact");
            yield return UiTestWait.Delay(0.6f, "collapsed");
            yield return UiTestWait.Capture(Path.Combine(folder, "guide-2-controls-collapsed.png"));

            popup.GuideState.SelectTab(GuideTabKind.Mechanics);
            popup.GuideState.SelectCard("mech.power");
            yield return UiTestWait.Delay(0.6f, "mechanics");
            yield return UiTestWait.Capture(Path.Combine(folder, "guide-3-mechanics.png"));

            popup.GuideState.SelectTab(GuideTabKind.Resources);
            popup.GuideState.SelectCard("fac.storage");
            yield return UiTestWait.Delay(0.6f, "resources");
            yield return UiTestWait.Capture(Path.Combine(folder, "guide-4-resources.png"));

            popup.GuideState.SetFilter(GuideFilter.Facilities);
            popup.GuideState.SelectCard("fac.portal");
            yield return UiTestWait.Delay(0.6f, "facilities");
            yield return UiTestWait.Capture(Path.Combine(folder, "guide-5-facilities.png"));

            chrome.CloseGameGuide();
            yield return WaitHidden(popup);
        }

        [UnityTest]
        public IEnumerator Guide_CaptureOpenCloseFrames()
        {
            yield return Load();
            var chrome = Chrome();
            var folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../Temp/guide-frames"));
            if (Directory.Exists(folder)) Directory.Delete(folder, true);
            Directory.CreateDirectory(folder);

            chrome.OpenGameGuide();
            var popup = Popup();
            popup.ManualTick = true;
            var frame = 0;
            const float step = 1f / 30f;
            var elapsed = 0f;
            while (popup.State != GameGuidePopupView.PopupState.Open && elapsed < 3f)
            {
                yield return UiTestWait.Capture(Path.Combine(folder, "open-" + frame.ToString("000") + ".png"));
                popup.Tick(step);
                elapsed += step;
                frame++;
            }

            for (var i = 0; i < 8; i++)
            {
                popup.Tick(step);
            }

            yield return UiTestWait.Capture(Path.Combine(folder, "open-" + frame.ToString("000") + ".png"));
            chrome.CloseGameGuide();
            frame = 0;
            elapsed = 0f;
            while (popup.State == GameGuidePopupView.PopupState.Closing && elapsed < 2f)
            {
                yield return UiTestWait.Capture(Path.Combine(folder, "close-" + frame.ToString("000") + ".png"));
                popup.Tick(step);
                elapsed += step;
                frame++;
            }

            popup.ManualTick = false;
            yield return WaitHidden(popup);
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SubTerra.App.Core.Data;
using SubTerra.App.Editor.DataValidation;
using SubTerra.App.Run;
using SubTerra.App.Save;
using SubTerra.App.UI;
using SubTerra.App.UI.Guide;
using SubTerra.App.UI.HUD;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.Tests.UI
{
    /// <summary>B-140 가이드 개편: 콘텐츠·상태·시간표·시연·창 구조의 정적/순수 검증.</summary>
    public sealed class PromptB140GameGuideTests
    {
        private sealed class FakeData : IGuideData
        {
            public readonly List<GuideBuildingInfo> BuildingList = new List<GuideBuildingInfo>();

            public IReadOnlyList<GuideBuildingInfo> Buildings => BuildingList;

            public bool TryGetItem(string itemId, out GuideItemInfo info)
            {
                var rare = DataIds.RareItems.IsRare(itemId);
                info = new GuideItemInfo(itemId, ItemDisplayNames.Mineral(itemId), null, 3.5f, 777, rare);
                return itemId.StartsWith("mineral.") || rare;
            }

            public bool TryGetBuilding(string buildingId, out GuideBuildingInfo info)
            {
                foreach (var building in BuildingList)
                {
                    if (building.Id == buildingId)
                    {
                        info = building;
                        return true;
                    }
                }

                info = default;
                return false;
            }

            public bool IsUpgradeMaterial(string itemId) => itemId == DataIds.Minerals.Copper;
        }

        [SetUp]
        public void SetUp()
        {
            GameGuideState.ResetSessionForTests();
        }

        [TearDown]
        public void TearDown()
        {
            GameGuideState.ResetSessionForTests();
        }

        // ---------------------------------------------------------------- 콘텐츠

        [Test]
        public void Catalog_HasThreeTabsWithRequestedCards()
        {
            Assert.That(GameGuideCatalog.TabTitle(GuideTabKind.Controls), Is.EqualTo("기본 조작"));
            Assert.That(GameGuideCatalog.TabTitle(GuideTabKind.Mechanics), Is.EqualTo("핵심 메커니즘"));
            Assert.That(GameGuideCatalog.TabTitle(GuideTabKind.Resources), Is.EqualTo("자원·시설"));
            Assert.That(System.Enum.GetValues(typeof(GuideTabKind)).Length, Is.EqualTo(3), "탭은 정확히 세 개");

            var controls = GameGuideCatalog.ForTab(GuideTabKind.Controls).Select(c => c.Id).ToArray();
            Assert.That(controls, Is.EqualTo(new[]
            {
                "ctrl.move", "ctrl.ladder", "ctrl.elevator", "ctrl.interact", "ctrl.mine", "ctrl.build",
                "ctrl.inventory", "ctrl.drone", "ctrl.minimap", "ctrl.clock", "ctrl.guide"
            }));
            var mechanics = GameGuideCatalog.ForTab(GuideTabKind.Mechanics).Select(c => c.Id).ToArray();
            Assert.That(mechanics, Is.EqualTo(new[]
            {
                "mech.base", "mech.health", "mech.power", "mech.cargo", "mech.grid", "mech.core",
                "mech.structure", "mech.gas", "mech.reset"
            }));
            var resources = GameGuideCatalog.ForTab(GuideTabKind.Resources);
            Assert.That(resources.Count(c => c.Kind == GuideCardKind.Resource), Is.EqualTo(5), "구리·철·리튬·엔진 연료·골드");
            Assert.That(resources.Count(c => c.Kind == GuideCardKind.Facility), Is.EqualTo(10),
                "보관함·충전기·보건소·정산 콘솔·코어·사다리·버팀목·조명·긴급 탈출 포탈·엘리베이터");
        }

        [Test]
        public void Catalog_IdsAreUnique_RelatedLinksAndStepTargetsResolve()
        {
            var ids = GameGuideCatalog.All.Select(c => c.Id).ToArray();
            Assert.That(ids.Distinct().Count(), Is.EqualTo(ids.Length));
            foreach (var card in GameGuideCatalog.All)
            {
                foreach (var related in card.Related)
                {
                    Assert.That(GameGuideCatalog.TryGet(related, out var target), Is.True, card.Id + " -> " + related);
                    Assert.That(target.Id, Is.Not.EqualTo(card.Id), card.Id + " 자기 자신으로 연결");
                }
            }

            Assert.That(GameGuideCatalog.FirstSteps.Count, Is.EqualTo(5));
            var titles = GameGuideCatalog.FirstSteps.Select(s => s.Title).ToArray();
            Assert.That(titles, Is.EqualTo(new[] { "지하로 이동", "자원 채굴", "상태 확인", "지상으로 귀환", "판매·업그레이드" }));
            foreach (var step in GameGuideCatalog.FirstSteps)
            {
                Assert.That(GameGuideCatalog.TryGet(step.TargetCardId, out _), Is.True, step.Title);
                Assert.That(GuideDemoLibrary.TryGet(step.DemoId, out _), Is.True, step.Title + " 시연");
            }

            Assert.That(GameGuideCatalog.FirstExploreTitle, Is.EqualTo("처음이라면, 이 순서로 탐사하세요"));
        }

        [Test]
        public void Catalog_EveryCardHasDemoClip_AndKeysParse()
        {
            foreach (var card in GameGuideCatalog.All)
            {
                Assert.That(GuideDemoLibrary.TryGet(card.DemoId, out var clip), Is.True, card.Id);
                Assert.That(clip.Duration, Is.GreaterThan(0.5f), card.Id);
                Assert.That(clip.ThumbTime, Is.InRange(0f, clip.Duration), card.Id + " 썸네일 시각");
                if (card.Tab != GuideTabKind.Resources)
                {
                    Assert.That(card.Lines.Count, Is.GreaterThan(0), card.Id + " 설명");
                }
            }

            foreach (var card in GameGuideCatalog.ForTab(GuideTabKind.Controls))
            {
                Assert.That(GuideKeyTokens.Parse(card.Keys).Count, Is.GreaterThan(0), card.Id + " 키 표기");
            }
        }

        [Test]
        public void Catalog_UsesCurrentGameNames_AndNoDeveloperWording()
        {
            var all = string.Join("\n", GameGuideCatalog.All.SelectMany(AllText));
            var forbidden = new[] { "클램프", "최근 추가", "의료실", "clamp", "TODO", "협동", "멀티플레이" };
            foreach (var word in forbidden)
            {
                Assert.That(all, Does.Not.Contain(word), "가이드 문구에 쓰지 않을 표현: " + word);
            }

            Assert.That(GameGuideCatalog.Get("fac.clinic").Title, Is.EqualTo("보건소"));
            Assert.That(GameGuideCatalog.Get("fac.settlement").Title, Is.EqualTo("정산 콘솔"));
            Assert.That(GameGuideCatalog.Get("fac.core").Title, Is.EqualTo("전진기지 코어"));
            Assert.That(GameGuideCatalog.Get("fac.support").Title, Is.EqualTo("버팀목"));
            Assert.That(GameGuideCatalog.Get("fac.portal").Title, Is.EqualTo(ItemDisplayNames.Building(DataIds.Buildings.EmergencyEscapePortal)));
            Assert.That(GameGuideCatalog.Get("res.fuel").Title, Is.EqualTo("엔진 연료"));
        }

        [Test]
        public void Controls_InputsMatchActualCode()
        {
            Assert.That(GameGuideCatalog.Get("ctrl.move").Keys, Is.EqualTo("A D / ← →"));
            Assert.That(GameGuideCatalog.Get("ctrl.ladder").Keys, Is.EqualTo("W S / ↑ ↓"));
            Assert.That(GameGuideCatalog.Get("ctrl.elevator").Keys, Is.EqualTo("E"));
            Assert.That(GameGuideCatalog.Get("ctrl.mine").Keys, Is.EqualTo("MOUSE / Enter"));
            Assert.That(GameGuideCatalog.Get("ctrl.build").Keys, Is.EqualTo("B -> C"));
            Assert.That(GameGuideCatalog.Get("ctrl.inventory").Keys, Is.EqualTo("I"));
            Assert.That(GameGuideCatalog.Get("ctrl.drone").Keys, Is.EqualTo("Tab"));
            Assert.That(GameGuideCatalog.Get("ctrl.clock").Keys, Is.EqualTo("T"));
            Assert.That(GameGuideCatalog.Get("ctrl.guide").Keys, Is.EqualTo("G"));

            // 사다리에는 E 상호작용이 없다.
            var ladder = Plain("ctrl.ladder");
            Assert.That(ladder, Does.Contain("E 키 같은 별도 상호작용은 없습니다"));
            Assert.That(Plain("fac.ladder"), Does.Contain("E 키는 쓰지 않습니다"));
            Assert.That(GameGuideCatalog.Get("fac.ladder").Keys, Is.EqualTo("W S"));

            // B-142: 미니맵은 M 하나로 닫힘 → 가로형 → 정사각형 → 닫힘을 순환한다.
            var minimap = Plain("ctrl.minimap");
            Assert.That(minimap, Does.Contain("한 단계만 전환됩니다"));
            Assert.That(minimap, Does.Not.Contain("Ctrl"));
            Assert.That(GameGuideCatalog.Get("ctrl.minimap").Keys, Is.EqualTo("M"));

            // 건설은 B로 열고 C로 설치한다.
            Assert.That(Plain("ctrl.build"), Does.Contain("B 키로 시설 건설 창을 열고"));
            Assert.That(Plain("ctrl.build"), Does.Contain("C 키"));
            Assert.That(Plain("ctrl.mine"), Does.Contain("Enter"));
            Assert.That(Plain("ctrl.move"), Does.Contain("Space"));
        }

        [Test]
        public void Controls_MinimapTextMatchesActualBehaviour()
        {
            // 가이드 문구의 순환 순서는 실제 상태 기계 순서와 같아야 한다.
            Assert.That(GameGuideCatalog.MinimapKeys, Is.EqualTo("M"));
            var mode = SubTerra.App.Integration.MinimapBoardMode.Closed;
            mode = SubTerra.App.Integration.MinimapBoardTimeline.Next(mode);
            Assert.That(mode, Is.EqualTo(SubTerra.App.Integration.MinimapBoardMode.Wide));
            mode = SubTerra.App.Integration.MinimapBoardTimeline.Next(mode);
            Assert.That(mode, Is.EqualTo(SubTerra.App.Integration.MinimapBoardMode.Square));
            Assert.That(SubTerra.App.Integration.MinimapBoardTimeline.Next(mode),
                Is.EqualTo(SubTerra.App.Integration.MinimapBoardMode.Closed));
            var text = Plain("ctrl.minimap");
            Assert.That(text, Does.Contain("닫힘 → 가로형 → 작은 정사각형 → 닫힘"));
            Assert.That(text, Does.Contain("길게 눌러도 한 번만"));
            Assert.That(text, Does.Not.Contain("50%"));
        }

        [Test]
        public void Controls_InteractTextKeepsNameBubbleRule()
        {
            var text = Plain("ctrl.interact");
            Assert.That(text, Does.Contain("가까워지면 시설 이름이 말풍선으로 표시됩니다(버팀목·사다리 제외)"));
            Assert.That(ItemDisplayNames.ShowsProximityName(DataIds.Buildings.SupportBasic), Is.False);
            Assert.That(ItemDisplayNames.ShowsProximityName(DataIds.Buildings.LadderBasic), Is.False);
            foreach (var name in new[] { "충전기", "보관함", "정산 콘솔", "전진기지 코어", "긴급 탈출 포탈" })
            {
                Assert.That(text, Does.Contain(name));
            }
        }

        [Test]
        public void Mechanics_RescueFlowKeepsRequestedOrder_AndUsesCodeConstants()
        {
            var steps = GameGuideCatalog.Get("mech.power").Lines.Where(l => l.Kind == GuideLineKind.Step).ToArray();
            Assert.That(steps.Length, Is.EqualTo(4));
            Assert.That(steps[0].Text, Does.Contain("구출 팝업이 열립니다"));
            Assert.That(steps[1].Text, Does.Contain("구출 버튼이 나타납니다"));
            Assert.That(steps[2].Text, Does.Contain("R"));
            Assert.That(steps[2].Text, Does.Contain("구출을 실행하지 않습니다"));
            Assert.That(steps[3].Text, Does.Contain("비용을 확인"));

            var cost = GameGuideCatalog.Get("mech.power").Lines.First(l => l.Label == "구출 비용").Text;
            Assert.That(cost, Does.Contain(EmergencyRescueService.MaximumGoldCost + "G"));
            Assert.That(cost, Does.Contain(EmergencyRescueService.MineralLossPercent + "%"));
            Assert.That(cost, Does.Contain("보관함에 넣은 자원은 차감되지 않습니다"));
        }

        [Test]
        public void Mechanics_ResetAndCoreFollowActualRules()
        {
            var reset = Plain("mech.reset");
            Assert.That(reset, Does.Contain(MineResetService.BaseFeeGold + "G"));
            Assert.That(reset, Does.Contain("3시간"));
            Assert.That(reset, Does.Contain("초기화되는 것"));
            Assert.That(reset, Does.Contain("유지되는 것"));
            Assert.That(reset, Does.Contain("업그레이드"));

            var core = Plain("mech.core");
            Assert.That(core, Does.Contain("CCTV"));
            Assert.That(core, Does.Contain(ItemDisplayNames.Building(DataIds.Buildings.ChargerBasic)));
            Assert.That(core, Does.Contain(ItemDisplayNames.Building(DataIds.Buildings.ClinicBasic)));
            Assert.That(core, Does.Contain(ItemDisplayNames.Building(DataIds.Buildings.SettlementBasic)));
            Assert.That(Plain("mech.cargo"), Does.Contain("화물이 가득 찼습니다. 귀환해 화물을 비우세요."));
            Assert.That(Plain("mech.structure"), Does.Contain("점멸"));
        }

        [Test]
        public void FirstEnterStep_DescribesRealEntry_NotAnInventedElevatorBoarding()
        {
            var step = GameGuideCatalog.FirstSteps[0];
            Assert.That(step.Caption, Does.Contain("지하 탐사 시작"));
            Assert.That(Plain("ctrl.elevator"), Does.Contain("지하 탐사 시작"));
        }

        [Test]
        public void Detail_UsesCatalogValues_NotHardcodedNumbers()
        {
            var data = new FakeData();
            var detail = GuideDetailBuilder.Build(GameGuideCatalog.Get("res.copper"), data);
            var text = GuideDetailBuilder.ToPlainText(detail);
            Assert.That(text, Does.Contain("777G"));
            Assert.That(text, Does.Contain("3.5"));
            Assert.That(text, Does.Contain("업그레이드 재료"));

            var fuel = GuideDetailBuilder.Build(GameGuideCatalog.Get("res.fuel"), data);
            var fuelText = GuideDetailBuilder.ToPlainText(fuel);
            Assert.That(fuelText, Does.Contain("정산 콘솔에서는 정산할 수 없고"));
            Assert.That(fuelText, Does.Contain("최대 선택"));
            Assert.That(text, Does.Not.Contain("정산할 수 없고"), "일반 광물은 콘솔에서 판매할 수 있다");
        }

        [Test]
        public void Detail_PowerRequirementAndCostsComeFromBuildingData()
        {
            var data = new FakeData();
            data.BuildingList.Add(new GuideBuildingInfo(DataIds.Buildings.ChargerBasic, "충전기", null, string.Empty, 7,
                new[] { new GuideCost("구리", 9) }));
            data.BuildingList.Add(new GuideBuildingInfo(DataIds.Buildings.StorageBasic, "보관함", null, string.Empty, 0,
                new[] { new GuideCost("철", 4) }));

            var charger = GuideDetailBuilder.ToPlainText(GuideDetailBuilder.Build(GameGuideCatalog.Get("fac.charger"), data));
            Assert.That(charger, Does.Contain("구리 9"));
            Assert.That(charger, Does.Contain("전력망 연결이 필요한 시설"));
            var storage = GuideDetailBuilder.ToPlainText(GuideDetailBuilder.Build(GameGuideCatalog.Get("fac.storage"), data));
            Assert.That(storage, Does.Contain("철 4"));
            Assert.That(storage, Does.Contain("전력이 필요 없는 시설"));

            var grid = GuideDetailBuilder.ToPlainText(GuideDetailBuilder.Build(GameGuideCatalog.Get("mech.grid"), data));
            Assert.That(grid, Does.Contain("전력이 필요한 시설: 충전기"));
            Assert.That(grid, Does.Contain("전력이 필요 없는 시설: 보관함"));
        }

        [Test]
        public void KeyTokens_ParseKeysSeparatorsMouseAndArrows()
        {
            var tokens = GuideKeyTokens.Parse("MOUSE / Enter");
            Assert.That(tokens.Select(t => t.Kind).ToArray(),
                Is.EqualTo(new[] { GuideKeyTokenKind.Mouse, GuideKeyTokenKind.Slash, GuideKeyTokenKind.Key }));
            tokens = GuideKeyTokens.Parse("B -> C");
            Assert.That(tokens[1].Kind, Is.EqualTo(GuideKeyTokenKind.Arrow));
            tokens = GuideKeyTokens.Parse("M / Ctrl + M");
            Assert.That(tokens.Count, Is.EqualTo(5));
            Assert.That(tokens[3].Kind, Is.EqualTo(GuideKeyTokenKind.Plus));
            Assert.That(GuideKeyTokens.Parse(string.Empty), Is.Empty);
        }

        [Test]
        public void Font_CoversEveryGuideCharacter()
        {
            var skin = AssetDatabase.LoadAssetAtPath<MineResetTimedPopupSkin>("Assets/_Project/Resources/UI/MineResetTimedPopupSkin.asset");
            Assert.That(skin, Is.Not.Null);
            var source = skin.font != null ? skin.font.sourceFontFile : null;
            Assert.That(source, Is.Not.Null, "동적 폰트의 원본 폰트");
            var texts = new List<string>
            {
                GameGuidePopupView.Title, "접기", "펼치기", "관련 안내", "전체", "자원", "시설", "REC", "E키를 눌러 귀환", "구조 위험 주의!"
            };
            foreach (var card in GameGuideCatalog.All) texts.AddRange(AllText(card));
            foreach (var step in GameGuideCatalog.FirstSteps) { texts.Add(step.Title); texts.Add(step.Caption); }
            foreach (var clip in GuideDemoLibrary.All)
                foreach (var layer in clip.Layers)
                    if (layer.Kind == DemoLayerKind.Text) texts.Add(layer.Content);

            var missing = new HashSet<char>();
            foreach (var text in texts)
            {
                foreach (var c in text)
                {
                    if (!char.IsWhiteSpace(c) && !source.HasCharacter(c)) missing.Add(c);
                }
            }

            Assert.That(missing, Is.Empty, "폰트에 없는 글자: " + new string(missing.ToArray()));
        }

        // ---------------------------------------------------------------- 상태

        [Test]
        public void State_SelectCardMovesTabAndRevealsInList()
        {
            var state = new GameGuideState();
            Assert.That(state.Tab, Is.EqualTo(GuideTabKind.Controls));
            Assert.That(state.FirstExploreExpanded, Is.True, "처음에는 펼친 상태");
            Assert.That(state.SelectCard("mech.cargo"), Is.True);
            Assert.That(state.Tab, Is.EqualTo(GuideTabKind.Mechanics));
            Assert.That(state.CurrentCardId, Is.EqualTo("mech.cargo"));
            Assert.That(state.ConsumeReveal(), Is.EqualTo("mech.cargo"), "목록에서 보이도록 스크롤 요청");
            Assert.That(state.ConsumeReveal(), Is.Empty, "한 번 읽으면 지워진다");
            Assert.That(state.SelectCard("nope"), Is.False);

            state.SelectTab(GuideTabKind.Controls);
            Assert.That(state.CurrentCardId, Is.EqualTo("ctrl.move"), "탭별 기본은 첫 카드");
            state.SelectTab(GuideTabKind.Mechanics);
            Assert.That(state.CurrentCardId, Is.EqualTo("mech.cargo"), "탭을 돌아오면 선택 유지");
        }

        [Test]
        public void State_FilterHidesCards_AndLinkedCardReleasesFilter()
        {
            var state = new GameGuideState();
            state.SelectTab(GuideTabKind.Resources);
            state.SetFilter(GuideFilter.Resources);
            var shown = state.VisibleCards(GuideTabKind.Resources);
            Assert.That(shown.Count, Is.EqualTo(5));
            Assert.That(shown.All(c => c.Kind == GuideCardKind.Resource), Is.True);
            state.SetFilter(GuideFilter.Facilities);
            Assert.That(state.VisibleCards(GuideTabKind.Resources).Count, Is.EqualTo(10));
            Assert.That(state.CurrentCardId, Is.EqualTo("fac.storage"), "필터에 가려진 선택은 첫 보이는 카드로");

            state.SetFilter(GuideFilter.Resources);
            state.SelectCard("fac.clinic");
            Assert.That(state.Filter, Is.EqualTo(GuideFilter.All), "관련 안내로 필터에 가려진 카드를 열면 필터를 푼다");
            Assert.That(state.CurrentCardId, Is.EqualTo("fac.clinic"));
        }

        [Test]
        public void State_SessionIsSharedUntilReset_AndIntroFlagResets()
        {
            GameGuideState.Session.SelectCard("ctrl.mine");
            GameGuideState.Session.SetFirstExplore(false);
            GameGuideState.IntroPlayed = true;
            Assert.That(GameGuideState.Session.CurrentCardId, Is.EqualTo("ctrl.mine"));
            Assert.That(GameGuideState.Session.FirstExploreExpanded, Is.False);
            GameGuideState.ResetSessionForTests();
            Assert.That(GameGuideState.Session.CurrentCardId, Is.EqualTo("ctrl.move"));
            Assert.That(GameGuideState.Session.FirstExploreExpanded, Is.True);
            Assert.That(GameGuideState.IntroPlayed, Is.False);
        }

        // ---------------------------------------------------------------- 시간표

        [Test]
        public void Timeline_DurationsMatchRequestedRanges()
        {
            Assert.That(GameGuideTimeline.FullOpenDuration, Is.InRange(1.0f, 1.3f), "첫 등장 1~1.3초");
            Assert.That(GameGuideTimeline.QuickOpenDuration, Is.InRange(0.5f, 0.7f), "재열기 0.5~0.7초");
            Assert.That(GameGuideTimeline.CloseDuration, Is.InRange(0.3f, 0.5f), "닫기 0.3~0.5초");
        }

        [Test]
        public void Timeline_Open_BookRisesPastCenterThenSettles()
        {
            var peak = float.MinValue;
            var previous = float.MinValue;
            var wentBack = false;
            for (var t = 0f; t <= 0.6f; t += 0.005f)
            {
                var frame = GameGuideTimeline.Open(t, true);
                peak = Mathf.Max(peak, frame.BookY);
                if (previous > float.MinValue && frame.BookY < previous - 0.001f && frame.BookY < peak - 1f) wentBack = true;
                previous = frame.BookY;
            }

            var start = GameGuideTimeline.Open(0f, true);
            Assert.That(start.BookY, Is.LessThan(-100f), "화면 중앙보다 아래에서 시작");
            Assert.That(start.BookScale, Is.LessThan(0.5f), "작게 시작");
            Assert.That(start.BookAlpha, Is.Zero);
            Assert.That(peak, Is.GreaterThan(5f), "중앙을 조금 지나친다");
            Assert.That(peak, Is.LessThan(40f), "과하게 흔들리지 않는다");
            Assert.That(wentBack, Is.True, "짧게 되돌아온다");
            Assert.That(GameGuideTimeline.Open(0.6f, true).BookY, Is.EqualTo(0f).Within(0.01f));
            Assert.That(GameGuideTimeline.Open(0.6f, true).BookScale, Is.EqualTo(1f).Within(0.01f));
        }

        [Test]
        public void Timeline_Open_PhasesRunInOrder_AndContentAppearsAfterFrame()
        {
            var full = GameGuideTimeline.FullOpenDuration;
            var cover = FirstTime(t => GameGuideTimeline.Open(t, true).CoverOpen > 0.01f);
            var flip = FirstTime(t => GameGuideTimeline.Open(t, true).Flip0 > 0.01f);
            var expand = FirstTime(t => GameGuideTimeline.Open(t, true).Expand > 0.01f);
            var panel = FirstTime(t => GameGuideTimeline.Open(t, true).PanelAlpha > 0.01f);
            var tabs = FirstTime(t => GameGuideTimeline.Open(t, true).Tabs > 0.01f);
            var list = FirstTime(t => GameGuideTimeline.Open(t, true).List > 0.01f);
            var detail = FirstTime(t => GameGuideTimeline.Open(t, true).Detail > 0.01f);
            Assert.That(cover, Is.LessThan(flip), "표지가 열린 뒤 페이지가 넘어간다");
            Assert.That(flip, Is.LessThan(expand), "페이지 뒤 창으로 변형");
            Assert.That(expand, Is.LessThanOrEqualTo(panel));
            Assert.That(tabs, Is.LessThan(list));
            Assert.That(list, Is.LessThan(detail), "탭 → 목록 → 상세 순서");
            Assert.That(GameGuideTimeline.Open(tabs, true).Expand, Is.GreaterThan(0.95f), "프레임이 최종 크기에 닿은 뒤 내용");
            var end = GameGuideTimeline.Open(full, true);
            Assert.That(end.Expand, Is.EqualTo(1f).Within(1e-4f));
            Assert.That(end.PanelAlpha, Is.EqualTo(1f).Within(1e-4f));
            Assert.That(end.Detail, Is.EqualTo(1f).Within(1e-4f));
            Assert.That(end.PageAlpha, Is.LessThan(0.01f), "책 페이지 면은 사라진다");

            AssertMonotonic(t => GameGuideTimeline.Open(t, true).Expand, full, "expand");
            AssertMonotonic(t => GameGuideTimeline.Open(t, true).PanelAlpha, full, "panel");
        }

        [Test]
        public void Timeline_FullIntroFlipsThreePages_QuickFlipsOne()
        {
            var fullFlips = 0;
            var quickFlips = 0;
            for (var t = 0f; t <= GameGuideTimeline.FullOpenDuration; t += 0.004f)
            {
                var f = GameGuideTimeline.Open(t, true);
                fullFlips = Mathf.Max(fullFlips, Count(f));
            }

            for (var t = 0f; t <= GameGuideTimeline.QuickOpenDuration; t += 0.004f)
            {
                var f = GameGuideTimeline.Open(t, false);
                quickFlips = Mathf.Max(quickFlips, Count(f));
            }

            Assert.That(fullFlips, Is.InRange(2, 3), "페이지 2~3장이 빠르게 넘어간다");
            Assert.That(quickFlips, Is.EqualTo(1), "재열기는 페이지 넘김을 줄인다");
        }

        [Test]
        public void Timeline_Close_ContentFirstThenFrameShrinksThenBookCloses()
        {
            var open = GameGuideTimeline.Open(GameGuideTimeline.FullOpenDuration, true);
            var contentGone = FirstCloseTime(u => GameGuideTimeline.Close(u, open).MaxContent < 0.01f);
            var shrunk = FirstCloseTime(u => GameGuideTimeline.Close(u, open).Expand < 0.01f);
            var bookClosed = FirstCloseTime(u => GameGuideTimeline.Close(u, open).CoverOpen < 0.01f);
            var gone = FirstCloseTime(u => GameGuideTimeline.Close(u, open).BookAlpha < 0.01f);
            Assert.That(contentGone, Is.LessThan(shrunk), "내용이 먼저 사라진다");
            Assert.That(shrunk, Is.LessThanOrEqualTo(bookClosed), "프레임이 책 크기가 된 뒤 책이 닫힌다");
            Assert.That(bookClosed, Is.LessThanOrEqualTo(gone));
            Assert.That(GameGuideTimeline.Close(GameGuideTimeline.CloseDuration, open).BookScale, Is.LessThan(0.6f), "작아지며 사라진다");
            for (var u = 0f; u <= GameGuideTimeline.CloseDuration; u += 0.005f)
            {
                var frame = GameGuideTimeline.Close(u, open);
                Assert.That(Count(frame), Is.Zero, "닫을 때 페이지 넘김을 반복하지 않는다");
            }

            AssertMonotonicDown(u => GameGuideTimeline.Close(u, open).Expand, GameGuideTimeline.CloseDuration, "expand");
            AssertMonotonicDown(u => GameGuideTimeline.Close(u, open).Detail, GameGuideTimeline.CloseDuration, "detail");
        }

        [Test]
        public void Timeline_CloseDuringOpen_NeverExceedsTheValuesAtInterruption()
        {
            foreach (var t in new[] { 0.2f, 0.5f, 0.8f, 0.95f, 1.1f })
            {
                var from = GameGuideTimeline.Open(t, true);
                for (var u = 0f; u <= GameGuideTimeline.CloseDuration; u += 0.01f)
                {
                    var frame = GameGuideTimeline.Close(u, from);
                    Assert.That(frame.Expand, Is.LessThanOrEqualTo(from.Expand + 1e-4f), "t=" + t);
                    Assert.That(frame.Detail, Is.LessThanOrEqualTo(from.Detail + 1e-4f), "t=" + t);
                    Assert.That(frame.PanelAlpha, Is.LessThanOrEqualTo(from.PanelAlpha + 1e-4f), "t=" + t);
                    Assert.That(frame.BookScale, Is.LessThanOrEqualTo(Mathf.Max(from.BookScale, 1f) + 1e-4f), "t=" + t);
                }
            }
        }

        [Test]
        public void Timeline_ReopenWhileClosing_ResumesAtMatchingExpand()
        {
            foreach (var full in new[] { true, false })
            {
                var t = GameGuideTimeline.OpenTimeForExpand(0.5f, full);
                Assert.That(GameGuideTimeline.Open(t, full).Expand, Is.EqualTo(0.5f).Within(0.02f));
                Assert.That(GameGuideTimeline.OpenTimeForExpand(0f, full), Is.Zero);
            }
        }

        // ---------------------------------------------------------------- 시연

        [Test]
        public void Demo_AllClipsUseSpritesThatExistInTheSkin()
        {
            var skin = AssetDatabase.LoadAssetAtPath<GameGuideSkin>(PromptB140GameGuideSkinBuilder.SkinAssetPath);
            Assert.That(skin, Is.Not.Null, "SubTerra/UI/Build Prompt-B 140 Game Guide Skin으로 생성");
            var sprites = new GuideSprites(skin, null);
            foreach (var clip in GuideDemoLibrary.All)
            {
                foreach (var key in clip.UsedSpriteKeys())
                {
                    if (key.StartsWith("item:") || key.StartsWith("bld:")) continue;
                    Assert.That(sprites.Get(key), Is.Not.Null, clip.Id + " -> " + key);
                }
            }
        }

        [Test]
        public void Demo_TracksInterpolateHoldAndPulse()
        {
            var smooth = DemoTrack.Smooth(0f, 0f, 1f, 10f);
            Assert.That(smooth.Eval(-1f), Is.Zero);
            Assert.That(smooth.Eval(0.5f), Is.EqualTo(5f).Within(0.01f));
            Assert.That(smooth.Eval(2f), Is.EqualTo(10f));
            var hold = DemoTrack.Hold(0f, 1f, 1f, 2f, 2f, 3f);
            Assert.That(hold.Eval(0.99f), Is.EqualTo(1f));
            Assert.That(hold.Eval(1.01f), Is.EqualTo(2f));
            var pulse = DemoTrack.Pulse(0.5f, 1f);
            Assert.That(pulse.Eval(0.2f), Is.Zero);
            Assert.That(pulse.Eval(0.8f), Is.EqualTo(1f));
            Assert.That(pulse.Eval(2f), Is.Zero);
            Assert.Throws<System.ArgumentException>(() => DemoTrack.Smooth(0f));
        }

        // ---------------------------------------------------------------- 창 구조

        [Test]
        public void Popup_BuildsThreeTabsFirstExploreCardAndSeparateScrolls()
        {
            using (var rig = new PromptB140GuideCaptureRig(960, 540))
            {
                var popup = rig.Popup;
                popup.Show();
                rig.Advance(GameGuideTimeline.FullOpenDuration + 0.1f);
                Assert.That(popup.State, Is.EqualTo(GameGuidePopupView.PopupState.Open));
                Assert.That(popup.IsFullIntro, Is.True);
                Assert.That(popup.TitleString, Is.EqualTo("SUB-TERRA 게임 가이드"));
                Assert.That(popup.TabButtons.Count, Is.EqualTo(3));
                Assert.That(popup.TabButtons.Select(b => b.GetComponentInChildren<TMP_Text>().text).ToArray(),
                    Is.EqualTo(new[] { "기본 조작", "핵심 메커니즘", "자원·시설" }));
                Assert.That(popup.CloseButton, Is.Not.Null);
                Assert.That(popup.StepTiles.Count, Is.EqualTo(5));
                Assert.That(popup.FirstExploreVisible, Is.True);
                Assert.That(popup.FirstExploreHeight, Is.GreaterThan(200f), "처음에는 펼친 상태");
                Assert.That(popup.Cards(GuideTabKind.Controls).Count, Is.EqualTo(11));
                Assert.That(popup.ListScroll(GuideTabKind.Controls), Is.Not.SameAs(popup.DetailScroll),
                    "목록과 상세는 서로 다른 스크롤");
                Assert.That(popup.ListScroll(GuideTabKind.Controls).vertical, Is.True);
                Assert.That(popup.DetailScroll.vertical, Is.True);
                Assert.That(popup.ListScroll(GuideTabKind.Controls).content.IsChildOf(popup.DetailScroll.transform), Is.False);
                Assert.That(popup.DetailStage.Clip, Is.Not.Null);
                Assert.That(popup.IsDemoPlaying, Is.True, "상세 시연은 열림이 끝난 뒤 재생");
                Assert.That(popup.BlocksInput, Is.True);
            }
        }

        [Test]
        public void Popup_TopAreaNeverMovesWhenTabsOrFirstExploreChange()
        {
            using (var rig = new PromptB140GuideCaptureRig(960, 540))
            {
                var popup = rig.Popup;
                popup.Show();
                rig.Advance(1.5f);
                var names = new[] { "Title", "CloseButton", "Tab_0", "Tab_1", "Tab_2" };
                var before = names.ToDictionary(n => n, n => Snapshot(popup, n));
                var state = popup.GuideState;
                state.SetFirstExplore(false);
                rig.Advance(0.5f);
                AssertSame(popup, names, before, "첫 탐사 안내 접기");
                state.SelectTab(GuideTabKind.Mechanics);
                rig.Advance(0.3f);
                AssertSame(popup, names, before, "핵심 메커니즘 탭");
                state.SelectTab(GuideTabKind.Resources);
                rig.Advance(0.3f);
                AssertSame(popup, names, before, "자원·시설 탭");
                state.SelectTab(GuideTabKind.Controls);
                state.SetFirstExplore(true);
                rig.Advance(0.5f);
                AssertSame(popup, names, before, "다시 기본 조작·펼침");

                var tabs = names.Skip(2).Select(n => Snapshot(popup, n)).ToArray();
                Assert.That(tabs[0].size, Is.EqualTo(tabs[1].size));
                Assert.That(tabs[1].size, Is.EqualTo(tabs[2].size), "세 탭은 같은 폭");
                var firstTabLeft = tabs[0].pos.x - tabs[0].size.x * 0.5f;
                var lastTabRight = tabs[2].pos.x + tabs[2].size.x * 0.5f;
                Assert.That(firstTabLeft, Is.EqualTo(72f).Within(0.5f), "탭 영역은 본문 폭 전체를 균등 분할");
                Assert.That(lastTabRight, Is.EqualTo(GameGuidePopupView.CardWidth - 72f).Within(0.5f));
            }
        }

        [Test]
        public void Popup_FirstExploreCollapseKeepsContentAndSessionState()
        {
            using (var rig = new PromptB140GuideCaptureRig(960, 540))
            {
                var popup = rig.Popup;
                popup.Show();
                rig.Advance(1.5f);
                var expanded = popup.FirstExploreHeight;
                popup.GuideState.SetFirstExplore(false);
                rig.Advance(0.5f);
                Assert.That(popup.FirstExploreHeight, Is.LessThan(expanded * 0.5f), "접으면 아이콘·제목 한 줄만 남는다");
                popup.GuideState.SelectCard("ctrl.build");
                popup.GuideState.SelectTab(GuideTabKind.Mechanics);
                popup.BeginClose();
                rig.Advance(GameGuideTimeline.CloseDuration + 0.05f);
                Assert.That(popup.State, Is.EqualTo(GameGuidePopupView.PopupState.Hidden));

                popup.Show();
                rig.Advance(GameGuideTimeline.QuickOpenDuration + 0.05f);
                Assert.That(popup.IsFullIntro, Is.False, "같은 세션의 재열기는 짧은 연출");
                Assert.That(popup.GuideState.Tab, Is.EqualTo(GuideTabKind.Mechanics), "선택한 탭 유지");
                Assert.That(popup.GuideState.SelectedId(GuideTabKind.Controls), Is.EqualTo("ctrl.build"), "선택한 카드 유지");
                Assert.That(popup.GuideState.FirstExploreExpanded, Is.False, "안내 카드 펼침 상태 유지");
            }
        }

        [Test]
        public void Popup_ResourcesTabHasFilterAndIconGrid()
        {
            using (var rig = new PromptB140GuideCaptureRig(960, 540))
            {
                var popup = rig.Popup;
                popup.Show();
                rig.Advance(1.5f);
                popup.GuideState.SelectTab(GuideTabKind.Resources);
                rig.Advance(0.3f);
                Assert.That(popup.FilterButtons.Select(b => b.GetComponentInChildren<TMP_Text>().text).ToArray(),
                    Is.EqualTo(new[] { "전체", "자원", "시설" }));
                var cards = popup.Cards(GuideTabKind.Resources);
                Assert.That(cards.Count(c => c.gameObject.activeSelf), Is.EqualTo(15));
                Assert.That(cards.All(c => c.CardLayout == GameGuideCardView.Layout.Icon), Is.True);
                popup.GuideState.SetFilter(GuideFilter.Resources);
                rig.Advance(0.2f);
                Assert.That(cards.Count(c => c.gameObject.activeSelf), Is.EqualTo(5));
                popup.GuideState.SetFilter(GuideFilter.Facilities);
                rig.Advance(0.2f);
                Assert.That(cards.Count(c => c.gameObject.activeSelf), Is.EqualTo(10));
                Assert.That(popup.FilterButtons[2].Selected, Is.True);
                Assert.That(popup.FilterButtons[0].Selected, Is.False);
            }
        }

        [Test]
        public void Popup_RelatedLinkMovesToTargetTabAndCard()
        {
            using (var rig = new PromptB140GuideCaptureRig(960, 540))
            {
                var popup = rig.Popup;
                popup.Show();
                rig.Advance(1.5f);
                var tile = popup.StepTiles[2];
                tile.Button.onClick.Invoke();
                rig.Advance(0.3f);
                Assert.That(popup.GuideState.Tab, Is.EqualTo(GuideTabKind.Mechanics), "상태 확인 단계 → 체력과 회복");
                Assert.That(popup.GuideState.CurrentCardId, Is.EqualTo("mech.health"));
                Assert.That(popup.ShownCardId, Is.EqualTo("mech.health"));
                var link = popup.GetComponentsInChildren<GameGuideButton>(true)
                    .First(b => b.name == "Related_fac.clinic");
                link.Button.onClick.Invoke();
                rig.Advance(0.3f);
                Assert.That(popup.GuideState.Tab, Is.EqualTo(GuideTabKind.Resources));
                Assert.That(popup.ShownCardId, Is.EqualTo("fac.clinic"));
                var clinicCard = popup.Cards(GuideTabKind.Resources).First(c => c.Def.Id == "fac.clinic");
                Assert.That(clinicCard.IsSelected, Is.True);
            }
        }

        [Test]
        public void Popup_SelectingFarCardScrollsItIntoTheListViewport()
        {
            using (var rig = new PromptB140GuideCaptureRig(960, 540))
            {
                var popup = rig.Popup;
                popup.Show();
                rig.Advance(1.5f);
                var list = popup.ListScroll(GuideTabKind.Controls);
                Assert.That(list.content.anchoredPosition.y, Is.Zero.Within(0.5f), "처음에는 맨 위");
                popup.GuideState.SelectCard("ctrl.guide");
                rig.Advance(0.1f);
                Canvas.ForceUpdateCanvases();
                Assert.That(list.content.anchoredPosition.y, Is.GreaterThan(50f), "목록이 해당 카드까지 스크롤됨");
                var card = popup.Cards(GuideTabKind.Controls).First(c => c.Def.Id == "ctrl.guide");
                var top = -(card.Rect.anchoredPosition.y + card.Rect.sizeDelta.y * 0.5f);
                var bottom = top + card.Rect.sizeDelta.y;
                var viewport = list.viewport.rect.height;
                Assert.That(top, Is.GreaterThanOrEqualTo(list.content.anchoredPosition.y - 0.5f));
                Assert.That(bottom, Is.LessThanOrEqualTo(list.content.anchoredPosition.y + viewport + 0.5f), "카드가 뷰포트 안");
            }
        }

        [Test]
        public void Button_HoverAndFocusUseBrightTealWithDarkText()
        {
            using (var rig = new PromptB140GuideCaptureRig(960, 540))
            {
                var popup = rig.Popup;
                popup.Show();
                rig.Advance(1.5f);
                var close = popup.CloseButton;
                close.Step(1f);
                var idleFace = close.FaceColor;
                var idleContent = close.ContentColor;
                Assert.That(idleFace.b, Is.LessThan(0.2f), "평상시 진한 남색");
                Assert.That(idleContent.r + idleContent.g + idleContent.b, Is.GreaterThan(2.7f), "평상시 흰 글자");

                close.OnPointerEnter(null);
                close.Step(1f);
                var hover = close.FaceColor;
                Assert.That(hover.g, Is.GreaterThan(0.85f), "호버: 밝은 청록 면");
                Assert.That(close.ContentColor.r + close.ContentColor.g + close.ContentColor.b, Is.LessThan(0.5f), "호버: 어두운 글자");
                close.OnPointerExit(null);
                close.Step(1f);
                Assert.That(close.FaceColor, Is.EqualTo(idleFace), "해제하면 처음 값으로 돌아온다(누적 없음)");

                close.OnSelect(null);
                close.Step(1f);
                Assert.That(close.FaceColor.g, Is.GreaterThan(0.85f), "키보드 포커스도 같은 강조");
                close.OnDeselect(null);

                var tab = popup.TabButtons[0];
                Assert.That(tab.Selected, Is.True);
                tab.Step(1f);
                Assert.That(tab.UnderlineVisible, Is.True, "선택된 탭: 하단 청록선");
                Assert.That(tab.BorderColor.a, Is.EqualTo(1f).Within(0.01f), "선택된 탭: 밝은 청록 테두리");
                Assert.That(popup.TabButtons[1].UnderlineVisible, Is.False);
            }
        }

        [Test]
        public void Sprites_PortalHasNoGameIconSoItFallsBackToTheDrawnPortal()
        {
            var sprites = new GuideSprites(null, null);
            Assert.That(sprites.Get("bld:" + DataIds.Buildings.EmergencyEscapePortal), Is.Not.Null, "그린 포탈은 스킨 없이도 나온다");
            Assert.That(sprites.Get("bld:" + DataIds.Buildings.ChargerBasic), Is.Null, "스킨이 없으면 월드 스프라이트도 없다");
            var skin = AssetDatabase.LoadAssetAtPath<GameGuideSkin>(PromptB140GameGuideSkinBuilder.SkinAssetPath);
            var withSkin = new GuideSprites(skin, null);
            var portal = withSkin.Get("bld:" + DataIds.Buildings.EmergencyEscapePortal);
            Assert.That(portal, Is.Not.Null, "카탈로그의 2x2 자리표시 아이콘 대신 월드 프리팹 구성으로 그린 포탈");
            Assert.That(GuideSprites.SceneKeyFor(DataIds.Buildings.ChargerBasic), Is.EqualTo("fac.charger"));
        }

        [Test]
        public void Popup_TextFitsItsBoxInEveryTab_AtReferenceResolution()
        {
            using (var rig = new PromptB140GuideCaptureRig(960, 540))
            {
                var popup = rig.Popup;
                popup.Show();
                rig.Advance(1.5f);
                var problems = new List<string>();
                foreach (var tab in new[] { GuideTabKind.Controls, GuideTabKind.Mechanics, GuideTabKind.Resources })
                {
                    popup.GuideState.SelectTab(tab);
                    foreach (var card in GameGuideCatalog.ForTab(tab))
                    {
                        popup.GuideState.SelectCard(card.Id);
                        rig.Advance(0.02f);
                        Canvas.ForceUpdateCanvases();
                        foreach (var text in popup.GetComponentsInChildren<TMP_Text>(false))
                        {
                            if (text.GetComponentInParent<GameGuideStageView>() != null) continue;
                            if (text.textWrappingMode != TextWrappingModes.NoWrap) continue;
                            if (text.enableAutoSizing) continue;
                            if (text.overflowMode == TextOverflowModes.Ellipsis) continue;
                            var width = text.rectTransform.rect.width;
                            var preferred = text.GetPreferredValues(text.text).x;
                            if (preferred > width + 2f && width > 1f)
                                problems.Add(tab + "/" + card.Id + ": '" + text.text + "' " + preferred.ToString("0") + ">" + width.ToString("0"));
                        }
                    }
                }

                Assert.That(problems.Distinct().ToArray(), Is.Empty, string.Join("\n", problems.Distinct().Take(10)));
            }
        }

        [Test]
        public void Popup_CloseClearsInputBlockers_AndStopsDemo()
        {
            using (var rig = new PromptB140GuideCaptureRig(960, 540))
            {
                var popup = rig.Popup;
                popup.Show();
                rig.Advance(0.5f);
                Assert.That(popup.BlocksInput, Is.True, "등장 중에도 클릭이 게임으로 통과하지 않는다");
                popup.BeginClose();
                Assert.That(popup.IsDemoPlaying, Is.False, "닫기를 시작하면 시연 재생을 멈춘다");
                rig.Advance(0.1f);
                Assert.That(popup.IsClosing, Is.True);
                rig.Advance(GameGuideTimeline.CloseDuration);
                Assert.That(popup.State, Is.EqualTo(GameGuidePopupView.PopupState.Hidden));
                Assert.That(popup.gameObject.activeSelf, Is.False, "닫은 뒤 투명 패널이 입력을 막지 않는다");
                Assert.That(popup.BlocksInput, Is.False);
                Assert.That(popup.IsDemoPlaying, Is.False);
            }
        }

        [Test]
        public void Popup_RapidOpenCloseKeepsASingleWindow()
        {
            using (var rig = new PromptB140GuideCaptureRig(960, 540))
            {
                var popup = rig.Popup;
                for (var i = 0; i < 6; i++)
                {
                    popup.Show();
                    rig.Advance(0.07f * (i + 1));
                    popup.BeginClose();
                    rig.Advance(0.05f);
                }

                popup.Show();
                rig.Advance(2f);
                Assert.That(Object.FindObjectsByType<GameGuidePopupView>(FindObjectsInactive.Include).Length, Is.EqualTo(1));
                Assert.That(popup.State, Is.EqualTo(GameGuidePopupView.PopupState.Open));
                Assert.That(popup.GetComponentsInChildren<GameGuideStageView>(true).Count(s => s.IsPlaying), Is.EqualTo(1),
                    "재생 중인 시연은 상세 하나뿐");
                popup.BeginClose();
                rig.Advance(1f);
                Assert.That(popup.gameObject.activeSelf, Is.False);
            }
        }

        [Test]
        public void Popup_DemoStopsWhenCardChangesAndRestartsForNewClip()
        {
            using (var rig = new PromptB140GuideCaptureRig(960, 540))
            {
                var popup = rig.Popup;
                popup.Show();
                rig.Advance(1.5f);
                var first = popup.DetailStage.Clip;
                rig.Advance(0.4f);
                Assert.That(popup.DetailStage.Clock, Is.GreaterThan(0.3f));
                popup.GuideState.SelectCard("ctrl.ladder");
                rig.Advance(0.02f);
                Assert.That(popup.DetailStage.Clip, Is.Not.SameAs(first), "카드가 바뀌면 이전 재생을 정리하고 새 장면");
                Assert.That(popup.DetailStage.Clock, Is.LessThan(0.2f));
                Assert.That(popup.DetailStage.IsPlaying, Is.True);
                popup.GuideState.SelectTab(GuideTabKind.Mechanics);
                rig.Advance(0.02f);
                Assert.That(popup.DetailStage.Clip.Id, Is.EqualTo("base"));
            }
        }

        [Test]
        public void HostView_KeepsLegacyContractsAndMapsTabs()
        {
            var host = new GameObject("GuideHost");
            try
            {
                var view = host.AddComponent<GameGuidePanelView>();
                Assert.That(GameGuidePanelView.TabCount, Is.EqualTo(3));
                Assert.That(GameGuidePanelView.GetTabTitle(GameGuidePanelView.GuideTab.Resources), Is.EqualTo("자원·시설"));
                view.SelectTab(GameGuidePanelView.GuideTab.Mechanics);
                Assert.That(view.ActiveTab, Is.EqualTo(GameGuidePanelView.GuideTab.Mechanics));
                view.SelectTabIndex(9);
                Assert.That(view.ActiveTab, Is.EqualTo(GameGuidePanelView.GuideTab.Mechanics), "범위 밖 인덱스는 무시");
                Assert.That(GameGuidePanelView.GetTabBody(GameGuidePanelView.GuideTab.Controls), Does.Contain("채굴"));
                Assert.That(view.OwnsCanvas(null), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        // ---------------------------------------------------------------- 도우미

        private static IEnumerable<string> AllText(GuideCardDef card)
        {
            yield return card.Title;
            yield return card.Summary;
            foreach (var line in card.Lines)
            {
                yield return line.Label;
                yield return line.Text;
            }
        }

        private static string Plain(string id)
        {
            return GuideDetailBuilder.ToPlainText(GuideDetailBuilder.Build(GameGuideCatalog.Get(id), null));
        }

        private static (Vector2 pos, Vector2 size) Snapshot(GameGuidePopupView popup, string name)
        {
            var rect = popup.GetComponentsInChildren<RectTransform>(true).First(r => r.name == name);
            return (rect.anchoredPosition, rect.sizeDelta);
        }

        private static void AssertSame(GameGuidePopupView popup, string[] names, Dictionary<string, (Vector2 pos, Vector2 size)> before, string when)
        {
            foreach (var name in names)
            {
                var now = Snapshot(popup, name);
                Assert.That(now.pos.x, Is.EqualTo(before[name].pos.x).Within(0.01f), name + " x (" + when + ")");
                Assert.That(now.pos.y, Is.EqualTo(before[name].pos.y).Within(0.01f), name + " y (" + when + ")");
                Assert.That(now.size, Is.EqualTo(before[name].size), name + " size (" + when + ")");
            }
        }

        private static int Count(GuideFrame f)
        {
            return (f.Flip0 > 0.001f && f.Flip0 < 0.999f ? 1 : 0)
                + (f.Flip1 > 0.001f && f.Flip1 < 0.999f ? 1 : 0)
                + (f.Flip2 > 0.001f && f.Flip2 < 0.999f ? 1 : 0);
        }

        private static float FirstTime(System.Func<float, bool> predicate)
        {
            for (var t = 0f; t <= GameGuideTimeline.FullOpenDuration; t += 0.002f)
            {
                if (predicate(t)) return t;
            }

            return float.MaxValue;
        }

        private static float FirstCloseTime(System.Func<float, bool> predicate)
        {
            for (var u = 0f; u <= GameGuideTimeline.CloseDuration + 0.001f; u += 0.002f)
            {
                if (predicate(u)) return u;
            }

            return float.MaxValue;
        }

        private static void AssertMonotonic(System.Func<float, float> f, float duration, string label)
        {
            var previous = f(0f);
            for (var t = 0.004f; t <= duration; t += 0.004f)
            {
                var value = f(t);
                Assert.That(value, Is.GreaterThanOrEqualTo(previous - 1e-4f), label + " t=" + t);
                previous = value;
            }
        }

        private static void AssertMonotonicDown(System.Func<float, float> f, float duration, string label)
        {
            var previous = f(0f);
            for (var t = 0.004f; t <= duration; t += 0.004f)
            {
                var value = f(t);
                Assert.That(value, Is.LessThanOrEqualTo(previous + 1e-4f), label + " u=" + t);
                previous = value;
            }
        }
    }
}

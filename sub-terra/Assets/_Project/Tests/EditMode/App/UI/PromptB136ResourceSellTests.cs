using System.Collections.Generic;
using NUnit.Framework;
using SubTerra.App.Core.Data;
using SubTerra.App.Economy;
using SubTerra.App.Inventory;
using SubTerra.App.Outpost;
using SubTerra.App.State;
using SubTerra.App.UI.Economy;
using SubTerra.App.UI.Outpost;
using SubTerra.App.UI.Sell;
using SubTerra.Shared;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SubTerra.App.Tests.UI
{
    /// <summary>B-136 지상 판매창·정산 콘솔 공통 판매 UI: 수량 규칙, 예상 결과, 확정 재검증, 일괄 거래, 연출 상태.</summary>
    public sealed class PromptB136ResourceSellTests
    {
        private const string Copper = DataIds.Minerals.Copper;
        private const string Iron = DataIds.Minerals.Iron;
        private const string Lithium = DataIds.Minerals.Lithium;
        private const string Fuel = DataIds.RareItems.EngineFuel;

        private readonly List<GameObject> created = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            for (var i = 0; i < created.Count; i++)
            {
                if (created[i] != null)
                {
                    Object.DestroyImmediate(created[i]);
                }
            }

            created.Clear();
        }

        // ---------- 수량 규칙 ----------

        [Test]
        public void PlusFive_Accumulates_TwoBecomesSeventeenAfterThreePresses()
        {
            var session = Session(new FakeBackend().With(Copper, 30, 10));
            session.Adjust(Copper, 2);
            for (var i = 0; i < 3; i++)
            {
                session.Adjust(Copper, ResourceSellRowView.PlusFive);
            }

            Assert.That(session.GetQuantity(Copper), Is.EqualTo(17));
        }

        [Test]
        public void PlusFive_StopsAtSellableQuantity()
        {
            var session = Session(new FakeBackend().With(Copper, 13, 10));
            session.Adjust(Copper, 12);
            session.Adjust(Copper, ResourceSellRowView.PlusFive);
            Assert.That(session.GetQuantity(Copper), Is.EqualTo(13));
            session.Adjust(Copper, ResourceSellRowView.PlusTen);
            Assert.That(session.GetQuantity(Copper), Is.EqualTo(13));
        }

        [Test]
        public void Quantity_NeverBelowZero_AndMaxSetsOwned()
        {
            var session = Session(new FakeBackend().With(Copper, 7, 10));
            Assert.That(session.Adjust(Copper, -1), Is.False);
            Assert.That(session.GetQuantity(Copper), Is.Zero);
            session.SetMax(Copper);
            Assert.That(session.GetQuantity(Copper), Is.EqualTo(7));
            session.Adjust(Copper, -1);
            Assert.That(session.GetQuantity(Copper), Is.EqualTo(6));
            session.Adjust(Copper, int.MaxValue);
            Assert.That(session.GetQuantity(Copper), Is.EqualTo(7));
        }

        [Test]
        public void SelectAll_FillsOnlyBulkRows_DoesNotSell_AndKeepsManualRareQuantity()
        {
            var backend = new FakeBackend().With(Copper, 4, 10).With(Iron, 3, 15).WithRare(Fuel, 2, 100, canSell: true);
            var session = Session(backend);
            session.Adjust(Fuel, 1);
            session.SelectAllBulk();

            Assert.That(session.GetQuantity(Copper), Is.EqualTo(4));
            Assert.That(session.GetQuantity(Iron), Is.EqualTo(3));
            Assert.That(session.GetQuantity(Fuel), Is.EqualTo(1), "희귀 품목은 최대 선택 대상이 아니다");
            Assert.That(backend.Commits, Is.Zero, "최대 선택은 즉시 판매하지 않는다");
            Assert.That(session.HasBulkTargets, Is.False);
            Assert.That(session.BulkRuleNotice, Is.Not.Empty, "제외 품목이 있으면 규칙 안내가 보인다");

            session.ResetSelection();
            Assert.That(session.Selection.IsEmpty, Is.True);
        }

        [Test]
        public void SettlementRareRow_CannotBeAdjusted()
        {
            var session = Session(new FakeBackend().With(Copper, 4, 10).WithRare(Fuel, 2, 100, canSell: false));
            Assert.That(session.Adjust(Fuel, 1), Is.False);
            Assert.That(session.SetMax(Fuel), Is.False);
            session.SelectAllBulk();
            Assert.That(session.GetQuantity(Fuel), Is.Zero);
        }

        [Test]
        public void Open_ResetsQuantitiesAndReadsLatestValues()
        {
            var backend = new FakeBackend().With(Copper, 5, 10);
            var session = Session(backend);
            session.Adjust(Copper, 3);
            backend.SetOwned(Copper, 9);
            backend.Gold = 777;
            session.Open();
            Assert.That(session.GetQuantity(Copper), Is.Zero);
            Assert.That(session.Snapshot.Lines[0].Owned, Is.EqualTo(9));
            Assert.That(session.Snapshot.Gold, Is.EqualTo(777));
        }

        // ---------- 예상 결과 ----------

        [Test]
        public void Quote_UsesRealPricesAndUnitWeights_NotItemCount()
        {
            var backend = new FakeBackend().With(Copper, 13, 10, 1.5f).With(Lithium, 7, 40, 1f);
            backend.Gold = 10335;
            backend.Weight = 32.5f;
            var session = Session(backend);
            session.Adjust(Copper, 5);
            session.Adjust(Lithium, 2);
            var quote = session.Quote;

            Assert.That(quote.TotalQuantity, Is.EqualTo(7));
            Assert.That(quote.TotalGold, Is.EqualTo(130));
            Assert.That(quote.GoldAfter, Is.EqualTo(10465));
            Assert.That(quote.CargoAfter, Is.EqualTo(32.5f - 7.5f - 2f).Within(0.001f), "화물은 개수가 아닌 실제 무게로 계산");
        }

        [Test]
        public void Quote_BonusRule_FollowsEachScreen()
        {
            // 15G·15G 두 줄, 보너스 50%: 행 단위 반올림 8+8=16, 합계 반올림 15 → 화면별 실제 규칙이 다르다.
            var perLine = new FakeBackend { BonusPercent = 50, Mode = ResourceSellBonusMode.PerLine }
                .With(Copper, 3, 5).With(Iron, 1, 15);
            var onTotal = new FakeBackend { BonusPercent = 50, Mode = ResourceSellBonusMode.OnTotal }
                .With(Copper, 3, 5).With(Iron, 1, 15);
            var a = Session(perLine);
            var b = Session(onTotal);
            a.SelectAllBulk();
            b.SelectAllBulk();
            Assert.That(a.Quote.TotalGold, Is.EqualTo(30 + 8 + 8));
            Assert.That(b.Quote.TotalGold, Is.EqualTo(30 + 15));
        }

        // ---------- 확정 ----------

        [Test]
        public void Confirm_WhenOwnedDroppedSinceDisplay_AdjustsWithoutTrading()
        {
            var backend = new FakeBackend().With(Copper, 10, 10);
            var session = Session(backend);
            session.Adjust(Copper, 8);
            backend.SetOwned(Copper, 5, raise: false);

            var outcome = session.Confirm();

            Assert.That(outcome.Status, Is.EqualTo(ResourceSellConfirmStatus.Adjusted));
            Assert.That(backend.Commits, Is.Zero);
            Assert.That(session.GetQuantity(Copper), Is.EqualTo(5));
            Assert.That(session.Quote.TotalGold, Is.EqualTo(50), "화면 예상값도 새 수량으로 바뀐다");

            var second = session.Confirm();
            Assert.That(second.IsSold, Is.True);
            Assert.That(backend.LastItems[0].Value, Is.EqualTo(5), "실제 거래 = 화면 수량");
        }

        [Test]
        public void Confirm_LiveRefreshClampsSelectionWhenOwnedChanges()
        {
            var backend = new FakeBackend().With(Copper, 10, 10);
            var session = Session(backend);
            session.Adjust(Copper, 9);
            backend.SetOwned(Copper, 4);
            Assert.That(session.GetQuantity(Copper), Is.EqualTo(4));
        }

        [Test]
        public void Confirm_Failure_KeepsSelection_AndReportsMessage()
        {
            var backend = new FakeBackend { FailCommit = true }.With(Copper, 10, 10);
            var session = Session(backend);
            session.Adjust(Copper, 3);
            var outcome = session.Confirm();
            Assert.That(outcome.Status, Is.EqualTo(ResourceSellConfirmStatus.Failed));
            Assert.That(outcome.SoldItemIds, Is.Empty);
            Assert.That(session.GetQuantity(Copper), Is.EqualTo(3));
        }

        [Test]
        public void Confirm_ReentryDuringCommit_IsBusy_AndTradesOnce()
        {
            var backend = new FakeBackend().With(Copper, 10, 10);
            var session = Session(backend);
            ResourceSellConfirmOutcome inner = null;
            backend.DuringCommit = () => inner = session.Confirm();
            session.Adjust(Copper, 2);
            var outcome = session.Confirm();
            Assert.That(outcome.IsSold, Is.True);
            Assert.That(inner.Status, Is.EqualTo(ResourceSellConfirmStatus.Busy));
            Assert.That(backend.Commits, Is.EqualTo(1));
            Assert.That(session.Selection.IsEmpty, Is.True, "성공 후 판매 예정 수량은 0으로 돌아간다");
            Assert.That(session.Confirm().Status, Is.EqualTo(ResourceSellConfirmStatus.Empty), "같은 거래가 두 번 실행되지 않는다");
        }

        // ---------- 서비스 일괄 거래 ----------

        [Test]
        public void EconomyBatch_SellsAllAtomically_PerLineBonus_SavesOnce()
        {
            var (economy, inventory, state) = Surface(bonusPercent: 50);
            inventory.TryAddMineral(Copper, 3);
            inventory.TryAddMineral(Iron, 1);
            var saves = 0;
            var events = 0;
            economy.AutoSaveRequested += _ => saves++;
            economy.TransactionCompleted += _ => events++;

            var result = economy.TrySellMinerals(Pairs(Copper, 3, Iron, 1));

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(state.Player.Gold, Is.EqualTo(30 + 15 + 15 + 8), "광물별 판매와 같은 행 단위 보너스");
            Assert.That(inventory.State.GetQuantity(Copper), Is.Zero);
            Assert.That(inventory.State.GetQuantity(Iron), Is.Zero);
            Assert.That(saves, Is.EqualTo(1));
            Assert.That(events, Is.EqualTo(1));
        }

        [Test]
        public void EconomyBatch_OneInvalidLine_ChangesNothing()
        {
            var (economy, inventory, state) = Surface();
            inventory.TryAddMineral(Copper, 3);
            inventory.TryAddMineral(Iron, 1);
            var result = economy.TrySellMinerals(Pairs(Copper, 3, Iron, 2));
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(inventory.State.GetQuantity(Copper), Is.EqualTo(3));
            Assert.That(inventory.State.GetQuantity(Iron), Is.EqualTo(1));
            Assert.That(state.Player.Gold, Is.Zero);
        }

        [Test]
        public void EconomyBatch_RespectsSurfaceSellGate()
        {
            var catalog = Catalog();
            var state = GameState.CreateNew();
            var inventory = new InventoryService(catalog, 100f, state);
            var gate = new SceneSellGate { IsSellAllowed = false };
            var economy = new EconomyService(inventory, catalog, state, gate);
            inventory.TryAddMineral(Copper, 2);
            Assert.That(economy.TrySellMinerals(Pairs(Copper, 2)).IsSuccess, Is.False);
            Assert.That(inventory.State.GetQuantity(Copper), Is.EqualTo(2));
            gate.IsSellAllowed = true;
            Assert.That(economy.TrySellMinerals(Pairs(Copper, 2)).IsSuccess, Is.True);
        }

        [Test]
        public void SurfaceBackend_MaxSelectThenSell_MatchesFormerSellAll_AndKeepsFuel()
        {
            var (economy, inventory, state) = Surface();
            inventory.TryAddMineral(Copper, 2);
            inventory.TryAddMineral(Fuel, 1);
            var presenter = new EconomyPanelPresenter(null);
            presenter.Bind(economy, null, inventory, state);
            var session = new ResourceSellSession(new SurfaceSellBackend(presenter, economy, inventory, state, null));
            session.Open();

            Assert.That(session.Snapshot.TryFind(Fuel, out var fuel), Is.True);
            Assert.That(fuel.CanSell, Is.True, "지상에서는 엔진 연료도 직접 지정해 팔 수 있다");
            Assert.That(fuel.InBulk, Is.False);
            session.SelectAllBulk();
            var outcome = session.Confirm();

            Assert.That(outcome.IsSold, Is.True);
            Assert.That(state.Player.Gold, Is.EqualTo(20), "기존 전체 판매 결과와 같다(EngineFuelSellTests)");
            Assert.That(inventory.State.GetQuantity(Fuel), Is.EqualTo(1));

            session.Adjust(Fuel, 1);
            Assert.That(session.Confirm().IsSold, Is.True);
            Assert.That(state.Player.Gold, Is.EqualTo(120));
            Assert.That(outcome.GoldBefore, Is.Zero);
            Assert.That(outcome.GoldAfter, Is.EqualTo(20));
            session.Dispose();
            presenter.Unbind();
        }

        [Test]
        public void SettlementBatch_BonusOnTotal_SingleSave_AndQuestSignalShape()
        {
            var (service, inventory, state) = Settlement(bonusPercent: 50);
            inventory.TryAddMineral(Copper, 3);
            inventory.TryAddMineral(Iron, 1);
            var saves = 0;
            var results = new List<OutpostOperationResult>();
            service.AutoSaveRequested += _ => saves++;
            service.OperationCompleted += results.Add;

            var result = service.TrySettlePlayerCargoBatch(Pairs(Copper, 3, Iron, 1));

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(state.Player.Gold, Is.EqualTo(45 + 23), "기존 화물 전체 정산처럼 합계에 보너스");
            Assert.That(result.Kind, Is.EqualTo(OutpostOperationKind.SettlePlayerCargo));
            Assert.That(result.Quantity, Is.EqualTo(4));
            Assert.That(result.GoldDelta, Is.EqualTo(68));
            Assert.That(saves, Is.EqualTo(1));
            Assert.That(results.Count, Is.EqualTo(1));
        }

        [Test]
        public void SettlementBatch_RejectsRare_AndInsufficient_WithoutPartialChange()
        {
            var (service, inventory, state) = Settlement();
            inventory.TryAddMineral(Copper, 3);
            inventory.TryAddMineral(Fuel, 1);
            Assert.That(service.TrySettlePlayerCargoBatch(Pairs(Copper, 3, Fuel, 1)).IsSuccess, Is.False);
            Assert.That(service.TrySettlePlayerCargoBatch(Pairs(Copper, 4)).IsSuccess, Is.False);
            Assert.That(inventory.State.GetQuantity(Copper), Is.EqualTo(3));
            Assert.That(inventory.State.GetQuantity(Fuel), Is.EqualTo(1));
            Assert.That(state.Player.Gold, Is.Zero);
        }

        [Test]
        public void SettlementBatch_DuplicateSettlementId_IsRejected()
        {
            var (service, inventory, _) = Settlement();
            inventory.TryAddMineral(Copper, 5);
            Assert.That(service.TrySettlePlayerCargoBatch(Pairs(Copper, 1), "dup").IsSuccess, Is.True);
            var again = service.TrySettlePlayerCargoBatch(Pairs(Copper, 1), "dup");
            Assert.That(again.Status, Is.EqualTo(OutpostOperationStatus.AlreadyProcessed));
            Assert.That(inventory.State.GetQuantity(Copper), Is.EqualTo(4));
        }

        [Test]
        public void SettlementBackend_RareRowVisibleButUnsellable_AndSaleGoesThroughPresenter()
        {
            var (service, inventory, state) = Settlement();
            inventory.TryAddMineral(Copper, 4);
            inventory.TryAddMineral(Fuel, 1);
            var presenter = new OutpostPanelPresenter(null);
            presenter.Bind(service);
            var session = new ResourceSellSession(new SettlementSellBackend(presenter, service, null));
            session.Open();

            Assert.That(session.Snapshot.TryFind(Fuel, out var fuel), Is.True);
            Assert.That(fuel.CanSell, Is.False);
            Assert.That(fuel.Note, Is.Not.Empty);
            session.SelectAllBulk();
            var outcome = session.Confirm();
            Assert.That(outcome.IsSold, Is.True);
            Assert.That(state.Player.Gold, Is.EqualTo(40));
            Assert.That(inventory.State.GetQuantity(Fuel), Is.EqualTo(1));
            Assert.That(session.Snapshot.Gold, Is.EqualTo(40));
            session.Dispose();
            presenter.Unbind();
        }

        // ---------- 시간표 ----------

        [Test]
        public void Timeline_Durations_MatchRequest()
        {
            Assert.That(ResourceSellTimeline.OpenDuration, Is.InRange(0.9f, 1.3f));
            Assert.That(ResourceSellTimeline.CloseDuration, Is.InRange(0.25f, 0.4f));
            Assert.That(ResourceSellTimeline.CoinCount, Is.GreaterThanOrEqualTo(3));
            Assert.That(ResourceSellTimeline.OpenCardScale(0f), Is.LessThan(0.7f), "멀리서 작게 시작");
            Assert.That(ResourceSellTimeline.OpenBrightness(0f), Is.LessThan(0.5f), "낮은 밝기로 시작");
            Assert.That(ResourceSellTimeline.OpenCardScale(ResourceSellTimeline.OpenDuration), Is.EqualTo(1f).Within(0.0001f));
            Assert.That(ResourceSellTimeline.CloseCardScale(ResourceSellTimeline.CloseDuration), Is.LessThan(0.7f), "뒤로 물러난다");
        }

        [Test]
        public void Timeline_CoinsStackOneByOne_SpinAndBurstAtImpact()
        {
            var stagger = ResourceSellTimeline.CoinStagger;
            for (var i = 0; i < ResourceSellTimeline.CoinCount; i++)
            {
                Assert.That(ResourceSellTimeline.CoinDropProgress(i * stagger - 0.001f, i), Is.Zero, "차례가 오기 전엔 보이지 않는다");
                Assert.That(ResourceSellTimeline.CoinDropProgress(i * stagger + ResourceSellTimeline.CoinDrop, i),
                    Is.EqualTo(1f).Within(0.0001f));
            }

            Assert.That(ResourceSellTimeline.CoinDropProgress(stagger + 0.02f, 0),
                Is.GreaterThan(ResourceSellTimeline.CoinDropProgress(stagger + 0.02f, 1)), "하나씩 차례로 쌓인다");
            var lastLanding = (ResourceSellTimeline.CoinCount - 1) * stagger + ResourceSellTimeline.CoinDrop;
            Assert.That(lastLanding, Is.LessThan(ResourceSellTimeline.ImpactTime), "다 쌓인 뒤에 부딪힌다");

            var min = 1f;
            var max = 0f;
            for (var t = 0f; t < ResourceSellTimeline.ImpactTime; t += 0.01f)
            {
                var width = ResourceSellTimeline.CoinSpinWidth(t, 0);
                min = Mathf.Min(min, width);
                max = Mathf.Max(max, width);
            }

            Assert.That(min, Is.LessThan(0.4f), "금화가 옆면까지 돈다");
            Assert.That(max, Is.GreaterThan(0.9f));
            Assert.That(min, Is.GreaterThan(0f), "옆면에서도 완전히 사라지지 않는다");

            Assert.That(ResourceSellTimeline.StackAlpha(ResourceSellTimeline.ImpactTime - 0.01f), Is.EqualTo(1f));
            Assert.That(ResourceSellTimeline.StackAlpha(ResourceSellTimeline.ImpactTime), Is.Zero, "부딪히면 더미는 사라진다");
            Assert.That(ResourceSellTimeline.BurstActive(ResourceSellTimeline.ImpactTime - 0.01f), Is.False);
            Assert.That(ResourceSellTimeline.BurstActive(ResourceSellTimeline.ImpactTime + 0.01f), Is.True);
            Assert.That(ResourceSellTimeline.BurstAlpha(ResourceSellTimeline.ImpactTime + 0.01f), Is.GreaterThan(0.9f));
            Assert.That(ResourceSellTimeline.BurstAlpha(ResourceSellTimeline.ImpactTime + ResourceSellTimeline.BurstDuration), Is.Zero);
            Assert.That(ResourceSellTimeline.ImpactFlash(ResourceSellTimeline.ImpactTime + 0.09f), Is.GreaterThan(0.5f));
            Assert.That(ResourceSellTimeline.Approach(ResourceSellTimeline.ImpactTime), Is.EqualTo(1f).Within(0.0001f), "충돌 때 가장 가깝다");
        }

        [Test]
        public void Timeline_ClosedWindowOpensAfterImpact_Monotonic_NoOvershoot_GlowVanishes()
        {
            Assert.That(ResourceSellTimeline.UnfoldStart, Is.GreaterThanOrEqualTo(ResourceSellTimeline.ImpactTime), "부딪힌 뒤에 벌어진다");
            Assert.That(ResourceSellTimeline.Gap(ResourceSellTimeline.ImpactTime), Is.Zero, "그때까지 양쪽 끝은 맞닿아 있다");
            Assert.That(ResourceSellTimeline.InteriorHeight(ResourceSellTimeline.UnfoldStart),
                Is.EqualTo(ResourceSellTimeline.MinInteriorHeight).Within(0.0001f), "가운데 가는 선에서 시작");

            var previousGap = 0f;
            var previousHeight = 0f;
            var previousScale = 0f;
            for (var t = 0f; t <= ResourceSellTimeline.OpenDuration; t += 0.01f)
            {
                var gap = ResourceSellTimeline.Gap(t);
                var height = ResourceSellTimeline.InteriorHeight(t);
                var scale = ResourceSellTimeline.OpenCardScale(t);
                Assert.That(gap, Is.GreaterThanOrEqualTo(previousGap - 1e-5f));
                Assert.That(gap, Is.LessThanOrEqualTo(1f), "반동(1 초과) 없음");
                Assert.That(height, Is.GreaterThanOrEqualTo(previousHeight - 1e-5f));
                Assert.That(height, Is.LessThanOrEqualTo(1f));
                Assert.That(scale, Is.GreaterThanOrEqualTo(previousScale - 1e-5f));
                Assert.That(scale, Is.LessThanOrEqualTo(1f));
                previousGap = gap;
                previousHeight = height;
                previousScale = scale;
            }

            Assert.That(ResourceSellTimeline.Gap(ResourceSellTimeline.OpenDuration), Is.EqualTo(1f).Within(0.0001f));
            Assert.That(ResourceSellTimeline.InteriorHeight(ResourceSellTimeline.OpenDuration), Is.EqualTo(1f).Within(0.0001f));

            Assert.That(ResourceSellTimeline.EdgeGlow(ResourceSellTimeline.ImpactTime - 0.05f), Is.GreaterThan(0f), "맞닿은 선이 빛난다");
            Assert.That(ResourceSellTimeline.EdgeGlow(ResourceSellTimeline.UnfoldStart + 0.15f), Is.GreaterThan(0f));
            Assert.That(ResourceSellTimeline.EdgeGlow(ResourceSellTimeline.OpenDuration), Is.EqualTo(0f).Within(0.0001f));
            Assert.That(ResourceSellTimeline.HologramAmount(ResourceSellTimeline.UnfoldStart + 0.05f), Is.GreaterThan(0.5f));
            Assert.That(ResourceSellTimeline.HologramAmount(ResourceSellTimeline.OpenDuration), Is.EqualTo(0f).Within(0.0001f));
            Assert.That(ResourceSellTimeline.HologramSolid(ResourceSellTimeline.OpenDuration), Is.EqualTo(1f).Within(0.0001f));

            // 내용(글자·버튼)은 펼침 후반부터 드러나고 등장이 끝나기 전에 모두 보인다.
            Assert.That(ResourceSellTimeline.Reveal(ResourceSellTimeline.HeaderReveal - 0.01f, ResourceSellTimeline.HeaderReveal), Is.Zero);
            Assert.That(ResourceSellTimeline.HeaderReveal, Is.GreaterThan(ResourceSellTimeline.UnfoldStart + ResourceSellTimeline.UnfoldDuration * 0.5f));
            Assert.That(ResourceSellTimeline.FooterReveal + ResourceSellTimeline.RevealDuration, Is.LessThanOrEqualTo(ResourceSellTimeline.OpenDuration));
        }

        [Test]
        public void Timeline_Close_FoldsEdgesBackTogether()
        {
            Assert.That(ResourceSellTimeline.CloseGap(0f), Is.EqualTo(1f).Within(0.0001f));
            Assert.That(ResourceSellTimeline.CloseGap(ResourceSellTimeline.CloseDuration), Is.Zero);
            var previous = 1f;
            for (var t = 0f; t <= ResourceSellTimeline.CloseDuration; t += 0.01f)
            {
                var gap = ResourceSellTimeline.CloseGap(t);
                Assert.That(gap, Is.LessThanOrEqualTo(previous + 1e-5f));
                Assert.That(gap, Is.InRange(0f, 1f));
                previous = gap;
            }

            Assert.That(ResourceSellTimeline.CloseContentAlpha(ResourceSellTimeline.CloseContentFade), Is.Zero, "내용이 먼저 사라진다");
            Assert.That(ResourceSellTimeline.CloseShellAlpha(ResourceSellTimeline.CloseDuration), Is.Zero);
        }

        // ---------- 팝업 ----------

        [Test]
        public void Popup_Layout_CloseClearsFrameCorner_SpacingIsRoomy_AndProjectionIsApartFromSellButton()
        {
            var popup = OpenPopup(new FakeBackend().With(Copper, 13, 10).With(Lithium, 7, 40));
            var card = Corners(popup.CardRect);

            // X 버튼 오른쪽 위 모서리는 프레임 모서리 장식(안쪽 대각선 약 100)에서 충분히 떨어져 있다.
            var close = Corners(popup.CloseButton.transform);
            var closeFromRight = card[2].x - close[2].x;
            var closeFromTop = card[2].y - close[2].y;
            Assert.That(closeFromRight + closeFromTop, Is.GreaterThanOrEqualTo(120f), "X 버튼이 외부 프레임과 겹치지 않는다");
            Assert.That(closeFromRight, Is.GreaterThanOrEqualTo(60f));

            // 골드 표시와 X 버튼 사이도 벌어져 있다.
            var goldRight = Corners(popup.CardRect.Find("Content/Header/GoldValue"))[2].x;
            Assert.That(close[0].x - goldRight, Is.GreaterThanOrEqualTo(20f));

            // 요약 막대는 프레임 모서리 장식과 겹치지 않는 안쪽에 있다.
            var summary = Corners(popup.CardRect.Find("Content/Footer/Summary"));
            Assert.That(summary[0].x - card[0].x + (summary[0].y - card[0].y), Is.GreaterThanOrEqualTo(120f));
            Assert.That(card[3].x - summary[3].x + (summary[3].y - card[3].y), Is.GreaterThanOrEqualTo(120f));

            // 예상 골드는 판매 버튼과 붙어 있지 않다.
            var projection = Corners(popup.CardRect.Find("Content/Footer/Projection"));
            var sell = Corners(popup.SellButton.transform);
            Assert.That(sell[0].x - projection[2].x, Is.GreaterThanOrEqualTo(32f), "예상 골드와 판매 버튼 사이 간격");
            var caption = Corners(popup.CardRect.Find("Content/Footer/ProjectionCaption"));
            var goldCaption = Corners(popup.CardRect.Find("Content/Footer/GoldCaption"));
            var goldRowRight = Corners(popup.CardRect.Find("Content/Footer/TotalGold"))[2].x;
            Assert.That(caption[0].x - goldRowRight, Is.GreaterThanOrEqualTo(32f), "구역 사이 간격");
            Assert.That(goldCaption[0].x, Is.LessThan(caption[0].x));

            // 행 사이도 넉넉히 떨어져 있다.
            var first = Corners(popup.Rows[0].transform);
            var second = Corners(popup.Rows[1].transform);
            Assert.That(first[0].y - second[1].y, Is.GreaterThanOrEqualTo(12f), "행 간격");
        }

        [Test]
        public void Popup_RowsAlignWithHeader_ButtonsFollowLimits_AndSellLabelShowsGold()
        {
            var backend = new FakeBackend().With(Copper, 13, 10).With(Lithium, 7, 40);
            var popup = OpenPopup(backend);
            var session = popup.Session;

            Assert.That(popup.VisibleRowCount, Is.EqualTo(2));
            var row = popup.Rows[0];
            Assert.That(row.MinusButton.Button.interactable, Is.False, "0에서는 감소 비활성");
            Assert.That(row.PlusButton.Button.interactable, Is.True);
            Assert.That(popup.SellButton.Button.interactable, Is.False);

            session.Adjust(Copper, 5);
            session.Adjust(Lithium, 2);
            Assert.That(row.QuantityString, Is.EqualTo("5"));
            Assert.That(row.GoldString, Is.EqualTo("50G"));
            Assert.That(popup.SellLabelString, Is.EqualTo("판매 · +130G"));
            Assert.That(popup.TotalQuantityString, Is.EqualTo("7개"));
            Assert.That(popup.SellButton.Button.interactable, Is.True);
            row.SnapActive();
            Assert.That(row.OutlineAlpha, Is.GreaterThan(0.5f), "수량이 있는 행은 청록 테두리로 강조");
            Assert.That(popup.Rows[1].IsActiveRow, Is.True);

            session.SetMax(Copper);
            Assert.That(row.PlusButton.Button.interactable, Is.False);
            Assert.That(row.PlusFiveButton.Button.interactable, Is.False);
            Assert.That(row.PlusTenButton.Button.interactable, Is.False);
            Assert.That(row.MaxButton.Button.interactable, Is.False);
            Assert.That(row.MinusButton.Button.interactable, Is.True);

            // 열 머리글과 행 열의 x 좌표가 같다.
            foreach (var pair in new[]
                     {
                         new[] { "Col_보유", "Owned" }, new[] { "Col_개당 가격", "UnitPrice" }, new[] { "Col_받을 골드", "Gold" }
                     })
            {
                var header = Corners(popup.CardRect.Find("Content/List/ColumnHeader/" + pair[0]));
                var cell = Corners(row.transform.Find(pair[1]));
                Assert.That(header[2].x, Is.EqualTo(cell[2].x).Within(0.5f), pair[0] + " 오른쪽 끝");
                if (pair[1] != "Gold")
                {
                    Assert.That(header[0].x, Is.EqualTo(cell[0].x).Within(0.5f), pair[0] + " 왼쪽 끝");
                }
            }

            // 수량 조절 버튼과 받을 골드 칸이 겹치지 않는다.
            var maxRight = Corners(row.MaxButton.transform)[2].x;
            var coinLeft = Corners(row.transform.Find("GoldCoin"))[0].x;
            Assert.That(maxRight, Is.LessThan(coinLeft));
        }

        private static Vector3[] Corners(Transform target)
        {
            var corners = new Vector3[4];
            ((RectTransform)target).GetWorldCorners(corners);
            return corners;
        }

        [Test]
        public void Popup_EmptyState_ShowsMessage_AndDisablesSell()
        {
            var popup = OpenPopup(new FakeBackend());
            Assert.That(popup.EmptyVisible, Is.True);
            Assert.That(popup.VisibleRowCount, Is.Zero);
            Assert.That(popup.SellButton.Button.interactable, Is.False);
            Assert.That(popup.SelectAllButton.Button.interactable, Is.False);
        }

        [Test]
        public void Popup_ScrollbarOnlyWhenListOverflows()
        {
            var few = new FakeBackend();
            for (var i = 0; i < 4; i++)
            {
                few.With("mineral.t" + i, 3, 10);
            }

            Assert.That(OpenPopup(few).ScrollbarVisible, Is.False);

            var many = new FakeBackend();
            for (var i = 0; i < 9; i++)
            {
                many.With("mineral.t" + i, 3, 10);
            }

            var popup = OpenPopup(many);
            Assert.That(popup.ScrollbarVisible, Is.True);
            Assert.That(popup.Scroll.vertical, Is.True);
        }

        [Test]
        public void Popup_StateMachine_OpenCloseReopen_LeavesNothingBehind()
        {
            var backend = new FakeBackend().With(Copper, 5, 10);
            backend.Gold = 500;
            var popup = OpenPopup(backend, settle: false);
            Assert.That(popup.State, Is.EqualTo(ResourceSellPopupView.PopupState.Opening));
            Assert.That(popup.IsShellSegmented, Is.True);

            // 등장 연출만으로 골드 값은 바뀌지 않는다.
            for (var t = 0f; t < ResourceSellTimeline.OpenDuration; t += 0.05f)
            {
                popup.Tick(0.05f);
                Assert.That(popup.GoldValueString, Is.EqualTo("500G"));
            }

            popup.Tick(0.1f);
            Assert.That(popup.State, Is.EqualTo(ResourceSellPopupView.PopupState.Open));
            Assert.That(popup.IsShellSegmented, Is.False, "정착하면 이음새 없는 한 장");
            Assert.That(popup.AnyEffectVisible, Is.False);

            // 등장 도중 닫기 → 종료 도중 다시 열기 → 닫기.
            popup.BeginClose();
            popup.Show();
            popup.Tick(0.2f);
            popup.BeginClose();
            Assert.That(popup.State, Is.EqualTo(ResourceSellPopupView.PopupState.Closing));
            Assert.That(popup.IsInteractive, Is.False, "닫는 동안 입력을 받지 않는다");
            popup.Tick(ResourceSellTimeline.CloseDuration + 0.01f);
            Assert.That(popup.State, Is.EqualTo(ResourceSellPopupView.PopupState.Hidden));
            Assert.That(popup.gameObject.activeSelf, Is.False, "닫힌 뒤 입력 차단 영역이 남지 않는다");
            Assert.That(popup.AnyEffectVisible, Is.False);
        }

        [Test]
        public void Popup_SaleEffect_CountsUpThenSnapsOnClose()
        {
            var backend = new FakeBackend().With(Copper, 5, 10);
            backend.Gold = 100;
            var popup = OpenPopup(backend);
            popup.Session.Adjust(Copper, 5);
            var outcome = popup.RequestSell();
            Assert.That(outcome.IsSold, Is.True);
            Assert.That(backend.Gold, Is.EqualTo(150), "실제 데이터는 연출을 기다리지 않고 반영");
            Assert.That(popup.IsSaleEffectActive, Is.True);
            Assert.That(popup.GoldValueString, Is.EqualTo("100G"));
            popup.Tick(0.4f);
            Assert.That(popup.DisplayedGold, Is.InRange(101, 149));
            popup.BeginClose();
            Assert.That(popup.IsSaleEffectActive, Is.False);
            Assert.That(popup.GoldValueString, Is.EqualTo("150G"), "닫으면 숫자 연출을 정리하고 최신 값");
        }

        [Test]
        public void Popup_FailedSale_PlaysNoEffect()
        {
            var backend = new FakeBackend { FailCommit = true }.With(Copper, 5, 10);
            var popup = OpenPopup(backend);
            popup.Session.Adjust(Copper, 1);
            var outcome = popup.RequestSell();
            Assert.That(outcome.IsSold, Is.False);
            Assert.That(popup.IsSaleEffectActive, Is.False);
            Assert.That(popup.StatusString, Is.Not.Empty);
        }

        // ---------- 버튼 ----------

        [Test]
        public void Button_HoverFocusDisabledAndPress_DoNotAccumulate()
        {
            var popup = OpenPopup(new FakeBackend().With(Copper, 5, 10));
            var button = popup.Rows[0].PlusButton;
            var normal = button.FaceColor;
            var data = new PointerEventData(EventSystem.current);

            for (var i = 0; i < 6; i++)
            {
                button.OnPointerEnter(data);
                button.Step(0.02f);
                button.OnPointerExit(data);
                button.Step(0.02f);
            }

            button.OnPointerEnter(data);
            button.Step(0.5f);
            Assert.That(button.HoverLevel, Is.EqualTo(1f));
            Assert.That(button.ContentColor.r, Is.LessThan(0.2f), "호버 시 짙은 남색 글자");
            button.OnPointerExit(data);
            button.Step(0.06f);
            Assert.That(button.HoverLevel, Is.InRange(0.1f, 0.9f), "호버 해제는 약 0.12초 동안 복귀");
            button.Step(0.2f);
            Assert.That(button.HoverLevel, Is.Zero);
            Assert.That(button.FaceColor, Is.EqualTo(normal));

            button.OnSelect(data);
            button.Step(0.5f);
            Assert.That(button.HoverLevel, Is.EqualTo(1f), "키보드 포커스도 같은 피드백");
            button.OnDeselect(data);

            button.OnPointerDown(new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left });
            Assert.That(button.transform.localScale.x, Is.EqualTo(1f));
            button.Step(0f);
            Assert.That(button.transform.localScale.x, Is.EqualTo(ResourceSellButton.PressedScale).Within(0.001f));
            button.OnPointerUp(data);
            button.Step(0.5f);
            Assert.That(button.transform.localScale.x, Is.EqualTo(1f));

            popup.Session.SetMax(Copper);
            button.OnPointerEnter(data);
            button.Step(0.5f);
            Assert.That(button.HoverLevel, Is.Zero, "비활성 버튼은 호버해도 밝아지지 않는다");
        }

        [Test]
        public void SellButton_IsPrimary_WithStrongerDefaultGlow()
        {
            var popup = OpenPopup(new FakeBackend().With(Copper, 5, 10));
            Assert.That(popup.SellButton.Kind, Is.EqualTo(ResourceSellButton.Variant.Primary));
            Assert.That(popup.SelectAllButton.Kind, Is.EqualTo(ResourceSellButton.Variant.Normal));
            Assert.That(popup.CloseButton.Button, Is.Not.Null);
        }

        // ---------- helpers ----------

        private ResourceSellPopupView OpenPopup(FakeBackend backend, bool settle = true)
        {
            var root = new GameObject("B136Canvas", typeof(Canvas));
            created.Add(root);
            var popup = ResourceSellPopupView.Create(root.transform, true);
            popup.ManualTick = true;
            popup.Attach(new ResourceSellSession(backend));
            popup.Show();
            if (settle)
            {
                popup.Tick(ResourceSellTimeline.OpenDuration + 0.01f);
            }

            return popup;
        }

        private static ResourceSellSession Session(FakeBackend backend)
        {
            var session = new ResourceSellSession(backend);
            session.Open();
            return session;
        }

        private static List<KeyValuePair<string, int>> Pairs(string a, int qa, string b = null, int qb = 0)
        {
            var list = new List<KeyValuePair<string, int>> { new KeyValuePair<string, int>(a, qa) };
            if (b != null)
            {
                list.Add(new KeyValuePair<string, int>(b, qb));
            }

            return list;
        }

        private static InMemoryMineralCatalog Catalog()
        {
            var catalog = new InMemoryMineralCatalog();
            catalog.Register(Copper, 1.5f, 10, "구리");
            catalog.Register(Iron, 2f, 15, "철");
            catalog.Register(Fuel, 1f, 100, "엔진 연료");
            return catalog;
        }

        private static (EconomyService, InventoryService, GameState) Surface(int bonusPercent = 0)
        {
            var catalog = Catalog();
            var state = GameState.CreateNew();
            var inventory = new InventoryService(catalog, 100f, state);
            var economy = new EconomyService(inventory, catalog, state, effects: new Bonus(bonusPercent));
            return (economy, inventory, state);
        }

        private static (OutpostService, InventoryService, GameState) Settlement(int bonusPercent = 0)
        {
            var catalog = Catalog();
            var state = GameState.CreateNew();
            var inventory = new InventoryService(catalog, 100f, state);
            var service = new OutpostService(inventory, catalog, state, effects: new Bonus(bonusPercent));
            service.ApplyRuntimeStatus(new OutpostStatusDto
            {
                outpostInstanceId = "outpost.1",
                isActive = true,
                isInInteractionRange = true,
                interactionFacilityInstanceId = "settlement.1",
                interactionFacilityBuildingId = DataIds.Buildings.SettlementBasic,
                connectedFacilities = new List<ConnectedFacilityStatusDto>
                {
                    new ConnectedFacilityStatusDto
                    {
                        instanceId = "settlement.1",
                        buildingId = DataIds.Buildings.SettlementBasic,
                        isActive = true
                    }
                }
            });
            return (service, inventory, state);
        }

        private sealed class Bonus : IUpgradeEffectProvider
        {
            private readonly int percent;
            public Bonus(int percent) => this.percent = percent;
            public int GetDrillLevel() => 0;
            public float GetDrillSpeedMultiplier() => 1f;
            public float GetEnergyEfficiencyMultiplier() => 1f;
            public int GetMaximumEnergy(int baseMaximum) => baseMaximum;
            public float GetMaximumCargoWeight(float baseMaximum) => baseMaximum;
            public float GetDroneScanRadius(float baseRadius) => baseRadius;
            public float GetDroneRescuePreservation(float basePreservation) => basePreservation;
            public float GetGasResistance() => 0f;
            public int GetGoldGainBonusPercent() => percent;
            public int GetMiningYieldBonus(string mineralId) => 0;
        }

        /// <summary>판매 창 단위 검증용 가짜 경계. 거래는 단순 차감·지급.</summary>
        private sealed class FakeBackend : IResourceSellBackend
        {
            private readonly List<ResourceSellLine> lines = new List<ResourceSellLine>();

            public int Gold;
            public float Weight = 10f;
            public int BonusPercent;
            public ResourceSellBonusMode Mode = ResourceSellBonusMode.PerLine;
            public bool FailCommit;
            public int Commits;
            public System.Action DuringCommit;
            public IReadOnlyList<KeyValuePair<string, int>> LastItems;

            public event System.Action Changed;

            public string Hint => "hint";
            public string BulkRuleNotice => "rare excluded";

            public FakeBackend With(string id, int owned, int price, float weight = 1f)
            {
                lines.Add(new ResourceSellLine(id, id, owned, price, weight, null));
                return this;
            }

            public FakeBackend WithRare(string id, int owned, int price, bool canSell)
            {
                lines.Add(new ResourceSellLine(id, id, owned, price, 1f, null, canSell, false, "note"));
                return this;
            }

            public void SetOwned(string id, int owned, bool raise = true)
            {
                for (var i = 0; i < lines.Count; i++)
                {
                    var l = lines[i];
                    if (l.ItemId == id)
                    {
                        lines[i] = new ResourceSellLine(l.ItemId, l.DisplayName, owned, l.UnitPrice, l.UnitWeight, l.Icon, l.CanSell, l.InBulk, l.Note);
                    }
                }

                if (raise)
                {
                    Changed?.Invoke();
                }
            }

            public ResourceSellSnapshot Read(bool fresh)
            {
                return new ResourceSellSnapshot(lines.ToArray(), Gold, Weight, 60f, BonusPercent, Mode);
            }

            public ResourceSellCommitResult Commit(IReadOnlyList<KeyValuePair<string, int>> items)
            {
                Commits++;
                LastItems = items;
                DuringCommit?.Invoke();
                if (FailCommit)
                {
                    return new ResourceSellCommitResult(false, 0, "실패");
                }

                var total = 0;
                for (var i = 0; i < items.Count; i++)
                {
                    for (var j = 0; j < lines.Count; j++)
                    {
                        var l = lines[j];
                        if (l.ItemId != items[i].Key)
                        {
                            continue;
                        }

                        total += l.UnitPrice * items[i].Value;
                        Weight -= l.UnitWeight * items[i].Value;
                        lines[j] = new ResourceSellLine(l.ItemId, l.DisplayName, l.Owned - items[i].Value, l.UnitPrice, l.UnitWeight, l.Icon, l.CanSell, l.InBulk, l.Note);
                    }
                }

                lines.RemoveAll(l => l.Owned <= 0);
                Gold += total;
                Changed?.Invoke();
                return new ResourceSellCommitResult(true, total, "ok");
            }

            public void Dispose()
            {
            }
        }
    }
}

using System.Collections.Generic;
using SubTerra.App.Core.Data;
using SubTerra.App.Economy;
using SubTerra.App.Inventory;
using SubTerra.App.State;
using SubTerra.Shared;
using UnityEngine;

namespace SubTerra.App.UI.Economy
{
    /// <summary>
    /// 판매·제작 UI Presenter.
    /// 버튼 핸들러는 EconomyService/CraftingService만 호출하고 GameState·Inventory를 직접 쓰지 않는다.
    /// 처리 중 busy 가드로 동일 프레임 중복 클릭이 한 건만 반영되게 한다.
    /// 목록·선택·수량 UI 상태는 Presenter에 두고 View는 표시만 한다.
    /// </summary>
    public sealed class EconomyPanelPresenter
    {
        private readonly IEconomyPanelView view;
        private EconomyService economy;
        private CraftingService crafting;
        private InventoryService inventory;
        private GameState gameState;
        private GameDataCatalog catalog;
        private bool busy;

        // Sell-all 루프 중 InventoryChanged로 목록이 재빌드·선택이 리셋되지 않게 억제.
        private bool suppressListRebuildFromInventory;
        // Sell-all 중 중간 TransactionCompleted 메시지가 집계 문구를 덮어쓰지 않게 억제.
        private bool suppressStatusFromTransactions;

        private string selectedMineralId = string.Empty;
        private int sellQuantity = 1;
        private int selectedOwned;
        private int selectedUnitPrice;

        public EconomyPanelPresenter(IEconomyPanelView view)
        {
            this.view = view;
        }

        public bool IsBusy => busy;
        public bool IsBound => economy != null;
        public string SelectedMineralId => selectedMineralId;
        public int SellQuantity => sellQuantity;

        /// <summary>
        /// 서비스 바인딩. inventory/gameState null이면 목록·크레딧 라벨을 skip하고 RequestSell만 동작(기존 테스트 호환).
        /// </summary>
        public void Bind(
            EconomyService economyService,
            CraftingService craftingService = null,
            InventoryService inventoryService = null,
            GameState state = null,
            GameDataCatalog gameDataCatalog = null)
        {
            Unbind();
            economy = economyService;
            crafting = craftingService;
            inventory = inventoryService;
            gameState = state;
            catalog = gameDataCatalog;

            if (economy != null)
            {
                economy.TransactionCompleted += OnEconomyTransaction;
            }

            if (crafting != null)
            {
                crafting.CraftCompleted += OnCraftCompleted;
            }

            if (inventory != null)
            {
                inventory.InventoryChanged += OnInventoryChanged;
            }

            if (gameState != null)
            {
                gameState.CreditsChanged += OnCreditsChanged;
            }

            view?.SetBusy(false);
            view?.SetStatusMessage(string.Empty);
            view?.SetStatusDetail(string.Empty);

            RefreshSellList();
            RefreshCreditsLabel();
        }

        public void Unbind()
        {
            if (economy != null)
            {
                economy.TransactionCompleted -= OnEconomyTransaction;
                economy = null;
            }

            if (crafting != null)
            {
                crafting.CraftCompleted -= OnCraftCompleted;
                crafting = null;
            }

            if (inventory != null)
            {
                inventory.InventoryChanged -= OnInventoryChanged;
                inventory = null;
            }

            if (gameState != null)
            {
                gameState.CreditsChanged -= OnCreditsChanged;
                gameState = null;
            }

            catalog = null;
            busy = false;
            suppressListRebuildFromInventory = false;
            suppressStatusFromTransactions = false;
            selectedMineralId = string.Empty;
            sellQuantity = 1;
            selectedOwned = 0;
            selectedUnitPrice = 0;
            view?.SetBusy(false);
        }

        /// <summary>스냅샷에서 보유&gt;0 행만 구성하고 선택/수량/미리보기를 동기화한다.</summary>
        public void RefreshSellList()
        {
            if (view == null)
            {
                return;
            }

            if (inventory == null)
            {
                view.SetSellRows(System.Array.Empty<SellMineralRowReadModel>());
                view.SetEmptySellState(true, "판매할 광물이 없습니다. 탐사 후 귀환하세요.");
                view.SetSellActionsEnabled(false, false);
                view.SetSelectedMineral(string.Empty, 0, 0, 0);
                view.SetSellQuantityControls(0, 0, 0);
                view.SetPreviewCredits(0, "예상 골드 +0");
                return;
            }

            var snapshot = inventory.GetSnapshot();
            var stacks = snapshot != null ? snapshot.Stacks : null;
            var rows = new List<SellMineralRowReadModel>();
            if (stacks != null)
            {
                for (var i = 0; i < stacks.Count; i++)
                {
                    var stack = stacks[i];
                    if (stack.Quantity <= 0)
                    {
                        continue;
                    }

                    var isSelected = stack.MineralId == selectedMineralId;
                    var preview = 0;
                    if (EconomyPricing.TryComputeGoldGain(stack.UnitPrice, stack.Quantity, out var line, out _))
                    {
                        preview = line;
                    }

                    rows.Add(new SellMineralRowReadModel(
                        stack.MineralId,
                        string.IsNullOrEmpty(stack.DisplayName) ? stack.MineralId : stack.DisplayName,
                        stack.Quantity,
                        stack.UnitPrice,
                        preview,
                        isSelected,
                        ResolveIcon(stack.MineralId)));
                }
            }

            // 선택 ID가 목록에서 사라지면 해제 또는 첫 행 재선택.
            if (!string.IsNullOrEmpty(selectedMineralId))
            {
                var stillOwned = false;
                for (var i = 0; i < rows.Count; i++)
                {
                    if (rows[i].MineralId == selectedMineralId)
                    {
                        stillOwned = true;
                        selectedOwned = rows[i].OwnedQuantity;
                        selectedUnitPrice = rows[i].UnitPrice;
                        break;
                    }
                }

                if (!stillOwned)
                {
                    selectedMineralId = string.Empty;
                    selectedOwned = 0;
                    selectedUnitPrice = 0;
                    sellQuantity = 1;
                }
            }

            if (string.IsNullOrEmpty(selectedMineralId) && rows.Count > 0)
            {
                // 자동 재선택 없음: 빈 선택 유지. 사용자가 행을 고른다.
            }
            else if (!string.IsNullOrEmpty(selectedMineralId))
            {
                sellQuantity = ClampQty(sellQuantity, selectedOwned);
            }

            view.SetSellRows(rows);
            var isEmpty = rows.Count == 0;
            view.SetEmptySellState(isEmpty, isEmpty
                ? "판매할 광물이 없습니다. 탐사 후 귀환하세요."
                : string.Empty);

            PushSelectionToView();
            PushPreviewToView();
            view.SetSellActionsEnabled(
                !busy && !string.IsNullOrEmpty(selectedMineralId) && sellQuantity >= 1 && selectedOwned >= sellQuantity,
                !busy && rows.Count > 0);
        }

        public void RefreshCreditsLabel()
        {
            if (view == null || gameState == null)
            {
                return;
            }

            view.SetCreditsLabel(gameState.Player.Gold);
        }

        /// <summary>선택 하이라이트. 기본 판매 수량 = 1 (owned≥1 보장).</summary>
        public void SelectMineral(string mineralId)
        {
            if (string.IsNullOrEmpty(mineralId) || inventory == null)
            {
                return;
            }

            var snapshot = inventory.GetSnapshot();
            var owned = snapshot != null ? snapshot.GetQuantity(mineralId) : 0;
            if (owned <= 0)
            {
                return;
            }

            selectedMineralId = mineralId;
            selectedOwned = owned;
            selectedUnitPrice = ResolveUnitPrice(mineralId, snapshot);
            sellQuantity = 1;

            // 선택 상태 반영을 위해 행 IsSelected 재구성.
            RefreshSellList();
        }

        /// <summary>클램프 [1, owned]. 선택 없으면 컨트롤 비활성.</summary>
        public void SetSellQuantity(int qty)
        {
            if (string.IsNullOrEmpty(selectedMineralId) || selectedOwned <= 0)
            {
                return;
            }

            sellQuantity = ClampQty(qty, selectedOwned);
            PushSelectionToView();
            PushPreviewToView();
            view?.SetSellActionsEnabled(
                !busy && sellQuantity >= 1,
                !busy && HasAnyOwned());
        }

        /// <summary>+/- 조절. Max 버튼은 delta 대신 owned로 SetSellQuantity 호출을 권장.</summary>
        public void AdjustSellQuantity(int delta)
        {
            if (string.IsNullOrEmpty(selectedMineralId) || selectedOwned <= 0)
            {
                return;
            }

            SetSellQuantity(sellQuantity + delta);
        }

        /// <summary>선택 없거나 qty&lt;1이면 no-op(서비스 미호출). 그 외 RequestSell.</summary>
        public EconomyTransactionResult RequestSellSelected()
        {
            if (string.IsNullOrEmpty(selectedMineralId) || sellQuantity < 1)
            {
                return EconomyTransactionResult.Fail(
                    EconomyTransactionStatus.InvalidRequest,
                    EconomyTransactionKind.Sell,
                    string.Empty,
                    "No selection or invalid quantity — no-op.");
            }

            return RequestSell(selectedMineralId, sellQuantity);
        }

        /// <summary>
        /// 스택별 순차 TrySellMineral. busy 스팬은 전체 루프 1회.
        /// 부분 성공 스택은 롤백하지 않고, 종료 후 집계 메시지만 표시한다.
        /// </summary>
        public void RequestSellAll()
        {
            if (busy)
            {
                var busyResult = EconomyTransactionResult.Fail(
                    EconomyTransactionStatus.Busy,
                    EconomyTransactionKind.Sell,
                    "처리 중입니다.",
                    "Presenter re-entry blocked.");
                ApplyResultToView(busyResult);
                return;
            }

            if (economy == null)
            {
                var missing = EconomyTransactionResult.Fail(
                    EconomyTransactionStatus.DependencyMissing,
                    EconomyTransactionKind.Sell,
                    "경제 서비스가 없습니다.",
                    "EconomyService not bound.");
                ApplyResultToView(missing);
                return;
            }

            if (inventory == null)
            {
                view?.SetStatusMessage("판매할 광물이 없습니다. 탐사 후 귀환하세요.");
                view?.SetStatusDetail(string.Empty);
                return;
            }

            // 루프 전 스냅샷 1회 고정 — 중간에 목록이 바뀌어도 대상 목록은 유지.
            var snapshot = inventory.GetSnapshot();
            var targets = new List<InventoryStackEntry>();
            if (snapshot != null && snapshot.Stacks != null)
            {
                for (var i = 0; i < snapshot.Stacks.Count; i++)
                {
                    if (snapshot.Stacks[i].Quantity > 0)
                    {
                        targets.Add(snapshot.Stacks[i]);
                    }
                }
            }

            if (targets.Count == 0)
            {
                view?.SetStatusMessage("판매할 광물이 없습니다. 탐사 후 귀환하세요.");
                view?.SetStatusDetail(string.Empty);
                return;
            }

            busy = true;
            view?.SetBusy(true);
            // InventoryChanged mid-loop → 목록 리빌드 억제. 종료 후 1회 refresh.
            suppressListRebuildFromInventory = true;
            // TransactionCompleted → 중간 메시지 덮어쓰기 방지. 집계 문자열만 표시.
            suppressStatusFromTransactions = true;

            var successKinds = 0;
            var goldTotal = 0;
            var attempted = targets.Count;
            var lastFailDetail = string.Empty;

            try
            {
                for (var i = 0; i < targets.Count; i++)
                {
                    var stack = targets[i];
                    // 중첩 RequestSell 금지 — busy 재진입 차단을 피하기 위해 서비스 직접 호출.
                    var result = economy.TrySellMineral(stack.MineralId, stack.Quantity);
                    if (result.IsSuccess)
                    {
                        successKinds++;
                        goldTotal += result.GoldDelta;
                    }
                    else
                    {
                        lastFailDetail = result.Diagnostic;
                    }
                    // 실패 시 다음 스택 계속 (이미 커밋된 스택 유지).
                }
            }
            finally
            {
                busy = false;
                view?.SetBusy(false);
                suppressListRebuildFromInventory = false;
                suppressStatusFromTransactions = false;
            }

            RefreshSellList();
            RefreshCreditsLabel();

            if (successKinds == 0)
            {
                view?.SetStatusMessage("판매할 수 없습니다.");
                view?.SetStatusDetail(lastFailDetail ?? string.Empty);
            }
            else if (successKinds < attempted)
            {
                view?.SetStatusMessage($"부분 판매: {successKinds}/{attempted} 성공 · +{goldTotal}G");
                view?.SetStatusDetail(string.Empty);
            }
            else
            {
                view?.SetStatusMessage($"{successKinds}종 판매 · +{goldTotal}G");
                view?.SetStatusDetail(string.Empty);
            }
        }

        /// <summary>
        /// 판매 요청. 처리 중이면 Busy 결과를 표시하고 서비스를 다시 호출하지 않는다.
        /// </summary>
        public EconomyTransactionResult RequestSell(string mineralId, int quantity)
        {
            if (busy)
            {
                var busyResult = EconomyTransactionResult.Fail(
                    EconomyTransactionStatus.Busy,
                    EconomyTransactionKind.Sell,
                    "처리 중입니다.",
                    "Presenter re-entry blocked.");
                ApplyResultToView(busyResult);
                return busyResult;
            }

            if (economy == null)
            {
                var missing = EconomyTransactionResult.Fail(
                    EconomyTransactionStatus.DependencyMissing,
                    EconomyTransactionKind.Sell,
                    "경제 서비스가 없습니다.",
                    "EconomyService not bound.");
                ApplyResultToView(missing);
                return missing;
            }

            busy = true;
            view?.SetBusy(true);
            try
            {
                // State 직접 변경 금지 — 서비스 트랜잭션만 호출.
                return economy.TrySellMineral(mineralId, quantity);
            }
            finally
            {
                busy = false;
                view?.SetBusy(false);
            }
        }

        /// <summary>
        /// 제작·설치 요청. 배치 실패/자원 부족을 서비스 결과로 구분해 표시한다.
        /// </summary>
        public EconomyTransactionResult RequestCraft(
            string buildingId,
            IReadOnlyList<ItemCostDto> costs,
            IBuildingPlacementGate placement)
        {
            if (busy)
            {
                var busyResult = EconomyTransactionResult.Fail(
                    EconomyTransactionStatus.Busy,
                    EconomyTransactionKind.Craft,
                    "처리 중입니다.",
                    "Presenter re-entry blocked.");
                ApplyResultToView(busyResult);
                return busyResult;
            }

            if (crafting == null)
            {
                var missing = EconomyTransactionResult.Fail(
                    EconomyTransactionStatus.DependencyMissing,
                    EconomyTransactionKind.Craft,
                    "제작 서비스가 없습니다.",
                    "CraftingService not bound.");
                ApplyResultToView(missing);
                return missing;
            }

            busy = true;
            view?.SetBusy(true);
            try
            {
                return crafting.TryCraftBuilding(buildingId, costs, placement);
            }
            finally
            {
                busy = false;
                view?.SetBusy(false);
            }
        }

        private void OnInventoryChanged(InventorySnapshot _)
        {
            if (suppressListRebuildFromInventory)
            {
                return;
            }

            RefreshSellList();
        }

        private void OnCreditsChanged(int _)
        {
            RefreshCreditsLabel();
        }

        private void OnEconomyTransaction(EconomyTransactionResult result)
        {
            // 판매·Spend 결과 표시. Craft는 CraftCompleted에서 최종 메시지를 덮어쓸 수 있다.
            if (result.Kind == EconomyTransactionKind.Craft)
            {
                return;
            }

            if (suppressStatusFromTransactions)
            {
                return;
            }

            ApplyResultToView(result);
            // 단건 판매 성공 후에도 목록·크레딧을 갱신한다(InventoryChanged/CreditsChanged와 이중일 수 있으나 안전).
            if (!suppressListRebuildFromInventory)
            {
                RefreshSellList();
                RefreshCreditsLabel();
            }
        }

        private void OnCraftCompleted(EconomyTransactionResult result)
        {
            ApplyResultToView(result);
        }

        private void ApplyResultToView(EconomyTransactionResult result)
        {
            if (view == null)
            {
                return;
            }

            // 사용자 메시지와 디버그 진단을 분리 표시.
            view.SetStatusMessage(result.UserMessage);
            view.SetStatusDetail(result.IsSuccess ? string.Empty : result.Diagnostic);
        }

        private void PushSelectionToView()
        {
            if (view == null)
            {
                return;
            }

            if (string.IsNullOrEmpty(selectedMineralId) || selectedOwned <= 0)
            {
                view.SetSelectedMineral(string.Empty, 0, 0, 0);
                view.SetSellQuantityControls(0, 0, 0);
                return;
            }

            view.SetSelectedMineral(selectedMineralId, sellQuantity, selectedOwned, selectedUnitPrice);
            view.SetSellQuantityControls(sellQuantity, 1, selectedOwned);
        }

        private void PushPreviewToView()
        {
            if (view == null)
            {
                return;
            }

            if (string.IsNullOrEmpty(selectedMineralId) || sellQuantity < 1)
            {
                view.SetPreviewCredits(0, "예상 골드 +0");
                return;
            }

            if (!EconomyPricing.TryComputeGoldGain(selectedUnitPrice, sellQuantity, out var gain, out _))
            {
                view.SetPreviewCredits(0, "예상 골드 +0");
                return;
            }

            view.SetPreviewCredits(gain, "예상 골드 +" + gain);
        }

        private int ResolveUnitPrice(string mineralId, InventorySnapshot snapshot)
        {
            if (snapshot != null && snapshot.Stacks != null)
            {
                for (var i = 0; i < snapshot.Stacks.Count; i++)
                {
                    if (snapshot.Stacks[i].MineralId == mineralId)
                    {
                        return snapshot.Stacks[i].UnitPrice;
                    }
                }
            }

            return 0;
        }

        private Sprite ResolveIcon(string mineralId)
        {
            if (catalog == null || string.IsNullOrEmpty(mineralId))
            {
                return null;
            }

            return catalog.TryGetMineral(mineralId, out var data) ? data.Icon : null;
        }

        private bool HasAnyOwned()
        {
            if (inventory == null)
            {
                return false;
            }

            var snapshot = inventory.GetSnapshot();
            if (snapshot?.Stacks == null)
            {
                return false;
            }

            for (var i = 0; i < snapshot.Stacks.Count; i++)
            {
                if (snapshot.Stacks[i].Quantity > 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static int ClampQty(int qty, int owned)
        {
            if (owned <= 0)
            {
                return 0;
            }

            if (qty < 1)
            {
                return 1;
            }

            return qty > owned ? owned : qty;
        }
    }
}

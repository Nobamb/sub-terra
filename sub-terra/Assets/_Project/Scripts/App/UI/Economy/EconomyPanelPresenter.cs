using System.Collections.Generic;
using SubTerra.App.Economy;
using SubTerra.Shared;

namespace SubTerra.App.UI.Economy
{
    /// <summary>
    /// 판매·제작 UI Presenter.
    /// 버튼 핸들러는 EconomyService/CraftingService만 호출하고 GameState·Inventory를 직접 쓰지 않는다.
    /// 처리 중 busy 가드로 동일 프레임 중복 클릭이 한 건만 반영되게 한다.
    /// </summary>
    public sealed class EconomyPanelPresenter
    {
        private readonly IEconomyPanelView view;
        private EconomyService economy;
        private CraftingService crafting;
        private bool busy;

        public EconomyPanelPresenter(IEconomyPanelView view)
        {
            this.view = view;
        }

        public bool IsBusy => busy;
        public bool IsBound => economy != null;

        public void Bind(EconomyService economyService, CraftingService craftingService = null)
        {
            Unbind();
            economy = economyService;
            crafting = craftingService;

            if (economy != null)
            {
                economy.TransactionCompleted += OnEconomyTransaction;
            }

            if (crafting != null)
            {
                crafting.CraftCompleted += OnCraftCompleted;
            }

            view?.SetBusy(false);
            view?.SetStatusMessage(string.Empty);
            view?.SetStatusDetail(string.Empty);
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

            busy = false;
            view?.SetBusy(false);
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

        private void OnEconomyTransaction(EconomyTransactionResult result)
        {
            // 판매·Spend 결과 표시. Craft는 CraftCompleted에서 최종 메시지를 덮어쓸 수 있다.
            if (result.Kind == EconomyTransactionKind.Craft)
            {
                return;
            }

            ApplyResultToView(result);
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
    }
}

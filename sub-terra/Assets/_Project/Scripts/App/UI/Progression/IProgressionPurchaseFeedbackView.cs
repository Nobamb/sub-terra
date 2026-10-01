using SubTerra.App.Progression;

namespace SubTerra.App.UI.Progression
{
    /// <summary>
    /// 구매 결과를 연출로 표현하는 선택적 View 계약.
    /// 비용 차감과 레벨 반영은 ProgressionService가 확정하고, View는 그 결과만 표현한다.
    /// 기존 IProgressionPanelView 구현체(테스트 포함)를 깨지 않도록 별도 인터페이스로 둔다.
    /// </summary>
    public interface IProgressionPurchaseFeedbackView
    {
        void OnPurchaseCompleted(ProgressionPurchaseResult result);
    }
}

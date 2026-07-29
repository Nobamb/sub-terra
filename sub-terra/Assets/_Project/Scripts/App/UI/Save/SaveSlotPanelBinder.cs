using SubTerra.App.Save;
using UnityEngine;

namespace SubTerra.App.UI.Save
{
    /// <summary>MainMenu의 슬롯 View를 전역 저장 런타임과 연결한다.</summary>
    public sealed class SaveSlotPanelBinder : MonoBehaviour
    {
        [SerializeField] private SaveSlotPanelView view;

        private SaveSlotPanelPresenter presenter;

        public bool IsBound => presenter != null;

        private void OnEnable()
        {
            var runtime = SaveRuntimeController.Instance;
            if (view == null)
            {
                view = GetComponent<SaveSlotPanelView>();
            }

            if (runtime == null || view == null)
            {
                return;
            }

            presenter = new SaveSlotPanelPresenter(view, runtime.Loader);
            presenter.ContinueRequested += Continue;
            presenter.StartNewGameRequested += StartNewGame;
            presenter.Refresh();
        }

        private void OnDisable()
        {
            if (presenter == null)
            {
                return;
            }

            presenter.ContinueRequested -= Continue;
            presenter.StartNewGameRequested -= StartNewGame;
            presenter.Dispose();
            presenter = null;
        }

        public bool HasRequiredReferences()
        {
            return view != null && view.HasRequiredReferences();
        }

        private void Continue(int slotId)
        {
            var runtime = SaveRuntimeController.Instance;
            if (runtime == null)
            {
                return;
            }

            runtime.BeginContinue(slotId, OnContinueCompleted);
        }

        private void OnContinueCompleted(ContinueResult result)
        {
            if (this == null || result == null || result.IsSuccess)
            {
                return;
            }

            view.ShowLoadResult(result.Load);
            if (presenter != null)
            {
                presenter.Refresh();
            }
        }

        private void StartNewGame(int slotId)
        {
            // Phase L: 기존 세이브가 있으면 확인 없이 덮어쓰지 않는다.
            // SaveSlotPanel 단독 경로는 빈 슬롯 전용. 덮어쓰기는 MainMenu 확인 UI를 사용한다.
            SaveRuntimeController.Instance?.StartNewGame(slotId, confirmOverwrite: false);
        }
    }
}

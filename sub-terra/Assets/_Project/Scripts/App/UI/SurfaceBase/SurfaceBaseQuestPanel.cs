using SubTerra.App.Core;
using SubTerra.App.Audio;
using SubTerra.Shared.Audio;
using SubTerra.App.State;
using SubTerra.App.Tutorial;
using SubTerra.App.UI.Tutorial;
using UnityEngine;

namespace SubTerra.App.UI.SurfaceBase
{
    /// <summary>기존 퀘스트 상세창을 지상에서 읽기 전용으로 연다. 진행·보상 처리는 연결하지 않는다.</summary>
    public sealed class SurfaceBaseQuestPanel : MonoBehaviour
    {
        private const SfxId QuestClickSfx = SfxId.UiClick;
        [SerializeField] private UnityEngine.UI.Button questButton;
        [SerializeField] private DemoObjectiveView questView;
        private DemoObjectiveDirector director;
        private DemoObjectivePresenter presenter;
        private GameState boundState;

        public bool IsOpen => presenter != null && presenter.IsDetailsOpen;

        private void OnEnable()
        {
            UiSfx.Attach(questButton, QuestClickSfx, hover: true);
            questButton?.onClick.AddListener(OnQuestClicked);
            if (questView == null) return;
            questView.DetailsDismissRequested += Close;
            questView.DetailsPrevRequested += Previous;
            questView.DetailsNextRequested += Next;
        }

        private void OnDisable()
        {
            questButton?.onClick.RemoveListener(OnQuestClicked);
            if (questView != null)
            {
                questView.DetailsDismissRequested -= Close;
                questView.DetailsPrevRequested -= Previous;
                questView.DetailsNextRequested -= Next;
            }
            Close();
        }

        private void OnQuestClicked() => Open(GameBootstrapper.Instance?.State);

        public bool Open(GameState state)
        {
            if (questView == null || state?.Progress == null) return false;
            if (IsOpen && boundState == state)
            {
                questView.SetDetailsVisible(true);
                return true;
            }
            Close();
            boundState = state;
            // GameState를 Director에 바인딩하지 않아 조회·닫기로 원본 진행을 변경하지 않는다.
            director = new DemoObjectiveDirector();
            director.RestoreFromProgress(state.Progress);
            questView.gameObject.SetActive(true);
            presenter = new DemoObjectivePresenter(questView);
            presenter.Bind(director);
            presenter.OpenDetails();
            boundState.DemoProgressChanged += RefreshProgress;
            return true;
        }

        public bool TryCloseTopWindow(Canvas canvas)
        {
            if (!IsOpen || !PopupWindowSorting.Contains(canvas, questView.gameObject)) return false;
            Close();
            return true;
        }

        public void Close()
        {
            if (boundState != null) boundState.DemoProgressChanged -= RefreshProgress;
            boundState = null;
            presenter?.Dispose();
            presenter = null;
            director = null;
            // 상세창이 역순 닫힘을 마칠 때까지 상위 Canvas를 유지한다.
        }

        private void RefreshProgress() => director?.RestoreFromProgress(boundState?.Progress);
        private void Previous() => presenter?.ShowPreviousQuest();
        private void Next() => presenter?.ShowNextQuest();
    }
}

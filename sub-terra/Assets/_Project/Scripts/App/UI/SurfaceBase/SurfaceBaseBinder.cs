using SubTerra.App.Core;
using SubTerra.App.Save;
using SubTerra.App.UI.Economy;
using SubTerra.App.UI.Progression;
using UnityEngine;

namespace SubTerra.App.UI.SurfaceBase
{
    /// <summary>
    /// Surface Base Scene 조립.
    /// 판매·제작·업그레이드는 기존 Economy/Progression 바인더에 연결하고
    /// 목표·잠금·탐사 시작만 Surface Presenter가 담당한다.
    /// 탐사 단일 비행 가드는 SaveRuntimeController.TryStartExploration 한 경로만 사용한다.
    /// </summary>
    public sealed class SurfaceBaseBinder : MonoBehaviour
    {
        [SerializeField] private SurfaceBaseView view;
        [SerializeField] private EconomyPanelBinder economyBinder;
        [SerializeField] private ProgressionPanelBinder progressionBinder;

        private SurfaceBasePresenter presenter;

        public SurfaceBasePresenter Presenter => presenter;
        public bool IsBound => presenter != null;

        private void OnEnable()
        {
            if (view == null)
            {
                view = GetComponent<SurfaceBaseView>();
            }

            var runtime = SaveRuntimeController.Instance;
            var bootstrap = GameBootstrapper.Instance;
            if (view == null || runtime == null || bootstrap == null)
            {
                return;
            }

            // 런타임이 Economy/Progression을 소유. Surface는 복제 트랜잭션을 만들지 않는다.
            runtime.EnsureGameplayServices();
            if (economyBinder != null && runtime.Economy != null)
            {
                economyBinder.BindTo(runtime.Economy, runtime.Crafting);
            }

            if (progressionBinder != null && runtime.Progression != null)
            {
                // 구매 성공 후 TryUnlockDeepZone에 넘길 완료 목표 수(GameState).
                var state = bootstrap.State;
                progressionBinder.BindTo(
                    runtime.Progression,
                    () => state != null && state.Progress != null
                        ? state.Progress.CompletedObjectives
                        : 0);
            }

            presenter = new SurfaceBasePresenter(view);
            presenter.Bind(bootstrap.State, runtime.Progression);
            view.ExploreClicked += OnExploreClicked;
            view.RefreshClicked += OnRefreshClicked;
            presenter.RefreshReadModel();
        }

        private void OnDisable()
        {
            if (view != null)
            {
                view.ExploreClicked -= OnExploreClicked;
                view.RefreshClicked -= OnRefreshClicked;
            }

            if (presenter != null)
            {
                presenter.Dispose();
                presenter = null;
            }

            economyBinder?.Unbind();
            progressionBinder?.Presenter?.Unbind();
        }

        public bool HasRequiredReferences()
        {
            return view != null && view.HasRequiredReferences();
        }

        private void OnExploreClicked()
        {
            if (presenter == null)
            {
                return;
            }

            // 단일 진입점: 런타임 가드+Scene 로드 결과만 Presenter에 전달한다.
            // Presenter가 자체 가드로 선점 성공 처리하지 않는다.
            presenter.RequestExplorationStart(TryStartExplorationViaRuntime);
        }

        private void OnRefreshClicked()
        {
            presenter?.RefreshReadModel();
        }

        /// <summary>
        /// 런타임 TryStartExploration만 호출. 실패 시 reason을 그대로 돌려 실패 UI를 연다.
        /// </summary>
        private (bool success, string reason) TryStartExplorationViaRuntime()
        {
            var runtime = SaveRuntimeController.Instance;
            if (runtime == null)
            {
                return (false, "저장 런타임 없음");
            }

            // 유효 슬롯·State 없거나 로드 실패 시 광산 Scene에 들어가지 않는다.
            if (!runtime.TryStartExploration(out var reason))
            {
                return (false, reason);
            }

            return (true, string.Empty);
        }
    }
}

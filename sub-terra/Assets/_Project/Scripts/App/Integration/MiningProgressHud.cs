using SubTerra.App.UI.PlayerStatus;
using SubTerra.Gameplay.Mining;
using UnityEngine;

namespace SubTerra.App.Integration
{
    /// <summary>
    /// 채굴 상태를 플레이어 머리 위 문구 스택으로 보낸다.
    /// 진행("채굴 N%")과 결과는 청록 정보 문구, 실패는 노란 경고 문구로 뜬다.
    /// 진행 항목은 같은 키로 갱신만 하고, 완료 시 같은 자리에서 결과 문구로 바뀐다.
    /// </summary>
    public sealed class MiningProgressHud : MonoBehaviour
    {
        private const string ProgressKey = "mining.progress";
        private const string FailureKey = "mining.failure";

        // 예전 화면 공간 게이지 루트. 씬 호환을 위해 남겨 두고, 항상 꺼 둔다.
        [SerializeField] private GameObject statusRoot;

        private MiningSystem boundSystem;
        private Transform playerTarget;
        private PlayerHeadMessageStack messages;
        private System.Func<bool> isPlacementActive;
        private string pendingYieldText;

        public void BindTo(MiningSystem system, Transform player)
        {
            if (boundSystem == system && playerTarget == player)
            {
                return;
            }

            if (boundSystem != null)
            {
                boundSystem.ProgressChanged -= OnProgressChanged;
            }

            boundSystem = system;
            playerTarget = player;
            if (statusRoot != null)
            {
                statusRoot.SetActive(false);
            }

            if (boundSystem != null)
            {
                boundSystem.ProgressChanged += OnProgressChanged;
            }
        }

        public void BindTo(MiningSystem system) => BindTo(system, playerTarget);

        /// <summary>문구를 그릴 머리 위 스택을 넘긴다. 없으면 아무 문구도 띄우지 않는다.</summary>
        public void SetMessageStack(PlayerHeadMessageStack stack)
        {
            messages = stack;
        }

        /// <summary>지금 시설 설치 준비(선택) 상태인지 알려 주는 조회. 없으면 항상 채굴 문구를 쓴다.</summary>
        public void SetPlacementProbe(System.Func<bool> probe)
        {
            isPlacementActive = probe;
        }

        /// <summary>
        /// 채굴 커밋이 Completed 이벤트를 보내기 전에 수확 문구를 맡겨 둔다.
        /// Completed에서 진행 문구를 이 결과 문구로 바꾼다.
        /// </summary>
        public void SetPendingYieldFeedback(string text)
        {
            pendingYieldText = string.IsNullOrEmpty(text) ? null : text;
        }

        private void OnDisable()
        {
            if (boundSystem != null)
            {
                boundSystem.ProgressChanged -= OnProgressChanged;
                boundSystem = null;
            }
        }

        private void OnProgressChanged(MiningProgressState state)
        {
            if (messages == null)
            {
                return;
            }

            switch (state.Phase)
            {
                case MiningPhase.Mining:
                    messages.SetProgress(
                        ProgressKey,
                        $"채굴 {Mathf.RoundToInt(state.Progress * 100f)}%",
                        Mathf.Clamp01(state.Progress));
                    break;
                case MiningPhase.Failed:
                    pendingYieldText = null;
                    messages.Dismiss(ProgressKey);
                    messages.ShowWarning(FailureKey, FailureMessage(state.FailureReason));
                    break;
                case MiningPhase.Completed when !string.IsNullOrEmpty(pendingYieldText):
                    messages.ShowInfo(ProgressKey, pendingYieldText);
                    pendingYieldText = null;
                    break;
                default:
                    messages.Dismiss(ProgressKey);
                    break;
            }
        }

        /// <summary>설치 준비 중 시설과 겹친 칸을 눌렀을 때 채굴 문구 대신 쓰는 안내.</summary>
        public const string PlacementBlockedMessage = "설치할 수 없는 지형입니다.";

        private string FailureMessage(MiningFailureReason reason)
        {
            return FailureMessage(reason, isPlacementActive != null && isPlacementActive());
        }

        /// <summary>
        /// 설치 준비 상태에서 누른 칸이 채굴 대상이 아니면(시설 등 보호 칸, 타일 없는 빈 칸, 캘 수 없는 지형)
        /// 채굴이 아니라 설치 시도이므로 설치 문구를 쓴다. 드릴·전력·화물 같은 채굴 조건 안내는 그대로 둔다.
        /// </summary>
        public static string FailureMessage(MiningFailureReason reason, bool placementActive)
        {
            if (placementActive
                && (reason == MiningFailureReason.NotMineable || reason == MiningFailureReason.InvalidTarget))
            {
                return PlacementBlockedMessage;
            }

            return reason switch
            {
                MiningFailureReason.DrillLevelTooLow => "드릴 레벨이 부족합니다. 지상에서 업그레이드하세요.",
                MiningFailureReason.InsufficientEnergy => "채굴 전력이 부족합니다.",
                MiningFailureReason.InventoryFull => "화물이 가득 찼습니다. 귀환해 화물을 비우세요.",
                MiningFailureReason.OutOfRange => "채굴 범위를 벗어났습니다.",
                MiningFailureReason.NotMineable => "채굴할 수 없는 지형입니다.",
                MiningFailureReason.DeepZoneLocked => "심층 구역이 해금되어야 채굴할 수 있는 자원입니다.",
                _ => "채굴할 수 없습니다."
            };
        }
    }
}

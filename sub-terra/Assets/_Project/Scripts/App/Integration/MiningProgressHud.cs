using SubTerra.Gameplay.Mining;
using TMPro;
using UnityEngine;

namespace SubTerra.App.Integration
{
    /// <summary>채굴 진행률과 차단 사유를 MiningSystem 이벤트에서만 표시한다.</summary>
    public sealed class MiningProgressHud : MonoBehaviour
    {
        [SerializeField] private GameObject statusRoot;
        [SerializeField] private TMP_Text statusText;

        private MiningSystem boundSystem;

        public void BindTo(MiningSystem system)
        {
            if (boundSystem == system)
            {
                return;
            }

            if (boundSystem != null)
            {
                boundSystem.ProgressChanged -= OnProgressChanged;
            }

            boundSystem = system;
            if (boundSystem != null)
            {
                boundSystem.ProgressChanged += OnProgressChanged;
            }

            SetVisible(false);
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
            if (statusText == null)
            {
                return;
            }

            statusText.text = state.Phase switch
            {
                MiningPhase.Mining => $"채굴 {Mathf.RoundToInt(state.Progress * 100f)}%  |  전력 {state.EnergyCost}",
                MiningPhase.Completed => "채굴 완료",
                MiningPhase.Cancelled => "채굴 취소",
                MiningPhase.Failed => FailureMessage(state.FailureReason),
                _ => string.Empty
            };
            SetVisible(state.Phase != MiningPhase.Idle);
        }

        private void SetVisible(bool visible)
        {
            if (statusRoot != null)
            {
                statusRoot.SetActive(visible);
            }
        }

        private static string FailureMessage(MiningFailureReason reason)
        {
            return reason switch
            {
                MiningFailureReason.DrillLevelTooLow => "드릴 레벨이 부족합니다.",
                MiningFailureReason.InsufficientEnergy => "채굴 전력이 부족합니다.",
                MiningFailureReason.InventoryFull => "화물이 가득 찼습니다.",
                MiningFailureReason.OutOfRange => "채굴 범위를 벗어났습니다.",
                MiningFailureReason.NotMineable => "채굴할 수 없는 지형입니다.",
                _ => "채굴할 수 없습니다."
            };
        }
    }
}

using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SubTerra.App.UI.Economy
{
    /// <summary>
    /// 판매·제작 결과 표시용 최소 View.
    /// TMP/UGUI 텍스트에 메시지만 쓰고, 버튼 핸들러는 Presenter에 위임한다.
    /// </summary>
    public sealed class EconomyPanelView : MonoBehaviour, IEconomyPanelView
    {
        [SerializeField] private TMP_Text statusMessageText;
        [SerializeField] private TMP_Text statusDetailText;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Selectable[] controlsToDisableWhenBusy;

        public void SetStatusMessage(string message)
        {
            if (statusMessageText != null)
            {
                statusMessageText.text = message ?? string.Empty;
            }
        }

        public void SetStatusDetail(string detail)
        {
            if (statusDetailText != null)
            {
                statusDetailText.text = detail ?? string.Empty;
            }
        }

        public void SetBusy(bool busy)
        {
            if (controlsToDisableWhenBusy == null)
            {
                return;
            }

            for (var i = 0; i < controlsToDisableWhenBusy.Length; i++)
            {
                var control = controlsToDisableWhenBusy[i];
                if (control != null)
                {
                    control.interactable = !busy;
                }
            }
        }

        public void SetVisible(bool visible)
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = visible ? 1f : 0f;
                canvasGroup.interactable = visible;
                canvasGroup.blocksRaycasts = visible;
                return;
            }

            gameObject.SetActive(visible);
        }

#if UNITY_EDITOR
        public void EditorBind(
            TMP_Text message,
            TMP_Text detail,
            CanvasGroup group,
            Selectable[] controls)
        {
            statusMessageText = message;
            statusDetailText = detail;
            canvasGroup = group;
            controlsToDisableWhenBusy = controls;
        }
#endif
    }
}

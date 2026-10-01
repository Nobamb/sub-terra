using UnityEngine;
using UnityEngine.EventSystems;

namespace SubTerra.App.UI.Progression
{
    /// <summary>
    /// prompt-B 118-1: 업그레이드 모달의 어두운 배경 위에서 스크롤해도 트리가 확대/축소되도록 창으로 넘긴다.
    /// </summary>
    public sealed class UpgradeTreeScrollForwarder : MonoBehaviour, IScrollHandler
    {
        [SerializeField] private ProgressionPanelView target;

        public void OnScroll(PointerEventData eventData)
        {
            if (target == null)
            {
                target = GetComponentInChildren<ProgressionPanelView>(true);
            }

            if (target != null)
            {
                target.OnScroll(eventData);
            }
        }
    }
}

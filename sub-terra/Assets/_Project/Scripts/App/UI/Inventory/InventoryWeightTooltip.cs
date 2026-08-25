using UnityEngine;
using UnityEngine.EventSystems;

namespace SubTerra.App.UI.Inventory
{
    /// <summary>인벤토리 중량 물음표를 가리키는 동안 도움말을 표시한다.</summary>
    public sealed class InventoryWeightTooltip :
        MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler
    {
        [SerializeField] private GameObject tooltipRoot;

        public GameObject TooltipRoot => tooltipRoot;

        private void Awake()
        {
            SetVisible(false);
        }

        private void OnDisable()
        {
            SetVisible(false);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            SetVisible(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            SetVisible(false);
        }

        private void SetVisible(bool visible)
        {
            if (tooltipRoot != null)
            {
                tooltipRoot.SetActive(visible);
            }
        }
    }
}

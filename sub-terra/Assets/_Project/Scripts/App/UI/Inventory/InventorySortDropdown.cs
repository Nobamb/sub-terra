using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SubTerra.App.UI.Inventory
{
    /// <summary>
    /// prompt-B 190: 적재 내역 정렬 드롭다운.
    /// 인벤토리 창은 팝업 정렬(32100+) Canvas로 올라가므로 TMP 기본 목록(30000)이 창 뒤에 깔린다.
    /// 목록을 만들 때 창보다 위로 올리고(차단막은 TMP가 목록-1로 맞춘다), 채굴 Enter로는 열리지 않게 한다.
    /// </summary>
    public sealed class InventorySortDropdown : TMP_Dropdown
    {
        // TMP_Dropdown이 펼친 목록 Canvas에 넣는 기본 정렬값.
        public const int DefaultListOrder = 30000;

        public static int ListOrderAbove(int windowOrder)
        {
            return Mathf.Max(DefaultListOrder, windowOrder + 2);
        }

        protected override GameObject CreateDropdownList(GameObject template)
        {
            var list = base.CreateDropdownList(template);
            var listCanvas = list != null ? list.GetComponent<Canvas>() : null;
            var windowCanvas = transform.parent != null ? transform.parent.GetComponentInParent<Canvas>() : null;
            if (listCanvas != null && windowCanvas != null)
            {
                listCanvas.overrideSorting = true;
                listCanvas.sortingOrder = ListOrderAbove(windowCanvas.sortingOrder);
            }

            return list;
        }

        public override void OnSubmit(BaseEventData eventData)
        {
            // 항목을 고른 뒤 선택이 남아 있어도 채굴 Enter가 목록을 다시 열지 않게 한다(마우스로만 연다).
        }
    }
}

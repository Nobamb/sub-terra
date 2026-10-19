using UnityEngine;
using UnityEngine.EventSystems;

namespace SubTerra.App.UI.MainMenu
{
    /// <summary>
    /// 슬롯 삭제 버튼 호버 시 쓰레기통 아이콘을 흰색으로 바꾼다.
    /// 판의 호버 발광과 아이콘이 같은 청록이라 묻히는 것을 막는다. 게임 상태는 바꾸지 않는다.
    /// </summary>
    public sealed class SlotDeleteButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Tooltip("평소 아이콘 위에 겹쳐 둔 흰색 아이콘. 호버 때 알파가 올라간다.")]
        [SerializeField] private UnityEngine.UI.Image whiteIcon;
        [SerializeField] private float fadeSeconds = 0.15f;

        private bool hovered;
        private float amount;

        public float WhiteAmount => amount;

        public void OnPointerEnter(PointerEventData eventData) => hovered = true;
        public void OnPointerExit(PointerEventData eventData) => hovered = false;

        private void OnEnable()
        {
            hovered = false;
            amount = 0f;
            Apply();
        }

        private void Update()
        {
            var target = hovered ? 1f : 0f;
            if (Mathf.Approximately(amount, target)) return;
            amount = Mathf.MoveTowards(amount, target, Time.unscaledDeltaTime / Mathf.Max(0.01f, fadeSeconds));
            Apply();
        }

        private void Apply()
        {
            if (whiteIcon == null) return;
            var c = whiteIcon.color;
            c.a = amount;
            whiteIcon.color = c;
        }
    }
}

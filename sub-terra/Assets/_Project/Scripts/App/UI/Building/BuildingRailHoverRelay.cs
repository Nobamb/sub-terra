using UnityEngine;
using UnityEngine.EventSystems;

namespace SubTerra.App.UI.Building
{
    /// <summary>
    /// B-188 레일 아이콘 한 칸의 마우스 올림·내림을 View에 알린다. 선택·클릭은 BuildingMenuEntryButton이 그대로 맡는다.
    /// </summary>
    [RequireComponent(typeof(BuildingMenuEntryButton))]
    public sealed class BuildingRailHoverRelay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private BuildingMenuView view;

        private BuildingMenuEntryButton entry;

        public void OnPointerEnter(PointerEventData eventData)
        {
            var target = View();
            if (target != null)
            {
                target.ShowRailHover(BuildingId(), (RectTransform)transform);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            var target = View();
            if (target != null)
            {
                target.HideRailHover((RectTransform)transform);
            }
        }

        private void OnDisable()
        {
            var target = View();
            if (target != null)
            {
                target.HideRailHover((RectTransform)transform);
            }
        }

        private string BuildingId()
        {
            if (entry == null)
            {
                entry = GetComponent<BuildingMenuEntryButton>();
            }

            return entry != null ? entry.BuildingId : string.Empty;
        }

        private BuildingMenuView View()
        {
            if (view == null)
            {
                view = GetComponentInParent<BuildingMenuView>(true);
            }

            return view;
        }

#if UNITY_EDITOR
        public void EditorSet(BuildingMenuView target)
        {
            view = target;
        }
#endif
    }
}

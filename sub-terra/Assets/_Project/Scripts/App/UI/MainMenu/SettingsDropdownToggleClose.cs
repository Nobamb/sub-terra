using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SubTerra.App.UI.MainMenu
{
    /// <summary>
    /// 열린 드롭다운 헤더를 다시 누르면 닫는다.
    /// TMP는 이미 열린 Show를 무시하므로, 클릭 처리가 끝난 다음 프레임에 Hide한다.
    /// </summary>
    public sealed class SettingsDropdownToggleClose : MonoBehaviour, IPointerDownHandler, IPointerClickHandler
    {
        private TMP_Dropdown dropdown;
        private bool closeAfterClick;
        private Coroutine closeRoutine;

        private void Awake()
        {
            dropdown = GetComponent<TMP_Dropdown>();
        }

        private void OnDisable()
        {
            if (closeRoutine != null)
            {
                StopCoroutine(closeRoutine);
                closeRoutine = null;
            }

            closeAfterClick = false;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            closeAfterClick = ShouldClose(dropdown != null && dropdown.IsExpanded);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!closeAfterClick || dropdown == null)
            {
                return;
            }

            closeAfterClick = false;
            if (closeRoutine != null)
            {
                StopCoroutine(closeRoutine);
            }

            closeRoutine = StartCoroutine(HideAfterClick());
        }

        public static bool ShouldClose(bool expandedAtPointerDown)
        {
            return expandedAtPointerDown;
        }

        private IEnumerator HideAfterClick()
        {
            yield return null;
            closeRoutine = null;
            if (dropdown != null && dropdown.IsExpanded)
            {
                dropdown.Hide();
            }
        }
    }
}

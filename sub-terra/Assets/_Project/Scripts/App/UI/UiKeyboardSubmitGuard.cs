using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SubTerra.App.UI
{
    /// <summary>
    /// Enter/Submit가 남아 있는 UI 선택(단축키 버튼 등)에 전달되지 않도록 보호한다.
    /// prompt-B 35-3: 시설 건설 창이 열린 채 Enter를 누르면 EventSystem Submit이
    /// 게임 가이드 토글 등 의도하지 않은 버튼을 다시 누르는 회귀를 막는다.
    /// </summary>
    public static class UiKeyboardSubmitGuard
    {
        /// <summary>현재 EventSystem 선택을 해제한다. 선택이 없으면 아무 것도 하지 않는다.</summary>
        public static void ClearSelection()
        {
            var eventSystem = EventSystem.current;
            if (eventSystem == null || eventSystem.currentSelectedGameObject == null)
            {
                return;
            }

            eventSystem.SetSelectedGameObject(null);
        }

        /// <summary>
        /// 키보드 네비게이션으로 단축키 버튼이 선택되지 않게 하고,
        /// 클릭 직후 선택을 남겨 두지 않는다(다음 Enter가 onClick을 재실행하지 않음).
        /// </summary>
        public static void ConfigurePointerPreferredButton(Button button)
        {
            if (button == null)
            {
                return;
            }

            var nav = button.navigation;
            nav.mode = Navigation.Mode.None;
            button.navigation = nav;

            // 동일 리스너 중복 등록 방지.
            button.onClick.RemoveListener(ClearSelection);
            button.onClick.AddListener(ClearSelection);
        }

        /// <summary>대상 하위의 모든 Button에 pointer-preferred 설정을 적용한다.</summary>
        public static void ConfigureButtonsUnder(Transform root)
        {
            if (root == null)
            {
                return;
            }

            var buttons = root.GetComponentsInChildren<Button>(true);
            for (var i = 0; i < buttons.Length; i++)
            {
                ConfigurePointerPreferredButton(buttons[i]);
            }
        }
    }
}

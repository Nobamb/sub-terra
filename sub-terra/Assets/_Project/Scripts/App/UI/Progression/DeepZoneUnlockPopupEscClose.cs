using UnityEngine;
using UnityEngine.InputSystem;

namespace SubTerra.App.UI.Progression
{
    /// <summary>
    /// 심층 해금 팝업 전용 ESC 닫기.
    /// 팝업이 업그레이드 패널과 다른 Canvas에 붙어도 닫기 버튼과 같은 Hide 경로를 탄다.
    /// </summary>
    public sealed class DeepZoneUnlockPopupEscClose : MonoBehaviour
    {
        private ProgressionPanelView owner;

        public void Bind(ProgressionPanelView view)
        {
            owner = view;
        }

        private void Update()
        {
            if (owner == null || !isActiveAndEnabled)
            {
                return;
            }

            var keyboard = Keyboard.current;
            if (keyboard == null || !keyboard.escapeKey.wasPressedThisFrame)
            {
                return;
            }

            owner.HideDeepZoneUnlockPopup();
        }
    }
}

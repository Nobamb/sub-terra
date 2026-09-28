using UnityEngine;

namespace SubTerra.App.UI.Progression
{
    /// <summary>
    /// 심층 해금 팝업을 기존 Scene 참조와 연결한다.
    /// 팝업이 업그레이드 패널과 다른 Canvas에 붙어도 닫기 버튼과 같은 Hide 경로를 탄다.
    /// </summary>
    public sealed class DeepZoneUnlockPopupEscClose : MonoBehaviour
    {
        private ProgressionPanelView owner;

        public void Bind(ProgressionPanelView view)
        {
            owner = view;
        }

        public bool Close() => owner != null && owner.TryHideDeepZoneUnlockPopup();
    }
}

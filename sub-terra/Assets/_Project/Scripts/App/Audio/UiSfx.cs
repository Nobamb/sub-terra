using SubTerra.Shared.Audio;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SubTerra.App.Audio
{
    /// <summary>
    /// 버튼에 공통 클릭·호버 소리를 붙이는 컴포넌트. 코드로 만든 버튼은 UiSfx.Attach를, 프리팹은 직접 붙인다.
    /// 비활성 버튼을 누르면 UiCancelFail이 난다(failWhenDisabled가 꺼져 있으면 무음).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UiSfxButton : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler
    {
        [SerializeField] private Button button;
        [SerializeField] private SfxId clickSfx = SfxId.UiClick;
        [SerializeField] private bool hover;
        [SerializeField] private bool failWhenDisabled = true;

        private bool listening;

        public SfxId ClickSfx => clickSfx;
        public bool Hover => hover;
        public bool FailWhenDisabled => failWhenDisabled;

        public void Configure(Button target, SfxId click, bool playHover, bool failOnDisabledClick = true)
        {
            Unlisten();
            button = target;
            clickSfx = click;
            hover = playHover;
            failWhenDisabled = failOnDisabledClick;
            Listen();
        }

        private void Awake()
        {
            if (button == null)
            {
                button = GetComponent<Button>();
            }
        }

        private void OnEnable()
        {
            if (button == null)
            {
                button = GetComponent<Button>();
            }

            Listen();
        }

        private void OnDisable()
        {
            Unlisten();
        }

        private void Listen()
        {
            if (listening || button == null)
            {
                return;
            }

            button.onClick.AddListener(OnClicked);
            listening = true;
        }

        private void Unlisten()
        {
            if (!listening)
            {
                return;
            }

            if (button != null)
            {
                button.onClick.RemoveListener(OnClicked);
            }

            listening = false;
        }

        private void OnClicked()
        {
            if (clickSfx != SfxId.None)
            {
                SfxPlayer.Play(clickSfx);
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            // 활성 버튼은 onClick이 소리를 내므로, 눌렸는데 반응하지 않는 버튼만 여기서 처리한다.
            if (failWhenDisabled && button != null && !button.IsInteractable())
            {
                SfxPlayer.Play(SfxId.UiCancelFail);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (hover && button != null && button.IsInteractable())
            {
                SfxPlayer.Play(SfxId.UiHover);
            }
        }
    }

    public static class UiSfx
    {
        /// <summary>같은 버튼에 여러 번 불러도 소리가 한 번만 나도록 기존 컴포넌트를 재사용한다.</summary>
        public static UiSfxButton Attach(Button button, SfxId click = SfxId.UiClick, bool hover = false,
            bool failWhenDisabled = true)
        {
            if (button == null)
            {
                return null;
            }

            var component = button.GetComponent<UiSfxButton>();
            if (component == null)
            {
                component = button.gameObject.AddComponent<UiSfxButton>();
            }

            component.Configure(button, click, hover, failWhenDisabled);
            return component;
        }
    }
}

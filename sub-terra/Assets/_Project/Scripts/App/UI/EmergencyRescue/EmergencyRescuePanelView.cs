using System;
using SubTerra.App.Run;
using SubTerra.App.Tutorial;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.UI.EmergencyRescue
{
    /// <summary>
    /// 전력 고갈 구출 UI의 진입점. 팝업(EmergencyRescuePopupView)과 머리 위 홀로그램 안내(EmergencyRescueChipView)를
    /// 묶어 컨트롤러에 표시·닫기·재열기 API만 보여 준다. 구출 규칙·비용 계산·입력 처리는 여기에 없다.
    /// </summary>
    public sealed class EmergencyRescuePanelView : MonoBehaviour
    {
        private EmergencyRescuePopupView popup;
        private EmergencyRescueChipView chip;

        /// <summary>등장 중이거나 열려 있음. 닫는 중은 포함하지 않는다.</summary>
        public bool IsOpen => popup != null && popup.IsOpen;
        public bool IsClosing => popup != null && popup.IsClosing;
        /// <summary>팝업이 화면에 있음(등장·열림·닫는 중).</summary>
        public bool IsPopupVisible => popup != null && popup.IsVisible;
        /// <summary>등장이 끝나 입력을 받을 수 있음.</summary>
        public bool IsStable => popup != null && popup.IsStable;
        public bool IsChipVisible => chip != null && chip.IsShown;
        public EmergencyRescuePopupView Popup => popup;
        public EmergencyRescueChipView Chip => chip;
        public EmergencyRescueCost DisplayedCost => popup != null ? popup.DisplayedCost : null;

        public static EmergencyRescuePanelView Create(
            Transform canvasRoot,
            TMP_FontAsset font)
        {
            if (canvasRoot == null)
            {
                return null;
            }

            var root = new GameObject(
                "EmergencyRescueOverlay",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Canvas),
                typeof(GraphicRaycaster));
            root.transform.SetParent(canvasRoot, false);
            EmergencyRescueUi.Stretch(root.GetComponent<RectTransform>());
            var popupCanvas = root.GetComponent<Canvas>();
            popupCanvas.overrideSorting = true;
            popupCanvas.sortingOrder = UiLayerPriority.EmergencyRescueModal;
            // 딤 면은 뒤쪽 클릭만 막고, 알파는 팝업 연출이 정한다.
            var blocker = root.GetComponent<Image>();
            blocker.raycastTarget = true;

            var view = root.AddComponent<EmergencyRescuePanelView>();
            view.popup = root.AddComponent<EmergencyRescuePopupView>();
            view.popup.Build(font);
            view.chip = EmergencyRescueChipView.Create(canvasRoot, font);
            return view;
        }

        public void SetFollowTarget(Transform player)
        {
            if (chip == null)
            {
                return;
            }

            var follow = chip.GetComponent<EmergencyRescueChipFollow>();
            if (follow != null)
            {
                follow.SetTarget(player);
            }
        }

        public void SetIconResolver(Func<string, Sprite> resolver)
        {
            if (popup != null)
            {
                popup.SetIconResolver(resolver);
            }
        }

        public void Bind(Action rescue, Action close, Action reopen)
        {
            if (popup != null)
            {
                ReplaceListener(popup.RescueButton, rescue);
                ReplaceListener(popup.CloseButton, close);
            }

            if (chip != null)
            {
                ReplaceListener(chip.Button, reopen);
            }
        }

        /// <summary>팝업을 연다(이미 열려 있으면 내용만 갱신). 닫는 중이면 false.</summary>
        public bool Show(EmergencyRescueCost cost, string message = null)
        {
            return popup != null && popup.Show(cost, message);
        }

        public void SetMessage(string message)
        {
            if (popup != null)
            {
                popup.SetMessage(message);
            }
        }

        /// <summary>강한 글리치 종료 연출 뒤 숨기고 onClosed를 부른다.</summary>
        public bool BeginClose(Action onClosed)
        {
            return popup != null && popup.BeginClose(onClosed);
        }

        /// <summary>연출 없이 팝업을 즉시 숨긴다. 대기 중인 닫기 콜백은 버려진다.</summary>
        public void HideImmediate()
        {
            if (popup != null)
            {
                popup.HideImmediate();
            }
        }

        public void SetChipVisible(bool visible)
        {
            if (chip == null)
            {
                return;
            }

            if (visible)
            {
                chip.Show();
            }
            else
            {
                chip.HideImmediate();
            }
        }

        /// <summary>입력으로 팝업이 열릴 때 안내를 정리한다. 키캡이 눌렸다가 짧게 사라진다.</summary>
        public void DismissChip(bool withPress)
        {
            if (chip != null)
            {
                chip.Dismiss(withPress);
            }
        }

        public void SetInteractable(bool interactable)
        {
            if (popup != null)
            {
                popup.SetInteractable(interactable);
            }
        }

        private void OnDestroy()
        {
            if (chip == null)
            {
                return;
            }

            // 칩은 팝업과 다른 부모 아래에 있어 따로 정리한다. 에디트 모드 테스트에서는 즉시 파괴한다.
            if (Application.isPlaying)
            {
                Destroy(chip.gameObject);
            }
            else
            {
                DestroyImmediate(chip.gameObject);
            }
        }

        private static void ReplaceListener(Button button, Action action)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveAllListeners();
            if (action != null)
            {
                button.onClick.AddListener(() => action());
            }
        }
    }
}

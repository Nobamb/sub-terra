using System;
using SubTerra.App.UI.Guide;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.UI.HUD
{
    /// <summary>
    /// 게임 가이드 호스트(prompt-B 31 → B-140).
    /// 씬·프리팹의 GameGuidePanel은 그대로 두고, 플레이 중에는 코드로 만든 GameGuidePopupView를 대신 보여 준다.
    /// 기존 열기·닫기 경로(HudPanelChromeController의 G 키·오른쪽 메뉴·X)와 SetVisible·CloseButton 계약은 유지한다.
    /// 예전 프리팹의 글자 본문은 더 이상 쓰지 않으므로 플레이 중에는 숨긴다.
    /// </summary>
    public sealed class GameGuidePanelView : MonoBehaviour
    {
        public const int TabCount = 3;
        public const float GuideFontSize = 16f;

        public enum GuideTab
        {
            Controls = 0,
            Mechanics = 1,
            Resources = 2
        }

        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button[] tabButtons = new Button[TabCount];
        [SerializeField] private TMP_Text[] tabLabels = new TMP_Text[TabCount];
        [SerializeField] private TMP_Text bodyText;
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private RectTransform contentRoot;
        [SerializeField] private GuideTab activeTab = GuideTab.Controls;

        private GameGuidePopupView popup;

        public Button CloseButton => closeButton;
        public GuideTab ActiveTab => (GuideTab)(int)GameGuideState.Session.Tab;
        public GameGuidePopupView Popup => popup;

        public bool IsOpen
        {
            get
            {
                if (popup != null)
                {
                    return popup.IsOpen;
                }

                return (panelRoot != null ? panelRoot : gameObject).activeSelf;
            }
        }

        /// <summary>가이드 창의 X 버튼. 호스트(HudPanelChromeController)가 닫기 상태를 갱신한다.</summary>
        public event Action CloseRequested;

        /// <summary>가이드 창이 쓰는 Canvas인지(최상위 팝업 닫기 판정용).</summary>
        public bool OwnsCanvas(Canvas canvas)
        {
            return popup != null && popup.OwnsCanvas(canvas);
        }

        public void SetVisible(bool visible)
        {
            if (!Application.isPlaying)
            {
                SetVisibleLegacy(visible);
                return;
            }

            if (visible)
            {
                gameObject.SetActive(true);
                transform.SetAsLastSibling();
                if (panelRoot != null)
                {
                    // 예전 글자 본문 패널은 새 가이드 창으로 대체되었다.
                    panelRoot.SetActive(false);
                }

                try
                {
                    EnsurePopup();
                    if (popup != null)
                    {
                        popup.Show();
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError("[SubTerra] Game guide popup failed: " + ex);
                }

                return;
            }

            if (popup != null)
            {
                popup.BeginClose();
            }

            gameObject.SetActive(false);
        }

        public void SelectTab(GuideTab tab)
        {
            GameGuideState.Session.SelectTab((GuideTabKind)(int)tab);
        }

        public void SelectTabIndex(int index)
        {
            if (index < 0 || index >= TabCount)
            {
                return;
            }

            SelectTab((GuideTab)index);
        }

        public bool HasRequiredReferences()
        {
            if (closeButton == null || bodyText == null)
            {
                return false;
            }

            if (tabButtons == null || tabButtons.Length < TabCount)
            {
                return false;
            }

            for (var i = 0; i < TabCount; i++)
            {
                if (tabButtons[i] == null)
                {
                    return false;
                }
            }

            return true;
        }

        public static string GetTabTitle(GuideTab tab)
        {
            return GameGuideCatalog.TabTitle((GuideTabKind)(int)tab);
        }

        /// <summary>카드 제목·키·요약을 한 덩어리 글로 돌려준다(접근성·구형 빌더 호환용).</summary>
        public static string GetTabBody(GuideTab tab)
        {
            return GameGuideCatalog.PlainText((GuideTabKind)(int)tab);
        }

        private void OnDestroy()
        {
            if (popup != null)
            {
                popup.CloseRequested -= OnPopupCloseRequested;
                Destroy(popup.gameObject);
                popup = null;
            }
        }

        private void EnsurePopup()
        {
            if (popup != null)
            {
                return;
            }

            var canvas = GetComponentInParent<Canvas>(true);
            if (canvas == null)
            {
                return;
            }

            popup = GameGuidePopupView.Create(canvas.rootCanvas.transform, GameDataGuideData.FromBootstrap());
            if (popup != null)
            {
                popup.CloseRequested += OnPopupCloseRequested;
            }
        }

        private void OnPopupCloseRequested()
        {
            if (CloseRequested != null)
            {
                CloseRequested.Invoke();
            }
            else
            {
                SetVisible(false);
            }
        }

        // 에디터(비플레이)에서의 기존 동작: 루트 활성화와 PanelRoot 토글만 한다.
        private void SetVisibleLegacy(bool visible)
        {
            if (visible)
            {
                gameObject.SetActive(true);
                transform.SetAsLastSibling();
            }

            if (panelRoot != null)
            {
                panelRoot.SetActive(visible);
            }

            if (!visible)
            {
                gameObject.SetActive(false);
            }
        }
    }
}

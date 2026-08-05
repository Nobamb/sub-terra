using SubTerra.App.UI.Building;
using SubTerra.App.UI.Drone;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.UI.HUD
{
    /// <summary>
    /// 시설 건설·Digger-Bot 패널의 닫기/재오픈 토글만 담당한다.
    /// 레이아웃 수치 자체는 에디터 빌더가 Prefab/Scene에 적용한다.
    /// </summary>
    public sealed class HudPanelChromeController : MonoBehaviour
    {
        [SerializeField] private BuildingMenuView buildingMenuView;
        [SerializeField] private BuildingMenuBinder buildingMenuBinder;
        [SerializeField] private GameObject buildingMenuRoot;
        [SerializeField] private Button buildingCloseButton;
        [SerializeField] private Button buildingOpenButton;

        [SerializeField] private DroneDialoguePanelView diggerBotView;
        [SerializeField] private GameObject diggerBotRoot;
        [SerializeField] private Button diggerCloseButton;
        [SerializeField] private Button diggerOpenButton;

        [SerializeField] private bool buildingMenuOpen = true;
        [SerializeField] private bool diggerBotOpen = true;

        public bool IsBuildingMenuOpen => buildingMenuOpen;
        public bool IsDiggerBotOpen => diggerBotOpen;

        private void Awake()
        {
            WireButtons();
            ApplyBuildingMenuVisible(buildingMenuOpen, cancelSelection: false);
            ApplyDiggerBotVisible(diggerBotOpen);
        }

        private void OnEnable()
        {
            WireButtons();
        }

        private void OnDisable()
        {
            UnwireButtons();
        }

        /// <summary>시설 건설 패널을 닫는다. 진행 중 Preview는 취소한다.</summary>
        public void CloseBuildingMenu()
        {
            ApplyBuildingMenuVisible(false, cancelSelection: true);
        }

        /// <summary>우측 열기 버튼으로 시설 건설 패널을 다시 연다.</summary>
        public void OpenBuildingMenu()
        {
            ApplyBuildingMenuVisible(true, cancelSelection: false);
        }

        public void ToggleBuildingMenu()
        {
            if (buildingMenuOpen)
            {
                CloseBuildingMenu();
            }
            else
            {
                OpenBuildingMenu();
            }
        }

        /// <summary>Digger-Bot(대사+추천 통합) 패널을 닫는다.</summary>
        public void CloseDiggerBot()
        {
            ApplyDiggerBotVisible(false);
        }

        /// <summary>드론 버튼으로 Digger-Bot 패널을 다시 연다.</summary>
        public void OpenDiggerBot()
        {
            ApplyDiggerBotVisible(true);
        }

        public void ToggleDiggerBot()
        {
            ApplyDiggerBotVisible(!diggerBotOpen);
        }

        public bool HasRequiredReferences()
        {
            return buildingMenuRoot != null
                && buildingOpenButton != null
                && diggerBotRoot != null
                && diggerOpenButton != null;
        }

        private void WireButtons()
        {
            if (buildingCloseButton != null)
            {
                buildingCloseButton.onClick.RemoveListener(CloseBuildingMenu);
                buildingCloseButton.onClick.AddListener(CloseBuildingMenu);
            }

            if (buildingOpenButton != null)
            {
                buildingOpenButton.onClick.RemoveListener(OpenBuildingMenu);
                buildingOpenButton.onClick.AddListener(OpenBuildingMenu);
            }

            if (diggerCloseButton != null)
            {
                diggerCloseButton.onClick.RemoveListener(CloseDiggerBot);
                diggerCloseButton.onClick.AddListener(CloseDiggerBot);
            }

            if (diggerOpenButton != null)
            {
                diggerOpenButton.onClick.RemoveListener(OpenDiggerBot);
                diggerOpenButton.onClick.AddListener(OpenDiggerBot);
            }
        }

        private void UnwireButtons()
        {
            if (buildingCloseButton != null)
            {
                buildingCloseButton.onClick.RemoveListener(CloseBuildingMenu);
            }

            if (buildingOpenButton != null)
            {
                buildingOpenButton.onClick.RemoveListener(OpenBuildingMenu);
            }

            if (diggerCloseButton != null)
            {
                diggerCloseButton.onClick.RemoveListener(CloseDiggerBot);
            }

            if (diggerOpenButton != null)
            {
                diggerOpenButton.onClick.RemoveListener(OpenDiggerBot);
            }
        }

        private void ApplyBuildingMenuVisible(bool visible, bool cancelSelection)
        {
            buildingMenuOpen = visible;

            if (cancelSelection && !visible && buildingMenuBinder != null)
            {
                buildingMenuBinder.CancelSelection();
            }

            if (buildingMenuView != null)
            {
                buildingMenuView.SetVisible(visible);
            }
            else if (buildingMenuRoot != null)
            {
                buildingMenuRoot.SetActive(visible);
            }

            // 닫혀 있을 때만 우측 열기 버튼을 보여 겹침을 줄인다.
            if (buildingOpenButton != null)
            {
                buildingOpenButton.gameObject.SetActive(!visible);
            }
        }

        private void ApplyDiggerBotVisible(bool visible)
        {
            diggerBotOpen = visible;

            if (diggerBotView != null)
            {
                diggerBotView.SetVisible(visible);
            }
            else if (diggerBotRoot != null)
            {
                diggerBotRoot.SetActive(visible);
            }

            if (diggerOpenButton != null)
            {
                diggerOpenButton.gameObject.SetActive(!visible);
            }
        }
    }
}

using SubTerra.App.UI.Building;
using SubTerra.App.UI.Drone;
using SubTerra.App.UI.Inventory;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SubTerra.App.UI.HUD
{
    /// <summary>
    /// 시설 건설·Digger-Bot·게임 가이드·인벤토리 패널의 닫기/재오픈 토글과
    /// 가이드에 명시된 단축키(B/I)를 담당한다.
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

        [SerializeField] private GameGuidePanelView gameGuideView;
        [SerializeField] private GameObject gameGuideRoot;
        [SerializeField] private Button gameGuideCloseButton;
        [SerializeField] private Button gameGuideOpenButton;

        [SerializeField] private InventoryPanelView inventoryPanelView;
        [SerializeField] private GameObject inventoryPanelRoot;
        [SerializeField] private Button inventoryCloseButton;

        [SerializeField] private bool buildingMenuOpen = true;
        [SerializeField] private bool diggerBotOpen = true;
        [SerializeField] private bool gameGuideOpen;
        [SerializeField] private bool inventoryPanelOpen;

        public bool IsBuildingMenuOpen => buildingMenuOpen;
        public bool IsDiggerBotOpen => diggerBotOpen;
        public bool IsGameGuideOpen => gameGuideOpen;
        public bool IsInventoryPanelOpen => inventoryPanelOpen;

        private void Awake()
        {
            WireButtons();
            ApplyBuildingMenuVisible(buildingMenuOpen, cancelSelection: false);
            ApplyDiggerBotVisible(diggerBotOpen);
            ApplyGameGuideVisible(gameGuideOpen);
            ApplyInventoryPanelVisible(inventoryPanelOpen);
        }

        private void OnEnable()
        {
            WireButtons();
        }

        private void OnDisable()
        {
            UnwireButtons();
        }

        private void Update()
        {
            // 게임 가이드 조작법: B=시설 건설, I=화물/인벤토리
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.bKey.wasPressedThisFrame)
            {
                ToggleBuildingMenu();
            }

            if (keyboard.iKey.wasPressedThisFrame)
            {
                ToggleInventoryPanel();
            }
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

        /// <summary>게임 가이드 패널을 닫는다.</summary>
        public void CloseGameGuide()
        {
            ApplyGameGuideVisible(false);
        }

        /// <summary>우측 게임 가이드 버튼으로 가이드 패널을 연다.</summary>
        public void OpenGameGuide()
        {
            ApplyGameGuideVisible(true);
        }

        public void ToggleGameGuide()
        {
            ApplyGameGuideVisible(!gameGuideOpen);
        }

        /// <summary>화물/인벤토리 패널을 닫는다.</summary>
        public void CloseInventoryPanel()
        {
            ApplyInventoryPanelVisible(false);
        }

        /// <summary>I 키 등으로 화물/인벤토리 패널을 연다.</summary>
        public void OpenInventoryPanel()
        {
            ApplyInventoryPanelVisible(true);
        }

        public void ToggleInventoryPanel()
        {
            ApplyInventoryPanelVisible(!inventoryPanelOpen);
        }

        public bool HasRequiredReferences()
        {
            return buildingMenuRoot != null
                && buildingOpenButton != null
                && diggerBotRoot != null
                && diggerOpenButton != null
                && gameGuideRoot != null
                && gameGuideOpenButton != null;
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

            if (gameGuideCloseButton != null)
            {
                gameGuideCloseButton.onClick.RemoveListener(CloseGameGuide);
                gameGuideCloseButton.onClick.AddListener(CloseGameGuide);
            }

            if (gameGuideOpenButton != null)
            {
                gameGuideOpenButton.onClick.RemoveListener(OpenGameGuide);
                gameGuideOpenButton.onClick.AddListener(OpenGameGuide);
            }

            if (inventoryCloseButton != null)
            {
                inventoryCloseButton.onClick.RemoveListener(CloseInventoryPanel);
                inventoryCloseButton.onClick.AddListener(CloseInventoryPanel);
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

            if (gameGuideCloseButton != null)
            {
                gameGuideCloseButton.onClick.RemoveListener(CloseGameGuide);
            }

            if (gameGuideOpenButton != null)
            {
                gameGuideOpenButton.onClick.RemoveListener(OpenGameGuide);
            }

            if (inventoryCloseButton != null)
            {
                inventoryCloseButton.onClick.RemoveListener(CloseInventoryPanel);
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

        private void ApplyGameGuideVisible(bool visible)
        {
            gameGuideOpen = visible;

            if (gameGuideView != null)
            {
                // View가 root 활성/비활성과 최전면 정렬까지 처리한다.
                gameGuideView.SetVisible(visible);
            }
            else if (gameGuideRoot != null)
            {
                gameGuideRoot.SetActive(visible);
                if (visible)
                {
                    gameGuideRoot.transform.SetAsLastSibling();
                }
            }

            // 가이드 열기 버튼은 항상 우측에서 접근 가능하게 유지한다.
            if (gameGuideOpenButton != null)
            {
                gameGuideOpenButton.gameObject.SetActive(true);
            }
        }

        private void ApplyInventoryPanelVisible(bool visible)
        {
            inventoryPanelOpen = visible;

            if (inventoryPanelView != null)
            {
                inventoryPanelView.SetVisible(visible);
            }
            else if (inventoryPanelRoot != null)
            {
                inventoryPanelRoot.SetActive(visible);
            }

            // View가 panelRoot만 토글해도 루트 참조가 있으면 동기화한다.
            if (inventoryPanelRoot != null
                && inventoryPanelView != null
                && inventoryPanelRoot.activeSelf != visible)
            {
                inventoryPanelRoot.SetActive(visible);
            }

            if (visible && inventoryPanelRoot != null)
            {
                inventoryPanelRoot.transform.SetAsLastSibling();
            }
        }
    }
}

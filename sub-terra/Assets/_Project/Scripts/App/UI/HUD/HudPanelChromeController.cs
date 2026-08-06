using SubTerra.App.UI.Building;
using SubTerra.App.UI.Drone;
using SubTerra.App.UI.Inventory;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SubTerra.App.UI.HUD
{
    /// <summary>
    /// 시설 건설·Digger-Bot·게임 가이드·인벤토리 패널의 닫기/토글과
    /// 가이드에 명시된 단축키(B/I/G)를 담당한다.
    /// prompt-B 32: 우측 중앙 재열기 버튼 없이, 우측 상단 단축키·X 닫기만 사용한다.
    /// 레이아웃 수치 자체는 에디터 빌더가 Prefab/Scene에 적용한다.
    /// </summary>
    public sealed class HudPanelChromeController : MonoBehaviour
    {
        [SerializeField] private BuildingMenuView buildingMenuView;
        [SerializeField] private BuildingMenuBinder buildingMenuBinder;
        [SerializeField] private GameObject buildingMenuRoot;
        [SerializeField] private Button buildingCloseButton;
        // prompt-B 32: 우측 중앙 시설 재열기 버튼은 사용하지 않는다(레거시 호환 필드).
        [SerializeField] private Button buildingOpenButton;

        [SerializeField] private DroneDialoguePanelView diggerBotView;
        [SerializeField] private GameObject diggerBotRoot;
        [SerializeField] private Button diggerCloseButton;
        [SerializeField] private Button diggerOpenButton;

        [SerializeField] private GameGuidePanelView gameGuideView;
        [SerializeField] private GameObject gameGuideRoot;
        [SerializeField] private Button gameGuideCloseButton;
        // prompt-B 32: 우측 중앙 가이드 버튼 대신 우측 상단 [G] 단축키/버튼만 사용.
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
            // 게임 가이드 조작법: B=시설 건설, I=화물/인벤토리, G=게임 가이드
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

            if (keyboard.gKey.wasPressedThisFrame)
            {
                ToggleGameGuide();
            }
        }

        /// <summary>시설 건설 패널을 닫는다. 진행 중 Preview는 취소한다.</summary>
        public void CloseBuildingMenu()
        {
            ApplyBuildingMenuVisible(false, cancelSelection: true);
        }

        /// <summary>시설 건설 패널을 연다.</summary>
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

        /// <summary>우측 상단 게임 가이드(G) 버튼/단축키로 가이드 패널을 연다.</summary>
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

        /// <summary>I 키·화물(I) 버튼으로 화물/인벤토리 패널을 연다.</summary>
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
            // prompt-B 32: 우측 중앙 재열기 버튼은 필수가 아니다.
            return buildingMenuRoot != null
                && diggerBotRoot != null
                && diggerOpenButton != null
                && gameGuideRoot != null;
        }

        private void WireButtons()
        {
            if (buildingCloseButton != null)
            {
                buildingCloseButton.onClick.RemoveListener(CloseBuildingMenu);
                buildingCloseButton.onClick.AddListener(CloseBuildingMenu);
            }

            // 레거시 우측 중앙 시설 버튼이 남아 있으면 토글로 연결(빌더는 제거한다).
            if (buildingOpenButton != null)
            {
                buildingOpenButton.onClick.RemoveListener(ToggleBuildingMenu);
                buildingOpenButton.onClick.AddListener(ToggleBuildingMenu);
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
                gameGuideOpenButton.onClick.RemoveListener(ToggleGameGuide);
                gameGuideOpenButton.onClick.AddListener(ToggleGameGuide);
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
                buildingOpenButton.onClick.RemoveListener(ToggleBuildingMenu);
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
                gameGuideOpenButton.onClick.RemoveListener(ToggleGameGuide);
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

            // 패널 전체(루트)를 토글한다. PanelRoot만 끄면 X 버튼이 루트에 남아 상태가 꼬인다.
            if (buildingMenuRoot != null)
            {
                buildingMenuRoot.SetActive(visible);
            }

            if (buildingMenuView != null)
            {
                buildingMenuView.SetVisible(visible);
            }

            // prompt-B 32: 우측 중앙 재열기 버튼은 항상 숨긴다.
            if (buildingOpenButton != null)
            {
                buildingOpenButton.gameObject.SetActive(false);
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

            if (gameGuideRoot != null)
            {
                gameGuideRoot.SetActive(visible);
                if (visible)
                {
                    gameGuideRoot.transform.SetAsLastSibling();
                }
            }

            // 우측 중앙 레거시 가이드 버튼이 있으면 숨긴다(상단 [G]만 사용).
            if (gameGuideOpenButton != null)
            {
                gameGuideOpenButton.gameObject.SetActive(false);
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

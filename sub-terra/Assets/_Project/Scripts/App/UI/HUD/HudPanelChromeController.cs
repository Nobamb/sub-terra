using SubTerra.App.UI.Building;
using SubTerra.App.UI.Drone;
using SubTerra.App.UI.Inventory;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SubTerra.App.UI.HUD
{
    /// <summary>
    /// 시설 건설·Digger-Bot·게임 가이드·인벤토리 패널의 닫기/토글과
    /// 가이드에 명시된 단축키(B/I/G/Tab)를 담당한다.
    /// prompt-B 34: Tab 또는 드론 클릭으로 digger-bot 창 토글, X로 닫기.
    /// prompt-B 35-3: 시설 건설 중 Enter가 UI Submit으로 게임 가이드를 토글하지 않게 한다.
    /// EventSystem보다 먼저 Enter를 처리해 선택 해제가 Submit보다 앞서 적용되게 한다.
    /// </summary>
    [DefaultExecutionOrder(-200)]
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
        // 레거시 필드: 드론 재오픈 버튼은 사용하지 않는다(항상 숨김).
        [SerializeField] private Button diggerOpenButton;
        // Tab/드론 클릭 토글 대상(DiggerBot_Runtime).
        [SerializeField] private Transform diggerBotWorldTarget;
        [SerializeField, Min(0.2f)] private float diggerClickRadius = 1.25f;
        // DroneUiBinder가 붙은 합성 루트. 창을 닫아도 분석·말풍선이 계속 동작하도록 항상 활성 유지.
        [SerializeField] private GameObject diggerHostRoot;

        [SerializeField] private GameGuidePanelView gameGuideView;
        [SerializeField] private GameObject gameGuideRoot;
        [SerializeField] private Button gameGuideCloseButton;
        // prompt-B 32: 우측 중앙 가이드 버튼 대신 우측 상단 [G] 단축키/버튼만 사용.
        [SerializeField] private Button gameGuideOpenButton;

        [SerializeField] private InventoryPanelView inventoryPanelView;
        [SerializeField] private GameObject inventoryPanelRoot;
        [SerializeField] private Button inventoryCloseButton;
        // prompt-B 33-1: 우측 상단 인벤토리(I) 단축키 버튼(토글).
        [SerializeField] private Button inventoryOpenButton;

        [SerializeField] private bool buildingMenuOpen = true;
        // digger-bot 창은 시작 시 닫힌 상태. Tab/드론 클릭으로 연다.
        [SerializeField] private bool diggerBotOpen;
        [SerializeField] private bool gameGuideOpen;
        // prompt-B 33-1: 시작 시 인벤토리 창은 닫힌 상태.
        [SerializeField] private bool inventoryPanelOpen;

        public bool IsBuildingMenuOpen => buildingMenuOpen;
        public bool IsDiggerBotOpen => diggerBotOpen;
        public bool IsGameGuideOpen => gameGuideOpen;
        public bool IsInventoryPanelOpen => inventoryPanelOpen;

        private void Awake()
        {
            ResolveDiggerWorldTargetIfNeeded();
            ResolveInventoryPanelIfNeeded();
            EnsureDiggerHostActive();
            WireButtons();
            ConfigurePointerPreferredChromeButtons();
            ApplyBuildingMenuVisible(buildingMenuOpen, cancelSelection: false);
            ApplyDiggerBotVisible(diggerBotOpen);
            ApplyGameGuideVisible(gameGuideOpen);
            // prompt-B 36-1: 시작 시 인벤토리는 항상 닫힌 상태.
            inventoryPanelOpen = false;
            ApplyInventoryPanelVisible(false);
        }

        private void OnEnable()
        {
            ResolveDiggerWorldTargetIfNeeded();
            ResolveInventoryPanelIfNeeded();
            EnsureDiggerHostActive();
            WireButtons();
            ConfigurePointerPreferredChromeButtons();
        }

        private void Start()
        {
            // 다른 UI 초기화가 PanelRoot를 다시 켠 경우 한 번 더 닫아 둔다.
            ResolveInventoryPanelIfNeeded();
            if (!inventoryPanelOpen)
            {
                ApplyInventoryPanelVisible(false);
            }
        }

        private void OnDisable()
        {
            UnwireButtons();
        }

        private void Update()
        {
            // 게임 가이드 조작법: B=시설 건설, I=화물/인벤토리, G=게임 가이드, Tab=Digger-Bot
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                // prompt-B 35-3: 시설 건설 창이 열린 동안 Enter는 배치 전용.
                // EventSystem Submit이 선택된 단축키(게임 가이드 등)를 다시 누르지 않게
                // 매 프레임(EventSystem보다 먼저) 선택을 해제한다.
                if (buildingMenuOpen
                    && (keyboard.enterKey.wasPressedThisFrame
                        || keyboard.numpadEnterKey.wasPressedThisFrame))
                {
                    UiKeyboardSubmitGuard.ClearSelection();
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

                // prompt-B 34: Tab 키로 digger-bot 창 활성/비활성.
                if (keyboard.tabKey.wasPressedThisFrame)
                {
                    ToggleDiggerBot();
                }
            }

            TryToggleDiggerBotByWorldClick();
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
            // digger 재오픈 버튼은 더 이상 필수가 아니다(Tab/드론 클릭만 사용).
            return buildingMenuRoot != null
                && diggerBotRoot != null
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

            // 드론 재오픈 버튼은 제거 대상. 남아 있어도 숨기고 연결하지 않는다.
            if (diggerOpenButton != null)
            {
                diggerOpenButton.onClick.RemoveListener(OpenDiggerBot);
                diggerOpenButton.gameObject.SetActive(false);
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
                // Scene 영속 리스너와 런타임 리스너가 겹치면 토글이 두 번 호출되어
                // 창이 열렸다 바로 닫히는 것처럼 보인다. 단일 리스너만 남긴다.
                inventoryCloseButton.onClick.RemoveAllListeners();
                inventoryCloseButton.onClick.AddListener(CloseInventoryPanel);
            }

            if (inventoryOpenButton != null)
            {
                inventoryOpenButton.onClick.RemoveAllListeners();
                inventoryOpenButton.onClick.AddListener(ToggleInventoryPanel);
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

            if (inventoryOpenButton != null)
            {
                inventoryOpenButton.onClick.RemoveListener(ToggleInventoryPanel);
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

            // 창을 열거나 닫을 때 남아 있는 UI 선택을 비워 Enter Submit 잔여 효과를 끊는다.
            if (visible)
            {
                UiKeyboardSubmitGuard.ClearSelection();
                // 시설 목록 버튼도 키보드 Submit 대상에서 제외한다.
                if (buildingMenuRoot != null)
                {
                    UiKeyboardSubmitGuard.ConfigureButtonsUnder(buildingMenuRoot.transform);
                }
            }
        }

        /// <summary>
        /// 우측 상단 단축키·패널 닫기 버튼이 Enter Submit/키보드 네비로
        /// 의도치 않게 재실행되지 않도록 설정한다 (prompt-B 35-3).
        /// </summary>
        private void ConfigurePointerPreferredChromeButtons()
        {
            UiKeyboardSubmitGuard.ConfigurePointerPreferredButton(buildingCloseButton);
            UiKeyboardSubmitGuard.ConfigurePointerPreferredButton(buildingOpenButton);
            UiKeyboardSubmitGuard.ConfigurePointerPreferredButton(diggerCloseButton);
            UiKeyboardSubmitGuard.ConfigurePointerPreferredButton(diggerOpenButton);
            UiKeyboardSubmitGuard.ConfigurePointerPreferredButton(gameGuideCloseButton);
            UiKeyboardSubmitGuard.ConfigurePointerPreferredButton(gameGuideOpenButton);
            UiKeyboardSubmitGuard.ConfigurePointerPreferredButton(inventoryCloseButton);
            UiKeyboardSubmitGuard.ConfigurePointerPreferredButton(inventoryOpenButton);

            // PanelShortcutBar(시설/인벤/가이드 등)는 SerializeField가 아닐 수 있어 이름으로 찾는다.
            var shortcutBar = FindShortcutBarTransform();
            if (shortcutBar != null)
            {
                UiKeyboardSubmitGuard.ConfigureButtonsUnder(shortcutBar);
            }

            if (buildingMenuRoot != null)
            {
                UiKeyboardSubmitGuard.ConfigureButtonsUnder(buildingMenuRoot.transform);
            }
        }

        private Transform FindShortcutBarTransform()
        {
            // Canvas 자신 또는 자식에서 단축키 바를 찾는다.
            var self = transform.Find("PanelShortcutBar");
            if (self != null)
            {
                return self;
            }

            var nested = GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < nested.Length; i++)
            {
                if (nested[i] != null && nested[i].name == "PanelShortcutBar")
                {
                    return nested[i];
                }
            }

            return null;
        }

        private void ApplyDiggerBotVisible(bool visible)
        {
            diggerBotOpen = visible;

            // 분석/말풍선용 호스트는 절대 끄지 않는다.
            EnsureDiggerHostActive();

            // digger-bot 창 본체만 토글한다.
            if (diggerBotRoot != null)
            {
                diggerBotRoot.SetActive(visible);
                if (visible)
                {
                    diggerBotRoot.transform.SetAsLastSibling();
                    // 부모 레이아웃 안에서도 최상단으로 올려 가려지지 않게 한다.
                    if (diggerBotRoot.transform.parent != null)
                    {
                        diggerBotRoot.transform.parent.SetAsLastSibling();
                    }
                }
            }

            if (diggerBotView != null)
            {
                diggerBotView.SetVisible(visible);
            }

            // 드론 재오픈 버튼은 사용하지 않는다.
            if (diggerOpenButton != null)
            {
                diggerOpenButton.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// DiggerBotPanel(호스트)이 PanelToggle 등으로 꺼져 있으면
        /// 말풍선 분석 Binder까지 함께 죽어 복구한다.
        /// </summary>
        private void EnsureDiggerHostActive()
        {
            if (diggerHostRoot == null)
            {
                // 호스트 미지정 시 digger 창의 부모(보통 DiggerBotPanel)를 호스트로 본다.
                if (diggerBotRoot != null && diggerBotRoot.transform.parent != null)
                {
                    diggerHostRoot = diggerBotRoot.transform.parent.gameObject;
                }
            }

            if (diggerHostRoot != null && !diggerHostRoot.activeSelf)
            {
                diggerHostRoot.SetActive(true);
            }
        }

        /// <summary>
        /// UI 위를 클릭한 경우가 아니면, 월드 드론 근처 좌클릭으로 digger-bot 창을 토글한다.
        /// </summary>
        private void TryToggleDiggerBotByWorldClick()
        {
            var mouse = Mouse.current;
            if (mouse == null || !mouse.leftButton.wasPressedThisFrame)
            {
                return;
            }

            if (IsPointerOverUi())
            {
                return;
            }

            ResolveDiggerWorldTargetIfNeeded();
            if (diggerBotWorldTarget == null)
            {
                return;
            }

            var camera = Camera.main;
            if (camera == null)
            {
                return;
            }

            var screen = mouse.position.ReadValue();
            var world = camera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, 0f));
            world.z = diggerBotWorldTarget.position.z;
            var distance = Vector2.Distance(world, diggerBotWorldTarget.position);
            if (distance <= diggerClickRadius)
            {
                ToggleDiggerBot();
            }
        }

        private void ResolveDiggerWorldTargetIfNeeded()
        {
            if (diggerBotWorldTarget != null)
            {
                return;
            }

            var runtime = GameObject.Find("DiggerBot_Runtime");
            if (runtime != null)
            {
                diggerBotWorldTarget = runtime.transform;
            }
        }

        private static bool IsPointerOverUi()
        {
            if (EventSystem.current == null)
            {
                return false;
            }

            // 마우스/터치가 UI 위에 있으면 월드 클릭 토글을 하지 않는다.
            if (EventSystem.current.IsPointerOverGameObject())
            {
                return true;
            }

            var mouse = Mouse.current;
            if (mouse == null)
            {
                return false;
            }

            var eventData = new PointerEventData(EventSystem.current)
            {
                position = mouse.position.ReadValue()
            };
            var results = new System.Collections.Generic.List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);
            return results.Count > 0;
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
            ResolveInventoryPanelIfNeeded();

            if (inventoryPanelView != null)
            {
                // View가 있으면 Binder 구독 유지를 위해 루트는 켠 채 PanelRoot만 토글한다.
                if (inventoryPanelRoot != null && !inventoryPanelRoot.activeSelf)
                {
                    inventoryPanelRoot.SetActive(true);
                }

                inventoryPanelView.SetVisible(visible);
            }
            else if (inventoryPanelRoot != null)
            {
                // View 없는 테스트·레거시 배치는 루트 자체를 토글한다.
                inventoryPanelRoot.SetActive(visible);
            }

            if (visible && inventoryPanelRoot != null)
            {
                inventoryPanelRoot.transform.SetAsLastSibling();
            }
        }

        /// <summary>
        /// prompt-B 36-1: Scene Prefab 인스턴스 ID가 깨져 Inspector 참조가 비어도
        /// 배정된 루트(또는 씬 내 InventoryPanel)에서 패널·닫기 버튼을 다시 찾는다.
        /// 다른 테스트 오브젝트의 임의 View를 붙잡지 않도록 루트 우선으로 해석한다.
        /// </summary>
        private void ResolveInventoryPanelIfNeeded()
        {
            if (inventoryPanelView != null
                && inventoryPanelRoot != null
                && !IsViewUnderRoot(inventoryPanelView, inventoryPanelRoot))
            {
                // 루트와 무관한 View가 붙어 있으면 버리고 다시 찾는다.
                inventoryPanelView = null;
            }

            if (inventoryPanelView == null && inventoryPanelRoot != null)
            {
                inventoryPanelView = inventoryPanelRoot.GetComponent<InventoryPanelView>()
                    ?? inventoryPanelRoot.GetComponentInChildren<InventoryPanelView>(true);
            }

            if (inventoryPanelView == null && inventoryPanelRoot == null)
            {
                inventoryPanelView = FindFirstObjectByType<InventoryPanelView>(
                    FindObjectsInactive.Include);
            }

            if (inventoryPanelView != null && inventoryPanelRoot == null)
            {
                inventoryPanelRoot = inventoryPanelView.gameObject;
            }

            if (inventoryCloseButton == null)
            {
                inventoryCloseButton = ResolveInventoryCloseButton(inventoryPanelView, inventoryPanelRoot);
            }

            if (inventoryPanelRoot == null)
            {
                var binder = FindFirstObjectByType<InventoryPanelBinder>(
                    FindObjectsInactive.Include);
                if (binder != null)
                {
                    inventoryPanelRoot = binder.gameObject;
                    if (inventoryPanelView == null)
                    {
                        inventoryPanelView = binder.PanelView
                            ?? binder.GetComponent<InventoryPanelView>();
                    }

                    if (inventoryCloseButton == null)
                    {
                        inventoryCloseButton = ResolveInventoryCloseButton(
                            inventoryPanelView,
                            inventoryPanelRoot);
                    }
                }
            }
        }

        private static Button ResolveInventoryCloseButton(
            InventoryPanelView view,
            GameObject root)
        {
            if (view != null && view.CloseButton != null)
            {
                return view.CloseButton;
            }

            // Scene 인스턴스가 closeButton을 null로 덮어쓴 경우 이름 기반으로 복구한다.
            Transform searchRoot = null;
            if (view != null && view.PanelRoot != null)
            {
                searchRoot = view.PanelRoot.transform;
            }
            else if (view != null)
            {
                searchRoot = view.transform;
            }
            else if (root != null)
            {
                searchRoot = root.transform;
            }

            if (searchRoot == null)
            {
                return null;
            }

            var named = searchRoot.Find("CloseButton");
            if (named != null)
            {
                return named.GetComponent<Button>();
            }

            var buttons = searchRoot.GetComponentsInChildren<Button>(true);
            for (var i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] != null && buttons[i].gameObject.name == "CloseButton")
                {
                    return buttons[i];
                }
            }

            return null;
        }

        private static bool IsViewUnderRoot(InventoryPanelView view, GameObject root)
        {
            if (view == null || root == null)
            {
                return false;
            }

            return view.gameObject == root
                || view.transform.IsChildOf(root.transform);
        }
    }
}

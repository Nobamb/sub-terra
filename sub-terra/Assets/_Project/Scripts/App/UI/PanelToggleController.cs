using System;
using System.Collections.Generic;
using SubTerra.App.UI.Building;
using SubTerra.App.UI.Progression;
using SubTerra.App.UI.SurfaceBase;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SubTerra.App.UI
{
    /// <summary>
    /// 탐사 화면의 보조 패널을 한 곳에서 열고 닫는다.
    /// 입력은 표현 계층에만 머물고, 패널 내부의 게임 규칙은 기존 Binder/Presenter가 소유한다.
    /// </summary>
    public sealed class PanelToggleController : MonoBehaviour
    {
        [Serializable]
        private sealed class PanelReference
        {
            [SerializeField] private RuntimePanelId panelId;
            [SerializeField] private GameObject panelRoot;
            [SerializeField] private bool visibleOnStart;

            public RuntimePanelId PanelId => panelId;
            public GameObject PanelRoot => panelRoot;
            public bool VisibleOnStart => visibleOnStart;
        }

        [SerializeField] private PanelReference[] panels = Array.Empty<PanelReference>();
        [SerializeField] private BuildingMenuBinder buildingMenu;

        private readonly PanelVisibilityState state = new PanelVisibilityState();
        private readonly Dictionary<RuntimePanelId, GameObject> panelRoots =
            new Dictionary<RuntimePanelId, GameObject>();

        public event Action<RuntimePanelId, bool> VisibilityChanged;

        private void Awake()
        {
            panelRoots.Clear();
            for (var i = 0; i < panels.Length; i++)
            {
                var panel = panels[i];
                if (panel == null || panel.PanelRoot == null || panelRoots.ContainsKey(panel.PanelId))
                {
                    continue;
                }

                panelRoots.Add(panel.PanelId, panel.PanelRoot);
                SetVisible(panel.PanelId, panel.VisibleOnStart, false);
            }
        }

        private void Update()
        {
            if (UiPauseGate.IsHeld)
            {
                return;
            }

            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            // prompt-B 32: B/I/G는 HudPanelChromeController가 단일 소유한다.
            // 여기서 같이 토글하면 창이 열렸다 닫히거나 X·닫기 상태가 엇갈린다.
            // 업그레이드(U)만 이 컨트롤러가 담당한다.
            if (keyboard.uKey.wasPressedThisFrame)
            {
                Toggle(RuntimePanelId.Upgrade);
            }
        }

        public bool IsVisible(RuntimePanelId panelId)
        {
            return state.IsVisible(panelId);
        }

        public void Toggle(RuntimePanelId panelId)
        {
            if (!panelRoots.ContainsKey(panelId))
            {
                return;
            }

            SetVisible(panelId, state.Toggle(panelId));
        }

        public void SetVisible(RuntimePanelId panelId, bool visible)
        {
            SetVisible(panelId, visible, true);
        }

        private void SetVisible(RuntimePanelId panelId, bool visible, bool animate)
        {
            if (!panelRoots.TryGetValue(panelId, out var panelRoot) || panelRoot == null)
            {
                return;
            }

            if (!visible && panelId == RuntimePanelId.Building)
            {
                buildingMenu?.CancelSelection();
            }

            state.SetVisible(panelId, visible);
            if (panelId == RuntimePanelId.Upgrade)
            {
                SetUpgradeVisible(panelRoot, visible, animate);
            }
            else
            {
                panelRoot.SetActive(visible);
            }

            VisibilityChanged?.Invoke(panelId, visible);
        }

        /// <summary>
        /// B-167: 지상 기지와 같은 자물쇠 연출로 열고 닫는다. 닫기 요청 즉시 IsVisible은 false가 되고,
        /// 창 루트는 닫기 연출이 끝난 뒤 꺼진다. 닫는 중 다시 열면 지금 모양에서 이어서 연다.
        /// </summary>
        private static void SetUpgradeVisible(GameObject panelRoot, bool visible, bool animate)
        {
            var motion = panelRoot.GetComponent<UpgradeWindowMotion>();
            if (motion == null)
            {
                motion = panelRoot.AddComponent<UpgradeWindowMotion>();
            }

            var canvas = panelRoot.GetComponent<Canvas>();
            if (!visible)
            {
                if (!animate || !panelRoot.activeSelf)
                {
                    motion.CompleteImmediately(false);
                    panelRoot.SetActive(false);
                    return;
                }

                // 닫는 중인 창이 X키의 '맨 위 창'으로 잡혀 다음 창 닫기를 막지 않게 정렬 목록에서 먼저 뺀다(그리는 순서는 유지).
                PopupWindowSorting.Remove(canvas);
                motion.BeginClose(() =>
                {
                    if (panelRoot != null) panelRoot.SetActive(false);
                });
                return;
            }

            panelRoot.SetActive(true);
            // 업그레이드 창은 열릴 때 맨 앞 + 높은 sortingOrder로 올려 다른 HUD에 묻히지 않게 한다.
            panelRoot.transform.SetAsLastSibling();
            var progressionView = panelRoot.GetComponent<ProgressionPanelView>();
            if (progressionView == null) progressionView = panelRoot.GetComponentInChildren<ProgressionPanelView>(true);
            if (progressionView != null) progressionView.BringToFront();
            PopupWindowSorting.BringToFront(canvas);
            if (animate)
            {
                motion.BeginOpen();
            }
            else
            {
                motion.CompleteImmediately(true);
            }
        }

        public void ToggleBuilding() => Toggle(RuntimePanelId.Building);
        public void ToggleInventory() => Toggle(RuntimePanelId.Inventory);
        public void ToggleUpgrade() => Toggle(RuntimePanelId.Upgrade);
        public void ToggleGameGuide() => Toggle(RuntimePanelId.GameGuide);
        public void ToggleDiggerBot() => Toggle(RuntimePanelId.DiggerBot);

        public void CloseBuilding() => SetVisible(RuntimePanelId.Building, false);
        public void CloseInventory() => SetVisible(RuntimePanelId.Inventory, false);
        public void CloseUpgrade() => SetVisible(RuntimePanelId.Upgrade, false);
        public void CloseGameGuide() => SetVisible(RuntimePanelId.GameGuide, false);
        public void CloseDiggerBot() => SetVisible(RuntimePanelId.DiggerBot, false);
    }
}

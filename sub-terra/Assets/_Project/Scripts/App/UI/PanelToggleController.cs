using System;
using System.Collections.Generic;
using SubTerra.App.UI.Building;
using SubTerra.App.UI.Progression;
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
                SetVisible(panel.PanelId, panel.VisibleOnStart);
            }
        }

        private void Update()
        {
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
            if (!panelRoots.TryGetValue(panelId, out var panelRoot) || panelRoot == null)
            {
                return;
            }

            if (!visible && panelId == RuntimePanelId.Building)
            {
                buildingMenu?.CancelSelection();
            }

            state.SetVisible(panelId, visible);
            panelRoot.SetActive(visible);

            // 업그레이드 창은 열릴 때 맨 앞 + 높은 sortingOrder로 올려 다른 HUD에 묻히지 않게 한다.
            if (visible && panelId == RuntimePanelId.Upgrade)
            {
                panelRoot.transform.SetAsLastSibling();
                var progressionView = panelRoot.GetComponent<ProgressionPanelView>()
                    ?? panelRoot.GetComponentInChildren<ProgressionPanelView>(true);
                progressionView?.BringToFront();
            }

            VisibilityChanged?.Invoke(panelId, visible);
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

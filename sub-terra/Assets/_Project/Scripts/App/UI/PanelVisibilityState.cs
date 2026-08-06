using System.Collections.Generic;

namespace SubTerra.App.UI
{
    /// <summary>
    /// 화면 패널의 표시 상태만 소유한다. 실제 GameObject 활성화는
    /// <see cref="PanelToggleController"/>가 담당한다.
    /// </summary>
    public sealed class PanelVisibilityState
    {
        private readonly HashSet<RuntimePanelId> visiblePanels = new HashSet<RuntimePanelId>();

        public bool IsVisible(RuntimePanelId panelId)
        {
            return visiblePanels.Contains(panelId);
        }

        public bool SetVisible(RuntimePanelId panelId, bool visible)
        {
            return visible ? visiblePanels.Add(panelId) : visiblePanels.Remove(panelId);
        }

        public bool Toggle(RuntimePanelId panelId)
        {
            if (visiblePanels.Contains(panelId))
            {
                visiblePanels.Remove(panelId);
                return false;
            }

            visiblePanels.Add(panelId);
            return true;
        }
    }

    public enum RuntimePanelId
    {
        Building,
        Inventory,
        Upgrade,
        GameGuide,
        DiggerBot
    }
}

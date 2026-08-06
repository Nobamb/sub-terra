using NUnit.Framework;
using SubTerra.App.UI;

namespace SubTerra.App.Tests.UI
{
    public sealed class PanelVisibilityStateTests
    {
        [Test]
        public void Toggle_ChangesOnlyRequestedPanelVisibility()
        {
            var state = new PanelVisibilityState();
            state.SetVisible(RuntimePanelId.Building, true);

            var inventoryVisible = state.Toggle(RuntimePanelId.Inventory);

            Assert.That(inventoryVisible, Is.True);
            Assert.That(state.IsVisible(RuntimePanelId.Building), Is.True);
            Assert.That(state.IsVisible(RuntimePanelId.Inventory), Is.True);
        }

        [Test]
        public void SetVisibleFalse_HidesPanelIdempotently()
        {
            var state = new PanelVisibilityState();
            state.SetVisible(RuntimePanelId.Upgrade, true);

            state.SetVisible(RuntimePanelId.Upgrade, false);
            var changedAgain = state.SetVisible(RuntimePanelId.Upgrade, false);

            Assert.That(state.IsVisible(RuntimePanelId.Upgrade), Is.False);
            Assert.That(changedAgain, Is.False);
        }
    }
}

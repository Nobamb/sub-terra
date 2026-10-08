using System.Collections;
using NUnit.Framework;
using SubTerra.App.Economy;
using SubTerra.App.Inventory;
using SubTerra.App.State;
using SubTerra.App.UI.Economy;
using UnityEngine;
using UnityEngine.TestTools;

namespace SubTerra.App.Tests.PlayMode
{
    public sealed class EconomyPanelBinderLifecycleTests
    {
        [UnityTest]
        public IEnumerator BindBeforeAwake_PreservesPresenterAndReleasesInventorySubscription()
        {
            var root = new GameObject("Inactive economy lifecycle test");
            root.SetActive(false);
            var catalog = new InMemoryMineralCatalog();
            catalog.Register("mineral.copper", 1f, 10, "Copper");
            var state = GameState.CreateNew();
            var inventory = new InventoryService(catalog, 100f, state);
            var economy = new EconomyService(inventory, catalog, state);
            root.AddComponent<EconomyPanelView>();
            var binder = root.AddComponent<EconomyPanelBinder>();
            EconomyPanelPresenter original = null;
            try
            {
                binder.BindTo(economy, null, inventory, state);
                original = binder.Presenter;
                Assert.That(original.IsBound, Is.True);
                root.SetActive(true);
                yield return null;
                Assert.That(binder.Presenter, Is.SameAs(original), "Delayed Awake must preserve the bound presenter");
                Assert.That(binder.IsBound, Is.True);
                binder.Unbind();
                Assert.That(original.IsBound, Is.False);
                Object.Destroy(root);
                yield return null;
                Assert.DoesNotThrow(() => inventory.AddMineral("mineral.copper", 1));
                Assert.That(inventory.State.GetQuantity("mineral.copper"), Is.EqualTo(1));
            }
            finally
            {
                if (root != null) Object.Destroy(root);
                if (original != null) original.Unbind();
            }
        }
    }
}

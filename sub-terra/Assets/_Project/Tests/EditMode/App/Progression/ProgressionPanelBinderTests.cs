using System.Reflection;
using NUnit.Framework;
using SubTerra.App.Core.Data;
using SubTerra.App.Progression;
using SubTerra.App.UI.Progression;
using UnityEngine;

namespace SubTerra.App.Tests.Progression
{
    public sealed class ProgressionPanelBinderTests
    {
        [Test]
        public void FirstDeepZoneUnlock_ShowsPopup_WithoutShowingItAgainOnRebind()
        {
            var state = new UpgradeState();
            Assert.That(state.TryRestore(new[]
            {
                new UpgradeLevelState(DataIds.Upgrades.DroneScan, 2),
                new UpgradeLevelState(DataIds.Upgrades.GasResistance, 1)
            }), Is.True);
            var service = new ProgressionService(state, null, null);
            var root = new GameObject("ProgressionPanel");
            var view = root.AddComponent<ProgressionPanelView>();
            var binder = root.AddComponent<ProgressionPanelBinder>();
            var popup = new GameObject("DeepZoneUnlockPopup");
            popup.SetActive(false);
            SetPrivateField(view, "deepZoneUnlockPopupRoot", popup);

            binder.BindTo(service, () => 1);

            Assert.That(state.IsZoneUnlocked(DataIds.Zones.Deep), Is.True);
            Assert.That(popup.activeSelf, Is.True);

            popup.SetActive(false);
            binder.BindTo(service, () => 1);
            Assert.That(popup.activeSelf, Is.False);

            Object.DestroyImmediate(root);
            Object.DestroyImmediate(popup);
        }

        private static void SetPrivateField(object target, string name, object value)
        {
            var field = target.GetType().GetField(
                name,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, "Missing field: " + name);
            field.SetValue(target, value);
        }
    }
}

using System.Reflection;
using NUnit.Framework;
using SubTerra.App.Core.Data;
using SubTerra.App.Progression;
using SubTerra.App.UI.Progression;
using TMPro;
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
                new UpgradeLevelState(DataIds.Upgrades.DrillSpeed, 2),
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

            binder.BindTo(service, () => 12);

            Assert.That(state.IsZoneUnlocked(DataIds.Zones.Deep), Is.True);
            Assert.That(popup.activeSelf, Is.True);

            popup.SetActive(false);
            binder.BindTo(service, () => 12);
            Assert.That(popup.activeSelf, Is.False);

            Object.DestroyImmediate(root);
            Object.DestroyImmediate(popup);
        }

        [Test]
        public void DroneScanDetail_ShowsAbsoluteRadiusWithoutBonusDelta()
        {
            var root = new GameObject("ProgressionPanel");
            var detailRoot = new GameObject("DetailText");
            detailRoot.transform.SetParent(root.transform);
            var detail = detailRoot.AddComponent<TextMeshProUGUI>();
            var view = root.AddComponent<ProgressionPanelView>();
            SetPrivateField(view, "detailText", detail);

            view.SetSelectedUpgrade(new UpgradeSnapshot(
                DataIds.Upgrades.DroneScan,
                "드론 스캔 범위",
                0,
                2,
                0f,
                3f,
                null,
                true));

            Assert.That(detail.text, Does.Contain("현재 반경 0  →  다음 반경 3"));
            Assert.That(detail.text, Does.Not.Contain("(+3)"));
            Object.DestroyImmediate(root);
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

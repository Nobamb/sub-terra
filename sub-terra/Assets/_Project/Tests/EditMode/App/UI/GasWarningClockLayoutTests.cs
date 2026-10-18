using System.Reflection;
using NUnit.Framework;
using SubTerra.App.UI.HUD;
using UnityEditor;
using UnityEngine;

namespace SubTerra.App.Tests.UI
{
    public sealed class GasWarningClockLayoutTests
    {
        [TearDown]
        public void TearDown()
        {
            GasWarningClockClearance.ResetForTests();
        }

        [Test]
        public void Position_RestoresRestingPointWhenClockIsOff_AndKeepsXBelowClock()
        {
            var resting = new Vector2(4f, -16f);

            Assert.That(GasWarningClockLayout.Position(false, resting), Is.EqualTo(resting));

            var below = GasWarningClockLayout.Position(true, resting);
            Assert.That(below.x, Is.EqualTo(resting.x));
            Assert.That(below.y, Is.EqualTo(-98f));
            Assert.That(below.y, Is.LessThan(GasWarningClockLayout.ClockTopY - GasWarningClockLayout.ClockHeight));
        }

        [Test]
        public void Overlay_MovesWarningBelowClockWhileClockIsOn_ThenRestoresRestingPosition()
        {
            var host = new GameObject("GasClockLayoutHost", typeof(RectTransform));
            GameObject warning = null;
            try
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/_Project/Prefabs/UI/GasWarningPanel.prefab");
                Assert.That(prefab, Is.Not.Null);
                warning = Object.Instantiate(prefab);
                var rect = warning.GetComponent<RectTransform>();
                var view = warning.GetComponent<GasWarningPanelView>();
                Assert.That(view, Is.Not.Null);
                Assert.That(rect.anchoredPosition, Is.EqualTo(new Vector2(0f, -16f)));

                var overlay = MineResetClockOverlay.Create(host.transform);
                var clock = overlay.transform.Find("ClockRoot").gameObject;
                view.SyncClockClearance();
                Assert.That(view.RestingAnchoredPosition, Is.EqualTo(new Vector2(0f, -16f)));
                Assert.That(rect.anchoredPosition, Is.EqualTo(new Vector2(0f, -16f)));

                clock.SetActive(true);
                Assert.That(overlay.IsClockOnScreen, Is.True);
                InvokeLateUpdate(overlay);
                view.SyncClockClearance();
                Assert.That(rect.anchoredPosition, Is.EqualTo(new Vector2(0f, GasWarningClockLayout.BelowClockY)));

                clock.SetActive(false);
                Assert.That(overlay.IsClockOnScreen, Is.False);
                InvokeLateUpdate(overlay);
                view.SyncClockClearance();
                Assert.That(rect.anchoredPosition, Is.EqualTo(new Vector2(0f, -16f)));
            }
            finally
            {
                if (warning != null)
                {
                    Object.DestroyImmediate(warning);
                }

                if (host != null)
                {
                    Object.DestroyImmediate(host);
                }
            }
        }

        private static void InvokeLateUpdate(MineResetClockOverlay overlay)
        {
            var method = typeof(MineResetClockOverlay).GetMethod(
                "LateUpdate",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            method.Invoke(overlay, null);
        }
    }
}

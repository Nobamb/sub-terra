using System.Collections;
using System.Reflection;
using NUnit.Framework;
using SubTerra.App.Integration;
using SubTerra.Gameplay.Mining;
using UnityEngine;
using UnityEngine.TestTools;

namespace SubTerra.App.Tests.PlayMode.MineDemo
{
    public sealed class PromptB79MiningFailureHudPlayModeTests
    {
        [Test]
        public void FailureBubble_DefaultDuration_IsTenSeconds()
        {
            var hudObject = new GameObject("MiningProgressHud");
            var hud = hudObject.AddComponent<MiningProgressHud>();

            Assert.That(GetPrivate<float>(hud, "failureDisplayDuration"), Is.EqualTo(10f));

            Object.DestroyImmediate(hudObject);
        }

        [UnityTest]
        public IEnumerator FailureBubble_HidesAfterConfiguredRealTime()
        {
            var systemObject = new GameObject("MiningSystem");
            var system = systemObject.AddComponent<MiningSystem>();
            var hudObject = new GameObject("MiningProgressHud");
            var statusRoot = new GameObject("StatusRoot");
            statusRoot.transform.SetParent(hudObject.transform, false);
            var hud = hudObject.AddComponent<MiningProgressHud>();
            SetPrivate(hud, "statusRoot", statusRoot);
            SetPrivate(hud, "failureDisplayDuration", 0.05f);
            hud.BindTo(system);

            Assert.That(system.TryStartMining(Vector3Int.zero), Is.False);
            Assert.That(statusRoot.activeSelf, Is.True);

            yield return new WaitForSecondsRealtime(0.08f);

            Assert.That(statusRoot.activeSelf, Is.False);
            Object.Destroy(systemObject);
            Object.Destroy(hudObject);
        }

        private static void SetPrivate(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            field.SetValue(target, value);
        }

        private static T GetPrivate<T>(object target, string fieldName)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            return (T)field.GetValue(target);
        }
    }
}

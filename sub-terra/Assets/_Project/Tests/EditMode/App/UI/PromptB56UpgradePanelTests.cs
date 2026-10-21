using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SubTerra.App.Core.Data;
using SubTerra.App.Editor.DataValidation;
using SubTerra.App.Progression;
using SubTerra.App.UI.Progression;
using SubTerra.Shared;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SubTerra.App.Tests.UI
{
    public sealed class PromptB56UpgradePanelTests
    {
        [Test]
        public void UpgradePanel_DoesNotContainSharedEquipmentUpgradeTitle()
        {
            var scene = SceneManager.GetSceneByPath(
                PromptB56UpgradePanelBuilder.IntegrationScenePath);
            var openedHere = !scene.IsValid() || !scene.isLoaded;
            if (openedHere)
            {
                scene = EditorSceneManager.OpenScene(
                    PromptB56UpgradePanelBuilder.IntegrationScenePath,
                    OpenSceneMode.Additive);
            }

            try
            {
                var labels = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<ProgressionPanelView>(true))
                    .SelectMany(view => view.GetComponentsInChildren<TMP_Text>(true));

                Assert.That(
                    labels.Any(label => label.text == "장비 업그레이드 [U]"),
                    Is.False);
            }
            finally
            {
                if (openedHere)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        [Test]
        public void Issue223_DeepZoneTabMapsLegacyUnlockToLevelOneAndKeepsFutureLevelsLocked()
        {
            var root = new GameObject("ProgressionPanel");
            var detailObject = new GameObject("UpgradeDetail");
            detailObject.transform.SetParent(root.transform);
            var detail = detailObject.AddComponent<TextMeshProUGUI>();
            var view = root.AddComponent<ProgressionPanelView>();
            SetPrivateField(view, "detailText", detail);

            try
            {
                view.SetActiveCategory(UpgradeCategory.DeepZone);
                view.SetDeepZoneAccess(new ZoneAccessResult(true, false, string.Empty));

                Assert.That(detail.text, Does.Contain("심층 구역 Lv.1/3 · 36~40M"));
                Assert.That(detail.text, Does.Contain("Lv.2 · 41~75M · 예정 (Demo 미지원)"));
                Assert.That(detail.text, Does.Contain("Lv.3 · 76~100M · 예정 (Demo 미지원)"));
                Assert.That(detail.text, Does.Not.Contain("모든 구역 해금"));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void DrillSpeed_FromLevelZero_ShowsRareResourceAndIronDescriptions()
        {
            var root = new GameObject("ProgressionPanel");
            var detailObject = new GameObject("UpgradeDetail");
            detailObject.transform.SetParent(root.transform);
            var detail = detailObject.AddComponent<TextMeshProUGUI>();
            var view = root.AddComponent<ProgressionPanelView>();
            SetPrivateField(view, "detailText", detail);

            try
            {
                view.SetSelectedUpgrade(CreateDrillSpeedSnapshot(0));

                Assert.That(detail.text, Does.Contain("레벨이 오를수록 더 희귀한 자원을 채취"));
                Assert.That(detail.text, Does.Contain("Lv.1 업그레이드 시 철을 채취"));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void DrillSpeed_AfterLevelZero_HidesIronDescription(int currentLevel)
        {
            var root = new GameObject("ProgressionPanel");
            var detailObject = new GameObject("UpgradeDetail");
            detailObject.transform.SetParent(root.transform);
            var detail = detailObject.AddComponent<TextMeshProUGUI>();
            var view = root.AddComponent<ProgressionPanelView>();
            SetPrivateField(view, "detailText", detail);

            try
            {
                view.SetSelectedUpgrade(CreateDrillSpeedSnapshot(currentLevel));

                Assert.That(detail.text, Does.Contain("레벨이 오를수록 더 희귀한 자원을 채취"));
                Assert.That(detail.text, Does.Not.Contain("Lv.1 업그레이드 시 철을 채취"));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static UpgradeSnapshot CreateDrillSpeedSnapshot(int currentLevel)
        {
            return new UpgradeSnapshot(
                DataIds.Upgrades.DrillSpeed,
                "드릴 속도",
                currentLevel,
                3,
                currentLevel * 0.1f,
                (currentLevel + 1) * 0.1f,
                new[] { new ItemCostDto(DataIds.Minerals.Copper, 1) },
                true);
        }

        [Test]
        public void Issue223_DisplayDepthAndMaximumComeFromSettings()
        {
            var settings = ScriptableObject.CreateInstance<DeepZoneDisplaySettings>();
            try
            {
                settings.EditorSet(new[]
                {
                    new DeepZoneDisplayLevel(10, 20, true, string.Empty),
                    new DeepZoneDisplayLevel(21, 30, false, "추가 조건")
                });
                var unlocked = settings.BuildText(new ZoneAccessResult(true, false, string.Empty), false);
                Assert.That(unlocked, Does.Contain("Lv.1/2 · 10~20M"));
                Assert.That(unlocked, Does.Contain("Lv.2 · 21~30M · 예정"));
                var locked = settings.BuildText(new ZoneAccessResult(false, false, "조건 부족"), false);
                Assert.That(locked, Does.Contain("Lv.0/2 · 잠금"));
                Assert.That(locked, Does.Contain("조건 부족"));
            }
            finally { Object.DestroyImmediate(settings); }
        }

        [Test]
        public void Issue223_SurfaceTitleUsesUpgradeWithoutChangingLayout()
        {
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/UI/SurfaceBasePanel.prefab");
            var instance = Object.Instantiate(prefab);
            try
            {
                var view = instance.GetComponentInChildren<ProgressionPanelView>(true);
                var title = instance.GetComponentsInChildren<TMP_Text>(true).First(t => t.name == "UpgradeTitle");
                var size = title.rectTransform.sizeDelta;
                view.ApplyWindowTitle();
                Assert.That(title.text, Is.EqualTo("업그레이드"));
                Assert.That(title.rectTransform.sizeDelta, Is.EqualTo(size));
            }
            finally { Object.DestroyImmediate(instance); }
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, fieldName);
            field.SetValue(target, value);
        }
    }
}

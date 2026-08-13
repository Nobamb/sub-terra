using System.IO;
using NUnit.Framework;
using SubTerra.App.Editor.DataValidation;
using SubTerra.Gameplay.Hazards;
using UnityEditor;
using UnityEngine;

namespace SubTerra.App.Tests.Integration
{
    public sealed class PromptB50GasVisionTests
    {
        [Test]
        public void PromptB50_GasZonePrefab_HasSpawnVisualAndFiveBlockRadius()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                PromptB50GasVisionBuilder.GasZonePrefabPath);
            Assert.That(prefab, Is.Not.Null);
            Assert.That(prefab.GetComponent<GasZone>(), Is.Not.Null);
            Assert.That(prefab.GetComponent<GasZoneVisual>(), Is.Not.Null);
            Assert.That(prefab.transform.Find(PromptB50GasVisionBuilder.GasCloudName), Is.Not.Null);

            var trigger = prefab.GetComponent<CircleCollider2D>();
            Assert.That(trigger, Is.Not.Null);
            Assert.That(trigger.isTrigger, Is.True);
            Assert.That(trigger.radius, Is.EqualTo(GasVisualRules.GasRadiusBlocks).Within(0.001f));
        }

        [Test]
        public void PromptB50_LightPrefab_HasFivePercentRedClearance()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                PromptB50GasVisionBuilder.LightPrefabPath);
            Assert.That(prefab, Is.Not.Null);

            var clearance = prefab.transform.Find(
                PromptB50GasVisionBuilder.PoweredVisualRootName + "/"
                + PromptB50GasVisionBuilder.LightClearanceName);
            Assert.That(clearance, Is.Not.Null);

            var source = clearance.GetComponent<GasVisionClearanceSource>();
            Assert.That(source, Is.Not.Null);
            Assert.That(source.Radius, Is.EqualTo(GasVisualRules.LightClearRadiusBlocks).Within(0.001f));

            var renderer = clearance.GetComponent<SpriteRenderer>();
            Assert.That(renderer, Is.Not.Null);
            Assert.That(renderer.color.r, Is.GreaterThan(0.8f));
            Assert.That(renderer.color.a, Is.EqualTo(GasVisualRules.LightClearRedOpacity).Within(0.001f));
            Assert.That(clearance.GetComponent<SpriteMask>(), Is.Not.Null);
        }

        [Test]
        public void PromptB50_IntegrationScene_WiresPrefabRadiusAndDarkOverlay()
        {
            var path = Path.Combine(
                Application.dataPath,
                "_Project",
                "Scenes",
                "App",
                "Mine_Demo_Integration.unity");
            var scene = File.ReadAllText(path);

            Assert.That(scene, Does.Contain("defaultRadius: 5"));
            Assert.That(scene, Does.Contain("guid: 2fb6cdf936002d64a91946e92ab9b5b9"));
            Assert.That(scene, Does.Contain("m_Name: GasVisionOverlay"));
            Assert.That(scene, Does.Contain("initialVisionObscuration: 0.35"));
            Assert.That(scene, Does.Contain("maximumVisionObscuration: 0.95"));
            Assert.That(scene, Does.Contain("approachFadeSeconds: 1"));
            Assert.That(scene, Does.Contain("m_Name: GasWorldVeil"));
        }
    }
}

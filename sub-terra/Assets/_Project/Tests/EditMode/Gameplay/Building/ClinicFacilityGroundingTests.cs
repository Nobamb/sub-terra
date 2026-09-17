using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace SubTerra.Gameplay.Building.Tests
{
    public sealed class ClinicFacilityGroundingTests
    {
        private const string ClinicPrefabPath = "Assets/_Project/Prefabs/Gameplay/Power/ClinicFacility.prefab";

        [Test]
        public void ClinicArtwork_OverlapsFirstRockRowWithoutChangingItsShape()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ClinicPrefabPath);
            Assert.That(prefab, Is.Not.Null);

            Transform visualRoot = prefab.transform.Find("VisualRoot");
            Transform poweredVisual = prefab.transform.Find("PoweredVisualRoot");

            Assert.That(visualRoot, Is.Not.Null);
            Assert.That(poweredVisual, Is.Not.Null);
            SpriteRenderer[] renderers = visualRoot.GetComponentsInChildren<SpriteRenderer>(true);
            SpriteRenderer artwork = System.Array.Find(
                renderers,
                renderer => renderer != null && renderer.enabled && renderer.sprite != null);
            Assert.That(artwork, Is.Not.Null);
            Assert.That(artwork.bounds.size.x, Is.EqualTo(1.8f).Within(0.02f));
            Assert.That(artwork.bounds.min.y, Is.EqualTo(-1.2f).Within(0.015f));
            Assert.That(poweredVisual.localPosition.y, Is.EqualTo(artwork.bounds.center.y).Within(0.015f));
        }
    }
}

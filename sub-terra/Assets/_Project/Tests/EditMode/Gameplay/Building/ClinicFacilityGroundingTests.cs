using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace SubTerra.Gameplay.Building.Tests
{
    public sealed class ClinicFacilityGroundingTests
    {
        private const string ClinicPrefabPath = "Assets/_Project/Prefabs/Gameplay/Power/ClinicFacility.prefab";

        [Test]
        public void ClinicArtwork_MatchesSharedFacilityGroundContact()
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
            Bounds opaque = FacilityGroundedVisual.GetVisibleBounds(artwork.sprite);
            float visibleWidth = artwork.transform.TransformPoint(opaque.max).x - artwork.transform.TransformPoint(opaque.min).x;
            Assert.That(visibleWidth, Is.EqualTo(1.8f).Within(0.02f));
            float expectedContact = -1f + FacilityFoundationVisual.ArtworkContactHeight;
            Assert.That(artwork.transform.TransformPoint(opaque.min).y, Is.EqualTo(expectedContact).Within(0.015f));
            Assert.That(FacilityGroundedVisual.TryGetGeometry(artwork.sprite, "building.clinic.basic",
                new Vector2Int(2, 2), out Vector3 offset, out Vector3 scale), Is.True);
            Assert.That(offset.y + opaque.min.y * scale.y, Is.EqualTo(expectedContact).Within(0.015f));
            Assert.That(poweredVisual.localPosition.y, Is.EqualTo(artwork.bounds.center.y).Within(0.015f));
        }
    }
}

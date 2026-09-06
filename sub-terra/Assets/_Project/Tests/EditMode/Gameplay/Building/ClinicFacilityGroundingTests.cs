using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace SubTerra.Gameplay.Building.Tests
{
    public sealed class ClinicFacilityGroundingTests
    {
        private const string ClinicPrefabPath = "Assets/_Project/Prefabs/Gameplay/Power/ClinicFacility.prefab";

        [Test]
        public void ClinicVisuals_ShareTheGroundingOffset()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ClinicPrefabPath);
            Assert.That(prefab, Is.Not.Null);

            Transform socketVisual = prefab.transform.Find("VisualRoot/FacilitySocketVisual");
            Transform poweredVisual = prefab.transform.Find("PoweredVisualRoot");

            Assert.That(socketVisual, Is.Not.Null);
            Assert.That(poweredVisual, Is.Not.Null);
            Assert.That(socketVisual.localPosition.y, Is.EqualTo(0.23f).Within(0.001f));
            Assert.That(poweredVisual.localPosition.y, Is.EqualTo(0.23f).Within(0.001f));
            Assert.That(prefab.transform.Find("VisualRoot/FacilitySocketVisual/FacilityAlcove"), Is.Null);
            Assert.That(
                prefab.transform.Find("VisualRoot/FacilitySocketVisual/MVP_Grounding/FacilitySocket/FoundationTile").gameObject.activeSelf,
                Is.False,
                "시설 타일 레이어가 바닥 표현을 맡으므로 본체의 큰 기초판은 표시하지 않는다.");
        }
    }
}

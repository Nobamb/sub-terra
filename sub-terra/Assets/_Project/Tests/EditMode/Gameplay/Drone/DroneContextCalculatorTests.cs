using NUnit.Framework;
using UnityEngine;

namespace SubTerra.Gameplay.Drone.Tests
{
    public sealed class DroneContextCalculatorTests
    {
        [Test]
        public void CalculateDepth_ClampsPositionsAboveSurfaceToZero()
        {
            Assert.That(DroneContextCalculator.CalculateDepth(0f, 3f), Is.EqualTo(0));
            Assert.That(DroneContextCalculator.CalculateDepth(0f, -4f), Is.EqualTo(4));
        }

        [Test]
        public void FindNearestDistance_ReturnsClosestActiveBase()
        {
            GameObject first = new("First"); first.transform.position = new Vector3(5f, 0f, 0f);
            GameObject second = new("Second"); second.transform.position = new Vector3(2f, 0f, 0f);

            float distance = DroneContextCalculator.FindNearestDistance(Vector2.zero, new[] { first.transform, second.transform });

            Assert.That(distance, Is.EqualTo(2f));
            Object.DestroyImmediate(first); Object.DestroyImmediate(second);
        }
    }
}

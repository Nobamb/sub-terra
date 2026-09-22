using System.Reflection;
using NUnit.Framework;
using SubTerra.Gameplay.Drone;
using UnityEngine;

namespace SubTerra.Gameplay.Tests.Drone
{
    public sealed class DroneFollowerFacingTests
    {
        [Test]
        public void FollowTarget_FlipsSpriteTowardHorizontalDestination_AndKeepsLastFacingWhenStationary()
        {
            var drone = new GameObject("Drone");
            var target = new GameObject("Target");
            try
            {
                var renderer = drone.AddComponent<SpriteRenderer>();
                var follower = drone.AddComponent<DroneFollower>();
                follower.SetTarget(target.transform);

                target.transform.position = new Vector3(4f, 0f, 0f);
                InvokeLateUpdate(follower);
                Assert.That(renderer.flipX, Is.False, "오른쪽 목표를 향할 때 원본 스프라이트 방향을 유지해야 합니다.");

                target.transform.position = new Vector3(-4f, 0f, 0f);
                InvokeLateUpdate(follower);
                Assert.That(renderer.flipX, Is.True, "왼쪽 목표를 향할 때 스프라이트가 반전되어야 합니다.");

                target.transform.position = drone.transform.position - new Vector3(-1.2f, 1f, 0f);
                InvokeLateUpdate(follower);
                Assert.That(renderer.flipX, Is.True, "수평 이동 목표가 없으면 마지막 방향을 유지해야 합니다.");
            }
            finally
            {
                Object.DestroyImmediate(drone);
                Object.DestroyImmediate(target);
            }
        }

        private static void InvokeLateUpdate(DroneFollower follower)
        {
            typeof(DroneFollower)
                .GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.Invoke(follower, null);
        }
    }
}

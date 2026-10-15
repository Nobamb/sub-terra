using System.Reflection;
using NUnit.Framework;
using SubTerra.Gameplay.Drone;
using UnityEditor;
using UnityEngine;

namespace SubTerra.Gameplay.Tests.Drone
{
    public sealed class DroneFollowerFacingTests
    {
        [Test]
        public void ClosePlayerAndLeftJump_DoNotSnapDroneAcrossPlayer()
        {
            var drone = new GameObject("NearDrone");
            var target = new GameObject("JumpingPlayer");
            try
            {
                drone.AddComponent<SpriteRenderer>();
                var follower = drone.AddComponent<DroneFollower>();
                follower.SetTarget(target.transform);
                drone.transform.position = new Vector3(0.1f, 0f, -2f);
                follower.Tick(1f / 60f);
                Assert.That(drone.transform.position.x, Is.GreaterThan(0f), "접근 시 반대편 고정 오프셋으로 순간 이동하지 않음");
                for (int i = 0; i < 30; i++)
                {
                    target.transform.position += new Vector3(-0.03f, 0.02f, 0f);
                    Vector3 before = drone.transform.position;
                    follower.Tick(1f / 60f);
                    Assert.That(Vector3.Distance(before, drone.transform.position), Is.LessThan(0.2f));
                    Assert.That(drone.transform.position.z, Is.EqualTo(-2f));
                }
                Vector3 paused = drone.transform.position;
                follower.Tick(0f);
                Assert.That(drone.transform.position, Is.EqualTo(paused));
            }
            finally
            {
                Object.DestroyImmediate(drone);
                Object.DestroyImmediate(target);
            }
        }

        private const string DronePrefabPath = "Assets/_Project/Prefabs/Gameplay/Drone/DiggerBot_Runtime.prefab";
        private const string RightFacingSpritePath = "Assets/_Project/Art/Characters/Drone/digger_bot_side_right.png";

        [Test]
        public void RuntimePrefab_AssignsTheRightFacingSprite()
        {
            var rightFacingSprite = AssetDatabase.LoadAssetAtPath<Sprite>(RightFacingSpritePath);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DronePrefabPath);

            Assert.That(rightFacingSprite, Is.Not.Null, "우측 방향 드론 스프라이트가 있어야 합니다.");
            Assert.That(prefab, Is.Not.Null, "런타임 드론 Prefab이 있어야 합니다.");

            var follower = prefab.GetComponent<DroneFollower>();
            Assert.That(follower, Is.Not.Null, "런타임 Prefab에 DroneFollower가 있어야 합니다.");
            Assert.That(
                GetPrivateField<Sprite>(follower, "rightFacingSprite"),
                Is.SameAs(rightFacingSprite),
                "런타임 Prefab이 우측 방향 스프라이트를 참조해야 합니다.");
        }

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
                var texture = new Texture2D(1, 1);
                var rightFacingSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
                SetPrivateField(follower, "rightFacingSprite", rightFacingSprite);

                target.transform.position = new Vector3(4f, 0f, 0f);
                InvokeLateUpdate(follower);
                Assert.That(renderer.sprite, Is.SameAs(rightFacingSprite), "방향 전용 스프라이트를 사용해야 합니다.");
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

        private static void SetPrivateField<T>(DroneFollower follower, string name, T value)
        {
            typeof(DroneFollower)
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(follower, value);
        }

        private static T GetPrivateField<T>(DroneFollower follower, string name) where T : Object
        {
            return typeof(DroneFollower)
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
                ?.GetValue(follower) as T;
        }
    }
}

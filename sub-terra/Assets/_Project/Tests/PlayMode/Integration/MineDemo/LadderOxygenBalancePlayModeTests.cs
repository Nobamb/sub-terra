#if UNITY_EDITOR
using System.Collections;
using System.Globalization;
using NUnit.Framework;
using SubTerra.Gameplay.Player;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace SubTerra.App.Tests.PlayMode
{
    public sealed class LadderOxygenBalancePlayModeTests
    {
        private const string PlayerPath = "Assets/_Project/Prefabs/Gameplay/Player/Player.prefab";
        private const string SettingsPath = "Assets/_Project/Data/Player/PlayerSurvivalSettings.asset";
        private const float SurfaceY = 21f;

        [UnityTest]
        public IEnumerator Issue334_LadderAndJumpToSameLandingWithEmptyAndFullCargo()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPath);
            var settings = AssetDatabase.LoadAssetAtPath<PlayerSurvivalSettings>(SettingsPath);
            Assert.That(prefab, Is.Not.Null);
            Assert.That(settings, Is.Not.Null);
            var previousCaptureDelta = Time.captureDeltaTime;
            Time.captureDeltaTime = Time.fixedDeltaTime;
            try
            {
                foreach (float weight in new[] { 0f, 100f })
                {
                    var ladder = new Ascent();
                    var jump = new Ascent();
                    yield return Measure(prefab, settings, weight, true, ladder);
                    yield return null;
                    yield return Measure(prefab, settings, weight, false, jump);
                    yield return null;
                    Debug.Log(string.Format(CultureInfo.InvariantCulture,
                        "[Issue334] load={0}% ladder={1:F3}s/{2:F3}O2 jump={3:F3}s/{4:F3}O2",
                        weight, ladder.Seconds, ladder.Oxygen, jump.Seconds, jump.Oxygen));
                    // One physics tick of oxygen at these depths is about 0.04 units.
                    Assert.That(ladder.Oxygen, Is.LessThanOrEqualTo(jump.Oxygen + 0.05f),
                        $"Ladder costs more oxygen at {weight}% cargo.");
                }
            }
            finally
            {
                Time.captureDeltaTime = previousCaptureDelta;
            }
        }

        private static IEnumerator Measure(GameObject prefab, PlayerSurvivalSettings settings, float weight,
            bool useLadder, Ascent result)
        {
            var course = new GameObject(useLadder ? "Ladder course" : "Jump course");
            try
            {
                // Both runs start on the same floor and end grounded on the same one-way shelf.
                var floor = new GameObject("Start floor");
                floor.transform.SetParent(course.transform);
                floor.transform.position = new Vector3(0f, -0.1f);
                floor.AddComponent<BoxCollider2D>().size = new Vector2(2f, 0.2f);
                var shelf = new GameObject("Landing shelf");
                shelf.transform.SetParent(course.transform);
                shelf.transform.position = new Vector3(0f, 0.95f);
                var shelfCollider = shelf.AddComponent<BoxCollider2D>();
                shelfCollider.size = new Vector2(2f, 0.1f);
                shelf.AddComponent<PlatformEffector2D>().useOneWay = true;
                shelfCollider.usedByEffector = true;

                if (useLadder)
                {
                    var ladder = new GameObject("Ladder");
                    ladder.transform.SetParent(course.transform);
                    ladder.transform.position = new Vector3(0f, 0.9f);
                    var trigger = ladder.AddComponent<BoxCollider2D>();
                    trigger.size = new Vector2(0.6f, 2.6f);
                    trigger.isTrigger = true;
                    ladder.AddComponent<LadderZone>();
                }

                var player = Object.Instantiate(prefab, new Vector3(0f, 0.34f), Quaternion.identity, course.transform);
                var movement = player.GetComponent<PlayerMovement>();
                player.GetComponent<PlayerController>().enabled = false;
                var body = player.GetComponent<Rigidbody2D>();
                var playerCollider = player.GetComponent<Collider2D>();
                var survivalHost = new GameObject("Survival");
                survivalHost.transform.SetParent(course.transform);
                survivalHost.SetActive(false);
                var survival = survivalHost.AddComponent<PlayerSurvivalController>();
                var serialized = new SerializedObject(survival);
                serialized.FindProperty("settings").objectReferenceValue = settings;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                survivalHost.SetActive(true);
                survival.BindTarget(player.transform);
                survival.BindMovement(movement);
                survival.BindOxygenDepth(() => SurfaceY - body.position.y);
                survival.SetOxygenCargoLoadRatio(CargoLoadEffectPolicy.EvaluateLoadRatio(weight, 100f));
                movement.SetCargoSpeedMultiplier(CargoSpeedPolicy.Evaluate(weight, 100f));
                movement.SetCargoJumpMultiplier(CargoLoadEffectPolicy.EvaluateJumpMultiplier(weight, 100f));
                Physics2D.SyncTransforms();

                for (int i = 0; i < 90 && !(useLadder ? movement.IsClimbing : movement.IsGrounded); i++)
                    yield return null;
                Assert.That(useLadder ? movement.IsClimbing : movement.IsGrounded, Is.True, "Course entry.");
                survival.RefillOxygen();
                var startOxygen = survival.OxygenRemaining;
                var startTime = Time.timeAsDouble;
                if (useLadder) movement.SetVerticalMoveInput(1f);
                else movement.RequestJump();

                for (int i = 0; i < 150; i++)
                {
                    yield return null;
                    if (useLadder && movement.IsClimbing && playerCollider.bounds.min.y >= 1.04f)
                        movement.ExitLadder();
                    if (!movement.IsGrounded || movement.IsClimbing || body.position.y < 1.3f
                        || Mathf.Abs(body.linearVelocityY) > 0.05f) continue;
                    result.Seconds = Time.timeAsDouble - startTime;
                    result.Oxygen = startOxygen - survival.OxygenRemaining;
                    Assert.That(result.Seconds, Is.GreaterThan(0d));
                    Assert.That(result.Oxygen, Is.GreaterThan(0f));
                    yield break;
                }

                Assert.Fail($"{(useLadder ? "Ladder" : "Jump")} could not land at {weight}% cargo. "
                    + $"Y={body.position.y:F3}, foot={playerCollider.bounds.min.y:F3}, vy={body.linearVelocityY:F3}, "
                    + $"grounded={movement.IsGrounded}, climbing={movement.IsClimbing}.");
            }
            finally
            {
                Object.Destroy(course);
            }
        }

        private sealed class Ascent
        {
            public double Seconds;
            public float Oxygen;
        }
    }
}
#endif

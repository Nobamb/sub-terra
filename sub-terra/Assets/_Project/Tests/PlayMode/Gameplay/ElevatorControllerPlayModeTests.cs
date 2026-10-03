using System.Collections;
using System.Reflection;
using NUnit.Framework;
using SubTerra.Gameplay.Player;
using SubTerra.Shared;
using UnityEngine;
using UnityEngine.TestTools;

namespace SubTerra.Gameplay.Player.Tests
{
    public sealed class ElevatorControllerPlayModeTests
    {
        private GameObject elevatorObject;
        private GameObject playerObject;
        private ElevatorController elevator;
        private PlayerMovement movement;
        private Rigidbody2D body;
        private RecordingTravelPort port;

        [SetUp]
        public void SetUp()
        {
            var portObject = new GameObject("TravelPort");
            port = portObject.AddComponent<RecordingTravelPort>();

            playerObject = new GameObject("Player");
            body = playerObject.AddComponent<Rigidbody2D>();
            body.gravityScale = 3f;
            playerObject.AddComponent<CapsuleCollider2D>();
            movement = playerObject.AddComponent<PlayerMovement>();

            elevatorObject = new GameObject("Elevator");
            elevatorObject.AddComponent<BoxCollider2D>();
            elevator = elevatorObject.AddComponent<ElevatorController>();
            SetField(elevator, "riderMovement", movement);
            SetField(elevator, "riderBody", body);
            SetField(elevator, "callDelaySeconds", 0f);
            SetField(elevator, "travelDelaySeconds", 0f);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(elevatorObject);
            Object.DestroyImmediate(playerObject);
            if (port != null)
            {
                Object.DestroyImmediate(port.gameObject);
            }
        }

        [UnityTest]
        public IEnumerator DuplicateRequest_LocksRiderAndTravelsOnlyOnce()
        {
            SetField(elevator, "callDelaySeconds", 0.05f);
            Assert.IsTrue(elevator.RequestTravel());
            Assert.IsFalse(elevator.RequestTravel());
            Assert.IsFalse(movement.CanMove);
            Assert.AreEqual(RigidbodyType2D.Kinematic, body.bodyType);
            Assert.AreEqual(0f, body.gravityScale);

            yield return new WaitForSecondsRealtime(0.1f);

            Assert.AreEqual(1, port.CallCount);
            Assert.AreEqual(ElevatorTravelState.Arrived, elevator.State);
            Assert.IsFalse(movement.CanMove);
            Assert.AreEqual(RigidbodyType2D.Kinematic, body.bodyType);
            Assert.AreEqual(0f, body.gravityScale);
        }

        [Test]
        public void DisablingDuringCall_RestoresRiderControlAndPhysics()
        {
            SetField(elevator, "callDelaySeconds", 10f);
            Assert.IsTrue(elevator.RequestTravel());

            elevator.enabled = false;

            Assert.IsTrue(movement.CanMove);
            Assert.AreEqual(RigidbodyType2D.Dynamic, body.bodyType);
            Assert.AreEqual(3f, body.gravityScale);
            Assert.AreEqual(0, port.CallCount);
        }

        [UnityTest]
        public IEnumerator DoorVisual_KeepsCabinRaisedAndClosedAfterTravel()
        {
            var left = new GameObject("LeftDoor", typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
            var right = new GameObject("RightDoor", typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
            var cabin = new GameObject("ElevatorCabin").transform;
            left.transform.SetParent(elevatorObject.transform, false);
            right.transform.SetParent(elevatorObject.transform, false);
            cabin.SetParent(elevatorObject.transform, false);
            var boardingAnchor = new GameObject("BoardingAnchor").transform;
            boardingAnchor.SetParent(cabin, false);
            boardingAnchor.localPosition = new Vector3(0f, -0.65f, 0f);
            SetField(elevator, "boardingAnchor", boardingAnchor);
            var visual = elevatorObject.AddComponent<ElevatorDoorVisual>();
            SetVisualField(visual, "leftDoor", left);
            SetVisualField(visual, "rightDoor", right);
            SetVisualField(visual, "cabinRoot", cabin);
            SetVisualField(visual, "initialOpenDelay", 0.04f);
            SetVisualField(visual, "departurePause", 0.18f);
            SetVisualField(visual, "openingDuration", 0.18f);
            SetVisualField(visual, "slideDuration", 0.22f);
            SetVisualField(visual, "liftDuration", 0.18f);
            SetVisualField(visual, "liftDistance", 1.4f);
            SetVisualField(visual, "liftToExit", false);
            SetField(elevator, "doorVisual", visual);
            visual.enabled = false;
            visual.enabled = true;
            Assert.That(visual.ClosedFraction, Is.EqualTo(1f).Within(0.01f));
            Assert.That(left.enabled && right.enabled, Is.True);

            yield return new WaitForSecondsRealtime(0.12f);
            Assert.That(visual.ClosedFraction, Is.GreaterThan(0f).And.LessThan(1f));
            Assert.That(left.transform.localPosition.x, Is.LessThan(-0.29f));

            yield return new WaitForSecondsRealtime(0.18f);
            Assert.That(visual.ClosedFraction, Is.EqualTo(0f).Within(0.01f));
            Assert.That(left.enabled || right.enabled, Is.False);

            SetField(elevator, "callDelaySeconds", 0.35f);
            SetField(elevator, "travelDelaySeconds", 0.05f);
            Assert.That(elevator.RequestTravel(), Is.True);

            yield return new WaitForSecondsRealtime(0.11f);
            Assert.That(elevator.State, Is.EqualTo(ElevatorTravelState.Calling));
            Assert.That(visual.ClosedFraction, Is.GreaterThan(0f).And.LessThan(1f));
            Assert.That(left.enabled && right.enabled, Is.True);
            Assert.That(left.transform.localPosition.x, Is.GreaterThan(-0.9f));

            yield return new WaitForSecondsRealtime(0.25f);
            Assert.That(visual.ClosedFraction, Is.EqualTo(1f).Within(0.01f));

            yield return new WaitForSecondsRealtime(0.22f);
            Assert.That(elevator.State, Is.EqualTo(ElevatorTravelState.Moving));
            Assert.That(visual.LiftFraction, Is.GreaterThanOrEqualTo(0f).And.LessThan(1f));

            yield return new WaitForSecondsRealtime(0.24f);
            Assert.That(visual.LiftFraction, Is.EqualTo(1f).Within(0.01f));
            Assert.That(cabin.localPosition.y, Is.EqualTo(1.4f).Within(0.01f));
            Assert.That(body.position.y, Is.EqualTo(boardingAnchor.position.y).Within(0.02f));

            yield return new WaitForSecondsRealtime(0.24f);
            Assert.That(elevator.State, Is.EqualTo(ElevatorTravelState.Arrived));
            Assert.That(visual.ClosedFraction, Is.EqualTo(1f).Within(0.01f));
            Assert.That(visual.LiftFraction, Is.EqualTo(1f).Within(0.01f));
            Assert.That(cabin.localPosition.y, Is.EqualTo(1.4f).Within(0.01f));
            Assert.That(left.enabled && right.enabled, Is.True);
        }

        [UnityTest]
        public IEnumerator Departure_ReachesHoistEndBeforeTravelling()
        {
            var cabin = new GameObject("ElevatorCabin").transform;
            cabin.SetParent(elevatorObject.transform, false);
            var anchor = new GameObject("BoardingAnchor").transform;
            anchor.SetParent(cabin, false);
            SetField(elevator, "boardingAnchor", anchor);

            var rails = new GameObject("HoistRails", typeof(SpriteRenderer));
            rails.transform.SetParent(elevatorObject.transform, false);
            rails.transform.localPosition = Vector3.up * 4f;
            rails.transform.localScale = new Vector3(0.1f, 8f, 1f);
            var sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f),
                new Vector2(0.5f, 0.5f), 1f);
            var railRenderer = rails.GetComponent<SpriteRenderer>();
            railRenderer.sprite = sprite;
            var artwork = new GameObject("Artwork", typeof(SpriteRenderer));
            artwork.transform.SetParent(cabin, false);
            var artworkRenderer = artwork.GetComponent<SpriteRenderer>();
            artworkRenderer.sprite = sprite;

            var visual = elevatorObject.AddComponent<ElevatorDoorVisual>();
            SetVisualField(visual, "cabinRoot", cabin);
            SetVisualField(visual, "initialOpenDelay", 0f);
            SetVisualField(visual, "slideDuration", 0.01f);
            SetVisualField(visual, "departurePause", 0.01f);
            SetField(elevator, "doorVisual", visual);
            SetField(elevator, "callDelaySeconds", 0f);
            SetField(elevator, "travelDelaySeconds", 0f);

            try
            {
                Assert.IsTrue(elevator.RequestTravel());
                float timeout = Time.realtimeSinceStartup + 4f;
                while (port.CallCount == 0 && Time.realtimeSinceStartup < timeout)
                {
                    Assert.AreEqual(ElevatorTravelState.Moving, elevator.State);
                    yield return null;
                }

                Assert.AreEqual(1, port.CallCount);
                Assert.That(artworkRenderer.bounds.min.y,
                    Is.GreaterThanOrEqualTo(railRenderer.bounds.max.y + 0.34f));
                Assert.That(body.position.y, Is.EqualTo(anchor.position.y).Within(0.02f));
                Assert.That(visual.LiftFraction, Is.EqualTo(1f));
            }
            finally
            {
                Object.DestroyImmediate(sprite);
            }
        }

        [UnityTest]
        public IEnumerator FailedTravel_ReturnsCabinAndRiderBeforeOpeningDoors()
        {
            var cabin = new GameObject("ElevatorCabin").transform;
            cabin.SetParent(elevatorObject.transform, false);
            var boardingAnchor = new GameObject("BoardingAnchor").transform;
            boardingAnchor.SetParent(cabin, false);
            boardingAnchor.localPosition = new Vector3(0f, -0.65f, 0f);
            SetField(elevator, "boardingAnchor", boardingAnchor);

            var left = new GameObject("LeftDoor", typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
            var right = new GameObject("RightDoor", typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
            left.transform.SetParent(elevatorObject.transform, false);
            right.transform.SetParent(elevatorObject.transform, false);
            var visual = elevatorObject.AddComponent<ElevatorDoorVisual>();
            SetVisualField(visual, "leftDoor", left);
            SetVisualField(visual, "rightDoor", right);
            SetVisualField(visual, "cabinRoot", cabin);
            SetVisualField(visual, "initialOpenDelay", 0f);
            SetVisualField(visual, "departurePause", 0.01f);
            SetVisualField(visual, "slideDuration", 0.01f);
            SetVisualField(visual, "liftDuration", 0.05f);
            SetVisualField(visual, "liftDistance", 1.4f);
            SetVisualField(visual, "liftToExit", false);
            SetField(elevator, "doorVisual", visual);
            SetField(elevator, "callDelaySeconds", 0f);
            SetField(elevator, "travelDelaySeconds", 0f);
            port.ShouldSucceed = false;
            visual.enabled = false;
            visual.enabled = true;

            Assert.IsTrue(elevator.RequestTravel());
            yield return new WaitForSecondsRealtime(0.3f);

            Assert.AreEqual(ElevatorTravelState.Blocked, elevator.State);
            Assert.That(visual.LiftFraction, Is.EqualTo(0f).Within(0.01f));
            Assert.That(cabin.localPosition.y, Is.EqualTo(0f).Within(0.01f));
            Assert.IsTrue(movement.CanMove);
            Assert.AreEqual(RigidbodyType2D.Dynamic, body.bodyType);
            Assert.That(visual.ClosedFraction, Is.LessThan(1f));
        }

        [Test]
        public void RiderInsideElevator_ClaimsSharedInteractionPriority()
        {
            Assert.IsTrue(elevator.TryClaimInteractionPriority());

            SetField<PlayerMovement>(elevator, "riderMovement", null);
            SetField<Rigidbody2D>(elevator, "riderBody", null);
            playerObject.transform.position = Vector3.right * 20f;
            Physics2D.SyncTransforms();

            Assert.IsFalse(elevator.TryClaimInteractionPriority());
        }

        [Test]
        public void BlockedExit_RejectsBeforeLockOrTravel()
        {
            var exit = new GameObject("Exit").transform;
            exit.SetParent(elevatorObject.transform);
            var obstacle = new GameObject("Obstacle");
            obstacle.layer = 8;
            obstacle.transform.position = exit.position;
            obstacle.AddComponent<BoxCollider2D>();
            SetField(elevator, "safeExitPoint", exit);
            SetField(elevator, "exitBlockerLayers", (LayerMask)(1 << 8));
            Physics2D.SyncTransforms();

            Assert.IsFalse(elevator.RequestTravel());

            Assert.AreEqual(ElevatorTravelState.Blocked, elevator.State);
            Assert.IsTrue(movement.CanMove);
            Assert.AreEqual(0, port.CallCount);
            Object.DestroyImmediate(obstacle);
        }

        private static void SetField<T>(object target, string name, T value)
        {
            typeof(ElevatorController)
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(target, value);
        }

        private static void SetVisualField<T>(ElevatorDoorVisual target, string name, T value)
        {
            typeof(ElevatorDoorVisual)
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(target, value);
        }

        public sealed class RecordingTravelPort : MonoBehaviour, IElevatorTravelPort
        {
            public int CallCount { get; private set; }
            public bool ShouldSucceed { get; set; } = true;
            public ElevatorTravelState State => ElevatorTravelState.Idle;

            public bool TryTravel(ElevatorDestination destination, out string reason)
            {
                CallCount++;
                reason = string.Empty;
                return ShouldSucceed;
            }
        }
    }
}

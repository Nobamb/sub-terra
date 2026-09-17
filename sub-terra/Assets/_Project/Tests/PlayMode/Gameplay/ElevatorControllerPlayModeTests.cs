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
            Assert.IsTrue(movement.CanMove);
            Assert.AreEqual(RigidbodyType2D.Dynamic, body.bodyType);
            Assert.AreEqual(3f, body.gravityScale);
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
        public IEnumerator MineArrival_OpensTheInitiallyClosedDoors()
        {
            Object.DestroyImmediate(elevatorObject);
            port.State = ElevatorTravelState.Arrived;
            elevatorObject = new GameObject("ArrivingElevator");
            elevatorObject.AddComponent<BoxCollider2D>();
            elevator = elevatorObject.AddComponent<ElevatorController>();
            Assert.That(elevator.State, Is.EqualTo(ElevatorTravelState.Arrived));

            var left = new GameObject("LeftDoor", typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
            var right = new GameObject("RightDoor", typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
            left.transform.SetParent(elevatorObject.transform, false);
            right.transform.SetParent(elevatorObject.transform, false);
            var visual = elevatorObject.AddComponent<ElevatorDoorVisual>();
            SetVisualField(visual, "leftDoor", left);
            SetVisualField(visual, "rightDoor", right);
            visual.enabled = false;
            visual.enabled = true;

            Assert.That(left.enabled && right.enabled, Is.True);
            Assert.That(visual.ClosedFraction, Is.EqualTo(1f).Within(0.01f));
            yield return new WaitForSecondsRealtime(0.35f);
            Assert.That(left.enabled || right.enabled, Is.False);
            Assert.That(visual.ClosedFraction, Is.EqualTo(0f).Within(0.01f));
        }

        [UnityTest]
        public IEnumerator DoorVisual_ClosesDuringCallAndReopensAfterArrival()
        {
            var left = new GameObject("LeftDoor", typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
            var right = new GameObject("RightDoor", typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
            var artwork = new GameObject("Artwork").transform;
            var openingMask = new GameObject("OpeningMask").transform;
            var riderVisual = new GameObject(
                "VisualRoot", typeof(SpriteRenderer), typeof(PlayerAnimationController)).transform;
            left.transform.SetParent(elevatorObject.transform, false);
            right.transform.SetParent(elevatorObject.transform, false);
            artwork.SetParent(elevatorObject.transform, false);
            openingMask.SetParent(elevatorObject.transform, false);
            riderVisual.SetParent(playerObject.transform, false);
            artwork.localPosition = new Vector3(0f, -0.35f, 0f);
            openingMask.localPosition = new Vector3(0f, -0.55f, 0f);
            var visual = elevatorObject.AddComponent<ElevatorDoorVisual>();
            SetVisualField(visual, "leftDoor", left);
            SetVisualField(visual, "rightDoor", right);
            SetVisualField(visual, "artwork", artwork);
            SetVisualField(visual, "openingMask", openingMask);
            visual.enabled = false;
            visual.enabled = true;
            Assert.That(left.enabled, Is.False);
            Assert.That(right.enabled, Is.False);

            SetField(elevator, "callDelaySeconds", 0.35f);
            SetField(elevator, "travelDelaySeconds", 0.65f);
            Assert.That(elevator.RequestTravel(), Is.True);

            yield return new WaitForSecondsRealtime(0.18f);
            Assert.That(elevator.State, Is.EqualTo(ElevatorTravelState.Calling));
            Assert.That(visual.ClosedFraction, Is.GreaterThan(0f).And.LessThan(1f));
            Assert.That(left.enabled && right.enabled, Is.True);
            Assert.That(left.transform.localPosition.x, Is.GreaterThan(-0.9f));
            Assert.That(artwork.localPosition.y, Is.EqualTo(-0.35f).Within(0.01f));
            Assert.That(riderVisual.localPosition.y, Is.EqualTo(0f).Within(0.01f));

            yield return new WaitForSecondsRealtime(0.22f);
            Assert.That(visual.ClosedFraction, Is.EqualTo(1f).Within(0.01f));
            Assert.That(elevator.State, Is.EqualTo(ElevatorTravelState.Moving));
            Assert.That(artwork.localPosition.y, Is.GreaterThan(-0.35f));
            Assert.That(riderVisual.localPosition.y, Is.GreaterThan(0f));

            yield return new WaitForSecondsRealtime(0.4f);
            Assert.That(elevator.State, Is.EqualTo(ElevatorTravelState.Moving));
            Assert.That(artwork.localPosition.y, Is.GreaterThan(0.2f));
            Assert.That(openingMask.localPosition.y - artwork.localPosition.y,
                Is.EqualTo(-0.2f).Within(0.01f));
            Assert.That(riderVisual.localPosition.y - artwork.localPosition.y,
                Is.EqualTo(0.35f).Within(0.01f));

            yield return new WaitForSecondsRealtime(1.2f);
            Assert.That(elevator.State, Is.EqualTo(ElevatorTravelState.Arrived));
            Assert.That(visual.ClosedFraction, Is.EqualTo(0f).Within(0.01f));
            Assert.That(left.enabled || right.enabled, Is.False);
            Assert.That(artwork.localPosition.y, Is.EqualTo(-0.35f).Within(0.01f));
            Assert.That(openingMask.localPosition.y, Is.EqualTo(-0.55f).Within(0.01f));
            Assert.That(riderVisual.localPosition.y, Is.EqualTo(0f).Within(0.01f));
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
            public ElevatorTravelState State { get; set; } = ElevatorTravelState.Idle;

            public bool TryTravel(ElevatorDestination destination, out string reason)
            {
                CallCount++;
                reason = string.Empty;
                return true;
            }
        }
    }
}

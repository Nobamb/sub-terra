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

        public sealed class RecordingTravelPort : MonoBehaviour, IElevatorTravelPort
        {
            public int CallCount { get; private set; }
            public ElevatorTravelState State => ElevatorTravelState.Idle;

            public bool TryTravel(ElevatorDestination destination, out string reason)
            {
                CallCount++;
                reason = string.Empty;
                return true;
            }
        }
    }
}

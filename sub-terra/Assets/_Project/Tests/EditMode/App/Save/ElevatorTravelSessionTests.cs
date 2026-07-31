using System.Collections.Generic;
using NUnit.Framework;
using SubTerra.App.Core;
using SubTerra.App.Save;
using SubTerra.App.State;
using SubTerra.Shared;

namespace SubTerra.App.Tests.Save
{
    public sealed class ElevatorTravelSessionTests
    {
        [Test]
        public void CallAndDepart_TransitionsThroughExplicitStatesAndChargesOnce()
        {
            var state = GameState.CreateNew();
            state.SetCurrentEnergy(20);
            var scenes = new RecordingSceneLoader();
            var session = new ElevatorTravelSession(state);
            var transitions = new List<ElevatorTravelState>();
            session.StateChanged += transitions.Add;

            Assert.IsTrue(session.TryCall(SceneNames.Integration, 5, true, out var callFailure));
            Assert.IsFalse(session.TryCall(SceneNames.Integration, 5, true, out var duplicateFailure));
            Assert.AreEqual(ElevatorTravelFailure.None, callFailure);
            Assert.AreEqual(ElevatorTravelFailure.Busy, duplicateFailure);
            Assert.AreEqual(15, state.Player.Energy);

            Assert.IsTrue(session.TryDepart(scenes, out var departFailure));

            Assert.AreEqual(ElevatorTravelFailure.None, departFailure);
            CollectionAssert.AreEqual(
                new[]
                {
                    ElevatorTravelState.Calling,
                    ElevatorTravelState.Moving,
                    ElevatorTravelState.Arrived
                },
                transitions);
            Assert.AreEqual(1, scenes.LoadCount);
            Assert.AreEqual(SceneNames.Integration, scenes.LastScene);
            Assert.AreEqual(15, state.Player.Energy);
        }

        [Test]
        public void Call_BlockedExitOrInsufficientEnergy_DoesNotCharge()
        {
            var state = GameState.CreateNew();
            state.SetCurrentEnergy(4);
            var session = new ElevatorTravelSession(state);

            Assert.IsFalse(session.TryCall(SceneNames.Integration, 5, true, out var energyFailure));
            Assert.AreEqual(ElevatorTravelFailure.InsufficientEnergy, energyFailure);
            Assert.AreEqual(ElevatorTravelState.Blocked, session.State);
            Assert.AreEqual(4, state.Player.Energy);

            session.Reset();
            Assert.IsFalse(session.TryCall(SceneNames.Integration, 0, false, out var exitFailure));
            Assert.AreEqual(ElevatorTravelFailure.BlockedExit, exitFailure);
            Assert.AreEqual(4, state.Player.Energy);
        }

        [Test]
        public void Depart_WhenSceneLoadFails_RefundsEnergyAndBlocks()
        {
            var state = GameState.CreateNew();
            state.SetCurrentEnergy(20);
            var session = new ElevatorTravelSession(state);
            var scenes = new RecordingSceneLoader { Result = false };

            Assert.IsTrue(session.TryCall(SceneNames.Integration, 5, true, out _));
            Assert.IsFalse(session.TryDepart(scenes, out var failure));

            Assert.AreEqual(ElevatorTravelFailure.SceneLoadFailed, failure);
            Assert.AreEqual(ElevatorTravelState.Blocked, session.State);
            Assert.AreEqual(20, state.Player.Energy);
        }

        private sealed class RecordingSceneLoader : ISceneLoader
        {
            public bool Result { get; set; } = true;
            public int LoadCount { get; private set; }
            public string LastScene { get; private set; }

            public bool Load(string sceneName)
            {
                LoadCount++;
                LastScene = sceneName;
                return Result;
            }
        }
    }
}

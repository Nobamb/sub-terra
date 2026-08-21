using System.Collections;
using NUnit.Framework;
using SubTerra.App.State;
using SubTerra.App.Tutorial;
using UnityEngine.TestTools;

namespace SubTerra.App.Tests.PlayMode.DemoFlow
{
    public sealed class DemoFlowPlayModeTests
    {
        [UnityTest]
        public IEnumerator PromptB60_OrderedSignalsCompleteAllSeventeenQuests()
        {
            var state = GameState.CreateNew();
            var director = new DemoObjectiveDirector();
            director.BindGameState(state);
            director.ResetNewGame();

            for (var i = 0; i < DemoObjectiveCatalog.All.Count; i++)
            {
                var current = DemoObjectiveCatalog.GetRequired(director.CurrentObjectiveId);
                Assert.That(current, Is.Not.Null);
                Assert.That(director.HandleSignal(current.RequiredSignal).Advanced, Is.True);
            }

            Assert.That(director.IsDemoComplete, Is.True);
            Assert.That(state.Progress.IsDemoComplete, Is.True);
            Assert.That(state.Progress.CompletedObjectives, Is.EqualTo(17));
            yield return null;
        }

        [UnityTest]
        public IEnumerator PromptB60_RestoreRejectsPastAndFutureSignals()
        {
            var state = GameState.CreateNew();
            state.SetDemoProgress(DemoObjectiveIds.PlaceLadder, 7, false);
            var director = new DemoObjectiveDirector();
            director.BindGameState(state);
            director.RestoreFromProgress(state.Progress);

            Assert.That(director.HandleSignal(DemoProgressSignal.SupportPlacedInDanger).Advanced, Is.False);
            Assert.That(director.HandleSignal(DemoProgressSignal.LightPlacedAtDepth).Advanced, Is.False);
            Assert.That(director.CurrentObjectiveId, Is.EqualTo(DemoObjectiveIds.PlaceLadder));
            Assert.That(director.CompletedCount, Is.EqualTo(7));
            yield return null;
        }
    }
}

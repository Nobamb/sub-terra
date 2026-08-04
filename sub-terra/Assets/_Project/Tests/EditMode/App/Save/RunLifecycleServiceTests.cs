using NUnit.Framework;
using SubTerra.App.Save;
using SubTerra.App.State;
using SubTerra.Shared;

namespace SubTerra.App.Tests.Save
{
    public sealed class RunLifecycleServiceTests
    {
        [Test]
        public void BeginExploration_ResetsRunAndTracksMaximumDepth()
        {
            var state = GameState.CreateNew();
            state.SetDepth(12);
            var service = new RunLifecycleService(state);

            Assert.That(service.TryBeginExploration(out var reason), Is.True, reason);
            Assert.That(service.Phase, Is.EqualTo(RunLifecyclePhase.Active));
            Assert.That(state.Run.Depth, Is.Zero);
            Assert.That(state.Run.MaximumDepth, Is.Zero);

            state.SetDepth(18);
            state.SetDepth(7);
            Assert.That(state.Run.MaximumDepth, Is.EqualTo(18));
        }

        [Test]
        public void NormalReturn_UsesOnlyActiveInRangeOutpostCheckpoint()
        {
            var state = GameState.CreateNew();
            var service = new RunLifecycleService(state);
            Assert.That(service.TryBeginExploration(out _), Is.True);

            var status = new OutpostStatusDto
            {
                isActive = true,
                isInInteractionRange = true,
                checkpointId = "outpost.alpha",
                checkpointX = 14,
                checkpointY = -3
            };

            Assert.That(service.TryPrepareNormalReturn(status, out var target, out var reason), Is.True, reason);
            Assert.That(target.Kind, Is.EqualTo(RunReturnTargetKind.OutpostCheckpoint));
            Assert.That(target.CheckpointId, Is.EqualTo("outpost.alpha"));
            Assert.That(target.X, Is.EqualTo(14));
            Assert.That(service.CompleteNormalReturn(out reason), Is.True, reason);
            Assert.That(service.Phase, Is.EqualTo(RunLifecyclePhase.Completed));
        }

        [Test]
        public void InvalidCheckpoint_FallsBackAndFailedTransitionPreservesActiveRun()
        {
            var state = GameState.CreateNew();
            state.SetDepth(22);
            var service = new RunLifecycleService(state);
            Assert.That(service.TryBeginExploration(out _), Is.True);
            state.SetDepth(22);

            var unavailable = new OutpostStatusDto
            {
                isActive = false,
                isInInteractionRange = true,
                checkpointId = "outpost.offline"
            };

            Assert.That(service.TryPrepareNormalReturn(unavailable, out var target, out _), Is.True);
            Assert.That(target.Kind, Is.EqualTo(RunReturnTargetKind.SurfaceFallback));
            Assert.That(service.TryPrepareNormalReturn(unavailable, out _, out _), Is.False);

            service.AbortPendingReturn();
            Assert.That(service.Phase, Is.EqualTo(RunLifecyclePhase.Active));
            Assert.That(state.Run.Depth, Is.EqualTo(22));
            Assert.That(state.Run.MaximumDepth, Is.EqualTo(22));
        }
    }
}

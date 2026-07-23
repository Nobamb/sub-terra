using NUnit.Framework;
using SubTerra.App.State;

namespace SubTerra.App.Tests
{
    public sealed class GameStateTests
    {
        [Test]
        public void CreateNew_UsesSafeDefaults()
        {
            var state = GameState.CreateNew();
            Assert.That(GameState.IsComplete(state), Is.True);
            Assert.That(state.Player.Energy, Is.EqualTo(100));
            Assert.That(state.Player.Gold, Is.Zero);
            Assert.That(state.Player.Cargo, Is.Zero);
            Assert.That(state.Run.IsSafe, Is.True);
            Assert.That(state.Run.Depth, Is.Zero);
            Assert.That(state.Progress.CompletedObjectives, Is.Zero);
        }

        [Test]
        public void FromParts_RejectsNullSubStates()
        {
            Assert.That(GameState.FromParts(null, new ProgressState(0), new RunState(0, true)), Is.Null);
            Assert.That(GameState.FromParts(
                new PlayerState(100, 0, 0f, 0f),
                null,
                new RunState(0, true)), Is.Null);
            Assert.That(GameState.FromParts(
                new PlayerState(100, 0, 0f, 0f),
                new ProgressState(0),
                null), Is.Null);
        }

        [Test]
        public void FromParts_AcceptsCompleteSubStates()
        {
            var state = GameState.FromParts(
                new PlayerState(40, 7, 2f, 0.1f),
                new ProgressState(1),
                new RunState(5, false));

            Assert.That(GameState.IsComplete(state), Is.True);
            Assert.That(state.Player.Energy, Is.EqualTo(40));
            Assert.That(state.Player.Gold, Is.EqualTo(7));
            Assert.That(state.Progress.CompletedObjectives, Is.EqualTo(1));
            Assert.That(state.Run.Depth, Is.EqualTo(5));
            Assert.That(state.Run.IsSafe, Is.False);
        }

        [Test]
        public void IsComplete_FalseForNull()
        {
            Assert.That(GameState.IsComplete(null), Is.False);
        }

        [Test]
        public void AddGold_DoesNotGoBelowZero()
        {
            var state = GameState.CreateNew();
            state.Player.AddGold(10);
            Assert.That(state.Player.Gold, Is.EqualTo(10));
            state.Player.AddGold(-25);
            Assert.That(state.Player.Gold, Is.Zero);
        }

        [Test]
        public void SubStates_AreNotPubliclyReplaceable()
        {
            // 컴파일 타임에 private set이므로 런타임 교체 API가 없음을 구조적으로 보장한다.
            // 공개 표면은 Player/Progress/Run 읽기 + 의도 기반 변경 메서드만 허용한다.
            var state = GameState.CreateNew();
            var playerType = typeof(GameState).GetProperty(nameof(GameState.Player));
            Assert.That(playerType, Is.Not.Null);
            Assert.That(playerType.CanRead, Is.True);
            Assert.That(playerType.SetMethod, Is.Not.Null);
            Assert.That(playerType.SetMethod.IsPublic, Is.False);
        }
    }
}

using System;
using SubTerra.App.Core;
using SubTerra.App.State;
using SubTerra.Shared;

namespace SubTerra.App.Save
{
    public enum ElevatorTravelFailure
    {
        None,
        InvalidState,
        InvalidDestination,
        Busy,
        InsufficientEnergy,
        BlockedExit,
        SceneLoadFailed
    }

    /// <summary>
    /// 호출 예약과 출발을 분리해 중복 입력이 전력 차감이나 Scene 로드를 반복하지 않게 한다.
    /// </summary>
    public sealed class ElevatorTravelSession
    {
        private readonly GameState gameState;
        private string destinationScene = string.Empty;
        private int chargedEnergy;

        public ElevatorTravelSession(GameState state)
        {
            gameState = state;
        }

        public ElevatorTravelState State { get; private set; } = ElevatorTravelState.Idle;
        public event Action<ElevatorTravelState> StateChanged;

        public bool TryCall(
            string sceneName,
            int energyCost,
            bool isExitClear,
            out ElevatorTravelFailure failure)
        {
            if (State == ElevatorTravelState.Calling || State == ElevatorTravelState.Moving)
            {
                failure = ElevatorTravelFailure.Busy;
                return false;
            }

            if (gameState == null || !GameState.IsComplete(gameState))
            {
                return Block(ElevatorTravelFailure.InvalidState, out failure);
            }

            if (string.IsNullOrWhiteSpace(sceneName))
            {
                return Block(ElevatorTravelFailure.InvalidDestination, out failure);
            }

            if (!isExitClear)
            {
                return Block(ElevatorTravelFailure.BlockedExit, out failure);
            }

            var cost = Math.Max(0, energyCost);
            if (gameState.Player.Energy < cost)
            {
                return Block(ElevatorTravelFailure.InsufficientEnergy, out failure);
            }

            destinationScene = sceneName;
            chargedEnergy = cost;
            if (chargedEnergy > 0)
            {
                gameState.SetCurrentEnergy(gameState.Player.Energy - chargedEnergy);
            }

            SetState(ElevatorTravelState.Calling);
            failure = ElevatorTravelFailure.None;
            return true;
        }

        public bool TryDepart(ISceneLoader sceneLoader, out ElevatorTravelFailure failure, bool deferArrival = false)
        {
            if (State != ElevatorTravelState.Calling)
            {
                failure = ElevatorTravelFailure.Busy;
                return false;
            }

            if (sceneLoader == null)
            {
                RefundEnergy();
                return Block(ElevatorTravelFailure.SceneLoadFailed, out failure);
            }

            SetState(ElevatorTravelState.Moving);
            if (!sceneLoader.Load(destinationScene))
            {
                RefundEnergy();
                return Block(ElevatorTravelFailure.SceneLoadFailed, out failure);
            }

            if (!deferArrival) CompleteArrival(true);
            failure = ElevatorTravelFailure.None;
            return true;
        }

        public void CompleteArrival(bool succeeded)
        {
            if (State != ElevatorTravelState.Moving) return;
            if (!succeeded)
            {
                RefundEnergy();
                Block(ElevatorTravelFailure.SceneLoadFailed, out _);
                return;
            }

            chargedEnergy = 0;
            destinationScene = string.Empty;
            SetState(ElevatorTravelState.Arrived);
        }

        public void Reset()
        {
            if (State == ElevatorTravelState.Calling || State == ElevatorTravelState.Moving)
            {
                RefundEnergy();
            }

            destinationScene = string.Empty;
            chargedEnergy = 0;
            SetState(ElevatorTravelState.Idle);
        }

        private bool Block(ElevatorTravelFailure value, out ElevatorTravelFailure failure)
        {
            destinationScene = string.Empty;
            chargedEnergy = 0;
            SetState(ElevatorTravelState.Blocked);
            failure = value;
            return false;
        }

        private void RefundEnergy()
        {
            if (chargedEnergy > 0 && gameState != null)
            {
                gameState.SetCurrentEnergy(gameState.Player.Energy + chargedEnergy);
            }
        }

        private void SetState(ElevatorTravelState state)
        {
            if (State == state)
            {
                return;
            }

            State = state;
            StateChanged?.Invoke(state);
        }
    }
}

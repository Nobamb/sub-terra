using System.Collections.Generic;
using SubTerra.App.UI;
using SubTerra.Gameplay.Building;
using SubTerra.Gameplay.Mining;
using SubTerra.Gameplay.Player;
using SubTerra.Shared;
using UnityEngine;

namespace SubTerra.App.Integration
{
    /// <summary>
    /// UiPauseGate가 잡힌 동안 월드 쪽 입력 컴포넌트(이동·채굴·건설·엘리베이터·포탈)를 끄고,
    /// 풀리면 이 컴포넌트가 끈 것만 되살린다. Gameplay는 App을 참조할 수 없어 Integration에서 연결한다.
    /// </summary>
    [DefaultExecutionOrder(-400)]
    public sealed class PauseInputSuspender : MonoBehaviour
    {
        private readonly List<Behaviour> suspended = new List<Behaviour>();

        public bool IsSuspended => suspended.Count > 0;

        private void OnEnable()
        {
            UiPauseGate.Changed += Sync;
            Sync();
        }

        private void OnDisable()
        {
            UiPauseGate.Changed -= Sync;
            Resume();
        }

        private void Sync()
        {
            if (UiPauseGate.IsHeld)
            {
                Suspend();
            }
            else
            {
                Resume();
            }
        }

        private void Suspend()
        {
            if (suspended.Count > 0)
            {
                return;
            }

            Collect(FindObjectsByType<PlayerController>(FindObjectsSortMode.None));
            Collect(FindObjectsByType<PlayerMiningController>(FindObjectsSortMode.None));
            Collect(FindObjectsByType<BuildingPlacementInput>(FindObjectsSortMode.None));
            Collect(FindObjectsByType<EmergencyEscapePortal>(FindObjectsSortMode.None));

            // 이동 중인 엘리베이터를 끄면 탑승이 풀리므로 대기 상태일 때만 입력을 막는다.
            var elevators = FindObjectsByType<ElevatorController>(FindObjectsSortMode.None);
            for (var i = 0; i < elevators.Length; i++)
            {
                if (elevators[i] != null && elevators[i].State == ElevatorTravelState.Idle)
                {
                    Collect(elevators[i]);
                }
            }

            // 마지막으로 읽은 이동 입력이 남아 정지 해제 직후 한 틱 흐르지 않게 비운다.
            var movements = FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);
            for (var i = 0; i < movements.Length; i++)
            {
                if (movements[i] != null)
                {
                    movements[i].SetMoveInput(0f);
                    movements[i].SetVerticalMoveInput(0f);
                }
            }
        }

        private void Collect(Behaviour[] behaviours)
        {
            for (var i = 0; i < behaviours.Length; i++)
            {
                Collect(behaviours[i]);
            }
        }

        private void Collect(Behaviour behaviour)
        {
            if (behaviour != null && behaviour.enabled)
            {
                behaviour.enabled = false;
                suspended.Add(behaviour);
            }
        }

        private void Resume()
        {
            for (var i = 0; i < suspended.Count; i++)
            {
                if (suspended[i] != null)
                {
                    suspended[i].enabled = true;
                }
            }

            suspended.Clear();
        }
    }
}

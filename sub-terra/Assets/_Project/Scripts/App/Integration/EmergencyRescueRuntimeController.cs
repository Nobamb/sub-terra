using SubTerra.App.Drone.Dialogue;
using SubTerra.App.Run;
using SubTerra.App.Save;
using SubTerra.App.State;
using SubTerra.App.UI.Drone;
using SubTerra.App.UI.EmergencyRescue;
using SubTerra.App.UI.HUD;
using SubTerra.Gameplay.Building;
using SubTerra.Gameplay.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SubTerra.App.Integration
{
    /// <summary>
    /// 전력 0 알림, R/칩 재호출, 비용 결제와 엘리베이터 이동을 조립한다.
    /// RunFailure 경로를 사용하지 않으므로 이동·점프·사다리 입력을 잠그지 않는다.
    /// </summary>
    public sealed class EmergencyRescueRuntimeController : MonoBehaviour
    {
        private const float DroneReminderDelaySeconds = 18f;
        private const string ReminderText = "구출이 필요하면 전력 옆 버튼을 누르세요";

        private SaveRuntimeController runtime;
        private GameState gameState;
        private EmergencyRescueService service;
        private Transform playerTransform;
        private Transform elevatorCenter;
        private EmergencyRescuePanelView view;
        private bool initialPopupShown;
        private bool rescueCompleted;
        private bool reminderShown;
        private float closedAt = -1f;

        public bool IsPanelOpen => view != null && view.IsOpen;
        public bool IsChipVisible => view != null && view.IsChipVisible;
        public bool IsRescueAvailable => service != null && service.IsAvailable && !rescueCompleted;

        private void Update()
        {
            if (!IsRescueAvailable)
            {
                return;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.rKey.wasPressedThisFrame)
            {
                OpenPanel();
            }

            if (keyboard != null
                && keyboard.escapeKey.wasPressedThisFrame
                && IsPanelOpen)
            {
                ClosePanel();
            }

            if (!IsPanelOpen
                && !reminderShown
                && closedAt >= 0f
                && Time.unscaledTime - closedAt >= DroneReminderDelaySeconds)
            {
                reminderShown = true;
                ShowDroneReminder();
            }
        }

        public void Bind(
            SaveRuntimeController saveRuntime,
            GameState state,
            Transform player,
            HudBinder hud)
        {
            Unbind();
            runtime = saveRuntime;
            gameState = state;
            playerTransform = player;
            service = runtime != null && runtime.InventoryService != null && GameState.IsComplete(state)
                ? new EmergencyRescueService(state, runtime.InventoryService)
                : null;

            EnsureView(hud);
            if (view != null)
            {
                view.Bind(TryRescue, ClosePanel, OpenPanel);
            }

            if (gameState != null)
            {
                gameState.EnergyChanged += OnEnergyChanged;
            }

            if (IsRescueAvailable)
            {
                BeginDepletionEpisode();
            }
            else
            {
                HideAll();
            }
        }

        public void Unbind()
        {
            if (gameState != null)
            {
                gameState.EnergyChanged -= OnEnergyChanged;
            }

            runtime = null;
            gameState = null;
            service = null;
            playerTransform = null;
            elevatorCenter = null;
            ResetEpisode();
        }

        private void OnDestroy()
        {
            Unbind();
        }

        public void OpenPanel()
        {
            if (!IsRescueAvailable || view == null)
            {
                return;
            }

            initialPopupShown = true;
            closedAt = -1f;
            view.SetChipVisible(false);
            view.SetInteractable(true);
            view.Show(service.GetCurrentCost());
        }

        public void ClosePanel()
        {
            if (view == null)
            {
                return;
            }

            view.Close();
            bool showChip = IsRescueAvailable;
            view.SetChipVisible(showChip);
            if (showChip)
            {
                closedAt = Time.unscaledTime;
            }
        }

        private void OnEnergyChanged(EnergyReadModel energy)
        {
            if (energy.Current > 0)
            {
                ResetEpisode();
                return;
            }

            if (gameState?.Run?.LifecyclePhase == RunLifecyclePhase.Active)
            {
                BeginDepletionEpisode();
            }
        }

        private void BeginDepletionEpisode()
        {
            if (!initialPopupShown)
            {
                OpenPanel();
                return;
            }

            if (view != null && !view.IsOpen)
            {
                view.SetChipVisible(true);
            }
        }

        private void TryRescue()
        {
            if (!IsRescueAvailable || view == null)
            {
                return;
            }

            if (!HasElevatorDestination())
            {
                view.SetMessage("엘리베이터 위치를 찾지 못했습니다. 잠시 후 다시 시도해 주세요.");
                return;
            }

            view.SetInteractable(false);
            if (!service.TryRescue(out _, out EmergencyRescueFailure failure))
            {
                view.SetInteractable(true);
                view.Show(
                    service.GetCurrentCost(),
                    failure == EmergencyRescueFailure.InventoryChanged
                        ? "화물 상태가 변경되었습니다. 최신 비용을 다시 확인해 주세요."
                        : "현재는 긴급 구출을 요청할 수 없습니다.");
                return;
            }

            MoveToResolvedElevator();
            rescueCompleted = true;
            view.Close();
            view.SetChipVisible(false);
            runtime?.SaveCurrent(AutoSaveReason.Manual);
        }

        private bool HasElevatorDestination()
        {
            if (playerTransform == null)
            {
                return false;
            }

            if (elevatorCenter == null)
            {
                elevatorCenter = FindElevatorCenter();
            }

            return elevatorCenter != null;
        }

        private void MoveToResolvedElevator()
        {
            Vector3 target = elevatorCenter.position;
            target.z = playerTransform.position.z;
            Rigidbody2D body = playerTransform.GetComponent<Rigidbody2D>();
            if (body != null)
            {
                body.linearVelocity = Vector2.zero;
                body.angularVelocity = 0f;
                body.position = target;
            }

            playerTransform.position = target;
        }

        private void EnsureView(HudBinder hud)
        {
            if (view != null)
            {
                return;
            }

            view = FindAnyObjectByType<EmergencyRescuePanelView>(FindObjectsInactive.Include);
            if (view != null || hud == null || hud.BasicHud == null)
            {
                return;
            }

            var canvas = hud.GetComponentInParent<Canvas>(true);
            if (canvas == null)
            {
                return;
            }

            var energy = hud.BasicHud.EnergyText;
            view = EmergencyRescuePanelView.Create(
                canvas.transform,
                energy != null ? energy.rectTransform : null,
                energy != null ? energy.font : null);
        }

        private void ShowDroneReminder()
        {
            var dialogue = new DroneDialogueResult(
                "dialogue.emergency_rescue.reminder",
                ReminderText,
                false,
                true,
                false);

            var socket = FindAnyObjectByType<DroneDialogueSocket>(FindObjectsInactive.Exclude);
            if (socket != null)
            {
                socket.SetDialogue(dialogue);
            }

            var panel = FindAnyObjectByType<DroneDialoguePanelView>(FindObjectsInactive.Include);
            if (panel != null)
            {
                panel.SetDialogue(dialogue);
            }
        }

        private void ResetEpisode()
        {
            initialPopupShown = false;
            rescueCompleted = false;
            reminderShown = false;
            closedAt = -1f;
            HideAll();
        }

        private void HideAll()
        {
            if (view != null)
            {
                view.Close();
                view.SetChipVisible(false);
            }
        }

        private static Transform FindElevatorCenter()
        {
            var elevators = FindObjectsByType<ElevatorController>(FindObjectsInactive.Exclude);
            for (var i = 0; i < elevators.Length; i++)
            {
                ElevatorController elevator = elevators[i];
                if (elevator == null)
                {
                    continue;
                }

                var transforms = elevator.GetComponentsInChildren<Transform>(true);
                for (var j = 0; j < transforms.Length; j++)
                {
                    if (transforms[j] != null && transforms[j].name == "BoardingAnchor")
                    {
                        return transforms[j];
                    }
                }

                return elevator.transform;
            }

            GameObject byName = GameObject.Find("BoardingAnchor");
            return byName != null ? byName.transform : null;
        }
    }
}

using SubTerra.App.Core;
using SubTerra.App.Core.Data;
using SubTerra.App.Drone.Dialogue;
using SubTerra.App.Run;
using SubTerra.App.Save;
using SubTerra.App.State;
using SubTerra.App.UI;
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
    /// 전력 0 알림, R/머리 위 칩 재호출, 비용 결제와 엘리베이터 이동을 조립한다.
    /// RunFailure 경로를 사용하지 않으므로 이동·점프·사다리 입력을 잠그지 않는다.
    /// </summary>
    public sealed class EmergencyRescueRuntimeController : MonoBehaviour
    {
        private const float DroneReminderDelaySeconds = 18f;
        private const string ReminderText = "구출이 필요하면 머리 위 버튼을 누르거나 R 키를 누르세요";

        private SaveRuntimeController runtime;
        private GameState gameState;
        private EmergencyRescueService service;
        private Transform playerTransform;
        private Transform elevatorCenter;
        private EmergencyRescuePanelView view;
        private bool initialPopupShown;
        private bool rescueCompleted;
        private bool reminderShown;
        // 닫는 연출 도중 재열기 입력이 들어오면 닫기가 끝난 직후 같은 팝업을 다시 연다.
        private bool pendingReopen;
        private float closedAt = -1f;

        /// <summary>팝업이 화면에 있음(등장·열림·닫는 중). Esc 닫기 경로가 이 값을 본다.</summary>
        public bool IsPanelOpen => view != null && view.IsPopupVisible;
        public bool IsChipVisible => view != null && view.IsChipVisible;
        public bool IsRescueAvailable => service != null && service.IsAvailable && !rescueCompleted;

        private void Update()
        {
            if (!IsRescueAvailable || UiPauseGate.IsHeld)
            {
                return;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.rKey.wasPressedThisFrame)
            {
                // R은 팝업을 다시 여는 입력일 뿐이다. 구출 확정은 팝업의 버튼만 한다.
                OpenPanel();
            }

            if (!IsPanelOpen
                && !pendingReopen
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
                view.SetFollowTarget(player);
                view.SetIconResolver(ResolveIcon);
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

            if (view != null)
            {
                view.SetFollowTarget(null);
                // 씬 전환·재바인딩 때 이전 서비스를 가리키는 버튼 이벤트를 남기지 않는다.
                view.Bind(null, null, null);
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

            if (view.IsClosing)
            {
                // 닫는 도중 재열기 입력: 전환을 끊지 않고 닫기가 끝난 직후 한 번만 다시 연다.
                pendingReopen = true;
                return;
            }

            if (view.IsOpen)
            {
                // 등장 중이거나 이미 열림: 중복 생성·재생 없이 무시한다.
                return;
            }

            initialPopupShown = true;
            closedAt = -1f;
            pendingReopen = false;
            // 안내 버튼은 키캡 눌림 피드백과 함께 짧게 사라지지만, 팝업 열기는 그것을 기다리지 않는다.
            view.DismissChip(true);
            view.SetInteractable(true);
            // 다시 열 때마다 현재 비용을 새로 읽어 표시한다.
            view.Show(service.GetCurrentCost());
        }

        public void ClosePanel()
        {
            if (view == null || !view.IsOpen)
            {
                return;
            }

            pendingReopen = false;
            view.BeginClose(OnPopupClosed);
        }

        // 강한 글리치 종료 연출이 끝난 뒤에만 플레이어 위 안내를 보여 준다.
        private void OnPopupClosed()
        {
            if (view == null)
            {
                return;
            }

            if (!IsRescueAvailable)
            {
                pendingReopen = false;
                HideAll();
                return;
            }

            if (pendingReopen)
            {
                pendingReopen = false;
                OpenPanel();
                return;
            }

            view.SetChipVisible(true);
            closedAt = Time.unscaledTime;
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

            // 이미 한 번 보여 준 고갈 상황: 팝업을 자동으로 다시 열지 않고, 팝업이 없을 때만 안내 버튼을 둔다.
            if (view != null && !view.IsPopupVisible)
            {
                view.SetChipVisible(true);
            }
        }

        private void TryRescue()
        {
            // 등장·종료 도중이나 같은 입력으로 막 열린 팝업에서는 확정하지 않는다. 확정은 안정된 팝업의 버튼뿐이다.
            if (!IsRescueAvailable || view == null || !view.IsStable)
            {
                return;
            }

            if (!HasElevatorDestination())
            {
                view.SetMessage("엘리베이터 위치를 찾지 못했습니다. 잠시 후 다시 시도해 주세요.");
                return;
            }

            // 표시한 비용과 지금 적용될 비용이 다르면 결제하지 않고 최신 값을 먼저 보여 준다.
            EmergencyRescueCost current = service.GetCurrentCost();
            if (!EmergencyRescueCostRows.AreSame(view.DisplayedCost, current))
            {
                view.Show(current, "화물 상태가 변경되었습니다. 최신 비용을 다시 확인해 주세요.");
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
            pendingReopen = false;
            HideAll();
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
            pendingReopen = false;
            closedAt = -1f;
            HideAll();
        }

        // 더 이상 필요 없는 안내를 연출 없이 즉시 치운다(대기 중인 닫기 콜백도 버려진다).
        private void HideAll()
        {
            if (view != null)
            {
                view.HideImmediate();
                view.SetChipVisible(false);
            }
        }

        private static Sprite ResolveIcon(string resourceId)
        {
            GameBootstrapper bootstrap = GameBootstrapper.Instance;
            var catalog = bootstrap != null ? bootstrap.AssignedCatalog as GameDataCatalog : null;
            if (catalog == null || string.IsNullOrEmpty(resourceId))
            {
                return null;
            }

            return catalog.TryGetInventoryItem(resourceId, out MineralData data) && data != null
                ? data.Icon
                : null;
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

using System.Collections.Generic;
using SubTerra.App.Run;
using SubTerra.Shared;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.UI.EmergencyEscape
{
    /// <summary>긴급 탈출 선택 창 입력과 Runtime Bridge를 연결한다.</summary>
    public sealed class EmergencyEscapePanelBinder : MonoBehaviour
    {
        [SerializeField] private EmergencyEscapePanelView view;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button closeButton;

        private IEmergencyEscapePortalPort port;
        private IEmergencyEscapeDestinationPreview preview;
        private IReadOnlyList<EmergencyEscapeDestinationOption> options =
            System.Array.Empty<EmergencyEscapeDestinationOption>();
        private bool busy;

        public bool IsOpen
        {
            get
            {
                if (view == null)
                {
                    return false;
                }

                return view.IsOpen;
            }
        }

        private void Awake()
        {
            if (view == null)
            {
                view = GetComponent<EmergencyEscapePanelView>();
            }

            WireButtons();
            view?.SetVisible(false);
        }

        private void OnEnable()
        {
            WireButtons();
            WireDestinationEvents();
        }

        private void OnDisable()
        {
            UnwireButtons();
            UnwireDestinationEvents();
        }

        public void BindTo(IEmergencyEscapePortalPort escapePort)
        {
            port = escapePort;
            preview = escapePort as IEmergencyEscapeDestinationPreview;
        }

        /// <summary>목적지 목록과 비용을 채운 뒤 패널을 연다. 엘리베이터를 기본 선택한다.</summary>
        public void Open(
            IReadOnlyList<EmergencyEscapeDestinationOption> destinationOptions,
            EmergencyEscapeCost cost)
        {
            options = destinationOptions
                ?? System.Array.Empty<EmergencyEscapeDestinationOption>();
            if (view == null)
            {
                return;
            }

            view.SetDestinations(options, 0);
            view.SetCost(cost.Gold, cost.Energy);
            view.SetResult(string.Empty, false);
            view.SetBusy(false);
            busy = false;
            view.SetVisible(true);
            // 기본 선택(엘리베이터)부터 위치를 보여 준다.
            PreviewDestinationAt(view.SelectedDestinationIndex);
        }

        public void Close()
        {
            busy = false;
            preview?.ClearDestinationPreview();
            view?.SetBusy(false);
            view?.SetVisible(false);
        }

        /// <summary>드롭다운 선택 변경 시 해당 목적지로 시점을 옮긴다.</summary>
        public void PreviewDestinationAt(int index)
        {
            if (preview == null || options == null || options.Count == 0)
            {
                return;
            }

            if (index < 0 || index >= options.Count)
            {
                index = 0;
            }

            var selected = options[index];
            preview.TryPreviewDestination(selected.Kind, selected.InstanceId, out _);
        }

        public void ConfirmSelectedDestination()
        {
            if (busy)
            {
                return;
            }

            if (port == null)
            {
                view?.SetResult("긴급 탈출 경로가 준비되지 않았습니다.", true);
                return;
            }

            if (options == null || options.Count == 0)
            {
                view?.SetResult("이동할 목적지가 없습니다.", true);
                return;
            }

            var index = view != null ? view.SelectedDestinationIndex : 0;
            if (index < 0 || index >= options.Count)
            {
                index = 0;
            }

            var selected = options[index];
            busy = true;
            view?.SetBusy(true);
            try
            {
                var success = port.TryEscapeTo(
                    selected.Kind,
                    selected.InstanceId,
                    out var reason);
                view?.SetResult(reason, !success);
                if (success)
                {
                    Close();
                }
            }
            finally
            {
                busy = false;
                view?.SetBusy(false);
            }
        }

        private void WireButtons()
        {
            if (confirmButton != null)
            {
                confirmButton.onClick.RemoveListener(ConfirmSelectedDestination);
                confirmButton.onClick.AddListener(ConfirmSelectedDestination);
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(Close);
                closeButton.onClick.AddListener(Close);
            }
        }

        private void UnwireButtons()
        {
            if (confirmButton != null)
            {
                confirmButton.onClick.RemoveListener(ConfirmSelectedDestination);
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(Close);
            }
        }

        private void WireDestinationEvents()
        {
            if (view == null)
            {
                return;
            }

            view.DestinationSelected -= HandleDestinationSelected;
            view.DestinationSelected += HandleDestinationSelected;
        }

        private void UnwireDestinationEvents()
        {
            if (view == null)
            {
                return;
            }

            view.DestinationSelected -= HandleDestinationSelected;
        }

        private void HandleDestinationSelected(int index)
        {
            PreviewDestinationAt(index);
        }
    }
}

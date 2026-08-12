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

                var root = view.gameObject;
                return root.activeInHierarchy;
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
        }

        private void OnDisable()
        {
            UnwireButtons();
        }

        public void BindTo(IEmergencyEscapePortalPort escapePort)
        {
            port = escapePort;
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
        }

        public void Close()
        {
            busy = false;
            view?.SetBusy(false);
            view?.SetVisible(false);
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
    }
}

using SubTerra.Gameplay.Player;
using SubTerra.Gameplay.Power;
using SubTerra.Shared;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SubTerra.Gameplay.Building
{
    /// <summary>통과 가능한 포탈의 E 입력과 전력 상태만 소유한다. 목적 선택 UI는 App 포트가 연다.</summary>
    [RequireComponent(typeof(Collider2D), typeof(PowerNode))]
    public sealed class EmergencyEscapePortal : MonoBehaviour
    {
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private string interactActionPath = "Player/Interact";
        [SerializeField] private PowerNode powerNode;

        private InputAction interactAction;
        private PlayerMovement rider;
        private IEmergencyEscapePortalPort escapePort;
        private int lastRequestFrame = -1;

        public bool HasRider => rider != null;
        public bool IsPowered => powerNode != null && powerNode.IsPowered;
        public string LastReason { get; private set; } = string.Empty;

        private void Awake()
        {
            var zone = GetComponent<Collider2D>();
            zone.isTrigger = true;
            if (powerNode == null)
            {
                powerNode = GetComponent<PowerNode>();
            }

            ResolveInput();
            ResolvePort();
        }

        private void OnEnable()
        {
            ResolveInput();
            if (interactAction != null)
            {
                interactAction.started += OnInteractStarted;
                interactAction.Enable();
            }
        }

        private void OnDisable()
        {
            if (interactAction != null)
            {
                interactAction.started -= OnInteractStarted;
                interactAction.Disable();
            }

            rider = null;
        }

        private void Update()
        {
            if (interactAction != null && interactAction.WasPressedThisFrame())
            {
                RequestEscape();
            }
        }

        private void OnTriggerEnter2D(Collider2D other) => TryAcquireRider(other);

        private void OnTriggerStay2D(Collider2D other)
        {
            if (rider == null)
            {
                TryAcquireRider(other);
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (rider != null && other.GetComponentInParent<PlayerMovement>() == rider)
            {
                rider = null;
            }
        }

        private void OnInteractStarted(InputAction.CallbackContext _) => RequestEscape();

        /// <summary>탑승·전력 조건을 통과하면 목적지 선택 패널을 연다.</summary>
        public bool RequestEscape()
        {
            if (lastRequestFrame == Time.frameCount)
            {
                return false;
            }

            lastRequestFrame = Time.frameCount;
            if (rider == null)
            {
                LastReason = "포탈 안에서만 사용할 수 있습니다.";
                return false;
            }

            if (!IsPowered)
            {
                LastReason = "전력 30이 연결되어야 사용할 수 있습니다.";
                return false;
            }

            ResolvePort();
            if (escapePort == null)
            {
                LastReason = "긴급 탈출 경로가 준비되지 않았습니다.";
                return false;
            }

            var success = escapePort.TryOpenEscapePanel(out var reason);
            LastReason = reason ?? string.Empty;
            return success;
        }

        private void TryAcquireRider(Collider2D other)
        {
            var movement = other != null ? other.GetComponentInParent<PlayerMovement>() : null;
            if (movement != null)
            {
                rider = movement;
            }
        }

        private void ResolveInput()
        {
            if (interactAction == null && inputActions != null)
            {
                interactAction = inputActions.FindAction(interactActionPath, false);
            }
        }

        private void ResolvePort()
        {
            if (escapePort != null)
            {
                return;
            }

            var behaviours = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude);
            for (var i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IEmergencyEscapePortalPort port)
                {
                    escapePort = port;
                    return;
                }
            }
        }
    }
}

using SubTerra.Gameplay.Player;
using SubTerra.Gameplay.Power;
using SubTerra.Shared;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SubTerra.Gameplay.Building
{
    /// <summary>
    /// 통과 가능한 포탈의 E 입력·근접 탑승만 소유한다.
    /// 목적지 선택 UI·결제는 App 포트가 처리한다.
    /// </summary>
    [RequireComponent(typeof(Collider2D), typeof(PowerNode))]
    public sealed class EmergencyEscapePortal : MonoBehaviour
    {
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private string interactActionPath = "Player/Interact";
        [SerializeField] private PowerNode powerNode;
        [SerializeField, Min(0.5f)] private float interactionRadius = 2.5f;

        private InputAction interactAction;
        private PlayerMovement rider;
        private IEmergencyEscapePortalPort escapePort;
        private int lastRequestFrame = -1;

        public bool HasRider => rider != null;

        /// <summary>
        /// 카탈로그 전력 수요 30은 건설 정의용이다.
        /// 런타임 사용은 전력망에 등록된 시설이면 허용한다.
        /// (전진기지 공급 5 &lt; 수요 30이라 용량 기반 PowerNode.IsPowered만으로는 패널이 영구 차단된다.)
        /// </summary>
        public bool IsPowered
        {
            get
            {
                EnsurePowerNode();
                return HasPowerService;
            }
        }

        public string LastReason { get; private set; } = string.Empty;

        private bool HasPowerService =>
            powerNode != null && powerNode.Network != null;

        private void EnsurePowerNode()
        {
            if (powerNode == null)
            {
                powerNode = GetComponent<PowerNode>();
            }
        }

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
                // 공용 Interact 액션을 Disable 하지 않는다. 다른 시설·플레이어 입력을 끊지 않기 위함.
                interactAction.started += OnInteractStarted;
                if (!interactAction.enabled)
                {
                    interactAction.Enable();
                }
            }
        }

        private void OnDisable()
        {
            if (interactAction != null)
            {
                interactAction.started -= OnInteractStarted;
            }

            rider = null;
        }

        private void Update()
        {
            if (WasInteractPressedThisFrame())
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

        /// <summary>포탈 근처에서 E 입력을 받으면 목적지 선택 패널을 연다.</summary>
        public bool RequestEscape()
        {
            if (lastRequestFrame == Time.frameCount)
            {
                return false;
            }

            lastRequestFrame = Time.frameCount;

            if (rider == null)
            {
                TryAcquireRiderFromOverlap();
            }

            if (rider == null)
            {
                LastReason = "포탈 근처에서만 사용할 수 있습니다.";
                return false;
            }

            EnsurePowerNode();
            if (!HasPowerService)
            {
                LastReason = "전력망에 연결된 포탈만 사용할 수 있습니다.";
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

        private bool WasInteractPressedThisFrame()
        {
            if (interactAction != null && interactAction.WasPressedThisFrame())
            {
                return true;
            }

            // InputAction이 비활성·미배선이어도 E 탭은 받도록 폴백한다.
            var keyboard = Keyboard.current;
            return keyboard != null && keyboard.eKey.wasPressedThisFrame;
        }

        private void TryAcquireRider(Collider2D other)
        {
            var movement = other != null ? other.GetComponentInParent<PlayerMovement>() : null;
            if (movement != null)
            {
                rider = movement;
            }
        }

        /// <summary>
        /// Trigger Enter를 놓친 경우·콜라이더 가장자리 근처에서도
        /// 포탈 반경 안의 플레이어를 탑승자로 인식한다.
        /// </summary>
        private void TryAcquireRiderFromOverlap()
        {
            var zone = GetComponent<Collider2D>();
            if (zone != null)
            {
                var filter = new ContactFilter2D
                {
                    useTriggers = true,
                    useLayerMask = false
                };
                var hits = new Collider2D[12];
                var count = zone.Overlap(filter, hits);
                for (var i = 0; i < count; i++)
                {
                    if (hits[i] == null)
                    {
                        continue;
                    }

                    TryAcquireRider(hits[i]);
                    if (rider != null)
                    {
                        return;
                    }
                }
            }

            // 트리거 바깥이지만 "근처"에 있는 플레이어도 허용한다.
            var radius = Mathf.Max(0.5f, interactionRadius);
            var nearby = Physics2D.OverlapCircleAll(transform.position, radius);
            for (var i = 0; i < nearby.Length; i++)
            {
                TryAcquireRider(nearby[i]);
                if (rider != null)
                {
                    return;
                }
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

            // 비활성 오브젝트에 붙은 브리지도 포함해 패널 경로를 찾는다.
            var behaviours = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include);
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

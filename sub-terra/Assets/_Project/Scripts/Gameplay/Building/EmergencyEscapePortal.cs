using System.Collections.Generic;
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

        private static readonly List<EmergencyEscapePortal> ActivePortals = new List<EmergencyEscapePortal>();

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

        /// <summary>
        /// 캐릭터 판정 영역이 포탈 영역 안에 완전히 들어가 있는지. 걸치면 false라 다른 시설이 우선한다.
        /// </summary>
        public bool IsPlayerFullyInside(PlayerMovement player)
        {
            var zone = GetComponent<Collider2D>();
            if (zone == null || player == null || !player.TryGetBodyBounds(out var body))
            {
                return false;
            }

            return EscapePortalContainment.IsFullyInside(body, zone.bounds);
        }

        /// <summary>공용 Interact 입력에서 다른 시설보다 포탈이 먼저 처리되어야 하는지(완전 포함일 때만 true).</summary>
        public bool TryClaimInteractionPriority()
        {
            if (rider == null)
            {
                TryAcquireRiderFromOverlap();
            }

            return IsPlayerFullyInside(rider);
        }

        /// <summary>활성 포탈 중 하나라도 플레이어를 완전히 품고 있으면 true.</summary>
        public static bool AnyClaimsInteractionPriority()
        {
            for (var i = 0; i < ActivePortals.Count; i++)
            {
                var portal = ActivePortals[i];
                if (portal != null && portal.TryClaimInteractionPriority())
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>이 시설이 포탈이면 플레이어가 완전히 들어가 있을 때만 대상으로 본다. 포탈이 아니면 false.</summary>
        public static bool IsPortalTarget(BuildingInstance instance, PlayerMovement player)
        {
            if (instance == null)
            {
                return false;
            }

            var portal = instance.GetComponent<EmergencyEscapePortal>();
            return portal != null && portal.IsPlayerFullyInside(player);
        }

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
            if (!ActivePortals.Contains(this))
            {
                ActivePortals.Add(this);
            }

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
            ActivePortals.Remove(this);
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

            // 캐릭터가 포탈 영역 안에 완전히 들어가야만 포탈이 대상이 된다. 걸치면 다른 시설이 우선한다.
            if (rider == null || !IsPlayerFullyInside(rider))
            {
                LastReason = "포탈 안으로 완전히 들어가야 사용할 수 있습니다.";
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

        /// <summary>Trigger Enter를 놓친 경우에도 포탈 영역과 겹친 플레이어를 탑승자로 인식한다.</summary>
        private void TryAcquireRiderFromOverlap()
        {
            var zone = GetComponent<Collider2D>();
            if (zone == null)
            {
                return;
            }

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

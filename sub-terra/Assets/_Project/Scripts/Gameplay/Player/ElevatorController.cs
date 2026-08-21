using System.Collections;
using SubTerra.Shared;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SubTerra.Gameplay.Player
{
    /// <summary>
    /// Mine 정거장의 탑승·입력·물리 잠금을 맡는다. 전력과 Scene 전환은 App 포트에 위임한다.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class ElevatorController : MonoBehaviour
    {
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private string interactActionPath = "Player/Interact";
        [SerializeField] private ElevatorDestination destination = ElevatorDestination.SurfaceBase;
        [SerializeField] private Transform boardingAnchor;
        [SerializeField] private Transform safeExitPoint;
        [SerializeField] private Vector2 safeExitSize = new(0.8f, 1.2f);
        [SerializeField] private LayerMask exitBlockerLayers;
        [SerializeField] private TMP_Text statusText;
        [SerializeField, Min(0f)] private float callDelaySeconds = 0.35f;
        [SerializeField, Min(0f)] private float travelDelaySeconds = 0.65f;

        private InputAction interactAction;
        private PlayerMovement riderMovement;
        private Rigidbody2D riderBody;
        private RigidbodyType2D riderBodyType;
        private float riderGravity;
        private bool riderLocked;
        private IElevatorTravelPort travelPort;
        private Coroutine travelRoutine;

        public ElevatorTravelState State { get; private set; } = ElevatorTravelState.Idle;
        public bool HasRider => riderMovement != null;

        /// <summary>공용 Interact 입력에서 시설 UI보다 엘리베이터 이동이 먼저 처리되어야 하는지 확인한다.</summary>
        public bool TryClaimInteractionPriority()
        {
            if (State == ElevatorTravelState.Calling || State == ElevatorTravelState.Moving)
            {
                return true;
            }

            if (riderMovement == null)
            {
                TryAcquireRiderFromOverlap();
            }

            return riderMovement != null;
        }

        private void Awake()
        {
            var zone = GetComponent<Collider2D>();
            zone.isTrigger = true;
            ResolveInput();
            ResolvePort();
            SetState(ElevatorTravelState.Idle);
        }

        private void OnEnable()
        {
            ResolveInput();
            if (interactAction != null)
            {
                // InputSystem_Actions의 Interact는 Hold interaction이라
                // performed는 길게 눌러야만 발생한다. 탭은 started/WasPressedThisFrame로 받는다.
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

            if (travelRoutine != null)
            {
                StopCoroutine(travelRoutine);
                travelRoutine = null;
            }

            ReleaseRider();
        }

        private void Update()
        {
            // started 콜백과 이중 안전장치. Hold interaction에서도 누른 프레임에 반응.
            if (interactAction != null && interactAction.WasPressedThisFrame())
            {
                RequestTravel();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryAcquireRider(other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            // 콜라이더 크기 변경·스폰 직후 등 Enter를 놓친 경우에도 탑승 인식.
            if (riderMovement == null)
            {
                TryAcquireRider(other);
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (riderMovement == null
                || other.GetComponentInParent<PlayerMovement>() != riderMovement
                || State == ElevatorTravelState.Calling
                || State == ElevatorTravelState.Moving)
            {
                return;
            }

            riderMovement = null;
            riderBody = null;
            RefreshStatus();
        }

        private void OnInteractStarted(InputAction.CallbackContext _)
        {
            RequestTravel();
        }

        public bool RequestTravel()
        {
            if (riderMovement == null)
            {
                TryAcquireRiderFromOverlap();
            }

            if (riderMovement == null
                || State == ElevatorTravelState.Calling
                || State == ElevatorTravelState.Moving)
            {
                return false;
            }

            if (!IsExitClear())
            {
                SetState(ElevatorTravelState.Blocked);
                return false;
            }

            ResolvePort();
            if (travelPort == null)
            {
                SetState(ElevatorTravelState.Blocked);
                return false;
            }

            LockRider();
            SetState(ElevatorTravelState.Calling);
            travelRoutine = StartCoroutine(TravelRoutine());
            return true;
        }

        private IEnumerator TravelRoutine()
        {
            if (callDelaySeconds > 0f)
            {
                yield return new WaitForSecondsRealtime(callDelaySeconds);
            }

            SetState(ElevatorTravelState.Moving);
            if (travelDelaySeconds > 0f)
            {
                yield return new WaitForSecondsRealtime(travelDelaySeconds);
            }

            if (!travelPort.TryTravel(destination, out var reason))
            {
                SetState(ElevatorTravelState.Blocked);
                if (statusText != null && !string.IsNullOrWhiteSpace(reason))
                {
                    statusText.text = "Blocked · " + reason;
                }
                ReleaseRider();
                travelRoutine = null;
                yield break;
            }

            SetState(ElevatorTravelState.Arrived);
            ReleaseRider();
            travelRoutine = null;
        }

        private void LockRider()
        {
            riderMovement.SetCanMove(false);
            riderLocked = true;
            if (riderBody == null)
            {
                return;
            }

            riderBodyType = riderBody.bodyType;
            riderGravity = riderBody.gravityScale;
            riderBody.linearVelocity = Vector2.zero;
            riderBody.angularVelocity = 0f;
            riderBody.bodyType = RigidbodyType2D.Kinematic;
            riderBody.gravityScale = 0f;
            if (boardingAnchor != null)
            {
                riderBody.position = boardingAnchor.position;
            }
        }

        private void ReleaseRider()
        {
            if (!riderLocked)
            {
                return;
            }

            if (riderBody != null)
            {
                riderBody.bodyType = riderBodyType;
                riderBody.gravityScale = riderGravity;
                riderBody.linearVelocity = Vector2.zero;
            }

            riderMovement?.SetCanMove(true);
            riderLocked = false;
        }

        private bool IsExitClear()
        {
            if (safeExitPoint == null || exitBlockerLayers.value == 0)
            {
                return true;
            }

            return Physics2D.OverlapBox(
                safeExitPoint.position,
                safeExitSize,
                0f,
                exitBlockerLayers) == null;
        }

        private void TryAcquireRider(Collider2D other)
        {
            var movement = other.GetComponentInParent<PlayerMovement>();
            if (movement == null)
            {
                return;
            }

            riderMovement = movement;
            riderBody = movement.GetComponent<Rigidbody2D>();
            if (State == ElevatorTravelState.Arrived || State == ElevatorTravelState.Blocked)
            {
                SetState(ElevatorTravelState.Idle);
            }
            else
            {
                RefreshStatus();
            }
        }

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
            var hits = new Collider2D[8];
            var count = zone.Overlap(filter, hits);
            for (var i = 0; i < count; i++)
            {
                if (hits[i] == null)
                {
                    continue;
                }

                TryAcquireRider(hits[i]);
                if (riderMovement != null)
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
            if (travelPort != null)
            {
                return;
            }

            var behaviours = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude);
            for (var index = 0; index < behaviours.Length; index++)
            {
                if (behaviours[index] is IElevatorTravelPort port)
                {
                    travelPort = port;
                    if (State == ElevatorTravelState.Idle
                        && port.State == ElevatorTravelState.Arrived)
                    {
                        SetState(ElevatorTravelState.Arrived);
                    }
                    return;
                }
            }
        }

        private void SetState(ElevatorTravelState state)
        {
            State = state;
            RefreshStatus();
        }

        private void RefreshStatus()
        {
            if (statusText == null)
            {
                return;
            }

            statusText.text = State switch
            {
                ElevatorTravelState.Idle when HasRider => "Idle · E 귀환",
                ElevatorTravelState.Idle => "Idle · 탑승 대기",
                ElevatorTravelState.Calling => "Calling · 문 닫는 중",
                ElevatorTravelState.Moving => "Moving · 지상 이동 중",
                ElevatorTravelState.Arrived when HasRider => "Arrived · E 귀환",
                ElevatorTravelState.Arrived => "Arrived · Mine 도착",
                _ => "Blocked · 이동 불가"
            };
        }

        private void OnDrawGizmosSelected()
        {
            if (safeExitPoint == null)
            {
                return;
            }

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(safeExitPoint.position, safeExitSize);
        }
    }
}

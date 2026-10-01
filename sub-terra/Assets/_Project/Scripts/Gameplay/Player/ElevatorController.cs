using System;
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
        [SerializeField, Min(0f)] private float travelDelaySeconds = 0.22f;

        private InputAction interactAction;
        private ElevatorDoorVisual doorVisual;
        private PlayerMovement riderMovement;
        private Rigidbody2D riderBody;
        private RigidbodyType2D riderBodyType;
        private float riderGravity;
        private bool riderLocked;
        private bool travelCommitted;
        private PlayerCameraFollow departureCameraFollow;
        private IElevatorTravelPort travelPort;
        private Coroutine travelRoutine;

        public ElevatorTravelState State { get; private set; } = ElevatorTravelState.Idle;
        public event Action<ElevatorTravelState> StateChanged;
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
            doorVisual = GetComponent<ElevatorDoorVisual>();
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

            // Scene 전환이 이미 승인된 경우 플레이어와 객실은 함께 언로드된다.
            // 이 시점에 탑승자를 원위치에 복구하면 상승 연출이 되감겨 보인다.
            if (travelCommitted)
            {
                RestoreCameraFollow();
                return;
            }

            ReleaseRider();
            if (State == ElevatorTravelState.Calling || State == ElevatorTravelState.Moving)
            {
                SetState(ElevatorTravelState.Idle);
            }
        }

        private void Update()
        {
            // started 콜백과 이중 안전장치. Hold interaction에서도 누른 프레임에 반응.
            if (interactAction != null && interactAction.WasPressedThisFrame())
            {
                RequestTravel();
            }
        }

        private void LateUpdate()
        {
            if (!riderLocked || riderBody == null || boardingAnchor == null)
            {
                return;
            }

            // 엘리베이터 객실과 동일한 위치를 유지해 이동 중에도 발이 바닥에 붙게 한다.
            riderBody.position = boardingAnchor.position;
            riderBody.rotation = boardingAnchor.eulerAngles.z;
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
            bool animateCabin = doorVisual != null && doorVisual.isActiveAndEnabled;
            if (animateCabin)
            {
                while (!doorVisual.IsLiftAtTop)
                {
                    yield return null;
                }
            }
            else if (travelDelaySeconds > 0f)
            {
                // 비주얼이 없는 테스트/대체 엘리베이터도 기존 이동 대기 시간을 유지한다.
                yield return new WaitForSecondsRealtime(travelDelaySeconds);
            }

            if (animateCabin && travelDelaySeconds > 0f)
            {
                // 상승이 끝난 뒤 짧게 정지해 문과 승강기 동작을 읽을 시간을 준다.
                yield return new WaitForSecondsRealtime(travelDelaySeconds);
            }

            travelCommitted = true;
            if (!travelPort.TryTravel(destination, out var reason))
            {
                travelCommitted = false;
                if (animateCabin)
                {
                    doorVisual.ReturnCabinToGround();
                    while (!doorVisual.IsLiftAtGround)
                    {
                        yield return null;
                    }
                }

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
            // 성공 시 기존 씬의 엘리베이터와 탑승자는 언로드까지 상단에서 유지한다.
            travelRoutine = null;
        }

        private void LockRider()
        {
            riderMovement.SetCanMove(false);
            riderLocked = true;
            Camera travelCamera = Camera.main;
            if (doorVisual != null && doorVisual.isActiveAndEnabled && travelCamera != null)
            {
                var follow = travelCamera.GetComponent<PlayerCameraFollow>();
                if (follow != null && follow.enabled)
                {
                    // 탑승자를 추적하면 객실이 계속 화면 중앙에 남으므로 출발 구도를 유지한다.
                    departureCameraFollow = follow;
                    departureCameraFollow.enabled = false;
                }
            }
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
            RestoreCameraFollow();
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

        private void RestoreCameraFollow()
        {
            if (departureCameraFollow != null)
            {
                departureCameraFollow.enabled = true;
                departureCameraFollow = null;
            }
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
            bool changed = State != state;
            State = state;
            RefreshStatus();
            if (changed)
            {
                StateChanged?.Invoke(state);
            }
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

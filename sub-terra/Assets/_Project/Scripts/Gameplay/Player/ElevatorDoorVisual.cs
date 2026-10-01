using SubTerra.Shared;
using UnityEngine;

namespace SubTerra.Gameplay.Player
{
    /// <summary>Animates the two front shutters without owning travel or boarding rules.</summary>
    [RequireComponent(typeof(ElevatorController))]
    public sealed class ElevatorDoorVisual : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer leftDoor;
        [SerializeField] private SpriteRenderer rightDoor;
        [SerializeField] private SpriteMask doorOpeningMask;
        [SerializeField] private Transform cabinRoot;
        [SerializeField, Min(0.01f)] private float slideDuration = 0.28f;
        [SerializeField, Min(0.01f)] private float openingDuration = 0.52f;
        [SerializeField, Min(0f)] private float initialOpenDelay = 0.16f;
        [SerializeField, Min(0f)] private float departurePause = 0.18f;
        [SerializeField, Min(0.01f)] private float liftDuration = 0.55f;
        [SerializeField, Min(0f)] private float liftDistance = 1.8f;
        [SerializeField] private bool liftToExit = true;
        [SerializeField] private float openDoorOffset = 1.05f;
        [SerializeField] private float closedDoorOffset = 0.34f;
        [SerializeField] private float doorCenterY = -0.55f;
        [SerializeField] private Vector2 doorScale = new(1.9f, 0.87f);
        [SerializeField] private Vector2 openingMaskSize = new(1.36f, 1.16f);

        private ElevatorController elevator;
        private float closedFraction;
        private float targetFraction;
        private float transitionStartFraction;
        private float transitionElapsed;
        private float transitionDuration;
        private float openingDelayRemaining;
        private bool transitionActive;
        private float liftFraction;
        private float liftTargetFraction;
        private float liftStartFraction;
        private float liftElapsed;
        private float liftTransitionDuration;
        private bool liftTransitionActive;
        private float departurePauseRemaining;
        private bool liftStartPending;
        private float departureDistance;
        private float departureDuration;

        public float ClosedFraction => closedFraction;
        public float LiftFraction => liftFraction;
        public Transform CabinRoot => cabinRoot;
        public bool IsLiftAtTop => !liftTransitionActive
            && !liftStartPending
            && liftFraction >= 0.999f;
        public bool IsLiftAtGround => !liftTransitionActive
            && !liftStartPending
            && liftFraction <= 0.001f;

        private void OnEnable()
        {
            elevator = GetComponent<ElevatorController>();
            elevator.StateChanged += OnStateChanged;
            ApplyGeometry();
            departureDistance = liftDistance;
            departureDuration = liftDuration;
            SnapLiftTo(0f);

            if (ShouldClose(elevator.State))
            {
                SnapTo(1f);
                return;
            }

            // 정거장 진입 시 닫힌 문을 먼저 보여 준 뒤 열어, 도착 연출이
            // 순간적으로 열린 모습으로 튀지 않도록 한다.
            closedFraction = 1f;
            targetFraction = 0f;
            openingDelayRemaining = initialOpenDelay;
            transitionActive = false;
            ApplyPose();
            if (openingDelayRemaining <= 0f)
            {
                BeginTransition(0f);
            }
        }

        private void OnDisable()
        {
            if (elevator != null)
            {
                elevator.StateChanged -= OnStateChanged;
            }
        }

        private void Update()
        {
            UpdateDoor();
            UpdateLift();
        }

        private void UpdateDoor()
        {
            if (openingDelayRemaining > 0f)
            {
                openingDelayRemaining -= Time.unscaledDeltaTime;
                if (openingDelayRemaining > 0f)
                {
                    return;
                }

                BeginTransition(targetFraction);
            }

            if (!transitionActive)
            {
                return;
            }

            transitionElapsed += Time.unscaledDeltaTime;
            float progress = transitionDuration <= 0f
                ? 1f
                : Mathf.Clamp01(transitionElapsed / transitionDuration);
            float eased = SmootherStep(progress);
            closedFraction = Mathf.Lerp(transitionStartFraction, targetFraction, eased);
            ApplyPose();

            if (progress >= 1f)
            {
                closedFraction = targetFraction;
                transitionActive = false;
                ApplyPose();
            }
        }

        private void UpdateLift()
        {
            if (liftStartPending)
            {
                if (closedFraction < 0.999f)
                {
                    return;
                }

                departurePauseRemaining -= Time.unscaledDeltaTime;
                if (departurePauseRemaining <= 0f)
                {
                    liftStartPending = false;
                    BeginLift(1f);
                }

                return;
            }

            if (!liftTransitionActive)
            {
                return;
            }

            liftElapsed += Time.unscaledDeltaTime;
            float progress = liftTransitionDuration <= 0f
                ? 1f
                : Mathf.Clamp01(liftElapsed / liftTransitionDuration);
            float eased = progress * progress * progress;
            liftFraction = Mathf.Lerp(liftStartFraction, liftTargetFraction, eased);
            ApplyLiftPose();

            if (progress >= 1f)
            {
                liftFraction = liftTargetFraction;
                liftTransitionActive = false;
                ApplyLiftPose();
            }
        }

        private void OnStateChanged(ElevatorTravelState state)
        {
            openingDelayRemaining = 0f;
            BeginTransition(ShouldClose(state) ? 1f : 0f);

            if (state == ElevatorTravelState.Moving)
            {
                ScheduleDepartureLift();
            }
            else if (state == ElevatorTravelState.Idle && IsLiftAtGround)
            {
                BeginTransition(0f);
            }
        }

        private void ScheduleDepartureLift()
        {
            ResolveDepartureEndpoint();
            liftTargetFraction = 1f;
            liftStartPending = departurePause > 0f;
            departurePauseRemaining = departurePause;
            if (!liftStartPending)
            {
                BeginLift(1f);
            }
        }

        private void ResolveDepartureEndpoint()
        {
            departureDistance = liftDistance;
            departureDuration = liftDuration;
            if (!liftToExit || cabinRoot == null)
            {
                return;
            }

            // 종점은 레일 끝과 출발 화면 상단 중 더 높은 곳이다.
            // 객실 바닥까지 화면 밖으로 나간 뒤에만 Scene 전환을 승인한다.
            float topY = cabinRoot.position.y;
            Transform rails = transform.Find("HoistRails");
            if (rails != null)
            {
                foreach (SpriteRenderer renderer in rails.GetComponentsInChildren<SpriteRenderer>())
                {
                    if (renderer.enabled && renderer.sprite != null)
                    {
                        topY = Mathf.Max(topY, renderer.bounds.max.y);
                    }
                }
            }

            Camera travelCamera = Camera.main;
            if (travelCamera != null)
            {
                float depth = travelCamera.WorldToViewportPoint(cabinRoot.position).z;
                topY = Mathf.Max(topY,
                    travelCamera.ViewportToWorldPoint(new Vector3(0.5f, 1f, depth)).y);
            }

            float bottomY = cabinRoot.position.y;
            foreach (SpriteRenderer renderer in cabinRoot.GetComponentsInChildren<SpriteRenderer>())
            {
                if (renderer.sprite != null)
                {
                    bottomY = Mathf.Min(bottomY, renderer.bounds.min.y);
                }
            }

            Vector3 endpoint = cabinRoot.position;
            endpoint.y = topY + (cabinRoot.position.y - bottomY) + 0.35f;
            departureDistance = Mathf.Max(liftDistance, transform.InverseTransformPoint(endpoint).y);
            departureDuration = Mathf.Max(liftDuration, Mathf.Clamp(departureDistance / 8f, 1.1f, 2.4f));
        }

        /// <summary>여행 실패 시 탑승자와 함께 객실을 출발 위치로 되돌린다.</summary>
        public void ReturnCabinToGround()
        {
            liftStartPending = false;
            departurePauseRemaining = 0f;
            BeginLift(0f);
        }

        private static bool ShouldClose(ElevatorTravelState state)
        {
            return state == ElevatorTravelState.Calling
                || state == ElevatorTravelState.Moving
                || state == ElevatorTravelState.Arrived;
        }

        private void BeginTransition(float target)
        {
            targetFraction = Mathf.Clamp01(target);
            float distance = Mathf.Abs(targetFraction - closedFraction);
            if (distance <= 0.0001f)
            {
                SnapTo(targetFraction);
                return;
            }

            transitionStartFraction = closedFraction;
            transitionElapsed = 0f;
            float fullDuration = targetFraction > closedFraction
                ? slideDuration
                : openingDuration;
            transitionDuration = Mathf.Max(0.01f, fullDuration * distance);
            transitionActive = true;
        }

        private void SnapTo(float target)
        {
            openingDelayRemaining = 0f;
            targetFraction = Mathf.Clamp01(target);
            closedFraction = targetFraction;
            transitionStartFraction = closedFraction;
            transitionElapsed = 0f;
            transitionDuration = 0f;
            transitionActive = false;
            ApplyPose();
        }

        private void BeginLift(float target)
        {
            liftTargetFraction = Mathf.Clamp01(target);
            float distance = Mathf.Abs(liftTargetFraction - liftFraction);
            if (distance <= 0.0001f || cabinRoot == null || departureDistance <= 0f)
            {
                SnapLiftTo(liftTargetFraction);
                return;
            }

            liftStartFraction = liftFraction;
            liftElapsed = 0f;
            liftTransitionDuration = Mathf.Max(0.01f, departureDuration * distance);
            liftTransitionActive = true;
        }

        private void SnapLiftTo(float target)
        {
            liftStartPending = false;
            departurePauseRemaining = 0f;
            liftTargetFraction = Mathf.Clamp01(target);
            liftFraction = liftTargetFraction;
            liftStartFraction = liftFraction;
            liftElapsed = 0f;
            liftTransitionDuration = 0f;
            liftTransitionActive = false;
            ApplyLiftPose();
        }

        private static float SmootherStep(float value)
        {
            float t = Mathf.Clamp01(value);
            return t * t * t * (t * (t * 6f - 15f) + 10f);
        }

        private void ApplyGeometry()
        {
            RefreshFixedShaftGeometry();
            if (cabinRoot == null)
            {
                cabinRoot = transform.Find("ElevatorCabin");
            }

            if (leftDoor != null)
            {
                leftDoor.transform.localScale = new Vector3(doorScale.x, doorScale.y, 1f);
            }

            if (rightDoor != null)
            {
                rightDoor.transform.localScale = new Vector3(doorScale.x, doorScale.y, 1f);
            }

            if (doorOpeningMask == null)
            {
                Transform maskTransform = transform.Find("DoorOpeningMask");
                if (maskTransform != null)
                {
                    doorOpeningMask = maskTransform.GetComponent<SpriteMask>();
                }
            }

            if (doorOpeningMask != null)
            {
                doorOpeningMask.transform.localPosition = new Vector3(0f, doorCenterY, 0f);
                doorOpeningMask.transform.localScale =
                    new Vector3(openingMaskSize.x, openingMaskSize.y, 1f);
            }
        }

        /// <summary>Keep the shaft attached to the ground while only the cabin travels.</summary>
        public void RefreshFixedShaftGeometry()
        {
            Transform rails = transform.Find("HoistRails");
            if (rails == null) return;
            Transform channel = rails.Find("LeftChannel");
            SpriteRenderer template = channel != null ? channel.GetComponent<SpriteRenderer>() : null;
            if (template == null || template.sprite == null) return;

            // The three protected ground cells end at local Y = -1.
            // Extend the existing rails downward without changing their upper endpoint.
            const float bottom = -1f;
            foreach (string side in new[] { "Left", "Right" })
            {
                foreach (string part in new[] { "Channel", "Face", "Highlight", "Cable" })
                {
                    Transform rail = rails.Find(side + part);
                    if (rail == null) continue;
                    SpriteRenderer renderer = rail.GetComponent<SpriteRenderer>();
                    if (renderer == null || renderer.sprite == null) continue;
                    float spriteHeight = renderer.sprite.bounds.size.y;
                    if (spriteHeight <= 0f) continue;
                    float top = rail.localPosition.y + spriteHeight * rail.localScale.y * 0.5f;
                    Vector3 position = rail.localPosition;
                    position.y = (top + bottom) * 0.5f;
                    rail.localPosition = position;
                    Vector3 scale = rail.localScale;
                    scale.y = (top - bottom) / spriteHeight;
                    rail.localScale = scale;
                }
            }

            FixedShaftPart(rails, template, "GroundPlate", new Vector2(0f, -0.96f),
                new Vector2(1.76f, 0.08f), new Color(0.20f, 0.23f, 0.26f), 2);
            FixedShaftPart(rails, template, "GroundPlateEdge", new Vector2(0f, -0.915f),
                new Vector2(1.76f, 0.025f), new Color(0.46f, 0.50f, 0.53f), 2);
            for (int side = -1; side <= 1; side += 2)
            {
                string name = side < 0 ? "Left" : "Right";
                FixedShaftPart(rails, template, name + "Foot", new Vector2(side * 0.67f, -0.90f),
                    new Vector2(0.30f, 0.16f), new Color(0.32f, 0.35f, 0.39f), 2);
                FixedShaftPart(rails, template, name + "FootBolt", new Vector2(side * 0.67f, -0.88f),
                    new Vector2(0.055f, 0.055f), new Color(0.94f, 0.55f, 0.08f), 2);
            }
        }

        private static void FixedShaftPart(Transform rails, SpriteRenderer template, string name,
            Vector2 position, Vector2 size, Color color, int order)
        {
            Transform part = rails.Find(name);
            if (part == null)
            {
                part = new GameObject(name).transform;
                part.SetParent(rails, false);
            }

            part.localPosition = new Vector3(position.x, position.y, 0f);
            part.localScale = new Vector3(size.x / template.sprite.bounds.size.x,
                size.y / template.sprite.bounds.size.y, 1f);
            SpriteRenderer renderer = part.GetComponent<SpriteRenderer>();
            if (renderer == null) renderer = part.gameObject.AddComponent<SpriteRenderer>();
            renderer.sprite = template.sprite;
            renderer.sharedMaterial = template.sharedMaterial;
            renderer.drawMode = SpriteDrawMode.Simple;
            renderer.color = color;
            renderer.sortingLayerID = template.sortingLayerID;
            renderer.sortingOrder = order;
        }

        private void ApplyPose()
        {
            float offset = Mathf.Lerp(openDoorOffset, closedDoorOffset, closedFraction);
            bool visible = closedFraction > 0.001f;
            if (leftDoor != null)
            {
                leftDoor.transform.localPosition = new Vector3(-offset, doorCenterY, 0f);
                leftDoor.enabled = visible;
            }

            if (rightDoor != null)
            {
                rightDoor.transform.localPosition = new Vector3(offset, doorCenterY, 0f);
                rightDoor.enabled = visible;
            }
        }

        private void ApplyLiftPose()
        {
            if (cabinRoot == null)
            {
                return;
            }

            cabinRoot.localPosition = Vector3.up * Mathf.Lerp(0f, departureDistance, liftFraction);
        }
    }
}

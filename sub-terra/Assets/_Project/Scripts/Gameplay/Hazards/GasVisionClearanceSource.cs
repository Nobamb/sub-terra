using System.Collections.Generic;
using SubTerra.Gameplay.Power;
using SubTerra.Shared;
using UnityEngine;

namespace SubTerra.Gameplay.Hazards
{
    /// <summary>
    /// 조명 등 가스 시야를 뚫는 원형 범위. 활성 상태에서만 등록된다.
    /// </summary>
    public sealed class GasVisionClearanceSource : MonoBehaviour
    {
        private static readonly List<GasVisionClearanceSource> Active = new();

        [SerializeField, Min(0.1f)] private float radius = GasVisualRules.LightClearRadiusBlocks;
        [SerializeField] private Sprite softSprite;
        [SerializeField, Range(0f, 1f)] private float unpoweredLevel = GasVisualRules.LightUnpoweredLevel;

        private PowerFacility facility;
        private bool facilityResolved;
        private float revealProgress = 1f;
        private float revealDelay;
        private SpriteRenderer clearanceRenderer;

        /// <summary>가스 시야를 뚫는 판정 반지름. 켠 직후에도 항상 최종값이다.</summary>
        public float Radius => radius;

        /// <summary>
        /// B-204: 후광·암전 구멍·범위 스프라이트가 그리는 시각 반지름. 켠 직후 중심에서 Radius까지 빠르게 커진다.
        /// 판정(IsCleared·Contains·TryGetNearestLight)에는 쓰지 않는다.
        /// </summary>
        public float VisualRadius => radius * RangeSpreadMath.RadiusFraction(revealProgress);

        /// <summary>범위 표시 불투명도 비율(0~1). 켠 직후 점점 짙어진다.</summary>
        public float VisualOpacity => RangeSpreadMath.OpacityFraction(revealProgress);

        public bool IsRevealing => revealProgress < 1f;
        public Vector2 WorldPosition => transform.position;

        /// <summary>
        /// B-196-1: 후광·암전 구멍 같은 "빛 시각"을 보여 줄지(LightLevel > 0).
        /// 구멍 판정(IsCleared/TryGetNearestLight)에는 쓰이지 않는다.
        /// </summary>
        public bool IsLit => LightLevel > 0.0001f;

        /// <summary>
        /// 빛 세기 비율(0~1). 조명은 전원 없이도 가스 시야를 뚫으므로(IsCleared) 전원이 없어도 약하게 빛난다.
        /// 케이블 전원(IsPowered)이나 공급 범위 안이면 1, 아니면 unpoweredLevel. 시설이 없으면 1.
        /// unpoweredLevel을 0으로 두면 전원이 꺼질 때 후광·구멍이 완전히 사라진다.
        /// </summary>
        public float LightLevel
        {
            get
            {
                if (!facilityResolved)
                {
                    facility = GetComponentInParent<PowerFacility>(true);
                    facilityResolved = true;
                }

                if (facility == null || facility.IsInteractionAvailable || facility.IsInSupplyRange)
                {
                    return 1f;
                }

                return Mathf.Clamp01(unpoweredLevel);
            }
        }

        public void SetUnpoweredLevel(float level)
        {
            unpoweredLevel = Mathf.Clamp01(level);
        }

        /// <summary>시설을 다시 찾는다. 시설 컴포넌트를 나중에 붙인 경우에만 필요하다.</summary>
        public void RefreshPowerBinding()
        {
            facilityResolved = false;
        }

        /// <summary>
        /// B-204: 방금 설치해 켠 조명의 시각 범위를 중심에서 빠르게 퍼뜨린다. 판정 반지름은 그대로 최종값이다.
        /// 복원·이미 켜진 조명은 부르지 않으며, 모션 감소면 연출 없이 최종 범위로 둔다.
        /// </summary>
        public void BeginReveal(float delaySeconds = 0f)
        {
            revealProgress = AccessibilityPreferences.ReduceMotion ? 1f : 0f;
            // B-206: 건설 연출이 끝난 뒤 퍼지도록 시작만 미룬다.
            revealDelay = revealProgress >= 1f ? 0f : Mathf.Max(0f, delaySeconds);
            ApplySoftVisual();
        }

        /// <summary>켠 직후 퍼짐을 deltaTime만큼 진행한다. Update가 부르며 테스트도 직접 부를 수 있다.</summary>
        public void AdvanceReveal(float deltaTime)
        {
            if (revealProgress >= 1f)
            {
                return;
            }

            if (revealDelay > 0f)
            {
                revealDelay -= Mathf.Max(0f, deltaTime);
                if (revealDelay > 0f)
                {
                    return;
                }

                deltaTime = -revealDelay;
                revealDelay = 0f;
            }

            revealProgress = AccessibilityPreferences.ReduceMotion
                ? 1f
                : RangeSpreadMath.Advance(revealProgress, deltaTime, RangeSpreadMath.LightSpreadSeconds);
            ApplySoftVisual();
        }

        private void Update()
        {
            AdvanceReveal(Time.deltaTime);
        }

        public static IReadOnlyList<GasVisionClearanceSource> ActiveSources => Active;

        public static IReadOnlyList<GasVisionClearanceSource> ResolveActive()
        {
            if (Active.Count > 0)
            {
                return Active;
            }

            return Object.FindObjectsByType<GasVisionClearanceSource>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);
        }

        private void OnEnable()
        {
            if (!Active.Contains(this))
            {
                Active.Add(this);
            }

            facilityResolved = false;
            // 다시 켜지는 경우(비활성→활성)에는 퍼짐 없이 최종 범위로 바로 보인다.
            revealProgress = 1f;
            ApplySoftVisual();
            LightHaloVisual.EnsureFor(this);
        }

        /// <summary>
        /// 같은 오브젝트의 붉은 시야 스프라이트를 가장자리가 부드러운 스프라이트로 맞춘다.
        /// 지름은 시각 반지름(켠 직후 외에는 판정 반지름 그대로)이라 시각이 판정 범위보다 커지지 않는다.
        /// </summary>
        public void ApplySoftVisual()
        {
            if (clearanceRenderer == null)
            {
                clearanceRenderer = GetComponent<SpriteRenderer>();
            }

            var renderer = clearanceRenderer;
            if (renderer == null)
            {
                return;
            }

            var sprite = renderer.sprite;
            if (sprite == null || sprite.name == "Knob")
            {
                renderer.sprite = softSprite != null ? softSprite : GasSoftSprites.Radial;
            }

            var tint = GasVisualRules.LightClearTint;
            tint.a = GasVisualRules.LightClearRedOpacity * VisualOpacity;
            renderer.color = tint;
            GasZoneVisual.FitSpriteToWorldDiameter(renderer, VisualRadius * 2f);
        }

        private void OnDisable()
        {
            Active.Remove(this);
        }

        public bool Contains(Vector2 worldPosition)
        {
            return isActiveAndEnabled
                && Vector2.Distance(WorldPosition, worldPosition) <= radius;
        }

        public static bool IsCleared(Vector2 worldPosition)
        {
            return GasVisionHoleEvaluator.TryGetNearestLight(worldPosition, out _, out _);
        }

        public void SetRadius(float nextRadius)
        {
            radius = Mathf.Max(0.1f, nextRadius);
            if (isActiveAndEnabled)
            {
                ApplySoftVisual();
            }
        }
    }
}

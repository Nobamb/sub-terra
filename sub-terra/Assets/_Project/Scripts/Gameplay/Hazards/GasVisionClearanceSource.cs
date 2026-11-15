using System.Collections.Generic;
using SubTerra.Gameplay.Power;
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

        public float Radius => radius;
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
            ApplySoftVisual();
            LightHaloVisual.EnsureFor(this);
        }

        /// <summary>
        /// 같은 오브젝트의 붉은 시야 스프라이트를 가장자리가 부드러운 스프라이트로 맞춘다.
        /// 지름은 판정 반지름(radius) 그대로라 시각이 판정 범위보다 커지지 않는다.
        /// </summary>
        public void ApplySoftVisual()
        {
            var renderer = GetComponent<SpriteRenderer>();
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
            tint.a = GasVisualRules.LightClearRedOpacity;
            renderer.color = tint;
            GasZoneVisual.FitSpriteToWorldDiameter(renderer, radius * 2f);
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

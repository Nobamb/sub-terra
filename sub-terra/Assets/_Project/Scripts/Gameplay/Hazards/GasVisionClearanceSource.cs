using System.Collections.Generic;
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

        public float Radius => radius;
        public Vector2 WorldPosition => transform.position;

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

            ApplySoftVisual();
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

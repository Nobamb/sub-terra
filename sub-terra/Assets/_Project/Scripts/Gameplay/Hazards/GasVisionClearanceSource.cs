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
        }
    }
}

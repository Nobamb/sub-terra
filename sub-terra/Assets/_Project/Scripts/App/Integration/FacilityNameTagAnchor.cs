using SubTerra.Gameplay.Building;
using UnityEngine;

namespace SubTerra.App.Integration
{
    /// <summary>시설 그림의 윗면 중앙 바로 위. 일반 화면과 CCTV가 같은 기준을 쓴다.</summary>
    public static class FacilityNameTagAnchor
    {
        public const float Gap = 0.1f;
        public const float FallbackHeight = 0.5f;

        public static Vector2 Compute(BuildingInstance instance)
        {
            var renderer = FindPrimary(instance.transform);
            if (renderer != null)
            {
                var bounds = renderer.bounds;
                return new Vector2(bounds.center.x, bounds.max.y + Gap);
            }

            var position = instance.transform.position;
            return new Vector2(position.x, position.y + FallbackHeight + Gap);
        }

        // VisualRoot의 첫 그림을 시설 본체로 본다. 없으면 자식 중 첫 그림.
        private static SpriteRenderer FindPrimary(Transform root)
        {
            var visualRoot = root.Find("VisualRoot");
            var scope = visualRoot != null ? visualRoot : root;
            var renderers = scope.GetComponentsInChildren<SpriteRenderer>(false);
            for (var i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null && renderers[i].enabled && renderers[i].sprite != null)
                {
                    return renderers[i];
                }
            }

            return null;
        }
    }
}

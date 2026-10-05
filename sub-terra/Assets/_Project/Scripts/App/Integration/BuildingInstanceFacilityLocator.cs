using System.Collections.Generic;
using SubTerra.App.UI.Outpost;
using SubTerra.Gameplay.Building;
using UnityEngine;

namespace SubTerra.App.Integration
{
    /// <summary>
    /// 시설 인스턴스 ID로 실제 설치된 시설의 월드 위치를 찾는다. 코어 CCTV 관찰 전용이다.
    /// 시설을 선택할 때만 조회하며, 한 번 찾은 시설은 보관해 두고 사라지면 다시 찾는다.
    /// </summary>
    public sealed class BuildingInstanceFacilityLocator : IFacilityWorldLocator
    {
        private readonly Dictionary<string, BuildingInstance> cache = new Dictionary<string, BuildingInstance>();

        public bool TryGetWorldCenter(string instanceId, out Vector2 center)
        {
            center = Vector2.zero;
            if (string.IsNullOrEmpty(instanceId))
            {
                return false;
            }

            if (!cache.TryGetValue(instanceId, out var instance) || instance == null)
            {
                instance = Find(instanceId);
                if (instance == null)
                {
                    cache.Remove(instanceId);
                    return false;
                }

                cache[instanceId] = instance;
            }

            center = VisualCenter(instance);
            return true;
        }

        private static BuildingInstance Find(string instanceId)
        {
            var instances = Object.FindObjectsByType<BuildingInstance>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);
            for (var i = 0; i < instances.Length; i++)
            {
                if (instances[i] != null && instances[i].InstanceId == instanceId)
                {
                    return instances[i];
                }
            }

            return null;
        }

        // 그림이 있으면 그림의 중심, 없으면 오브젝트 위치를 쓴다.
        private static Vector2 VisualCenter(BuildingInstance instance)
        {
            var renderer = instance.GetComponentInChildren<SpriteRenderer>();
            return renderer != null && renderer.sprite != null
                ? (Vector2)renderer.bounds.center
                : (Vector2)instance.transform.position;
        }
    }
}

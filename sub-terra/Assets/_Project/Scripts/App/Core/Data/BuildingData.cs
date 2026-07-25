using System.Collections.Generic;
using UnityEngine;

namespace SubTerra.App.Core.Data
{
    /// <summary>
    /// 시설 정적 정의. Runtime Prefab·전력·비용만 두고 현재 설치 여부/전력 상태는 넣지 않는다.
    /// </summary>
    [CreateAssetMenu(fileName = "BuildingData", menuName = "SubTerra/Data/Building", order = 20)]
    public sealed class BuildingData : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField] private GameObject runtimePrefab;
        [SerializeField] private Sprite icon;
        [SerializeField] private int powerDraw;
        [SerializeField] private List<ItemCostEntry> buildCosts = new List<ItemCostEntry>();

        public string Id => id;
        public string DisplayName => displayName;
        public GameObject RuntimePrefab => runtimePrefab;
        public Sprite Icon => icon;
        public int PowerDraw => powerDraw;
        public IReadOnlyList<ItemCostEntry> BuildCosts => buildCosts;

#if UNITY_EDITOR
        public void EditorSet(
            string permanentId,
            string name,
            GameObject prefab,
            int power,
            List<ItemCostEntry> costs)
        {
            EditorSet(permanentId, name, prefab, icon, power, costs);
        }

        public void EditorSet(
            string permanentId,
            string name,
            GameObject prefab,
            Sprite dataIcon,
            int power,
            List<ItemCostEntry> costs)
        {
            id = permanentId;
            displayName = name;
            runtimePrefab = prefab;
            icon = dataIcon;
            powerDraw = power;
            buildCosts = costs ?? new List<ItemCostEntry>();
        }
#endif
    }
}

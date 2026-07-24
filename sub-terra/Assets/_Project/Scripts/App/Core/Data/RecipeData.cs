using System.Collections.Generic;
using UnityEngine;

namespace SubTerra.App.Core.Data
{
    /// <summary>제작 레시피 정적 정의. 입력·출력 ID와 수량만 보유한다.</summary>
    [CreateAssetMenu(fileName = "RecipeData", menuName = "SubTerra/Data/Recipe", order = 30)]
    public sealed class RecipeData : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField] private string resultBuildingId;
        [SerializeField] private List<ItemCostEntry> inputs = new List<ItemCostEntry>();
        [SerializeField] private List<ItemCostEntry> outputs = new List<ItemCostEntry>();

        public string Id => id;
        public string DisplayName => displayName;
        public string ResultBuildingId => resultBuildingId;
        public IReadOnlyList<ItemCostEntry> Inputs => inputs;
        public IReadOnlyList<ItemCostEntry> Outputs => outputs;

#if UNITY_EDITOR
        public void EditorSet(
            string permanentId,
            string name,
            string buildingId,
            List<ItemCostEntry> inputCosts,
            List<ItemCostEntry> outputItems)
        {
            id = permanentId;
            displayName = name;
            resultBuildingId = buildingId;
            inputs = inputCosts ?? new List<ItemCostEntry>();
            outputs = outputItems ?? new List<ItemCostEntry>();
        }
#endif
    }
}

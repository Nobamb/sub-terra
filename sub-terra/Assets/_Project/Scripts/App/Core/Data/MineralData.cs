using UnityEngine;

namespace SubTerra.App.Core.Data
{
    /// <summary>
    /// 광물의 영구 ID와 경제 정보를 관리하는 정적 정의.
    /// 보유량·골드는 GameState에, 내구도·채굴 시간·구조 영향은 MiningTileData에 둔다.
    /// </summary>
    [CreateAssetMenu(fileName = "MineralData", menuName = "SubTerra/Data/Mineral", order = 10)]
    public sealed class MineralData : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField] private float unitWeight = 1f;
        [SerializeField] private int unitPrice = 1;
        [SerializeField] private Sprite icon;

        public string Id => id;
        public string DisplayName => displayName;
        public float UnitWeight => unitWeight;
        public int UnitPrice => unitPrice;
        public Sprite Icon => icon;

#if UNITY_EDITOR
        public void EditorSet(string permanentId, string name, float weight, int price)
        {
            EditorSet(permanentId, name, weight, price, icon);
        }

        public void EditorSet(string permanentId, string name, float weight, int price, Sprite dataIcon)
        {
            id = permanentId;
            displayName = name;
            unitWeight = weight;
            unitPrice = price;
            icon = dataIcon;
        }
#endif
    }
}

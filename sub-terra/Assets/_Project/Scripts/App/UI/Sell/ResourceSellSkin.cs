using UnityEngine;

namespace SubTerra.App.UI.Sell
{
    /// <summary>
    /// 판매 창이 런타임에 쓰는 기존 아이콘 참조. 창이 코드로 만들어지므로 빌드에서도 찾을 수 있게 Resources에 둔다.
    /// 프레임·패널·폰트는 다른 개선 팝업과 같은 MineResetTimedPopupSkin을 그대로 쓴다.
    /// </summary>
    public sealed class ResourceSellSkin : ScriptableObject
    {
        public const string ResourcePath = "UI/ResourceSellSkin";

        public Sprite goldIcon;
        public Sprite cargoIcon;
    }
}

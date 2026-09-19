using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.UI.HUD
{
    /// <summary>메뉴 버튼 왼쪽 아이콘. 스프라이트는 menu-concept1과 같은 6종이다.</summary>
    public sealed class SideMenuIcon : MonoBehaviour
    {
        [SerializeField, Range(0, 5)] private int kind;
        [SerializeField] private Image image;

        public int Kind => kind;
        public Image Image => image;
    }
}

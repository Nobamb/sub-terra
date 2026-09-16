using UnityEngine;

namespace SubTerra.App.UI.MainMenu
{
    /// <summary>Sub-Terra 로고 뒤의 청록백색 아우라가 은은하게 호흡하도록 한다.</summary>
    [RequireComponent(typeof(UnityEngine.UI.RawImage))]
    public sealed class MenuTitleAura : MonoBehaviour
    {
        private UnityEngine.UI.RawImage image;
        private void Awake() => image = GetComponent<UnityEngine.UI.RawImage>();

        private void Update()
        {
            if (image == null) return;
            var alpha = 0.45f + Mathf.Sin(Time.unscaledTime * 1.4f) * 0.12f;
            var c = image.color;
            c.a = alpha;
            image.color = c;
        }
    }
}

using UnityEngine;

namespace SubTerra.App.UI.MainMenu
{
    /// <summary>배경의 청록 채널만 미세하게 변화시켜 정적 신호에 호흡을 준다.</summary>
    [RequireComponent(typeof(UnityEngine.UI.RawImage))]
    public sealed class MenuSignalPulse : MonoBehaviour
    {
        private UnityEngine.UI.RawImage image;
        private void Awake() => image = GetComponent<UnityEngine.UI.RawImage>();

        private void Update()
        {
            var level = 0.97f + Mathf.Sin(Time.unscaledTime * 0.45f) * 0.03f;
            image.color = new Color(1, level, level, 1);
        }
    }
}

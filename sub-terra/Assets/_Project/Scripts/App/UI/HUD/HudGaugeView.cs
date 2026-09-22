using UnityEngine;

namespace SubTerra.App.UI.HUD
{
    /// <summary>원본 게이지를 늘리지 않고 오른쪽부터 잘라낸다. 감소만 0.5초 보간한다.</summary>
    public sealed class HudGaugeView : MonoBehaviour
    {
        [SerializeField] private UnityEngine.UI.Image fill;
        private float target;
        private float start;
        private float elapsed;
        private bool initialized;
        public float DisplayedFraction => fill != null ? fill.fillAmount : 0f;

        public void SetValue(float current, float maximum)
        {
            float next = maximum > 0f ? Mathf.Clamp01(current / maximum) : 0f;
            if (fill == null) return;
            if (!initialized || !isActiveAndEnabled || next >= fill.fillAmount)
            {
                fill.fillAmount = next;
                elapsed = 0.5f;
            }
            else if (!Mathf.Approximately(next, target))
            {
                start = fill.fillAmount;
                elapsed = 0f;
            }
            target = next;
            initialized = true;
        }

        private void Update()
        {
            if (fill == null || elapsed >= 0.5f) return;
            elapsed = Mathf.Min(0.5f, elapsed + Time.unscaledDeltaTime);
            fill.fillAmount = Mathf.Lerp(start, target, elapsed / 0.5f);
        }

        private void OnDisable()
        {
            if (fill != null && initialized) fill.fillAmount = target;
            initialized = false;
            elapsed = 0.5f;
        }
    }
}

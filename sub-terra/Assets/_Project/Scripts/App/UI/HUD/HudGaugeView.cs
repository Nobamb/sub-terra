using UnityEngine;

namespace SubTerra.App.UI.HUD
{
    public sealed class HealthDamageTrail
    {
        private float current;
        private float start;
        private float elapsed = 1f;
        private bool initialized;
        public float Fraction { get; private set; }
        public void Reset(float next)
        {
            Fraction = current = start = System.Math.Max(0f, System.Math.Min(1f, next));
            elapsed = 1f;
            initialized = true;
        }
        public void Set(float next)
        {
            next = System.Math.Max(0f, System.Math.Min(1f, next));
            if (!initialized || next >= Fraction)
            {
                Reset(next);
                return;
            }
            else if (next < current)
            {
                start = Fraction;
                elapsed = 0f;
            }
            // 재생으로 조금 회복되어도 잔상을 지우거나 대기 시간을 다시 시작하지 않는다.
            current = next;
            initialized = true;
        }
        public void Advance(float seconds)
        {
            elapsed += System.Math.Max(0f, seconds);
            float progress = System.Math.Max(0f, System.Math.Min(1f, (elapsed - 0.35f) / 0.4f));
            Fraction = System.Math.Max(current,
                System.Math.Min(Fraction, start + (current - start) * progress));
        }
    }

    /// <summary>체력은 즉시 감소+잔상, 다른 게이지는 기존 0.5초 감소 보간을 유지한다.</summary>
    public sealed class HudGaugeView : MonoBehaviour
    {
        [SerializeField] private UnityEngine.UI.Image fill;
        private float target;
        private float start;
        private float elapsed;
        private bool initialized;
        private bool damageTrailEnabled;
        private float trailMaximum;
        private UnityEngine.UI.Image trailFill;
        private readonly HealthDamageTrail damageTrail = new HealthDamageTrail();
        public float DisplayedFraction => fill != null ? fill.fillAmount : 0f;

        public void EnableDamageTrail()
        {
            damageTrailEnabled = true;
            if (fill == null || trailFill != null) return;
            trailFill = Instantiate(fill, fill.transform.parent);
            trailFill.name = "DamageTrail";
            trailFill.transform.SetSiblingIndex(fill.transform.GetSiblingIndex());
            // 빨간 원본에 주황 tint를 곱하면 빨간색으로 남는다. 원본은 윤곽 마스크로만 쓴다.
            trailFill.color = Color.white;
            trailFill.raycastTarget = false;
            trailFill.fillAmount = fill.fillAmount;
            var mask = trailFill.gameObject.AddComponent<UnityEngine.UI.Mask>();
            mask.showMaskGraphic = false;
            var colorObject = new GameObject("TrailColor", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            colorObject.transform.SetParent(trailFill.transform, false);
            var rect = (RectTransform)colorObject.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var colorFill = colorObject.GetComponent<UnityEngine.UI.Image>();
            colorFill.color = new Color(1f, 0.65f, 0.1f, 1f);
            colorFill.raycastTarget = false;
        }

        public void SetValue(float current, float maximum)
        {
            float next = maximum > 0f ? Mathf.Clamp01(current / maximum) : 0f;
            if (fill == null) return;
            if (damageTrailEnabled)
            {
                if (!initialized || !isActiveAndEnabled || maximum != trailMaximum) damageTrail.Reset(next);
                else damageTrail.Set(next);
                trailMaximum = maximum;
                fill.fillAmount = next;
                if (trailFill != null) trailFill.fillAmount = damageTrail.Fraction;
                target = next;
                initialized = true;
                return;
            }
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
            if (damageTrailEnabled)
            {
                damageTrail.Advance(Time.unscaledDeltaTime);
                if (trailFill != null) trailFill.fillAmount = damageTrail.Fraction;
                return;
            }
            if (fill == null || elapsed >= 0.5f) return;
            elapsed = Mathf.Min(0.5f, elapsed + Time.unscaledDeltaTime);
            fill.fillAmount = Mathf.Lerp(start, target, elapsed / 0.5f);
        }

        private void OnDisable()
        {
            if (fill != null && initialized) fill.fillAmount = target;
            damageTrail.Reset(target);
            if (trailFill != null) trailFill.fillAmount = target;
            initialized = false;
            elapsed = 0.5f;
        }
    }
}

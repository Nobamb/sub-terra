using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.UI.Progression
{
    /// <summary>
    /// 업그레이드 트리 연출 공용 보조. 업그레이드 창이 게임을 멈춰도 재생되도록
    /// 모든 시간을 unscaledDeltaTime으로 진행한다. 연출은 매 프레임 절대값을 쓰므로 누적 변형이 없다.
    /// </summary>
    public static class UpgradeTreeTween
    {
        public static readonly Color Cyan = new Color(0.25f, 0.92f, 0.96f, 1f);
        public static readonly Color CyanDim = new Color(0.12f, 0.36f, 0.4f, 1f);
        public static readonly Color Gold = new Color(0.98f, 0.8f, 0.34f, 1f);
        public static readonly Color Warning = new Color(1f, 0.42f, 0.32f, 1f);
        public static readonly Color TextMain = new Color(0.86f, 0.96f, 0.98f, 1f);
        public static readonly Color TextDim = new Color(0.52f, 0.7f, 0.74f, 1f);

        /// <summary>duration 동안 0→1 진행값을 step에 넘긴다. 마지막에는 정확히 1을 한 번 전달한다.</summary>
        public static IEnumerator Run(float duration, Action<float> step)
        {
            if (duration <= 0f)
            {
                step(1f);
                yield break;
            }

            var elapsed = 0f;
            while (elapsed < duration)
            {
                step(Mathf.Clamp01(elapsed / duration));
                yield return null;
                elapsed += Time.unscaledDeltaTime;
            }

            step(1f);
        }

        public static IEnumerator WaitUnscaled(float seconds)
        {
            var elapsed = 0f;
            while (elapsed < seconds)
            {
                yield return null;
                elapsed += Time.unscaledDeltaTime;
            }
        }

        public static float EaseOut(float t)
        {
            t = Mathf.Clamp01(t);
            return 1f - (1f - t) * (1f - t);
        }

        /// <summary>0→1→0 으로 오르내리는 부드러운 펄스.</summary>
        public static float Pulse(float t)
        {
            return Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI);
        }

        public static void SetAlpha(Graphic graphic, float alpha)
        {
            if (graphic == null)
            {
                return;
            }

            var color = graphic.color;
            color.a = alpha;
            graphic.color = color;
        }

        public static void SetColor(Graphic graphic, Color color, float alpha)
        {
            if (graphic == null)
            {
                return;
            }

            color.a = alpha;
            graphic.color = color;
        }
    }
}

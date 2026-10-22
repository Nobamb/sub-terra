using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Timeline = SubTerra.App.UI.Progression.DeepZoneUnlockArtTimeline;

namespace SubTerra.App.UI.Progression
{
    /// <summary>
    /// 심층 해금 창의 그림 연출. 창 글리치(StartBriefingPopupMotion)가 본문을 보이기 시작하면 시계를 시작한다.
    /// 그림은 위에서 아래로 드러나고, 다 드러나면 발견 섬광 뒤 푸른 빛이 광물마다 다른 주기로 호흡한다.
    /// 모든 시간은 비스케일이라 업그레이드 창이 게임을 멈춰도 재생된다.
    /// </summary>
    public sealed class DeepZoneUnlockArtMotion : MonoBehaviour
    {
        // 유지 발광 세기. 발견 섬광(Peak)보다 항상 약하다.
        private const float BorderGlowMin = 0.18f;
        private const float BorderGlowAmp = 0.2f;
        private const float CrystalMin = 0.14f;
        private const float CrystalAmp = 0.34f;
        private const float CrystalPeak = 0.95f;
        private const float CoreMin = 0.2f;
        private const float CoreAmp = 0.3f;
        private const float HaloMin = 0.1f;
        private const float HaloAmp = 0.12f;
        private const float MoteRise = 150f;
        private const float GhostAlpha = 0.16f;

        public struct Layers
        {
            public CanvasGroup Content;
            public RectTransform Hero;
            public Image Ghost;
            public Image Emblem;
            public Image Border;
            public Image BorderGlow;
            public Image[] CrystalGlows;
            public Image CoreGlow;
            public Image Halo;
            public Image FrameFlash;
            public Image ScanLine;
            public Image SweepLine;
            public Image[] Rings;
            public Image[] Motes;
            public TMP_Text Counter;
        }

        private Layers layers;
        private int minDepth;
        private int maxDepth;
        private bool started;
        private float time;
        private float lastCounter = -1f;

        public bool IsStarted => started;
        public float Time => time;

        public void Bind(Layers bound)
        {
            layers = bound;
            Restart();
        }

        public void SetDepth(int min, int max)
        {
            minDepth = min;
            maxDepth = max;
            lastCounter = -1f;
        }

        /// <summary>처음 상태로 되돌린다. 본문이 다시 보이기 시작하면 연출을 처음부터 재생한다.</summary>
        public void Restart()
        {
            started = false;
            time = 0f;
            lastCounter = -1f;
            Apply(0f);
        }

        /// <summary>연출 없이 다 드러난 상태(정지 화면·Edit Mode 확인용).</summary>
        public void Snap(float at)
        {
            started = true;
            time = at;
            Apply(time);
        }

        private void OnDisable()
        {
            started = false;
            time = 0f;
        }

        private void Update()
        {
            if (!started)
            {
                if (layers.Content == null || layers.Content.alpha <= 0.01f)
                {
                    return;
                }

                started = true;
                time = 0f;
            }

            // 큰 프레임 간격에 섬광·파동이 건너뛰어지지 않게 한 프레임 진행량을 제한한다.
            time += Mathf.Min(UnityEngine.Time.unscaledDeltaTime, 0.05f);
            Apply(time);
        }

        private void Apply(float t)
        {
            var reveal = started ? Timeline.Reveal(t) : 0f;
            var border = started ? Timeline.BorderIn(t) : 0f;
            var flash = started ? Timeline.Flash(t) : 0f;
            var idle = started ? Timeline.Idle(t) : 0f;

            // 윤곽은 테두리와 함께 켜지고 그림이 다 드러나면 물러난다.
            SetAlpha(layers.Ghost, GhostAlpha * border * (1f - reveal));
            SetFill(layers.Emblem, reveal, 1f);
            SetFill(layers.Border, 1f, border);
            var borderBreath = Timeline.Breath(t, 2.8f, 0f);
            SetFill(layers.BorderGlow, 1f, border * Mathf.Max(
                Timeline.Level(BorderGlowMin + BorderGlowAmp * borderBreath, 1f, flash, idle),
                (1f - border) * border * 2.4f));

            if (layers.CrystalGlows != null)
            {
                for (var i = 0; i < layers.CrystalGlows.Length; i++)
                {
                    // 광물 무리마다 주기와 위상을 달리해 한꺼번에 깜박이지 않게 한다.
                    var breath = Timeline.Breath(t, 2.6f + 0.7f * i, 0.13f + 0.37f * i);
                    SetFill(layers.CrystalGlows[i], reveal,
                        Timeline.Level(CrystalMin + CrystalAmp * breath, CrystalPeak, flash, idle));
                }
            }

            var coreBreath = Timeline.Breath(t, 3.4f, 0.6f);
            SetFill(layers.CoreGlow, reveal, Timeline.Level(CoreMin + CoreAmp * coreBreath, 1f, flash, idle));
            if (layers.CoreGlow != null)
            {
                var pop = 1f + 0.06f * flash;
                layers.CoreGlow.rectTransform.localScale = new Vector3(pop, pop, 1f);
            }

            if (layers.Halo != null)
            {
                SetAlpha(layers.Halo, Timeline.Level(HaloMin + HaloAmp * Timeline.Breath(t, 4.2f, 0.25f), 0.6f, flash, idle));
                var scale = 1f + 0.08f * flash;
                layers.Halo.rectTransform.localScale = new Vector3(scale, scale, 1f);
            }

            SetAlpha(layers.FrameFlash, flash * 0.8f);
            ApplyScan(t, reveal);
            ApplySweep(t);
            ApplyRings(t);
            ApplyMotes(t, idle);
            ApplyCounter(t);
        }

        // 드러나는 경계를 따라 내려가는 굴착 빛.
        private void ApplyScan(float t, float reveal)
        {
            var scan = layers.ScanLine;
            if (scan == null) return;
            var height = layers.Emblem != null ? layers.Emblem.rectTransform.rect.height : 0f;
            scan.rectTransform.anchoredPosition = new Vector2(0f, height * (0.5f - reveal));
            SetAlpha(scan, started ? Timeline.ScanAlpha(t) : 0f);
        }

        private void ApplySweep(float t)
        {
            var sweep = layers.SweepLine;
            if (sweep == null) return;
            var p = started ? Timeline.Sweep(t) : -1f;
            var height = layers.Emblem != null ? layers.Emblem.rectTransform.rect.height : 0f;
            sweep.rectTransform.anchoredPosition = new Vector2(0f, height * (0.5f - Mathf.Max(0f, p)));
            SetAlpha(sweep, Timeline.SweepAlpha(p) * 0.4f);
        }

        // 탐지 파동. 두 겹을 조금 어긋나게 퍼뜨려 음파처럼 보이게 한다.
        private void ApplyRings(float t)
        {
            if (layers.Rings == null) return;
            for (var i = 0; i < layers.Rings.Length; i++)
            {
                var ring = layers.Rings[i];
                if (ring == null) continue;
                var p = started ? Timeline.Ping(t - i * 0.22f) : -1f;
                var scale = Timeline.PingScale(p);
                ring.rectTransform.localScale = new Vector3(scale, scale, 1f);
                SetAlpha(ring, Timeline.PingAlpha(p) * (i == 0 ? 0.75f : 0.4f));
            }
        }

        // 심층에서 떠오르는 빛 입자. 위치는 시간만으로 정해져 다시 열어도 같은 모양이다.
        private void ApplyMotes(float t, float strength)
        {
            if (layers.Motes == null) return;
            var area = layers.Emblem != null ? layers.Emblem.rectTransform.rect.size : new Vector2(600f, 380f);
            for (var i = 0; i < layers.Motes.Length; i++)
            {
                var mote = layers.Motes[i];
                if (mote == null) continue;
                var life = 3.6f + 0.8f * (i % 4);
                var p = Mathf.Repeat(t / life + i * 0.137f, 1f);
                var baseX = (Frac(i * 0.618034f + 0.11f) * 2f - 1f) * area.x * 0.36f;
                var baseY = -area.y * 0.42f + Frac(i * 0.381966f + 0.37f) * area.y * 0.3f;
                var sway = Mathf.Sin((t + i * 1.7f) * 0.9f) * 8f;
                mote.rectTransform.anchoredPosition = new Vector2(baseX + sway, baseY + p * MoteRise);
                SetAlpha(mote, Mathf.Sin(p * Mathf.PI) * 0.8f * strength);
            }
        }

        // 굴착하듯 깊이 숫자가 0에서 실제 범위까지 오른다. 다 오른 뒤에는 다시 쓰지 않는다.
        private void ApplyCounter(float t)
        {
            if (layers.Counter == null || maxDepth <= 0 || !started) return;
            var c = Timeline.Counter(t);
            if (Mathf.Approximately(c, lastCounter)) return;
            lastCounter = c;
            layers.Counter.text = DeepZoneUnlockPopupText.Range(
                Mathf.RoundToInt(minDepth * c),
                Mathf.RoundToInt(maxDepth * c));
        }

        private static float Frac(float v) => v - Mathf.Floor(v);

        private static void SetFill(Image image, float fill, float alpha)
        {
            if (image == null) return;
            image.fillAmount = fill;
            SetAlpha(image, alpha);
        }

        private static void SetAlpha(Graphic graphic, float alpha)
        {
            if (graphic == null) return;
            var color = graphic.color;
            color.a = Mathf.Clamp01(alpha);
            graphic.color = color;
            graphic.enabled = color.a > 0.002f;
        }
    }
}

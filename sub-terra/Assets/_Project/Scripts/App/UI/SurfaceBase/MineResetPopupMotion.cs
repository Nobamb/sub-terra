using System;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.UI.SurfaceBase
{
    /// <summary>
    /// '새 광산 구역' 팝업의 등장(TV 화면 켜짐)·유지 발광·닫기 연출. 시간값은 MineResetPopupTimeline이 정한다.
    /// 패널은 RectMask2D 높이만 바꿔 드러내고, 글자는 CanvasGroup 알파만 바꿔 찌그러지지 않는다.
    /// 게임 시간이 멈춰도 돌도록 unscaled 시간을 쓴다.
    /// </summary>
    public sealed class MineResetPopupMotion : MonoBehaviour
    {
        private enum Phase
        {
            Hidden,
            Opening,
            Closing
        }

        [SerializeField] private CanvasGroup rootGroup;
        [SerializeField] private Image backdrop;
        [SerializeField] private float backdropAlpha = 0.82f;
        [SerializeField] private RectTransform card;
        [SerializeField] private RectTransform body;
        [SerializeField] private Image scanLine;
        [SerializeField] private Image edgeTop;
        [SerializeField] private Image edgeBottom;
        [SerializeField] private Image frameFlash;
        [SerializeField] private CanvasGroup content;
        [SerializeField] private RectTransform hexScale;
        [SerializeField] private Image hexMine;
        [SerializeField] private Image hexBorder;
        [SerializeField] private Image hexBorderGlow;
        [SerializeField] private Image hexCrystalGlow;
        [SerializeField] private Image hexTunnelGlow;
        [SerializeField] private Image hexRings;
        [SerializeField] private Image coreGlow;
        [SerializeField] private Image[] caveGlows;
        [SerializeField] private Image[] motes;

        // 유지 발광 세기. 첫 등장 섬광(Peak)보다 항상 약하다.
        private const float BorderGlowMin = 0.16f;
        private const float BorderGlowAmp = 0.16f;
        private const float BorderGlowPeak = 1f;
        private const float CrystalMin = 0.12f;
        private const float CrystalAmp = 0.26f;
        private const float CrystalPeak = 0.75f;
        private const float CaveMin = 0.1f;
        private const float CaveAmp = 0.32f;
        private const float CavePeak = 0.7f;
        private const float MoteRise = 64f;

        private Phase phase = Phase.Hidden;
        private float openTime;
        private float closeTime;
        private float foldStart;
        private Action onClosed;
        private bool closedWhileDisabled;

        public bool IsClosing => phase == Phase.Closing;
        public bool IsAnimatingOpen => phase == Phase.Opening && openTime < MineResetPopupTimeline.OpenDuration;
        public float OpenTime => openTime;

        public void PlayOpen()
        {
            if (phase == Phase.Opening)
            {
                return;
            }

            phase = Phase.Opening;
            openTime = 0f;
            closeTime = 0f;
            onClosed = null;
            closedWhileDisabled = false;
            ApplyFit();
            Apply(0f, 1f, 1f, 0f);
            SetGroups(interactableRoot: true);
        }

        /// <summary>연출 없이 완전히 열린 상태(Edit Mode·정지 화면용).</summary>
        public void SnapOpen()
        {
            phase = Phase.Opening;
            openTime = MineResetPopupTimeline.OpenDuration;
            closeTime = 0f;
            onClosed = null;
            ApplyFit();
            Apply(openTime, 1f, 1f, openTime);
            SetGroups(interactableRoot: true);
        }

        public void PlayClose(Action closed)
        {
            if (phase == Phase.Hidden)
            {
                closed?.Invoke();
                return;
            }

            if (phase == Phase.Closing)
            {
                return;
            }

            phase = Phase.Closing;
            closeTime = 0f;
            // 덜 열린 채로 닫아도 지금 모습에서 이어서 접는다.
            foldStart = Mathf.Min(openTime, MineResetPopupTimeline.SettleTime);
            onClosed = closed;
            SetGroups(interactableRoot: false);
        }

        /// <summary>즉시 숨김 상태로 정리한다. 다음 열기는 처음부터 다시 재생된다.</summary>
        public void ResetHidden()
        {
            phase = Phase.Hidden;
            onClosed = null;
            closedWhileDisabled = false;
            if (rootGroup != null) rootGroup.alpha = 1f;
        }

        private void OnDisable()
        {
            if (phase == Phase.Closing)
            {
                // 비활성 중에는 SetActive를 부를 수 없으므로 다시 켜질 때 마저 닫는다.
                closedWhileDisabled = onClosed != null;
                phase = Phase.Hidden;
                if (rootGroup != null) rootGroup.alpha = 1f;
                return;
            }

            if (phase == Phase.Opening)
            {
                // 열리던 중 꺼졌다면 다음 열기는 처음부터 재생한다.
                phase = Phase.Hidden;
            }
        }

        private void Update()
        {
            if (closedWhileDisabled)
            {
                closedWhileDisabled = false;
                FinishClose();
                return;
            }

            // 씬 로딩 직후 큰 프레임 간격으로 연출을 건너뛰지 않게 한 프레임 진행량을 제한한다.
            var dt = Mathf.Min(Time.unscaledDeltaTime, 1f / 20f);
            if (phase == Phase.Opening)
            {
                openTime += dt;
                Apply(openTime, 1f, 1f, openTime);
            }
            else if (phase == Phase.Closing)
            {
                openTime += dt;
                closeTime += dt;
                var pose = MineResetPopupTimeline.EvaluateClose(closeTime, foldStart);
                Apply(openTime, pose.Glow, pose.Alpha, pose.FoldTime, true);
                if (pose.Finished)
                {
                    FinishClose();
                }
            }
        }

        private void FinishClose()
        {
            var callback = onClosed;
            onClosed = null;
            phase = Phase.Hidden;
            if (rootGroup != null) rootGroup.alpha = 1f;
            callback?.Invoke();
        }

        private void SetGroups(bool interactableRoot)
        {
            if (rootGroup != null)
            {
                rootGroup.interactable = interactableRoot;
                // 닫히는 동안에도 뒤 화면 클릭은 막는다.
                rootGroup.blocksRaycasts = true;
            }
        }

        private void ApplyFit()
        {
            if (card == null) return;
            var area = ((RectTransform)transform).rect;
            var size = card.sizeDelta;
            var scale = MineResetPopupTimeline.FitScale(area.width, area.height, size.x, size.y, 12f);
            card.localScale = new Vector3(scale, scale, 1f);
        }

        /// <summary>t는 호흡·떠다니는 입자용 경과 시간, poseT는 열기 시간표를 읽는 시각(닫을 때는 거꾸로 줄어든다).</summary>
        private void Apply(float t, float glow, float alpha, float poseT, bool folding = false)
        {
            var pose = MineResetPopupTimeline.EvaluateOpen(poseT);
            if (folding)
            {
                // 거꾸로 감을 때 완성 섬광이 다시 터지지 않게 한다.
                pose.HexFlash = 0f;
            }

            if (rootGroup != null) rootGroup.alpha = alpha;
            SetAlpha(backdrop, backdropAlpha * pose.Backdrop);

            var cardSize = card != null ? card.sizeDelta : new Vector2(1332f, 1021f);
            if (scanLine != null)
            {
                var rect = scanLine.rectTransform;
                rect.sizeDelta = new Vector2(cardSize.x * pose.ScanWidth, rect.sizeDelta.y);
                SetAlpha(scanLine, pose.ScanAlpha);
            }

            var height = Mathf.Lerp(MineResetPopupTimeline.RevealMinHeight, cardSize.y, pose.Reveal);
            if (body != null)
            {
                body.sizeDelta = new Vector2(cardSize.x, height);
            }

            PlaceEdge(edgeTop, height * 0.5f, pose.EdgeAlpha);
            PlaceEdge(edgeBottom, -height * 0.5f, pose.EdgeAlpha);
            SetAlpha(frameFlash, pose.FrameFlash * 0.85f * glow);

            if (content != null)
            {
                content.alpha = pose.Content;
                content.interactable = pose.Interactable;
                content.blocksRaycasts = true;
            }

            if (coreGlow != null)
            {
                coreGlow.rectTransform.localScale = new Vector3(pose.CoreScale, pose.CoreScale, 1f);
                SetAlpha(coreGlow, pose.CoreAlpha * glow);
            }

            if (hexScale != null)
            {
                // 가로세로 같은 배율이라 육각형과 광산 그림이 찌그러지지 않는다.
                hexScale.localScale = new Vector3(pose.HexScale, pose.HexScale, 1f);
            }

            SetAlpha(hexMine, pose.HexAlpha);
            SetAlpha(hexRings, pose.RingAlpha * 0.36f * Mathf.Lerp(1f, glow, 0.6f));

            var flash = pose.HexFlash;
            var idle = pose.Idle;
            var borderBreath = MineResetPopupTimeline.Breath(t, 2.8f, 0f);
            SetAlpha(hexBorder, pose.HexAlpha * (0.86f + 0.14f * borderBreath * idle));
            SetAlpha(hexBorderGlow, pose.HexAlpha * glow * Level(BorderGlowMin + BorderGlowAmp * borderBreath, BorderGlowPeak, flash, idle));
            SetAlpha(hexCrystalGlow, pose.HexAlpha * glow * Level(
                CrystalMin + CrystalAmp * MineResetPopupTimeline.Breath(t, 3.1f, 0.2f), CrystalPeak, flash, idle));
            SetAlpha(hexTunnelGlow, pose.HexAlpha * glow * Level(
                CrystalMin + CrystalAmp * MineResetPopupTimeline.Breath(t, 3.6f, 0.55f), CrystalPeak, flash, idle));

            if (caveGlows != null)
            {
                var gate = MineResetPopupTimeline.Progress(t, MineResetPopupTimeline.ContentStart, MineResetPopupTimeline.HexEnd);
                for (var i = 0; i < caveGlows.Length; i++)
                {
                    // 광물마다 주기와 위상을 달리해 한꺼번에 깜박이지 않게 한다.
                    var breath = MineResetPopupTimeline.Breath(t, 2.4f + 0.55f * i, 0.17f + 0.31f * i);
                    SetAlpha(caveGlows[i], gate * glow * Level(CaveMin + CaveAmp * breath, CavePeak, flash, idle));
                }
            }

            ApplyMotes(t, idle * glow);
        }

        /// <summary>완성 섬광(flash)은 유지 발광보다 밝게 시작해 유지 수준으로 감쇠한다.</summary>
        private static float Level(float idleLevel, float peak, float flash, float idle)
        {
            var settled = idleLevel * idle;
            return settled + (peak - settled) * flash;
        }

        private void ApplyMotes(float t, float strength)
        {
            if (motes == null) return;
            for (var i = 0; i < motes.Length; i++)
            {
                var mote = motes[i];
                if (mote == null) continue;
                var life = 4.2f + 0.7f * (i % 4);
                var p = Mathf.Repeat(t / life + i * 0.137f, 1f);
                var baseX = (Frac(i * 0.618034f + 0.11f) * 2f - 1f) * 250f;
                var baseY = -120f + Frac(i * 0.381966f + 0.37f) * 140f;
                var sway = Mathf.Sin((t + i * 1.7f) * 0.9f) * 7f;
                mote.rectTransform.anchoredPosition = new Vector2(baseX + sway, baseY + p * MoteRise);
                SetAlpha(mote, Mathf.Sin(p * Mathf.PI) * 0.75f * strength);
            }
        }

        private static float Frac(float v) => v - Mathf.Floor(v);

        private static void PlaceEdge(Image edge, float y, float alpha)
        {
            if (edge == null) return;
            edge.rectTransform.anchoredPosition = new Vector2(0f, y);
            SetAlpha(edge, alpha);
        }

        private static void SetAlpha(Graphic graphic, float alpha)
        {
            if (graphic == null) return;
            var color = graphic.color;
            color.a = Mathf.Clamp01(alpha);
            graphic.color = color;
        }
    }
}

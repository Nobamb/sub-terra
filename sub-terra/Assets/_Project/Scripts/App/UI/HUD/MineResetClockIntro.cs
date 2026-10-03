using System;
using UnityEngine;

namespace SubTerra.App.UI.HUD
{
    /// <summary>
    /// 만료 알림 직전 시계 연출을 재생한다. 시각 효과만 맡으며 초기화·저장 로직과 묶지 않는다.
    /// 게임 시간이 멈춰도 돌도록 unscaled 시간을 쓴다.
    /// </summary>
    public sealed class MineResetClockIntro : MonoBehaviour
    {
        [SerializeField] private RectTransform visual;
        [SerializeField] private CanvasGroup visualGroup;
        [SerializeField] private RectTransform hand;

        private float time;
        private bool playing;
        private Action onFinished;
        private Action<float> onDim;

        public bool IsPlaying => playing;

        public void Bind(RectTransform clock, CanvasGroup group, RectTransform handRect, Action<float> dimChanged)
        {
            visual = clock;
            visualGroup = group;
            hand = handRect;
            onDim = dimChanged;
        }

        public void Play(Action finished)
        {
            time = 0f;
            playing = true;
            onFinished = finished;
            gameObject.SetActive(true);
            Apply(0f);
        }

        /// <summary>콜백 없이 즉시 멈추고 숨긴다.</summary>
        public void Stop()
        {
            playing = false;
            onFinished = null;
            gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            playing = false;
            onFinished = null;
        }

        private void Update()
        {
            if (!playing)
            {
                return;
            }

            // 씬 로딩 직후 큰 프레임 간격으로 연출을 건너뛰지 않게 한 프레임 진행량을 제한한다.
            time += Mathf.Min(Time.unscaledDeltaTime, 1f / 20f);
            if (time >= MineResetClockIntroTimeline.Duration)
            {
                Apply(MineResetClockIntroTimeline.Duration);
                var callback = onFinished;
                Stop();
                callback?.Invoke();
                return;
            }

            Apply(time);
        }

        /// <summary>시각 t의 모습을 그대로 적용한다(Edit Mode 정지 화면 확인에도 쓴다).</summary>
        public void Apply(float t)
        {
            var pose = MineResetClockIntroTimeline.Evaluate(t);
            if (visual != null)
            {
                visual.localScale = new Vector3(pose.Scale, pose.Scale, 1f);
                visual.anchoredPosition = new Vector2(pose.ShakeX, 0f);
            }

            if (visualGroup != null)
            {
                visualGroup.alpha = pose.Alpha;
            }

            if (hand != null)
            {
                hand.localRotation = Quaternion.Euler(0f, 0f, pose.HandAngle);
            }

            if (onDim != null)
            {
                onDim(pose.Dim);
            }
        }
    }
}

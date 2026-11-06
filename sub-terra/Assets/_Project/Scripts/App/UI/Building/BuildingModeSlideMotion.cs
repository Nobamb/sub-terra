using System;
using SubTerra.App.Audio;
using SubTerra.Shared;
using SubTerra.Shared.Audio;
using UnityEngine;

namespace SubTerra.App.UI.Building
{
    /// <summary>
    /// B-188: 건설 모드 레일이 화면 왼쪽에서 밀려 들어오고, 닫을 때 같은 길로 빠진다.
    /// 소리는 기존 창 켜짐·꺼짐(WindowTvSfx)만 그대로 쓰고 새 효과음은 만들지 않는다.
    /// 루트 RectTransform은 Scene 배치값을 유지하고 내용(PanelRoot)만 움직인다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BuildingModeSlideMotion : MonoBehaviour
    {
        public const float OpenDuration = 0.16f;
        public const float CloseDuration = 0.14f;
        public const float SlideDistance = 96f;

        private RectTransform content;
        private CanvasGroup group;
        private Vector2 restPosition;
        private float restAlpha = 1f;
        private bool restInteractable = true;
        private bool captured;
        private float progress = 1f;
        private Action closeCompleted;
        private bool shown;

        public bool IsPlaying { get; private set; }
        public bool IsClosing { get; private set; }
        public bool IsShown => shown;
        public float Progress => progress;

        public void Configure(RectTransform target)
        {
            if (target == null || target == content)
            {
                return;
            }

            Restore();
            content = target;
            captured = false;
        }

        public void PlayOpen()
        {
            if (!Capture())
            {
                return;
            }

            if (IsClosing)
            {
                // 닫던 레일을 다시 열면 지금 위치에서 되돌아 들어온다.
                IsClosing = false;
                closeCompleted = null;
                group.interactable = restInteractable;
                PlayOpenSound();
                ApplyFrame();
                return;
            }

            if (IsPlaying || shown)
            {
                return;
            }

            PlayOpenSound();
            if (AccessibilityPreferences.ReduceMotion)
            {
                Restore();
                return;
            }

            progress = 0f;
            IsPlaying = true;
            ApplyFrame();
        }

        public void PlayClose(Action completed)
        {
            if (IsClosing)
            {
                closeCompleted = completed;
                return;
            }

            var wasShown = shown;
            PlayCloseSound();
            if (!wasShown || !gameObject.activeInHierarchy || !Capture() || AccessibilityPreferences.ReduceMotion)
            {
                Restore();
                completed?.Invoke();
                return;
            }

            if (!IsPlaying)
            {
                progress = 1f;
                IsPlaying = true;
            }

            IsClosing = true;
            closeCompleted = completed;
            // 닫히는 동안 버튼은 막되 뒤로 클릭이 새지 않게 raycast는 유지한다.
            group.interactable = false;
            ApplyFrame();
        }

        private void Update() => Advance(Time.unscaledDeltaTime);

        public void Advance(float deltaTime)
        {
            if (!IsPlaying)
            {
                return;
            }

            var rate = IsClosing ? -1f / CloseDuration : 1f / OpenDuration;
            progress = Mathf.Clamp01(progress + Mathf.Max(0f, deltaTime) * rate);
            if (IsClosing && progress <= 0f)
            {
                FinishClose();
            }
            else if (!IsClosing && progress >= 1f)
            {
                Restore();
            }
            else
            {
                ApplyFrame();
            }
        }

        private bool Capture()
        {
            if (content == null)
            {
                content = transform as RectTransform;
            }

            if (group == null)
            {
                group = GetComponent<CanvasGroup>();
                if (group == null)
                {
                    group = gameObject.AddComponent<CanvasGroup>();
                }
            }

            if (content == null)
            {
                return false;
            }

            if (!captured && !IsPlaying)
            {
                restPosition = content.anchoredPosition;
                restAlpha = group.alpha;
                restInteractable = group.interactable;
                captured = true;
            }

            return true;
        }

        private void ApplyFrame()
        {
            var eased = Mathf.SmoothStep(0f, 1f, progress);
            content.anchoredPosition = restPosition + new Vector2(-SlideDistance * (1f - eased), 0f);
            group.alpha = restAlpha * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0f, 0.7f, progress));
        }

        private void PlayOpenSound()
        {
            if (shown)
            {
                return;
            }

            shown = true;
            WindowTvSfx.Play(SfxId.WindowTvOpen);
        }

        private void PlayCloseSound()
        {
            if (!shown)
            {
                return;
            }

            shown = false;
            WindowTvSfx.Play(SfxId.WindowTvClose);
        }

        private void FinishClose()
        {
            var completed = closeCompleted;
            Restore();
            completed?.Invoke();
        }

        private void OnDisable()
        {
            // 부모가 꺼지거나 씬을 떠나 숨으면 소리 없이 제자리로 돌린다.
            shown = false;
            if (IsClosing)
            {
                FinishClose();
            }
            else
            {
                Restore();
            }
        }

        private void Restore()
        {
            if (captured && content != null)
            {
                content.anchoredPosition = restPosition;
            }

            if (captured && group != null)
            {
                group.alpha = restAlpha;
                group.interactable = restInteractable;
            }

            progress = 1f;
            IsPlaying = false;
            IsClosing = false;
            closeCompleted = null;
        }
    }
}

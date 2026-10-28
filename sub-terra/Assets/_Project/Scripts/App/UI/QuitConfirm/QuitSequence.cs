using System;
using SubTerra.App.UI.MainMenu;
using UnityEngine;

namespace SubTerra.App.UI.QuitConfirm
{
    public enum QuitRequestResult
    {
        /// <summary>종료 확인 창을 띄웠다.</summary>
        Shown = 0,
        /// <summary>저장 중이라 창을 띄우지 않았다(호출한 쪽이 "저장 중" 문구를 띄운다).</summary>
        DeferredWhileSaving = 1,
        /// <summary>일시정지 중·탑승 중·설정 창 등으로 막혔다.</summary>
        Blocked = 2,
        /// <summary>이미 창이 떠 있거나 종료 연출 중이다.</summary>
        Busy = 3
    }

    /// <summary>저장·종료 대상. 실제는 SaveRuntimeController, 테스트는 가짜.</summary>
    public interface IQuitTarget
    {
        bool IsDirty { get; }
        bool IsSaveInProgress { get; }
        void SaveBeforeQuit();
        void Shutdown();
    }

    /// <summary>종료 연출 화면. 메인 화면은 MainMenuIntroDirector, 지상·광산은 QuitOutroOverlay.</summary>
    public interface IQuitOutroView
    {
        float QuitOutroDuration { get; }
        void BeginQuitOutro(bool reducedMotion);
        /// <summary>progress 0 = 지금 화면, 1 = 다 접힌 검은 화면.</summary>
        void ApplyQuitOutro(float progress);
    }

    /// <summary>
    /// B-170 게임 종료 순서. 요청(QuitPolicy 판단) → 확인 창 → 종료 누름(먼저 저장) → 종료 연출 → 저장 끝나면 끄기.
    /// 연출 중 아무 입력이면 바로 끈다. 상태만 가진 순수 계산이라 EditMode에서 검증한다.
    /// </summary>
    public sealed class QuitSequence
    {
        public enum Phase
        {
            Idle = 0,
            Confirming = 1,
            Outro = 2,
            WaitingSave = 3,
            Done = 4
        }

        private readonly IQuitTarget target;
        private readonly Action outroStarted;
        private IQuitOutroView outro;
        private bool reduced;
        private float elapsed;
        private float duration;

        public QuitSequence(IQuitTarget target, Action outroStarted = null)
        {
            this.target = target;
            this.outroStarted = outroStarted;
        }

        public Phase Current { get; private set; }
        public float OutroElapsed => elapsed;
        public float OutroDuration => duration;
        public int SaveCount { get; private set; }
        public int ShutdownCount { get; private set; }

        public QuitRequestResult Request(bool blocked)
        {
            if (Current != Phase.Idle) return QuitRequestResult.Busy;
            if (blocked) return QuitRequestResult.Blocked;
            var saving = target != null && target.IsSaveInProgress;
            var dirty = target != null && target.IsDirty;
            if (QuitPolicy.Decide(dirty, saving) == QuitDecision.DeferWhileSaving)
                return QuitRequestResult.DeferredWhileSaving;
            Current = Phase.Confirming;
            return QuitRequestResult.Shown;
        }

        /// <summary>취소·ESC·X로 창이 다 닫힌 뒤 부른다.</summary>
        public bool Cancel()
        {
            if (Current != Phase.Confirming) return false;
            Current = Phase.Idle;
            return true;
        }

        /// <summary>"종료" 클릭: 먼저 저장하고 종료 연출을 시작한다.</summary>
        public bool Confirm(IQuitOutroView view, bool reducedMotion)
        {
            if (Current != Phase.Confirming) return false;
            if (target != null)
            {
                target.SaveBeforeQuit();
                SaveCount++;
            }

            outro = view;
            reduced = reducedMotion;
            elapsed = 0f;
            duration = reducedMotion
                ? QuitOutroTimeline.ReducedMotionDelay
                : view != null ? Mathf.Max(0f, view.QuitOutroDuration) : 0f;
            Current = Phase.Outro;
            if (view != null) view.BeginQuitOutro(reducedMotion);
            if (outroStarted != null) outroStarted();
            return true;
        }

        public void Tick(float deltaSeconds, bool anyInput)
        {
            if (Current == Phase.Outro)
            {
                elapsed += Mathf.Max(0f, deltaSeconds);
                var progress = duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
                if (anyInput) progress = 1f;
                if (outro != null && !reduced) outro.ApplyQuitOutro(progress);
                if (progress >= 1f) Current = Phase.WaitingSave;
            }

            // 저장이 끝나지 않았으면 접힌 화면에서 기다린다.
            if (Current == Phase.WaitingSave && (target == null || !target.IsSaveInProgress))
            {
                Current = Phase.Done;
                ShutdownCount++;
                if (target != null) target.Shutdown();
            }
        }
    }
}

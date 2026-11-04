using System;
using SubTerra.App.Audio;
using SubTerra.App.Core;
using SubTerra.App.State;
using SubTerra.App.UI;
using SubTerra.App.UI.MainMenu;
using SubTerra.Shared;
using SubTerra.Shared.Audio;
using UnityEngine;

namespace SubTerra.App.Integration
{
    /// <summary>B-184의 두 근접 반복음이 공유하는 단일 소스·설정·정지 처리.</summary>
    public abstract class ProximityLoopAudio : MonoBehaviour
    {
        public const float BlockedFadeOut = 0.08f;
        private SfxId loopId;
        private SfxLibrary library;
        private Func<bool> isFailureHandling = () => false;
        private Func<bool> canAct = () => true;
        private Func<bool> isRiding = () => false;
        private Func<bool> isPaused = () => UiPauseGate.IsHeld || ControlPreferences.IsSettingsOpen;
        private Func<bool> isMuted = () => SettingsRuntimeApplier.IsMasterMuted
            || SettingsRuntimeApplier.IsMuted(AudioChannel.Sfx);
        private Func<bool> isRunEnding = DefaultRunEnding;
        private bool playing;
        private bool paused;
        private bool errorLogged;

        public AudioSource Source { get; private set; }
        public float Level { get; private set; }
        protected bool IsPaused => isPaused();

        protected void ConfigurePlayback(SfxId id, Func<bool> failureHandling,
            Func<bool> playerCanAct, Func<bool> elevatorRiding)
        {
            StopNow();
            loopId = id;
            isFailureHandling = failureHandling ?? (() => false);
            canAct = playerCanAct ?? (() => true);
            isRiding = elevatorRiding ?? (() => false);
        }

        public void ConfigureForTests(SfxLibrary testLibrary, Func<bool> testMuted,
            Func<bool> testPaused, Func<bool> testRunEnding)
        {
            library = testLibrary;
            if (testMuted != null) isMuted = testMuted;
            if (testPaused != null) isPaused = testPaused;
            if (testRunEnding != null) isRunEnding = testRunEnding;
        }

        protected void TickPlayback(float target, float deltaTime, float fadeIn, float fadeOut)
        {
            if (library == null)
                library = SfxPlayer.Instance != null ? SfxPlayer.Instance.Library
                    : Resources.Load<SfxLibrary>(SfxLibrary.ResourcePath);
            if (isMuted() || library == null || !library.TryGetEntry(loopId, out var entry) || entry.Clip == null)
            {
                StopNow();
                return;
            }

            bool blocked = isFailureHandling() || !canAct() || isRiding() || isRunEnding();
            if (!blocked && isPaused())
            {
                if (playing && !paused) Source.Pause();
                paused = playing;
                return;
            }
            if (paused)
            {
                Source.UnPause();
                paused = false;
            }
            if (blocked) target = 0f;
            target = Mathf.Clamp01(target);
            float duration = blocked ? BlockedFadeOut : target > Level ? fadeIn : fadeOut;
            Level = duration <= 0f ? target
                : Mathf.MoveTowards(Level, target, Mathf.Max(0f, deltaTime) / duration);
            if (Level <= 0f)
            {
                StopNow();
                return;
            }
            if (Source == null)
            {
                var child = new GameObject("ProximityLoop_" + loopId);
                child.transform.SetParent(transform, false);
                Source = child.AddComponent<AudioSource>();
                Source.playOnAwake = false;
                Source.loop = true;
                Source.spatialBlend = 0f;
                Source.priority = 128;
            }
            Source.volume = entry.Volume * library.GetBusVolume(entry.Bus)
                * SettingsRuntimeApplier.EffectiveVolume(AudioChannel.Sfx) * Level;
            if (!playing)
            {
                Source.clip = entry.Clip;
                Source.pitch = entry.Pitch;
                Source.time = 0f;
                Source.Play();
                playing = true;
            }
        }

        protected void StopNow()
        {
            Level = 0f;
            paused = false;
            playing = false;
            if (Source != null)
            {
                Source.Stop();
                Source.volume = 0f;
            }
        }

        public abstract void Tick(float deltaTime);

        protected virtual void LateUpdate()
        {
            try { Tick(Time.unscaledDeltaTime); }
            catch (Exception exception)
            {
                StopNow();
                if (!errorLogged)
                {
                    errorLogged = true;
                    Debug.LogWarning("[ProximityLoopAudio] " + loopId + " 갱신 실패: " + exception.Message);
                }
            }
        }

        protected virtual void OnDisable() => StopNow();
        protected virtual void OnDestroy() => StopNow();

        private static bool DefaultRunEnding()
        {
            var root = GameBootstrapper.Instance;
            if (root == null || root.State == null || root.State.Run == null) return false;
            var phase = root.State.Run.LifecyclePhase;
            return phase == RunLifecyclePhase.Returning || phase == RunLifecyclePhase.Completed;
        }
    }
}

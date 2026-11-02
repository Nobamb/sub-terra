using System;
using SubTerra.App.UI;
using SubTerra.App.UI.MainMenu;
using SubTerra.Shared.Audio;
using UnityEngine;

namespace SubTerra.App.Audio
{
    public static class SliderDragTiming
    {
        public static double HeadEnd(double start, int samples, int frequency) => start + (double)samples / frequency;

        public static double ReleaseAt(double now, double loopStart, int samples, int frequency)
        {
            if (now <= loopStart) return loopStart;
            var length = (double)samples / frequency;
            // DSP double의 반올림 오차 때문에 정확한 경계에서 한 바퀴 더 기다리지 않는다.
            return loopStart + Math.Ceiling((now - loopStart) / length - 1e-9) * length;
        }
    }

    /// <summary>설정 슬라이더 전용. 원샷 풀과 분리해 앞·반복·끝을 DSP 시간에 맞춰 잇는다.</summary>
    public sealed class SliderDragLoopAudio : MonoBehaviour
    {
        public const SfxId HeadSfx = SfxId.SettingsSliderDragHead;
        public const SfxId LoopSfx = SfxId.SettingsSliderDragLoop;
        public const SfxId TailSfx = SfxId.SettingsSliderDragTail;
        public const bool KeepLoopWhileHeld = true;
        public const float StopFadeSeconds = 0.03f;
        private const double ScheduleLeadSeconds = 0.02;

        private SfxLibrary library;
        private readonly AudioSource[] sources = new AudioSource[3];
        private readonly SfxEntry[] entries = new SfxEntry[3];
        private Func<bool> muted = () => SettingsRuntimeApplier.IsMasterMuted
            || SettingsRuntimeApplier.IsMuted(AudioChannel.Sfx);
        private Func<bool> paused = () => UiPauseGate.IsHeld;
        private bool focused = true;
        private bool suspended;
        private bool held;
        private bool playing;
        private bool restartPending;
        private float stopFade;
        private double loopStart;
        private double tailStart = double.PositiveInfinity;
        private double endsAt;

        public bool IsHeld => held;
        public bool IsPlaying => playing;
        public bool CanPlay => isActiveAndEnabled && focused && !suspended && !muted() && !paused() && ResolveEntries();
        public AudioSource HeadSource => sources[0];
        public AudioSource LoopSource => sources[1];
        public AudioSource TailSource => sources[2];

        public void ConfigureForTests(SfxLibrary value, Func<bool> isMuted, Func<bool> isPaused)
        {
            library = value;
            muted = isMuted;
            paused = isPaused;
        }

        public bool Begin()
        {
            if (!CanPlay) return false;
            // 빠른 재잡기도 이전 끝소리를 짧게 줄인 뒤 새 앞소리로 시작한다.
            if (playing)
            {
                Cancel();
                held = restartPending = true;
            }
            else StartHead();
            return true;
        }

        public void Release()
        {
            if (!held) return;
            held = false;
            if (restartPending || !playing) return;
            var now = AudioSettings.dspTime;
            tailStart = SliderDragTiming.ReleaseAt(now, loopStart, entries[1].Clip.samples, entries[1].Clip.frequency);
            if (now < loopStart) sources[1].Stop();
            else sources[1].SetScheduledEndTime(tailStart);
            sources[2].PlayScheduled(tailStart);
            endsAt = SliderDragTiming.HeadEnd(tailStart, entries[2].Clip.samples, entries[2].Clip.frequency);
        }

        public void Cancel()
        {
            held = restartPending = false;
            if (!playing || stopFade > 0f) return;
            stopFade = StopFadeSeconds;
            // 아직 시작되지 않은 반복·끝은 페이드 중에도 새로 울리면 안 된다.
            var now = AudioSettings.dspTime;
            if (now < loopStart) sources[1].Stop();
            if (now < tailStart) sources[2].Stop();
        }

        private void StartHead()
        {
            EnsureSources();
            for (var i = 0; i < sources.Length; i++)
            {
                sources[i].Stop();
                sources[i].clip = entries[i].Clip;
                sources[i].pitch = 1f;
            }
            var start = AudioSettings.dspTime + ScheduleLeadSeconds;
            loopStart = SliderDragTiming.HeadEnd(start, entries[0].Clip.samples, entries[0].Clip.frequency);
            tailStart = endsAt = double.PositiveInfinity;
            held = playing = true;
            stopFade = 0f;
            ApplyVolume(1f);
            sources[0].PlayScheduled(start);
            sources[1].PlayScheduled(loopStart);
        }

        private void LateUpdate()
        {
            if (!playing) return;
            if (!CanPlay) Cancel();
            if (held && !KeepLoopWhileHeld && AudioSettings.dspTime >= loopStart) Release();
            if (stopFade > 0f)
            {
                stopFade = Mathf.Max(0f, stopFade - Time.unscaledDeltaTime);
                ApplyVolume(stopFade / StopFadeSeconds);
                if (stopFade > 0f) return;
                var restart = restartPending;
                var stillHeld = held;
                StopNow();
                if (restart && CanPlay)
                {
                    StartHead();
                    if (!stillHeld) Release();
                }
                return;
            }
            if (AudioSettings.dspTime >= endsAt) StopNow();
            else ApplyVolume(1f);
        }

        private void ApplyVolume(float fade)
        {
            if (library == null) return;
            for (var i = 0; i < sources.Length; i++)
                if (sources[i] != null && entries[i] != null)
                    sources[i].volume = entries[i].Volume * library.GetBusVolume(entries[i].Bus)
                        * SettingsRuntimeApplier.EffectiveVolume(AudioChannel.Sfx) * fade;
        }

        private bool ResolveEntries()
        {
            if (library == null) library = SfxPlayer.Instance != null
                ? SfxPlayer.Instance.Library : Resources.Load<SfxLibrary>(SfxLibrary.ResourcePath);
            return library != null
                && library.TryGetEntry(HeadSfx, out entries[0]) && entries[0].Clip != null
                && library.TryGetEntry(LoopSfx, out entries[1]) && entries[1].Clip != null
                && library.TryGetEntry(TailSfx, out entries[2]) && entries[2].Clip != null;
        }

        private void EnsureSources()
        {
            for (var i = 0; i < sources.Length; i++)
            {
                if (sources[i] != null) continue;
                var source = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 0f;
                source.loop = i == 1;
                source.ignoreListenerPause = true;
                sources[i] = source;
            }
        }

        private void StopNow()
        {
            held = playing = restartPending = false;
            stopFade = 0f;
            foreach (var source in sources) if (source != null) source.Stop();
        }

        private void OnDisable() => StopNow();
        private void OnApplicationFocus(bool value) { focused = value; if (!value) Cancel(); }
        private void OnApplicationPause(bool value) { suspended = value; if (value) Cancel(); }
    }
}

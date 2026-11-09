using System;
using SubTerra.App.UI.MainMenu;
using SubTerra.Shared.Audio;
using UnityEngine;

namespace SubTerra.App.Audio
{
    /// <summary>빈 슬롯을 쓸 때만 나는 TV 노이즈. 원샷 풀과 분리된 소스 하나를 쓴다.</summary>
    public sealed class EmptySlotNoiseAudio : MonoBehaviour
    {
        // B-183 ★ 재생 길이·호버 크기·페이드.
        public const SfxId EmptySlotNoiseSfx = SfxId.EmptySlotNoiseLoop;
        public const float EmptySlotSelectSeconds = 0.6f;
        public const float EmptySlotFadeOut = 0.25f;
        public const float EmptySlotHoverScale = 0.6f;
        public const bool EmptySlotHoverNoise = true;

        private SfxLibrary library;
        private AudioSource source;
        private bool hovered;
        private float selectRemaining;
        private float fade;
        private float fadeOutStart;
        private float fadeOutRemaining;
        private Func<bool> isMuted = () => SettingsRuntimeApplier.IsMasterMuted
            || SettingsRuntimeApplier.IsMuted(AudioChannel.Sfx);

        public AudioSource Source => source;

        public void SelectSlot(bool empty)
        {
            selectRemaining = empty ? EmptySlotSelectSeconds : 0f;
        }

        public void SetHover(bool empty) => hovered = empty;

        public void Tick(float deltaTime, bool blocked)
        {
            if (blocked || isMuted())
            {
                StopNow();
                return;
            }

            if (library == null)
            {
                library = SfxPlayer.Instance != null ? SfxPlayer.Instance.Library
                    : Resources.Load<SfxLibrary>(SfxLibrary.ResourcePath);
            }

            if (library == null || !library.TryGetEntry(EmptySlotNoiseSfx, out var entry) || entry.Clip == null)
            {
                StopNow();
                return;
            }

            var selected = selectRemaining > 0f;
            selectRemaining = Mathf.Max(0f, selectRemaining - deltaTime);
            var target = selected ? 1f : EmptySlotHoverNoise && hovered ? EmptySlotHoverScale : 0f;
            if (target > 0f)
            {
                fade = target;
                fadeOutStart = target;
                fadeOutRemaining = EmptySlotFadeOut;
            }
            else
            {
                fadeOutRemaining = Mathf.Max(0f, fadeOutRemaining - deltaTime);
                fade = fadeOutStart * fadeOutRemaining / EmptySlotFadeOut;
            }
            var volume = entry.Volume * library.GetBusVolume(entry.Bus)
                * SettingsRuntimeApplier.EffectiveVolume(AudioChannel.Sfx) * fade;
            if (volume <= 0f)
            {
                if (source != null)
                {
                    source.Stop();
                    source.volume = 0f;
                }
                return;
            }

            if (source == null)
            {
                source = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.loop = true;
                source.spatialBlend = 0f;
                source.ignoreListenerPause = true;
            }

            source.clip = entry.Clip;
            source.pitch = entry.Pitch;
            source.volume = volume;
            if (!source.isPlaying) source.Play();
        }

        public void StopNow()
        {
            hovered = false;
            selectRemaining = 0f;
            fade = 0f;
            fadeOutStart = 0f;
            fadeOutRemaining = 0f;
            if (source != null)
            {
                source.Stop();
                source.volume = 0f;
            }
        }

        private void OnDisable() => StopNow();

        public void ConfigureForTests(SfxLibrary testLibrary, Func<bool> testMuted)
        {
            library = testLibrary;
            if (testMuted != null) isMuted = testMuted;
        }
    }
}

using System;
using System.Collections.Generic;
using SubTerra.Shared.Audio;
using UnityEngine;

namespace SubTerra.App.Audio
{
    public enum SfxBus
    {
        UI = 0,
        World = 1,
        Alert = 2
    }

    /// <summary>효과음 한 칸. 음원 파일은 그대로 두고 음량·피치·재생 구간·페이드만 여기서 맞춘다.</summary>
    [Serializable]
    public sealed class SfxEntry
    {
        [SerializeField] private SfxId id;
        [SerializeField] private AudioClip clip;
        [SerializeField] private SfxBus bus;
        [Range(0f, 1f)] [SerializeField] private float volume = 1f;
        [Tooltip("기본 피치. 호출한 쪽이 넘기는 피치 배율과 곱해진다.")]
        [SerializeField] private float pitch = 1f;
        [Tooltip("클립 안에서 재생을 시작할 위치(초). 앞의 무음을 건너뛸 때 쓴다.")]
        [SerializeField] private float startTime;
        [Tooltip("클립 시간 기준 최대 재생 길이(초). 0이면 끝까지 재생한다.")]
        [SerializeField] private float maxDuration;
        [Tooltip("끝에서부터 음량을 0으로 내리는 시간(초).")]
        [SerializeField] private float fadeOut;

        public SfxEntry(SfxId id, SfxBus bus, float volume = 1f, float pitch = 1f,
            float startTime = 0f, float maxDuration = 0f, float fadeOut = 0f)
        {
            this.id = id;
            this.bus = bus;
            this.volume = volume;
            this.pitch = pitch;
            this.startTime = startTime;
            this.maxDuration = maxDuration;
            this.fadeOut = fadeOut;
        }

        public SfxId Id => id;
        public AudioClip Clip => clip;
        public SfxBus Bus => bus;
        public float Volume => volume;
        public float Pitch => pitch;
        public float StartTime => startTime;
        public float MaxDuration => maxDuration;
        public float FadeOut => fadeOut;

#if UNITY_EDITOR
        public void EditorSetClip(AudioClip value) => clip = value;
#endif
    }

    /// <summary>효과음 칸 목록과 버스 음량. Resources/SfxLibrary.asset 하나만 쓴다.</summary>
    [CreateAssetMenu(menuName = "SubTerra/Audio/Sfx Library")]
    public sealed class SfxLibrary : ScriptableObject
    {
        public const string ResourcePath = "SfxLibrary";

        [SerializeField] private List<SfxEntry> entries = new List<SfxEntry>();
        [Range(0f, 1f)] [SerializeField] private float uiBusVolume = 1f;
        [Range(0f, 1f)] [SerializeField] private float worldBusVolume = 1f;
        [Range(0f, 1f)] [SerializeField] private float alertBusVolume = 1f;

        public IReadOnlyList<SfxEntry> Entries => entries;

        public bool TryGetEntry(SfxId id, out SfxEntry entry)
        {
            for (var i = 0; i < entries.Count; i++)
            {
                if (entries[i] != null && entries[i].Id == id)
                {
                    entry = entries[i];
                    return true;
                }
            }

            entry = null;
            return false;
        }

        public float GetBusVolume(SfxBus bus)
        {
            switch (bus)
            {
                case SfxBus.World: return worldBusVolume;
                case SfxBus.Alert: return alertBusVolume;
                default: return uiBusVolume;
            }
        }

#if UNITY_EDITOR
        public void EditorAddEntry(SfxEntry entry) => entries.Add(entry);
        public void EditorSetBusVolumes(float ui, float world, float alert)
        {
            uiBusVolume = ui;
            worldBusVolume = world;
            alertBusVolume = alert;
        }
#endif
    }
}

using System;
using System.Collections.Generic;
using SubTerra.App.Audio;
using SubTerra.App.Core.Data;
using SubTerra.App.UI;
using SubTerra.App.UI.MainMenu;
using SubTerra.Gameplay.Building;
using SubTerra.Gameplay.Player;
using SubTerra.Shared.Audio;
using UnityEngine;

namespace SubTerra.App.Integration
{
    /// <summary>
    /// B-162: 긴급 탈출 포탈 근처에서만 나는 반복 대기음. 원샷 풀(SfxPlayer)과 따로 AudioSource 하나만 쓴다.
    /// 플레이어와 가장 가까운 포탈 하나를 기준으로 음량·또렷함을 정하고, 기준 포탈이 바뀌면 위치만 옮긴다.
    /// 효과음 음량·음소거를 따르고, UI 정지(UiPauseGate)·탐사 실패 처리 중에는 멈춘다(B-164: 정지 중 작게 → 멈춤).
    /// </summary>
    public sealed class PortalIdleLoopAudio : MonoBehaviour
    {
        public const float RescanInterval = 0.5f;

        [Tooltip("이 거리(칸) 안에서는 최대 음량")]
        [SerializeField, Min(0f)] private float fullVolumeDistance = PortalIdleLoopMath.DefaultFullDistance;
        [Tooltip("이 거리(칸) 이상이면 무음·정지")]
        [SerializeField, Min(0f)] private float silentDistance = PortalIdleLoopMath.DefaultSilentDistance;
        [Tooltip("0(가까움)→1(멂) 진행값에 대한 음량 배율")]
        [SerializeField] private AnimationCurve falloff = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
        [Tooltip("가까울 때 저역 통과 차단 주파수(Hz)")]
        [SerializeField] private float nearCutoff = 22000f;
        [Tooltip("멀 때 저역 통과 차단 주파수(Hz). 멀수록 먹먹하게 들린다.")]
        [SerializeField] private float farCutoff = 1500f;

        private readonly List<Transform> portals = new List<Transform>();
        private readonly List<Vector2> portalPositions = new List<Vector2>();
        private AudioSource source;
        private AudioLowPassFilter lowPass;
        private Transform listener;
        private RunFailureRuntimeController failure;
        private SfxLibrary library;
        private Func<bool> isMuted = () => SettingsRuntimeApplier.IsMasterMuted
            || SettingsRuntimeApplier.IsMuted(AudioChannel.Sfx);
        private Func<bool> isPaused = () => UiPauseGate.IsHeld;
        private float rescanTimer;
        private bool errorLogged;

        /// <summary>기준 포탈 번호(마지막 계산 기준). 없으면 -1.</summary>
        public int NearestIndex { get; private set; } = -1;
        /// <summary>거리 배율(칸 음량·버스·설정 제외).</summary>
        public float Gain { get; private set; }
        /// <summary>반복음을 틀고 있는가.</summary>
        public bool IsActive { get; private set; }
        public AudioSource Source => source;

        /// <summary>테스트용. 라이브러리·음소거·정지 판정을 주입한다.</summary>
        public void ConfigureForTests(SfxLibrary testLibrary, Func<bool> testMuted, Func<bool> testPaused)
        {
            library = testLibrary;
            if (testMuted != null) isMuted = testMuted;
            if (testPaused != null) isPaused = testPaused;
        }

        private void OnDisable()
        {
            Stop();
        }

        private void Update()
        {
            try
            {
                rescanTimer -= Time.unscaledDeltaTime;
                if (rescanTimer <= 0f)
                {
                    rescanTimer = RescanInterval;
                    Rescan();
                }

                portalPositions.Clear();
                for (var i = 0; i < portals.Count; i++)
                {
                    if (portals[i] != null)
                    {
                        portalPositions.Add(portals[i].position);
                    }
                }

                var suppressed = listener == null || (failure != null && failure.IsHandling);
                Tick(listener != null ? (Vector2)listener.position : Vector2.zero, portalPositions, suppressed);
            }
            catch (Exception exception)
            {
                Stop();
                if (!errorLogged)
                {
                    errorLogged = true;
                    Debug.LogWarning("[PortalIdleLoopAudio] 대기음 갱신 실패: " + exception.Message);
                }
            }
        }

        /// <summary>테스트와 Update가 같은 경로로 계산한다. suppressed(실패 처리·암전 등)나 UI 정지면 바로 멈추고, 풀리면 다시 낸다.</summary>
        public void Tick(Vector2 listenerPosition, IReadOnlyList<Vector2> portalList, bool suppressed)
        {
            NearestIndex = PortalIdleLoopMath.Nearest(listenerPosition, portalList, out var distance);
            var gain = NearestIndex >= 0
                ? PortalIdleLoopMath.Gain(distance, fullVolumeDistance, silentDistance, falloff)
                : 0f;
            if (suppressed || isPaused() || isMuted() || gain <= 0f || !TryResolve(out var entry, out var busVolume))
            {
                Gain = 0f;
                Stop();
                return;
            }

            EnsureSource();
            Gain = gain;
            var portal = portalList[NearestIndex];
            source.transform.position = new Vector3(portal.x, portal.y, source.transform.position.z);
            source.volume = entry.Volume * busVolume * SettingsRuntimeApplier.EffectiveVolume(AudioChannel.Sfx) * gain;
            lowPass.cutoffFrequency = PortalIdleLoopMath.Cutoff(gain, nearCutoff, farCutoff);
            if (!IsActive || source.clip != entry.Clip)
            {
                // 기준 포탈이 바뀌어도 여기로 오지 않으므로 처음부터 다시 틀지 않는다.
                source.clip = entry.Clip;
                source.Play();
                IsActive = true;
            }
        }

        private void Stop()
        {
            if (!IsActive)
            {
                return;
            }

            IsActive = false;
            if (source != null)
            {
                source.Stop();
            }
        }

        private bool TryResolve(out SfxEntry entry, out float busVolume)
        {
            entry = null;
            busVolume = 1f;
            if (library == null)
            {
                library = SfxPlayer.Instance != null
                    ? SfxPlayer.Instance.Library
                    : Resources.Load<SfxLibrary>(SfxLibrary.ResourcePath);
            }

            if (library == null || !library.TryGetEntry(SfxId.PortalIdleLoop, out entry) || entry.Clip == null)
            {
                return false;
            }

            busVolume = library.GetBusVolume(entry.Bus);
            return true;
        }

        private void EnsureSource()
        {
            if (source != null)
            {
                return;
            }

            var child = new GameObject("PortalIdleLoopSource");
            child.transform.SetParent(transform, false);
            source = child.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = 0f;
            source.priority = 128;
            lowPass = child.AddComponent<AudioLowPassFilter>();
        }

        private void Rescan()
        {
            if (listener == null)
            {
                var movement = FindAnyObjectByType<PlayerMovement>(FindObjectsInactive.Exclude);
                listener = movement != null ? movement.transform : null;
            }

            if (failure == null)
            {
                failure = FindAnyObjectByType<RunFailureRuntimeController>(FindObjectsInactive.Exclude);
            }

            // 근접 이름표와 같은 기준: 설치된 건물 중 긴급 탈출 포탈만.
            portals.Clear();
            var buildings = FindObjectsByType<BuildingInstance>(FindObjectsInactive.Exclude);
            for (var i = 0; i < buildings.Length; i++)
            {
                var building = buildings[i];
                if (building != null && building.BuildingId == DataIds.Buildings.EmergencyEscapePortal)
                {
                    portals.Add(building.transform);
                }
            }
        }
    }
}

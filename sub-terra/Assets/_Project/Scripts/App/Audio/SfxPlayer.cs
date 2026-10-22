using System;
using System.Collections.Generic;
using SubTerra.App.Core;
using SubTerra.App.UI.MainMenu;
using SubTerra.Shared.Audio;
using UnityEngine;

namespace SubTerra.App.Audio
{
    /// <summary>
    /// 효과음 재생기. SfxRequests를 구독하고, AudioSource 풀로 소리를 낸다.
    /// 시간은 비스케일이라 UiPauseGate로 게임이 멈춰도 UI 소리와 경보 반복이 이어진다.
    /// 마스터 음량은 AudioListener.volume이 맡고, 효과음 채널 음량(설정창 효과음 슬라이더)은 여기서 곱한다.
    /// </summary>
    public sealed class SfxPlayer : MonoBehaviour
    {
        public const int PoolVoiceCount = 8;
        public const float MinRepeatInterval = 0.05f;
        private const int UiPriority = 128;
        private const int AlertPriority = 0;

        private sealed class Voice
        {
            public AudioSource Source;
            public SfxId Id;
            public bool Active;
            public float StartedAt;
            public float EndsAt;
            public float Duration;
            public float FadeOut;
            public float Volume;
        }

        private static SfxPlayer instance;

        private readonly List<Voice> pool = new List<Voice>();
        private readonly Dictionary<SfxId, float> lastPlayedAt = new Dictionary<SfxId, float>();
        private readonly HashSet<SfxId> warned = new HashSet<SfxId>();
        private Voice alertVoice;
        private SfxLibrary library;
        private bool libraryWarned;
        private bool madeDontDestroy;
        private bool attachedToBootstrap;
        private Func<float> clock = () => Time.unscaledTime;
        private Func<bool> isMuted = () => SettingsRuntimeApplier.IsMasterMuted
            || SettingsRuntimeApplier.IsMuted(AudioChannel.Sfx);

        private SfxId alarmId;
        private int alarmRemaining;
        private float alarmInterval;
        private float alarmPitch;
        private float alarmNextAt;

        public static SfxPlayer Instance => instance;

        /// <summary>지금 소리가 나는 UI·World 소스 수.</summary>
        public int ActivePoolVoiceCount
        {
            get
            {
                var count = 0;
                for (var i = 0; i < pool.Count; i++)
                {
                    if (pool[i].Active) count++;
                }

                return count;
            }
        }

        public bool IsAlertActive => alertVoice != null && alertVoice.Active;
        public int AlarmRepeatsRemaining => alarmRemaining;

        /// <summary>요청이 실제 재생까지 간 횟수(연타 제한·무음에 걸린 요청은 세지 않는다).</summary>
        public int StartedCount { get; private set; }

        public SfxLibrary Library => ResolveLibrary();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            instance = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateOnLoad()
        {
            if (instance != null)
            {
                return;
            }

            try
            {
                new GameObject("SfxPlayer").AddComponent<SfxPlayer>();
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[SfxPlayer] 생성 실패: " + exception.Message);
            }
        }

        /// <summary>다른 코드가 SfxRequests를 거치지 않고 바로 부르는 진입점. 플레이어가 없으면 무음이다.</summary>
        public static void Play(SfxId id, float pitch = 1f)
        {
            SfxRequests.Play(id, pitch);
        }

        /// <summary>테스트용. 라이브러리·시계·음소거 판정을 주입한 플레이어를 만든다.</summary>
        public static SfxPlayer CreateForTests(SfxLibrary testLibrary, Func<float> testClock, Func<bool> testMuted)
        {
            instance = null;
            var go = new GameObject("SfxPlayerTest");
            var player = go.AddComponent<SfxPlayer>();
            // EditMode에서는 Awake가 불리지 않으므로 직접 초기화한다.
            player.Initialize();
            player.Subscribe();
            player.library = testLibrary;
            player.libraryWarned = true;
            if (testClock != null) player.clock = testClock;
            if (testMuted != null) player.isMuted = testMuted;
            return player;
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Initialize();
        }

        private void Initialize()
        {
            instance = this;
            if (pool.Count == 0)
            {
                BuildPool();
            }

            AttachToRuntimeRoot();
        }

        private void OnEnable()
        {
            if (instance == this)
            {
                Subscribe();
            }
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Subscribe()
        {
            Unsubscribe();
            SfxRequests.PlayRequested += HandlePlay;
            SfxRequests.AlarmRequested += HandleAlarm;
            SettingsRuntimeApplier.MasterMuteChanged += HandleMuteChanged;
            SettingsRuntimeApplier.ChannelChanged += HandleChannelChanged;
        }

        private void Unsubscribe()
        {
            SfxRequests.PlayRequested -= HandlePlay;
            SfxRequests.AlarmRequested -= HandleAlarm;
            SettingsRuntimeApplier.MasterMuteChanged -= HandleMuteChanged;
            SettingsRuntimeApplier.ChannelChanged -= HandleChannelChanged;
        }

        /// <summary>테스트용. EditMode에서는 OnDisable/OnDestroy가 불리지 않아 구독과 인스턴스를 직접 정리한다.</summary>
        public void ReleaseForTests()
        {
            Unsubscribe();
            StopAll();
            if (instance == this)
            {
                instance = null;
            }

            DestroyImmediate(gameObject);
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        private void Update()
        {
            if (!attachedToBootstrap)
            {
                AttachToRuntimeRoot();
            }

            Tick();
        }

        /// <summary>끝난 소리를 정리하고, 진행 중인 소리의 페이드와 경보 반복을 갱신한다.</summary>
        public void Tick()
        {
            var now = clock();
            for (var i = 0; i < pool.Count; i++)
            {
                TickVoice(pool[i], now);
            }

            TickVoice(alertVoice, now);

            if (alarmRemaining > 0 && now >= alarmNextAt)
            {
                StartAlarmBurst(now);
            }
        }

        private void TickVoice(Voice voice, float now)
        {
            if (voice == null || !voice.Active)
            {
                return;
            }

            var elapsed = now - voice.StartedAt;
            if (now >= voice.EndsAt)
            {
                StopVoice(voice);
                return;
            }

            // 매 프레임 다시 곱하므로 재생 중에 효과음 슬라이더를 움직여도 따라간다.
            voice.Source.volume = voice.Volume * SettingsRuntimeApplier.EffectiveVolume(AudioChannel.Sfx)
                * SfxPlayback.FadeGain(elapsed, voice.Duration, voice.FadeOut);
        }

        private void HandlePlay(SfxId id, float pitch)
        {
            try
            {
                PlayNow(id, pitch);
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[SfxPlayer] 재생 실패 " + id + ": " + exception.Message);
            }
        }

        private void HandleAlarm(SfxId id, int repeat, float interval, float pitch)
        {
            try
            {
                PlayAlarmNow(id, repeat, interval, pitch);
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[SfxPlayer] 경보 재생 실패 " + id + ": " + exception.Message);
            }
        }

        private void HandleMuteChanged()
        {
            if (isMuted())
            {
                StopAll();
            }
        }

        private void HandleChannelChanged(AudioChannel channel)
        {
            // 마스터는 MasterMuteChanged가 따로 처리한다. 효과음만 끄면 재생 중인 소리를 바로 멈춘다.
            if (channel == AudioChannel.Sfx && isMuted())
            {
                StopAll();
            }
        }

        public void StopAll()
        {
            alarmRemaining = 0;
            for (var i = 0; i < pool.Count; i++)
            {
                StopVoice(pool[i]);
            }

            StopVoice(alertVoice);
        }

        /// <summary>요청을 바로 처리한다. 음소거·연타 제한·빈 칸 판정은 모두 여기서 거친다.</summary>
        public void PlayNow(SfxId id, float pitch = 1f)
        {
            if (isMuted())
            {
                return;
            }

            var now = clock();
            if (lastPlayedAt.TryGetValue(id, out var last) && SfxPlayback.IsThrottled(now, last, MinRepeatInterval))
            {
                return;
            }

            if (!TryResolve(id, out var entry, out var busVolume))
            {
                return;
            }

            lastPlayedAt[id] = now;
            if (entry.Bus == SfxBus.Alert)
            {
                // 새 경보 계열 소리가 오면 진행 중인 반복은 끝낸다.
                alarmRemaining = 0;
                StartVoice(alertVoice, entry, busVolume, pitch, 0f, now);
                return;
            }

            StartVoice(AcquirePoolVoice(), entry, busVolume, pitch, 0f, now);
        }

        /// <summary>경보를 repeat번 반복한다. 진행 중인 반복은 끊고 새로 시작하며 루프는 쓰지 않는다.</summary>
        public void PlayAlarmNow(SfxId id, int repeat, float interval, float pitch)
        {
            if (repeat <= 0 || isMuted())
            {
                return;
            }

            if (!TryResolve(id, out _, out _))
            {
                return;
            }

            alarmId = id;
            alarmRemaining = repeat;
            alarmInterval = Mathf.Max(0.05f, interval);
            alarmPitch = pitch;
            StartAlarmBurst(clock());
        }

        private void StartAlarmBurst(float now)
        {
            alarmRemaining--;
            if (!TryResolve(alarmId, out var entry, out var busVolume))
            {
                alarmRemaining = 0;
                return;
            }

            // 마지막 한 번만 소리 길이를 다 쓰고, 나머지는 다음 반복 전에 끝낸다.
            var limit = alarmRemaining > 0 ? alarmInterval : 0f;
            StartVoice(alertVoice, entry, busVolume, alarmPitch, limit, now);
            alarmNextAt = now + alarmInterval;
        }

        private bool TryResolve(SfxId id, out SfxEntry entry, out float busVolume)
        {
            entry = null;
            busVolume = 1f;
            var lib = ResolveLibrary();
            if (lib == null)
            {
                return false;
            }

            if (!lib.TryGetEntry(id, out entry) || entry.Clip == null)
            {
                // 비어 있는 칸은 무음. 경고는 칸마다 한 번만 남긴다.
                if (warned.Add(id))
                {
                    Debug.LogWarning("[SfxPlayer] " + id + " 칸에 클립이 없어 무음으로 건너뜁니다.");
                }

                return false;
            }

            busVolume = lib.GetBusVolume(entry.Bus);
            return true;
        }

        private SfxLibrary ResolveLibrary()
        {
            if (library != null)
            {
                return library;
            }

            library = Resources.Load<SfxLibrary>(SfxLibrary.ResourcePath);
            if (library == null && !libraryWarned)
            {
                libraryWarned = true;
                Debug.LogWarning("[SfxPlayer] Resources/SfxLibrary.asset이 없어 효과음을 재생하지 않습니다.");
            }

            return library;
        }

        private void StartVoice(Voice voice, SfxEntry entry, float busVolume, float pitchMultiplier,
            float realTimeLimit, float now)
        {
            var clip = entry.Clip;
            var pitch = Mathf.Clamp(entry.Pitch * pitchMultiplier, SfxPlayback.MinPitch, SfxPlayback.MaxPitch);
            var plan = SfxPlayback.Plan(clip.length, entry.StartTime, entry.MaxDuration, entry.FadeOut, pitch, realTimeLimit);

            var source = voice.Source;
            source.Stop();
            source.clip = clip;
            source.pitch = pitch;
            source.volume = entry.Volume * busVolume * SettingsRuntimeApplier.EffectiveVolume(AudioChannel.Sfx);
            source.time = plan.StartTime;
            source.Play();

            voice.Id = entry.Id;
            voice.Active = true;
            voice.StartedAt = now;
            voice.Duration = plan.RealDuration;
            voice.EndsAt = now + plan.RealDuration;
            voice.FadeOut = plan.FadeOut;
            voice.Volume = entry.Volume * busVolume;
            StartedCount++;
        }

        private Voice AcquirePoolVoice()
        {
            Voice oldest = null;
            for (var i = 0; i < pool.Count; i++)
            {
                var voice = pool[i];
                if (!voice.Active)
                {
                    return voice;
                }

                if (oldest == null || voice.StartedAt < oldest.StartedAt)
                {
                    oldest = voice;
                }
            }

            // 풀이 찼으면 가장 오래된 UI·World 소리를 끊는다. Alert 소스는 건드리지 않는다.
            return oldest;
        }

        private static void StopVoice(Voice voice)
        {
            if (voice == null)
            {
                return;
            }

            voice.Active = false;
            if (voice.Source != null)
            {
                voice.Source.Stop();
                voice.Source.clip = null;
            }
        }

        private void BuildPool()
        {
            for (var i = 0; i < PoolVoiceCount; i++)
            {
                pool.Add(new Voice { Source = CreateSource(UiPriority) });
            }

            alertVoice = new Voice { Source = CreateSource(AlertPriority) };
        }

        private AudioSource CreateSource(int priority)
        {
            var source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
            source.priority = priority;
            source.ignoreListenerPause = true;
            return source;
        }

        private void AttachToRuntimeRoot()
        {
            var bootstrap = GameBootstrapper.Instance;
            if (bootstrap == null || bootstrap.IsDuplicateDiscarded)
            {
                if (Application.isPlaying && transform.parent == null && !madeDontDestroy)
                {
                    madeDontDestroy = true;
                    DontDestroyOnLoad(gameObject);
                }

                return;
            }

            if (transform.parent != bootstrap.transform)
            {
                transform.SetParent(bootstrap.transform, false);
            }

            attachedToBootstrap = true;
        }
    }
}

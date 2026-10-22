using System.Collections.Generic;
using SubTerra.App.UI.MainMenu;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SubTerra.App.Audio
{
    /// <summary>
    /// 이름이 "BGM_"로 시작하는 AudioSource에 배경음 채널 음량을 곱한다.
    /// 처음 본 음량을 기준으로 기억하므로 재생·멈춤·전환 코드는 음량을 신경 쓰지 않아도 된다.
    /// 마스터 음량은 AudioListener.volume이 따로 곱한다.
    /// </summary>
    public sealed class BgmVolumeApplier : MonoBehaviour
    {
        public const string SourceNamePrefix = "BGM_";
        private const float RescanInterval = 1f;

        private static readonly Dictionary<AudioSource, float> baselines = new Dictionary<AudioSource, float>();
        private static readonly List<AudioSource> stale = new List<AudioSource>();
        private static BgmVolumeApplier instance;

        private float nextScanAt;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            instance = null;
            baselines.Clear();
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
                var go = new GameObject("BgmVolumeApplier");
                DontDestroyOnLoad(go);
                go.AddComponent<BgmVolumeApplier>();
            }
            catch (System.Exception exception)
            {
                Debug.LogWarning("[BgmVolumeApplier] 생성 실패: " + exception.Message);
            }
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
        }

        private void OnEnable()
        {
            if (instance != this)
            {
                return;
            }

            SceneManager.sceneLoaded += OnSceneLoaded;
            SettingsRuntimeApplier.ChannelChanged += OnChannelChanged;
            ApplyNow();
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SettingsRuntimeApplier.ChannelChanged -= OnChannelChanged;
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
            // 씬이 로드된 뒤에 생긴 BGM_ 소스도 놓치지 않게 가끔 다시 훑는다.
            if (Time.unscaledTime < nextScanAt)
            {
                return;
            }

            nextScanAt = Time.unscaledTime + RescanInterval;
            ApplyNow();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ApplyNow();
        }

        private static void OnChannelChanged(AudioChannel channel)
        {
            if (channel == AudioChannel.Bgm)
            {
                ApplyNow();
            }
        }

        /// <summary>BGM_ 소스를 찾아 기준 음량 × 배경음 채널 음량을 넣는다.</summary>
        public static void ApplyNow()
        {
            var channel = SettingsRuntimeApplier.EffectiveVolume(AudioChannel.Bgm);
            var sources = Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (var i = 0; i < sources.Length; i++)
            {
                var source = sources[i];
                if (source == null || !source.name.StartsWith(SourceNamePrefix, System.StringComparison.Ordinal))
                {
                    continue;
                }

                if (!baselines.TryGetValue(source, out var baseline))
                {
                    baseline = source.volume;
                    baselines[source] = baseline;
                }

                source.volume = Mathf.Clamp01(baseline * channel);
            }

            PruneDestroyed();
        }

        /// <summary>테스트용. 기억해 둔 기준 음량을 비운다.</summary>
        public static void ResetBaselinesForTests()
        {
            baselines.Clear();
        }

        private static void PruneDestroyed()
        {
            stale.Clear();
            foreach (var pair in baselines)
            {
                if (pair.Key == null)
                {
                    stale.Add(pair.Key);
                }
            }

            for (var i = 0; i < stale.Count; i++)
            {
                baselines.Remove(stale[i]);
            }

            stale.Clear();
        }
    }
}

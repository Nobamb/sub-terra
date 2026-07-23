using System;
using UnityEngine;
using SubTerra.App.State;

namespace SubTerra.App.Core
{
    /// <summary>
    /// Bootstrap 전역 Root.
    /// 단일 DontDestroyOnLoad 루트만 유지하고,
    /// 카탈로그 검증 → 세이브 확인 → State 생성/복원 → MainMenu 순으로 초기화한다.
    /// </summary>
    public sealed class GameBootstrapper : MonoBehaviour
    {
        public static GameBootstrapper Instance { get; private set; }

        public GameState State { get; private set; }
        public bool InitializationFailed { get; private set; }

        /// <summary>한 번 성공한 초기화 이후 재실행으로 상태를 덮어쓰지 않기 위한 플래그.</summary>
        public bool IsInitialized { get; private set; }

        /// <summary>Awake에서 중복 Root로 판정되어 폐기 대상인지.</summary>
        public bool IsDuplicateDiscarded { get; private set; }

        public IDataCatalogPort Catalog { get; set; } = new NullCatalog();
        public ISavePort Save { get; set; } = new EmptySave();
        public ISceneLoader Scenes { get; set; } = new UnitySceneLoader();

        // Domain Reload 꺼짐 설정에서도 static 싱글톤이 이전 플레이 값을 유지하지 않게 한다.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null;
        }

        /// <summary>
        /// 테스트 간 static 오염 제거. 런타임 게임 플로우에서는 호출하지 않는다.
        /// </summary>
        public static void ResetInstanceForTests()
        {
            if (Instance != null)
            {
                var go = Instance.gameObject;
                Instance = null;
                if (go != null)
                {
                    DestroyImmediate(go);
                }
            }

            Instance = null;
        }

        private void Awake()
        {
            // 동일 Root 재생성 시 새 인스턴스는 폐기 대상으로 표시하고 기존 전역 상태를 보존한다.
            if (Instance != null && Instance != this)
            {
                MarkDiscardedDuplicate();
                return;
            }

            ClaimInstance();
        }

        private void OnDestroy()
        {
            // 활성 Root만 static을 비운다. 중복본 파괴가 전역 Instance를 지우면 안 된다.
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Start()
        {
            // Destroy()는 프레임 말 제거라 중복 Root에도 Start가 호출될 수 있다.
            // 폐기 대상·비활성 Root는 초기화·씬 전환을 절대 수행하지 않는다.
            if (IsDuplicateDiscarded || !CanInitializeAsRoot())
            {
                return;
            }

            // Play Mode에서는 Start에서 자동 초기화한다.
            // Edit Mode 테스트는 Awake만 거친 뒤 포트를 주입하고 Initialize를 직접 호출한다.
            if (!IsInitialized && !InitializationFailed)
            {
                Initialize();
            }
        }

        public bool Initialize()
        {
            return Initialize(Catalog, Save, Scenes);
        }

        /// <summary>
        /// 초기화 계약(순서 고정):
        /// 1) 포트 null 검사
        /// 2) 카탈로그 검증
        /// 3) 세이브 확인 후 State 신규/복원
        /// 4) 상태 완전성 검사
        /// 5) MainMenu 전환(성공 시에만)
        /// 실패 시 전환을 막고 원인을 한 번만 기록한다.
        /// </summary>
        public bool Initialize(IDataCatalogPort catalog, ISavePort save, ISceneLoader scenes)
        {
            // 중복 Bootstrap(폐기 예정 Root)은 기존 전역 State/씬을 건드리지 않는다.
            if (IsDuplicateDiscarded)
            {
                return false;
            }

            if (!CanInitializeAsRoot())
            {
                return false;
            }

            // 이미 성공한 초기화는 의도된 리셋 경로 없이는 State를 덮어쓰지 않는다.
            if (IsInitialized && !InitializationFailed)
            {
                return true;
            }

            if (InitializationFailed)
            {
                return false;
            }

            Catalog = catalog;
            Save = save;
            Scenes = scenes;

            try
            {
                if (catalog == null || save == null || scenes == null)
                {
                    return Fail("Bootstrap ports missing.");
                }

                if (!catalog.Validate(out var reason))
                {
                    // reason에는 데이터 ID 등만 두고, 세이브 원문·경로·비밀값은 넣지 않는 것이 포트 구현 책임이다.
                    return Fail("Catalog validation failed: " + (string.IsNullOrEmpty(reason) ? "unknown" : reason));
                }

                GameState nextState;
                if (save.HasSave)
                {
                    nextState = save.Load();
                }
                else
                {
                    nextState = GameState.CreateNew();
                }

                // 세이브가 null 또는 하위 상태 누락을 반환하면 성공으로 취급하지 않는다.
                if (!GameState.IsComplete(nextState))
                {
                    return Fail("State initialization failed: incomplete or missing state.");
                }

                State = nextState;

                if (!scenes.Load(SceneNames.MainMenu))
                {
                    return Fail("Scene transition failed: MainMenu.");
                }

                IsInitialized = true;
                return true;
            }
            catch (Exception exception)
            {
                // 예외 메시지에 로컬 경로가 포함될 수 있어 타입명만 남긴다.
                return Fail("Initialization exception: " + exception.GetType().Name);
            }
        }

        /// <summary>
        /// 이 객체가 전역 Root로 초기화를 수행해도 되는지 판정한다.
        /// Instance가 비어 있으면 채택하고, 다른 활성 Root가 있으면 거부한다.
        /// </summary>
        private bool CanInitializeAsRoot()
        {
            if (IsDuplicateDiscarded)
            {
                return false;
            }

            if (Instance == null)
            {
                ClaimInstance();
                return true;
            }

            return Instance == this;
        }

        private void ClaimInstance()
        {
            Instance = this;
            IsDuplicateDiscarded = false;
            if (Application.isPlaying)
            {
                DontDestroyOnLoad(gameObject);
            }
        }

        private void MarkDiscardedDuplicate()
        {
            // Destroy()는 프레임 말 제거이므로 Start/Initialize 가드용 플래그와 비활성을 먼저 건다.
            IsDuplicateDiscarded = true;
            enabled = false;
            if (Application.isPlaying)
            {
                Destroy(gameObject);
            }
        }

        /// <summary>실패 폐쇄: 플래그 설정 + 단일 오류 로그. 세이브 본문/경로는 기록하지 않는다.</summary>
        private bool Fail(string reason)
        {
            InitializationFailed = true;
            IsInitialized = false;
            Debug.LogError("[SubTerra] " + reason);
            return false;
        }
    }
}

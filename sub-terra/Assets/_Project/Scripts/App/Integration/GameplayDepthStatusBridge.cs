using SubTerra.App.State;
using SubTerra.Gameplay.Drone;
using SubTerra.Gameplay.Player;
using UnityEngine;

namespace SubTerra.App.Integration
{
    /// <summary>
    /// 플레이어 월드 Y를 깊이(m)로 변환해 GameState에 반영한다.
    /// HUD는 DepthChanged만 구독하므로, 이 브리지가 실시간 깊이 생산자 역할을 한다.
    /// 임계치·위험 판정은 하지 않고 위치 → 정수 깊이 변환만 수행한다.
    /// </summary>
    public sealed class GameplayDepthStatusBridge : MonoBehaviour
    {
        [SerializeField] private Transform playerTransform;
        [SerializeField] private PlayerMovement playerMovement;
        [Tooltip("지표면 기준 Y. 플레이어가 이 값보다 아래에 있을수록 깊이가 커진다.")]
        [SerializeField] private float surfaceY;
        [Tooltip("0이면 매 프레임 갱신. 0보다 크면 해당 초 간격으로 샘플링.")]
        [SerializeField, Min(0f)] private float updateInterval;

        private GameState gameState;
        private float nextUpdateTime;
        private int lastPublishedDepth = int.MinValue;

        /// <summary>현재 브리지가 마지막으로 GameState에 반영한 깊이(m).</summary>
        public int LastPublishedDepth => lastPublishedDepth == int.MinValue ? 0 : lastPublishedDepth;

        public float SurfaceY
        {
            get => surfaceY;
            set
            {
                surfaceY = value;
                PublishDepth(force: true);
            }
        }

        public void BindGameState(GameState state)
        {
            gameState = state;
            PublishDepth(force: true);
        }

        public void SetPlayer(Transform player)
        {
            playerTransform = player;
            if (player != null)
            {
                playerMovement = player.GetComponent<PlayerMovement>();
            }

            PublishDepth(force: true);
        }

        public void SetSurfaceY(float y)
        {
            SurfaceY = y;
        }

        /// <summary>위치 변경 직후(텔레포트·리스폰 등) 즉시 깊이를 다시 샘플링한다.</summary>
        public void Refresh()
        {
            PublishDepth(force: true);
        }

        private void Update()
        {
            if (updateInterval > 0f && Time.unscaledTime < nextUpdateTime)
            {
                return;
            }

            if (updateInterval > 0f)
            {
                nextUpdateTime = Time.unscaledTime + updateInterval;
            }

            PublishDepth(force: false);
        }

        private void PublishDepth(bool force)
        {
            if (gameState == null)
            {
                return;
            }

            var player = ResolvePlayer();
            if (player == null)
            {
                return;
            }

            // DroneContext와 동일한 공식: max(0, round(surfaceY - playerY))
            var depth = DroneContextCalculator.CalculateDepth(surfaceY, player.position.y);
            if (!force && depth == lastPublishedDepth)
            {
                return;
            }

            lastPublishedDepth = depth;
            // SetDepth는 동일 값이면 이벤트를 생략한다. MaximumDepth는 RunState에서 자동 갱신.
            gameState.SetDepth(depth);
        }

        private Transform ResolvePlayer()
        {
            if (playerTransform != null)
            {
                return playerTransform;
            }

            if (playerMovement == null)
            {
                playerMovement = FindFirstObjectByType<PlayerMovement>();
            }

            if (playerMovement != null)
            {
                playerTransform = playerMovement.transform;
            }

            return playerTransform;
        }
    }
}

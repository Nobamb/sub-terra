using UnityEngine;

namespace SubTerra.Gameplay.Player
{
    /// <summary>플레이어가 사다리 Trigger 안에 있을 때만 중력 없는 수직 이동 모드를 연다.</summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class LadderZone : MonoBehaviour
    {
        [Header("Top Exit")]
        [SerializeField, Min(0f)] private float topExitFootTolerance = 0.08f;

        private void Reset()
        {
            var zone = GetComponent<Collider2D>();
            if (zone != null)
            {
                zone.isTrigger = true;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var movement = other.GetComponentInParent<PlayerMovement>();
            if (movement != null)
            {
                movement.EnterLadder(this);
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            var movement = other.GetComponentInParent<PlayerMovement>();
            if (movement != null)
            {
                movement.ExitLadder(this);
            }
        }

        /// <summary>
        /// 사다리 상단 발판에 올라선 상태인지 판정한다.
        /// Trigger가 발판과 조금 겹쳐도, 발이 상단 경계에 닿으면 등반 상태를 끝낼 수 있다.
        /// </summary>
        public bool IsAtTopExit(Collider2D climber)
        {
            var zone = GetComponent<Collider2D>();
            if (zone == null || climber == null)
            {
                return false;
            }

            return climber.bounds.min.y >= zone.bounds.max.y - topExitFootTolerance;
        }
    }
}

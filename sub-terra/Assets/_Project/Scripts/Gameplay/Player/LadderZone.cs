using UnityEngine;

namespace SubTerra.Gameplay.Player
{
    /// <summary>플레이어가 사다리 Trigger 안에 있을 때만 중력 없는 수직 이동 모드를 연다.</summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class LadderZone : MonoBehaviour
    {
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
    }
}

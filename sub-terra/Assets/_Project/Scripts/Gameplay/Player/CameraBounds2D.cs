using UnityEngine;

namespace SubTerra.Gameplay.Player
{
    /// <summary>Scene별 카메라가 표시할 수 있는 고정 월드 영역을 제공합니다.</summary>
    [DisallowMultipleComponent]
    public sealed class CameraBounds2D : MonoBehaviour
    {
        [SerializeField] private Vector2 center;
        [SerializeField] private Vector2 size = new(20f, 12f);

        private int version;

        public Bounds WorldBounds => new(
            new Vector3(center.x, center.y, 0f),
            new Vector3(Mathf.Max(0f, size.x), Mathf.Max(0f, size.y), 0f));

        public int Version => version;

        public void SetWorldBounds(Vector2 newCenter, Vector2 newSize)
        {
            center = newCenter;
            size = new Vector2(
                Mathf.Max(0f, newSize.x),
                Mathf.Max(0f, newSize.y));
            version++;
        }

        private void OnValidate()
        {
            size.x = Mathf.Max(0f, size.x);
            size.y = Mathf.Max(0f, size.y);
            version++;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.25f, 0.8f, 1f, 0.9f);
            Gizmos.DrawWireCube(WorldBounds.center, WorldBounds.size);
        }
    }
}

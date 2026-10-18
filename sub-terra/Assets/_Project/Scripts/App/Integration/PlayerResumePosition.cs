using SubTerra.App.Save;
using SubTerra.Gameplay.Player;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace SubTerra.App.Integration
{
    /// <summary>복원된 지형과 플레이어 몸 전체를 검사한 뒤 입력 게이트가 열리기 전에 배치한다.</summary>
    public static class PlayerResumePosition
    {
        private const float Skin = 0.02f;
        private const int SearchRadius = 3;

        public static PlayerPositionSaveData Capture(PlayerMovement player)
        {
            if (player == null) return null;
            var value = player.Position;
            var position = new PlayerPositionSaveData { hasPosition = true, x = value.x, y = value.y };
            return position.IsValid ? position : null;
        }

        public static bool TryRestore(PlayerMovement player, Tilemap terrain, PlayerPositionSaveData saved)
        {
            if (saved == null || !saved.IsValid) return true; // 구세이브는 기존 시작점 유지.
            if (player == null || terrain == null) return false;
            Physics2D.SyncTransforms();
            if (!player.TryGetBodyBounds(out var bounds)) return false;
            var offset = (Vector2)bounds.center - player.Position;
            var halfSize = (Vector2)bounds.extents;
            var target = new Vector2(saved.x, saved.y);
            if (!IsClear(player, terrain, target + offset, halfSize)
                || !HasSupport(player, terrain, target + offset, halfSize))
            {
                var found = false;
                var cell = terrain.WorldToCell(target);
                // 막힌 원점은 가까운 발판부터 확인한다. 전 광산 탐색/지형 변경은 하지 않는다.
                for (var radius = 1; radius <= SearchRadius && !found; radius++)
                for (var y = radius; y >= -radius && !found; y--)
                for (var x = -radius; x <= radius && !found; x++)
                {
                    if (Mathf.Max(Mathf.Abs(x), Mathf.Abs(y)) != radius) continue;
                    var candidateCell = cell + new Vector3Int(x, y, 0);
                    var bottom = terrain.CellToWorld(candidateCell);
                    var candidate = new Vector2(
                        terrain.GetCellCenterWorld(candidateCell).x - offset.x,
                        bottom.y + halfSize.y - offset.y + Skin);
                    if (!IsClear(player, terrain, candidate + offset, halfSize)
                        || !HasSupport(player, terrain, candidate + offset, halfSize)) continue;
                    target = candidate;
                    found = true;
                }

                if (!found)
                {
                    // 근처에 설 자리가 없으면 Scene의 기존 안전 시작점으로 복귀한다.
                    target = player.Position;
                    if (!IsClear(player, terrain, target + offset, halfSize)
                        || !HasSupport(player, terrain, target + offset, halfSize)) return false;
                }
            }

            player.ExitLadder();
            player.SetMoveInput(0f);
            player.SetVerticalMoveInput(0f);
            var body = player.GetComponent<Rigidbody2D>();
            if (body != null)
            {
                body.linearVelocity = Vector2.zero;
                body.angularVelocity = 0f;
                body.position = target;
            }
            player.transform.position = new Vector3(target.x, target.y, player.transform.position.z);
            Physics2D.SyncTransforms();
            if (Camera.main != null)
                Camera.main.SendMessage("SnapToTarget", SendMessageOptions.DontRequireReceiver);
            return true;
        }

        private static bool IsClear(PlayerMovement player, Tilemap terrain, Vector2 center, Vector2 halfSize)
        {
            var inset = Vector2.Max(Vector2.one * Skin, halfSize - Vector2.one * Skin);
            var min = terrain.WorldToCell(center - inset);
            var max = terrain.WorldToCell(center + inset);
            var limits = terrain.cellBounds;
            if (!limits.Contains(min) || !limits.Contains(max)) return false;
            for (var y = min.y; y <= max.y; y++)
            for (var x = min.x; x <= max.x; x++)
                if (terrain.HasTile(new Vector3Int(x, y, 0))) return false;

            foreach (var hit in Physics2D.OverlapBoxAll(center, inset * 2f, 0f))
            {
                if (hit.isTrigger || hit.transform == player.transform || hit.transform.IsChildOf(player.transform)) continue;
                return false;
            }
            return true;
        }

        private static bool HasSupport(PlayerMovement player, Tilemap terrain, Vector2 center, Vector2 halfSize)
        {
            var feet = center + Vector2.down * (halfSize.y + Skin);
            if (terrain.HasTile(terrain.WorldToCell(feet + Vector2.down * 0.12f))) return true;
            foreach (var hit in Physics2D.RaycastAll(feet, Vector2.down, 0.12f))
                if (!hit.collider.isTrigger && !hit.transform.IsChildOf(player.transform) && hit.normal.y > 0.5f)
                    return true;
            foreach (var hit in Physics2D.OverlapBoxAll(center, halfSize * 2f, 0f))
                if (hit.isTrigger && hit.GetComponentInParent<LadderZone>() != null) return true;
            return false;
        }
    }
}

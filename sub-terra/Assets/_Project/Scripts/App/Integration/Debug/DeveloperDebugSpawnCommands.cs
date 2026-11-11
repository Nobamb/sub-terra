#if UNITY_EDITOR || SUBTERRA_BUILD_DEVELOPMENT
using System;
using System.Globalization;
using SubTerra.App.Core;
using SubTerra.App.Core.Data;
using SubTerra.Gameplay.Mining;
using SubTerra.Gameplay.Player;
using SubTerra.Gameplay.Snapshot;
using SubTerra.Gameplay.Structural;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace SubTerra.App.Integration
{
    /// <summary>
    /// 스폰·낙석 명령이 만지는 광산 씬 대상. 탐색을 비우면 플레이 중 인스턴스를 찾는다.
    /// </summary>
    public sealed class DeveloperDebugSpawnWorld
    {
        /// <summary>PlayerMiningController.reach 기본값. Enter 목표 칸 계산에 쓴다.</summary>
        public const float DefaultMiningReach = 1.35f;

        public DeveloperDebugSpawnWorld(
            string sceneName,
            PlayerMovement player,
            Tilemap foreground,
            MiningTileResolver resolver,
            WorldSnapshotSystem snapshot,
            float miningReach = DefaultMiningReach)
        {
            SceneName = sceneName ?? string.Empty;
            Player = player;
            Foreground = foreground;
            Resolver = resolver;
            Snapshot = snapshot;
            MiningReach = miningReach;
        }

        public string SceneName { get; }
        public PlayerMovement Player { get; }
        public Tilemap Foreground { get; }
        public MiningTileResolver Resolver { get; }
        public WorldSnapshotSystem Snapshot { get; }
        public float MiningReach { get; }

        /// <summary>hit·fall·hitgas 명령이 쓰는 선택 대상. 비어 있으면 해당 명령이 없음으로 알린다.</summary>
        public StructuralIntegritySystem Structural { get; set; }
        public GasExposureEffectController GasEffect { get; set; }
        public GameDataCatalog Catalog { get; set; }
    }

    /// <summary>
    /// 125-4 블록 스폰. 125-2 등록부에 명령을 넣고, 타일 ID는 여기 둔다.
    /// DemoWorld는 참조하지 않는다. 가스는 설치만 하고, 신호 타일의 채굴 규칙은 바꾸지 않는다.
    /// B-193: 바라보는 직선의 빈 칸을 먼저 쓰고, 막혀 있으면 Enter 자동 채굴과 같은 칸의 블록을 바꾼다.
    /// </summary>
    public static class DeveloperDebugSpawnCommands
    {
        private const string RockId = "tile.rock.normal";
        private const string CopperId = "tile.copper";
        private const string IronId = "tile.iron";
        private const string LithiumId = "tile.lithium";
        private const string GasId = "tile.gas-pocket";
        private const string SignalId = "tile.locked.signal";
        private const string GoldSuffix = ".gold";
        private const int MaximumStraightScanCells = 3;
        public const string NoTargetMessage = "눈앞에 놓거나 바꿀 칸이 없습니다";

        public static void Register(DeveloperDebugCommandRegistry registry)
        {
            if (registry == null)
            {
                return;
            }

            registry.Register(new DeveloperDebugCommandSpec(
                "spawn",
                new[] { "spawn", "생성" },
                "<rock|copper|iron|lithium|gas|engine_fuel> [golden]",
                "spawn copper",
                HandleSpawn));
        }

        public static DeveloperDebugSpawnWorld FindLive()
        {
            var scene = SceneManager.GetActiveScene();
            var mining = UnityEngine.Object.FindAnyObjectByType<PlayerMiningController>(FindObjectsInactive.Include);
            var bootstrap = GameBootstrapper.Instance;
            return new DeveloperDebugSpawnWorld(
                scene.name,
                UnityEngine.Object.FindAnyObjectByType<PlayerMovement>(FindObjectsInactive.Include),
                FindForeground(),
                UnityEngine.Object.FindAnyObjectByType<MiningTileResolver>(FindObjectsInactive.Include),
                UnityEngine.Object.FindAnyObjectByType<WorldSnapshotSystem>(FindObjectsInactive.Include),
                mining != null ? mining.Reach : DeveloperDebugSpawnWorld.DefaultMiningReach)
            {
                Structural = UnityEngine.Object.FindAnyObjectByType<StructuralIntegritySystem>(FindObjectsInactive.Include),
                GasEffect = UnityEngine.Object.FindAnyObjectByType<GasExposureEffectController>(FindObjectsInactive.Include),
                Catalog = bootstrap != null ? bootstrap.AssignedCatalog as GameDataCatalog : null
            };
        }

        /// <summary>
        /// Enter 자동 채굴과 같은 목표 칸(MiningSystem.TryGetDirectionalCell)을 고른다.
        /// 옆 칸을 먼저 보고 없으면 위·아래를 보며, 블록이 있는 칸만 대상이라 빈 칸에는 놓지 않는다.
        /// </summary>
        public static bool TryResolveFacingCell(
            Tilemap tilemap,
            Vector2 origin,
            float facingDirection,
            float range,
            out Vector3Int cell)
        {
            return MiningSystem.TryGetDirectionalCell(tilemap, origin, facingDirection, range, out cell);
        }

        /// <summary>
        /// spawn 목표 칸. 바라보는 쪽 직선(플레이어와 같은 줄)에서 비어 있는 칸을 먼저 쓰고,
        /// 그 줄이 막혀 있을 때만 Enter 채굴과 같은 칸의 블록을 바꾼다.
        /// 플레이어가 서 있는 칸에는 놓지 않는다.
        /// </summary>
        public static bool TryResolveSpawnCell(
            Tilemap tilemap,
            Vector2 origin,
            float facingDirection,
            float range,
            out Vector3Int cell)
        {
            cell = default;
            if (tilemap == null || Mathf.Approximately(facingDirection, 0f))
            {
                return false;
            }

            var direction = facingDirection > 0f ? 1 : -1;
            var candidate = tilemap.WorldToCell(origin);
            var allowed = Mathf.Max(0f, range) + 0.5f;
            for (var step = 0; step < MaximumStraightScanCells; step++)
            {
                candidate.x += direction;
                var center = tilemap.GetCellCenterWorld(candidate);
                if (Mathf.Abs(center.x - origin.x) > allowed || Mathf.Abs(center.y - origin.y) > allowed)
                {
                    break;
                }

                if (!tilemap.HasTile(candidate))
                {
                    cell = candidate;
                    return true;
                }
            }

            return TryResolveFacingCell(tilemap, origin, facingDirection, range, out cell);
        }

        /// <summary>
        /// "명령 종류 [golden]" 인자를 타일 ID로 바꾼다. spawn·hit·fall이 같은 종류·별칭을 쓴다.
        /// </summary>
        public static bool TryReadTileId(
            string[] tokens,
            string label,
            string example,
            out string tileId,
            out string error)
        {
            tileId = string.Empty;
            error = label + " 인자 오류. 예: " + example;
            if (tokens == null || tokens.Length < 2 || tokens.Length > 3)
            {
                return false;
            }

            if (!TryKind(tokens[1], out var baseId, out var goldAllowed))
            {
                return false;
            }

            var gold = false;
            if (tokens.Length == 3)
            {
                if (!IsGold(tokens[2]))
                {
                    return false;
                }

                gold = true;
            }

            if (gold && !goldAllowed)
            {
                error = "엔진연료는 골드 변형이 없습니다";
                return false;
            }

            tileId = gold ? baseId + GoldSuffix : baseId;
            error = string.Empty;
            return true;
        }

        private static string HandleSpawn(string[] tokens, DeveloperDebugCommandContext context)
        {
            if (!TryReadTileId(tokens, "spawn", "spawn copper", out var tileId, out var argumentError))
            {
                return argumentError;
            }

            if (context == null)
            {
                return "광산 정보 없음";
            }

            var world = context.FindSpawnWorld();
            if (world == null)
            {
                return "광산 정보 없음";
            }

            if (!string.Equals(world.SceneName, SceneNames.Integration, StringComparison.Ordinal))
            {
                return "광산 씬에서만 사용할 수 있습니다";
            }

            if (world.Player == null)
            {
                return "플레이어 없음";
            }

            if (world.Foreground == null)
            {
                return "광산 타일맵 없음";
            }

            if (world.Resolver == null)
            {
                return "타일 리졸버 없음";
            }

            if (world.Snapshot == null)
            {
                return "스냅샷 없음";
            }

            if (!TryResolveSpawnCell(
                    world.Foreground,
                    world.Player.Position,
                    world.Player.FacingDirection,
                    world.MiningReach,
                    out var cell))
            {
                return NoTargetMessage;
            }

            if (!world.Resolver.TryFindTileById(tileId, out var tile) || tile == null)
            {
                return "타일 없음: " + tileId;
            }

            var durability = 1f;
            if (world.Resolver.TryResolve(tile, out var definition) && definition.durability > 0f)
            {
                durability = definition.durability;
            }

            world.Foreground.SetTile(cell, tile);
            world.Snapshot.RecordChangedTile(cell.x, cell.y, tileId, durability);
            RefreshCollider(world.Foreground);
            return "spawn " + tileId + " ("
                + cell.x.ToString(CultureInfo.InvariantCulture)
                + ", "
                + cell.y.ToString(CultureInfo.InvariantCulture)
                + ")";
        }

        private static bool TryKind(string token, out string tileId, out bool goldAllowed)
        {
            goldAllowed = true;
            if (IsAny(token, "rock", "normal", "일반", "암석"))
            {
                tileId = RockId;
                return true;
            }

            if (IsAny(token, "copper", "구리"))
            {
                tileId = CopperId;
                return true;
            }

            if (IsAny(token, "iron", "철"))
            {
                tileId = IronId;
                return true;
            }

            if (IsAny(token, "lithium", "리튬"))
            {
                tileId = LithiumId;
                return true;
            }

            if (IsAny(token, "gas", "가스"))
            {
                tileId = GasId;
                return true;
            }

            if (IsAny(token, "engine_fuel", "엔진연료"))
            {
                tileId = SignalId;
                goldAllowed = false;
                return true;
            }

            tileId = string.Empty;
            return false;
        }

        private static bool IsGold(string token)
        {
            return IsAny(token, "golden", "gold", "골드");
        }

        private static bool IsAny(string token, params string[] expected)
        {
            if (expected == null)
            {
                return false;
            }

            for (var i = 0; i < expected.Length; i++)
            {
                if (Same(token, expected[i]))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool Same(string token, string expected)
        {
            return string.Equals(token, expected, Comparison(expected));
        }

        private static StringComparison Comparison(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return StringComparison.Ordinal;
            }

            for (var i = 0; i < value.Length; i++)
            {
                if (value[i] > 127)
                {
                    return StringComparison.Ordinal;
                }
            }

            return StringComparison.OrdinalIgnoreCase;
        }

        private static Tilemap FindForeground()
        {
            var maps = UnityEngine.Object.FindObjectsByType<Tilemap>(FindObjectsInactive.Include);
            for (var i = 0; i < maps.Length; i++)
            {
                var map = maps[i];
                if (map != null && map.name == DepthDarknessBlockVisual.ForegroundTilemapName)
                {
                    return map;
                }
            }

            return null;
        }

        private static void RefreshCollider(Tilemap tilemap)
        {
            if (tilemap == null)
            {
                return;
            }

            var collider = tilemap.GetComponent<TilemapCollider2D>();
            if (collider == null)
            {
                return;
            }

            tilemap.RefreshAllTiles();
            if (collider.hasTilemapChanges)
            {
                collider.ProcessTilemapChanges();
            }
        }
    }
}
#endif

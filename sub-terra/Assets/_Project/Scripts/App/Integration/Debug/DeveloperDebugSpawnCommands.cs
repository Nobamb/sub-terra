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
    /// 스폰 명령이 만지는 광산 씬 대상. 탐색을 비우면 플레이 중 인스턴스를 찾는다.
    /// </summary>
    public sealed class DeveloperDebugSpawnWorld
    {
        public DeveloperDebugSpawnWorld(
            string sceneName,
            PlayerMovement player,
            Tilemap foreground,
            MiningTileResolver resolver,
            WorldSnapshotSystem snapshot)
        {
            SceneName = sceneName ?? string.Empty;
            Player = player;
            Foreground = foreground;
            Resolver = resolver;
            Snapshot = snapshot;
        }

        public string SceneName { get; }
        public PlayerMovement Player { get; }
        public Tilemap Foreground { get; }
        public MiningTileResolver Resolver { get; }
        public WorldSnapshotSystem Snapshot { get; }
    }

    /// <summary>
    /// 125-4 블록 스폰. 125-2 등록부에 명령을 넣고, 타일 ID는 여기 둔다.
    /// DemoWorld는 참조하지 않는다. 가스는 설치만 하고, 신호 타일의 채굴 규칙은 바꾸지 않는다.
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
        private const string ArgError = "spawn 인자 오류. 예: spawn copper";
        private const string TileArguments = "<rock|copper|iron|lithium|gas|engine_fuel> [golden]";

        public static void Register(DeveloperDebugCommandRegistry registry)
        {
            if (registry == null)
            {
                return;
            }

            registry.Register(new DeveloperDebugCommandSpec(
                "spawn",
                new[] { "spawn", "생성" },
                TileArguments,
                "spawn copper",
                HandleSpawn));
            registry.Register(new DeveloperDebugCommandSpec(
                "hit", new[] { "hit" }, TileArguments, "hit iron golden", HandleHit));
            registry.Register(new DeveloperDebugCommandSpec(
                "fall", new[] { "fall" }, TileArguments, "fall lithium", HandleFall));
            registry.Register(new DeveloperDebugCommandSpec(
                "hitgas", new[] { "hitgas" }, "[n]", "hitgas 3", HandleHitGas));
            registry.Register(new DeveloperDebugCommandSpec(
                "falling", new[] { "falling" }, "[n]m", "falling 30m", HandleFalling));
        }

        public static DeveloperDebugSpawnWorld FindLive()
        {
            var scene = SceneManager.GetActiveScene();
            return new DeveloperDebugSpawnWorld(
                scene.name,
                UnityEngine.Object.FindAnyObjectByType<PlayerMovement>(FindObjectsInactive.Include),
                FindForeground(),
                UnityEngine.Object.FindAnyObjectByType<MiningTileResolver>(FindObjectsInactive.Include),
                UnityEngine.Object.FindAnyObjectByType<WorldSnapshotSystem>(FindObjectsInactive.Include));
        }

        public static bool TryResolveFacingCell(
            Tilemap tilemap,
            Vector2 origin,
            float facingDirection,
            out Vector3Int cell,
            float reach = PlayerMiningController.DefaultReach)
        {
            return MiningSystem.TryGetDirectionalCell(tilemap, origin, facingDirection, reach, out cell);
        }

        private static string HandleSpawn(string[] tokens, DeveloperDebugCommandContext context)
        {
            if (!TryRead(tokens, out var tileId, out var argumentError))
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

            var miningController = world.Player.GetComponent<PlayerMiningController>();
            if (!TryResolveFacingCell(
                    world.Foreground,
                    world.Player.Position,
                    world.Player.FacingDirection,
                    out var cell,
                    miningController != null ? miningController.Reach : PlayerMiningController.DefaultReach))
            {
                return "채굴 방향에 교체할 타일이 없습니다";
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

        private static string HandleHit(string[] tokens, DeveloperDebugCommandContext context)
        {
            if (!TryGetCommandTile(tokens, context, out var world, out var tile, out var error)) return error;
            var survival = context.FindPlayer();
            if (survival == null) return "플레이어 생존 정보 없음";
            if (!world.Resolver.TryResolve(tile, out var definition)) return "타일 정의 없음";

            var bootstrap = GameBootstrapper.Instance;
            var catalog = bootstrap != null ? bootstrap.AssignedCatalog as GameDataCatalog : null;
            float multiplier = IntegrationRuntimeBinder.GetCollapseDamageMultiplier(definition, catalog);
            return survival.ApplyCollapseImpact(multiplier) ? "hit " + definition.tileId : "낙석 피해가 적용되지 않았습니다";
        }

        private static string HandleFall(string[] tokens, DeveloperDebugCommandContext context)
        {
            if (!TryGetCommandTile(tokens, context, out var world, out var tile, out var error)) return error;
            var structural = UnityEngine.Object.FindAnyObjectByType<StructuralIntegritySystem>();
            if (structural == null || world.Player == null || world.Foreground == null)
                return "낙석에 필요한 광산 정보 없음";

            Vector2 head = world.Player.Position;
            if (world.Player.TryGetBodyBounds(out var bounds)) head.y = bounds.max.y;
            Vector3Int cell = world.Foreground.WorldToCell(head) + Vector3Int.up;
            return structural.TryPlayDebugCollapse(cell, tile) ? "fall " + tokens[1] : "낙석을 시작할 수 없습니다";
        }

        private static string HandleHitGas(string[] tokens, DeveloperDebugCommandContext context)
        {
            if (!IsMine(context?.FindSpawnWorld())) return "광산 씬에서만 사용할 수 있습니다";
            int tickCount = 1;
            if (tokens.Length > 2 || (tokens.Length == 2 && (!int.TryParse(tokens[1], NumberStyles.None,
                    CultureInfo.InvariantCulture, out tickCount) || tickCount < 1)))
                return "hitgas 인자 오류. 예: hitgas 3";

            var effect = UnityEngine.Object.FindAnyObjectByType<GasExposureEffectController>();
            if (effect == null) return "가스 노출 컨트롤러 없음";
            effect.ScheduleDebugGasDamage(tickCount);
            return "hitgas " + tickCount.ToString(CultureInfo.InvariantCulture);
        }

        private static string HandleFalling(string[] tokens, DeveloperDebugCommandContext context)
        {
            if (!IsMine(context?.FindSpawnWorld())) return "광산 씬에서만 사용할 수 있습니다";
            float distance = 0f;
            if (tokens.Length > 2 || (tokens.Length == 2 && (!tokens[1].EndsWith("m", StringComparison.OrdinalIgnoreCase)
                    || !float.TryParse(tokens[1].Substring(0, tokens[1].Length - 1),
                        NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out distance)
                    || float.IsNaN(distance) || float.IsInfinity(distance))))
                return "falling 인자 오류. 예: falling 30m";

            var survival = context.FindPlayer();
            if (survival == null) return "플레이어 생존 정보 없음";
            return survival.ApplyDebugLanding(distance) ? "낙하 피해 적용" : "낙하 피해가 적용되지 않았습니다";
        }

        private static bool TryGetCommandTile(string[] tokens, DeveloperDebugCommandContext context,
            out DeveloperDebugSpawnWorld world, out TileBase tile, out string error)
        {
            world = null;
            tile = null;
            if (!TryRead(tokens, out var tileId, out error))
            {
                if (error == ArgError) error = tokens[0] + " 인자 오류. 예: " + tokens[0] + " copper";
                return false;
            }

            world = context?.FindSpawnWorld();
            if (!IsMine(world)) error = "광산 씬에서만 사용할 수 있습니다";
            else if (world.Resolver == null) error = "타일 리졸버 없음";
            else if (!world.Resolver.TryFindTileById(tileId, out tile)) error = "타일 없음: " + tileId;
            else return true;
            return false;
        }

        private static bool IsMine(DeveloperDebugSpawnWorld world)
        {
            return world != null && string.Equals(world.SceneName, SceneNames.Integration, StringComparison.Ordinal);
        }

        private static bool TryRead(string[] tokens, out string tileId, out string error)
        {
            tileId = string.Empty;
            error = ArgError;
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

        private static bool TryKind(string token, out string tileId, out bool goldAllowed)
        {
            goldAllowed = true;
            if (IsAny(token, "rock", "normal", "일반"))
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

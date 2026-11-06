using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SubTerra.App.Core;
using SubTerra.App.Integration;
using SubTerra.App.Save;
using SubTerra.App.UI.Tutorial;
using SubTerra.Gameplay.Building;
using SubTerra.Gameplay.Player;
using SubTerra.Gameplay.Power;
using SubTerra.Gameplay.Snapshot;
using SubTerra.Shared;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace SubTerra.App.Tests.PlayMode.Outpost
{
    /// <summary>
    /// B-185-1: 실제 플레이 흐름(Bootstrap → 새 게임 → 광산)에서 BuildingPlacementSystem.TryPlaceAt으로 설치해
    /// 코어 범위 안 충전기의 켜짐 연출이 시작되는지 본다. 코어 먼저 / 충전기 먼저 두 순서와 복원 무연출을 함께 확인한다.
    /// </summary>
    public sealed class PromptB185PowerOnEffectPlayModeTests
    {
        private const string CoreId = "building.outpost_core.basic";
        private const string ChargerId = "building.charger.basic";
        private const string ProtectedGroundTileName = "ElevatorProtectedBlock";

        private readonly List<PowerFacility> started = new();
        private readonly List<int> batches = new();
        private bool bootstrapSmoke;

        [SetUp]
        public void SetUp()
        {
            started.Clear();
            batches.Clear();
            PowerOnEffect.Started += OnStarted;
            PowerOnEffect.BatchStarted += OnBatchStarted;
        }

        [TearDown]
        public void TearDown()
        {
            PowerOnEffect.Started -= OnStarted;
            PowerOnEffect.BatchStarted -= OnBatchStarted;
        }

        [UnityTearDown]
        public IEnumerator CleanSmokeScene()
        {
            if (!bootstrapSmoke) yield break;
            bootstrapSmoke = false;
            GameBootstrapper.ResetInstanceForTests();
            var clean = SceneManager.CreateScene("B185SmokeCleanup-" + Guid.NewGuid().ToString("N"));
            SceneManager.SetActiveScene(clean);
            for (var i = SceneManager.sceneCount - 1; i >= 0; i--)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene != clean && scene.isLoaded) yield return SceneManager.UnloadSceneAsync(scene);
            }
        }

        private void OnStarted(PowerFacility facility, bool isBatch) => started.Add(facility);
        private void OnBatchStarted(int count, Vector3 center) => batches.Add(count);

        [UnityTest]
        public IEnumerator Bootstrap_Mine_PlaceChargerInCoreRange_BothOrders_PlaysPowerOnEffect()
        {
            bootstrapSmoke = true;
            GameBootstrapper.ResetInstanceForTests();
            yield return SceneManager.LoadSceneAsync(SceneNames.Bootstrap);
            yield return WaitFor(() => SceneManager.GetActiveScene().name == SceneNames.MainMenu
                && SaveRuntimeController.Instance != null && SaveRuntimeController.Instance.InventoryService != null,
                "Bootstrap runtime and MainMenu");
            var runtime = SaveRuntimeController.Instance;
            IsolateSaves(runtime);
            Assert.That(runtime.StartNewGame(1), Is.True);
            yield return WaitFor(() => SceneManager.GetActiveScene().name == SceneNames.SurfaceBase
                && runtime.IsUiReady, "new-game SurfaceBase");
            Assert.That(runtime.TryStartExploration(out var reason), Is.True, reason);
            yield return WaitFor(() => SceneManager.GetActiveScene().name == SceneNames.Integration
                && runtime.IsUiReady, "mine runtime ready");
            var binder = Object.FindAnyObjectByType<IntegrationRuntimeBinder>();
            Assert.That(binder, Is.Not.Null);
            yield return WaitFor(() => binder.IsUiActivated, "IntegrationRuntimeBinder.IsUiActivated");
            yield return CloseBriefing();
            Assert.That(started, Is.Empty, "탐사 진입(복원) 중에는 연출 없음");

            var placement = Object.FindAnyObjectByType<BuildingPlacementSystem>();
            var elevator = Object.FindAnyObjectByType<ElevatorController>();
            Assert.That(placement, Is.Not.Null);
            Assert.That(elevator, Is.Not.Null);
            var tilemap = (Tilemap)Get(placement, "terrainTilemap");
            Assert.That(tilemap, Is.Not.Null);
            var definitions = (BuildingPlacementDefinition[])Get(placement, "restoreDefinitions");
            var core = definitions.Single(definition => definition != null && definition.BuildingId == CoreId);
            var charger = definitions.Single(definition => definition != null && definition.BuildingId == ChargerId);
            // 이 검사는 켜짐 판정만 본다. 설치 거리·구역 제한과 비용은 대상이 아니므로 풀어 둔다.
            Set(placement, "placementOrigin", null);
            Set(placement, "allowedPlacementArea", null);
            placement.SetResourceWallet(new FreeWallet());
            var groundTile = FindGroundTile(tilemap);
            Assert.That(groundTile, Is.Not.Null);

            // 엘리베이터 범위(10칸) 밖의 두 지점. 두 지점끼리도 코어 범위가 겹치지 않게 떨어뜨린다.
            Vector3Int elevatorCell = tilemap.WorldToCell(elevator.transform.position);
            var siteA = elevatorCell + new Vector3Int(18, -14, 0);
            var siteB = elevatorCell + new Vector3Int(18, -44, 0);
            Carve(tilemap, groundTile, siteA);
            Carve(tilemap, groundTile, siteB);

            // A. 코어 먼저 → 범위 안에 충전기 설치.
            var placedCore = Place(placement, core, siteA);
            Assert.That(started, Is.Empty, "코어 자체는 연출 대상이 아님");
            var chargerA = Place(placement, charger, siteA + new Vector3Int(4, 0, 0));
            var facilityA = chargerA.GetComponent<PowerFacility>();
            Assert.That(facilityA, Is.Not.Null);
            Assert.That(facilityA.IsInSupplyRange, Is.True, Describe(facilityA, placedCore, elevator));
            Assert.That(facilityA.PowerOnEffectInstance, Is.Not.Null, "범위 안 설치 → 연출 시작");
            Assert.That(facilityA.PowerOnEffectInstance.IsPlaying, Is.True);
            Assert.That(started, Is.EqualTo(new[] { facilityA }));
            Assert.That(batches, Is.EqualTo(new[] { 1 }));

            // B. 충전기 먼저(범위 밖, 연출 없음) → 코어를 나중에 설치해 범위가 생김.
            started.Clear();
            batches.Clear();
            var chargerB = Place(placement, charger, siteB + new Vector3Int(4, 0, 0));
            var facilityB = chargerB.GetComponent<PowerFacility>();
            Assert.That(facilityB.IsInSupplyRange, Is.False, Describe(facilityB, null, elevator));
            Assert.That(facilityB.PowerOnEffectInstance, Is.Null);
            Assert.That(started, Is.Empty, "범위 밖 설치는 연출 없음");
            MovePlayerNear(siteB, tilemap);
            yield return new WaitForSeconds(0.5f);
            Place(placement, core, siteB);
            Assert.That(facilityB.IsInSupplyRange, Is.True);
            Assert.That(facilityB.PowerOnEffectInstance.IsPlaying, Is.True, "코어를 나중에 설치해도 연출 시작");
            Assert.That(started, Is.EqualTo(new[] { facilityB }));
            Assert.That(batches, Is.EqualTo(new[] { 1 }));

            // 실제 Update로 진행되는지(정지 아님) 확인하고 화면을 남긴다.
            yield return new WaitForSeconds(PowerOnEffect.GlowRiseSeconds);
            Assert.That(facilityB.PowerOnEffectInstance.CurrentGlowAlpha, Is.GreaterThan(0f));
            yield return SaveScreen("charger-after-core.png");
            yield return new WaitForSeconds(PowerOnEffect.PowerOnDuration + 0.2f);
            Assert.That(facilityB.PowerOnEffectInstance.IsPlaying, Is.False, "0.7초 뒤 연출 정리");
            Assert.That(facilityB.PowerOnEffectInstance.EffectRoot.activeSelf, Is.False);

            // 같은 상태를 다시 판정해도(켜짐→켜짐) 연출 없음.
            started.Clear();
            batches.Clear();
            Object.FindAnyObjectByType<PowerNetworkSystem>().RequestRebuild();
            Assert.That(started, Is.Empty);

            // 복원(이어하기·탐사 진입과 같은 RestoreSnapshot)은 연출 없음.
            var world = Object.FindAnyObjectByType<WorldSnapshotSystem>();
            Assert.That(world, Is.Not.Null);
            var snapshot = world.CaptureSnapshot();
            Assert.That(world.RestoreSnapshot(snapshot), Is.True, world.LastRestoreFailureReason);
            yield return null;
            yield return null;
            var restored = Object.FindObjectsByType<PowerFacility>(FindObjectsSortMode.None)
                .Where(facility => facility.GetComponent<BuildingInstance>() != null
                    && facility.GetComponent<BuildingInstance>().BuildingId == ChargerId)
                .ToArray();
            Assert.That(restored.Count(facility => facility.IsInSupplyRange), Is.GreaterThanOrEqualTo(2));
            Assert.That(started, Is.Empty, "복원으로 켜진 시설은 연출 없음");
            Assert.That(batches, Is.Empty);
        }

        private static GameObject Place(BuildingPlacementSystem placement, BuildingPlacementDefinition definition, Vector3Int origin)
        {
            placement.Select(definition);
            Assert.That(placement.CanPlaceLocationAt(origin, out var failure), Is.True,
                definition.BuildingId + " at " + origin + ": " + failure);
            var result = placement.TryPlaceAt(origin);
            Assert.That(result.IsSuccess, Is.True, definition.BuildingId + ": " + result.Failure);
            var instance = Object.FindObjectsByType<BuildingInstance>(FindObjectsSortMode.None)
                .Single(candidate => candidate.InstanceId == result.InstanceId);
            return instance.gameObject;
        }

        /// <summary>코어(2x2)와 충전기(2x2)가 들어갈 빈 공간과 그 아래 바닥을 만든다.</summary>
        private static void Carve(Tilemap tilemap, TileBase ground, Vector3Int origin)
        {
            for (var x = origin.x - 3; x <= origin.x + 8; x++)
            {
                tilemap.SetTile(new Vector3Int(x, origin.y - 1, origin.z), ground);
                for (var y = origin.y; y <= origin.y + 4; y++)
                {
                    tilemap.SetTile(new Vector3Int(x, y, origin.z), null);
                }
            }
        }

        private static TileBase FindGroundTile(Tilemap tilemap)
        {
            var bounds = tilemap.cellBounds;
            foreach (var cell in bounds.allPositionsWithin)
            {
                var tile = tilemap.GetTile(cell);
                if (tile != null && tile.name != ProtectedGroundTileName) return tile;
            }
            return null;
        }

        private static void MovePlayerNear(Vector3Int origin, Tilemap tilemap)
        {
            var movement = Object.FindAnyObjectByType<PlayerMovement>();
            if (movement == null) return;
            var target = tilemap.GetCellCenterWorld(origin + new Vector3Int(-2, 1, 0));
            var body = movement.GetComponent<Rigidbody2D>();
            if (body != null)
            {
                body.position = target;
                body.linearVelocity = Vector2.zero;
            }
            movement.transform.position = target;
            Physics2D.SyncTransforms();
        }

        private static string Describe(PowerFacility facility, GameObject coreObject, ElevatorController elevator)
        {
            var position = facility.transform.position;
            return "facility=" + position
                + "; core=" + (coreObject != null ? coreObject.transform.position.ToString() : "none")
                + "; elevator=" + elevator.transform.position
                + "; elevatorDistance=" + Vector2.Distance(position, elevator.transform.position);
        }

        private static IEnumerator CloseBriefing()
        {
            var briefing = Object.FindAnyObjectByType<StartBriefingPopupMotion>();
            if (briefing == null || !briefing.isActiveAndEnabled) yield break;
            var guidance = Object.FindAnyObjectByType<DemoObjectiveView>();
            Assert.That(guidance, Is.Not.Null);
            yield return WaitFor(() => briefing.IsShown, "start briefing shown");
            Assert.That(guidance.TryCloseGuidance(), Is.True);
            yield return WaitFor(() => Time.timeScale > 0f, "briefing closed");
        }

        private static void IsolateSaves(SaveRuntimeController runtime)
        {
            var testRoot = Path.GetFullPath(Path.Combine(Application.dataPath,
                "../Temp/B185-smoke-" + Guid.NewGuid().ToString("N")));
            Assert.That(Directory.Exists(testRoot), Is.False, "새 임시 경로만 사용한다");
            var paths = new SavePathPolicy(testRoot);
            var fileSystem = new PhysicalSaveFileSystem();
            var mapper = new SaveDataMapper(new SystemSaveClock());
            var json = new SaveJsonCodec(new SaveMigrationService());
            // 새 게임·자동저장·썸네일 모두 사용자 슬롯 대신 이번 검사 전용 Temp로 보낸다.
            Set(runtime, "saveService", new SaveService(fileSystem, paths, mapper, json));
            Set(runtime, "loadService", new LoadService(fileSystem, paths, mapper, json));
            Set(runtime, "<Thumbnails>k__BackingField", new SaveThumbnailService(paths));
        }

        private static IEnumerator WaitFor(Func<bool> condition, string description)
        {
            var deadline = Time.realtimeSinceStartup + 20f;
            while (!condition() && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(condition(), Is.True, "Timed out: " + description);
        }

        private static IEnumerator SaveScreen(string fileName)
        {
            yield return new WaitForEndOfFrame();
            // 화면 확인은 절반 해상도로 남긴다(rule 17).
            var texture = ScreenCapture.CaptureScreenshotAsTexture();
            var target = RenderTexture.GetTemporary(Mathf.Max(1, texture.width / 2), Mathf.Max(1, texture.height / 2));
            var previous = RenderTexture.active;
            Graphics.Blit(texture, target);
            RenderTexture.active = target;
            var small = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
            small.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
            small.Apply();
            var output = Path.GetFullPath(Path.Combine(Application.dataPath,
                "../../work_process/MVP2/power/B-185-1-verification/" + fileName));
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            File.WriteAllBytes(output, small.EncodeToPNG());
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(target);
            Object.Destroy(texture);
            Object.Destroy(small);
        }

        private static object Get(object target, string field) =>
            target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);

        private static void Set(object target, string field, object value) =>
            target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);

        private sealed class FreeWallet : IResourceWallet
        {
            public bool CanAfford(IReadOnlyList<ItemCostDto> costs) => true;
            public bool TrySpend(IReadOnlyList<ItemCostDto> costs) => true;
        }
    }
}

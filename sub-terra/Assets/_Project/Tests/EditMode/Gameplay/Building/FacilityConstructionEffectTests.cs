using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SubTerra.Gameplay.Power;
using SubTerra.Shared;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace SubTerra.Gameplay.Building.Tests
{
    /// <summary>B-206: 새 설치 0.5초 건설 연출. 시각 전용이며 기능·복원·이동·모션 감소는 그대로다.</summary>
    public sealed class FacilityConstructionEffectTests
    {
        private const string PrefabRoot = "Assets/_Project/Prefabs/Gameplay/";

        private static readonly (string id, string path)[] Facilities =
        {
            ("building.ladder.basic", PrefabRoot + "Traversal/Ladder_Buildable.prefab"),
            ("building.storage.basic", PrefabRoot + "Power/StorageFacility.prefab"),
            ("building.charger.basic", PrefabRoot + "Power/ChargerFacility.prefab"),
            ("building.light.basic", PrefabRoot + "Power/LightFacility.prefab"),
            ("building.clinic.basic", PrefabRoot + "Power/ClinicFacility.prefab"),
            ("building.settlement.basic", PrefabRoot + "Power/SettlementFacility.prefab"),
            ("building.outpost_core.basic", PrefabRoot + "Power/OutpostCore.prefab"),
            ("building.escape_portal.emergency", PrefabRoot + "Buildings/EmergencyEscapePortal.prefab"),
            ("building.support.basic", PrefabRoot + "Buildings/SupportPillar.prefab")
        };

        public static IEnumerable<string> FacilityIds => Facilities.Select(entry => entry.id);

        private readonly List<GameObject> spawned = new();
        private bool previousReduceMotion;

        [SetUp]
        public void SetUp()
        {
            previousReduceMotion = AccessibilityPreferences.ReduceMotion;
            AccessibilityPreferences.ReduceMotion = false;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in spawned)
                if (go != null) Object.DestroyImmediate(go);
            spawned.Clear();
            while (PowerOnEffect.IsSuppressed) PowerOnEffect.EndSuppression();
            PowerOnEffect.FlushPendingStarts();
            BuildingPlacementActivity.ResetForTests();
            AccessibilityPreferences.ReduceMotion = previousReduceMotion;
        }

        [Test]
        public void EveryFacility_HasItsOwnRecipe_FinishingWithinTheBuildLength()
        {
            var styles = new HashSet<string>();
            foreach (string id in FacilityIds)
            {
                Assert.That(FacilityConstructionRecipes.TryGet(id, out ConstructionRecipe recipe), Is.True, id);
                Assert.That(recipe.Pieces, Is.Not.Empty, id);
                Assert.That(recipe.Shapes, Is.Not.Empty, id);
                Assert.That(styles.Add(recipe.Style), Is.True, "시설마다 연출이 달라야 한다: " + recipe.Style);
                foreach (ConstructionPiece piece in recipe.Pieces)
                    Assert.That(piece.Delay + piece.Duration, Is.LessThanOrEqualTo(1.0001f), id);
                foreach (ConstructionShape shape in recipe.Shapes)
                    Assert.That(shape.End, Is.LessThanOrEqualTo(1.0001f), id);
            }
            Assert.That(styles, Has.Count.EqualTo(9));
            Assert.That(FacilityConstructionRecipes.TryGet("building.elevator.basic", out _), Is.False, "엘리베이터는 제외(#308)");
            Assert.That(FacilityConstructionEffect.BuildAnimSeconds, Is.EqualTo(0.5f));
        }

        [Test]
        public void RecipePiecesAndShapes_DifferBetweenFacilities()
        {
            var signatures = new HashSet<string>();
            foreach (string id in FacilityIds)
            {
                FacilityConstructionRecipes.TryGet(id, out ConstructionRecipe recipe);
                string signature = string.Join("|", recipe.Pieces.Select(p =>
                    $"{p.Source}:{p.X0:0.##},{p.X1:0.##},{p.Y0:0.##},{p.Y1:0.##}:{p.From.x:0.##},{p.From.y:0.##}:{p.Delay:0.##}:{p.Ease}"));
                Assert.That(signatures.Add(signature), Is.True, id + " 연출이 다른 시설과 같다");
            }
        }

        [TestCaseSource(nameof(FacilityIds))]
        public void NewFacility_ShowsBuildPiecesFor0_5s_ThenRestoresRealArtwork(string buildingId)
        {
            GameObject building = Spawn(buildingId);
            var enabledBefore = building.GetComponentsInChildren<SpriteRenderer>(true)
                .ToDictionary(renderer => renderer, renderer => renderer.enabled);
            int rendererCount = enabledBefore.Count;

            FacilityConstructionEffect effect = FacilityConstructionEffect.Begin(building, buildingId);

            Assert.That(effect, Is.Not.Null, buildingId);
            Assert.That(effect.IsPlaying, Is.True);
            Assert.That(effect.PieceCount, Is.GreaterThan(0));
            Assert.That(effect.HiddenRendererCount, Is.GreaterThan(0), "완성본이 첫 프레임부터 가려져야 팝인이 없다");
            Assert.That(building.GetComponentsInChildren<SpriteRenderer>(true)
                .Where(renderer => enabledBefore.ContainsKey(renderer) && enabledBefore[renderer] && renderer.forceRenderingOff),
                Is.Not.Empty);

            effect.Tick(0.25f);
            Assert.That(effect.IsPlaying, Is.True, "0.25초에는 아직 건설 중");

            effect.Tick(0.2499f);
            Assert.That(effect.IsPlaying, Is.True);
            effect.Tick(0.01f);

            Assert.That(effect.IsPlaying, Is.False, "0.5초가 지나면 완성본으로 넘어간다");
            Assert.That(effect.EffectRoot, Is.Null);
            Assert.That(building.GetComponentsInChildren<SpriteRenderer>(true).Length, Is.EqualTo(rendererCount),
                "연출용 조각·효과가 남지 않는다");
            foreach (var pair in enabledBefore)
            {
                Assert.That(pair.Key.forceRenderingOff, Is.False, pair.Key.name);
                Assert.That(pair.Key.enabled, Is.EqualTo(pair.Value), "enabled 값은 건드리지 않는다: " + pair.Key.name);
            }
        }

        [TestCaseSource(nameof(FacilityIds))]
        public void LastFrameBeforeFinish_PiecesSitExactlyOnTheFinishedArtwork(string buildingId)
        {
            GameObject building = Spawn(buildingId);
            FacilityConstructionEffect effect = FacilityConstructionEffect.Begin(building, buildingId);
            Assert.That(effect, Is.Not.Null);
            effect.Tick(0.499f);

            foreach (SpriteRenderer piece in building.GetComponentsInChildren<SpriteRenderer>(true)
                .Where(renderer => renderer.gameObject.name == "BuildPiece"))
            {
                Assert.That(piece.enabled, Is.True, buildingId);
                // 끝에서는 오프셋·스케일이 거의 0·1이라 완성 그림 위에 겹쳐 굳는다.
                Assert.That(piece.transform.localScale.x, Is.EqualTo(1f).Within(0.03f), buildingId);
                Assert.That(piece.transform.localScale.y, Is.EqualTo(1f).Within(0.03f), buildingId);
                Assert.That(piece.color.a, Is.GreaterThan(0.9f), buildingId);
            }
        }

        [Test]
        public void Remaining_ReportsTimeLeftForTheInstance()
        {
            GameObject building = Spawn("building.charger.basic");
            building.GetComponent<BuildingInstance>().Initialize("charger-b206", "building.charger.basic");
            FacilityConstructionEffect effect = FacilityConstructionEffect.Begin(building, "building.charger.basic");

            Assert.That(FacilityConstructionEffect.RemainingSecondsFor("charger-b206"), Is.EqualTo(0.5f).Within(0.001f));
            effect.Tick(0.2f);
            Assert.That(FacilityConstructionEffect.RemainingSecondsFor("charger-b206"), Is.EqualTo(0.3f).Within(0.001f));
            Assert.That(FacilityConstructionEffect.RemainingSecondsFor("other"), Is.EqualTo(0f));
            effect.Tick(1f);
            Assert.That(FacilityConstructionEffect.RemainingSecondsFor("charger-b206"), Is.EqualTo(0f));
        }

        [Test]
        public void ReduceMotion_SkipsTheEffect_AndKeepsTheFinishedArtVisible()
        {
            AccessibilityPreferences.ReduceMotion = true;
            GameObject building = Spawn("building.storage.basic");

            Assert.That(FacilityConstructionEffect.Begin(building, "building.storage.basic"), Is.Null);
            Assert.That(building.GetComponent<FacilityConstructionEffect>(), Is.Null);
            Assert.That(building.GetComponentsInChildren<SpriteRenderer>(true).Any(renderer => renderer.forceRenderingOff), Is.False);
        }

        [Test]
        public void UnknownFacility_GetsNoEffect()
        {
            GameObject building = Spawn("building.charger.basic");
            Assert.That(FacilityConstructionEffect.Begin(building, "building.test.unknown"), Is.Null);
            Assert.That(building.GetComponent<FacilityConstructionEffect>(), Is.Null);
        }

        [Test]
        public void PowerOn_WaitsForConstruction_ThenStartsOnce_WhileFunctionIsImmediate()
        {
            var started = new List<PowerFacility>();
            void OnStarted(PowerFacility facility, bool isBatch) => started.Add(facility);
            PowerOnEffect.Started += OnStarted;
            try
            {
                GameObject building = Spawn("building.charger.basic");
                var facility = building.GetComponent<PowerFacility>();
                FacilityConstructionEffect effect = FacilityConstructionEffect.Begin(building, "building.charger.basic");

                facility.SetSupplyRangePowered(true, true);

                Assert.That(facility.IsInSupplyRange, Is.True, "전력 판정은 설치 즉시 켜진다");
                Assert.That(facility.PowerOnEffectInstance == null || !facility.PowerOnEffectInstance.IsPlaying, Is.True,
                    "켜짐 연출(청록 빛·정전기)은 건설 연출 뒤로 미룬다");
                effect.Tick(0.4f);
                Assert.That(started, Is.Empty);

                effect.Tick(0.1f);

                Assert.That(facility.PowerOnEffectInstance, Is.Not.Null);
                Assert.That(facility.PowerOnEffectInstance.IsPlaying, Is.True, "건설이 끝나는 순간 PowerOnEffect 시작");
                Assert.That(started, Is.EqualTo(new[] { facility }));
            }
            finally
            {
                PowerOnEffect.Started -= OnStarted;
            }
        }

        [Test]
        public void PowerOn_WithoutConstruction_StillStartsImmediately()
        {
            GameObject building = Spawn("building.charger.basic");
            var facility = building.GetComponent<PowerFacility>();

            facility.SetSupplyRangePowered(true, true);

            Assert.That(facility.PowerOnEffectInstance, Is.Not.Null);
            Assert.That(facility.PowerOnEffectInstance.IsPlaying, Is.True, "복원·이동·모션 감소 등 건설 연출이 없으면 기존과 같다");
        }

        [Test]
        public void PowerOn_LeavingRangeDuringConstruction_DoesNotStartTheEffect()
        {
            GameObject building = Spawn("building.charger.basic");
            var facility = building.GetComponent<PowerFacility>();
            FacilityConstructionEffect effect = FacilityConstructionEffect.Begin(building, "building.charger.basic");
            facility.SetSupplyRangePowered(true, true);
            facility.SetSupplyRangePowered(false, true);

            effect.Tick(0.6f);

            Assert.That(facility.PowerOnEffectInstance == null || !facility.PowerOnEffectInstance.IsPlaying, Is.True);
        }

        [Test]
        public void DisablingDuringConstruction_CleansUpWithoutStartingPowerOn()
        {
            GameObject building = Spawn("building.charger.basic");
            var facility = building.GetComponent<PowerFacility>();
            FacilityConstructionEffect effect = FacilityConstructionEffect.Begin(building, "building.charger.basic");
            facility.SetSupplyRangePowered(true, true);

            effect.enabled = false;
            // EditMode는 OnDisable을 자동 호출하지 않는다.
            typeof(FacilityConstructionEffect).GetMethod("OnDisable", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(effect, null);

            Assert.That(effect.IsPlaying, Is.False);
            Assert.That(building.GetComponentsInChildren<SpriteRenderer>(true).Any(renderer => renderer.forceRenderingOff), Is.False);
            Assert.That(facility.PowerOnEffectInstance == null || !facility.PowerOnEffectInstance.IsPlaying, Is.True);
        }

        [Test]
        public void RangeSpread_StartIsDelayedByTheConstructionTime_ButNotTheJudgement()
        {
            var go = new GameObject("B206_Range");
            spawned.Add(go);
            var indicator = go.AddComponent<PowerSupplyRangeIndicator>();
            indicator.Configure(PowerSupplyRangeIndicator.DefaultRadius);

            indicator.BeginSpread(0.5f);
            indicator.AdvanceSpread(0.4f);
            Assert.That(indicator.IsSpreading, Is.True);
            Assert.That(indicator.VisualRadius, Is.LessThan(PowerSupplyRangeIndicator.DefaultRadius * 0.05f), "건설 중에는 아직 안 퍼짐");
            Assert.That(indicator.Radius, Is.EqualTo(PowerSupplyRangeIndicator.DefaultRadius), "판정 반지름은 처음부터 최종값");

            indicator.AdvanceSpread(0.1f);
            indicator.AdvanceSpread(RangeSpreadMath.CoreSpreadSeconds * 0.5f);
            Assert.That(indicator.VisualRadius, Is.GreaterThan(PowerSupplyRangeIndicator.DefaultRadius * 0.5f));
            indicator.AdvanceSpread(RangeSpreadMath.CoreSpreadSeconds);
            Assert.That(indicator.IsSpreading, Is.False);
        }

        [Test]
        public void RangeSpread_NoDelayStaysTheSameAsBefore()
        {
            var go = new GameObject("B206_Range0");
            spawned.Add(go);
            var indicator = go.AddComponent<PowerSupplyRangeIndicator>();
            indicator.Configure(PowerSupplyRangeIndicator.DefaultRadius);

            indicator.BeginSpread();
            indicator.AdvanceSpread(RangeSpreadMath.CoreSpreadSeconds * 0.5f);
            Assert.That(indicator.IsSpreading, Is.True);
            Assert.That(indicator.VisualRadius, Is.GreaterThan(PowerSupplyRangeIndicator.DefaultRadius * 0.3f));
        }

        [Test]
        public void PlaceAt_StartsEffectButFunctionIsImmediate_MovePlaysAgain()
        {
            var setup = new PlacementSetup("building.charger.basic", PrefabRoot + "Power/ChargerFacility.prefab", new Vector2Int(2, 2));
            try
            {
                var result = setup.Placement.TryPlaceAt(Vector3Int.zero);

                Assert.That(result.IsSuccess, Is.True);
                var instance = setup.BuildingRoot.GetChild(0).GetComponent<BuildingInstance>();
                var effect = instance.GetComponent<FacilityConstructionEffect>();
                Assert.That(effect, Is.Not.Null);
                Assert.That(effect.IsPlaying, Is.True, "새 설치는 건설 연출");
                Assert.That(setup.Wallet.SpendCount, Is.EqualTo(1), "비용은 설치 즉시 차감");
                Assert.That(instance.GetComponent<PowerNode>(), Is.Not.Null);
                Assert.That(setup.Placement.IsGroundSupportingBuilding(new Vector3Int(0, -1, 0)), Is.True);

                setup.Placement.Select(setup.Definition);
                Assert.That(setup.Placement.TryPlaceAt(Vector3Int.zero).Failure,
                    Is.EqualTo(BuildingPlacementFailure.Occupied), "연출 중에도 같은 칸에 겹쳐 짓지 못한다");
                setup.Placement.ClearSelection();

                effect.Tick(0.5f);
                Assert.That(effect.IsPlaying, Is.False);

                Assert.That(setup.Placement.TryBeginMove(instance), Is.True);
                Assert.That(setup.Placement.TryMoveAt(new Vector3Int(4, 0, 0)).IsSuccess, Is.True);
                Assert.That(effect.IsPlaying, Is.True, "옮긴 시설도 같은 건설 연출을 다시 보여 준다");
                Assert.That(effect.Remaining, Is.EqualTo(FacilityConstructionEffect.BuildAnimSeconds).Within(0.001f));
                Assert.That(setup.Wallet.SpendCount, Is.EqualTo(1), "이동은 무료, 비용 재차감 없음");
                Assert.That(setup.Placement.IsGroundSupportingBuilding(new Vector3Int(4, -1, 0)), Is.True, "옮긴 직후 새 칸 점유");

                effect.Tick(0.5f);
                Assert.That(effect.IsPlaying, Is.False);
                Assert.That(instance.GetComponentsInChildren<SpriteRenderer>(true).Any(renderer => renderer.forceRenderingOff), Is.False);
            }
            finally { setup.Dispose(); }
        }

        [Test]
        public void Move_DuringConstruction_RestartsAtTheNewSpot_AndReduceMotionSkips()
        {
            var setup = new PlacementSetup("building.charger.basic", PrefabRoot + "Power/ChargerFacility.prefab", new Vector2Int(2, 2));
            try
            {
                Assert.That(setup.Placement.TryPlaceAt(Vector3Int.zero).IsSuccess, Is.True);
                var instance = setup.BuildingRoot.GetChild(0).GetComponent<BuildingInstance>();
                var effect = instance.GetComponent<FacilityConstructionEffect>();
                effect.Tick(0.3f);

                Assert.That(setup.Placement.TryBeginMove(instance), Is.True);
                Assert.That(setup.Placement.TryMoveAt(new Vector3Int(4, 0, 0)).IsSuccess, Is.True);
                Assert.That(effect.IsPlaying, Is.True);
                Assert.That(effect.Elapsed, Is.EqualTo(0f), "새 위치에서 처음부터");
                Assert.That(instance.GetComponentsInChildren<SpriteRenderer>(true).Count(renderer => renderer.gameObject.name == "BuildPiece"),
                    Is.EqualTo(effect.PieceCount), "앞 연출의 조각이 남지 않는다");

                effect.Tick(1f);
                AccessibilityPreferences.ReduceMotion = true;
                Assert.That(setup.Placement.TryBeginMove(instance), Is.True);
                Assert.That(setup.Placement.TryMoveAt(new Vector3Int(0, 0, 0)).IsSuccess, Is.True);
                Assert.That(effect.IsPlaying, Is.False, "모션 감소면 이동 후에도 연출 없이 즉시 완성본");
            }
            finally { setup.Dispose(); }
        }

        private GameObject Spawn(string buildingId)
        {
            string path = Facilities.First(entry => entry.id == buildingId).path;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.That(prefab, Is.Not.Null, path);
            GameObject go = Object.Instantiate(prefab);
            spawned.Add(go);
            // 사다리 프리팹에는 BuildingInstance가 없고 설치 때 TryPlaceAt이 붙인다.
            BuildingInstance instance = go.GetComponent<BuildingInstance>();
            if (instance == null) instance = go.AddComponent<BuildingInstance>();
            instance.Initialize(buildingId + "-test", buildingId,
                buildingId == "building.charger.basic" ? new Vector2Int(2, 2) : default);
            return go;
        }

        private sealed class RecordingWallet : IResourceWallet
        {
            public int SpendCount { get; private set; }
            public bool CanAfford(IReadOnlyList<ItemCostDto> costs) => true;

            public bool TrySpend(IReadOnlyList<ItemCostDto> costs)
            {
                SpendCount++;
                return true;
            }
        }

        private sealed class PlacementSetup
        {
            public GameObject Host { get; }
            public Transform BuildingRoot { get; }
            public BuildingPlacementSystem Placement { get; }
            public BuildingPlacementDefinition Definition { get; }
            public RecordingWallet Wallet { get; }
            private readonly Tile tile;

            public PlacementSetup(string buildingId, string prefabPath, Vector2Int footprint)
            {
                Host = new GameObject("B206_PlacementSetup");
                Host.SetActive(false);
                var grid = new GameObject("Grid");
                grid.transform.SetParent(Host.transform);
                grid.AddComponent<Grid>();
                var terrainObject = new GameObject("Terrain");
                terrainObject.transform.SetParent(grid.transform);
                var terrain = terrainObject.AddComponent<Tilemap>();
                terrainObject.AddComponent<TilemapRenderer>();
                BuildingRoot = new GameObject("Buildings").transform;
                BuildingRoot.SetParent(Host.transform);
                var origin = new GameObject("PlayerOrigin").transform;
                origin.SetParent(Host.transform);
                var areaObject = new GameObject("AllowedArea");
                areaObject.transform.SetParent(Host.transform);
                var area = areaObject.AddComponent<BoxCollider2D>();
                area.isTrigger = true;
                area.size = new Vector2(40f, 40f);

                tile = ScriptableObject.CreateInstance<Tile>();
                for (int x = -1; x < 8; x++) terrain.SetTile(new Vector3Int(x, -1, 0), tile);

                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                Definition = ScriptableObject.CreateInstance<BuildingPlacementDefinition>();
                Definition.EditorSet(buildingId, prefab, footprint, true);
                Wallet = new RecordingWallet();
                Placement = Host.AddComponent<BuildingPlacementSystem>();
                SetField(Placement, "terrainTilemap", terrain);
                SetField(Placement, "buildingRoot", BuildingRoot);
                SetField(Placement, "placementOrigin", origin);
                SetField(Placement, "maximumPlacementDistance", 20f);
                SetField(Placement, "allowedPlacementArea", area);
                SetField(Placement, "restoreDefinitions", new[] { Definition });
                Host.SetActive(true);
                Placement.SetResourceWallet(Wallet);
                Physics2D.SyncTransforms();
                Placement.Select(Definition);
            }

            public void Dispose()
            {
                Placement.ClearSelection();
                BuildingPlacementActivity.ResetForTests();
                Object.DestroyImmediate(Host);
                Object.DestroyImmediate(Definition);
                Object.DestroyImmediate(tile);
            }
        }

        private static void SetField(object target, string name, object value)
        {
            FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, "Missing field: " + name);
            field.SetValue(target, value);
        }
    }
}

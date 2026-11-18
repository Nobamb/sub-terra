using System.Reflection;
using NUnit.Framework;
using SubTerra.Gameplay.Mining;
using SubTerra.Gameplay.Player;
using SubTerra.Shared;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Tilemaps;

namespace SubTerra.App.Tests.EditMode.Readiness
{
    /// <summary>B-202: 채굴 과녁이 실제 채굴 칸과 같은 칸을 고르고, 규칙대로 숨고, 우선권·모션 감소를 따르는지 확인한다.</summary>
    public sealed class MiningTargetReticleTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private const float Reach = PlayerMiningController.DefaultReach;
        private static readonly Vector2 Origin = new Vector2(.5f, .5f);

        private sealed class DrillStub : IUpgradeEffectProvider
        {
            public int Level;
            public int GetDrillLevel() => Level;
            public float GetDrillSpeedMultiplier() => 1f;
            public float GetEnergyEfficiencyMultiplier() => 1f;
            public int GetMaximumEnergy(int baseMaximum) => baseMaximum;
            public float GetMaximumCargoWeight(float baseMaximum) => baseMaximum;
            public float GetDroneScanRadius(float baseRadius) => baseRadius;
            public float GetDroneRescuePreservation(float basePreservation) => basePreservation;
            public float GetGasResistance() => 0f;
            public int GetMiningYieldBonus(string mineralId) => 0;
            public int GetGoldGainBonusPercent() => 0;
            public float GetMaximumOxygen(float baseMaximum) => baseMaximum;
            public float GetOxygenDrainReduction() => 0f;
        }

        private GameObject world;
        private GameObject player;
        private Tilemap map;
        private MiningSystem mining;
        private MiningTileResolver resolver;
        private Tile mineable;
        private Tile hard;
        private Tile drillTwo;
        private PlayerMovement movement;
        private PlayerAnimationController animation;
        private PlayerMiningController controller;
        private bool wasSettingsOpen;
        private bool wasReduceMotion;

        [SetUp]
        public void SetUp()
        {
            wasSettingsOpen = ControlPreferences.IsSettingsOpen;
            wasReduceMotion = AccessibilityPreferences.ReduceMotion;
            ControlPreferences.IsSettingsOpen = false;
            AccessibilityPreferences.ReduceMotion = false;
            BuildingPlacementActivity.ResetForTests();

            world = new GameObject("B202 test world", typeof(Grid));
            var tiles = new GameObject("Tiles", typeof(Tilemap));
            tiles.transform.SetParent(world.transform);
            map = tiles.GetComponent<Tilemap>();
            resolver = world.AddComponent<MiningTileResolver>();
            mining = world.AddComponent<MiningSystem>();
            Set(mining, "foregroundTilemap", map);
            Set(mining, "tileResolver", resolver);
            mineable = ScriptableObject.CreateInstance<Tile>();
            hard = ScriptableObject.CreateInstance<Tile>();
            drillTwo = ScriptableObject.CreateInstance<Tile>();
            resolver.RegisterRuntime(mineable, new MiningTileDto("tile.b202", "", 0, true, 1f, 5f, 0f, false));
            resolver.RegisterRuntime(hard, new MiningTileDto("tile.b202.hard", "", 0, false, 1f, 5f, 0f, false));
            resolver.RegisterRuntime(drillTwo, new MiningTileDto("tile.b202.drill", "", 0, true, 1f, 5f, 0f, false, 2));
            mining.SetRuntimeServices(null, new DrillStub { Level = 0 });

            player = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Project/Prefabs/Gameplay/Player/Player.prefab"));
            player.transform.position = new Vector3(Origin.x, Origin.y, 0f);
            movement = player.GetComponent<PlayerMovement>();
            animation = player.GetComponentInChildren<PlayerAnimationController>();
            controller = player.GetComponent<PlayerMiningController>();
            if (controller == null) controller = player.AddComponent<PlayerMiningController>();
            Invoke(movement, "Awake");
            player.GetComponent<Rigidbody2D>().position = Origin;
            Invoke(animation, "Awake");
            Invoke(controller, "Awake");
            Set(controller, "miningSystem", mining);
            Set(movement, "<IsGrounded>k__BackingField", true);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(player);
            Object.DestroyImmediate(world);
            Object.DestroyImmediate(mineable);
            Object.DestroyImmediate(hard);
            Object.DestroyImmediate(drillTwo);
            BuildingPlacementActivity.ResetForTests();
            ControlPreferences.IsSettingsOpen = wasSettingsOpen;
            AccessibilityPreferences.ReduceMotion = wasReduceMotion;
        }

        private static Vector3Int Cell(int x, int y) => new Vector3Int(x, y, 0);

        private int Side => movement.FacingDirection >= 0f ? 1 : -1;

        // ───── 키보드: 엔터 ─────

        [Test]
        public void Enter_LookupMatchesStartedCell_ForSideThenUpThenDown()
        {
            Vector3Int side = Cell(Side, 0);
            Vector3Int up = side + Vector3Int.up;
            Vector3Int down = side + Vector3Int.down;
            map.SetTile(up, mineable);
            map.SetTile(down, mineable);
            map.SetTile(side, mineable);

            AssertEnterMatchesStart(side);

            map.SetTile(side, null);
            AssertEnterMatchesStart(up);

            map.SetTile(up, null);
            AssertEnterMatchesStart(down);

            map.SetTile(down, null);
            Assert.That(mining.TryGetKeyboardTarget(Origin, movement.FacingDirection, Vector2.zero, false, Reach, out _), Is.False);
            Assert.That(mining.TryStartMiningFrom(Origin, movement.FacingDirection, Reach), Is.False);
        }

        private void AssertEnterMatchesStart(Vector3Int expected)
        {
            Assert.That(mining.TryGetKeyboardTarget(Origin, movement.FacingDirection, Vector2.zero, false, Reach, out var cell), Is.True);
            Assert.That(cell, Is.EqualTo(expected));
            Assert.That(mining.TryStartMiningFrom(Origin, movement.FacingDirection, Reach), Is.True);
            Assert.That(mining.ActiveCell, Is.EqualTo(cell));
            mining.CancelMining();
        }

        // ───── 키보드: 채굴 방향키 ─────

        [TestCase(0, 1)]
        [TestCase(0, -1)]
        [TestCase(1, 0)]
        [TestCase(-1, 0)]
        public void DirectionKey_LookupMatchesStartedAdjacentCell(int x, int y)
        {
            map.SetTile(Cell(x, y), mineable);
            Assert.That(mining.TryGetKeyboardTarget(Origin, movement.FacingDirection, new Vector2(x, y), false, Reach, out var cell), Is.True);
            Assert.That(cell, Is.EqualTo(Cell(x, y)));
            Assert.That(mining.TryStartMiningInDirection(Origin, new Vector2(x, y), Reach), Is.True);
            Assert.That(mining.ActiveCell, Is.EqualTo(cell));
        }

        [Test]
        public void DirectionKey_DoesNotFallBackToOtherCells()
        {
            map.SetTile(Cell(1, 1), mineable);
            map.SetTile(Cell(1, -1), mineable);
            // 오른쪽 방향키인데 옆칸이 비어 있어도 위·아래로 대체하지 않는다. 칸은 돌려주되 지형 검사에서 걸린다.
            Assert.That(mining.TryGetKeyboardTarget(Origin, 1f, Vector2.right, false, Reach, out var cell), Is.True);
            Assert.That(cell, Is.EqualTo(Cell(1, 0)));
            Assert.That(mining.IsTargetTerrainMineable(cell), Is.False);
        }

        // ───── 사다리 ─────

        [TestCase(0, 1)]
        [TestCase(0, -1)]
        public void Ladder_VerticalDirectionHasNoTarget(int x, int y)
        {
            map.SetTile(Cell(x, y), mineable);
            Assert.That(mining.TryGetKeyboardTarget(Origin, 1f, new Vector2(x, y), true, Reach, out _), Is.False);
        }

        [Test]
        public void Ladder_EnterUsesFacingSideOnlyWithoutVerticalFallback()
        {
            map.SetTile(Cell(1, 1), mineable);
            map.SetTile(Cell(1, -1), mineable);
            Assert.That(mining.TryGetKeyboardTarget(Origin, 1f, Vector2.zero, true, Reach, out var cell), Is.True);
            Assert.That(cell, Is.EqualTo(Cell(1, 0)));
            Assert.That(mining.IsTargetTerrainMineable(cell), Is.False, "옆칸이 비면 과녁은 없다");

            map.SetTile(Cell(1, 0), mineable);
            Assert.That(mining.TryGetKeyboardTarget(Origin, 1f, Vector2.zero, true, Reach, out cell), Is.True);
            Assert.That(cell, Is.EqualTo(Cell(1, 0)));
            Assert.That(mining.TryGetKeyboardTarget(Origin, -1f, Vector2.left, true, Reach, out cell), Is.True);
            Assert.That(cell, Is.EqualTo(Cell(-1, 0)));
        }

        [Test]
        public void Ladder_PointerAllowsOnlySameRowOtherColumn()
        {
            foreach (var offset in new[] { Cell(0, 1), Cell(0, -1), Cell(1, 1), Cell(-1, 1), Cell(1, 0), Cell(-1, 0), Cell(0, 0) })
                map.SetTile(offset, mineable);
            foreach (var offset in new[] { Cell(0, 1), Cell(0, -1), Cell(1, 1), Cell(-1, 1), Cell(0, 0) })
                Assert.That(mining.TryGetPointerTarget(map.GetCellCenterWorld(offset), Origin, true, Reach, out _), Is.False, offset.ToString());
            Assert.That(mining.TryGetPointerTarget(map.GetCellCenterWorld(Cell(1, 0)), Origin, true, Reach, out var cell), Is.True);
            Assert.That(cell, Is.EqualTo(Cell(1, 0)));
            // 같은 점이 바닥에서는 위·아래 칸도 허용된다.
            Assert.That(mining.TryGetPointerTarget(map.GetCellCenterWorld(Cell(0, 1)), Origin, false, Reach, out _), Is.True);
        }

        // ───── 마우스 ─────

        [Test]
        public void Pointer_LookupMatchesClickCell_AndRespectsReach()
        {
            map.SetTile(Cell(1, 1), mineable);
            map.SetTile(Cell(3, 0), mineable);
            Vector2 inside = map.GetCellCenterWorld(Cell(1, 1)) + new Vector3(.2f, -.2f, 0f);
            Assert.That(mining.TryGetPointerTarget(inside, Origin, false, Reach, out var cell), Is.True);
            Assert.That(cell, Is.EqualTo(Cell(1, 1)));
            Assert.That(mining.TryStartMiningAtWorldPoint(inside, Origin, Reach), Is.True);
            Assert.That(mining.ActiveCell, Is.EqualTo(cell));
            mining.CancelMining();

            Vector2 far = map.GetCellCenterWorld(Cell(3, 0));
            Assert.That(mining.TryGetPointerTarget(far, Origin, false, Reach, out _), Is.False);
            Assert.That(mining.TryStartMiningAtWorldPoint(far, Origin, Reach), Is.False);
        }

        // ───── 지형·드릴·전력 ─────

        [Test]
        public void Terrain_HidesForNoTileUnmineableAndLowDrill_ButStartFailureStillReported()
        {
            map.SetTile(Cell(1, 0), mineable);
            map.SetTile(Cell(-1, 0), hard);
            map.SetTile(Cell(0, 1), drillTwo);
            Assert.That(mining.IsTargetTerrainMineable(Cell(1, 0)), Is.True);
            Assert.That(mining.IsTargetTerrainMineable(Cell(0, -1)), Is.False, "타일 없음");
            Assert.That(mining.IsTargetTerrainMineable(Cell(-1, 0)), Is.False, "캘 수 없음");
            Assert.That(mining.IsTargetTerrainMineable(Cell(0, 1)), Is.False, "드릴 레벨 부족");

            Assert.That(mining.TryStartMining(Cell(0, 1)), Is.False);
            Assert.That(mining.LastFailure, Is.EqualTo(MiningFailureReason.DrillLevelTooLow), "실패 HUD용 사유는 그대로");
        }

        [Test]
        public void Terrain_ProtectedCellHides()
        {
            map.SetTile(Cell(1, 0), mineable);
            mining.SetCellProtectionPredicate(cell => cell == Cell(1, 0));
            Assert.That(mining.IsTargetTerrainMineable(Cell(1, 0)), Is.False);
        }

        [Test]
        public void NoPower_DoesNotHideTarget()
        {
            map.SetTile(Cell(1, 0), mineable);
            mining.SetMiningPowerAvailable(false);
            Assert.That(mining.IsTargetTerrainMineable(Cell(1, 0)), Is.True);
        }

        // ───── 컨트롤러 조회 ─────

        [Test]
        public void Controller_KeyboardAndPointerLookups_UseSameRulesAsStart()
        {
            map.SetTile(Cell(Side, 0), mineable);
            Assert.That(controller.TryResolveKeyboardReticleCell(out var cell), Is.True);
            Assert.That(cell, Is.EqualTo(Cell(Side, 0)));

            map.SetTile(Cell(0, 1), mineable);
            Vector2 world = map.GetCellCenterWorld(Cell(0, 1));
            Assert.That(controller.TryResolvePointerReticleCell(world, out cell), Is.True);
            Assert.That(cell, Is.EqualTo(Cell(0, 1)));

            map.SetTile(Cell(0, 1), drillTwo);
            Assert.That(controller.TryResolvePointerReticleCell(world, out _), Is.False);
        }

        [Test]
        public void Controller_PointerOutOfReachFallsBackToKeyboard_AndRecoversOnValidHover()
        {
            var keyboardCell = Cell(Side, 0);
            var pointerCell = Cell(0, 1);
            var outOfReachCell = Cell(3, 0);
            map.SetTile(keyboardCell, mineable);
            map.SetTile(pointerCell, mineable);
            map.SetTile(outOfReachCell, mineable);

            var camera = Camera.main;
            if (camera == null)
            {
                camera = new GameObject("Mining reticle test camera", typeof(Camera)).GetComponent<Camera>();
                camera.transform.SetParent(world.transform);
                camera.transform.position = new Vector3(0f, 0f, -10f);
                camera.orthographic = true;
                camera.tag = "MainCamera";
            }

            var previousMouse = Mouse.current;
            var mouse = InputSystem.AddDevice<Mouse>();
            try
            {
                mouse.MakeCurrent();
                controller.TargetPriority.NotePointerInput();
                InputState.Change(mouse.position, (Vector2)camera.WorldToScreenPoint(map.GetCellCenterWorld(pointerCell)));
                Assert.That(controller.TryResolveReticleCell(out var cell), Is.True);
                Assert.That(cell, Is.EqualTo(pointerCell));

                InputState.Change(mouse.position, (Vector2)camera.WorldToScreenPoint(map.GetCellCenterWorld(outOfReachCell)));
                Assert.That(controller.TryResolveReticleCell(out cell), Is.True);
                Assert.That(cell, Is.EqualTo(keyboardCell));
                Assert.That(controller.TargetPriority.Source, Is.EqualTo(MiningTargetSource.Pointer));

                InputState.Change(mouse.position, (Vector2)camera.WorldToScreenPoint(map.GetCellCenterWorld(pointerCell)));
                Assert.That(controller.TryResolveReticleCell(out cell), Is.True);
                Assert.That(cell, Is.EqualTo(pointerCell));
            }
            finally
            {
                InputSystem.RemoveDevice(mouse);
                previousMouse?.MakeCurrent();
            }
        }

        [Test]
        public void Controller_PriorityPicksPointerOrKeyboardCell_AndMiningPinsActiveCell()
        {
            map.SetTile(Cell(Side, 0), mineable);
            map.SetTile(Cell(0, -1), mineable);
            Assert.That(controller.TargetPriority.Source, Is.EqualTo(MiningTargetSource.Keyboard));
            Assert.That(controller.TryResolveReticleCell(out var cell), Is.True);
            Assert.That(cell, Is.EqualTo(Cell(Side, 0)));

            // 채굴 중에는 우선권과 무관하게 캐는 칸에 붙는다.
            Assert.That(mining.TryStartMining(Cell(0, -1)), Is.True);
            controller.TargetPriority.NotePointerInput();
            Assert.That(controller.TryResolveReticleCell(out cell), Is.True);
            Assert.That(cell, Is.EqualTo(Cell(0, -1)));

            // 채굴이 끝나면 현재 입력 우선순위로 다음 목표를 고른다.
            mining.CancelMining();
            controller.TargetPriority.NoteKeyboardInput();
            Assert.That(controller.TryResolveReticleCell(out cell), Is.True);
            Assert.That(cell, Is.EqualTo(Cell(Side, 0)));
        }

        // ───── 게이트와 표시 ─────

        [Test]
        public void Reticle_ShowsOnKeyboardTarget_AndHidesInSettingsAndBuildMode()
        {
            map.SetTile(Cell(Side, 0), mineable);
            Invoke(controller, "UpdateTargetReticle", .016f);
            var reticle = controller.Reticle;
            Assert.That(reticle, Is.Not.Null);
            Assert.That(reticle.IsVisible, Is.True);
            Assert.That((Vector3)reticle.TargetCenter, Is.EqualTo(map.GetCellCenterWorld(Cell(Side, 0))));

            ControlPreferences.IsSettingsOpen = true;
            Invoke(controller, "UpdateTargetReticle", .016f);
            Assert.That(reticle.IsVisible, Is.False);
            ControlPreferences.IsSettingsOpen = false;

            Invoke(controller, "UpdateTargetReticle", .016f);
            Assert.That(reticle.IsVisible, Is.True);
            BuildingPlacementActivity.Begin();
            Invoke(controller, "UpdateTargetReticle", .016f);
            Assert.That(reticle.IsVisible, Is.False);
            BuildingPlacementActivity.End();

            map.SetTile(Cell(Side, 0), null);
            Invoke(controller, "UpdateTargetReticle", .016f);
            Assert.That(reticle.IsVisible, Is.False, "목표 없음");
        }

        [Test]
        public void Reticle_DisablingControllerHidesIt()
        {
            map.SetTile(Cell(Side, 0), mineable);
            Invoke(controller, "UpdateTargetReticle", .016f);
            var reticle = controller.Reticle;
            Assert.That(reticle.IsVisible, Is.True);
            Invoke(controller, "OnDisable"); // EditMode에서는 자동 호출되지 않는다
            Assert.That(reticle.IsVisible, Is.False);
        }

        // ───── 순수 로직 ─────

        [Test]
        public void Priority_LastInputWins_AndStartsWithKeyboard()
        {
            var priority = new MiningTargetPriority();
            Assert.That(priority.Source, Is.EqualTo(MiningTargetSource.Keyboard));
            priority.NotePointerInput();
            Assert.That(priority.Source, Is.EqualTo(MiningTargetSource.Pointer));
            priority.NoteKeyboardInput();
            Assert.That(priority.Source, Is.EqualTo(MiningTargetSource.Keyboard));
            priority.NotePointerInput();
            priority.Reset();
            Assert.That(priority.Source, Is.EqualTo(MiningTargetSource.Keyboard));
        }

        [Test]
        public void Motion_SlidesOnTargetChange_AndPinchesSlowly()
        {
            var motion = new MiningReticleMotion();
            motion.SetTarget(Vector2.zero, false);
            motion.Tick(0f, false);
            Assert.That(motion.Visible, Is.True);
            Assert.That(motion.Inset, Is.EqualTo(0f).Within(1e-4f));

            motion.SetTarget(new Vector2(2f, 0f), false);
            motion.Tick(MiningReticleMotion.SlideSeconds * .5f, false);
            Assert.That(motion.Position.x, Is.GreaterThan(0f).And.LessThan(2f), "미끄러지는 중");
            motion.Tick(MiningReticleMotion.SlideSeconds, false);
            Assert.That(motion.Position.x, Is.EqualTo(2f).Within(1e-4f));

            motion.Tick(MiningReticleMotion.PinchPeriodSeconds * .5f - MiningReticleMotion.SlideSeconds * 1.5f, false);
            Assert.That(motion.Inset, Is.EqualTo(MiningReticleMotion.PinchDepth).Within(1e-3f), "반 주기에 가장 조여든다");
        }

        [Test]
        public void Motion_ReduceMotionKeepsStaticCornersWithoutSlide()
        {
            AccessibilityPreferences.ReduceMotion = true;
            var motion = new MiningReticleMotion();
            motion.SetTarget(Vector2.zero, true);
            motion.SetTarget(new Vector2(3f, 1f), true);
            motion.Tick(.3f, true);
            Assert.That(motion.Position, Is.EqualTo(new Vector2(3f, 1f)));
            Assert.That(motion.Inset, Is.EqualTo(0f));
            motion.Tick(MiningReticleMotion.PinchPeriodSeconds * .5f, true);
            Assert.That(motion.Inset, Is.EqualTo(0f));
        }

        [Test]
        public void Reticle_ReduceMotionPlacesStaticCornersImmediately()
        {
            var reticle = player.AddComponent<MiningTargetReticle>();
            AccessibilityPreferences.ReduceMotion = true;
            reticle.Present(true, new Vector3(1.5f, .5f, 0f), Vector2.one, .5f);
            reticle.Present(true, new Vector3(4.5f, 2.5f, 0f), Vector2.one, .016f);
            Assert.That(reticle.CurrentCenter, Is.EqualTo(new Vector2(4.5f, 2.5f)));
            Assert.That(reticle.Inset, Is.EqualTo(0f));
            Assert.That(reticle.Corners[0].transform.position.x, Is.EqualTo(4f).Within(1e-4f));
            Assert.That(reticle.Corners[3].transform.position.y, Is.EqualTo(3f).Within(1e-4f));
            foreach (var corner in reticle.Corners)
                Assert.That(corner.sortingOrder, Is.LessThan(90));
        }

        private static void Set(object target, string name, object value) => target.GetType().GetField(name, Private).SetValue(target, value);
        private static void Invoke(object target, string name, params object[] args) => target.GetType().GetMethod(name, Private).Invoke(target, args);
    }
}

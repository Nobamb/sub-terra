using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SubTerra.Gameplay.Power;
using SubTerra.Shared;
using UnityEditor;
using UnityEngine;

namespace SubTerra.Gameplay.Building.Tests
{
    /// <summary>B-214: 시설 30초 주기 연출과 포탈 내부 홀 회전. Tick을 직접 불러 시간을 진행한다.</summary>
    public sealed class FacilityIdleAnimationTests
    {
        private const string PrefabRoot = "Assets/_Project/Prefabs/Gameplay/";

        private static readonly (string id, string path)[] Facilities =
        {
            ("building.light.basic", PrefabRoot + "Power/LightFacility.prefab"),
            ("building.charger.basic", PrefabRoot + "Power/ChargerFacility.prefab"),
            ("building.clinic.basic", PrefabRoot + "Power/ClinicFacility.prefab"),
            ("building.storage.basic", PrefabRoot + "Power/StorageFacility.prefab"),
            ("building.settlement.basic", PrefabRoot + "Power/SettlementFacility.prefab"),
            ("building.outpost_core.basic", PrefabRoot + "Power/OutpostCore.prefab"),
            ("building.escape_portal.emergency", PrefabRoot + "Buildings/EmergencyEscapePortal.prefab")
        };

        public static IEnumerable<string> IdleIds => Facilities.Take(6).Select(entry => entry.id);

        private readonly List<GameObject> spawned = new();
        private bool previousReduceMotion;
        private int sequence;

        [SetUp]
        public void SetUp()
        {
            previousReduceMotion = AccessibilityPreferences.ReduceMotion;
            AccessibilityPreferences.ReduceMotion = false;
            FacilityIdleAnimation.SetPeriodOverride(0f);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in spawned)
                if (go != null) Object.DestroyImmediate(go);
            spawned.Clear();
            FacilityIdleAnimation.SetPeriodOverride(0f);
            while (PowerOnEffect.IsSuppressed) PowerOnEffect.EndSuppression();
            PowerOnEffect.FlushPendingStarts();
            AccessibilityPreferences.ReduceMotion = previousReduceMotion;
        }

        // ── 표·곡선 ──

        [Test]
        public void Table_MapsEveryIdleFacility_AndGivesTheDocumentedLengths()
        {
            Assert.That(FacilityIdleAnchors.KindFor("building.light.basic"), Is.EqualTo(FacilityIdleKind.Light));
            Assert.That(FacilityIdleAnchors.KindFor("building.charger.basic"), Is.EqualTo(FacilityIdleKind.Charger));
            Assert.That(FacilityIdleAnchors.KindFor("building.clinic.basic"), Is.EqualTo(FacilityIdleKind.Clinic));
            Assert.That(FacilityIdleAnchors.KindFor("building.storage.basic"), Is.EqualTo(FacilityIdleKind.Storage));
            Assert.That(FacilityIdleAnchors.KindFor("building.settlement.basic"), Is.EqualTo(FacilityIdleKind.Settlement));
            Assert.That(FacilityIdleAnchors.KindFor("building.outpost_core.basic"), Is.EqualTo(FacilityIdleKind.Outpost));
            Assert.That(FacilityIdleAnchors.KindFor("building.escape_portal.emergency"), Is.EqualTo(FacilityIdleKind.Portal));
            Assert.That(FacilityIdleAnchors.KindFor("building.elevator.basic"), Is.EqualTo(FacilityIdleKind.None), "엘리베이터는 대상이 아니다");
            Assert.That(FacilityIdleAnchors.KindFor("building.ladder.basic"), Is.EqualTo(FacilityIdleKind.None));

            Assert.That(FacilityIdleAnchors.DurationOf(FacilityIdleKind.Light), Is.EqualTo(1.0f));
            Assert.That(FacilityIdleAnchors.DurationOf(FacilityIdleKind.Charger), Is.EqualTo(2.6f));
            Assert.That(FacilityIdleAnchors.DurationOf(FacilityIdleKind.Clinic), Is.EqualTo(2.8f));
            Assert.That(FacilityIdleAnchors.DurationOf(FacilityIdleKind.Storage), Is.EqualTo(56f / 12f).Within(0.0001f), "보관함은 영상 56프레임 ÷ 12fps");
            Assert.That(FacilityIdleAnchors.DurationOf(FacilityIdleKind.Settlement), Is.EqualTo(1.6f));
            Assert.That(FacilityIdleAnchors.DurationOf(FacilityIdleKind.Outpost), Is.EqualTo(2.0f));
            Assert.That(FacilityIdleAnimation.DefaultPeriodSeconds, Is.EqualTo(30f));
            Assert.That(FacilityIdleAnimation.DefaultJitterSeconds, Is.EqualTo(3f));
        }

        [Test]
        public void LightCurve_DipsToThePoint35AtLeastTwice_AndEndsBright()
        {
            float min = 1f;
            int dips = 0;
            bool inDip = false;
            for (int i = 0; i <= 1000; i++)
            {
                float value = FacilityIdleAnchors.LightBrightness(i / 1000f);
                min = Mathf.Min(min, value);
                bool low = value < 0.5f;
                if (low && !inDip) dips++;
                inDip = low;
            }
            Assert.That(min, Is.EqualTo(0.35f).Within(0.001f));
            Assert.That(dips, Is.InRange(2, 3));
            Assert.That(FacilityIdleAnchors.LightBrightness(0f), Is.EqualTo(1f));
            Assert.That(FacilityIdleAnchors.LightBrightness(1f), Is.EqualTo(1f));
        }

        [Test]
        public void IconFill_StartsEmpty_FillsBottomToTop_AndEndsAsTheOriginalIcon()
        {
            float end = FacilityIdleAnchors.ChargerFillEnd;
            Assert.That(FacilityIdleAnchors.IconFillLevel(0f, end), Is.EqualTo(0f));
            Assert.That(FacilityIdleAnchors.IconFillLevel(FacilityIdleAnchors.IconFillStart, end), Is.EqualTo(0f));
            Assert.That(FacilityIdleAnchors.IconFillLevel(end, end), Is.EqualTo(1f));
            float previous = 0f;
            for (int i = 0; i <= 100; i++)
            {
                float level = FacilityIdleAnchors.IconFillLevel(end * i / 100f, end);
                Assert.That(level, Is.GreaterThanOrEqualTo(previous), "차오름은 줄어들지 않는다");
                previous = level;
            }

            Assert.That(FacilityIdleAnchors.IconFillFrame(0f), Is.EqualTo(0), "처음은 속이 빈 껍데기");
            Assert.That(FacilityIdleAnchors.IconFillFrame(0.001f), Is.EqualTo(1));
            Assert.That(FacilityIdleAnchors.IconFillFrame(0.999f), Is.EqualTo(FacilityIdleAnchors.IconFillFrames - 2), "가득 차기 전에는 원래 아이콘이 아니다");
            Assert.That(FacilityIdleAnchors.IconFillFrame(1f), Is.EqualTo(FacilityIdleAnchors.IconFillFrames - 1), "가득 차면 원래 아이콘");
        }

        [Test]
        public void IconAndStorageSheets_AreLoadedFromResources()
        {
            for (int i = 0; i < FacilityIdleAnchors.IconFillFrames; i++)
            {
                Assert.That(FacilityIdleSprites.ChargerIcon(i), Is.Not.Null, "ChargerIdleIconSheet " + i);
                Assert.That(FacilityIdleSprites.ClinicIcon(i), Is.Not.Null, "ClinicIdleIconSheet " + i);
            }
            Assert.That(FacilityIdleSprites.ChargerIcon(0), Is.Not.SameAs(FacilityIdleSprites.ChargerIcon(1)));
            Assert.That(FacilityIdleSprites.ChargerIcon(0).rect.width, Is.EqualTo(FacilityIdleAnchors.IconFillCell));
            Assert.That(FacilityIdleSprites.HasStorageOpenSheet, Is.True, "Resources/Facilities/StorageOpenSheet.png");
            Assert.That(FacilityIdleAnchors.StorageOpenFrame(0f), Is.EqualTo(0));
            Assert.That(FacilityIdleAnchors.StorageOpenFrame(1f), Is.EqualTo(12));
            Assert.That(FacilityIdleAnchors.StorageOpenFrame(99f), Is.EqualTo(FacilityIdleAnchors.StorageOpenFrames - 1), "끝 프레임에서 멈춘다");
        }

        // ── 주기·엇갈림·건너뜀 ──

        [Test]
        public void NewFacility_StartsBetween3And30Seconds_AndOnlyWhenPowered()
        {
            GameObject light = Spawn("building.light.basic");
            FacilityIdleAnimation animation = light.GetComponent<FacilityIdleAnimation>();
            Assert.That(animation, Is.Not.Null, "BuildingInstance.Initialize가 런타임에 붙인다");
            Assert.That(animation.SecondsUntilNext, Is.InRange(3f, 40f));
            Assert.That(animation.IsPlaying, Is.False);
        }

        [TestCaseSource(nameof(IdleIds))]
        public void Playing_ShowsTheEffect_ThenLeavesNothingBehind(string buildingId)
        {
            GameObject building = Spawn(buildingId);
            Power(building, true);
            var animation = building.GetComponent<FacilityIdleAnimation>();
            int rendererCount = building.GetComponentsInChildren<SpriteRenderer>(true).Length;
            int objectCount = building.GetComponentsInChildren<Transform>(true).Length;
            var enabledBefore = building.GetComponentsInChildren<SpriteRenderer>(true)
                .ToDictionary(renderer => renderer, renderer => renderer.enabled);

            animation.ScheduleAfter(0.1f);
            animation.Tick(0.05f);
            Assert.That(animation.IsPlaying, Is.False);
            animation.Tick(0.1f);
            Assert.That(animation.IsPlaying, Is.True, buildingId);
            Assert.That(animation.EffectRoot, Is.Not.Null);
            Assert.That(animation.EffectRoot.GetComponentsInChildren<SpriteRenderer>(true), Is.Not.Empty);

            float duration = FacilityIdleAnchors.DurationOf(animation.Kind);
            animation.Tick(duration * 0.5f);
            Assert.That(animation.IsPlaying, Is.True, "연출 길이의 절반에서는 아직 재생 중");
            animation.Tick(duration);

            Assert.That(animation.IsPlaying, Is.False);
            Assert.That(animation.EffectRoot, Is.Null);
            Assert.That(building.GetComponentsInChildren<SpriteRenderer>(true).Length, Is.EqualTo(rendererCount), "남은 조각이 없다");
            Assert.That(building.GetComponentsInChildren<Transform>(true).Length, Is.EqualTo(objectCount), "남은 GameObject가 없다");
            foreach (var pair in enabledBefore)
            {
                Assert.That(pair.Key.forceRenderingOff, Is.False, pair.Key.name);
                Assert.That(pair.Key.enabled, Is.EqualTo(pair.Value), pair.Key.name);
            }
            Assert.That(animation.SecondsUntilNext, Is.InRange(27f, 40f), "다음 주기는 30±3초");
            var dim = building.GetComponent<FacilityVisualDim>();
            if (dim != null) Assert.That(dim.Multiplier, Is.EqualTo(1f));
        }

        [Test]
        public void Storage_PlaysTheVideoFrames_OverTheArtwork_ThenRestoresIt()
        {
            GameObject building = Spawn("building.storage.basic");
            Power(building, true);
            var animation = building.GetComponent<FacilityIdleAnimation>();
            SpriteRenderer artwork = building.transform.Find("VisualRoot/Artwork").GetComponent<SpriteRenderer>();

            animation.ScheduleAfter(0f);
            animation.Tick(0.01f);
            Assert.That(animation.IsPlaying, Is.True);
            Assert.That(artwork.forceRenderingOff, Is.True, "원래 Artwork는 가려 두 겹으로 보이지 않게 한다");
            SpriteRenderer video = building.GetComponentsInChildren<SpriteRenderer>(true).Single(r => r.name == "StorageOpenVideo");
            Assert.That(video.sprite.name, Is.EqualTo("StorageOpen_0"));
            Assert.That(video.sortingOrder, Is.EqualTo(artwork.sortingOrder));

            // 첫 프레임은 원래 그림의 보이는 영역을 그대로 덮는다(월드 좌표 기준 0.03칸 이내).
            Bounds visible = FacilityGroundedVisual.GetVisibleBounds(artwork.sprite);
            Vector3 artMin = artwork.transform.TransformPoint(visible.min), artMax = artwork.transform.TransformPoint(visible.max);
            Assert.That(video.bounds.min.x, Is.EqualTo(artMin.x).Within(0.03f));
            Assert.That(video.bounds.max.x, Is.EqualTo(artMax.x).Within(0.03f));
            Assert.That(video.bounds.min.y, Is.EqualTo(artMin.y).Within(0.03f));
            Assert.That(video.bounds.max.y, Is.GreaterThan(artMax.y), "뚜껑이 들릴 위쪽 여백이 있다");

            animation.Tick(2f);
            Assert.That(video.sprite.name, Is.EqualTo("StorageOpen_" + FacilityIdleAnchors.StorageOpenFrame(2.01f)), "12fps로 프레임이 넘어간다");
            animation.Tick(FacilityIdleAnchors.StorageSeconds);

            Assert.That(animation.IsPlaying, Is.False);
            Assert.That(artwork.forceRenderingOff, Is.False);
            Assert.That(building.GetComponentsInChildren<SpriteRenderer>(true).Any(r => r.name == "StorageOpenVideo"), Is.False);
        }

        [TestCase("building.charger.basic", 2)]
        [TestCase("building.charger.basic", 1)]
        [TestCase("building.clinic.basic", 2)]
        public void ChargerAndClinic_IconAppearsEmpty_ThenFillsToTheOriginal(string buildingId, int footprintSize)
        {
            GameObject building = Spawn(buildingId, new Vector2Int(footprintSize, footprintSize));
            Power(building, true);
            var animation = building.GetComponent<FacilityIdleAnimation>();
            SpriteRenderer artwork = building.transform.Find("VisualRoot/Artwork").GetComponent<SpriteRenderer>();
            float fillEnd = buildingId == "building.charger.basic" ? FacilityIdleAnchors.ChargerFillEnd : FacilityIdleAnchors.ClinicFillEnd;

            animation.ScheduleAfter(0f);
            animation.Tick(0.01f);
            animation.Tick(0.2f);
            SpriteRenderer icon = building.GetComponentsInChildren<SpriteRenderer>(true).Single(r => r.name == "IconFill");
            Assert.That(icon.sprite.name, Does.EndWith("_0"), "처음은 속이 빈 아이콘");
            Assert.That(icon.enabled, Is.True);
            if (animation.Kind == FacilityIdleKind.Clinic)
            {
                Assert.That(icon.bounds.center.y, Is.GreaterThan(artwork.bounds.max.y - 0.1f), "보건소 하트는 시설 위에 나타난다");
            }
            Assert.That(icon.sortingOrder, Is.GreaterThan(artwork.sortingOrder));

            animation.Tick(fillEnd * 0.5f);
            int middle = int.Parse(icon.sprite.name.Substring(icon.sprite.name.LastIndexOf('_') + 1));
            Assert.That(middle, Is.InRange(1, FacilityIdleAnchors.IconFillFrames - 2), "차오르는 중");

            animation.Tick(fillEnd + 0.05f - animation.Elapsed);   // 가득 찬 직후
            Assert.That(animation.IsPlaying, Is.True);
            Assert.That(icon.sprite.name, Does.EndWith("_" + (FacilityIdleAnchors.IconFillFrames - 1)), "원래 아이콘으로 돌아온다");
            if (animation.Kind == FacilityIdleKind.Charger)
            {
                Bounds visible = FacilityGroundedVisual.GetVisibleBounds(artwork.sprite);
                float canopyTop = artwork.transform.TransformPoint(visible.max).y;
                Assert.That(icon.bounds.max.y, Is.LessThan(canopyTop), "번쩍이는 순간에도 번개는 차양 아래에 있다");
                animation.Tick(FacilityIdleAnchors.ChargerSeconds - 0.2f - animation.Elapsed);
                Assert.That(icon.bounds.max.y, Is.LessThan(canopyTop), "사라지기 직전에도 번개는 천장과 이름표 영역에 닿지 않는다");
            }
            animation.Tick(FacilityIdleAnchors.DurationOf(animation.Kind));
            Assert.That(animation.IsPlaying, Is.False);
        }

        [Test]
        public void Portal_RotatesClockwise_AroundTheHoleCenter()
        {
            GameObject portal = Spawn("building.escape_portal.emergency");
            var network = new GameObject("PortalTestNetwork").AddComponent<PowerNetworkSystem>();
            spawned.Add(network.gameObject);
            portal.GetComponent<PowerNode>().SetNetwork(network);
            var spin = portal.GetComponent<PortalHoleSpin>();
            spin.Tick(0.0001f);
            Transform pivot = spin.HoleRoot.transform.Find("HoleSpin");
            float before = pivot.localEulerAngles.z;
            spin.Tick(1f);
            float after = pivot.localEulerAngles.z;
            Assert.That(Mathf.DeltaAngle(before, after), Is.EqualTo(-45f).Within(0.5f), "시계방향 = z 음수, 초당 45°");
        }

        [Test]
        public void SettlementSheet_HasOresPoofAndCoinFrames_WithPointFilter()
        {
            Assert.That(FacilityIdleSprites.HasSettlementSheet, Is.True, "Resources/Facilities/SettlementCoinSheet.png");
            for (int i = 0; i < 3; i++) Assert.That(FacilityIdleSprites.Ore(i), Is.Not.Null);
            for (int i = 0; i < 3; i++) Assert.That(FacilityIdleSprites.Poof(i), Is.Not.Null);
            for (int i = 0; i < 5; i++) Assert.That(FacilityIdleSprites.CoinFrame(i), Is.Not.Null);
            Sprite ore = FacilityIdleSprites.Ore(0);
            Assert.That(ore.texture.filterMode, Is.EqualTo(FilterMode.Point));
            Assert.That(ore.rect.width, Is.EqualTo(16f));
            Assert.That(FacilityIdleSprites.Ore(0), Is.Not.SameAs(FacilityIdleSprites.Ore(1)));
        }

        [Test]
        public void Settlement_ShowsThreeOresThatTurnIntoCoins_InsideTheScreen()
        {
            GameObject settlement = Spawn("building.settlement.basic");
            Power(settlement, true);
            var animation = settlement.GetComponent<FacilityIdleAnimation>();
            Assert.That(animation.PlayNow(), Is.True);
            SpriteRenderer[] All() => animation.EffectRoot.GetComponentsInChildren<SpriteRenderer>(true);

            animation.Tick(0.4f);   // 원석 단계
            Assert.That(All().Count(r => r.name.StartsWith("Ore") && r.enabled), Is.EqualTo(3));
            Assert.That(All().Count(r => r.name.StartsWith("Coin") && r.enabled), Is.EqualTo(0));

            animation.Tick(0.85f);  // t=1.25 금화 단계(깜빡임 시작 직후 구간은 피한다)
            Assert.That(All().Count(r => r.name.StartsWith("Ore") && r.enabled), Is.EqualTo(0), "원석은 사라지고");
            Assert.That(All().Count(r => r.name.StartsWith("Coin")), Is.EqualTo(3));

            // 도트가 화면 폭 안에 머문다.
            foreach (SpriteRenderer r in All().Where(r => r.enabled))
                Assert.That(r.bounds.size.x, Is.LessThan(settlement.transform.Find("VisualRoot/Artwork").GetComponent<SpriteRenderer>().bounds.size.x * FacilityIdleAnchors.SettlementScreenWidth), r.name);
            animation.EndEffect();
        }

        [Test]
        public void Light_DimsTheHaloValue_AtTheDipAndRestoresIt()
        {
            GameObject light = Spawn("building.light.basic");
            Power(light, true);
            var animation = light.GetComponent<FacilityIdleAnimation>();
            var dim = light.GetComponent<FacilityVisualDim>();
            Assert.That(dim, Is.Not.Null);

            animation.ScheduleAfter(0f);
            animation.Tick(0.001f);
            animation.Tick(0.06f);
            Assert.That(dim.Multiplier, Is.EqualTo(0.35f).Within(0.02f));
            animation.Tick(2f);
            Assert.That(dim.Multiplier, Is.EqualTo(1f));
        }

        [Test]
        public void NoPower_SkipsThatCycleOnly_AndWaitsForTheNextOne()
        {
            GameObject clinic = Spawn("building.clinic.basic");
            var animation = clinic.GetComponent<FacilityIdleAnimation>();
            Assert.That(animation.IsPowered, Is.False);

            animation.ScheduleAfter(0.1f);
            animation.Tick(0.2f);

            Assert.That(animation.IsPlaying, Is.False, "전력 없음");
            Assert.That(clinic.transform.Find(FacilityIdleAnimation.EffectRootName), Is.Null);
            Assert.That(animation.SecondsUntilNext, Is.InRange(27f, 40f), "그 주기만 건너뛰고 다음 주기를 기다린다");

            Power(clinic, true);
            animation.ScheduleAfter(0.1f);
            animation.Tick(0.2f);
            Assert.That(animation.IsPlaying, Is.True, "전력이 들어오면 연출한다");
        }

        [Test]
        public void SupplyRangeAlone_CountsAsOn_AndOutpostIsAlwaysOn()
        {
            GameObject charger = Spawn("building.charger.basic");
            var animation = charger.GetComponent<FacilityIdleAnimation>();
            Assert.That(animation.IsPowered, Is.False);
            charger.GetComponent<PowerFacility>().SetSupplyRangePowered(true, false);
            Assert.That(animation.IsPowered, Is.True, "IsInteractionAvailable || IsInSupplyRange");

            GameObject outpost = Spawn("building.outpost_core.basic");
            Assert.That(outpost.GetComponent<FacilityIdleAnimation>().IsPowered, Is.True);
        }

        [Test]
        public void LightAndStorage_InSupplyRange_PlayEvenThoughTheBridgeNeverSetsIsInSupplyRange()
        {
            // 조명·보관함은 GameplayEventBridge가 IsInSupplyRange를 채우지 않는다(범위와 무관하게 동작).
            // 연출은 별도 연출 전용 범위 값(IsInVisualSupplyRange)으로 켜짐을 본다.
            foreach (string id in new[] { "building.light.basic", "building.storage.basic" })
            {
                GameObject building = Spawn(id);
                var animation = building.GetComponent<FacilityIdleAnimation>();
                Assert.That(animation.PlayNow(), Is.False, id + " 범위 밖");

                building.GetComponent<PowerFacility>().SetVisualSupplyRange(true);
                Assert.That(building.GetComponent<PowerFacility>().IsInSupplyRange, Is.False, "기존 판정 값은 그대로");
                Assert.That(animation.IsPowered, Is.True, id);
                Assert.That(animation.PlayNow(), Is.True, id);
                animation.EndEffect();   // EditMode에서는 OnDestroy가 불리지 않으므로 조각 Sprite를 직접 정리한다.
            }
        }

        [Test]
        public void PowerCut_FadesWithin0_15Seconds_AndRestoresTheOriginalArtwork()
        {
            GameObject storage = Spawn("building.storage.basic");
            Power(storage, true);
            var animation = storage.GetComponent<FacilityIdleAnimation>();
            SpriteRenderer artwork = storage.transform.Find("VisualRoot/Artwork").GetComponent<SpriteRenderer>();
            animation.ScheduleAfter(0f);
            animation.Tick(0.01f);
            animation.Tick(0.3f);
            Assert.That(animation.IsPlaying, Is.True);

            Power(storage, false);
            animation.Tick(0.05f);
            Assert.That(animation.IsFading, Is.True);
            animation.Tick(FacilityIdleAnimation.InterruptFadeSeconds);

            Assert.That(animation.IsPlaying, Is.False, "0.15초 안에 사라진다");
            Assert.That(artwork.forceRenderingOff, Is.False);
            Assert.That(storage.GetComponentsInChildren<SpriteRenderer>(true).Any(r => r.name == "StorageOpenVideo"), Is.False);
            Assert.That(storage.transform.Find(FacilityIdleAnimation.EffectRootName), Is.Null);
        }

        [Test]
        public void ConstructionEffect_DelaysTheIdleEffect_UntilItFinishes()
        {
            GameObject charger = Spawn("building.charger.basic");
            Power(charger, true);
            var animation = charger.GetComponent<FacilityIdleAnimation>();
            FacilityConstructionEffect build = FacilityConstructionEffect.Begin(charger, "building.charger.basic");
            Assert.That(build, Is.Not.Null);

            animation.ScheduleAfter(0f);
            animation.Tick(0.01f);
            Assert.That(animation.IsPlaying, Is.False, "건설 연출 중에는 겹치지 않는다");
            Assert.That(charger.transform.Find(FacilityIdleAnimation.EffectRootName), Is.Null);

            build.Complete();
            animation.ScheduleAfter(0f);
            animation.Tick(0.01f);
            Assert.That(animation.IsPlaying, Is.True);
        }

        [Test]
        public void Relocating_EndsTheEffect_AndReschedulesFromScratch()
        {
            GameObject clinic = Spawn("building.clinic.basic");
            Power(clinic, true);
            var animation = clinic.GetComponent<FacilityIdleAnimation>();
            animation.ScheduleAfter(0f);
            animation.Tick(0.01f);
            Assert.That(animation.IsPlaying, Is.True);

            clinic.GetComponent<BuildingInstance>().Initialize("clinic-moved", "building.clinic.basic");

            Assert.That(animation.IsPlaying, Is.False);
            Assert.That(clinic.transform.Find(FacilityIdleAnimation.EffectRootName), Is.Null);
            Assert.That(animation.SecondsUntilNext, Is.InRange(3f, 40f));
            Assert.That(clinic.GetComponents<FacilityIdleAnimation>(), Has.Length.EqualTo(1));
        }

        [Test]
        public void ReducedMotion_KeepsThePeriodicEffect()
        {
            AccessibilityPreferences.ReduceMotion = true;
            GameObject settlement = Spawn("building.settlement.basic");
            Power(settlement, true);
            var animation = settlement.GetComponent<FacilityIdleAnimation>();
            animation.ScheduleAfter(0f);
            animation.Tick(0.01f);
            Assert.That(animation.IsPlaying, Is.True, "화면 전체가 움직이는 연출이 아니라 줄인 움직임에서도 유지");
        }

        [Test]
        public void SameKind_NeverStartsWithinHalfASecondOfEachOther()
        {
            var animations = new List<FacilityIdleAnimation>();
            for (int i = 0; i < 6; i++) animations.Add(Spawn("building.light.basic").GetComponent<FacilityIdleAnimation>());
            // 모두 같은 시각을 원해도 서로 0.5초 이상 벌어진다.
            foreach (var animation in animations) animation.ScheduleAfter(10f);

            var times = animations.Select(a => a.SecondsUntilNext).OrderBy(x => x).ToList();
            for (int i = 1; i < times.Count; i++)
                Assert.That(times[i] - times[i - 1], Is.GreaterThanOrEqualTo(FacilityIdleAnimation.StaggerWindowSeconds - 0.001f),
                    string.Join(", ", times));
            Assert.That(times[0], Is.GreaterThanOrEqualTo(10f), "밀리기만 한다");
        }

        [Test]
        public void DifferentKinds_DoNotStaggerEachOther()
        {
            var light = Spawn("building.light.basic").GetComponent<FacilityIdleAnimation>();
            var clinic = Spawn("building.clinic.basic").GetComponent<FacilityIdleAnimation>();
            light.ScheduleAfter(10f);
            clinic.ScheduleAfter(10f);
            Assert.That(clinic.SecondsUntilNext, Is.EqualTo(10f).Within(0.0001f));
        }

        [Test]
        public void DebugPeriod_HasAMinimumOf2Seconds_AndResets()
        {
            Assert.That(FacilityIdleAnimation.SetPeriodOverride(0.5f), Is.EqualTo(2f));
            GameObject clinic = Spawn("building.clinic.basic");
            var animation = clinic.GetComponent<FacilityIdleAnimation>();
            Assert.That(animation.CurrentPeriod, Is.EqualTo(2f));
            animation.ScheduleAfter(0f);
            animation.Tick(0.01f);   // 전력 없음 → 건너뜀 → 새 주기로 다음 대기
            Assert.That(animation.SecondsUntilNext, Is.InRange(1f, 3.1f));

            FacilityIdleAnimation.SetPeriodOverride(0f);
            Assert.That(FacilityIdleAnimation.PeriodOverride, Is.LessThanOrEqualTo(0f));
            Assert.That(animation.CurrentPeriod, Is.EqualTo(30f));
        }

        [Test]
        public void PlayNow_IgnoresTheSchedule_ButNotPower()
        {
            GameObject light = Spawn("building.light.basic");
            var animation = light.GetComponent<FacilityIdleAnimation>();
            Assert.That(animation.PlayNow(), Is.False, "꺼진 시설은 재생하지 않는다");
            Power(light, true);
            Assert.That(animation.PlayNow(), Is.True);
            Assert.That(animation.PlayNow(), Is.False, "이미 재생 중");
        }

        [Test]
        public void Destroying_AFacilityMidEffect_CleansUp()
        {
            GameObject storage = Spawn("building.storage.basic");
            Power(storage, true);
            var animation = storage.GetComponent<FacilityIdleAnimation>();
            Assert.That(animation.PlayNow(), Is.True);
            animation.EndEffect();
            Assert.That(animation.IsPlaying, Is.False);
            Assert.That(storage.transform.Find("VisualRoot/Artwork").GetComponent<SpriteRenderer>().forceRenderingOff, Is.False);
            Assert.That(storage.transform.Find(FacilityIdleAnimation.EffectRootName), Is.Null);
        }

        // ── 포탈 ──

        [Test]
        public void Portal_HasNoPeriodicEffect_ButGetsTheHoleSpin()
        {
            GameObject portal = Spawn("building.escape_portal.emergency");
            Assert.That(portal.GetComponent<FacilityIdleAnimation>(), Is.Null);
            Assert.That(portal.GetComponent<PortalHoleSpin>(), Is.Not.Null);
        }

        [Test]
        public void Portal_SpinsOnlyWhilePowered_AndResumesFromTheSameAngle()
        {
            GameObject portal = Spawn("building.escape_portal.emergency");
            var spin = portal.GetComponent<PortalHoleSpin>();
            SpriteRenderer artwork = portal.transform.Find("VisualRoot/Artwork").GetComponent<SpriteRenderer>();
            Sprite active = artwork.sprite;
            Sprite inactive = Resources.Load<Sprite>("Facilities/PortalInactive");
            Assert.That(inactive, Is.Not.Null);

            // 전력망 연결 전: 빈 링 상태라 소용돌이를 만들지도 않는다.
            artwork.sprite = inactive;
            spin.Tick(1f);
            Assert.That(spin.IsHoleVisible, Is.False);
            Assert.That(spin.HoleRoot, Is.Null, "꺼짐에서는 홀 오브젝트도 없다");

            var network = new GameObject("PortalTestNetwork").AddComponent<PowerNetworkSystem>();
            spawned.Add(network.gameObject);
            var node = portal.GetComponent<PowerNode>();
            node.SetNetwork(network);
            artwork.sprite = active;

            spin.Tick(0.0001f);
            Assert.That(spin.IsHoleVisible, Is.True, "켜짐(웜홀 그림)에서 홀이 보인다");
            float start = spin.Angle;
            spin.Tick(2f);
            Assert.That(Mathf.DeltaAngle(start, spin.Angle), Is.EqualTo(45f * 2f).Within(0.5f), "8초에 한 바퀴 = 초당 45°");

            var renderers = spin.HoleRoot.GetComponentsInChildren<SpriteRenderer>(true);
            Assert.That(renderers, Has.Length.EqualTo(1), "웜홀 복사본만(별도 소용돌이 없음)");
            Assert.That(renderers.All(r => r.maskInteraction == SpriteMaskInteraction.VisibleInsideMask), Is.True, "링 테두리는 가리지 않는다");
            var mask = spin.HoleRoot.GetComponentInChildren<SpriteMask>(true);
            Assert.That(mask, Is.Not.Null);
            Assert.That(mask.transform.localScale.x * 0.5f, Is.LessThan(FacilityIdleAnchors.PortalHoleRadius * FacilityGroundedVisual.GetVisibleBounds(active).size.x), "홀보다 살짝 작은 마스크");

            // 전력 끊김: 소용돌이는 바로 사라지고 각도는 멈춘다.
            node.SetNetwork(null);
            artwork.sprite = inactive;
            float stopped = spin.Angle;
            spin.Tick(1f);
            Assert.That(spin.IsHoleVisible, Is.False);
            Assert.That(spin.Angle, Is.EqualTo(stopped));
            Assert.That(renderers.All(r => !r.enabled), Is.True);

            // 다시 연결하면 같은 각도에서 이어진다.
            node.SetNetwork(network);
            artwork.sprite = active;
            spin.Tick(0f);
            Assert.That(spin.IsHoleVisible, Is.True);
            Assert.That(spin.Angle, Is.EqualTo(stopped).Within(0.001f));
            spin.Tick(1f);
            Assert.That(Mathf.DeltaAngle(stopped, spin.Angle), Is.EqualTo(45f).Within(0.5f));
        }

        [Test]
        public void Portal_HidesTheHoleSpinWhileTheConstructionEffectPlays_AndKeepsSpinningInReducedMotion()
        {
            AccessibilityPreferences.ReduceMotion = true;
            GameObject portal = Spawn("building.escape_portal.emergency");
            var network = new GameObject("PortalTestNetwork").AddComponent<PowerNetworkSystem>();
            spawned.Add(network.gameObject);
            portal.GetComponent<PowerNode>().SetNetwork(network);
            var spin = portal.GetComponent<PortalHoleSpin>();
            spin.Tick(0.1f);
            Assert.That(spin.IsHoleVisible, Is.True, "줄인 움직임에서도 회전은 그대로");

            AccessibilityPreferences.ReduceMotion = false;
            FacilityConstructionEffect build = FacilityConstructionEffect.Begin(portal, "building.escape_portal.emergency");
            Assert.That(build, Is.Not.Null);
            spin.Tick(0.1f);
            Assert.That(spin.IsHoleVisible, Is.False, "건설 연출 중에는 완성본이 가려지므로 회전도 숨긴다");
            build.Complete();
            spin.Tick(0.1f);
            Assert.That(spin.IsHoleVisible, Is.True);
        }

        private void Power(GameObject building, bool on)
        {
            var facility = building.GetComponent<PowerFacility>();
            if (facility != null) facility.SetSupplyRangePowered(on, false);
        }

        private GameObject Spawn(string buildingId, Vector2Int? footprint = null)
        {
            string path = Facilities.First(entry => entry.id == buildingId).path;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.That(prefab, Is.Not.Null, path);
            GameObject go = Object.Instantiate(prefab);
            spawned.Add(go);
            BuildingInstance instance = go.GetComponent<BuildingInstance>();
            if (instance == null) instance = go.AddComponent<BuildingInstance>();
            instance.Initialize($"{buildingId}-idle{sequence++}", buildingId,
                footprint ?? (buildingId == "building.charger.basic" ? new Vector2Int(2, 2) : default));
            return go;
        }
    }
}

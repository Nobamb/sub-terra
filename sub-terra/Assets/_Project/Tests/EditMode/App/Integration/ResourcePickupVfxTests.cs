using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using SubTerra.App.Core.Data;
using SubTerra.App.Integration;
using SubTerra.App.UI.FacilityNameTag;
using SubTerra.App.UI.PlayerStatus;
using SubTerra.Gameplay.Mining;
using SubTerra.Shared;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SubTerra.App.Tests.Integration
{
    /// <summary>B-194: 수확 홀로그램 위 자원 아이콘(청록 덮개 → 위에서 아래 스캔 → 원래 색, 연료 후광).</summary>
    public sealed class ResourcePickupVfxTests
    {
        private const string Key = MiningProgressHud.ProgressKey;
        private GameObject host;
        private ResourcePickupVfx vfx;
        private PlayerHeadMessageStack stack;
        private MiningSystem mining;
        private Sprite sprite;
        private Texture2D texture;
        private bool reduce;

        [SetUp]
        public void SetUp()
        {
            reduce = AccessibilityPreferences.ReduceMotion;
            AccessibilityPreferences.ReduceMotion = false;
            host = new GameObject("PickupTest");
            mining = host.AddComponent<MiningSystem>();
            stack = host.AddComponent<PlayerHeadMessageStack>();
            vfx = host.AddComponent<ResourcePickupVfx>();
            vfx.ManualTick = true;
            texture = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            var pixels = new Color32[256];
            for (var i = 0; i < pixels.Length; i++) pixels[i] = new Color32(255, 255, 255, 255);
            texture.SetPixels32(pixels);
            texture.Apply();
            sprite = Sprite.Create(texture, new Rect(0f, 0f, 16f, 16f), Vector2.one * 0.5f, 16f);
            vfx.BindTo(mining, stack, _ => sprite);
            stack.Model.SetProgress(Key, "채굴 50%", 0.5f);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(host);
            Object.DestroyImmediate(sprite);
            Object.DestroyImmediate(texture);
            AccessibilityPreferences.ReduceMotion = reduce;
        }

        private void Mined(string id)
        {
            var handlers = (Action<Vector3Int, MiningTileDto>)typeof(MiningSystem)
                .GetField("TileMined", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(mining);
            handlers?.Invoke(Vector3Int.zero, new MiningTileDto { mineralId = id, quantity = 1 });
        }

        private void Progress(MiningPhase phase)
        {
            var handlers = (Action<MiningProgressState>)typeof(MiningSystem)
                .GetField("ProgressChanged", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(mining);
            handlers?.Invoke(new MiningProgressState(phase, MiningFailureReason.None, 0f, 0f, 0));
        }

        private ResourceHologramIcon Active()
        {
            foreach (var icon in vfx.Icons) if (icon != null && icon.IsActive) return icon;
            return null;
        }

        private void Run(float seconds)
        {
            for (var t = 0f; t < seconds - 0.0001f; t += 0.01f) vfx.Tick(0.01f);
        }

        // ---------- 순수 시간표 ----------

        [Test]
        public void Timeline_ScanRevealsFromTopToBottom_InsideTwoToThreeTenths()
        {
            Assert.That(ResourceHologramTimeline.DefaultScanSeconds, Is.InRange(0.2f, 0.3f));
            Assert.That(ResourceHologramTimeline.ClampScan(0.01f), Is.EqualTo(0.2f));
            Assert.That(ResourceHologramTimeline.ClampScan(5f), Is.EqualTo(0.3f));

            const int rows = 8;
            for (var r = 0; r < rows; r++) Assert.That(ResourceHologramTimeline.RowReveal(0f, r, rows), Is.Zero, "시작은 모두 덮여 있다");
            for (var r = 0; r < rows; r++) Assert.That(ResourceHologramTimeline.RowReveal(1f, r, rows), Is.EqualTo(1f), "끝은 모두 드러난다");

            Assert.That(ResourceHologramTimeline.RowReveal(0.5f, 0, rows), Is.EqualTo(1f), "위쪽 줄이 먼저 드러난다");
            Assert.That(ResourceHologramTimeline.RowReveal(0.5f, rows - 1, rows), Is.Zero, "아래쪽 줄은 아직 덮여 있다");
            var previous = 2f;
            for (var r = 0; r < rows; r++)
            {
                var value = ResourceHologramTimeline.RowReveal(0.4f, r, rows);
                Assert.That(value, Is.LessThanOrEqualTo(previous), "아래로 갈수록 늦게 드러난다");
                previous = value;
            }
        }

        [Test]
        public void Timeline_FuelHaloOnlyAfterScan_AndOnlyOnce()
        {
            var scan = 0.22f;
            Assert.That(ResourceHologramTimeline.FuelHaloProgress(0.1f, scan), Is.LessThan(0f));
            Assert.That(ResourceHologramTimeline.FuelHaloProgress(scan - 0.001f, scan), Is.LessThan(0f));
            Assert.That(ResourceHologramTimeline.FuelHaloProgress(scan + 0.1f, scan), Is.InRange(0f, 1f));
            Assert.That(ResourceHologramTimeline.FuelHaloProgress(scan + ResourceHologramTimeline.FuelHaloSeconds + 0.01f, scan),
                Is.LessThan(0f), "한 번 지나가면 다시 나오지 않는다");
            Assert.That(ResourceHologramTimeline.FuelHaloAlpha(-1f), Is.Zero);
            Assert.That(ResourceHologramTimeline.FuelHaloAlpha(0.2f), Is.GreaterThan(0.9f));
        }

        [Test]
        public void Timeline_NoiseIsDeterministic()
        {
            Assert.That(ResourceHologramTimeline.StripeAlpha(3, 0.1f), Is.EqualTo(ResourceHologramTimeline.StripeAlpha(3, 0.1f)));
            Assert.That(ResourceHologramTimeline.Jitter(2, 0.2f), Is.InRange(-1f, 1f));
            Assert.That(ResourceHologramTimeline.StripeAlpha(0, 0f), Is.GreaterThan(0f));
        }

        // ---------- 연결: 지급 확정 + 같은 자원 채굴 완료 ----------

        [Test]
        public void OnlyAcceptedMatchingResourcePlaysOnce_AndRebindingDoesNotDuplicate()
        {
            Mined(DataIds.Minerals.Copper);
            Assert.That(vfx.ActiveCount, Is.Zero, "지급 확정이 없으면 안 뜬다");

            vfx.BindTo(mining, stack, _ => sprite);
            vfx.QueueAccepted(DataIds.Minerals.Copper, 2);
            Mined(DataIds.Minerals.Copper);
            Mined(DataIds.Minerals.Copper);
            Assert.That(vfx.ActiveCount, Is.EqualTo(1), "한 번 지급에 한 번만");

            vfx.QueueAccepted(DataIds.Minerals.Copper, 1);
            Mined(DataIds.Minerals.Iron);
            Mined(DataIds.Minerals.Copper);
            Assert.That(vfx.ActiveCount, Is.EqualTo(1), "다른 자원의 지급은 소비되고 새지 않는다");

            vfx.QueueAccepted(DataIds.Minerals.Copper, 0);
            Mined(DataIds.Minerals.Copper);
            Assert.That(vfx.ActiveCount, Is.EqualTo(1), "0개(골드만 있는 블록 포함)는 안 뜬다");
        }

        [Test]
        public void FailedOrCancelledMining_ClearsPendingAndShowsNothing()
        {
            foreach (var phase in new[] { MiningPhase.Failed, MiningPhase.Cancelled })
            {
                vfx.QueueAccepted(DataIds.Minerals.Copper, 1);
                Progress(phase);
                Mined(DataIds.Minerals.Copper);
                Assert.That(vfx.ActiveCount, Is.Zero);
            }
        }

        [Test]
        public void NoShownYield_ShowsNothing_AndNothingIsReserved()
        {
            stack.Model.Dismiss(Key);
            for (var i = 0; i < 20; i++) stack.Model.Advance(0.05f);
            Assert.That(vfx.PlayAccepted(DataIds.Minerals.Copper, 1), Is.False);
            Assert.That(vfx.ActiveCount, Is.Zero);
        }

        // ---------- 청록 덮개 → 스캔 → 원래 색 ----------

        [Test]
        public void Scan_StartsTealCovered_ThenRevealsOriginalColorTopToBottom()
        {
            Assert.That(vfx.PlayAccepted(DataIds.Minerals.Copper, 1), Is.True);
            var icon = Active();
            Assert.That(icon.RowCount, Is.GreaterThan(2));
            vfx.Tick(0.01f);
            for (var r = 0; r < icon.RowCount; r++)
            {
                Assert.That(icon.RowCovered(r), Is.True, "시작엔 모든 줄이 청록 덮개 아래");
                if (r == 0) continue; // 맨 윗줄은 스캔이 막 닿기 시작해 번쩍이는 중이다.
                var color = icon.RowColor(r);
                Assert.That(color.r, Is.LessThan(0.65f), "청록(붉은 성분이 적다)");
                Assert.That(color.g, Is.GreaterThan(0.9f));
            }

            Run(ResourceHologramTimeline.DefaultScanSeconds * 0.5f);
            Assert.That(icon.ScanLineVisible, Is.True);
            Assert.That(icon.RowCovered(0), Is.False, "위쪽 줄은 먼저 드러난다");
            Assert.That(icon.RowColor(0).r, Is.GreaterThan(0.95f));
            Assert.That(icon.RowCovered(icon.RowCount - 1), Is.True, "아래쪽 줄은 아직 덮여 있다");

            Run(ResourceHologramTimeline.DefaultScanSeconds);
            Assert.That(icon.ScanLineVisible, Is.False);
            for (var r = 0; r < icon.RowCount; r++)
            {
                Assert.That(icon.RowCovered(r), Is.False);
                var color = icon.RowColor(r);
                Assert.That(color.r, Is.GreaterThan(0.95f), "원래 색이 청록에 묻히지 않는다");
                Assert.That(color.a, Is.GreaterThan(0.85f));
            }
        }

        [Test]
        public void ScanTakesBetweenTwoAndThreeTenths_AndIsAdjustable()
        {
            vfx.PlayAccepted(DataIds.Minerals.Copper, 1);
            var icon = Active();
            Run(0.19f);
            Assert.That(icon.RowCovered(icon.RowCount - 1), Is.True, "0.2초 전에는 끝나지 않는다");
            Run(0.12f);
            Assert.That(icon.RowCovered(icon.RowCount - 1), Is.False, "0.3초 안에는 끝난다");

            vfx.ScanSeconds = 10f;
            Assert.That(vfx.ScanSeconds, Is.EqualTo(ResourceHologramTimeline.MaxScanSeconds));
            vfx.ScanSeconds = 0.25f;
            vfx.PlayAccepted(DataIds.Minerals.Iron, 1);
            Assert.That(Active().ScanSeconds, Is.EqualTo(0.25f));
        }

        // ---------- 엔진 연료 ----------

        [Test]
        public void Fuel_IsBigger_HasThreeSparks_AndWarmHaloOnceAfterScanOnly()
        {
            vfx.PlayAccepted(DataIds.Minerals.Copper, 1);
            var normalScale = Active().Body.localScale.x;
            Assert.That(Active().HaloVisible, Is.False);
            Assert.That(Active().VisibleSparkCount, Is.Zero, "일반 광물은 후광·반짝임이 없다");
            Run(1f);
            Assert.That(Active().HaloPlayCount, Is.Zero);
            Assert.That(Active().VisibleSparkCount, Is.Zero);

            stack.Model.SetProgress(Key, "채굴 20%", 0.2f);
            stack.Model.ShowResult(Key, "엔진 연료 +1");
            vfx.PlayAccepted(DataIds.RareItems.EngineFuel, 1);
            var icon = Active();
            Assert.That(icon.IsRare, Is.True);
            Assert.That(icon.Body.localScale.x, Is.GreaterThan(normalScale * 1.2f), "연료 아이콘이 더 크다");

            Run(ResourceHologramTimeline.DefaultScanSeconds * 0.8f);
            Assert.That(icon.HaloVisible, Is.False, "스캔이 끝나기 전에는 후광이 없다");
            Assert.That(icon.HaloPlayCount, Is.Zero);
            Assert.That(icon.VisibleSparkCount, Is.EqualTo(3));

            Run(ResourceHologramTimeline.DefaultScanSeconds * 0.2f + 0.08f);
            Assert.That(icon.HaloVisible, Is.True, "색이 다 드러난 뒤 후광이 퍼진다");
            Assert.That(icon.HaloPlayCount, Is.EqualTo(1));
            var halo = icon.HaloColor;
            Assert.That(halo.r, Is.GreaterThan(0.9f));
            Assert.That(halo.b, Is.LessThan(0.4f), "따뜻한 색(금색·주황)이다");

            Run(1f);
            Assert.That(icon.HaloVisible, Is.False);
            Assert.That(icon.HaloPlayCount, Is.EqualTo(1), "후광은 한 번만");
            Assert.That(icon.VisibleSparkCount, Is.EqualTo(3));
        }

        // ---------- 머리 위 스택과의 관계 ----------

        [Test]
        public void Icon_ReservesHeightAboveOnly_AndDoesNotEatMaxEntries()
        {
            var model = stack.Model;
            var entry = model.ShowResult(Key, "구리 +1");
            vfx.PlayAccepted(DataIds.Minerals.Copper, 1);
            Assert.That(entry.Headroom, Is.GreaterThan(0f));
            var count = model.Entries.Count;
            model.ShowInfo("a", "A");
            model.ShowInfo("b", "B");
            model.ShowInfo("c", "C");
            for (var i = 0; i < 20; i++) model.Advance(0.05f);

            Assert.That(model.Entries.Count, Is.EqualTo(count + 3));
            var wanted = 0;
            foreach (var e in model.Entries) if (e.Wanted) wanted++;
            Assert.That(wanted, Is.EqualTo(PlayerStatusTimeline.MaxEntries), "아이콘 때문에 문구가 더 빨리 밀려나지 않는다");
            Assert.That(entry.Wanted, Is.True);
            var a = model.Find("a");
            Assert.That(a.Offset, Is.EqualTo(entry.Height + entry.Headroom + PlayerStatusTimeline.StackGap).Within(0.01f),
                "위 항목은 아이콘 높이만큼 올라간다(겹치지 않는다)");
        }

        [Test]
        public void IconFollowsEntry_AndNextMiningReleasesIt()
        {
            stack.Model.ShowResult(Key, "구리 +1");
            vfx.PlayAccepted(DataIds.Minerals.Copper, 1);
            var icon = Active();
            vfx.Tick(0.02f);
            var shown = stack.Model.Find(Key);
            var expected = stack.WorldPositionOf(shown.Offset + shown.Height);
            Assert.That(icon.Root.position, Is.EqualTo(expected));
            Assert.That(icon.Root.gameObject.layer, Is.EqualTo(FacilityNameTagLayers.MainScreen));
            foreach (var renderer in icon.Root.GetComponentsInChildren<SpriteRenderer>(true))
            {
                Assert.That(FacilityNameTagLayers.IsHiddenFromCctv(renderer.gameObject), Is.True, "CCTV에 찍히지 않는다");
            }

            var entry = stack.Model.SetProgress(Key, "채굴 5%", 0.05f);
            Assert.That(entry.Headroom, Is.Zero, "다음 채굴이 시작되면 자리를 비운다");
            Run(ResourceHologramTimeline.ReleaseSeconds + 0.05f);
            Assert.That(vfx.ActiveCount, Is.Zero);
        }

        [Test]
        public void IconLeavesTogetherWithEntry()
        {
            stack.Model.ShowResult(Key, "구리 +1");
            vfx.PlayAccepted(DataIds.Minerals.Copper, 1);
            for (var i = 0; i < 70; i++)
            {
                stack.Model.Advance(0.05f);
                vfx.Tick(0.05f);
            }

            Assert.That(vfx.ActiveCount, Is.Zero, "수확 문구가 사라지면 아이콘도 사라진다");
        }

        // ---------- 연출 구성(프로젝터·팝인·글리치·폭발) ----------

        [Test]
        public void Timeline_PopOvershootsThenSettles_AndBobStartsAfterPop()
        {
            Assert.That(ResourceHologramTimeline.PopScale(0f), Is.EqualTo(0.35f).Within(0.001f), "작게 시작");
            var peak = 0f;
            for (var t = 0f; t <= ResourceHologramTimeline.PopSeconds; t += 0.005f) peak = Mathf.Max(peak, ResourceHologramTimeline.PopScale(t));
            Assert.That(peak, Is.GreaterThan(1.03f), "한 번 튀어 오른다");
            Assert.That(ResourceHologramTimeline.PopScale(ResourceHologramTimeline.PopSeconds), Is.EqualTo(1f).Within(0.001f));
            Assert.That(ResourceHologramTimeline.Bob(0.1f), Is.Zero);
            Assert.That(Mathf.Abs(ResourceHologramTimeline.Bob(1f)), Is.GreaterThan(0.01f));
            Assert.That(ResourceHologramTimeline.GlitchAmount(0.1f, 0.22f), Is.EqualTo(1f));
            Assert.That(ResourceHologramTimeline.GlitchAmount(0.5f, 0.22f), Is.Zero);
            Assert.That(ResourceHologramTimeline.BurstProgress(0.1f, 0.22f), Is.LessThan(0f));
            Assert.That(ResourceHologramTimeline.BurstProgress(0.3f, 0.22f), Is.InRange(0f, 1f));
        }

        [Test]
        public void Projector_BeamRingBracketsAndMotesAppearWithTheIcon()
        {
            vfx.PlayAccepted(DataIds.Minerals.Copper, 1);
            var icon = Active();
            Run(0.1f);
            Assert.That(icon.BeamVisible, Is.True, "프레임에서 아이콘으로 빔이 퍼진다");
            Assert.That(icon.BaseRingVisible, Is.True, "프레임 위 바닥 링");
            Assert.That(icon.VisibleBracketCount, Is.EqualTo(8), "코너 브래킷 8개");
            Assert.That(icon.VisibleMoteCount, Is.GreaterThan(8), "데이터 입자");
            Assert.That(icon.GhostVisible, Is.True, "스캔 중엔 색수차 유령이 떤다");
            Assert.That(icon.PopScaleNow, Is.LessThan(1.2f));
            Run(0.6f);
            Assert.That(icon.GhostVisible, Is.False, "스캔이 끝나면 유령은 합쳐져 사라진다");
            Assert.That(icon.BeamVisible, Is.True, "표시되는 동안 빔은 남는다");
            Assert.That(icon.PopScaleNow, Is.EqualTo(1f).Within(0.001f));
        }

        [Test]
        public void Burst_FlashShockwaveAndSparksFireOnceRightAfterScan()
        {
            vfx.PlayAccepted(DataIds.Minerals.Copper, 1);
            var icon = Active();
            Run(ResourceHologramTimeline.DefaultScanSeconds - 0.03f);
            Assert.That(icon.ShockVisible, Is.False);
            Assert.That(icon.BurstPlayCount, Is.Zero);
            Run(0.08f);
            Assert.That(icon.ShockVisible, Is.True, "충격파 고리");
            Assert.That(icon.FlashVisible, Is.True, "섬광");
            Assert.That(icon.VisibleBurstCount, Is.EqualTo(12), "방사형 불꽃");
            Assert.That(icon.BurstPlayCount, Is.EqualTo(1));
            Run(1f);
            Assert.That(icon.ShockVisible, Is.False);
            Assert.That(icon.VisibleBurstCount, Is.Zero);
            Assert.That(icon.BurstPlayCount, Is.EqualTo(1), "폭발은 한 번만");
        }

        [Test]
        public void FuelBurstIsGoldAndBigger_AndHasRays_WhileOrdinaryHasNone()
        {
            vfx.PlayAccepted(DataIds.Minerals.Copper, 1);
            Run(0.5f);
            Assert.That(Active().VisibleRayCount, Is.Zero, "일반 광물엔 광선이 없다");
            var normalShock = Active().ShockColor;
            Assert.That(normalShock.b, Is.GreaterThan(normalShock.r), "일반 충격파는 청록 계열");

            stack.Model.SetProgress(Key, "채굴 20%", 0.2f);
            stack.Model.ShowResult(Key, "엔진 연료 +1");
            vfx.PlayAccepted(DataIds.RareItems.EngineFuel, 1);
            var fuel = Active();
            Run(ResourceHologramTimeline.DefaultScanSeconds + 0.1f);
            Assert.That(fuel.VisibleRayCount, Is.EqualTo(8), "금빛 광선 8줄");
            Assert.That(fuel.VisibleBurstCount, Is.EqualTo(20), "연료는 불꽃이 더 많다");
            Assert.That(fuel.ShockColor.r, Is.GreaterThan(fuel.ShockColor.b), "연료 충격파는 따뜻한 색");
        }

        // ---------- 풀 ----------

        [Test]
        public void PoolIsBounded()
        {
            stack.Model.MaxEntries = 40;
            for (var i = 0; i < 24; i++)
            {
                var key = "k" + i;
                stack.Model.ShowInfo(key, "x");
                Assert.That(vfx.PlayAccepted(DataIds.Minerals.Iron, 1, key), Is.True);
            }

            Assert.That(vfx.AllocatedCount, Is.EqualTo(ResourcePickupVfx.Capacity));
            Assert.That(vfx.ActiveCount, Is.EqualTo(ResourcePickupVfx.Capacity));
            Assert.That(ResourcePickupVfx.Capacity, Is.EqualTo(8));

            for (var i = 0; i < 32; i++) vfx.PlayAccepted(DataIds.Minerals.Copper, 1, "k23");
            Assert.That(vfx.AllocatedCount, Is.EqualTo(ResourcePickupVfx.Capacity));

            vfx.Unbind();
            Assert.That(vfx.ActiveCount, Is.Zero);
            Assert.That(vfx.AllocatedCount, Is.EqualTo(ResourcePickupVfx.Capacity), "풀은 재사용한다");
        }

        [Test]
        public void DisableHidesEverything_AndLaterMiningShowsNothing()
        {
            stack.Model.ShowResult(Key, "구리 +1");
            vfx.PlayAccepted(DataIds.Minerals.Copper, 1);
            var entry = stack.Model.Find(Key);
            vfx.enabled = false;
            // 일반 MonoBehaviour의 EditMode 검사는 실제 비활성화 콜백을 실행하지 않는다.
            typeof(ResourcePickupVfx).GetMethod("OnDisable", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(vfx, null);
            Assert.That(vfx.ActiveCount, Is.Zero);
            Assert.That(entry.Headroom, Is.Zero);
            vfx.QueueAccepted(DataIds.Minerals.Copper, 1);
            Mined(DataIds.Minerals.Copper);
            Assert.That(vfx.ActiveCount, Is.Zero);
        }

        // ---------- 모션 감소·소리·수량 ----------

        [Test]
        public void ReduceMotionDoesNotShortenScanOrRemoveHalo()
        {
            var normal = Sample();
            AccessibilityPreferences.ReduceMotion = true;
            vfx.Unbind();
            vfx.BindTo(mining, stack, _ => sprite);
            stack.Model.SetProgress(Key, "채굴 50%", 0.5f);
            var reduced = Sample();
            Assert.That(reduced, Is.EqualTo(normal), "모션 감소여도 스캔·후광·반짝임이 같다");
            Assert.That(reduced, Does.Contain("halo=1"));
            Assert.That(reduced, Does.Contain("sparks=3"));
        }

        private string Sample()
        {
            stack.Model.ShowResult(Key, "엔진 연료 +1");
            vfx.PlayAccepted(DataIds.RareItems.EngineFuel, 1);
            var icon = Active();
            var report = "";
            foreach (var t in new[] { 0.05f, 0.1f, 0.1f, 0.1f, 0.2f })
            {
                Run(t);
                report += $"{icon.ScanProgress:F2}/{icon.RowCovered(icon.RowCount - 1)}/{icon.VisibleSparkCount};";
            }

            return report + "halo=" + icon.HaloPlayCount + ";sparks=" + icon.VisibleSparkCount;
        }

        [Test]
        public void VfxSourceHasNoRewardSoundOrMotionReductionLogic()
        {
            foreach (var name in new[] { "ResourcePickupVfx.cs", "ResourceHologramIcon.cs", "ResourceHologramTimeline.cs" })
            {
                var source = File.ReadAllText(Path.Combine(Application.dataPath, "_Project/Scripts/App/Integration", name));
                Assert.That(source, Does.Not.Contain("SfxPlayer"), name);
                Assert.That(source, Does.Not.Contain("SfxId"), name);
                Assert.That(source, Does.Not.Contain("InventoryService"), name);
                Assert.That(source, Does.Not.Contain("IResourceWallet"), name);
                Assert.That(source, Does.Not.Contain("ReduceMotion"), name);
            }
        }
    }
}

using System.Collections.Generic;
using NUnit.Framework;
using SubTerra.App.Core.Data;
using SubTerra.App.Editor.DataValidation;
using SubTerra.App.Inventory;
using SubTerra.App.State;
using SubTerra.App.Tutorial;
using SubTerra.App.UI.Tutorial;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SubTerra.App.Tests.Tutorial
{
    /// <summary>
    /// prompt-B 153 화물 버리기 창: 버릴 수량(min·필요), 문구, 페이지 전환, 완료 뒤 한 번 지급, 연타, 폰트 글자.
    /// </summary>
    public sealed class PromptB153CargoDiscardPopupTests
    {
        private const string Copper = DataIds.Minerals.Copper;
        private const string Iron = DataIds.Minerals.Iron;
        private const string Lithium = DataIds.Minerals.Lithium;

        private readonly List<GameObject> spawned = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            for (var i = 0; i < spawned.Count; i++)
            {
                if (spawned[i] != null)
                {
                    Object.DestroyImmediate(spawned[i]);
                }
            }

            spawned.Clear();
        }

        // ---------------- 수량 계산 ----------------

        [Test]
        public void Clamp_NeverMoreThanOwned_NeverNegative()
        {
            Assert.That(CargoDiscardMath.Clamp(5, 3), Is.EqualTo(3));
            Assert.That(CargoDiscardMath.Clamp(10, 3), Is.EqualTo(3));
            Assert.That(CargoDiscardMath.Clamp(1, 3), Is.EqualTo(1));
            Assert.That(CargoDiscardMath.Clamp(int.MaxValue, 80), Is.EqualTo(80));
            Assert.That(CargoDiscardMath.Clamp(5, 0), Is.Zero);
            Assert.That(CargoDiscardMath.Clamp(-4, 7), Is.Zero);
        }

        [Test]
        public void Needed_IsSmallestWholeCount()
        {
            Assert.That(CargoDiscardMath.Needed(9.5f, 2f, 80).Count, Is.EqualTo(5));
            Assert.That(CargoDiscardMath.Needed(9.5f, 1.5f, 80).Count, Is.EqualTo(7));
            Assert.That(CargoDiscardMath.Needed(9.5f, 0.8f, 80).Count, Is.EqualTo(12));
            // 경계: 나누어떨어지면 한 개 더 나오지 않는다.
            Assert.That(CargoDiscardMath.Needed(3f, 1.5f, 80).Count, Is.EqualTo(2));
            Assert.That(CargoDiscardMath.Needed(4f, 2f, 80).Count, Is.EqualTo(2));
            Assert.That(CargoDiscardMath.Needed(2.4f, 0.8f, 80).Count, Is.EqualTo(3));
            Assert.That(CargoDiscardMath.Needed(0.1f, 2f, 80).Count, Is.EqualTo(1));
        }

        [Test]
        public void Needed_NotEnoughOwned_DumpsAllAndReportsRemaining()
        {
            var need = CargoDiscardMath.Needed(9.5f, 2f, 3);

            Assert.That(need.Count, Is.EqualTo(3));
            Assert.That(need.Covers, Is.False);
            Assert.That(need.Remaining, Is.EqualTo(3.5f));
            Assert.That(CargoDiscardText.NeededLabel(need), Is.EqualTo("전부 버려도 3.5 부족"));
        }

        [Test]
        public void Needed_NoShortfallOrNothingOwned_IsZero()
        {
            Assert.That(CargoDiscardMath.Needed(0f, 2f, 10).Count, Is.Zero);
            Assert.That(CargoDiscardMath.Needed(-1f, 2f, 10).Count, Is.Zero);
            Assert.That(CargoDiscardMath.Needed(5f, 2f, 0).Count, Is.Zero);
            Assert.That(CargoDiscardText.NeededLabel(CargoDiscardMath.Needed(0f, 2f, 10)), Is.Empty);
        }

        [Test]
        public void Needed_FreesExactlyEnough_OneLessDoesNotFit()
        {
            // 20/20(구리 4 = 6, 철 5 = 10, 리튬 5 = 4) + 보상 구리 3(4.5) → 부족 4.5
            foreach (var mineral in new[] { Copper, Iron })
            {
                var env = CreateFakeEnv(20f, out var presenter, (Copper, 4), (Iron, 5), (Lithium, 5));
                var model = env.Rewards.DescribeCargoShortfall();
                Assert.That(model.Shortfall, Is.EqualTo(4.5f));

                var unit = UnitOf(mineral);
                var need = CargoDiscardMath.Needed(model.Shortfall, unit, env.Inventory.State.GetQuantity(mineral));
                Assert.That(need.Covers, Is.True, mineral);
                Assert.That(need.Count, Is.EqualTo(3), mineral);

                env.Inventory.TryReduceMineral(mineral, need.Count - 1);
                Assert.That(env.Rewards.CanFitPending(), Is.False, mineral + ": 하나 적게 버리면 모자라야 한다");
                env.Inventory.TryReduceMineral(mineral, 1);
                Assert.That(env.Rewards.CanFitPending(), Is.True, mineral + ": 필요 개수면 들어가야 한다");
                presenter.Dispose();
            }

            var lithiumEnv = CreateFakeEnv(20f, out _, (Copper, 4), (Iron, 5), (Lithium, 5));
            var lithiumNeed = CargoDiscardMath.Needed(4.5f, 0.8f, 5);
            Assert.That(lithiumNeed.Covers, Is.False);
            Assert.That(lithiumNeed.Count, Is.EqualTo(5));
            Assert.That(lithiumNeed.Remaining, Is.EqualTo(0.5f));
            Assert.That(lithiumEnv.Rewards.CanFitPending(), Is.False);
        }

        // ---------------- 문구 ----------------

        [Test]
        public void Text_RowDetailFreedAndNeededFormats()
        {
            Assert.That(CargoDiscardText.Quantity(80), Is.EqualTo("×80"));
            Assert.That(CargoDiscardText.Detail(2f, 80), Is.EqualTo("개당 2 · 합계 160"));
            Assert.That(CargoDiscardText.Detail(0.8f, 3), Is.EqualTo("개당 0.8 · 합계 2.4"));
            Assert.That(CargoDiscardText.Freed(5, 2f), Is.EqualTo("−10"));
            Assert.That(CargoDiscardText.Freed(80, 2f), Is.EqualTo("−160"));
            Assert.That(CargoDiscardText.Freed(0, 2f), Is.Empty);
            Assert.That(CargoDiscardText.NeededLabel(CargoDiscardMath.Needed(9.5f, 2f, 80)), Is.EqualTo("−10 (5개)"));
            Assert.That(CargoDiscardText.ButtonLabel(CargoDiscardKind.One), Is.EqualTo("1개"));
            Assert.That(CargoDiscardText.ButtonLabel(CargoDiscardKind.Five), Is.EqualTo("5개"));
            Assert.That(CargoDiscardText.ButtonLabel(CargoDiscardKind.Ten), Is.EqualTo("10개"));
            Assert.That(CargoDiscardText.ButtonLabel(CargoDiscardKind.Needed), Is.EqualTo("필요"));
            Assert.That(CargoDiscardText.ButtonLabel(CargoDiscardKind.All), Is.EqualTo("전부"));
        }

        [Test]
        public void PopupView_RowsShowCountsWeightsAndDimEmptyRow()
        {
            var view = CreateView(out _, out var dump);
            var rows = new[]
            {
                new QuestDumpRow(Copper, "구리", 80, 1.5f),
                new QuestDumpRow(Iron, "철", 3, 2f),
                new QuestDumpRow(Lithium, "리튬", 0, 0.8f)
            };
            view.SetDumpChoice(CargoShortfallModel.From(170f, 170f, 9.5f), new QuestReward(3, 3, 0, 200));
            view.SetDumpRows(rows);
            dump.Show(false);
            dump.Tick(1f);

            Assert.That(dump.State, Is.EqualTo(CargoDiscardPopupView.PopupState.Open));
            Assert.That(dump.TitleString, Is.EqualTo("화물 버리기"));
            Assert.That(dump.HeaderRewardString, Is.EqualTo("구리 ×3 · 철 ×3 · 골드 ×200"));
            Assert.That(dump.LoadString, Is.EqualTo("170 / 170"));
            Assert.That(dump.ShortfallString, Is.EqualTo("부족한 공간 9.5"));

            Assert.That(dump.NameString(Copper), Is.EqualTo("구리"));
            Assert.That(dump.QuantityString(Copper), Is.EqualTo("×80"));
            Assert.That(dump.DetailString(Copper), Is.EqualTo("개당 1.5 · 합계 120"));
            Assert.That(dump.FreedString(Copper, CargoDiscardKind.Ten), Is.EqualTo("−15"));
            Assert.That(dump.FreedString(Copper, CargoDiscardKind.Needed), Is.EqualTo("−10.5 (7개)"));
            Assert.That(dump.FreedString(Copper, CargoDiscardKind.All), Is.EqualTo("−120"));

            // 보유 3에서 5개·10개 → 3개만 비운다.
            Assert.That(dump.FreedString(Iron, CargoDiscardKind.Five), Is.EqualTo("−6"));
            Assert.That(dump.FreedString(Iron, CargoDiscardKind.Needed), Is.EqualTo("전부 버려도 3.5 부족"));
            Assert.That(dump.GetButton(Iron, CargoDiscardKind.Five).interactable, Is.True, "보유가 적어도 누를 수 있다");

            Assert.That(dump.RowAlpha(Lithium), Is.LessThan(0.5f));
            Assert.That(dump.QuantityString(Lithium), Is.EqualTo("×0"));
            foreach (var kind in CargoDiscardText.Kinds)
            {
                Assert.That(dump.GetButton(Lithium, kind).interactable, Is.False, "0개 줄 " + kind);
                Assert.That(dump.GetButton(Copper, kind).interactable, Is.True, "구리 " + kind);
            }

            Assert.That(dump.RequestCount(Copper, CargoDiscardKind.Needed), Is.EqualTo(7));
            Assert.That(dump.RequestCount(Iron, CargoDiscardKind.Needed), Is.EqualTo(3));
            Assert.That(dump.RequestCount(Copper, CargoDiscardKind.All), Is.EqualTo(int.MaxValue));
        }

        [Test]
        public void PopupView_LargeQuantityFitsRow()
        {
            var view = CreateView(out _, out var dump);
            view.SetDumpChoice(CargoShortfallModel.From(170f, 170f, 9.5f), new QuestReward(0, 2, 0, 0));
            view.SetDumpRows(new[] { new QuestDumpRow(Iron, "철", 999, 2f) });
            dump.Show(false);
            dump.Tick(1f);

            Assert.That(dump.QuantityString(Iron), Is.EqualTo("×999"));
            Assert.That(dump.DetailString(Iron), Is.EqualTo("개당 2 · 합계 1998"));
        }

        // ---------------- Presenter 흐름 ----------------

        [Test]
        public void Presenter_FixedButtons_DumpMinOfRequestAndOwned_NoGold()
        {
            var env = CreateFakeEnv(4f, out var presenter, (Copper, 2));
            presenter.ChooseDumpInventory();
            var gold = env.State.Player.Gold;

            presenter.DumpMineral(Copper, 5);
            Assert.That(env.Inventory.State.GetQuantity(Copper), Is.Zero, "보유 2에서 5개 → 2개만");
            presenter.DumpMineral(Copper, 10);
            presenter.DumpMineral(Copper, 1);
            Assert.That(env.Inventory.State.GetQuantity(Copper), Is.Zero);
            Assert.That(env.State.Player.Gold, Is.EqualTo(gold), "버린 자원은 판매 금액을 받지 않는다");
            Assert.That(presenter.IsInputLocked, Is.False);
        }

        [Test]
        public void Presenter_AllButton_DumpsExactlyOwned()
        {
            var env = CreateFakeEnv(8f, out var presenter, (Iron, 3), (Lithium, 2));
            presenter.ChooseDumpInventory();

            presenter.DumpMineral(Lithium, int.MaxValue);

            Assert.That(env.Inventory.State.GetQuantity(Lithium), Is.Zero);
            Assert.That(env.Inventory.State.GetQuantity(Iron), Is.EqualTo(3));
        }

        [Test]
        public void Presenter_ReachingZero_GrantsOnce_RepeatedClicksIgnored()
        {
            // 8/8(철 4) + 구리 3(4.5) → 부족 4.5 → 철 필요 3개.
            var env = CreateFakeEnv(8f, out var presenter, (Iron, 4));
            presenter.ChooseDumpInventory();

            presenter.DumpMineral(Iron, 3);
            presenter.DumpMineral(Iron, 3);
            presenter.DumpMineral(Iron, 1);

            Assert.That(env.Inventory.State.GetQuantity(Iron), Is.EqualTo(1));
            Assert.That(env.Inventory.State.GetQuantity(Copper), Is.EqualTo(3), "보상은 한 번만");
            Assert.That(env.State.Progress.QuestRewardSettledCount, Is.EqualTo(1));
            Assert.That(env.Rewards.HasPending, Is.False);
            Assert.That(presenter.IsDumpOpen, Is.False);
            Assert.That(presenter.IsInputLocked, Is.False);
        }

        // ---------------- View 흐름 ----------------

        [Test]
        public void PageFlip_CapacityToDumpAndBack_NoFold_NeverBothVisible()
        {
            var env = CreateViewEnv(4f, 1, out var view, out var presenter, out var capacity, out var dump, out _, (Copper, 2));
            capacity.Tick(1f);
            Assert.That(capacity.State, Is.EqualTo(CargoFullPopupView.PopupState.Open));

            view.OnCapacityDumpClicked();
            Assert.That(capacity.IsPaging, Is.True, "버릴 자원 선택은 페이지 넘김");
            var elapsed = RunUntil(capacity, dump, () => dump.State == CargoDiscardPopupView.PopupState.Open);
            Assert.That(elapsed, Is.LessThanOrEqualTo(0.3f), "약 0.25초");
            Assert.That(presenter.IsDumpOpen, Is.True);
            Assert.That(dump.QuantityString(Copper), Is.EqualTo("×2"));

            // 구리 1개 버림 → 아직 부족 → 갱신된 값으로 돌아간다.
            view.OnDumpCopperOneClicked();
            dump.Tick(1f);
            Assert.That(env.Inventory.State.GetQuantity(Copper), Is.EqualTo(1));
            Assert.That(dump.LoadString, Is.EqualTo("1.5 / 4"));

            view.OnDumpClosedClicked();
            Assert.That(dump.IsPaging, Is.True, "돌아가기도 페이지 넘김");
            RunUntil(capacity, dump, () => capacity.State == CargoFullPopupView.PopupState.Open);
            Assert.That(presenter.IsCapacityOpen, Is.True);
            Assert.That(capacity.LoadString, Is.EqualTo("1.5 / 4"));
            Assert.That(capacity.ShortfallString, Is.EqualTo("부족한 공간 2"));
            Assert.That(dump.gameObject.activeSelf, Is.False);
            Assert.That(presenter.IsInputLocked, Is.False);
        }

        [Test]
        public void Complete_ShowsClearedLocksButtons_ThenFoldsAndClaimFollows()
        {
            // 두 번째 보상이 이어진다. 8/8(철 4) + 구리 3 → 철 필요 3개.
            var env = CreateViewEnv(8f, 2, out var view, out var presenter, out var capacity, out var dump, out var claim, (Iron, 4));
            capacity.Tick(1f);
            view.OnCapacityDumpClicked();
            RunUntil(capacity, dump, () => dump.State == CargoDiscardPopupView.PopupState.Open);

            Assert.That(dump.RequestCount(Iron, CargoDiscardKind.Needed), Is.EqualTo(3));
            view.OnDumpIronNeededClicked();
            view.OnDumpIronNeededClicked();
            view.OnDumpIronOneClicked();
            view.OnDumpIronAllClicked();

            Assert.That(env.Inventory.State.GetQuantity(Iron), Is.EqualTo(1), "필요 개수만 버리고 연타는 무시");
            Assert.That(env.Inventory.State.GetQuantity(Copper), Is.EqualTo(3), "보상은 정확히 한 번");
            Assert.That(env.State.Progress.QuestRewardSettledCount, Is.EqualTo(1));
            Assert.That(dump.IsClosing, Is.True);
            Assert.That(dump.ActiveCloseMode, Is.EqualTo(CargoDiscardPopupView.CloseMode.Complete));
            Assert.That(dump.GetButton(Iron, CargoDiscardKind.One).interactable, Is.False);
            Assert.That(claim.activeSelf, Is.False, "클리어 창은 닫힘 뒤에 뜬다");

            var shownCleared = false;
            var elapsed = 0f;
            while (dump.gameObject.activeSelf && elapsed < 2f)
            {
                dump.Tick(0.02f);
                elapsed += 0.02f;
                shownCleared |= dump.ShortfallString == CargoDiscardText.Cleared;
                if (dump.gameObject.activeSelf)
                {
                    Assert.That(claim.activeSelf, Is.False);
                }
            }

            Assert.That(shownCleared, Is.True);
            Assert.That(elapsed, Is.GreaterThanOrEqualTo(dump.Timing.completeHold));
            Assert.That(dump.State, Is.EqualTo(CargoDiscardPopupView.PopupState.Hidden));
            Assert.That(claim.activeSelf, Is.True);
            Assert.That(presenter.IsClaimOpen, Is.True);
            Assert.That(presenter.IsInputLocked, Is.False);
        }

        [Test]
        public void BackKey_WhileClosing_IsIgnored()
        {
            CreateViewEnv(4f, 1, out var view, out var presenter, out var capacity, out var dump, out _, (Copper, 2));
            capacity.Tick(1f);
            view.OnCapacityDumpClicked();
            RunUntil(capacity, dump, () => dump.State == CargoDiscardPopupView.PopupState.Open);

            Assert.That(view.TryCloseTopPopup(), Is.True);
            Assert.That(view.TryCloseTopPopup(), Is.True);
            RunUntil(capacity, dump, () => capacity.State == CargoFullPopupView.PopupState.Open);

            Assert.That(presenter.IsCapacityOpen, Is.True);
            Assert.That(capacity.IsChoiceReady, Is.True);
        }

        // ---------------- 연출 시간표 ----------------

        [Test]
        public void Timeline_PageWithinLimit_EffectsFireOnce()
        {
            var timing = new CargoDiscardTiming().Sanitized();
            Assert.That(timing.PageDuration, Is.EqualTo(0.25f).Within(0.02f));
            Assert.That(timing.gaugeTweenDuration, Is.EqualTo(0.2f).Within(0.001f));
            Assert.That(timing.completeHold, Is.EqualTo(0.4f).Within(0.001f));
            Assert.That(new CargoDiscardTiming { pageInDuration = 3f, pageOutDuration = 2f }.Sanitized().PageDuration,
                Is.LessThanOrEqualTo(CargoDiscardTiming.MaxPageDuration + 0.0001f));

            Assert.That(CountRises(t => CargoDiscardTimeline.QuantityBlink(t, timing)), Is.EqualTo(1));
            Assert.That(CountRises(t => CargoDiscardTimeline.ClearedFlash(t, timing)), Is.EqualTo(1));
            Assert.That(CargoDiscardTimeline.GaugeTween(timing.gaugeTweenDuration, timing), Is.EqualTo(1f));
            Assert.That(CargoDiscardTimeline.EvaluatePageIn(timing.pageInDuration, timing).Reveal, Is.EqualTo(1f));
            Assert.That(CargoDiscardTimeline.EvaluatePageOut(timing.pageOutDuration, timing).Content, Is.Zero);
        }

        // ---------------- 씬·폰트 ----------------

        [Test]
        public void IntegrationScene_DumpPanelKeepsNamesAndWiring()
        {
            WithScene(scene =>
            {
                var root = Find(scene, "DemoObjectiveRoot");
                var panel = root.transform.Find("QuestRewardDumpPanel");
                Assert.That(panel, Is.Not.Null);
                Assert.That(panel.gameObject.activeSelf, Is.False);
                Assert.That(panel.GetComponent<Canvas>(), Is.Null, "정렬 순서는 그대로 둔다");
                var popup = panel.GetComponent<CargoDiscardPopupView>();
                Assert.That(popup, Is.Not.Null);
                var view = root.GetComponent<DemoObjectiveView>();
                Assert.That(new SerializedObject(view).FindProperty("dumpPopup").objectReferenceValue, Is.EqualTo(popup));
                Assert.That(view.HasOverflowReferences(), Is.True);

                var names = new[] { ("DumpCopper", "Copper"), ("DumpIron", "Iron"), ("DumpLithium", "Lithium") };
                foreach (var (rowName, mineral) in names)
                {
                    var row = panel.Find(rowName);
                    Assert.That(row, Is.Not.Null, rowName);
                    foreach (var kind in CargoDiscardText.Kinds)
                    {
                        var button = row.Find(CargoDiscardPopupView.ButtonName(kind)).GetComponent<Button>();
                        var suffix = kind == CargoDiscardKind.Needed ? "Needed" : kind.ToString();
                        Assert.That(button.onClick.GetPersistentMethodName(0), Is.EqualTo("OnDump" + mineral + suffix + "Clicked"));
                        Assert.That(button.GetComponentInChildren<TMP_Text>(true).text, Is.EqualTo(CargoDiscardText.ButtonLabel(kind)));
                    }
                }

                var back = panel.Find("DumpCloseButton").GetComponent<Button>();
                Assert.That(back.onClick.GetPersistentMethodName(0), Is.EqualTo(nameof(DemoObjectiveView.OnDumpClosedClicked)));
                Assert.That(back.GetComponentInChildren<TMP_Text>(true).text, Is.EqualTo(CargoDiscardText.BackTitle));
            });
        }

        [Test]
        public void Font_HasEveryPopupCharacter()
        {
            WithScene(scene =>
            {
                var root = Find(scene, "DemoObjectiveRoot");
                var fonts = new[]
                {
                    root.transform.Find("QuestRewardDumpPanel/DumpTitle").GetComponent<TMP_Text>().font,
                    root.transform.Find("QuestRewardCapacityPanel/CapacityTitle").GetComponent<TMP_Text>().font
                };

                foreach (var font in fonts)
                {
                    Assert.That(font, Is.Not.Null);
                    var missing = string.Empty;
                    foreach (var c in CargoDiscardText.FontCharacters)
                    {
                        if (!char.IsWhiteSpace(c) && !HasStoredGlyph(font, c) && missing.IndexOf(c) < 0)
                        {
                            missing += c;
                        }
                    }

                    Assert.That(missing, Is.Empty, AssetDatabase.GetAssetPath(font) + "에 저장된 글자가 없다");
                    Assert.That(font.HasCharacters(CargoFullPopupText.Footer), Is.True);
                }
            });
        }

        // 메모리 조회표가 아니라 저장된 글자 표와 아틀라스로 확인한다(동적 추가만으로는 다시 열 때 빈칸이 된다).
        private static bool HasStoredGlyph(TMP_FontAsset font, char c)
        {
            var fonts = new List<TMP_FontAsset> { font };
            if (font.fallbackFontAssetTable != null)
            {
                fonts.AddRange(font.fallbackFontAssetTable);
            }

            foreach (var candidate in fonts)
            {
                if (candidate == null || candidate.characterTable == null)
                {
                    continue;
                }

                foreach (var character in candidate.characterTable)
                {
                    if (character.unicode == c && character.glyph != null
                        && character.glyph.atlasIndex < candidate.atlasTextures.Length
                        && candidate.atlasTextures[character.glyph.atlasIndex] != null)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        // ---------------- 도우미 ----------------

        private static int CountRises(System.Func<float, float> curve)
        {
            var rises = 0;
            var previous = 0f;
            for (var t = 0f; t <= 2f; t += 0.002f)
            {
                var value = curve(t);
                if (previous <= 0.001f && value > 0.001f)
                {
                    rises++;
                }

                previous = value;
            }

            Assert.That(curve(5f), Is.Zero, "끝난 뒤에는 다시 켜지지 않는다");
            return rises;
        }

        /// <summary>두 창을 같이 진행하며, 접기 연출이 없고 두 창이 동시에 켜지지 않는지 확인한다.</summary>
        private static float RunUntil(CargoFullPopupView capacity, CargoDiscardPopupView dump, System.Func<bool> done)
        {
            var elapsed = 0f;
            while (!done() && elapsed < 2f)
            {
                capacity.Tick(0.01f);
                dump.Tick(0.01f);
                elapsed += 0.01f;
                Assert.That(capacity.gameObject.activeSelf && dump.gameObject.activeSelf, Is.False, "두 창이 같이 보이면 안 된다");
                if (capacity.gameObject.activeSelf)
                {
                    Assert.That(capacity.ShellReveal, Is.EqualTo(1f).Within(0.001f), "화물 부족 창이 접히면 안 된다");
                }

                if (dump.gameObject.activeSelf)
                {
                    Assert.That(dump.ShellReveal, Is.EqualTo(1f).Within(0.001f), "화물 버리기 창이 접히면 안 된다");
                }
            }

            Assert.That(done(), Is.True, "시간 안에 끝나야 한다");
            return elapsed;
        }

        private DemoObjectiveView CreateView(out CargoFullPopupView capacity, out CargoDiscardPopupView dump)
        {
            return CreateView(out capacity, out dump, out _);
        }

        private DemoObjectiveView CreateView(out CargoFullPopupView capacity, out CargoDiscardPopupView dump, out GameObject claim)
        {
            var canvasObject = new GameObject("CargoCanvas", typeof(RectTransform), typeof(Canvas));
            spawned.Add(canvasObject);
            var view = canvasObject.AddComponent<DemoObjectiveView>();

            var capacityPanel = new GameObject("QuestRewardCapacityPanel", typeof(RectTransform), typeof(Image));
            capacityPanel.transform.SetParent(canvasObject.transform, false);
            var title = NewText(capacityPanel.transform, "CapacityTitle");
            var body = NewText(capacityPanel.transform, "CapacityBody");
            var dumpChoice = NewButton(capacityPanel.transform, "CapacityDumpButton");
            var forfeit = NewButton(capacityPanel.transform, "CapacityForfeitButton");
            capacity = capacityPanel.AddComponent<CargoFullPopupView>();
            capacity.Configure(title, body, dumpChoice, forfeit);
            capacity.ManualTick = true;
            capacityPanel.SetActive(false);

            var dumpPanel = new GameObject("QuestRewardDumpPanel", typeof(RectTransform), typeof(Image));
            dumpPanel.transform.SetParent(canvasObject.transform, false);
            NewText(dumpPanel.transform, "DumpTitle");
            var summary = NewText(dumpPanel.transform, "DumpSummary");
            foreach (var rowName in new[] { "DumpCopper", "DumpIron", "DumpLithium" })
            {
                var row = new GameObject(rowName, typeof(RectTransform));
                row.transform.SetParent(dumpPanel.transform, false);
                NewText(row.transform, "Label");
                NewButton(row.transform, "DumpOne");
                NewButton(row.transform, "DumpAll");
            }

            NewButton(dumpPanel.transform, "DumpCloseButton");
            dump = dumpPanel.AddComponent<CargoDiscardPopupView>();
            dump.ManualTick = true;
            dumpPanel.SetActive(false);

            claim = new GameObject("QuestClearRewardPanel", typeof(RectTransform));
            claim.transform.SetParent(canvasObject.transform, false);
            claim.SetActive(false);

            var serialized = new SerializedObject(view);
            serialized.FindProperty("capacityRoot").objectReferenceValue = capacityPanel;
            serialized.FindProperty("capacityTitleText").objectReferenceValue = title;
            serialized.FindProperty("capacityBodyText").objectReferenceValue = body;
            serialized.FindProperty("capacityPopup").objectReferenceValue = capacity;
            serialized.FindProperty("dumpRoot").objectReferenceValue = dumpPanel;
            serialized.FindProperty("dumpSummaryText").objectReferenceValue = summary;
            serialized.FindProperty("dumpPopup").objectReferenceValue = dump;
            serialized.FindProperty("claimRoot").objectReferenceValue = claim;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        /// <summary>실제 DemoObjectiveView + 두 창 + Presenter. 첫 보상이 화물 부족으로 막힌 상태에서 시작한다.</summary>
        private Env CreateViewEnv(
            float capacity,
            int completed,
            out DemoObjectiveView view,
            out DemoObjectivePresenter presenter,
            out CargoFullPopupView capacityPopup,
            out CargoDiscardPopupView dumpPopup,
            out GameObject claim,
            params (string id, int count)[] cargo)
        {
            view = CreateView(out capacityPopup, out dumpPopup, out claim);
            var env = CreateState(capacity, completed, cargo);
            presenter = new DemoObjectivePresenter(view);
            view.CapacityDumpRequested += presenter.ChooseDumpInventory;
            view.CapacityForfeitRequested += presenter.ChooseForfeitReward;
            view.DumpClosedRequested += presenter.CloseDumpPanel;
            view.DumpRequested += presenter.DumpMineral;
            view.ClaimConfirmRequested += presenter.ConfirmClaim;
            Bind(env, presenter);
            Assert.That(capacityPopup.IsVisible, Is.True, "준비: 보상이 화물 부족으로 막혀야 한다");
            return env;
        }

        private static Env CreateFakeEnv(float capacity, out DemoObjectivePresenter presenter, params (string id, int count)[] cargo)
        {
            var env = CreateState(capacity, 1, cargo);
            presenter = new DemoObjectivePresenter(new FakeView());
            Bind(env, presenter);
            Assert.That(presenter.IsCapacityOpen, Is.True, "준비: 첫 보상이 화물 부족으로 막혀야 한다");
            return env;
        }

        private static Env CreateState(float capacity, int completed, (string id, int count)[] cargo)
        {
            var state = GameState.CreateNew();
            var inventory = new InventoryService(CreateCatalog(), capacity, state);
            var rewards = new QuestRewardService();
            rewards.Bind(inventory, state);
            foreach (var (id, count) in cargo)
            {
                inventory.TryAddMineral(id, count);
            }

            state.SetDemoProgress(DemoObjectiveIds.Ordered[completed], completed, false);
            return new Env(state, inventory, rewards);
        }

        private static void Bind(Env env, DemoObjectivePresenter presenter)
        {
            var director = new DemoObjectiveDirector();
            director.BindGameState(env.State);
            director.RestoreFromProgress(env.State.Progress);
            presenter.Bind(director, env.Rewards);
            presenter.ConfirmClaim();
        }

        private static float UnitOf(string mineralId)
        {
            return mineralId == Copper ? 1.5f : mineralId == Iron ? 2f : 0.8f;
        }

        private static InMemoryMineralCatalog CreateCatalog()
        {
            var catalog = new InMemoryMineralCatalog();
            catalog.Register(Copper, 1.5f, 10, "구리");
            catalog.Register(Iron, 2f, 15, "철");
            catalog.Register(Lithium, 0.8f, 40, "리튬");
            return catalog;
        }

        private static TMP_Text NewText(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.AddComponent<TextMeshProUGUI>();
        }

        private static Button NewButton(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            NewText(go.transform, "Label");
            return go.GetComponent<Button>();
        }

        private static void WithScene(System.Action<Scene> body)
        {
            var path = PromptB153CargoDiscardPopupBuilder.IntegrationScenePath;
            var scene = SceneManager.GetSceneByPath(path);
            var closeAfter = !scene.IsValid() || !scene.isLoaded;
            if (closeAfter)
            {
                scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            }

            try
            {
                body(scene);
            }
            finally
            {
                if (closeAfter && scene.IsValid() && scene.isLoaded)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        private static GameObject Find(Scene scene, string objectName)
        {
            var roots = scene.GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                var transforms = roots[i].GetComponentsInChildren<Transform>(true);
                for (var j = 0; j < transforms.Length; j++)
                {
                    if (transforms[j].name == objectName)
                    {
                        return transforms[j].gameObject;
                    }
                }
            }

            return null;
        }

        private readonly struct Env
        {
            public GameState State { get; }
            public InventoryService Inventory { get; }
            public QuestRewardService Rewards { get; }

            public Env(GameState state, InventoryService inventory, QuestRewardService rewards)
            {
                State = state;
                Inventory = inventory;
                Rewards = rewards;
            }
        }

        private sealed class FakeView : IDemoObjectiveView
        {
            public void SetObjective(DemoObjectiveReadModel model) { }
            public void SetGuidanceVisible(bool visible) { }
            public void SetGuidanceText(string title, string body) { }
            public void SetInputLocked(bool locked) { }
            public void SetHazardYield(bool yieldToHazard) { }
            public void SetDemoCompleteVisible(bool visible, string summary) { }
            public void SetDetailsVisible(bool visible) { }
            public void SetDetailsText(string title, string body, string nextAction) { }
            public void SetDetailsReward(string rewardText) { }
            public void SetDetailsStatus(string statusText) { }
            public void SetDetailsIndex(string indexText) { }
            public void SetDetailsNavInteractable(bool previousEnabled, bool nextEnabled) { }
            public void SetCapacityChoiceVisible(bool visible) { }
            public void SetCapacityChoiceText(string title, string body) { }
            public void SetCapacityChoice(CargoShortfallModel shortfall, QuestReward reward) { }
            public void SetDumpPanelVisible(bool visible) { }
            public void SetDumpSummary(string summary) { }
            public void SetDumpChoice(CargoShortfallModel shortfall, QuestReward reward) { }
            public void SetDumpRows(IReadOnlyList<QuestDumpRow> rows) { }
            public void SetClaimVisible(bool visible) { }
            public void SetClaimText(string title, string questTitle, string rewardText, string hint) { }
        }
    }
}

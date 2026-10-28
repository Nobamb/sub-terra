using System;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using SubTerra.App.Core;
using SubTerra.App.Editor.DataValidation;
using SubTerra.App.Inventory;
using SubTerra.App.Progression;
using SubTerra.App.Save;
using SubTerra.App.State;
using SubTerra.App.Tutorial;
using SubTerra.App.UI.MainMenu;
using SubTerra.App.UI.SurfaceBase;
using SubTerra.Shared;
using UnityEditor;
using UnityEngine;

namespace SubTerra.App.Tests.UI.MainMenu
{
    /// <summary>B-150 메인화면 개편: 부팅 타임라인, 자석 프레임, 슬롯 메타, 삭제 확인, 이어하기 비활성.</summary>
    public sealed class PromptB150MainMenuTests
    {
        private string testRoot;
        private SavePathPolicy paths;
        private SaveService save;
        private LoadService load;

        [SetUp]
        public void SetUp()
        {
            // 실제 사용자 세이브를 건드리지 않도록 임시 폴더만 쓴다.
            testRoot = Path.Combine(Path.GetTempPath(), "subterra-b150-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(testRoot);
            paths = new SavePathPolicy(testRoot);
            var fileSystem = new PhysicalSaveFileSystem();
            var mapper = new SaveDataMapper(new SystemSaveClock());
            var json = new SaveJsonCodec(new SaveMigrationService());
            save = new SaveService(fileSystem, paths, mapper, json);
            load = new LoadService(fileSystem, paths, mapper, json);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(testRoot)) Directory.Delete(testRoot, true);
        }

        [Test]
        public void Boot_RunsInOrder_WithinTwoSeconds()
        {
            var timeline = new MainMenuBootTimeline();
            Assert.That(timeline.TotalDuration, Is.LessThanOrEqualTo(MainMenuBootTimeline.MaxDuration));

            float lineAt = -1, openAt = -1, headerAt = -1, connectedAt = -1, logoAt = -1, subtitleAt = -1, continueAt = -1;
            var cardAt = new[] { -1f, -1f, -1f };
            var secondaryAt = new[] { -1f, -1f, -1f };
            const float step = 0.005f;
            for (var t = 0f; t <= timeline.TotalDuration + step; t += step)
            {
                var f = timeline.Sample();
                var now = timeline.Elapsed;
                if (lineAt < 0 && f.LineWidth > 0f) lineAt = now;
                if (openAt < 0 && f.Open > 0f) openAt = now;
                if (headerAt < 0 && f.HeaderTyping > 0f) headerAt = now;
                if (connectedAt < 0 && f.HeaderConnected) connectedAt = now;
                if (logoAt < 0 && f.LogoAlpha > 0f) logoAt = now;
                if (subtitleAt < 0 && f.SubtitleTyping > 0f) subtitleAt = now;
                for (var i = 0; i < 3; i++)
                {
                    if (cardAt[i] < 0 && timeline.CardAlpha(i) > 0f) cardAt[i] = now;
                    if (secondaryAt[i] < 0 && timeline.SecondaryAlpha(i) > 0f) secondaryAt[i] = now;
                }
                if (continueAt < 0 && timeline.ContinueAlpha() > 0f) continueAt = now;
                timeline.Advance(step);
            }

            Assert.That(lineAt, Is.LessThan(openAt), "가로선 → 펼침");
            Assert.That(openAt, Is.LessThan(headerAt), "펼침 → 부팅 문구");
            Assert.That(headerAt, Is.LessThan(connectedAt), "타이핑 뒤 '신호 연결됨'");
            Assert.That(headerAt, Is.LessThan(logoAt), "부팅 문구 → 로고");
            Assert.That(logoAt, Is.LessThan(subtitleAt), "로고 → 부제");
            Assert.That(subtitleAt, Is.LessThan(cardAt[0]), "부제 → 카드");
            Assert.That(cardAt[1] - cardAt[0], Is.EqualTo(0.16f).Within(0.011f), "카드 0.16초 간격(B-170)");
            Assert.That(cardAt[2] - cardAt[1], Is.EqualTo(0.16f).Within(0.011f));
            Assert.That(cardAt[0], Is.LessThan(continueAt), "카드 → 이어하기");
            Assert.That(continueAt, Is.LessThan(secondaryAt[0]), "이어하기 → 새 게임·설정·종료");
            Assert.That(secondaryAt[0], Is.LessThan(secondaryAt[2]));
            Assert.That(timeline.IsComplete, Is.True);
        }

        [Test]
        public void Boot_Skip_JumpsToEndState()
        {
            var timeline = new MainMenuBootTimeline();
            timeline.Advance(0.05f);
            Assert.That(timeline.CardAlpha(0), Is.Zero);
            timeline.Skip();
            var f = timeline.Sample();
            Assert.That(timeline.IsComplete, Is.True);
            Assert.That(f.Open, Is.EqualTo(1f));
            Assert.That(f.LineAlpha, Is.Zero);
            Assert.That(f.HeaderConnected, Is.True);
            Assert.That(f.LogoAlpha, Is.EqualTo(1f));
            Assert.That(f.LogoGlitch, Is.Zero);
            Assert.That(f.SubtitleTyping, Is.EqualTo(1f));
            for (var i = 0; i < 3; i++)
            {
                Assert.That(timeline.CardAlpha(i), Is.EqualTo(1f));
                Assert.That(timeline.CardOffset(i), Is.Zero);
                Assert.That(timeline.SecondaryAlpha(i), Is.EqualTo(1f));
            }
            Assert.That(timeline.ContinueAlpha(), Is.EqualTo(1f));
        }

        [Test]
        public void Frame_AttachAndDetach_UseConfiguredDurations()
        {
            var frame = new SlotFrameMagnetTimeline();
            frame.Select(0);
            var start = frame.Sample(0);
            Assert.That(start.Alpha, Is.Zero);
            Assert.That(start.Distance, Is.InRange(24f, 40f), "카드 바깥 24~40px에서 출발");

            var minDistance = float.MaxValue;
            var flashed = false;
            for (var i = 0; i < 25; i++)
            {
                frame.Tick(0.01f);
                var s = frame.Sample(0);
                minDistance = Mathf.Min(minDistance, s.Distance);
                flashed |= s.Flash > 0.5f;
            }

            Assert.That(frame.PhaseOf(0), Is.EqualTo(SlotFramePhase.Attached), "약 0.25초에 맞물림");
            Assert.That(frame.Sample(0).Alpha, Is.EqualTo(1f));
            Assert.That(frame.Sample(0).Distance, Is.EqualTo(0f).Within(0.01f));
            Assert.That(minDistance, Is.LessThan(0f), "끝에서 살짝 튕기듯(overshoot)");
            Assert.That(flashed, Is.True, "맞물리는 순간 이음새가 번쩍인다");

            frame.Select(1);
            Assert.That(frame.PhaseOf(0), Is.EqualTo(SlotFramePhase.Detaching));
            Assert.That(frame.PhaseOf(1), Is.EqualTo(SlotFramePhase.Attaching), "떨어지는 동시에 새 카드에 붙는다");
            frame.Tick(0.17f);
            Assert.That(frame.PhaseOf(0), Is.EqualTo(SlotFramePhase.Detaching));
            frame.Tick(0.02f);
            Assert.That(frame.PhaseOf(0), Is.EqualTo(SlotFramePhase.Off), "약 0.18초에 떨어짐");
            Assert.That(frame.Sample(0).Alpha, Is.Zero);
        }

        [Test]
        public void Frame_RapidSelection_KeepsFrameOnSingleCard()
        {
            var frame = new SlotFrameMagnetTimeline();
            var random = new System.Random(150);
            var last = 0;
            frame.Select(0);
            for (var i = 0; i < 400; i++)
            {
                if (random.Next(3) == 0)
                {
                    last = random.Next(3);
                    frame.Select(last);
                }

                frame.Tick((float)random.NextDouble() * 0.03f);
                Assert.That(frame.HolderCount, Is.LessThanOrEqualTo(1), "step " + i);
                Assert.That(frame.Holder, Is.EqualTo(last));
            }

            frame.Tick(1f);
            frame.Tick(1f);
            for (var i = 0; i < 3; i++)
                Assert.That(frame.Sample(i).Alpha > 0f, Is.EqualTo(i == last), "card " + i);
        }

        [Test]
        public void Frame_Hover_AttachesWithoutChangingSelection()
        {
            var frame = new SlotFrameMagnetTimeline();
            frame.Select(0);
            frame.Tick(1f);

            frame.SetHover(2);
            Assert.That(frame.Holder, Is.EqualTo(0), "호버는 선택을 바꾸지 않는다");
            Assert.That(frame.Hover, Is.EqualTo(2));
            Assert.That(frame.PhaseOf(2), Is.EqualTo(SlotFramePhase.Attaching), "호버 카드에도 프레임이 붙는다");
            Assert.That(frame.PhaseOf(0), Is.EqualTo(SlotFramePhase.Attached), "선택 카드 프레임은 유지");
            Assert.That(frame.HolderCount, Is.EqualTo(2));

            frame.Tick(1f);
            frame.SetHover(1);
            Assert.That(frame.PhaseOf(2), Is.EqualTo(SlotFramePhase.Detaching), "호버가 옮겨가면 이전 카드는 떨어진다");
            Assert.That(frame.PhaseOf(1), Is.EqualTo(SlotFramePhase.Attaching));
            Assert.That(frame.PhaseOf(0), Is.EqualTo(SlotFramePhase.Attached));

            frame.SetHover(-1);
            frame.Tick(1f);
            Assert.That(frame.Sample(1).Alpha, Is.Zero, "마우스가 벗어나면 프레임이 떨어진다");
            Assert.That(frame.Sample(0).Alpha, Is.EqualTo(1f), "선택 카드는 그대로");
        }

        [Test]
        public void Frame_HoverOnSelectedCard_KeepsItAttached()
        {
            var frame = new SlotFrameMagnetTimeline();
            frame.Select(1);
            frame.Tick(1f);
            frame.SetHover(1);
            Assert.That(frame.PhaseOf(1), Is.EqualTo(SlotFramePhase.Attached), "이미 붙은 카드에 호버해도 다시 붙지 않는다");
            frame.SetHover(-1);
            Assert.That(frame.PhaseOf(1), Is.EqualTo(SlotFramePhase.Attached), "선택된 카드는 호버가 풀려도 유지");

            // 호버 중인 카드를 클릭해 선택해도 프레임이 다시 붙지 않는다.
            frame.SetHover(2);
            frame.Tick(1f);
            frame.Select(2);
            Assert.That(frame.PhaseOf(2), Is.EqualTo(SlotFramePhase.Attached));
            Assert.That(frame.PhaseOf(1), Is.EqualTo(SlotFramePhase.Detaching));
            frame.SetHover(-1);
            Assert.That(frame.PhaseOf(2), Is.EqualTo(SlotFramePhase.Attached), "선택 카드 프레임 유지");
        }

        [Test]
        public void Frame_RapidHoverAndSelect_NeverExceedsTwoCards()
        {
            var frame = new SlotFrameMagnetTimeline();
            var random = new System.Random(151);
            for (var i = 0; i < 400; i++)
            {
                var roll = random.Next(4);
                if (roll == 0) frame.Select(random.Next(3));
                else if (roll == 1) frame.SetHover(random.Next(-1, 3));
                frame.Tick((float)random.NextDouble() * 0.03f);
                Assert.That(frame.HolderCount, Is.LessThanOrEqualTo(2), "step " + i);
            }
        }

        [Test]
        public void View_HoverShowsFrameOnly_ClickSelects()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PromptB150MainMenuBuilder.PrefabPath);
            var instance = UnityEngine.Object.Instantiate(prefab);
            var view = instance.GetComponent<MainMenuView>();
            var onEnable = typeof(MainMenuView).GetMethod("OnEnable", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var onDisable = typeof(MainMenuView).GetMethod("OnDisable", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            try
            {
                var selected = new System.Collections.Generic.List<int>();
                view.SlotSelected += selected.Add;
                onEnable.Invoke(view, null);
                view.SetSelectedSlot(1, true, "msg");

                var cards = instance.GetComponentsInChildren<SaveSlotCardView>(true);
                cards[2].OnPointerEnter(null);
                Assert.That(selected, Is.Empty, "호버만으로는 슬롯이 선택되지 않는다");
                Assert.That(view.CurrentSelectedSlotId, Is.EqualTo(1));
                Assert.That(view.FrameTimeline.Hover, Is.EqualTo(2), "호버 카드에 프레임");
                Assert.That(view.FrameTimeline.Holder, Is.EqualTo(0));

                cards[2].OnPointerExit(null);
                Assert.That(view.FrameTimeline.Hover, Is.EqualTo(-1));

                cards[2].GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
                Assert.That(selected, Is.EqualTo(new[] { 3 }), "클릭하면 슬롯이 선택된다");
            }
            finally
            {
                onDisable.Invoke(view, null);
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void DeleteButton_Hover_FadesInWhiteTrashIcon()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PromptB150MainMenuBuilder.PrefabPath);
            var instance = UnityEngine.Object.Instantiate(prefab);
            try
            {
                var card = instance.GetComponentsInChildren<SaveSlotCardView>(true)[0];
                var fx = card.transform.Find("DeleteButton").GetComponent<SlotDeleteButtonHover>();
                var white = card.transform.Find("DeleteButton/IconWhite").GetComponent<UnityEngine.UI.Image>();
                var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
                typeof(SlotDeleteButtonHover).GetMethod("OnEnable", flags).Invoke(fx, null);
                Assert.That(white.color.a, Is.Zero, "평소엔 흰색 아이콘이 숨겨져 있다");

                fx.OnPointerEnter(null);
                var update = typeof(SlotDeleteButtonHover).GetMethod("Update", flags);
                for (var i = 0; i < 200 && fx.WhiteAmount < 1f; i++)
                {
                    System.Threading.Thread.Sleep(5);
                    update.Invoke(fx, null);
                }
                Assert.That(fx.WhiteAmount, Is.EqualTo(1f), "호버하면 흰색 아이콘이 완전히 켜진다");
                Assert.That(white.color.a, Is.EqualTo(1f));

                fx.OnPointerExit(null);
                for (var i = 0; i < 200 && fx.WhiteAmount > 0f; i++)
                {
                    System.Threading.Thread.Sleep(5);
                    update.Invoke(fx, null);
                }
                Assert.That(white.color.a, Is.Zero, "벗어나면 다시 숨겨진다");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void Metadata_ReadsMaximumDepthAndObjectives()
        {
            Assert.That(save.Save(1, Context(maxDepth: 30, depth: 12, objectives: 5)).IsSuccess, Is.True);
            var meta = load.GetSlotMetadata(1);
            Assert.That(meta.MaximumDepth, Is.EqualTo(30));
            Assert.That(meta.Depth, Is.EqualTo(12));
            Assert.That(meta.CompletedObjectives, Is.EqualTo(5));
            Assert.That(SaveSlotCardFormatter.MaximumDepth(meta), Is.EqualTo("30m"));
            Assert.That(SaveSlotCardFormatter.Objectives(meta), Is.EqualTo("5/" + DemoObjectiveIds.RequiredCount));
            var label = SaveSlotCardFormatter.BuildLabel(meta, SlotContinueEligibility.Ready);
            Assert.That(label, Does.Contain("탐사 기록 01"));
            Assert.That(label, Does.Contain("최대 깊이 30m"));
            Assert.That(label, Does.Contain("목표 5/" + DemoObjectiveIds.RequiredCount));
            Assert.That(label, Does.Contain("마지막 저장"));
        }

        [TestCase(5)]
        [TestCase(SaveVersions.Current)]
        public void Metadata_OldSaveWithoutValues_ShowsDash(int version)
        {
            Assert.That(save.Save(2, Context(maxDepth: 0, depth: 0, objectives: 0)).IsSuccess, Is.True);
            paths.TryGetPaths(2, out var slot);
            var text = File.ReadAllText(slot.Normal);
            text = Regex.Replace(text, "\"saveVersion\"\\s*:\\s*\\d+", "\"saveVersion\":" + version);
            text = Regex.Replace(text, "\"maximumDepth\"\\s*:\\s*-?\\d+\\s*,\\s*", string.Empty);
            text = Regex.Replace(text, "\"completedObjectives\"\\s*:\\s*-?\\d+\\s*,\\s*", string.Empty);
            Assert.That(text, Does.Not.Contain("maximumDepth"));
            File.WriteAllText(slot.Normal, text);

            var meta = load.GetSlotMetadata(2);
            Assert.That(meta.CanContinue, Is.True, "저장 형식은 바뀌지 않아 옛 세이브도 읽힌다");
            Assert.That(meta.MaximumDepth, Is.EqualTo(-1));
            Assert.That(meta.CompletedObjectives, Is.EqualTo(-1));
            Assert.That(SaveSlotCardFormatter.MaximumDepth(meta), Is.EqualTo("-"));
            Assert.That(SaveSlotCardFormatter.Objectives(meta), Is.EqualTo("-"));
            Assert.That(File.ReadAllText(slot.Normal), Is.EqualTo(text),
                "메타 조회는 v5/현재 버전 파일의 형식을 다시 쓰지 않는다");
        }

        [Test]
        public void Delete_RequiresConfirm_AndRemovesBackup()
        {
            Assert.That(save.Save(1, Context(10, 3, 1)).IsSuccess, Is.True);
            Assert.That(save.Save(1, Context(10, 3, 1)).IsSuccess, Is.True);
            paths.TryGetPaths(1, out var slot);
            Assert.That(File.Exists(slot.Normal) && File.Exists(slot.Backup), Is.True);

            var view = new RecordingView();
            using (var presenter = new MainMenuPresenter(view, load, "test"))
            {
                var deletions = 0;
                presenter.DeleteSlotConfirmed += id =>
                {
                    deletions++;
                    presenter.CompleteDeleteSlot(id, save.DeleteSlot(id));
                };
                presenter.Refresh();

                Assert.That(presenter.RequestDeleteSlot(1), Is.True);
                Assert.That(view.DeleteVisible, Is.True);
                Assert.That(deletions, Is.Zero, "확인 전에는 삭제하지 않는다");
                Assert.That(File.Exists(slot.Normal), Is.True);

                presenter.CancelDeleteSlot();
                Assert.That(view.DeleteVisible, Is.False);
                Assert.That(deletions, Is.Zero);
                Assert.That(File.Exists(slot.Normal) && File.Exists(slot.Backup), Is.True, "취소 시 파일 불변");

                Assert.That(presenter.ConfirmDeleteSlot(), Is.Zero, "열린 확인 창 없이 확인해도 삭제 없음");
                Assert.That(deletions, Is.Zero);

                presenter.RequestDeleteSlot(1);
                Assert.That(presenter.ConfirmDeleteSlot(), Is.EqualTo(1));
                Assert.That(deletions, Is.EqualTo(1));
                Assert.That(File.Exists(slot.Normal), Is.False);
                Assert.That(File.Exists(slot.Backup), Is.False, "백업 파일도 함께 지운다");
                Assert.That(view.Kinds[1], Is.EqualTo(SaveSlotCardKind.Empty), "삭제 뒤 카드가 빈 슬롯으로 갱신");
                Assert.That(view.Continue, Is.True);
                Assert.That(view.PrimaryLabel, Is.EqualTo("새 탐사 시작"));
            }
        }

        [Test]
        public void Delete_EmptySlot_HasNoButtonAndNoConfirm()
        {
            var view = new RecordingView();
            using (var presenter = new MainMenuPresenter(view, load, "test"))
            {
                presenter.Refresh();
                Assert.That(view.Kinds[2], Is.EqualTo(SaveSlotCardKind.Empty));
                Assert.That(presenter.RequestDeleteSlot(2), Is.False);
                Assert.That(view.DeleteVisible, Is.False);
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PromptB150MainMenuBuilder.PrefabPath);
            var instance = UnityEngine.Object.Instantiate(prefab);
            try
            {
                var menu = instance.GetComponent<MainMenuView>();
                menu.SetSlotKind(1, SaveSlotCardKind.Ready);
                menu.SetSlotKind(2, SaveSlotCardKind.Empty);
                menu.SetSlotKind(3, SaveSlotCardKind.Corrupted);
                menu.SetSelectedSlot(1, true, "msg");
                var cards = instance.GetComponentsInChildren<SaveSlotCardView>(true);
                Assert.That(cards.Length, Is.EqualTo(3));
                Assert.That(cards[0].IsDeleteAvailable, Is.True);
                Assert.That(cards[1].IsDeleteAvailable, Is.False, "빈 슬롯엔 휴지통이 없다");
                Assert.That(cards[2].IsDeleteAvailable, Is.True);
                Assert.That(cards[0].DeleteAlpha, Is.GreaterThan(cards[2].DeleteAlpha), "선택 카드에서만 또렷하게");
                Assert.That(cards[0].FrameAlpha, Is.EqualTo(1f));
                Assert.That(cards[1].FrameAlpha + cards[2].FrameAlpha, Is.Zero, "프레임은 한 카드에만");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void Issue219_PrimaryActionStartsEmptySlotsButDisablesCorruptedSlots()
        {
            Assert.That(save.Save(1, Context(10, 3, 1)).IsSuccess, Is.True);
            paths.TryGetPaths(3, out var broken);
            Directory.CreateDirectory(Path.GetDirectoryName(broken.Normal));
            File.WriteAllText(broken.Normal, "{ broken");

            var view = new RecordingView();
            using (var presenter = new MainMenuPresenter(view, load, "test"))
            {
                presenter.Refresh();
                presenter.SelectSlot(1);
                Assert.That(view.Continue, Is.True);
                presenter.SelectSlot(2);
                Assert.That(view.Continue, Is.True, "빈 슬롯은 새 탐사를 시작할 수 있다.");
                Assert.That(view.PrimaryLabel, Is.EqualTo("새 탐사 시작"));
                presenter.SelectSlot(3);
                Assert.That(view.Continue, Is.False, "손상 슬롯");
                Assert.That(view.PrimaryLabel, Is.EqualTo("이어하기"));
                Assert.That(view.Kinds[3], Is.EqualTo(SaveSlotCardKind.Corrupted));
            }

            Assert.That(SaveSlotCardFormatter.BuildLabel(load.GetSlotMetadata(3), SlotContinueEligibility.Unrecoverable),
                Does.Contain("이어하기 불가"));
        }

        [Test]
        public void Issue219_PrimaryButtonLabelChangesWithoutResizing()
        {
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Project/Prefabs/UI/MainMenuPanel.prefab");
            var instance = UnityEngine.Object.Instantiate(prefab);
            try
            {
                var view = instance.GetComponent<MainMenuView>();
                var button = (UnityEngine.UI.Button)typeof(MainMenuView).GetField("continueButton",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(view);
                var rect = (RectTransform)button.transform;
                var size = rect.sizeDelta;
                var position = rect.anchoredPosition;
                view.SetPrimaryAction("새 탐사 시작", true);
                Assert.That(button.interactable, Is.True);
                Assert.That(button.GetComponentInChildren<TMPro.TMP_Text>(true).text, Is.EqualTo("새 탐사 시작"));
                view.SetPrimaryAction("이어하기", true);
                Assert.That(button.GetComponentInChildren<TMPro.TMP_Text>(true).text, Is.EqualTo("이어하기"));
                Assert.That(rect.sizeDelta, Is.EqualTo(size));
                Assert.That(rect.anchoredPosition, Is.EqualTo(position));
            }
            finally { UnityEngine.Object.DestroyImmediate(instance); }
        }

        [Test]
        public void Prefab_UsesSurfaceBaseButtons_AndMagnetFramePieces()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PromptB150MainMenuBuilder.PrefabPath);
            var content = prefab.transform.Find("MenuContent");
            Assert.That(content.Find("BootHeader"), Is.Not.Null);
            Assert.That(content.Find("Subtitle").GetComponent<TMPro.TMP_Text>().text, Is.EqualTo(PromptB150MainMenuBuilder.Subtitle));
            Assert.That(prefab.GetComponent<MainMenuIntroDirector>(), Is.Not.Null);
            Assert.That(prefab.transform.Find("Atmosphere").GetComponent<MainMenuAtmosphere>(), Is.Not.Null);
            Assert.That(prefab.transform.Find("BootOverlay").gameObject.activeSelf, Is.False);
            Assert.That(prefab.transform.Find("DeleteConfirm").gameObject.activeSelf, Is.False);
            Assert.That(prefab.transform.Find("Atmosphere").GetSiblingIndex(),
                Is.LessThan(content.GetSiblingIndex()), "분위기 레이어는 메뉴 뒤");

            var cont = content.Find("ContinueButton");
            Assert.That(AssetDatabase.GetAssetPath(cont.Find("Frame").GetComponent<UnityEngine.UI.Image>().sprite),
                Is.EqualTo(PromptB150MainMenuBuilder.ExplorationButtonPath));
            foreach (var name in new[] { "ContinueButton", "NewGameButton", "SettingsButton", "QuitButton" })
            {
                var button = content.Find(name);
                Assert.That(button.GetComponent<SurfaceBaseButtonFeedback>(), Is.Not.Null, name);
                Assert.That(button.Find("Icon"), Is.Not.Null, name + " icon");
                Assert.That(button.GetComponent<MenuSpriteButtonSkin>(), Is.Null, name);
            }
            Assert.That(AssetDatabase.GetAssetPath(content.Find("QuitButton/Frame").GetComponent<UnityEngine.UI.Image>().sprite),
                Is.EqualTo(PromptB150MainMenuBuilder.DangerOffPath), "종료는 빨간 위험 버튼");
            Assert.That(AssetDatabase.GetAssetPath(content.Find("NewGameButton/Frame").GetComponent<UnityEngine.UI.Image>().sprite),
                Is.EqualTo(PromptB150MainMenuBuilder.ButtonOffPath));

            for (var i = 1; i <= 3; i++)
            {
                var slot = content.Find("Slot" + i);
                var card = slot.GetComponent<SaveSlotCardView>();
                Assert.That(card.FramePieceCount, Is.EqualTo(6), "모서리 4 + 위·아래 바");
                foreach (var piece in PromptB150MainMenuBuilder.FramePieceNames)
                    Assert.That(slot.Find("MagnetFrame/" + piece), Is.Not.Null, piece);
                Assert.That(slot.Find("Selection/TriangleSelector"), Is.Null, "화살표와 프레임을 함께 쓰지 않는다");
                Assert.That(slot.Find("DeleteButton"), Is.Not.Null);
                Assert.That(slot.Find("Thumbnail/SignalNoise"), Is.Not.Null);

                // 휴지통은 설정창 X버튼 판에서 X만 뺀 기본/호버 발광 이미지 + 쓰레기통 아이콘.
                var plate = slot.Find("DeleteButton").GetComponent<UnityEngine.UI.Image>();
                Assert.That(AssetDatabase.GetAssetPath(plate.sprite), Is.EqualTo(PromptB150MainMenuBuilder.DeleteOffPath));
                var hover = slot.Find("DeleteButton/HoverOverlay").GetComponent<UnityEngine.UI.Image>();
                Assert.That(AssetDatabase.GetAssetPath(hover.sprite), Is.EqualTo(PromptB150MainMenuBuilder.DeleteOnPath));
                Assert.That(slot.Find("DeleteButton").GetComponent<UnityEngine.UI.Button>().targetGraphic, Is.EqualTo(hover));
                Assert.That(hover.color.a, Is.EqualTo(1f), "ColorTint는 이미지 색에 곱해지므로 알파 1이어야 호버 때 켜진다");
                Assert.That(slot.Find("DeleteButton").GetComponent<UnityEngine.UI.Button>().colors.normalColor.a, Is.Zero, "평소엔 꺼짐");
                Assert.That(slot.Find("DeleteButton").GetComponent<UnityEngine.UI.Button>().colors.highlightedColor.a, Is.EqualTo(1f));
                var icon = slot.Find("DeleteButton/Icon");
                Assert.That(AssetDatabase.GetAssetPath(icon.GetComponent<UnityEngine.UI.Image>().sprite),
                    Is.EqualTo(PromptB150MainMenuBuilder.TrashIconPath));
                Assert.That(icon.GetSiblingIndex(), Is.GreaterThan(hover.transform.GetSiblingIndex()), "아이콘이 발광 위");
                var whiteIcon = slot.Find("DeleteButton/IconWhite");
                Assert.That(AssetDatabase.GetAssetPath(whiteIcon.GetComponent<UnityEngine.UI.Image>().sprite),
                    Is.EqualTo(PromptB150MainMenuBuilder.TrashWhiteIconPath), "호버용 흰색 쓰레기통");
                Assert.That(whiteIcon.GetSiblingIndex(), Is.GreaterThan(icon.GetSiblingIndex()), "흰색 아이콘이 기본 아이콘 위");
                Assert.That(whiteIcon.GetComponent<UnityEngine.UI.Image>().color.a, Is.Zero, "평소엔 숨김");
                Assert.That(slot.Find("DeleteButton").GetComponent<SlotDeleteButtonHover>(), Is.Not.Null);

                // 프레임은 카드 바깥에 안쪽 모서리가 카드 가장자리에 붙는다.
                var rect = (RectTransform)slot;
                var halfW = rect.sizeDelta.x * 0.5f;
                var halfH = rect.sizeDelta.y * 0.5f;
                var tl = (RectTransform)slot.Find("MagnetFrame/CornerTL");
                var tlInnerX = tl.anchoredPosition.x - tl.sizeDelta.x * 0.5f + PromptB150MainMenuBuilder.CornerThickness;
                Assert.That(tlInnerX, Is.EqualTo(-halfW + PromptB150MainMenuBuilder.FrameOverlap).Within(0.01f),
                    "모서리 조각 세로 팔의 안쪽 면이 카드 왼쪽 가장자리에 붙는다");
                var top = (RectTransform)slot.Find("MagnetFrame/BarTop");
                var topInner = top.anchoredPosition.y + top.sizeDelta.y * 0.5f - PromptB150MainMenuBuilder.BarThickness;
                Assert.That(topInner, Is.EqualTo(halfH - PromptB150MainMenuBuilder.FrameOverlap).Within(0.01f),
                    "위쪽 바의 안쪽 면이 카드 위 가장자리에 붙는다");
            }

            var atmosphere = prefab.transform.Find("Atmosphere");
            Assert.That(atmosphere.Find("StaticBurst"), Is.Null, "TV 신호 노이즈는 글리치로 대체");
            Assert.That(atmosphere.Find("EdgeGlitch"), Is.Not.Null);
            Assert.That(content.Find("ContinueButton/Icon"), Is.Not.Null);
            var continueIcon = (RectTransform)content.Find("ContinueButton/Icon");
            var continueLabel = (RectTransform)content.Find("ContinueButton/Label");
            var gap = (continueLabel.anchoredPosition.x - continueLabel.sizeDelta.x * 0.5f + 8f)
                - (continueIcon.anchoredPosition.x + continueIcon.sizeDelta.x * 0.5f);
            Assert.That(gap, Is.EqualTo(PromptB150MainMenuBuilder.ContinueIconGap).Within(0.5f), "이어하기 아이콘과 글자 사이 간격");
            Assert.That(continueIcon.anchoredPosition.x, Is.GreaterThan(-196f), "아이콘이 글자 쪽으로 붙는다");
        }

        private static SaveCaptureContext Context(int maxDepth, int depth, int objectives)
        {
            var state = GameState.CreateNew();
            state.SetGold(500);
            state.SetDepth(maxDepth);
            state.SetDepth(depth);
            state.SetDemoProgress(string.Empty, objectives, false);
            return new SaveCaptureContext(state, new InventoryState(), new UpgradeState(), null, null,
                SceneNames.SurfaceBase, "b150-test");
        }

        private sealed class RecordingView : IMainMenuView
        {
            public bool DeleteVisible;
            public bool Continue;
            public string PrimaryLabel;
            public readonly SaveSlotCardKind[] Kinds = new SaveSlotCardKind[4];

            public void SetSlotDisplay(int slotId, string label, bool canContinue, string statusText) { }
            public void SetSelectedSlot(int slotId, bool canContinue, string message) => Continue = canContinue;
            public void SetPrimaryAction(string label, bool enabled) { PrimaryLabel = label; Continue = enabled; }
            public void SetOverwriteConfirmVisible(bool visible, int slotId) { }
            public void SetSettingsVisible(bool visible) { }
            public void SetSettingsDraft(SettingsValues values) { }
            public void SetVersionLabel(string version) { }
            public void SetMessage(string message) { }
            public void SetQuitBlockedMessage(string message) { }
            public void SetSlotKind(int slotId, SaveSlotCardKind kind) => Kinds[slotId] = kind;
            public void SetDeleteConfirmVisible(bool visible, int slotId) => DeleteVisible = visible;
        }
    }
}

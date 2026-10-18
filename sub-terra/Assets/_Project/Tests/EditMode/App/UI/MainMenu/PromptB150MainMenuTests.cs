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
        public void Boot_RunsInOrder_WithinOneSecond()
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
            Assert.That(cardAt[1] - cardAt[0], Is.EqualTo(0.08f).Within(0.011f), "카드 0.08초 간격");
            Assert.That(cardAt[2] - cardAt[1], Is.EqualTo(0.08f).Within(0.011f));
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

        [Test]
        public void Metadata_OldSaveWithoutValues_ShowsDash()
        {
            Assert.That(save.Save(2, Context(maxDepth: 0, depth: 0, objectives: 0)).IsSuccess, Is.True);
            paths.TryGetPaths(2, out var slot);
            var text = File.ReadAllText(slot.Normal);
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
            Assert.That(SaveVersions.Current, Is.EqualTo(5), "B-150은 SaveVersion을 올리지 않는다");
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
                Assert.That(view.Continue, Is.False);
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
        public void Continue_DisabledForEmptyAndCorruptedSlots()
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
                Assert.That(view.Continue, Is.False, "빈 슬롯");
                presenter.SelectSlot(3);
                Assert.That(view.Continue, Is.False, "손상 슬롯");
                Assert.That(view.Kinds[3], Is.EqualTo(SaveSlotCardKind.Corrupted));
            }

            Assert.That(SaveSlotCardFormatter.BuildLabel(load.GetSlotMetadata(3), SlotContinueEligibility.Unrecoverable),
                Does.Contain("이어하기 불가"));
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
            }
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
            public readonly SaveSlotCardKind[] Kinds = new SaveSlotCardKind[4];

            public void SetSlotDisplay(int slotId, string label, bool canContinue, string statusText) { }
            public void SetSelectedSlot(int slotId, bool canContinue, string message) => Continue = canContinue;
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

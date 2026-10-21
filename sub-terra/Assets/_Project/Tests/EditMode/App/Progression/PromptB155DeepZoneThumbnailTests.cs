using System.Linq;
using NUnit.Framework;
using SubTerra.App.Editor.DataValidation;
using SubTerra.App.Progression;
using SubTerra.App.UI.Progression;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.Tests.Progression
{
    /// <summary>
    /// prompt-B 155: 업그레이드 창 심층 구역 레벨별 썸네일, 잠김 흑백·반짝임, 해금 연출 1회 재생.
    /// </summary>
    public sealed class PromptB155DeepZoneThumbnailTests
    {
        private const string SurfacePrefabPath = "Assets/_Project/Prefabs/UI/SurfaceBasePanel.prefab";
        private static readonly ZoneAccessResult Locked = new ZoneAccessResult(false, false, "조건 부족");
        private static readonly ZoneAccessResult UnlockedBefore = new ZoneAccessResult(true, false, string.Empty);
        private static readonly ZoneAccessResult UnlockedNow = new ZoneAccessResult(true, true, string.Empty);

        private GameObject root;
        private DeepZoneDisplaySettings settings;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("B155Root", typeof(RectTransform));
            ((RectTransform)root.transform).sizeDelta = new Vector2(1006f, 106f);
            settings = MakeSettings(
                new DeepZoneDisplayLevel(36, 40, true, string.Empty),
                new DeepZoneDisplayLevel(41, 75, false, "Lv.1 해금 · Demo 목표 13개 완료"),
                new DeepZoneDisplayLevel(76, 100, false, "Lv.2 해금 · 골드/희귀 자원 조건 준비 중"));
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(settings);
        }

        private static DeepZoneDisplaySettings MakeSettings(params DeepZoneDisplayLevel[] levels)
        {
            var value = ScriptableObject.CreateInstance<DeepZoneDisplaySettings>();
            value.EditorSet(levels);
            return value;
        }

        private DeepZoneThumbnailStrip NewStrip()
        {
            return DeepZoneThumbnailStrip.Create((RectTransform)root.transform, null);
        }

        // ------------------------------------------------------------------ 설정

        [Test]
        public void UnlockedLevelCount_FollowsExistingUnlockOnly()
        {
            Assert.That(settings.UnlockedLevelCount(Locked), Is.EqualTo(0));
            Assert.That(settings.UnlockedLevelCount(UnlockedBefore), Is.EqualTo(1));
            Assert.That(settings.UnlockedLevelCount(UnlockedNow), Is.EqualTo(1));

            var empty = MakeSettings();
            try
            {
                Assert.That(empty.UnlockedLevelCount(UnlockedBefore), Is.EqualTo(0));
            }
            finally { Object.DestroyImmediate(empty); }
        }

        [Test]
        public void Labels_ComeFromSettings()
        {
            Assert.That(settings.LevelTitle(0), Is.EqualTo("Lv.1 · 36~40M"));
            Assert.That(settings.LevelTitle(1), Is.EqualTo("Lv.2 · 41~75M (예정)"));
            Assert.That(settings.LevelCondition(0, false), Is.EqualTo(DeepZoneUnlockRule.Mvp.DescribeUpgradeRequirements()));
            Assert.That(settings.LevelCondition(2, false), Does.Contain("준비 중"));
            Assert.That(settings.LevelCondition(1, true), Is.EqualTo("해금됨"));
            Assert.That(settings.BuildSummary(Locked), Is.EqualTo("심층 구역 Lv.0/3"));
            Assert.That(settings.BuildSummary(UnlockedBefore), Is.EqualTo("심층 구역 Lv.1/3"));

            var custom = MakeSettings(new DeepZoneDisplayLevel(10, 20, true, string.Empty));
            try
            {
                Assert.That(custom.LevelTitle(0), Is.EqualTo("Lv.1 · 10~20M"));
                Assert.That(custom.BuildSummary(UnlockedBefore), Is.EqualTo("심층 구역 Lv.1/1 · 모든 구역 해금"));
            }
            finally { Object.DestroyImmediate(custom); }
        }

        [Test]
        public void ProjectSettings_HaveThreeLevelsWithArtAndCalmGlint()
        {
            var asset = Resources.Load<DeepZoneDisplaySettings>(DeepZoneDisplaySettings.ResourcePath);
            Assert.That(asset, Is.Not.Null);
            Assert.That(asset.Levels.Count, Is.EqualTo(3));
            Assert.That(asset.Levels[2].PlannedRequirement, Does.Contain("준비 중"));
            for (var i = 0; i < asset.Levels.Count; i++)
            {
                Assert.That(asset.Levels[i].Thumbnail, Is.Not.Null, "thumbnail " + i);
                Assert.That(asset.Levels[i].MineralMask, Is.Not.Null, "mask " + i);
            }

            Assert.That(asset.ThumbnailStyle.glintPeriod, Is.GreaterThanOrEqualTo(2f));
        }

        [Test]
        public void Builder_FillsOnlyEmptySpritesAndKeepsDepthAndText()
        {
            var texture = new Texture2D(4, 4);
            var custom = Sprite.Create(texture, new Rect(0, 0, 4, 4), Vector2.one * 0.5f);
            var target = MakeSettings(
                new DeepZoneDisplayLevel(11, 22, true, "직접 쓴 문구"),
                new DeepZoneDisplayLevel(33, 44, false, "두 번째"));
            try
            {
                target.Levels[0].EditorSetArt(custom, null);
                Issue223DisplaySettingsBuilder.AssignMissingArt(target);
                Assert.That(target.Levels[0].Thumbnail, Is.SameAs(custom));
                Assert.That(target.Levels[0].MineralMask, Is.Not.Null);
                Assert.That(target.Levels[1].Thumbnail, Is.SameAs(
                    AssetDatabase.LoadAssetAtPath<Sprite>(Issue223DisplaySettingsBuilder.ThumbnailPath(1))));
                Assert.That(target.Levels[0].MinimumDepth, Is.EqualTo(11));
                Assert.That(target.Levels[0].MaximumDepth, Is.EqualTo(22));
                Assert.That(target.Levels[0].PlannedRequirement, Is.EqualTo("직접 쓴 문구"));
                Assert.That(target.Levels[1].MinimumDepth, Is.EqualTo(33));
            }
            finally
            {
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(custom);
                Object.DestroyImmediate(texture);
            }
        }

        [Test]
        public void Shader_LoadsFromResources()
        {
            var shader = Resources.Load<Shader>(DeepZoneThumbnailStrip.ShaderResourceName);
            Assert.That(shader, Is.Not.Null);
            Assert.That(shader.name, Is.EqualTo(DeepZoneThumbnailStrip.ShaderName));
            Assert.That(shader.isSupported, Is.True);
        }

        // ------------------------------------------------------------------ 칸 구성

        [Test]
        public void CellCount_FollowsSettings_AndRefreshDoesNotDuplicate()
        {
            var two = MakeSettings(
                new DeepZoneDisplayLevel(10, 20, true, string.Empty),
                new DeepZoneDisplayLevel(21, 30, false, "추가 조건"));
            try
            {
                var strip = NewStrip();
                strip.Apply(two, Locked);
                strip.Apply(two, Locked);
                strip.Apply(two, UnlockedBefore);
                Assert.That(strip.CellCount, Is.EqualTo(2));
                Assert.That(strip.transform.childCount, Is.EqualTo(2));
                Assert.That(strip.GetCellState(0).Title, Is.EqualTo("Lv.1 · 10~20M"));
                Assert.That(strip.GetCellState(1).Title, Is.EqualTo("Lv.2 · 21~30M (예정)"));
            }
            finally { Object.DestroyImmediate(two); }
        }

        [Test]
        public void MissingSprites_FallBackWithoutException()
        {
            var strip = NewStrip();
            Assert.DoesNotThrow(() => strip.Apply(settings, Locked));
            for (var i = 0; i < strip.CellCount; i++)
            {
                var state = strip.GetCellState(i);
                Assert.That(state.HasSprite, Is.False);
                Assert.That(state.GlintEnabled, Is.False);
                Assert.That(state.LockVisible, Is.True);
            }

            // 레벨이 오를수록 대체 칸이 어두워진다.
            var style = settings.ThumbnailStyle;
            Assert.That(style.FallbackColorFor(2).grayscale, Is.LessThan(style.FallbackColorFor(0).grayscale));
        }

        [Test]
        public void ThumbnailsDoNotReceiveRaycasts()
        {
            var strip = NewStrip();
            strip.Apply(settings, Locked);
            var graphics = strip.GetComponentsInChildren<Graphic>(true);
            Assert.That(graphics.Length, Is.GreaterThan(0));
            Assert.That(graphics.All(g => !g.raycastTarget), Is.True);
            Assert.That(strip.GetComponentsInChildren<Selectable>(true), Is.Empty);
        }

        // ------------------------------------------------------------------ 연출 시점

        [Test]
        public void LockedFirstDraw_IsGrayWithLock()
        {
            var strip = NewStrip();
            strip.Apply(settings, Locked);
            var state = strip.GetCellState(0);
            Assert.That(state.Unlocked, Is.False);
            Assert.That(state.LockVisible, Is.True);
            if (state.HasMaterial)
            {
                Assert.That(state.Saturation, Is.EqualTo(0f));
            }

            Assert.That(state.Condition, Is.EqualTo(DeepZoneUnlockRule.Mvp.DescribeUpgradeRequirements()));
        }

        [Test]
        public void UnlockedWithoutDidUnlockNow_DrawsColorImmediately()
        {
            var strip = NewStrip();
            strip.Apply(settings, Locked);
            strip.Apply(settings, UnlockedBefore);
            AssertUnlockedNoMotion(strip);

            var fresh = NewStrip();
            fresh.Apply(settings, UnlockedNow);
            AssertUnlockedNoMotion(fresh);
        }

        private static void AssertUnlockedNoMotion(DeepZoneThumbnailStrip strip)
        {
            var state = strip.GetCellState(0);
            Assert.That(strip.HasPendingReveal, Is.False);
            Assert.That(state.Unlocked, Is.True);
            Assert.That(state.LockVisible, Is.False);
            if (state.HasMaterial)
            {
                Assert.That(state.Saturation, Is.EqualTo(1f));
            }
            else
            {
                Assert.That(state.ThumbnailColor.r, Is.GreaterThan(0f));
            }

            Assert.That(state.Condition, Is.EqualTo("해금됨"));
            Assert.That(strip.GetCellState(1).LockVisible, Is.True);
        }

        [Test]
        public void UnlockNow_WaitsForPopupThenPlaysOnce()
        {
            var popupOpen = true;
            var strip = NewStrip();
            strip.RevealBlocked = () => popupOpen;
            strip.Apply(settings, Locked);
            strip.Apply(settings, UnlockedNow);

            Assert.That(strip.HasPendingReveal, Is.True);
            Assert.That(strip.GetCellState(0).LockVisible, Is.True, "해금 창이 열린 동안은 잠김 모습을 유지한다");
            Assert.That(strip.TryPlayPending(), Is.False);

            // 해금 창이 닫힌 뒤 새로고침이 와도(DidUnlockNow=false) 대기는 남아 있다가 한 번 재생된다.
            strip.Apply(settings, UnlockedBefore);
            Assert.That(strip.HasPendingReveal, Is.True);
            popupOpen = false;
            Assert.That(strip.TryPlayPending(), Is.True);
            Assert.That(strip.HasPendingReveal, Is.False);
            Assert.That(strip.TryPlayPending(), Is.False);

            strip.Apply(settings, UnlockedNow);
            Assert.That(strip.HasPendingReveal, Is.False);
            Assert.That(strip.TryPlayPending(), Is.False);
            Assert.That(strip.GetCellState(0).LockVisible, Is.False);
        }

        [Test]
        public void RevealEndState_EqualsDirectUnlockedState()
        {
            var direct = NewStrip();
            direct.Apply(settings, UnlockedBefore);

            var animated = NewStrip();
            animated.Apply(settings, Locked);
            animated.Apply(settings, UnlockedNow);
            animated.SampleReveal(0, 0.1f);
            Assert.That(animated.GetCellState(0).LockVisible, Is.True);
            animated.SampleReveal(0, 0.5f);
            var mid = animated.GetCellState(0);
            if (mid.HasMaterial)
            {
                Assert.That(mid.RevealRadius, Is.GreaterThan(0f));
            }

            animated.SampleReveal(0, 1f);
            AssertSameState(direct.GetCellState(0), animated.GetCellState(0));

            // 실제 재생 경로(에디트 모드는 끝 상태로 바로 맞춤)도 같은 끝 상태다.
            var played = NewStrip();
            played.Apply(settings, Locked);
            played.Apply(settings, UnlockedNow);
            Assert.That(played.TryPlayPending(), Is.True);
            AssertSameState(direct.GetCellState(0), played.GetCellState(0));
        }

        private static void AssertSameState(DeepZoneThumbnailStrip.CellState expected, DeepZoneThumbnailStrip.CellState actual)
        {
            Assert.That(actual.Unlocked, Is.EqualTo(expected.Unlocked));
            Assert.That(actual.LockVisible, Is.EqualTo(expected.LockVisible));
            Assert.That(actual.Saturation, Is.EqualTo(expected.Saturation));
            Assert.That(actual.RevealRadius, Is.EqualTo(expected.RevealRadius));
            Assert.That(actual.ThumbnailColor, Is.EqualTo(expected.ThumbnailColor));
            Assert.That(actual.FrameColor, Is.EqualTo(expected.FrameColor));
            Assert.That(actual.Condition, Is.EqualTo(expected.Condition));
        }

        [Test]
        public void Tracker_RelockClearsPendingAndFirstDrawNeverAnimates()
        {
            var tracker = new DeepZoneRevealTracker();
            tracker.Apply(1, true);
            Assert.That(tracker.HasPending, Is.False);
            Assert.That(tracker.IsRevealed(0), Is.True);

            tracker.Apply(0, false);
            tracker.Apply(1, true);
            Assert.That(tracker.IsPending(0), Is.True);
            tracker.Apply(0, false);
            Assert.That(tracker.HasPending, Is.False);
            Assert.That(tracker.TryTakePending(out _, out _), Is.False);
        }

        // ------------------------------------------------------------------ 업그레이드 창 연결

        [Test]
        public void SurfaceWindow_BuildsThumbnailStripUnderTree()
        {
            var instance = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(SurfacePrefabPath));
            try
            {
                var tree = instance.GetComponentInChildren<UpgradeTreeView>(true);
                var initialBottom = tree.Viewport.offsetMin.y;
                tree.SetDeepZoneStatus(Locked);
                tree.SetDeepZoneStatus(Locked);
                Assert.That(tree.DeepZoneThumbnails, Is.Not.Null);
                Assert.That(tree.DeepZoneThumbnails.CellCount, Is.EqualTo(3));
                Assert.That(tree.DeepZoneSummary.text, Is.EqualTo("심층 구역 Lv.0/3"));
                Assert.That(tree.Viewport.offsetMin.y - initialBottom, Is.EqualTo(148f).Within(0.01f));
                Assert.That(instance.GetComponentsInChildren<DeepZoneThumbnailStrip>(true).Length, Is.EqualTo(1));
                Assert.That(tree.DeepZoneThumbnails.GetCellState(0).HasSprite, Is.True);
                Assert.That(tree.DeepZoneThumbnails.GetCellState(2).Condition, Does.Contain("준비 중"));
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void SurfaceWindowFont_HasNewGlyphs()
        {
            var instance = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(SurfacePrefabPath));
            try
            {
                var tree = instance.GetComponentInChildren<UpgradeTreeView>(true);
                tree.SetDeepZoneStatus(Locked);
                var font = tree.DeepZoneSummary.font;
                Assert.That(font, Is.Not.Null);
                const string glyphs = "심층구역모든해금됨예정준비중·~MLv.0123456789()";
                var ok = font.HasCharacters(glyphs, out var missing, true, false);
                Assert.That(ok, Is.True, "missing: " + (missing == null ? string.Empty : string.Join(",", missing)));
                foreach (var text in tree.DeepZoneThumbnails.GetComponentsInChildren<TMP_Text>(true))
                {
                    Assert.That(text.font, Is.SameAs(font));
                    Assert.That(text.fontSizeMin, Is.GreaterThanOrEqualTo(11f));
                }
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using NUnit.Framework;
using SubTerra.App.Audio;
using SubTerra.App.UI;
using SubTerra.App.UI.SurfaceBase;
using SubTerra.Shared;
using SubTerra.Shared.Audio;
using UnityEngine;

namespace SubTerra.App.Tests.UI
{
    public sealed partial class PromptB169UnlockAndTvTests
    {
        // ---------- C 업그레이드 창 자물쇠 소리 ----------

        private static readonly (SfxId Id, int Number, string File, SfxBus Bus, float Volume)[] SlotTable =
        {
            (SfxId.UnlockRattle, 76, "unlock_rattle", SfxBus.UI, 0.5f),
            (SfxId.UnlockClick, 77, "unlock_click", SfxBus.UI, 0.7f),
            (SfxId.UnlockGlow, 78, "unlock_glow", SfxBus.UI, 0.45f),
            (SfxId.WindowTvOpen, 79, "window_tv_open", SfxBus.UI, 0.4f),
            (SfxId.WindowTvClose, 80, "window_tv_close", SfxBus.UI, 0.5f),
            (SfxId.UpgradeLockCrack, 81, "upgrade_lock_crack", SfxBus.UI, 0.25f),
            (SfxId.UpgradeLockShatter, 82, "upgrade_lock_shatter", SfxBus.UI, 0.55f),
            (SfxId.UpgradeWindowSuckIn, 83, "upgrade_window_suck_in", SfxBus.UI, 0.35f),
            (SfxId.UpgradeLockRegather, 84, "upgrade_lock_regather", SfxBus.UI, 0.5f),
        };

        [Test]
        public void SurfaceUpgradeWindow_FirstOpen_Reopen_Close_PlayEachLockSoundOnce()
        {
            var instance = Spawn();
            var view = instance.GetComponent<SurfaceBaseView>();
            var motion = view.UpgradeMotion;
            UseTestClock(motion);
            view.RequestUpgradeOpen();
            AssertLockCycle(motion, view.RequestUpgradeClose, view.RequestUpgradeOpen);
        }

        [Test]
        public void MineUpgradeWindow_SameSoundsAsSurface()
        {
            var panels = CreateMineRig(out var card);
            panels.ToggleUpgrade();
            var motion = card.GetComponent<UpgradeWindowMotion>();
            UseTestClock(motion);
            AssertLockCycle(motion, panels.CloseUpgrade, panels.ToggleUpgrade);
        }

        [Test]
        public void UpgradeWindow_LargeFrameSkip_EachSoundOnce()
        {
            var instance = Spawn();
            var view = instance.GetComponent<SurfaceBaseView>();
            var motion = view.UpgradeMotion;
            motion.ManualTick = true;
            UseTestClock(motion);

            view.RequestUpgradeOpen();
            for (var i = 0; i < 3; i++)
            {
                now += 0.6f;
                motion.Tick(0.6f);
            }

            Assert.That(Count(SfxId.UnlockRattle), Is.EqualTo(1));
            Assert.That(Count(SfxId.UnlockClick), Is.EqualTo(1));
            Assert.That(Count(SfxId.UpgradeLockCrack), Is.EqualTo(1));
            Assert.That(Count(SfxId.UpgradeLockShatter), Is.EqualTo(1));

            requests.Clear();
            view.RequestUpgradeClose();
            now += 0.6f;
            motion.Tick(0.6f);
            Assert.That(Count(SfxId.UpgradeWindowSuckIn), Is.EqualTo(1));
            Assert.That(Count(SfxId.UpgradeLockRegather), Is.EqualTo(1));
        }

        [Test]
        public void UpgradeWindow_RapidToggle_DoesNotStackSounds()
        {
            var instance = Spawn();
            var view = instance.GetComponent<SurfaceBaseView>();
            var motion = view.UpgradeMotion;
            motion.ManualTick = true;
            UseTestClock(motion);

            view.RequestUpgradeOpen();
            Run(motion, 1.3f);
            Assert.That(motion.IsSettled, Is.True);

            requests.Clear();
            var close = 0;
            var crack = 0;
            var shattered = 0;
            motion.CloseStarted += () => close++;
            motion.CrackStarted += () => crack++;
            motion.Shattered += () => shattered++;

            view.RequestUpgradeClose();
            Run(motion, 0.05f);
            view.RequestUpgradeOpen();
            Run(motion, 0.05f);
            view.RequestUpgradeClose();
            Run(motion, 0.05f);
            view.RequestUpgradeOpen();
            Run(motion, 0.8f);

            Assert.That(Count(SfxId.UpgradeWindowSuckIn), Is.LessThanOrEqualTo(close));
            Assert.That(Count(SfxId.UnlockClick), Is.LessThanOrEqualTo(crack));
            Assert.That(Count(SfxId.UpgradeLockShatter), Is.LessThanOrEqualTo(shattered));
            // 다시 열기 규칙: 흔들림·처음 금 소리는 나지 않는다.
            Assert.That(Count(SfxId.UnlockRattle), Is.EqualTo(0));
            Assert.That(Count(SfxId.UpgradeLockCrack), Is.EqualTo(0));
            Assert.That(motion.IsSettled, Is.True);
        }

        [Test]
        public void UpgradeWindow_ReduceMotion_NoLockSounds_TvOnAndOffOnce()
        {
            AccessibilityPreferences.ReduceMotion = true;
            var instance = Spawn();
            var view = instance.GetComponent<SurfaceBaseView>();

            view.RequestUpgradeOpen();
            Assert.That(requests, Is.Empty, "모션 감소: 자물쇠 소리 없음");
            Assert.That(WindowTvSfx.StartedCount, Is.EqualTo(1));
            Assert.That(WindowTvSfx.LastId, Is.EqualTo(SfxId.WindowTvOpen));

            view.RequestUpgradeClose();
            Assert.That(requests, Is.Empty);
            Assert.That(WindowTvSfx.StartedCount, Is.EqualTo(2));
            Assert.That(WindowTvSfx.LastId, Is.EqualTo(SfxId.WindowTvClose));

            var panels = CreateMineRig(out _);
            panels.ToggleUpgrade();
            Assert.That(requests, Is.Empty);
            Assert.That(WindowTvSfx.StartedCount, Is.EqualTo(3));
            Assert.That(WindowTvSfx.LastId, Is.EqualTo(SfxId.WindowTvOpen));

            panels.CloseUpgrade();
            Assert.That(requests, Is.Empty);
            Assert.That(WindowTvSfx.StartedCount, Is.EqualTo(4));
            Assert.That(WindowTvSfx.LastId, Is.EqualTo(SfxId.WindowTvClose));
        }

        [Test]
        public void UpgradeWindow_SetUpgradeVisible_IsSilent()
        {
            var instance = Spawn();
            var view = instance.GetComponent<SurfaceBaseView>();

            for (var i = 0; i < 2; i++)
            {
                view.SetUpgradeVisible(true);
                view.SetUpgradeVisible(false);
            }

            Assert.That(requests, Is.Empty);
            Assert.That(WindowTvSfx.StartedCount, Is.EqualTo(0));

            AccessibilityPreferences.ReduceMotion = true;
            for (var i = 0; i < 2; i++)
            {
                view.SetUpgradeVisible(true);
                view.SetUpgradeVisible(false);
            }

            Assert.That(requests, Is.Empty);
            Assert.That(WindowTvSfx.StartedCount, Is.EqualTo(0));
        }

        [Test]
        public void Slots81To84_ValuesAndFiles()
        {
            var defaults = new Dictionary<SfxId, (SfxEntry Entry, string File)>();
            foreach (var pair in SfxLibraryDefaults.Create())
            {
                defaults[pair.Entry.Id] = (pair.Entry, pair.FileName);
            }

            var library = Resources.Load<SfxLibrary>(SfxLibrary.ResourcePath);
            Assert.That(library, Is.Not.Null, "Resources/SfxLibrary");

            foreach (var row in SlotTable)
            {
                var label = row.Id.ToString();
                Assert.That((int)row.Id, Is.EqualTo(row.Number), label);

                Assert.That(defaults.TryGetValue(row.Id, out var actual), Is.True, label);
                Assert.That(actual.File, Is.EqualTo(row.File), label);
                Assert.That(actual.Entry.Bus, Is.EqualTo(row.Bus), label);
                Assert.That(actual.Entry.Volume, Is.EqualTo(row.Volume).Within(0.0001f), label);
                Assert.That(actual.Entry.StartTime, Is.EqualTo(0f).Within(0.0001f), label);
                // 흔들림 소리만 0.3초 자르고 페이드(B-168 값 그대로).
                var maxDuration = row.Id == SfxId.UnlockRattle ? 0.3f : 0f;
                var fadeOut = row.Id == SfxId.UnlockRattle ? 0.1f : 0f;
                Assert.That(actual.Entry.MaxDuration, Is.EqualTo(maxDuration).Within(0.0001f), label);
                Assert.That(actual.Entry.FadeOut, Is.EqualTo(fadeOut).Within(0.0001f), label);

                Assert.That(library.TryGetEntry(row.Id, out var asset), Is.True, label);
                Assert.That(asset.Clip, Is.Not.Null, label);
                Assert.That(asset.Clip.name, Is.EqualTo(row.File), label);
                Assert.That(asset.Bus, Is.EqualTo(row.Bus), label);
                Assert.That(asset.Volume, Is.EqualTo(row.Volume).Within(0.0001f), label);
            }
        }

        // ---------- C 도우미 ----------

        /// <summary>모션 소리 훅에 테스트 시계(now)를 넣는다(같은 소리 0.1초 제한).</summary>
        private void UseTestClock(UpgradeWindowMotion motion)
        {
            var sfx = motion.GetComponent<UpgradeWindowMotionSfx>();
            Assert.That(sfx, Is.Not.Null, "효과음 컴포넌트가 붙어 있어야 한다");
            sfx.Clock = () => now;
        }

        /// <summary>처음 열기(이미 시작됨) → 닫기 → 다시 열기 한 바퀴의 자물쇠 소리를 확인한다.</summary>
        private void AssertLockCycle(UpgradeWindowMotion motion, Action close, Action reopen)
        {
            Run(motion, 1.3f);
            Assert.That(requests, Is.EqualTo(new[]
            {
                SfxId.UnlockRattle, SfxId.UnlockClick, SfxId.UpgradeLockCrack, SfxId.UpgradeLockShatter,
            }), "처음 열기");

            requests.Clear();
            close();
            Run(motion, 0.4f);
            Assert.That(requests, Is.EqualTo(new[] { SfxId.UpgradeWindowSuckIn, SfxId.UpgradeLockRegather }), "닫기");

            requests.Clear();
            reopen();
            Run(motion, 0.6f);
            Assert.That(requests, Is.EqualTo(new[] { SfxId.UnlockClick, SfxId.UpgradeLockShatter }), "다시 열기");
            Assert.That(WindowTvSfx.StartedCount, Is.EqualTo(0));
        }
    }
}

using System.IO;
using NUnit.Framework;
using SubTerra.App.Audio;
using SubTerra.App.Editor.DataValidation;
using SubTerra.Gameplay.Structural;
using SubTerra.Shared.Audio;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace SubTerra.App.Tests.Audio
{
    public sealed class PromptB156SfxTests
    {
        private SfxLibrary library;
        private SfxPlayer player;
        private float now;
        private bool muted;
        private bool autoAdvance;
        private readonly System.Collections.Generic.List<Object> created = new System.Collections.Generic.List<Object>();

        [SetUp]
        public void SetUp()
        {
            now = 10f;
            muted = false;
            autoAdvance = false;
            library = ScriptableObject.CreateInstance<SfxLibrary>();
            created.Add(library);
        }

        [TearDown]
        public void TearDown()
        {
            if (player != null)
            {
                player.ReleaseForTests();
                player = null;
            }

            for (var i = 0; i < created.Count; i++)
            {
                if (created[i] != null) Object.DestroyImmediate(created[i]);
            }

            created.Clear();
        }

        private AudioClip NewClip(float seconds)
        {
            var clip = AudioClip.Create("sfx-test", Mathf.CeilToInt(44100 * seconds), 1, 44100, false);
            created.Add(clip);
            return clip;
        }

        private void AddEntry(SfxId id, SfxBus bus, float seconds, float volume = 1f,
            float startTime = 0f, float maxDuration = 0f, float fadeOut = 0f)
        {
            var entry = new SfxEntry(id, bus, volume, 1f, startTime, maxDuration, fadeOut);
            if (seconds > 0f) entry.EditorSetClip(NewClip(seconds));
            library.EditorAddEntry(entry);
        }

        private SfxPlayer NewPlayer()
        {
            // autoAdvance가 켜지면 시계를 읽을 때마다 1초씩 올려 연타 제한이 개입하지 않게 한다.
            player = SfxPlayer.CreateForTests(library, () =>
            {
                if (autoAdvance) now += 1f;
                return now;
            }, () => muted);
            return player;
        }

        // ---- StructuralRiskFeedback ----

        [Test]
        public void RiskFeedback_NoLongerBuildsToneInCode()
        {
            var path = Path.Combine(Application.dataPath, "_Project/Scripts/Gameplay/Structural/StructuralRiskFeedback.cs");
            var source = File.ReadAllText(path);
            Assert.That(source, Does.Not.Contain("AudioClip.Create"));
            Assert.That(source, Does.Not.Contain("warningTone"));
            Assert.That(source, Does.Not.Contain("PlayOneShot"));
            Assert.That(typeof(StructuralRiskFeedback).GetField("warningTone",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic), Is.Null);
        }

        [Test]
        public void RiskAlarm_RepeatsMoreAndFasterAsRiskRises()
        {
            Assert.That(StructuralRiskFeedback.ResolveAlarmRepeat(StructuralRiskLevel.Caution), Is.EqualTo(1));
            Assert.That(StructuralRiskFeedback.ResolveAlarmRepeat(StructuralRiskLevel.Danger), Is.EqualTo(2));
            Assert.That(StructuralRiskFeedback.ResolveAlarmRepeat(StructuralRiskLevel.CollapseImminent), Is.EqualTo(3));
            Assert.That(StructuralRiskFeedback.ResolveAlarmInterval(StructuralRiskLevel.Danger), Is.EqualTo(0.34f));
            Assert.That(StructuralRiskFeedback.ResolveAlarmInterval(StructuralRiskLevel.CollapseImminent), Is.EqualTo(0.24f));
            Assert.That(StructuralRiskFeedback.ResolveAlarmInterval(StructuralRiskLevel.CollapseImminent),
                Is.LessThan(StructuralRiskFeedback.ResolveAlarmInterval(StructuralRiskLevel.Danger)));
            Assert.That(StructuralRiskFeedback.GetPitch(StructuralRiskLevel.Caution), Is.EqualTo(0.85f));
            Assert.That(StructuralRiskFeedback.GetPitch(StructuralRiskLevel.Danger), Is.EqualTo(1.05f));
            Assert.That(StructuralRiskFeedback.GetPitch(StructuralRiskLevel.CollapseImminent), Is.EqualTo(1.3f));
        }

        [TestCase(StructuralRiskLevel.Stable, StructuralRiskLevel.Caution, StructuralRiskFeedback.RiskSound.Alarm)]
        [TestCase(StructuralRiskLevel.Caution, StructuralRiskLevel.CollapseImminent, StructuralRiskFeedback.RiskSound.Alarm)]
        [TestCase(StructuralRiskLevel.Danger, StructuralRiskLevel.Caution, StructuralRiskFeedback.RiskSound.Tick)]
        [TestCase(StructuralRiskLevel.Caution, StructuralRiskLevel.Stable, StructuralRiskFeedback.RiskSound.Tick)]
        [TestCase(StructuralRiskLevel.Danger, StructuralRiskLevel.Danger, StructuralRiskFeedback.RiskSound.None)]
        [TestCase(StructuralRiskLevel.Stable, StructuralRiskLevel.Stable, StructuralRiskFeedback.RiskSound.None)]
        public void RiskSound_FollowsDirectionOfChange(
            StructuralRiskLevel previous, StructuralRiskLevel next, StructuralRiskFeedback.RiskSound expected)
        {
            Assert.That(StructuralRiskFeedback.ResolveRiskSound(previous, next), Is.EqualTo(expected));
        }

        // ---- SfxRequests / SfxPlayer ----

        [Test]
        public void Requests_WithoutListener_DoNotThrow()
        {
            Assert.DoesNotThrow(() => SfxRequests.Play(SfxId.UiClick));
            Assert.DoesNotThrow(() => SfxRequests.PlayAlarm(SfxId.AlarmMineWail, 3, 0.24f, 1.3f));
        }

        [Test]
        public void EmptyClipSlot_IsSilentWithoutException()
        {
            // 대신 낼 칸이 없는 빈 칸은 무음이고 경고는 한 번뿐이다.
            AddEntry(SfxId.UiScanTick, SfxBus.UI, 0f);
            NewPlayer();
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("UiScanTick"));
            Assert.DoesNotThrow(() => player.PlayNow(SfxId.UiScanTick));
            now += 1f;
            Assert.DoesNotThrow(() => player.PlayNow(SfxId.UiScanTick));
            Assert.DoesNotThrow(() => player.PlayNow(SfxId.UiHover));
            Assert.That(player.StartedCount, Is.Zero);
        }

        [Test]
        public void SameSfxWithinMinInterval_PlaysOnce()
        {
            AddEntry(SfxId.UiClick, SfxBus.UI, 0.5f);
            NewPlayer();
            player.PlayNow(SfxId.UiClick);
            now += 0.04f;
            player.PlayNow(SfxId.UiClick);
            Assert.That(player.StartedCount, Is.EqualTo(1));
            now += 0.02f;
            player.PlayNow(SfxId.UiClick);
            Assert.That(player.StartedCount, Is.EqualTo(2));
        }

        [Test]
        public void FullPool_StealsOldestAndKeepsAlert()
        {
            var ids = new[]
            {
                SfxId.UiClick, SfxId.UiHover, SfxId.UiSlotSelect, SfxId.UiConfirm, SfxId.UiCancelFail,
                SfxId.UiPopupOpen, SfxId.UiPopupClose, SfxId.UiScanTick, SfxId.UiCctvOn, SfxId.UiMineResetWhoosh
            };
            foreach (var id in ids) AddEntry(id, SfxBus.UI, 3f);
            AddEntry(SfxId.AlarmMineWail, SfxBus.Alert, 1f);
            NewPlayer();

            player.PlayAlarmNow(SfxId.AlarmMineWail, 1, 0.3f, 1f);
            Assert.That(player.IsAlertActive, Is.True);
            foreach (var id in ids)
            {
                now += 0.1f;
                player.PlayNow(id);
            }

            Assert.That(player.ActivePoolVoiceCount, Is.EqualTo(SfxPlayer.PoolVoiceCount));
            Assert.That(player.IsAlertActive, Is.True);
        }

        [Test]
        public void MasterMuted_IgnoresRequests()
        {
            AddEntry(SfxId.UiClick, SfxBus.UI, 0.5f);
            AddEntry(SfxId.AlarmMineWail, SfxBus.Alert, 1f);
            NewPlayer();
            muted = true;
            player.PlayNow(SfxId.UiClick);
            player.PlayAlarmNow(SfxId.AlarmMineWail, 3, 0.24f, 1f);
            Assert.That(player.StartedCount, Is.Zero);
            Assert.That(player.AlarmRepeatsRemaining, Is.Zero);
        }

        [Test]
        public void Alarm_RepeatsOnIntervalAndRestartsOnNewAlarm()
        {
            AddEntry(SfxId.AlarmMineWail, SfxBus.Alert, 1.8f, maxDuration: 0.55f, fadeOut: 0.12f);
            NewPlayer();
            player.PlayAlarmNow(SfxId.AlarmMineWail, 3, 0.24f, 1.3f);
            Assert.That(player.StartedCount, Is.EqualTo(1));
            Assert.That(player.AlarmRepeatsRemaining, Is.EqualTo(2));

            now += 0.25f;
            player.Tick();
            Assert.That(player.StartedCount, Is.EqualTo(2));
            Assert.That(player.AlarmRepeatsRemaining, Is.EqualTo(1));

            // 새 경보는 남은 반복을 끊고 처음부터 시작한다.
            player.PlayAlarmNow(SfxId.AlarmMineWail, 2, 0.34f, 1.05f);
            Assert.That(player.AlarmRepeatsRemaining, Is.EqualTo(1));
            now += 0.35f;
            player.Tick();
            Assert.That(player.AlarmRepeatsRemaining, Is.Zero);
            Assert.That(player.StartedCount, Is.EqualTo(4));
        }

        [Test]
        public void TrimmedVoice_EndsAtMaxDurationOnUnscaledClock()
        {
            AddEntry(SfxId.UiMineResetWhoosh, SfxBus.UI, 7.32f, startTime: 0.55f, maxDuration: 1.6f, fadeOut: 0.4f);
            NewPlayer();
            player.PlayNow(SfxId.UiMineResetWhoosh);
            Assert.That(player.ActivePoolVoiceCount, Is.EqualTo(1));
            now += 1.5f;
            player.Tick();
            Assert.That(player.ActivePoolVoiceCount, Is.EqualTo(1));
            now += 0.2f;
            player.Tick();
            Assert.That(player.ActivePoolVoiceCount, Is.Zero);
        }

        [Test]
        public void UiSfx_AttachTwice_KeepsOneComponentAndPlaysOncePerClick()
        {
            AddEntry(SfxId.UiClick, SfxBus.UI, 0.5f);
            NewPlayer();
            autoAdvance = true;
            var go = new GameObject("sfx-button", typeof(Button));
            created.Add(go);
            var button = go.GetComponent<Button>();
            UiSfx.Attach(button);
            UiSfx.Attach(button);
            Assert.That(go.GetComponents<UiSfxButton>().Length, Is.EqualTo(1));

            button.onClick.Invoke();
            Assert.That(player.StartedCount, Is.EqualTo(1));
        }

        [Test]
        public void UiSfx_DisabledButton_PlaysCancelFail()
        {
            AddEntry(SfxId.UiClick, SfxBus.UI, 0.5f);
            AddEntry(SfxId.UiCancelFail, SfxBus.UI, 0.5f);
            NewPlayer();
            var go = new GameObject("sfx-button", typeof(Button));
            created.Add(go);
            var button = go.GetComponent<Button>();
            var sfx = UiSfx.Attach(button);
            button.interactable = false;
            sfx.OnPointerClick(null);
            Assert.That(player.StartedCount, Is.EqualTo(1));
        }

        // ---- 재생 구간 / 라이브러리 ----

        [Test]
        public void Playback_TrimsLongClipsAndKeepsFadeInsideDuration()
        {
            var plan = SfxPlayback.Plan(7.32f, 0.55f, 1.6f, 0.4f, 1f);
            Assert.That(plan.StartTime, Is.EqualTo(0.55f).Within(0.0001f));
            Assert.That(plan.RealDuration, Is.EqualTo(1.6f).Within(0.0001f));
            Assert.That(plan.FadeOut, Is.EqualTo(0.4f).Within(0.0001f));

            var untouched = SfxPlayback.Plan(1.1f, 0f, 0f, 0f, 1f);
            Assert.That(untouched.RealDuration, Is.EqualTo(1.1f).Within(0.0001f));

            var limited = SfxPlayback.Plan(1.8f, 0f, 0.55f, 0.12f, 1.3f, 0.24f);
            Assert.That(limited.RealDuration, Is.EqualTo(0.24f).Within(0.0001f));
            Assert.That(limited.FadeOut, Is.LessThanOrEqualTo(0.12f));
            Assert.That(SfxPlayback.FadeGain(0.24f, 0.24f, 0.12f), Is.EqualTo(0f).Within(0.0001f));
            Assert.That(SfxPlayback.FadeGain(0.05f, 0.24f, 0.12f), Is.EqualTo(1f));
        }

        [Test]
        public void Defaults_HaveAllSlotsWithUniqueIds()
        {
            var defaults = SfxLibraryDefaults.Create();
            // B-156 15칸 + B-160 18칸(광물 튀어나옴 포함) + B-162 10칸 + B-163 6칸 + B-164 15칸.
            Assert.That(defaults.Count, Is.EqualTo(64));
            var ids = new System.Collections.Generic.HashSet<SfxId>();
            foreach (var pair in defaults) Assert.That(ids.Add(pair.Entry.Id), Is.True);
            // 3번(구 UiBack)은 비워 둔다. 이름으로 쓰면 Obsolete 경고가 나므로 번호로 확인한다.
            Assert.That(ids.Contains((SfxId)3), Is.False);
            Assert.That(ids.Contains(SfxId.UiSlotSelect), Is.True);
            Assert.That((int)SfxId.UiSlotSelect, Is.EqualTo(16));
        }

        [TestCase("Assets/_Project/Audio/SFX/UI/ui_mine_reset_whoosh.mp3", SfxId.UiMineResetWhoosh)]
        [TestCase("Assets/_Project/Audio/SFX/UI/ui_popup_close.mp3", SfxId.UiPopupClose)]
        [TestCase("Assets/_Project/Audio/SFX/World/world_holo_open.mp3", SfxId.WorldHoloOpen)]
        public void Defaults_PlayOnlyFrontPartOfClipsLongerThanTwoSeconds(string path, SfxId id)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip == null) Assert.Inconclusive("클립이 아직 import되지 않았다: " + path);
            Assert.That(clip.length, Is.GreaterThan(2f));
            foreach (var pair in SfxLibraryDefaults.Create())
            {
                if (pair.Entry.Id != id) continue;
                var plan = SfxPlayback.Plan(clip.length, pair.Entry.StartTime, pair.Entry.MaxDuration,
                    pair.Entry.FadeOut, pair.Entry.Pitch);
                Assert.That(plan.RealDuration, Is.LessThanOrEqualTo(2f));
                Assert.That(plan.FadeOut, Is.GreaterThan(0f));
                return;
            }

            Assert.Fail("기본값에 칸이 없다: " + id);
        }

        [Test]
        public void Builder_FillsEmptyClipsWithoutOverwritingTunedValues()
        {
            var tuned = new SfxEntry(SfxId.UiClick, SfxBus.UI, volume: 0.123f, pitch: 1.1f);
            library.EditorAddEntry(tuned);
            PromptB156SfxLibraryBuilder.AssignMissingClips(library);
            Assert.That(tuned.Volume, Is.EqualTo(0.123f));
            Assert.That(tuned.Pitch, Is.EqualTo(1.1f));
        }

        [Test]
        public void AddressDocument_ListsAllFifteenFileNames()
        {
            var path = Path.GetFullPath(Path.Combine(Application.dataPath, "../../init/sound-effect-address.md"));
            Assert.That(File.Exists(path), Is.True, path);
            var text = File.ReadAllText(path);
            foreach (var pair in SfxLibraryDefaults.Create())
            {
                Assert.That(text, Does.Contain("| " + pair.FileName + " |"), pair.FileName);
            }

            Assert.That(text, Does.Not.Contain("| ui_back |"));
            Assert.That(text, Does.Contain("| ui_slot_select |"));
        }
    }
}

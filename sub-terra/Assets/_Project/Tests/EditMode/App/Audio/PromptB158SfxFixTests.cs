using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using SubTerra.App.Audio;
using SubTerra.App.Editor.DataValidation;
using SubTerra.App.Integration;
using SubTerra.App.UI.EmergencyRescue;
using SubTerra.App.UI.MainMenu;
using SubTerra.App.UI.Outpost;
using SubTerra.App.UI.SurfaceBase;
using SubTerra.Gameplay.Structural;
using SubTerra.Shared.Audio;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace SubTerra.App.Tests.Audio
{
    /// <summary>B-158: 빈 칸 대체, 버튼 호버·클릭 연결, CCTV 닫힘, HUD 기준 위험도 경보.</summary>
    public sealed class PromptB158SfxFixTests
    {
        private const string OutpostPanelPrefabPath = "Assets/_Project/Prefabs/UI/OutpostPanel.prefab";
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

        private readonly List<Object> created = new List<Object>();
        private readonly List<SfxId> played = new List<SfxId>();
        private SfxLibrary library;
        private SfxPlayer player;
        private float now;

        [SetUp]
        public void SetUp()
        {
            now = 10f;
            played.Clear();
            library = ScriptableObject.CreateInstance<SfxLibrary>();
            created.Add(library);
            SfxRequests.PlayRequested += Record;
        }

        [TearDown]
        public void TearDown()
        {
            SfxRequests.PlayRequested -= Record;
            if (player != null)
            {
                player.ReleaseForTests();
                player = null;
            }

            for (var i = created.Count - 1; i >= 0; i--)
            {
                if (created[i] != null) Object.DestroyImmediate(created[i]);
            }

            created.Clear();
        }

        private void Record(SfxId id, float pitch) => played.Add(id);

        private int CountPlayed(SfxId id)
        {
            var count = 0;
            foreach (var value in played)
            {
                if (value == id) count++;
            }

            return count;
        }

        private AudioClip AddEntry(SfxId id, float seconds)
        {
            var entry = new SfxEntry(id, SfxBus.UI);
            AudioClip clip = null;
            if (seconds > 0f)
            {
                clip = AudioClip.Create("sfx-" + id, Mathf.CeilToInt(44100 * seconds), 1, 44100, false);
                created.Add(clip);
                entry.EditorSetClip(clip);
            }

            library.EditorAddEntry(entry);
            return clip;
        }

        private void NewPlayer()
        {
            player = SfxPlayer.CreateForTests(library, () => now, () => false);
        }

        private GameObject Instantiate(string path)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.That(prefab, Is.Not.Null, path);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            created.Add(instance);
            return instance;
        }

        private static void InvokeOnEnable(object target)
        {
            target.GetType().GetMethod("OnEnable", Private).Invoke(target, null);
        }

        private static Button ButtonField(object view, string name)
        {
            var field = view.GetType().GetField(name, Private);
            Assert.That(field, Is.Not.Null, name);
            return (Button)field.GetValue(view);
        }

        private static void AssertSfx(Button button, SfxId click, bool hover, bool failWhenDisabled, string label)
        {
            Assert.That(button, Is.Not.Null, label);
            var sfx = button.GetComponent<UiSfxButton>();
            Assert.That(sfx, Is.Not.Null, label);
            Assert.That(sfx.ClickSfx, Is.EqualTo(click), label);
            Assert.That(sfx.Hover, Is.EqualTo(hover), label);
            Assert.That(sfx.FailWhenDisabled, Is.EqualTo(failWhenDisabled), label);
            Assert.That(button.GetComponents<UiSfxButton>().Length, Is.EqualTo(1), label);
        }

        // ---- 빈 칸 대체 ----

        [Test]
        public void EmptySlotSelect_PlaysUiClickOnceWithoutWarning()
        {
            var click = AddEntry(SfxId.UiClick, 0.5f);
            AddEntry(SfxId.UiSlotSelect, 0f);
            NewPlayer();

            player.PlayNow(SfxId.UiSlotSelect);

            Assert.That(player.StartedCount, Is.EqualTo(1));
            Assert.That(PlayingClips(), Is.EqualTo(new[] { click }));
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void FilledSlotSelect_PlaysItsOwnClip()
        {
            AddEntry(SfxId.UiClick, 0.5f);
            var own = AddEntry(SfxId.UiSlotSelect, 0.3f);
            NewPlayer();

            player.PlayNow(SfxId.UiSlotSelect);

            Assert.That(player.StartedCount, Is.EqualTo(1));
            Assert.That(PlayingClips(), Is.EqualTo(new[] { own }));
        }

        [Test]
        public void FallbackTable_OnlyMapsSlotSelectToClick()
        {
            Assert.That(SfxLibraryDefaults.Fallbacks.Count, Is.EqualTo(1));
            Assert.That(SfxLibraryDefaults.Fallbacks[SfxId.UiSlotSelect], Is.EqualTo(SfxId.UiClick));
        }

        private List<AudioClip> PlayingClips()
        {
            var clips = new List<AudioClip>();
            foreach (var source in player.GetComponents<AudioSource>())
            {
                if (source.clip != null) clips.Add(source.clip);
            }

            return clips;
        }

        // ---- UiSfx.Attach ----

        [Test]
        public void Attach_FailWhenDisabledFalse_IsSilentOnDisabledClick_DefaultPlaysCancelFail()
        {
            var silentGo = new GameObject("silent", typeof(Button));
            var defaultGo = new GameObject("default", typeof(Button));
            created.Add(silentGo);
            created.Add(defaultGo);
            var silent = UiSfx.Attach(silentGo.GetComponent<Button>(), SfxId.None, true, failWhenDisabled: false);
            var fallback = UiSfx.Attach(defaultGo.GetComponent<Button>(), SfxId.None, true);
            silentGo.GetComponent<Button>().interactable = false;
            defaultGo.GetComponent<Button>().interactable = false;

            silent.OnPointerClick(null);
            Assert.That(played, Is.Empty);

            fallback.OnPointerClick(null);
            Assert.That(played, Is.EqualTo(new[] { SfxId.UiCancelFail }));
        }

        // ---- 화면별 버튼 연결 ----

        [Test]
        public void MainMenuView_AttachesHoverAndClickPerButton_SlotsUntouched()
        {
            var instance = Instantiate(PromptB150MainMenuBuilder.PrefabPath);
            var view = instance.GetComponent<MainMenuView>();
            InvokeOnEnable(view);
            InvokeOnEnable(view);

            AssertSfx(ButtonField(view, "settingsButton"), SfxId.UiClick, true, true, "settings");
            // B-170: 종료 확인 창 등장음이 클릭음을 대신한다.
            AssertSfx(ButtonField(view, "quitButton"), SfxId.None, true, true, "quit");
            AssertSfx(ButtonField(view, "continueButton"), SfxId.None, true, true, "continue");
            AssertSfx(ButtonField(view, "newGameButton"), SfxId.None, true, true, "newGame");
            AssertSfx(ButtonField(view, "overwriteConfirmButton"), SfxId.None, true, true, "overwriteConfirm");
            AssertSfx(ButtonField(view, "overwriteCancelButton"), SfxId.None, true, true, "overwriteCancel");
            AssertSfx(ButtonField(view, "overwriteCloseButton"), SfxId.None, true, true, "overwriteClose");

            var slots = (Button[])typeof(MainMenuView).GetField("slotButtons", Private).GetValue(view);
            foreach (var slot in slots)
            {
                Assert.That(slot.GetComponent<UiSfxButton>(), Is.Null, slot.name);
            }

            typeof(MainMenuView).GetMethod("OnDisable", Private).Invoke(view, null);
        }

        [Test]
        public void SurfaceBaseView_ResetMineButtonsHoverOnly_ConfirmSilentWhenDisabled()
        {
            var instance = Instantiate(MineResetSurfaceBaseLayoutBuilder.SurfaceBasePrefabPath);
            var view = instance.GetComponent<SurfaceBaseView>();
            InvokeOnEnable(view);

            AssertSfx(ButtonField(view, "resetMineButton"), SfxId.None, true, true, "resetMine");
            AssertSfx(ButtonField(view, "resetMineConfirmYesButton"), SfxId.None, true, false, "resetMineYes");
            AssertSfx(ButtonField(view, "resetMineConfirmNoButton"), SfxId.None, true, true, "resetMineNo");

            typeof(SurfaceBaseView).GetMethod("OnDisable", Private).Invoke(view, null);
        }

        [Test]
        public void ServiceAndCctvPopups_CloseButtonsHoverOnly()
        {
            var view = Instantiate(OutpostPanelPrefabPath).GetComponent<OutpostPanelView>();
            // Show 없이 켜지면 팝업은 스스로 다시 꺼지지만, 버튼 연결은 그 전에 끝난다.
            InvokeOnEnable(view.ServicePopup);
            InvokeOnEnable(view.CorePopup);

            AssertSfx(view.ServiceCloseButton, SfxId.None, true, true, "service close");
            AssertSfx(view.CoreCloseButton, SfxId.None, true, true, "cctv close");
        }

        [Test]
        public void EmergencyRescue_RescueConfirmsAndCloseHoversOnly_SurvivesBind()
        {
            var canvas = new GameObject("B158Canvas", typeof(RectTransform), typeof(Canvas));
            created.Add(canvas);
            var view = EmergencyRescuePanelView.Create(canvas.transform, null);
            var rescued = 0;
            var closed = 0;
            view.Bind(() => rescued++, () => closed++, null);

            AssertSfx(view.Popup.RescueButton, SfxId.UiConfirm, true, true, "rescue");
            AssertSfx(view.Popup.CloseButton, SfxId.None, true, true, "close");

            view.Popup.RescueButton.onClick.Invoke();
            view.Popup.CloseButton.onClick.Invoke();
            Assert.That(rescued, Is.EqualTo(1));
            Assert.That(closed, Is.EqualTo(1));
            Assert.That(played, Is.EqualTo(new[] { SfxId.UiConfirm }));
        }

        // ---- CCTV 닫힘 ----

        [Test]
        public void CctvPopup_HidePlaysPopupCloseOnce_EvenWhenCalledTwice()
        {
            var view = Instantiate(OutpostPanelPrefabPath).GetComponent<OutpostPanelView>();
            view.SetMode(OutpostPanelMode.Core);
            view.SetVisible(true);
            var popup = view.CorePopup;
            Assert.That(popup.IsShown, Is.True);
            played.Clear();

            popup.Hide();
            popup.Hide();
            popup.HideImmediate();

            Assert.That(CountPlayed(SfxId.UiPopupClose), Is.EqualTo(1));
        }

        // ---- 위험도 경보 ----

        [TestCase(StructuralRiskLevel.Caution)]
        [TestCase(StructuralRiskLevel.Danger)]
        [TestCase(StructuralRiskLevel.CollapseImminent)]
        public void AlarmGate_RisingFromStable_Alarms(StructuralRiskLevel next)
        {
            var gate = new StructuralAlarmGate(() => now);
            Assert.That(gate.Evaluate(StructuralRiskLevel.Stable, next), Is.EqualTo(StructuralRiskFeedback.RiskSound.Alarm));
        }

        [Test]
        public void AlarmGate_SameLevelWithinCooldown_IsSilent_AfterCooldownAlarmsAgain()
        {
            var gate = new StructuralAlarmGate(() => now);
            Assert.That(gate.Evaluate(StructuralRiskLevel.Stable, StructuralRiskLevel.Caution),
                Is.EqualTo(StructuralRiskFeedback.RiskSound.Alarm));
            now += 0.5f;
            gate.Evaluate(StructuralRiskLevel.Caution, StructuralRiskLevel.Stable);
            now += 1f;
            Assert.That(gate.Evaluate(StructuralRiskLevel.Stable, StructuralRiskLevel.Caution),
                Is.EqualTo(StructuralRiskFeedback.RiskSound.None));

            // 단계마다 따로 잰다: 다른 단계는 바로 울린다.
            Assert.That(gate.Evaluate(StructuralRiskLevel.Caution, StructuralRiskLevel.Danger),
                Is.EqualTo(StructuralRiskFeedback.RiskSound.Alarm));

            now += StructuralAlarmGate.RepeatCooldownSeconds;
            Assert.That(gate.Evaluate(StructuralRiskLevel.Stable, StructuralRiskLevel.Caution),
                Is.EqualTo(StructuralRiskFeedback.RiskSound.Alarm));
        }

        [TestCase(StructuralRiskLevel.Danger)]
        [TestCase(StructuralRiskLevel.Caution)]
        [TestCase(StructuralRiskLevel.Stable)]
        public void AlarmGate_FallingFromCollapseImminent_HasNoTick(StructuralRiskLevel next)
        {
            var gate = new StructuralAlarmGate(() => now);
            Assert.That(gate.Evaluate(StructuralRiskLevel.CollapseImminent, next),
                Is.EqualTo(StructuralRiskFeedback.RiskSound.None));
        }

        [Test]
        public void AlarmGate_DangerToCaution_Ticks_RepeatedTickWithinCooldownIsSilent()
        {
            var gate = new StructuralAlarmGate(() => now);
            Assert.That(gate.Evaluate(StructuralRiskLevel.Danger, StructuralRiskLevel.Caution),
                Is.EqualTo(StructuralRiskFeedback.RiskSound.Tick));
            now += 1f;
            Assert.That(gate.Evaluate(StructuralRiskLevel.Caution, StructuralRiskLevel.Stable),
                Is.EqualTo(StructuralRiskFeedback.RiskSound.None));
            now += StructuralAlarmGate.RepeatCooldownSeconds;
            Assert.That(gate.Evaluate(StructuralRiskLevel.Caution, StructuralRiskLevel.Stable),
                Is.EqualTo(StructuralRiskFeedback.RiskSound.Tick));
        }

        [Test]
        public void RiskFeedback_NoLongerPlaysAlarm()
        {
            var path = Path.Combine(Application.dataPath, "_Project/Scripts/Gameplay/Structural/StructuralRiskFeedback.cs");
            var source = File.ReadAllText(path);
            Assert.That(source, Does.Not.Contain("SfxRequests.PlayAlarm"));
            // B-168: 천장 붕괴음만 여기서 낸다. 위험 경보는 계속 GameplayHazardStatusBridge 몫이다.
            Assert.That(source.Replace("SfxRequests.Play(SfxId.CeilingCollapse)", string.Empty),
                Does.Not.Contain("SfxRequests.Play("));
        }
    }
}

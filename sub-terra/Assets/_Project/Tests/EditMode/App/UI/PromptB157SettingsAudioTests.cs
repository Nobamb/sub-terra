using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using SubTerra.App.Audio;
using SubTerra.App.Editor.DataValidation;
using SubTerra.App.UI.MainMenu;
using SubTerra.App.UI.SurfaceBase;
using SubTerra.Shared.Audio;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.Tests.UI
{
    public sealed class PromptB157SettingsAudioTests
    {
        private static readonly string[] PrefKeys =
        {
            "subterra.settings.masterVolume",
            SettingsRuntimeApplier.PrefMasterMuted,
            SettingsRuntimeApplier.PrefBgmVolume,
            SettingsRuntimeApplier.PrefSfxVolume,
            SettingsRuntimeApplier.PrefBgmMuted,
            SettingsRuntimeApplier.PrefSfxMuted
        };

        private readonly Dictionary<string, float> savedPrefs = new Dictionary<string, float>();
        private readonly List<Object> created = new List<Object>();
        private readonly List<SfxPlayer> players = new List<SfxPlayer>();
        private float oldListenerVolume;

        [SetUp]
        public void SetUp()
        {
            savedPrefs.Clear();
            foreach (var key in PrefKeys)
            {
                if (PlayerPrefs.HasKey(key)) savedPrefs[key] = PlayerPrefs.GetFloat(key, PlayerPrefs.GetInt(key, 0));
                PlayerPrefs.DeleteKey(key);
            }

            oldListenerVolume = AudioListener.volume;
            SettingsRuntimeApplier.RestoreAppliedVolume(SettingsValues.CreateDefaults());
            BgmVolumeApplier.ResetBaselinesForTests();
        }

        [TearDown]
        public void TearDown()
        {
            for (var i = 0; i < players.Count; i++)
            {
                if (players[i] != null) players[i].ReleaseForTests();
            }

            players.Clear();
            for (var i = 0; i < created.Count; i++)
            {
                if (created[i] != null) Object.DestroyImmediate(created[i]);
            }

            created.Clear();
            foreach (var key in PrefKeys)
            {
                if (!savedPrefs.TryGetValue(key, out var value))
                {
                    PlayerPrefs.DeleteKey(key);
                    continue;
                }

                if (key.EndsWith("Muted")) PlayerPrefs.SetInt(key, (int)value);
                else PlayerPrefs.SetFloat(key, value);
            }

            PlayerPrefs.Save();
            SettingsRuntimeApplier.RestoreAppliedVolume(SettingsRuntimeApplier.LoadOrDefaults());
            AudioListener.volume = oldListenerVolume;
            BgmVolumeApplier.ResetBaselinesForTests();
        }

        private GameObject Spawn(string path)
        {
            var instance = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path));
            created.Add(instance);
            return instance;
        }

        private static SettingsMenuSkin EnableSkin(GameObject instance)
        {
            var skin = instance.transform.Find("SettingsPanel").GetComponent<SettingsMenuSkin>();
            // EditMode에서는 활성 콜백을 테스트가 직접 실행한다.
            typeof(SettingsMenuSkin).GetMethod("OnEnable", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(skin, null);
            return skin;
        }

        private static Slider MasterSlider(SettingsMenuSkin skin) =>
            (Slider)new SerializedObject(skin).FindProperty("volumeSlider").objectReferenceValue;

        private AudioSource NewBgmSource(string sourceName, float volume)
        {
            var go = new GameObject(sourceName);
            created.Add(go);
            var source = go.AddComponent<AudioSource>();
            source.volume = volume;
            return source;
        }

        // ---- 값과 저장 ----

        [Test]
        public void NewKeysMissing_ChannelsAreFullAndMasterKeepsSavedValue()
        {
            PlayerPrefs.SetFloat("subterra.settings.masterVolume", 0.3f);
            var values = SettingsRuntimeApplier.LoadOrDefaults();
            Assert.That(values.BgmVolume, Is.EqualTo(1f));
            Assert.That(values.SfxVolume, Is.EqualTo(1f));
            Assert.That(values.MasterVolume, Is.EqualTo(0.3f));
            Assert.That(SettingsRuntimeApplier.IsMuted(AudioChannel.Bgm), Is.False);
            Assert.That(SettingsRuntimeApplier.IsMuted(AudioChannel.Sfx), Is.False);
            Assert.That(SettingsValues.CreateDefaults().MasterVolume, Is.EqualTo(0.5f));
        }

        [Test]
        public void SaveThenLoad_KeepsThreeVolumesAndTwoMutes()
        {
            var values = SettingsValues.CreateDefaults();
            values.MasterVolume = 0.4f;
            values.BgmVolume = 0.25f;
            values.SfxVolume = 0.75f;
            SettingsRuntimeApplier.Save(values);
            SettingsRuntimeApplier.SetMuted(AudioChannel.Bgm, true, 0.25f);
            SettingsRuntimeApplier.SetMuted(AudioChannel.Sfx, true, 0.75f);

            var loaded = SettingsRuntimeApplier.LoadOrDefaults();
            Assert.That(loaded.MasterVolume, Is.EqualTo(0.4f));
            Assert.That(loaded.BgmVolume, Is.EqualTo(0.25f));
            Assert.That(loaded.SfxVolume, Is.EqualTo(0.75f));
            Assert.That(SettingsRuntimeApplier.IsMuted(AudioChannel.Bgm), Is.True);
            Assert.That(SettingsRuntimeApplier.IsMuted(AudioChannel.Sfx), Is.True);
        }

        [Test]
        public void CloneAndCopy_CarryChannelVolumes()
        {
            var values = SettingsValues.CreateDefaults();
            values.BgmVolume = 0.2f;
            values.SfxVolume = 0.6f;
            var clone = values.Clone();
            Assert.That(clone.BgmVolume, Is.EqualTo(0.2f));
            Assert.That(clone.SfxVolume, Is.EqualTo(0.6f));
            var target = SettingsValues.CreateDefaults();
            target.CopyFrom(values);
            Assert.That(target.BgmVolume, Is.EqualTo(0.2f));
            Assert.That(target.SfxVolume, Is.EqualTo(0.6f));
        }

        [Test]
        public void PreviewThenRestore_ReturnsToAppliedValues()
        {
            var applied = SettingsValues.CreateDefaults();
            applied.BgmVolume = 0.8f;
            applied.SfxVolume = 0.9f;
            SettingsRuntimeApplier.RestoreAppliedVolume(applied);
            SettingsRuntimeApplier.PreviewVolume(AudioChannel.Bgm, 0.2f);
            SettingsRuntimeApplier.PreviewVolume(AudioChannel.Sfx, 0.1f);
            Assert.That(SettingsRuntimeApplier.EffectiveVolume(AudioChannel.Bgm), Is.EqualTo(0.2f));
            Assert.That(SettingsRuntimeApplier.EffectiveVolume(AudioChannel.Sfx), Is.EqualTo(0.1f));

            SettingsRuntimeApplier.RestoreAppliedVolume(applied);
            Assert.That(SettingsRuntimeApplier.EffectiveVolume(AudioChannel.Bgm), Is.EqualTo(0.8f));
            Assert.That(SettingsRuntimeApplier.EffectiveVolume(AudioChannel.Sfx), Is.EqualTo(0.9f));
        }

        // ---- 줄과 카드 ----

        [TestCase(PromptB104SettingsMenuBuilder.MainPrefab)]
        [TestCase(PromptB104SettingsMenuBuilder.SurfacePrefab)]
        public void Rows_AreCreatedOnceInOrderInsideTheGrownCard(string path)
        {
            var instance = Spawn(path);
            var skin = EnableSkin(instance);
            EnableSkin(instance);
            var card = instance.transform.Find("SettingsPanel/SettingsCard") as RectTransform;
            Assert.That(card, Is.Not.Null);

            foreach (var name in new[] { "BgmVolumeSlider", "SfxVolumeSlider", "BgmVolumeCaption", "SfxVolumeCaption",
                         "BgmVolumeLabel", "SfxVolumeLabel" })
            {
                Assert.That(CountChildren(card, name), Is.EqualTo(1), name);
            }

            Assert.That(card.sizeDelta.y, Is.EqualTo(810f + SettingsAudioRows.CardHeightGrowth));
            Assert.That(card.Find(SettingsAudioRows.StretchedArtName).GetComponent<Image>().type, Is.EqualTo(Image.Type.Sliced));
            Assert.That(card.GetChild(0).name, Is.EqualTo(SettingsAudioRows.StretchedArtName));

            var master = MasterSlider(skin);
            var bgm = card.Find("BgmVolumeSlider").GetComponent<Slider>();
            var sfx = card.Find("SfxVolumeSlider").GetComponent<Slider>();
            Assert.That(master.transform.Find("MasterMuteButton"), Is.Not.Null);
            Assert.That(bgm.transform.Find("BgmMuteButton"), Is.Not.Null);
            Assert.That(sfx.transform.Find("SfxMuteButton"), Is.Not.Null);
            Assert.That(bgm.transform.Find("MasterMuteButton"), Is.Null);

            var masterY = ((RectTransform)master.transform).anchoredPosition.y;
            var bgmY = ((RectTransform)bgm.transform).anchoredPosition.y;
            var sfxY = ((RectTransform)sfx.transform).anchoredPosition.y;
            Assert.That(masterY - bgmY, Is.EqualTo(SettingsAudioRows.RowSpacing));
            Assert.That(bgmY - sfxY, Is.EqualTo(SettingsAudioRows.RowSpacing));
            Assert.That(((RectTransform)bgm.transform).rect.height, Is.LessThan(SettingsAudioRows.RowSpacing));

            foreach (var slider in new[] { master, bgm, sfx })
            {
                var corners = new Vector3[4];
                ((RectTransform)slider.transform).GetWorldCorners(corners);
                foreach (var corner in corners)
                    Assert.That(card.rect.Contains(card.InverseTransformPoint(corner)), Is.True, slider.name);
            }
        }

        [TestCase(PromptB104SettingsMenuBuilder.MainPrefab)]
        [TestCase(PromptB104SettingsMenuBuilder.SurfacePrefab)]
        public void ExistingBlocks_KeepTheirSpacingFromTheirCardEdge(string path)
        {
            var instance = Spawn(path);
            var card = instance.transform.Find("SettingsPanel/SettingsCard") as RectTransform;
            float title = ((RectTransform)card.Find("SettingsTitle")).anchoredPosition.y;
            float footer = ((RectTransform)card.Find("SettingsApply")).anchoredPosition.y;
            float display = ((RectTransform)card.Find("DisplayTitle")).anchoredPosition.y;
            EnableSkin(instance);
            Assert.That(((RectTransform)card.Find("SettingsTitle")).anchoredPosition.y,
                Is.EqualTo(title + SettingsAudioRows.RowSpacing));
            Assert.That(((RectTransform)card.Find("SettingsApply")).anchoredPosition.y,
                Is.EqualTo(footer - SettingsAudioRows.RowSpacing));
            Assert.That(((RectTransform)card.Find("DisplayTitle")).anchoredPosition.y,
                Is.EqualTo(display - SettingsAudioRows.RowSpacing));
            Assert.That(((RectTransform)card.Find("SfxVolumeSlider")).anchoredPosition.y,
                Is.GreaterThan(display));
        }

        [TestCase(PromptB104SettingsMenuBuilder.MainPrefab)]
        [TestCase(PromptB104SettingsMenuBuilder.SurfacePrefab)]
        public void ViewDraft_RoundTripsThreeVolumes(string path)
        {
            var instance = Spawn(path);
            var main = instance.GetComponent<MainMenuView>();
            var surface = instance.GetComponent<SurfaceBaseView>();
            Component view = main != null ? (Component)main : surface;
            view.GetType().GetMethod("OnEnable", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(view, null);
            EnableSkin(instance);

            var draft = SettingsValues.CreateDefaults();
            draft.MasterVolume = 0.35f;
            draft.BgmVolume = 0.45f;
            draft.SfxVolume = 0.55f;
            if (main != null) main.SetSettingsDraft(draft); else surface.SetSettingsDraft(draft);
            var read = main != null ? main.ReadSettingsDraft(null) : surface.ReadSettingsDraft(null);
            Assert.That(read.MasterVolume, Is.EqualTo(0.35f));
            Assert.That(read.BgmVolume, Is.EqualTo(0.45f));
            Assert.That(read.SfxVolume, Is.EqualTo(0.55f));

            var card = instance.transform.Find("SettingsPanel/SettingsCard");
            Assert.That(card.Find("BgmVolumeLabel").GetComponent<TMPro.TMP_Text>().text, Is.EqualTo("45%"));
            Assert.That(card.Find("SfxVolumeLabel").GetComponent<TMPro.TMP_Text>().text, Is.EqualTo("55%"));
        }

        // ---- 확성기 ----

        [TestCase(PromptB104SettingsMenuBuilder.MainPrefab, AudioChannel.Bgm)]
        [TestCase(PromptB104SettingsMenuBuilder.SurfacePrefab, AudioChannel.Bgm)]
        [TestCase(PromptB104SettingsMenuBuilder.MainPrefab, AudioChannel.Sfx)]
        [TestCase(PromptB104SettingsMenuBuilder.SurfacePrefab, AudioChannel.Sfx)]
        public void RowMute_KeepsValueRestoresAndOnlyAffectsItsChannel(string path, AudioChannel channel)
        {
            var instance = Spawn(path);
            var skin = EnableSkin(instance);
            var card = instance.transform.Find("SettingsPanel/SettingsCard");
            var slider = card.Find(channel + "VolumeSlider").GetComponent<Slider>();
            var other = channel == AudioChannel.Bgm ? AudioChannel.Sfx : AudioChannel.Bgm;
            var icon = slider.transform.Find(channel + "MuteButton").GetComponent<SettingsMuteButton>();
            var masterIcon = MasterSlider(skin).transform.Find("MasterMuteButton").GetComponent<SettingsMuteButton>();
            Assert.That(SettingsMuteButton.Ensure(slider, channel), Is.SameAs(icon));

            slider.value = 0.4f;
            Assert.That(SettingsRuntimeApplier.EffectiveVolume(channel), Is.EqualTo(0.4f));

            icon.Button.onClick.Invoke();
            Assert.That(icon.ShowsMuted, Is.True);
            Assert.That(slider.value, Is.EqualTo(0.4f));
            Assert.That(SettingsRuntimeApplier.EffectiveVolume(channel), Is.Zero);
            Assert.That(SettingsRuntimeApplier.IsMuted(other), Is.False);
            Assert.That(SettingsRuntimeApplier.IsMasterMuted, Is.False);
            Assert.That(masterIcon.ShowsMuted, Is.False);

            icon.Button.onClick.Invoke();
            Assert.That(icon.ShowsMuted, Is.False);
            Assert.That(SettingsRuntimeApplier.EffectiveVolume(channel), Is.EqualTo(0.4f));

            icon.Button.onClick.Invoke();
            slider.value = 0.7f;
            Assert.That(SettingsRuntimeApplier.IsMuted(channel), Is.False);
            Assert.That(icon.ShowsMuted, Is.False);
            Assert.That(SettingsRuntimeApplier.EffectiveVolume(channel), Is.EqualTo(0.7f));

            slider.value = 0f;
            Assert.That(icon.ShowsMuted, Is.True);
        }

        // ---- 실제 소리 ----

        [Test]
        public void BgmChannel_ScalesOnlyBgmSourcesFromTheirFirstSeenVolume()
        {
            var title = NewBgmSource("BGM_Title", 1f);
            var quiet = NewBgmSource("BGM_Quiet", 0.5f);
            var other = NewBgmSource("Ambience", 1f);

            SettingsRuntimeApplier.PreviewVolume(AudioChannel.Bgm, 0.5f);
            BgmVolumeApplier.ApplyNow();
            Assert.That(title.volume, Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(quiet.volume, Is.EqualTo(0.25f).Within(0.0001f));
            Assert.That(other.volume, Is.EqualTo(1f));

            SettingsRuntimeApplier.SetMuted(AudioChannel.Bgm, true, 0.5f);
            BgmVolumeApplier.ApplyNow();
            Assert.That(title.volume, Is.Zero);

            SettingsRuntimeApplier.SetMuted(AudioChannel.Bgm, false, 1f);
            BgmVolumeApplier.ApplyNow();
            Assert.That(title.volume, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(quiet.volume, Is.EqualTo(0.5f).Within(0.0001f));
        }

        private sealed class SfxFixture
        {
            public SfxLibrary Library;
            public SfxPlayer Player;
            public AudioClip Clip;
        }

        private SfxFixture NewSfx(float entryVolume)
        {
            var fixture = new SfxFixture { Library = ScriptableObject.CreateInstance<SfxLibrary>() };
            fixture.Clip = AudioClip.Create("b157-sfx", 44100, 1, 44100, false);
            created.Add(fixture.Library);
            created.Add(fixture.Clip);
            var entry = new SfxEntry(SfxId.UiClick, SfxBus.UI, entryVolume, 1f);
            entry.EditorSetClip(fixture.Clip);
            fixture.Library.EditorAddEntry(entry);
            float now = 10f;
            // 음소거 판정은 주입하지 않아 SfxPlayer 기본값(마스터 또는 효과음 음소거)을 그대로 쓴다.
            fixture.Player = SfxPlayer.CreateForTests(fixture.Library, () => now, null);
            players.Add(fixture.Player);
            return fixture;
        }

        private static AudioSource PlayingSource(SfxPlayer player)
        {
            foreach (var source in player.GetComponents<AudioSource>())
            {
                if (source.clip != null) return source;
            }

            return null;
        }

        [Test]
        public void SfxChannel_MultipliesEntryAndBusVolume()
        {
            var fixture = NewSfx(0.8f);
            SettingsRuntimeApplier.PreviewVolume(AudioChannel.Sfx, 0.5f);
            fixture.Player.PlayNow(SfxId.UiClick);
            var source = PlayingSource(fixture.Player);
            Assert.That(source, Is.Not.Null);
            float bus = fixture.Library.GetBusVolume(SfxBus.UI);
            Assert.That(source.volume, Is.EqualTo(0.8f * bus * 0.5f).Within(0.0001f));

            SettingsRuntimeApplier.PreviewVolume(AudioChannel.Sfx, 0.25f);
            fixture.Player.Tick();
            Assert.That(PlayingSource(fixture.Player).volume, Is.EqualTo(0.8f * bus * 0.25f).Within(0.0001f));
        }

        [Test]
        public void SfxMute_SilencesSfxButNotBgm_AndBgmMuteKeepsSfx()
        {
            var bgm = NewBgmSource("BGM_Underground", 1f);
            var fixture = NewSfx(1f);

            SettingsRuntimeApplier.SetMuted(AudioChannel.Sfx, true, 1f);
            BgmVolumeApplier.ApplyNow();
            fixture.Player.PlayNow(SfxId.UiClick);
            Assert.That(fixture.Player.StartedCount, Is.Zero);
            Assert.That(bgm.volume, Is.EqualTo(1f));

            SettingsRuntimeApplier.SetMuted(AudioChannel.Sfx, false, 1f);
            SettingsRuntimeApplier.SetMuted(AudioChannel.Bgm, true, 1f);
            BgmVolumeApplier.ApplyNow();
            fixture.Player.PlayNow(SfxId.UiClick);
            Assert.That(fixture.Player.StartedCount, Is.EqualTo(1));
            Assert.That(bgm.volume, Is.Zero);
        }

        [Test]
        public void MasterMute_ZeroesAudioListener()
        {
            SettingsRuntimeApplier.SetMasterMuted(true, 0.6f);
            Assert.That(AudioListener.volume, Is.Zero);
            Assert.That(SettingsRuntimeApplier.IsMuted(AudioChannel.Bgm), Is.False);
            Assert.That(SettingsRuntimeApplier.IsMuted(AudioChannel.Sfx), Is.False);
        }

        private static int CountChildren(Transform parent, string childName)
        {
            var count = 0;
            for (var i = 0; i < parent.childCount; i++)
            {
                if (parent.GetChild(i).name == childName) count++;
            }

            return count;
        }
    }
}

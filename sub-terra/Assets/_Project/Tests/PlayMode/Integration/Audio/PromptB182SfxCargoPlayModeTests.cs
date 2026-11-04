using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using SubTerra.App.Audio;
using SubTerra.App.Tutorial;
using SubTerra.App.UI.Tutorial;
using SubTerra.Shared.Audio;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SubTerra.Integration.Tests.Audio
{
    public sealed class PromptB182SfxCargoPlayModeTests
    {
        private readonly List<SfxId> played = new List<SfxId>();
        private GameObject root;
        private CargoFullPopupView full;
        private CargoDiscardPopupView dump;

        [SetUp]
        public void SetUp()
        {
            played.Clear();
            SfxRequests.PlayRequested += Record;
            WindowTvSfx.ResetForTests(Resources.Load<SfxLibrary>(SfxLibrary.ResourcePath), null, () => false);
            root = new GameObject("B182RuntimeCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            var panel = Panel("CargoFull");
            full = panel.AddComponent<CargoFullPopupView>();
            full.Configure(Label(panel.transform, "CapacityTitle"), Label(panel.transform, "CapacityBody"),
                Button(panel.transform, "CapacityDumpButton"), Button(panel.transform, "CapacityForfeitButton"));
            panel.SetActive(false);
            panel = Panel("CargoDiscard");
            dump = panel.AddComponent<CargoDiscardPopupView>();
            panel.SetActive(false);
        }

        [TearDown]
        public void TearDown()
        {
            SfxRequests.PlayRequested -= Record;
            WindowTvSfx.ReleaseForTests();
            if (root != null) Object.DestroyImmediate(root);
        }

        private void Record(SfxId id, float pitch) => played.Add(id);
        private GameObject Panel(string name)
        {
            var panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(root.transform, false);
            return panel;
        }

        private static TMP_Text Label(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            return go.GetComponent<TMP_Text>();
        }

        private static Button Button(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            Label(go.transform, "Label");
            return go.GetComponent<Button>();
        }

        private static IEnumerator SaveScreen(string name)
        {
            yield return new WaitForEndOfFrame();
            var texture = ScreenCapture.CaptureScreenshotAsTexture();
            var scaled = RenderTexture.GetTemporary(Mathf.Max(1, texture.width / 2), Mathf.Max(1, texture.height / 2));
            var previous = RenderTexture.active;
            Graphics.Blit(texture, scaled);
            RenderTexture.active = scaled;
            var small = new Texture2D(scaled.width, scaled.height, TextureFormat.RGB24, false);
            small.ReadPixels(new Rect(0, 0, scaled.width, scaled.height), 0, 0);
            small.Apply();
            Directory.CreateDirectory("Temp/b182-play-screens");
            File.WriteAllBytes("Temp/b182-play-screens/" + name + ".png", small.EncodeToPNG());
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(scaled);
            Object.Destroy(texture);
            Object.Destroy(small);
        }

        [UnityTest]
        public IEnumerator RuntimeOpen_AutoUpdate_OverflowOnce_PageAndCleanupQuiet()
        {
            full.Render(CargoShortfallModel.From(170f, 170f, 9.5f), new QuestReward(3, 3, 0, 200));
            full.Show(false);
            Assert.That(WindowTvSfx.StartedCount, Is.EqualTo(1));
            Assert.That(WindowTvSfx.LastVolumeScale, Is.EqualTo(0.6f));
            Assert.That(WindowTvSfx.Source.clip, Is.Not.Null);
            Assert.That(WindowTvSfx.Source.ignoreListenerPause, Is.True);
            yield return new WaitUntil(() => full.State == CargoFullPopupView.PopupState.Open);
            Assert.That(played, Is.EqualTo(new[] { SfxId.CargoHeavyWarning }));
            yield return SaveScreen("cargo-full");
            full.HideImmediate();
            full.Show(true);
            yield return new WaitUntil(() => full.State == CargoFullPopupView.PopupState.Open);
            Assert.That(WindowTvSfx.StartedCount, Is.EqualTo(1));
            Assert.That(played, Has.Count.EqualTo(1));
            full.HideImmediate();
            Assert.That(WindowTvSfx.StartedCount, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator RuntimeComplete_AutoUpdate_FoldsOnceAfterHold()
        {
            dump.Render(CargoShortfallModel.From(170f, 170f, 9.5f), new QuestReward(3, 3, 0, 200),
                new[] { new QuestDumpRow("mineral.copper", "구리", 80, 1.5f),
                    new QuestDumpRow("mineral.iron", "철", 25, 2f), new QuestDumpRow("mineral.lithium", "리튬", 0, 0.8f) });
            dump.Show(false);
            yield return new WaitUntil(() => dump.State == CargoDiscardPopupView.PopupState.Open);
            yield return SaveScreen("cargo-discard");
            dump.Render(CargoShortfallModel.From(159.5f, 170f, 9.5f), new QuestReward(3, 3, 0, 200),
                new[] { new QuestDumpRow("mineral.copper", "구리", 73, 1.5f) });
            dump.Close(CargoDiscardPopupView.CloseMode.Complete);
            yield return new WaitForSecondsRealtime(dump.Timing.gaugeTweenDuration + 0.05f);
            Assert.That(dump.ShortfallString, Is.EqualTo(CargoDiscardText.Cleared));
            yield return SaveScreen("cargo-complete");
            Assert.That(WindowTvSfx.LastId, Is.EqualTo(SfxId.WindowTvOpen));
            yield return new WaitUntil(() => dump.State == CargoDiscardPopupView.PopupState.Hidden);
            Assert.That(WindowTvSfx.StartedCount, Is.EqualTo(2));
            Assert.That(WindowTvSfx.LastId, Is.EqualTo(SfxId.WindowTvClose));
            Assert.That(WindowTvSfx.LastVolumeScale, Is.EqualTo(0.6f));
        }

        [UnityTest]
        public IEnumerator ManualTickPreview_AndQuietRestoration_AreSilentInPlayMode()
        {
            full.ManualTick = true;
            full.Show(false);
            full.Tick(2f);
            full.DumpButton.GetComponent<UiSfxButton>().OnPointerEnter(null);
            full.HideImmediate();
            full.ManualTick = false;
            full.SfxQuiet = true;
            full.Show(false);
            full.SfxQuiet = false;
            full.Tick(2f);
            dump.ManualTick = true;
            dump.Show(false);
            dump.Tick(2f);
            dump.BackButton.GetComponent<UiSfxButton>().OnPointerEnter(null);
            dump.Close(CargoDiscardPopupView.CloseMode.Complete);
            dump.Tick(2f);
            yield return null;
            Assert.That(WindowTvSfx.StartedCount, Is.Zero);
            Assert.That(played, Is.Empty);
        }
    }
}

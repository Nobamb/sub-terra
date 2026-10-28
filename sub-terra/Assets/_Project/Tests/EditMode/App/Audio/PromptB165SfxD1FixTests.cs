using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using SubTerra.App.Audio;
using SubTerra.App.Editor.DataValidation;
using SubTerra.App.Integration;
using SubTerra.App.Run;
using SubTerra.App.UI;
using SubTerra.App.UI.EmergencyEscape;
using SubTerra.Shared;
using SubTerra.Shared.Audio;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SubTerra.App.Tests.Audio
{
    /// <summary>B-165: 효과음 D-1 고침(점프·큰 착지·일반 획득음 음량, 탈출 뒤 소리, ESC 설정창 반복 소리 멈춤).</summary>
    public sealed class PromptB165SfxD1FixTests
    {
        private static readonly (SfxId Id, float Volume)[] Volumes =
        {
            (SfxId.Jump, 0.4f),
            (SfxId.LandHeavy, 0.7f),
            (SfxId.PickupCommon, 0.65f),
        };

        private readonly List<Object> created = new List<Object>();
        private readonly List<SfxId> played = new List<SfxId>();

        [SetUp]
        public void SetUp()
        {
            played.Clear();
            ControlPreferences.IsSettingsOpen = false;
            SfxRequests.PlayRequested += Record;
        }

        [TearDown]
        public void TearDown()
        {
            SfxRequests.PlayRequested -= Record;
            ControlPreferences.IsSettingsOpen = false;
            for (var i = created.Count - 1; i >= 0; i--)
            {
                if (created[i] != null) Object.DestroyImmediate(created[i]);
            }

            created.Clear();
        }

        private void Record(SfxId id, float pitch) => played.Add(id);

        private int Count(SfxId id)
        {
            var count = 0;
            foreach (var entry in played)
            {
                if (entry == id) count++;
            }

            return count;
        }

        private static SfxLibrary LoadLibrary()
        {
            return AssetDatabase.LoadAssetAtPath<SfxLibrary>(PromptB156SfxLibraryBuilder.AssetPath);
        }

        // ---- 칸 음량 ----

        [Test]
        public void Volumes_DefaultsAndAssetMatch()
        {
            var defaults = new Dictionary<SfxId, SfxEntry>();
            foreach (var pair in SfxLibraryDefaults.Create())
            {
                defaults[pair.Entry.Id] = pair.Entry;
            }

            var library = LoadLibrary();
            Assert.That(library, Is.Not.Null);
            foreach (var row in Volumes)
            {
                Assert.That(defaults.TryGetValue(row.Id, out var entry), Is.True, row.Id.ToString());
                Assert.That(entry.Volume, Is.EqualTo(row.Volume), row.Id.ToString());
                Assert.That(library.TryGetEntry(row.Id, out var asset), Is.True, row.Id.ToString());
                Assert.That(asset.Volume, Is.EqualTo(row.Volume).Within(0.0001f), row.Id.ToString());
            }

            Assert.That(defaults.Count, Is.EqualTo(85), "칸 수는 그대로(B-170에서 85칸)");
        }

        // ---- ESC 설정창 ----

        [Test]
        public void WorldLoop_StopsWhileSettingsOpen_ResumesAfterClose()
        {
            Assume.That(UiPauseGate.IsHeld, Is.False);
            var host = new GameObject("B165Loop");
            created.Add(host);
            var loop = WorldLoopAudio.Attach(host, SfxId.DrillLoop, () => true, () => false, () => true);
            // 정지 판정은 주입하지 않고 실제 판정(UiPauseGate·설정창)을 쓴다.
            loop.ConfigureForTests(LoadLibrary(), () => false, null);

            loop.Tick(0.1f);
            Assert.That(loop.IsPlaying, Is.True);

            ControlPreferences.IsSettingsOpen = true;
            loop.Tick(0.016f);
            Assert.That(loop.IsPlaying, Is.False, "설정창이 열리면 바로 멈춤");
            loop.Tick(0.016f);
            Assert.That(loop.IsPlaying, Is.False);

            ControlPreferences.IsSettingsOpen = false;
            loop.Tick(0.02f);
            Assert.That(loop.IsPlaying, Is.True, "닫히고 조건이 남으면 다시");
            Assert.That(loop.Fade, Is.InRange(0.01f, 0.99f), "0.05초 페이드인");
        }

        [Test]
        public void PortalIdleLoop_StopsWhileSettingsOpen_ResumesAfterClose()
        {
            Assume.That(UiPauseGate.IsHeld, Is.False);
            var host = new GameObject("B165Portal");
            created.Add(host);
            var loop = host.AddComponent<PortalIdleLoopAudio>();
            loop.ConfigureForTests(LoadLibrary(), () => false, null);
            var portals = new List<Vector2> { new Vector2(1f, 0f) };

            loop.Tick(Vector2.zero, portals, false);
            Assert.That(loop.IsActive, Is.True);

            ControlPreferences.IsSettingsOpen = true;
            loop.Tick(Vector2.zero, portals, false);
            Assert.That(loop.IsActive, Is.False, "설정창이 열리면 멈춤");

            ControlPreferences.IsSettingsOpen = false;
            loop.Tick(Vector2.zero, portals, false);
            Assert.That(loop.IsActive, Is.True, "닫히면 다시");
        }

        // ---- 긴급 탈출 ----

        [Test]
        public void Escape_Success_NoArriveOrFailSfxAfterOneSecond()
        {
            var host = new GameObject("B165Escape");
            created.Add(host);
            var view = host.AddComponent<EmergencyEscapePanelView>();
            var binder = host.AddComponent<EmergencyEscapePanelBinder>();
            SetField(view, "panelRoot", host);
            SetField(binder, "view", view);
            var port = new FakePort();
            binder.BindTo(port);
            binder.Open(port.GetDestinationOptions(), new EmergencyEscapeCost(0, 0));

            binder.ConfirmSelectedDestination();
            Assert.That(port.EscapeCount, Is.EqualTo(1));
            Assert.That(Count(SfxId.PortalWarp), Is.EqualTo(1));

            // 1초(60프레임) 동안 Update를 돌린다.
            var update = typeof(EmergencyEscapePanelBinder).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic);
            for (var i = 0; i < 60; i++) update.Invoke(binder, null);

            Assert.That(Count(SfxId.EscapeArrive), Is.Zero);
            Assert.That(Count(SfxId.UiCancelFail), Is.Zero);
        }

        private static void SetField(object target, string name, object value)
        {
            var field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, name);
            field.SetValue(target, value);
        }

        private sealed class FakePort : IEmergencyEscapePortalPort
        {
            public int EscapeCount { get; private set; }

            public bool TryOpenEscapePanel(out string reason)
            {
                reason = string.Empty;
                return true;
            }

            public IReadOnlyList<EmergencyEscapeDestinationOption> GetDestinationOptions()
            {
                return new[]
                {
                    new EmergencyEscapeDestinationOption(EmergencyEscapeDestination.Elevator, string.Empty, "엘리베이터")
                };
            }

            public bool TryEscapeTo(EmergencyEscapeDestination kind, string outpostInstanceId, out string reason)
            {
                EscapeCount++;
                reason = string.Empty;
                return true;
            }
        }
    }
}

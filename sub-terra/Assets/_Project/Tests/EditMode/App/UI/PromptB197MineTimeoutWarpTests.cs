using System.Collections;
using System.IO;
using NUnit.Framework;
using SubTerra.App.UI.EmergencyEscape;
using SubTerra.Shared;
using UnityEngine;

namespace SubTerra.App.Tests.UI
{
    /// <summary>
    /// prompt-B 197: 광산 타이머 만료 시 글리치 암전 → 완전히 검을 때만 지상 로드 → 켜질 때는 이미 지상.
    /// 구출 글리치와 같은 시간표·설정을 쓰므로 그 검사(PromptB195EscapeWarpTests)는 그대로 유지된다.
    /// </summary>
    public sealed class PromptB197MineTimeoutWarpTests
    {
        [TearDown]
        public void TearDown()
        {
            AccessibilityPreferences.ReduceMotion = false;
        }

        // 지상 씬 로드를 흉내 낸다: 로드 접수 후 몇 프레임 뒤에야 "지상 활성".
        private sealed class FakeLoad
        {
            public bool LoadRequested;
            public bool SurfaceActive;
            public int CallCount;
            public float AlphaAtLoad = -1f;
            public bool GlitchAtLoad;
            public bool OverlayAliveDuringLoad = true;
            public bool GlitchDuringLoad;
            public MineTimeoutWarpSequence Warp;

            public IEnumerator Run()
            {
                CallCount++;
                AlphaAtLoad = Warp.Overlay != null ? Warp.Overlay.BlackoutAlpha : -1f;
                GlitchAtLoad = Warp.Overlay != null && Warp.Overlay.GlitchVisible;
                LoadRequested = true;
                for (var i = 0; i < 5; i++)
                {
                    // 씬이 바뀌는 동안에도 오버레이가 살아 있어야 한다.
                    OverlayAliveDuringLoad &= Warp.Overlay != null && Warp.Overlay.Root != null;
                    GlitchDuringLoad |= Warp.Overlay != null && Warp.Overlay.GlitchVisible;
                    yield return null;
                }

                SurfaceActive = true;
            }
        }

        [Test]
        public void Warp_LoadsSurfaceOnlyWhileFullyBlack_AndOnlyOnce()
        {
            var load = new FakeLoad();
            var warp = new MineTimeoutWarpSequence(new EscapeWarpSettings(), load.Run, false);
            load.Warp = warp;

            var run = warp.Run();
            var brightAfterLoadWithoutSurface = false;
            while (run.MoveNext())
            {
                var alpha = warp.Overlay != null ? warp.Overlay.BlackoutAlpha : 0f;
                // 지상이 열리기 전에는 화면이 밝아지면 안 된다.
                if (load.LoadRequested && !load.SurfaceActive && alpha < 0.999f)
                {
                    brightAfterLoadWithoutSurface = true;
                }
            }

            Assert.That(load.CallCount, Is.EqualTo(1));
            Assert.That(load.AlphaAtLoad, Is.GreaterThanOrEqualTo(0.999f), "지상 로드는 완전히 검을 때만 시작한다.");
            Assert.That(load.OverlayAliveDuringLoad, Is.True, "씬을 여는 동안 오버레이가 남아 있어야 한다.");
            Assert.That(brightAfterLoadWithoutSurface, Is.False, "지상이 열리기 전에 화면이 밝아지면 안 된다.");
            Assert.That(load.SurfaceActive, Is.True, "켜질 때는 이미 지상이다.");
            Assert.That(warp.BlackRan, Is.True);
        }

        [Test]
        public void Warp_WhileLoadingBlack_GlitchIsNotFrozenOnScreen_AndArrivalGlitchPlaysAfter()
        {
            var load = new FakeLoad();
            var warp = new MineTimeoutWarpSequence(new EscapeWarpSettings(), load.Run, false);
            load.Warp = warp;

            var run = warp.Run();
            var glitchFramesAfterLoad = 0;
            while (run.MoveNext())
            {
                if (load.SurfaceActive && warp.Overlay != null && warp.Overlay.GlitchVisible)
                {
                    glitchFramesAfterLoad++;
                }
            }

            Assert.That(load.GlitchAtLoad, Is.False, "암전 대기 중에는 굳은 글리치 장면이 남지 않는다.");
            Assert.That(load.GlitchDuringLoad, Is.False);
            Assert.That(glitchFramesAfterLoad, Is.GreaterThan(0), "켜지면서 도착 글리치가 이어져 가라앉는다.");
        }

        [Test]
        public void Warp_AfterLoad_ScreenOnIsNotTruncatedByTheLoadHitch()
        {
            // 로드 뒤 첫 프레임은 고정 간격으로 진행한다: 켜짐 구간의 프레임 수가 로드 길이와 무관하다.
            var shortLoad = new FakeLoad();
            var shortWarp = new MineTimeoutWarpSequence(new EscapeWarpSettings(), shortLoad.Run, false);
            shortLoad.Warp = shortWarp;
            var longLoad = new FakeLoad();
            var longWarp = new MineTimeoutWarpSequence(new EscapeWarpSettings(), longLoad.Run, false);
            longLoad.Warp = longWarp;

            Assert.That(CountFramesAfterLoad(shortWarp, shortLoad), Is.EqualTo(CountFramesAfterLoad(longWarp, longLoad)));
        }

        private static int CountFramesAfterLoad(MineTimeoutWarpSequence warp, FakeLoad load)
        {
            var run = warp.Run();
            var frames = 0;
            while (run.MoveNext())
            {
                if (load.SurfaceActive)
                {
                    frames++;
                }
            }

            return frames;
        }

        [Test]
        public void GlitchSfx_ReusesStartBriefingCloseSound_WithoutNewIds()
        {
            Assert.That(EscapeGlitchSfx.Id, Is.EqualTo(SubTerra.Shared.Audio.SfxId.BriefingClose));
            Assert.That(
                SubTerra.App.UI.Tutorial.StartBriefingPopupMotion.CompactCloseSfx,
                Is.Not.EqualTo(EscapeGlitchSfx.Id),
                "게임 시작 팝업 닫힘은 BriefingClose(PlaySfx)를 쓴다.");
            Assert.That(EscapeGlitchSfx.Play(false), Is.False, "재생 중이 아니면(EditMode) 소리를 내지 않는다.");
            Assert.That(EscapeGlitchSfx.Play(true), Is.False, "모션 감소면 글리치가 없으니 소리도 없다.");
        }

        [Test]
        public void GlitchSfx_IsStartedByRescueAndMineTimeoutPaths_NotByTheSharedRunner()
        {
            var root = Path.Combine(Application.dataPath, "_Project", "Scripts", "App");
            var rescue = File.ReadAllText(Path.Combine(root, "Integration", "EmergencyRescueRuntimeController.cs"));
            var sequence = File.ReadAllText(Path.Combine(root, "UI", "EmergencyEscape", "MineTimeoutWarpSequence.cs"));
            var runner = File.ReadAllText(Path.Combine(root, "Integration", "EscapeWarpRunner.cs"));
            Assert.That(rescue, Does.Contain("EscapeGlitchSfx.Play("));
            Assert.That(sequence, Does.Contain("EscapeGlitchSfx.Play("));
            Assert.That(runner, Does.Not.Contain("SfxPlayer"), "공용 진행기는 소리를 내지 않는다(포탈 웜홀은 무음 유지).");
        }

        [Test]
        public void Warp_ShowsGlitchWhileDarkening()
        {
            var load = new FakeLoad();
            var warp = new MineTimeoutWarpSequence(new EscapeWarpSettings(), load.Run, false);
            load.Warp = warp;

            var run = warp.Run();
            var glitchSeen = false;
            while (run.MoveNext())
            {
                if (!load.LoadRequested && warp.Overlay != null && warp.Overlay.GlitchVisible)
                {
                    glitchSeen = true;
                }
            }

            Assert.That(glitchSeen, Is.True, "암전 구간에 글리치가 보여야 한다.");
        }

        [Test]
        public void Warp_ReduceMotion_NeverShowsGlitch_AndStillLoadsWhileBlack()
        {
            AccessibilityPreferences.ReduceMotion = true;
            var load = new FakeLoad();
            var warp = new MineTimeoutWarpSequence(new EscapeWarpSettings(), load.Run);
            load.Warp = warp;
            Assert.That(warp.ReduceMotion, Is.True);

            var run = warp.Run();
            var glitchSeen = false;
            while (run.MoveNext())
            {
                if (warp.Overlay != null && warp.Overlay.GlitchVisible)
                {
                    glitchSeen = true;
                }
            }

            Assert.That(glitchSeen, Is.False, "모션 감소면 글리치 강도 0(페이드만).");
            Assert.That(load.CallCount, Is.EqualTo(1));
            Assert.That(load.AlphaAtLoad, Is.GreaterThanOrEqualTo(0.999f));
        }

        [Test]
        public void Warp_DestroysOverlayWhenFinished()
        {
            var load = new FakeLoad();
            var warp = new MineTimeoutWarpSequence(new EscapeWarpSettings(), load.Run, false);
            load.Warp = warp;

            var run = warp.Run();
            GameObject root = null;
            while (run.MoveNext())
            {
                if (root == null && warp.Overlay != null)
                {
                    root = warp.Overlay.Root;
                }
            }

            Assert.That(warp.IsFinished, Is.True);
            Assert.That(warp.Overlay, Is.Null);
            Assert.That(root == null, Is.True, "끝나면 오버레이 루트가 정리된다.");
        }

        [Test]
        public void Warp_Abort_CleansUpOverlay()
        {
            var warp = new MineTimeoutWarpSequence(new EscapeWarpSettings(), null, false);
            var run = warp.Run();
            run.MoveNext();
            Assert.That(warp.Overlay, Is.Not.Null);
            var root = warp.Overlay.Root;

            warp.Abort();

            Assert.That(warp.IsFinished, Is.True);
            Assert.That(root == null, Is.True);
        }

        [Test]
        public void TimedResetFlow_ShowsPopupOnlyAfterWarp_AndKeepsResetRules()
        {
            var path = Path.Combine(
                Application.dataPath, "_Project", "Scripts", "App", "Save", "SaveRuntimeController.cs");
            var source = File.ReadAllText(path);
            var start = source.IndexOf("private IEnumerator ExecuteTimedMineReset", System.StringComparison.Ordinal);
            Assert.That(start, Is.GreaterThan(0));
            var end = source.IndexOf("private IEnumerator RunTimedResetWarp", start, System.StringComparison.Ordinal);
            Assert.That(end, Is.GreaterThan(start));
            var body = source.Substring(start, end - start);

            var warpAt = body.IndexOf("RunTimedResetWarp()", System.StringComparison.Ordinal);
            var popupAt = body.IndexOf("ShowTimedResetPopup(", System.StringComparison.Ordinal);
            Assert.That(warpAt, Is.GreaterThan(0));
            Assert.That(popupAt, Is.GreaterThan(warpAt), "완료 팝업은 워프 뒤에 연다.");
            Assert.That(body, Does.Contain("MineResetService.TryTimedReset("));
            Assert.That(body, Does.Contain("AutoSaveReason.MineReset"));
            Assert.That(body, Does.Not.Contain("UnitySceneLoader().Load("), "지상 로드는 암전 중 콜백에서만 한다.");
        }
    }
}

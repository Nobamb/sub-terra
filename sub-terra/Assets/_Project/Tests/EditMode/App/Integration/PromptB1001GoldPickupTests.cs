using System.IO;
using NUnit.Framework;
using SubTerra.App.Editor.DataValidation;
using SubTerra.App.Integration;
using SubTerra.App.UI.FacilityNameTag;
using SubTerra.App.UI.HUD;
using SubTerra.Gameplay.Mining;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.Tests.Integration
{
    /// <summary>골드 획득 연출: 확정값 분리·문구·팔레트·Vfx 라이프사이클 (prompt-B 100-1 / 114 / 142).</summary>
    public sealed class PromptB1001GoldPickupTests
    {
        [TearDown]
        public void CleanupWorldRoots()
        {
            // 에디트 모드에서는 OnDestroy가 호출되지 않으므로 테스트가 만든 월드 루트를 직접 지운다.
            GameObject leftover;
            while ((leftover = GameObject.Find("GoldPickupWorldRoot")) != null)
            {
                Object.DestroyImmediate(leftover);
            }
        }

        [Test]
        public void PickupText_SplitsBaseAndBonusFromConfirmedValues()
        {
            Assert.That(GoldPickupPresentation.FormatMainText(20), Is.EqualTo("+20 G"));
            Assert.That(GoldPickupPresentation.FormatBonusText(20, 0), Is.EqualTo(string.Empty));
            Assert.That(GoldPickupPresentation.FormatMainText(35, 15), Is.EqualTo("+20 G"));
            Assert.That(GoldPickupPresentation.FormatBonusText(35, 15), Is.EqualTo("추가 골드 +15 G"));
            Assert.That(GoldPickupPresentation.FormatMainText(0), Is.EqualTo(string.Empty));
            Assert.That(GoldPickupPresentation.FormatMainText(-3), Is.EqualTo(string.Empty));
            Assert.That(GoldPickupPresentation.FormatBonusText(5, 9), Is.EqualTo("추가 골드 +5 G"));
            Assert.That(GoldPickupPresentation.FormatBonusText(-5, 9), Is.EqualTo(string.Empty));
            Assert.That(GoldPickupPresentation.FormatBonusText(5, -2), Is.EqualTo(string.Empty));
        }

        [Test]
        public void PickupText_BaseAndBonusSumEqualsAcceptedGoldWithoutRecomputing()
        {
            Assert.That(GoldPickupPresentation.BaseGold(35, 15) + GoldPickupPresentation.ClampBonus(35, 15), Is.EqualTo(35));
            Assert.That(GoldPickupPresentation.BaseGold(5, 9) + GoldPickupPresentation.ClampBonus(5, 9), Is.EqualTo(5));
            Assert.That(GoldPickupPresentation.BaseGold(0, 4), Is.Zero);
            Assert.That(GoldPickupPresentation.ClampBonus(-1, 4), Is.Zero);
        }

        [Test]
        public void PickupText_UsesNoThousandsSeparatorLikeHudGold()
        {
            Assert.That(GoldPickupPresentation.FormatBaseLine(1234567), Is.EqualTo("+1234567 G"));
            Assert.That(HudFormatter.FormatGold(1234567), Is.EqualTo("골드 1234567"));
            Assert.That(GoldPickupPresentation.FormatBonusLine(12000), Is.EqualTo("추가 골드 +12000 G"));
        }

        [Test]
        public void PickupColors_UseWarmGoldPaletteNotCyan()
        {
            Color[] colors =
            {
                GoldPickupPresentation.MainTopColor,
                GoldPickupPresentation.MainBottomColor,
                GoldPickupPresentation.BonusColor
            };
            for (var index = 0; index < colors.Length; index++)
            {
                Assert.That(colors[index].r, Is.GreaterThan(0.9f));
                Assert.That(colors[index].b, Is.LessThan(colors[index].g));
                Assert.That(colors[index].b, Is.LessThan(0.5f));
            }

            Color[] frame =
            {
                GoldPickupPresentation.FrameFillColor,
                GoldPickupPresentation.FrameGlowColor,
                GoldPickupPresentation.FrameEdgeColor,
                GoldPickupPresentation.FrameAccentColor,
                GoldPickupPresentation.LeverKnobColor
            };
            for (var index = 0; index < frame.Length; index++)
            {
                Assert.That(frame[index].r, Is.GreaterThan(frame[index].b), "청록 계열이면 안 된다: " + index);
                Assert.That(frame[index].g, Is.GreaterThanOrEqualTo(frame[index].b));
            }
        }

        [Test]
        public void HudPulse_PeaksWithinRequestedRangeAndReturns()
        {
            Assert.That(GoldPickupPresentation.EvaluatePulse(0f), Is.EqualTo(1f));
            float peak = GoldPickupPresentation.EvaluatePulse(GoldPickupPresentation.PulseSeconds * 0.5f);
            Assert.That(peak, Is.InRange(1.05f, 1.1f));
            Assert.That(GoldPickupPresentation.EvaluatePulse(GoldPickupPresentation.PulseSeconds), Is.EqualTo(1f));
            Assert.That(GoldPickupPresentation.HudParticleCount, Is.InRange(2, 4));

            var start = new Vector2(0f, 0f);
            var end = new Vector2(300f, 200f);
            Assert.That(GoldPickupPresentation.EvaluateHudPath(start, end, 0f, 0), Is.EqualTo(start));
            Assert.That(Vector2.Distance(GoldPickupPresentation.EvaluateHudPath(start, end, 1f, 1), end), Is.LessThan(0.001f));
        }

        [Test]
        public void HudLaunchTimes_AreRelativeToExitStartAndStaggered()
        {
            Assert.That(GoldPickupPresentation.HudLaunchTime(0), Is.Zero);
            Assert.That(
                GoldPickupPresentation.HudLaunchTime(1) - GoldPickupPresentation.HudLaunchTime(0),
                Is.EqualTo(GoldPickupPresentation.HudStagger).Within(0.0001f));
            Assert.That(
                GoldPickupPresentation.HudLaunchTime(GoldPickupPresentation.HudParticleCount - 1),
                Is.LessThan(GoldPickupPopupTimeline.ExitDuration),
                "모든 HUD 입자는 퇴장 구간 안에 발사된다.");
        }

        [Test]
        public void Vfx_PlaySpawnsDustAtCellAndNoCellCoins()
        {
            var texture = CreateTexture();
            var sprite = Sprite.Create(texture, new Rect(0f, 0f, 8f, 8f), new Vector2(0.5f, 0.5f), 8f);
            var host = new GameObject("GoldPickupHost");
            var vfx = host.AddComponent<GoldPickupVfx>();
            SetPrivate(vfx, "coinSprite", sprite);

            vfx.Play(20, Vector3.zero, Vector3.up);
            Assert.That(vfx.ActiveDustCount, Is.EqualTo(1));
            Assert.That(vfx.ActiveCoinCount, Is.Zero, "채굴 칸 SpawnCoins는 제거되었다.");
            Assert.That(GameObject.Find("GoldPickupCoin"), Is.Null);
            Assert.That(vfx.IsPopupPlaying, Is.False, "폰트가 없으면 팝업은 만들지 않는다.");

            vfx.Tick(GoldPickupPresentation.DustCleanupSeconds + 0.1f);
            Assert.That(vfx.ActiveDustCount, Is.EqualTo(0));

            vfx.ClearEffects();
            Object.DestroyImmediate(host);
            Object.DestroyImmediate(texture);
        }

        [Test]
        public void Vfx_PlayShowsOneGoldPopupWithConfirmedValues()
        {
            var font = LoadFontOrIgnore();
            var host = new GameObject("GoldPickupHost");
            var vfx = host.AddComponent<GoldPickupVfx>();
            SetPrivate(vfx, "pickupFont", font);

            vfx.Play(35, Vector3.zero, Vector3.up, 15);
            Assert.That(vfx.ActivePopupCount, Is.EqualTo(1));
            Assert.That(vfx.PopupState.BaseTotal, Is.EqualTo(20));
            Assert.That(vfx.PopupState.BonusTotal, Is.EqualTo(15));

            vfx.Tick(0.6f);
            GoldPickupPopupVisual visual = vfx.PopupVisual;
            Assert.That(visual.MainFill.text, Is.EqualTo("+20 G"));
            Assert.That(visual.BonusFill.text, Is.EqualTo("추가 골드 +15 G"));
            Assert.That(visual.Root.GetComponentsInChildren<GraphicRaycaster>(true), Is.Empty);
            Assert.That(visual.Root.GetComponentsInChildren<Graphic>(true), Is.Not.Empty);
            foreach (Graphic graphic in visual.Root.GetComponentsInChildren<Graphic>(true))
            {
                Assert.That(graphic.raycastTarget, Is.False, graphic.name);
            }

            vfx.ClearEffects();
            Assert.That(vfx.ActivePopupCount, Is.Zero);
            Object.DestroyImmediate(host);
        }

        [Test]
        public void Vfx_PopupRunsToCompletionAndLeavesNothingBehind()
        {
            var font = LoadFontOrIgnore();
            var host = new GameObject("GoldPickupHost");
            var vfx = host.AddComponent<GoldPickupVfx>();
            SetPrivate(vfx, "pickupFont", font);

            vfx.Play(20, Vector3.zero, Vector3.up);
            for (var i = 0; i < 100 && vfx.IsPopupPlaying; i++)
            {
                vfx.Tick(0.02f);
            }

            Assert.That(vfx.IsPopupPlaying, Is.False);
            Assert.That(GameObject.Find("GoldPickupPopup"), Is.Null, "퇴장 후 프레임·레버·잔상·금화가 남지 않는다.");
            Object.DestroyImmediate(host);
        }

        [Test]
        public void Vfx_ConsecutivePickupsMergeIntoOnePopup()
        {
            var font = LoadFontOrIgnore();
            var texture = CreateTexture();
            var sprite = Sprite.Create(texture, new Rect(0f, 0f, 8f, 8f), new Vector2(0.5f, 0.5f), 8f);
            var host = new GameObject("GoldPickupHost");
            var vfx = host.AddComponent<GoldPickupVfx>();
            SetPrivate(vfx, "pickupFont", font);
            SetPrivate(vfx, "coinSprite", sprite);

            vfx.Play(35, Vector3.zero, Vector3.up, 15);
            vfx.Tick(0.3f);
            int coinsBefore = vfx.ActiveCoinCount;
            vfx.Play(30, Vector3.zero, Vector3.up, 10);

            Assert.That(vfx.ActivePopupCount, Is.EqualTo(1));
            Assert.That(vfx.PopupState.BaseTotal, Is.EqualTo(40));
            Assert.That(vfx.PopupState.BonusTotal, Is.EqualTo(25));
            Assert.That(vfx.PopupState.MergeCount, Is.EqualTo(1));
            Assert.That(
                vfx.ActiveCoinCount - coinsBefore,
                Is.EqualTo(GoldPickupPopupTimeline.MergeCoinCount(30)),
                "합치기 증가분 기준으로 작은 금화만 추가한다.");

            vfx.Tick(0.5f);
            Assert.That(vfx.PopupVisual.MainFill.text, Is.EqualTo("+40 G"));
            Assert.That(vfx.PopupVisual.BonusFill.text, Is.EqualTo("추가 골드 +25 G"));

            vfx.ClearEffects();
            Object.DestroyImmediate(host);
            Object.DestroyImmediate(texture);
        }

        [Test]
        public void Vfx_HudParticlesLaunchOncePerPopupEvenWhenExitIsCancelled()
        {
            var font = LoadFontOrIgnore();
            var texture = CreateTexture();
            var sprite = Sprite.Create(texture, new Rect(0f, 0f, 8f, 8f), new Vector2(0.5f, 0.5f), 8f);
            var cameraObject = new GameObject("MainCamera", typeof(Camera)) { tag = "MainCamera" };
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
            cameraObject.GetComponent<Camera>().orthographic = true;

            var canvasObject = new GameObject("HudCanvas", typeof(RectTransform), typeof(Canvas));
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var hudObject = new GameObject("BasicHUD", typeof(RectTransform));
            hudObject.transform.SetParent(canvasObject.transform, false);
            var hudView = hudObject.AddComponent<BasicHudView>();
            var goldObject = new GameObject("GoldText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            goldObject.transform.SetParent(hudObject.transform, false);
            var goldText = goldObject.GetComponent<TextMeshProUGUI>();
            goldText.font = font;
            var goldRect = goldText.rectTransform;
            goldRect.pivot = new Vector2(0f, 1f);
            goldRect.sizeDelta = new Vector2(146f, 31f);
            goldRect.anchoredPosition = new Vector2(60f, -154f);
            SetPrivate(hudView, "goldText", goldText);

            var host = new GameObject("GoldPickupHost");
            var vfx = host.AddComponent<GoldPickupVfx>();
            SetPrivate(vfx, "coinSprite", sprite);
            SetPrivate(vfx, "pickupFont", font);
            vfx.SetHudView(hudView);

            // 퇴장 시작 직후에는 HUD 입자가 아직 없고, 시작 후 순서대로 발사된다.
            vfx.Play(20, Vector3.zero, Vector3.up);
            vfx.Tick(GoldPickupPopupTimeline.BaseFallEnd + GoldPickupPopupTimeline.ExitLead - 0.02f);
            Assert.That(vfx.ActiveHudParticleCount, Is.Zero);
            Assert.That(vfx.PopupState.HudSequenceStarted, Is.False);

            int peak = 0;
            var sawPulse = false;
            for (var i = 0; i < 6; i++)
            {
                vfx.Tick(0.03f);
                peak = Mathf.Max(peak, vfx.ActiveHudParticleCount);
            }

            Assert.That(vfx.PopupState.HudSequenceStarted, Is.True);
            Assert.That(vfx.ActiveHudParticleCount, Is.EqualTo(GoldPickupPresentation.HudParticleCount));

            // 퇴장 도중 합치기로 취소·재개돼도 같은 팝업은 HUD 입자를 다시 발사하지 않는다.
            vfx.Play(5, Vector3.zero, Vector3.up);
            Assert.That(vfx.PopupState.Exiting, Is.False);
            for (var i = 0; i < 120 && vfx.IsPopupPlaying; i++)
            {
                vfx.Tick(0.02f);
                peak = Mathf.Max(peak, vfx.ActiveHudParticleCount);
                sawPulse |= vfx.IsPulsing && goldRect.localScale.x > 1f;
            }

            Assert.That(vfx.IsPopupPlaying, Is.False);
            Assert.That(peak, Is.EqualTo(GoldPickupPresentation.HudParticleCount));

            // 입자가 도착하면 Gold HUD가 Pulse하고, 끝나면 크기·위치가 정확히 원복된다.
            for (var i = 0; i < 80 && (vfx.ActiveHudParticleCount > 0 || vfx.IsPulsing); i++)
            {
                vfx.Tick(0.02f);
                sawPulse |= vfx.IsPulsing && goldRect.localScale.x > 1f;
            }

            Assert.That(sawPulse, Is.True);
            Assert.That(vfx.IsPulsing, Is.False);
            Assert.That(goldRect.localScale, Is.EqualTo(Vector3.one));
            Assert.That(goldRect.anchoredPosition, Is.EqualTo(new Vector2(60f, -154f)));

            Object.DestroyImmediate(host);
            Object.DestroyImmediate(canvasObject);
            Object.DestroyImmediate(cameraObject);
            Object.DestroyImmediate(texture);
        }

        [Test]
        public void Vfx_ClearEffectsMidPulseRestoresHudAndRemovesPopup()
        {
            var font = LoadFontOrIgnore();
            var texture = CreateTexture();
            var sprite = Sprite.Create(texture, new Rect(0f, 0f, 8f, 8f), new Vector2(0.5f, 0.5f), 8f);
            var cameraObject = new GameObject("MainCamera", typeof(Camera)) { tag = "MainCamera" };
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
            cameraObject.GetComponent<Camera>().orthographic = true;
            var canvasObject = new GameObject("HudCanvas", typeof(RectTransform), typeof(Canvas));
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var hudObject = new GameObject("BasicHUD", typeof(RectTransform));
            hudObject.transform.SetParent(canvasObject.transform, false);
            var hudView = hudObject.AddComponent<BasicHudView>();
            var goldObject = new GameObject("GoldText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            goldObject.transform.SetParent(hudObject.transform, false);
            var goldText = goldObject.GetComponent<TextMeshProUGUI>();
            goldText.font = font;
            var goldRect = goldText.rectTransform;
            goldRect.pivot = new Vector2(0f, 1f);
            goldRect.sizeDelta = new Vector2(146f, 31f);
            goldRect.anchoredPosition = new Vector2(60f, -154f);
            SetPrivate(hudView, "goldText", goldText);

            var host = new GameObject("GoldPickupHost");
            var vfx = host.AddComponent<GoldPickupVfx>();
            SetPrivate(vfx, "coinSprite", sprite);
            SetPrivate(vfx, "pickupFont", font);
            vfx.SetHudView(hudView);

            vfx.Play(20, Vector3.zero, Vector3.up);
            for (var i = 0; i < 200 && !vfx.IsPulsing; i++)
            {
                vfx.Tick(0.02f);
            }

            Assume.That(vfx.IsPulsing, Is.True);
            vfx.Tick(0.05f);
            Assert.That(goldRect.localScale.x, Is.GreaterThan(1f));

            // OnDisable이 호출하는 정리 경로.
            vfx.ClearEffects();
            Assert.That(vfx.ActivePopupCount, Is.Zero);
            Assert.That(vfx.ActiveHudParticleCount, Is.Zero);
            Assert.That(vfx.ActiveCoinCount, Is.Zero);
            Assert.That(vfx.IsPulsing, Is.False);
            Assert.That(goldRect.localScale, Is.EqualTo(Vector3.one));
            Assert.That(goldRect.anchoredPosition, Is.EqualTo(new Vector2(60f, -154f)));
            Assert.That(GameObject.Find("GoldPickupPopup"), Is.Null);

            Object.DestroyImmediate(host);
            Object.DestroyImmediate(canvasObject);
            Object.DestroyImmediate(cameraObject);
            Object.DestroyImmediate(texture);
        }

        [Test]
        public void FailedMining_ClearsPendingGoldWithoutPlaying()
        {
            var host = new GameObject("GoldPickupHost");
            var vfx = host.AddComponent<GoldPickupVfx>();
            var mining = host.AddComponent<MiningSystem>();
            vfx.BindTo(mining, host.transform);
            vfx.SetPendingGold(50, 10);
            Assert.That(vfx.PendingGold, Is.EqualTo(50));

            mining.TryStartMining(Vector3Int.zero);
            Assert.That(vfx.PendingGold, Is.EqualTo(0));
            Assert.That(vfx.ActivePopupCount, Is.EqualTo(0));

            Object.DestroyImmediate(host);
        }

        [Test]
        public void SetPendingGold_ClampsNegativeAndExcessBonus()
        {
            var host = new GameObject("GoldPickupHost");
            var vfx = host.AddComponent<GoldPickupVfx>();
            vfx.SetPendingGold(10, 99);
            Assert.That(vfx.PendingGold, Is.EqualTo(10));
            vfx.SetPendingGold(-5, 3);
            Assert.That(vfx.PendingGold, Is.Zero);
            Object.DestroyImmediate(host);
        }

        [Test]
        public void AssetsAndWiring_UseDedicatedFontAndVfx()
        {
            Assert.That(File.Exists(Path.Combine(Application.dataPath, "_Project/Art/FX/gold_coin_01.png")), Is.True);
            Assert.That(File.Exists(Path.Combine(Application.dataPath, "_Project/Fonts/SeoulAlrimTTF-Heavy.ttf")), Is.True);

            var hud = File.ReadAllText(Path.Combine(Application.dataPath, "_Project/Prefabs/UI/HUDCanvas.prefab"));
            var scene = File.ReadAllText(
                Path.Combine(Application.dataPath, "_Project/Scenes/App/Mine_Demo_Integration.unity"));
            var binder = File.ReadAllText(
                Path.Combine(Application.dataPath, "_Project/Scripts/App/Integration/IntegrationRuntimeBinder.cs"));

            Assert.That(hud, Does.Contain("GoldPickupVfx"));
            Assert.That(scene, Does.Match(@"goldPickupVfx: \{fileID: [1-9]"));
            Assert.That(binder, Does.Contain("goldPickupVfx.SetPendingGold"));
            Assert.That(binder, Does.Contain("goldPickupVfx.BindTo"));

            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(PromptB1001GoldPickupBuilder.FontSdfPath);
            Assert.That(font, Is.Not.Null);
            foreach (char glyph in "+G0123456789 골드")
            {
                Assert.That(font.characterLookupTable.ContainsKey(glyph), Is.True, "missing glyph " + glyph);
            }
        }

        [Test]
        public void FontSeed_CoversPopupPhrasesAndFontAssetOnlyAfterBuilderRerun()
        {
            string[] phrases =
            {
                GoldPickupPresentation.FormatBaseLine(1234567890),
                GoldPickupPresentation.FormatBonusLine(1234567890)
            };
            for (var p = 0; p < phrases.Length; p++)
            {
                foreach (char glyph in phrases[p])
                {
                    Assert.That(
                        PromptB1001GoldPickupBuilder.SeedCharacters.IndexOf(glyph) >= 0,
                        Is.True,
                        "빌더 SeedCharacters에 없는 글자: " + glyph);
                }
            }

            // 폰트 에셋 YAML은 직접 수정하지 않으므로, 빌더 재실행 전에는 "추가"가 없을 수 있다.
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(PromptB1001GoldPickupBuilder.FontSdfPath);
            Assert.That(font, Is.Not.Null);
            Assume.That(
                font.characterLookupTable.ContainsKey('추') && font.characterLookupTable.ContainsKey('가'),
                Is.True,
                "SubTerra/UI/Build Prompt-B 142 Gold Pickup Popup Font 메뉴를 한 번 실행해야 한다.");
        }

        [Test]
        public void MainScreenLayer_HidesPopupFromCctvAndKeepsSortingAboveNameTag()
        {
            Assert.That(GoldPickupPopupVisual.SortingOrder, Is.GreaterThan(FacilityNameTagVisual.WorldSortingOrder));
            var font = LoadFontOrIgnore();
            var visual = GoldPickupPopupVisual.Create(null, font, null);
            try
            {
                Assert.That(visual.Root.layer, Is.EqualTo(FacilityNameTagLayers.MainScreen));
                Assert.That(FacilityNameTagLayers.IsHiddenFromCctv(visual.Root), Is.True);
                foreach (Transform child in visual.Root.GetComponentsInChildren<Transform>(true))
                {
                    Assert.That(child.gameObject.layer, Is.EqualTo(FacilityNameTagLayers.MainScreen), child.name);
                }
            }
            finally
            {
                visual.Destroy();
            }
        }

        private static TMP_FontAsset LoadFontOrIgnore()
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(PromptB1001GoldPickupBuilder.FontSdfPath);
            Assume.That(font, Is.Not.Null);
            return font;
        }

        private static Texture2D CreateTexture()
        {
            var texture = new Texture2D(8, 8);
            texture.SetPixel(0, 0, Color.yellow);
            texture.Apply();
            return texture;
        }

        private static void SetPrivate(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(
                fieldName,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, fieldName);
            field.SetValue(target, value);
        }
    }
}

using System.IO;
using NUnit.Framework;
using SubTerra.App.Editor.DataValidation;
using SubTerra.App.Integration;
using SubTerra.App.UI.HUD;
using SubTerra.Gameplay.Mining;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace SubTerra.App.Tests.Integration
{
    public sealed class PromptB1001GoldPickupTests
    {
        [Test]
        public void PickupText_SplitsBaseAndBonusFromConfirmedValues()
        {
            Assert.That(GoldPickupPresentation.FormatMainText(20), Is.EqualTo("+20G"));
            Assert.That(GoldPickupPresentation.FormatBonusText(20, 0), Is.EqualTo(string.Empty));
            Assert.That(GoldPickupPresentation.FormatMainText(35, 15), Is.EqualTo("+20G"));
            Assert.That(GoldPickupPresentation.FormatBonusText(35, 15), Is.EqualTo("BONUS +15G"));
            Assert.That(GoldPickupPresentation.FormatMainText(0), Is.EqualTo(string.Empty));
            Assert.That(GoldPickupPresentation.FormatMainText(-3), Is.EqualTo(string.Empty));
            Assert.That(GoldPickupPresentation.FormatBonusText(5, 9), Is.EqualTo("BONUS +5G"));
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
        }

        [Test]
        public void TextTimeline_PopsThenRisesAndFades()
        {
            GoldPickupPresentation.EvaluateText(0f, out float a0, out float y0, out float s0);
            Assert.That(a0, Is.EqualTo(0f));
            Assert.That(y0, Is.EqualTo(0f));
            Assert.That(s0, Is.LessThan(1f));

            GoldPickupPresentation.EvaluateText(GoldPickupPresentation.TextPopSeconds, out float aPop, out _, out float sPop);
            Assert.That(aPop, Is.EqualTo(1f));
            Assert.That(sPop, Is.EqualTo(GoldPickupPresentation.TextPopScalePeak).Within(0.0001f));
            Assert.That(sPop, Is.InRange(1.05f, 1.2f));

            GoldPickupPresentation.EvaluateText(0.6f, out float aHold, out float yHold, out float sHold);
            Assert.That(aHold, Is.EqualTo(1f));
            Assert.That(sHold, Is.EqualTo(1f));

            GoldPickupPresentation.EvaluateText(1.2f, out float aOut, out float yOut, out _);
            Assert.That(aOut, Is.GreaterThan(0f).And.LessThan(1f));
            Assert.That(yOut, Is.GreaterThan(yHold));

            GoldPickupPresentation.EvaluateText(GoldPickupPresentation.TextDuration, out float aEnd, out float yEnd, out _);
            Assert.That(aEnd, Is.EqualTo(0f));
            Assert.That(yEnd, Is.EqualTo(GoldPickupPresentation.TextRise));
        }

        [Test]
        public void BonusLine_AppearsAfterMainLine()
        {
            GoldPickupPresentation.EvaluateBonus(GoldPickupPresentation.BonusDelaySeconds * 0.5f, out float early, out _);
            Assert.That(early, Is.EqualTo(0f));
            GoldPickupPresentation.EvaluateBonus(0.5f, out float mid, out float midScale);
            Assert.That(mid, Is.EqualTo(1f));
            Assert.That(midScale, Is.EqualTo(1f));
        }

        [Test]
        public void Coins_HopAndFallBackWithFade()
        {
            Assert.That(GoldPickupPresentation.CoinCount, Is.InRange(2, 4));
            var origin = new Vector3(2f, 1f, 0f);
            float duration = GoldPickupPresentation.CoinFlightDuration(1);
            var start = GoldPickupPresentation.EvaluateCoinPosition(origin, 1, 0f);
            var peak = GoldPickupPresentation.EvaluateCoinPosition(origin, 1, duration * 0.5f);
            var end = GoldPickupPresentation.EvaluateCoinPosition(origin, 1, duration);
            Assert.That(peak.y - start.y, Is.InRange(0.3f, 0.8f));
            Assert.That(end.y, Is.EqualTo(start.y).Within(0.001f));
            Assert.That(GoldPickupPresentation.CoinAlpha(0.3f), Is.EqualTo(1f));
            Assert.That(GoldPickupPresentation.CoinAlpha(1f), Is.EqualTo(0f));
            Assert.That(GoldPickupPresentation.CoinScale(1), Is.GreaterThan(GoldPickupPresentation.CoinScale(2)));
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
        public void Vfx_PlaySpawnsCoinsAndDustThenExpires()
        {
            var texture = CreateTexture();
            var sprite = Sprite.Create(texture, new Rect(0f, 0f, 8f, 8f), new Vector2(0.5f, 0.5f), 8f);
            var host = new GameObject("GoldPickupHost");
            var vfx = host.AddComponent<GoldPickupVfx>();
            SetPrivate(vfx, "coinSprite", sprite);

            vfx.Play(20, Vector3.zero, Vector3.up);
            Assert.That(vfx.ActiveCoinCount, Is.EqualTo(GoldPickupPresentation.CoinCount));
            Assert.That(vfx.ActiveDustCount, Is.EqualTo(1));
            Assert.That(vfx.IsTextPlaying, Is.False);

            vfx.Tick(0.12f);
            var coin = GameObject.Find("GoldPickupCoin");
            Assert.That(coin, Is.Not.Null);
            Assert.That(coin.GetComponent<SpriteRenderer>().color.a, Is.GreaterThan(0.5f));

            vfx.Tick(Mathf.Max(GoldPickupPresentation.MaxCoinLifetime(), GoldPickupPresentation.DustCleanupSeconds) + 0.1f);
            Assert.That(vfx.ActiveCoinCount, Is.EqualTo(0));
            Assert.That(vfx.ActiveDustCount, Is.EqualTo(0));

            Object.DestroyImmediate(host);
            Object.DestroyImmediate(texture);
        }

        [Test]
        public void Vfx_ConsecutivePickupsStackTextsAndEvictOldest()
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(PromptB1001GoldPickupBuilder.FontSdfPath);
            Assume.That(font, Is.Not.Null);
            var host = new GameObject("GoldPickupHost");
            var vfx = host.AddComponent<GoldPickupVfx>();
            SetPrivate(vfx, "pickupFont", font);

            vfx.Play(35, Vector3.zero, Vector3.up, 15);
            var first = GameObject.Find("GoldPickupText");
            Assert.That(first, Is.Not.Null);
            Assert.That(first.transform.Find("MainLine/Fill").GetComponent<TMP_Text>().text, Is.EqualTo("+20G"));
            Assert.That(first.transform.Find("BonusLine/Fill").GetComponent<TMP_Text>().text, Is.EqualTo("BONUS +15G"));
            Assert.That(first.GetComponentInChildren<TMP_Text>().raycastTarget, Is.False);

            vfx.Tick(0.3f);
            float firstY = first.transform.position.y;
            vfx.Play(20, Vector3.zero, Vector3.up);
            vfx.Tick(0.5f);
            Assert.That(vfx.ActiveTextCount, Is.EqualTo(2));
            Assert.That(
                first.transform.position.y - firstY,
                Is.GreaterThan(GoldPickupPresentation.TextLineStep * 0.9f),
                "이전 문구는 새 문구 높이만큼 위로 밀려야 한다.");

            vfx.Play(20, Vector3.zero, Vector3.up);
            vfx.Play(20, Vector3.zero, Vector3.up);
            vfx.Tick(GoldPickupPresentation.TextEvictSeconds + 0.01f);
            Assert.That(vfx.ActiveTextCount, Is.LessThanOrEqualTo(GoldPickupPresentation.MaxActiveTexts));

            vfx.Tick(GoldPickupPresentation.TextDuration + 0.1f);
            Assert.That(vfx.ActiveTextCount, Is.EqualTo(0));
            Object.DestroyImmediate(host);
        }

        [Test]
        public void Vfx_HudParticlesPulseGoldAndRestoreTransform()
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(PromptB1001GoldPickupBuilder.FontSdfPath);
            Assume.That(font, Is.Not.Null);
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
            vfx.Tick(GoldPickupPresentation.HudLaunchTime(GoldPickupPresentation.HudParticleCount - 1) + 0.01f);
            Assert.That(vfx.ActiveHudParticleCount, Is.EqualTo(GoldPickupPresentation.HudParticleCount));

            vfx.Tick(GoldPickupPresentation.HudFlightSeconds - 0.02f);
            vfx.Tick(0.04f);
            Assert.That(vfx.IsPulsing, Is.True);
            Assert.That(goldRect.localScale.x, Is.GreaterThan(1f));

            vfx.Tick(GoldPickupPresentation.PulseSeconds + GoldPickupPresentation.HudStagger * 3f);
            vfx.Tick(GoldPickupPresentation.PulseSeconds + 0.01f);
            Assert.That(vfx.ActiveHudParticleCount, Is.EqualTo(0));
            Assert.That(vfx.IsPulsing, Is.False);
            Assert.That(goldRect.localScale, Is.EqualTo(Vector3.one));
            Assert.That(goldRect.anchoredPosition, Is.EqualTo(new Vector2(60f, -154f)));

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
            Assert.That(vfx.ActiveCoinCount, Is.EqualTo(0));

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
            foreach (char glyph in "+BONUSG0123456789")
            {
                Assert.That(font.characterLookupTable.ContainsKey(glyph), Is.True, "missing glyph " + glyph);
            }
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

using System.IO;
using NUnit.Framework;
using SubTerra.App.Integration;
using SubTerra.Gameplay.Mining;
using UnityEngine;

namespace SubTerra.App.Tests.Integration
{
    public sealed class PromptB1001GoldPickupTests
    {
        [Test]
        public void PickupText_UsesRequestedFormatAndColors()
        {
            Assert.That(GoldPickupPresentation.FormatPickupText(20), Is.EqualTo("20G 골드 획득!"));
            Assert.That(GoldPickupPresentation.FormatPickupText(0), Is.EqualTo(string.Empty));
            Assert.That(GoldPickupPresentation.FormatPickupText(-3), Is.EqualTo(string.Empty));
            Assert.That(ColorUtility.ToHtmlStringRGBA(GoldPickupPresentation.FillColor), Is.EqualTo("FFFB19FF"));
        }

        [Test]
        public void CoinAlpha_PeaksAtMidHeight()
        {
            Assert.That(GoldPickupPresentation.CoinAlpha(0f), Is.EqualTo(0f));
            Assert.That(GoldPickupPresentation.CoinAlpha(0.25f), Is.EqualTo(0.5f));
            Assert.That(GoldPickupPresentation.CoinAlpha(0.5f), Is.EqualTo(1f));
            Assert.That(GoldPickupPresentation.CoinAlpha(0.75f), Is.EqualTo(0.5f));
            Assert.That(GoldPickupPresentation.CoinAlpha(1f), Is.EqualTo(0f));
        }

        [Test]
        public void TextTimeline_FadesInHoldsThenDescends()
        {
            GoldPickupPresentation.EvaluateText(0f, out float a0, out float y0);
            Assert.That(a0, Is.EqualTo(0f));
            Assert.That(y0, Is.EqualTo(0f));

            GoldPickupPresentation.EvaluateText(0.25f, out float aIn, out float yIn);
            Assert.That(aIn, Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(yIn, Is.EqualTo(GoldPickupPresentation.TextRise * 0.5f).Within(0.0001f));

            GoldPickupPresentation.EvaluateText(1.0f, out float aHold, out float yHold);
            Assert.That(aHold, Is.EqualTo(1f));
            Assert.That(yHold, Is.EqualTo(GoldPickupPresentation.TextRise));

            GoldPickupPresentation.EvaluateText(1.75f, out float aOut, out float yOut);
            Assert.That(aOut, Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(yOut, Is.EqualTo(GoldPickupPresentation.TextRise * 0.5f).Within(0.0001f));

            GoldPickupPresentation.EvaluateText(2.0f, out float aEnd, out float yEnd);
            Assert.That(aEnd, Is.EqualTo(0f));
            Assert.That(yEnd, Is.EqualTo(0f));
        }

        [Test]
        public void Vfx_PlaySpawnsCoinsAndTickExpiresThem()
        {
            var texture = new Texture2D(8, 8);
            texture.SetPixel(0, 0, Color.yellow);
            texture.Apply();
            var sprite = Sprite.Create(texture, new Rect(0f, 0f, 8f, 8f), new Vector2(0.5f, 0.5f), 8f);
            var host = new GameObject("GoldPickupHost");
            var vfx = host.AddComponent<GoldPickupVfx>();
            SetPrivate(vfx, "coinSprite", sprite);

            vfx.Play(20, Vector3.zero, Vector3.up);
            Assert.That(vfx.ActiveCoinCount, Is.EqualTo(GoldPickupPresentation.CoinCount));
            Assert.That(vfx.IsTextPlaying, Is.False);

            vfx.Tick(0.18f);
            Assert.That(vfx.ActiveCoinCount, Is.EqualTo(GoldPickupPresentation.CoinCount));
            var coin = GameObject.Find("GoldPickupCoin");
            Assert.That(coin, Is.Not.Null);
            var renderer = coin.GetComponent<SpriteRenderer>();
            Assert.That(renderer, Is.Not.Null);
            Assert.That(renderer.color.a, Is.GreaterThan(0.1f));

            vfx.Tick(GoldPickupPresentation.MaxCoinLifetime() + 0.1f);
            Assert.That(vfx.ActiveCoinCount, Is.EqualTo(0));

            Object.DestroyImmediate(host);
            Object.DestroyImmediate(texture);
        }

        [Test]
        public void Coins_UseParabolicArcAndPerspectiveScale()
        {
            Assert.That(GoldPickupPresentation.CoinCount, Is.EqualTo(5));
            var origin = new Vector3(2f, 1f, 0f);
            var start = GoldPickupPresentation.EvaluateCoinPosition(origin, 0, 0f);
            float midTime = GoldPickupPresentation.CoinFlightDuration(0) * 0.5f;
            var mid = GoldPickupPresentation.EvaluateCoinPosition(origin, 0, midTime);
            var peak = GoldPickupPresentation.EvaluateCoinPosition(
                origin,
                0,
                GoldPickupPresentation.CoinFlightDuration(0));

            Assert.That(mid.y, Is.GreaterThan(start.y));
            Assert.That(peak.y, Is.GreaterThan(mid.y));
            Assert.That(Mathf.Abs(mid.x - start.x), Is.GreaterThan(0.01f));
            float chordY = Mathf.Lerp(start.y, peak.y, 0.5f);
            Assert.That(mid.y, Is.GreaterThan(chordY));

            float minScale = float.MaxValue;
            float maxScale = float.MinValue;
            for (var index = 0; index < GoldPickupPresentation.CoinCount; index++)
            {
                float scale = GoldPickupPresentation.CoinScale(index);
                if (scale < minScale) minScale = scale;
                if (scale > maxScale) maxScale = scale;
            }

            Assert.That(maxScale, Is.GreaterThan(minScale + 0.04f));
            Assert.That(GoldPickupPresentation.CoinScale(2), Is.GreaterThan(GoldPickupPresentation.CoinScale(4)));
        }

        [Test]
        public void FailedMining_ClearsPendingGoldWithoutPlaying()
        {
            var host = new GameObject("GoldPickupHost");
            var vfx = host.AddComponent<GoldPickupVfx>();
            var mining = host.AddComponent<MiningSystem>();
            vfx.BindTo(mining, host.transform);
            vfx.SetPendingGold(50);
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
            Assert.That(
                File.ReadAllText(
                    Path.Combine(Application.dataPath, "_Project/Scripts/App/Integration/GoldPickupVfx.cs")),
                Does.Contain("pickupFont"));
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

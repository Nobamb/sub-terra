using System.Collections;
using NUnit.Framework;
using SubTerra.App.Integration;
using SubTerra.Gameplay.Mining;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.Tilemaps;

namespace SubTerra.App.Tests.PlayMode.MineDemo
{
    public sealed class PromptB1001GoldPickupPlayModeTests
    {
        [UnityTest]
        public IEnumerator Play_SpawnsCoinsFromCellThenExpires()
        {
            var texture = new Texture2D(8, 8);
            texture.SetPixel(0, 0, Color.yellow);
            texture.Apply();
            var sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, 8f, 8f),
                new Vector2(0.5f, 0.5f),
                8f);

            var grid = new GameObject("Grid", typeof(Grid));
            var mapObject = new GameObject("ForegroundTilemap");
            mapObject.transform.SetParent(grid.transform, false);
            var map = mapObject.AddComponent<Tilemap>();
            var host = new GameObject("GoldPickupHost");
            var player = new GameObject("Player");
            player.transform.position = new Vector3(1f, 2f, 0f);
            var vfx = host.AddComponent<GoldPickupVfx>();
            SetPrivate(vfx, "coinSprite", sprite);
            SetPrivate(vfx, "foregroundTilemap", map);

            var mining = host.AddComponent<MiningSystem>();
            vfx.BindTo(mining, player.transform, map);
            vfx.SetPendingGold(20);

            Vector3 origin = map.GetCellCenterWorld(new Vector3Int(3, -4, 0));
            vfx.Play(20, origin, player.transform.position + Vector3.up);
            Assert.That(vfx.ActiveCoinCount, Is.EqualTo(GoldPickupPresentation.CoinCount));

            var renderers = Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Exclude);
            Assert.That(renderers.Length, Is.GreaterThanOrEqualTo(GoldPickupPresentation.CoinCount));
            float minY = float.MaxValue;
            for (var i = 0; i < renderers.Length; i++)
            {
                if (renderers[i].transform.position.y < minY)
                {
                    minY = renderers[i].transform.position.y;
                }
            }

            Assert.That(minY, Is.LessThan(origin.y));

            yield return new WaitForSecondsRealtime(GoldPickupPresentation.MaxCoinLifetime() + 0.2f);
            Assert.That(vfx.ActiveCoinCount, Is.EqualTo(0));

            Object.Destroy(host);
            Object.Destroy(player);
            Object.Destroy(grid);
            Object.Destroy(texture);
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

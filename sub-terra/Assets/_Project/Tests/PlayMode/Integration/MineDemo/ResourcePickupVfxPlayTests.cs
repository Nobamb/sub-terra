#if UNITY_EDITOR
using System.Collections;
using System.IO;
using NUnit.Framework;
using SubTerra.App.Core.Data;
using SubTerra.App.Integration;
using SubTerra.App.Tests.PlayMode;
using SubTerra.Gameplay.Mining;
using SubTerra.Gameplay.Player;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SubTerra.App.Tests.PlayMode.MineDemo
{
    public sealed class ResourcePickupVfxPlayTests
    {
        private UiTestEnvironment env;
        private Camera capture;
        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            if (capture != null) Object.Destroy(capture.gameObject);
            env?.Dispose();
            var empty = SceneManager.CreateScene("ResourcePickupCleanup");
            SceneManager.SetActiveScene(empty);
            for (var i = SceneManager.sceneCount - 1; i >= 0; i--)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene != empty) yield return SceneManager.UnloadSceneAsync(scene);
            }
        }
        [UnityTest]
        public IEnumerator MiningCommitPlaysOnce_WithoutChangingRewards_AndRareLooksDistinct()
        {
            env = new UiTestEnvironment();
            yield return env.LoadIntegration(sceneLoadTimeout: 30f);
            var vfx = Object.FindAnyObjectByType<ResourcePickupVfx>();
            Assert.That(vfx, Is.Not.Null, "Runtime binder must attach the VFX.");
            vfx.ManualTick = true;
            var mining = Object.FindAnyObjectByType<MiningSystem>();
            var player = Object.FindAnyObjectByType<PlayerMovement>();
            var catalog = UnityEditor.AssetDatabase.LoadAssetAtPath<GameDataCatalog>("Assets/_Project/Data/Catalog/GameDataCatalog.asset");
            var resolver = (MiningTileResolver)new UnityEditor.SerializedObject(mining)
                .FindProperty("tileResolver").objectReferenceValue;
            Assert.That(resolver.TryFindTileById("tile.copper", out var copperTile), Is.True);
            var map = mining.ForegroundTilemap;
            var cell = map.WorldToCell(player.Position) + new Vector3Int(4, -2, 0);
            map.SetTile(cell, copperTile); // 슬롯 0의 격리 월드만 변경한다.
            env.Save.InventoryService.SetMaximumCapacity(10000f);
            var before = env.Save.InventoryService.State.GetQuantity(DataIds.Minerals.Copper);
            Assert.That(mining.TryMineInstant(cell), Is.True, mining.LastFailure.ToString());
            Assert.That(vfx.ActiveCount, Is.EqualTo(1));
            var after = env.Save.InventoryService.State.GetQuantity(DataIds.Minerals.Copper);
            Assert.That(after, Is.GreaterThan(before));
            vfx.Tick(1f);
            Assert.That(vfx.ActiveCount, Is.Zero);
            Assert.That(env.Save.InventoryService.State.GetQuantity(DataIds.Minerals.Copper), Is.EqualTo(after));
            Assert.That(mining.TryMineInstant(cell), Is.False);
            Assert.That(vfx.ActiveCount, Is.Zero, "Empty/failed mining must not start a new effect.");

            var origin = (Vector3)player.Position + Vector3.up * 0.4f;
            Assert.That(vfx.PlayAccepted(DataIds.Minerals.Copper, 1, origin + Vector3.left * 0.7f), Is.True);
            Assert.That(vfx.PlayAccepted(DataIds.RareItems.EngineFuel, 1, origin + Vector3.right * 0.7f), Is.True);
            if (SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                capture = new GameObject("ResourceCapture", typeof(Camera)).GetComponent<Camera>();
                capture.orthographic = true;
                capture.orthographicSize = 0.95f;
                capture.transform.position = (Vector3)player.Position + new Vector3(0f, 0.1f, -10f);
                capture.nearClipPlane = 0.1f;
                capture.farClipPlane = 50f;
                capture.enabled = false;
                var output = Path.GetFullPath("../work_process/images/resource-pickup-vfx");
                Directory.CreateDirectory(output);
                foreach (var age in new[] { 0.12f, 0.12f, 0.24f })
                {
                    vfx.Tick(age);
                    var target = RenderTexture.GetTemporary(320, 200, 24);
                    var previous = RenderTexture.active;
                    var image = new Texture2D(320, 200, TextureFormat.RGB24, false);
                    try
                    {
                        capture.targetTexture = target;
                        UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(capture,
                            new UnityEngine.Rendering.RenderPipeline.StandardRequest { destination = target });
                        RenderTexture.active = target;
                        image.ReadPixels(new Rect(0, 0, 320, 200), 0, 0);
                        image.Apply();
                        File.WriteAllBytes(Path.Combine(output, "remaining-" + vfx.ActiveCount + ".png"), image.EncodeToPNG());
                    }
                    finally
                    {
                        capture.targetTexture = null;
                        RenderTexture.active = previous;
                        RenderTexture.ReleaseTemporary(target);
                        Object.Destroy(image);
                    }
                }
            }
            vfx.enabled = false;
            Assert.That(vfx.ActiveCount, Is.Zero, "Real disable must hide all flights.");
        }
    }
}
#endif

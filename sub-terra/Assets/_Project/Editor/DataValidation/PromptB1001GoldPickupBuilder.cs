#if UNITY_EDITOR
using System;
using System.IO;
using SubTerra.App.Integration;
using SubTerra.Gameplay.Mining;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace SubTerra.App.Editor.DataValidation
{
    /// <summary>prompt-B 100-1 골드 획득 연출 에셋·HUD·Integration 배선만 갱신한다.</summary>
    public static class PromptB1001GoldPickupBuilder
    {
        public const string CoinSpritePath = "Assets/_Project/Art/FX/gold_coin_01.png";
        public const string FontTtfPath = "Assets/_Project/Fonts/SeoulAlrimTTF-Heavy.ttf";
        public const string FontSdfPath = "Assets/_Project/Fonts/SeoulAlrimTTF-Heavy_SDF.asset";
        public const string HudPrefabPath = "Assets/_Project/Prefabs/UI/HUDCanvas.prefab";
        public const string IntegrationScenePath =
            "Assets/_Project/Scenes/App/Mine_Demo_Integration.unity";

        private const string FlagPath = "Temp/subterra-prompt-b100-1-build.flag";
        private const string DonePath = "Temp/subterra-prompt-b100-1-build.done";
        private const string EditModeFlagPath = "Temp/subterra-prompt-b100-1-editmode.flag";
        private const string EditModeDonePath = "Temp/subterra-prompt-b100-1-editmode.done";
        private const string PlayModeFlagPath = "Temp/subterra-prompt-b100-1-playmode.flag";
        private const string PlayModeDonePath = "Temp/subterra-prompt-b100-1-playmode.done";
        private const string SeedCharacters = "0123456789G 골드획득!";
        private static TestRunnerApi activeApi;

        [InitializeOnLoadMethod]
        private static void WatchFlag()
        {
            EditorApplication.update -= PollFlag;
            EditorApplication.update += PollFlag;
        }

        private static void PollFlag()
        {
            if (File.Exists(FlagPath))
            {
                try
                {
                    File.Delete(FlagPath);
                    string result = Build();
                    File.WriteAllText(DonePath, result);
                    Debug.Log("[SubTerra] " + result);
                }
                catch (Exception ex)
                {
                    File.WriteAllText(DonePath, "FAIL " + ex.GetType().Name + " " + ex.Message);
                    Debug.LogError("[SubTerra] Prompt-B 100-1 gold pickup build failed: " + ex);
                }
            }

            if (File.Exists(EditModeFlagPath))
            {
                try
                {
                    File.Delete(EditModeFlagPath);
                    RunFocusedTests(
                        TestMode.EditMode,
                        "SubTerra.App.Tests.Integration.PromptB1001GoldPickupTests",
                        EditModeDonePath);
                }
                catch (Exception ex)
                {
                    File.WriteAllText(EditModeDonePath, "FAIL " + ex.GetType().Name + " " + ex.Message);
                }
            }

            if (File.Exists(PlayModeFlagPath))
            {
                try
                {
                    File.Delete(PlayModeFlagPath);
                    if (activeApi != null && !EditorApplication.isPlaying)
                    {
                        UnityEngine.Object.DestroyImmediate(activeApi);
                        activeApi = null;
                    }

                    RunFocusedTests(
                        TestMode.PlayMode,
                        "SubTerra.App.Tests.PlayMode.MineDemo.PromptB1001GoldPickupPlayModeTests",
                        PlayModeDonePath);
                }
                catch (Exception ex)
                {
                    File.WriteAllText(PlayModeDonePath, "FAIL " + ex.GetType().Name + " " + ex.Message);
                }
            }
        }

        private static void RunFocusedTests(TestMode mode, string testName, string donePath)
        {
            if (activeApi != null)
            {
                File.WriteAllText(donePath, "BUSY");
                return;
            }

            const string bootstrapPrefKey = "SubTerra.BootstrapPlayModeStartScene.Enabled";
            SceneAsset previousStartScene = null;
            bool previousBootstrapPref = true;
            if (mode == TestMode.PlayMode)
            {
                previousStartScene = EditorSceneManager.playModeStartScene;
                previousBootstrapPref = EditorPrefs.GetBool(bootstrapPrefKey, true);
                EditorPrefs.SetBool(bootstrapPrefKey, false);
                EditorSceneManager.playModeStartScene = null;
            }

            activeApi = ScriptableObject.CreateInstance<TestRunnerApi>();
            var receiver = new FocusedResultWriter(donePath, () =>
            {
                if (mode == TestMode.PlayMode)
                {
                    EditorPrefs.SetBool(bootstrapPrefKey, previousBootstrapPref);
                    EditorSceneManager.playModeStartScene = previousStartScene;
                }

                if (activeApi != null)
                {
                    UnityEngine.Object.DestroyImmediate(activeApi);
                    activeApi = null;
                }
            });
            activeApi.RegisterCallbacks(receiver);
            activeApi.Execute(new ExecutionSettings(new Filter
            {
                testMode = mode,
                testNames = new[] { testName }
            }));
        }

        private sealed class FocusedResultWriter : ICallbacks
        {
            private readonly string path;
            private readonly Action onFinished;
            private int pass;
            private int fail;
            private readonly System.Text.StringBuilder sb = new System.Text.StringBuilder();

            public FocusedResultWriter(string path, Action onFinished)
            {
                this.path = path;
                this.onFinished = onFinished;
            }

            public void RunStarted(ITestAdaptor testsToRun)
            {
                sb.AppendLine("Started " + testsToRun.Name);
            }

            public void RunFinished(ITestResultAdaptor result)
            {
                sb.AppendLine("Result: " + result.TestStatus);
                sb.AppendLine("Pass: " + pass + " Fail: " + fail);
                File.WriteAllText(path, sb.ToString());
                onFinished?.Invoke();
            }

            public void TestStarted(ITestAdaptor test)
            {
            }

            public void TestFinished(ITestResultAdaptor result)
            {
                if (result.HasChildren)
                {
                    return;
                }

                if (result.TestStatus == TestStatus.Passed)
                {
                    pass++;
                    return;
                }

                if (result.TestStatus == TestStatus.Failed)
                {
                    fail++;
                    sb.AppendLine("FAIL: " + result.FullName);
                    sb.AppendLine("  " + result.Message);
                }
            }
        }

        [MenuItem("SubTerra/UI/Build Prompt-B 100-1 Gold Pickup Vfx")]
        public static void BuildFromMenu()
        {
            Debug.Log("[SubTerra] " + Build());
        }

        public static string Build()
        {
            ConfigureCoinSprite();
            TMP_FontAsset font = GetOrCreatePickupFont();
            Sprite coin = AssetDatabase.LoadAssetAtPath<Sprite>(CoinSpritePath);
            if (coin == null)
            {
                throw new InvalidOperationException("골드 코인 스프라이트를 읽지 못했습니다: " + CoinSpritePath);
            }

            if (font == null)
            {
                throw new InvalidOperationException("SeoulAlrim TMP 폰트를 만들지 못했습니다: " + FontSdfPath);
            }

            WireHudPrefab(coin, font);
            WireIntegrationScene();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return "Prompt-B 100-1 gold pickup vfx wired. sprite=" + CoinSpritePath + " font=" + FontSdfPath;
        }

        private static void ConfigureCoinSprite()
        {
            var importer = AssetImporter.GetAtPath(CoinSpritePath) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException("골드 코인 PNG가 없습니다: " + CoinSpritePath);
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.spritePixelsPerUnit = 256f;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.npotScale = TextureImporterNPOTScale.None;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteAlignment = (int)SpriteAlignment.Center;
            settings.spritePivot = new Vector2(0.5f, 0.5f);
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }

        private static TMP_FontAsset GetOrCreatePickupFont()
        {
            var fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontSdfPath);
            if (fontAsset != null)
            {
                fontAsset.atlasPopulationMode = AtlasPopulationMode.Dynamic;
                fontAsset.TryAddCharacters(SeedCharacters, out _);
                EditorUtility.SetDirty(fontAsset);
                return fontAsset;
            }

            var source = AssetDatabase.LoadAssetAtPath<Font>(FontTtfPath);
            if (source == null)
            {
                return null;
            }

            fontAsset = TMP_FontAsset.CreateFontAsset(
                source,
                48,
                6,
                UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA,
                512,
                512,
                AtlasPopulationMode.Dynamic);
            if (fontAsset == null)
            {
                return null;
            }

            fontAsset.name = Path.GetFileNameWithoutExtension(FontSdfPath);
            fontAsset.atlasPopulationMode = AtlasPopulationMode.Dynamic;
            AssetDatabase.CreateAsset(fontAsset, FontSdfPath);
            if (fontAsset.material != null && !AssetDatabase.Contains(fontAsset.material))
            {
                fontAsset.material.name = fontAsset.name + " Material";
                AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
            }

            if (fontAsset.atlasTextures != null)
            {
                for (var i = 0; i < fontAsset.atlasTextures.Length; i++)
                {
                    var atlas = fontAsset.atlasTextures[i];
                    if (atlas == null || AssetDatabase.Contains(atlas))
                    {
                        continue;
                    }

                    atlas.name = fontAsset.name + " Atlas";
                    AssetDatabase.AddObjectToAsset(atlas, fontAsset);
                }
            }

            fontAsset.TryAddCharacters(SeedCharacters, out _);
            EditorUtility.SetDirty(fontAsset);
            AssetDatabase.SaveAssets();
            return fontAsset;
        }

        private static void WireHudPrefab(Sprite coin, TMP_FontAsset font)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(HudPrefabPath);
            try
            {
                var vfx = root.GetComponent<GoldPickupVfx>() ?? root.AddComponent<GoldPickupVfx>();
                var serialized = new SerializedObject(vfx);
                serialized.FindProperty("coinSprite").objectReferenceValue = coin;
                serialized.FindProperty("pickupFont").objectReferenceValue = font;
                serialized.FindProperty("coinWorldScale").floatValue = GoldPickupPresentation.CoinWorldScale;
                serialized.FindProperty("textFontSize").floatValue = 32f;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(vfx);
                PrefabUtility.SaveAsPrefabAsset(root, HudPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void WireIntegrationScene()
        {
            Scene scene = EditorSceneManager.OpenScene(IntegrationScenePath, OpenSceneMode.Single);
            var binder = FindInScene<IntegrationRuntimeBinder>(scene);
            var vfx = FindInScene<GoldPickupVfx>(scene);
            var mining = FindInScene<MiningSystem>(scene);
            var tilemap = FindForeground(scene);
            if (binder == null || vfx == null)
            {
                throw new InvalidOperationException(
                    "Integration Scene에 GoldPickupVfx 또는 IntegrationRuntimeBinder가 없습니다.");
            }

            var serializedVfx = new SerializedObject(vfx);
            if (tilemap != null)
            {
                serializedVfx.FindProperty("foregroundTilemap").objectReferenceValue = tilemap;
            }

            serializedVfx.ApplyModifiedPropertiesWithoutUndo();

            var serializedBinder = new SerializedObject(binder);
            serializedBinder.FindProperty("goldPickupVfx").objectReferenceValue = vfx;
            if (mining != null)
            {
                serializedBinder.FindProperty("miningSystem").objectReferenceValue = mining;
            }

            serializedBinder.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(binder);
            EditorUtility.SetDirty(vfx);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static Tilemap FindForeground(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Tilemap[] maps = root.GetComponentsInChildren<Tilemap>(true);
                for (var i = 0; i < maps.Length; i++)
                {
                    if (maps[i] != null
                        && maps[i].name.IndexOf("Foreground", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return maps[i];
                    }
                }
            }

            return FindInScene<Tilemap>(scene);
        }

        private static T FindInScene<T>(Scene scene) where T : Component
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                var component = root.GetComponentInChildren<T>(true);
                if (component != null)
                {
                    return component;
                }
            }

            return null;
        }
    }
}
#endif

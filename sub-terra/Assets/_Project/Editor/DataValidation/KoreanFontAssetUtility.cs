using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace SubTerra.App.Editor.DataValidation
{
    public static class KoreanFontAssetUtility
    {
        public const string FontDir = "Assets/_Project/Fonts";
        public const string NotoTtfPath = "Assets/_Project/Fonts/NotoSansKR-Regular.ttf";
        public const string NotoSdfPath1 = "Assets/_Project/Fonts/NotoSansKR-Regular_SDF.asset";
        public const string NotoSdfPath2 = "Assets/_Project/Fonts/NotoSansKR-Regular SDF.asset";

        public static TMP_FontAsset GetOrCreateKoreanFontAsset()
        {
            var fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(NotoSdfPath1);
            if (fontAsset == null)
            {
                fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(NotoSdfPath2);
            }

            if (fontAsset != null)
            {
                if (!HasRenderingResources(fontAsset))
                {
                    var font = AssetDatabase.LoadAssetAtPath<Font>(NotoTtfPath);
                    if (font == null || !RebuildDynamicSdf(fontAsset, font))
                    {
                        return null;
                    }
                }

                EnsureRenderingResourceNames(fontAsset);
                EnsureFallbackRegistered(fontAsset);
                return fontAsset;
            }

            if (File.Exists(NotoTtfPath))
            {
                var font = AssetDatabase.LoadAssetAtPath<Font>(NotoTtfPath);
                if (font != null)
                {
                    fontAsset = CreateDynamicSdf(font, NotoSdfPath1);
                    if (fontAsset != null)
                    {
                        EnsureFallbackRegistered(fontAsset);
                        return fontAsset;
                    }
                }
            }

            return null;
        }

        private static TMP_FontAsset CreateDynamicSdf(Font font, string savePath)
        {
            var fontAsset = TMP_FontAsset.CreateFontAsset(
                font,
                36,
                4,
                UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA,
                512,
                512,
                AtlasPopulationMode.Dynamic);

            if (fontAsset != null)
            {
                fontAsset.name = Path.GetFileNameWithoutExtension(savePath);
                fontAsset.atlasPopulationMode = AtlasPopulationMode.Dynamic;
                AssetDatabase.CreateAsset(fontAsset, savePath);
                AddRenderingResources(fontAsset);
                AssetDatabase.SaveAssets();
            }

            return fontAsset;
        }

        private static bool RebuildDynamicSdf(TMP_FontAsset fontAsset, Font font)
        {
            var rebuilt = TMP_FontAsset.CreateFontAsset(
                font,
                36,
                4,
                UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA,
                512,
                512,
                AtlasPopulationMode.Dynamic);
            if (rebuilt == null)
            {
                return false;
            }

            rebuilt.atlasPopulationMode = AtlasPopulationMode.Dynamic;
            var assetName = fontAsset.name;
            if (string.IsNullOrEmpty(assetName))
            {
                assetName = Path.GetFileNameWithoutExtension(
                    AssetDatabase.GetAssetPath(fontAsset));
            }

            EditorUtility.CopySerialized(rebuilt, fontAsset);
            fontAsset.name = assetName;
            AddRenderingResources(fontAsset);
            EditorUtility.SetDirty(fontAsset);
            AssetDatabase.SaveAssets();
            return HasRenderingResources(fontAsset);
        }

        private static void AddRenderingResources(TMP_FontAsset fontAsset)
        {
            if (fontAsset.material != null
                && !AssetDatabase.Contains(fontAsset.material))
            {
                fontAsset.material.name = fontAsset.name + " Material";
                AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
            }

            var atlasTextures = fontAsset.atlasTextures;
            if (atlasTextures == null)
            {
                return;
            }

            for (var i = 0; i < atlasTextures.Length; i++)
            {
                var atlas = atlasTextures[i];
                if (atlas == null || AssetDatabase.Contains(atlas))
                {
                    continue;
                }

                atlas.name = fontAsset.name + " Atlas";
                AssetDatabase.AddObjectToAsset(atlas, fontAsset);
            }
        }

        private static void EnsureRenderingResourceNames(TMP_FontAsset fontAsset)
        {
            var path = AssetDatabase.GetAssetPath(fontAsset);
            var assetName = Path.GetFileNameWithoutExtension(path);
            var changed = false;

            if (!string.IsNullOrEmpty(assetName) && fontAsset.name != assetName)
            {
                fontAsset.name = assetName;
                EditorUtility.SetDirty(fontAsset);
                changed = true;
            }

            if (fontAsset.material != null)
            {
                var materialName = assetName + " Material";
                if (fontAsset.material.name != materialName)
                {
                    fontAsset.material.name = materialName;
                    EditorUtility.SetDirty(fontAsset.material);
                    changed = true;
                }
            }

            var atlasTextures = fontAsset.atlasTextures;
            if (atlasTextures != null)
            {
                for (var i = 0; i < atlasTextures.Length; i++)
                {
                    var atlas = atlasTextures[i];
                    var atlasName = assetName + " Atlas";
                    if (atlas == null || atlas.name == atlasName)
                    {
                        continue;
                    }

                    atlas.name = atlasName;
                    EditorUtility.SetDirty(atlas);
                    changed = true;
                }
            }

            if (changed)
            {
                AssetDatabase.SaveAssets();
            }
        }

        private static bool HasRenderingResources(TMP_FontAsset fontAsset)
        {
            return fontAsset != null
                && fontAsset.material != null
                && fontAsset.atlasTextures != null
                && fontAsset.atlasTextures.Length > 0
                && fontAsset.atlasTextures[0] != null;
        }

        private static void EnsureFallbackRegistered(TMP_FontAsset fontAsset)
        {
            if (fontAsset == null)
            {
                return;
            }

            var defaultFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            if (defaultFont != null)
            {
                if (defaultFont.fallbackFontAssetTable == null)
                {
                    defaultFont.fallbackFontAssetTable = new List<TMP_FontAsset>();
                }

                if (!defaultFont.fallbackFontAssetTable.Contains(fontAsset))
                {
                    defaultFont.fallbackFontAssetTable.Add(fontAsset);
                    EditorUtility.SetDirty(defaultFont);
                }
            }
        }
    }
}

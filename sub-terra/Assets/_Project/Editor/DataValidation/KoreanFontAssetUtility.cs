using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace SubTerra.App.Editor.DataValidation
{
    public static class KoreanFontAssetUtility
    {
        [MenuItem("SubTerra/UI/Repair Korean Font Atlas")]
        public static void RepairFromMenu()
        {
            Debug.Log("[SubTerra] " + RepairIfBroken());
        }

        public static string RepairIfBroken()
        {
            var fontAsset = GetOrCreateKoreanFontAsset();
            if (fontAsset == null)
            {
                return "Korean font repair failed.";
            }

            var atlas = fontAsset.atlasTexture;
            var size = atlas != null ? atlas.width + "x" + atlas.height : "null";
            var chars = fontAsset.characterTable != null ? fontAsset.characterTable.Count : 0;
            StripStaleAtlasTextures(fontAsset);
            atlas = fontAsset.atlasTextures != null && fontAsset.atlasTextures.Length > 0
                ? fontAsset.atlasTextures[0]
                : fontAsset.atlasTexture;
            size = atlas != null ? atlas.width + "x" + atlas.height : "null";
            chars = fontAsset.characterTable != null ? fontAsset.characterTable.Count : 0;
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return "Korean font ready atlas=" + size + " chars=" + chars;
        }

        public const string FontDir = "Assets/_Project/Fonts";
        public const string NotoTtfPath = "Assets/_Project/Fonts/NotoSansKR-Regular.ttf";
        public const string NotoSdfPath1 = "Assets/_Project/Fonts/NotoSansKR-Regular_SDF.asset";
        public const string NotoSdfPath2 = "Assets/_Project/Fonts/NotoSansKR-Regular SDF.asset";

        private const int AtlasSize = 1024;

        private const string SeedCharacters =
            "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789"
            + " .,;:!?-_'/\\\"()[]{}|+*=#%&@<>~^`"
            + "새게임이어하기설정종료슬롯골드깊이선택가능확인취소덮어쓰기"
            + "해상도언어프레임마스터음량기본값적용전환자동한국어화면진동억제"
            + "구리철리튬버팀목사다리조명충전기보관함정산콘솔전진기지코어긴급탈출포탈"
            + "드릴속도전력이율최대화물중량드론스캔범위구조보존가스저항"
            + "채굴탐사안전위험전력연결미연결인벤토리판매제작업그레이드목표"
            + "시설건설배치미리보기설치취소충전보관함정산체크포인트"
            + "유독가스탁함생성접근범위불투명도";

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
                EnsureSeedCharacters(fontAsset);
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
                AtlasSize,
                AtlasSize,
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
                AtlasSize,
                AtlasSize,
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
            SetClearDynamicDataOnBuild(fontAsset, false);
            AddRenderingResources(fontAsset);
            EnsureSeedCharacters(fontAsset);
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
            if (fontAsset == null
                || fontAsset.material == null
                || fontAsset.atlasTextures == null
                || fontAsset.atlasTextures.Length == 0)
            {
                return false;
            }

            var atlas = fontAsset.atlasTextures[0];
            return atlas != null && atlas.width > 1 && atlas.height > 1;
        }

        private static void EnsureSeedCharacters(TMP_FontAsset fontAsset)
        {
            if (fontAsset == null)
            {
                return;
            }

            fontAsset.atlasPopulationMode = AtlasPopulationMode.Dynamic;
            SetClearDynamicDataOnBuild(fontAsset, false);
            fontAsset.TryAddCharacters(SeedCharacters, out _);
            EditorUtility.SetDirty(fontAsset);
        }

        private static void StripStaleAtlasTextures(TMP_FontAsset fontAsset)
        {
            if (fontAsset == null)
            {
                return;
            }

            var keep = fontAsset.atlasTextures != null && fontAsset.atlasTextures.Length > 0
                ? fontAsset.atlasTextures[0]
                : null;
            if (keep == null)
            {
                return;
            }

            var path = AssetDatabase.GetAssetPath(fontAsset);
            var assets = AssetDatabase.LoadAllAssetsAtPath(path);
            for (var i = 0; i < assets.Length; i++)
            {
                var texture = assets[i] as Texture2D;
                if (texture == null || texture == keep)
                {
                    continue;
                }

                Object.DestroyImmediate(texture, true);
            }

            if (fontAsset.material != null)
            {
                fontAsset.material.mainTexture = keep;
                fontAsset.material.SetFloat("_TextureWidth", keep.width);
                fontAsset.material.SetFloat("_TextureHeight", keep.height);
                EditorUtility.SetDirty(fontAsset.material);
            }

            var so = new SerializedObject(fontAsset);
            var atlasTexture = so.FindProperty("m_AtlasTexture");
            if (atlasTexture != null)
            {
                atlasTexture.objectReferenceValue = keep;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            fontAsset.ReadFontAssetDefinition();
            EditorUtility.SetDirty(fontAsset);
        }

        private static void SetClearDynamicDataOnBuild(TMP_FontAsset fontAsset, bool clear)
        {
            var so = new SerializedObject(fontAsset);
            var property = so.FindProperty("m_ClearDynamicDataOnBuild");
            if (property == null)
            {
                return;
            }

            property.boolValue = clear;
            so.ApplyModifiedPropertiesWithoutUndo();
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

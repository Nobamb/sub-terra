using System;
using SubTerra.App.UI.HUD;
using SubTerra.App.UI.Progression;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace SubTerra.App.Editor.DataValidation
{
    /// <summary>
    /// 프롬프트 B-154: 심층 구역 해금 창 스킨(Resources/UI/DeepZoneUnlockPopupSkin.asset)만 만든다.
    /// 창은 ProgressionPanelView가 런타임에 만들므로 씬·프리팹은 건드리지 않는다.
    /// 깊이는 MineLayerDistribution.asset에서 signalDepth가 속한 띠를 읽는다(채굴 경계와 같은 규칙).
    /// </summary>
    public static class PromptB154DeepZoneUnlockPopupBuilder
    {
        public const string SkinAssetPath = "Assets/_Project/Resources/UI/DeepZoneUnlockPopupSkin.asset";
        public const string DistributionPath = "Assets/_Project/Data/World/MineLayerDistribution.asset";
        private const string SettingsArt = "Assets/_Project/Art/UI/MainMenu/Settings/";
        public const string MineResetSkinPath = "Assets/_Project/Resources/UI/MineResetTimedPopupSkin.asset";
        // B-154-1 심층 발견 그림: Codex 생성 원본을 work_process/.../deep-zone-popup/tools/build_deep_zone_art.py로 레이어화했다.
        public const string ArtPath = "Assets/_Project/Art/UI/Gameplay/DeepZone/Unlock/";
        private static readonly string[] ArtFiles =
        {
            "deep-zone-emblem.png",
            "deep-zone-border.png",
            "deep-zone-border-glow.png",
            "deep-zone-glow-crystals-left.png",
            "deep-zone-glow-crystals-right.png",
            "deep-zone-glow-core.png",
            "deep-zone-ring.png"
        };

        [MenuItem("SubTerra/UI/Build Prompt-B 154 Deep Zone Unlock Popup")]
        public static void BuildFromMenu()
        {
            Debug.Log("[SubTerra] " + Build());
        }

        public static string Build()
        {
            if (EditorApplication.isPlaying)
            {
                throw new InvalidOperationException("Stop Play Mode first.");
            }

            if (!TryReadDeepBand(out var minDepth, out var maxDepth))
            {
                throw new InvalidOperationException("MineLayerDistribution에서 심층 띠를 찾지 못했습니다: " + DistributionPath);
            }

            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(PromptB120StartBriefingBuilder.BriefingFontPath);
            if (font == null)
            {
                throw new InvalidOperationException("브리핑 폰트가 없습니다: " + PromptB120StartBriefingBuilder.BriefingFontPath);
            }

            // 정적 아틀라스에 새 글자만 굽고 다시 정적으로 돌린다. 기존 글자는 건드리지 않는다.
            var fontReport = KoreanFontAssetUtility.EnsureCharactersSaved(font, DeepZoneUnlockPopupText.FontCharacters);
            if (font.atlasPopulationMode != AtlasPopulationMode.Static)
            {
                font.atlasPopulationMode = AtlasPopulationMode.Static;
                EditorUtility.SetDirty(font);
                AssetDatabase.SaveAssetIfDirty(font);
            }

            if (!font.HasCharacters(DeepZoneUnlockPopupText.FontCharacters, out var missing))
            {
                throw new InvalidOperationException("브리핑 폰트에 글자를 넣지 못했습니다: " + new string(missing.ToArray()));
            }

            var skin = AssetDatabase.LoadAssetAtPath<DeepZoneUnlockPopupSkin>(SkinAssetPath);
            if (skin == null)
            {
                if (!AssetDatabase.IsValidFolder("Assets/_Project/Resources/UI"))
                {
                    AssetDatabase.CreateFolder("Assets/_Project/Resources", "UI");
                }

                skin = ScriptableObject.CreateInstance<DeepZoneUnlockPopupSkin>();
                AssetDatabase.CreateAsset(skin, SkinAssetPath);
            }

            // B-154-1: 카드 프레임·바탕·구분선·스캔 빛·입자는 광산 초기화 알림 스킨이 가리키는 그림을 그대로 쓴다.
            var mineReset = AssetDatabase.LoadAssetAtPath<MineResetTimedPopupSkin>(MineResetSkinPath);
            if (mineReset == null)
            {
                throw new InvalidOperationException("광산 초기화 스킨이 없습니다: " + MineResetSkinPath);
            }

            skin.frame = mineReset.frame;
            skin.frameGlow = mineReset.frameGlow;
            skin.panel = mineReset.panel;
            skin.titleDivider = mineReset.titleDivider;
            skin.scanLine = mineReset.scanLine;
            skin.mote = mineReset.mote;

            ConfigureArtImport();
            skin.emblem = Load(ArtPath + "deep-zone-emblem.png");
            skin.emblemBorder = Load(ArtPath + "deep-zone-border.png");
            skin.emblemBorderGlow = Load(ArtPath + "deep-zone-border-glow.png");
            skin.emblemCrystalGlows = new[]
            {
                Load(ArtPath + "deep-zone-glow-crystals-left.png"),
                Load(ArtPath + "deep-zone-glow-crystals-right.png")
            };
            skin.emblemCoreGlow = Load(ArtPath + "deep-zone-glow-core.png");
            skin.emblemRing = Load(ArtPath + "deep-zone-ring.png");

            skin.buttonOff = Load(SettingsArt + "button-active-off.png");
            skin.buttonOn = Load(SettingsArt + "button-active-on.png");
            skin.buttonSweep = Load(SettingsArt + "slider-glow.png");
            skin.buttonParticle = Load(SettingsArt + "particle-dot.png");
            skin.font = font;
            skin.minDepth = minDepth;
            skin.maxDepth = maxDepth;
            EditorUtility.SetDirty(skin);
            AssetDatabase.SaveAssetIfDirty(skin);
            return "Prompt-B 154 deep zone unlock popup skin built: " + SkinAssetPath
                + " depth " + minDepth + "~" + maxDepth + ", font " + fontReport;
        }

        /// <summary>MineLayerTilemapGenerator.ConfigureDeepZoneMiningBoundary와 같이 signalDepth가 속한 띠를 고른다.</summary>
        public static bool TryReadDeepBand(out int minDepth, out int maxDepth)
        {
            minDepth = 0;
            maxDepth = 0;
            var asset = AssetDatabase.LoadMainAssetAtPath(DistributionPath);
            if (asset == null)
            {
                return false;
            }

            // DemoWorld 어셈블리를 참조하지 않도록 직렬화 값으로 읽는다.
            var serialized = new SerializedObject(asset);
            var signal = serialized.FindProperty("signalDepth");
            var bands = serialized.FindProperty("bands");
            if (signal == null || bands == null || !bands.isArray)
            {
                return false;
            }

            for (var i = 0; i < bands.arraySize; i++)
            {
                var band = bands.GetArrayElementAtIndex(i);
                var min = band.FindPropertyRelative("minDepth").intValue;
                var max = band.FindPropertyRelative("maxDepth").intValue;
                if (signal.intValue >= min && signal.intValue <= max)
                {
                    minDepth = min;
                    maxDepth = max;
                    return true;
                }
            }

            return false;
        }

        // 광산 초기화 그림(MineResetSurfaceBaseLayoutBuilder)과 같은 가져오기 설정.
        private static void ConfigureArtImport()
        {
            foreach (var name in ArtFiles)
            {
                var path = ArtPath + name;
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null)
                {
                    Debug.LogWarning("[SubTerra] Deep zone unlock art missing: " + path);
                    continue;
                }

                var changed = importer.textureType != TextureImporterType.Sprite
                    || importer.spriteImportMode != SpriteImportMode.Single
                    || importer.mipmapEnabled
                    || !importer.alphaIsTransparency
                    || importer.maxTextureSize != 2048
                    || importer.textureCompression != TextureImporterCompression.CompressedHQ
                    || importer.wrapMode != TextureWrapMode.Clamp;
                if (!changed)
                {
                    continue;
                }

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.maxTextureSize = 2048;
                importer.textureCompression = TextureImporterCompression.CompressedHQ;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Bilinear;
                importer.SaveAndReimport();
            }
        }

        private static Sprite Load(string path)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
            {
                Debug.LogWarning("[SubTerra] Deep zone unlock popup sprite missing: " + path);
            }

            return sprite;
        }
    }
}

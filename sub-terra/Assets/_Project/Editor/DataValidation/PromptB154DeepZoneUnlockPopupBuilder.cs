using System;
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

            skin.frame = Load(PromptB120StartBriefingBuilder.FramePath);
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

using SubTerra.App.UI.Sell;
using UnityEditor;
using UnityEngine;

namespace SubTerra.App.Editor.DataValidation
{
    /// <summary>
    /// 프롬프트 B-136: 공통 판매 팝업이 런타임에 쓰는 아이콘 참조 에셋(ResourceSellSkin)만 만든다.
    /// 새 스프라이트는 없고 지상 기지 상단 자원 표시의 기존 골드·화물 아이콘을 가리킨다.
    /// 프리팹·씬은 열거나 저장하지 않는다.
    /// </summary>
    public static class PromptB136ResourceSellSkinBuilder
    {
        public const string SkinAssetPath = "Assets/_Project/Resources/UI/ResourceSellSkin.asset";
        public const string GoldIconPath = "Assets/_Project/Art/UI/SurfaceBase/Icons/icon-gold.png";
        public const string CargoIconPath = "Assets/_Project/Art/UI/SurfaceBase/Icons/icon-cargo.png";

        [MenuItem("SubTerra/UI/Build Prompt-B 136 Resource Sell Skin")]
        public static void BuildFromMenu()
        {
            Build();
        }

        public static ResourceSellSkin Build()
        {
            var skin = AssetDatabase.LoadAssetAtPath<ResourceSellSkin>(SkinAssetPath);
            if (skin == null)
            {
                skin = ScriptableObject.CreateInstance<ResourceSellSkin>();
                AssetDatabase.CreateAsset(skin, SkinAssetPath);
            }

            skin.goldIcon = Load(GoldIconPath);
            skin.cargoIcon = Load(CargoIconPath);
            EditorUtility.SetDirty(skin);
            AssetDatabase.SaveAssetIfDirty(skin);
            return skin;
        }

        private static Sprite Load(string path)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
            {
                Debug.LogWarning("[SubTerra] Resource sell icon missing: " + path);
            }

            return sprite;
        }
    }
}

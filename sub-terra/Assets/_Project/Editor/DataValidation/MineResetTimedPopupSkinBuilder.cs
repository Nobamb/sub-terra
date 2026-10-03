using SubTerra.App.UI.HUD;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace SubTerra.App.Editor.DataValidation
{
    /// <summary>
    /// 프롬프트 B-124: 3시간 만료 알림이 런타임에 쓰는 그림 참조 에셋(MineResetTimedPopupSkin)만 만든다.
    /// 새 스프라이트는 만들지 않고, B-123-2 확인창이 쓰는 기존 그림 경로를 그대로 가리킨다.
    /// 프리팹·씬은 열거나 저장하지 않는다.
    /// </summary>
    public static class MineResetTimedPopupSkinBuilder
    {
        public const string SkinAssetPath = "Assets/_Project/Resources/UI/MineResetTimedPopupSkin.asset";
        private const string ArtFolder = MineResetSurfaceBaseLayoutBuilder.ArtFolder;

        [MenuItem("SubTerra/UI/Build Prompt-B 124 Timed Reset Popup Skin")]
        public static void BuildFromMenu()
        {
            Build();
        }

        public static MineResetTimedPopupSkin Build()
        {
            var skin = AssetDatabase.LoadAssetAtPath<MineResetTimedPopupSkin>(SkinAssetPath);
            if (skin == null)
            {
                EnsureFolder("Assets/_Project/Resources", "UI");
                skin = ScriptableObject.CreateInstance<MineResetTimedPopupSkin>();
                AssetDatabase.CreateAsset(skin, SkinAssetPath);
            }

            skin.frame = Load(MineResetSurfaceBaseLayoutBuilder.FramePath);
            skin.frameGlow = Load(MineResetSurfaceBaseLayoutBuilder.FrameGlowPath);
            skin.panel = Load(MineResetSurfaceBaseLayoutBuilder.PanelPath);
            skin.cave = Load(MineResetSurfaceBaseLayoutBuilder.CavePath);
            var glowPaths = MineResetSurfaceBaseLayoutBuilder.CaveGlowPaths;
            skin.caveGlows = new Sprite[glowPaths.Length];
            for (var i = 0; i < glowPaths.Length; i++)
            {
                skin.caveGlows[i] = Load(glowPaths[i]);
            }

            skin.hexMine = Load(MineResetSurfaceBaseLayoutBuilder.HexMinePath);
            skin.hexBorder = Load(MineResetSurfaceBaseLayoutBuilder.HexBorderPath);
            skin.hexBorderGlow = Load(MineResetSurfaceBaseLayoutBuilder.HexBorderGlowPath);
            skin.hexCrystalGlow = Load(MineResetSurfaceBaseLayoutBuilder.HexCrystalGlowPath);
            skin.hexTunnelGlow = Load(MineResetSurfaceBaseLayoutBuilder.HexTunnelGlowPath);
            skin.hexRings = Load(MineResetSurfaceBaseLayoutBuilder.HexRingsPath);
            skin.coreGlow = Load(MineResetSurfaceBaseLayoutBuilder.CoreGlowPath);
            skin.scanLine = Load(MineResetSurfaceBaseLayoutBuilder.ScanLinePath);
            skin.titleDivider = Load(MineResetSurfaceBaseLayoutBuilder.TitleDividerPath);
            skin.buttonConfirm = Load(MineResetSurfaceBaseLayoutBuilder.ButtonConfirmPath);
            skin.buttonConfirmHover = Load(MineResetSurfaceBaseLayoutBuilder.ButtonConfirmHoverPath);
            skin.mote = Load(MineResetSurfaceBaseLayoutBuilder.MotePath);
            skin.titleGlowMaterial = AssetDatabase.LoadAssetAtPath<Material>(MineResetSurfaceBaseLayoutBuilder.TitleMaterialPath);
            skin.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(MineResetSurfaceBaseLayoutBuilder.FontPath);
            EditorUtility.SetDirty(skin);
            AssetDatabase.SaveAssets();
            return skin;
        }

        private static Sprite Load(string path)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
            {
                Debug.LogWarning("[SubTerra] Timed reset popup sprite missing: " + path);
            }

            return sprite;
        }

        private static void EnsureFolder(string parent, string name)
        {
            if (!AssetDatabase.IsValidFolder(parent + "/" + name))
            {
                AssetDatabase.CreateFolder(parent, name);
            }
        }
    }
}

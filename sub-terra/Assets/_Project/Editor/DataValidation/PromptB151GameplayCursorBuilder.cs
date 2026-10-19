using System.Collections.Generic;
using System.Linq;
using SubTerra.App.UI.GameplayCursor;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace SubTerra.App.Editor.DataValidation
{
    /// <summary>
    /// 프롬프트 B-151: 드릴(2×4)·집게(1×4) 커서 시트를 자르고 끝(왼쪽 위) 피벗을 맞춘 뒤 GameplayCursorSkin을 만든다.
    /// 시트 폭이 칸 수로 나눠떨어지지 않아(1774/4, 1254/4) Grid 대신 같은 크기 칸으로 수동 슬라이스한다. 프리팹·씬은 열지 않는다.
    /// </summary>
    public static class PromptB151GameplayCursorBuilder
    {
        public const string ArtPath = "Assets/_Project/Art/UI/Gameplay/Cursor/";
        public const string DrillSheetPath = ArtPath + "mouse-cursor.png";
        public const string ClawSheetPath = ArtPath + "cursor-right-click.png";
        public const string SkinAssetPath = "Assets/_Project/Resources/UI/GameplayCursorSkin.asset";

        // 칸 크기와 칸 안 끝 좌표(왼쪽 위 기준 px). 알파 경계로 측정한 값이며 모든 칸이 같은 위치에 정렬돼 있다.
        public const int DrillCell = 443;
        public const int ClawCell = 313;
        public static readonly Vector2 DrillTipPx = new Vector2(55f, 60f);
        public static readonly Vector2 ClawTipPx = new Vector2(64f, 44f);

        [MenuItem("SubTerra/UI/Build Prompt-B 151 Gameplay Cursor")]
        public static void BuildFromMenu()
        {
            Build();
        }

        public static GameplayCursorSkin Build()
        {
            SliceSheet(DrillSheetPath, "drill", 4, 2, DrillCell, DrillTipPx);
            SliceSheet(ClawSheetPath, "claw", 4, 1, ClawCell, ClawTipPx);

            var skin = AssetDatabase.LoadAssetAtPath<GameplayCursorSkin>(SkinAssetPath);
            if (skin == null)
            {
                skin = ScriptableObject.CreateInstance<GameplayCursorSkin>();
                AssetDatabase.CreateAsset(skin, SkinAssetPath);
            }

            // 연출 수치는 인스펙터 조정값을 보존하고 스프라이트 참조만 갱신한다.
            skin.drillFrames = LoadFrames(DrillSheetPath, "drill", GameplayCursorSkin.DrillFrameCount);
            skin.clawFrames = LoadFrames(ClawSheetPath, "claw", GameplayCursorSkin.ClawFrameCount);
            EditorUtility.SetDirty(skin);
            AssetDatabase.SaveAssetIfDirty(skin);
            Debug.Log("[PromptB151GameplayCursorBuilder] Build completed.");
            return skin;
        }

        public static string FrameName(string prefix, int index)
        {
            return prefix + "_" + (index + 1).ToString("00");
        }

        private static Sprite[] LoadFrames(string path, string prefix, int count)
        {
            var sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToDictionary(s => s.name);
            var frames = new Sprite[count];
            for (var i = 0; i < count; i++)
            {
                if (!sprites.TryGetValue(FrameName(prefix, i), out frames[i]))
                    Debug.LogError("[PromptB151GameplayCursorBuilder] Missing frame: " + FrameName(prefix, i));
            }
            return frames;
        }

        private static void SliceSheet(string path, string prefix, int columns, int rows, int cell, Vector2 tipPx)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError("[PromptB151GameplayCursorBuilder] Missing art: " + path);
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.alphaIsTransparency = true;
            // 443px 칸을 48px 안팎으로 줄여 그리므로 밉맵으로 계단·반짝임을 줄인다.
            importer.mipmapEnabled = true;
            importer.filterMode = FilterMode.Trilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 2048;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.GetSourceTextureWidthAndHeight(out var width, out var height);

            var factory = new SpriteDataProviderFactories();
            factory.Init();
            var provider = factory.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();
            // 재실행해도 스프라이트 ID(참조)가 바뀌지 않게 이름이 같은 기존 칸의 ID를 이어 쓴다.
            var existing = provider.GetSpriteRects().ToDictionary(r => r.name, r => r.spriteID);
            var pivot = new Vector2(tipPx.x / cell, 1f - tipPx.y / cell);
            var rects = new List<SpriteRect>();
            for (var row = 0; row < rows; row++)
                for (var col = 0; col < columns; col++)
                {
                    var name = FrameName(prefix, row * columns + col);
                    var x = Mathf.FloorToInt(col * (float)width / columns);
                    rects.Add(new SpriteRect
                    {
                        name = name,
                        rect = new Rect(x, height - (row + 1) * cell, cell, cell),
                        alignment = SpriteAlignment.Custom,
                        pivot = pivot,
                        spriteID = existing.TryGetValue(name, out var id) ? id : GUID.Generate()
                    });
                }

            provider.SetSpriteRects(rects.ToArray());
            var names = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
            if (names != null) names.SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)));
            provider.Apply();
            importer.SaveAndReimport();
        }
    }
}

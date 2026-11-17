using System.Collections.Generic;
using System.IO;
using SubTerra.App.Core.Data;
using SubTerra.Gameplay.Building;
using SubTerra.Gameplay.Structural;
using SubTerra.Shared;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace SubTerra.App.Editor.DataValidation
{
    /// <summary>
    /// B-199 완충판: 기존 버팀목 프리팹을 윗면 1장 + 반복 기둥(Tiled) 구조로 바꾸고 CushionPad를 붙인다.
    /// 명시한 경로(프리팹·데이터·임시 아트·건설 메뉴 라벨)만 건드리며 씬은 저장하지 않는다.
    /// 임시 아트는 기존 support_pillar_mine.png를 잘라 만들고, 이미 있는 파일은 덮어쓰지 않는다(최종 아트 보호).
    /// </summary>
    public static class PromptB199CushionPadBuilder
    {
        public const string SupportPrefabPath = PhaseFSupportBuilder.SupportPrefabPath;
        public const string SupportDataPath = PhaseFSupportBuilder.SupportDataPath;
        public const string SourceArtPath = PhaseFSupportBuilder.SupportArtPath;
        public const string TopArtPath = "Assets/_Project/Art/Facilities/MVP/cushion_pad_top.png";
        public const string PostArtPath = "Assets/_Project/Art/Facilities/MVP/cushion_pad_post.png";
        public const string BuildingMenuPrefabPath = "Assets/_Project/Prefabs/UI/BuildingMenu.prefab";
        public const string DisplayName = "완충판";
        public const string Description =
            "착지 피해를 약 10%로 줄여 주는 발판입니다. 위에 서서 위/아래 방향키로 높이를 조절합니다.";

        // 원본 256x563 PNG(위쪽이 y=0 기준). 윗면: 위쪽 150행, 기둥 한 조각: x 78~177, 150~291행.
        private const int SourceWidth = 256;
        private const int SourceHeight = 563;
        private const int TopRows = 150;
        private const int PostX = 78;
        private const int PostWidthPx = 100;
        private const int PostTopRow = 150;
        private const int PostRows = 142;
        private const float PixelsPerUnit = 256f;

        [MenuItem("SubTerra/MVP2/Build B-199 Cushion Pad")]
        public static void BuildFromMenu() => Debug.Log("[SubTerra] " + Build());

        public static string Build()
        {
            EnsureTemporaryArt();
            var top = AssetDatabase.LoadAssetAtPath<Sprite>(TopArtPath);
            var post = AssetDatabase.LoadAssetAtPath<Sprite>(PostArtPath);
            if (top == null || post == null)
                throw new System.InvalidOperationException("Cushion pad sprites could not be imported.");

            var prefab = BuildPrefab(top, post);
            RenameData(prefab);
            RenameMenuLabel();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return "B-199 cushion pad prefab/data wired (min "
                + CushionPadRules.DefaultMinHeight + ", max " + CushionPadRules.DefaultMaxHeight + ").";
        }

        private static void EnsureTemporaryArt()
        {
            bool topExists = File.Exists(TopArtPath);
            bool postExists = File.Exists(PostArtPath);
            if (!topExists || !postExists)
            {
                var bytes = File.ReadAllBytes(SourceArtPath);
                var source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                try
                {
                    source.LoadImage(bytes);
                    if (source.height != SourceHeight || source.width != SourceWidth)
                        throw new System.InvalidOperationException(
                            "support_pillar_mine.png size changed: " + source.width + "x" + source.height);
                    if (!topExists) WriteCrop(source, 0, SourceHeight - TopRows, source.width, TopRows, TopArtPath);
                    if (!postExists)
                        WriteCrop(source, PostX, SourceHeight - PostTopRow - PostRows, PostWidthPx, PostRows, PostArtPath);
                }
                finally
                {
                    Object.DestroyImmediate(source);
                }

                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            }

            ConfigureImporter(TopArtPath, new Vector2(0.5f, 1f), SpriteMeshType.FullRect);
            ConfigureImporter(PostArtPath, new Vector2(0.5f, 0f), SpriteMeshType.FullRect);
        }

        private static void WriteCrop(Texture2D source, int x, int y, int width, int height, string path)
        {
            var crop = new Texture2D(width, height, TextureFormat.RGBA32, false);
            try
            {
                crop.SetPixels(source.GetPixels(x, y, width, height));
                crop.Apply();
                File.WriteAllBytes(path, crop.EncodeToPNG());
            }
            finally
            {
                Object.DestroyImmediate(crop);
            }
        }

        private static void ConfigureImporter(string path, Vector2 pivot, SpriteMeshType meshType)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new System.InvalidOperationException("Not a texture: " + path);

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            bool changed = importer.textureType != TextureImporterType.Sprite
                || settings.spriteMode != (int)SpriteImportMode.Single
                || !Mathf.Approximately(settings.spritePixelsPerUnit, PixelsPerUnit)
                || settings.spriteMeshType != meshType
                || settings.spriteAlignment != (int)SpriteAlignment.Custom
                || settings.spritePivot != pivot;
            if (!changed) return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = meshType;
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = pivot;
            settings.spriteExtrude = 1;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }

        private static GameObject BuildPrefab(Sprite top, Sprite post)
        {
            var root = PrefabUtility.LoadPrefabContents(SupportPrefabPath);
            try
            {
                var visual = root.transform.Find("VisualRoot");
                if (visual == null) throw new System.InvalidOperationException("VisualRoot is missing in " + SupportPrefabPath);

                // 미리보기는 VisualRoot의 첫 활성 렌더러를 대표로 쓰므로 윗면(Cap)을 맨 앞에 둔다.
                var cap = visual.Find("Cap");
                var postTransform = visual.Find("Post");
                if (cap == null || postTransform == null)
                    throw new System.InvalidOperationException("Cap/Post visuals are missing.");
                cap.SetSiblingIndex(0);

                var capRenderer = cap.GetComponent<SpriteRenderer>();
                capRenderer.enabled = true;
                capRenderer.sprite = top;
                capRenderer.drawMode = SpriteDrawMode.Simple;
                capRenderer.color = Color.white;
                capRenderer.sortingOrder = 5;
                cap.localScale = Vector3.one;

                var postRenderer = postTransform.GetComponent<SpriteRenderer>();
                postRenderer.sprite = post;
                postRenderer.color = Color.white;
                postRenderer.drawMode = SpriteDrawMode.Tiled;
                postRenderer.tileMode = SpriteTileMode.Continuous;
                postRenderer.sortingOrder = 4;
                postTransform.localScale = Vector3.one;
                postRenderer.size = new Vector2(post.bounds.size.x, 1f);

                var collider = root.GetComponent<BoxCollider2D>();
                collider.size = new Vector2(1f, 0.4f);
                collider.usedByEffector = true;

                var pad = root.GetComponent<CushionPad>();
                if (pad == null) pad = root.AddComponent<CushionPad>();
                var so = new SerializedObject(pad);
                so.FindProperty("minHeight").floatValue = CushionPadRules.DefaultMinHeight;
                so.FindProperty("maxHeight").floatValue = CushionPadRules.DefaultMaxHeight;
                so.FindProperty("stepHeight").floatValue = CushionPadRules.DefaultStepHeight;
                so.FindProperty("pivotAboveGround").floatValue = 0.5f;
                so.FindProperty("surfaceThickness").floatValue = 0.4f;
                so.FindProperty("postOverlap").floatValue = 0.04f;
                so.FindProperty("surfaceCollider").objectReferenceValue = collider;
                so.FindProperty("topRenderer").objectReferenceValue = capRenderer;
                so.FindProperty("postRenderer").objectReferenceValue = postRenderer;
                so.ApplyModifiedPropertiesWithoutUndo();

                // 저장되는 기본 배치는 항상 최소 높이다. 새로 설치하면 이 상태에서 시작한다.
                ApplyMinimumLayout(pad, collider, capRenderer, postRenderer);

                PrefabUtility.SaveAsPrefabAsset(root, SupportPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            return AssetDatabase.LoadAssetAtPath<GameObject>(SupportPrefabPath);
        }

        private static void ApplyMinimumLayout(
            CushionPad pad, BoxCollider2D collider, SpriteRenderer top, SpriteRenderer post)
        {
            // Awake 전 프리팹 내용이라 같은 규칙을 직접 적용한다(CushionPad.ApplyLayout과 동일 식).
            const float pivotAboveGround = 0.5f;
            const float thickness = 0.4f;
            const float overlap = 0.04f;
            float height = pad.MinHeight;
            float surfaceY = height - pivotAboveGround;
            collider.offset = new Vector2(0f, surfaceY - thickness * 0.5f);
            top.transform.localPosition = new Vector3(0f, surfaceY, 0f);
            post.transform.localPosition = new Vector3(0f, -pivotAboveGround, 0f);
            post.size = new Vector2(post.size.x, Mathf.Max(0.01f, height - top.sprite.bounds.size.y + overlap));
        }

        private static void RenameData(GameObject prefab)
        {
            var data = AssetDatabase.LoadAssetAtPath<BuildingData>(SupportDataPath);
            if (data == null) return;
            // 아이디·아이콘·비용(구리 1개)은 그대로, 표시 이름과 설명만 바꾼다.
            data.EditorSet(
                data.Id,
                DisplayName,
                Description,
                prefab,
                data.Icon,
                data.PowerDraw,
                new List<ItemCostEntry>(data.BuildCosts));
            EditorUtility.SetDirty(data);
        }

        private static void RenameMenuLabel()
        {
            var root = PrefabUtility.LoadPrefabContents(BuildingMenuPrefabPath);
            try
            {
                bool changed = false;
                foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
                {
                    if (text.text != "버팀목") continue;
                    text.text = DisplayName;
                    changed = true;
                }

                if (changed) PrefabUtility.SaveAsPrefabAsset(root, BuildingMenuPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}

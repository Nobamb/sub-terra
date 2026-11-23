using UnityEditor;
using UnityEngine;

namespace SubTerra.App.Editor.DataValidation
{
    /// <summary>
    /// 프롬프트 B-209: 광부 선택 화면 그림(Resources/UI/MinerSelect)의 import 설정.
    /// 상수 경로의 PNG만 다시 가져온다. 프리팹·씬은 열거나 저장하지 않는다.
    /// 그림 원본은 work_process/MVP2/images/B-209/source, 정리 스크립트는 work_process/tools/prepare_miner_select_art.py.
    /// </summary>
    public static class PromptB209MinerSelectArtBuilder
    {
        public const string Folder = "Assets/_Project/Resources/UI/MinerSelect/";

        private struct Entry
        {
            public string Name;
            /// <summary>9분할 테두리(왼쪽, 아래, 오른쪽, 위). 0이면 통째로 쓴다.</summary>
            public Vector4 Border;
        }

        private static readonly Entry[] Entries =
        {
            new Entry { Name = "miner-card-frame.png", Border = Vector4.zero },
            new Entry { Name = "miner-pedestal.png", Border = Vector4.zero },
            new Entry { Name = "miner-screen-frame.png", Border = new Vector4(270f, 170f, 270f, 170f) },
        };

        [MenuItem("SubTerra/MVP2/Build Prompt-B 209 Miner Select Art Import")]
        public static void Build()
        {
            var changed = 0;
            foreach (var entry in Entries)
            {
                var path = Folder + entry.Name;
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null)
                {
                    Debug.LogWarning("[SubTerra] B-209 art missing: " + path);
                    continue;
                }

                if (Apply(importer, entry.Border))
                {
                    importer.SaveAndReimport();
                    changed++;
                }
            }

            Debug.Log("[SubTerra] B-209 miner select art import settings applied: " + changed + " changed.");
        }

        private static bool Apply(TextureImporter importer, Vector4 border)
        {
            var dirty = false;
            if (importer.textureType != TextureImporterType.Sprite) { importer.textureType = TextureImporterType.Sprite; dirty = true; }
            if (importer.spriteImportMode != SpriteImportMode.Single) { importer.spriteImportMode = SpriteImportMode.Single; dirty = true; }
            if (!importer.alphaIsTransparency) { importer.alphaIsTransparency = true; dirty = true; }
            if (importer.mipmapEnabled) { importer.mipmapEnabled = false; dirty = true; }
            if (importer.filterMode != FilterMode.Bilinear) { importer.filterMode = FilterMode.Bilinear; dirty = true; }
            if (importer.wrapMode != TextureWrapMode.Clamp) { importer.wrapMode = TextureWrapMode.Clamp; dirty = true; }
            if (importer.textureCompression != TextureImporterCompression.CompressedHQ)
            {
                importer.textureCompression = TextureImporterCompression.CompressedHQ;
                dirty = true;
            }

            if (!Mathf.Approximately(importer.spritePixelsPerUnit, 100f)) { importer.spritePixelsPerUnit = 100f; dirty = true; }
            if (importer.spriteBorder != border) { importer.spriteBorder = border; dirty = true; }
            return dirty;
        }
    }
}

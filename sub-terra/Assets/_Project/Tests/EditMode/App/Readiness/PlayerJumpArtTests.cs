using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using SubTerra.App.UI.Guide;
using SubTerra.Gameplay.Player;
using UnityEditor;
using UnityEngine;

namespace SubTerra.App.Tests.EditMode.Readiness
{
    // B-175: 점프 새 그림 3묶음의 프리팹 배열·가져오기 설정·정렬 실측.
    public sealed class PlayerJumpArtTests
    {
        private const string Art = "Assets/_Project/Art/Characters/Player/Frames/";
        private const string Runtime = "../work_process/images/player-jump-20261029/runtime-frames/";
        private SerializedObject fields;
        private GameObject prefab;

        [SetUp]
        public void SetUp()
        {
            prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Gameplay/Player/Player.prefab");
            fields = new SerializedObject(prefab.GetComponentInChildren<PlayerAnimationController>(true));
        }

        [TestCase("jumpReadyFrames", "JumpReady", "jump_ready", "jump-ready", 8)]
        [TestCase("jumpRiseFrames", "JumpRise", "jump_rise", "jump-rise", 24)]
        [TestCase("jumpFallFrames", "JumpFall", "jump_fall", "jump-fall", 24)]
        public void Prefab_HasEveryApprovedUniqueJumpDrawing(string field, string folder, string prefix, string runtime, int count)
        {
            var frames = fields.FindProperty(field);
            Assert.That(frames.arraySize, Is.EqualTo(count));
            var hashes = new HashSet<string>();
            for (int i = 1; i <= count; i++)
            {
                var sprite = (Sprite)frames.GetArrayElementAtIndex(i - 1).objectReferenceValue;
                string path = Art + folder + "/" + prefix + "_" + i.ToString("D2") + ".png";
                Assert.That(sprite, Is.Not.Null, path);
                Assert.That(AssetDatabase.GetAssetPath(sprite), Is.EqualTo(path));
                byte[] bytes = File.ReadAllBytes(path);
                CollectionAssert.AreEqual(File.ReadAllBytes(Runtime + runtime + "-" + i.ToString("D2") + ".png"), bytes);
                using (var sha = System.Security.Cryptography.SHA256.Create())
                    hashes.Add(System.Convert.ToBase64String(sha.ComputeHash(bytes)));
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                Assert.That(importer.spritePixelsPerUnit, Is.EqualTo(256f));
                Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Bilinear));
                Assert.That(importer.mipmapEnabled, Is.False);
                Assert.That(importer.textureCompression, Is.EqualTo(TextureImporterCompression.Uncompressed));
                Assert.That(sprite.pivot / sprite.rect.size, Is.EqualTo(new Vector2(.5f, .5f)));
                var pixels = Load(bytes, out int width, out int height);
                Assert.That(width, Is.EqualTo(256));
                Assert.That(height, Is.EqualTo(256));
                var bounds = AlphaBounds(pixels, width, height);
                Assert.That(bounds.xMin >= 2 && bounds.yMin >= 2 && bounds.xMax <= 254 && bounds.yMax <= 254,
                    path + " 가장자리 여백 " + bounds);
            }
            Assert.That(hashes.Count, Is.EqualTo(count));
        }

        [Test]
        public void ReadyOne_MatchesIdleFootAndHelmet_OldJumpFilesStayUnreferenced()
        {
            var idle = Load(File.ReadAllBytes(Art + "Idle/player_idle_01.png"), out int w, out int h);
            var ready = Load(File.ReadAllBytes(Art + "JumpReady/jump_ready_01.png"), out _, out _);
            var readyBounds = AlphaBounds(ready, w, h);
            Assert.That(readyBounds.yMax - 1, Is.EqualTo(241).Within(2), "ready 1 발끝(위에서)");
            float idleHelmet = HelmetWidth(idle, w, h);
            Assert.That(HelmetWidth(ready, w, h), Is.EqualTo(idleHelmet).Within(idleHelmet * 0.05f), "헬멧 폭 ±5%");

            Assert.That(fields.FindProperty("jumpFrames").arraySize, Is.Zero, "이전 10장 참조는 끊는다");
            for (int i = 1; i <= 10; i++)
                Assert.That(File.Exists(Art + "Jump/jump_" + i.ToString("D2") + ".png"), Is.True, "이전 그림 파일은 남긴다");

            var settings = fields.FindProperty("jumpAnimation");
            Assert.That(settings.FindPropertyRelative("fallBobCompensation").floatValue, Is.Zero, "출렁임 보정 기본 끔");
            Assert.That(settings.FindPropertyRelative("fallBobOffsets").arraySize, Is.EqualTo(15));
        }

        [Test]
        public void Guide_JumpDemoUsesNewFoldedLegRiseDrawing()
        {
            var skin = AssetDatabase.LoadAssetAtPath<GameGuideSkin>("Assets/_Project/Resources/UI/GameGuideSkin.asset");
            Assert.That(skin.Get("player.jump.4"), Is.EqualTo(fields.FindProperty("jumpRiseFrames")
                .GetArrayElementAtIndex(11).objectReferenceValue));
        }

        private static Color32[] Load(byte[] bytes, out int width, out int height)
        {
            var texture = new Texture2D(2, 2);
            try
            {
                Assert.That(texture.LoadImage(bytes), Is.True);
                width = texture.width;
                height = texture.height;
                return texture.GetPixels32();
            }
            finally { Object.DestroyImmediate(texture); }
        }

        // 위에서 잰 좌표(x, y는 위가 0). yMax/xMax는 마지막 칸 다음.
        private static RectInt AlphaBounds(Color32[] pixels, int width, int height)
        {
            int x0 = width, y0 = height, x1 = -1, y1 = -1;
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                if (pixels[(height - 1 - y) * width + x].a < 32) continue;
                x0 = Mathf.Min(x0, x); x1 = Mathf.Max(x1, x);
                y0 = Mathf.Min(y0, y); y1 = Mathf.Max(y1, y);
            }
            return new RectInt(x0, y0, x1 - x0 + 1, y1 - y0 + 1);
        }

        // 정렬 스크립트(prepare_b172_frames.helmet_bounds)와 같은 기준: 위 22% 주황 헬멧, 오른쪽 전등 제외.
        private static float HelmetWidth(Color32[] pixels, int width, int height)
        {
            var bounds = AlphaBounds(pixels, width, height);
            int bandBottom = bounds.yMin + Mathf.RoundToInt(bounds.height * 0.22f);
            int lightLeft = int.MaxValue;
            int lightRight = bounds.xMin + Mathf.RoundToInt(bounds.width * 0.85f);
            for (int y = bounds.yMin; y < bandBottom; y++)
            for (int x = lightRight; x < bounds.xMax; x++)
            {
                var c = pixels[(height - 1 - y) * width + x];
                if (c.a >= 128 && c.r >= 220 && c.g >= 210 && c.b >= 175 && c.r - c.b < 95)
                    lightLeft = Mathf.Min(lightLeft, x);
            }
            Assert.That(lightLeft, Is.LessThan(int.MaxValue), "헬멧 전등을 찾지 못함");
            int cap = lightLeft - 8, min = int.MaxValue, max = -1;
            for (int y = bounds.yMin; y < bandBottom; y++)
            for (int x = bounds.xMin; x < cap; x++)
            {
                var c = pixels[(height - 1 - y) * width + x];
                if (c.a >= 128 && c.r >= 105 && c.r > c.g * 1.22f && c.g > c.b * 1.18f && c.g >= 38 && c.g <= 205)
                {
                    min = Mathf.Min(min, x);
                    max = Mathf.Max(max, x);
                }
            }
            return max - min + 1;
        }
    }
}

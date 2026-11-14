#if UNITY_EDITOR
using System;
using System.IO;
using SubTerra.Gameplay.Hazards;
using UnityEditor;
using UnityEngine;

namespace SubTerra.App.Editor.DataValidation
{
    /// <summary>
    /// B-196: 가스 구름용 심리스 흑백 노이즈 2장, 부드러운 방사형 스프라이트, 공유 머티리얼을 만든다.
    /// 이미 있으면 다시 만들지 않는다(재실행해도 에셋·GUID가 그대로다). 경로는 상수로만 쓴다.
    /// </summary>
    public static class GasVfxAssetBuilder
    {
        public const string ArtFolder = "Assets/_Project/Art/VFX/Gas";
        public const string NoiseAPath = ArtFolder + "/gas-noise-01.png";
        public const string NoiseBPath = ArtFolder + "/gas-noise-02.png";
        public const string SoftSpritePath = ArtFolder + "/" + GasVisualRules.SoftSpriteName + ".png";
        public const string MaterialPath = "Assets/_Project/Visuals/GasCloud.mat";

        public readonly struct Assets
        {
            public readonly Texture2D NoiseA;
            public readonly Texture2D NoiseB;
            public readonly Sprite SoftSprite;
            public readonly Material Material;

            public Assets(Texture2D noiseA, Texture2D noiseB, Sprite softSprite, Material material)
            {
                NoiseA = noiseA;
                NoiseB = noiseB;
                SoftSprite = softSprite;
                Material = material;
            }
        }

        [MenuItem("SubTerra/UI/Build Gas Realistic VFX Assets (B-196)")]
        public static void BuildFromMenu()
        {
            var assets = EnsureAssets();
            Debug.Log("[SubTerra] B-196 gas vfx assets ready: " + AssetDatabase.GetAssetPath(assets.Material));
        }

        public static Assets EnsureAssets()
        {
            Directory.CreateDirectory(AbsolutePath(ArtFolder));

            WriteIfMissing(NoiseAPath, () => NoisePng(128, 1337));
            WriteIfMissing(NoiseBPath, () => NoisePng(128, 7919));
            WriteIfMissing(SoftSpritePath, () => SoftRadialPng(128));
            AssetDatabase.Refresh();

            ConfigureNoise(NoiseAPath);
            ConfigureNoise(NoiseBPath);
            ConfigureSoftSprite(SoftSpritePath);

            var noiseA = AssetDatabase.LoadAssetAtPath<Texture2D>(NoiseAPath);
            var noiseB = AssetDatabase.LoadAssetAtPath<Texture2D>(NoiseBPath);
            var soft = AssetDatabase.LoadAssetAtPath<Sprite>(SoftSpritePath);
            if (noiseA == null || noiseB == null || soft == null)
            {
                throw new InvalidOperationException("Gas VFX textures failed to import.");
            }

            return new Assets(noiseA, noiseB, soft, EnsureMaterial(noiseA, noiseB));
        }

        private static Material EnsureMaterial(Texture2D noiseA, Texture2D noiseB)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                var shader = Shader.Find(GasVisualRules.CloudShaderName);
                if (shader == null)
                {
                    throw new InvalidOperationException("Missing shader " + GasVisualRules.CloudShaderName);
                }

                material = new Material(shader) { name = "GasCloud" };
                AssetDatabase.CreateAsset(material, MaterialPath);
            }

            var changed = false;
            if (material.GetTexture("_NoiseA") != noiseA)
            {
                material.SetTexture("_NoiseA", noiseA);
                changed = true;
            }

            if (material.GetTexture("_NoiseB") != noiseB)
            {
                material.SetTexture("_NoiseB", noiseB);
                changed = true;
            }

            if (changed)
            {
                EditorUtility.SetDirty(material);
                AssetDatabase.SaveAssetIfDirty(material);
            }

            return material;
        }

        private static void WriteIfMissing(string assetPath, Func<byte[]> create)
        {
            var absolute = AbsolutePath(assetPath);
            if (File.Exists(absolute))
            {
                return;
            }

            File.WriteAllBytes(absolute, create());
        }

        private static string AbsolutePath(string assetPath)
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));
        }

        private static void ConfigureNoise(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                return;
            }

            if (importer.textureType == TextureImporterType.Default
                && !importer.sRGBTexture
                && importer.wrapMode == TextureWrapMode.Repeat
                && importer.textureCompression == TextureImporterCompression.Uncompressed)
            {
                return;
            }

            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = false;
            importer.alphaSource = TextureImporterAlphaSource.None;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Bilinear;
            importer.mipmapEnabled = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        private static void ConfigureSoftSprite(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                return;
            }

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            if (importer.textureType == TextureImporterType.Sprite
                && settings.spriteMeshType == SpriteMeshType.FullRect
                && importer.textureCompression == TextureImporterCompression.Uncompressed)
            {
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 128f;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteAlignment = (int)SpriteAlignment.Center;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }

        /// <summary>이어붙여도 이음새가 없는 흑백 fBm 노이즈(격자를 주기로 감싼다).</summary>
        private static byte[] NoisePng(int size, int seed)
        {
            var values = new float[size * size];
            var periods = new[] { 4, 8, 16 };
            var amplitudes = new[] { 0.55f, 0.30f, 0.15f };
            var min = float.MaxValue;
            var max = float.MinValue;
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var sum = 0f;
                    for (var o = 0; o < periods.Length; o++)
                    {
                        sum += amplitudes[o] * Lattice(x / (float)size, y / (float)size, periods[o], seed + o * 101);
                    }

                    values[y * size + x] = sum;
                    min = Mathf.Min(min, sum);
                    max = Mathf.Max(max, sum);
                }
            }

            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color32[size * size];
            var range = Mathf.Max(0.0001f, max - min);
            for (var i = 0; i < pixels.Length; i++)
            {
                var v = (byte)Mathf.RoundToInt(Mathf.Clamp01((values[i] - min) / range) * 255f);
                pixels[i] = new Color32(v, v, v, 255);
            }

            texture.SetPixels32(pixels);
            var png = texture.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(texture);
            return png;
        }

        private static float Lattice(float u, float v, int period, int seed)
        {
            var fx = u * period;
            var fy = v * period;
            var x0 = Mathf.FloorToInt(fx);
            var y0 = Mathf.FloorToInt(fy);
            var tx = Smooth(fx - x0);
            var ty = Smooth(fy - y0);
            var a = Hash(x0 % period, y0 % period, seed);
            var b = Hash((x0 + 1) % period, y0 % period, seed);
            var c = Hash(x0 % period, (y0 + 1) % period, seed);
            var d = Hash((x0 + 1) % period, (y0 + 1) % period, seed);
            return Mathf.Lerp(Mathf.Lerp(a, b, tx), Mathf.Lerp(c, d, tx), ty);
        }

        private static float Smooth(float t)
        {
            return t * t * (3f - 2f * t);
        }

        private static float Hash(int x, int y, int seed)
        {
            unchecked
            {
                var h = (uint)(x * 374761393 + y * 668265263 + seed * 2147483647);
                h = (h ^ (h >> 13)) * 1274126177u;
                h ^= h >> 16;
                return (h & 0xFFFFFF) / (float)0x1000000;
            }
        }

        private static byte[] SoftRadialPng(int size)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color32[size * size];
            var center = (size - 1) * 0.5f;
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var dx = (x - center) / center;
                    var dy = (y - center) / center;
                    var r = Mathf.Sqrt(dx * dx + dy * dy);
                    var a = (byte)Mathf.RoundToInt((1f - GasVisualMath.SmoothStep(0.2f, 1f, r)) * 255f);
                    pixels[y * size + x] = new Color32(255, 255, 255, a);
                }
            }

            texture.SetPixels32(pixels);
            var png = texture.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(texture);
            return png;
        }
    }
}
#endif

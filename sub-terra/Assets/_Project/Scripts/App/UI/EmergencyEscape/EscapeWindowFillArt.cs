using System;
using System.Collections.Generic;
using UnityEngine;

namespace SubTerra.App.UI.EmergencyEscape
{
    /// <summary>
    /// 긴급 탈출 포탈 창 안쪽을 불투명하게 채우는 그림. 프레임 그림(mine-reset-frame.png)의 바깥 실루엣을 구해,
    /// 그 안쪽(테두리 장식과 가운데 구멍 전부)만 어두운 면으로 칠한다. 모서리가 깎인 프레임 밖으로는 한 픽셀도 나가지 않는다.
    /// 공유 스프라이트 원본은 읽기만 하고 수정하지 않으며, 새 이미지 파일 없이 런타임에 한 번 만든다.
    /// </summary>
    public static class EscapeWindowFillArt
    {
        /// <summary>프레임 안쪽 면 색. 육각 무늬·목록 패널이 이 위에 얹힌다.</summary>
        public static readonly Color FillColor = new Color(0.012f, 0.04f, 0.062f, 1f);
        /// <summary>계산용으로 줄이는 긴 변의 최대 픽셀. 4M 픽셀 원본을 그대로 훑지 않는다.</summary>
        public const int MaxSide = 1024;
        /// <summary>이 값 미만의 알파를 "비어 있음"으로 본다.</summary>
        public const float EmptyAlpha = 0.1f;

        private static Sprite cached;
        private static Sprite cachedSource;

        /// <summary>
        /// 가장자리에서 시작해 비어 있는 칸을 따라 번지는 영역(= 프레임 바깥)을 구하고, 그 반대(= 프레임 안쪽)를 돌려준다.
        /// 순수 계산이라 Edit Mode에서 모양을 검사할 수 있다.
        /// </summary>
        public static bool[] ComputeInside(float[] alpha, int width, int height, float emptyAlpha = EmptyAlpha)
        {
            var total = width * height;
            var outside = new bool[total];
            var stack = new Stack<int>();
            for (var x = 0; x < width; x++)
            {
                Seed(alpha, outside, stack, x, emptyAlpha);
                Seed(alpha, outside, stack, ((height - 1) * width) + x, emptyAlpha);
            }

            for (var y = 0; y < height; y++)
            {
                Seed(alpha, outside, stack, y * width, emptyAlpha);
                Seed(alpha, outside, stack, (y * width) + width - 1, emptyAlpha);
            }

            while (stack.Count > 0)
            {
                var index = stack.Pop();
                var x = index % width;
                var y = index / width;
                if (x > 0) { Seed(alpha, outside, stack, index - 1, emptyAlpha); }
                if (x < width - 1) { Seed(alpha, outside, stack, index + 1, emptyAlpha); }
                if (y > 0) { Seed(alpha, outside, stack, index - width, emptyAlpha); }
                if (y < height - 1) { Seed(alpha, outside, stack, index + width, emptyAlpha); }
            }

            var inside = new bool[total];
            for (var i = 0; i < total; i++)
            {
                inside[i] = !outside[i];
            }

            return inside;
        }

        /// <summary>알파 배열에서 안쪽 면 스프라이트를 만든다. 안쪽만 불투명, 바깥은 완전히 투명.</summary>
        public static Sprite FromAlpha(float[] alpha, int width, int height)
        {
            var inside = ComputeInside(alpha, width, height);
            var pixels = new Color32[width * height];
            Color32 filled = FillColor;
            var empty = new Color32(filled.r, filled.g, filled.b, 0);
            for (var i = 0; i < pixels.Length; i++)
            {
                pixels[i] = inside[i] ? filled : empty;
            }

            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave,
                name = "EscapeWindowFill"
            };
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            var sprite = Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 100f);
            sprite.name = "EscapeWindowFill";
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }

        /// <summary>프레임 스프라이트의 실루엣 안쪽을 채우는 스프라이트. 읽을 수 없으면 null.</summary>
        public static Sprite FromFrame(Sprite frame)
        {
            if (frame == null || frame.texture == null)
            {
                return null;
            }

            if (cached != null && ReferenceEquals(cachedSource, frame))
            {
                return cached;
            }

            try
            {
                var rect = frame.textureRect;
                var scale = Mathf.Min(1f, MaxSide / Mathf.Max(rect.width, rect.height));
                var width = Mathf.Max(8, Mathf.RoundToInt(rect.width * scale));
                var height = Mathf.Max(8, Mathf.RoundToInt(rect.height * scale));
                var alpha = ReadAlpha(frame.texture, rect, width, height);
                if (alpha == null)
                {
                    return null;
                }

                cached = FromAlpha(alpha, width, height);
                cachedSource = frame;
                return cached;
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[SubTerra] 포탈 창 안쪽 채움 생성 실패: " + exception.GetType().Name);
                return null;
            }
        }

        // 원본 텍스처가 읽기 불가여도 되도록 임시 렌더 텍스처로 줄여 그린 뒤 알파만 읽는다.
        private static float[] ReadAlpha(Texture source, Rect rect, int width, int height)
        {
            var target = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            var previous = RenderTexture.active;
            Texture2D readable = null;
            try
            {
                Graphics.Blit(
                    source,
                    target,
                    new Vector2(rect.width / source.width, rect.height / source.height),
                    new Vector2(rect.x / source.width, rect.y / source.height));
                RenderTexture.active = target;
                readable = new Texture2D(width, height, TextureFormat.RGBA32, false);
                readable.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
                var pixels = readable.GetPixels32();
                var alpha = new float[pixels.Length];
                for (var i = 0; i < pixels.Length; i++)
                {
                    alpha[i] = pixels[i].a / 255f;
                }

                return alpha;
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(target);
                if (readable != null)
                {
                    if (Application.isPlaying)
                    {
                        UnityEngine.Object.Destroy(readable);
                    }
                    else
                    {
                        UnityEngine.Object.DestroyImmediate(readable);
                    }
                }
            }
        }

        private static void Seed(float[] alpha, bool[] outside, Stack<int> stack, int index, float emptyAlpha)
        {
            if (outside[index] || alpha[index] >= emptyAlpha)
            {
                return;
            }

            outside[index] = true;
            stack.Push(index);
        }
    }
}

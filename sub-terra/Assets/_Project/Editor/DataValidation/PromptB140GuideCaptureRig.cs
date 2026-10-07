using System;
using System.IO;
using SubTerra.App.UI.Guide;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.Editor.DataValidation
{
    /// <summary>
    /// 프롬프트 B-140 확인용: 에디터(비플레이)에서 가이드 창을 임시 캔버스에 올려 시간표를 직접 진행시키고
    /// 카메라 렌더 결과를 PNG로 저장한다. 씬·프리팹은 열거나 저장하지 않으며 끝나면 임시 오브젝트를 모두 지운다.
    /// </summary>
    public sealed class PromptB140GuideCaptureRig : IDisposable
    {
        private readonly GameObject root;
        private readonly Camera camera;
        private readonly RenderTexture target;
        private readonly Canvas canvas;

        public GameGuidePopupView Popup { get; }
        public int Width { get; }
        public int Height { get; }

        public PromptB140GuideCaptureRig(int width, int height, Texture background = null)
        {
            Width = width;
            Height = height;
            root = new GameObject("GuideCaptureRig");
            camera = new GameObject("Camera").AddComponent<Camera>();
            camera.transform.SetParent(root.transform, false);
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.05f, 0.07f, 0.09f, 1f);
            camera.cullingMask = ~0;
            target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32) { antiAliasing = 1 };
            camera.targetTexture = target;

            var canvasGo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(root.transform, false);
            canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1f;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            if (background != null)
            {
                var bg = new GameObject("Background", typeof(RectTransform), typeof(RawImage));
                bg.transform.SetParent(canvasGo.transform, false);
                var rect = (RectTransform)bg.transform;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = rect.offsetMax = Vector2.zero;
                bg.GetComponent<RawImage>().texture = background;
            }

            Popup = GameGuidePopupView.Create(canvas.transform, null);
            Popup.ManualTick = true;
        }

        public void Settle()
        {
            Canvas.ForceUpdateCanvases();
        }

        /// <summary>시간표를 seconds만큼 진행한다(0.02초 단위).</summary>
        public void Advance(float seconds)
        {
            var remaining = seconds;
            while (remaining > 0.0001f)
            {
                var step = Mathf.Min(0.02f, remaining);
                Popup.Tick(step);
                remaining -= step;
            }
        }

        public Texture2D Render()
        {
            Canvas.ForceUpdateCanvases();
            camera.Render();
            var previous = RenderTexture.active;
            RenderTexture.active = target;
            var texture = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
            texture.ReadPixels(new Rect(0f, 0f, Width, Height), 0, 0);
            texture.Apply();
            RenderTexture.active = previous;
            return texture;
        }

        public void SavePng(string path)
        {
            var texture = Render();
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
        }

        public void Dispose()
        {
            if (target != null)
            {
                camera.targetTexture = null;
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
            }

            if (root != null)
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }
    }
}

using System;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;

namespace SubTerra.App.Save
{
    /// <summary>썸네일은 세이브 JSON과 독립된 부가 파일이며 실패가 저장 결과를 바꾸지 않는다.</summary>
    public sealed class SaveThumbnailService
    {
        public const int Width = 512;
        public const int Height = 288;
        private readonly SavePathPolicy paths;

        public SaveThumbnailService(SavePathPolicy pathPolicy) => paths = pathPolicy;

        public string GetPath(int slotId)
        {
            return paths.IsValidSlot(slotId)
                ? Path.Combine(paths.RootDirectory, "save_slot_" + slotId + "_thumbnail.png")
                : null;
        }

        public bool Capture(int slotId, Camera camera)
        {
            if (camera == null || !paths.IsValidSlot(slotId)) return false;
            var target = RenderTexture.GetTemporary(Width, Height, 24, RenderTextureFormat.ARGB32);
            var previousActive = RenderTexture.active;
            var previousTarget = camera.targetTexture;
            var previousAspect = camera.aspect;
            Texture2D texture = null;
            try
            {
                // Overlay HUD는 카메라 렌더에 포함되지 않는다. 카메라 구조 자체는 변경하지 않는다.
                camera.aspect = (float)Width / Height;
                if (GraphicsSettings.currentRenderPipeline != null)
                {
                    var request = new RenderPipeline.StandardRequest { destination = target };
                    if (!RenderPipeline.SupportsRenderRequest(camera, request)) return false;
                    RenderPipeline.SubmitRenderRequest(camera, request);
                }
                else
                {
                    camera.targetTexture = target;
                    camera.Render();
                }
                RenderTexture.active = target;
                texture = new Texture2D(Width, Height, TextureFormat.RGB24, false);
                texture.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
                texture.Apply();
                return Write(slotId, texture.EncodeToPNG());
            }
            catch (Exception)
            {
                return false;
            }
            finally
            {
                camera.targetTexture = previousTarget;
                camera.aspect = previousAspect;
                RenderTexture.active = previousActive;
                RenderTexture.ReleaseTemporary(target);
                Release(texture);
            }
        }

        public bool Write(int slotId, byte[] png)
        {
            var path = GetPath(slotId);
            if (path == null || png == null || png.Length == 0) return false;
            try
            {
                Directory.CreateDirectory(paths.RootDirectory);
                File.WriteAllBytes(path + ".tmp", png);
                if (File.Exists(path)) File.Replace(path + ".tmp", path, null);
                else File.Move(path + ".tmp", path);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
            finally
            {
                try { if (File.Exists(path + ".tmp")) File.Delete(path + ".tmp"); }
                catch (Exception) { /* 부가 파일 정리가 세이브를 실패시키지 않도록 한다. */ }
            }
        }

        public Texture2D Load(int slotId)
        {
            var path = GetPath(slotId);
            Texture2D texture = null;
            try
            {
                if (path == null || !File.Exists(path)) return null;
                if (new FileInfo(path).Length > 2 * 1024 * 1024) return null;
                var png = File.ReadAllBytes(path);
                // PNG 헤더의 크기를 먼저 검사하여 손상 파일/과대 이미지의 디코딩을 막는다.
                if (png.Length < 24 || png[0] != 137 || png[1] != 80 || png[2] != 78 || png[3] != 71
                    || ReadInt(png, 16) != Width || ReadInt(png, 20) != Height) return null;
                texture = new Texture2D(2, 2, TextureFormat.RGB24, false);
                if (!texture.LoadImage(png, true))
                {
                    Release(texture);
                    return null;
                }
                return texture;
            }
            catch (Exception)
            {
                Release(texture);
                return null;
            }
        }

        public void Delete(int slotId)
        {
            var path = GetPath(slotId);
            try
            {
                if (path == null) return;
                if (File.Exists(path)) File.Delete(path);
                if (File.Exists(path + ".tmp")) File.Delete(path + ".tmp");
            }
            catch (Exception) { /* 썸네일 삭제 실패는 새 게임 저장 결과와 무관하다. */ }
        }

        private static int ReadInt(byte[] bytes, int offset) =>
            (bytes[offset] << 24) | (bytes[offset + 1] << 16) | (bytes[offset + 2] << 8) | bytes[offset + 3];

        private static void Release(Texture2D texture)
        {
            if (texture == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(texture);
            else UnityEngine.Object.DestroyImmediate(texture);
        }
    }
}

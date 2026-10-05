using System;
using UnityEngine;

namespace SubTerra.App.UI.Outpost
{
    /// <summary>
    /// CCTV 전용 미리보기 카메라 하나와 RenderTexture 하나를 재사용한다.
    /// 시설을 바꿔도 새로 만들지 않고 위치만 옮기며, 화면이 닫혀 있는 동안에는 렌더링을 끈다.
    /// 메인 카메라는 읽기만 하고 절대 바꾸지 않는다.
    /// </summary>
    public sealed class CoreCctvCameraRig : IDisposable
    {
        public const string CameraName = "CoreCctvCamera";
        private const int MinTextureSize = 32;
        private const float CameraDepth = -100f;
        private const float CameraZ = -10f;

        private GameObject host;
        private Camera camera;
        private RenderTexture texture;
        private int pixelsPerUnit = 100;

        private Vector2 position;
        private Vector2 moveFrom;
        private Vector2 moveTo;
        private float moveClock;
        private bool moving;
        private bool hasPosition;

        public Camera Camera => camera;
        public RenderTexture Texture => texture;
        public bool IsCreated => camera != null;
        public bool IsRendering => camera != null && camera.enabled;
        public bool IsMoving => moving;
        public bool HasPosition => hasPosition;
        public Vector2 Position => position;
        public Vector2 Target => moving ? moveTo : position;
        /// <summary>메인 카메라와 같은 화면 배율(월드 1칸당 화면 픽셀 수). 그림이 늘어나거나 번지지 않게 정수로 맞춘다.</summary>
        public int PixelsPerUnit => pixelsPerUnit;
        public float MoveSpeed => moving ? CoreCctvTimeline.MoveSpeed(moveClock) : 0f;

        /// <summary>이동 방향의 화면 단위 벡터. 멈춰 있으면 0.</summary>
        public Vector2 MoveDirection
        {
            get
            {
                var delta = moveTo - moveFrom;
                return moving && delta.sqrMagnitude > 0.0001f ? delta.normalized : Vector2.zero;
            }
        }

        public void EnsureCreated()
        {
            if (camera != null)
            {
                return;
            }

            host = new GameObject(CameraName);
            host.tag = "Untagged";
            camera = host.AddComponent<Camera>();
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.02f, 0.035f, 0.05f, 1f);
            // 일반 화면 이름표 레이어는 제외한다. CCTV 이름표는 팝업이 따로 그린다.
            camera.cullingMask = SubTerra.App.UI.FacilityNameTag.FacilityNameTagLayers.CctvCullingMask;
            camera.depth = CameraDepth;
            camera.nearClipPlane = 0.3f;
            camera.farClipPlane = 1000f;
            camera.allowHDR = false;
            camera.allowMSAA = false;
            camera.useOcclusionCulling = false;
            camera.stereoTargetEye = StereoTargetEyeMask.None;
            camera.enabled = false;
            host.transform.position = new Vector3(0f, 0f, CameraZ);
        }

        /// <summary>
        /// 화면에 보이는 픽셀 크기와 같은 RenderTexture를 쓴다. 크기가 그대로면 아무것도 하지 않는다.
        /// 다시 만들어졌으면 true(표시하는 쪽이 텍스처를 다시 연결해야 한다).
        /// </summary>
        public bool Resize(int width, int height)
        {
            EnsureCreated();
            var max = Mathf.Max(MinTextureSize, SystemInfo.maxTextureSize);
            width = Mathf.Clamp(width, MinTextureSize, max);
            height = Mathf.Clamp(height, MinTextureSize, max);
            RefreshScale();

            var recreated = false;
            if (texture == null || texture.width != width || texture.height != height)
            {
                ReleaseTexture();
                texture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32)
                {
                    name = "CoreCctvRenderTexture",
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp,
                    antiAliasing = 1,
                    useMipMap = false,
                    autoGenerateMips = false
                };
                texture.Create();
                camera.targetTexture = texture;
                recreated = true;
            }

            camera.orthographicSize = height / (2f * pixelsPerUnit);
            ApplyPosition();
            return recreated;
        }

        public void SetRendering(bool rendering)
        {
            if (camera == null)
            {
                return;
            }

            camera.enabled = rendering && texture != null;
        }

        public void SnapTo(Vector2 target)
        {
            moving = false;
            moveClock = 0f;
            position = target;
            hasPosition = true;
            ApplyPosition();
        }

        /// <summary>
        /// 현재 위치에서 target으로 빠르게 이동한다. 이동 중이면 지금 위치에서 새 목표로 다시 시작해
        /// 가장 마지막 선택만 따라간다. 이미 그 자리면 아무것도 하지 않는다.
        /// </summary>
        public bool MoveTo(Vector2 target)
        {
            if (!hasPosition)
            {
                SnapTo(target);
                return false;
            }

            if (moving && (moveTo - target).sqrMagnitude < 0.0001f)
            {
                return false;
            }

            if (!moving && (position - target).sqrMagnitude < 0.0001f)
            {
                return false;
            }

            moveFrom = position;
            moveTo = target;
            moveClock = 0f;
            moving = true;
            return true;
        }

        public void Tick(float deltaSeconds)
        {
            if (!moving)
            {
                return;
            }

            moveClock += deltaSeconds;
            if (moveClock >= CoreCctvTimeline.MoveDuration)
            {
                position = moveTo;
                moving = false;
                moveClock = 0f;
            }
            else
            {
                position = Vector2.LerpUnclamped(moveFrom, moveTo, CoreCctvTimeline.MoveEase(moveClock));
            }

            ApplyPosition();
        }

        public void Dispose()
        {
            moving = false;
            hasPosition = false;
            if (camera != null)
            {
                camera.targetTexture = null;
                camera.enabled = false;
            }

            ReleaseTexture();
            if (host != null)
            {
                if (Application.isPlaying)
                {
                    UnityEngine.Object.Destroy(host);
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(host);
                }
            }

            host = null;
            camera = null;
        }

        private void RefreshScale()
        {
            var main = Camera.main;
            if (main != null && main.orthographic && main.orthographicSize > 0.01f && main.pixelHeight > 0)
            {
                pixelsPerUnit = Mathf.Max(8, Mathf.RoundToInt(main.pixelHeight / (2f * main.orthographicSize)));
            }
        }

        private void ApplyPosition()
        {
            if (host == null)
            {
                return;
            }

            // 텍셀 격자에 맞춰 위치를 고정하면 이동이 멈춘 뒤 그림이 일렁이지 않는다.
            var snap = 1f / pixelsPerUnit;
            host.transform.position = new Vector3(
                Mathf.Round(position.x / snap) * snap,
                Mathf.Round(position.y / snap) * snap,
                CameraZ);
        }

        private void ReleaseTexture()
        {
            if (camera != null && camera.targetTexture == texture)
            {
                camera.targetTexture = null;
            }

            if (texture != null)
            {
                texture.Release();
                if (Application.isPlaying)
                {
                    UnityEngine.Object.Destroy(texture);
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(texture);
                }
            }

            texture = null;
        }
    }
}

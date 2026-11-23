using System;
using System.Collections;
using UnityEngine;

namespace SubTerra.App.UI.MainMenu
{
    /// <summary>
    /// 광부 확정 뒤 닫힘 글리치. 닫히기 직전 화면을 한 장 캡처해 전체 화면 RawImage에 깔고,
    /// 글리치 셰이더(찢김·RGB 분리·블록 밀림·노이즈 → TV 꺼짐)로 흔든다.
    /// 메인 메뉴 씬에 속해 씬이 바뀌면 함께 사라진다. 캡처·셰이더가 없으면 HasCapture=false로 남고 패널이 막대 글리치를 쓴다.
    /// </summary>
    public sealed class MinerSelectScreenGlitch : MonoBehaviour
    {
        /// <summary>메뉴·선택 화면보다 위, 인트로 오버레이(32_767)보다 아래.</summary>
        public const int SortingOrder = 32_700;

        private static readonly int IntensityId = Shader.PropertyToID("_Intensity");
        private static readonly int SeedId = Shader.PropertyToID("_Seed");
        private static readonly int CollapseId = Shader.PropertyToID("_Collapse");
        private static readonly int LineWidthId = Shader.PropertyToID("_LineWidth");
        private static readonly int FlashId = Shader.PropertyToID("_Flash");
        private static readonly int BlackId = Shader.PropertyToID("_Black");

        private UnityEngine.UI.RawImage image;
        private Material material;
        private Texture2D capture;
        private bool failed;

        public bool HasCapture => capture != null && image != null && image.enabled;
        public bool Failed => failed;

        public static MinerSelectScreenGlitch Create()
        {
            var shader = MinerSelectArt.GlitchShader();
            if (shader == null || !Application.isPlaying) return null;
            var go = new GameObject("MinerSelectGlitchOverlay", typeof(RectTransform));
            var glitch = go.AddComponent<MinerSelectScreenGlitch>();
            glitch.Build(shader);
            return glitch;
        }

        private void Build(Shader shader)
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = SortingOrder;
            var scaler = gameObject.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            // 전환 중 클릭이 아래로 새지 않게 막는다.
            gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            var rect = MinerSelectPanel.NewRect(transform, "Screen", Vector2.zero, Vector2.zero);
            rect.StretchFull();
            image = rect.gameObject.AddComponent<UnityEngine.UI.RawImage>();
            material = new Material(shader) { name = "MinerSelectGlitch", hideFlags = HideFlags.DontSave };
            image.material = material;
            image.color = Color.white;
            image.raycastTarget = true;
            image.enabled = false;
        }

        /// <summary>이번 프레임이 다 그려진 뒤 화면을 캡처한다.</summary>
        public void Begin()
        {
            StartCoroutine(CaptureAtEndOfFrame());
        }

        private IEnumerator CaptureAtEndOfFrame()
        {
            yield return new WaitForEndOfFrame();
            try
            {
                capture = ScreenCapture.CaptureScreenshotAsTexture();
                if (capture != null)
                {
                    capture.wrapMode = TextureWrapMode.Clamp;
                    image.texture = capture;
                    image.enabled = true;
                }
                else
                {
                    failed = true;
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[SubTerra] Miner select glitch capture failed; using bar glitch. " + exception.Message);
                failed = true;
            }
        }

        public void Apply(MinerSelectTransitionFrame frame)
        {
            if (material == null) return;
            material.SetFloat(IntensityId, frame.Intensity);
            material.SetFloat(SeedId, frame.Seed * 0.731f);
            material.SetFloat(CollapseId, frame.Collapse);
            material.SetFloat(LineWidthId, frame.LineWidth);
            material.SetFloat(FlashId, frame.Flash);
            material.SetFloat(BlackId, frame.IsComplete ? 1f : 0f);
        }

        private void OnDestroy()
        {
            if (capture != null) Destroy(capture);
            if (material != null) Destroy(material);
        }
    }
}

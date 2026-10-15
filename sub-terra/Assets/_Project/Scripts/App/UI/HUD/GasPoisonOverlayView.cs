using SubTerra.App.Core;
using SubTerra.App.State;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.UI.HUD
{
    /// <summary>
    /// 가스 중독 화면 연출 View. 가장자리 비네팅 Image와 전체 필터 Image에 GasPoisonOverlayTimeline 값만 적용한다.
    /// 단계는 HUD와 같은 경로(내 GameState.GasExposureChanged)로만 받는다. 내 GameState만 보므로 다른 플레이어 상태와 무관하다.
    /// Screen Space Overlay HUD 캔버스의 가장 뒤 형제로 두어 HUD·팝업을 가리지 않고, CCTV 카메라에도 찍히지 않는다.
    /// 사망·런 종료(Returning/Completed)와 비활성화(씬 전환)에서는 페이드 없이 즉시 끈다.
    /// </summary>
    public sealed class GasPoisonOverlayView : MonoBehaviour
    {
        private const float MaxFrameSeconds = 0.1f;

        [SerializeField] private Image vignetteImage;
        [SerializeField] private Image filterImage;
        [SerializeField] private Image streakImage;
        [SerializeField] private Color vignetteColor = new Color(0.1f, 0.55f, 0.16f, 1f);
        [SerializeField] private Color streakColor = new Color(0.3f, 0.95f, 0.4f, 1f);
        [SerializeField] private Color filterColor = new Color(0.22f, 0.82f, 0.34f, 1f);
        [SerializeField] private GasPoisonOverlaySettings settings = new GasPoisonOverlaySettings();

        private GasPoisonOverlayTimeline timeline;
        private GameState boundState;
        private Texture2D vignetteTexture;
        private Sprite vignetteSprite;
        private Texture2D streakTexture;
        private Sprite streakSprite;
        private bool shown;

        public GasPoisonOverlayTimeline Timeline => timeline;
        public Image VignetteImage => vignetteImage;
        public Image FilterImage => filterImage;
        public Image StreakImage => streakImage;

        private void Awake()
        {
            EnsureTimeline();
            PrepareImages();
            Hide();
        }

        private void OnEnable()
        {
            EnsureTimeline();
            timeline.Reset();
            Hide();
            TryBind();
        }

        private void OnDisable()
        {
            Unbind();
            if (timeline != null)
            {
                timeline.Reset();
            }

            Hide();
        }

        private void OnDestroy()
        {
            Release(ref vignetteSprite, ref vignetteTexture);
            Release(ref streakSprite, ref streakTexture);
        }

        private static void Release(ref Sprite sprite, ref Texture2D texture)
        {
            if (sprite != null)
            {
                Destroy(sprite);
                sprite = null;
            }

            if (texture != null)
            {
                Destroy(texture);
                texture = null;
            }
        }

        private void Update()
        {
            if (boundState == null)
            {
                TryBind();
                if (boundState == null)
                {
                    return;
                }
            }

            var phase = boundState.Run.LifecyclePhase;
            if (phase == RunLifecyclePhase.Returning || phase == RunLifecyclePhase.Completed)
            {
                if (shown || timeline.Target != GasRiskLevel.Safe)
                {
                    timeline.Reset();
                    Hide();
                }

                return;
            }

            if (!shown && timeline.Target == GasRiskLevel.Safe)
            {
                return;
            }

            var dt = Mathf.Min(Time.unscaledDeltaTime, MaxFrameSeconds);
            var frame = timeline.Advance(dt);
            if (frame.IsVisible)
            {
                Apply(frame);
            }
            else
            {
                Hide();
            }
        }

        /// <summary>테스트·수동 주입용. 활성 중에 호출하면 즉시 재바인드한다.</summary>
        public void BindTo(GameState state)
        {
            Unbind();
            EnsureTimeline();
            boundState = state;
            if (boundState == null)
            {
                return;
            }

            boundState.GasExposureChanged += OnGasExposureChanged;
            timeline.SetLevel(boundState.Run.GasExposure);
        }

        /// <summary>즉시 끈다. 다음 판에 남지 않게 외부에서 부를 수 있다.</summary>
        public void ResetImmediate()
        {
            EnsureTimeline();
            timeline.Reset();
            Hide();
        }

        public bool HasRequiredReferences()
        {
            return vignetteImage != null && filterImage != null;
        }

        private void TryBind()
        {
            try
            {
                var root = GameBootstrapper.Instance;
                if (root != null && root.State != null)
                {
                    BindTo(root.State);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[GasPoisonOverlayView] 상태 바인딩 실패: " + ex.Message);
            }
        }

        private void Unbind()
        {
            if (boundState != null)
            {
                boundState.GasExposureChanged -= OnGasExposureChanged;
                boundState = null;
            }
        }

        private void OnGasExposureChanged(GasRiskLevel level)
        {
            timeline.SetLevel(level);
        }

        private void EnsureTimeline()
        {
            if (timeline == null)
            {
                timeline = new GasPoisonOverlayTimeline(settings);
            }
        }

        private void PrepareImages()
        {
            if (vignetteImage != null)
            {
                vignetteImage.raycastTarget = false;
                if (vignetteImage.sprite == null)
                {
                    vignetteTexture = GasPoisonVignetteTexture.Create();
                    vignetteSprite = ToSprite(vignetteTexture);
                    vignetteImage.sprite = vignetteSprite;
                }
            }

            if (streakImage != null)
            {
                streakImage.raycastTarget = false;
                if (streakImage.sprite == null)
                {
                    streakTexture = GasPoisonVignetteTexture.CreateStreaks();
                    streakSprite = ToSprite(streakTexture);
                    streakImage.sprite = streakSprite;
                }
            }

            if (filterImage != null)
            {
                filterImage.raycastTarget = false;
            }
        }

        private static Sprite ToSprite(Texture2D texture)
        {
            var sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }

        private void Apply(GasPoisonOverlayFrame frame)
        {
            shown = true;
            if (vignetteImage != null)
            {
                var color = vignetteColor;
                color.a = frame.VignetteAlpha;
                vignetteImage.color = color;
                vignetteImage.rectTransform.localScale = new Vector3(frame.VignetteScale, frame.VignetteScale, 1f);
                vignetteImage.enabled = true;
            }

            if (filterImage != null)
            {
                var color = filterColor;
                color.a = frame.FilterAlpha;
                filterImage.color = color;
                filterImage.enabled = true;
            }

            if (streakImage != null)
            {
                var color = streakColor;
                color.a = frame.StreakAlpha;
                streakImage.color = color;
                streakImage.enabled = true;
            }
        }

        private void Hide()
        {
            shown = false;
            if (vignetteImage != null)
            {
                vignetteImage.enabled = false;
            }

            if (filterImage != null)
            {
                filterImage.enabled = false;
            }

            if (streakImage != null)
            {
                streakImage.enabled = false;
            }
        }
    }
}

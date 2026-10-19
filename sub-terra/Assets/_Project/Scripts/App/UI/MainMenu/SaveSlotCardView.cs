using System;
using SubTerra.App.Save;
using SubTerra.App.UI.Outpost;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SubTerra.App.UI.MainMenu
{
    public sealed class SaveSlotCardView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private const float NoiseFrameSeconds = 1f / 24f;

        [SerializeField] private UnityEngine.UI.RawImage thumbnail;
        [SerializeField] private GameObject placeholder;
        [SerializeField] private CanvasGroup selection;
        [SerializeField] private UnityEngine.UI.RawImage innerGlow;

        [Header("B-150")]
        [SerializeField] private CanvasGroup introGroup;
        [SerializeField] private UnityEngine.UI.RawImage signalNoise;
        [SerializeField] private UnityEngine.UI.Button deleteButton;
        [SerializeField] private CanvasGroup deleteGroup;
        [SerializeField] private UnityEngine.UI.Image exitFlash;
        [Tooltip("모서리 4조각 + 위·아래 바. 제자리 기준 바깥 방향으로 날아온다.")]
        [SerializeField] private RectTransform[] framePieces = new RectTransform[0];
        [SerializeField] private UnityEngine.UI.Image[] frameGlows = new UnityEngine.UI.Image[0];
        [SerializeField] private Color seamFlashColor = new Color(0.45f, 0.95f, 1f, 1f);
        [SerializeField, Range(0f, 1f)] private float deleteIdleAlpha = 0.72f;
        [SerializeField, Range(0f, 1f)] private float emptyNoiseAlpha = 0.55f;
        [SerializeField] private float thumbnailRevealSeconds = 0.22f;

        private Texture2D loadedTexture;
        private bool loaded;
        private float targetAlpha;
        private SaveSlotCardKind kind = SaveSlotCardKind.Empty;
        private bool selected;
        private bool hasImage;
        private float revealClock = -1f;
        private float noiseClock;
        private Vector2[] frameHomes;
        private Vector2[] frameDirections;
        private UnityEngine.UI.Graphic[] frameGraphics;
        private Vector2 introHome;
        private bool hasIntroHome;

        public event Action<SaveSlotCardView> Hovered;
        public event Action<SaveSlotCardView> Unhovered;
        public event Action<SaveSlotCardView> DeleteClicked;

        public SaveSlotCardKind Kind => kind;
        public bool IsDeleteAvailable => deleteButton != null && deleteButton.gameObject.activeSelf;
        public float DeleteAlpha => deleteGroup != null ? deleteGroup.alpha : 0f;
        public float FrameAlpha { get; private set; }
        public int FramePieceCount => framePieces != null ? framePieces.Length : 0;

        private void Awake()
        {
            CacheFrame();
            CacheIntroHome();
            if (signalNoise != null) signalNoise.texture = CoreCctvArt.Noise();
            ApplyFrame(new SlotFrameSample(SlotFramePhase.Off, 0f, 0f, 0f));
        }

        private void OnEnable()
        {
            if (deleteButton != null) deleteButton.onClick.AddListener(OnDeleteClicked);
        }

        private void OnDisable()
        {
            if (deleteButton != null) deleteButton.onClick.RemoveListener(OnDeleteClicked);
        }

        public void OnPointerEnter(PointerEventData eventData) => Hovered?.Invoke(this);
        public void OnPointerExit(PointerEventData eventData) => Unhovered?.Invoke(this);

        public void ShowThumbnail(int slotId, bool canContinue)
        {
            if (!loaded && canContinue)
            {
                var runtime = SaveRuntimeController.Instance;
                if (runtime != null && runtime.Thumbnails != null)
                {
                    loadedTexture = runtime.Thumbnails.Load(slotId);
                    loaded = true;
                }
            }

            // 삭제 뒤에는 이전 썸네일을 다시 쓰지 않는다.
            if (!canContinue && loaded)
            {
                if (loadedTexture != null) Destroy(loadedTexture);
                loadedTexture = null;
                loaded = false;
            }

            hasImage = canContinue && loadedTexture != null;
            if (thumbnail != null)
            {
                thumbnail.texture = hasImage ? loadedTexture : null;
                thumbnail.color = hasImage ? Color.white : new Color(0.04f, 0.09f, 0.12f);
            }
            if (placeholder != null) placeholder.SetActive(!hasImage);
        }

        /// <summary>빈 슬롯은 "신호 없음" 노이즈, 손상 슬롯은 기존 주황 표시. 빈 슬롯에는 삭제 버튼이 없다.</summary>
        public void SetKind(SaveSlotCardKind value)
        {
            kind = value;
            if (deleteButton != null) deleteButton.gameObject.SetActive(value != SaveSlotCardKind.Empty);
            var text = placeholder != null ? placeholder.GetComponent<TMP_Text>() : null;
            if (text != null)
            {
                text.text = value == SaveSlotCardKind.Empty ? "신호 없음"
                    : value == SaveSlotCardKind.Corrupted ? "<color=#E6A23C>신호 손상</color>"
                    : "NO VISUAL\nRECORD";
            }
            RefreshDelete();
        }

        public void SetSelected(bool value)
        {
            selected = value;
            targetAlpha = value ? 1 : 0;
            RefreshDelete();
        }

        /// <summary>부팅 연출: 카드 알파와 아래에서 올라오는 거리.</summary>
        public void SetIntro(float alpha, float offsetY)
        {
            CacheIntroHome();
            if (introGroup != null) introGroup.alpha = alpha;
            var rect = transform as RectTransform;
            if (rect != null && hasIntroHome) rect.anchoredPosition = introHome + new Vector2(0f, -offsetY);
        }

        /// <summary>카드가 나타날 때 썸네일이 노이즈에서 화면으로 짧게 바뀐다.</summary>
        public void BeginThumbnailReveal()
        {
            if (revealClock < 0f) revealClock = 0f;
        }

        public void FinishThumbnailReveal() => revealClock = thumbnailRevealSeconds;

        public void SetExitFlash(float amount)
        {
            if (exitFlash == null) return;
            var c = exitFlash.color;
            c.a = Mathf.Clamp01(amount) * 0.65f;
            exitFlash.color = c;
        }

        public void ApplyFrame(SlotFrameSample sample)
        {
            CacheFrame();
            FrameAlpha = sample.Alpha;
            if (framePieces == null) return;
            for (var i = 0; i < framePieces.Length; i++)
            {
                var piece = framePieces[i];
                if (piece == null) continue;
                var visible = sample.Alpha > 0.001f || sample.Flash > 0.001f;
                if (piece.gameObject.activeSelf != visible) piece.gameObject.SetActive(visible);
                if (!visible) continue;
                piece.anchoredPosition = frameHomes[i] + frameDirections[i] * sample.Distance;
                var graphic = frameGraphics[i];
                if (graphic != null)
                {
                    var c = graphic.color;
                    c.a = sample.Alpha;
                    graphic.color = c;
                }

                var glow = frameGlows != null && i < frameGlows.Length ? frameGlows[i] : null;
                if (glow != null)
                {
                    var g = seamFlashColor;
                    g.a = sample.Flash * 0.9f;
                    glow.color = g;
                }
            }
        }

        private void Update()
        {
            var dt = Time.unscaledDeltaTime;
            if (selection != null)
            {
                selection.alpha = Mathf.MoveTowards(selection.alpha, targetAlpha, dt / 0.2f);
            }
            if (innerGlow != null && targetAlpha > 0.5f)
            {
                var pulse = 0.26f + Mathf.Sin(Time.unscaledTime * 2.2f) * 0.06f;
                var c = innerGlow.color;
                c.a = pulse;
                innerGlow.color = c;
            }

            UpdateNoise(dt);
        }

        private void UpdateNoise(float dt)
        {
            if (signalNoise == null) return;
            if (revealClock >= 0f && revealClock < thumbnailRevealSeconds) revealClock += dt;
            float alpha;
            if (hasImage)
            {
                // 이미지가 있으면 등장 직후 노이즈가 걷히며 화면이 드러난다.
                alpha = revealClock < 0f ? 1f : 1f - Mathf.Clamp01(revealClock / Mathf.Max(0.01f, thumbnailRevealSeconds));
            }
            else
            {
                alpha = kind == SaveSlotCardKind.Empty ? emptyNoiseAlpha : emptyNoiseAlpha * 0.6f;
            }

            var color = signalNoise.color;
            color.a = alpha;
            signalNoise.color = color;
            if (alpha <= 0.001f) return;
            noiseClock += dt;
            if (noiseClock < NoiseFrameSeconds) return;
            noiseClock = 0f;
            signalNoise.uvRect = new Rect(UnityEngine.Random.value, UnityEngine.Random.value, 1.25f, 0.7f);
        }

        private void RefreshDelete()
        {
            if (deleteGroup == null) return;
            deleteGroup.alpha = selected ? 1f : deleteIdleAlpha;
        }

        private void CacheIntroHome()
        {
            if (hasIntroHome) return;
            var rect = transform as RectTransform;
            if (rect == null) return;
            introHome = rect.anchoredPosition;
            hasIntroHome = true;
        }

        private void CacheFrame()
        {
            if (frameHomes != null || framePieces == null) return;
            frameHomes = new Vector2[framePieces.Length];
            frameDirections = new Vector2[framePieces.Length];
            frameGraphics = new UnityEngine.UI.Graphic[framePieces.Length];
            for (var i = 0; i < framePieces.Length; i++)
            {
                if (framePieces[i] == null) continue;
                var home = framePieces[i].anchoredPosition;
                frameHomes[i] = home;
                // 모서리는 대각선, 위·아래 바는 세로로 바깥을 향한다.
                var dir = new Vector2(Mathf.Abs(home.x) > 1f ? Mathf.Sign(home.x) : 0f, Mathf.Abs(home.y) > 1f ? Mathf.Sign(home.y) : 0f);
                frameDirections[i] = dir.sqrMagnitude > 0f ? dir.normalized : Vector2.up;
                frameGraphics[i] = framePieces[i].GetComponent<UnityEngine.UI.Graphic>();
            }
        }

        private void OnDeleteClicked() => DeleteClicked?.Invoke(this);

        private void OnDestroy()
        {
            if (loadedTexture != null) Destroy(loadedTexture);
        }
    }
}

using SubTerra.App.UI.Outpost;
using UnityEngine;

namespace SubTerra.App.UI.MainMenu
{
    /// <summary>
    /// B-150 배경 분위기: 떠다니는 먼지, 옅은 스캔라인, 몇 초에 한 번 짧은 지지직, 메뉴 뒤 어둠(비네트).
    /// 메뉴 글자보다 뒤에 그려지고 입력을 받지 않는다. 텍스처는 CCTV 팝업과 같은 절차 생성 그림을 쓴다.
    /// </summary>
    public sealed class MainMenuAtmosphere : MonoBehaviour
    {
        private struct Mote
        {
            public RectTransform Rect;
            public UnityEngine.UI.Image Image;
            public Vector2 Pos;
            public Vector2 Vel;
            public float Phase;
            public float Alpha;
        }

        [SerializeField] private UnityEngine.UI.Image backdrop;
        [SerializeField] private UnityEngine.UI.Image vignette;
        [SerializeField] private UnityEngine.UI.RawImage scanlines;
        [SerializeField] private UnityEngine.UI.RawImage staticBurst;
        [SerializeField] private RectTransform dustRoot;

        [Header("Tuning")]
        [SerializeField, Range(0f, 1f)] private float backdropAlpha = 0.42f;
        [SerializeField, Range(0f, 1f)] private float vignetteAlpha = 0.85f;
        [SerializeField, Range(0f, 0.3f)] private float scanlineAlpha = 0.05f;
        [Tooltip("스캔라인 한 줄 간격(px)")]
        [SerializeField] private float scanlineSpacing = 4f;
        [SerializeField] private float scanlineScrollSpeed = 0.25f;
        [SerializeField] private Vector2 burstInterval = new Vector2(3.5f, 7f);
        [SerializeField] private float burstDuration = 0.12f;
        [SerializeField, Range(0f, 0.3f)] private float burstAlpha = 0.07f;
        [SerializeField] private int dustCount = 28;
        [SerializeField, Range(0f, 1f)] private float dustAlpha = 0.28f;
        [SerializeField] private Vector2 dustSize = new Vector2(2f, 4.5f);
        [SerializeField] private float dustSpeed = 9f;
        [SerializeField] private Color dustColor = new Color(0.62f, 0.9f, 0.95f, 1f);

        private Mote[] motes;
        private float burstClock;
        private float nextBurst;
        private float noiseStep;

        private void Awake()
        {
            if (backdrop != null)
            {
                backdrop.sprite = CoreCctvArt.Soft();
                backdrop.color = new Color(0f, 0f, 0f, backdropAlpha);
            }

            if (vignette != null)
            {
                vignette.sprite = CoreCctvArt.Vignette();
                vignette.color = new Color(1f, 1f, 1f, vignetteAlpha);
            }

            if (scanlines != null)
            {
                scanlines.texture = CoreCctvArt.Scanline();
                scanlines.color = new Color(0.55f, 0.9f, 1f, scanlineAlpha);
            }

            if (staticBurst != null)
            {
                staticBurst.texture = CoreCctvArt.Noise();
                staticBurst.color = new Color(1f, 1f, 1f, 0f);
            }

            nextBurst = Random.Range(burstInterval.x, burstInterval.y);
        }

        private void Update()
        {
            var dt = Time.unscaledDeltaTime;
            UpdateScanlines();
            UpdateBurst(dt);
            UpdateDust(dt);
        }

        private void UpdateScanlines()
        {
            if (scanlines == null) return;
            var height = scanlines.rectTransform.rect.height;
            var lines = Mathf.Max(1f, height / Mathf.Max(1f, scanlineSpacing));
            scanlines.uvRect = new Rect(0f, Time.unscaledTime * scanlineScrollSpeed, 1f, lines);
        }

        private void UpdateBurst(float dt)
        {
            if (staticBurst == null) return;
            burstClock += dt;
            var color = staticBurst.color;
            if (burstClock < nextBurst)
            {
                if (color.a != 0f)
                {
                    color.a = 0f;
                    staticBurst.color = color;
                }
                return;
            }

            var local = burstClock - nextBurst;
            if (local >= burstDuration)
            {
                burstClock = 0f;
                nextBurst = Random.Range(burstInterval.x, burstInterval.y);
                color.a = 0f;
                staticBurst.color = color;
                return;
            }

            // 지지직: 짧게 두 번 끊어지는 노이즈.
            var u = local / burstDuration;
            color.a = burstAlpha * (u < 0.4f || u > 0.6f ? 1f : 0.25f);
            staticBurst.color = color;
            noiseStep += dt;
            if (noiseStep >= 1f / 30f)
            {
                noiseStep = 0f;
                staticBurst.uvRect = new Rect(Random.value, Random.value, 3f, 1.6f);
            }
        }

        private void UpdateDust(float dt)
        {
            if (dustRoot == null || dustCount <= 0) return;
            EnsureDust();
            var half = dustRoot.rect.size * 0.5f;
            for (var i = 0; i < motes.Length; i++)
            {
                var mote = motes[i];
                mote.Phase += dt;
                mote.Pos += (mote.Vel + new Vector2(Mathf.Sin(mote.Phase * 0.7f + i) * 3f, 0f)) * dt;
                if (mote.Pos.y > half.y + 8f) mote.Pos = new Vector2(Random.Range(-half.x, half.x), -half.y - 8f);
                if (mote.Pos.x > half.x + 8f) mote.Pos.x = -half.x - 8f;
                else if (mote.Pos.x < -half.x - 8f) mote.Pos.x = half.x + 8f;
                mote.Rect.anchoredPosition = mote.Pos;
                var c = dustColor;
                c.a = mote.Alpha * (0.6f + 0.4f * Mathf.Sin(mote.Phase * 1.3f + i * 0.9f));
                mote.Image.color = c;
                motes[i] = mote;
            }
        }

        private void EnsureDust()
        {
            if (motes != null) return;
            var half = dustRoot.rect.size * 0.5f;
            motes = new Mote[dustCount];
            var sprite = CoreCctvArt.Dot();
            for (var i = 0; i < motes.Length; i++)
            {
                var go = new GameObject("Mote_" + i, typeof(RectTransform), typeof(UnityEngine.UI.Image));
                go.transform.SetParent(dustRoot, false);
                var rect = (RectTransform)go.transform;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
                var size = Random.Range(dustSize.x, dustSize.y);
                rect.sizeDelta = new Vector2(size, size);
                var image = go.GetComponent<UnityEngine.UI.Image>();
                image.sprite = sprite;
                image.raycastTarget = false;
                motes[i] = new Mote
                {
                    Rect = rect,
                    Image = image,
                    Pos = new Vector2(Random.Range(-half.x, half.x), Random.Range(-half.y, half.y)),
                    Vel = new Vector2(Random.Range(-0.4f, 0.4f) * dustSpeed, Random.Range(0.4f, 1f) * dustSpeed),
                    Phase = Random.value * 6f,
                    Alpha = Random.Range(0.35f, 1f) * dustAlpha
                };
            }
        }
    }
}

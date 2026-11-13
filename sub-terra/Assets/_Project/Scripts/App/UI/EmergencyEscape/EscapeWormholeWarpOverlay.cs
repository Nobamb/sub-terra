using System.Collections.Generic;
using SubTerra.App.UI.Outpost;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.UI.EmergencyEscape
{
    /// <summary>캐릭터 스프라이트 하나의 화면 배치. 웜홀 중심(그룹 원점) 기준 픽셀.</summary>
    public sealed class EscapeWarpProxySprite
    {
        public Sprite Sprite;
        public Vector2 Offset;
        public Vector2 Size;
        public Vector2 Pivot = new Vector2(0.5f, 0.5f);
        public float RotationZ;
        public bool FlipX;
        public Color Tint = Color.white;
    }

    /// <summary>
    /// 포탈 워프의 전체 화면 연출 레이어. 런타임에 만들고 끝나면 지운다. 새 이미지 파일은 없다.
    /// 아래에서 위로: 검은 전체화면 → 빨려 드는 줄기 → 웜홀(창과 같은 EscapeWormholeArt 소용돌이·코어·입자)
    /// → 캐릭터 복제(검은 화면 위에서도 보이며 웜홀 속으로 줄어든다) → 한 점으로 모일 때의 번쩍임.
    /// 검은 이미지는 클릭을 막아 연출 중 월드 입력이 새지 않게 한다. 글리치 레이어는 없다.
    /// </summary>
    public sealed class EscapeWormholeWarpOverlay
    {
        /// <summary>글리치 오버레이(EscapeWarpOverlay)와 같은 층. 한 번에 하나만 만든다.</summary>
        public const int SortingOrder = EscapeWarpOverlay.SortingOrder;
        /// <summary>입자 그래픽의 기준 고리 반지름(EscapeWormholeParticleGraphic.ringRadius 기본값).</summary>
        private const float ParticleReferenceRadius = 76f;
        private const float OuterSwirlRatio = 1.6f;
        private const float InnerSwirlRatio = 1.25f;
        private const float FlashRatio = 6.5f;

        private static readonly Color FlashColor = new Color(0.78f, 1f, 1f, 1f);

        private readonly GameObject root;
        private readonly Image blackImage;
        private readonly RectTransform suctionRect;
        private readonly EscapeWormholeSuctionGraphic suction;
        private readonly RectTransform holder;
        private readonly CanvasGroup holderGroup;
        private readonly RectTransform outerSwirl;
        private readonly RectTransform innerSwirl;
        private readonly RectTransform core;
        private readonly RectTransform particleRect;
        private readonly EscapeWormholeParticleGraphic particles;
        private readonly RectTransform playerGroup;
        private readonly RectTransform flashRect;
        private readonly Image flashImage;
        private readonly List<Image> proxyImages = new List<Image>();

        public GameObject Root => root;
        public float BlackoutAlpha => blackImage != null ? blackImage.color.a : 0f;
        public bool WormholeVisible => holder != null && holder.gameObject.activeSelf;
        public float CurrentScale => holder != null ? holder.localScale.x : 0f;
        public bool PlayerVisible => playerGroup != null && playerGroup.gameObject.activeSelf;
        public float PlayerScale => playerGroup != null ? playerGroup.localScale.x : 0f;
        public int ProxyCount => proxyImages.Count;
        public float FlashAlpha => flashImage != null ? flashImage.color.a : 0f;
        public bool SuctionActive => suctionRect != null && suctionRect.gameObject.activeSelf;

        public EscapeWormholeWarpOverlay()
        {
            root = new GameObject("EscapeWormholeWarpOverlay", typeof(RectTransform));
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = SortingOrder;
            root.AddComponent<GraphicRaycaster>();

            var black = NewRect("Black", root.transform);
            Stretch(black);
            blackImage = black.gameObject.AddComponent<Image>();
            blackImage.color = new Color(0f, 0f, 0f, 0f);
            blackImage.raycastTarget = true;

            suctionRect = NewCentered("Suction", root.transform);
            suction = suctionRect.gameObject.AddComponent<EscapeWormholeSuctionGraphic>();
            suctionRect.gameObject.SetActive(false);

            holder = NewCentered("Wormhole", root.transform);
            holderGroup = holder.gameObject.AddComponent<CanvasGroup>();
            holderGroup.blocksRaycasts = false;
            holderGroup.interactable = false;
            outerSwirl = NewImage("SwirlOuter", holder, EscapeWormholeArt.Swirl(0));
            innerSwirl = NewImage("SwirlInner", holder, EscapeWormholeArt.Swirl(1));
            core = NewImage("Core", holder, EscapeWormholeArt.Core());
            particleRect = NewRect("Particles", holder);
            particleRect.anchorMin = new Vector2(0.5f, 0.5f);
            particleRect.anchorMax = new Vector2(0.5f, 0.5f);
            particleRect.sizeDelta = Vector2.zero;
            particles = particleRect.gameObject.AddComponent<EscapeWormholeParticleGraphic>();
            particles.Prime(0.6f);

            playerGroup = NewCentered("Player", root.transform);
            playerGroup.gameObject.SetActive(false);

            flashRect = NewCentered("Flash", root.transform);
            flashImage = flashRect.gameObject.AddComponent<Image>();
            // 모서리가 각진 사각 후광이 아니라 부드러운 원형 빛(코어 CCTV와 같은 그림)을 쓴다.
            flashImage.sprite = CoreCctvArt.Soft();
            flashImage.raycastTarget = false;
            flashImage.color = new Color(FlashColor.r, FlashColor.g, FlashColor.b, 0f);
            flashRect.gameObject.SetActive(false);

            SetDiameter(150f);
            holder.gameObject.SetActive(false);
        }

        /// <summary>캐릭터 모습을 복제해 그룹에 넣는다. 한 번만 부른다.</summary>
        public void SetPlayerProxy(IReadOnlyList<EscapeWarpProxySprite> sprites)
        {
            for (var i = 0; i < proxyImages.Count; i++)
            {
                if (proxyImages[i] != null)
                {
                    Object.Destroy(proxyImages[i].gameObject);
                }
            }

            proxyImages.Clear();
            if (sprites == null)
            {
                return;
            }

            for (var i = 0; i < sprites.Count; i++)
            {
                var source = sprites[i];
                if (source == null || source.Sprite == null)
                {
                    continue;
                }

                var rect = NewRect("Proxy" + i, playerGroup);
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = source.Pivot;
                rect.sizeDelta = source.Size;
                rect.anchoredPosition = source.Offset;
                rect.localRotation = Quaternion.Euler(0f, 0f, source.RotationZ);
                rect.localScale = new Vector3(source.FlipX ? -1f : 1f, 1f, 1f);
                var image = rect.gameObject.AddComponent<Image>();
                image.sprite = source.Sprite;
                image.color = source.Tint;
                image.raycastTarget = false;
                proxyImages.Add(image);
            }
        }

        /// <param name="screenPosition">웜홀 중심의 화면 픽셀 좌표</param>
        /// <param name="diameterPixels">웜홀 기본 지름(픽셀)</param>
        public void Apply(EscapeWormholeWarpFrame frame, Vector2 screenPosition, float diameterPixels, float deltaTime)
        {
            if (root == null)
            {
                return;
            }

            var diameter = Mathf.Max(8f, diameterPixels);
            blackImage.color = new Color(0f, 0f, 0f, Mathf.Clamp01(frame.BlackoutAlpha));

            var scale = Mathf.Max(0f, frame.WormholeScale);
            var holeVisible = scale > 0.002f;
            SetActive(holder, holeVisible);
            if (holeVisible)
            {
                holder.anchoredPosition = screenPosition;
                SetDiameter(diameter);
                holder.localScale = new Vector3(scale, scale, 1f);
                holderGroup.alpha = Mathf.Clamp01(scale * 1.6f);
                outerSwirl.localRotation = Quaternion.Euler(0f, 0f, frame.WormholeAngle);
                innerSwirl.localRotation = Quaternion.Euler(0f, 0f, frame.WormholeAngle * 1.4f);
                particles.Tick(Mathf.Clamp(deltaTime, 0f, 0.1f), scale > 0.3f && scale < 1.8f);
            }

            var suctionOn = frame.SuctionStrength > 0.003f;
            SetActive(suctionRect, suctionOn);
            if (suctionOn)
            {
                suctionRect.anchoredPosition = screenPosition;
                var outer = Mathf.Max(Screen.width, Screen.height) * 0.8f;
                suction.Configure(
                    frame.SuctionTime,
                    frame.SuctionStrength,
                    outer,
                    Mathf.Max(6f, diameter * 0.5f * Mathf.Max(0.35f, scale)));
            }

            var playerScale = Mathf.Clamp01(frame.PlayerScale);
            var playerOn = playerScale > 0.01f && proxyImages.Count > 0;
            SetActive(playerGroup, playerOn);
            if (playerOn)
            {
                playerGroup.anchoredPosition = screenPosition;
                playerGroup.localScale = new Vector3(playerScale, playerScale, 1f);
                playerGroup.localRotation = Quaternion.Euler(0f, 0f, frame.PlayerSpin);
            }

            var flash = Mathf.Clamp01(frame.FlashAlpha);
            SetActive(flashRect, flash > 0.01f);
            if (flash > 0.01f)
            {
                flashRect.anchoredPosition = screenPosition;
                var size = diameter * FlashRatio * Mathf.Lerp(0.55f, 1f, flash);
                flashRect.sizeDelta = new Vector2(size, size);
                flashImage.color = new Color(FlashColor.r, FlashColor.g, FlashColor.b, flash * 0.95f);
            }
        }

        public void Destroy()
        {
            if (root == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Object.Destroy(root);
            }
            else
            {
                Object.DestroyImmediate(root);
            }
        }

        private void SetDiameter(float diameter)
        {
            holder.sizeDelta = new Vector2(diameter, diameter);
            outerSwirl.sizeDelta = new Vector2(diameter * OuterSwirlRatio, diameter * OuterSwirlRatio);
            innerSwirl.sizeDelta = new Vector2(diameter * InnerSwirlRatio, diameter * InnerSwirlRatio);
            core.sizeDelta = new Vector2(diameter, diameter);
            var particleScale = (diameter * 0.5f) / ParticleReferenceRadius;
            particleRect.localScale = new Vector3(particleScale, particleScale, 1f);
        }

        private static void SetActive(Component component, bool active)
        {
            if (component != null && component.gameObject.activeSelf != active)
            {
                component.gameObject.SetActive(active);
            }
        }

        private static RectTransform NewImage(string name, Transform parent, Sprite sprite)
        {
            var rect = NewRect(name, parent);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false;
            return rect;
        }

        /// <summary>왼쪽 아래 기준 앵커의 점 오브젝트. anchoredPosition이 곧 화면 픽셀 좌표다.</summary>
        private static RectTransform NewCentered(string name, Transform parent)
        {
            var rect = NewRect(name, parent);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = Vector2.zero;
            return rect;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            return rect;
        }
    }
}

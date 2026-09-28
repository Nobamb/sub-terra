using System.Collections.Generic;
using SubTerra.App.UI.HUD;
using SubTerra.Gameplay.Mining;
using SubTerra.Shared;
using TMPro;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

namespace SubTerra.App.Integration
{
    /// <summary>
    /// 골드 지급이 확정된 채굴 칸에서 금화·금빛 가루를 튀기고,
    /// 플레이어 머리 위에 +금액 / BONUS 문구를 띄운다. 끝 무렵 작은 입자가 Gold HUD로 날아간다.
    /// 골드 데이터는 이미 지급된 뒤이며 이 컴포넌트는 시각 피드백만 담당한다.
    /// </summary>
    public sealed class GoldPickupVfx : MonoBehaviour
    {
        public const int WorldSortingOrder = 120;
        private const int MaxHudParticles = 9;
        private const float BonusFontRatio = 0.58f;
        private const float MainLineHeight = 46f;
        private const float BonusLineHeight = 28f;

        [SerializeField] private Sprite coinSprite;
        [SerializeField] private TMP_FontAsset pickupFont;
        [SerializeField] private Tilemap foregroundTilemap;
        // B-114에서 기준 크기를 Presentation으로 옮기고 배율만 노출한다.
        [SerializeField] private float coinScaleMultiplier = 1f;
        [SerializeField] private float mainFontSize = GoldPickupPresentation.MainFontSize;
        [SerializeField] private BasicHudView hudView;

        private MiningSystem boundSystem;
        private Transform playerTarget;
        private Collider2D playerCollider;
        private int pendingGold;
        private int pendingGoldBonus;
        private Transform worldRoot;
        private readonly List<CoinAnim> coins = new List<CoinAnim>(6);
        private readonly List<DustAnim> dusts = new List<DustAnim>(2);
        private readonly List<TextAnim> texts = new List<TextAnim>(GoldPickupPresentation.MaxActiveTexts + 1);
        private readonly List<HudParticle> hudParticles = new List<HudParticle>(MaxHudParticles);
        private readonly List<PulseTarget> pulseTargets = new List<PulseTarget>(2);
        private float pulseElapsed = -1f;

        public int PendingGold => pendingGold;
        public int ActiveCoinCount => coins.Count;
        public int ActiveDustCount => dusts.Count;
        public int ActiveTextCount => texts.Count;
        public int ActiveHudParticleCount => hudParticles.Count;
        public bool IsTextPlaying => texts.Count > 0;
        public bool IsPulsing => pulseElapsed >= 0f;

        public void BindTo(MiningSystem system, Transform player, Tilemap tilemap = null)
        {
            if (boundSystem != null)
            {
                boundSystem.TileMined -= OnTileMined;
                boundSystem.ProgressChanged -= OnProgressChanged;
            }

            boundSystem = system;
            playerTarget = player;
            playerCollider = playerTarget != null
                ? playerTarget.GetComponent<Collider2D>()
                : null;
            if (tilemap != null)
            {
                foregroundTilemap = tilemap;
            }

            if (foregroundTilemap == null)
            {
                foregroundTilemap = FindForegroundTilemap();
            }

            if (hudView == null)
            {
                hudView = FindAnyObjectByType<BasicHudView>(FindObjectsInactive.Include);
            }

            if (boundSystem != null)
            {
                boundSystem.TileMined += OnTileMined;
                boundSystem.ProgressChanged += OnProgressChanged;
            }
        }

        public void SetHudView(BasicHudView view)
        {
            RestorePulseTargets();
            hudView = view;
        }

        public void SetPendingGold(int amount, int bonus = 0)
        {
            pendingGold = amount > 0 ? amount : 0;
            pendingGoldBonus = Mathf.Clamp(bonus, 0, pendingGold);
        }

        public void Play(int goldAmount, Vector3 origin, Vector3 playerHead, int goldBonus = 0)
        {
            if (goldAmount <= 0)
            {
                return;
            }

            EnsureWorldRoot();
            SpawnCoins(origin);
            SpawnDust(origin);
            SpawnText(goldAmount, playerHead, goldBonus);
        }

        public void Tick(float unscaledDeltaTime)
        {
            if (unscaledDeltaTime < 0f)
            {
                unscaledDeltaTime = 0f;
            }

            TickCoins(unscaledDeltaTime);
            TickDust(unscaledDeltaTime);
            TickTexts(unscaledDeltaTime);
            TickHudParticles(unscaledDeltaTime);
            TickPulse(unscaledDeltaTime);
        }

        private void Update()
        {
            Tick(Time.unscaledDeltaTime);
        }

        private void OnDisable()
        {
            if (boundSystem != null)
            {
                boundSystem.TileMined -= OnTileMined;
                boundSystem.ProgressChanged -= OnProgressChanged;
                boundSystem = null;
            }

            // 비활성화·장면 전환 시 남은 연출을 즉시 정리하고 HUD 크기를 원복한다.
            ClearAll();
        }

        private void OnDestroy()
        {
            ClearAll();
            if (worldRoot != null)
            {
                DestroyHelper(worldRoot.gameObject);
                worldRoot = null;
            }
        }

        private void OnTileMined(Vector3Int cell, MiningTileDto tile)
        {
            if (pendingGold <= 0)
            {
                return;
            }

            int gold = pendingGold;
            int bonus = pendingGoldBonus;
            pendingGold = 0;
            pendingGoldBonus = 0;
            Play(gold, CellCenter(cell), HeadPosition(), bonus);
        }

        private void OnProgressChanged(MiningProgressState state)
        {
            if (state.Phase == MiningPhase.Failed || state.Phase == MiningPhase.Cancelled)
            {
                pendingGold = 0;
                pendingGoldBonus = 0;
            }
        }

        private void SpawnCoins(Vector3 origin)
        {
            if (coinSprite == null)
            {
                return;
            }

            float scaleRatio = coinScaleMultiplier > 0f ? coinScaleMultiplier : 1f;
            for (var index = 0; index < GoldPickupPresentation.CoinCount; index++)
            {
                var coinObject = new GameObject("GoldPickupCoin");
                coinObject.transform.SetParent(worldRoot, false);
                var renderer = coinObject.AddComponent<SpriteRenderer>();
                renderer.sprite = coinSprite;
                renderer.sortingOrder = WorldSortingOrder + GoldPickupPresentation.CoinSortingBias(index);
                renderer.color = new Color(1f, 1f, 1f, 0f);
                float scale = GoldPickupPresentation.CoinScale(index) * scaleRatio;
                coinObject.transform.localScale = new Vector3(scale, scale, 1f);
                coinObject.transform.position = GoldPickupPresentation.CoinStart(origin, index);

                coins.Add(new CoinAnim
                {
                    Renderer = renderer,
                    Index = index,
                    Origin = origin,
                    BaseScale = scale,
                    Delay = GoldPickupPresentation.CoinDelay(index),
                    Duration = GoldPickupPresentation.CoinFlightDuration(index),
                    Elapsed = 0f
                });
            }
        }

        private void SpawnDust(Vector3 origin)
        {
            if (coinSprite == null)
            {
                return;
            }

            Material material = coins.Count > 0 && coins[coins.Count - 1].Renderer != null
                ? coins[coins.Count - 1].Renderer.sharedMaterial
                : null;
            if (material == null)
            {
                return;
            }

            var dustObject = new GameObject("GoldPickupDust");
            dustObject.transform.SetParent(worldRoot, false);
            dustObject.transform.position = origin;
            var system = dustObject.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = system.main;
            main.duration = 0.1f;
            main.loop = false;
            main.playOnAwake = false;
            main.useUnscaledTime = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = GoldPickupPresentation.DustCount + 4;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, GoldPickupPresentation.DustLifetimeMax);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.1f, 2.4f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.035f, 0.075f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                GoldPickupPresentation.DustBrightColor,
                GoldPickupPresentation.DustDeepColor);
            main.gravityModifier = 0.55f;

            var emission = system.emission;
            emission.enabled = false;

            var shape = system.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 38f;
            shape.radius = 0.08f;
            shape.rotation = new Vector3(-90f, 0f, 0f);

            var colorOverLifetime = system.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var fade = new Gradient();
            fade.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.9f, 0.55f), new GradientAlphaKey(0f, 1f) });
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(fade);

            var sizeOverLifetime = system.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.35f));

            var sheet = system.textureSheetAnimation;
            sheet.enabled = true;
            sheet.mode = ParticleSystemAnimationMode.Sprites;
            sheet.AddSprite(coinSprite);

            var renderer = dustObject.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sortingOrder = WorldSortingOrder + 10;

            system.Play(true);
            system.Emit(GoldPickupPresentation.DustCount);
            dusts.Add(new DustAnim { Particles = system, Elapsed = 0f });
        }

        private void SpawnText(int goldAmount, Vector3 playerHead, int goldBonus)
        {
            string mainLabel = GoldPickupPresentation.FormatMainText(goldAmount, goldBonus);
            string bonusLabel = GoldPickupPresentation.FormatBonusText(goldAmount, goldBonus);
            if (string.IsNullOrEmpty(mainLabel) || pickupFont == null)
            {
                return;
            }

            bool hasBonus = !string.IsNullOrEmpty(bonusLabel);
            float fontSize = mainFontSize > 0f ? mainFontSize : GoldPickupPresentation.MainFontSize;

            // 연속 획득: 이전 문구를 새 문구 높이만큼 위로 밀고, 너무 많으면 가장 오래된 것부터 빠르게 지운다.
            float push = GoldPickupPresentation.StackStep(hasBonus);
            int alive = 0;
            for (var index = texts.Count - 1; index >= 0; index--)
            {
                TextAnim previous = texts[index];
                previous.StackTarget += push;
                if (previous.EvictElapsed < 0f)
                {
                    alive++;
                    if (alive >= GoldPickupPresentation.MaxActiveTexts)
                    {
                        previous.EvictElapsed = 0f;
                    }
                }
            }

            var root = new GameObject("GoldPickupText", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup));
            root.transform.SetParent(worldRoot, false);
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.overrideSorting = true;
            canvas.sortingOrder = WorldSortingOrder + 20;
            canvas.additionalShaderChannels =
                AdditionalCanvasShaderChannels.TexCoord1
                | AdditionalCanvasShaderChannels.Normal
                | AdditionalCanvasShaderChannels.Tangent;

            var rootGroup = root.GetComponent<CanvasGroup>();
            rootGroup.interactable = false;
            rootGroup.blocksRaycasts = false;

            float bonusBlock = hasBonus ? BonusLineHeight : 0f;
            var rect = root.GetComponent<RectTransform>();
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = new Vector2(480f, MainLineHeight + bonusBlock);
            root.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
            root.transform.position = playerHead;

            var anim = new TextAnim
            {
                Root = root,
                Anchor = playerHead,
                HasBonus = hasBonus,
                EvictElapsed = -1f
            };

            anim.MainLine = CreateLine(
                root.transform,
                "MainLine",
                mainLabel,
                fontSize,
                bonusBlock + MainLineHeight * 0.5f,
                MainLineHeight,
                true,
                out anim.MainGroup);
            if (hasBonus)
            {
                anim.BonusLine = CreateLine(
                    root.transform,
                    "BonusLine",
                    bonusLabel,
                    fontSize * BonusFontRatio,
                    BonusLineHeight * 0.5f,
                    BonusLineHeight,
                    false,
                    out anim.BonusGroup);
            }

            texts.Add(anim);
            ApplyText(anim);
        }

        private RectTransform CreateLine(
            Transform parent,
            string name,
            string label,
            float fontSize,
            float centerY,
            float height,
            bool isMain,
            out CanvasGroup group)
        {
            var line = new GameObject(name, typeof(RectTransform), typeof(CanvasGroup));
            line.transform.SetParent(parent, false);
            var lineRect = line.GetComponent<RectTransform>();
            lineRect.anchorMin = new Vector2(0.5f, 0f);
            lineRect.anchorMax = new Vector2(0.5f, 0f);
            lineRect.pivot = new Vector2(0.5f, 0.5f);
            lineRect.sizeDelta = new Vector2(480f, height);
            lineRect.anchoredPosition = new Vector2(0f, centerY);
            group = line.GetComponent<CanvasGroup>();
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;

            // 외곽선 머티리얼 인스턴스를 만들지 않도록 어두운 복제 글자를 그림자로 깐다.
            var shadow = CreateLabel(line.transform, "Shadow", label, fontSize, new Vector2(2f, -3f));
            shadow.color = GoldPickupPresentation.ShadowColor;

            var fill = CreateLabel(line.transform, "Fill", label, fontSize, Vector2.zero);
            if (isMain)
            {
                fill.color = Color.white;
                fill.enableVertexGradient = true;
                fill.colorGradient = new VertexGradient(
                    GoldPickupPresentation.MainTopColor,
                    GoldPickupPresentation.MainTopColor,
                    GoldPickupPresentation.MainBottomColor,
                    GoldPickupPresentation.MainBottomColor);
            }
            else
            {
                fill.color = GoldPickupPresentation.BonusColor;
                fill.characterSpacing = 4f;
                shadow.characterSpacing = 4f;
            }

            return lineRect;
        }

        private TextMeshProUGUI CreateLabel(Transform parent, string name, string label, float fontSize, Vector2 offset)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);
            var textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = offset;
            textRect.offsetMax = offset;
            var text = textObject.GetComponent<TextMeshProUGUI>();
            text.font = pickupFont;
            text.fontSize = fontSize;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
            text.raycastTarget = false;
            text.outlineWidth = 0f;
            text.text = label;
            return text;
        }

        private void TickCoins(float dt)
        {
            for (var index = coins.Count - 1; index >= 0; index--)
            {
                CoinAnim anim = coins[index];
                if (anim.Renderer == null)
                {
                    coins.RemoveAt(index);
                    continue;
                }

                anim.Elapsed += dt;
                float local = anim.Elapsed - anim.Delay;
                if (local < 0f)
                {
                    continue;
                }

                float duration = anim.Duration > 0f
                    ? anim.Duration
                    : GoldPickupPresentation.CoinFlightDuration(anim.Index);
                Transform coin = anim.Renderer.transform;
                coin.position = GoldPickupPresentation.EvaluateCoinPosition(anim.Origin, anim.Index, local);
                float spin = GoldPickupPresentation.CoinSpinScaleX(anim.Index, local);
                coin.localScale = new Vector3(anim.BaseScale * spin, anim.BaseScale, 1f);
                Color color = anim.Renderer.color;
                color.a = GoldPickupPresentation.CoinFadeIn(local)
                    * GoldPickupPresentation.CoinAlpha(local / duration);
                anim.Renderer.color = color;

                if (local >= duration)
                {
                    DestroyHelper(anim.Renderer.gameObject);
                    coins.RemoveAt(index);
                }
            }
        }

        private void TickDust(float dt)
        {
            for (var index = dusts.Count - 1; index >= 0; index--)
            {
                DustAnim anim = dusts[index];
                anim.Elapsed += dt;
                if (anim.Particles == null || anim.Elapsed >= GoldPickupPresentation.DustCleanupSeconds)
                {
                    if (anim.Particles != null)
                    {
                        DestroyHelper(anim.Particles.gameObject);
                    }

                    dusts.RemoveAt(index);
                }
            }
        }

        private void TickTexts(float dt)
        {
            for (var index = texts.Count - 1; index >= 0; index--)
            {
                TextAnim anim = texts[index];
                if (anim.Root == null)
                {
                    texts.RemoveAt(index);
                    continue;
                }

                anim.Elapsed += dt;
                if (anim.EvictElapsed >= 0f)
                {
                    anim.EvictElapsed += dt;
                }

                float follow = 1f - Mathf.Exp(-GoldPickupPresentation.TextStackFollowSpeed * dt);
                anim.StackOffset = Mathf.Lerp(anim.StackOffset, anim.StackTarget, follow);
                TryLaunchHudParticles(anim);
                ApplyText(anim);

                bool expired = anim.Elapsed >= GoldPickupPresentation.TextDuration
                    || anim.EvictElapsed >= GoldPickupPresentation.TextEvictSeconds;
                if (expired)
                {
                    DestroyHelper(anim.Root);
                    texts.RemoveAt(index);
                }
            }
        }

        private void ApplyText(TextAnim anim)
        {
            GoldPickupPresentation.EvaluateText(anim.Elapsed, out float alpha, out float yOffset, out float scale);
            float evict = anim.EvictElapsed >= 0f
                ? 1f - Mathf.Clamp01(anim.EvictElapsed / GoldPickupPresentation.TextEvictSeconds)
                : 1f;

            if (anim.MainGroup != null)
            {
                anim.MainGroup.alpha = alpha * evict;
            }

            if (anim.MainLine != null)
            {
                anim.MainLine.localScale = new Vector3(scale, scale, 1f);
            }

            if (anim.HasBonus)
            {
                GoldPickupPresentation.EvaluateBonus(anim.Elapsed, out float bonusAlpha, out float bonusScale);
                if (anim.BonusGroup != null)
                {
                    anim.BonusGroup.alpha = bonusAlpha * evict;
                }

                if (anim.BonusLine != null)
                {
                    anim.BonusLine.localScale = new Vector3(bonusScale, bonusScale, 1f);
                }
            }

            Vector3 head = playerTarget != null ? HeadPosition() : anim.Anchor;
            anim.Root.transform.position = head
                + Vector3.up * (GoldPickupPresentation.TextBaseLift + yOffset + anim.StackOffset);
        }

        private void TryLaunchHudParticles(TextAnim anim)
        {
            if (anim.EvictElapsed >= 0f || anim.Launched >= GoldPickupPresentation.HudParticleCount)
            {
                return;
            }

            if (coinSprite == null || !TryGetHudLayer(out RectTransform layer, out _))
            {
                anim.Launched = GoldPickupPresentation.HudParticleCount;
                return;
            }

            while (anim.Launched < GoldPickupPresentation.HudParticleCount
                && anim.Elapsed >= GoldPickupPresentation.HudLaunchTime(anim.Launched))
            {
                int particleIndex = anim.Launched;
                anim.Launched++;
                if (hudParticles.Count >= MaxHudParticles)
                {
                    continue;
                }

                Vector3 start = anim.MainLine != null ? anim.MainLine.position : anim.Root.transform.position;
                var particleObject = new GameObject("GoldPickupHudParticle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                particleObject.transform.SetParent(layer, false);
                particleObject.transform.SetAsLastSibling();
                var rect = particleObject.GetComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
                float size = GoldPickupPresentation.HudParticlePixelSize * (1f - 0.12f * particleIndex);
                rect.sizeDelta = new Vector2(size, size);
                var image = particleObject.GetComponent<Image>();
                image.sprite = coinSprite;
                image.preserveAspect = true;
                image.raycastTarget = false;
                image.color = new Color(1f, 0.93f, 0.7f, 0f);

                var particle = new HudParticle
                {
                    Rect = rect,
                    Image = image,
                    Index = particleIndex,
                    StartWorld = start,
                    Elapsed = 0f,
                    JustLaunched = true
                };
                hudParticles.Add(particle);
                UpdateHudParticle(particle, layer);
            }
        }

        private void TickHudParticles(float dt)
        {
            if (hudParticles.Count == 0)
            {
                return;
            }

            if (!TryGetHudLayer(out RectTransform layer, out _))
            {
                ClearHudParticles();
                return;
            }

            for (var index = hudParticles.Count - 1; index >= 0; index--)
            {
                HudParticle particle = hudParticles[index];
                if (particle.Rect == null)
                {
                    hudParticles.RemoveAt(index);
                    continue;
                }

                // 이번 프레임에 발사된 입자는 다음 프레임부터 진행한다.
                if (particle.JustLaunched)
                {
                    particle.JustLaunched = false;
                    continue;
                }

                particle.Elapsed += dt;
                UpdateHudParticle(particle, layer);
                if (particle.Elapsed >= GoldPickupPresentation.HudFlightSeconds)
                {
                    DestroyHelper(particle.Rect.gameObject);
                    hudParticles.RemoveAt(index);
                    StartPulse();
                }
            }
        }

        private void UpdateHudParticle(HudParticle particle, RectTransform layer)
        {
            Canvas canvas = layer.GetComponentInParent<Canvas>();
            Camera uiCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;
            Camera worldCamera = Camera.main;
            if (worldCamera == null)
            {
                return;
            }

            // 매 프레임 월드·HUD 좌표를 다시 변환해 카메라 이동과 해상도 변화에 대응한다.
            Vector2 startScreen = worldCamera.WorldToScreenPoint(particle.StartWorld);
            Vector2 endScreen = RectTransformUtility.WorldToScreenPoint(uiCamera, HudTargetWorld());
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(layer, startScreen, uiCamera, out Vector2 startLocal)
                || !RectTransformUtility.ScreenPointToLocalPointInRectangle(layer, endScreen, uiCamera, out Vector2 endLocal))
            {
                return;
            }

            // 레이어 피벗 기준 로컬 좌표를 중앙 앵커 기준 anchoredPosition으로 맞춘다.
            Vector2 pivotToCenter = layer.rect.center;
            float progress = GoldPickupPresentation.HudFlightProgress(particle.Elapsed);
            particle.Rect.anchoredPosition = GoldPickupPresentation.EvaluateHudPath(
                startLocal - pivotToCenter,
                endLocal - pivotToCenter,
                progress,
                particle.Index);
            float scale = Mathf.Lerp(1f, 0.7f, progress);
            particle.Rect.localScale = new Vector3(scale, scale, 1f);
            Color color = particle.Image.color;
            color.a = GoldPickupPresentation.HudParticleAlpha(particle.Elapsed);
            particle.Image.color = color;
        }

        private bool TryGetHudLayer(out RectTransform layer, out TextMeshProUGUI goldText)
        {
            layer = null;
            goldText = hudView != null ? hudView.GoldText : null;
            if (goldText == null || !goldText.isActiveAndEnabled)
            {
                return false;
            }

            layer = goldText.rectTransform.parent as RectTransform;
            return layer != null;
        }

        private Vector3 HudTargetWorld()
        {
            if (!TryGetHudLayer(out RectTransform layer, out TextMeshProUGUI goldText))
            {
                return Vector3.zero;
            }

            RectTransform icon = FindGoldIcon(layer);
            RectTransform target = icon != null ? icon : goldText.rectTransform;
            return target.TransformPoint(target.rect.center);
        }

        private static RectTransform FindGoldIcon(RectTransform layer)
        {
            // PromptB106HudBuilder가 만든 금화 아이콘. 없으면 Gold 텍스트로 대신한다.
            return layer != null ? layer.Find("HudIcon3") as RectTransform : null;
        }

        private void StartPulse()
        {
            if (pulseElapsed < 0f)
            {
                CapturePulseTargets();
            }

            pulseElapsed = 0f;
        }

        private void CapturePulseTargets()
        {
            pulseTargets.Clear();
            if (!TryGetHudLayer(out RectTransform layer, out TextMeshProUGUI goldText))
            {
                return;
            }

            RectTransform textRect = goldText.rectTransform;
            // 왼쪽 정렬 텍스트이므로 글자가 시작되는 왼쪽 중앙을 기준으로 키운다.
            pulseTargets.Add(new PulseTarget(
                textRect,
                new Vector2(textRect.rect.xMin, textRect.rect.center.y),
                goldText));
            RectTransform icon = FindGoldIcon(layer);
            if (icon != null)
            {
                pulseTargets.Add(new PulseTarget(icon, icon.rect.center, null));
            }
        }

        private void TickPulse(float dt)
        {
            if (pulseElapsed < 0f)
            {
                return;
            }

            pulseElapsed += dt;
            if (pulseElapsed >= GoldPickupPresentation.PulseSeconds)
            {
                RestorePulseTargets();
                return;
            }

            float scale = GoldPickupPresentation.EvaluatePulse(pulseElapsed);
            float flash = (scale - 1f) / (GoldPickupPresentation.PulsePeak - 1f);
            for (var index = 0; index < pulseTargets.Count; index++)
            {
                pulseTargets[index].Apply(scale, flash);
            }
        }

        private void RestorePulseTargets()
        {
            for (var index = 0; index < pulseTargets.Count; index++)
            {
                pulseTargets[index].Restore();
            }

            pulseTargets.Clear();
            pulseElapsed = -1f;
        }

        private Vector3 CellCenter(Vector3Int cell)
        {
            if (foregroundTilemap != null)
            {
                return foregroundTilemap.GetCellCenterWorld(cell);
            }

            return new Vector3(cell.x + 0.5f, cell.y + 0.5f, 0f);
        }

        private Vector3 HeadPosition()
        {
            if (playerTarget == null)
            {
                return Vector3.zero;
            }

            float top = playerCollider != null
                ? playerCollider.bounds.max.y
                : playerTarget.position.y;
            return new Vector3(playerTarget.position.x, top + 0.12f, playerTarget.position.z);
        }

        private void EnsureWorldRoot()
        {
            if (worldRoot != null)
            {
                return;
            }

            var root = new GameObject("GoldPickupWorldRoot");
            worldRoot = root.transform;
        }

        private void ClearHudParticles()
        {
            for (var index = 0; index < hudParticles.Count; index++)
            {
                if (hudParticles[index].Rect != null)
                {
                    DestroyHelper(hudParticles[index].Rect.gameObject);
                }
            }

            hudParticles.Clear();
        }

        private void ClearAll()
        {
            for (var index = 0; index < coins.Count; index++)
            {
                if (coins[index].Renderer != null)
                {
                    DestroyHelper(coins[index].Renderer.gameObject);
                }
            }

            coins.Clear();
            for (var index = 0; index < dusts.Count; index++)
            {
                if (dusts[index].Particles != null)
                {
                    DestroyHelper(dusts[index].Particles.gameObject);
                }
            }

            dusts.Clear();
            for (var index = 0; index < texts.Count; index++)
            {
                if (texts[index].Root != null)
                {
                    DestroyHelper(texts[index].Root);
                }
            }

            texts.Clear();
            ClearHudParticles();
            RestorePulseTargets();
        }

        private static Tilemap FindForegroundTilemap()
        {
            Tilemap[] maps = FindObjectsByType<Tilemap>(FindObjectsInactive.Include);
            for (var index = 0; index < maps.Length; index++)
            {
                Tilemap map = maps[index];
                if (map != null && map.name.IndexOf("Foreground", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return map;
                }
            }

            return maps.Length > 0 ? maps[0] : null;
        }

        private static void DestroyHelper(Object target)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(target);
            }
            else
            {
                DestroyImmediate(target);
            }
        }

        private sealed class CoinAnim
        {
            public SpriteRenderer Renderer;
            public int Index;
            public Vector3 Origin;
            public float BaseScale;
            public float Delay;
            public float Duration;
            public float Elapsed;
        }

        private sealed class DustAnim
        {
            public ParticleSystem Particles;
            public float Elapsed;
        }

        private sealed class TextAnim
        {
            public GameObject Root;
            public RectTransform MainLine;
            public RectTransform BonusLine;
            public CanvasGroup MainGroup;
            public CanvasGroup BonusGroup;
            public Vector3 Anchor;
            public bool HasBonus;
            public float Elapsed;
            public float StackOffset;
            public float StackTarget;
            public float EvictElapsed;
            public int Launched;
        }

        private sealed class HudParticle
        {
            public RectTransform Rect;
            public Image Image;
            public int Index;
            public Vector3 StartWorld;
            public float Elapsed;
            public bool JustLaunched;
        }

        private sealed class PulseTarget
        {
            private readonly RectTransform rect;
            private readonly TextMeshProUGUI text;
            private readonly Vector3 originalScale;
            private readonly Vector2 originalPosition;
            private readonly Vector2 focus;
            private readonly Color originalColor;

            public PulseTarget(RectTransform rect, Vector2 focus, TextMeshProUGUI text)
            {
                this.rect = rect;
                this.text = text;
                this.focus = focus;
                originalScale = rect.localScale;
                originalPosition = rect.anchoredPosition;
                originalColor = text != null ? text.color : Color.white;
            }

            public void Apply(float scale, float flash)
            {
                if (rect == null)
                {
                    return;
                }

                // 피벗을 바꾸지 않고 focus 지점이 제자리에 있도록 위치를 보정한다.
                rect.localScale = new Vector3(originalScale.x * scale, originalScale.y * scale, originalScale.z);
                rect.anchoredPosition = originalPosition - focus * (scale - 1f);
                if (text != null)
                {
                    text.color = Color.Lerp(originalColor, GoldPickupPresentation.MainTopColor, Mathf.Clamp01(flash) * 0.7f);
                }
            }

            public void Restore()
            {
                if (rect == null)
                {
                    return;
                }

                rect.localScale = originalScale;
                rect.anchoredPosition = originalPosition;
                if (text != null)
                {
                    text.color = originalColor;
                }
            }
        }
    }
}

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
    /// 골드 지급이 확정되면 채굴 칸에 금빛 가루를 튀기고, 플레이어 머리 위에 금색 홀로그램 팝업 하나를 띄운다
    /// (prompt-B 142). 연속 획득은 살아 있는 팝업에 합산하고, 퇴장 시작에 작은 입자가 Gold HUD로 날아간다.
    /// 골드 데이터는 이미 지급된 뒤이며 이 컴포넌트는 시각 피드백만 담당한다.
    /// </summary>
    public sealed class GoldPickupVfx : MonoBehaviour
    {
        public const int WorldSortingOrder = 120;
        private const int MaxHudParticles = 9;

        [SerializeField] private Sprite coinSprite;
        [SerializeField] private TMP_FontAsset pickupFont;
        [SerializeField] private Tilemap foregroundTilemap;
        // 금빛 가루 스프라이트 배율. 팝업 금화 크기는 GoldPickupPopupTimeline이 정한다.
        [SerializeField] private float coinScaleMultiplier = 1f;
        [SerializeField] private BasicHudView hudView;

        private MiningSystem boundSystem;
        private Transform playerTarget;
        private Collider2D playerCollider;
        private int pendingGold;
        private int pendingGoldBonus;
        private Transform worldRoot;
        private readonly List<DustAnim> dusts = new List<DustAnim>(2);
        private PopupRuntime popup;
        private Material dustMaterial;
        private readonly List<HudParticle> hudParticles = new List<HudParticle>(MaxHudParticles);
        private readonly List<PulseTarget> pulseTargets = new List<PulseTarget>(2);
        private float pulseElapsed = -1f;

        public int PendingGold => pendingGold;
        public int ActiveCoinCount => popup != null && popup.Visual != null ? popup.Visual.ActiveCoinCount : 0;
        public int ActiveDustCount => dusts.Count;
        public int ActivePopupCount => IsPopupAlive() ? 1 : 0;
        public int ActiveHudParticleCount => hudParticles.Count;
        public bool IsPopupPlaying => IsPopupAlive();
        public GoldPickupPopupState PopupState => IsPopupAlive() ? popup.State : null;
        public GoldPickupPopupVisual PopupVisual => IsPopupAlive() ? popup.Visual : null;
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
            SpawnDust(origin);
            ShowPopup(
                GoldPickupPresentation.BaseGold(goldAmount, goldBonus),
                GoldPickupPresentation.ClampBonus(goldAmount, goldBonus),
                playerHead);
        }

        public void Tick(float unscaledDeltaTime)
        {
            if (unscaledDeltaTime < 0f)
            {
                unscaledDeltaTime = 0f;
            }

            TickPopup(unscaledDeltaTime);
            TickDust(unscaledDeltaTime);
            TickHudParticles(unscaledDeltaTime);
            TickPulse(unscaledDeltaTime);
        }

        /// <summary>남은 팝업·금빛 가루·HUD 입자를 즉시 지우고 HUD 크기를 원복한다.</summary>
        public void ClearEffects()
        {
            ClearAll();
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

        private void SpawnDust(Vector3 origin)
        {
            if (coinSprite == null)
            {
                return;
            }

            Material material = GetDustMaterial();
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
            float sizeRatio = coinScaleMultiplier > 0f ? coinScaleMultiplier : 1f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.035f * sizeRatio, 0.075f * sizeRatio);
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

        private bool IsPopupAlive()
        {
            return popup != null && popup.Visual != null && popup.Visual.IsAlive;
        }

        private void ShowPopup(int baseGold, int bonusGold, Vector3 playerHead)
        {
            if (pickupFont == null)
            {
                return;
            }

            if (popup != null && !IsPopupAlive())
            {
                popup = null;
            }

            // 살아 있는 팝업(퇴장 중 포함)이 있으면 새로 만들지 않고 확정값만 합산한다.
            if (popup != null && !popup.State.Finished)
            {
                int increase = popup.State.Merge(baseGold, bonusGold);
                if (increase > 0)
                {
                    popup.Visual.SpawnBurst(GoldPickupPopupTimeline.MergeCoinCount(increase), -18f);
                    popup.Anchor = playerHead;
                }

                return;
            }

            DestroyPopup();
            var state = new GoldPickupPopupState(baseGold, bonusGold);
            var visual = GoldPickupPopupVisual.Create(worldRoot, pickupFont, coinSprite);
            popup = new PopupRuntime { State = state, Visual = visual, Anchor = playerHead };
            visual.SetWorldPosition(playerHead + Vector3.up * GoldPickupPresentation.TextBaseLift, 0f);
            visual.Tick(0f, state);
        }

        private void TickPopup(float dt)
        {
            if (popup == null)
            {
                return;
            }

            if (!IsPopupAlive())
            {
                popup = null;
                return;
            }

            GoldPickupPopupState state = popup.State;
            GoldPickupPopupVisual visual = popup.Visual;
            state.Advance(dt);
            if (state.TakeBaseLanding())
            {
                visual.SpawnBurst(GoldPickupPopupTimeline.BaseLandingCoinCount(state.BaseTotal));
                visual.FlashEdge();
            }

            if (state.TakeBonusLanding())
            {
                visual.SpawnBurst(GoldPickupPopupTimeline.BonusLandingCoinCount(state.BonusTotal), 18f);
                visual.StartSweep();
            }

            visual.Tick(dt, state);
            Vector3 head = playerTarget != null ? HeadPosition() : popup.Anchor;
            visual.SetWorldPosition(
                head + Vector3.up * GoldPickupPresentation.TextBaseLift,
                GoldPickupPopupTimeline.ExitRiseWorld(state.ExitAmount));
            TryLaunchHudParticles(popup);

            bool hudDone = popup.HudLaunched >= GoldPickupPresentation.HudParticleCount;
            if (state.Finished && hudDone)
            {
                DestroyPopup();
            }
        }

        private void DestroyPopup()
        {
            if (popup != null && popup.Visual != null)
            {
                popup.Visual.Destroy();
            }

            popup = null;
        }

        private void TryLaunchHudParticles(PopupRuntime target)
        {
            // 퇴장이 처음 시작될 때만 발사한다. 퇴장이 취소·재개돼도 같은 팝업은 다시 발사하지 않는다.
            if (!target.State.HudSequenceStarted
                || target.HudLaunched >= GoldPickupPresentation.HudParticleCount)
            {
                return;
            }

            if (coinSprite == null || !TryGetHudLayer(out RectTransform layer, out _))
            {
                target.HudLaunched = GoldPickupPresentation.HudParticleCount;
                return;
            }

            while (target.HudLaunched < GoldPickupPresentation.HudParticleCount
                && target.State.HudClock >= GoldPickupPresentation.HudLaunchTime(target.HudLaunched))
            {
                int particleIndex = target.HudLaunched;
                target.HudLaunched++;
                if (hudParticles.Count >= MaxHudParticles)
                {
                    continue;
                }

                Vector3 start = target.Visual.MainLineWorldPosition;
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

        private Material GetDustMaterial()
        {
            if (dustMaterial != null)
            {
                return dustMaterial;
            }

            // 기본 스프라이트 머티리얼을 SpriteRenderer에서 한 번만 빌려 온다.
            var probe = new GameObject("GoldPickupDustProbe");
            probe.transform.SetParent(worldRoot, false);
            var renderer = probe.AddComponent<SpriteRenderer>();
            dustMaterial = renderer.sharedMaterial;
            // 같은 프레임에 SpriteRenderer가 남지 않도록 즉시 지운다.
            DestroyImmediate(probe);
            return dustMaterial;
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
            DestroyPopup();
            for (var index = 0; index < dusts.Count; index++)
            {
                if (dusts[index].Particles != null)
                {
                    DestroyHelper(dusts[index].Particles.gameObject);
                }
            }

            dusts.Clear();
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

        private sealed class DustAnim
        {
            public ParticleSystem Particles;
            public float Elapsed;
        }

        private sealed class PopupRuntime
        {
            public GoldPickupPopupState State;
            public GoldPickupPopupVisual Visual;
            public Vector3 Anchor;
            public int HudLaunched;
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

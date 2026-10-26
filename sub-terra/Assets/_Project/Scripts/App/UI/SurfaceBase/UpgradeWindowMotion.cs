using System;
using SubTerra.App.Audio;
using SubTerra.App.UI.Outpost;
using SubTerra.Shared;
using UnityEngine;

namespace SubTerra.App.UI.SurfaceBase
{
    /// <summary>
    /// B-167 업그레이드 창(UpgradeModal) 열기·닫기 연출. 큰 청록 금속 자물쇠가 가운데에서 금이 가며 부서지고
    /// 그 자리에서 카드가 펼쳐진다. 카드는 scale·CanvasGroup 알파로만 움직여 트리 맞춤(sizeDelta)을 건드리지 않고,
    /// 끝나면 원래 값으로 정확히 되돌린다. 자물쇠·금·조각은 처음 쓸 때 코드로 한 번 만든다.
    /// 프리팹·씬에는 없고 런타임에 붙인다. 지상 기지는 SurfaceBaseView가 딤 루트(upgradeRoot)에, 광산은 PanelToggleController가
    /// 카드(UpgradePanel) 자체에 붙인다. 카드 자체에 붙으면 딤이 없고 자물쇠는 카드 옆(같은 부모)에 그린다.
    /// 시간표는 UpgradeWindowMotionTimeline이 정한다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UpgradeWindowMotion : MonoBehaviour
    {
        public enum MotionState
        {
            Hidden = 0,
            Opening = 1,
            Open = 2,
            Closing = 3
        }

        private const float MaxStep = 0.05f;
        private const float MinScale = 0.001f;
        private static readonly Color CrackColor = new Color(0.72f, 1f, 1f, 1f);
        private static readonly Color GlowColor = new Color(0.35f, 0.95f, 1f, 1f);

        private static bool introPlayed;

        private RectTransform card;
        private CanvasGroup cardGroup;
        private UnityEngine.UI.Image dimmer;
        private Vector3 originalScale = Vector3.one;
        private float originalAlpha = 1f;
        private bool originalInteractable = true;
        private bool originalBlocksRaycasts = true;
        private float originalDimAlpha = 1f;
        private bool captured;

        private RectTransform fxRoot;
        private Canvas fxCanvas;
        private RectTransform lockRoot;
        private UnityEngine.UI.Image body;
        private UnityEngine.UI.Image legLeft;
        private UnityEngine.UI.Image legRight;
        private UnityEngine.UI.Image shackleTop;
        private UnityEngine.UI.Image crack;
        private UnityEngine.UI.Image glow;
        private UnityEngine.UI.Image[] shards;
        private Vector2 legRightBase;
        private Vector2 shackleTopBase;
        private Texture2D crackTexture;
        private Color32[] crackBuffer;
        private int crackCount = -1;

        private MotionState state;
        private bool fullOpen;
        private float clock;
        private float closeClock;
        private UpgradeWindowFrame frame;
        private UpgradeWindowFrame closeFrom;
        private float closeOpenClock;
        private bool rewinding;
        private Action closeFinished;

        /// <summary>이번 플레이 세션에서 전체(처음) 열기 연출을 이미 보여 줬는지. 게임 가이드와 같은 규칙이다.</summary>
        public static bool IntroPlayed
        {
            get => introPlayed;
            set => introPlayed = value;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetForPlaySession()
        {
            // 도메인 리로드를 끈 에디터에서도 새 플레이마다 처음 상태로 시작한다.
            introPlayed = false;
        }

        public static void ResetSessionForTests()
        {
            introPlayed = false;
        }

        // 효과음 훅(B-167에서는 소리를 달지 않는다).
        public event Action ShakeStarted;
        public event Action CrackStarted;
        public event Action Shattered;
        public event Action CloseStarted;
        public event Action FragmentsRegathered;

        /// <summary>창이 완전히 펼쳐지고 조작이 풀린 프레임에 한 번. B-169 노드 해금 연출은 여기서 시작한다.</summary>
        public event Action OpenCompleted;
        public event Action CloseCompleted;

        /// <summary>확인용: true면 Update가 시간을 진행하지 않고 테스트가 Tick으로 직접 진행한다.</summary>
        public bool ManualTick { get; set; }

        public MotionState State => state;
        public bool IsOpening => state == MotionState.Opening;
        public bool IsClosing => state == MotionState.Closing;
        public bool IsSettled => state == MotionState.Open;
        public bool IsFullOpen => fullOpen;
        public float Clock => state == MotionState.Closing ? closeClock : clock;
        public UpgradeWindowFrame Frame => frame;
        public RectTransform Card => card;
        public RectTransform LockRoot => lockRoot;
        public RectTransform EffectsRoot => fxRoot;
        public bool EffectsVisible => fxRoot != null && fxRoot.gameObject.activeSelf;

        /// <summary>t가 속한 업그레이드 창이 지금 열리는 중인지(심층 썸네일 해금 연출을 미룰 때 쓴다).</summary>
        public static bool IsOpeningFor(Transform t)
        {
            var motion = t != null ? t.GetComponentInParent<UpgradeWindowMotion>(true) : null;
            return motion != null && motion.IsOpening;
        }

        /// <summary>
        /// 연다. 세션 첫 열기는 흔들림이 있는 전체 연출, 이후는 짧은 연출이다. 닫는 중이면 닫던 길을 되감아 지금 모양에서 이어서 연다.
        /// 루트 활성화·정렬은 호출한 쪽(SurfaceBaseView)이 먼저 한다.
        /// </summary>
        public void BeginOpen()
        {
            if (state == MotionState.Opening || state == MotionState.Open)
            {
                return;
            }

            var resume = state == MotionState.Closing;
            closeFinished = null;
            if (!resume)
            {
                Capture();
            }

            if (card == null || AccessibilityPreferences.ReduceMotion || !IsLive())
            {
                state = MotionState.Opening;
                Settle(true);
                return;
            }

            state = MotionState.Opening;
            SetCardLocked();
            if (resume)
            {
                // 닫던 길을 거꾸로 되감아 지금 모양에서 이어서 연다. 이미 지난 훅은 다시 내지 않는다.
                rewinding = true;
                if (fullOpen && closeOpenClock < UpgradeWindowMotionTimeline.FullCrackStartTime)
                {
                    // 처음 열기의 흔들림 전에 닫혔다면 되감은 뒤 다시 열기 규칙(흔들림 없음)으로 잇는다.
                    closeFrom.LockAngle = 0f;
                }

                ApplyFrame(UpgradeWindowMotionTimeline.Close(closeClock, closeFrom));
                return;
            }

            fullOpen = !introPlayed;
            introPlayed = true;
            rewinding = false;
            clock = 0f;
            ApplyFrame(UpgradeWindowMotionTimeline.Open(clock, fullOpen));
        }

        /// <summary>닫는 연출을 시작한다. 여는 중에도 받아들이며, 끝나면 finished(루트 비활성화)를 한 번 부른다.</summary>
        public bool BeginClose(Action finished)
        {
            if (state == MotionState.Closing)
            {
                return false;
            }

            if (state == MotionState.Hidden)
            {
                finished?.Invoke();
                return false;
            }

            if (state == MotionState.Open)
            {
                Capture();
                closeOpenClock = UpgradeWindowMotionTimeline.OpenDuration(fullOpen);
                closeFrom = UpgradeWindowMotionTimeline.Open(closeOpenClock, fullOpen);
                closeClock = 0f;
            }
            else if (rewinding)
            {
                // 되감던 중 다시 닫으면 같은 닫기 길에서 그대로 이어 간다.
                rewinding = false;
            }
            else
            {
                closeOpenClock = clock;
                closeFrom = frame;
                closeClock = 0f;
            }

            closeFinished = finished;
            state = MotionState.Closing;
            if (card == null || AccessibilityPreferences.ReduceMotion || !IsLive())
            {
                FinishClose();
                return true;
            }

            SetCardLocked();
            UiKeyboardSubmitGuard.ClearSelection();
            Raise(CloseStarted);
            ApplyFrame(UpgradeWindowMotionTimeline.Close(0f, closeFrom));
            return true;
        }

        /// <summary>연출 없이 바로 끝 상태로 맞춘다(설정 창 강제 숨김·Bind·OnDisable 경로). 진행 중 연출은 정리한다.</summary>
        public void CompleteImmediately(bool visible)
        {
            if (visible)
            {
                if (state == MotionState.Open)
                {
                    return;
                }

                closeFinished = null;
                if (state == MotionState.Hidden)
                {
                    Capture();
                }

                Settle(true);
                return;
            }

            if (state == MotionState.Hidden)
            {
                Restore();
                HideEffects();
                return;
            }

            closeFinished = null;
            FinishClose();
        }

        private void Update()
        {
            if (ManualTick || (state != MotionState.Opening && state != MotionState.Closing))
            {
                return;
            }

            Tick(Mathf.Min(Time.unscaledDeltaTime, MaxStep));
        }

        /// <summary>테스트와 Update가 같은 경로로 시간을 진행한다.</summary>
        public void Tick(float dt)
        {
            dt = Mathf.Max(0f, dt);
            if (AccessibilityPreferences.ReduceMotion)
            {
                // 연출 중에 모션 감소가 켜지면 바로 끝 상태로.
                if (state == MotionState.Opening) Settle(true);
                else if (state == MotionState.Closing) FinishClose();
                return;
            }

            switch (state)
            {
                case MotionState.Opening:
                    if (rewinding)
                    {
                        Rewind(dt);
                        break;
                    }

                    var previous = clock;
                    clock += dt;
                    if (fullOpen && SfxPlayback.Crossed(previous, clock, UpgradeWindowMotionTimeline.ShakeStartTime))
                    {
                        Raise(ShakeStarted);
                    }

                    if (SfxPlayback.Crossed(previous, clock, UpgradeWindowMotionTimeline.CrackStartTime(fullOpen)))
                    {
                        Raise(CrackStarted);
                    }

                    if (SfxPlayback.Crossed(previous, clock, UpgradeWindowMotionTimeline.ShatterTime(fullOpen)))
                    {
                        Raise(Shattered);
                    }

                    if (clock >= UpgradeWindowMotionTimeline.OpenDuration(fullOpen))
                    {
                        Settle(true);
                    }
                    else
                    {
                        ApplyFrame(UpgradeWindowMotionTimeline.Open(clock, fullOpen));
                    }

                    break;
                case MotionState.Closing:
                    var before = closeClock;
                    closeClock += dt;
                    if (closeFrom.Shatter > 0f
                        && SfxPlayback.Crossed(before, closeClock, UpgradeWindowMotionTimeline.RegatherTime))
                    {
                        Raise(FragmentsRegathered);
                    }

                    if (closeClock >= UpgradeWindowMotionTimeline.CloseDuration)
                    {
                        FinishClose();
                    }
                    else
                    {
                        ApplyFrame(UpgradeWindowMotionTimeline.Close(closeClock, closeFrom));
                    }

                    break;
            }
        }

        private void Rewind(float dt)
        {
            closeClock -= dt;
            if (closeClock > 0f)
            {
                ApplyFrame(UpgradeWindowMotionTimeline.Close(closeClock, closeFrom));
                return;
            }

            rewinding = false;
            closeClock = 0f;
            clock = closeOpenClock;
            if (fullOpen && clock < UpgradeWindowMotionTimeline.FullCrackStartTime)
            {
                // 나타남 구간은 같은 모양이라 비율만 줄여 다시 열기 시간표로 옮긴다.
                clock = clock < UpgradeWindowMotionTimeline.ShakeStartTime
                    ? clock * UpgradeWindowMotionTimeline.QuickCrackStartTime / UpgradeWindowMotionTimeline.ShakeStartTime
                    : UpgradeWindowMotionTimeline.QuickCrackStartTime;
                fullOpen = false;
            }

            if (clock >= UpgradeWindowMotionTimeline.OpenDuration(fullOpen))
            {
                Settle(true);
            }
            else
            {
                ApplyFrame(UpgradeWindowMotionTimeline.Open(clock, fullOpen));
            }
        }

        private void OnDisable()
        {
            // 씬 이동·부모 비활성화로 꺼지면 끝 상태로 정리하고 효과를 끈다.
            if (state == MotionState.Closing)
            {
                FinishClose();
            }
            else if (state == MotionState.Opening)
            {
                Settle(false);
            }
            else
            {
                HideEffects();
            }
        }

        private void OnDestroy()
        {
            // 카드 자체에 붙은 경우 효과는 카드 밖(형제)에 있으므로 함께 지운다.
            if (fxRoot != null && fxRoot.parent != transform)
            {
                if (Application.isPlaying) Destroy(fxRoot.gameObject);
                else DestroyImmediate(fxRoot.gameObject);
            }

            if (crackTexture != null)
            {
                if (Application.isPlaying) Destroy(crackTexture);
                else DestroyImmediate(crackTexture);
                crackTexture = null;
            }
        }

        private bool HostIsCard => card != null && card == transform;

        private bool IsLive()
        {
            return enabled && gameObject.activeInHierarchy;
        }

        private void Capture()
        {
            ResolveTargets();
            if (card == null)
            {
                return;
            }

            originalScale = card.localScale;
            originalAlpha = cardGroup.alpha;
            originalInteractable = cardGroup.interactable;
            originalBlocksRaycasts = cardGroup.blocksRaycasts;
            originalDimAlpha = dimmer != null ? dimmer.color.a : 1f;
            captured = true;
        }

        private void ResolveTargets()
        {
            if (card == null)
            {
                var drag = GetComponentInChildren<PopupWindowDrag>(true);
                card = drag != null ? drag.transform as RectTransform : null;
            }

            if (card != null && cardGroup == null)
            {
                cardGroup = card.GetComponent<CanvasGroup>();
                if (cardGroup == null)
                {
                    cardGroup = card.gameObject.AddComponent<CanvasGroup>();
                }
            }

            // 카드 자체에 붙었으면 그 Image는 창 배경이라 딤으로 쓰지 않는다.
            if (dimmer == null && !HostIsCard)
            {
                dimmer = GetComponent<UnityEngine.UI.Image>();
            }
        }

        private void SetCardLocked()
        {
            // 딤은 raycast를 유지하고 카드 안 노드·구매 버튼·창 드래그만 막는다.
            if (cardGroup == null) return;
            cardGroup.interactable = false;
            cardGroup.blocksRaycasts = false;
        }

        private void Settle(bool notify)
        {
            state = MotionState.Open;
            rewinding = false;
            clock = UpgradeWindowMotionTimeline.OpenDuration(fullOpen);
            frame = UpgradeWindowMotionTimeline.Open(clock, fullOpen);
            Restore();
            HideEffects();
            if (notify)
            {
                // B-169: 노드 자물쇠 해금 연출은 이 이벤트를 구독해 창이 다 열린 뒤 시작한다.
                Raise(OpenCompleted);
            }
        }

        private void FinishClose()
        {
            state = MotionState.Hidden;
            rewinding = false;
            clock = 0f;
            closeClock = 0f;
            frame = default;
            Restore();
            HideEffects();
            var finished = closeFinished;
            closeFinished = null;
            finished?.Invoke();
            Raise(CloseCompleted);
        }

        private void Restore()
        {
            if (!captured || card == null)
            {
                return;
            }

            card.localScale = originalScale;
            cardGroup.alpha = originalAlpha;
            cardGroup.interactable = originalInteractable;
            cardGroup.blocksRaycasts = originalBlocksRaycasts;
            SetDim(originalDimAlpha);
        }

        private void SetDim(float alpha)
        {
            if (dimmer == null) return;
            var color = dimmer.color;
            color.a = alpha;
            dimmer.color = color;
        }

        private void ApplyFrame(UpgradeWindowFrame f)
        {
            frame = f;
            if (card == null)
            {
                return;
            }

            var expand = Mathf.Max(MinScale, f.Expand);
            card.localScale = new Vector3(originalScale.x * expand, originalScale.y * expand, originalScale.z);
            cardGroup.alpha = originalAlpha * f.WindowAlpha;
            SetDim(originalDimAlpha * f.Dim);

            EnsureEffects();
            fxRoot.gameObject.SetActive(true);
            if (!HostIsCard) fxRoot.SetAsLastSibling();
            // 카드가 드래그로 옮겨져 있어도 카드 가운데에서 부서지고 펼쳐진다.
            fxRoot.position = card.TransformPoint(card.rect.center);
            var cardCanvas = card.GetComponent<Canvas>();
            var rootCanvas = GetComponent<Canvas>();
            var reference = cardCanvas != null ? cardCanvas : rootCanvas;
            if (reference != null)
            {
                fxCanvas.sortingLayerID = reference.sortingLayerID;
                fxCanvas.sortingOrder = reference.sortingOrder + 1;
            }

            var unit = UpgradeWindowMotionTimeline.LockUnit;
            lockRoot.anchoredPosition = new Vector2(0f, f.LockY);
            lockRoot.localRotation = Quaternion.Euler(0f, 0f, f.LockAngle);
            lockRoot.localScale = new Vector3(f.LockScale, f.LockScale, 1f);
            legRight.rectTransform.anchoredPosition = legRightBase + new Vector2(0f, f.ShackleLift * unit);
            shackleTop.rectTransform.anchoredPosition = shackleTopBase + new Vector2(0f, f.ShackleLift * unit);
            lockRoot.gameObject.SetActive(f.LockAlpha > 0.001f);
            SetAlpha(body, Color.white, f.LockAlpha);
            SetAlpha(legLeft, Color.white, f.LockAlpha);
            SetAlpha(legRight, Color.white, f.LockAlpha);
            SetAlpha(shackleTop, Color.white, f.LockAlpha);
            SetAlpha(crack, CrackColor, f.LockAlpha);
            PaintCrack(f.Crack);

            glow.rectTransform.sizeDelta = UpgradeWindowMotionTimeline.LockSize * (1.7f * Mathf.Max(0.5f, f.LockScale));
            glow.rectTransform.anchoredPosition = new Vector2(0f, f.LockY);
            SetAlpha(glow, GlowColor, 0.75f * f.Glow);

            var shardsVisible = f.ShardAlpha > 0.001f;
            for (var i = 0; i < shards.Length; i++)
            {
                shards[i].gameObject.SetActive(shardsVisible);
                if (!shardsVisible) continue;
                UpgradeWindowMotionTimeline.ShardPose(i, f.ShardSpread, out var position, out var angle);
                shards[i].rectTransform.anchoredPosition = new Vector2(0f, f.LockY) + position * f.LockScale;
                shards[i].rectTransform.localRotation = Quaternion.Euler(0f, 0f, angle);
                SetAlpha(shards[i], Color.white, f.ShardAlpha);
            }
        }

        private void PaintCrack(float progress)
        {
            var count = UpgradeWindowLockArt.VisibleCrackCount(progress);
            if (count == crackCount) return;
            crackCount = UpgradeWindowLockArt.PaintCrack(crackTexture, progress, crackBuffer);
        }

        private static void SetAlpha(UnityEngine.UI.Image image, Color color, float alpha)
        {
            color.a *= Mathf.Clamp01(alpha);
            image.color = color;
        }

        private void HideEffects()
        {
            if (fxRoot != null && fxRoot.gameObject.activeSelf)
            {
                fxRoot.gameObject.SetActive(false);
            }
        }

        private void EnsureEffects()
        {
            if (fxRoot != null)
            {
                return;
            }

            var root = new GameObject("UpgradeWindowLockFx", typeof(RectTransform), typeof(Canvas));
            root.layer = gameObject.layer;
            fxRoot = (RectTransform)root.transform;
            // 카드 안에 두면 카드 scale(0→1)에 같이 줄어드므로 카드 자체가 호스트면 형제로 둔다.
            fxRoot.SetParent(HostIsCard && transform.parent != null ? transform.parent : transform, false);
            root.AddComponent<UnityEngine.UI.LayoutElement>().ignoreLayout = true;
            fxRoot.anchorMin = fxRoot.anchorMax = new Vector2(0.5f, 0.5f);
            fxRoot.pivot = new Vector2(0.5f, 0.5f);
            fxRoot.sizeDelta = UpgradeWindowMotionTimeline.LockSize;
            fxCanvas = root.GetComponent<Canvas>();
            fxCanvas.overrideSorting = true;

            glow = NewImage("LockGlow", fxRoot, CoreCctvArt.Soft(), Vector2.zero, UpgradeWindowMotionTimeline.LockSize * 2.1f);

            var unit = UpgradeWindowMotionTimeline.LockUnit;
            lockRoot = new GameObject("Lock", typeof(RectTransform)).GetComponent<RectTransform>();
            lockRoot.gameObject.layer = gameObject.layer;
            lockRoot.SetParent(fxRoot, false);
            lockRoot.anchorMin = lockRoot.anchorMax = new Vector2(0.5f, 0.5f);
            lockRoot.pivot = new Vector2(0.5f, 0.5f);
            lockRoot.sizeDelta = UpgradeWindowMotionTimeline.LockSize;
            // 노드·썸네일 자물쇠와 같은 4조각 배치(PromptB118UpgradeTreeBuilder, DeepZoneThumbnailStrip). 몸통이 고리 다리를 덮는다.
            legLeft = NewImage("ShackleLeft", lockRoot, UpgradeWindowLockArt.ShackleLeg(), new Vector2(-8f, 6f) * unit, new Vector2(5f, 16f) * unit);
            legRight = NewImage("ShackleRight", lockRoot, UpgradeWindowLockArt.ShackleLeg(), new Vector2(8f, 6f) * unit, new Vector2(5f, 16f) * unit);
            shackleTop = NewImage("ShackleTop", lockRoot, UpgradeWindowLockArt.ShackleTop(), new Vector2(0f, 14f) * unit, new Vector2(21f, 5f) * unit);
            body = NewImage("LockBody", lockRoot, UpgradeWindowLockArt.Body(), new Vector2(0f, -6f) * unit, new Vector2(26f, 18f) * unit);
            legRightBase = legRight.rectTransform.anchoredPosition;
            shackleTopBase = shackleTop.rectTransform.anchoredPosition;

            crackTexture = UpgradeWindowLockArt.NewCrackTexture();
            crackBuffer = new Color32[UpgradeWindowLockArt.CrackWidth * UpgradeWindowLockArt.CrackHeight];
            var crackSprite = Sprite.Create(crackTexture,
                new Rect(0f, 0f, UpgradeWindowLockArt.CrackWidth, UpgradeWindowLockArt.CrackHeight),
                new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            crackSprite.hideFlags = HideFlags.HideAndDontSave;
            crack = NewImage("Crack", body.rectTransform, crackSprite, Vector2.zero, Vector2.zero);
            crack.rectTransform.anchorMin = Vector2.zero;
            crack.rectTransform.anchorMax = Vector2.one;
            crackCount = -1;

            shards = new UnityEngine.UI.Image[UpgradeWindowMotionTimeline.ShardCount];
            for (var i = 0; i < shards.Length; i++)
            {
                shards[i] = NewImage("Shard" + i, fxRoot, UpgradeWindowLockArt.Shard(i), Vector2.zero, new Vector2(11f, 11f) * unit);
                shards[i].gameObject.SetActive(false);
            }

            root.SetActive(false);
        }

        private UnityEngine.UI.Image NewImage(string name, RectTransform parent, Sprite sprite, Vector2 position, Vector2 size)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Image));
            obj.layer = gameObject.layer;
            obj.transform.SetParent(parent, false);
            var image = obj.GetComponent<UnityEngine.UI.Image>();
            image.sprite = sprite;
            image.raycastTarget = false;
            var rect = image.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return image;
        }

        private static void Raise(Action action)
        {
            if (action == null) return;
            try
            {
                action.Invoke();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using SubTerra.App.Audio;
using SubTerra.App.Core.Data;
using SubTerra.App.Progression;
using SubTerra.Shared.Audio;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SubTerra.App.UI.Progression
{
    public enum UpgradeNodeVisualState
    {
        Locked = 0,
        Ready = 1,
        Insufficient = 2,
        Maximum = 3
    }

    /// <summary>
    /// 업그레이드 트리 노드 하나. 업그레이드 하나당 노드 하나이며 레벨마다 노드를 만들지 않는다.
    /// 상태 표시만 담당하고 비용·레벨은 ProgressionService 결과를 그대로 그린다.
    /// 아이콘·이름·블라인드·자물쇠·발광·파편은 독립 Image/TMP이며 모두 raycastTarget=false다.
    /// </summary>
    public sealed class UpgradeTreeNodeView : MonoBehaviour, IPointerClickHandler
    {
        private const float ShakeDuration = 0.3f;
        private const float ShakeAmplitude = 9f;
        private const float LevelUpDuration = 0.65f;

        // B-169: 심층 썸네일 해금과 같은 1초 시간표. 노드 자물쇠는 썸네일 것의 1/1.3 크기라 떨어지는 거리만 비율로 줄인다.
        public const float UnlockDuration = 1f;
        private const float ThumbnailLockScale = 1.3f;
        private const float UnlockShackleKick = 6f;
        private const float UnlockDrop = 40f / ThumbnailLockScale;
        private const float UnlockDrift = DeepZoneThumbnailStrip.LockDrift / ThumbnailLockScale;
        private const float UnlockDropAngle = 25f;

        [SerializeField] private string upgradeId;
        [SerializeField] private Button button;
        [SerializeField] private RectTransform body;
        [SerializeField] private Image glow;
        [SerializeField] private Image outline;
        [SerializeField] private Image plate;
        [SerializeField] private Image selection;
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text nameLabel;
        [SerializeField] private TMP_Text levelLabel;
        [SerializeField] private Image readyMark;
        [SerializeField] private Image[] edgeBars = Array.Empty<Image>();
        [SerializeField] private CanvasGroup blind;
        [SerializeField] private TMP_Text questionLabel;
        [SerializeField] private RectTransform lockRoot;
        [SerializeField] private Image[] lockParts = Array.Empty<Image>();
        [SerializeField] private Image[] fragments = Array.Empty<Image>();

        private UpgradeSnapshot snapshot;
        private bool hasSnapshot;
        private bool selected;
        // B-169: 해금 연출을 기다리는 동안 풀린 정보 위에 블라인드·자물쇠를 그대로 둔다. 창이 꺼져도 유지한다.
        private bool revealPending;
        private UpgradeNodeVisualState visualState = UpgradeNodeVisualState.Locked;
        private Coroutine shakeRoutine;
        private Coroutine levelUpRoutine;
        private bool unlockPlaying;
        private float unlockElapsed;
        private float unlockU;
        private Action<SfxId> unlockCue;
        private readonly List<SfxId> cueBuffer = new List<SfxId>();
        private bool lockBaseCaptured;
        private Vector2 lockBase;
        private Vector2 shackleTopBase;
        private Vector2 shackleRightBase;

        public string UpgradeId => upgradeId;
        public Sprite IconSprite => icon != null ? icon.sprite : null;
        public UpgradeNodeVisualState VisualState => visualState;
        public bool IsRevealPending => revealPending;
        public bool IsBlindVisible => blind != null && blind.alpha > 0.01f;
        public float BlindAlpha => blind != null ? blind.alpha : 0f;
        public bool IsLockVisible => lockRoot != null && lockRoot.gameObject.activeSelf;
        public bool IsUnlockPlaying => unlockPlaying;
        public bool IsAnimating => shakeRoutine != null || levelUpRoutine != null || unlockPlaying;

        /// <summary>확인용: true면 Update가 해금 연출을 진행하지 않고 테스트가 StepUnlock으로 직접 진행한다.</summary>
        public bool ManualTick { get; set; }

        public event Action<string> Clicked;

        public void Configure(string permanentId)
        {
            upgradeId = permanentId ?? string.Empty;
        }

        private void Awake()
        {
            if (button != null)
            {
                var nav = button.navigation;
                nav.mode = Navigation.Mode.None;
                button.navigation = nav;
                button.transition = Selectable.Transition.None;
            }
        }

        private void OnEnable()
        {
            StopRoutines();
            ResetVisuals();
            if (hasSnapshot)
            {
                Apply(snapshot);
            }
        }

        private void OnDisable()
        {
            // 연출 도중 닫아도 확정된 구매 결과는 State에 있으므로 시각 상태만 정리한다.
            // 해금 연출 중이었으면 끝 상태(풀림)로, 아직 시작 안 한 대기는 다음에 열 때 재생한다.
            StopRoutines();
            ResetVisuals();
        }

        private void Update()
        {
            if (unlockPlaying && !ManualTick)
            {
                StepUnlock(Time.unscaledDeltaTime);
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData != null && eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            Clicked?.Invoke(upgradeId);
        }

        public void SetSelected(bool value)
        {
            selected = value;
            if (selection != null)
            {
                selection.enabled = value;
            }
        }

        public void Apply(UpgradeSnapshot value)
        {
            snapshot = value;
            hasSnapshot = true;

            var unlocked = value.IsUnlocked;
            if (!unlocked)
            {
                visualState = UpgradeNodeVisualState.Locked;
            }
            else if (value.IsMaximumLevel)
            {
                visualState = UpgradeNodeVisualState.Maximum;
            }
            else
            {
                visualState = value.CanAffordNextLevel
                    ? UpgradeNodeVisualState.Ready
                    : UpgradeNodeVisualState.Insufficient;
            }

            // 정보는 확정된 상태대로 채운다. B-169: 해금 연출 대기 중에는 그 위를 블라인드·자물쇠로 가린 채 둔다.
            var showContent = unlocked;
            if (icon != null)
            {
                icon.enabled = showContent;
            }

            if (nameLabel != null)
            {
                nameLabel.text = showContent
                    ? ItemDisplayNames.PreferDisplay(value.UpgradeId, value.DisplayName)
                    : string.Empty;
            }

            if (levelLabel != null)
            {
                levelLabel.text = !showContent
                    ? string.Empty
                    : value.IsMaximumLevel
                        ? "MAX"
                        : "Lv." + value.CurrentLevel + "/" + value.MaximumLevel;
            }

            if (unlockPlaying)
            {
                // 연출 중 다시 그려도 지금 장면을 유지한다(블라인드·자물쇠는 StepUnlock이 그린다).
                return;
            }

            ApplyStateColors();
            SetBlindVisible(!unlocked || revealPending);
        }

        /// <summary>B-169: UpgradeTreeView가 해금 대기 여부를 알려 준다. 대기면 풀린 뒤에도 잠긴 모습으로 둔다.</summary>
        public void SetRevealPending(bool pending)
        {
            if (revealPending == pending || unlockPlaying)
            {
                return;
            }

            revealPending = pending;
            if (hasSnapshot)
            {
                ApplyStateColors();
                SetBlindVisible(!snapshot.IsUnlocked || revealPending);
            }
        }

        // 대기 중에는 테두리·준비 표시도 잠김 색으로 둬 풀린 상태가 미리 드러나지 않게 한다.
        private UpgradeNodeVisualState DisplayState => revealPending ? UpgradeNodeVisualState.Locked : visualState;

        private void ApplyStateColors()
        {
            var outlineColor = UpgradeTreeTween.CyanDim;
            var outlineAlpha = 0.9f;
            var levelColor = UpgradeTreeTween.TextDim;
            var iconAlpha = 1f;
            var shown = DisplayState;
            switch (shown)
            {
                case UpgradeNodeVisualState.Ready:
                    outlineColor = UpgradeTreeTween.Cyan;
                    outlineAlpha = 0.85f;
                    levelColor = UpgradeTreeTween.Cyan;
                    break;
                case UpgradeNodeVisualState.Insufficient:
                    outlineColor = new Color(0.2f, 0.5f, 0.56f, 1f);
                    outlineAlpha = 0.75f;
                    levelColor = UpgradeTreeTween.TextMain;
                    iconAlpha = 0.78f;
                    break;
                case UpgradeNodeVisualState.Maximum:
                    outlineColor = UpgradeTreeTween.Gold;
                    outlineAlpha = 0.9f;
                    levelColor = UpgradeTreeTween.Gold;
                    break;
                default:
                    outlineAlpha = 0.55f;
                    break;
            }

            UpgradeTreeTween.SetColor(outline, outlineColor, outlineAlpha);
            if (levelLabel != null)
            {
                levelLabel.color = levelColor;
            }

            if (icon != null)
            {
                UpgradeTreeTween.SetAlpha(icon, iconAlpha);
            }

            if (readyMark != null)
            {
                readyMark.enabled = shown == UpgradeNodeVisualState.Ready;
            }

            if (selection != null)
            {
                selection.enabled = selected;
            }
        }

        private void SetBlindVisible(bool visible)
        {
            if (blind != null)
            {
                blind.alpha = visible ? 1f : 0f;
            }

            if (lockRoot != null)
            {
                lockRoot.gameObject.SetActive(visible);
            }

            for (var i = 0; i < fragments.Length; i++)
            {
                if (fragments[i] != null)
                {
                    fragments[i].gameObject.SetActive(false);
                }
            }
        }

        /// <summary>정지·복원: 위치, 크기, 색, 발광, 파편을 기본값으로 되돌린다.</summary>
        public void ResetVisuals()
        {
            if (body != null)
            {
                body.anchoredPosition = Vector2.zero;
                body.localScale = Vector3.one;
                body.localRotation = Quaternion.identity;
            }

            UpgradeTreeTween.SetAlpha(glow, 0f);
            for (var i = 0; i < edgeBars.Length; i++)
            {
                UpgradeTreeTween.SetAlpha(edgeBars[i], 0f);
            }

            if (lockRoot != null)
            {
                if (lockBaseCaptured)
                {
                    ApplyLockPose(new UnlockLockPose(Vector2.zero, 0f, 1f, 0f, 1f, true));
                }
                else
                {
                    lockRoot.localRotation = Quaternion.identity;
                    lockRoot.anchoredPosition = new Vector2(0f, lockRoot.anchoredPosition.y);
                }
            }

            for (var i = 0; i < lockParts.Length; i++)
            {
                if (lockParts[i] != null)
                {
                    lockParts[i].enabled = true;
                }
            }

            for (var i = 0; i < fragments.Length; i++)
            {
                if (fragments[i] != null)
                {
                    fragments[i].gameObject.SetActive(false);
                }
            }

            if (questionLabel != null)
            {
                UpgradeTreeTween.SetAlpha(questionLabel, 1f);
            }

            if (levelLabel != null)
            {
                levelLabel.rectTransform.localScale = Vector3.one;
            }

            if (hasSnapshot)
            {
                ApplyStateColors();
                SetBlindVisible(!snapshot.IsUnlocked || revealPending);
            }
        }

        private void StopRoutines()
        {
            shakeRoutine = null;
            levelUpRoutine = null;
            if (unlockPlaying)
            {
                // 시작한 해금 연출은 끝 상태(풀림)로 맞춘다.
                unlockPlaying = false;
                revealPending = false;
                unlockCue = null;
            }

            StopAllCoroutines();
        }

        // ---- 자금 부족: 좌우 흔들림 + 경고색 ----
        public void PlayShake()
        {
            if (!isActiveAndEnabled || body == null)
            {
                return;
            }

            if (shakeRoutine != null)
            {
                StopCoroutine(shakeRoutine);
            }

            shakeRoutine = StartCoroutine(ShakeRoutine());
        }

        private IEnumerator ShakeRoutine()
        {
            yield return UpgradeTreeTween.Run(ShakeDuration, t =>
            {
                var damping = 1f - t;
                body.anchoredPosition = new Vector2(
                    Mathf.Sin(t * Mathf.PI * 7f) * ShakeAmplitude * damping,
                    0f);
                // 경고색은 즉시 최대, 이후 원래 색으로 복귀한다.
                var warn = 1f - t;
                if (outline != null)
                {
                    outline.color = Color.Lerp(
                        ColorForState(),
                        new Color(UpgradeTreeTween.Warning.r, UpgradeTreeTween.Warning.g, UpgradeTreeTween.Warning.b, 1f),
                        warn);
                }
            });
            body.anchoredPosition = Vector2.zero;
            ApplyStateColors();
            shakeRoutine = null;
        }

        private Color ColorForState()
        {
            switch (visualState)
            {
                case UpgradeNodeVisualState.Ready:
                    return UpgradeTreeTween.Cyan;
                case UpgradeNodeVisualState.Maximum:
                    return UpgradeTreeTween.Gold;
                case UpgradeNodeVisualState.Insufficient:
                    return new Color(0.2f, 0.5f, 0.56f, 1f);
                default:
                    return UpgradeTreeTween.CyanDim;
            }
        }

        // ---- 레벨업 성공: 테두리 순환 + 발광 + 확대 후 복귀 ----
        public void PlayLevelUp()
        {
            if (!isActiveAndEnabled || body == null)
            {
                return;
            }

            if (levelUpRoutine != null)
            {
                StopCoroutine(levelUpRoutine);
            }

            levelUpRoutine = StartCoroutine(LevelUpRoutine());
        }

        private IEnumerator LevelUpRoutine()
        {
            yield return UpgradeTreeTween.Run(LevelUpDuration, t =>
            {
                var scale = 1f + 0.08f * UpgradeTreeTween.Pulse(Mathf.Min(1f, t * 1.4f));
                body.localScale = new Vector3(scale, scale, 1f);
                UpgradeTreeTween.SetColor(glow, UpgradeTreeTween.Cyan, 0.85f * UpgradeTreeTween.Pulse(t));

                // 청록빛 헤드가 테두리(상→우→하→좌)를 한 바퀴 돈다.
                var head = Mathf.Clamp01(t / 0.85f) * edgeBars.Length;
                var fade = 1f - Mathf.Clamp01((t - 0.85f) / 0.15f);
                for (var i = 0; i < edgeBars.Length; i++)
                {
                    var distance = Mathf.Abs(Mathf.Repeat(head - (i + 0.5f) + edgeBars.Length * 0.5f, edgeBars.Length) - edgeBars.Length * 0.5f);
                    UpgradeTreeTween.SetColor(
                        edgeBars[i],
                        UpgradeTreeTween.Cyan,
                        Mathf.Clamp01(1.5f - distance) * fade);
                }

                if (levelLabel != null)
                {
                    var pop = 1f + 0.25f * UpgradeTreeTween.Pulse(Mathf.Clamp01((t - 0.1f) / 0.4f));
                    levelLabel.rectTransform.localScale = new Vector3(pop, pop, 1f);
                }
            });
            body.localScale = Vector3.one;
            UpgradeTreeTween.SetAlpha(glow, 0f);
            for (var i = 0; i < edgeBars.Length; i++)
            {
                UpgradeTreeTween.SetAlpha(edgeBars[i], 0f);
            }

            if (levelLabel != null)
            {
                levelLabel.rectTransform.localScale = Vector3.one;
            }

            levelUpRoutine = null;
        }

        // ---- B-169 신규 해금: 흔들림 → 딸깍 → 떨어지며 기울어 사라짐 → 블라인드가 걷히며 청록 발광 ----

        /// <summary>
        /// 해금 연출을 처음부터 시작한다. cue가 있으면 0/0.2/0.65 시점을 넘는 프레임에 소리를 넘긴다(없으면 연출만).
        /// 창이 꺼져 있으면 끝 상태(풀림)로 바로 맞춘다.
        /// </summary>
        public void BeginUnlock(Action<SfxId> cue)
        {
            if (!isActiveAndEnabled || body == null)
            {
                revealPending = false;
                if (hasSnapshot)
                {
                    Apply(snapshot);
                }

                return;
            }

            CaptureLockBase();
            unlockPlaying = true;
            unlockElapsed = 0f;
            unlockU = UnlockRevealTimeline.BeforeStart;
            unlockCue = cue;
            if (lockRoot != null)
            {
                lockRoot.gameObject.SetActive(true);
            }

            for (var i = 0; i < fragments.Length; i++)
            {
                if (fragments[i] != null)
                {
                    fragments[i].gameObject.SetActive(false);
                }
            }

            SampleUnlock(0f);
        }

        /// <summary>해금 연출을 dt만큼 진행한다. Update와 테스트가 같은 경로를 쓴다.</summary>
        public void StepUnlock(float deltaTime)
        {
            if (!unlockPlaying)
            {
                return;
            }

            unlockElapsed += Mathf.Max(0f, deltaTime);
            SampleUnlock(Mathf.Clamp01(unlockElapsed / UnlockDuration));
        }

        private void SampleUnlock(float u)
        {
            cueBuffer.Clear();
            UnlockRevealTimeline.CollectCues(unlockU, u, cueBuffer);
            var crossedGlow = SfxPlayback.Crossed(unlockU, u, UnlockRevealTimeline.GlowAt);
            unlockU = u;

            var pose = UnlockRevealTimeline.SampleLock(u, UnlockShackleKick, UnlockDrop, UnlockDropAngle, UnlockDrift);
            if (lockRoot != null)
            {
                if (pose.Visible)
                {
                    ApplyLockPose(pose);
                }

                lockRoot.gameObject.SetActive(pose.Visible);
            }

            if (crossedGlow)
            {
                // 자물쇠가 사라지는 순간부터 풀린 색으로 그린다.
                revealPending = false;
                ApplyStateColors();
            }

            var reveal = u < UnlockRevealTimeline.FallEnd
                ? 0f
                : (u - UnlockRevealTimeline.FallEnd) / (1f - UnlockRevealTimeline.FallEnd);
            if (blind != null)
            {
                blind.alpha = 1f - UpgradeTreeTween.EaseOut(reveal);
            }

            UpgradeTreeTween.SetColor(glow, UpgradeTreeTween.Cyan, reveal > 0f ? 0.9f * UpgradeTreeTween.Pulse(reveal) : 0f);

            for (var i = 0; i < cueBuffer.Count; i++)
            {
                Raise(cueBuffer[i]);
            }

            if (u >= 1f)
            {
                FinishUnlock();
            }
        }

        private void FinishUnlock()
        {
            unlockPlaying = false;
            unlockCue = null;
            revealPending = false;
            UpgradeTreeTween.SetAlpha(glow, 0f);
            if (lockBaseCaptured)
            {
                ApplyLockPose(new UnlockLockPose(Vector2.zero, 0f, 1f, 0f, 1f, true));
            }

            if (hasSnapshot)
            {
                Apply(snapshot);
            }
        }

        private void Raise(SfxId id)
        {
            if (unlockCue == null)
            {
                return;
            }

            try
            {
                unlockCue(id);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        private void CaptureLockBase()
        {
            if (lockBaseCaptured || lockRoot == null)
            {
                return;
            }

            lockBase = new Vector2(0f, lockRoot.anchoredPosition.y);
            var top = FindLockPart("ShackleTop");
            var right = FindLockPart("ShackleRight");
            shackleTopBase = top != null ? top.rectTransform.anchoredPosition : Vector2.zero;
            shackleRightBase = right != null ? right.rectTransform.anchoredPosition : Vector2.zero;
            lockBaseCaptured = true;
        }

        private void ApplyLockPose(UnlockLockPose pose)
        {
            if (lockRoot == null)
            {
                return;
            }

            lockRoot.anchoredPosition = lockBase + pose.Offset;
            lockRoot.localRotation = Quaternion.Euler(0f, 0f, pose.Angle);
            lockRoot.localScale = new Vector3(pose.Scale, pose.Scale, 1f);
            var top = FindLockPart("ShackleTop");
            if (top != null)
            {
                top.rectTransform.anchoredPosition = shackleTopBase + new Vector2(0f, pose.ShackleLift);
            }

            var right = FindLockPart("ShackleRight");
            if (right != null)
            {
                right.rectTransform.anchoredPosition = shackleRightBase + new Vector2(0f, pose.ShackleLift);
            }

            for (var i = 0; i < lockParts.Length; i++)
            {
                UpgradeTreeTween.SetAlpha(lockParts[i], pose.Alpha);
            }
        }

        private Image FindLockPart(string partName)
        {
            for (var i = 0; i < lockParts.Length; i++)
            {
                if (lockParts[i] != null && lockParts[i].name == partName)
                {
                    return lockParts[i];
                }
            }

            return null;
        }
    }
}

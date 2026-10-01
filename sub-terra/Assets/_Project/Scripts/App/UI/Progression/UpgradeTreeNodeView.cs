using System;
using System.Collections;
using SubTerra.App.Core.Data;
using SubTerra.App.Progression;
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
        private const float UnlockNodeDuration = 0.7f;

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
        private bool revealPending;
        private UpgradeNodeVisualState visualState = UpgradeNodeVisualState.Locked;
        private Coroutine shakeRoutine;
        private Coroutine levelUpRoutine;
        private Coroutine unlockRoutine;

        public string UpgradeId => upgradeId;
        public Sprite IconSprite => icon != null ? icon.sprite : null;
        public UpgradeNodeVisualState VisualState => visualState;
        public bool IsRevealPending => revealPending;
        public bool IsBlindVisible => blind != null && blind.alpha > 0.01f;
        public bool IsAnimating => shakeRoutine != null || levelUpRoutine != null || unlockRoutine != null;

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
            revealPending = false;
            if (hasSnapshot)
            {
                Apply(snapshot);
            }
        }

        private void OnDisable()
        {
            // 연출 도중 닫아도 확정된 구매 결과는 State에 있으므로 시각 상태만 정리한다.
            StopRoutines();
            ResetVisuals();
            revealPending = false;
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

        /// <summary>신규 해금 연출이 끝날 때까지 블라인드를 유지하도록 표시한다.</summary>
        public void HoldBlindForReveal()
        {
            if (isActiveAndEnabled)
            {
                revealPending = true;
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

            // 미해금 노드는 이름·아이콘·레벨을 비워 숨긴다. 연출 대기 중인 노드는 블라인드가 가린다.
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

            ApplyStateColors();
            if (!IsRunning(unlockRoutine))
            {
                SetBlindVisible(!unlocked || revealPending);
            }
        }

        private void ApplyStateColors()
        {
            var outlineColor = UpgradeTreeTween.CyanDim;
            var outlineAlpha = 0.9f;
            var levelColor = UpgradeTreeTween.TextDim;
            var iconAlpha = 1f;
            switch (visualState)
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
                readyMark.enabled = visualState == UpgradeNodeVisualState.Ready;
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
                lockRoot.localRotation = Quaternion.identity;
                lockRoot.anchoredPosition = new Vector2(0f, lockRoot.anchoredPosition.y);
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
                SetBlindVisible(!snapshot.IsUnlocked);
            }
        }

        private void StopRoutines()
        {
            shakeRoutine = null;
            levelUpRoutine = null;
            unlockRoutine = null;
            StopAllCoroutines();
        }

        private static bool IsRunning(Coroutine routine)
        {
            return routine != null;
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

        // ---- 신규 해금: 자물쇠 진동 → 파손 → 블라인드 제거 → 청록 점등 ----
        public void PlayUnlock(float delay)
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

            if (unlockRoutine != null)
            {
                StopCoroutine(unlockRoutine);
            }

            revealPending = true;
            SetBlindVisible(true);
            unlockRoutine = StartCoroutine(UnlockRoutine(delay));
        }

        private IEnumerator UnlockRoutine(float delay)
        {
            if (delay > 0f)
            {
                yield return UpgradeTreeTween.WaitUnscaled(delay);
            }

            // 1) 자물쇠 진동 (0.2s)
            yield return UpgradeTreeTween.Run(0.2f, t =>
            {
                if (lockRoot != null)
                {
                    lockRoot.localRotation = Quaternion.Euler(
                        0f,
                        0f,
                        Mathf.Sin(t * Mathf.PI * 8f) * 12f * (0.5f + t * 0.5f));
                }
            });

            // 2) 파손: 자물쇠 숨기고 파편 소수를 흩뿌린다 (0.2s)
            if (lockRoot != null)
            {
                lockRoot.localRotation = Quaternion.identity;
            }

            for (var i = 0; i < lockParts.Length; i++)
            {
                if (lockParts[i] != null)
                {
                    lockParts[i].enabled = false;
                }
            }

            var origin = lockRoot != null ? lockRoot.anchoredPosition : Vector2.zero;
            for (var i = 0; i < fragments.Length; i++)
            {
                if (fragments[i] != null)
                {
                    fragments[i].gameObject.SetActive(true);
                    UpgradeTreeTween.SetAlpha(fragments[i], 1f);
                    fragments[i].rectTransform.anchoredPosition = origin;
                }
            }

            yield return UpgradeTreeTween.Run(0.2f, t =>
            {
                for (var i = 0; i < fragments.Length; i++)
                {
                    if (fragments[i] == null)
                    {
                        continue;
                    }

                    var angle = (30f + 360f / Mathf.Max(1, fragments.Length) * i) * Mathf.Deg2Rad;
                    var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle) - 0.35f * t);
                    fragments[i].rectTransform.anchoredPosition = origin + direction * (46f * UpgradeTreeTween.EaseOut(t));
                    UpgradeTreeTween.SetAlpha(fragments[i], 1f - t);
                }
            });

            for (var i = 0; i < fragments.Length; i++)
            {
                if (fragments[i] != null)
                {
                    fragments[i].gameObject.SetActive(false);
                }
            }

            // 3) 블라인드 걷힘 (0.15s) + 4) 청록 점등 (0.3s)
            revealPending = false;
            if (hasSnapshot)
            {
                Apply(snapshot);
            }

            if (lockRoot != null)
            {
                lockRoot.gameObject.SetActive(false);
            }

            yield return UpgradeTreeTween.Run(UnlockNodeDuration - 0.4f, t =>
            {
                if (blind != null)
                {
                    blind.alpha = 1f - UpgradeTreeTween.EaseOut(Mathf.Clamp01(t / 0.5f));
                }

                UpgradeTreeTween.SetColor(glow, UpgradeTreeTween.Cyan, 0.9f * UpgradeTreeTween.Pulse(Mathf.Clamp01((t - 0.2f) / 0.8f)));
            });

            SetBlindVisible(false);
            UpgradeTreeTween.SetAlpha(glow, 0f);
            for (var i = 0; i < lockParts.Length; i++)
            {
                if (lockParts[i] != null)
                {
                    lockParts[i].enabled = true;
                }
            }

            unlockRoutine = null;
        }
    }
}

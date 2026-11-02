using System.Collections;
using SubTerra.App.Audio;
using SubTerra.App.UI;
using SubTerra.Shared.Audio;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.UI.Tutorial
{
    public sealed class QuestClearPopupMotion : MonoBehaviour
    {
        [SerializeField] private RectTransform checkSign;
        [SerializeField] private RectTransform borderGlint;
        [SerializeField] private Image borderGlintImage;

        private RectTransform panel;
        private StartBriefingPopupMotion transition;
        private Coroutine motion;
        private bool closing;
        public bool IsClosing => closing;

        /// <summary>B-180: 프리팹 빌더(B-95-1)가 만든 닫기 버튼 이름.</summary>
        public const string CloseButtonName = "ClaimCloseButton";

        // B-180-1: 완료음은 체크 아이콘이 커지기 시작할 때, 닫을 때는 시작 브리핑 종료와 같은 글리치음.
        public const SfxId CheckAppearSfx = SfxId.QuestReward;
        public const SfxId CloseSfx = SfxId.BriefingClose;

        /// <summary>이번 열기에서 완료음을 냈는지(확인용). 다시 열면 지운다.</summary>
        public bool CheckSfxPlayed { get; private set; }

        /// <summary>B-180: 등장 연출이 끝나 표시 중이고 닫는 중이 아닐 때만 닫기 호버음을 낸다.</summary>
        public bool CanPlayCloseHover => !closing && transition != null && transition.IsShown;

        private void Awake()
        {
            panel = (RectTransform)transform;
            transition = gameObject.AddComponent<StartBriefingPopupMotion>();
            transition.enabled = false;
            transition.ConfigureQuestClear(borderGlint);
            transition.SetCompactCloseSfx(CloseSfx);
            AttachCloseHoverSfx();
        }

        // B-180: 닫힘음·보상음은 B-178 경로가 내므로 클릭은 무음, 호버만 붙인다.
        // ConfigureQuestClear가 본문을 ClaimContent 아래로 옮기므로 하위 전체에서 이름으로 찾는다.
        private void AttachCloseHoverSfx()
        {
            Button button = null;
            foreach (var candidate in GetComponentsInChildren<Button>(true))
            {
                if (candidate.name == CloseButtonName)
                {
                    button = candidate;
                    break;
                }
            }

            if (button == null)
            {
                Debug.LogWarning("[QuestClearPopupMotion] 닫기 버튼을 찾지 못해 호버음을 붙이지 않았습니다.");
                return;
            }

            UiSfx.Attach(button, SfxId.None, hover: true, failWhenDisabled: false, hoverGate: () => CanPlayCloseHover);
        }

        private void OnEnable()
        {
            Open();
        }

        public void Open()
        {
            if (panel == null)
            {
                panel = (RectTransform)transform;
            }

            if (motion != null) StopCoroutine(motion);
            // B-178: 닫히던 창을 다시 열면(연달아 퀘스트 클리어) 닫힘음·보상음과 같은 프레임이라 열림음을 늦춘다.
            var reopening = closing;
            closing = false;
            CheckSfxPlayed = false;
            panel.anchoredPosition = Vector2.zero;
            if (checkSign != null) checkSign.localScale = Vector3.zero;
            if (borderGlint != null) borderGlint.gameObject.SetActive(false);
            transition.enabled = false;
            transition.enabled = true;
            if (reopening) transition.DelayNextOpenSfx(StartBriefingPopupMotion.ReopenOpenSfxDelay);
            motion = StartCoroutine(Enter());
        }

        private void OnDisable()
        {
            if (motion != null) StopCoroutine(motion);
            motion = null;
            PopupWindowSorting.Remove(GetComponent<Canvas>());
        }

        public void Close()
        {
            if (closing) return;
            closing = true;
            if (motion != null) StopCoroutine(motion);
            if (checkSign != null) checkSign.localScale = Vector3.one;
            if (borderGlint != null) borderGlint.gameObject.SetActive(false);
            panel.anchoredPosition = Vector2.zero;
            transition.RequestQuestClose();
            motion = null;
        }

        private IEnumerator Enter()
        {
            while (!transition.IsShown) yield return null;
            PlayCheckSfx();
            if (checkSign != null)
            {
                yield return ScaleCheck(0f, 1.1f, 0.2f);
                yield return ScaleCheck(1.1f, 1f, 0.1f);
            }

            if (borderGlint != null)
            {
                borderGlint.gameObject.SetActive(true);
                var elapsed = 0f;
                while (elapsed < 0.3f)
                {
                    elapsed += Time.unscaledDeltaTime;
                    PlaceGlint(Mathf.Clamp01(elapsed / 0.3f));
                    yield return null;
                }

                borderGlint.gameObject.SetActive(false);
            }

            motion = null;
        }

        // 등장 코루틴이 체크를 키우기 직전에 한 번. 닫는 중이거나 꺼졌으면(코루틴이 멈춰 여기 오지 않음) 무음.
        private void PlayCheckSfx()
        {
            if (closing || CheckSfxPlayed)
            {
                return;
            }

            CheckSfxPlayed = true;
            SfxPlayer.Play(CheckAppearSfx);
        }

        private IEnumerator ScaleCheck(float from, float to, float duration)
        {
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                checkSign.localScale = Vector3.one * Mathf.Lerp(from, to, t);
                yield return null;
            }

            checkSign.localScale = Vector3.one * to;
        }

        private void PlaceGlint(float progress)
        {
            // The frame art occupies the central outline of the 900 x 450 sprite.
            var perimeter = 2f * (790f + 310f);
            var distance = progress * perimeter;
            Vector2 position;
            float rotation;
            if (distance < 790f)
            {
                position = new Vector2(-395f + distance, 155f);
                rotation = 0f;
            }
            else if (distance < 1100f)
            {
                position = new Vector2(395f, 155f - (distance - 790f));
                rotation = 90f;
            }
            else if (distance < 1890f)
            {
                position = new Vector2(395f - (distance - 1100f), -155f);
                rotation = 0f;
            }
            else
            {
                position = new Vector2(-395f, -155f + (distance - 1890f));
                rotation = 90f;
            }

            borderGlint.anchoredPosition = new Vector2(
                position.x * panel.rect.width / 900f,
                position.y * panel.rect.height / 450f);
            borderGlint.localRotation = Quaternion.Euler(0f, 0f, rotation);
            if (borderGlintImage != null) borderGlintImage.color = new Color(0.18f, 1f, 0.96f, 0.95f);
        }
    }

}

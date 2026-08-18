using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.UI.HUD
{
    /// <summary>
    /// prompt-B 31번: 탭 기반 게임 가이드 패널.
    /// 대분류 탭 + 스크롤 본문. 폰트 16px(NotoSansKR) 기준.
    /// </summary>
    public sealed class GameGuidePanelView : MonoBehaviour
    {
        public const int TabCount = 3;
        public const float GuideFontSize = 16f;

        public enum GuideTab
        {
            Controls = 0,
            Mechanics = 1,
            Resources = 2
        }

        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button[] tabButtons = new Button[TabCount];
        [SerializeField] private TMP_Text[] tabLabels = new TMP_Text[TabCount];
        [SerializeField] private TMP_Text bodyText;
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private RectTransform contentRoot;
        [SerializeField] private GuideTab activeTab = GuideTab.Controls;

        private static readonly Color TabActive = new Color(0.22f, 0.42f, 0.55f, 1f);
        private static readonly Color TabInactive = new Color(0.12f, 0.18f, 0.24f, 0.95f);
        private static readonly Color TabLabelActive = new Color(0.95f, 0.98f, 1f, 1f);
        private static readonly Color TabLabelInactive = new Color(0.72f, 0.78f, 0.84f, 1f);

        public Button CloseButton => closeButton;
        public GuideTab ActiveTab => activeTab;
        public bool IsOpen => (panelRoot != null ? panelRoot : gameObject).activeSelf;

        private void Awake()
        {
            WireTabButtons();
            ApplyTab(activeTab, force: true);
        }

        private void OnEnable()
        {
            WireTabButtons();
            ApplyTab(activeTab, force: true);
        }

        private void OnDisable()
        {
            UnwireTabButtons();
        }

        public void SetVisible(bool visible)
        {
            // root가 비활성이면 자식 PanelRoot만 켜도 화면에 안 나온다.
            // 열 때 root를 켜고 맨 앞으로, 닫을 때 root 전체를 끈다.
            if (visible)
            {
                gameObject.SetActive(true);
                transform.SetAsLastSibling();
            }

            if (panelRoot != null)
            {
                panelRoot.SetActive(visible);
            }

            if (!visible)
            {
                gameObject.SetActive(false);
            }

            if (visible)
            {
                ApplyTab(activeTab, force: true);
            }
        }

        public void SelectTab(GuideTab tab)
        {
            ApplyTab(tab, force: false);
        }

        public void SelectTabIndex(int index)
        {
            if (index < 0 || index >= TabCount)
            {
                return;
            }

            SelectTab((GuideTab)index);
        }

        public bool HasRequiredReferences()
        {
            if (closeButton == null || bodyText == null)
            {
                return false;
            }

            if (tabButtons == null || tabButtons.Length < TabCount)
            {
                return false;
            }

            for (var i = 0; i < TabCount; i++)
            {
                if (tabButtons[i] == null)
                {
                    return false;
                }
            }

            return true;
        }

        public static string GetTabTitle(GuideTab tab)
        {
            switch (tab)
            {
                case GuideTab.Controls:
                    return "기본 조작";
                case GuideTab.Mechanics:
                    return "핵심 메커니즘";
                case GuideTab.Resources:
                    return "자원·오브젝트";
                default:
                    return "가이드";
            }
        }

        public static string GetTabBody(GuideTab tab)
        {
            switch (tab)
            {
                case GuideTab.Controls:
                    return ControlsBody;
                case GuideTab.Mechanics:
                    return MechanicsBody;
                case GuideTab.Resources:
                    return ResourcesBody;
                default:
                    return string.Empty;
            }
        }

        private void WireTabButtons()
        {
            if (tabButtons == null)
            {
                return;
            }

            for (var i = 0; i < tabButtons.Length && i < TabCount; i++)
            {
                var button = tabButtons[i];
                if (button == null)
                {
                    continue;
                }

                var index = i;
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => SelectTabIndex(index));
            }
        }

        private void UnwireTabButtons()
        {
            if (tabButtons == null)
            {
                return;
            }

            for (var i = 0; i < tabButtons.Length; i++)
            {
                if (tabButtons[i] != null)
                {
                    tabButtons[i].onClick.RemoveAllListeners();
                }
            }
        }

        private void ApplyTab(GuideTab tab, bool force)
        {
            if (!force && activeTab == tab && bodyText != null
                && bodyText.text == GetTabBody(tab))
            {
                return;
            }

            activeTab = tab;

            if (bodyText != null)
            {
                bodyText.fontSize = GuideFontSize;
                bodyText.text = GetTabBody(tab);
                bodyText.textWrappingMode = TextWrappingModes.Normal;
                bodyText.overflowMode = TextOverflowModes.Overflow;
            }

            RefreshTabVisuals();
            RefreshContentSize();

            if (scrollRect != null)
            {
                scrollRect.verticalNormalizedPosition = 1f;
            }
        }

        private void RefreshTabVisuals()
        {
            for (var i = 0; i < TabCount; i++)
            {
                var active = (int)activeTab == i;
                if (tabButtons != null && i < tabButtons.Length && tabButtons[i] != null)
                {
                    var image = tabButtons[i].GetComponent<Image>();
                    if (image != null)
                    {
                        image.color = active ? TabActive : TabInactive;
                    }
                }

                if (tabLabels != null && i < tabLabels.Length && tabLabels[i] != null)
                {
                    tabLabels[i].color = active ? TabLabelActive : TabLabelInactive;
                    tabLabels[i].fontStyle = active ? FontStyles.Bold : FontStyles.Normal;
                }
            }
        }

        private void RefreshContentSize()
        {
            if (bodyText == null || contentRoot == null)
            {
                return;
            }

            bodyText.ForceMeshUpdate();
            var preferred = bodyText.GetPreferredValues(
                bodyText.text,
                bodyText.rectTransform.rect.width > 1f
                    ? bodyText.rectTransform.rect.width
                    : contentRoot.rect.width,
                0f);

            var height = Mathf.Max(preferred.y + 24f, 120f);
            var size = contentRoot.sizeDelta;
            size.y = height;
            contentRoot.sizeDelta = size;

            var bodyRect = bodyText.rectTransform;
            bodyRect.anchorMin = new Vector2(0f, 1f);
            bodyRect.anchorMax = new Vector2(1f, 1f);
            bodyRect.pivot = new Vector2(0.5f, 1f);
            bodyRect.anchoredPosition = new Vector2(0f, -8f);
            bodyRect.sizeDelta = new Vector2(0f, height);
            bodyRect.offsetMin = new Vector2(16f, bodyRect.offsetMin.y);
            bodyRect.offsetMax = new Vector2(-16f, bodyRect.offsetMax.y);
        }

        // --- 가이드 본문 (prompt-B 31) ---

        private const string ControlsBody =
            "1. 기본 게임 조작법 (Controls Guide)\n\n"
            + "캐릭터 이동 및 수직 이동\n"
            + "· 좌우 이동: A / D 키 또는 ← / → (방향키)\n"
            + "· 사다리 이동 / 수직 탐사: 사다리 위치에서 W / S 키 또는 ↑ / ↓ (방향키)\n"
            + "· 엘리베이터: E 키 (지상-지하 이동)\n"
            + "· 시설 상호작용: 충전기·보관함·정산 콘솔·전진기지 코어·긴급 탈출 포탈 근처에서 E 키\n"
            + "· 시설 구분: 가까이 가면 말풍선으로 시설명이 표시됩니다 (버팀목·사다리 제외)\n\n"
            + "지형 채굴 (Mining)\n"
            + "· 근접 타일 채굴: 채굴하고자 하는 블록(암반/광물)을 마우스 Left Click(왼쪽 클릭) 또는 Enter 키\n"
            + "· 팁: 캐릭터 근처의 이웃한 타일만 채굴 가능하며, 업그레이드에 따라 채굴 속도가 빨라집니다.\n\n"
            + "UI 및 건설/가이드 조작\n"
            + "· 시설 건설 메뉴: B 키 (건설 창 열기/닫기)\n"
            + "· 화물/인벤토리 확인: I 키\n"
            + "· Digger-Bot (드론) & 통합 가이드 창: Tab 키 또는 화면의 Digger-Bot 버튼 클릭\n"
            + "· 게임 가이드: 우측 '게임 가이드' 버튼";

        private const string MechanicsBody =
            "2. 핵심 게임 메커니즘 (Game Mechanics)\n\n"
            + "지하 탐사 및 자원 채굴 (Mining & Cargo)\n"
            + "· 표면 기지(Surface Base)에서 엘리베이터를 타고 지하 심층으로 내려가 암반을 캐며 길을 뚫고 광물을 수집합니다.\n"
            + "· 채굴한 자원은 플레이어의 화물 인벤토리에 저장되며, 화물 용량이 가득 차면 더 이상 채굴할 수 없으므로 기지로 귀환해야 합니다.\n\n"
            + "생존 요소 및 환경 위험 (Hazards & Survival)\n"
            + "· 독성 가스 (Gas Hazard): 지하 깊은 곳의 가스 포켓 근처에 닿으면 체력이 지속적으로 감소합니다. 가스 저항 장비로 대비하세요.\n"
            + "· 지반 붕괴 및 구조 위험 (Structural Integrity): 아래가 빈 천장만 위험합니다. 아래·옆을 더 파면 균열이 짙어지고, 빨간 칸 중 점멸하는 칸만 곧 떨어집니다. 먼저 피하고, 미리 준비한 버팀목을 점멸 반경에 설치하면 붕괴를 취소할 수 있습니다. 점멸하지 않는 빨간 칸은 아직 떨어지지 않습니다.\n"
            + "· 에너지 및 전력 (Power System): 지하 전력망 시설을 구축하여 조명을 밝히고 충전기를 통해 장비 에너지를 유지하세요.\n\n"
            + "귀환, 정산 및 장비 성과 (Return & Progression)\n"
            + "· 탐사 후 안전하게 표면 기지로 귀환하면 수집한 자원을 판매하여 크레딧을 얻거나 제작 재료로 사용합니다.\n"
            + "· 탐사 실패 (체력 고갈 / 붕괴 갇힘): 탐사에 실패하면 미정산 화물의 일부분(30~50%)을 손실하고 복구 지점에서 재시작합니다. (Digger-Bot 구조 업그레이드로 손실을 줄일 수 있습니다.)\n"
            + "· 기지 발전 & 깊이 확장: 크레딧과 광물로 드릴 성능, 화물 용량, 드론 스캔, 가스 저항을 업그레이드하여 더 깊은 심층 구역(Deep Signal)으로 진입하세요.";

        private const string ResourcesBody =
            "3. 자원 종류 및 게임 내 주요 오브젝트 (Resources & Objects)\n\n"
            + "자원 종류 (Minerals & Terrain)\n"
            + "· 암반 (Bedrock): 지하의 기본 지형 블록입니다. 채굴하여 이동 동선을 확보합니다.\n"
            + "· 철 (Iron): 주황/은색 광물 블록입니다. 주된 장비 제작, 시설 건설, 기지 보강에 쓰이는 기본 자원입니다.\n"
            + "· 구리 (Copper): 붉은/갈색 광물 블록입니다. 전력 시설 제작 및 정산 시 높은 크레딧으로 환금되는 자원입니다.\n"
            + "· 리튬 (Lithium): 푸른빛의 희귀 광물 블록입니다. 심층 구역에서 발견되며 드론 및 고성능 장비 업그레이드에 필수적입니다.\n\n"
            + "위험 지형 및 특수 오브젝트\n"
            + "· 가스 포켓 (Gas Pocket): 유독 가스를 분출하는 지형입니다. 가스 드론 경고가 뜨면 빠르게 회피해야 합니다.\n"
            + "· 불안정 균열 지반 (Unstable Ground): 붕괴 위험이 높은 지형입니다. 붕괴 시 지반이 무너져 내려 동선이 막힙니다.\n"
            + "· 심층 봉인 신호 (Deep Signal): 최하층 탐사 목표 지점입니다.\n\n"
            + "설치 시설 및 보조 장치 (Facilities & Companions)\n"
            + "· 표면 기지 (Surface Base): 정산, 판매, 업그레이드가 이루어지는 안전한 지상 거점입니다.\n"
            + "· 버팀목 (Support Prop): 지반에 설치하여 주변 구조 안정도를 회복시키고 붕괴를 막아주는 건설물입니다.\n"
            + "· 충전기 & 조명 (Charger & Light): 지하 전력망을 연결하여 시야를 확보하고 에너지를 공급하는 시설입니다.\n"
            + "· Digger-Bot (드론): 플레이어를 따라다니며 위험 알림, 지형/자원 스캔 분석, 행동 추천, 탐사 실패 시 화물 보존 구조를 수행하는 탐사 보조 로봇입니다.";
    }
}

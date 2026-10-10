using System;
using System.Collections.Generic;
using System.Globalization;
using SubTerra.App.Core.Data;
using SubTerra.App.Outpost;
using SubTerra.App.Run;
using SubTerra.App.Save;
using SubTerra.Shared.Localization;

namespace SubTerra.App.UI.Guide
{
    /// <summary>
    /// 게임 가이드 콘텐츠 정의. 표시 코드와 분리된 순수 데이터이며 카드 ID로만 서로를 가리킨다.
    /// 숫자(비용·대기 시간·타이머)는 게임 코드의 상수/카탈로그에서 읽어 문구를 만든다.
    /// 문구의 근거 코드는 work_process/MVP2/guide-ui/prompt-b140-result.md에 정리한다.
    /// </summary>
    public static class GameGuideCatalog
    {
        public const string FirstExploreTitle = "처음이라면, 이 순서로 탐사하세요";
        public const string FirstExploreHint = "각 단계를 선택하면 관련 안내를 볼 수 있습니다.";
        public const string FirstExploreCollapsedLabel = "첫 탐사 안내";

        /// <summary>미니맵 단축키. ExplorationMinimap은 M 하나로 닫힘 → 가로형 → 정사각형을 순환한다.</summary>
        public const string MinimapKeys = "M";

        private static List<GuideCardDef> cards;
        private static Dictionary<string, GuideCardDef> byId;
        private static List<GuideFirstStep> firstSteps;

        public static IReadOnlyList<GuideCardDef> All
        {
            get
            {
                Ensure();
                return cards;
            }
        }

        public static IReadOnlyList<GuideFirstStep> FirstSteps
        {
            get
            {
                Ensure();
                return firstSteps;
            }
        }

        public static bool TryGet(string id, out GuideCardDef card)
        {
            Ensure();
            card = null;
            return !string.IsNullOrEmpty(id) && byId.TryGetValue(id, out card);
        }

        public static GuideCardDef Get(string id)
        {
            return TryGet(id, out var card) ? card : null;
        }

        public static List<GuideCardDef> ForTab(GuideTabKind tab)
        {
            Ensure();
            var list = new List<GuideCardDef>();
            for (var i = 0; i < cards.Count; i++)
            {
                if (cards[i].Tab == tab)
                {
                    list.Add(cards[i]);
                }
            }

            return list;
        }

        public static string TabTitle(GuideTabKind tab)
        {
            switch (tab)
            {
                case GuideTabKind.Controls: return "기본 조작";
                case GuideTabKind.Mechanics: return "핵심 메커니즘";
                default: return "자원·시설";
            }
        }

        /// <summary>플레인 텍스트 요약(접근성·구형 빌더 호환용).</summary>
        public static string PlainText(GuideTabKind tab)
        {
            Ensure();
            var builder = new System.Text.StringBuilder();
            builder.Append(TabTitle(tab)).Append('\n');
            for (var i = 0; i < cards.Count; i++)
            {
                var card = cards[i];
                if (card.Tab != tab)
                {
                    continue;
                }

                builder.Append("· ").Append(card.Title);
                if (!string.IsNullOrEmpty(card.Keys))
                {
                    builder.Append(" [").Append(card.Keys).Append(']');
                }

                builder.Append(" - ").Append(card.Summary).Append('\n');
            }

            return builder.ToString();
        }

        private static void Ensure()
        {
            if (cards != null)
            {
                return;
            }

            cards = new List<GuideCardDef>();
            Build();
            byId = new Dictionary<string, GuideCardDef>(StringComparer.Ordinal);
            for (var i = 0; i < cards.Count; i++)
            {
                byId[cards[i].Id] = cards[i];
            }
        }

        private static string N(string buildingId) => ItemDisplayNames.Building(buildingId);

        private static GuideCardDef Add(GuideCardDef card)
        {
            cards.Add(card);
            return card;
        }

        private static GuideCardDef Control(string id, string title, string summary, string keys, string demo)
        {
            return Add(new GuideCardDef(id, GuideTabKind.Controls, GuideCardKind.Control, title, summary, keys, demo));
        }

        private static GuideCardDef Mechanic(string id, string title, string summary, string demo)
        {
            return Add(new GuideCardDef(id, GuideTabKind.Mechanics, GuideCardKind.Mechanic, title, summary, string.Empty, demo));
        }

        private static GuideCardDef Resource(string id, string title, string summary, string sourceId)
        {
            return Add(new GuideCardDef(id, GuideTabKind.Resources, GuideCardKind.Resource, title, summary, string.Empty, id, sourceId));
        }

        private static GuideCardDef Facility(string id, string title, string summary, string keys, string sourceId)
        {
            return Add(new GuideCardDef(id, GuideTabKind.Resources, GuideCardKind.Facility, title, summary, keys, id, sourceId));
        }

        private static string Hours(double seconds)
        {
            return (seconds / 3600d).ToString("0.##", CultureInfo.InvariantCulture) + "시간";
        }

        private static string Minutes(double seconds)
        {
            return Math.Round(seconds / 60d).ToString("0", CultureInfo.InvariantCulture) + "분";
        }

        private static void Build()
        {
            var charger = N(DataIds.Buildings.ChargerBasic);
            var clinic = N(DataIds.Buildings.ClinicBasic);
            var storage = N(DataIds.Buildings.StorageBasic);
            var settlement = N(DataIds.Buildings.SettlementBasic);
            var core = N(DataIds.Buildings.OutpostCoreBasic);
            var portal = N(DataIds.Buildings.EmergencyEscapePortal);
            var ladder = N(DataIds.Buildings.LadderBasic);
            var support = N(DataIds.Buildings.SupportBasic);
            var light = N(DataIds.Buildings.LightBasic);
            var cooldown = Minutes(OutpostService.FacilityUseCooldownSeconds);
            var resetHours = Hours(MineResetService.CycleDurationSeconds);
            var elevatorEnergy = SaveRuntimeController.MineElevatorEnergyCost;
            var elevatorEnergyText = elevatorEnergy > 0
                ? "전력 " + elevatorEnergy + "이 필요합니다."
                : "귀환에는 전력이 들지 않습니다.";

            // ---------------------------------------------------------------- 기본 조작
            Control("ctrl.move", "좌우 이동", "좌우로 이동하며 광산을 탐사합니다.", "A D / ← →", "move")
                .Data("사용 조건", "지상·지하 어디서나 쓸 수 있습니다. 화물이 무거울수록 이동 속도와 점프력이 줄어듭니다.")
                .Step("이동", "A·D 또는 ←·→ 키를 누르는 동안 그 방향으로 이동합니다.")
                .Step("점프", "Space 키로 점프합니다.")
                .Tip("이동 키 묶음은 조작 방식(Esc 설정)에 따라 다릅니다. 기본 방식은 WASD와 방향키가 모두 이동이고, 한쪽을 이동으로 고르면 다른 쪽은 상하좌우 방향 채굴 키가 됩니다.")
                .Link("ctrl.ladder", "ctrl.mine");

            Control("ctrl.ladder", "사다리 승하강", "설치한 사다리를 타고 위아래로 이동합니다.", "W S / ↑ ↓", "ladder")
                .Data("사용 조건", "사다리에 닿은 상태에서 위·아래 이동 키를 누릅니다. E 키 같은 별도 상호작용은 없습니다.")
                .Step("사다리 접근", "사다리 칸에 닿습니다.")
                .Step("오르내리기", "W·S 또는 ↑·↓ 키로 위아래로 이동합니다.")
                .Step("이탈", "Space로 점프하면 사다리에서 벗어납니다.")
                .Tip("긴 낙하 도중 사다리를 잡으면 낙하 피해를 받지 않습니다. 사다리는 전력이 필요 없습니다.")
                .Link("fac.ladder");

            Control("ctrl.elevator", "엘리베이터", "엘리베이터로 지상 기지에 귀환합니다.", "E", "elevator")
                .Data("사용 조건", "광산의 엘리베이터 안에 서서 E 키를 누릅니다. 탑승하면 '귀환' 안내가 표시됩니다.")
                .Data("결과", "지상 기지로 이동합니다. " + elevatorEnergyText)
                .Tip("지상 기지에서 광산으로 내려갈 때는 엘리베이터 대신 '지하 탐사 시작' 버튼을 누릅니다.")
                .Link("mech.base", "fac.elevator");

            Control("ctrl.interact", "시설 상호작용", "시설 가까이에서 E 키로 사용합니다.", "E", "interact")
                .Data("사용 조건", storage + "·" + charger + "·" + clinic + "·" + settlement + "·" + core + "·" + portal
                    + " 가까이로 갑니다. 가까워지면 시설 이름이 말풍선으로 표시됩니다(버팀목·사다리 제외).")
                .Data("결과", "시설별 창이 열리거나 효과가 적용됩니다. " + charger + "·" + clinic + "·" + settlement
                    + "은 전력 공급 범위 안에서만 쓸 수 있습니다.")
                .Tip(charger + "와 " + clinic + "는 한 번 쓰면 재사용 대기 시간(" + cooldown + ")이 있습니다. 대기 시간은 지하 탐사 중에 흐르고, 남은 시간은 안내 문구로 확인합니다.")
                .Link("fac.storage", "fac.charger", "fac.clinic", "mech.grid");

            Control("ctrl.mine", "채굴", "가까운 블록을 캐서 자원을 얻습니다.", "MOUSE / Enter", "mine")
                .Data("사용 조건", "플레이어와 맞닿은 가까운 블록만 채굴할 수 있습니다. 채굴할 때마다 전력이 소모되고, 얻는 자원이 화물에 들어갈 자리가 있어야 합니다.")
                .Step("마우스 왼쪽 클릭", "클릭한 블록을 채굴합니다.")
                .Step("Enter", "바라보는 방향의 블록을 채굴합니다.")
                .Step("방향 채굴", "방향 채굴 방식이면 WASD 또는 방향키(이동에 쓰지 않는 쪽)로 상하좌우 블록을 채굴합니다.")
                .Warning("화물이 가득 차면 채굴이 막히고, 전력이 0이면 채굴할 수 없습니다.")
                .Tip("창 위를 클릭할 때는 채굴되지 않습니다. 드릴 업그레이드로 채굴 속도를 높일 수 있습니다.")
                .Link("mech.cargo", "mech.power", "mech.structure");

            Control("ctrl.build", "시설 건설", "B로 건설 창을 열고 C로 설치합니다.", "B -> C", "build")
                .Step("건설 창 열기", "B 키로 시설 건설 창을 열고 설치할 시설을 고릅니다.")
                .Step("설치", "C 키를 누르면 플레이어 가까운 설치 가능한 칸에 설치합니다. 미리보기 칸을 마우스로 가리켜 왼쪽 클릭해도 설치됩니다.")
                .Step("취소", "B 또는 X로 창을 닫으면 선택이 취소됩니다.")
                .Data("사용 조건", "시설마다 정해진 재료(광물)를 가지고 있어야 합니다. 재료가 부족하거나 설치할 수 없는 칸이면 설치되지 않습니다.")
                .Link("fac.support", "fac.ladder", "fac.light");

            Control("ctrl.inventory", "인벤토리", "화물과 보유 자원을 확인합니다.", "I", "inventory")
                .Data("확인 내용", "보유한 자원의 종류와 수량, 화물 무게를 볼 수 있습니다.")
                .Tip("I 키를 다시 누르거나 창의 닫기 버튼으로 닫습니다.")
                .Link("mech.cargo");

            Control("ctrl.drone", "Digger-Bot", "탐사 보조 로봇의 조언을 확인합니다.", "Tab", "drone")
                .Step("열기·닫기", "Tab 키 또는 월드의 Digger-Bot을 마우스로 클릭하면 Digger-Bot 창이 열리고 닫힙니다.")
                .Text("Digger-Bot은 생존·구조 위험·가스·전력·귀환·탐사 순으로 상황을 살펴 지금 할 일을 추천합니다.")
                .Tip("창을 닫아도 Digger-Bot의 분석과 경고 말풍선은 계속 동작합니다.")
                .Link("mech.structure", "mech.gas");

            Control("ctrl.minimap", "미니맵", "M으로 광산 관측판을 열고, 작게 줄이고, 닫습니다.", MinimapKeys, "minimap")
                .Step("M", "M을 누를 때마다 닫힘 → 가로형 → 작은 정사각형 → 닫힘으로 한 단계만 전환됩니다. 길게 눌러도 한 번만 전환됩니다.")
                .Step("Ctrl+M", "미니맵은 기본적으로 반투명(50%)입니다. Ctrl+M을 누를 때마다 0.3초 만에 불투명과 반투명이 번갈아 바뀝니다.")
                .Data("표시 내용", "화면에 보이는 범위의 남은 블록, 채굴한 빈 공간, 내 위치(깜빡이는 붉은 불빛)와 시설의 실제 위치·크기를 표시합니다.")
                .Tip("미니맵 아래의 아이콘은 이름 없이 모양과 색으로 시설을 구분합니다. 각 아이콘의 설명은 '자원·시설' 탭의 시설 항목에서 볼 수 있습니다.");

            Control("ctrl.clock", "광산 초기화 타이머", "남은 탐사 시간을 화면 상단 시계로 확인합니다.", "T", "clock")
                .Step("T", "화면 상단 중앙의 전자시계를 켜고 끕니다.")
                .Data("표시 조건", "지하 탐사 중에만 보이고 시간이 흐릅니다. 지상 기지에서는 멈춥니다.")
                .Data("결과", "시계가 0이 되면 광산이 자동으로 초기화되고 지상 기지로 이동합니다.")
                .Link("mech.reset");

            Control("ctrl.guide", "가이드 열기", "G 키 또는 오른쪽 메뉴에서 이 창을 엽니다.", "G", "guide")
                .Step("G", "G 키로 가이드를 열고 닫습니다.")
                .Step("오른쪽 메뉴", "/ 키로 여닫는 오른쪽 메뉴의 가이드 버튼으로도 열 수 있습니다.")
                .Step("닫기", "오른쪽 위 X 버튼이나 X 키로 닫습니다.")
                .Tip("가이드를 열어 두어도 게임은 멈추지 않습니다.");

            // ---------------------------------------------------------------- 핵심 메커니즘
            Mechanic("mech.base", "지상 기지의 역할", "탐사를 준비하고 정산하는 안전 구역", "base")
                .Data("할 수 있는 일", "지하 탐사 시작 · 자원 판매 · 업그레이드 · 새 광산 초기화")
                .Data("도착하면", "전력이 가득 충전됩니다.")
                .Text("판매해서 얻은 골드는 새 광산 초기화와 긴급 구출 같은 비용에, 광물은 업그레이드와 시설 건설에 쓰입니다.")
                .Tip("지상 기지에서는 광산 초기화 타이머가 멈춥니다.")
                .Link("mech.reset", "fac.settlement");

            Mechanic("mech.health", "체력과 회복", "위험을 피하고 보건소에서 회복", "health")
                .Data("줄어드는 때", "떨어지는 돌에 맞을 때 · 높은 곳에서 길게 낙하할 때 · 가스에 오래 머물 때")
                .Data("회복 방법", clinic + "에서 E (전력 공급 범위 안, 재사용 대기 " + cooldown + ") · 업그레이드 '"
                    + ItemDisplayNames.Upgrade(DataIds.Upgrades.HealthRegeneration) + "'")
                .Warning("체력이 0이 되면 탐사에 실패합니다. 가진 화물의 일부를 잃고 지상 기지로 돌아갑니다.")
                .Tip("'" + ItemDisplayNames.Upgrade(DataIds.Upgrades.DroneRescue) + "' 업그레이드로 화물 손실을 줄일 수 있습니다.")
                .Link("fac.clinic", "mech.structure", "mech.gas");

            Mechanic("mech.power", "전력과 충전·구출", "전력을 확인하고 충전·구출을 이용", "power")
                .Data("전력을 쓰는 때", "채굴할 때마다 · 가스 지대에 있을 때")
                .Data("충전 방법", "지상 기지에 도착하면 가득 충전 · " + charger + "에서 E (전력 공급 범위 안, 재사용 대기 " + cooldown + ")")
                .Step("전력 고갈", "전력이 0이 되면 구출 팝업이 열립니다.")
                .Step("팝업을 닫으면", "플레이어 머리 위에 구출 버튼이 나타납니다.")
                .Step("다시 열기", "버튼을 누르거나 R 키를 누르면 팝업이 다시 열립니다. R은 팝업만 열 뿐 구출을 실행하지 않습니다.")
                .Step("구출 실행", "팝업에서 비용을 확인하고 구출 버튼을 눌러야 실행되며, 엘리베이터 위치로 이동합니다.")
                .Data("구출 비용", "골드는 보유량 안에서 최대 " + EmergencyRescueService.MaximumGoldCost + "G, 가진 화물은 "
                    + EmergencyRescueService.MineralLossPercent + "%가 차감됩니다. 보관함에 넣은 자원은 차감되지 않습니다. 정확한 값은 팝업에서 확인하세요.")
                .Tip("전력이 0이어도 걷기·점프·사다리는 가능합니다. 엘리베이터까지 갈 수 있다면 귀환에 전력이 들지 않습니다.")
                .Link("fac.charger", "mech.cargo", "ctrl.elevator");

            Mechanic("mech.cargo", "화물 관리", "가득 차기 전에 보관하거나 판매", "cargo")
                .Data("화물 한도", "자원마다 무게가 다르고, 화물 무게가 한도에 닿으면 더 담지 못합니다. 한도는 업그레이드로 늘릴 수 있습니다.")
                .Warning("더 담을 수 없으면 채굴이 막히고 '화물이 가득 찼습니다. 귀환해 화물을 비우세요.'가 표시됩니다.")
                .Data("무거울수록", "이동 속도와 점프력이 줄고, 높은 곳에서 떨어질 때 충격이 커집니다.")
                .Data("비우는 방법", storage + "에 보관 · " + settlement + "에서 판매 · 지상 기지에서 판매")
                .Tip("탐사 실패나 구출로 줄어드는 것은 가진 화물뿐입니다. 보관함의 자원은 안전합니다.")
                .Link("fac.storage", "fac.settlement", "ctrl.inventory");

            Mechanic("mech.grid", "전력망", "시설의 연결 상태 확인", "grid")
                .Data("공급원", "엘리베이터와 " + core + "가 주변에 전력을 공급합니다. " + core + "의 공급 범위는 파란 원으로 표시됩니다.")
                .Data("연결이 필요한 시설", charger + "·" + clinic + "·" + settlement
                    + "은 공급 범위 안에 있어야 합니다. 범위 밖이면 '사용불가, 전력망 미연결' 안내가 나옵니다.")
                .Tip("어떤 시설이 연결돼 있는지는 " + core + "의 CCTV 목록에서 확인할 수 있습니다.")
                .Link("mech.core", "fac.light");

            Mechanic("mech.core", "전진기지 코어", "연결 시설 확인 · 가스 정화 · 체크포인트", "core")
                .Step("CCTV 열기", core + " 가까이에서 E 키를 누르면 CCTV 창이 열립니다.")
                .Step("목록 확인", "전력이 연결된 " + charger + "·" + clinic + "·" + settlement + "이 목록에 표시됩니다.")
                .Step("시설 선택", "목록을 클릭하거나 ↑·↓ 키로 고르면 CCTV 화면이 그 시설로 이동합니다.")
                .Data("코어의 역할", "주변 시설에 전력을 공급하고, 유독 가스 정화 안전지대를 만들며, 탐사 체크포인트가 됩니다.")
                .Tip("CCTV 창은 코어에서 멀어지면 닫히고, E 키로 다시 열 수 있습니다.")
                .Link("fac.core", "mech.grid", "mech.gas");

            Mechanic("mech.structure", "구조 위험", "버팀목과 귀환 경로 확보", "structure")
                .Text("아래가 비어 있는 천장 칸이 위험합니다. 블록을 계속 캐면 균열이 짙어지고, 위·아래가 모두 비게 되면 곧 떨어집니다.")
                .Step("상태 확인", "HUD의 구조 상태와 균열 표시를 살핍니다.")
                .Step("점멸하면 피하기", "빨간 칸 중 점멸하는 칸이 곧 떨어집니다. 먼저 벗어나세요.")
                .Step("미리 보강", "점멸하기 전에 근처에 " + support + "을 설치하면 위험도가 내려갑니다.")
                .Warning("떨어지는 돌에 맞으면 체력이 줄어듭니다.")
                .Tip("점멸하지 않는 빨간 칸은 아직 떨어지지 않습니다.")
                .Link("fac.support", "mech.health", "ctrl.build");

            Mechanic("mech.gas", "가스 지대", "가스를 피하고 정화 지대로 대피", "gas")
                .Data("가스 속에서", "이동이 느려지고 시야가 흐려지며 전력이 계속 줄어듭니다.")
                .Warning("오래 머물러 노출이 쌓이면 체력 피해나 행동불능으로 이어집니다. 가스 밖에서는 노출이 서서히 회복됩니다.")
                .Data("대처", core + "의 가스 정화 안전지대 안에서는 가스의 영향을 받지 않습니다. 업그레이드 '"
                    + ItemDisplayNames.Upgrade(DataIds.Upgrades.GasResistance) + "'로 영향을 줄일 수 있습니다.")
                .Tip("Digger-Bot이 가스 위험을 알려 주면 바로 벗어나세요.")
                .Link("mech.core", "ctrl.drone");

            var resetNow = LocalizationService.Get("mine_reset.confirm.reset.desc", "채굴한 타일 · 지하 시설 · 붕괴 · 가스");
            var keepNow = LocalizationService.Get("mine_reset.confirm.keep.desc", "업그레이드 · 심층 해금 · 보유 광물");
            Mechanic("mech.reset", "광산 초기화", "남은 시간을 확인하고 귀환 준비", "reset")
                .Data("자동 초기화", "지하 탐사 시간이 " + resetHours + " 쌓이면 새 광산이 만들어지고 지상 기지로 이동합니다. 지상 기지에서는 시간이 흐르지 않습니다.")
                .Data("직접 초기화", "지상 기지의 '새 광산 초기화' 버튼으로 골드를 내고 초기화합니다. 첫 비용은 " + MineResetService.BaseFeeGold
                    + "G이고 직접 초기화할 때마다 2배가 됩니다. 자동 초기화가 일어나면 첫 비용으로 돌아갑니다.")
                .Data("초기화되는 것", resetNow)
                .Data("유지되는 것", keepNow)
                .Tip("T 키로 남은 시간을 확인하세요. 탐사 시간은 " + resetHours + "으로 다시 시작합니다.")
                .Link("ctrl.clock", "mech.base");

            // ---------------------------------------------------------------- 자원
            Resource("res.copper", ItemDisplayNames.Mineral(DataIds.Minerals.Copper), "채굴 · 보관 · 판매", DataIds.Minerals.Copper)
                .Link("mech.cargo", "fac.storage");
            Resource("res.iron", ItemDisplayNames.Mineral(DataIds.Minerals.Iron), "채굴 · 보관 · 판매", DataIds.Minerals.Iron)
                .Link("mech.cargo", "fac.storage");
            Resource("res.lithium", ItemDisplayNames.Mineral(DataIds.Minerals.Lithium), "채굴 · 보관 · 판매", DataIds.Minerals.Lithium)
                .Link("mech.cargo", "fac.storage");
            Resource("res.fuel", ItemDisplayNames.Mineral(DataIds.RareItems.EngineFuel), "희귀 · 지상에서 판매", DataIds.RareItems.EngineFuel)
                .Link("fac.storage", "fac.settlement");
            Resource("res.gold", "골드", "판매·초기화·구출 비용", DataIds.Currency.Gold)
                .Data("얻는 방법", "자원을 판매하거나 금이 박힌 블록(금맥 칸)을 채굴하면 얻습니다.")
                .Data("쓰이는 곳", "새 광산 초기화 · 긴급 구출 · " + portal + " 이용 비용")
                .Tip("골드는 화물 무게에 포함되지 않습니다.")
                .Link("mech.reset", "mech.power", "fac.settlement");

            // ---------------------------------------------------------------- 시설
            Facility("fac.storage", storage, "자원을 맡기거나 꺼내기", "E", DataIds.Buildings.StorageBasic)
                .Step("열기", "가까이에서 E 키를 누르면 보관함 창이 열립니다.")
                .Step("보관·꺼내기", "자원과 수량을 고른 뒤 보관하거나 꺼냅니다.")
                .Data("연결 조건", "전력망 연결 없이 가까이에서 쓸 수 있습니다.")
                .Data("수량", "요청한 수량이 가진·보관한 수량보다 많으면 있는 만큼만 옮깁니다. 꺼낼 때 화물 무게 한도를 넘으면 꺼내지 못합니다.")
                .Tip("보관함의 자원은 탐사 실패·구출 비용에서 제외됩니다.")
                .Data("미니맵 아이콘", "주황색 정육면체 모양으로, 1×1칸 크기로 표시됩니다.")
                .Link("mech.cargo", "ctrl.interact");

            Facility("fac.charger", charger, "탐사 전력 충전", "E", DataIds.Buildings.ChargerBasic)
                .Data("효과", "전력을 가득 채웁니다.")
                .Data("연결 조건", "엘리베이터나 " + core + "의 전력 공급 범위 안에서만 쓸 수 있습니다.")
                .Data("재사용", "한 번 쓰면 " + cooldown + " 동안 다시 쓸 수 없습니다.")
                .Data("미니맵 아이콘", "노란색 번개 모양입니다. 전력을 받지 못하면 흐리게 표시됩니다.")
                .Link("ctrl.interact", "mech.grid", "mech.power");

            Facility("fac.clinic", clinic, "체력 회복", "E", DataIds.Buildings.ClinicBasic)
                .Data("효과", "체력을 최대치까지 회복합니다.")
                .Data("연결 조건", "엘리베이터나 " + core + "의 전력 공급 범위 안에서만 쓸 수 있습니다.")
                .Data("재사용", "한 번 쓰면 " + cooldown + " 동안 다시 쓸 수 없습니다.")
                .Data("미니맵 아이콘", "초록색 십자 모양입니다. 전력을 받지 못하면 흐리게 표시됩니다.")
                .Link("ctrl.interact", "mech.grid", "mech.health");

            Facility("fac.settlement", settlement, "보유 자원 판매", "E", DataIds.Buildings.SettlementBasic)
                .Data("효과", "가진 광물을 골드로 정산합니다. 정산할 자원을 고르는 판매 창이 열립니다.")
                .Data("연결 조건", "엘리베이터나 " + core + "의 전력 공급 범위 안에서만 쓸 수 있습니다.")
                .Data("제외", ItemDisplayNames.Mineral(DataIds.RareItems.EngineFuel) + " 같은 희귀 품목은 정산할 수 없고 지상 기지에서 판매합니다.")
                .Data("미니맵 아이콘", "보라색 계산기 모양으로, 가로 1칸 × 세로 2칸 크기로 표시됩니다. 전력을 받지 못하면 흐리게 표시됩니다.")
                .Link("ctrl.interact", "res.fuel", "mech.grid");

            Facility("fac.core", core, "기지 핵심 시설", "E", DataIds.Buildings.OutpostCoreBasic)
                .Data("효과", "주변 시설에 전력을 공급하고 유독 가스 정화 안전지대를 만들며 탐사 체크포인트가 됩니다.")
                .Data("사용", "가까이에서 E 키로 CCTV 창을 열어 연결된 시설을 확인합니다.")
                .Data("미니맵 아이콘", "청록색 육각형 모양입니다.")
                .Link("mech.core", "mech.grid", "mech.gas");

            Facility("fac.ladder", ladder, "다른 높이로 이동", "W S", DataIds.Buildings.LadderBasic)
                .Data("사용", "닿은 상태에서 위·아래 이동 키로 오르내립니다. E 키는 쓰지 않습니다.")
                .Link("ctrl.ladder", "ctrl.build");

            Facility("fac.support", support, "구조 위험 완화", string.Empty, DataIds.Buildings.SupportBasic)
                .Data("효과", "설치한 곳 주변 천장의 붕괴 위험도를 낮춥니다.")
                .Link("mech.structure", "ctrl.build");

            Facility("fac.light", light, "지하 구역을 밝힘", string.Empty, DataIds.Buildings.LightBasic)
                .Data("효과", "전력이 연결된 구역을 밝힙니다.")
                .Data("미니맵 아이콘", "노란색 전구 모양으로, 세로로 긴 1×2칸 크기로 표시됩니다. 전력을 받지 못하면 흐리게 표시됩니다.")
                .Link("ctrl.build", "mech.grid");

            Facility("fac.portal", portal, "긴급 이동 시설", "E", DataIds.Buildings.EmergencyEscapePortal)
                .Step("열기", "전력망에 등록된 포탈 가까이에서 E 키를 누릅니다.")
                .Step("목적지 선택", "엘리베이터 또는 설치한 " + core + " 중에서 고릅니다.")
                .Step("비용 확인 후 이동", "비용을 확인하고 실행하면 선택한 곳으로 이동합니다.")
                .Data("비용", "골드 " + EmergencyEscapeService.GoldCost + "G와 최대 전력의 "
                    + Math.Round(EmergencyEscapeService.MaximumEnergyCostRatio * 100d).ToString("0", CultureInfo.InvariantCulture) + "%")
                .Data("미니맵 아이콘", "분홍색 웜홀 모양으로, 안쪽 소용돌이가 천천히 돕니다. 2×2칸 크기로 표시됩니다.")
                .Link("ctrl.interact", "mech.core");

            Add(new GuideCardDef("fac.elevator", GuideTabKind.Resources, GuideCardKind.Facility, "엘리베이터", "지상 기지로 귀환", "E", "fac.elevator"))
                .Data("사용", "광산의 엘리베이터 안에서 E 키를 누르면 지상 기지로 귀환합니다. " + elevatorEnergyText)
                .Data("전력 공급", "엘리베이터 주변에는 전력이 공급됩니다. " + charger + "·" + clinic + "·" + settlement + "을 가까이 두면 쓸 수 있습니다.")
                .Data("미니맵 아이콘", "하늘색 위·아래 화살표 모양입니다. 승강로를 따라 화살표가 세로로 늘어서 표시됩니다.")
                .Link("ctrl.elevator", "mech.grid");

            // ---------------------------------------------------------------- 첫 탐사 안내
            firstSteps = new List<GuideFirstStep>
            {
                new GuideFirstStep("01", "지하로 이동", "지상 기지에서 지하 탐사 시작", "first.enter", "icon.mine", "mech.base"),
                new GuideFirstStep("02", "자원 채굴", "인접한 광물을 채굴", "first.mine", "icon.copper", "ctrl.mine"),
                new GuideFirstStep("03", "상태 확인", "체력·전력·화물을 확인", "first.status", "icon.cargo", "mech.health"),
                new GuideFirstStep("04", "지상으로 귀환", "위험해지기 전에 돌아오기", "first.return", "icon.elevator", "ctrl.elevator"),
                new GuideFirstStep("05", "판매·업그레이드", "자원을 팔고 다음 탐사 준비", "first.sell", "icon.sell", "fac.settlement")
            };
        }
    }
}

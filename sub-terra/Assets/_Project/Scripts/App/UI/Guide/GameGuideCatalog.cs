using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SubTerra.App.Core.Data;
using SubTerra.App.Outpost;
using SubTerra.App.Progression;
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
        public static string FirstExploreTitle => L("처음이라면, 이 순서로 탐사하세요", "Your First Expedition");
        public static string FirstExploreHint => L("각 단계를 선택하면 관련 안내를 볼 수 있습니다.", "Select a step to open its guide.");
        public static string FirstExploreCollapsedLabel => L("첫 탐사 안내", "First Expedition");

        /// <summary>미니맵 단축키. ExplorationMinimap은 M 하나로 닫힘 → 가로형 → 정사각형을 순환한다.</summary>
        public const string MinimapKeys = "M";

        private static List<GuideCardDef> cards;
        private static Dictionary<string, GuideCardDef> byId;
        private static List<GuideFirstStep> firstSteps;
        private static GameLanguage catalogLanguage;
        private static string L(string korean, string english) => LocalizationService.Text(korean, english);

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
                case GuideTabKind.Controls: return L("기본 조작", "Controls");
                case GuideTabKind.Mechanics: return L("핵심 메커니즘", "Mechanics");
                default: return L("자원·시설", "Resources & Facilities");
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
            var language = LocalizationService.Current;
            if (cards != null && catalogLanguage == language)
            {
                return;
            }

            catalogLanguage = language;
            cards = new List<GuideCardDef>();
            Build();
            byId = new Dictionary<string, GuideCardDef>(StringComparer.Ordinal);
            for (var i = 0; i < cards.Count; i++)
            {
                byId[cards[i].Id] = cards[i];
            }
        }

        private static string N(string buildingId) => ItemDisplayNames.LocalizedBuilding(buildingId);

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
            return (seconds / 3600d).ToString("0.##", CultureInfo.InvariantCulture) + L("시간", " h");
        }

        private static string Minutes(double seconds)
        {
            return Math.Round(seconds / 60d).ToString("0", CultureInfo.InvariantCulture) + L("분", " min");
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
                ? L("전력 " + elevatorEnergy + "이 필요합니다.", $"Requires {elevatorEnergy} power.")
                : L("귀환에는 전력이 들지 않습니다.", "Returning uses no power.");

            var requirements = string.Join(" · ", DeepZoneUnlockRule.Mvp.UpgradeRequirements.Select(requirement => ItemDisplayNames.LocalizedUpgrade(requirement.UpgradeId) + " Lv." + requirement.RequiredLevel));
            var regen = ItemDisplayNames.LocalizedUpgrade(DataIds.Upgrades.HealthRegeneration);
            var rescue = ItemDisplayNames.LocalizedUpgrade(DataIds.Upgrades.DroneRescue);
            var gasResistance = ItemDisplayNames.LocalizedUpgrade(DataIds.Upgrades.GasResistance);

            // ---------------------------------------------------------------- 기본 조작
            Control("ctrl.move", L("좌우 이동", "Move"), L("좌우로 이동하며 광산을 탐사합니다.", "Move left or right to explore the mine."), "A D / ← →", "move")
                .Data(L("사용 조건", "Availability"), L("지상·지하 어디서나 쓸 수 있습니다. 화물이 무거울수록 이동 속도와 점프력이 줄어듭니다.", "Works above and below ground. Heavy cargo reduces movement speed and jump height."))
                .Step(L("이동", "Move"), L("A·D 또는 ←·→ 키를 누르는 동안 그 방향으로 이동합니다.", "Hold A/D or Left/Right to move in that direction."))
                .Step(L("점프", "Jump"), L("Space 키로 점프합니다.", "Press Space to jump."))
                .Tip(L("이동 키 묶음은 조작 방식(Esc 설정)에 따라 다릅니다. 기본 방식은 WASD와 방향키가 모두 이동이고, 한쪽을 이동으로 고르면 다른 쪽은 상하좌우 방향 채굴 키가 됩니다.", "The movement keys depend on the control mode in Esc settings. By default, WASD and the arrow keys both move. If one set moves, the other mines in four directions."))
                .Link("ctrl.ladder", "ctrl.mine");

            Control("ctrl.ladder", L("사다리 승하강", "Climb a Ladder"), L("설치한 사다리를 타고 위아래로 이동합니다.", "Use a ladder to move up or down."), "W S / ↑ ↓", "ladder")
                .Data(L("사용 조건", "Requirements"), L("사다리에 닿은 상태에서 위·아래 이동 키를 누릅니다. E 키 같은 별도 상호작용은 없습니다.", "While touching a ladder, press an up or down movement key. No interaction key is needed."))
                .Step(L("사다리 접근", "Approach"), L("사다리 칸에 닿습니다.", "Move into the ladder tile."))
                .Step(L("오르내리기", "Climb"), L("W·S 또는 ↑·↓ 키로 위아래로 이동합니다.", "Use W/S or Up/Down to climb."))
                .Step(L("이탈", "Get Off"), L("Space로 점프하면 사다리에서 벗어납니다.", "Press Space to jump away."))
                .Tip(L("긴 낙하 도중 사다리를 잡으면 낙하 피해를 받지 않습니다. 사다리는 전력이 필요 없습니다.", "Grabbing a ladder during a long fall prevents fall damage. Ladders need no power."))
                .Link("fac.ladder");

            Control("ctrl.elevator", L("엘리베이터", "Elevator"), L("엘리베이터로 지상 기지에 귀환합니다.", "Return to the surface base in the elevator."), "E", "elevator")
                .Data(L("사용 조건", "Requirements"), L("광산의 엘리베이터 안에 서서 E 키를 누릅니다. 탑승하면 '귀환' 안내가 표시됩니다.", "Stand inside the mine elevator and press E. The return prompt appears when you enter."))
                .Data(L("결과", "Result"), L("지상 기지로 이동합니다. " + elevatorEnergyText, $"Returns you to the surface base. {elevatorEnergyText}"))
                .Tip(L("지상 기지에서 광산으로 내려갈 때는 엘리베이터 대신 '지하 탐사 시작' 버튼을 누릅니다.", "At the surface base, select Start Expedition to go back down."))
                .Link("mech.base", "fac.elevator");

            Control("ctrl.interact", L("시설 상호작용", "Use Facilities"), L("시설 가까이에서 E 키로 사용합니다.", "Press E near a facility to use it."), "E", "interact")
                .Data(L("사용 조건", "Requirements"), L(storage + "·" + charger + "·" + clinic + "·" + settlement + "·" + core + "·" + portal
                    + " 가까이로 갑니다. 가까워지면 시설 이름이 말풍선으로 표시됩니다(" + support + "·사다리 제외).", "Approach a storage chest, charger, clinic, settlement terminal, outpost or emergency portal. Nearby facilities show a name bubble, except cushion pads and ladders."))
                .Data(L("결과", "Result"), L("시설별 창이 열리거나 효과가 적용됩니다. " + charger + "·" + clinic + "·" + settlement
                    + "은 전력 공급 범위 안에서만 쓸 수 있습니다.", "The facility opens a window or applies an effect. Chargers, clinics and settlement terminals must be within power range."))
                .Tip(L(charger + "와 " + clinic + "는 한 번 쓰면 재사용 대기 시간(" + cooldown + ")이 있습니다. 대기 시간은 지하 탐사 중에 흐르고, 남은 시간은 안내 문구로 확인합니다.", $"Chargers and clinics have a {cooldown} cooldown. It counts down while exploring underground. Check the prompt for the remaining time."))
                .Link("fac.storage", "fac.charger", "fac.clinic", "mech.grid");

            Control("ctrl.mine", L("채굴", "Mine"), L("가까운 블록을 캐서 자원을 얻습니다.", "Mine nearby blocks to collect resources."), "MOUSE / Enter", "mine")
                .Data(L("사용 조건", "Requirements"), L("플레이어와 맞닿은 가까운 블록만 채굴할 수 있습니다. 채굴할 때마다 전력이 소모되고, 얻는 자원이 화물에 들어갈 자리가 있어야 합니다.", "You can mine adjacent blocks. Each block uses power and the resource must fit in your cargo."))
                .Step(L("마우스 왼쪽 클릭", "Left Click"), L("클릭한 블록을 채굴합니다.", "Mine the selected block."))
                .Step(L("Enter", "Enter"), L("바라보는 방향의 블록을 채굴합니다.", "Mine the block in the direction you face."))
                .Step(L("방향 채굴", "Directional Mining"), L("방향 채굴 방식이면 WASD 또는 방향키(이동에 쓰지 않는 쪽)로 상하좌우 블록을 채굴합니다.", "In directional mode, use WASD or the arrow keys, whichever set is unused for movement, to mine in four directions."))
                .Warning(L("화물이 가득 차면 채굴이 막히고, 전력이 0이면 채굴할 수 없습니다.", "Mining stops when your cargo is full or your power reaches zero."))
                .Tip(L("창 위를 클릭할 때는 채굴되지 않습니다. 드릴 업그레이드로 채굴 속도를 높일 수 있습니다.", "Clicks on open windows do not mine. Upgrade your drill to mine faster."))
                .Link("mech.cargo", "mech.power", "mech.landing");

            Control("ctrl.build", L("시설 건설", "Build Facilities"), L("B로 건설 창을 열고 C로 설치합니다.", "Press B to open Build and C to place."), "B -> C", "build")
                .Step(L("건설 창 열기", "Open Build"), L("B 키로 시설 건설 창을 열고 설치할 시설을 고릅니다.", "Press B and select a facility."))
                .Step(L("설치", "Place"), L("C 키를 누르면 플레이어 가까운 설치 가능한 칸에 설치합니다. 미리보기 칸을 마우스로 가리켜 왼쪽 클릭해도 설치됩니다.", "Press C to build on a valid tile near you. You can also click the placement preview."))
                .Step(L("취소", "Cancel"), LocalizationService.Get("guide.build.close"))
                .Data(L("사용 조건", "Requirements"), L("시설마다 정해진 재료(광물)를 가지고 있어야 합니다. 재료가 부족하거나 설치할 수 없는 칸이면 설치되지 않습니다.", "Each facility needs specific minerals. You need enough materials and a valid tile."))
                .Link("fac.support", "fac.ladder", "fac.light");

            Control("ctrl.inventory", L("인벤토리", "Inventory"), L("화물과 보유 자원을 확인합니다.", "Check your cargo and resources."), "I", "inventory")
                .Data(L("확인 내용", "Shown Here"), L("보유한 자원의 종류와 수량, 화물 무게를 볼 수 있습니다.", "See resource types, quantities and cargo weight."))
                .Tip(L("I 키를 다시 누르거나 창의 닫기 버튼으로 닫습니다.", "Press I again or use the close button."))
                .Link("mech.cargo");

            Control("ctrl.drone", L("Digger-Bot", "Digger-Bot"), L("탐사 보조 로봇의 조언을 확인합니다.", "Get advice from your exploration robot."), "Tab", "drone")
                .Step(L("열기·닫기", "Open / Close"), L("Tab 키 또는 월드의 Digger-Bot을 마우스로 클릭하면 Digger-Bot 창이 열리고 닫힙니다.", "Press Tab or click Digger-Bot in the world to toggle its window."))
                .Text(L("Digger-Bot은 생존·가스·전력·귀환·탐사 상황을 살펴 지금 할 일을 추천합니다.", "Digger-Bot checks your survival, gas, power and exploration status to suggest your next move."))
                .Tip(L("창을 닫아도 Digger-Bot의 분석과 경고 말풍선은 계속 동작합니다.", "Digger-Bot keeps analyzing and showing warning bubbles while its window is closed."))
                .Link("mech.oxygen", "mech.gas");

            Control("ctrl.minimap", L("미니맵", "Minimap"), L("M으로 광산 관측판을 열고, 작게 줄이고, 닫습니다.", "Press M to change the mine map view."), MinimapKeys, "minimap")
                .Step(L("M", "M"), L("M을 누를 때마다 닫힘 → 가로형 → 작은 정사각형 → 닫힘으로 한 단계만 전환됩니다. 길게 눌러도 한 번만 전환됩니다.", "Press M to cycle from hidden to wide, compact and back to hidden. Holding it changes the view once."))
                .Step(L("Ctrl+M", "Ctrl+M"), L("미니맵은 기본적으로 반투명(50%)입니다. Ctrl+M을 누를 때마다 0.3초 만에 불투명과 반투명이 번갈아 바뀝니다.", "The minimap starts at 50% opacity. Ctrl+M toggles between opaque and translucent over 0.3 seconds."))
                .Data(L("표시 내용", "Map Contents"), L("화면에 보이는 범위의 남은 블록, 채굴한 빈 공간, 내 위치(깜빡이는 붉은 불빛)와 시설의 실제 위치·크기를 표시합니다.", "Shows blocks, mined spaces, your flashing red marker and the positions and sizes of facilities in view."))
                .Tip(L("미니맵 아래의 아이콘은 이름 없이 모양과 색으로 시설을 구분합니다. 각 아이콘의 설명은 '자원·시설' 탭의 시설 항목에서 볼 수 있습니다.", "The icons below the map identify facilities by shape and color. See the individual entries on the Resources & Facilities tab."));

            Control("ctrl.clock", L("광산 초기화 타이머", "Mine Reset Timer"), L("남은 탐사 시간을 화면 상단 시계로 확인합니다.", "Check the clock at the top of the screen for time left."), "T", "clock")
                .Step(L("T", "T"), L("화면 상단 중앙의 전자시계를 켜고 끕니다.", "Toggle the clock at the top center of the screen."))
                .Data(L("표시 조건", "When Shown"), L("지하 탐사 중에만 보이고 시간이 흐릅니다. 지상 기지에서는 멈춥니다.", "Shown and counting down underground. Paused at the surface base."))
                .Data(L("결과", "Result"), L("시계가 0이 되면 광산이 자동으로 초기화되고 지상 기지로 이동합니다.", "At zero, the mine resets automatically and you return to the surface base."))
                .Link("mech.reset");

            Control("ctrl.guide", L("가이드 열기", "Open the Guide"), L("G 키 또는 오른쪽 메뉴에서 이 창을 엽니다.", "Press G or use the right-hand menu."), "G", "guide")
                .Step(L("G", "G"), L("G 키로 가이드를 열고 닫습니다.", "Press G to toggle the guide."))
                .Step(L("오른쪽 메뉴", "Right-hand Menu"), L("/ 키로 여닫는 오른쪽 메뉴의 가이드 버튼으로도 열 수 있습니다.", "Press / to toggle the menu and select Guide."))
                .Step(L("닫기", "Close"), LocalizationService.Get("guide.upgrade.close"))
                .Tip(L("광산에서 가이드를 열면 게임이 일시정지됩니다. 가이드를 닫는 연출이 끝나면 탐사가 재개됩니다.", "Opening the guide in the mine pauses the game. Exploration resumes after the closing animation."));

            // ---------------------------------------------------------------- 핵심 메커니즘
            Mechanic("mech.base", L("지상 기지의 역할", "Surface Base"), L("탐사를 준비하고 정산하는 안전 구역", "Prepare and sell resources in a safe area."), "base")
                .Data(L("할 수 있는 일", "Available Actions"), L("지하 탐사 시작 · 자원 판매 · 업그레이드 · 새 광산 초기화", "Start an expedition, sell resources, buy upgrades or reset the mine."))
                .Data(L("심층 구역 해금", "Deep Zone Access"), L(requirements + "을 모두 갖추면 자동 해금됩니다. 업그레이드 창에서 조건을 확인하세요.", $"Meet all requirements ({requirements}) to unlock the deep zone. Check the upgrade window for the conditions."))
                .Data(L("도착하면", "On Arrival"), L("전력이 가득 충전됩니다.", "Your power is fully recharged."))
                .Text(L("판매해서 얻은 골드는 새 광산 초기화와 긴급 구출 같은 비용에, 광물은 업그레이드와 시설 건설에 쓰입니다.", "Gold from sales pays for mine resets and emergency rescue. Minerals are used for upgrades and facilities."))
                .Tip(L("지상 기지에서는 광산 초기화 타이머가 멈춥니다.", "The mine reset timer stops at the surface base."))
                .Link("mech.reset", "fac.settlement");

            Mechanic("mech.health", L("체력과 회복", "Health and Healing"), L("위험을 피하고 보건소에서 회복", "Avoid hazards and recover at clinics."), "health")
                .Data(L("줄어드는 때", "Damage Sources"), L("떨어지는 돌에 맞을 때 · 높은 곳에서 길게 낙하할 때 · 가스에 오래 머물 때", "Falling rocks, long falls and prolonged gas exposure."))
                .Data(L("회복 방법", "Healing"), L(clinic + "에서 E (전력 공급 범위 안, 재사용 대기 " + cooldown + ") · 업그레이드 '"
                    + ItemDisplayNames.Upgrade(DataIds.Upgrades.HealthRegeneration) + "'", $"Press E at a powered clinic ({cooldown} cooldown), or buy the {regen} upgrade."))
                .Warning(L("체력이 0이 되면 탐사에 실패합니다. 가진 화물의 일부를 잃고 지상 기지로 돌아갑니다.", "At zero health, the expedition fails. You lose some cargo and return to the surface base."))
                .Tip(L("'" + ItemDisplayNames.Upgrade(DataIds.Upgrades.DroneRescue) + "' 업그레이드로 화물 손실을 줄일 수 있습니다.", $"The {rescue} upgrade reduces cargo losses."))
                .Link("fac.clinic", "mech.oxygen", "mech.landing", "mech.gas");

            Mechanic("mech.power", L("전력과 충전·구출", "Power, Charging & Rescue"), L("전력을 확인하고 충전·구출을 이용", "Check power, recharge and request rescue."), "power")
                .Data(L("채굴 전력", "Mining Power"), L("블록을 캘 때마다 소모됩니다. 소모량은 채굴한 블록에 따라 다릅니다.", "Every block costs power to mine. The cost depends on the block."))
                .Data(L("충전 방법", "Recharging"), L("지상 기지에 도착하면 가득 충전 · " + charger + "에서 E (전력 공급 범위 안, 재사용 대기 " + cooldown + ")", $"Power refills at the surface base. Underground, press E at a powered charger ({cooldown} cooldown)."))
                .Step(L("안내 버튼 확인", "Check the Rescue Prompt"), L("전력이 0이 되거나 산소가 위험 단계에 들어가면 머리 위에 구출 버튼이 나타납니다.", "A rescue button appears above you at zero power or when oxygen becomes critical."))
                .Step(L("직접 귀환", "Return on Foot"), L("전력이 없어도 걷기·점프·사다리로 엘리베이터까지 갈 수 있습니다.", "You can walk, jump and climb to the elevator with zero power."))
                .Step(L("도움 요청", "Request Help"), L("버튼이나 R 키로 구출 팝업을 엽니다. R만 눌러서는 구출을 실행하지 않습니다.", "Use the button or press R to open the rescue window. R alone does not trigger a rescue."))
                .Step(L("구출 실행", "Confirm Rescue"), L("팝업에서 비용을 확인하고 구출 버튼을 눌러야 실행되며, 엘리베이터 위치로 이동합니다.", "Check the cost and choose Rescue to teleport to the elevator."))
                .Data(L("구출 비용", "Rescue Cost"), L("골드는 보유량 안에서 최대 " + EmergencyRescueService.MaximumGoldCost + "G, 가진 화물은 "
                    + EmergencyRescueService.MineralLossPercent + "%가 차감됩니다. 보관함에 넣은 자원은 차감되지 않습니다. 정확한 값은 팝업에서 확인하세요.", $"Rescue costs up to {EmergencyRescueService.MaximumGoldCost}G from your balance and {EmergencyRescueService.MineralLossPercent}% of your carried minerals. Stored resources are safe. Check the window for the exact cost."))
                .Tip(L("전력이 0이어도 걷기·점프·사다리는 가능합니다. 엘리베이터까지 갈 수 있다면 귀환에 전력이 들지 않습니다.", "Walking, jumping and climbing still work at zero power. If you reach the elevator, returning costs no power."))
                .Link("fac.charger", "mech.cargo", "ctrl.elevator", "mech.oxygen");

            Mechanic("mech.cargo", L("화물 관리", "Cargo Management"), L("가득 차기 전에 보관하거나 판매", "Store or sell resources before cargo fills up."), "cargo")
                .Data(L("화물 한도", "Cargo Limit"), L("자원마다 무게가 다르고, 화물 무게가 한도에 닿으면 더 담지 못합니다. 한도는 업그레이드로 늘릴 수 있습니다.", "Resources have different weights. You cannot carry more once you reach the limit. Upgrades increase cargo capacity."))
                .Warning(L("더 담을 수 없으면 채굴이 막히고 '화물이 가득 찼습니다. 귀환해 화물을 비우세요.'가 표시됩니다.", "If there is no room, mining stops and a prompt tells you to return and unload."))
                .Data(L("무거울수록", "Heavy Cargo"), L("이동 속도와 점프력이 줄고, 산소 소모와 높은 곳에서의 착지 충격이 커집니다.", "Heavy cargo reduces movement speed and jump height and increases oxygen use and fall damage."))
                .Data(L("비우는 방법", "Free Up Space"), L(storage + "에 보관 · " + settlement + "에서 판매 · 지상 기지에서 판매", "Store resources in a chest, or sell them at a settlement terminal or the surface base."))
                .Tip(L("탐사 실패나 구출로 줄어드는 것은 가진 화물뿐입니다. 보관함의 자원은 안전합니다.", "Only carried cargo is lost on failure or rescue. Resources in storage are safe."))
                .Link("fac.storage", "fac.settlement", "ctrl.inventory", "mech.oxygen", "mech.landing");

            Mechanic("mech.grid", L("전력망", "Power Grid"), L("시설의 연결 상태 확인", "Check which facilities are connected."), "grid")
                .Data(L("공급원", "Power Sources"), L("엘리베이터와 " + core + "가 주변에 전력을 공급합니다. " + core + "의 공급 범위는 파란 원으로 표시됩니다.", "The elevator and outposts supply power nearby. An outpost's power range is shown as a blue circle."))
                .Data(L("연결이 필요한 시설", "Facilities That Need Power"), L(charger + "·" + clinic + "·" + settlement
                    + "은 공급 범위 안에 있어야 합니다. 범위 밖이면 '사용불가, 전력망 미연결' 안내가 나옵니다.", "Chargers, clinics and settlement terminals work within the power range. Out-of-range facilities show a disconnected message."))
                .Tip(L("어떤 시설이 연결돼 있는지는 " + core + "의 CCTV 목록에서 확인할 수 있습니다.", "See the CCTV list at the outpost for connected facilities."))
                .Link("mech.core", "fac.light");

            Mechanic("mech.core", L((ItemDisplayNames.OutpostName), "Outpost"), L("연결 시설 확인 · 가스 정화 · 체크포인트", "Check facilities, purify gas and set a checkpoint."), "core")
                .Step(L("CCTV 열기", "Open CCTV"), L(core + " 가까이에서 E 키를 누르면 CCTV 창이 열립니다.", "Press E near the outpost to open the CCTV window."))
                .Step(L("목록 확인", "Check the List"), L("전력이 연결된 " + charger + "·" + clinic + "·" + settlement + "가 목록에 표시됩니다.", "The list shows powered chargers, clinics and settlement terminals."))
                .Step(L("시설 선택", "Select a Facility"), L("목록을 클릭하거나 ↑·↓ 키로 고르면 CCTV 화면이 그 시설로 이동합니다.", "Click an entry or use Up/Down to move the camera to that facility."))
                .Data(L(core + "의 역할", "Outpost Functions"), L("주변 시설에 전력을 공급하고, 유독 가스 정화 안전지대를 만들며, 탐사 체크포인트가 됩니다.", "Provides power, creates a gas purification zone and serves as a checkpoint."))
                .Tip(L("CCTV 창은 " + core + "에서 멀어지면 닫히고, E 키로 다시 열 수 있습니다.", "The CCTV window closes when you leave the outpost. Press E to reopen it."))
                .Link("fac.core", "mech.grid", "mech.gas");

            Mechanic("mech.oxygen", L("산소", "Oxygen"), L("남은 산소를 살피고 귀환로 확보", "Watch your oxygen and keep a return route."), "structure")
                .Data(L("산소 회복", "Recovery"), L("깊이 0~10m에서는 가스가 있어도 산소가 최대량까지 회복됩니다.", "At 0–10 m, oxygen refills to maximum, even in gas."))
                .Data(L("산소 소모", "Oxygen Use"), L("10m를 넘으면 산소가 줄어듭니다. 더 깊이 내려가거나 움직이거나 짐이 무거우면 소모가 빨라집니다.", "Below 10 m, oxygen drains faster at greater depths, during movement or with heavy cargo."))
                .Data(L("가스 지대", "Gas Zones"), L("10m를 넘는 곳에서 정화되지 않은 가스에 노출되면 산소 소모가 기본 2배로 늘어납니다.", "Unpurified gas below 10 m doubles the default oxygen drain rate."))
                .Data(L("산소 위험도", "Oxygen Status"), L("HUD에서 안전·주의·위험을 확인하세요. 산소가 0이면 체력이 주기적으로 줄어듭니다.", "The HUD shows Safe, Caution or Critical. At zero oxygen, you take periodic health damage."))
                .Data(L("구출 요청", "Request Rescue"), L("산소가 위험 단계(기본 약 15% 이하)에 들어가면 머리 위에 구출 요청 표식이 나타납니다.", "The rescue marker appears above you when oxygen reaches the critical level (about 15% or less by default)."))
                .Tip(L("무거운 짐을 들고 오를 때 사다리는 화물로 인한 산소 소모 증가가 제한됩니다.", "When climbing with heavy cargo, ladders cap the extra oxygen use from cargo weight."))
                .Link("fac.ladder", "mech.power", "mech.gas");

            Mechanic("mech.landing", L("착지 위험", "Fall Damage"), L("낙하 높이와 화물 무게에 주의", "Watch the drop height and cargo weight."), "landing")
                .Data(L("추락 시", "On Landing"),
                    L("높은 높이에서 떨어질수록, 인벤토리의 무게가 무거울 수록 착지 피해가 커지게 됩니다. 완충판 위에 착지하게 될 경우, 피해가 10%까지 줄어들게 됩니다.",
                        "Longer falls and heavier cargo increase fall damage. Landing on a cushion pad reduces damage to 10% of normal."))
                .Tip(L("높은 곳에서 내려올 때는 짐을 비우거나, 사다리를 이용하거나, 완충판 위에 착지하면 피해를 줄일 수 있습니다.", "Unload cargo, use a ladder or land on a cushion pad to reduce damage from high drops."))
                .Link("fac.support", "mech.cargo");

            Mechanic("mech.gas", L("가스 지대", "Gas Zones"), L("가스를 피하고 정화 지대로 대피", "Avoid gas and move into a purification zone."), "gas")
                .Data(L("가스 속에서", "Inside Gas"), L("이동이 느려지고 시야가 흐려집니다.", "Movement slows and visibility drops."))
                .Data(L("산소 소모", "Oxygen Use"), L("깊이 10m를 넘는 곳에서 정화되지 않은 가스에 노출되면 산소 소모가 기본 2배로 늘어납니다. 10m 이내에서는 산소가 회복됩니다.", "Unpurified gas below 10 m doubles the default oxygen drain rate. At 10 m or less, oxygen recovers."))
                .Warning(L("오래 머물러 노출이 쌓이면 체력 피해나 행동불능으로 이어집니다. 가스 밖에서는 노출이 서서히 회복됩니다.", "Prolonged exposure causes health damage or incapacitates you. Exposure fades after you leave the gas."))
                .Data(L("대처", "Protection"), L(core + "의 가스 정화 안전지대 안에서는 가스의 영향을 받지 않습니다. 업그레이드 '"
                    + ItemDisplayNames.Upgrade(DataIds.Upgrades.GasResistance) + "'로 영향을 줄일 수 있습니다.", $"Outposts provide gas purification zones. The {gasResistance} upgrade reduces the effects of gas."))
                .Tip(L("Digger-Bot이 가스 위험을 알려 주면 바로 벗어나세요.", "Leave as soon as Digger-Bot warns you about gas."))
                .Link("mech.core", "ctrl.drone", "mech.oxygen");

            var resetNow = LocalizationService.Get("mine_reset.confirm.reset.desc", "채굴한 타일 · 지하 시설 · 가스");
            var keepNow = LocalizationService.Get("mine_reset.confirm.keep.desc", "업그레이드 · 심층 해금 · 보유 광물");
            Mechanic("mech.reset", L("광산 초기화", "Mine Reset"), L("남은 시간을 확인하고 귀환 준비", "Check the time left and plan your return."), "reset")
                .Data(L("자동 초기화", "Automatic Reset"), L("지하 탐사 시간이 " + resetHours + " 쌓이면 새 광산이 만들어지고 지상 기지로 이동합니다. 지상 기지에서는 시간이 흐르지 않습니다.", $"After {resetHours} underground, a fresh mine is generated and you are returned to the surface base. Time stops at the base."))
                .Data(L("직접 초기화", "Manual Reset"), L("지상 기지의 '새 광산 초기화' 버튼으로 골드를 내고 초기화합니다. 첫 비용은 " + MineResetService.BaseFeeGold
                    + "G이고 직접 초기화할 때마다 2배가 됩니다. 자동 초기화가 일어나면 첫 비용으로 돌아갑니다.", $"Choose Reset Mine at the surface base to pay for a reset. The initial fee is {MineResetService.BaseFeeGold}G and doubles with each manual reset. An automatic reset restores the initial fee."))
                .Data(L("초기화되는 것", "Reset"), resetNow)
                .Data(L("유지되는 것", "Kept"), keepNow)
                .Tip(L("T 키로 남은 시간을 확인하세요. 탐사 시간은 " + resetHours + "으로 다시 시작합니다.", $"Press T to see the remaining time. The timer restarts at {resetHours}."))
                .Link("ctrl.clock", "mech.base");

            // ---------------------------------------------------------------- 자원
            Resource("res.copper", ItemDisplayNames.LocalizedMineral(DataIds.Minerals.Copper), L("채굴 · 보관 · 판매", "Mine, store or sell"), DataIds.Minerals.Copper)
                .Link("mech.cargo", "fac.storage");
            Resource("res.iron", ItemDisplayNames.LocalizedMineral(DataIds.Minerals.Iron), L("채굴 · 보관 · 판매", "Mine, store or sell"), DataIds.Minerals.Iron)
                .Link("mech.cargo", "fac.storage");
            Resource("res.lithium", ItemDisplayNames.LocalizedMineral(DataIds.Minerals.Lithium), L("채굴 · 보관 · 판매", "Mine, store or sell"), DataIds.Minerals.Lithium)
                .Data(L("심층 해금", "Deep Zone Access"), L(requirements + " 조건을 모두 갖추면 해금됩니다.", $"Meet the requirements ({requirements}) to unlock the deep zone."))
                .Link("mech.cargo", "fac.storage");
            Resource("res.fuel", ItemDisplayNames.LocalizedMineral(DataIds.RareItems.EngineFuel), L("희귀 · 지상에서 판매", "Rare item, sold at the surface"), DataIds.RareItems.EngineFuel)
                .Link("fac.storage", "fac.settlement");
            Resource("res.gold", L("골드", "Gold"), L("판매·초기화·구출 비용", "Pays for sales-related costs, resets and rescue"), DataIds.Currency.Gold)
                .Data(L("얻는 방법", "How to Obtain"), L("자원을 판매하거나 금이 박힌 블록(금맥 칸)을 채굴하면 얻습니다.", "Sell resources or mine a gold-bearing block."))
                .Data(L("쓰이는 곳", "Uses"), L("새 광산 초기화 · 긴급 구출 · " + portal + " 이용 비용", "Pays for mine resets, emergency rescue and emergency portals."))
                .Tip(L("골드는 화물 무게에 포함되지 않습니다.", "Gold does not count toward cargo weight."))
                .Link("mech.reset", "mech.power", "fac.settlement");

            // ---------------------------------------------------------------- 시설
            Facility("fac.storage", storage, L("자원을 맡기거나 꺼내기", "Deposit or withdraw resources"), "E", DataIds.Buildings.StorageBasic)
                .Step(L("열기", "Open"), L("가까이에서 E 키를 누르면 보관함 창이 열립니다.", "Press E near the chest to open it."))
                .Step(L("보관·꺼내기", "Transfer"), L("자원과 수량을 고른 뒤 보관하거나 꺼냅니다.", "Choose a resource and an amount, then deposit or withdraw."))
                .Data(L("연결 조건", "Power"), L("전력망 연결 없이 가까이에서 쓸 수 있습니다.", "Use nearby without a power connection."))
                .Data(L("수량", "Quantity"), L("요청한 수량이 가진·보관한 수량보다 많으면 있는 만큼만 옮깁니다. 꺼낼 때 화물 무게 한도를 넘으면 꺼내지 못합니다.", "A request above the available quantity transfers only what is available. Cargo capacity limits withdrawals."))
                .Tip(L("보관함의 자원은 탐사 실패·구출 비용에서 제외됩니다.", "Stored resources are safe from loss on expedition failure or rescue."))
                .Data(L("미니맵 아이콘", "Minimap Icon"), L("주황색 정육면체 모양으로, 1×1칸 크기로 표시됩니다.", "Orange cube, one tile wide and tall."))
                .Link("mech.cargo", "ctrl.interact");

            Facility("fac.charger", charger, L("탐사 전력 충전", "Recharge your power"), "E", DataIds.Buildings.ChargerBasic)
                .Data(L("효과", "Effect"), L("전력을 가득 채웁니다.", "Refills your power."))
                .Data(L("연결 조건", "Power"), L("엘리베이터나 " + core + "의 전력 공급 범위 안에서만 쓸 수 있습니다.", "Use within range of an elevator or outpost power supply."))
                .Data(L("재사용", "Cooldown"), L("한 번 쓰면 " + cooldown + " 동안 다시 쓸 수 없습니다.", $"Wait {cooldown} between uses."))
                .Data(L("미니맵 아이콘", "Minimap Icon"), L("노란색 번개 모양입니다. 전력을 받지 못하면 흐리게 표시됩니다.", "Yellow lightning bolt. Dim when unpowered."))
                .Link("ctrl.interact", "mech.grid", "mech.power");

            Facility("fac.clinic", clinic, L("체력 회복", "Restore health"), "E", DataIds.Buildings.ClinicBasic)
                .Data(L("효과", "Effect"), L("체력을 최대치까지 회복합니다.", "Restores you to full health."))
                .Data(L("연결 조건", "Power"), L("엘리베이터나 " + core + "의 전력 공급 범위 안에서만 쓸 수 있습니다.", "Use within range of an elevator or outpost power supply."))
                .Data(L("재사용", "Cooldown"), L("한 번 쓰면 " + cooldown + " 동안 다시 쓸 수 없습니다.", $"Wait {cooldown} between uses."))
                .Data(L("미니맵 아이콘", "Minimap Icon"), L("초록색 십자 모양입니다. 전력을 받지 못하면 흐리게 표시됩니다.", "Green cross. Dim when unpowered."))
                .Link("ctrl.interact", "mech.grid", "mech.health");

            Facility("fac.settlement", settlement, L("보유 자원 판매", "Sell carried resources"), "E", DataIds.Buildings.SettlementBasic)
                .Data(L("효과", "Effect"), L("가진 광물을 골드로 정산합니다. 정산할 자원을 고르는 판매 창이 열립니다.", "Opens a window to choose minerals to sell for gold."))
                .Data(L("연결 조건", "Power"), L("엘리베이터나 " + core + "의 전력 공급 범위 안에서만 쓸 수 있습니다.", "Use within range of an elevator or outpost power supply."))
                .Data(L("제외", "Exceptions"), L(ItemDisplayNames.LocalizedMineral(DataIds.RareItems.EngineFuel) + " 같은 희귀 품목은 정산할 수 없고 지상 기지에서 판매합니다.", "Sell rare items such as engine fuel at the surface base."))
                .Data(L("미니맵 아이콘", "Minimap Icon"), L("보라색 코인 모양으로, 가로 1칸 × 세로 2칸 크기로 표시됩니다. 전력을 받지 못하면 흐리게 표시됩니다.", "Purple coin, one tile wide and two tall. Dim when unpowered."))
                .Link("ctrl.interact", "res.fuel", "mech.grid");

            Facility("fac.core", core, L("기지 핵심 시설", "Main outpost facility"), "E", DataIds.Buildings.OutpostCoreBasic)
                .Data(L("효과", "Effect"), L("주변 시설에 전력을 공급하고 유독 가스 정화 안전지대를 만들며 탐사 체크포인트가 됩니다.", "Powers nearby facilities, creates a gas purification zone and serves as a checkpoint."))
                .Data(L("사용", "Use"), L("가까이에서 E 키로 CCTV 창을 열어 연결된 시설을 확인합니다.", "Press E nearby to view connected facilities on CCTV."))
                .Data(L("미니맵 아이콘", "Minimap Icon"), L("청록색 육각형 모양입니다.", "Teal hexagon."))
                .Link("mech.core", "mech.grid", "mech.gas");

            Facility("fac.ladder", ladder, L("다른 높이로 이동", "Climb between levels"), "W S", DataIds.Buildings.LadderBasic)
                .Data(L("사용", "Use"), L("닿은 상태에서 위·아래 이동 키로 오르내립니다. E 키는 쓰지 않습니다.", "Press the up or down movement key while touching the ladder. E is unnecessary."))
                .Tip(L("사다리를 오를 때는 화물 무게에 따른 산소 소모 증가에 상한이 있습니다.", "Climbing caps the increase in oxygen consumption caused by cargo weight."))
                .Link("ctrl.ladder", "ctrl.build", "mech.oxygen");

            Facility("fac.support", support, L("높이를 바꾸는 착지 발판", "Adjustable landing platform"), string.Empty, DataIds.Buildings.SupportBasic)
                .Data(L("효과", "Effect"), L(support + " 위에 착지하게 될 경우, 피해가 10%까지 줄어들게 됩니다.", "Landing on a cushion pad reduces damage to 10% of normal."))
                .Data(L("높이 조절", "Adjust Height"), L(support + " 위에 서서 위/아래 방향키를 누르면 한 단계씩 높이거나 낮춥니다. 처음에는 가장 낮아 짐을 가득 싣고도 뛰어올라 탈 수 있습니다.", "Stand on the pad and press Up or Down to adjust it one step at a time. It starts at the lowest height so you can jump onto it with a full load."))
                .Link("mech.landing", "ctrl.build");

            Facility("fac.light", light, L("지하 구역을 밝힘", "Light an underground area"), string.Empty, DataIds.Buildings.LightBasic)
                .Data(L("효과", "Effect"), L("전력이 연결된 구역을 밝힙니다.", "Lights the area when powered."))
                .Data(L("미니맵 아이콘", "Minimap Icon"), L("노란색 전구 모양으로, 세로로 긴 1×2칸 크기로 표시됩니다. 전력을 받지 못하면 흐리게 표시됩니다.", "Yellow bulb, one tile wide and two tall. Dim when unpowered."))
                .Link("ctrl.build", "mech.grid");

            Facility("fac.portal", portal, L("긴급 이동 시설", "Emergency transport"), "E", DataIds.Buildings.EmergencyEscapePortal)
                .Step(L("열기", "Open"), L("전력망에 등록된 포탈 가까이에서 E 키를 누릅니다.", "Press E near a powered portal."))
                .Step(L("목적지 선택", "Choose a Destination"), L("엘리베이터 또는 설치한 " + core + " 중에서 고릅니다.", "Select the elevator or a built outpost."))
                .Step(L("비용 확인 후 이동", "Confirm and Travel"), L("비용을 확인하고 실행하면 선택한 곳으로 이동합니다.", "Check the cost and confirm to travel to the selected destination."))
                .Data(L("비용", "Cost"), L("골드 " + EmergencyEscapeService.GoldCost + "G와 최대 전력의 "
                    + Math.Round(EmergencyEscapeService.MaximumEnergyCostRatio * 100d).ToString("0", CultureInfo.InvariantCulture) + "%", $"{EmergencyEscapeService.GoldCost}G and {Math.Round(EmergencyEscapeService.MaximumEnergyCostRatio * 100d):0}% of maximum power."))
                .Data(L("미니맵 아이콘", "Minimap Icon"), L("분홍색 웜홀 모양으로, 안쪽 소용돌이가 천천히 돕니다. 2×2칸 크기로 표시됩니다.", "Pink wormhole with a slow swirl, two tiles wide and tall."))
                .Link("ctrl.interact", "mech.core");

            Add(new GuideCardDef("fac.elevator", GuideTabKind.Resources, GuideCardKind.Facility, L("엘리베이터", "Elevator"), L("지상 기지로 귀환", "Return to the surface base"), "E", "fac.elevator"))
                .Data(L("사용", "Use"), L("광산의 엘리베이터 안에서 E 키를 누르면 지상 기지로 귀환합니다. " + elevatorEnergyText, $"Press E while inside the mine elevator to return to the surface base. {elevatorEnergyText}"))
                .Data(L("전력 공급", "Power Supply"), L("엘리베이터 주변에는 전력이 공급됩니다. " + charger + "·" + clinic + "·" + settlement + "를 가까이 두면 쓸 수 있습니다.", "The area around the elevator powers chargers, clinics and settlement terminals."))
                .Data(L("미니맵 아이콘", "Minimap Icon"), L("하늘색 위·아래 화살표 모양입니다. 승강로를 따라 화살표가 세로로 늘어서 표시됩니다.", "Light blue up and down arrows stacked along the shaft."))
                .Link("ctrl.elevator", "mech.grid");

            // ---------------------------------------------------------------- 첫 탐사 안내
            firstSteps = new List<GuideFirstStep>
            {
                new GuideFirstStep("01", L("지하로 이동", "Go Underground"), L("지상 기지에서 지하 탐사 시작", "Start an expedition from the surface."), "first.enter", "icon.mine", "mech.base"),
                new GuideFirstStep("02", L("자원 채굴", "Mine Resources"), L("인접한 광물을 채굴", "Mine nearby ores."), "first.mine", "icon.copper", "ctrl.mine"),
                new GuideFirstStep("03", L("상태 확인", "Check Status"), L("체력·전력·화물을 확인", "Watch health, power and cargo."), "first.status", "icon.cargo", "mech.health"),
                new GuideFirstStep("04", L("지상으로 귀환", "Return to the Surface"), L("위험해지기 전에 돌아오기", "Head back before conditions get dangerous."), "first.return", "icon.elevator", "ctrl.elevator"),
                new GuideFirstStep("05", L("판매·업그레이드", "Sell & Upgrade"), L("지상 기지에서 자원을 팔고 다음 탐사 준비", "Sell resources and prepare for the next trip."), "first.sell", "icon.sell", "mech.base")
            };
        }
    }
}

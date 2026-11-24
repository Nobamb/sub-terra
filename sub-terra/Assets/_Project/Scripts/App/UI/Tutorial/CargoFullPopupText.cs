using static SubTerra.Shared.Localization.LocalizationService;
using System.Text;
using SubTerra.App.Tutorial;
using SubTerra.App.UI.HUD;

namespace SubTerra.App.UI.Tutorial
{
    /// <summary>화물 공간 부족 팝업(B-152) 문구. 무게 표시는 HUD 화물 표시와 같은 형식이다.</summary>
    public static class CargoFullPopupText
    {
        public static string Title => Text("화물 공간 부족", "Cargo Space Needed");
        public static string Guide => Text("보상을 받으려면 화물 공간을 확보하세요.", "Free up cargo space to claim the reward.");
        public static string RewardCaption => Text("퀘스트 보상", "Quest Reward");
        public static string LoadCaption => Text("화물 적재량", "Cargo Load");
        public static string DumpTitle => Text("버릴 자원 선택", "Discard Cargo");
        public static string DumpSubtitle => Text("기존 자원을 버리고 보상받기", "Discard carried resources and claim the reward");
        public static string ForfeitTitle => Text("보상 버리기", "Skip Reward");
        public const string ForfeitSuffix = " 수령 포기";
        public static string Footer => Text("버린 자원은 판매 금액을 받지 않습니다.", "Discarded resources earn no gold.");

        /// <summary>"철 ×2", 여러 종류면 "구리 ×3 · 철 ×2".</summary>
        public static string RewardLabel(QuestReward reward)
        {
            if (reward.IsEmpty)
            {
                return Get("quest.reward_none");
            }

            var builder = new StringBuilder();
            Append(builder, Get("quest.reward_copper"), reward.Copper);
            Append(builder, Get("quest.reward_iron"), reward.Iron);
            Append(builder, Get("quest.reward_lithium"), reward.Lithium);
            Append(builder, Get("quest.reward_gold"), reward.Gold);
            return builder.ToString();
        }

        public static string ForfeitSubtitle(QuestReward reward)
        {
            var label = RewardLabel(reward);
            return Text(label + ForfeitSuffix, "Skip " + label);
        }

        public static string LoadLabel(CargoShortfallModel model)
        {
            return HudFormatter.FormatCargoAmount(model.CurrentWeight)
                + " / "
                + HudFormatter.FormatCargoAmount(model.MaxCapacity);
        }

        public static string ShortfallLabel(CargoShortfallModel model)
        {
            return Text("부족한 공간 ", "Space needed: ") + HudFormatter.FormatCargoAmount(model.Shortfall);
        }

        private static void Append(StringBuilder builder, string label, int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            if (builder.Length > 0)
            {
                builder.Append(" · ");
            }

            builder.Append(label).Append(" ×").Append(amount);
        }
    }
}

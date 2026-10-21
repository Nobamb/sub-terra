using System.Text;
using SubTerra.App.Tutorial;
using SubTerra.App.UI.HUD;

namespace SubTerra.App.UI.Tutorial
{
    /// <summary>화물 공간 부족 팝업(B-152) 문구. 무게 표시는 HUD 화물 표시와 같은 형식이다.</summary>
    public static class CargoFullPopupText
    {
        public const string Title = "화물 공간 부족";
        public const string Guide = "보상을 받으려면 화물 공간을 확보하세요.";
        public const string RewardCaption = "퀘스트 보상";
        public const string LoadCaption = "화물 적재량";
        public const string DumpTitle = "버릴 자원 선택";
        public const string DumpSubtitle = "기존 자원을 버리고 보상받기";
        public const string ForfeitTitle = "보상 버리기";
        public const string ForfeitSuffix = " 수령 포기";
        public const string Footer = "버린 자원은 판매 금액을 받지 않습니다.";

        /// <summary>"철 ×2", 여러 종류면 "구리 ×3 · 철 ×2".</summary>
        public static string RewardLabel(QuestReward reward)
        {
            if (reward.IsEmpty)
            {
                return "없음";
            }

            var builder = new StringBuilder();
            Append(builder, "구리", reward.Copper);
            Append(builder, "철", reward.Iron);
            Append(builder, "리튬", reward.Lithium);
            Append(builder, "골드", reward.Gold);
            return builder.ToString();
        }

        public static string ForfeitSubtitle(QuestReward reward)
        {
            return RewardLabel(reward) + ForfeitSuffix;
        }

        public static string LoadLabel(CargoShortfallModel model)
        {
            return HudFormatter.FormatCargoAmount(model.CurrentWeight)
                + " / "
                + HudFormatter.FormatCargoAmount(model.MaxCapacity);
        }

        public static string ShortfallLabel(CargoShortfallModel model)
        {
            return "부족한 공간 " + HudFormatter.FormatCargoAmount(model.Shortfall);
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

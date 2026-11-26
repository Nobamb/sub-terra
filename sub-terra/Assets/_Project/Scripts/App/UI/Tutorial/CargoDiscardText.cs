using static SubTerra.Shared.Localization.LocalizationService;
using SubTerra.App.Tutorial;
using SubTerra.App.UI.HUD;

namespace SubTerra.App.UI.Tutorial
{
    /// <summary>화물 버리기 창(B-153)에서 버튼 하나가 버리는 양.</summary>
    public enum CargoDiscardKind
    {
        One = 0,
        Five = 1,
        Ten = 2,
        Needed = 3,
        All = 4
    }

    /// <summary>화물 버리기 창(B-153) 문구. 무게는 화물 부족 창과 같은 HUD 형식이다.</summary>
    public static class CargoDiscardText
    {
        public static string Title => Text("화물 버리기", "Discard Cargo");
        public static string RewardCaption => Text("받을 보상", "Pending Reward");
        public static string BackTitle => Text("돌아가기", "Back");
        public static string BackSubtitle => Text("화물 부족 창으로", "Return to cargo space warning");
        public static string Cleared => Text("공간 확보 완료", "Space available");
        public const string Minus = "−";

        public static readonly CargoDiscardKind[] Kinds =
        {
            CargoDiscardKind.One,
            CargoDiscardKind.Five,
            CargoDiscardKind.Ten,
            CargoDiscardKind.Needed,
            CargoDiscardKind.All
        };

        /// <summary>B-152·B-153 창이 쓰는 모든 글자. 폰트 에셋에 미리 넣고, 테스트가 빠진 글자를 찾는다.</summary>
        public static string FontCharacters
        {
            get
            {
                var characters = string.Empty;
                PreviewLanguage("ko", () => characters = CurrentFontCharacters);
                PreviewLanguage("en", () => characters += CurrentFontCharacters);
                return characters;
            }
        }

        private static string CurrentFontCharacters =>
            CargoFullPopupText.Title
            + CargoFullPopupText.Guide
            + CargoFullPopupText.RewardCaption
            + CargoFullPopupText.LoadCaption
            + CargoFullPopupText.DumpTitle
            + CargoFullPopupText.DumpSubtitle
            + CargoFullPopupText.ForfeitTitle
            + CargoFullPopupText.ForfeitSuffix
            + CargoFullPopupText.Footer
            + CargoFullPopupText.ShortfallLabel(CargoShortfallModel.From(1f, 0f, 0f))
            + CargoFullPopupText.RewardLabel(new QuestReward(1, 1, 1, 1))
            + CargoFullPopupText.RewardLabel(QuestReward.None)
            + Title
            + RewardCaption
            + BackTitle
            + BackSubtitle
            + Cleared
            + ButtonLabel(CargoDiscardKind.One)
            + ButtonLabel(CargoDiscardKind.Five)
            + ButtonLabel(CargoDiscardKind.Ten)
            + ButtonLabel(CargoDiscardKind.Needed)
            + ButtonLabel(CargoDiscardKind.All)
            + Detail(1f, 1)
            + NeededLabel(new CargoDiscardNeed(1, true, 0f, 1f))
            + NeededLabel(new CargoDiscardNeed(1, false, 1f, 1f))
            + "0123456789./×·−() ";

        public static string ButtonLabel(CargoDiscardKind kind)
        {
            switch (kind)
            {
                case CargoDiscardKind.One:
                    return Text("1개", "1 item");
                case CargoDiscardKind.Five:
                    return Text("5개", "5 items");
                case CargoDiscardKind.Ten:
                    return Text("10개", "10 items");
                case CargoDiscardKind.Needed:
                    return Text("필요", "Needed");
                default:
                    return Text("전부", "All");
            }
        }

        /// <summary>1개·5개·10개 요청 수. 필요·전부는 보유·부족에 따라 정한다.</summary>
        public static int FixedCount(CargoDiscardKind kind)
        {
            switch (kind)
            {
                case CargoDiscardKind.One:
                    return 1;
                case CargoDiscardKind.Five:
                    return 5;
                case CargoDiscardKind.Ten:
                    return 10;
                case CargoDiscardKind.All:
                    return int.MaxValue;
                default:
                    return 0;
            }
        }

        public static string Quantity(int quantity)
        {
            return "×" + (quantity < 0 ? 0 : quantity);
        }

        /// <summary>"개당 2 · 합계 160".</summary>
        public static string Detail(float unitWeight, int quantity)
        {
            return Text("개당 ", "Each ")
                + HudFormatter.FormatCargoAmount(CargoDiscardMath.Round2(unitWeight))
                + Text(" · 합계 ", " · Total ")
                + HudFormatter.FormatCargoAmount(CargoDiscardMath.Weight(quantity, unitWeight));
        }

        /// <summary>1개·5개·10개·전부 아래 "−3". 버릴 것이 없으면 빈 문자열.</summary>
        public static string Freed(int count, float unitWeight)
        {
            return count > 0 ? Minus + HudFormatter.FormatCargoAmount(CargoDiscardMath.Weight(count, unitWeight)) : string.Empty;
        }

        /// <summary>필요 아래 "−10 (5개)", 보유를 다 버려도 모자라면 "전부 버려도 1.5 부족".</summary>
        public static string NeededLabel(CargoDiscardNeed need)
        {
            if (need.Count <= 0)
            {
                return string.Empty;
            }

            if (!need.Covers)
            {
                var remaining = HudFormatter.FormatCargoAmount(need.Remaining);
                return Text($"전부 버려도 {remaining} 부족", $"Still short by {remaining}");
            }

            return Minus + HudFormatter.FormatCargoAmount(need.Freed) + " (" + Text(need.Count + "개", need.Count.ToString()) + ")";
        }
    }
}

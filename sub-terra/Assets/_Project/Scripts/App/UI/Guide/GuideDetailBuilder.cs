using System.Collections.Generic;
using System.Globalization;
using System.Text;
using SubTerra.App.Core.Data;
using UnityEngine;

namespace SubTerra.App.UI.Guide
{
    /// <summary>상세 패널이 그대로 그리는 결과물.</summary>
    public sealed class GuideDetailModel
    {
        public string CardId { get; set; }
        public string Title { get; set; }
        public string Keys { get; set; }
        public string DemoId { get; set; }
        public Sprite Icon { get; set; }
        public List<GuideLine> Lines { get; } = new List<GuideLine>();
        public List<GuideCardDef> Related { get; } = new List<GuideCardDef>();
    }

    /// <summary>
    /// 정적 카드 정의에 카탈로그 값(무게·가격·전력 요구·건설 재료)을 더해 상세 내용을 만든다.
    /// 값은 모두 IGuideData에서 읽으므로 가이드에 숫자를 따로 박지 않는다.
    /// </summary>
    public static class GuideDetailBuilder
    {
        public static GuideDetailModel Build(GuideCardDef card, IGuideData data)
        {
            var model = new GuideDetailModel
            {
                CardId = card.Id,
                Title = card.Title,
                Keys = card.Keys,
                DemoId = card.DemoId
            };

            var dynamicLines = new List<GuideLine>();
            if (card.Kind == GuideCardKind.Resource && data != null)
            {
                AddItemLines(card, data, dynamicLines, model);
            }
            else if (card.Kind == GuideCardKind.Facility && data != null)
            {
                AddFacilityLines(card, data, dynamicLines, model);
            }
            else if (card.Id == "mech.grid" && data != null)
            {
                AddGridLines(data, dynamicLines);
            }

            var inserted = false;
            for (var i = 0; i < card.Lines.Count; i++)
            {
                var line = card.Lines[i];
                if (!inserted && (line.Kind == GuideLineKind.Tip || line.Kind == GuideLineKind.Warning))
                {
                    model.Lines.AddRange(dynamicLines);
                    inserted = true;
                }

                model.Lines.Add(line);
            }

            if (!inserted)
            {
                model.Lines.AddRange(dynamicLines);
            }

            for (var i = 0; i < card.Related.Count; i++)
            {
                if (GameGuideCatalog.TryGet(card.Related[i], out var related))
                {
                    model.Related.Add(related);
                }
            }

            return model;
        }

        private static void AddItemLines(GuideCardDef card, IGuideData data, List<GuideLine> lines, GuideDetailModel model)
        {
            if (!data.TryGetItem(card.SourceId, out var item))
            {
                return;
            }

            model.Icon = item.Icon;
            var isMineral = !item.IsRare;
            lines.Add(new GuideLine(GuideLineKind.Data, "무게·가격",
                "개당 무게 " + Number(item.UnitWeight) + " · 개당 판매가 " + item.UnitPrice + "G"));

            if (isMineral)
            {
                lines.Add(new GuideLine(GuideLineKind.Data, "얻는 방법", "광석 블록을 채굴해 얻습니다."));
                var uses = new List<string>();
                var buildings = BuildingsUsing(data, item.Name);
                if (buildings.Count > 0)
                {
                    uses.Add("시설 건설 재료(" + string.Join(", ", buildings) + ")");
                }

                if (data.IsUpgradeMaterial(card.SourceId))
                {
                    uses.Add("업그레이드 재료");
                }

                uses.Add("판매");
                lines.Add(new GuideLine(GuideLineKind.Data, "용도", string.Join(" · ", uses)));
                lines.Add(new GuideLine(GuideLineKind.Data, "보관·판매",
                    "보관함에서 보관하고 꺼낼 수 있습니다. 정산 콘솔과 지상 기지 판매 창에서 판매합니다."));
            }
            else
            {
                lines.Add(new GuideLine(GuideLineKind.Data, "얻는 방법",
                    "광산의 희귀 신호 칸을 채굴하면 얻습니다. Digger-Bot이 '희귀 신호'로 알려 줍니다."));
                lines.Add(new GuideLine(GuideLineKind.Data, "보관·판매",
                    "보관함에 보관할 수 있습니다. 정산 콘솔에서는 정산할 수 없고, 지상 기지 판매 창에서 수량을 직접 정해 판매합니다."));
                lines.Add(new GuideLine(GuideLineKind.Tip, string.Empty,
                    "판매 창의 '최대 선택'에서는 제외되므로 필요한 수량을 직접 지정하세요."));
            }
        }

        private static void AddFacilityLines(GuideCardDef card, IGuideData data, List<GuideLine> lines, GuideDetailModel model)
        {
            if (string.IsNullOrEmpty(card.SourceId) || !data.TryGetBuilding(card.SourceId, out var building))
            {
                return;
            }

            model.Icon = building.Icon;
            if (building.Costs != null && building.Costs.Count > 0)
            {
                var parts = new List<string>();
                for (var i = 0; i < building.Costs.Count; i++)
                {
                    parts.Add(building.Costs[i].ItemName + " " + building.Costs[i].Quantity);
                }

                lines.Add(new GuideLine(GuideLineKind.Data, "건설 재료", string.Join(" · ", parts)));
            }

            lines.Add(new GuideLine(GuideLineKind.Data, "전력",
                building.PowerDraw > 0 ? "전력망 연결이 필요한 시설입니다." : "전력이 필요 없는 시설입니다."));
        }

        private static void AddGridLines(IGuideData data, List<GuideLine> lines)
        {
            var needs = new List<string>();
            var free = new List<string>();
            for (var i = 0; i < data.Buildings.Count; i++)
            {
                var building = data.Buildings[i];
                (building.PowerDraw > 0 ? needs : free).Add(building.Name);
            }

            if (needs.Count > 0)
            {
                lines.Add(new GuideLine(GuideLineKind.Data, "전력이 필요한 시설", string.Join(" · ", needs)));
            }

            if (free.Count > 0)
            {
                lines.Add(new GuideLine(GuideLineKind.Data, "전력이 필요 없는 시설", string.Join(" · ", free)));
            }
        }

        private static List<string> BuildingsUsing(IGuideData data, string itemName)
        {
            var names = new List<string>();
            for (var i = 0; i < data.Buildings.Count; i++)
            {
                var building = data.Buildings[i];
                for (var c = 0; building.Costs != null && c < building.Costs.Count; c++)
                {
                    if (building.Costs[c].ItemName == itemName)
                    {
                        names.Add(building.Name);
                        break;
                    }
                }
            }

            return names;
        }

        private static string Number(float value)
        {
            return value.ToString("0.##", CultureInfo.InvariantCulture);
        }

        /// <summary>플레인 텍스트 한 덩어리(테스트·접근성).</summary>
        public static string ToPlainText(GuideDetailModel model)
        {
            var builder = new StringBuilder();
            builder.Append(model.Title).Append('\n');
            for (var i = 0; i < model.Lines.Count; i++)
            {
                if (!string.IsNullOrEmpty(model.Lines[i].Label))
                {
                    builder.Append(model.Lines[i].Label).Append(": ");
                }

                builder.Append(model.Lines[i].Text).Append('\n');
            }

            return builder.ToString();
        }
    }
}

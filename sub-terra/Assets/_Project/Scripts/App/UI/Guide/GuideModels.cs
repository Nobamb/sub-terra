using System;
using System.Collections.Generic;

namespace SubTerra.App.UI.Guide
{
    public enum GuideTabKind
    {
        Controls = 0,
        Mechanics = 1,
        Resources = 2
    }

    public enum GuideCardKind
    {
        Control = 0,
        Mechanic = 1,
        Resource = 2,
        Facility = 3
    }

    public enum GuideFilter
    {
        All = 0,
        Resources = 1,
        Facilities = 2
    }

    public enum GuideLineKind
    {
        /// <summary>번호가 붙은 순서 한 줄. Label은 짧은 제목.</summary>
        Step = 0,
        Text = 1,
        Tip = 2,
        Warning = 3,
        /// <summary>왼쪽 라벨 + 오른쪽 설명 한 줄.</summary>
        Data = 4
    }

    public readonly struct GuideLine
    {
        public GuideLineKind Kind { get; }
        public string Label { get; }
        public string Text { get; }

        public GuideLine(GuideLineKind kind, string label, string text)
        {
            Kind = kind;
            Label = label ?? string.Empty;
            Text = text ?? string.Empty;
        }
    }

    /// <summary>카드 한 장의 정의. 표시 로직과 분리된 순수 데이터다.</summary>
    public sealed class GuideCardDef
    {
        public string Id { get; }
        public GuideTabKind Tab { get; }
        public GuideCardKind Kind { get; }
        public string Title { get; }
        public string Summary { get; }
        /// <summary>키캡 표기 문자열. GuideKeyTokens 문법(공백 구분).</summary>
        public string Keys { get; }
        public string DemoId { get; }
        /// <summary>카탈로그 연결용 ID(아이템 또는 시설). 비어 있으면 정적 카드.</summary>
        public string SourceId { get; }
        public IReadOnlyList<GuideLine> Lines => lines;
        public IReadOnlyList<string> Related => related;

        private readonly List<GuideLine> lines = new List<GuideLine>();
        private readonly List<string> related = new List<string>();

        public GuideCardDef(
            string id,
            GuideTabKind tab,
            GuideCardKind kind,
            string title,
            string summary,
            string keys,
            string demoId,
            string sourceId = "")
        {
            Id = id;
            Tab = tab;
            Kind = kind;
            Title = title;
            Summary = summary ?? string.Empty;
            Keys = keys ?? string.Empty;
            DemoId = demoId ?? string.Empty;
            SourceId = sourceId ?? string.Empty;
        }

        public bool IsResourceTabItem => Kind == GuideCardKind.Resource || Kind == GuideCardKind.Facility;

        public GuideCardDef Step(string label, string text)
        {
            lines.Add(new GuideLine(GuideLineKind.Step, label, text));
            return this;
        }

        public GuideCardDef Text(string text)
        {
            lines.Add(new GuideLine(GuideLineKind.Text, string.Empty, text));
            return this;
        }

        public GuideCardDef Data(string label, string text)
        {
            lines.Add(new GuideLine(GuideLineKind.Data, label, text));
            return this;
        }

        public GuideCardDef Tip(string text)
        {
            lines.Add(new GuideLine(GuideLineKind.Tip, string.Empty, text));
            return this;
        }

        public GuideCardDef Warning(string text)
        {
            lines.Add(new GuideLine(GuideLineKind.Warning, string.Empty, text));
            return this;
        }

        public GuideCardDef Link(params string[] ids)
        {
            related.AddRange(ids);
            return this;
        }
    }

    /// <summary>첫 탐사 안내의 한 단계.</summary>
    public sealed class GuideFirstStep
    {
        public string Number { get; }
        public string Title { get; }
        public string Caption { get; }
        public string DemoId { get; }
        public string IconKey { get; }
        public string TargetCardId { get; }

        public GuideFirstStep(string number, string title, string caption, string demoId, string iconKey, string targetCardId)
        {
            Number = number;
            Title = title;
            Caption = caption;
            DemoId = demoId;
            IconKey = iconKey;
            TargetCardId = targetCardId;
        }
    }

    public enum GuideKeyTokenKind
    {
        Key = 0,
        Mouse = 1,
        Slash = 2,
        Arrow = 3,
        Plus = 4
    }

    public readonly struct GuideKeyToken
    {
        public GuideKeyTokenKind Kind { get; }
        public string Label { get; }

        public GuideKeyToken(GuideKeyTokenKind kind, string label)
        {
            Kind = kind;
            Label = label ?? string.Empty;
        }
    }

    /// <summary>"A D / ← →", "MOUSE / Enter", "B -> C", "Ctrl + M" 형식을 키캡 토큰으로 푼다.</summary>
    public static class GuideKeyTokens
    {
        public static List<GuideKeyToken> Parse(string spec)
        {
            var result = new List<GuideKeyToken>();
            if (string.IsNullOrWhiteSpace(spec))
            {
                return result;
            }

            var parts = spec.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            for (var i = 0; i < parts.Length; i++)
            {
                switch (parts[i])
                {
                    case "MOUSE":
                        result.Add(new GuideKeyToken(GuideKeyTokenKind.Mouse, string.Empty));
                        break;
                    case "/":
                        result.Add(new GuideKeyToken(GuideKeyTokenKind.Slash, "/"));
                        break;
                    case "->":
                        result.Add(new GuideKeyToken(GuideKeyTokenKind.Arrow, "→"));
                        break;
                    case "+":
                        result.Add(new GuideKeyToken(GuideKeyTokenKind.Plus, "+"));
                        break;
                    default:
                        result.Add(new GuideKeyToken(GuideKeyTokenKind.Key, parts[i]));
                        break;
                }
            }

            return result;
        }
    }
}

using System;
using System.Collections.Generic;
using SubTerra.App.Run;
using SubTerra.Shared;

namespace SubTerra.App.UI.EmergencyEscape
{
    /// <summary>목적지 한 줄: 표시 이름, 이동 가능 여부, 이동할 수 없을 때 짧은 이유.</summary>
    public readonly struct EscapeDestinationRow
    {
        public EmergencyEscapeDestinationOption Option { get; }
        public bool Enabled { get; }
        public string Reason { get; }

        public EscapeDestinationRow(EmergencyEscapeDestinationOption option, bool enabled, string reason)
        {
            Option = option;
            Enabled = enabled;
            Reason = reason ?? string.Empty;
        }
    }

    /// <summary>
    /// 항상 펼쳐진 목적지 목록의 순수 모델. 창을 열 때마다 현재 목적지 목록으로 새로 만든다.
    /// 이동할 수 없는 항목도 선택(CCTV 미리보기)은 되지만 이동 버튼은 막는다.
    /// </summary>
    public sealed class EscapeDestinationListModel
    {
        public const string GoldShortReason = "골드 부족";
        public const string EnergyShortReason = "전력 부족";
        public const string UnavailableReason = "이용 불가";

        private readonly List<EscapeDestinationRow> rows = new List<EscapeDestinationRow>();

        public IReadOnlyList<EscapeDestinationRow> Rows => rows;
        public int Count => rows.Count;
        public int SelectedIndex { get; private set; }

        public static EscapeDestinationListModel Build(
            IReadOnlyList<EmergencyEscapeDestinationOption> options,
            EmergencyEscapePaymentFailure failure)
        {
            var model = new EscapeDestinationListModel();
            var reason = ReasonFor(failure);
            var enabled = failure == EmergencyEscapePaymentFailure.None;
            if (options != null)
            {
                for (var i = 0; i < options.Count; i++)
                {
                    model.rows.Add(new EscapeDestinationRow(options[i], enabled, enabled ? string.Empty : reason));
                }
            }

            return model;
        }

        public static string ReasonFor(EmergencyEscapePaymentFailure failure)
        {
            switch (failure)
            {
                case EmergencyEscapePaymentFailure.None:
                    return string.Empty;
                case EmergencyEscapePaymentFailure.InsufficientGold:
                    return GoldShortReason;
                case EmergencyEscapePaymentFailure.InsufficientEnergy:
                    return EnergyShortReason;
                default:
                    return UnavailableReason;
            }
        }

        public void Select(int index)
        {
            SelectedIndex = rows.Count == 0 ? 0 : Math.Min(Math.Max(index, 0), rows.Count - 1);
        }

        /// <summary>위·아래 키 이동. 끝에서 반대쪽 끝으로 넘어가지 않고 멈춘다.</summary>
        public bool Step(int delta)
        {
            if (rows.Count == 0)
            {
                return false;
            }

            var before = SelectedIndex;
            Select(SelectedIndex + delta);
            return SelectedIndex != before;
        }

        public bool TryGetSelected(out EscapeDestinationRow row)
        {
            if (rows.Count == 0)
            {
                row = default;
                return false;
            }

            row = rows[SelectedIndex];
            return true;
        }

        /// <summary>선택한 항목으로 지금 이동할 수 있는지.</summary>
        public bool CanTravel => TryGetSelected(out var row) && row.Enabled;
    }
}

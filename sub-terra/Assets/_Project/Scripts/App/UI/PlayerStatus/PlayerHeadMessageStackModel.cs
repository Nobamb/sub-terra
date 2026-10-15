using System;
using System.Collections.Generic;
using SubTerra.App.UI.FacilityNameTag;

namespace SubTerra.App.UI.PlayerStatus
{
    public enum PlayerStatusStyle
    {
        /// <summary>청록 정보 문구(채굴 진행·결과).</summary>
        Info,
        /// <summary>노랑 경고 문구(실패·사용불가).</summary>
        Warning
    }

    /// <summary>
    /// 머리 위 항목 하나. 문구 항목(메시지)과 골드 팝업 자리(슬롯)가 같은 목록에서 도착 순서대로 쌓인다.
    /// 슬롯은 그림을 그리지 않고 높이만 차지한다.
    /// </summary>
    public sealed class PlayerStatusEntry
    {
        private float slideFrom;
        private float slideTo;
        private float slideElapsed;

        internal PlayerStatusEntry(string key, PlayerStatusStyle style, bool isSlot, long order)
        {
            Key = key;
            Style = style;
            IsSlot = isSlot;
            Order = order;
        }

        public string Key { get; }
        public string Text { get; private set; } = string.Empty;
        public PlayerStatusStyle Style { get; }
        public bool IsSlot { get; }
        /// <summary>도착 순서. 작을수록 먼저 떴고 스택에서 아래에 있다.</summary>
        public long Order { get; }
        /// <summary>차지하는 높이(디자인 픽셀).</summary>
        public float Height { get; private set; }
        /// <summary>진행률 0~1. 음수면 게이지 없음.</summary>
        public float Progress { get; private set; } = -1f;
        /// <summary>표시 방향. false면 퇴장 중이다.</summary>
        public bool Wanted { get; private set; } = true;
        /// <summary>등장·퇴장 진행(0~1). 시설 이름표 시간표를 그대로 쓴다.</summary>
        public float Level { get; private set; }
        /// <summary>남은 표시 시간(초). 무한대면 진행 항목처럼 직접 내리기 전까지 유지된다.</summary>
        public float Hold { get; private set; } = float.PositiveInfinity;
        /// <summary>표시 방향이 켜진 뒤 흐른 시간. 경고 깜박임에 쓴다.</summary>
        public float ShownFor { get; private set; }
        /// <summary>현재 세로 위치(디자인 픽셀, 스택 기준점에서 위로).</summary>
        public float Offset { get; private set; }
        /// <summary>퇴장이 끝나 목록에서 빠질 항목.</summary>
        public bool IsFinished => !Wanted && Level <= 0f;

        internal void SetText(string text)
        {
            Text = text ?? string.Empty;
            Height = IsSlot ? Height : FacilityNameTagVisual.HeightFor(Text);
        }

        internal void SetSlotHeight(float height)
        {
            Height = Math.Max(0f, height);
        }

        internal void SetProgress(float progress)
        {
            Progress = progress < 0f ? -1f : Math.Min(1f, progress);
        }

        /// <summary>같은 문구를 다시 받았을 때: 표시 시간을 처음부터 다시 잰다(깜박임은 되돌리지 않는다).</summary>
        internal void Extend(float seconds)
        {
            Hold = seconds;
            Wanted = true;
        }

        internal void Leave()
        {
            Wanted = false;
        }

        /// <summary>목표 위치로 옮긴다. 목표가 바뀌었을 때만 현재 위치에서 다시 슬라이드를 시작한다.</summary>
        internal void MoveTo(float target)
        {
            if (Math.Abs(target - slideTo) < 0.001f)
            {
                return;
            }

            slideFrom = Offset;
            slideTo = target;
            slideElapsed = 0f;
        }

        internal void Place(float target)
        {
            Offset = target;
            slideFrom = target;
            slideTo = target;
            slideElapsed = PlayerStatusTimeline.SlideSeconds;
        }

        /// <summary>dt는 이미 PlayerStatusTimeline.ClampStep을 거친 값이다.</summary>
        internal void Advance(float dt)
        {
            if (Wanted)
            {
                ShownFor += dt;
                if (!IsSlot && !float.IsPositiveInfinity(Hold))
                {
                    Hold -= dt;
                    if (Hold <= 0f)
                    {
                        Leave();
                    }
                }
            }

            Level = FacilityNameTagTimeline.Advance(Level, Wanted, dt);
            slideElapsed += dt;
            Offset = PlayerStatusTimeline.SlideOffset(slideFrom, slideTo, slideElapsed);
        }
    }

    /// <summary>
    /// 플레이어 머리 위 문구 스택의 순수 상태. Unity 객체 없이 도착 순서·중복·상한·슬라이드를 계산한다.
    /// 목록은 도착 순서(오래된 것 먼저)이고, 스택에서는 목록 앞쪽이 아래, 뒤쪽이 위다.
    /// </summary>
    public sealed class PlayerHeadMessageStackModel
    {
        private readonly List<PlayerStatusEntry> entries = new List<PlayerStatusEntry>();
        private readonly List<PlayerStatusEntry> finished = new List<PlayerStatusEntry>();
        private long sequence;

        /// <summary>목록 전체(퇴장 중 포함), 도착 순서.</summary>
        public IReadOnlyList<PlayerStatusEntry> Entries => entries;
        /// <summary>마지막 Advance에서 퇴장을 끝내 목록에서 빠진 항목. 뷰가 그림을 치운다.</summary>
        public IReadOnlyList<PlayerStatusEntry> Finished => finished;
        public int MaxEntries { get; set; } = PlayerStatusTimeline.MaxEntries;

        public PlayerStatusEntry ShowInfo(string key, string text)
        {
            return Show(key, text, PlayerStatusStyle.Info, PlayerStatusTimeline.HoldSeconds);
        }

        public PlayerStatusEntry ShowWarning(string key, string text)
        {
            return Show(key, text, PlayerStatusStyle.Warning, PlayerStatusTimeline.HoldSeconds);
        }

        /// <summary>진행 문구. 시간 제한 없이 떠 있고, 같은 키로 계속 갱신한다.</summary>
        public PlayerStatusEntry SetProgress(string key, string text, float progress)
        {
            var entry = Show(key, text, PlayerStatusStyle.Info, float.PositiveInfinity);
            entry.SetProgress(progress);
            return entry;
        }

        /// <summary>키의 문구를 퇴장시킨다. 없으면 아무것도 하지 않는다.</summary>
        public void Dismiss(string key)
        {
            var entry = FindMessage(key);
            if (entry != null)
            {
                entry.Leave();
            }
        }

        /// <summary>골드 팝업처럼 그림을 따로 그리는 대상이 차지할 높이를 등록하거나 갱신한다.</summary>
        public PlayerStatusEntry RegisterSlot(string key, float heightPixels)
        {
            var slot = FindSlot(key);
            if (slot == null)
            {
                slot = new PlayerStatusEntry(key, PlayerStatusStyle.Info, true, ++sequence);
                slot.Place(ComputeTopTarget());
                entries.Add(slot);
            }

            slot.SetSlotHeight(heightPixels);
            Trim();
            return slot;
        }

        public void UnregisterSlot(string key)
        {
            for (var i = entries.Count - 1; i >= 0; i--)
            {
                if (entries[i].IsSlot && entries[i].Key == key)
                {
                    entries.RemoveAt(i);
                }
            }
        }

        public bool TryGetOffset(string key, out float offset)
        {
            var entry = FindAny(key);
            offset = entry != null ? entry.Offset : 0f;
            return entry != null;
        }

        /// <summary>키 항목의 현재 세로 오프셋(디자인 픽셀). 없으면 0.</summary>
        public float OffsetOf(string key)
        {
            return TryGetOffset(key, out var offset) ? offset : 0f;
        }

        public PlayerStatusEntry Find(string key) => FindAny(key);

        /// <summary>목록을 모두 비운다(스택 비활성화·씬 전환 시).</summary>
        public void Clear()
        {
            entries.Clear();
            finished.Clear();
        }

        /// <summary>
        /// 한 프레임 진행. 목표 위치는 "떠 있는 항목"만으로 아래→위 순서로 다시 계산하므로,
        /// 아래 항목이 퇴장을 시작하는 순간 위 항목이 슬라이드를 시작한다.
        /// </summary>
        public void Advance(float deltaSeconds)
        {
            var dt = PlayerStatusTimeline.ClampStep(deltaSeconds);
            finished.Clear();

            var cursor = 0f;
            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (!entry.Wanted)
                {
                    continue;
                }

                entry.MoveTo(cursor);
                cursor += entry.Height + PlayerStatusTimeline.StackGap;
            }

            for (var i = 0; i < entries.Count; i++)
            {
                entries[i].Advance(dt);
            }

            for (var i = entries.Count - 1; i >= 0; i--)
            {
                if (entries[i].IsFinished)
                {
                    finished.Add(entries[i]);
                    entries.RemoveAt(i);
                }
            }
        }

        private PlayerStatusEntry Show(string key, string text, PlayerStatusStyle style, float hold)
        {
            var existing = FindMessage(key);
            if (existing != null && existing.Style == style)
            {
                // 같은 키·같은 스타일: 자리는 그대로 두고 문구를 바꾸며 시간만 다시 잰다.
                existing.SetText(text);
                existing.Extend(hold);
                return existing;
            }

            if (existing != null)
            {
                // 스타일이 바뀌면(채굴 진행 → 경고) 옛 항목은 퇴장시키고 새 항목을 위에 쌓는다.
                existing.Leave();
            }

            var entry = new PlayerStatusEntry(key, style, false, ++sequence);
            entry.SetText(text);
            entry.Extend(hold);
            // 자기 자신을 넣기 전에 계산해야 맨 위 자리가 된다.
            entry.Place(ComputeTopTarget());
            entries.Add(entry);
            Trim();
            return entry;
        }

        private float ComputeTopTarget()
        {
            var cursor = 0f;
            for (var i = 0; i < entries.Count; i++)
            {
                if (entries[i].Wanted)
                {
                    cursor += entries[i].Height + PlayerStatusTimeline.StackGap;
                }
            }

            return cursor;
        }

        /// <summary>떠 있는 항목이 상한을 넘으면 가장 오래된 문구부터 퇴장시킨다. 골드 슬롯은 밀어내지 않는다.</summary>
        private void Trim()
        {
            var visible = 0;
            for (var i = 0; i < entries.Count; i++)
            {
                if (entries[i].Wanted)
                {
                    visible++;
                }
            }

            for (var i = 0; i < entries.Count && visible > MaxEntries; i++)
            {
                var entry = entries[i];
                if (entry.Wanted && !entry.IsSlot)
                {
                    entry.Leave();
                    visible--;
                }
            }
        }

        private PlayerStatusEntry FindMessage(string key)
        {
            for (var i = 0; i < entries.Count; i++)
            {
                if (!entries[i].IsSlot && entries[i].Wanted && entries[i].Key == key)
                {
                    return entries[i];
                }
            }

            return null;
        }

        private PlayerStatusEntry FindSlot(string key)
        {
            for (var i = 0; i < entries.Count; i++)
            {
                if (entries[i].IsSlot && entries[i].Key == key)
                {
                    return entries[i];
                }
            }

            return null;
        }

        private PlayerStatusEntry FindAny(string key)
        {
            for (var i = entries.Count - 1; i >= 0; i--)
            {
                if (entries[i].Key == key && entries[i].Wanted)
                {
                    return entries[i];
                }
            }

            return null;
        }
    }
}

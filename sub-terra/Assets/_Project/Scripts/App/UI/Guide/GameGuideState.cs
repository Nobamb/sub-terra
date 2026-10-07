using System;
using System.Collections.Generic;
using UnityEngine;

namespace SubTerra.App.UI.Guide
{
    /// <summary>
    /// 가이드 선택 상태: 탭, 탭별 선택 카드, 자원·시설 필터, 첫 탐사 안내 펼침.
    /// 같은 플레이 세션 안에서만 유지되며 저장 항목은 만들지 않는다.
    /// </summary>
    public sealed class GameGuideState
    {
        private static GameGuideState session;
        private static bool introPlayed;

        private readonly Dictionary<GuideTabKind, string> selected = new Dictionary<GuideTabKind, string>();

        public GuideTabKind Tab { get; private set; } = GuideTabKind.Controls;
        public GuideFilter Filter { get; private set; } = GuideFilter.All;
        public bool FirstExploreExpanded { get; private set; } = true;
        /// <summary>목록에서 이 카드를 보이게 스크롤해야 할 때 설정된다(한 번 읽으면 지워진다).</summary>
        public string RevealRequest { get; private set; } = string.Empty;

        public event Action Changed;

        /// <summary>플레이 세션 동안 공유하는 인스턴스.</summary>
        public static GameGuideState Session => session ?? (session = new GameGuideState());

        /// <summary>이번 플레이 세션에서 전체 등장 연출을 이미 보여 줬는지.</summary>
        public static bool IntroPlayed
        {
            get => introPlayed;
            set => introPlayed = value;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetForPlaySession()
        {
            // 도메인 리로드를 끈 에디터에서도 새 플레이마다 처음 상태로 시작한다.
            session = null;
            introPlayed = false;
        }

        public static void ResetSessionForTests()
        {
            session = null;
            introPlayed = false;
        }

        public string SelectedId(GuideTabKind tab)
        {
            if (selected.TryGetValue(tab, out var id) && GameGuideCatalog.TryGet(id, out var card)
                && card.Tab == tab && IsVisible(id))
            {
                return id;
            }

            var first = FirstVisible(tab);
            return first != null ? first.Id : string.Empty;
        }

        public string CurrentCardId => SelectedId(Tab);

        public void SelectTab(GuideTabKind tab)
        {
            if (Tab == tab)
            {
                return;
            }

            Tab = tab;
            RevealRequest = SelectedId(tab);
            Changed?.Invoke();
        }

        public void SetFilter(GuideFilter filter)
        {
            if (Filter == filter)
            {
                return;
            }

            Filter = filter;
            var current = SelectedId(GuideTabKind.Resources);
            if (!IsVisible(current))
            {
                var first = FirstVisible(GuideTabKind.Resources);
                selected[GuideTabKind.Resources] = first != null ? first.Id : string.Empty;
            }

            RevealRequest = SelectedId(GuideTabKind.Resources);
            Changed?.Invoke();
        }

        /// <summary>카드를 선택한다. 다른 탭의 카드면 탭도 옮기고, 필터에 가려지면 필터를 '전체'로 푼다.</summary>
        public bool SelectCard(string cardId)
        {
            if (!GameGuideCatalog.TryGet(cardId, out var card))
            {
                return false;
            }

            if (card.Tab == GuideTabKind.Resources && !IsVisible(card.Id))
            {
                Filter = GuideFilter.All;
            }

            Tab = card.Tab;
            selected[card.Tab] = card.Id;
            RevealRequest = card.Id;
            Changed?.Invoke();
            return true;
        }

        public void ToggleFirstExplore()
        {
            SetFirstExplore(!FirstExploreExpanded);
        }

        public void SetFirstExplore(bool expanded)
        {
            if (FirstExploreExpanded == expanded)
            {
                return;
            }

            FirstExploreExpanded = expanded;
            Changed?.Invoke();
        }

        public string ConsumeReveal()
        {
            var id = RevealRequest;
            RevealRequest = string.Empty;
            return id;
        }

        public List<GuideCardDef> VisibleCards(GuideTabKind tab)
        {
            var list = new List<GuideCardDef>();
            var all = GameGuideCatalog.All;
            for (var i = 0; i < all.Count; i++)
            {
                var card = all[i];
                if (card.Tab == tab && (tab != GuideTabKind.Resources || PassesFilter(card)))
                {
                    list.Add(card);
                }
            }

            return list;
        }

        public bool PassesFilter(GuideCardDef card)
        {
            switch (Filter)
            {
                case GuideFilter.Resources: return card.Kind == GuideCardKind.Resource;
                case GuideFilter.Facilities: return card.Kind == GuideCardKind.Facility;
                default: return true;
            }
        }

        private bool IsVisible(string cardId)
        {
            return GameGuideCatalog.TryGet(cardId, out var card)
                && (card.Tab != GuideTabKind.Resources || PassesFilter(card));
        }

        private GuideCardDef FirstVisible(GuideTabKind tab)
        {
            var list = VisibleCards(tab);
            return list.Count > 0 ? list[0] : null;
        }
    }
}

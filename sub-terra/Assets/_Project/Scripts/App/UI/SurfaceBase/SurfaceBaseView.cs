using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.UI.SurfaceBase
{
    /// <summary>Surface Base 표시. 경제 수치는 Economy/Progression 패널이 담당한다.</summary>
    public sealed class SurfaceBaseView : MonoBehaviour, ISurfaceBaseView
    {
        [SerializeField] private TMP_Text goalsText;
        [SerializeField] private TMP_Text deepZoneText;
        [SerializeField] private TMP_Text recentRunText;
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private Button exploreButton;
        [SerializeField] private Button refreshButton;

        public event Action ExploreClicked;
        public event Action RefreshClicked;

        private void OnEnable()
        {
            exploreButton?.onClick.AddListener(OnExplore);
            refreshButton?.onClick.AddListener(OnRefresh);
        }

        private void OnDisable()
        {
            exploreButton?.onClick.RemoveListener(OnExplore);
            refreshButton?.onClick.RemoveListener(OnRefresh);
        }

        public void SetGoals(int completedObjectives, string summary)
        {
            if (goalsText != null)
            {
                goalsText.text = summary ?? ("목표 " + completedObjectives);
            }
        }

        public void SetDeepZoneLock(bool unlocked, string reason)
        {
            if (deepZoneText != null)
            {
                deepZoneText.text = unlocked
                    ? "심층 구역: 해금"
                    : "심층 잠금: " + (reason ?? string.Empty);
            }
        }

        public void SetRecentRun(int depth, bool isSafe, string structural, string gas)
        {
            if (recentRunText != null)
            {
                recentRunText.text =
                    "최근 탐사 깊이 " + depth
                    + " / " + (isSafe ? "안전" : "위험")
                    + " / 구조 " + structural
                    + " / 가스 " + gas;
            }
        }

        public void SetExplorationBusy(bool busy)
        {
            if (exploreButton != null)
            {
                exploreButton.interactable = !busy;
            }
        }

        public void SetMessage(string message)
        {
            if (messageText != null)
            {
                messageText.text = message ?? string.Empty;
            }
        }

        public bool HasRequiredReferences()
        {
            return goalsText != null
                && deepZoneText != null
                && recentRunText != null
                && messageText != null
                && exploreButton != null;
        }

        private void OnExplore() => ExploreClicked?.Invoke();
        private void OnRefresh() => RefreshClicked?.Invoke();
    }
}
